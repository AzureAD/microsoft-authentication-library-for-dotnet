// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.IdentityModel.Abstractions;
using Microsoft.Identity.Client.TelemetryCore.TelemetryClient;
using Microsoft.Identity.Client.TelemetryCore.OpenTelemetry;

namespace Microsoft.Identity.Client.Internal.Requests
{
    /// <summary>
    /// Base class for all flows. Use by implementing <see cref="ExecuteAsync(CancellationToken)"/>
    /// and optionally calling protected helper methods such as SendTokenRequestAsync, which know
    /// how to use all params when making the request.
    /// </summary>
    internal abstract class RequestBase
    {
        internal AuthenticationRequestParameters AuthenticationRequestParameters { get; }
        internal ICacheSessionManager CacheManager => AuthenticationRequestParameters.CacheSessionManager;
        internal IServiceBundle ServiceBundle { get; }

        protected RequestBase(
            IServiceBundle serviceBundle,
            AuthenticationRequestParameters authenticationRequestParameters,
            IAcquireTokenParameters acquireTokenParameters)
        {
            ServiceBundle = serviceBundle ??
                throw new ArgumentNullException(nameof(serviceBundle));

            AuthenticationRequestParameters = authenticationRequestParameters ??
                throw new ArgumentNullException(nameof(authenticationRequestParameters));

            if (acquireTokenParameters == null)
            {
                throw new ArgumentNullException(nameof(acquireTokenParameters));
            }

            ValidateScopeInput(authenticationRequestParameters.Scope);
            acquireTokenParameters.LogParameters(AuthenticationRequestParameters.RequestContext.Logger);
        }

        /// <summary>
        /// Return a custom set of scopes to override the default MSAL logic of merging
        /// input scopes with reserved scopes (openid, profile etc.)
        /// Leave as is / return null otherwise
        /// </summary>
        protected virtual SortedSet<string> GetOverriddenScopes(ISet<string> inputScopes)
        {
            return null;
        }

        private void ValidateScopeInput(ISet<string> scopesToValidate)
        {
            if (scopesToValidate.Contains(AuthenticationRequestParameters.AppConfig.ClientId))
            {
                throw new ArgumentException("API does not accept client id as a user-provided scope");
            }
        }

        protected abstract Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken);

        public async Task<AuthenticationResult> RunAsync(CancellationToken cancellationToken = default)
        {
            Stopwatch sw = Stopwatch.StartNew();

            ApiEvent apiEvent = InitializeApiEvent(AuthenticationRequestParameters.Account?.HomeAccountId?.Identifier);
            AuthenticationRequestParameters.RequestContext.ApiEvent = apiEvent;
            MsalTelemetryEventDetails telemetryEventDetails = new MsalTelemetryEventDetails(TelemetryConstants.AcquireTokenEventName);
            ITelemetryClient[] telemetryClients = AuthenticationRequestParameters.RequestContext.ServiceBundle.Config.TelemetryClients;

            using (AuthenticationRequestParameters.RequestContext.CreateTelemetryHelper(apiEvent))
            {
                try
                {
                    AuthenticationRequestParameters.LogParameters();
                    LogRequestStarted(AuthenticationRequestParameters);

                    AuthenticationResult authenticationResult = await ExecuteAsync(cancellationToken).ConfigureAwait(false);
                    LogReturnedToken(authenticationResult);
                    UpdateTelemetry(sw, apiEvent, authenticationResult);
                    LogMetricsFromAuthResult(authenticationResult, AuthenticationRequestParameters.RequestContext.Logger);
                    LogSuccessfulTelemetryToClient(authenticationResult, telemetryEventDetails, telemetryClients);
                    LogMsalSuccessTelemetryToOtel(authenticationResult, apiEvent.ApiId.ToString(), sw.ElapsedTicks / (TimeSpan.TicksPerMillisecond / 1000));

                    return authenticationResult;
                }
                catch (MsalException ex)
                {
                    apiEvent.ApiErrorCode = ex.ErrorCode;
                    AuthenticationRequestParameters.RequestContext.Logger.ErrorPii(ex);
                    LogMsalErrorTelemetryToClient(ex, telemetryEventDetails, telemetryClients);

                    LogMsalFailedTelemetryToOtel(ex.ErrorCode);
                    throw;
                }
                catch (Exception ex)
                {
                    apiEvent.ApiErrorCode = ex.GetType().Name;
                    AuthenticationRequestParameters.RequestContext.Logger.ErrorPii(ex);
                    LogMsalErrorTelemetryToClient(ex, telemetryEventDetails, telemetryClients);
                    
                    LogMsalFailedTelemetryToOtel(ex.GetType().Name);
                    throw;
                }
                finally
                {
                    telemetryClients.TrackEvent(telemetryEventDetails);
                }
            }
        }

