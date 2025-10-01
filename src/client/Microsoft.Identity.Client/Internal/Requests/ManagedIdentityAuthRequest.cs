// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal class ManagedIdentityAuthRequest : RequestBase
    {
        private readonly AcquireTokenForManagedIdentityParameters _managedIdentityParameters;
        private readonly ManagedIdentityClient _managedIdentityClient;
        private static readonly SemaphoreSlim s_semaphoreSlim = new SemaphoreSlim(1, 1);

        public ManagedIdentityAuthRequest(
            IServiceBundle serviceBundle,
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenForManagedIdentityParameters managedIdentityParameters,
            ManagedIdentityClient managedIdentityClient)
            : base(serviceBundle, authenticationRequestParameters, managedIdentityParameters)
        {
            _managedIdentityParameters = managedIdentityParameters;
            _managedIdentityClient = managedIdentityClient;
        }

        protected override async Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            ILoggerAdapter logger = AuthenticationRequestParameters.RequestContext.Logger;

            bool popRequested = _managedIdentityParameters.IsMtlsPopRequested || AuthenticationRequestParameters.IsMtlsPopRequested;

            // If mtls_pop was requested and we already have a persisted cert, apply the op override before cache lookup
            ApplyMtlsOverrideIfCertPersisted(popRequested, logger);

            // 1) Honor ForceRefresh first (same order as original code)
            if (_managedIdentityParameters.ForceRefresh)
            {
                if (!string.IsNullOrEmpty(AuthenticationRequestParameters.Claims))
                {
                    logger.Warning("[ManagedIdentityRequest] Both ForceRefresh and Claims are set. Using ForceRefresh to skip cache.");
                }

                AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo = CacheRefreshReason.ForceRefreshOrClaims;

                // Copy claims (if any) so MI sources can add them
                if (string.IsNullOrEmpty(_managedIdentityParameters.Claims) && !string.IsNullOrEmpty(AuthenticationRequestParameters.Claims))
                {
                    _managedIdentityParameters.Claims = AuthenticationRequestParameters.Claims;
                }

                return await AcquireFreshTokenAsync(
                    CacheRefreshReason.ForceRefreshOrClaims,
                    "[ManagedIdentityRequest] Skipped using the cache because ForceRefresh was set.",
                    popRequested,
                    cancellationToken,
                    logger).ConfigureAwait(false);
            }

            // 2) Single cache lookup (do NOT count a hit here)
            MsalAccessTokenCacheItem cachedAccessTokenItem = await GetCachedAccessTokenAsync().ConfigureAwait(false);

            // 3) Claims present => force network and compute revoked-token hash (original behavior)
            bool hasClaims =
                !string.IsNullOrEmpty(_managedIdentityParameters.Claims) ||
                !string.IsNullOrEmpty(AuthenticationRequestParameters.Claims);

            if (hasClaims)
            {
                if (string.IsNullOrEmpty(_managedIdentityParameters.Claims))
                {
                    _managedIdentityParameters.Claims = AuthenticationRequestParameters.Claims;
                }

                AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo = CacheRefreshReason.ForceRefreshOrClaims;

                if (cachedAccessTokenItem != null)
                {
                    // Compute revoked‑token hash from the cached token
                    string cachedTokenHash = ServiceBundle.PlatformProxy.CryptographyManager.CreateSha256HashHex(cachedAccessTokenItem.Secret);
                    _managedIdentityParameters.RevokedTokenHash = cachedTokenHash;

                    logger.Info("[ManagedIdentityRequest] Claims present. Computed hash of the cached (revoked) token. Will request a fresh token.");
                }
                else
                {
                    logger.Info("[ManagedIdentityRequest] Claims present but no cached token found. Requesting a fresh token without revoked-token hash.");
                }

                return await AcquireFreshTokenAsync(
                    CacheRefreshReason.ForceRefreshOrClaims,
                    "[ManagedIdentityRequest] Claims provided; bypassing cache.",
                    popRequested,
                    cancellationToken,
                    logger).ConfigureAwait(false);
            }

            // 4) For IMDSv2 - bypass cache if binding cert is expiring soon (forces cert rotation)
            if (ShouldBypassCacheForRotation(cachedAccessTokenItem, logger))
            {
                cachedAccessTokenItem = null;
            }

            // 5) If PoP requested but no binding cert has been applied yet, bypass cache and mint one
            if (popRequested && AuthenticationRequestParameters.AuthenticationOperationOverride == null)
            {
                return await AcquireFreshTokenAsync(
                    CacheRefreshReason.NoCachedAccessToken,
                    "[ManagedIdentityRequest] mTLS PoP requested but no binding certificate applied; bypassing cache.",
                    popRequested,
                    cancellationToken,
                    logger).ConfigureAwait(false);
            }

            // 6) No ForceRefresh / no Claims flow: use the cache if possible
            if (cachedAccessTokenItem != null)
            {
                // Return cached token to the caller
                AuthenticationResult fromCache = CreateAuthenticationResultFromCache(cachedAccessTokenItem);
                logger.Info("[ManagedIdentityRequest] Access token retrieved from cache.");

                try
                {
                    // Decide if we should proactively refresh in the background
                    bool proactivelyRefresh = SilentRequestHelper.NeedsRefresh(cachedAccessTokenItem);

                    if (proactivelyRefresh)
                    {
                        logger.Info("[ManagedIdentityRequest] Initiating a proactive refresh (background).");

                        AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo = CacheRefreshReason.ProactivelyRefreshed;

                        // Kick off the background refresh (cancellable). Any cancellation/errors are handled inside the helper.
                        SilentRequestHelper.ProcessFetchInBackground(
                            cachedAccessTokenItem,
                            () =>
                            {
                                using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                                return AcquireFreshTokenAsync(
                                    CacheRefreshReason.ProactivelyRefreshed,
                                    "[ManagedIdentityRequest] Background proactive refresh in progress.",
                                    popRequested,
                                    tokenSource.Token,
                                    logger);
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
                    // If background refresh fails, fall back to the cached token and handle telemetry
                    return await HandleTokenRefreshErrorAsync(e, cachedAccessTokenItem).ConfigureAwait(false);
                }

                return fromCache;
            }

            // 7) No cached token -> go to network
            if (AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo != CacheRefreshReason.Expired)
            {
                AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo = CacheRefreshReason.NoCachedAccessToken;
            }

            logger.Info("[ManagedIdentityRequest] No cached access token found. Getting a token from the managed identity endpoint.");

            return await AcquireFreshTokenAsync(
                CacheRefreshReason.NoCachedAccessToken,
                "[ManagedIdentityRequest] Cache miss; acquiring new token.",
                popRequested,
                cancellationToken,
                logger).ConfigureAwait(false);
        }

        private void ApplyMtlsOverrideIfCertPersisted(bool popRequested, ILoggerAdapter logger)
        {
            if (!popRequested)
            {
                return;
            }

            if (AuthenticationRequestParameters.AuthenticationOperationOverride != null)
            {
                return;
            }

            var cert = _managedIdentityClient.MtlsBindingCertificate;
            if (cert != null && cert.NotAfter.ToUniversalTime() > DateTime.UtcNow.AddMinutes(1))
            {
                AuthenticationRequestParameters.AuthenticationOperationOverride =
                    new MtlsPopAuthenticationOperation(cert);

                logger.Info("[ManagedIdentityRequest] mTLS PoP requested. Applied MtlsPopAuthenticationOperation before cache lookup.");
            }
        }

        private bool ShouldBypassCacheForRotation(MsalAccessTokenCacheItem cachedItem, ILoggerAdapter logger)
        {
            if (cachedItem == null)
                return false;

            var cert = _managedIdentityClient.MtlsBindingCertificate;
            if (cert == null)
                return false;

            var remaining = cert.NotAfter.ToUniversalTime() - DateTime.UtcNow;
            if (remaining > TimeSpan.FromMinutes(1))
                return false;

            logger.Info("[Managed Identity] mTLS cert expiring soon; bypassing cached access token to rotate cert.");
            return true;
        }

        private async Task<AuthenticationResult> AcquireFreshTokenAsync(
            CacheRefreshReason cacheReason,
            string logMessage,
            bool popRequested,
            CancellationToken cancellationToken,
            ILoggerAdapter logger)
        {
            if (!string.IsNullOrEmpty(logMessage))
            {
                AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo = cacheReason;
                logger.Info(logMessage);
            }

            // Throttle network calls to MI endpoint to avoid HTTP 429s
            logger.Verbose(() => "[ManagedIdentityRequest] Entering managed identity request semaphore.");
            await s_semaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);
            logger.Verbose(() => "[ManagedIdentityRequest] Entered managed identity request semaphore.");

            try
            {
                // If we came here due to a cache miss, re-check after acquiring the semaphore
                if (cacheReason == CacheRefreshReason.NoCachedAccessToken)
                {
                    var recheck = await GetCachedAccessTokenAsync().ConfigureAwait(false);
                    if (recheck != null)
                    {
                        return CreateAuthenticationResultFromCache(recheck);
                    }
                }

                // If cancellation is already requested and this is a proactive refresh,
                // fall back to the cached token instead of throwing.
                if (cancellationToken.IsCancellationRequested &&
                    cacheReason == CacheRefreshReason.ProactivelyRefreshed)
                {
                    var fallback = await GetCachedAccessTokenAsync().ConfigureAwait(false);
                    if (fallback != null)
                    {
                        logger.Info("[ManagedIdentityRequest] Proactive refresh canceled before send; returning cached access token.");
                        return CreateAuthenticationResultFromCache(fallback);
                    }
                }

                // (Keep an early guard for non-proactive paths)
                cancellationToken.ThrowIfCancellationRequested();

                logger.Info("[ManagedIdentityRequest] Acquiring a token from the managed identity endpoint.");

                await ResolveAuthorityAsync().ConfigureAwait(false);

                // Propagate PoP and claims from common params to MI params
                _managedIdentityParameters.IsMtlsPopRequested = popRequested;

                if (string.IsNullOrEmpty(_managedIdentityParameters.Claims) &&
                    !string.IsNullOrEmpty(AuthenticationRequestParameters.Claims))
                {
                    _managedIdentityParameters.Claims = AuthenticationRequestParameters.Claims;
                }

                // Ensure the attestation provider reaches RequestContext for IMDSv2
                AuthenticationRequestParameters.RequestContext.AttestationTokenProvider ??=
                    _managedIdentityParameters.AttestationTokenProvider;

                try
                {
                    // SECOND (crucial) cancellation guard: right before we hit the wire.
                    if (cancellationToken.IsCancellationRequested &&
                        cacheReason == CacheRefreshReason.ProactivelyRefreshed)
                    {
                        var fallback = await GetCachedAccessTokenAsync().ConfigureAwait(false);
                        if (fallback != null)
                        {
                            logger.Info("[ManagedIdentityRequest] Proactive refresh canceled at send; returning cached access token.");
                            return CreateAuthenticationResultFromCache(fallback);
                        }
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    var managedIdentityResponse = await _managedIdentityClient
                        .SendTokenRequestForManagedIdentityAsync(
                            AuthenticationRequestParameters.RequestContext,
                            _managedIdentityParameters,
                            cancellationToken)
                        .ConfigureAwait(false);

                    // AFTER
                    if (_managedIdentityParameters.MtlsCertificate != null)
                    {
                        if (popRequested)
                        {
                            AuthenticationRequestParameters.AuthenticationOperationOverride =
                                new MtlsPopAuthenticationOperation(_managedIdentityParameters.MtlsCertificate);
                        }

                        // Persist binding for reuse/rotation across calls (Bearer & PoP)
                        _managedIdentityClient.MtlsBindingCertificate = _managedIdentityParameters.MtlsCertificate;
                        _managedIdentityParameters.MtlsCertificate = null;

                        logger.Info("[ManagedIdentityRequest] mTLS binding certificate persisted.");
                    }

                    var msalTokenResponse = MsalTokenResponse.CreateFromManagedIdentityResponse(managedIdentityResponse);
                    msalTokenResponse.Scope = AuthenticationRequestParameters.Scope.AsSingleString();

                    return await CacheTokenResponseAndCreateAuthenticationResultAsync(msalTokenResponse).ConfigureAwait(false);
                }
                catch (TaskCanceledException) when (cacheReason == CacheRefreshReason.ProactivelyRefreshed)
                {
                    // If cancellation hits while sending, prefer returning the cached token (test expectation)
                    var fallback = await GetCachedAccessTokenAsync().ConfigureAwait(false);
                    if (fallback != null)
                    {
                        logger.Info("[ManagedIdentityRequest] Proactive refresh canceled during send; returning cached access token.");
                        return CreateAuthenticationResultFromCache(fallback);
                    }

                    throw;
                }
                catch (OperationCanceledException) when (cacheReason == CacheRefreshReason.ProactivelyRefreshed)
                {
                    var fallback = await GetCachedAccessTokenAsync().ConfigureAwait(false);
                    if (fallback != null)
                    {
                        logger.Info("[ManagedIdentityRequest] Proactive refresh canceled during send; returning cached access token.");
                        return CreateAuthenticationResultFromCache(fallback);
                    }

                    throw;
                }
            }
            finally
            {
                s_semaphoreSlim.Release();
                logger.Verbose(() => "[ManagedIdentityRequest] Released managed identity request semaphore.");
            }
        }

        private async Task<MsalAccessTokenCacheItem> GetCachedAccessTokenAsync()
        {
            // Just return what the cache has; do not mark a hit here.
            return await CacheManager.FindAccessTokenAsync().ConfigureAwait(false);
        }

        private AuthenticationResult CreateAuthenticationResultFromCache(MsalAccessTokenCacheItem cachedAccessTokenItem)
        {
            // Count the hit only when returning the cached token
            AuthenticationRequestParameters.RequestContext.ApiEvent.IsAccessTokenCacheHit = true;
            Metrics.IncrementTotalAccessTokensFromCache();

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
