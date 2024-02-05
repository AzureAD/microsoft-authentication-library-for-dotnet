// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal abstract class MsiAuthRequest : RequestBase
    {
        protected readonly AcquireTokenForManagedIdentityParameters _managedIdentityParameters;
        protected static readonly SemaphoreSlim s_semaphoreSlim = new(1, 1);

        protected MsiAuthRequest(
            IServiceBundle serviceBundle,
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenForManagedIdentityParameters managedIdentityParameters)
            : base(serviceBundle, authenticationRequestParameters, managedIdentityParameters)
        {
            _managedIdentityParameters = managedIdentityParameters;
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
            AuthenticationResult authResult = null;

            IKeyMaterialManager keyMaterial = ServiceBundle.Config.KeyMaterialManagerForTest ??
                                              AuthenticationRequestParameters.RequestContext.ServiceBundle.PlatformProxy.GetKeyMaterialManager();

            // Skip checking cache for force refresh or when claims are present
            if (_managedIdentityParameters.ForceRefresh || !string.IsNullOrEmpty(_managedIdentityParameters.Claims))
            {
                AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo = CacheRefreshReason.ForceRefreshOrClaims;
                logger.Info("[ManagedIdentityRequest] Skipped looking for a cached access token because ForceRefresh or Claims was set.");

                // Managed Identity Client Certificate check 
                if (ServiceBundle.Config.ManagedIdentityClientCertificate != null && 
                    ServiceBundle.Config.ManagedIdentityClientCertificate.Thumbprint != keyMaterial.BindingCertificate.Thumbprint)
                {
                    logger.Info("[ManagedIdentityRequest] Managed Identity Client Certificate has been renewed.");
                }

                authResult = await GetAccessTokenAsync(keyMaterial, cancellationToken, logger).ConfigureAwait(false);
                return authResult;
            }

            // Check cache for AT 
            MsalAccessTokenCacheItem cachedAccessTokenItem = await GetCachedAccessTokenAsync().ConfigureAwait(false);

            if (cachedAccessTokenItem != null)
            {
                authResult = CreateAuthenticationResultFromCache(cachedAccessTokenItem);
                logger.Info("[ManagedIdentityRequest] Access token retrieved from cache.");

                try
                {
                    var proactivelyRefresh = SilentRequestHelper.NeedsRefresh(cachedAccessTokenItem);

                    // Proactive refresh logic 
                    if (proactivelyRefresh)
                    {
                        AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo = CacheRefreshReason.ProactivelyRefreshed;

                        SilentRequestHelper.ProcessFetchInBackground(
                            cachedAccessTokenItem,
                            () =>
                            {
                                using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                                return GetAccessTokenAsync(keyMaterial, tokenSource.Token, logger);
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
                // No AT in cache
                if (AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo != CacheRefreshReason.Expired)
                {
                    AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo = CacheRefreshReason.NoCachedAccessToken;
                }

                logger.Info("[ManagedIdentityRequest] No cached access token. Getting a token from the managed identity endpoint.");
                authResult = await GetAccessTokenAsync(keyMaterial, cancellationToken, logger).ConfigureAwait(false);
            }

            return authResult;
        }

        internal AuthenticationResult CreateAuthenticationResultFromCache(MsalAccessTokenCacheItem cachedAccessTokenItem)
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

        internal async Task<MsalAccessTokenCacheItem> GetCachedAccessTokenAsync()
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

        protected abstract Task<AuthenticationResult> GetAccessTokenAsync(IKeyMaterialManager keyMaterial, CancellationToken cancellationToken, ILoggerAdapter logger);

        protected override KeyValuePair<string, string>? GetCcsHeader(IDictionary<string, string> additionalBodyParameters)
        {
            return null;
        }

        // Override method to return a sorted set of scopes based on the input set.
        protected override SortedSet<string> GetOverriddenScopes(ISet<string> inputScopes)
        {
            // Create a new SortedSet from the inputScopes to ensure a consistent and sorted order.
            return new SortedSet<string>(inputScopes);
        }
    }
}
