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
using System.Collections.Generic;
using Microsoft.Identity.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.MSAL.NET.Unit
{
    class MyReceiver
    {
        public void OnEvents(List<Dictionary<string, string>> events)
        {
            foreach(var e in events)
            {
                foreach(var entry in e)
                {
                    Console.WriteLine("{0}: {1}", entry.Key, entry.Value);
                }
            }
        }
    }

    [TestClass]
    public class TelemetryTests
    {
        [TestInitialize]
        public void Initialize()
        {
        }

        [TestMethod]
        [TestCategory("TelemetryTests")]
        public void TelemetryPublicApiSample()
        {
            var telemetry = Telemetry.GetInstance();
            var receiver = new MyReceiver();
            telemetry.RegisterReceiver(receiver.OnEvents);

            // Or you can use a one-liner:
            Telemetry.GetInstance().RegisterReceiver(new MyReceiver().OnEvents);
        }

        [TestMethod]
        [TestCategory("TelemetryTests")]
        public void TelemetryIsSingleton()
        {
            var t1 = Telemetry.GetInstance();
            Assert.IsNotNull(t1);
            var t2 = Telemetry.GetInstance();
            Assert.AreEqual(t1, t2);
        }

        [TestMethod]
        [TestCategory("TelemetryInternalAPI")]
        public void TelemetryInternalApiSample()
        {
            // This section demonstrates the public API
            Telemetry telemetry = Telemetry.GetInstance();
            telemetry.RegisterReceiver(new MyReceiver().OnEvents);

            // The following section is quoted from previous ADAL Telemetry implementation,
            // in order to see the internal API usage are largely comparable.
            /*
            Telemetry telemetry = Telemetry.GetInstance();
            TestDispatcher dispatcher = new TestDispatcher();
            telemetry.RegisterDispatcher(dispatcher, false);
            dispatcher.clear();  // <-- This one was presumably the previous implementation detail, it is not part of the official internal API

            string requestIDThree = telemetry.CreateRequestId();
            telemetry.StartEvent(requestIDThree, "event_3");
            // do some stuff...
            DefaultEvent testDefaultEvent3 = new DefaultEvent();
            telemetry.StopEvent(requestIDThree, testDefaultEvent3, "event_3");

            telemetry.Flush(requestIDThree);
            */

            // This section demonstrates the MSAL internal API, with proper usage of try ... finally ... pattern
            var collection = telemetry.CreateEventCollection();  // Equivalent to: string ReqId = telemetry.CreateRequestId()
            try
            {
                var e1 = new Event("event foo");
                collection.Add(e1); // Roughly equivalent to: telemetry.StartEvent(ReqId, "event name");
                // do some stuff...
                e1.Stop(); // Equivalent to: Event e = new ApiEvent(...); telemetry.StopEvent(ReqId, "event name", e);

                var e2 = new Event("event bar");
                collection.Add(e2); // Roughly equivalent to: telemetry.StartEvent(ReqId, "event name");
                // do some stuff...
                e2.Stop(); // Equivalent to: Event e = new ApiEvent(...); telemetry.StopEvent(ReqId, "event name", e);
            }
            finally
            {
                telemetry.Flush(collection);
            }
        }
    }
}
