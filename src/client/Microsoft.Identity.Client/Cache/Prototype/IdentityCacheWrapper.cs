// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Identity.ServiceEssentials;

namespace Microsoft.Identity.Client.Cache.Prototype
{
    internal class IdentityCacheWrapper
    {
        internal static IIdentityCache s_iIdentityCache { get; set; }

        internal IdentityCacheWrapper(CacheOptions cacheOptions)
        {
            // Set (or overwrite) cache to user-specified implementation, otherwise set to default implementation, if not already set.
            s_iIdentityCache = cacheOptions?.IdentityCache ?? s_iIdentityCache ?? CreateDefaultCache(cacheOptions);
        }

        private IIdentityCache CreateDefaultCache(CacheOptions cacheOptions) => new DefaultInMemoryCache(cacheOptions);

        internal async Task<T> GetAsync<T>(string key)
        {
            var entry = await s_iIdentityCache.GetAsync<T>(string.Empty, key).ConfigureAwait(false);
            return entry == null ? default : entry.Value;
        }

        internal async Task SetAsync<T>(string key, T value, DateTimeOffset? cacheExpiry)
        {
            TimeSpan timeToExpire = cacheExpiry.HasValue ? cacheExpiry.Value - DateTimeOffset.UtcNow : TimeSpan.FromHours(1);
            await s_iIdentityCache.SetAsync<T>(string.Empty, key, value, new CacheEntryOptions(timeToExpire)).ConfigureAwait(false);
        }
    }
}
