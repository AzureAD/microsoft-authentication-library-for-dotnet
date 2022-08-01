// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.IdentityModel.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class TelemetryClientTests : TestBase
    {
        [TestMethod]
        public void TelemetryClientExperimental()
        {
            ITelemetryClient telemetryClient = new TelemetryClient(TestConstants.ClientId);

            var e = AssertException.Throws<MsalClientException>(() => ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithClientSecret("secret")
                .WithTelemetryClient(telemetryClient)
                .Build());

            Assert.AreEqual(MsalError.ExperimentalFeature, e.ErrorCode);
        }
    }

    internal class TelemetryClient : ITelemetryClient
    {
        public TelemetryClient(string clientId)
        {
            ClientId = clientId;
        }

        public string ClientId { get; set; }

        public void Initialize()
        {
            throw new NotImplementedException();
        }

        public bool IsEnabled()
        {
            throw new NotImplementedException();
        }

        public bool IsEnabled(string eventName)
        {
            throw new NotImplementedException();
        }

        public void TrackEvent(TelemetryEventDetails eventDetails)
        {
            throw new NotImplementedException();
        }

        public void TrackEvent(string eventName, IDictionary<string, string> stringProperties = null, IDictionary<string, long> longProperties = null, IDictionary<string, bool> boolProperties = null, IDictionary<string, DateTime> dateTimeProperties = null, IDictionary<string, double> doubleProperties = null, IDictionary<string, Guid> guidProperties = null)
        {
            throw new NotImplementedException();
        }
    }
}
