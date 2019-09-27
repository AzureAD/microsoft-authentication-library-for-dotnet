// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.TelemetryCore.Internal.Events;

namespace Microsoft.Identity.Client.TelemetryCore
{
    internal interface ITelemetryManager
    {
        TelemetryCallback Callback { get; }

        TelemetryHelper CreateTelemetryHelper(EventBase eventBase);

        void StartEvent(EventBase eventToStart);
        void StopEvent(EventBase eventToStop);
        void Flush(string correlationId);
        string FetchAndResetPreviousHttpTelemetryContent();
        string FetchCurrentHttpTelemetryContent(string currentRequestCorrelationId);
    }
}
