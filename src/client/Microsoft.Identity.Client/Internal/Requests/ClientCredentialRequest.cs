// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal class ClientCredentialRequest : RequestBase
    {
        private readonly AcquireTokenForClientParameters _clientParameters;
        private static readonly SemaphoreSlim s_semaphoreSlim = new SemaphoreSlim(1, 1);

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

            ILoggerAdapter logger = AuthenticationRequestParameters.RequestContext.Logger;

            if (AuthenticationRequestParameters.Authority is AadAuthority aadAuthority &&
                aadAuthority.IsCommonOrOrganizationsTenant())
            {
                logger.Error(MsalErrorMessage.ClientCredentialWrongAuthority);
            }

            AuthenticationResult authResult;

            // Skip checking cache when force refresh or claims are specified
            if (_clientParameters.ForceRefresh || !string.IsNullOrEmpty(AuthenticationRequestParameters.Claims))
            {
                AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo = CacheRefreshReason.ForceRefreshOrClaims;
                logger.Info("[ClientCredentialRequest] Skipped looking for a cached access token because ForceRefresh or Claims were set.");
                authResult = await GetAccessTokenAsync(cancellationToken, logger).ConfigureAwait(false);
                return authResult;
            }

            MsalAccessTokenCacheItem cachedAccessTokenItem = await GetCachedAccessTokenAsync().ConfigureAwait(false);

            // No access token or cached access token needs to be refreshed 
            if (cachedAccessTokenItem != null)
            {
                authResult = CreateAuthenticationResultFromCache(cachedAccessTokenItem);

                try
                {
                    var proactivelyRefresh = SilentRequestHelper.NeedsRefresh(cachedAccessTokenItem);

                    // If needed, refreshes token in the background
                    if (proactivelyRefresh)
                    {
                        AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo = CacheRefreshReason.ProactivelyRefreshed;

                        SilentRequestHelper.ProcessFetchInBackground(
                        cachedAccessTokenItem,
                        () =>
                        {
                            // Use a linked token source, in case the original cancellation token source is disposed before this background task completes.
                            using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                            return GetAccessTokenAsync(tokenSource.Token, logger);
                        }, logger);
                    }
                }
                catch (MsalServiceException e)
                {
                    return await HandleTokenRefreshErrorAsync(e, cachedAccessTokenItem).ConfigureAwait(false);
                }
            }
            else
            {
                //  No access token in the cache 
                if (AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo != CacheRefreshReason.Expired)
                {
                    AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo = CacheRefreshReason.NoCachedAccessToken;
                }

                authResult = await GetAccessTokenAsync(cancellationToken, logger).ConfigureAwait(false);
            }

            return authResult;
        }

        private async Task<AuthenticationResult> GetAccessTokenAsync(
            CancellationToken cancellationToken, 
            ILoggerAdapter logger)
        {
            await ResolveAuthorityAsync().ConfigureAwait(false);

            // Get a token from AAD
            if (ServiceBundle.Config.AppTokenProvider == null)
            {
                MsalTokenResponse msalTokenResponse = await SendTokenRequestAsync(GetBodyParameters(), cancellationToken).ConfigureAwait(false);
                return await CacheTokenResponseAndCreateAuthenticationResultAsync(msalTokenResponse).ConfigureAwait(false);
            }

            // Get a token from the app provider delegate
            AuthenticationResult authResult;
            MsalAccessTokenCacheItem cachedAccessTokenItem;

            // Allow only one call to the provider 
            logger.Verbose(() => "[ClientCredentialRequest] Entering client credential request semaphore.");
            await s_semaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);
            logger.Verbose(() => "[ClientCredentialRequest] Entered client credential request semaphore.");

            try
            {
                // Bypass cache and send request to token endpoint, when 
                // 1. Force refresh is requested, or
                // 2. Claims are passed, or 
                // 3. If the access token needs to be refreshed proactively. 
                if (_clientParameters.ForceRefresh ||
                    AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo == CacheRefreshReason.ProactivelyRefreshed ||
                    !string.IsNullOrEmpty(AuthenticationRequestParameters.Claims))
                {
                    authResult = await SendTokenRequestToAppTokenProviderAsync(logger, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    // Check the cache again after acquiring the semaphore in case the previous request cached a new token.
                    cachedAccessTokenItem = await GetCachedAccessTokenAsync().ConfigureAwait(false);

                    if (cachedAccessTokenItem == null)
                    {
                        authResult = await SendTokenRequestToAppTokenProviderAsync(logger, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        logger.Verbose(() => "[ClientCredentialRequest] Checking for a cached access token.");
                        authResult = CreateAuthenticationResultFromCache(cachedAccessTokenItem);
                    }
                }
                
                return authResult;
            }
            finally
            {
                s_semaphoreSlim.Release();
                logger.Verbose(() => "[ClientCredentialRequest] Released client credential request semaphore.");
            }
        }

        private async Task<AuthenticationResult> SendTokenRequestToAppTokenProviderAsync(ILoggerAdapter logger, 
            CancellationToken cancellationToken)
        {
            logger.Info("[ClientCredentialRequest] Acquiring a token from the token provider.");
            AppTokenProviderParameters appTokenProviderParameters = new AppTokenProviderParameters
            {
                Scopes = GetOverriddenScopes(AuthenticationRequestParameters.Scope),
                CorrelationId = AuthenticationRequestParameters.RequestContext.CorrelationId.ToString(),
                Claims = AuthenticationRequestParameters.Claims,
                TenantId = AuthenticationRequestParameters.Authority.TenantId,
                CancellationToken = cancellationToken,
            };

            AppTokenProviderResult externalToken = await ServiceBundle.Config.AppTokenProvider(appTokenProviderParameters).ConfigureAwait(false);

            var tokenResponse = MsalTokenResponse.CreateFromAppProviderResponse(externalToken);
            tokenResponse.Scope = appTokenProviderParameters.Scopes.AsSingleString();
            tokenResponse.CorrelationId = appTokenProviderParameters.CorrelationId;

            AuthenticationResult authResult = await CacheTokenResponseAndCreateAuthenticationResultAsync(tokenResponse)
                .ConfigureAwait(false);

            return authResult;
        }

        private async Task<MsalAccessTokenCacheItem> GetCachedAccessTokenAsync()
        {
            MsalAccessTokenCacheItem cachedAccessTokenItem = await CacheManager.FindAccessTokenAsync().ConfigureAwait(false);

            if (cachedAccessTokenItem != null && !_clientParameters.ForceRefresh)
            {
                AuthenticationRequestParameters.RequestContext.ApiEvent.IsAccessTokenCacheHit = true;
                Metrics.IncrementTotalAccessTokensFromCache();
                return cachedAccessTokenItem;
            }

            return null;
        }

        private AuthenticationResult CreateAuthenticationResultFromCache(MsalAccessTokenCacheItem cachedAccessTokenItem)
        {
            AuthenticationResult authResult = new AuthenticationResult(
                                                            cachedAccessTokenItem,
                                                            null,
                                                            AuthenticationRequestParameters.AuthenticationScheme,
                                                            AuthenticationRequestParameters.RequestContext.CorrelationId,
                                                            TokenSource.Cache,
                                                            AuthenticationRequestParameters.RequestContext.ApiEvent,
                                                            account: null,
                                                            spaAuthCode: null,
                                                            additionalResponseParameters: null);
            return authResult;
        }

        protected override SortedSet<string> GetOverriddenScopes(ISet<string> inputScopes)
        {
            // Client credentials should not add the reserved scopes
            // ("openid", "profile", and "offline_access")
            // because the access token is on behalf of an app (no profile, no ID token, no refresh token)
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
