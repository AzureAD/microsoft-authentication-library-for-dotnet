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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.Instance.Discovery;

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

        /// <summary>
        /// Return a custom set of scopes to override the default MSAL logic of merging
        /// input scopes with reserved scopes (openid, profile etc.)
        /// Leave as is / return null otherwise
        /// </summary>
        protected virtual SortedSet<string> GetOverridenScopes(ISet<string> inputScopes)
        {
            return null;
        }

        private void ValidateScopeInput(SortedSet<string> scopesToValidate)
        {
            if (scopesToValidate.Contains(AuthenticationRequestParameters.ClientId))
            {
                throw new ArgumentException("API does not accept client id as a user-provided scope");
            }
        }

        protected abstract Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken);

        internal virtual Task PreRunAsync()
        {
            return Task.FromResult(0);
        }

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
                        await PreRunAsync().ConfigureAwait(false);
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

        protected async Task ResolveAuthorityEndpointsAsync()
        {
            await AuthorityEndpoints.UpdateAuthorityEndpointsAsync(AuthenticationRequestParameters)
                .ConfigureAwait(false);
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
            string scopes = GetOverridenScopes(AuthenticationRequestParameters.Scope).AsSingleString();
            var tokenClient = new TokenClient(AuthenticationRequestParameters);

            return await tokenClient.SendTokenRequestAsync(
                additionalBodyParameters,
                scopes,
                tokenEndpoint,
                cancellationToken)
                .ConfigureAwait(false);
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
    }
}
