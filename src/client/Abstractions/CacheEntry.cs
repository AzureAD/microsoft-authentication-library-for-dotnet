// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.ServiceEssentials
{
    /// <summary>
    /// Represents a cache item. Can grow if needed.
    /// </summary>
    public class CacheEntry<T>
    {
        /// <summary>
        /// Gets the value in cache.
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// TODO: backpropagate ?? max category count? internal DistributedCacheEntry to carry details (we need it, not anyone else)
        /// </summary>
        public DateTimeOffset ExpirationTimeUTC { get; }

        /// <summary>
        /// </summary>
        public DateTimeOffset RefreshTimeUTC { get; }

        /// <summary>
        /// 
        /// </summary>
        public CacheEntry(T value, DateTimeOffset expirationTimeUTC, DateTimeOffset refreshTimeUTC)
        {
            Value = value;
            ExpirationTimeUTC = expirationTimeUTC;
            RefreshTimeUTC = refreshTimeUTC;
        }
    }
}
