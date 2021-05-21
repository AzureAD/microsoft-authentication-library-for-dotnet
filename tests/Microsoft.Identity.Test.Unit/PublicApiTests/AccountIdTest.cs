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

        [TestMethod]
        [Description("Test the parsing of HomeAccountId with no dot")]
        public void ParseFromStringNoDotTest()
        {
            AccountId accountId = AccountId.ParseFromString("uid");

            Assert.AreEqual("uid", accountId.ObjectId);
            Assert.AreEqual(null, accountId.TenantId);
        }

        [TestMethod]
        [Description("Test the parsing of HomeAccountId with 1 dot")]
        public void ParseFromStringOneDotTest()
        {
            AccountId accountId = AccountId.ParseFromString("uid.utid");

            Assert.AreEqual("uid", accountId.ObjectId);
            Assert.AreEqual("utid", accountId.TenantId);
        }

        [TestMethod]
        [Description("Test the parsing of HomeAccountId with 2 dots")]
        public void ParseFromStringWith2DotsTest()
        {
            AccountId accountId = AccountId.ParseFromString("uid.1.utid");

            Assert.AreEqual("uid.1", accountId.ObjectId);
            Assert.AreEqual("utid", accountId.TenantId);
        }

        [TestMethod]
        [Description("Test the parsing of HomeAccountId with more than 2 dots")]
        public void ParseFromStringWithMoreThan2DotsTest()
        {
            AccountId accountId = AccountId.ParseFromString("uid.1.2.3.utid");

            Assert.AreEqual("uid.1.2.3", accountId.ObjectId);
            Assert.AreEqual("utid", accountId.TenantId);
        }

        [TestMethod]
        [Description("Test the parsing of HomeAccountId with dot as first character")]
        public void ParseFromStringWithDotAsFirstCharacterTest()
        {
            AccountId accountId = AccountId.ParseFromString(".utid");

            Assert.AreEqual("", accountId.ObjectId);
            Assert.AreEqual("utid", accountId.TenantId);
        }

        [TestMethod]
        [Description("Test the parsing of HomeAccountId with dot as last character")]
        public void ParseFromStringWithDotAsLastCharacterTest()
        {
            AccountId accountId = AccountId.ParseFromString("uid.");

            Assert.AreEqual("uid", accountId.ObjectId);
            Assert.AreEqual("", accountId.TenantId);
        }

        [TestMethod]
        [Description("Test the parsing of HomeAccountId with dot as only character")]
        public void ParseFromStringWithDotAsOnlyCharacterTest()
        {
            AccountId accountId = AccountId.ParseFromString(".");

            Assert.AreEqual("", accountId.ObjectId);
            Assert.AreEqual("", accountId.TenantId);
        }
    }
}
