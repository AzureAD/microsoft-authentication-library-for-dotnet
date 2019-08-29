// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;

namespace Microsoft.Identity.Client.TelemetryCore
{
    internal sealed class TelemetryHelper : IDisposable
    {
        private readonly EventBase _eventToEnd;
        private readonly ITelemetryManager _telemetryManager;

        public TelemetryHelper(
            ITelemetryManager telemetryManager,
            EventBase eventBase)
        {
            _telemetryManager = telemetryManager;
            _eventToEnd = eventBase;
            _telemetryManager?.StartEvent(eventBase);
        }

        #region IDisposable Support

        private bool _disposedValue = false;

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _telemetryManager?.StopEvent(_eventToEnd);
                }

                _disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
