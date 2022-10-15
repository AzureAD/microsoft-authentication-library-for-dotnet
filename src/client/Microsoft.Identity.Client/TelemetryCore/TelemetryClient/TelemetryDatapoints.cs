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
        /// Total latency of L1 cache access. This data is captured in MSAL when accessing the internal cache or Microsoft.Identity.Web when accessing the memory cache.
        /// </summary>
        public long L1Latency { get; set; }

        /// <summary>
        /// Total latency of L2 cache access. This data is captured in Microsoft.Identity.Web when accessing the distributed cache.
        /// </summary>
        public long L2Latency { get; set; }
    }
}