        private void LogMsalSuccessTelemetryToOtel(AuthenticationResult authenticationResult, string apiId, long durationInUs)
        {
            // Log metrics
            ServiceBundle.PlatformProxy.OtelInstrumentation.LogSuccessMetrics(
                        ServiceBundle.PlatformProxy.GetProductName(),
                        apiId,
                        GetCacheLevel(authenticationResult).ToString(),
                        durationInUs,
                        authenticationResult.AuthenticationResultMetadata,
                        AuthenticationRequestParameters.RequestContext.Logger);
        }

        private void LogMsalFailedTelemetryToOtel(string errorCodeToLog)
        {
            // Log metrics
            ServiceBundle.PlatformProxy.OtelInstrumentation.LogFailedMetrics(
                        ServiceBundle.PlatformProxy.GetProductName(),
                        errorCodeToLog);
        }

        private void LogMsalErrorTelemetryToClient(Exception ex, MsalTelemetryEventDetails telemetryEventDetails, ITelemetryClient[] telemetryClients)
        {
            if (telemetryClients.HasEnabledClients(TelemetryConstants.AcquireTokenEventName))
            {
                telemetryEventDetails.SetProperty(TelemetryConstants.Succeeded, false);
                telemetryEventDetails.SetProperty(TelemetryConstants.ErrorMessage, ex.Message);

                if (ex is MsalClientException clientException)
                {
                    telemetryEventDetails.SetProperty(TelemetryConstants.ErrorCode, clientException.ErrorCode);
                    return;
                }

                if (ex is MsalServiceException serviceException)
                {
                    telemetryEventDetails.SetProperty(TelemetryConstants.ErrorCode, serviceException.ErrorCode);
                    telemetryEventDetails.SetProperty(TelemetryConstants.StsErrorCode, serviceException.ErrorCodes?.FirstOrDefault());
                    return;
                }

                telemetryEventDetails.SetProperty(TelemetryConstants.ErrorCode, ex.GetType().ToString());
            }
        }

