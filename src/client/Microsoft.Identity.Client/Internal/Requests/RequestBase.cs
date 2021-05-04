// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.TelemetryCore.Internal;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Cache.Items;

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

        private void LogRequestStarted(AuthenticationRequestParameters authenticationRequestParameters)
        {
            string messageWithPii = string.Format(
                CultureInfo.InvariantCulture,
                "=== Token Acquisition ({3}) started:\n\tAuthority: {0}\n\tScope: {1}\n\tClientId: {2}\n\t",
                authenticationRequestParameters.AuthorityInfo?.CanonicalAuthority,
                authenticationRequestParameters.Scope.AsSingleString(),
                authenticationRequestParameters.AppConfig.ClientId,
                GetType().Name);

            string messageWithoutPii = string.Format(
                CultureInfo.InvariantCulture,
                "=== Token Acquisition ({0}) started:\n\t",
                GetType().Name);

            if (authenticationRequestParameters.AuthorityInfo != null &&
                KnownMetadataProvider.IsKnownEnvironment(authenticationRequestParameters.AuthorityInfo?.Host))
            {
                messageWithoutPii += string.Format(
                    CultureInfo.CurrentCulture,
                    "\n\tAuthority Host: {0}",
                    authenticationRequestParameters.AuthorityInfo?.Host);
            }

            authenticationRequestParameters.RequestContext.Logger.InfoPii(messageWithPii, messageWithoutPii);

            if (authenticationRequestParameters.IsConfidentialClient && !CacheManager.TokenCacheInternal.IsTokenCacheSerialized())
            {
                authenticationRequestParameters.RequestContext.Logger.Error("The default token cache provided by MSAL is not designed to be performant when used in confidential client applications. Please use token cache serialization. See https://aka.ms/msal-net-cca-token-cache-serialization.");
            }
        }

        /// <summary>
        /// Return a custom set of scopes to override the default MSAL logic of merging
        /// input scopes with reserved scopes (openid, profile etc.)
        /// Leave as is / return null otherwise
        /// </summary>
        protected virtual SortedSet<string> GetOverridenScopes(ISet<string> inputScopes)
        {
            return null;
        }

        private void ValidateScopeInput(HashSet<string> scopesToValidate)
        {
            if (scopesToValidate.Contains(AuthenticationRequestParameters.AppConfig.ClientId))
            {
                throw new ArgumentException("API does not accept client id as a user-provided scope");
            }
        }

        protected abstract Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken);

        public async Task<AuthenticationResult> RunAsync(CancellationToken cancellationToken = default)
        {
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

                        apiEvent.TenantId = authenticationResult.TenantId;
                        apiEvent.AccountId = authenticationResult.UniqueId;
                        apiEvent.WasSuccessful = true;
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

            apiEvent.IsTokenCacheSerialized =
                !AuthenticationRequestParameters.CacheSessionManager.TokenCacheInternal.UsesDefaultSerialization &&
                AuthenticationRequestParameters.CacheSessionManager.TokenCacheInternal.IsTokenCacheSerialized();
            apiEvent.IsLegacyCacheEnabled = AuthenticationRequestParameters.RequestContext.ServiceBundle.Config.LegacyCacheCompatibilityEnabled;
            apiEvent.CacheInfo = (int)CacheInfoTelemetry.None;

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

            AuthenticationRequestParameters.RequestContext.Logger.Info("Saving Token Response to cache..");

            var tuple = await CacheManager.SaveTokenResponseAsync(msalTokenResponse).ConfigureAwait(false);
            var atItem = tuple.Item1;
            var idtItem = tuple.Item2;

            return new AuthenticationResult(
                atItem, 
                idtItem, 
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
            return SendTokenRequestAsync(
                AuthenticationRequestParameters.Endpoints.TokenEndpoint,
                additionalBodyParameters,
                cancellationToken);
        }

        protected Task<MsalTokenResponse> SendTokenRequestAsync(
            string tokenEndpoint,
            IDictionary<string, string> additionalBodyParameters,
            CancellationToken cancellationToken)
        {
            string scopes = GetOverridenScopes(AuthenticationRequestParameters.Scope).AsSingleString();
            var tokenClient = new TokenClient(AuthenticationRequestParameters);

            var CCSHeader = GetCCSHeader(additionalBodyParameters);
            if (CCSHeader != null)
            {
                tokenClient.AddHeaderToClient(CCSHeader.Item1, CCSHeader.Item2);
            }

            return tokenClient.SendTokenRequestAsync(
                additionalBodyParameters,
                scopes,
                tokenEndpoint,
                cancellationToken);
        }

        //The CCS header is used by the CCS service to help route requests to resources in Azure during requests to speed up authentication.
        //It consists of either the ObjectId.TenantId or the upn of the account signign in.
        //See https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2525
        protected virtual Tuple<string, string> GetCCSHeader(IDictionary<string, string> additionalBodyParameters)
        {
            if (AuthenticationRequestParameters?.Account?.HomeAccountId != null)
            {
                if (!String.IsNullOrEmpty(AuthenticationRequestParameters.Account.HomeAccountId.Identifier))
                {
                    var userObjectId = AuthenticationRequestParameters.Account.HomeAccountId.ObjectId;
                    var userTenantID = AuthenticationRequestParameters.Account.HomeAccountId.TenantId;
                    string OidCCSHeader = $@"""oid:<{userObjectId}>@<{userTenantID}>""";

                    return new Tuple<string, string>(Constants.OidCCSHeader, OidCCSHeader);
                }
                else if (!String.IsNullOrEmpty(AuthenticationRequestParameters.Account.Username))
                {
                    return GetCCSUpnHeader(AuthenticationRequestParameters.Account.Username);
                }
            }
            else if (additionalBodyParameters.ContainsKey(OAuth2Parameter.Username))
            {
                return GetCCSUpnHeader(additionalBodyParameters[OAuth2Parameter.Username]);
            }
            else if (!String.IsNullOrEmpty(AuthenticationRequestParameters.LoginHint))
            {
                return GetCCSUpnHeader (AuthenticationRequestParameters.LoginHint);
            }

            return null;
        }

        private Tuple<string, string> GetCCSUpnHeader(string upnHeader)
        {
            string OidCCSHeader = $@"""upn:<{upnHeader}>""";
            return new Tuple<string, string>(Constants.OidCCSHeader, OidCCSHeader);
        }

        private void LogReturnedToken(AuthenticationResult result)
        {
            if (result.AccessToken != null && 
                AuthenticationRequestParameters.RequestContext.Logger.IsLoggingEnabled(LogLevel.Info))
            {
                Uri canonicalAuthority = new Uri(AuthenticationRequestParameters.AuthorityInfo.CanonicalAuthority);
                AuthenticationRequestParameters.RequestContext.Logger.InfoPii(
                    $"Fetched access token from host {canonicalAuthority.Host}. Endpoint {canonicalAuthority}. ",
                    $"Fetched access token from host {canonicalAuthority.Host}. ");

                AuthenticationRequestParameters.RequestContext.Logger.Info(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "=== Token Acquisition finished successfully. An access token was returned with Expiration Time: {0} and Scopes {1}",
                        result.ExpiresOn, 
                        string.Join(" ", result.Scopes)));
            }
        }

        internal AuthenticationResult HandleTokenRefreshError(MsalServiceException e, MsalAccessTokenCacheItem cachedAccessTokenItem)
        {
            var logger = AuthenticationRequestParameters.RequestContext.Logger;
            bool isAadUnavailable = e.IsAadUnavailable();
            logger.Warning($"Fetching a new AT failed. Is AAD down? {isAadUnavailable}. Is there an AT in the cache that is usable? {cachedAccessTokenItem != null}");

            if (cachedAccessTokenItem != null && isAadUnavailable)
            {
                logger.Info("Returning existing access token. It is not expired, but should be refreshed. ");
                return new AuthenticationResult(
                    cachedAccessTokenItem,
                    null,
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
