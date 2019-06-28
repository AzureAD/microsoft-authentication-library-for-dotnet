// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.TelemetryCore.Internal;
using Microsoft.Identity.Client.TelemetryCore.Internal.Constants;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Test.Common;
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
    }

    [TestClass]
    public class TelemetryTests
    {
        private const string ClientId = "a1b3c3d4";

        private MyReceiver _myReceiver;
        private TelemetryManager _telemetryManager;
        private IServiceBundle _serviceBundle;
        private IPlatformProxy _platformProxy;
        private ICoreLogger _logger;
        private ICryptographyManager _crypto;

        [TestInitialize]
        public void Initialize()
        {
            TestCommon.ResetInternalStaticCaches();
            _myReceiver = new MyReceiver();
            _serviceBundle = TestCommon.CreateServiceBundleWithCustomHttpManager(null, clientId: ClientId);
            _logger = _serviceBundle.DefaultLogger;
            _platformProxy = _serviceBundle.PlatformProxy;
            _crypto = _platformProxy.CryptographyManager;
            _telemetryManager = new TelemetryManager(_serviceBundle.Config, _platformProxy, _myReceiver.HandleTelemetryEvents);
        }

        private const string TenantId = "1234";
        private const string UserId = "5678";

        [TestMethod]
        [TestCategory("TelemetryInternalAPI")]
        public void TelemetryInternalApiSample()
        {
            var correlationId = Guid.NewGuid().AsMatsCorrelationId();
            try
            {
                var e1 = new ApiEvent(_logger, _crypto, correlationId) { Authority = new Uri("https://login.microsoftonline.com"), AuthorityType = "Aad" };
                _telemetryManager.StartEvent(e1);
                // do some stuff...
                e1.WasSuccessful = true;
                _telemetryManager.StopEvent(e1);

                var e2 = new HttpEvent(correlationId) { HttpPath = new Uri("https://contoso.com"), UserAgent = "SomeUserAgent", QueryParams = "?a=1&b=2" };
                _telemetryManager.StartEvent(e2);
                // do some stuff...
                e2.HttpResponseStatus = 200;
                _telemetryManager.StopEvent(e2);
            }
            finally
            {
                _telemetryManager.Flush(correlationId);
            }
            Assert.IsTrue(_myReceiver.EventsReceived.Count > 0);
        }

        [TestMethod]
        [TestCategory("TelemetryInternalAPI")]
        public void TelemetrySkipEventsIfApiEventWasSuccessful()
        {
            _telemetryManager = new TelemetryManager(_serviceBundle.Config, _platformProxy, _myReceiver.HandleTelemetryEvents, true);

            var correlationId = Guid.NewGuid().AsMatsCorrelationId();
            try
            {
                var e1 = new ApiEvent(_logger, _crypto, correlationId) { Authority = new Uri("https://login.microsoftonline.com"), AuthorityType = "Aad" };
                _telemetryManager.StartEvent(e1);
                // do some stuff...
                e1.WasSuccessful = true;
                _telemetryManager.StopEvent(e1);

                var e2 = new UiEvent(correlationId) { UserCancelled = false };
                _telemetryManager.StartEvent(e2);
                _telemetryManager.StopEvent(e2);

                var e3 = new UiEvent(correlationId) { AccessDenied = false };
                _telemetryManager.StartEvent(e3);
                _telemetryManager.StopEvent(e3);
            }
            finally
            {
                _telemetryManager.Flush(correlationId);
            }
            Assert.AreEqual(0, _myReceiver.EventsReceived.Count);

            _telemetryManager = new TelemetryManager(_serviceBundle.Config, _platformProxy, _myReceiver.HandleTelemetryEvents);

            correlationId = Guid.NewGuid().AsMatsCorrelationId();
            try
            {
                var e1 = new ApiEvent(_logger, _crypto, correlationId) { Authority = new Uri("https://login.microsoftonline.com"), AuthorityType = "Aad" };
                _telemetryManager.StartEvent(e1);
                // do some stuff...
                e1.WasSuccessful = false;  // mimic an unsuccessful event, so that this batch should be dispatched
                _telemetryManager.StopEvent(e1);

                var e2 = new UiEvent(correlationId) { UserCancelled = true };
                _telemetryManager.StartEvent(e2);
                _telemetryManager.StopEvent(e2);

                var e3 = new UiEvent(correlationId) { AccessDenied = true };
                _telemetryManager.StartEvent(e3);
                _telemetryManager.StopEvent(e3);
            }
            finally
            {
                _telemetryManager.Flush(correlationId);
            }
            Assert.IsTrue(_myReceiver.EventsReceived.Count > 0);
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
            var correlationId = Guid.NewGuid().AsMatsCorrelationId();
            try
            {
                var anEvent = new UiEvent(correlationId);
                _telemetryManager.StartEvent(anEvent);
                _telemetryManager.StopEvent(anEvent);
            }
            finally
            {
                _telemetryManager.Flush(correlationId);
            }
            Assert.IsTrue(_myReceiver.EventsReceived[0][EventBase.EventNameKey].EndsWith("default_event"));
            Assert.IsTrue(_myReceiver.EventsReceived[1][EventBase.EventNameKey].EndsWith("ui_event"));
            Assert.AreNotEqual(_myReceiver.EventsReceived[1][EventBase.ElapsedTimeKey], "-1");
        }

        [TestMethod]
        [TestCategory("TelemetryInternalAPI")]
        public void TelemetryStartAnEventWithoutStoppingItLater() // Such event(s) becomes an orphaned event
        {
            var correlationId = Guid.NewGuid().AsMatsCorrelationId();
            try
            {
                var apiEvent = new ApiEvent(_logger, _crypto, correlationId) { Authority = new Uri("https://login.microsoftonline.com"), AuthorityType = "Aad" };
                _telemetryManager.StartEvent(apiEvent);
                var uiEvent = new UiEvent(correlationId);
                _telemetryManager.StartEvent(uiEvent);
                // Forgot to stop this event. A started event which never got stopped, becomes an orphan.
                // telemetry.StopEvent(reqId, uiEvent);
                _telemetryManager.StopEvent(apiEvent);
            }
            finally
            {
                Assert.IsFalse(_telemetryManager.CompletedEvents.IsEmpty); // There are completed event(s) inside
                Assert.IsFalse(_telemetryManager.EventsInProgress.IsEmpty); // There is an orphaned event inside
                _telemetryManager.Flush(correlationId);
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
            var correlationId = Guid.NewGuid().AsMatsCorrelationId();
            try
            {
                var apiEvent = new ApiEvent(_logger, _crypto, correlationId) { Authority = new Uri("https://login.microsoftonline.com"), AuthorityType = "Aad" };
                _telemetryManager.StartEvent(apiEvent);
                var uiEvent = new UiEvent(correlationId);
                // Forgot to start this event
                // telemetry.StartEvent(reqId, uiEvent);
                // Now attempting to stop a never-started event
                _telemetryManager.StopEvent(uiEvent); // This line will not cause any exception. The implementation simply ignores it.
                _telemetryManager.StopEvent(apiEvent);
            }
            finally
            {
                _telemetryManager.Flush(correlationId);
                Assert.IsTrue(_telemetryManager.CompletedEvents.IsEmpty && _telemetryManager.EventsInProgress.IsEmpty); // No memory leak here
            }
            Assert.IsNull(_myReceiver.EventsReceived.Find(anEvent =>  // Expect NOT finding such an event
                anEvent[EventBase.EventNameKey].EndsWith("ui_event")));
        }

        [TestMethod]
        [TestCategory("PiiLoggingEnabled set to true, TenantId , UserId, and Login Hint are hashed values")]
        public void PiiLoggingEnabledTrue_ApiEventFieldsHashedTest()
        {
            var serviceBundle = TestCommon.CreateServiceBundleWithCustomHttpManager(null, enablePiiLogging: true);
            _logger = serviceBundle.DefaultLogger;

            var correlationId = Guid.NewGuid().AsMatsCorrelationId();
            try
            {
                var e1 = new ApiEvent(_logger, _crypto, correlationId)
                {
                    Authority = new Uri("https://login.microsoftonline.com"),
                    AuthorityType = "Aad",
                    TenantId = TenantId,
                    AccountId = UserId,
                    LoginHint = "loginHint"
                };
                _telemetryManager.StartEvent(e1);
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

                _telemetryManager.StopEvent(e1);
            }
            finally
            {
                _telemetryManager.Flush(correlationId);
            }
            Assert.IsTrue(_myReceiver.EventsReceived.Count > 0);
        }

        [TestMethod]
        [TestCategory("PiiLoggingEnabled set to false, TenantId & UserId set to null values")]
        public void PiiLoggingEnabledFalse_TenantIdUserIdSetToNullValueTest()
        {
            var correlationId = Guid.NewGuid().AsMatsCorrelationId();
            try
            {
                var e1 = new ApiEvent(_logger, _crypto, correlationId)
                {
                    Authority = new Uri("https://login.microsoftonline.com"),
                    AuthorityType = "Aad",
                    TenantId = TenantId,
                    AccountId = UserId,
                    LoginHint = "loginHint"
                };

                _telemetryManager.StartEvent(e1);
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
                _telemetryManager.StopEvent(e1);
            }
            finally
            {
                _telemetryManager.Flush(correlationId);
            }
            Assert.IsTrue(_myReceiver.EventsReceived.Count > 0);
        }

        [TestMethod]
        [TestCategory("Check untrusted host Authority is set as null")]
        public void AuthorityNotInTrustedHostList_AuthorityIsSetAsNullValueTest()
        {
            var correlationId = Guid.NewGuid().AsMatsCorrelationId();
            try
            {
                var e1 = new ApiEvent(_logger, _crypto, correlationId) { Authority = new Uri("https://login.microsoftonline.com"), AuthorityType = "Aad" };
                _telemetryManager.StartEvent(e1);
                // do some stuff...
                e1.WasSuccessful = true;

                // Authority in trusted host list, should return authority with scrubbed tenant
                if (e1.ContainsKey(ApiEvent.AuthorityKey))
                {
                    Assert.AreEqual("https://login.microsoftonline.com/<tenant>", e1[ApiEvent.AuthorityKey]);
                }

                _telemetryManager.StopEvent(e1);

                // Authority host not in trusted host list, should return null
                var e2 = new ApiEvent(_logger, _crypto, correlationId) { Authority = new Uri("https://login.contoso.com"), AuthorityType = "Aad" };
                _telemetryManager.StartEvent(e2);
                // do some stuff...
                e2.WasSuccessful = true;

                if (e2.ContainsKey(ApiEvent.AuthorityKey))
                {
                    Assert.AreEqual(null, e2[ApiEvent.AuthorityKey]);
                }

                _telemetryManager.StopEvent(e2);
            }
            finally
            {
                _telemetryManager.Flush(correlationId);
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
                    var correlationId = Guid.NewGuid().AsMatsCorrelationId();
                    reqIdArray[i] = correlationId;
                    Task task= new Task(() =>
                    {
                        var e1 = new ApiEvent(_logger, _crypto, correlationId) { Authority = new Uri("https://login.microsoftonline.com"), AuthorityType = "Aad" };
                        _telemetryManager.StartEvent(e1);
                        // do some stuff...
                        e1.WasSuccessful = true;
                        _telemetryManager.StopEvent(e1);

                        var e2 = new HttpEvent(correlationId) { HttpPath = new Uri("https://contoso.com"), UserAgent = "SomeUserAgent", QueryParams = "?a=1&b=2" };
                        _telemetryManager.StartEvent(e2);
                        // do some stuff...
                        e2.HttpResponseStatus = 200;
                        _telemetryManager.StopEvent(e2);

                        var e3 = new HttpEvent(correlationId) { HttpPath = new Uri("https://contoso.com"), UserAgent = "SomeOtherUserAgent", QueryParams = "?a=3&b=4" };
                        _telemetryManager.StartEvent(e3);
                        // do some stuff...
                        e2.HttpResponseStatus = 200;
                        _telemetryManager.StopEvent(e3);

                        var e4 = new CacheEvent(CacheEvent.TokenCacheWrite, correlationId) { TokenType = CacheEvent.TokenTypes.AT };
                        _telemetryManager.StartEvent(e4);
                        // do some stuff...
                        _telemetryManager.StopEvent(e4);

                        var e5 = new CacheEvent(CacheEvent.TokenCacheDelete, correlationId) { TokenType = CacheEvent.TokenTypes.RT };
                        _telemetryManager.StartEvent(e5);
                        // do some stuff...
                        _telemetryManager.StopEvent(e5);
                    });
                    taskArray[i] = task;
                    task.Start();
                }
                Task.WaitAll(taskArray);
            }
            finally
            {
                foreach (string reqId in reqIdArray)
                {
                    _telemetryManager.Flush(reqId);
                }
            }
            // Every task should have one default event with these counts
            foreach(Dictionary<string, string> telemetryEvent in _myReceiver.EventsReceived)
            {
                if(telemetryEvent[EventBase.EventNameKey] == "msal.default_event")
                {
                    Assert.AreEqual("2", telemetryEvent[MsalTelemetryBlobEventNames.HttpEventCountTelemetryBatchKey]);
                    Assert.AreEqual("2", telemetryEvent[MsalTelemetryBlobEventNames.CacheEventCountConstStrKey]);
                    Assert.AreEqual("0", telemetryEvent[MsalTelemetryBlobEventNames.UiEventCountTelemetryBatchKey]);
                }
            }
        }
    }
}
