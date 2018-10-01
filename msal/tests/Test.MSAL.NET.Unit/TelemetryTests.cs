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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Client;
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Telemetry;
using Microsoft.Identity.Core.Instance;
using Microsoft.Identity.Core.Http;
using Test.Microsoft.Identity.Core.Unit;

namespace Test.MSAL.NET.Unit
{
    public class MyReceiver
    {
        public List<Dictionary<string, string>> EventsReceived { get; set; }

        public MyReceiver()
        {
            EventsReceived = new List<Dictionary<string, string>>();
        }

        public void OnEvents(List<Dictionary<string, string>> events)
        {
            EventsReceived = events;  // Only for testing purpose
            Console.WriteLine("{0} event(s) received", events.Count);
            foreach (var e in events)
            {
                Console.WriteLine("Event: {0}", e[EventBase.EventNameKey]);
                foreach (var entry in e)
                {
                    Console.WriteLine("  {0}: {1}", entry.Key, entry.Value);
                }
            }
        }
    }

    [TestClass]
    public class TelemetryTests
    {
        private readonly MyReceiver _myReceiver = new MyReceiver();

        [TestInitialize]
        public void Initialize()
        {
            Authority.ValidatedAuthorities.Clear();
            HttpClientFactory.ReturnHttpClientForMocks = true;
            HttpMessageHandlerFactory.ClearMockHandlers();
            new TestLogger(Guid.Empty);
        }

        private const string TenantId = "1234";
        private const string UserId = "5678";

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
                var e1 = new ApiEvent(new TestLogger()) { Authority = new Uri("https://login.microsoftonline.com"), AuthorityType = "Aad" };
                telemetry.StartEvent(reqId, e1);
                // do some stuff...
                e1.WasSuccessful = true;
                telemetry.StopEvent(reqId, e1);

                var e2 = new HttpEvent() { HttpPath = new Uri("https://contoso.com"), UserAgent = "SomeUserAgent", QueryParams = "?a=1&b=2" };
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
                var e1 = new ApiEvent(new TestLogger()) { Authority = new Uri("https://login.microsoftonline.com"), AuthorityType = "Aad" };
                telemetry.StartEvent(reqId, e1);
                // do some stuff...
                e1.WasSuccessful = true;
                telemetry.StopEvent(reqId, e1);

                var e2 = new UiEvent() { UserCancelled = false };
                telemetry.StartEvent(reqId, e2);
                telemetry.StopEvent(reqId, e2);

                var e3 = new UiEvent() {AccessDenied = false };
                telemetry.StartEvent(reqId, e3);
                telemetry.StopEvent(reqId, e3);
            }
            finally
            {
                telemetry.Flush(reqId);
            }
            Assert.AreEqual(0, myReceiver.EventsReceived.Count);

