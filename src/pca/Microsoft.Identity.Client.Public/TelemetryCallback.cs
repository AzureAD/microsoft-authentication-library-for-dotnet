// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// This callback is for the raw telemetry events (app, http, cache) that we want to aggregate using MATS.
    /// </summary>
    /// <param name="events"></param>
    internal delegate void TelemetryCallback(List<Dictionary<string, string>> events);
}
