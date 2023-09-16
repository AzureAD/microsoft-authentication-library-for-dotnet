// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Cache
{
    /// <summary>
    /// Identifies the type of cache that the token was read from.
    /// </summary>
    /// <remarks>
    /// Token cache serialization implementations must provide this value.
    /// </remarks>
    public enum CacheLevel
    {
        /// <summary>
        /// Indicates that the token was retrieved from the identity provider.
        /// </summary>
        None = 0,
        /// <summary>
        /// Indicates that the cache level used is unknown.
        /// Token was retrieved from cache but the token cache implementation didn't specify which cache level was used.
        /// </summary>
        Unknown = 1,
        /// <summary>
        /// Indicates that the token was read from the L1 cache.
        /// </summary>
        L1Cache = 2,
        /// <summary>
        /// Indicates that the token was read from the L2 cache.
        /// </summary>
        L2Cache = 3
    }
}
