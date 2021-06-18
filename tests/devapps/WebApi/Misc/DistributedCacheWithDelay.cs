using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace WebApi.Misc
{
    public class CacheSerializerHints
    {
        /// <summary>
        /// CancellationToken enabling cooperative cancellation between threads, thread pool, or Task objects.
        /// </summary>
        public CancellationToken CancellationToken { get; set; }
    }

    public abstract class MsalAbstractTokenCacheProvider
    {
        /// <summary>
        /// Initializes the token cache serialization.
        /// </summary>
        /// <param name="tokenCache">Token cache to serialize/deserialize.</param>
        public void Initialize(ITokenCache tokenCache)
        {
            if (tokenCache == null)
            {
                throw new ArgumentNullException(nameof(tokenCache));
            }

            tokenCache.SetBeforeAccessAsync(OnBeforeAccessAsync);
            tokenCache.SetAfterAccessAsync(OnAfterAccessAsync);
            tokenCache.SetBeforeWriteAsync(OnBeforeWriteAsync);
        }

        /// <summary>
        /// Initializes the token cache serialization.
        /// </summary>
        /// <param name="tokenCache">Token cache to serialize/deserialize.</param>
        /// <returns>A <see cref="Task"/> that represents a completed initialization operation.</returns>
        public Task InitializeAsync(ITokenCache tokenCache)
        {
            Initialize(tokenCache);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Raised AFTER MSAL added the new token in its in-memory copy of the cache.
        /// This notification is called every time MSAL accesses the cache, not just when a write takes place:
        /// If MSAL's current operation resulted in a cache change, the property TokenCacheNotificationArgs.HasStateChanged will be set to true.
        /// If that is the case, we call the TokenCache.SerializeMsalV3() to get a binary blob representing the latest cache content – and persist it.
        /// </summary>
        /// <param name="args">Contains parameters used by the MSAL call accessing the cache.</param>
        private async Task OnAfterAccessAsync(TokenCacheNotificationArgs args)
        {
            // The access operation resulted in a cache update.
            if (args.HasStateChanged)
            {
                CacheSerializerHints cacheSerializerHints = CreateHintsFromArgs(args);

                if (args.HasTokens)
                {
                    await WriteCacheBytesAsync(args.SuggestedCacheKey, args.TokenCache.SerializeMsalV3(), cacheSerializerHints).ConfigureAwait(false);
                }
                else
                {
                    // No token in the cache. we can remove the cache entry
                    await RemoveKeyAsync(args.SuggestedCacheKey, cacheSerializerHints).ConfigureAwait(false);
                }
            }
        }

        private static CacheSerializerHints CreateHintsFromArgs(TokenCacheNotificationArgs args)
        {
            return new CacheSerializerHints { CancellationToken = args.CancellationToken };
        }

        private async Task OnBeforeAccessAsync(TokenCacheNotificationArgs args)
        {
            if (!string.IsNullOrEmpty(args.SuggestedCacheKey))
            {
                byte[] tokenCacheBytes = await ReadCacheBytesAsync(args.SuggestedCacheKey, CreateHintsFromArgs(args)).ConfigureAwait(false);
                args.TokenCache.DeserializeMsalV3(tokenCacheBytes, shouldClearExistingCache: true);
            }
        }

        /// <summary>
        /// If you want to ensure that no concurrent write takes place, use this notification to place a lock on the entry.
        /// </summary>
        /// <param name="args">Token cache notification arguments.</param>
        /// <returns>A <see cref="Task"/> that represents a completed operation.</returns>
        protected virtual Task OnBeforeWriteAsync(TokenCacheNotificationArgs args)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Clear the cache.
        /// </summary>
        /// <param name="homeAccountId">HomeAccountId for a user account in the cache.</param>
        /// <returns>A <see cref="Task"/> that represents a completed clear operation.</returns>
        public async Task ClearAsync(string homeAccountId)
        {
            // This is a user token cache
            await RemoveKeyAsync(homeAccountId).ConfigureAwait(false);

            // TODO: Clear the cookie session if any. Get inspiration from
            // https://github.com/Azure-Samples/active-directory-aspnetcore-webapp-openidconnect-v2/issues/240
        }

        /// <summary>
        /// Method to be implemented by concrete cache serializers to write the cache bytes.
        /// </summary>
        /// <param name="cacheKey">Cache key.</param>
        /// <param name="bytes">Bytes to write.</param>
        /// <returns>A <see cref="Task"/> that represents a completed write operation.</returns>
        protected abstract Task WriteCacheBytesAsync(string cacheKey, byte[] bytes);

        /// <summary>
        /// Method to be overridden by concrete cache serializers to write the cache bytes.
        /// </summary>
        /// <param name="cacheKey">Cache key.</param>
        /// <param name="bytes">Bytes to write.</param>
        /// <param name="cacheSerializerHints">Hints for the cache serialization implementation optimization.</param>
        /// <returns>A <see cref="Task"/> that represents a completed write operation.</returns>
        protected virtual Task WriteCacheBytesAsync(string cacheKey, byte[] bytes, CacheSerializerHints cacheSerializerHints)
        {
            return WriteCacheBytesAsync(cacheKey, bytes); // default implementation avoids a breaking change.
        }

        /// <summary>
        /// Method to be implemented by concrete cache serializers to Read the cache bytes.
        /// </summary>
        /// <param name="cacheKey">Cache key.</param>
        /// <returns>Read bytes.</returns>
        protected abstract Task<byte[]> ReadCacheBytesAsync(string cacheKey);

        /// <summary>
        /// Method to be overridden by concrete cache serializers to Read the cache bytes.
        /// </summary>
        /// <param name="cacheKey">Cache key.</param>
        /// <param name="cacheSerializerHints">Hints for the cache serialization implementation optimization.</param>
        /// <returns>Read bytes.</returns>
        protected virtual Task<byte[]> ReadCacheBytesAsync(string cacheKey, CacheSerializerHints cacheSerializerHints)
        {
            return ReadCacheBytesAsync(cacheKey); // default implementation avoids a breaking change.
        }

        /// <summary>
        /// Method to be implemented by concrete cache serializers to remove an entry from the cache.
        /// </summary>
        /// <param name="cacheKey">Cache key.</param>
        /// <returns>A <see cref="Task"/> that represents a completed remove key operation.</returns>
        protected abstract Task RemoveKeyAsync(string cacheKey);

        /// <summary>
        /// Method to be overridden by concrete cache serializers to remove an entry from the cache.
        /// </summary>
        /// <param name="cacheKey">Cache key.</param>
        /// <param name="cacheSerializerHints">Hints for the cache serialization implementation optimization.</param>
        /// <returns>A <see cref="Task"/> that represents a completed remove key operation.</returns>
        protected virtual Task RemoveKeyAsync(string cacheKey, CacheSerializerHints cacheSerializerHints)
        {
            return RemoveKeyAsync(cacheKey); // default implementation avoids a breaking change.
        }
    }

    public class DistributedCacheWithDelay : MsalAbstractTokenCacheProvider
    {
        ConcurrentDictionary<string, byte[]> _data = new ConcurrentDictionary<string, byte[]>();
        private int _cacheAccessPenaltyMs;

        public DistributedCacheWithDelay(int penalty)
        {
            _cacheAccessPenaltyMs = penalty;
        }

        protected override async Task<byte[]> ReadCacheBytesAsync(string cacheKey)
        {
            await Task.Delay(_cacheAccessPenaltyMs).ConfigureAwait(false);

            if (_data.TryGetValue(cacheKey, out byte[] blob))
            {
                return blob;
            }

            return null;
        }

        protected override Task RemoveKeyAsync(string cacheKey)
        {
            bool removed = _data.TryRemove(cacheKey, out _);
            return Task.FromResult(0);
        }

        protected override async Task WriteCacheBytesAsync(string cacheKey, byte[] bytes)
        {
            await Task.Delay(_cacheAccessPenaltyMs).ConfigureAwait(false);

            // As per https://docs.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentdictionary-2?redirectedfrom=MSDN&view=net-5.0#remarks
            // the indexer is ok to store a key/value pair unconditionally          
            _data[cacheKey] = bytes;
        }
    }
}

