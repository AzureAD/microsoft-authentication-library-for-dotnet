// ------------------------------------------------------------------------------
// 
// Copyright (c) Microsoft Corporation.
// All rights reserved.
// 
// This code is licensed under the MIT License.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// ------------------------------------------------------------------------------

using System;
using Microsoft.Identity.Client.Mats.Internal.Events;

namespace Microsoft.Identity.Client.TelemetryCore
{
    internal sealed class TelemetryHelper : IDisposable
    {
        private readonly EventBase _eventToEnd;
        private readonly string _telemetryCorrelationId;
        private readonly ITelemetryManager _telemetryManager;

        public TelemetryHelper(
            ITelemetryManager telemetryManager,
            string telemetryCorrelationId,
            EventBase eventBase)
        {
            _telemetryManager = telemetryManager;
            _telemetryCorrelationId = telemetryCorrelationId;
            _eventToEnd = eventBase;
            _telemetryManager?.StartEvent(_telemetryCorrelationId, eventBase);
        }

        #region IDisposable Support

        private bool _disposedValue = false;

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _telemetryManager?.StopEvent(_telemetryCorrelationId, _eventToEnd);
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
