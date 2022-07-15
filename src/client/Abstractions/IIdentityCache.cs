// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.ServiceEssentials
{
    /// <summary>
    /// Represents a cache.
    /// </summary>
    public interface IIdentityCache
    {
        /// <summary>
        /// Gets a <see cref="CacheEntry{T}"/> with the given key.
        /// </summary>
        /// <param name="category">The category of the key.</param>
        /// <param name="key">The key for lookup in cache.</param>
        /// <param name="cancellationToken">
        /// Optional. The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.
        /// </param>
        /// <returns>
        /// Async task that returns the <see cref="CacheEntry{T}"/>.
        /// </returns>
        /// <remarks>
        /// </remarks>
        Task<CacheEntry<T>> GetAsync<T>(
            string category, string key, CancellationToken cancellationToken = default)
            where T : ICacheObject;

        /// <summary>
        /// Gets a <see cref="CacheEntry{T}"/> with the given key.
        /// </summary>
        /// <param name="category">The category of the key.</param>
        /// <param name ="key">The key for lookup in cache.</param>
        /// <param name="cancellationToken">
        /// Optional. The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.
        /// </param>
        /// <returns>
        /// </returns>
        /// <remarks>
        /// </remarks>/// 
        Task<CacheEntry<string>> GetAsync(
            string category, string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets the <paramref name="value"/> to the cache. 
        /// </summary>
        /// <param name="category">The category of the key.</param>
        /// <param name="key">The key for lookup in cache.</param>
        /// <param name="value">The value to be cached.</param>
        /// <param name="cacheEntryOptions">Options applied when creating the <see cref="CacheEntry{T}"/>.</param>
        /// <param name="cancellationToken">
        /// Optional. The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.
        /// </param>
        /// <returns>Async<see cref="Task"/>.</returns>
        Task SetAsync<T>(
            string category, string key, T value, CacheEntryOptions cacheEntryOptions, CancellationToken cancellationToken = default)
            where T : ICacheObject;

        /// <summary>
        /// Sets the <paramref name="value"/> to the cache. 
        /// </summary>
        /// <param name="category">The category of the key.</param>
        /// <param name="key">The key for lookup in cache.</param>
        /// <param name="value">The value to be cached.</param>
        /// <param name="cacheEntryOptions">Options applied when creating the <see cref="CacheEntry{T}"/>.</param>
        /// <param name="cancellationToken">
        /// Optional. The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.
        /// </param>
        /// <returns>Async<see cref="Task"/>.</returns>
        Task SetAsync(
            string category, string key, string value, CacheEntryOptions cacheEntryOptions, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes an item from the cache with the given key.
        /// </summary>
        /// <param name="category">The category of the key.</param>
        /// <param name="key">The key for lookup in cache.</param>
        /// <param name="cancellationToken">
        /// Optional. The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.
        /// </param>
        /// <returns>Async<see cref="Task"/>.</returns>
        Task RemoveAsync(
            string category, string key, CancellationToken cancellationToken = default);
    }
}
