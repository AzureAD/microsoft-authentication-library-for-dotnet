// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Test.Performance.Helpers
{
    public class InMemoryCache
    {
        private readonly ConcurrentDictionary<string, byte[]> _memoryCache = new ConcurrentDictionary<string, byte[]>();

        public InMemoryCache(ITokenCache tokenCache)
        {
            tokenCache?.SetBeforeAccess(BeforeAccessHandler);
            tokenCache?.SetAfterAccess(AfterAccessHandler);
        }

        /// <summary>
        /// Triggered right before MSAL needs to access the cache.
        /// Reload the internal cache from the external store in case it changed since the last access.
        /// </summary>
        /// <param name="args">Contains parameters used by the MSAL call accessing the cache.</param>
        private void BeforeAccessHandler(TokenCacheNotificationArgs args)
        {
            if (!string.IsNullOrEmpty(args.SuggestedCacheKey))
            {
                _memoryCache.TryGetValue(args.SuggestedCacheKey, out byte[] tokenCacheBytes);
                args.TokenCache.DeserializeMsalV3(tokenCacheBytes, shouldClearExistingCache: true);
            }
        }

        /// <summary>
        /// Triggered right after MSAL accessed the cache to persist changes into external store.
        /// </summary>
        /// <param name="args">Contains parameters used by the MSAL call accessing the cache.</param>
        private void AfterAccessHandler(TokenCacheNotificationArgs args)
        {
            if (args.HasStateChanged)
            {
                if (args.HasTokens)
                {
                    _memoryCache.TryAdd(args.SuggestedCacheKey, args.TokenCache.SerializeMsalV3());
                }
                else
                {
                    _memoryCache.TryRemove(args.SuggestedCacheKey, out _);
                }
            }
        }
    }
}
