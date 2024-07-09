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
        /// Csv string with details about the current header (api used, force refresh flag)
        /// </summary>
        string GetCurrentRequestHeader(ApiEvent currentApiEvent);
    }
}
