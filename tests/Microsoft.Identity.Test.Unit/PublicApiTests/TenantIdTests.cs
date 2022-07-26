// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class TenantIdTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
        }

        [DataTestMethod]
        [DataRow(TestConstants.AuthorityCommonTenant, "common", DisplayName = "Common endpoint")]
        [DataRow(TestConstants.AuthorityNotKnownCommon, "common", DisplayName = "Common endpoint")]
        [DataRow(TestConstants.AuthorityHomeTenant, "home", DisplayName = "Home endpoint")]
        [DataRow(TestConstants.AuthorityGuestTenant, "guest", DisplayName = "Guest endpoint")]
        [DataRow(TestConstants.AuthorityOrganizationsTenant, "organizations", DisplayName = "Organizations endpoint")]
        [DataRow(TestConstants.AadAuthorityWithTestTenantId, TestConstants.AadTenantId, DisplayName = "Tenant Id")]
        [DataRow(TestConstants.AuthorityConsumersTenant, "consumers", DisplayName = "Consumers Endpoint")]
        [DataRow(TestConstants.AuthorityConsumerTidTenant, TestConstants.MsaTenantId, DisplayName = "Consumer Tenant Id")]
        [DataRow(TestConstants.AuthorityNotKnownTenanted, TestConstants.Utid, DisplayName = "Tenant Id")]
        [DataRow(TestConstants.AuthorityUtidTenant, TestConstants.Utid, DisplayName = "Tenant Id")]
        [DataRow(TestConstants.AuthorityUtid2Tenant, TestConstants.Utid2, DisplayName = "Tenant Id")]
        [DataRow(TestConstants.B2CCustomDomain, TestConstants.B2CCustomDomain, DisplayName = "B2C Custom Domain Tenant Id")]
        [DataRow(TestConstants.B2CLoginAuthority, TestConstants.B2CLoginAuthority, DisplayName = "B2C Tenant Id")]
        [DataRow(TestConstants.B2CLoginAuthorityUsGov, TestConstants.B2CLoginAuthorityUsGov, DisplayName = "B2C US GOV Tenant Id")]
        [DataRow(TestConstants.B2CCustomDomain, TestConstants.B2CCustomDomain, DisplayName = "B2C Custom Domain Tenant Id")]
        [DataRow(TestConstants.B2CLoginAuthorityBlackforest, TestConstants.B2CLoginAuthorityBlackforest, DisplayName = "B2C Blackforest Tenant Id")]
        [DataRow(TestConstants.B2CLoginAuthorityMoonCake, TestConstants.B2CLoginAuthorityMoonCake, DisplayName = "B2C MoonCake Tenant Id")]
        [DataRow(TestConstants.AuthoritySovereignCNTenant, TestConstants.TenantId, DisplayName = "Sovereign Tenant Id")]
        [DataRow(TestConstants.AuthoritySovereignDETenant, TestConstants.TenantId, DisplayName = "Sovereign Tenant Id")]
        [DataRow(TestConstants.DstsAuthorityTenanted, "tenantid", DisplayName = "DSTS Tenant Id")]
        [DataRow(TestConstants.DstsAuthorityTenantless, "tenantid", DisplayName = "DSTS Tenantless Tenant Id")]
        [DataRow(TestConstants.DstsAuthorityCommon, "common", DisplayName = "DSTS Common Tenant Id")]
        public void ParseTest_Success(string authorityUrl, string expectedTenantId)
        {
            var tenantId = ClientApplicationBase.GetTenantId(new Uri(authorityUrl));

            Assert.AreEqual(expectedTenantId, tenantId);
        }

        [DataTestMethod]
        [DataRow(TestConstants.ADFSAuthority, DisplayName = "ADFS Authority")]
        [DataRow(TestConstants.ADFSAuthority2, DisplayName = "ADFS Authority")]
        public void ParseTest_NoTenantId(string authorityUrl)
        {
            var tenantId = ClientApplicationBase.GetTenantId(new Uri(authorityUrl));

            Assert.IsNull(tenantId);
        }
    }
}
