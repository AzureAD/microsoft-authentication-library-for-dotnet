// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.AuthScheme;
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

            if (skipCache)
            {
                AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo = CacheRefreshReason.ForceRefreshOrClaims;
                logger.Info("[ClientCredentialRequest] Skipped looking for a cached access token because ForceRefresh was requested, or there are Claims but no AccessTokenHashToRefresh.");
                authResult = await GetAccessTokenAsync(cancellationToken, logger).ConfigureAwait(false);
                return authResult;
            }

            MsalAccessTokenCacheItem cachedAccessTokenItem = await GetCachedAccessTokenAsync().ConfigureAwait(false);

            // Validate the cached token using the authentication operation
            if (AuthenticationRequestParameters.AuthenticationScheme != null &&
                cachedAccessTokenItem != null &&
                AuthenticationRequestParameters.AuthenticationScheme is IAuthenticationOperation2 authOp2)
            {
                var cacheValidationData = new MsalCacheValidationData();
                cacheValidationData.PersistedCacheParameters = cachedAccessTokenItem.PersistedCacheParameters;

                if (!await authOp2.ValidateCachedTokenAsync(cacheValidationData).ConfigureAwait(false))
                {
                    logger.Info("[ClientCredentialRequest] Cached token failed authentication operation validation.");
                    cachedAccessTokenItem = null;
                }
            }

            // No access token or cached access token needs to be refreshed 
            if (cachedAccessTokenItem != null)
            {
                authResult = await CreateAuthenticationResultFromCacheAsync(cachedAccessTokenItem).ConfigureAwait(false);

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
                    return await HandleTokenRefreshErrorAsync(e, cachedAccessTokenItem, cancellationToken).ConfigureAwait(false);
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

            AuthenticationResult authResult = null;
            int retryCount = 0;

            // Retry loop using the retry callback if configured
            while (true)
            {
                try
                {
                    // Get a token from AAD
                    if (ServiceBundle.Config.AppTokenProvider == null)
                    {
                        logger.Verbose(() => "[ClientCredentialRequest] Sending token request to AAD.");
                        MsalTokenResponse msalTokenResponse = await SendTokenRequestAsync(GetBodyParameters(), cancellationToken).ConfigureAwait(false);
                        authResult = await CacheTokenResponseAndCreateAuthenticationResultAsync(msalTokenResponse, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        // Get a token from the app provider delegate
                        authResult = await GetAccessTokenFromAppProviderAsync(cancellationToken, logger)
                            .ConfigureAwait(false);
                    }

                    // Success - invoke OnCompletion callback if configured
                    await InvokeOnCompletionCallbackAsync(authResult, exception: null, logger).ConfigureAwait(false);
                    
                    return authResult;
                }
                catch (MsalServiceException serviceEx)
                {
                    // Check if OnMsalServiceFailure is configured
                    if (AuthenticationRequestParameters.AppConfig.OnMsalServiceFailure != null)
                    {
                        logger.Info("[ClientCredentialRequest] MsalServiceException caught. Invoking OnMsalServiceFailure.");
                        
                        bool shouldRetry = await InvokeOnMsalServiceFailureCallbackAsync(serviceEx, logger)
                            .ConfigureAwait(false);
                        
                        if (shouldRetry)
                        {
                            retryCount++;
                            logger.Info($"[ClientCredentialRequest] OnMsalServiceFailure returned true. Retrying token request (Retry #{retryCount}).");
                            continue; // Retry the loop
                        }
                        
                        logger.Info("[ClientCredentialRequest] OnMsalServiceFailure returned false. Propagating exception.");
                    }
                    
                    // Invoke OnCompletion callback with failure result
                    await InvokeOnCompletionCallbackAsync(authResult: null, exception: serviceEx, logger).ConfigureAwait(false);
                    
                    // Re-throw if no callback or callback returned false
                    throw;
                }
                catch (MsalException ex)
                {
                    // For non-service exceptions (MsalClientException, etc.), invoke OnCompletion and re-throw
                    await InvokeOnCompletionCallbackAsync(authResult: null, exception: ex, logger).ConfigureAwait(false);
                    throw;
                }
            }
        }

        private async Task<AuthenticationResult> GetAccessTokenFromAppProviderAsync(
            CancellationToken cancellationToken,
            ILoggerAdapter logger)
        {
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
                        authResult = await CreateAuthenticationResultFromCacheAsync(cachedAccessTokenItem).ConfigureAwait(false);
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

        /// <summary>
        /// Invokes the OnMsalServiceFailure if configured.
        /// Returns true if the request should be retried, false otherwise.
        /// </summary>
        private async Task<bool> InvokeOnMsalServiceFailureCallbackAsync(
            MsalServiceException serviceException,
            ILoggerAdapter logger)
        {
            try
            {
                var tokenEndpoint = await AuthenticationRequestParameters.Authority.GetTokenEndpointAsync(AuthenticationRequestParameters.RequestContext).ConfigureAwait(false);
                var options = new AssertionRequestOptions(
                    AuthenticationRequestParameters.AppConfig, 
                    tokenEndpoint,
                    AuthenticationRequestParameters.AuthorityManager.Authority.TenantId);
                
                var executionResult = new ExecutionResult
                {
                    Successful = false,
                    Result = null,
                    Exception = serviceException,
                    ClientCertificate = AuthenticationRequestParameters.ResolvedCertificate
                };
                
                bool shouldRetry = await AuthenticationRequestParameters.AppConfig
                    .OnMsalServiceFailure(options, executionResult)
                    .ConfigureAwait(false);
                
                logger.Verbose(() => $"[ClientCredentialRequest] OnMsalServiceFailure returned: {shouldRetry}");
                return shouldRetry;
            }
            catch (Exception ex)
            {
                // If the callback throws, log and don't retry
                logger.Error($"[ClientCredentialRequest] OnMsalServiceFailure threw an exception: {ex.Message}");
                logger.ErrorPii(ex);
                return false;
            }
        }

        /// <summary>
        /// Invokes the OnCompletion if configured.
        /// Exceptions from the callback are caught and logged to prevent disrupting the authentication flow.
        /// </summary>
        private async Task InvokeOnCompletionCallbackAsync(
            AuthenticationResult authResult,
            MsalException exception,
            ILoggerAdapter logger)
        {
            if (AuthenticationRequestParameters.AppConfig.OnCompletion == null)
            {
                return;
            }

            try
            {
                logger.Verbose(() => "[ClientCredentialRequest] Invoking OnCompletion callback.");
                
                var tokenEndpoint = await AuthenticationRequestParameters.Authority.GetTokenEndpointAsync(AuthenticationRequestParameters.RequestContext).ConfigureAwait(false);
                var options = new AssertionRequestOptions(
                    AuthenticationRequestParameters.AppConfig, 
                    tokenEndpoint,
                    AuthenticationRequestParameters.AuthorityManager.Authority.TenantId);
                
                var executionResult = new ExecutionResult
                {
                    Successful = authResult != null,
                    Result = authResult,
                    Exception = exception,
                    ClientCertificate = AuthenticationRequestParameters.ResolvedCertificate
                };
                
                await AuthenticationRequestParameters.AppConfig
                    .OnCompletion(options, executionResult)
                    .ConfigureAwait(false);
                
                logger.Verbose(() => "[ClientCredentialRequest] OnCompletion callback completed successfully.");
            }
            catch (Exception ex)
            {
                // Catch and log any exceptions from the observer callback
                // Do not propagate - observer should not disrupt authentication flow
                logger.Error($"[ClientCredentialRequest] OnCompletion callback threw an exception: {ex.Message}");
                logger.ErrorPii(ex);
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

            AuthenticationResult authResult = await CacheTokenResponseAndCreateAuthenticationResultAsync(tokenResponse, cancellationToken)
                .ConfigureAwait(false);

            return authResult;
        }

        /// <summary>
        /// Checks if the token should be used from the cache and returns the cached access token if applicable.
        /// </summary>
        /// <returns></returns>
        private async Task<MsalAccessTokenCacheItem> GetCachedAccessTokenAsync()
        {
            // Fetch the cache item (could be null if none found).
            MsalAccessTokenCacheItem cacheItem =
                await CacheManager.FindAccessTokenAsync().ConfigureAwait(false);

            // If the item fails any checks (null, or hash mismatch),
            if (!ShouldUseCachedToken(cacheItem))
            {
                return null;
            }

            // Otherwise, record a successful cache hit and return the token.
            MarkAccessTokenAsCacheHit();
            return cacheItem;
        }

        /// <summary>
        /// Checks if the token should be used from the cache.
        /// </summary>
        /// <param name="cacheItem"></param>
        /// <returns></returns>
        private bool ShouldUseCachedToken(MsalAccessTokenCacheItem cacheItem)
        {
            // 1) No cached item 
            if (cacheItem == null)
            {
                return false;
            }

            // 2) If an mTLS cert is supplied for THIS request, reuse cache only if
            //    the cached token's KeyId matches the one provided in the request.
            X509Certificate2 requestCert = AuthenticationRequestParameters.MtlsCertificate;

            if (requestCert != null && AuthenticationRequestParameters.IsMtlsPopRequested)
            {
                string expectedKid = CoreHelpers.ComputeX5tS256KeyId(requestCert);

                // If the certificate cannot produce a valid KeyId (SPKI-SHA256), expectedKid will be null or empty.
                // In this case, the cache will be bypassed, as we cannot safely match the cached token to the certificate.
                if (!string.Equals(cacheItem.KeyId, expectedKid, StringComparison.Ordinal))
                {
                    AuthenticationRequestParameters.RequestContext.Logger.Verbose(() =>
                    "[ClientCredentialRequest] Cached token KeyId does not match request certificate (SPKI-SHA256 mismatch). Bypassing cache.");
                    return false;
                }
                
                AuthenticationRequestParameters.RequestContext.Logger.Verbose(() =>
                "[ClientCredentialRequest] Cached token KeyId matches request certificate (SPKI-SHA256). Using cached token.");
            }

            // 3) If the token's hash matches AccessTokenHashToRefresh, ignore it
            if (!string.IsNullOrEmpty(_clientParameters.AccessTokenHashToRefresh) &&
                IsMatchingTokenHash(cacheItem.Secret, _clientParameters.AccessTokenHashToRefresh))
            {
                AuthenticationRequestParameters.RequestContext.Logger.Info(
                    "[ClientCredentialRequest] A cached token was found and its hash matches AccessTokenHashToRefresh, so it is ignored.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if the token hash matches the hash provided in AccessTokenHashToRefresh.
        /// </summary>
        /// <param name="tokenSecret"></param>
        /// <param name="accessTokenHashToRefresh"></param>
        /// <returns></returns>
        private bool IsMatchingTokenHash(string tokenSecret, string accessTokenHashToRefresh)
        {
            string cachedTokenHash = _cryptoManager.CreateSha256HashHex(tokenSecret);
            return string.Equals(cachedTokenHash, accessTokenHashToRefresh, StringComparison.Ordinal);
        }

        /// <summary>
        /// Marks the request as a cache hit and increments the cache hit count.
        /// </summary>
        private void MarkAccessTokenAsCacheHit()
        {
            AuthenticationRequestParameters.RequestContext.ApiEvent.IsAccessTokenCacheHit = true;
            Metrics.IncrementTotalAccessTokensFromCache();
        }

        /// <summary>
        /// returns the cached access token item 
        /// </summary>
        /// <param name="cachedAccessTokenItem"></param>
        /// <returns></returns>
        private Task<AuthenticationResult> CreateAuthenticationResultFromCacheAsync(MsalAccessTokenCacheItem cachedAccessTokenItem)
        {
            return AuthenticationResult.CreateAsync(
                cachedAccessTokenItem,
                null,
                AuthenticationRequestParameters.AuthenticationScheme,
                AuthenticationRequestParameters.RequestContext.CorrelationId,
                TokenSource.Cache,
                AuthenticationRequestParameters.RequestContext.ApiEvent,
                account: null,
                spaAuthCode: null,
                additionalResponseParameters: null);
        }

        /// <summary>
        /// Gets overridden scopes for client credentials flow
        /// </summary>
        /// <param name="inputScopes"></param>
        /// <returns></returns>
        protected override SortedSet<string> GetOverriddenScopes(ISet<string> inputScopes)
        {
            // Client credentials should not add the reserved scopes
            // ("openid", "profile", and "offline_access")
            // because the access token is on behalf of an app (no profile, no ID token, no refresh token)
            return new SortedSet<string>(inputScopes);
        }

        /// <summary>
        /// Gets the body parameters for the client credentials flow
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, string> GetBodyParameters()
        {
            var dict = new Dictionary<string, string>
            {
                [OAuth2Parameter.GrantType] = OAuth2GrantType.ClientCredentials,
                [OAuth2Parameter.Scope] = AuthenticationRequestParameters.Scope.AsSingleString(),
                [OAuth2Parameter.ClientInfo] = "2"
            };

            return dict;
        }

        /// <summary>
        /// Gets the CCS header for the client credentials flow
        /// </summary>
        /// <param name="additionalBodyParameters"></param>
        /// <returns></returns>
        protected override KeyValuePair<string, string>? GetCcsHeader(IDictionary<string, string> additionalBodyParameters)
        {
            return null;
        }
    }
}
