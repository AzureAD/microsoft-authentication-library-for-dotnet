// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal class ClientCredentialRequest : RequestBase
    {
        private readonly AcquireTokenForClientParameters _clientParameters;
        private static readonly SemaphoreSlim s_semaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly ICryptographyManager _cryptoManager;

        public ClientCredentialRequest(
            IServiceBundle serviceBundle,
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenForClientParameters clientParameters)
            : base(serviceBundle, authenticationRequestParameters, clientParameters)
        {
            _clientParameters = clientParameters;
            _cryptoManager = serviceBundle.PlatformProxy.CryptographyManager;
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

            // Skip cache if either:
            // 1) ForceRefresh is set, or
            // 2) Claims are specified and there is no AccessTokenHashToRefresh.
            // This ensures that when both claims and AccessTokenHashToRefresh are set,
            // we do NOT skip the cache, allowing MSAL to attempt retrieving a matching
            // cached token by the provided hash before requesting a new token.
            bool skipCache = _clientParameters.ForceRefresh || 
                (!string.IsNullOrEmpty(AuthenticationRequestParameters.Claims) && 
                string.IsNullOrEmpty(_clientParameters.AccessTokenHashToRefresh));

            // Skip checking cache when either ForceRefresh is true
            // or (Claims are present without a token hash).
            if (skipCache)
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
                        }, logger, ServiceBundle, AuthenticationRequestParameters.RequestContext.ApiEvent,
                        AuthenticationRequestParameters.RequestContext.ApiEvent.CallerSdkApiId, 
                        AuthenticationRequestParameters.RequestContext.ApiEvent.CallerSdkVersion);
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
            // 1) Get the cached item 
            MsalAccessTokenCacheItem cachedAccessTokenItem = await CacheManager.FindAccessTokenAsync().ConfigureAwait(false);

            // 2) If no cached item or force refresh is requested, return null
            if (!IsValidCachedToken(cachedAccessTokenItem))
            {
                return null;
            }

            // 3) If there is a matching AccessTokenHashToRefresh, ignore this cached token
            if (IsTokenIgnoredByMatchingHash(cachedAccessTokenItem))
            {
                return null;
            }

            // 4) Otherwise, record a cache hit and return the cached token
            MarkAccessTokenAsCacheHit();
            return cachedAccessTokenItem;
        }

        /// <summary>
        /// Checks if the cached access token can be used.
        /// </summary>
        private bool IsValidCachedToken(MsalAccessTokenCacheItem cachedAccessTokenItem)
        {
            // Return false if the cache is empty
            if (cachedAccessTokenItem == null)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Determines whether the cached token should be ignored due to matching the AccessTokenHashToRefresh.
        /// </summary>
        private bool IsTokenIgnoredByMatchingHash(MsalAccessTokenCacheItem cachedAccessTokenItem)
        {
            if (string.IsNullOrEmpty(_clientParameters.AccessTokenHashToRefresh))
            {
                return false;
            }

            string cachedTokenHash = _cryptoManager.CreateSha256Hash(cachedAccessTokenItem.Secret);

            // If the hash of the cached token matches the hash to refresh, ignore the cached token
            bool matchesHash = string.Equals(
                cachedTokenHash,
                _clientParameters.AccessTokenHashToRefresh,
                StringComparison.Ordinal);

            if (matchesHash)
            {
                AuthenticationRequestParameters.RequestContext.Logger.Info(
                    "[ClientCredentialRequest] A cached token was found and its hash matches AccessTokenHashToRefresh, so it is ignored.");
                return true;
            }
            else
            {
                AuthenticationRequestParameters.RequestContext.Logger.Info(
                    "[ClientCredentialRequest] A cached token was found, but its hash does NOT match AccessTokenHashToRefresh. Using the cached token.");
                return false;
            }
        }

        /// <summary>
        /// Marks the current access token retrieval as a successful cache hit, 
        /// and increments any relevant telemetry or counters.
        /// </summary>
        private void MarkAccessTokenAsCacheHit()
        {
            // Mark the request as a cache hit
            AuthenticationRequestParameters.RequestContext.ApiEvent.IsAccessTokenCacheHit = true;
            Metrics.IncrementTotalAccessTokensFromCache();
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
