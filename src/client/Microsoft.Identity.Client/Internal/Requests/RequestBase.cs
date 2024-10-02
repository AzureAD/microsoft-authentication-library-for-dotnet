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
using Microsoft.Identity.Client.Internal.Broker;
using System.Runtime.ConstrainedExecution;
using Microsoft.Identity.Client.AuthScheme;

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

        protected abstract Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken);

        public async Task<AuthenticationResult> RunAsync(CancellationToken cancellationToken = default)
        {
            ApiEvent apiEvent = null;

            var measureTelemetryDurationResult = StopwatchService.MeasureCodeBlock(() =>
            {
                apiEvent = InitializeApiEvent(AuthenticationRequestParameters.Account?.HomeAccountId?.Identifier);
                AuthenticationRequestParameters.RequestContext.ApiEvent = apiEvent;
            });

            try
            {
                AuthenticationResult authenticationResult = null;
                var measureDurationResult = await StopwatchService.MeasureCodeBlockAsync(async () =>
                {
                    AuthenticationRequestParameters.LogParameters();
                    LogRequestStarted(AuthenticationRequestParameters);

                    authenticationResult = await ExecuteAsync(cancellationToken).ConfigureAwait(false);
                    LogReturnedToken(authenticationResult);
                }).ConfigureAwait(false);

                UpdateTelemetry(measureDurationResult.Milliseconds + measureTelemetryDurationResult.Milliseconds, apiEvent, authenticationResult);
                LogMetricsFromAuthResult(authenticationResult, AuthenticationRequestParameters.RequestContext.Logger);
                LogSuccessTelemetryToOtel(authenticationResult, apiEvent, measureDurationResult.Microseconds);

                return authenticationResult;
            }
            catch (MsalException ex)
            {
                apiEvent.ApiErrorCode = ex.ErrorCode;
                if (string.IsNullOrWhiteSpace(ex.CorrelationId))
                {
                    ex.CorrelationId = AuthenticationRequestParameters.CorrelationId.ToString();
                }
                AuthenticationRequestParameters.RequestContext.Logger.ErrorPii(ex);

                LogFailureTelemetryToOtel(ex.ErrorCode, apiEvent, apiEvent.CacheInfo);
                throw;
            }
            catch (Exception ex)
            {
                apiEvent.ApiErrorCode = ex.GetType().Name;
                AuthenticationRequestParameters.RequestContext.Logger.ErrorPii(ex);

                LogFailureTelemetryToOtel(ex.GetType().Name, apiEvent, apiEvent.CacheInfo);
                throw;
            }           
        }

        private void LogSuccessTelemetryToOtel(AuthenticationResult authenticationResult, ApiEvent apiEvent, long durationInUs)
        {
            // Log metrics
            ServiceBundle.PlatformProxy.OtelInstrumentation.LogSuccessMetrics(
                        ServiceBundle.PlatformProxy.GetProductName(),
                        apiEvent.ApiId,
                        apiEvent.CallerSdkApiId,
                        apiEvent.CallerSdkVersion,
                        GetCacheLevel(authenticationResult),
                        durationInUs,
                        authenticationResult.AuthenticationResultMetadata,
                        AuthenticationRequestParameters.RequestContext.Logger);
        }

        private void LogFailureTelemetryToOtel(string errorCodeToLog, ApiEvent apiEvent, CacheRefreshReason cacheRefreshReason)
        {
            // Log metrics
            ServiceBundle.PlatformProxy.OtelInstrumentation.LogFailureMetrics(
                        ServiceBundle.PlatformProxy.GetProductName(),
                        errorCodeToLog,
                        apiEvent.ApiId,
                        apiEvent.CallerSdkApiId, 
                        apiEvent.CallerSdkVersion,
                        cacheRefreshReason);
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
                        string scopeToAppend = splitString.Length > 1 ? splitString[1].TrimStart('/') + " " : splitString.FirstOrDefault();
                        stringBuilder.Append(scopeToAppend);
                    }

                    scopes = stringBuilder.ToString().TrimEnd(' ');
                }
                else
                {
                    scopes = AuthenticationRequestParameters.Scope.AsSingleString();
                }
            }

            return new(resource, scopes);
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

        private static void LogMetricsFromAuthResult(AuthenticationResult authenticationResult, ILoggerAdapter logger)
        {
            if (logger.IsLoggingEnabled(LogLevel.Always))
            {
                var metadata = authenticationResult.AuthenticationResultMetadata;
                logger.Always(
                    $"""
                     
                     [LogMetricsFromAuthResult] Cache Refresh Reason: {metadata.CacheRefreshReason}
                     [LogMetricsFromAuthResult] DurationInCacheInMs: {metadata.DurationInCacheInMs}
                     [LogMetricsFromAuthResult] DurationTotalInMs: {metadata.DurationTotalInMs}
                     [LogMetricsFromAuthResult] DurationInHttpInMs: {metadata.DurationInHttpInMs}
                     """);
                logger.AlwaysPii($"[LogMetricsFromAuthResult] TokenEndpoint: {metadata.TokenEndpoint ?? ""}",
                                    "TokenEndpoint: ****");
            }
        }

        private void UpdateTelemetry(long elapsedMilliseconds, ApiEvent apiEvent, AuthenticationResult authenticationResult)
        {
            authenticationResult.AuthenticationResultMetadata.DurationTotalInMs = elapsedMilliseconds;
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

            apiEvent.IsTokenCacheSerialized =
                AuthenticationRequestParameters.CacheSessionManager.TokenCacheInternal.IsAppSubscribedToSerializationEvents();

            apiEvent.IsLegacyCacheEnabled =
                AuthenticationRequestParameters.RequestContext.ServiceBundle.Config.LegacyCacheCompatibilityEnabled;

            apiEvent.CacheInfo = CacheRefreshReason.NotApplicable;
            apiEvent.TokenType = (TokenType)AuthenticationRequestParameters.AuthenticationScheme.TelemetryTokenType;
            apiEvent.AssertionType = GetAssertionType();

            UpdateCallerSdkDetails(apiEvent);

            // Give derived classes the ability to add or modify fields in the telemetry as needed.
            EnrichTelemetryApiEvent(apiEvent);

            return apiEvent;
        }

        private void UpdateCallerSdkDetails(ApiEvent apiEvent)
        {
            string callerSdkId;
            string callerSdkVer;

            // Check if ExtraQueryParameters contains caller-sdk-id and caller-sdk-ver
            if (AuthenticationRequestParameters.ExtraQueryParameters.TryGetValue("caller-sdk-id", out callerSdkId))
            {
                AuthenticationRequestParameters.ExtraQueryParameters.Remove("caller-sdk-id");
            } 
            else
            {
                callerSdkId = AuthenticationRequestParameters.RequestContext.ServiceBundle.Config.ClientName;
            }
            
            if (AuthenticationRequestParameters.ExtraQueryParameters.TryGetValue("caller-sdk-ver", out callerSdkVer))
            {
                AuthenticationRequestParameters.ExtraQueryParameters.Remove("caller-sdk-ver");
            }
            else
            {
                callerSdkVer = AuthenticationRequestParameters.RequestContext.ServiceBundle.Config.ClientVersion;
            }

            apiEvent.CallerSdkApiId = callerSdkId == null ? null : callerSdkId.Substring(0, Math.Min(callerSdkId.Length, Constants.CallerSdkIdMaxLength));
            apiEvent.CallerSdkVersion = callerSdkVer == null ? null : callerSdkVer.Substring(0, Math.Min(callerSdkVer.Length, Constants.CallerSdkVersionMaxLength));
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

            InjectPcaSsoPolicyHeader(tokenClient);

            return tokenClient.SendTokenRequestAsync(
                additionalBodyParameters,
                scopes,
                tokenEndpoint,
                cancellationToken);
        }

        private void InjectPcaSsoPolicyHeader(TokenClient tokenClient)
        {
            if (ServiceBundle.Config.IsPublicClient && ServiceBundle.Config.IsWebviewSsoPolicyEnabled)
            {
                IBroker broker = ServiceBundle.Config.BrokerCreatorFunc(
                    null,
                    ServiceBundle.Config,
                    AuthenticationRequestParameters.RequestContext.Logger);

                var ssoPolicyHeaders = broker.GetSsoPolicyHeaders();
                foreach (KeyValuePair<string, string> kvp in ssoPolicyHeaders)
                {
                    tokenClient.AddHeaderToClient(kvp.Key, kvp.Value);
                }
            }
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

            return new KeyValuePair<string, string>(Constants.CcsRoutingHintHeader, OidCcsHeader);
        }

        private void LogRequestStarted(AuthenticationRequestParameters authenticationRequestParameters)
        {
            if (authenticationRequestParameters.RequestContext.Logger.IsLoggingEnabled(LogLevel.Info))
            {
                string scopes = authenticationRequestParameters.Scope.AsSingleString();
                var type = GetType().Name;
                var messageWithPii = $"=== Token Acquisition ({type}) started:\n\tAuthority: {authenticationRequestParameters.AuthorityInfo?.CanonicalAuthority}\n\tScope: {scopes}\n\tClientId: {authenticationRequestParameters.AppConfig.ClientId}\n\t";

                var messageWithoutPii = $"=== Token Acquisition ({type}) started:\n\t Scopes: {scopes}";

                if (authenticationRequestParameters.AuthorityInfo != null &&
                    KnownMetadataProvider.IsKnownEnvironment(authenticationRequestParameters.AuthorityInfo?.Host))
                {
                    messageWithoutPii += $"\n\tAuthority Host: {authenticationRequestParameters.AuthorityInfo?.Host}";
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
