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
        /// Record a stopped event
        /// </summary>
        void RecordStoppedEvent(ApiEvent apiEvent);

        /// <summary>
        /// Csv string with details about the current header (api used, force refresh flag)
        /// </summary>
        string GetCurrentRequestHeader(ApiEvent currentApiEvent);

        /// <summary>
        /// Csv string with details about the previous failed requests made: api, correlation id, error
        /// </summary>
        /// <remarks>
        /// If AAD returns OK or a normal error (e.g. interaction required), then telemetry is recorded.
        /// If AAD returns a 5xx or 429 HTTP error (i.e. AAD is down), then telemetry has not been recorded and MSAL 
        /// will continue to hold on to this data until a successful request is made
        /// </remarks>
        string GetLastRequestHeader();

        /// <summary>
        /// Resets the state of failed requests. See <see cref="GetLastRequestHeader"/> for more details
        /// </summary>
        void ResetPreviousUnsentData();
    }
}
