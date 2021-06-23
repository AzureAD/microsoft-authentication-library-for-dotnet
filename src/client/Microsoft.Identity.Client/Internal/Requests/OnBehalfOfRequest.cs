// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal class OnBehalfOfRequest : RequestBase
    {
        private readonly AcquireTokenOnBehalfOfParameters _onBehalfOfParameters;
        private string _ccsRoutingHint;

        public OnBehalfOfRequest(
            IServiceBundle serviceBundle,
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenOnBehalfOfParameters onBehalfOfParameters)
            : base(serviceBundle, authenticationRequestParameters, onBehalfOfParameters)
        {
            _onBehalfOfParameters = onBehalfOfParameters;
        }

        protected override async Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            if (AuthenticationRequestParameters.Scope == null || AuthenticationRequestParameters.Scope.Count == 0)
            {
                throw new MsalClientException(
                    MsalError.ScopesRequired,
                    MsalErrorMessage.ScopesRequired);
            }

            await ResolveAuthorityAsync().ConfigureAwait(false);
            MsalAccessTokenCacheItem msalAccessTokenItem = null;
            var logger = AuthenticationRequestParameters.RequestContext.Logger;

            CacheInfoTelemetry cacheInfoTelemetry;
            if (!_onBehalfOfParameters.ForceRefresh && string.IsNullOrEmpty(AuthenticationRequestParameters.Claims))
            {
                // look for access token in the cache first.
                // no access token is found, then it means token does not exist
                // or new assertion has been passed. 
                // Look for a refresh token, if refresh token is found perform refresh token flow.
                // If a refresh token is not found, then it means refresh token does not exist or new assertion has been passed.
                // Fetch new access token for OBO
                using (logger.LogBlockDuration("[OBO Request] Looking in the cache for an access token"))
                {
                    msalAccessTokenItem = await CacheManager.FindAccessTokenAsync().ConfigureAwait(false);
                }

                if (msalAccessTokenItem != null && !msalAccessTokenItem.NeedsRefresh())
                {
                    var msalIdTokenItem = await CacheManager.GetIdTokenCacheItemAsync(msalAccessTokenItem.GetIdTokenItemKey()).ConfigureAwait(false);
                    logger.Info(
                        "[OBO Request] Found a valid access token in the cache. ID token also found? " + (msalIdTokenItem != null));

                    AuthenticationRequestParameters.RequestContext.ApiEvent.IsAccessTokenCacheHit = true;

                    Metrics.IncrementTotalAccessTokensFromCache();
                    return new AuthenticationResult(
                        msalAccessTokenItem,
                        msalIdTokenItem,
                        AuthenticationRequestParameters.AuthenticationScheme,
                        AuthenticationRequestParameters.RequestContext.CorrelationId,
                        TokenSource.Cache,
                        AuthenticationRequestParameters.RequestContext.ApiEvent,
                        CacheManager);
                }

                cacheInfoTelemetry = (msalAccessTokenItem == null) ? CacheInfoTelemetry.NoCachedAT : CacheInfoTelemetry.RefreshIn;
                logger.Verbose($"[OBO request] No valid access token found because {cacheInfoTelemetry} ");
            }
            else
            {
                logger.Info("[OBO Request] Skipped looking for an Access Token in the cache because ForceRefresh or Claims were set. ");
                cacheInfoTelemetry = CacheInfoTelemetry.ForceRefresh;
            }

            if (AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo == (int)CacheInfoTelemetry.None)
            {
                AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo = (int)cacheInfoTelemetry;
            }

            // No AT in the cache or AT needs to be refreshed
            try
            {
                return await RefreshRtOrFetchNewAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (MsalServiceException e)
            {
                return HandleTokenRefreshError(e, msalAccessTokenItem);
            }
        }

        private async Task<AuthenticationResult> RefreshRtOrFetchNewAccessTokenAsync(CancellationToken cancellationToken)
        {
            // Look for a refresh token
            MsalRefreshTokenCacheItem appRefreshToken = await CacheManager.FindRefreshTokenAsync().ConfigureAwait(false);

            // If a refresh token is not found, fetch a new access token
            if (appRefreshToken != null)
            {
                var clientInfo = ClientInfo.CreateFromJson(appRefreshToken.RawClientInfo);
                _ccsRoutingHint = CoreHelpers.GetCcsClientInfoHint(clientInfo.UniqueObjectIdentifier,
                                                                                  clientInfo.UniqueTenantIdentifier);

                var msalTokenResponse = await SilentRequestHelper.RefreshAccessTokenAsync(appRefreshToken, this, AuthenticationRequestParameters, cancellationToken)
                .ConfigureAwait(false);

                return await CacheTokenResponseAndCreateAuthenticationResultAsync(msalTokenResponse).ConfigureAwait(false);
            }

            AuthenticationRequestParameters.RequestContext.Logger.Info("[OBO request] No Refresh Token was found in the cache. Fetching OBO token from ESTS");

            return await FetchNewAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task<AuthenticationResult> FetchNewAccessTokenAsync(CancellationToken cancellationToken)
        {
            var msalTokenResponse = await SendTokenRequestAsync(GetBodyParameters(), cancellationToken).ConfigureAwait(false);
            if (msalTokenResponse.ClientInfo is null &&
                AuthenticationRequestParameters.AuthorityInfo.AuthorityType != AuthorityType.Adfs)
            {
                var logger = AuthenticationRequestParameters.RequestContext.Logger;
                logger.Info("[OBO request] This is an on behalf of request for a service principal as no client info returned in the token response.");
            }

            return await CacheTokenResponseAndCreateAuthenticationResultAsync(msalTokenResponse).ConfigureAwait(false);
        }

        protected override void EnrichTelemetryApiEvent(ApiEvent apiEvent)
        {
            apiEvent.IsConfidentialClient = true;
        }

        private Dictionary<string, string> GetBodyParameters()
        {
            var dict = new Dictionary<string, string>
            {
                [OAuth2Parameter.GrantType] = _onBehalfOfParameters.UserAssertion.AssertionType,
                [OAuth2Parameter.Assertion] = _onBehalfOfParameters.UserAssertion.Assertion,
                [OAuth2Parameter.RequestedTokenUse] = OAuth2RequestedTokenUse.OnBehalfOf
            };
            return dict;
        }

        protected override KeyValuePair<string, string>? GetCcsHeader(IDictionary<string, string> additionalBodyParameters)
        {
            if (string.IsNullOrEmpty(_ccsRoutingHint))
            {
                return null;
            }

            return new KeyValuePair<string, string>(Constants.CcsRoutingHintHeader, _ccsRoutingHint) as KeyValuePair<string, string>?;
        }
    }
}
