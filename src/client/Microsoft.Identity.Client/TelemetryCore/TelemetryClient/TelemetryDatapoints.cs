// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.IdentityModel.Abstractions;

namespace Microsoft.Identity.Client.TelemetryCore.TelemetryClient
{
    /// <summary>
    /// Stores the cache details to log to <see cref="ITelemetryClient"/>.
    /// </summary>
    public class TelemetryDatapoints
    {
        /// <summary>
        /// Cache used to fetch the token.
        /// </summary>
        public CacheUsed CacheUsed { get; set; }

        /// <summary>
        /// Total latency of L1 cache access.
        /// </summary>
        public long L1Latency { get; set; }

        /// <summary>
        /// Total latency of L2 cache access.
        /// </summary>
        public long L2Latency { get; set; }
    }

    /// <summary>
    /// Indicates the cache which was used to get the token.
    /// </summary>
    public enum CacheUsed
    {
        /// <summary>
        /// Indicates that the token was not cached
        /// </summary>
        None = 0,

        /// <summary>
        /// Token was obtained from Memory Cache
        /// </summary>
        MemoryCache = 1,

        /// <summary>
        /// Token was obtained from Distributed Cache
        /// </summary>
        DistributedCache = 2
    }
}
