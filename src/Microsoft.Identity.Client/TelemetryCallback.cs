// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.Identity.Client
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="events"></param>
    public delegate void TelemetryCallback(List<Dictionary<string, string>> events);
}
