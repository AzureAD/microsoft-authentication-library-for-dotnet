// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class AccountIdTest
    {
        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
        }

        [TestMethod]
        public void EqualityTest()
        {
            // Arrange
            AccountId accountId1 = new AccountId("a.b", "a", "b");
            AccountId accountId2 = new AccountId("a.b", "a", "b");

            // Act Assert
            Assert.AreEqual(accountId1, accountId2);
        }
    }
}