        private void LogSuccessfulTelemetryToClient(AuthenticationResult authenticationResult, MsalTelemetryEventDetails telemetryEventDetails, ITelemetryClient[] telemetryClients)
        {
            if (telemetryClients.HasEnabledClients(TelemetryConstants.AcquireTokenEventName))
            {
                telemetryEventDetails.SetProperty(TelemetryConstants.CacheInfoTelemetry, Convert.ToInt64(authenticationResult.AuthenticationResultMetadata.CacheRefreshReason));
                telemetryEventDetails.SetProperty(TelemetryConstants.TokenSource, Convert.ToInt64(authenticationResult.AuthenticationResultMetadata.TokenSource));
                telemetryEventDetails.SetProperty(TelemetryConstants.Duration, authenticationResult.AuthenticationResultMetadata.DurationTotalInMs);
                telemetryEventDetails.SetProperty(TelemetryConstants.DurationInCache, authenticationResult.AuthenticationResultMetadata.DurationInCacheInMs);
                telemetryEventDetails.SetProperty(TelemetryConstants.DurationInHttp, authenticationResult.AuthenticationResultMetadata.DurationInHttpInMs);
                telemetryEventDetails.SetProperty(TelemetryConstants.Succeeded, true);
                telemetryEventDetails.SetProperty(TelemetryConstants.TokenType, (int)AuthenticationRequestParameters.RequestContext.ApiEvent.TokenType);
                telemetryEventDetails.SetProperty(TelemetryConstants.RemainingLifetime, (authenticationResult.ExpiresOn - DateTime.Now).TotalMilliseconds);
                telemetryEventDetails.SetProperty(TelemetryConstants.ActivityId, authenticationResult.CorrelationId);

                if (authenticationResult.AuthenticationResultMetadata.RefreshOn.HasValue)
                {
                    telemetryEventDetails.SetProperty(TelemetryConstants.RefreshOn, DateTimeHelpers.DateTimeToUnixTimestampMilliseconds(authenticationResult.AuthenticationResultMetadata.RefreshOn.Value));
                }
                telemetryEventDetails.SetProperty(TelemetryConstants.AssertionType, (int)AuthenticationRequestParameters.RequestContext.ApiEvent.AssertionType);
                telemetryEventDetails.SetProperty(TelemetryConstants.Endpoint, AuthenticationRequestParameters.Authority.AuthorityInfo.CanonicalAuthority.ToString());

                telemetryEventDetails.SetProperty(TelemetryConstants.CacheLevel, (int)authenticationResult.AuthenticationResultMetadata.CacheLevel);
              
                Tuple<string, string> resourceAndScopes  = ParseScopesForTelemetry();
                if (resourceAndScopes.Item1 != null)
                {
                    telemetryEventDetails.SetProperty(TelemetryConstants.Resource, resourceAndScopes.Item1);
                }

                if (resourceAndScopes.Item2 != null)
                {
                    telemetryEventDetails.SetProperty(TelemetryConstants.Scopes, resourceAndScopes.Item2);
                }
            }
        }

        private Tuple<string, string> ParseScopesForTelemetry()
        {
            string resource = null;
            string scopes = null;
            if (AuthenticationRequestParameters.Scope.Count > 0)
            {
                string firstScope = AuthenticationRequestParameters.Scope.First();

                if (Uri.IsWellFormedUriString(firstScope, UriKind.Absolute))
                {
                    Uri firstScopeAsUri = new Uri(firstScope);
                    resource = $"{firstScopeAsUri.Scheme}://{firstScopeAsUri.Host}";

                    StringBuilder stringBuilder = new StringBuilder();

                    foreach (string scope in AuthenticationRequestParameters.Scope)
                    {
                        var splitString = scope.Split(new[] { firstScopeAsUri.Host }, StringSplitOptions.None);
                        string scopeToAppend = splitString.Count() > 1 ? splitString[1].TrimStart('/') + " " : splitString.FirstOrDefault();
                        stringBuilder.Append(scopeToAppend);
                    }

                    scopes = stringBuilder.ToString().TrimEnd(' ');
                }
                else
                {
                    scopes = AuthenticationRequestParameters.Scope.AsSingleString();
                }
            }

            return new Tuple<string, string>(resource, scopes);
        }

        private CacheLevel GetCacheLevel(AuthenticationResult authenticationResult)
        {
            if (authenticationResult.AuthenticationResultMetadata.TokenSource == TokenSource.Cache) //Check if token source is cache
            {
                if (AuthenticationRequestParameters.RequestContext.ApiEvent.CacheLevel > CacheLevel.Unknown) //Check if cache has indicated which level was used
                {
                    return AuthenticationRequestParameters.RequestContext.ApiEvent.CacheLevel;
                }

                //If no level was used, set to unknown
                return CacheLevel.Unknown;
            }

            return CacheLevel.None;
        }

