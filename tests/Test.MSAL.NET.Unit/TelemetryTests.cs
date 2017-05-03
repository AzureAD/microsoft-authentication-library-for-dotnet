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
using Microsoft.Identity.Client.Internal.Telemetry;

namespace Test.MSAL.NET.Unit
{
    class MyReceiver
    {
        internal List<Dictionary<string, string>> EventsReceived { get; set; }

        public MyReceiver()
        {
            EventsReceived = new List<Dictionary<string, string>>();
        }

        public void OnEvents(List<Dictionary<string, string>> events)
        {
            EventsReceived = events;  // Only for testing purpose
            Console.WriteLine("{0} event(s) received", events.Count);
            foreach(var e in events)
            {
                Console.WriteLine("Event: {0}", e[EventBase.ConstEventName]);
                foreach(var entry in e)
                {
                    Console.WriteLine("  {0}: {1}", entry.Key, entry.Value);
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
            Telemetry telemetry = new Telemetry();  // To isolate the test environment, we do not use a singleton here
            var myReceiver = new MyReceiver();
            telemetry.RegisterReceiver(myReceiver.OnEvents);

            telemetry.ClientId = "a1b3c3d4";
            var reqId = telemetry.GenerateNewRequestId();
            try
            {
                var e1 = new ApiEvent() {Authority = new Uri("https://login.microsoftonline.com"), AuthorityType = "Aad"};
                telemetry.StartEvent(reqId, e1);
                // do some stuff...
                e1.WasSuccessful = true;
                telemetry.StopEvent(reqId, e1);

                var e2 = new HttpEvent() {HttpPath = new Uri("https://contoso.com"), UserAgent = "SomeUserAgent", QueryParams = "?a=1&b=2"};
                telemetry.StartEvent(reqId, e2);
                // do some stuff...
                e2.HttpResponseStatus = 200;
                telemetry.StopEvent(reqId, e2);
            }
            finally
            {
                telemetry.Flush(reqId);
            }
            Assert.IsTrue(myReceiver.EventsReceived.Count > 0);
        }

        [TestMethod]
        [TestCategory("TelemetryInternalAPI")]
        public void TelemetrySkipEventsIfApiEventWasSuccessful()
        {
            Telemetry telemetry = new Telemetry();  // To isolate the test environment, we do not use a singleton here
            telemetry.TelemetryOnFailureOnly = true;
            var myReceiver = new MyReceiver();
            telemetry.RegisterReceiver(myReceiver.OnEvents);

            var reqId = telemetry.GenerateNewRequestId();
            try
            {
                var e1 = new ApiEvent() { Authority = new Uri("https://login.microsoftonline.com"), AuthorityType = "Aad" };
                telemetry.StartEvent(reqId, e1);
                // do some stuff...
                e1.WasSuccessful = true;
                telemetry.StopEvent(reqId, e1);

                var e2 = new UiEvent() { UserCancelled = false };
                telemetry.StartEvent(reqId, e2);
                telemetry.StopEvent(reqId, e2);
            }
            finally
            {
                telemetry.Flush(reqId);
            }
            Assert.AreEqual(0, myReceiver.EventsReceived.Count);

            reqId = telemetry.GenerateNewRequestId();
            try
            {
                var e1 = new ApiEvent() { Authority = new Uri("https://login.microsoftonline.com"), AuthorityType = "Aad" };
                telemetry.StartEvent(reqId, e1);
                // do some stuff...
                e1.WasSuccessful = false;  // mimic an unsuccessful event, so that this batch should be dispatched
                telemetry.StopEvent(reqId, e1);

                var e2 = new UiEvent() { UserCancelled = true };
                telemetry.StartEvent(reqId, e2);
                telemetry.StopEvent(reqId, e2);
            }
            finally
            {
                telemetry.Flush(reqId);
            }
            Assert.IsTrue(myReceiver.EventsReceived.Count > 0);
        }

        [TestMethod]
        [TestCategory("TelemetryInternalAPI")]
        public void TelemetryScrubTenantFromUri()
        {
            Assert.AreEqual("https://login.microsoftonline.com/<tenant>/oauth2/v2.0/token",
                EventBase.ScrubTenant(new Uri("https://login.microsoftonline.com/common/oauth2/v2.0/token")));

            Assert.AreEqual("https://login.microsoftonline.com/tfp/<tenant>/oauth2/v2.0/token",
                EventBase.ScrubTenant(new Uri("https://login.microsoftonline.com/tfp/contoso/oauth2/v2.0/token")));

            Assert.AreEqual("https://login.microsoftonline.com/<tenant>",
                EventBase.ScrubTenant(new Uri("https://login.microsoftonline.com/common")));

            Assert.AreEqual("https://login.microsoftonline.com/tfp/<tenant>",
                EventBase.ScrubTenant(new Uri("https://login.microsoftonline.com/tfp/contoso")));

            Assert.AreEqual(null, EventBase.ScrubTenant(new Uri("https://login.contoso.com/adfs")));
        }
    }
}