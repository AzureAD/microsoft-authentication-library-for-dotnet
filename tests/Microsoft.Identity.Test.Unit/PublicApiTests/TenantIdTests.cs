// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class TenantIdTests : TestBase
    {

        [DataTestMethod]
        [DataRow(TestConstants.AuthorityCommonTenant, TestConstants.Common, DisplayName = "Common endpoint")]
        [DataRow(TestConstants.AuthorityNotKnownCommon, TestConstants.Common, DisplayName = "Common endpoint")]
        [DataRow(TestConstants.AuthorityHomeTenant, "home", DisplayName = "Home endpoint")]
        [DataRow(TestConstants.AuthorityGuestTenant, "guest", DisplayName = "Guest endpoint")]
        [DataRow(TestConstants.AuthorityOrganizationsTenant, "organizations", DisplayName = "Organizations endpoint")]
        [DataRow(TestConstants.AadAuthorityWithTestTenantId, TestConstants.AadTenantId, DisplayName = "Tenant Id")]
        [DataRow(TestConstants.AuthorityConsumersTenant, "consumers", DisplayName = "Consumers Endpoint")]
        [DataRow(TestConstants.AuthorityConsumerTidTenant, TestConstants.MsaTenantId, DisplayName = "Consumer Tenant Id")]
        [DataRow(TestConstants.AuthorityNotKnownTenanted, TestConstants.Utid, DisplayName = "Tenant Id")]
        [DataRow(TestConstants.AuthorityUtidTenant, TestConstants.Utid, DisplayName = "Tenant Id")]
        [DataRow(TestConstants.AuthorityUtid2Tenant, TestConstants.Utid2, DisplayName = "Tenant Id")]
        [DataRow(TestConstants.B2CCustomDomain, TestConstants.CatsAreAwesome, DisplayName = "B2C Custom Domain Tenant Id")]
        [DataRow(TestConstants.B2CLoginAuthority, TestConstants.SomeTenantId, DisplayName = "B2C Tenant Id")]
        [DataRow(TestConstants.B2CLoginAuthorityUsGov, TestConstants.SomeTenantId, DisplayName = "B2C US GOV Tenant Id")]
        [DataRow(TestConstants.B2CCustomDomain, TestConstants.CatsAreAwesome, DisplayName = "B2C Custom Domain Tenant Id")]
        [DataRow(TestConstants.B2CLoginAuthorityBlackforest, TestConstants.SomeTenantId, DisplayName = "B2C Blackforest Tenant Id")]
        [DataRow(TestConstants.B2CLoginAuthorityMoonCake, TestConstants.SomeTenantId, DisplayName = "B2C MoonCake Tenant Id")]
        [DataRow(TestConstants.AuthoritySovereignCNTenant, TestConstants.TenantId, DisplayName = "Sovereign Tenant Id")]
        [DataRow(TestConstants.AuthoritySovereignDETenant, TestConstants.TenantId, DisplayName = "Sovereign Tenant Id")]
        [DataRow(TestConstants.DstsAuthorityTenanted, TestConstants.TenantId, DisplayName = "DSTS Tenant Id")]
        [DataRow(TestConstants.DstsAuthorityCommon, TestConstants.Common, DisplayName = "DSTS Common Tenant Id")]
        public void ParseTest_Success(string authorityUrl, string expectedTenantId)
        {
            var tenantId = AuthorityHelpers.GetTenantId(new Uri(authorityUrl));

            Assert.AreEqual(expectedTenantId, tenantId);
        }

        [DataTestMethod]
        [DynamicData(nameof(TestData.GetAuthorityWithExpectedTenantId), typeof(TestData), DynamicDataSourceType.Method)]
        public void ParseTestDynamic_Success(Uri authorityUrl, string expectedTenantId)
        {
            ParseTest_Success(authorityUrl.ToString(), expectedTenantId);
        }

        [DataTestMethod]
        [DataRow(TestConstants.ADFSAuthority, DisplayName = "ADFS Authority")]
        [DataRow(TestConstants.ADFSAuthority2, DisplayName = "ADFS Authority")]
        public void ParseTest_NoTenantId(string authorityUrl)
        {
            var tenantId = AuthorityHelpers.GetTenantId(new Uri(authorityUrl));

            Assert.IsNull(tenantId);
        }

        [TestMethod]
        public async Task TenantIdOverride()
        {
            using (var httpManager = new MockHttpManager())
            {
                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithAuthority("https://login.microsoftonline.com/tenanta")
                    .WithHttpManager(httpManager)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .Build();

                
                httpManager.AddInstanceDiscoveryMockHandler();
                var handler = httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                var result = await app
                    .AcquireTokenForClient(TestConstants.s_scope)
                    .WithTenantIdFromAuthority(new Uri("https://login.microsoftonline.com/tenantb"))
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(
                    "https://login.microsoftonline.com/tenantb/oauth2/v2.0/token",
                    handler.ActualRequestMessage.RequestUri.AbsoluteUri);

                Assert.AreEqual(TestConstants.s_scope.AsSingleString(), result.Scopes.AsSingleString());
            }
        }
    }
}
