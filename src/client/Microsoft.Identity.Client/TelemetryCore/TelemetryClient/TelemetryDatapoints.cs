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
        /// Total latency of L1 cache access. This data is captured in MSAL when accessing the internal cache or Microsoft.Identity.Web when accessing the memory cache.
        /// </summary>
        public long L1Latency { get; set; }

        /// <summary>
        /// Total latency of L2 cache access. This data is captured in Microsoft.Identity.Web when accessing the distributed cache.
        /// </summary>
        public long L2Latency { get; set; }
    }

    /// <summary>
    /// Indicates the cache which was used to get the token.
    /// </summary>
    public enum CacheUsed
    {
        /// <summary>
        /// Indicates that the token was not found in the cache.
        /// </summary>
        CacheMiss = 0,

        /// <summary>
        /// Token was obtained from MSAL's internal cache.
        /// </summary>
        MsalInternalCache = 1,

        /// <summary>
        /// Token was obtained from Memory Cache.
        /// </summary>
        MemoryCache = 2,

        /// <summary>
        /// Token was obtained from Distributed Cache.
        /// </summary>
        DistributedCache = 3,

        /// <summary>
        /// Some other cache was used but no metrics were logged.
        /// </summary>
        ExternalCache = 4
    }
}
