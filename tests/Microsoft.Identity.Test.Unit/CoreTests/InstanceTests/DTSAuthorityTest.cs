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
    public class DstsAuthorityTest

    {
        private const string _tenantlessDstsAuthority = "https://foo.bar.test.core.azure-test.net/dstsv2/";

        [TestMethod]
        public void Validate_MinNumberOfSegments()
        {
            try
            {
                var instance = Authority.CreateAuthority(_tenantlessDstsAuthority);

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
            string tenantedAuth = _tenantlessDstsAuthority + Guid.NewGuid().ToString() + "/";
            Authority authority = AuthorityTestHelper.CreateAuthorityFromUrl(tenantedAuth);

            string updatedAuthority = authority.GetTenantedAuthority("other_tenant_id");
            Assert.AreEqual(tenantedAuth, updatedAuthority, "Not changed, original authority already has tenant id");

            string updatedAuthority2 = authority.GetTenantedAuthority("other_tenant_id", true);
            Assert.AreEqual("https://foo.bar.test.core.azure-test.net/other_tenant_id/", updatedAuthority2, "Not changed with forced flag");
        }

        [TestMethod]
        public void TenantlessAuthorityChanges()
        {
            string commonAuth = _tenantlessDstsAuthority + "common/";
            Authority authority = AuthorityTestHelper.CreateAuthorityFromUrl(
                commonAuth);

            Assert.AreEqual("common", authority.TenantId);
        }
    }
}
