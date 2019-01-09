//----------------------------------------------------------------------
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
//------------------------------------------------------------------------------

#if TELEMETRY
extern alias Server;

using System;
using System.Collections.Generic;
using Server::Microsoft.Applications.Events;

namespace Microsoft.Identity.Client.DevAppsTelemetry
{
    public class ServerTelemetryHandler 
    {
        private ILogger _logger;
        private readonly string _msalEventNameKey;
        private readonly string _ariaTenantId;
        private readonly Guid _sessionId;

        public ServerTelemetryHandler()
        {
            EVTStatus status;
            LogManager.Start(new LogConfiguration());

            _ariaTenantId = TelemetryHandlerConstants.AriaTenantId;
            _logger = LogManager.GetLogger(_ariaTenantId, out status);

            _sessionId = Guid.NewGuid();
            _msalEventNameKey = TelemetryHandlerConstants.MsalEventNameKey;
        }

        public void OnEvents(List<Dictionary<string, string>> events)
        {
            SetEventProperties(events);
            UploadEventsToAria();
        }

        private void SetEventProperties(List<Dictionary<string, string>> events)
        {
            Guid scenarioId = Guid.NewGuid();
            Console.WriteLine("{0} event(s) received for scenarioId {1}",
                events.Count,
                scenarioId);
            foreach (var e in events)
            {
                Console.WriteLine("Event: {0}", e[_msalEventNameKey]);
                EventProperties eventData = new EventProperties
                {
                    Name = e[_msalEventNameKey]
                };

                eventData.SetProperty(TelemetryHandlerConstants.MsalSessionIdKey, _sessionId);
                eventData.SetProperty(TelemetryHandlerConstants.MsalScenarioIdKey, scenarioId);
                foreach (var entry in e)
                {
                    eventData.SetProperty(entry.Key, entry.Value);
                    Console.WriteLine("  {0}: {1}", entry.Key, entry.Value);
                }
                _logger.LogEvent(eventData);
            }
        }

        private void UploadEventsToAria()
        {
            LogManager.UploadNow();
            LogManagerProvider.DestroyLogManager(ariaTenantId);
        }
    }
}
#endif