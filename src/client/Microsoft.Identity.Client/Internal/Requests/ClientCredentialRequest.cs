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
using System;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal class ClientCredentialRequest : RequestBase
    {
        private readonly AcquireTokenForClientParameters _clientParameters;

        public ClientCredentialRequest(
            IServiceBundle serviceBundle,
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenForClientParameters clientParameters)
            : base(serviceBundle, authenticationRequestParameters, clientParameters)
        {
            _clientParameters = clientParameters;
        }

        protected override async Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            if (AuthenticationRequestParameters.Scope == null || AuthenticationRequestParameters.Scope.Count == 0)
            {
                throw new MsalClientException(
                    MsalError.ScopesRequired,
                    MsalErrorMessage.ScopesRequired);
            }

            MsalAccessTokenCacheItem cachedAccessTokenItem = null;
            var logger = AuthenticationRequestParameters.RequestContext.Logger;
            CacheInfoTelemetry cacheInfoTelemetry = CacheInfoTelemetry.None;

            if (!_clientParameters.ForceRefresh && 
                string.IsNullOrEmpty(AuthenticationRequestParameters.Claims))
            {
                cachedAccessTokenItem = await CacheManager.FindAccessTokenAsync().ConfigureAwait(false);

                if (cachedAccessTokenItem != null && !cachedAccessTokenItem.NeedsRefresh())
                {
                    AuthenticationRequestParameters.RequestContext.ApiEvent.IsAccessTokenCacheHit = true;

                    Metrics.IncrementTotalAccessTokensFromCache();
                    return new AuthenticationResult(
                        cachedAccessTokenItem,
                        null,
                        AuthenticationRequestParameters.AuthenticationScheme,
                        AuthenticationRequestParameters.RequestContext.CorrelationId,
                        TokenSource.Cache,
                        AuthenticationRequestParameters.RequestContext.ApiEvent);
                }

                cacheInfoTelemetry = (cachedAccessTokenItem == null) ? CacheInfoTelemetry.NoCachedAT : CacheInfoTelemetry.RefreshIn;
            }
            else
            {
                logger.Info("Skipped looking for an Access Token in the cache because ForceRefresh or Claims were set. ");

                if (_clientParameters.ForceRefresh)
                {
                    cacheInfoTelemetry = CacheInfoTelemetry.ForceRefresh;
                }
            }

            if (AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo == (int)CacheInfoTelemetry.None)
            {
                AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo = (int)cacheInfoTelemetry;
            }

            // No AT in the cache or AT needs to be refreshed
            try
            {
                return await FetchNewAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (MsalServiceException e)
            {
                return HandleTokenRefreshError(e, cachedAccessTokenItem);
            }
        }

        private async Task<AuthenticationResult> FetchNewAccessTokenAsync(CancellationToken cancellationToken)
        {
            await ResolveAuthorityAsync().ConfigureAwait(false);
            var msalTokenResponse = await SendTokenRequestAsync(GetBodyParameters(), cancellationToken).ConfigureAwait(false);
            return await CacheTokenResponseAndCreateAuthenticationResultAsync(msalTokenResponse).ConfigureAwait(false);
        }

        protected override void EnrichTelemetryApiEvent(ApiEvent apiEvent)
        {
            apiEvent.IsConfidentialClient = true;
        }

        protected override SortedSet<string> GetOverridenScopes(ISet<string> inputScopes)
        {           
            // Client credentials should not add the reserved scopes
            // "openid", "profile" and "offline_access" 
            // because AT is on behalf of an app (no profile, no IDToken, no RT)
            return new SortedSet<string>(inputScopes);
        }

        private Dictionary<string, string> GetBodyParameters()
        {
            var dict = new Dictionary<string, string>
            {
                [OAuth2Parameter.GrantType] = OAuth2GrantType.ClientCredentials,
                [OAuth2Parameter.Scope] = AuthenticationRequestParameters.Scope.AsSingleString()
            };
            return dict;
        }

        protected override KeyValuePair<string, string>? GetCcsHeader(IDictionary<string, string> additionalBodyParameters)
        {
            return null;
        }
    }
}
