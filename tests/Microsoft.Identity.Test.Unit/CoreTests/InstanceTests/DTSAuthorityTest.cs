// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CoreTests.InstanceTests
{
    [TestClass]
    public class DTSAuthorityTest
    {
        string _authorityUri = $"https://co2agg04-passive-dsts.dsts.core.azure-test.net/dstsv2/";

        [TestMethod]
        public void Validate_MinNumberOfSegments()
        {
            try
            {
                var instance = Authority.CreateAuthority(_authorityUri);

                Assert.Fail("test should have failed");
            }
            catch (Exception exc)
            {
                Assert.IsInstanceOfType(exc, typeof(ArgumentException));
                Assert.AreEqual(MsalErrorMessage.DstsAuthorityUriInvalidPath, exc.Message);
            }
        }

        [TestMethod]
        public void CreateAuthorityFromTenantedWithTenantTest()
        {
            string tenantedAuth = _authorityUri + Guid.NewGuid().ToString() + "/";
            Authority authority = AuthorityTestHelper.CreateAuthorityFromUrl(tenantedAuth);

            string updatedAuthority = authority.GetTenantedAuthority("other_tenant_id");
            Assert.AreEqual(tenantedAuth, updatedAuthority, "Not changed, original authority already has tenant id");

            string updatedAuthority2 = authority.GetTenantedAuthority("other_tenant_id", true);
            Assert.AreEqual("https://co2agg04-passive-dsts.dsts.core.azure-test.net/other_tenant_id/", updatedAuthority2, "Not changed with forced flag");
        }

        [TestMethod]
        public void CreateAuthorityFromCommonWithTenantTest()
        {
            Authority authority = AuthorityTestHelper.CreateAuthorityFromUrl("https://login.microsoft.com/common");

            string updatedAuthority = authority.GetTenantedAuthority("other_tenant_id");
            Assert.AreEqual("https://login.microsoft.com/other_tenant_id/", updatedAuthority, "Changed, original is common");

            string updatedAuthority2 = authority.GetTenantedAuthority("other_tenant_id", true);
            Assert.AreEqual("https://login.microsoft.com/other_tenant_id/", updatedAuthority2, "Changed with forced flag");
        }

        [TestMethod]
        public void TenantlessAuthorityChanges()
        {
            string commonAuth = _authorityUri + "common/";
            Authority authority = AuthorityTestHelper.CreateAuthorityFromUrl(
                commonAuth);

            Assert.AreEqual("common", authority.TenantId);
        }

        [TestMethod]
        public void CanonicalAuthorityInitTest()
        {
            string UriNoPort = TestConstants.B2CAuthority;
            string UriNoPortTailSlash = TestConstants.B2CAuthority;

            string UriDefaultPort = $"https://login.microsoftonline.in:443/tfp/tenant/{TestConstants.B2CSignUpSignIn}";

            string UriCustomPort = $"https://login.microsoftonline.in:444/tfp/tenant/{TestConstants.B2CSignUpSignIn}";
            string UriCustomPortTailSlash = $"https://login.microsoftonline.in:444/tfp/tenant/{TestConstants.B2CSignUpSignIn}/";
            string UriVanityPort = TestConstants.B2CLoginAuthority;

            var authority = new B2CAuthority(new AuthorityInfo(AuthorityType.B2C, UriNoPort, true));
            Assert.AreEqual(UriNoPortTailSlash, authority.AuthorityInfo.CanonicalAuthority);

            authority = new B2CAuthority(new AuthorityInfo(AuthorityType.B2C, UriDefaultPort, true));
            Assert.AreEqual(UriNoPortTailSlash, authority.AuthorityInfo.CanonicalAuthority);

            authority = new B2CAuthority(new AuthorityInfo(AuthorityType.B2C, UriCustomPort, true));
            Assert.AreEqual(UriCustomPortTailSlash, authority.AuthorityInfo.CanonicalAuthority);

            authority = new B2CAuthority(new AuthorityInfo(AuthorityType.B2C, UriVanityPort, true));
            Assert.AreEqual(UriVanityPort, authority.AuthorityInfo.CanonicalAuthority);
        }

    }
}
