// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.ServiceEssentials
{
    /// <summary>
    /// Represents a cache item.
    /// </summary>
    public interface ICacheEntry<T>
    {
        /// <summary>
        /// Gets the value in cache.
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// Checks if <see cref="CacheEntry{T}"/> is found in a cache
        /// and if it is still within its time-to-live.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the <see cref="CacheEntry{T}.Value"/> is found and
        /// is still within its time-to-live, <see langword="false"/> otherwise.
        /// </returns>
        bool IsValid();

        /// <summary>
        /// Checks if <see cref="CacheEntry{T}"/> is found in a cache
        /// and can be used as a last-known-good value.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the <see cref="CacheEntry{T}.Value"/> is found and
        /// can be used as a last-known-good value, <see langword="false"/> otherwise.
        /// </returns>
        bool IsValidAsLastKnownGood();
    }
}
