// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.Identity.Client.TelemetryCore
{
    internal interface ITelemetryReceiver
    {
        void HandleTelemetryEvents(List<Dictionary<string, string>> events);
    }
}
