// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Cache;
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
        public CacheTypeUsed? CacheTypeUsed { get; set; }
    }
}