        private void LogMetricsFromAuthResult(AuthenticationResult authenticationResult, ILoggerAdapter logger)
        {
            if (logger.IsLoggingEnabled(LogLevel.Always))
            {
                var sb = new StringBuilder(250);
                sb.AppendLine();
                sb.Append("[LogMetricsFromAuthResult] Cache Refresh Reason: ");
                sb.AppendLine(authenticationResult.AuthenticationResultMetadata.CacheRefreshReason.ToString());
                sb.Append("[LogMetricsFromAuthResult] DurationInCacheInMs: ");
                sb.AppendLine(authenticationResult.AuthenticationResultMetadata.DurationInCacheInMs.ToString());
                sb.Append("[LogMetricsFromAuthResult] DurationTotalInMs: ");
                sb.AppendLine(authenticationResult.AuthenticationResultMetadata.DurationTotalInMs.ToString());
                sb.Append("[LogMetricsFromAuthResult] DurationInHttpInMs: ");
                sb.AppendLine(authenticationResult.AuthenticationResultMetadata.DurationInHttpInMs.ToString());
                logger.Always(sb.ToString());
                logger.AlwaysPii($"[LogMetricsFromAuthResult] TokenEndpoint: {authenticationResult.AuthenticationResultMetadata.TokenEndpoint ?? ""}",
                                    "TokenEndpoint: ****");
            }
        }

        private void UpdateTelemetry(Stopwatch sw, ApiEvent apiEvent, AuthenticationResult authenticationResult)
        {
            sw.Stop();
            authenticationResult.AuthenticationResultMetadata.DurationTotalInMs = sw.ElapsedMilliseconds;
            authenticationResult.AuthenticationResultMetadata.DurationInHttpInMs = apiEvent.DurationInHttpInMs;
            authenticationResult.AuthenticationResultMetadata.DurationInCacheInMs = apiEvent.DurationInCacheInMs;
            authenticationResult.AuthenticationResultMetadata.TokenEndpoint = apiEvent.TokenEndpoint;
            authenticationResult.AuthenticationResultMetadata.CacheRefreshReason = apiEvent.CacheInfo;
            authenticationResult.AuthenticationResultMetadata.CacheLevel = GetCacheLevel(authenticationResult);
            authenticationResult.AuthenticationResultMetadata.Telemetry = apiEvent.MsalRuntimeTelemetry;
            authenticationResult.AuthenticationResultMetadata.RegionDetails = CreateRegionDetails(apiEvent);

            Metrics.IncrementTotalDurationInMs(authenticationResult.AuthenticationResultMetadata.DurationTotalInMs);
        }

        protected virtual void EnrichTelemetryApiEvent(ApiEvent apiEvent)
        {
            // In base classes have them override this to add their properties/fields to the event.
        }

        private ApiEvent InitializeApiEvent(string accountId)
        {
            ApiEvent apiEvent = new ApiEvent(AuthenticationRequestParameters.RequestContext.CorrelationId)
            {
                ApiId = AuthenticationRequestParameters.ApiId,
            };

            apiEvent.IsTokenCacheSerialized = AuthenticationRequestParameters.CacheSessionManager.TokenCacheInternal.IsExternalSerializationConfiguredByUser();
            apiEvent.IsLegacyCacheEnabled = AuthenticationRequestParameters.RequestContext.ServiceBundle.Config.LegacyCacheCompatibilityEnabled;
            apiEvent.CacheInfo = CacheRefreshReason.NotApplicable;
            apiEvent.TokenType = AuthenticationRequestParameters.AuthenticationScheme.TelemetryTokenType;
            apiEvent.AssertionType = GetAssertionType();

            // Give derived classes the ability to add or modify fields in the telemetry as needed.
            EnrichTelemetryApiEvent(apiEvent);

            return apiEvent;
        }

