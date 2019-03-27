// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Mats.Internal;
using Microsoft.Identity.Client.Mats.Internal.Constants;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.MatsTests
{
    [TestClass]
    public class ContextStoreTests : AbstractMatsTest
    {
        [TestInitialize]
        public override void Setup() => base.Setup();

        [TestCleanup]
        public override void TearDown() => base.TearDown();

        [TestMethod]
        public void AddContextCorrectlyAddsFieldsToPropertyBagContents()
        {
            string sessionId = "00000000-0000-0000-0000-000000000000";
            var contextStore = ContextStore.CreateContextStore(MatsAudienceType.PreProduction, "AppName", "1.0", "deviceId", "deviceNetworkState", sessionId, 1);
            var propertyBag = new PropertyBag(EventType.Scenario, null);
            var propertyList = new List<IPropertyBag> { propertyBag };
            contextStore.AddContext(propertyList);

            var contentsWithContext = propertyBag.GetContents();

            Assert.AreEqual(MatsConverter.AsString(MatsAudienceType.PreProduction), contentsWithContext.StringProperties[ContextPropertyNames.AppAudienceConstStrKey]);
            Assert.AreEqual("AppName", contentsWithContext.StringProperties[ContextPropertyNames.AppNameConstStrKey]);
            Assert.AreEqual("1.0", contentsWithContext.StringProperties[ContextPropertyNames.AppVerConstStrKey]);
            Assert.AreEqual("deviceId", contentsWithContext.StringProperties[ContextPropertyNames.DptiConstStrKey]);
            Assert.AreEqual(sessionId, contentsWithContext.StringProperties[ContextPropertyNames.SessionIdConstStrKey]);
            Assert.AreEqual(1, contentsWithContext.IntProperties[ContextPropertyNames.PlatformConstStrKey]);
        }

    }
}
