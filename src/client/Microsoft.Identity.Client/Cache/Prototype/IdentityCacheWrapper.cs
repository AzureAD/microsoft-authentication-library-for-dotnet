// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Identity.ServiceEssentials;
using Microsoft.Identity.ServiceEssentials.IdentityCache;
using Microsoft.IdentityModel.Abstractions;

namespace Microsoft.Identity.Client.Cache.Prototype
{
    internal class IdentityCacheWrapper
    {
        private static CacheOptions s_cacheOptions;
        private readonly IIdentityCache _identityCache;
        private static IIdentityLogger _identityLogger;
        private static readonly Lazy<IIdentityCache> s_defaultIIdentityCache = new Lazy<IIdentityCache>(
            () => CreateDefaultCache());
        private const string CategoryName = "tokens";

        // This cache instance (whether provided by the user or default one) will only ever be called/used if cache serialization is not enabled.
        // There are three options for this cache: user-provided, static default, non-static default.
        // User-provided cache takes precedence.
        // Default cache is created lazily (since it's possible that token cache serialization is enabled)
        internal IdentityCacheWrapper(CacheOptions cacheOptions, IIdentityLogger identityLogger)
        {
            s_cacheOptions = cacheOptions;
            _identityLogger = identityLogger;
            // Set (or overwrite) cache to user-specified implementation, otherwise set to default implementation, if not already set.

            if (cacheOptions.IdentityCache != null)
            {
                _identityCache = cacheOptions?.IdentityCache;
            }
            else if (cacheOptions.UseSharedCache)
            {
                _identityCache = s_defaultIIdentityCache.Value;
            }
            else
            {
                _identityCache = CreateDefaultCache();
            }
        }

        private static IIdentityCache CreateDefaultCache()
        {
            var memoryCacheOptions = new InMemoryCacheOptions()
            {
                MaxNumberOfItemsForCategory = new Dictionary<string, int>()
                {
                    { CategoryName, s_cacheOptions.SizeLimit },
                }
            };

            return new IdentityCachePrototype(memoryCacheOptions, _identityLogger, null);
        }

        internal async Task<T> GetAsync<T>(string key) where T : ICacheObject
        {
            var entry = await _identityCache.GetAsync<T>(CategoryName, key).ConfigureAwait(false);
            return entry == null ? default : entry.Value;
        }

        internal async Task SetAsync<T>(string key, T value, DateTimeOffset? cacheExpiry) where T : ICacheObject
        {
            TimeSpan expirationTimeRelativeToNow = cacheExpiry.HasValue ? cacheExpiry.Value - DateTimeOffset.UtcNow : TimeSpan.FromHours(1);
            await _identityCache.SetAsync(CategoryName, key, value, new CacheEntryOptions(expirationTimeRelativeToNow, 1)).ConfigureAwait(false);
        }
    }
}
