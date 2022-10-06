// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.IdentityModel.Abstractions;

namespace Microsoft.Identity.Client.TelemetryCore.TelemetryClient
{
    /// <summary>
    /// Contains the parameters of cache to log to telemetry.
    /// </summary>
    public class TelemetryCacheConstants
    {
        /// <summary>
        /// Constant to use as key to Cache Details dictionary.
        /// </summary>
        public const string CacheUsed = "CacheUsed";

        /// <summary>
        /// Constant to use as key to Cache Details dictionary.
        /// </summary>
        public const string L1Latency = "L1Latency";

        /// <summary>
        /// Constant to use as key to Cache Details dictionary.
        /// </summary>
        public const string L2Latency = "L2Latency";
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
        /// Token was obtained from L1
        /// </summary>
        L1 = 1,

        /// <summary>
        /// Token was obtained from L2
        /// </summary>
        L2 = 2
    }
}