        private AssertionType GetAssertionType()
        {
            if (ServiceBundle.Config.IsManagedIdentity ||
                ServiceBundle.Config.AppTokenProvider != null)
            {
                return AssertionType.ManagedIdentity;
            }

            if (ServiceBundle.Config.ClientCredential != null)
            {
                if (ServiceBundle.Config.ClientCredential.AssertionType == AssertionType.CertificateWithoutSni)
                {
                    if (ServiceBundle.Config.SendX5C)
                    {
                        return AssertionType.CertificateWithSni;
                    }

                    return AssertionType.CertificateWithoutSni;
                }

                return ServiceBundle.Config.ClientCredential.AssertionType;
            }

            return AssertionType.None;
        }

        protected async Task<AuthenticationResult> CacheTokenResponseAndCreateAuthenticationResultAsync(MsalTokenResponse msalTokenResponse)
        {
            // developer passed in user object.
            AuthenticationRequestParameters.RequestContext.Logger.Info("Checking client info returned from the server..");

            ClientInfo fromServer = null;

            if (!AuthenticationRequestParameters.IsClientCredentialRequest &&
                AuthenticationRequestParameters.ApiId != ApiEvent.ApiIds.AcquireTokenForSystemAssignedManagedIdentity &&
                AuthenticationRequestParameters.ApiId != ApiEvent.ApiIds.AcquireTokenForUserAssignedManagedIdentity &&
                AuthenticationRequestParameters.ApiId != ApiEvent.ApiIds.AcquireTokenByRefreshToken &&
                AuthenticationRequestParameters.AuthorityInfo.AuthorityType != AuthorityType.Adfs &&
                !(msalTokenResponse.ClientInfo is null))
            {
                //client_info is not returned from client credential and managed identity flows because there is no user present.
                fromServer = ClientInfo.CreateFromJson(msalTokenResponse.ClientInfo);
            }

            
            ValidateAccountIdentifiers(fromServer);

            AuthenticationRequestParameters.RequestContext.Logger.Info("Saving token response to cache..");

            var tuple = await CacheManager.SaveTokenResponseAsync(msalTokenResponse).ConfigureAwait(false);
            var atItem = tuple.Item1;
            var idtItem = tuple.Item2;
            Account account = tuple.Item3;

            return new AuthenticationResult(
                atItem,
                idtItem,
                AuthenticationRequestParameters.AuthenticationScheme,
                AuthenticationRequestParameters.RequestContext.CorrelationId,
                msalTokenResponse.TokenSource,
                AuthenticationRequestParameters.RequestContext.ApiEvent,
                account,
                msalTokenResponse.SpaAuthCode, 
                msalTokenResponse.CreateExtensionDataStringMap());
        }

        protected virtual void ValidateAccountIdentifiers(ClientInfo fromServer)
        {
            //No Op
        }

        protected Task ResolveAuthorityAsync()
        {
            return AuthenticationRequestParameters.AuthorityManager.RunInstanceDiscoveryAndValidationAsync();
        }

        internal async Task<MsalTokenResponse> SendTokenRequestAsync(
            IDictionary<string, string> additionalBodyParameters,
            CancellationToken cancellationToken)
        {
            var tokenEndpoint = await AuthenticationRequestParameters.Authority.GetTokenEndpointAsync(AuthenticationRequestParameters.RequestContext).ConfigureAwait(false);

            var tokenResponse = await SendTokenRequestAsync(
                tokenEndpoint,
                additionalBodyParameters,
                cancellationToken).ConfigureAwait(false);

            Metrics.IncrementTotalAccessTokensFromIdP();
            return tokenResponse;
        }

