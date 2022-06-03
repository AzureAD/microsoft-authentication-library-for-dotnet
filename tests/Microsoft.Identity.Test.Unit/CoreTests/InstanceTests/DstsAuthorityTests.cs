// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CoreTests.InstanceTests
{
    [TestClass]
    public class DstsAuthorityTests : TestBase

    {
        private const string TenantlessDstsAuthority = "https://foo.bar.test.core.azure-test.net/dstsv2/";
        private const string TenantedDstsAuthority = "https://foo.bar.dsts.core.azure-test.net/dstsv2/tenantId";

        [TestMethod]
        public void DstsEndpointsTest()
        {
            var instance = Authority.CreateAuthority(TenantedDstsAuthority);

            Assert.AreEqual($"{TenantedDstsAuthority}/auth2/v2.0/token", instance.GetTokenEndpoint());
            Assert.AreEqual($"{TenantedDstsAuthority}/auth2/v2.0/authorize", instance.GetAuthorizationEndpoint());
            Assert.AreEqual($"{TenantedDstsAuthority}/auth2/v2.0/devicecode", instance.GetDeviceCodeEndpoint());
        }

        [TestMethod]
        public void Validate_MinNumberOfSegments()
        {
            try
            {
                var instance = Authority.CreateAuthority(TenantlessDstsAuthority);

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
            string tenantedAuth = TenantlessDstsAuthority + Guid.NewGuid().ToString() + "/";
            Authority authority = AuthorityTestHelper.CreateAuthorityFromUrl(tenantedAuth);

            string updatedAuthority = authority.GetTenantedAuthority("other_tenant_id");
            Assert.AreEqual(tenantedAuth, updatedAuthority, "Not changed, original authority already has tenant id");

            string updatedAuthority2 = authority.GetTenantedAuthority("other_tenant_id", true);
            Assert.AreEqual("https://foo.bar.test.core.azure-test.net/other_tenant_id/", updatedAuthority2, "Not changed with forced flag");
        }

        [TestMethod]
        public void TenantlessAuthorityChanges()
        {
            string commonAuth = TenantlessDstsAuthority + "common/";
            Authority authority = AuthorityTestHelper.CreateAuthorityFromUrl(
                commonAuth);

            Assert.AreEqual("common", authority.TenantId);
        }
    }
}
