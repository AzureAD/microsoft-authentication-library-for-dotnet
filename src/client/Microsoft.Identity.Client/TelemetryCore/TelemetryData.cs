// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Cache;
using Microsoft.IdentityModel.Abstractions;

namespace Microsoft.Identity.Client.TelemetryCore.TelemetryClient
{
    /// <summary>
    /// Stores details to log to the <see cref="ITelemetryClient"/>.
    /// </summary>
    public class TelemetryData
    {
        /// <summary>
        /// Type of cache used. This data is captured from MSAL or Microsoft.Identity.Web to log to telemetry.
        /// </summary>
        public CacheLevel CacheLevel { get; set; } = CacheLevel.None;
    }
}
