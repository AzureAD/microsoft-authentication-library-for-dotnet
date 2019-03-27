// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.Mats.Internal
{
    internal class TelemetryDispatcher : ITelemetryDispatcher
    {
        private readonly Action<IMatsTelemetryBatch> _dispatchAction;

        public TelemetryDispatcher(Action<IMatsTelemetryBatch> dispatchAction)
        {
            _dispatchAction = dispatchAction;
        }

        public void DispatchEvent(IMatsTelemetryBatch batch)
        {
            _dispatchAction(batch);
        }
    }
}