        protected Task<MsalTokenResponse> SendTokenRequestAsync(
            string tokenEndpoint,
            IDictionary<string, string> additionalBodyParameters,
            CancellationToken cancellationToken)
        {
            string scopes = GetOverriddenScopes(AuthenticationRequestParameters.Scope).AsSingleString();
            var tokenClient = new TokenClient(AuthenticationRequestParameters);

            var CcsHeader = GetCcsHeader(additionalBodyParameters);
            if (CcsHeader != null && !string.IsNullOrEmpty(CcsHeader.Value.Key))
            {
                tokenClient.AddHeaderToClient(CcsHeader.Value.Key, CcsHeader.Value.Value);
            }

            return tokenClient.SendTokenRequestAsync(
                additionalBodyParameters,
                scopes,
                tokenEndpoint,
                cancellationToken);
        }

        //The AAD backup authentication system header is used by the AAD backup authentication system service
        //to help route requests to resources in Azure during requests to speed up authentication.
        //It consists of either the ObjectId.TenantId or the upn of the account signing in.
        //See https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2525
        protected virtual KeyValuePair<string, string>? GetCcsHeader(IDictionary<string, string> additionalBodyParameters)
        {
            if (AuthenticationRequestParameters?.Account?.HomeAccountId != null)
            {
                if (!String.IsNullOrEmpty(AuthenticationRequestParameters.Account.HomeAccountId.Identifier))
                {
                    var userObjectId = AuthenticationRequestParameters.Account.HomeAccountId.ObjectId;
                    var userTenantID = AuthenticationRequestParameters.Account.HomeAccountId.TenantId;
                    string OidCcsHeader = CoreHelpers.GetCcsClientInfoHint(userObjectId, userTenantID);

                    return new KeyValuePair<string, string>(Constants.CcsRoutingHintHeader, OidCcsHeader);
                }

                if (!String.IsNullOrEmpty(AuthenticationRequestParameters.Account.Username))
                {
                    return GetCcsUpnHeader(AuthenticationRequestParameters.Account.Username);
                }
            }

            if (additionalBodyParameters.TryGetValue(OAuth2Parameter.Username, out string username))
            {
                return GetCcsUpnHeader(username);
            }

            if (!String.IsNullOrEmpty(AuthenticationRequestParameters.LoginHint))
            {
                return GetCcsUpnHeader(AuthenticationRequestParameters.LoginHint);
            }

            return null;
        }

        protected KeyValuePair<string, string>? GetCcsUpnHeader(string upnHeader)
        {
            if (AuthenticationRequestParameters.Authority.AuthorityInfo.AuthorityType == AuthorityType.B2C) 
            {
                return null;
            }

            string OidCcsHeader = CoreHelpers.GetCcsUpnHint(upnHeader);

            return new KeyValuePair<string, string>(Constants.CcsRoutingHintHeader, OidCcsHeader) as KeyValuePair<string, string>?;
        }

        private void LogRequestStarted(AuthenticationRequestParameters authenticationRequestParameters)
        {
            if (authenticationRequestParameters.RequestContext.Logger.IsLoggingEnabled(LogLevel.Info))
            {
                string logFormat = "=== Token Acquisition ({3}) started:\n\tAuthority: {0}\n\tScope: {1}\n\tClientId: {2}\n\t";
                string scopes = authenticationRequestParameters.Scope.AsSingleString();
                string messageWithPii = string.Format(
                    CultureInfo.InvariantCulture,
                    logFormat,
                    authenticationRequestParameters.AuthorityInfo?.CanonicalAuthority,
                    scopes,
                    authenticationRequestParameters.AppConfig.ClientId,
                    GetType().Name);

                string messageWithoutPii = string.Format(
                    CultureInfo.InvariantCulture,
                    "=== Token Acquisition ({0}) started:\n\t Scopes: {1}",
                    GetType().Name,
                    scopes);

                if (authenticationRequestParameters.AuthorityInfo != null &&
                    KnownMetadataProvider.IsKnownEnvironment(authenticationRequestParameters.AuthorityInfo?.Host))
                {
                    messageWithoutPii += string.Format(
                        CultureInfo.CurrentCulture,
                        "\n\tAuthority Host: {0}",
                        authenticationRequestParameters.AuthorityInfo?.Host);
                }

                authenticationRequestParameters.RequestContext.Logger.InfoPii(messageWithPii, messageWithoutPii);
            }

            if (authenticationRequestParameters.AppConfig.IsConfidentialClient &&
                !authenticationRequestParameters.IsClientCredentialRequest &&
                !CacheManager.TokenCacheInternal.IsAppSubscribedToSerializationEvents())
            {
                authenticationRequestParameters.RequestContext.Logger.Warning(
                    "Only in-memory caching is used. The cache is not persisted and will be lost if the machine is restarted. It also does not scale for a web app or web API, where the number of users can grow large. In production, web apps and web APIs should use distributed caching like Redis. See https://aka.ms/msal-net-cca-token-cache-serialization");
            }
        }

