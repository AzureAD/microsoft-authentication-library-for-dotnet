// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Identity.Client.Mats.Internal;
using Microsoft.Identity.Client.Mats.Internal.Constants;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.MatsTests
{
    [TestClass]
    public class ActionComparerTests : AbstractMatsTest
    {
        [TestInitialize]
        public override void Setup() => base.Setup();

        [TestCleanup]
        public override void TearDown() => base.TearDown();

        [TestMethod]
        public void NonAggregatableActionsReturnFalse()
        {
            var action1 = new ActionPropertyBag(_errorStore);
            var action2 = new ActionPropertyBag(_errorStore);

            Assert.IsFalse(ActionComparer.IsEquivalentClass(action1, action2));
        }

        [TestMethod]
        public void SamePropertiesAndValuesReturnEquivalent()
        {
            var action1 = new ActionPropertyBag(_errorStore);
            var action2 = new ActionPropertyBag(_errorStore);

            action1.IsAggregable = true;
            action2.IsAggregable = true;
            action1.Add(ActionPropertyNames.TenantIdConstStrKey, "TenantId1");
            action2.Add(ActionPropertyNames.TenantIdConstStrKey, "TenantId1");

            Assert.IsTrue(ActionComparer.IsEquivalentClass(action1, action2));
        }

        [TestMethod]
        public void DifferentPropertiesReturnFalse()
        {
            var action1 = new ActionPropertyBag(_errorStore);
            var action2 = new ActionPropertyBag(_errorStore);

            action1.IsAggregable = true;
            action2.IsAggregable = true;
            action1.Add(ActionPropertyNames.TenantIdConstStrKey, "TenantId");

            Assert.IsFalse(ActionComparer.IsEquivalentClass(action1, action2));
        }

        [TestMethod]
        public void DifferentValuesReturnFalse()
        {
            var action1 = new ActionPropertyBag(_errorStore);
            var action2 = new ActionPropertyBag(_errorStore);

            action1.IsAggregable = true;
            action2.IsAggregable = true;

            action1.Add(ActionPropertyNames.TenantIdConstStrKey, "TenantId1");
            action2.Add(ActionPropertyNames.TenantIdConstStrKey, "TenantId2");

            Assert.IsFalse(ActionComparer.IsEquivalentClass(action1, action2));
        }
    }
}
