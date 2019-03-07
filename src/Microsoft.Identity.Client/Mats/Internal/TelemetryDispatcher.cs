// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Identity.Client.Mats.Internal
{
    internal class TelemetryDispatcher : ITelemetryDispatcher
    {
        private readonly Action<IMatsTelemetryBatch> _dispatchAction;
        public TelemetryDispatcher(Action<IMatsTelemetryBatch> dispatchAction)
        {
            _dispatchAction = dispatchAction;
        }

        public void DispatchEvent(IMatsTelemetryData data)
        {
            var batch = MatsTelemetryBatch.Create(data);
            _dispatchAction(batch);
        }
    }
}
