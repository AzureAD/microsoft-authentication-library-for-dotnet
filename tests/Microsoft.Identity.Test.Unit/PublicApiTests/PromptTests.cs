﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class PromptTests : TestBase
    {

        [TestMethod()]
        [TestCategory(TestCategories.PromptTests)]
        public void EqualityTest()
        {
            Prompt ub1 = Prompt.Consent;
            Prompt ub2 = Prompt.ForceLogin;

            Assert.AreNotEqual(ub1, ub2);
            Assert.AreEqual(ub1, Prompt.Consent);
            Assert.IsTrue(ub1 != ub2);
            Assert.IsTrue(ub1 == Prompt.Consent);
        }
    }
}
