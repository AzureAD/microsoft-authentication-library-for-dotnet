// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.;

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache.CacheImpl;

namespace WebApi.Misc
{
    internal interface ICacheSerializationProvider
    {
        // Important - do not use SetBefore / SetAfter methods, as these are reserved for app developers
        // Instead, use AfterAccess = x, BeforeAccess = y
        // See UapTokenCacheBlobStorage for an example
        void Initialize(ITokenCache tokenCache);
    }

    /// <summary>
    /// A token cache base that is useful for ConfidentialClient scenarios, as it partitions the cache using the SuggestedWebKey
    /// </summary>
    internal abstract class AbstractPartitionedCacheSerializer : ICacheSerializationProvider
    {

        /// <summary>
        /// Important - do not use SetBefore / SetAfter methods, as these are reserved for app developers
        /// Instead, use AfterAccess = x, BeforeAccess = y        
        /// </summary>
        public void Initialize(ITokenCache tokenCache)
        {
            if (tokenCache == null)
            {
                throw new ArgumentNullException(nameof(tokenCache));
            }

            tokenCache.SetBeforeAccess(OnBeforeAccess);

            tokenCache.SetAfterAccess(OnAfterAccess);
        }

        /// <summary>
        /// Raised AFTER MSAL added the new token in its in-memory copy of the cache.
        /// This notification is called every time MSAL accesses the cache, not just when a write takes place:
        /// If MSAL's current operation resulted in a cache change, the property TokenCacheNotificationArgs.HasStateChanged will be set to true.
        /// If that is the case, we call the TokenCache.SerializeMsalV3() to get a binary blob representing the latest cache content – and persist it.
        /// </summary>
        /// <param name="args">Contains parameters used by the MSAL call accessing the cache.</param>
        private void OnAfterAccess(TokenCacheNotificationArgs args)
        {
            // The access operation resulted in a cache update.
            if (args.HasStateChanged)
            {
                if (args.HasTokens)
                {
                    WriteCacheBytes(args.SuggestedCacheKey, args.TokenCache.SerializeMsalV3());
                }
                else
                {
                    // No token in the cache. we can remove the cache entry
                    RemoveKey(args.SuggestedCacheKey);
                }
            }
        }

        private void OnBeforeAccess(TokenCacheNotificationArgs args)
        {
            if (!string.IsNullOrEmpty(args.SuggestedCacheKey))
            {
                byte[] tokenCacheBytes = ReadCacheBytes(args.SuggestedCacheKey);
                args.TokenCache.DeserializeMsalV3(tokenCacheBytes, shouldClearExistingCache: true);
            }
        }

        /// <summary>
        /// Clear the cache.
        /// </summary>
        /// <param name="homeAccountId">HomeAccountId for a user account in the cache.</param>
        /// <returns>A <see cref="Task"/> that represents a completed clear operation.</returns>
        public void ClearAsync(string homeAccountId)
        {
            // This is a user token cache
            RemoveKey(homeAccountId);
        }

        /// <summary>
        /// Method to be implemented by concrete cache serializers to write the cache bytes.
        /// </summary>
        /// <param name="cacheKey">Cache key.</param>
        /// <param name="bytes">Bytes to write.</param>
        /// <returns>A <see cref="Task"/> that represents a completed write operation.</returns>
        protected abstract void WriteCacheBytes(string cacheKey, byte[] bytes);

        /// <summary>
        /// Method to be implemented by concrete cache serializers to Read the cache bytes.
        /// </summary>
        /// <param name="cacheKey">Cache key.</param>
        /// <returns>Read bytes.</returns>
        protected abstract byte[] ReadCacheBytes(string cacheKey);

        /// <summary>
        /// Method to be implemented by concrete cache serializers to remove an entry from the cache.
        /// </summary>
        /// <param name="cacheKey">Cache key.</param>
        /// <returns>A <see cref="Task"/> that represents a completed remove key operation.</returns>
        protected abstract void RemoveKey(string cacheKey);
    }
}

