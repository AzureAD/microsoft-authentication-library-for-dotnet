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
//------------

#if TELEMETRY
// Referencing alias set in project file since Aria server and
// client assemblies have the same fully-qualified type names. 
extern alias Client;

using AriaTelemetryProvider;
using System;
using System.Collections.Generic;
using Client::Microsoft.Applications.Events;
using System.Globalization;

namespace Microsoft.Identity.Client.AriaTelemetryProvider
{
    public class ClientTelemetryHandler
    {
        private readonly ILogger _ariaEventLogger;
        private readonly string _msalEventNameKey;
        private readonly string _ariaTenantId;
        private readonly Guid _sessionId;
        private readonly TransmitPolicy _ariaTransmitPolicy;
        private Logger _logger;

        public ClientTelemetryHandler()
        {
            // Aria configuration
            EVTStatus status;
            LogManager.Start(new LogConfiguration());

            _ariaTransmitPolicy = new TransmitPolicy
            {
                ProfileName = "RealTimeForALL",
                Rules = new List<Rules>
                {
                    new Rules
                    {
                        NetCost = NetCost.Low, PowerState = PowerState.Charging,
                        Timers = new Timers { Normal = 10, RealTime = 1 }
                    }
                }
            };

            LogManager.SetNetCost(_ariaTransmitPolicy.Rules[0].NetCost);
            LogManager.LoadTransmitProfiles(new List <TransmitPolicy> {_ariaTransmitPolicy});
            LogManager.SetTransmitProfile(_ariaTransmitPolicy.ProfileName);
            LogManager.SetPowerState(PowerState.Charging);

            _ariaTenantId = TelemetryHandlerConstants.AriaTenantId;
            _ariaEventLogger = LogManager.GetLogger(_ariaTenantId, out status);

            _sessionId = Guid.NewGuid();
            _msalEventNameKey = TelemetryHandlerConstants.MsalEventNameKey;

            // Set '_logger.WriteToConsole = true' to write out telemetry data to console
            _logger = new Logger();
        }

        public void OnEvents(List<Dictionary<string, string>> events)
        {
            SetEventProperties(events);
            UploadEventsToAria();
        }

        private void SetEventProperties(List<Dictionary<string, string>> events)
        {
            Guid scenarioId = Guid.NewGuid();
            _logger.Log(string.Format(CultureInfo.InvariantCulture,
                "{0} event(s) received for scenarioId {1}",
                events.Count,
                scenarioId));

            foreach (var msalEvent in events)
            {
                _logger.Log(string.Format(CultureInfo.InvariantCulture,
                    "Event: {0}",
                    msalEvent[_msalEventNameKey]));

                EventProperties eventData = new EventProperties
                {
                    Name = msalEvent[_msalEventNameKey]
                };

                eventData.SetProperty(TelemetryHandlerConstants.MsalSessionIdKey, _sessionId);
                eventData.SetProperty(TelemetryHandlerConstants.MsalScenarioIdKey, scenarioId);
                foreach (var entry in msalEvent)
                {
                    eventData.SetProperty(entry.Key, entry.Value);
                    _logger.Log(string.Format(CultureInfo.InvariantCulture,
                            "  {0}: {1}",
                              entry.Key,
                              entry.Value));
                }
                _ariaEventLogger.LogEvent(eventData);
            }
        }

        private void UploadEventsToAria()
        {
            LogManager.UploadNow();
            LogManagerProvider.DestroyLogManager(_ariaTenantId);
        }
    }
}
#endif