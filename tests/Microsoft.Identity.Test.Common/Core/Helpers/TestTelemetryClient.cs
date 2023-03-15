// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.TelemetryCore.TelemetryClient;
using Microsoft.IdentityModel.Abstractions;

namespace Microsoft.Identity.Test.Common.Core.Helpers
{
    internal class TestTelemetryClient : ITelemetryClient
    {
        public MsalTelemetryEventDetails TestTelemetryEventDetails { get; set; }

        public TestTelemetryClient(string clientId)
        {
            ClientId = clientId;
        }

        public string ClientId { get; set; }

        public void Initialize()
        {

        }

        public bool IsEnabled()
        {
            return true;
        }

        public bool IsEnabled(string eventName)
        {
            return TelemetryConstants.AcquireTokenEventName.Equals(eventName);
        }

        public void TrackEvent(TelemetryEventDetails eventDetails)
        {
            TestTelemetryEventDetails = (MsalTelemetryEventDetails)eventDetails;
        }

        public void TrackEvent(string eventName, IDictionary<string, string> stringProperties = null, IDictionary<string, long> longProperties = null, IDictionary<string, bool> boolProperties = null, IDictionary<string, DateTime> dateTimeProperties = null, IDictionary<string, double> doubleProperties = null, IDictionary<string, Guid> guidProperties = null)
        {
            throw new NotImplementedException();
        }
    }
}