            reqId = telemetry.GenerateNewRequestId();
            try
            {
                var e1 = new ApiEvent(new TestLogger()) { Authority = new Uri("https://login.microsoftonline.com"), AuthorityType = "Aad" };
                telemetry.StartEvent(reqId, e1);
                // do some stuff...
                e1.WasSuccessful = false;  // mimic an unsuccessful event, so that this batch should be dispatched
                telemetry.StopEvent(reqId, e1);

                var e2 = new UiEvent() { UserCancelled = true };
                telemetry.StartEvent(reqId, e2);
                telemetry.StopEvent(reqId, e2);

                var e3 = new UiEvent() { AccessDenied = true };
                telemetry.StartEvent(reqId, e3);
                telemetry.StopEvent(reqId, e3);
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

        [TestMethod]
        [TestCategory("TelemetryInternalAPI")]
        public void TelemetryContainsDefaultEventAsFirstEvent()
        {
            Telemetry telemetry = new Telemetry() { ClientId = "a1b2c3d4" };  // To isolate the test environment, we do not use a singleton here
            var myReceiver = new MyReceiver();
            telemetry.RegisterReceiver(myReceiver.OnEvents);
            var reqId = telemetry.GenerateNewRequestId();
            try
            {
                var anEvent = new UiEvent();
                telemetry.StartEvent(reqId, anEvent);
                telemetry.StopEvent(reqId, anEvent);
            }
            finally
            {
                telemetry.Flush(reqId);
            }
            Assert.IsTrue(myReceiver.EventsReceived[0][EventBase.EventNameKey].EndsWith("default_event"));
            Assert.IsTrue(myReceiver.EventsReceived[1][EventBase.EventNameKey].EndsWith("ui_event"));
            Assert.AreNotEqual(myReceiver.EventsReceived[1][EventBase.ElapsedTimeKey], "-1");
        }

        [TestMethod]
        [TestCategory("TelemetryInternalAPI")]
        public void TelemetryStartAnEventWithoutStoppingItLater() // Such event(s) becomes an orphaned event
        {
            Telemetry telemetry = new Telemetry() { ClientId = "a1b2c3d4" };  // To isolate the test environment, we do not use a singleton here
            var myReceiver = new MyReceiver();
            telemetry.RegisterReceiver(myReceiver.OnEvents);

            var reqId = telemetry.GenerateNewRequestId();
            try
            {
                var apiEvent = new ApiEvent(new TestLogger()) { Authority = new Uri("https://login.microsoftonline.com"), AuthorityType = "Aad" };
                telemetry.StartEvent(reqId, apiEvent);
                var uiEvent = new UiEvent();
                telemetry.StartEvent(reqId, uiEvent);
                // Forgot to stop this event. A started event which never got stopped, becomes an orphan.
                //telemetry.StopEvent(reqId, uiEvent);
                telemetry.StopEvent(reqId, apiEvent);
            }
            finally
            {
                Assert.IsFalse(telemetry.CompletedEvents.IsEmpty); // There are completed event(s) inside
                Assert.IsFalse(telemetry.EventsInProgress.IsEmpty); // There is an orphaned event inside
                telemetry.Flush(reqId);
                Assert.IsTrue(telemetry.CompletedEvents.IsEmpty); // Completed event(s) have been dispatched
                Assert.IsTrue(telemetry.EventsInProgress.IsEmpty); // The orphaned event is also dispatched, so there is no memory leak here.
            }
            Assert.IsNotNull(myReceiver.EventsReceived.Find(anEvent =>  // Expect finding such an event
                anEvent[EventBase.EventNameKey].EndsWith("ui_event") && anEvent[EventBase.ElapsedTimeKey] == "-1"));
        }

        [TestMethod]
        [TestCategory("TelemetryInternalAPI")]
        public void TelemetryStopAnEventWithoutStartingItBeforehand()
        {
            Telemetry telemetry = new Telemetry() { ClientId = "a1b2c3d4" };  // To isolate the test environment, we do not use a singleton here
            var myReceiver = new MyReceiver();
            telemetry.RegisterReceiver(myReceiver.OnEvents);

            var reqId = telemetry.GenerateNewRequestId();
            try
            {
                var apiEvent = new ApiEvent(new TestLogger()) { Authority = new Uri("https://login.microsoftonline.com"), AuthorityType = "Aad" };
                telemetry.StartEvent(reqId, apiEvent);
                var uiEvent = new UiEvent();
                // Forgot to start this event
                //telemetry.StartEvent(reqId, uiEvent);
                // Now attempting to stop a never-started event
                telemetry.StopEvent(reqId, uiEvent); // This line will not cause any exception. The implementation simply ignores it.
                telemetry.StopEvent(reqId, apiEvent);
            }
            finally
            {
                telemetry.Flush(reqId);
                Assert.IsTrue(telemetry.CompletedEvents.IsEmpty && telemetry.EventsInProgress.IsEmpty); // No memory leak here
            }
            Assert.IsNull(myReceiver.EventsReceived.Find(anEvent =>  // Expect NOT finding such an event
                anEvent[EventBase.EventNameKey].EndsWith("ui_event")));
        }

        [TestMethod]
        [TestCategory("PiiLoggingEnabled set to true, TenantId & UserId are hashed values")]
        public void PiiLoggingEnabledTrue_TenantAndUserIdHashedTest()
        {
            Telemetry telemetry = new Telemetry();  // To isolate the test environment, we do not use a singleton here
            var myReceiver = new MyReceiver();
            telemetry.RegisterReceiver(myReceiver.OnEvents);
            var logger = new TestLogger();
            logger.SetPiiLoggingEnabled(true);

            telemetry.ClientId = "a1b3c3d4";
            var reqId = telemetry.GenerateNewRequestId();
            try
            {
                var e1 = new ApiEvent(logger) { Authority = new Uri("https://login.microsoftonline.com"), AuthorityType = "Aad", TenantId = TenantId, AccountId = UserId };
                telemetry.StartEvent(reqId, e1);
                // do some stuff...
                e1.WasSuccessful = true;
                // TenantId and UserId are hashed

                if (e1.ContainsKey(ApiEvent.TenantIdKey))
                {
                    Assert.AreNotEqual(null, e1[ApiEvent.TenantIdKey]);
                    Assert.AreNotEqual(TenantId, e1[ApiEvent.TenantIdKey]);
                }

                if (e1.ContainsKey(ApiEvent.UserIdKey))
                {
                    Assert.AreNotEqual(null, e1[ApiEvent.UserIdKey]);
                    Assert.AreNotEqual(UserId, e1[ApiEvent.UserIdKey]);
                }

                telemetry.StopEvent(reqId, e1);
            }
            finally
            {
                telemetry.Flush(reqId);
            }
            Assert.IsTrue(myReceiver.EventsReceived.Count > 0);
        }

        [TestMethod]
        [TestCategory("PiiLoggingEnabled set to false, TenantId & UserId set to null values")]
        public void PiiLoggingEnabledFalse_TenantIdUserIdSetToNullValueTest()
        {
            Telemetry telemetry = new Telemetry();  // To isolate the test environment, we do not use a singleton here
            var myReceiver = new MyReceiver();
            telemetry.RegisterReceiver(myReceiver.OnEvents);
            var logger = new TestLogger();
            logger.SetPiiLoggingEnabled(false);

            telemetry.ClientId = "a1b3c3d4";
            var reqId = telemetry.GenerateNewRequestId();
            try
            {
                var e1 = new ApiEvent(logger) { Authority = new Uri("https://login.microsoftonline.com"), AuthorityType = "Aad", TenantId = TenantId, AccountId = UserId };
                telemetry.StartEvent(reqId, e1);
                // do some stuff...
                e1.WasSuccessful = true;

                // TenantId and UserId are pii and are not sent w/telemetry data
                if (e1.ContainsKey(ApiEvent.TenantIdKey))
                {
                    Assert.AreEqual(null, e1[ApiEvent.TenantIdKey]);
                }

                if (e1.ContainsKey(ApiEvent.UserIdKey))
                {
                    Assert.AreEqual(null, e1[ApiEvent.UserIdKey]);
                }

                telemetry.StopEvent(reqId, e1);
            }
            finally
            {
                telemetry.Flush(reqId);
            }
            Assert.IsTrue(myReceiver.EventsReceived.Count > 0);
        }

        [TestMethod]
        [TestCategory("Check untrusted host Authority is set as null")]
        public void AuthorityNotInTrustedHostList_AuthorityIsSetAsNullValueTest()
        {
            Telemetry telemetry = new Telemetry();  // To isolate the test environment, we do not use a singleton here
            var myReceiver = new MyReceiver();
            telemetry.RegisterReceiver(myReceiver.OnEvents);

            telemetry.ClientId = "a1b3c3d4";
            var reqId = telemetry.GenerateNewRequestId();
            try
            {
                var e1 = new ApiEvent(new TestLogger()) { Authority = new Uri("https://login.microsoftonline.com"), AuthorityType = "Aad" };
                telemetry.StartEvent(reqId, e1);
                // do some stuff...
                e1.WasSuccessful = true;

                // Authority in trusted host list, should return authority with scrubbed tenant
                if (e1.ContainsKey(ApiEvent.AuthorityKey))
                {
                    Assert.AreEqual("https://login.microsoftonline.com/<tenant>", e1[ApiEvent.AuthorityKey]);
                }

                telemetry.StopEvent(reqId, e1);

                // Authority host not in trusted host list, should return null
                var e2 = new ApiEvent(new TestLogger()) { Authority = new Uri("https://login.contoso.com"), AuthorityType = "Aad" };
                telemetry.StartEvent(reqId, e2);
                // do some stuff...
                e2.WasSuccessful = true;

                if (e2.ContainsKey(ApiEvent.AuthorityKey))
                {
                    Assert.AreEqual(null, e2[ApiEvent.AuthorityKey]);
                }

                telemetry.StopEvent(reqId, e2);
            }
            finally
            {
                telemetry.Flush(reqId);
            }
            Assert.IsTrue(myReceiver.EventsReceived.Count > 0);
        }
    }
}