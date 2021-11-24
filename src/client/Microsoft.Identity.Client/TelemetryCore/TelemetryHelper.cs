// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;

namespace Microsoft.Identity.Client.TelemetryCore
{
    internal sealed class TelemetryHelper : IDisposable
    {
        private readonly EventBase _eventToEnd;
        private readonly IHttpTelemetryManager _httpTelemetryManager;

        public TelemetryHelper(
            IHttpTelemetryManager httpTelemetryManager,
            EventBase eventBase)
        {
            _httpTelemetryManager = httpTelemetryManager;
            _eventToEnd = eventBase;
        }

        #region IDisposable Support

        private bool _disposedValue = false;

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
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
