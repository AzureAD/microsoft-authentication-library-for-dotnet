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

using System.Collections.Generic;
using Microsoft.Identity.Client.Mats.Internal.Events;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Test.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CoreTests.Telemetry
{
    [TestClass]
    public class TelemetryHelperTests
    {
        private const string RequestId = "therequestid";
        private const string ClientId = "theclientid";
        private _TestEvent _trackingEvent;
        private TelemetryManager _telemetryManager;
        private _TestReceiver _testReceiver;

        [TestInitialize]
        public void Setup()
        {
            _testReceiver = new _TestReceiver();
            _telemetryManager = new TelemetryManager(TestCommon.CreateDefaultServiceBundle().PlatformProxy, _testReceiver.HandleTelemetryEvents);
            _trackingEvent = new _TestEvent("tracking event");
        }

        private class _TestReceiver : ITelemetryReceiver
        {
            public readonly List<Dictionary<string, string>> ReceivedEvents = new List<Dictionary<string, string>>();

            /// <inheritdoc />
            public void HandleTelemetryEvents(List<Dictionary<string, string>> events)
            {
                ReceivedEvents.AddRange(events);
            }
        }

        private class _TestEvent : EventBase
        {
            public _TestEvent(string eventName) : base(eventName)
            {
            }
        }

        [TestMethod]
        [TestCategory("TelemetryHelperTests")]
        public void TestTelemetryHelper()
        {
            using (_telemetryManager.CreateTelemetryHelper(RequestId, ClientId, _trackingEvent))
            {
            }

            ValidateResults(ClientId, false);
        }

        [TestMethod]
        [TestCategory("TelemetryHelperTests")]
        public void TestTelemetryHelperWithFlush()
        {
            using (_telemetryManager.CreateTelemetryHelper(RequestId, ClientId, _trackingEvent, shouldFlush: true))
            {
            }

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
                Assert.AreEqual(12, first.Count);
                Assert.IsTrue(first.ContainsKey(EventBase.EventNameKey));
                Assert.AreEqual("msal.default_event", first[EventBase.EventNameKey]);
                Assert.AreEqual(expectedClientId, first["msal.client_id"]);

                var second = _testReceiver.ReceivedEvents[1];
                Assert.AreEqual(3, second.Count);
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
