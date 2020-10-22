// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Test.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Test.Unit.CoreTests.Telemetry
{
    [TestClass]
    public class TelemetryHelperTests
    {
        private const string CorrelationId = "thetelemetrycorrelationid";
        private const string ClientId = "theclientid";
        internal _TestEvent _trackingEvent;
        private RequestContext _requestContext;
        private _TestReceiver _testReceiver;

        [TestInitialize]
        public void Setup()
        {
            TestCommon.ResetInternalStaticCaches();
            _testReceiver = new _TestReceiver();
            var serviceBundle = TestCommon.CreateServiceBundleWithCustomHttpManager(
                null, 
                clientId: ClientId, telemetryCallback: _testReceiver.HandleTelemetryEvents);
            _requestContext = new RequestContext(serviceBundle, Guid.NewGuid());
            _trackingEvent = new _TestEvent("tracking event", CorrelationId);
        }

        internal class _TestReceiver : ITelemetryReceiver
        {
            public readonly List<Dictionary<string, string>> ReceivedEvents = new List<Dictionary<string, string>>();

            /// <inheritdoc />
            public void HandleTelemetryEvents(List<Dictionary<string, string>> events)
            {
                ReceivedEvents.AddRange(events);
            }
        }

        internal class _TestEvent : EventBase
        {
            public _TestEvent(string eventName, string correlationId) : base(eventName, correlationId)
            {
            }
        }

        [TestMethod]
        public void TestTelemetryHelper()
        {
            using (_requestContext.CreateTelemetryHelper(_trackingEvent))
            {
            }

            ValidateResults(ClientId, false);
        }

        [TestMethod]
        public void TestTelemetryHelperWithFlush()
        {
            using (_requestContext.CreateTelemetryHelper(_trackingEvent))
            {
            }

            _requestContext.ServiceBundle.MatsTelemetryManager.Flush(CorrelationId);

            ValidateResults(ClientId, true);
        }

        private void ValidateResults(
            string expectedClientId,
            bool shouldFlush)
        {
            if (shouldFlush)
            {
                Assert.AreEqual(2, _testReceiver.ReceivedEvents.Count);

                var first = _testReceiver.ReceivedEvents[0];
                Assert.AreEqual(13, first.Count);
                Assert.IsTrue(first.ContainsKey(EventBase.EventNameKey));
                Assert.AreEqual("msal.default_event", first[EventBase.EventNameKey]);
                Assert.AreEqual(expectedClientId, first["msal.client_id"]);

                var second = _testReceiver.ReceivedEvents[1];
                Assert.AreEqual(4, second.Count);
                Assert.IsTrue(second.ContainsKey(EventBase.EventNameKey));
                Assert.AreEqual("tracking event", second[EventBase.EventNameKey]);
            }
            else
            {
                Assert.AreEqual(0, _testReceiver.ReceivedEvents.Count);
            }
        }
    }
}