        private void LogReturnedToken(AuthenticationResult result)
        {
            if (result.AccessToken != null &&
                AuthenticationRequestParameters.RequestContext.Logger.IsLoggingEnabled(LogLevel.Info))
            {
                string scopes = string.Join(" ", result.Scopes);
                
                AuthenticationRequestParameters.RequestContext.Logger.Info("\n\t=== Token Acquisition finished successfully:");
                AuthenticationRequestParameters.RequestContext.Logger.InfoPii(
                       () => $" AT expiration time: {result.ExpiresOn}, scopes: {scopes}. " +
                            $"source: {result.AuthenticationResultMetadata.TokenSource}",
                       () => $" AT expiration time: {result.ExpiresOn}, scopes: {scopes}. " +
                            $"source: {result.AuthenticationResultMetadata.TokenSource}");

                if (result.AuthenticationResultMetadata.TokenSource != TokenSource.Cache)
                {
                    Uri canonicalAuthority = AuthenticationRequestParameters.AuthorityInfo.CanonicalAuthority;

                    AuthenticationRequestParameters.RequestContext.Logger.InfoPii(
                        () => $"Fetched access token from host {canonicalAuthority.Host}. Endpoint: {canonicalAuthority}. ",
                        () => $"Fetched access token from host {canonicalAuthority.Host}. ");
                }
            }
        }

        internal async Task<AuthenticationResult> HandleTokenRefreshErrorAsync(MsalServiceException e, MsalAccessTokenCacheItem cachedAccessTokenItem)
        {
            var logger = AuthenticationRequestParameters.RequestContext.Logger;
            
            logger.Warning($"Fetching a new AT failed. Is exception retry-able? {e.IsRetryable}. Is there an AT in the cache that is usable? {cachedAccessTokenItem != null}");

            if (cachedAccessTokenItem != null && e.IsRetryable)
            {
                logger.Info("Returning existing access token. It is not expired, but should be refreshed. ");

                var idToken = await CacheManager.GetIdTokenCacheItemAsync(cachedAccessTokenItem).ConfigureAwait(false);
                var account = await CacheManager.GetAccountAssociatedWithAccessTokenAsync(cachedAccessTokenItem).ConfigureAwait(false);

                return new AuthenticationResult(
                    cachedAccessTokenItem,
                    idToken,
                    AuthenticationRequestParameters.AuthenticationScheme,
                    AuthenticationRequestParameters.RequestContext.CorrelationId,
                    TokenSource.Cache,
                    AuthenticationRequestParameters.RequestContext.ApiEvent,
                    account, 
                    spaAuthCode: null, 
                    additionalResponseParameters: null);
            }

            logger.Warning("Either the exception does not indicate a problem with AAD or the token cache does not have an AT that is usable. ");
            throw e;
        }

        /// <summary>
        /// Creates the region Details
        /// </summary>
        /// <param name="apiEvent"></param>
        /// <returns></returns>
        private static RegionDetails CreateRegionDetails(ApiEvent apiEvent)
        {
            return new RegionDetails(
                apiEvent.RegionOutcome,
                apiEvent.RegionUsed,
                apiEvent.RegionDiscoveryFailureReason);
        }
    }
}
