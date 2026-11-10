// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.TelemetryCore.Internal.Events;

namespace Microsoft.Identity.Client.TelemetryCore
{
    /// <summary>
    /// Controls the HTTP telemetry that MSAL sends to AAD 
    /// via HTTP headers when contacting the /token endpoint.
    /// </summary>
    /// <remarks>
    /// - It is assumed that one manager is created for each application and shared between requests
    /// - Implementer must be thread safe, since one app can fire multiple requests
    /// </remarks>
    internal interface IHttpTelemetryManager
    {
        /// <summary>
        /// CSV string describing the current request. Eligible refresh reasons also consume
        /// the application's pending coalescing counters for a best-effort, first-attempt report.
        /// </summary>
        string GetCurrentRequestHeader(ApiEvent currentApiEvent);

        /// <summary>
        /// Records a post-admission cache result or a suppressed proactive refresh.
        /// </summary>
        void RecordCoalescedRequest(CacheRefreshReason initialReason);

        /// <summary>
        /// Records a successful coordinated acquisition, not individual HTTP attempts.
        /// </summary>
        void RecordSuccessfulAcquisition(CacheRefreshReason initialReason);
    }
}
