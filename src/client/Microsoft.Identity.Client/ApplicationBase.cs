// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Instance.Oidc;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.OAuth2.Throttling;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.Region;

namespace Microsoft.Identity.Client
{
    /// <inheritdoc/>
    public abstract class ApplicationBase : IApplicationBase
    {
        /// <summary>
        /// Default authority used for interactive calls.
        /// </summary>
        internal const string DefaultAuthority = "https://login.microsoftonline.com/common/";

        internal IServiceBundle ServiceBundle { get; }

        internal ApplicationBase(ApplicationConfiguration config)
        {
            ServiceBundle = Internal.ServiceBundle.Create(config);  
        }

        internal virtual async Task<AuthenticationRequestParameters> CreateRequestParametersAsync(
            AcquireTokenCommonParameters commonParameters,
            RequestContext requestContext,
            ITokenCacheInternal cache,
            CancellationToken cancellationToken)
        {
            Instance.Authority authority = await Instance.Authority.CreateAuthorityForRequestAsync(
               requestContext,
               commonParameters.AuthorityOverride).ConfigureAwait(false);

            var cacheKeyComponents = await InitializeCacheKeyComponentsAsync(commonParameters.CacheKeyComponents, cancellationToken).ConfigureAwait(false);

            return new AuthenticationRequestParameters(
                ServiceBundle,
                cache,
                commonParameters,
                requestContext,
                authority,
                cacheKeyComponents: cacheKeyComponents);
        }

        internal async Task<SortedList<string, string>> InitializeCacheKeyComponentsAsync(SortedList<string, Func<CancellationToken, Task<string>>> cacheKeyComponents, CancellationToken cancellationToken)
        {
            if (cacheKeyComponents != null && cacheKeyComponents.Count > 0)
            {
                var initializedCacheKeyComponents = new SortedList<string, string>();

                foreach (var kvp in cacheKeyComponents)
                {
                    if (kvp.Value != null)
                    {
                        initializedCacheKeyComponents.Add(kvp.Key, await kvp.Value.Invoke(cancellationToken).ConfigureAwait(false));
                    }
                }
                return initializedCacheKeyComponents;
            }

            return null;
        }

        internal static void GuardMobileFrameworks()
        {
#if ANDROID || iOS || MAC
            throw new PlatformNotSupportedException(
                "Confidential client and managed identity flows are not available on mobile platforms and on Mac." +
                "See https://aka.ms/msal-net-confidential-availability and https://aka.ms/msal-net-managed-identity for details.");
#endif
        }

        /// <summary>
        /// Resets the SDKs internal state, such as static caches, to facilitate testing. 
        /// This API is meant to be used by other SDKs that build on top of MSAL, and only by test code.
        /// </summary>
        public static void ResetStateForTest()
        {
            NetworkCacheMetadataProvider.ResetStaticCacheForTest();
            RegionManager.ResetStaticCacheForTest();
            OidcRetrieverWithCache.ResetCacheForTest();
            AuthorityManager.ClearValidationCache();
            SingletonThrottlingManager.GetInstance().ResetCache();
            ManagedIdentityClient.ResetSourceForTest();
            AuthorityManager.ClearValidationCache();
            PoPCryptoProviderFactory.Reset();

            InMemoryPartitionedAppTokenCacheAccessor.ClearStaticCacheForTest();
            InMemoryPartitionedUserTokenCacheAccessor.ClearStaticCacheForTest();
        }
    }
}
