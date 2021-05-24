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

        [TestMethod]
        [Description("These constructors are public. If they need to change, this is a breaking change!")]
        public void AccountIdPublicApi()
        {
           new AccountId("a.b", "a", "b");
           new AccountId("adfs");
        }

        [DataTestMethod]
        [DataRow("uid", "uid", null, DisplayName = "Parse from string with no dot")]
        [DataRow("uid.utid", "uid", "utid", DisplayName = "Parse from string with one dot")]
        [DataRow("uid.1.utid", "uid.1", "utid", DisplayName = "Parse from string with 2 dots")]
        [DataRow("uid.1.2.3.utid", "uid.1.2.3", "utid", DisplayName = "Parse from string with more than 2 dots")]
        [DataRow(".utid", "", "utid", DisplayName = "Parse from string with dot as first character")]
        [DataRow("uid.", "uid", "", DisplayName = "Parse from string with dot as last character")]
        [DataRow(".", "", "", DisplayName = "Parse from string with dot as only character")]
        public void ParseFromStringNoDotTest(string homeAccountId, string objectId, string tenantId)
        {
            AccountId accountId = AccountId.ParseFromString(homeAccountId);

            Assert.AreEqual(objectId, accountId.ObjectId);
            Assert.AreEqual(tenantId, accountId.TenantId);
        }
    }
}
