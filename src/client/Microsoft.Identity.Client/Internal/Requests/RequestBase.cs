// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.TelemetryCore.Internal;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Json.Linq;
using System.Text;
using Microsoft.Identity.Json;

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

            if (authenticationRequestParameters.Scope == null || authenticationRequestParameters.Scope.Count == 0)
            {
                throw new ArgumentNullException(nameof(authenticationRequestParameters.Scope));
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
                authenticationRequestParameters.ClientId,
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
        }

        protected virtual SortedSet<string> GetDecoratedScope(SortedSet<string> inputScope)
        {
            // OAuth spec states that scopes are case sensitive, but 
            // merge the reserved scopes in a case insensitive way, to 
            // avoid sending things like "openid OpenId" (note that EVO is tollerant of this)
            SortedSet<string> set = new SortedSet<string>(
                inputScope.ToArray(),
                StringComparer.OrdinalIgnoreCase);

            set.UnionWith(OAuth2Value.ReservedScopes);
            return set;
        }

        protected void ValidateScopeInput(SortedSet<string> scopesToValidate)
        {
            if (scopesToValidate.Contains(AuthenticationRequestParameters.ClientId))
            {
                throw new ArgumentException("API does not accept client id as a user-provided scope");
            }
        }

        internal abstract Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken);

        internal virtual Task PreRunAsync()
        {
            return Task.FromResult(0);
        }

        public async Task<AuthenticationResult> RunAsync(CancellationToken cancellationToken)
        {
            ApiEvent apiEvent = InitializeApiEvent(AuthenticationRequestParameters.Account?.HomeAccountId?.Identifier);

            try
            {
                using (ServiceBundle.TelemetryManager.CreateTelemetryHelper(apiEvent))
                {
                    try
                    {
                        await PreRunAsync().ConfigureAwait(false);
                        AuthenticationRequestParameters.LogParameters(AuthenticationRequestParameters.RequestContext.Logger);
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
                ServiceBundle.TelemetryManager.Flush(AuthenticationRequestParameters.RequestContext.CorrelationId.AsMatsCorrelationId());
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
                !AuthenticationRequestParameters.IsRefreshTokenRequest &&
                AuthenticationRequestParameters.AuthorityInfo.AuthorityType != AuthorityType.Adfs)
            {
                //client_info is not returned from client credential flows because there is no user present.
                fromServer = ClientInfo.CreateFromJson(msalTokenResponse.ClientInfo);
            }

            ValidateAccountIdentifiers(fromServer);

            IdToken idToken = IdToken.Parse(msalTokenResponse.IdToken);

            AuthenticationRequestParameters.TenantUpdatedCanonicalAuthority =
                   Authority.CreateAuthorityWithTenant(AuthenticationRequestParameters.Authority.AuthorityInfo, idToken?.TenantId);


            AuthenticationRequestParameters.RequestContext.Logger.Info("Saving Token Response to cache..");

            var tuple = await CacheManager.SaveTokenResponseAsync(msalTokenResponse).ConfigureAwait(false);
            var atItem = tuple.Item1;
            var idtItem = tuple.Item2;

            return new AuthenticationResult(
                atItem, 
                idtItem, 
                AuthenticationRequestParameters.AuthenticationScheme,
                AuthenticationRequestParameters.RequestContext.CorrelationId);
        }

        private void ValidateAccountIdentifiers(ClientInfo fromServer)
        {
            if (fromServer == null || AuthenticationRequestParameters?.Account?.HomeAccountId == null)
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

        internal async Task ResolveAuthorityEndpointsAsync()
        {
            // This will make a network call unless instance discovery is cached, but this ok
            // GetAccounts and AcquireTokenSilent do not need this
            await UpdateAuthorityWithPreferredNetworkHostAsync().ConfigureAwait(false);

            AuthenticationRequestParameters.Endpoints = await ServiceBundle.AuthorityEndpointResolutionManager.ResolveEndpointsAsync(
                AuthenticationRequestParameters.AuthorityInfo,
                AuthenticationRequestParameters.LoginHint,
                AuthenticationRequestParameters.RequestContext).ConfigureAwait(false);
        }

        protected Task<MsalTokenResponse> SendTokenRequestAsync(
            IDictionary<string, string> additionalBodyParameters,
            CancellationToken cancellationToken)
        {
            return SendTokenRequestAsync(
                AuthenticationRequestParameters.Endpoints.TokenEndpoint,
                additionalBodyParameters,
                cancellationToken);
        }

        protected async Task<MsalTokenResponse> SendTokenRequestAsync(
            string tokenEndpoint,
            IDictionary<string, string> additionalBodyParameters,
            CancellationToken cancellationToken)
        {
            OAuth2Client client = new OAuth2Client(ServiceBundle.DefaultLogger, ServiceBundle.HttpManager, ServiceBundle.TelemetryManager);
            client.AddBodyParameter(OAuth2Parameter.ClientId, AuthenticationRequestParameters.ClientId);
            client.AddBodyParameter(OAuth2Parameter.ClientInfo, "1");


#if DESKTOP || NETSTANDARD1_3 || NET_CORE
            if (AuthenticationRequestParameters.ClientCredential != null)
            {
                Dictionary<string, string> ccBodyParameters = ClientCredentialHelper.CreateClientCredentialBodyParameters(
                    AuthenticationRequestParameters.RequestContext.Logger,
                    ServiceBundle.PlatformProxy.CryptographyManager,
                    AuthenticationRequestParameters.ClientCredential,
                    AuthenticationRequestParameters.ClientId,
                    AuthenticationRequestParameters.Endpoints,
                    AuthenticationRequestParameters.SendX5C);

                foreach (var entry in ccBodyParameters)
                {
                    client.AddBodyParameter(entry.Key, entry.Value);
                }
            }
#endif

            client.AddBodyParameter(OAuth2Parameter.Scope,
                GetDecoratedScope(AuthenticationRequestParameters.Scope).AsSingleString());

            client.AddQueryParameter(OAuth2Parameter.Claims, AuthenticationRequestParameters.Claims);

            foreach (var kvp in additionalBodyParameters)
            {
                client.AddBodyParameter(kvp.Key, kvp.Value);
            }

            foreach (var kvp in AuthenticationRequestParameters.AuthenticationScheme.GetTokenRequestParams())
            {
                client.AddBodyParameter(kvp.Key, kvp.Value);
            }

            return await SendHttpMessageAsync(client, tokenEndpoint).ConfigureAwait(false);
        }


        private async Task<MsalTokenResponse> SendHttpMessageAsync(OAuth2Client client, string tokenEndpoint)
        {
            UriBuilder builder = new UriBuilder(tokenEndpoint);
            builder.AppendQueryParameters(AuthenticationRequestParameters.ExtraQueryParameters);
            MsalTokenResponse msalTokenResponse =
                await client
                    .GetTokenAsync(builder.Uri,
                        AuthenticationRequestParameters.RequestContext)
                    .ConfigureAwait(false);

            if (string.IsNullOrEmpty(msalTokenResponse.Scope))
            {
                msalTokenResponse.Scope = AuthenticationRequestParameters.Scope.AsSingleString();
                AuthenticationRequestParameters.RequestContext.Logger.Info("ScopeSet was missing from the token response, so using developer provided scopes in the result. ");
            }

            return msalTokenResponse;
        }

        private void LogReturnedToken(AuthenticationResult result)
        {
            if (result.AccessToken != null)
            {
                AuthenticationRequestParameters.RequestContext.Logger.Info(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "=== Token Acquisition finished successfully. An access token was returned with Expiration Time: {0} ===",
                        result.ExpiresOn));
            }
        }

        private async Task UpdateAuthorityWithPreferredNetworkHostAsync()
        {
            InstanceDiscoveryMetadataEntry metadata = await
                ServiceBundle.InstanceDiscoveryManager.GetMetadataEntryAsync(
                    AuthenticationRequestParameters.AuthorityInfo.CanonicalAuthority,
                    AuthenticationRequestParameters.RequestContext)
                .ConfigureAwait(false);

            AuthenticationRequestParameters.Authority = Authority.CreateAuthorityWithEnvironment(
                    AuthenticationRequestParameters.AuthorityInfo,
                    metadata.PreferredNetwork);
        }
    }
}
