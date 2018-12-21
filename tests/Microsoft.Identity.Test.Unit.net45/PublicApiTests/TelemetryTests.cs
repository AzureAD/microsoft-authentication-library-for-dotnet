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
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    public class MyReceiver : ITelemetryReceiver
    {
        public List<Dictionary<string, string>> EventsReceived { get; private set; }

        public MyReceiver()
        {
            EventsReceived = new List<Dictionary<string, string>>();
        }

        public void HandleTelemetryEvents(List<Dictionary<string, string>> events)
        {
            EventsReceived.AddRange(events);  // Only for testing purpose
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

        /// <inheritdoc />
        public bool OnlySendFailureTelemetry { get; set; }
    }

    [TestClass]
    public class TelemetryTests
    {
        private const string ClientId = "a1b3c3d4";

        private MyReceiver _myReceiver;
        private TelemetryManager _telemetryManager;

        [TestInitialize]
        public void Initialize()
        {
            TestCommon.ResetStateAndInitMsal();
            _myReceiver = new MyReceiver();
            _telemetryManager = new TelemetryManager(_myReceiver);
            Logger.PiiLoggingEnabled = false;
        }

        [TestCleanup]
        public void Cleanup()
        {
            Logger.PiiLoggingEnabled = false;
        }

        private const string TenantId = "1234";
        private const string UserId = "5678";

        [TestMethod]
        [TestCategory("TelemetryTests")]
        public void TelemetryPublicApiSample()
        {
            var telemetry = Telemetry.GetInstance();
            var receiver = new MyReceiver();
            telemetry.RegisterReceiver(receiver.HandleTelemetryEvents);

            // Or you can use a one-liner:
            Telemetry.GetInstance().RegisterReceiver(new MyReceiver().HandleTelemetryEvents);
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
            var reqId = _telemetryManager.GenerateNewRequestId();
            try
            {
                var e1 = new ApiEvent(new MsalLogger(Guid.NewGuid(), null)) { Authority = new Uri("https://login.microsoftonline.com"), AuthorityType = "Aad" };
                _telemetryManager.StartEvent(reqId, e1);
                // do some stuff...
                e1.WasSuccessful = true;
                _telemetryManager.StopEvent(reqId, e1);

                var e2 = new HttpEvent() { HttpPath = new Uri("https://contoso.com"), UserAgent = "SomeUserAgent", QueryParams = "?a=1&b=2" };
                _telemetryManager.StartEvent(reqId, e2);
                // do some stuff...
                e2.HttpResponseStatus = 200;
                _telemetryManager.StopEvent(reqId, e2);
            }
            finally
            {
                _telemetryManager.Flush(reqId, ClientId);
            }
            Assert.IsTrue(_myReceiver.EventsReceived.Count > 0);
        }

        [TestMethod]
        [TestCategory("TelemetryInternalAPI")]
        public void TelemetrySkipEventsIfApiEventWasSuccessful()
        {
            var myReceiver = new MyReceiver
            {
                OnlySendFailureTelemetry = true
            };

            var telemetryManager = new TelemetryManager(myReceiver);
            var telemetry = (ITelemetry)telemetryManager;

            var reqId = telemetryManager.GenerateNewRequestId();
            try
            {
                var e1 = new ApiEvent(new MsalLogger(Guid.NewGuid(), null)) { Authority = new Uri("https://login.microsoftonline.com"), AuthorityType = "Aad" };
                telemetry.StartEvent(reqId, e1);
                // do some stuff...
                e1.WasSuccessful = true;
                telemetry.StopEvent(reqId, e1);

                var e2 = new UiEvent() { UserCancelled = false };
                telemetry.StartEvent(reqId, e2);
                telemetry.StopEvent(reqId, e2);

                var e3 = new UiEvent() { AccessDenied = false };
                telemetry.StartEvent(reqId, e3);
                telemetry.StopEvent(reqId, e3);
            }
            finally
            {
                telemetry.Flush(reqId, ClientId);
            }
            Assert.AreEqual(0, myReceiver.EventsReceived.Count);

            reqId = telemetryManager.GenerateNewRequestId();
            try
            {
                var e1 = new ApiEvent(new MsalLogger(Guid.NewGuid(), null)) { Authority = new Uri("https://login.microsoftonline.com"), AuthorityType = "Aad" };
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
                telemetry.Flush(reqId, ClientId);
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
            var reqId = _telemetryManager.GenerateNewRequestId();
            try
            {
                var anEvent = new UiEvent();
                _telemetryManager.StartEvent(reqId, anEvent);
                _telemetryManager.StopEvent(reqId, anEvent);
            }
            finally
            {
                _telemetryManager.Flush(reqId, ClientId);
            }
            Assert.IsTrue(_myReceiver.EventsReceived[0][EventBase.EventNameKey].EndsWith("default_event"));
            Assert.IsTrue(_myReceiver.EventsReceived[1][EventBase.EventNameKey].EndsWith("ui_event"));
            Assert.AreNotEqual(_myReceiver.EventsReceived[1][EventBase.ElapsedTimeKey], "-1");
        }

        [TestMethod]
        [TestCategory("TelemetryInternalAPI")]
        public void TelemetryStartAnEventWithoutStoppingItLater() // Such event(s) becomes an orphaned event
        {
            var reqId = _telemetryManager.GenerateNewRequestId();
            try
            {
                var apiEvent = new ApiEvent(new MsalLogger(Guid.NewGuid(), null)) { Authority = new Uri("https://login.microsoftonline.com"), AuthorityType = "Aad" };
                _telemetryManager.StartEvent(reqId, apiEvent);
                var uiEvent = new UiEvent();
                _telemetryManager.StartEvent(reqId, uiEvent);
                // Forgot to stop this event. A started event which never got stopped, becomes an orphan.
                // telemetry.StopEvent(reqId, uiEvent);
                _telemetryManager.StopEvent(reqId, apiEvent);
            }
            finally
            {
                Assert.IsFalse(_telemetryManager.CompletedEvents.IsEmpty); // There are completed event(s) inside
                Assert.IsFalse(_telemetryManager.EventsInProgress.IsEmpty); // There is an orphaned event inside
                _telemetryManager.Flush(reqId, ClientId);
                Assert.IsTrue(_telemetryManager.CompletedEvents.IsEmpty); // Completed event(s) have been dispatched
                Assert.IsTrue(_telemetryManager.EventsInProgress.IsEmpty); // The orphaned event is also dispatched, so there is no memory leak here.
            }
            Assert.IsNotNull(_myReceiver.EventsReceived.Find(anEvent =>  // Expect finding such an event
                anEvent[EventBase.EventNameKey].EndsWith("ui_event") && anEvent[EventBase.ElapsedTimeKey] == "-1"));
        }

        [TestMethod]
        [TestCategory("TelemetryInternalAPI")]
        public void TelemetryStopAnEventWithoutStartingItBeforehand()
        {
            var reqId = _telemetryManager.GenerateNewRequestId();
            try
            {
                var apiEvent = new ApiEvent(new MsalLogger(Guid.NewGuid(), null)) { Authority = new Uri("https://login.microsoftonline.com"), AuthorityType = "Aad" };
                _telemetryManager.StartEvent(reqId, apiEvent);
                var uiEvent = new UiEvent();
                // Forgot to start this event
                // telemetry.StartEvent(reqId, uiEvent);
                // Now attempting to stop a never-started event
                _telemetryManager.StopEvent(reqId, uiEvent); // This line will not cause any exception. The implementation simply ignores it.
                _telemetryManager.StopEvent(reqId, apiEvent);
            }
            finally
            {
                _telemetryManager.Flush(reqId, ClientId);
                Assert.IsTrue(_telemetryManager.CompletedEvents.IsEmpty && _telemetryManager.EventsInProgress.IsEmpty); // No memory leak here
            }
            Assert.IsNull(_myReceiver.EventsReceived.Find(anEvent =>  // Expect NOT finding such an event
                anEvent[EventBase.EventNameKey].EndsWith("ui_event")));
        }

        [TestMethod]
        [TestCategory("PiiLoggingEnabled set to true, TenantId , UserId, and Login Hint are hashed values")]
        public void PiiLoggingEnabledTrue_ApiEventFieldsHashedTest()
        {
            var logger = new MsalLogger(Guid.NewGuid(), null);
            Logger.PiiLoggingEnabled = true;

            var reqId = _telemetryManager.GenerateNewRequestId();
            try
            {
                var e1 = new ApiEvent(logger)
                {
                    Authority = new Uri("https://login.microsoftonline.com"),
                    AuthorityType = "Aad",
                    TenantId = TenantId,
                    AccountId = UserId,
                    LoginHint = "loginHint"
                };
                _telemetryManager.StartEvent(reqId, e1);
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

                if (e1.ContainsKey(ApiEvent.LoginHintKey))
                {
                    Assert.AreNotEqual(null, e1[ApiEvent.LoginHintKey]);
                    Assert.AreNotEqual("loginHint", e1[ApiEvent.LoginHintKey]);
                }

                _telemetryManager.StopEvent(reqId, e1);
            }
            finally
            {
                _telemetryManager.Flush(reqId, ClientId);
            }
            Assert.IsTrue(_myReceiver.EventsReceived.Count > 0);
        }

        [TestMethod]
        [TestCategory("PiiLoggingEnabled set to false, TenantId & UserId set to null values")]
        public void PiiLoggingEnabledFalse_TenantIdUserIdSetToNullValueTest()
        {
            var logger = new MsalLogger(Guid.NewGuid(), null);
            Logger.PiiLoggingEnabled = false;

            var reqId = _telemetryManager.GenerateNewRequestId();
            try
            {
                var e1 = new ApiEvent(logger)
                {
                    Authority = new Uri("https://login.microsoftonline.com"),
                    AuthorityType = "Aad",
                    TenantId = TenantId,
                    AccountId = UserId,
                    LoginHint = "loginHint"
                };

                _telemetryManager.StartEvent(reqId, e1);
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

                if (e1.ContainsKey(ApiEvent.LoginHintKey))
                {
                    Assert.AreEqual(null, e1[ApiEvent.LoginHintKey]);
                }
                _telemetryManager.StopEvent(reqId, e1);
            }
            finally
            {
                _telemetryManager.Flush(reqId, ClientId);
            }
            Assert.IsTrue(_myReceiver.EventsReceived.Count > 0);
        }

        [TestMethod]
        [TestCategory("Check untrusted host Authority is set as null")]
        public void AuthorityNotInTrustedHostList_AuthorityIsSetAsNullValueTest()
        {
            var reqId = _telemetryManager.GenerateNewRequestId();
            try
            {
                var e1 = new ApiEvent(new MsalLogger(Guid.NewGuid(), null)) { Authority = new Uri("https://login.microsoftonline.com"), AuthorityType = "Aad" };
                _telemetryManager.StartEvent(reqId, e1);
                // do some stuff...
                e1.WasSuccessful = true;

                // Authority in trusted host list, should return authority with scrubbed tenant
                if (e1.ContainsKey(ApiEvent.AuthorityKey))
                {
                    Assert.AreEqual("https://login.microsoftonline.com/<tenant>", e1[ApiEvent.AuthorityKey]);
                }

                _telemetryManager.StopEvent(reqId, e1);

                // Authority host not in trusted host list, should return null
                var e2 = new ApiEvent(new MsalLogger(Guid.NewGuid(), null)) { Authority = new Uri("https://login.contoso.com"), AuthorityType = "Aad" };
                _telemetryManager.StartEvent(reqId, e2);
                // do some stuff...
                e2.WasSuccessful = true;

                if (e2.ContainsKey(ApiEvent.AuthorityKey))
                {
                    Assert.AreEqual(null, e2[ApiEvent.AuthorityKey]);
                }

                _telemetryManager.StopEvent(reqId, e2);
            }
            finally
            {
                _telemetryManager.Flush(reqId, ClientId);
            }
            Assert.IsTrue(_myReceiver.EventsReceived.Count > 0);
        }

        [TestMethod]
        [TestCategory("TelemetryInternalAPI")]
        public void TelemetryEventCountsAreCorrectTest()
        {
            string[] reqIdArray = new string[5];
            Task[] taskArray = new Task[5];
            try
            {
                for(int i=0; i < 5; i++)
                {
                    string reqId = _telemetryManager.GenerateNewRequestId();
                    reqIdArray[i] = reqId;
                    Task task= (new Task(() =>
                    {
                        var e1 = new ApiEvent(new MsalLogger(Guid.NewGuid(), null)) { Authority = new Uri("https://login.microsoftonline.com"), AuthorityType = "Aad" };
                        _telemetryManager.StartEvent(reqId, e1);
                        // do some stuff...
                        e1.WasSuccessful = true;
                        _telemetryManager.StopEvent(reqId, e1);

                        var e2 = new HttpEvent() { HttpPath = new Uri("https://contoso.com"), UserAgent = "SomeUserAgent", QueryParams = "?a=1&b=2" };
                        _telemetryManager.StartEvent(reqId, e2);
                        // do some stuff...
                        e2.HttpResponseStatus = 200;
                        _telemetryManager.StopEvent(reqId, e2);

                        var e3 = new HttpEvent() { HttpPath = new Uri("https://contoso.com"), UserAgent = "SomeOtherUserAgent", QueryParams = "?a=3&b=4" };
                        _telemetryManager.StartEvent(reqId, e3);
                        // do some stuff...
                        e2.HttpResponseStatus = 200;
                        _telemetryManager.StopEvent(reqId, e3);

                        var e4 = new CacheEvent(CacheEvent.TokenCacheWrite) { TokenType = CacheEvent.TokenTypes.AT };
                        _telemetryManager.StartEvent(reqId, e4);
                        // do some stuff...
                        _telemetryManager.StopEvent(reqId, e4);

                        var e5 = new CacheEvent(CacheEvent.TokenCacheDelete) { TokenType = CacheEvent.TokenTypes.RT };
                        _telemetryManager.StartEvent(reqId, e5);
                        // do some stuff...
                        _telemetryManager.StopEvent(reqId, e5);
                    }));
                    taskArray[i] = task;
                    task.Start();
                }
                Task.WaitAll(taskArray);
            }
            finally
            {
                foreach (string reqId in reqIdArray)
                {
                    _telemetryManager.Flush(reqId, ClientId);
                }
            }
            // Every task should have one default event with these counts
            foreach(Dictionary<string, string> telemetryEvent in _myReceiver.EventsReceived)
            {
                if(telemetryEvent[EventBase.EventNameKey] == TelemetryEventProperties.MsalDefaultEvent)
                {
                    Assert.AreEqual("2", telemetryEvent[TelemetryEventProperties.MsalHttpEventCount]);
                    Assert.AreEqual("2", telemetryEvent[TelemetryEventProperties.MsalCacheEventCount]);
                    Assert.AreEqual("0", telemetryEvent[TelemetryEventProperties.MsalUiEventCount]);
                }
            }
        }
    }
}
