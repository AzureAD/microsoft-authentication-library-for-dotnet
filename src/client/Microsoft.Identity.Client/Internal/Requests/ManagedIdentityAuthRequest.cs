// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal class ManagedIdentityAuthRequest : RequestBase
    {
        private readonly AcquireTokenForManagedIdentityParameters _managedIdentityParameters;
        private static readonly SemaphoreSlim s_semaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly ICryptographyManager _cryptoManager;

        public ManagedIdentityAuthRequest(
            IServiceBundle serviceBundle,
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenForManagedIdentityParameters managedIdentityParameters)
            : base(serviceBundle, authenticationRequestParameters, managedIdentityParameters)
        {
            _managedIdentityParameters = managedIdentityParameters;
            _cryptoManager = serviceBundle.PlatformProxy.CryptographyManager;
        }

        protected override async Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            AuthenticationResult authResult = null;
            ILoggerAdapter logger = AuthenticationRequestParameters.RequestContext.Logger;

            // 1. FIRST, handle ForceRefresh
            if (_managedIdentityParameters.ForceRefresh)
            {
                //log a warning if Claims are also set
                if (!string.IsNullOrEmpty(AuthenticationRequestParameters.Claims))
                {
                    logger.Warning("[ManagedIdentityRequest] Both ForceRefresh and Claims are set. Using ForceRefresh to skip cache.");
                }

                AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo = CacheRefreshReason.ForceRefreshOrClaims;
                logger.Info("[ManagedIdentityRequest] Skipped using the cache because ForceRefresh was set.");

                // Straight to the MI endpoint
                authResult = await GetAccessTokenAsync(cancellationToken, logger).ConfigureAwait(false);
                return authResult;
            }

            // 2. Otherwise, look for a cached token
            MsalAccessTokenCacheItem cachedAccessTokenItem = await GetCachedAccessTokenAsync()
                .ConfigureAwait(false);

            // If we have claims, we do NOT use the cached token (but we still need it to compute the hash).
            if (!string.IsNullOrEmpty(AuthenticationRequestParameters.Claims))
            {
                _managedIdentityParameters.Claims = AuthenticationRequestParameters.Claims;
                AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo = CacheRefreshReason.ForceRefreshOrClaims;

                // If there is a cached token, compute its hash for the “revoked token” scenario
                if (cachedAccessTokenItem != null)
                {
                    string cachedTokenHash = _cryptoManager.CreateSha256HashHex(cachedAccessTokenItem.Secret);
                    _managedIdentityParameters.RevokedTokenHash = cachedTokenHash;

                    logger.Info("[ManagedIdentityRequest] Claims are present. Computed hash of the cached (revoked) token. " +
                                "Will now request a fresh token from the MI endpoint.");
                }
                else
                {
                    logger.Info("[ManagedIdentityRequest] Claims are present, but no cached token was found. " +
                                "Requesting a fresh token from the MI endpoint without a revoked-token hash.");
                }

                // In both cases, we skip using the cached token and get a new one
                authResult = await GetAccessTokenAsync(cancellationToken, logger).ConfigureAwait(false);
                return authResult;
            }

            // 3. If we have no ForceRefresh and no claims, we can use the cache
            if (cachedAccessTokenItem != null)
            {
                authResult = CreateAuthenticationResultFromCache(cachedAccessTokenItem);

                logger.Info("[ManagedIdentityRequest] Access token retrieved from cache.");

                try
                {
                    var proactivelyRefresh = SilentRequestHelper.NeedsRefresh(cachedAccessTokenItem);

                    // If needed, refreshes token in the background
                    if (proactivelyRefresh)
                    {
                        logger.Info("[ManagedIdentityRequest] Initiating a proactive refresh.");

                        AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo = CacheRefreshReason.ProactivelyRefreshed;

                        SilentRequestHelper.ProcessFetchInBackground(
                        cachedAccessTokenItem,
                            () =>
                            {
                                // Use a linked token source, in case the original cancellation token source is disposed before this background task completes.
                                using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                                return GetAccessTokenAsync(tokenSource.Token, logger);
                            }, logger, ServiceBundle, AuthenticationRequestParameters.RequestContext.ApiEvent,
                        AuthenticationRequestParameters.RequestContext.ApiEvent.CallerSdkApiId,
                        AuthenticationRequestParameters.RequestContext.ApiEvent.CallerSdkVersion);
                    }
                }
                catch (MsalServiceException e)
                {
                    // If background refresh fails, we handle the exception
                    return await HandleTokenRefreshErrorAsync(e, cachedAccessTokenItem).ConfigureAwait(false);
                }
            }
            else
            {
                // No cached token
                if (AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo != CacheRefreshReason.Expired)
                {
                    AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo = CacheRefreshReason.NoCachedAccessToken;
                }

                logger.Info("[ManagedIdentityRequest] No cached access token found. " +
                            "Getting a token from the managed identity endpoint.");

                authResult = await GetAccessTokenAsync(cancellationToken, logger).ConfigureAwait(false);
            }

            return authResult;
        }

        private async Task<AuthenticationResult> GetAccessTokenAsync(
            CancellationToken cancellationToken,
            ILoggerAdapter logger)
        {
            AuthenticationResult authResult;
            MsalAccessTokenCacheItem cachedAccessTokenItem = null;

            // Requests to a managed identity endpoint must be throttled; 
            // otherwise, the endpoint will throw a HTTP 429.
            logger.Verbose(() => "[ManagedIdentityRequest] Entering managed identity request semaphore.");
            await s_semaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);
            logger.Verbose(() => "[ManagedIdentityRequest] Entered managed identity request semaphore.");

            try
            {
                // While holding the semaphore, decide whether to bypass the cache.
                // Re-check because another thread may have filled the cache while we waited.
                // Bypass when:
                // 1) ForceRefresh is requested
                // 2) Proactive refresh is in effect
                // 3) Claims are present (revocation flow)
                if (_managedIdentityParameters.ForceRefresh ||
                    AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo == CacheRefreshReason.ProactivelyRefreshed ||
                    !string.IsNullOrEmpty(_managedIdentityParameters.Claims))
                {
                    authResult = await SendTokenRequestForManagedIdentityAsync(logger, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    logger.Info("[ManagedIdentityRequest] Checking for a cached access token.");
                    cachedAccessTokenItem = await GetCachedAccessTokenAsync().ConfigureAwait(false);

                    // Check the cache again after acquiring the semaphore in case the previous request cached a new token.
                    if (cachedAccessTokenItem != null)
                    {
                        authResult = CreateAuthenticationResultFromCache(cachedAccessTokenItem);
                    }
                    else
                    {
                        authResult = await SendTokenRequestForManagedIdentityAsync(logger, cancellationToken).ConfigureAwait(false);
                    }
                }

                return authResult;
            }
            finally
            {
                s_semaphoreSlim.Release();
                logger.Verbose(() => "[ManagedIdentityRequest] Released managed identity request semaphore.");
            }
        }

        private async Task<AuthenticationResult> SendTokenRequestForManagedIdentityAsync(ILoggerAdapter logger, CancellationToken cancellationToken)
        {
            logger.Info("[ManagedIdentityRequest] Acquiring a token from the managed identity endpoint.");

            await ResolveAuthorityAsync().ConfigureAwait(false);

            ManagedIdentityClient managedIdentityClient =
                new ManagedIdentityClient(AuthenticationRequestParameters.RequestContext);

            ManagedIdentityResponse managedIdentityResponse =
                await managedIdentityClient
                .SendTokenRequestForManagedIdentityAsync(_managedIdentityParameters, cancellationToken)
                .ConfigureAwait(false);

            var msalTokenResponse = MsalTokenResponse.CreateFromManagedIdentityResponse(managedIdentityResponse);
            msalTokenResponse.Scope = AuthenticationRequestParameters.Scope.AsSingleString();

            return await CacheTokenResponseAndCreateAuthenticationResultAsync(msalTokenResponse).ConfigureAwait(false);
        }

        private async Task<MsalAccessTokenCacheItem> GetCachedAccessTokenAsync()
        {
            MsalAccessTokenCacheItem cachedAccessTokenItem = await CacheManager.FindAccessTokenAsync().ConfigureAwait(false);

            if (cachedAccessTokenItem != null)
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

        protected override KeyValuePair<string, string>? GetCcsHeader(IDictionary<string, string> additionalBodyParameters)
        {
            return null;
        }
    }
}
