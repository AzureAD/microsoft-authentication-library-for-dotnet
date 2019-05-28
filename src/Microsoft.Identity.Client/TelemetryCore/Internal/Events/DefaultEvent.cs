// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Globalization;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.TelemetryCore.Internal.Constants;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;

namespace Microsoft.Identity.Client.TelemetryCore.Internal.Events
{
    internal class DefaultEvent : EventBase
    {
        public DefaultEvent(IPlatformProxy platformProxy, string telemetryCorrelationId, string clientId, IDictionary<string, int> eventCount)
            : base(EventNamePrefix + "default_event", telemetryCorrelationId)
        {
            this[EventNamePrefix + "client_id"] = clientId;
            this[EventNamePrefix + "sdk_platform"] = platformProxy.GetProductName()?.ToLowerInvariant();
            this[EventNamePrefix + "sdk_version"] = MsalIdHelper.GetMsalVersion();
            this[EventNamePrefix + "application_name"] = HashPersonalIdentifier(platformProxy.CryptographyManager, platformProxy.GetCallingApplicationName()?.ToLowerInvariant());
            this[EventNamePrefix + "application_version"] = HashPersonalIdentifier(platformProxy.CryptographyManager, platformProxy.GetCallingApplicationVersion()?.ToLowerInvariant());
            this[EventNamePrefix + "device_id"] = HashPersonalIdentifier(platformProxy.CryptographyManager, platformProxy.GetDeviceId()?.ToLowerInvariant());
            this[MsalTelemetryBlobEventNames.UiEventCountTelemetryBatchKey] = GetEventCount(EventNamePrefix + "ui_event", eventCount);
            this[MsalTelemetryBlobEventNames.HttpEventCountTelemetryBatchKey] = GetEventCount(EventNamePrefix + "http_event", eventCount);
            this[MsalTelemetryBlobEventNames.CacheEventCountConstStrKey] = GetEventCount(EventNamePrefix + "cache_event", eventCount);
        }

        private string GetEventCount(string eventName, IDictionary<string, int> eventCount)
        {
            if (!eventCount.ContainsKey(eventName))
            {
                return "0";
            }
            return eventCount[eventName].ToString(CultureInfo.InvariantCulture);
        }
    }
}
