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

using System;
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Telemetry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.Microsoft.Identity.Core.Unit.Telemetry
{
    [TestClass]
    public class TelemetryHelperTests
    {
        private const string requestId = "therequestid";
        private _TestTelem _telem;
        private _TestEvent _startEvent;
        private _TestEvent _stopEvent;

        [TestInitialize]
        public void Setup()
        {
            _telem = new _TestTelem();
            CoreTelemetryService.InitializeCoreTelemetryService(_telem);

            _startEvent = new _TestEvent("start event");
            _stopEvent = new _TestEvent("stop event");
        }

        [TestCleanup]
        public void Cleanup()
        {
            _telem = null;
            CoreTelemetryService.InitializeCoreTelemetryService(null);
        }

        private class _TestEvent : EventBase
        {
            public _TestEvent(string eventName) : base(eventName)
            {
            }
        }

        private class _TestTelem : ITelemetry
        {
            public int NumFlushCalls { get; private set; } = 0;
            public int NumStartEventCalls { get; private set; } = 0;
            public int NumStopEventCalls { get; private set; } = 0;

            public string LastStartEventRequestId { get; private set; } = string.Empty;
            public string LastStopEventRequestId { get; private set; } = string.Empty;
            public string LastFlushEventRequestId { get; private set; } = string.Empty;

            public EventBase LastEventToStart { get; private set; }
            public EventBase LastEventToStop { get; private set; }

            public void Flush(string requestId)
            {
                NumFlushCalls++;
                LastFlushEventRequestId = requestId;
            }

            public void StartEvent(string requestId, EventBase eventToStart)
            {
                NumStartEventCalls++;
                LastStartEventRequestId = requestId;
                LastEventToStart = eventToStart;
            }

            public void StopEvent(string requestId, EventBase eventToStop)
            {
                NumStopEventCalls++;
                LastStopEventRequestId = requestId;
                LastEventToStop = eventToStop;
            }
        }

        [TestMethod]
        [TestCategory("TelemetryHelperTests")]
        public void TestTelemetryHelper()
        {
            using (CoreTelemetryService.CreateTelemetryHelper(requestId, _startEvent))
            {
            }

            ValidateResults(_telem, requestId, _startEvent, _startEvent, false);
        }

        [TestMethod]
        [TestCategory("TelemetryHelperTests")]
        public void TestTelemetryHelperWithFlush()
        {
            using (CoreTelemetryService.CreateTelemetryHelper(requestId, _startEvent, shouldFlush: true))
            {
            }

            ValidateResults(_telem, requestId, _startEvent, _startEvent, true);
        }

        [TestMethod]
        [TestCategory("TelemetryHelperTests")]
        public void TestTelemetryHelperWithDifferentStopStartEvents()
        {
            using (CoreTelemetryService.CreateTelemetryHelper(requestId, _startEvent, eventToEnd: _stopEvent))
            {
            }

            ValidateResults(_telem, requestId, _startEvent, _stopEvent, false);
        }

        [TestMethod]
        [TestCategory("TelemetryHelperTests")]
        public void TestTelemetryHelperWithDifferentStopStartEventsWithFlush()
        {
            using (CoreTelemetryService.CreateTelemetryHelper(requestId, _startEvent, eventToEnd: _stopEvent, shouldFlush: true))
            {
            }

            ValidateResults(_telem, requestId, _startEvent, _stopEvent, true);
        }

        private void ValidateResults(
            _TestTelem telem,
            string expectedRequestId,
            _TestEvent expectedStartEvent,
            _TestEvent expectedStopEvent,
            bool shouldFlush)
        {
            if (shouldFlush)
            {
                Assert.AreEqual(1, telem.NumFlushCalls);
                Assert.AreEqual(expectedRequestId, telem.LastFlushEventRequestId);
            }
            else
            {
                Assert.AreEqual(0, telem.NumFlushCalls);
                Assert.AreEqual(string.Empty, telem.LastFlushEventRequestId);
            }

            Assert.AreEqual(1, telem.NumStartEventCalls);
            Assert.AreEqual(1, telem.NumStopEventCalls);

            Assert.AreEqual(expectedRequestId, telem.LastStartEventRequestId);
            Assert.AreEqual(expectedRequestId, telem.LastStopEventRequestId);

            Assert.AreEqual(expectedStartEvent, telem.LastEventToStart);
            Assert.AreEqual(expectedStopEvent, telem.LastEventToStop);
        }
    }
}
