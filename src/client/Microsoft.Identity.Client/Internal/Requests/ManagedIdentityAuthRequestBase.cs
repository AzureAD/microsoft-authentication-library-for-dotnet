// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Internal.Requests
{
    /// <summary>
    /// Shared Managed Identity request pipeline:
    /// - ForceRefresh / Claims handling (incl. revoked-token hash)
    /// - Cache hit path + proactive background refresh
    /// - Single-flight semaphore for endpoint calls
    /// Derived classes only implement <see cref="SendTokenRequestAsync"/>.
    /// </summary>
    internal abstract class ManagedIdentityAuthRequestBase : RequestBase
    {
        protected readonly AcquireTokenForManagedIdentityParameters _managedIdentityParameters;
        protected static readonly SemaphoreSlim s_semaphoreSlim = new(1, 1);
        protected readonly ICryptographyManager _cryptoManager;

        protected ManagedIdentityAuthRequestBase(
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

            // 1) ForceRefresh wins (even if claims present)
            if (_managedIdentityParameters.ForceRefresh)
            {
                if (!string.IsNullOrEmpty(AuthenticationRequestParameters.Claims))
                {
                    logger.Warning("[ManagedIdentityRequest] Both ForceRefresh and Claims are set. Using ForceRefresh to skip cache.");
                }

                AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo = CacheRefreshReason.ForceRefreshOrClaims;
                logger.Info("[ManagedIdentityRequest] Skipped using the cache because ForceRefresh was set.");
                return await GetAccessTokenWithSemaphoreAsync(cancellationToken, logger).ConfigureAwait(false);
            }

            // 2) Try cache first
            var cachedAccessTokenItem = await GetCachedAccessTokenAsync().ConfigureAwait(false);

            // If claims are present, we don’t return the cached AT; we compute its hash (if any) for revocation and fetch fresh.
            if (!string.IsNullOrEmpty(AuthenticationRequestParameters.Claims))
            {
                _managedIdentityParameters.Claims = AuthenticationRequestParameters.Claims;
                AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo = CacheRefreshReason.ForceRefreshOrClaims;

                if (cachedAccessTokenItem != null)
                {
                    string cachedTokenHash = _cryptoManager.CreateSha256HashHex(cachedAccessTokenItem.Secret);
                    _managedIdentityParameters.RevokedTokenHash = cachedTokenHash;

                    logger.Info("[ManagedIdentityRequest] Claims present. Computed hash of cached (revoked) token; requesting fresh token.");
                }
                else
                {
                    logger.Info("[ManagedIdentityRequest] Claims present, but no cached token. Requesting fresh token.");
                }

                return await GetAccessTokenWithSemaphoreAsync(cancellationToken, logger).ConfigureAwait(false);
            }

            // 3) No ForceRefresh and no claims → cache path
            if (cachedAccessTokenItem != null)
            {
                authResult = CreateAuthenticationResultFromCache(cachedAccessTokenItem);
                logger.Info("[ManagedIdentityRequest] Access token retrieved from cache.");

                try
                {
                    var proactivelyRefresh = SilentRequestHelper.NeedsRefresh(cachedAccessTokenItem);

                    if (proactivelyRefresh)
                    {
                        logger.Info("[ManagedIdentityRequest] Initiating a proactive refresh.");
                        AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo = CacheRefreshReason.ProactivelyRefreshed;

                        SilentRequestHelper.ProcessFetchInBackground(
                            cachedAccessTokenItem,
                            () =>
                            {
                                using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                                return GetAccessTokenWithSemaphoreAsync(tokenSource.Token, logger);
                            },
                            logger,
                            ServiceBundle,
                            AuthenticationRequestParameters.RequestContext.ApiEvent,
                            AuthenticationRequestParameters.RequestContext.ApiEvent.CallerSdkApiId,
                            AuthenticationRequestParameters.RequestContext.ApiEvent.CallerSdkVersion);
                    }
                }
                catch (MsalServiceException e)
                {
                    return await HandleTokenRefreshErrorAsync(e, cachedAccessTokenItem).ConfigureAwait(false);
                }

                return authResult;
            }

            // 4) No cache
            if (AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo != CacheRefreshReason.Expired)
            {
                AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo = CacheRefreshReason.NoCachedAccessToken;
            }

            logger.Info("[ManagedIdentityRequest] No cached access token found. Getting a token from the managed identity endpoint.");
            return await GetAccessTokenWithSemaphoreAsync(cancellationToken, logger).ConfigureAwait(false);
        }

        private async Task<AuthenticationResult> GetAccessTokenWithSemaphoreAsync(
            CancellationToken cancellationToken,
            ILoggerAdapter logger)
        {
            logger.Verbose(() => "[ManagedIdentityRequest] Entering managed identity request semaphore.");
            await s_semaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);
            logger.Verbose(() => "[ManagedIdentityRequest] Entered managed identity request semaphore.");

            try
            {
                // Re-check cache policy while inside the semaphore in case another thread updated it.
                if (_managedIdentityParameters.ForceRefresh ||
                    AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo == CacheRefreshReason.ProactivelyRefreshed ||
                    !string.IsNullOrEmpty(_managedIdentityParameters.Claims))
                {
                    await ResolveAuthorityAsync().ConfigureAwait(false);
                    return await SendTokenRequestAsync(logger, cancellationToken).ConfigureAwait(false);
                }

                var cached = await GetCachedAccessTokenAsync().ConfigureAwait(false);
                if (cached != null)
                {
                    return CreateAuthenticationResultFromCache(cached);
                }

                await ResolveAuthorityAsync().ConfigureAwait(false);
                return await SendTokenRequestAsync(logger, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                s_semaphoreSlim.Release();
                logger.Verbose(() => "[ManagedIdentityRequest] Released managed identity request semaphore.");
            }
        }

        protected abstract Task<AuthenticationResult> SendTokenRequestAsync(ILoggerAdapter logger, CancellationToken cancellationToken);

        protected async Task<MsalAccessTokenCacheItem> GetCachedAccessTokenAsync()
        {
            var cached = await CacheManager.FindAccessTokenAsync().ConfigureAwait(false);
            if (cached != null)
            {
                AuthenticationRequestParameters.RequestContext.ApiEvent.IsAccessTokenCacheHit = true;
                Metrics.IncrementTotalAccessTokensFromCache();
            }
            return cached;
        }

        protected AuthenticationResult CreateAuthenticationResultFromCache(MsalAccessTokenCacheItem cached)
        {
            return new AuthenticationResult(
                cached,
                null,
                AuthenticationRequestParameters.AuthenticationScheme,
                AuthenticationRequestParameters.RequestContext.CorrelationId,
                TokenSource.Cache,
                AuthenticationRequestParameters.RequestContext.ApiEvent,
                account: null,
                spaAuthCode: null,
                additionalResponseParameters: null);
        }

        protected override KeyValuePair<string, string>? GetCcsHeader(IDictionary<string, string> _) => null;
    }
}
