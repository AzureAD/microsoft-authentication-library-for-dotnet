// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
//------------

#if ARIA_TELEMETRY_ENABLED
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
