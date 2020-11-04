// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;

namespace Microsoft.Identity.Client.TelemetryCore
{
    internal sealed class TelemetryHelper : IDisposable
    {
        private readonly EventBase _eventToEnd;
        private readonly IMatsTelemetryManager _telemetryManager;
        private readonly IHttpTelemetryManager _httpTelemetryManager;

        public TelemetryHelper(
            IMatsTelemetryManager telemetryManager,
            IHttpTelemetryManager httpTelemetryManager,
            EventBase eventBase)
        {
            _telemetryManager = telemetryManager;
            _httpTelemetryManager = httpTelemetryManager;
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
                    if (_eventToEnd is ApiEvent apiEvent)
                        _httpTelemetryManager?.RecordStoppedEvent(apiEvent);
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
