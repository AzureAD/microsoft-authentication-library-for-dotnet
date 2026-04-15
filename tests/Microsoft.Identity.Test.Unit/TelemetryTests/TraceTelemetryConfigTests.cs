// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using Microsoft.Identity.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.TelemetryTests
{
    [TestClass]
    public class TraceTelemetryConfigTests : TestBase
    {
#pragma warning disable CS0618 // Type or member is obsolete
        [TestMethod]
        public void Constructor_SetsSessionId()
        {
            var config = new TraceTelemetryConfig();
            Assert.IsNotNull(config.SessionId);
            Assert.IsTrue(Guid.TryParse(config.SessionId, out _), "SessionId should be a valid GUID");
        }

        [TestMethod]
        public void SessionId_IsUnique_AcrossInstances()
        {
            var config1 = new TraceTelemetryConfig();
            var config2 = new TraceTelemetryConfig();
            Assert.AreNotEqual(config1.SessionId, config2.SessionId);
        }

        [TestMethod]
        public void AudienceType_IsPreProduction()
        {
            var config = new TraceTelemetryConfig();
            Assert.AreEqual(TelemetryAudienceType.PreProduction, config.AudienceType);
        }

        [TestMethod]
        public void AllowedScopes_IsEmpty()
        {
            var config = new TraceTelemetryConfig();
            Assert.IsNotNull(config.AllowedScopes);
            foreach (var _ in config.AllowedScopes)
            {
                Assert.Fail("AllowedScopes should be empty");
            }
        }

        [TestMethod]
        public void DispatchAction_IsNotNull()
        {
            var config = new TraceTelemetryConfig();
            Assert.IsNotNull(config.DispatchAction);
        }
#pragma warning restore CS0618
    }
}
