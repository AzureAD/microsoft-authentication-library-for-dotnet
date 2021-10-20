// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.TelemetryCore.Internal;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.Utils;

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
        protected IServiceBundle ServiceBundle { get; }

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
            try
            {
                using (AuthenticationRequestParameters.RequestContext.CreateTelemetryHelper(apiEvent))
                {
                    try
                    {
                        AuthenticationRequestParameters.LogParameters();
                        LogRequestStarted(AuthenticationRequestParameters);

                        AuthenticationResult authenticationResult = await ExecuteAsync(cancellationToken).ConfigureAwait(false);
                        LogReturnedToken(authenticationResult);

                        UpdateTelemetry(sw, apiEvent, authenticationResult);
                        return authenticationResult;
                    }
                    catch (MsalException ex)
                    {
                        apiEvent.ApiErrorCode = ex.ErrorCode;
                        AuthenticationRequestParameters.RequestContext.Logger.ErrorPii(ex);
                        throw;
                    }
                    catch (Exception ex)
                    {
                        AuthenticationRequestParameters.RequestContext.Logger.ErrorPii(ex);
                        throw;
                    }
                }
            }
            finally
            {
                ServiceBundle.MatsTelemetryManager.Flush(AuthenticationRequestParameters.RequestContext.CorrelationId.AsMatsCorrelationId());
            }
        }

        private static void UpdateTelemetry(Stopwatch sw, ApiEvent apiEvent, AuthenticationResult authenticationResult)
        {
            apiEvent.TenantId = authenticationResult.TenantId;
            apiEvent.AccountId = authenticationResult.UniqueId;
            apiEvent.WasSuccessful = true;

            authenticationResult.AuthenticationResultMetadata.DurationTotalInMs = sw.ElapsedMilliseconds;
            authenticationResult.AuthenticationResultMetadata.DurationInHttpInMs = apiEvent.DurationInHttpInMs;
            authenticationResult.AuthenticationResultMetadata.DurationInCacheInMs = apiEvent.DurationInCacheInMs;
            authenticationResult.AuthenticationResultMetadata.TokenEndpoint = apiEvent.TokenEndpoint;
            authenticationResult.AuthenticationResultMetadata.CacheRefreshReason = (CacheRefreshReason)apiEvent.CacheInfo;

            Metrics.IncrementTotalDurationInMs(authenticationResult.AuthenticationResultMetadata.DurationTotalInMs);
        }

        protected virtual void EnrichTelemetryApiEvent(ApiEvent apiEvent)
        {
            // In base classes have them override this to add their properties/fields to the event.
        }

        private ApiEvent InitializeApiEvent(string accountId)
        {
            ApiEvent apiEvent = new ApiEvent(
                AuthenticationRequestParameters.RequestContext.Logger,
                ServiceBundle.PlatformProxy.CryptographyManager,
                AuthenticationRequestParameters.RequestContext.CorrelationId.AsMatsCorrelationId())
            {
                ApiId = AuthenticationRequestParameters.ApiId,
                ApiTelemId = AuthenticationRequestParameters.ApiTelemId,
                AccountId = accountId ?? "",
                WasSuccessful = false
            };

            foreach (var kvp in AuthenticationRequestParameters.GetApiTelemetryFeatures())
            {
                apiEvent[kvp.Key] = kvp.Value;
            }

            if (AuthenticationRequestParameters.AuthorityInfo != null)
            {
                apiEvent.Authority = new Uri(AuthenticationRequestParameters.AuthorityInfo.CanonicalAuthority);
                apiEvent.AuthorityType = AuthenticationRequestParameters.AuthorityInfo.AuthorityType.ToString();
            }

            apiEvent.IsTokenCacheSerialized = AuthenticationRequestParameters.CacheSessionManager.TokenCacheInternal.IsExternalSerializationConfiguredByUser();
            apiEvent.IsLegacyCacheEnabled = AuthenticationRequestParameters.RequestContext.ServiceBundle.Config.LegacyCacheCompatibilityEnabled;
            apiEvent.CacheInfo = (int)CacheRefreshReason.NotApplicable;

            // Give derived classes the ability to add or modify fields in the telemetry as needed.
            EnrichTelemetryApiEvent(apiEvent);

            return apiEvent;
        }

        protected async Task<AuthenticationResult> CacheTokenResponseAndCreateAuthenticationResultAsync(MsalTokenResponse msalTokenResponse)
        {
            // developer passed in user object.
            AuthenticationRequestParameters.RequestContext.Logger.Info("Checking client info returned from the server..");

            ClientInfo fromServer = null;

            if (!AuthenticationRequestParameters.IsClientCredentialRequest &&
                AuthenticationRequestParameters.ApiId != ApiEvent.ApiIds.AcquireTokenByRefreshToken &&
                AuthenticationRequestParameters.AuthorityInfo.AuthorityType != AuthorityType.Adfs &&
                !(msalTokenResponse.ClientInfo is null))
            {
                //client_info is not returned from client credential flows because there is no user present.
                fromServer = ClientInfo.CreateFromJson(msalTokenResponse.ClientInfo);
            }

            ValidateAccountIdentifiers(fromServer);

            AuthenticationRequestParameters.RequestContext.Logger.Info("Saving token response to cache..");

            var tuple = await CacheManager.SaveTokenResponseAsync(msalTokenResponse).ConfigureAwait(false);
            var atItem = tuple.Item1;
            var idtItem = tuple.Item2;
            var account = tuple.Item3;

            return new AuthenticationResult(
                atItem,
                idtItem,
                account?.TenantProfiles,
                AuthenticationRequestParameters.AuthenticationScheme,
                AuthenticationRequestParameters.RequestContext.CorrelationId,
                msalTokenResponse.TokenSource,
                AuthenticationRequestParameters.RequestContext.ApiEvent);
        }

        private void ValidateAccountIdentifiers(ClientInfo fromServer)
        {
            if (fromServer == null ||
                AuthenticationRequestParameters?.Account?.HomeAccountId == null ||
                PublicClientApplication.IsOperatingSystemAccount(AuthenticationRequestParameters?.Account))
            {
                return;
            }

            if (AuthenticationRequestParameters.AuthorityInfo.AuthorityType == AuthorityType.B2C &&
                fromServer.UniqueTenantIdentifier.Equals(AuthenticationRequestParameters.Account.HomeAccountId.TenantId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (fromServer.UniqueObjectIdentifier.Equals(AuthenticationRequestParameters.Account.HomeAccountId.ObjectId,
                    StringComparison.OrdinalIgnoreCase) &&
                fromServer.UniqueTenantIdentifier.Equals(AuthenticationRequestParameters.Account.HomeAccountId.TenantId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            AuthenticationRequestParameters.RequestContext.Logger.Error("Returned user identifiers do not match the sent user identifier");

            AuthenticationRequestParameters.RequestContext.Logger.ErrorPii(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Returned user identifiers (uid:{0} utid:{1}) does not match the sent user identifier (uid:{2} utid:{3})",
                    fromServer.UniqueObjectIdentifier,
                    fromServer.UniqueTenantIdentifier,
                    AuthenticationRequestParameters.Account.HomeAccountId.ObjectId,
                    AuthenticationRequestParameters.Account.HomeAccountId.TenantId),
                string.Empty);

            throw new MsalClientException(MsalError.UserMismatch, MsalErrorMessage.UserMismatchSaveToken);
        }

        protected Task ResolveAuthorityAsync()
        {
            return AuthenticationRequestParameters.AuthorityManager.RunInstanceDiscoveryAndValidationAsync();
        }

        internal Task<MsalTokenResponse> SendTokenRequestAsync(
            IDictionary<string, string> additionalBodyParameters,
            CancellationToken cancellationToken)
        {
            var tokenResponse = SendTokenRequestAsync(
                AuthenticationRequestParameters.Authority.GetTokenEndpoint(),
                additionalBodyParameters,
                cancellationToken);
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
        //It consists of either the ObjectId.TenantId or the upn of the account signign in.
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

            if (additionalBodyParameters.ContainsKey(OAuth2Parameter.Username))
            {
                return GetCcsUpnHeader(additionalBodyParameters[OAuth2Parameter.Username]);
            }

            if (!String.IsNullOrEmpty(AuthenticationRequestParameters.LoginHint))
            {
                return GetCcsUpnHeader(AuthenticationRequestParameters.LoginHint);
            }

            return new KeyValuePair<string, string>();
        }

        protected KeyValuePair<string, string>? GetCcsUpnHeader(string upnHeader)
        {
            string OidCcsHeader = CoreHelpers.GetCcsUpnHint(upnHeader);
            return new KeyValuePair<string, string>(Constants.CcsRoutingHintHeader, OidCcsHeader) as KeyValuePair<string, string>?;
        }

        private void LogRequestStarted(AuthenticationRequestParameters authenticationRequestParameters)
        {
            if (authenticationRequestParameters.RequestContext.Logger.IsLoggingEnabled(LogLevel.Info))
            {
                string scopes = authenticationRequestParameters.Scope.AsSingleString();
                string messageWithPii = string.Format(
                    CultureInfo.InvariantCulture,
                    "=== Token Acquisition ({3}) started:\n\tAuthority: {0}\n\tScope: {1}\n\tClientId: {2}\n\t",
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

            if (authenticationRequestParameters.IsConfidentialClient &&
                !authenticationRequestParameters.IsClientCredentialRequest &&
                !CacheManager.TokenCacheInternal.IsAppSubscribedToSerializationEvents())
            {
                authenticationRequestParameters.RequestContext.Logger.Error("The default token cache provided by MSAL is not designed to be performant when used in confidential client applications. Please use token cache serialization. See https://aka.ms/msal-net-cca-token-cache-serialization.");
            }
        }

        private void LogReturnedToken(AuthenticationResult result)
        {
            if (result.AccessToken != null &&
                AuthenticationRequestParameters.RequestContext.Logger.IsLoggingEnabled(LogLevel.Info))
            {
                int appHashCode = AuthenticationRequestParameters.AppConfig.GetHashCode();
                string scopes = string.Join(" ", result.Scopes);
                Uri canonicalAuthority = new Uri(AuthenticationRequestParameters.AuthorityInfo.CanonicalAuthority);
                AuthenticationRequestParameters.RequestContext.Logger.InfoPii(
                    $"Fetched access token from host {canonicalAuthority.Host}. Endpoint {canonicalAuthority}. ",
                    $"Fetched access token from host {canonicalAuthority.Host}. ");

                AuthenticationRequestParameters.RequestContext.Logger.Info("\n\t=== Token Acquisition finished successfully:");
                AuthenticationRequestParameters.RequestContext.Logger.InfoPii(
                        $" AT expiration time: {result.ExpiresOn}, scopes {scopes} " +
                            $"source {result.AuthenticationResultMetadata.TokenSource} from {canonicalAuthority} appHashCode {appHashCode}",
                        $" AT expiration time: {result.ExpiresOn}, scopes {scopes} " +
                            $"source {result.AuthenticationResultMetadata.TokenSource} from {canonicalAuthority.Host} appHashCode {appHashCode}");
            }
        }

        internal async Task<AuthenticationResult> HandleTokenRefreshErrorAsync(MsalServiceException e, MsalAccessTokenCacheItem cachedAccessTokenItem)
        {
            var logger = AuthenticationRequestParameters.RequestContext.Logger;
            bool isAadUnavailable = e.IsAadUnavailable();
            logger.Warning($"Fetching a new AT failed. Is AAD down? {isAadUnavailable}. Is there an AT in the cache that is usable? {cachedAccessTokenItem != null}");

            if (cachedAccessTokenItem != null && isAadUnavailable)
            {
                logger.Info("Returning existing access token. It is not expired, but should be refreshed. ");

                var idToken = await CacheManager.GetIdTokenCacheItemAsync(cachedAccessTokenItem).ConfigureAwait(false);
                var tenantProfiles = await CacheManager.GetTenantProfilesAsync(cachedAccessTokenItem.HomeAccountId).ConfigureAwait(false);

                return new AuthenticationResult(
                    cachedAccessTokenItem,
                    idToken,
                    tenantProfiles?.Values,
                    AuthenticationRequestParameters.AuthenticationScheme,
                    AuthenticationRequestParameters.RequestContext.CorrelationId,
                    TokenSource.Cache,
                    AuthenticationRequestParameters.RequestContext.ApiEvent);
            }

            logger.Warning("Either the exception does not indicate a problem with AAD or the token cache does not have an AT that is usable. ");
            throw e;
        }
    }
}
