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
using Microsoft.Identity.Client.Extensibility;

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

        /// <summary>
        /// Returns <c>true</c> if the internal token cache is disabled via <c>CacheOptions.DisableInternalCacheOptions</c>.
        /// </summary>
        protected bool IsInternalCacheDisabled =>
            CacheOptions.IsDisabledFor(ServiceBundle.Config.AccessorOptions);

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

            var requestStopwatch = Stopwatch.StartNew();
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

                // Compute the total duration once so the value stored on the exception metadata matches
                // the value logged to OpenTelemetry (the stopwatch keeps running, so re-reading it drifts).
                long totalDurationInMs = requestStopwatch.ElapsedMilliseconds + measureTelemetryDurationResult.Milliseconds;

                ex.AuthenticationResultMetadata = CreateFailureMetadata(apiEvent, totalDurationInMs);

                MsalServiceException serviceException = ex as MsalServiceException;
                int httpStatusCode = serviceException?.StatusCode ?? 0;

                LogFailureTelemetryToOtel(
                    ex.ErrorCode,
                    apiEvent,
                    apiEvent.CacheInfo,
                    httpStatusCode,
                    totalDurationInMs,
                    exception: ex,
                    rawStsErrorCode: serviceException?.ErrorCodes?.FirstOrDefault());
                throw;
            }
            catch (Exception ex)
            {
                apiEvent.ApiErrorCode = ex.GetType().Name;
                AuthenticationRequestParameters.RequestContext.Logger.ErrorPii(ex);

                // Compute the total duration once so the value on the synthesized metadata matches the
                // value logged to OpenTelemetry (the stopwatch keeps running, so re-reading it drifts).
                long totalDurationInMs = requestStopwatch.ElapsedMilliseconds + measureTelemetryDurationResult.Milliseconds;

                AuthenticationResultMetadata failureMetadata = CreateFailureMetadata(apiEvent, totalDurationInMs);

                // Expose the failure metadata on the ORIGINAL exception via its Data bag so downstream
                // header-creation providers - which catch the raw non-MSAL exception, not the enricher
                // wrapper - can surface token-acquisition diagnostics (Bug 3696194). The value is the same
                // strongly-typed object MSAL builds for the success path, so consumers reuse their mapper.
                // Guarded because a derived exception may expose a null or read-only Data bag, and telemetry
                // plumbing must never throw here and mask the caller's original exception. On .NET Framework /
                // netstandard the Data bag also rejects non-serializable values, so AuthenticationResultMetadata
                // is marked [Serializable] on those targets (see AuthenticationResultMetadata.cs).
                if (ex.Data is { IsReadOnly: false })
                {
                    ex.Data[MsalException.AuthenticationResultMetadataKey] = failureMetadata;
                }

                // The original exception is re-thrown below; MSAL never surfaces this wrapper. It exists only
                // so the OpenTelemetry tag enricher observes a populated ExecutionResult.Exception (carrying
                // failure metadata) for non-MSAL failures, mirroring the MsalException path above. The
                // originating exception's type is captured as the ErrorCode and it is preserved as the
                // InnerException so consumers retain full fidelity. Fall back to the type name when
                // FullName is null (some generic/array types) or Message is empty/whitespace, because the
                // MsalException ctor rejects a null/empty errorCode or errorMessage - without the fallback
                // that ArgumentNullException would replace the original exception we re-throw below.
                string enricherErrorCode = ex.GetType().FullName ?? ex.GetType().Name;
                string enricherErrorMessage = string.IsNullOrWhiteSpace(ex.Message) ? ex.GetType().Name : ex.Message;

                MsalException enricherException = new MsalException(enricherErrorCode, enricherErrorMessage, ex)
                {
                    AuthenticationResultMetadata = failureMetadata,
                    CorrelationId = AuthenticationRequestParameters.CorrelationId.ToString(),
                };

                LogFailureTelemetryToOtel(
                    ex.GetType().Name,
                    apiEvent,
                    apiEvent.CacheInfo,
                    httpStatusCode: 0,
                    totalDurationInMs: totalDurationInMs,
                    exception: enricherException);
                throw;
            }
        }

        private void LogSuccessTelemetryToOtel(AuthenticationResult authenticationResult, ApiEvent apiEvent, long durationInUs)
        {
            CacheLevel cacheLevel = GetCacheLevel(authenticationResult);

            // Invoke the caller-supplied enricher once per acquisition and merge the resulting fixed set of
            // extra tags into every instrument below, so the delegate is not re-run per metric.
            IReadOnlyList<KeyValuePair<string, object>> extraTags = OtelEnrichmentHelper.MaterializeExtraTags(
                AuthenticationRequestParameters.OtelTagsEnricher,
                () => new ExecutionResult
                {
                    Successful = true,
                    Result = authenticationResult,
                    ClientCertificate = AuthenticationRequestParameters.ResolvedCertificate
                },
                AuthenticationRequestParameters.RequestContext.Logger);

            // Log metrics
            ServiceBundle.PlatformProxy.OtelInstrumentation.LogSuccessMetrics(
                        ServiceBundle.PlatformProxy.GetProductName(),
                        apiEvent.ApiId,
                        apiEvent.CallerSdkApiId,
                        apiEvent.CallerSdkVersion,
                        cacheLevel,
                        durationInUs,
                        authenticationResult.AuthenticationResultMetadata,
                        AuthenticationRequestParameters.RequestContext.Logger,
                        authenticationResult.ExpiresOn,
                        extraTags);
        }

        private void LogFailureTelemetryToOtel(string errorCodeToLog, ApiEvent apiEvent, CacheRefreshReason cacheRefreshReason, int httpStatusCode, long totalDurationInMs, MsalException exception = null, string rawStsErrorCode = null)
        {
            // Invoke the caller-supplied enricher once per acquisition and merge the resulting fixed set of
            // extra tags into every instrument below, so the delegate is not re-run per metric.
            IReadOnlyList<KeyValuePair<string, object>> extraTags = OtelEnrichmentHelper.MaterializeExtraTags(
                AuthenticationRequestParameters.OtelTagsEnricher,
                () => new ExecutionResult
                {
                    Successful = false,
                    Exception = exception,
                    ClientCertificate = AuthenticationRequestParameters.ResolvedCertificate
                },
                AuthenticationRequestParameters.RequestContext.Logger);

            ServiceBundle.PlatformProxy.OtelInstrumentation.LogFailureMetrics(
                        ServiceBundle.PlatformProxy.GetProductName(),
                        errorCodeToLog,
                        apiEvent,
                        apiEvent.CallerSdkApiId,
                        apiEvent.CallerSdkVersion,
                        cacheRefreshReason,
                        apiEvent.TokenType,
                        httpStatusCode,
                        totalDurationInMs,
                        rawStsErrorCode,
                        AuthenticationRequestParameters.RequestContext.Logger,
                        extraTags);
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
            authenticationResult.AuthenticationResultMetadata.CachedAccessTokenCount = apiEvent.CachedAccessTokenCount;

            Metrics.IncrementTotalDurationInMs(authenticationResult.AuthenticationResultMetadata.DurationTotalInMs);
        }

        /// <summary>
        /// Builds the subset of <see cref="AuthenticationResultMetadata"/> that is available when a
        /// token request fails. Only values that were actually captured are populated; everything else
        /// is left at its default (0 / null). <see cref="AuthenticationResultMetadata.TokenSource"/> has
        /// no meaningful value on failure, so it stays at the constructor default and should not be relied on.
        /// </summary>
        internal static AuthenticationResultMetadata CreateFailureMetadata(ApiEvent apiEvent, long totalDurationInMs)
        {
            return new AuthenticationResultMetadata(TokenSource.IdentityProvider)
            {
                DurationTotalInMs = totalDurationInMs,
                DurationInHttpInMs = apiEvent.DurationInHttpInMs,
                DurationInCacheInMs = apiEvent.DurationInCacheInMs,
                CachedAccessTokenCount = apiEvent.CachedAccessTokenCount,
                CacheRefreshReason = apiEvent.CacheInfo,
                TokenEndpoint = apiEvent.TokenEndpoint,
                RegionDetails = CreateRegionDetails(apiEvent),
            };
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
            apiEvent.TokenType = AuthenticationRequestParameters.AuthenticationScheme.TelemetryTokenType;
            apiEvent.AssertionType = GetAssertionType();

            if (AuthenticationRequestParameters.ExtraQueryParameters.TryGetValue(Constants.ManagedCertKey, out string managedCertValue) 
                && !string.IsNullOrEmpty(managedCertValue))
            {
                apiEvent.IsManagedCertUsed = managedCertValue[0];
            }
            AuthenticationRequestParameters.ExtraQueryParameters.Remove(Constants.ManagedCertKey);

            UpdateCallerSdkDetails(apiEvent);

            // Give derived classes the ability to add or modify fields in the telemetry as needed.
            EnrichTelemetryApiEvent(apiEvent);

            return apiEvent;
        }

        private void UpdateCallerSdkDetails(ApiEvent apiEvent)
        {
            string callerSdkId;
            string callerSdkVer;

            RemoveCallerSdkCacheKeyComponents();

            // Check if ExtraQueryParameters contains caller-sdk-id and caller-sdk-ver
            if (AuthenticationRequestParameters.ExtraQueryParameters.TryGetValue(Constants.CallerSdkIdKey, out callerSdkId))
            {
                AuthenticationRequestParameters.ExtraQueryParameters.Remove(Constants.CallerSdkIdKey);
            }
            else
            {
                callerSdkId = AuthenticationRequestParameters.RequestContext.ServiceBundle.Config.ClientName;
            }

            if (AuthenticationRequestParameters.ExtraQueryParameters.TryGetValue(Constants.CallerSdkVersionKey, out callerSdkVer))
            {
                AuthenticationRequestParameters.ExtraQueryParameters.Remove(Constants.CallerSdkVersionKey);
            }
            else
            {
                callerSdkVer = AuthenticationRequestParameters.RequestContext.ServiceBundle.Config.ClientVersion;
            }

            apiEvent.CallerSdkApiId = callerSdkId == null ? null : callerSdkId.Substring(0, Math.Min(callerSdkId.Length, Constants.CallerSdkIdMaxLength));
            apiEvent.CallerSdkVersion = callerSdkVer == null ? null : callerSdkVer.Substring(0, Math.Min(callerSdkVer.Length, Constants.CallerSdkVersionMaxLength));
        }

        private void RemoveCallerSdkCacheKeyComponents()
        {
            RemoveCacheKeyComponent(Constants.CallerSdkIdKey);
            RemoveCacheKeyComponent(Constants.CallerSdkVersionKey);
        }

        private void RemoveCacheKeyComponent(string key)
        {
            var cacheKeyComponents = AuthenticationRequestParameters.CacheKeyComponents;

            if (cacheKeyComponents == null)
            {
                return;
            }

            foreach (string cacheKeyComponent in cacheKeyComponents.Keys
                .Where(cacheKeyComponent => string.Equals(cacheKeyComponent, key, StringComparison.OrdinalIgnoreCase))
                .ToArray())
            {
                cacheKeyComponents.Remove(cacheKeyComponent);
            }
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

        protected async Task<AuthenticationResult> CacheTokenResponseAndCreateAuthenticationResultAsync(MsalTokenResponse msalTokenResponse, CancellationToken cancellationToken = default)
        {
            // developer passed in user object.
            AuthenticationRequestParameters.RequestContext.Logger.Info("Checking client info returned from the server..");

            ClientInfo clientInfoFromServer = null;

            if (AuthenticationRequestParameters.ApiId != ApiEvent.ApiIds.AcquireTokenForSystemAssignedManagedIdentity &&
                AuthenticationRequestParameters.ApiId != ApiEvent.ApiIds.AcquireTokenForUserAssignedManagedIdentity &&
                AuthenticationRequestParameters.ApiId != ApiEvent.ApiIds.AcquireTokenByRefreshToken &&
                AuthenticationRequestParameters.AuthorityInfo.AuthorityType != AuthorityType.Adfs &&
                !(msalTokenResponse.ClientInfo is null))
            {
                //client_info is not returned from managed identity flows because there is no user present.
                clientInfoFromServer = ClientInfo.CreateFromJson(msalTokenResponse.ClientInfo);
                ValidateAccountIdentifiers(clientInfoFromServer);
            }

            AuthenticationRequestParameters.RequestContext.Logger.Info("Saving token response to cache..");

            var tuple = await CacheManager.SaveTokenResponseAsync(msalTokenResponse).ConfigureAwait(false);
            var atItem = tuple.Item1;
            var idtItem = tuple.Item2;
            Account account = tuple.Item3;
#if !MOBILE
            atItem?.AddAdditionalCacheParameters(clientInfoFromServer?.AdditionalResponseParameters);
#endif
            var authResult = await AuthenticationResult.CreateAsync(
                atItem,
                idtItem,
                AuthenticationRequestParameters.AuthenticationScheme,
                AuthenticationRequestParameters.RequestContext.CorrelationId,
                msalTokenResponse.TokenSource,
                AuthenticationRequestParameters.RequestContext.ApiEvent,
                account,
                msalTokenResponse.SpaAuthCode,
                msalTokenResponse.CreateExtensionDataStringMap(),
                cancellationToken).ConfigureAwait(false);

            authResult.RefreshToken = AuthenticationRequestParameters.AppConfig.IsConfidentialClient
                ? msalTokenResponse.RefreshToken
                : null;
            return authResult;
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

        internal async Task<AuthenticationResult> HandleTokenRefreshErrorAsync(
            MsalServiceException e, 
            MsalAccessTokenCacheItem cachedAccessTokenItem, 
            CancellationToken cancellationToken)
        {
            var logger = AuthenticationRequestParameters.RequestContext.Logger;

            logger.Warning($"Fetching a new AT failed. Is exception retry-able? {e.IsRetryable}. Is there an AT in the cache that is usable? {cachedAccessTokenItem != null}");

            if (cachedAccessTokenItem != null && e.IsRetryable)
            {
                logger.Info("Returning existing access token. It is not expired, but should be refreshed. ");

                var idToken = await CacheManager.GetIdTokenCacheItemAsync(cachedAccessTokenItem).ConfigureAwait(false);
                var account = await CacheManager.GetAccountAssociatedWithAccessTokenAsync(cachedAccessTokenItem).ConfigureAwait(false);

                return await AuthenticationResult.CreateAsync(
                    cachedAccessTokenItem,
                    idToken,
                    AuthenticationRequestParameters.AuthenticationScheme,
                    AuthenticationRequestParameters.RequestContext.CorrelationId,
                    TokenSource.Cache,
                    AuthenticationRequestParameters.RequestContext.ApiEvent,
                    account,
                    spaAuthCode: null,
                    additionalResponseParameters: null, 
                    cancellationToken: cancellationToken).ConfigureAwait(false);
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

        /// <summary>
        /// Validates a cached access token using the authentication operation, if the scheme implements <see cref="IAuthenticationOperation2"/>.
        /// Returns the original cache item if validation passes or is not applicable, or null if validation fails.
        /// </summary>
        internal static async Task<MsalAccessTokenCacheItem> ValidateCachedAccessTokenAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            MsalAccessTokenCacheItem cachedAccessTokenItem,
            string requestType)
        {
            if (cachedAccessTokenItem != null &&
                authenticationRequestParameters.AuthenticationScheme is IAuthenticationOperation2 authOp2)
            {
                var cacheValidationData = new MsalCacheValidationData();
                cacheValidationData.PersistedCacheParameters = cachedAccessTokenItem.PersistedCacheParameters;

                if (!await authOp2.ValidateCachedTokenAsync(cacheValidationData).ConfigureAwait(false))
                {
                    authenticationRequestParameters.RequestContext.Logger.Info(
                        $"[{requestType}] Cached token failed authentication operation validation.");
                    return null;
                }
            }

            return cachedAccessTokenItem;
        }
    }
}
