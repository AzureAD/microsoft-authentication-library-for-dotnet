// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class OBOTests : TestBase
    {
        [TestMethod]
        [DeploymentItem(@"Resources\MultiTenantTokenCache.json")]
        public async Task MultiTenantOBOAsync()
        {
            const string tenant1 = "72f988bf-86f1-41af-91ab-2d7cd011db47";
            const string tenant2 = "49f548d0-12b7-4169-a390-bb5304d24462";

            using (var httpManager = new MockHttpManager())
            {
                // Arrange
                httpManager.AddInstanceDiscoveryMockHandler();

                var cca = CreatePcaFromFileWithAuthority(httpManager);

                // Act
                var result1 = await cca.AcquireTokenOnBehalfOf(
                    new[] { "User.Read" }, 
                    new UserAssertion("jwt"))
                    .WithAuthority(AzureCloudInstance.AzurePublic, tenant1)
                    .ExecuteAsync().ConfigureAwait(false);

                var result2 = await cca.AcquireTokenOnBehalfOf(
                   new[] { "User.Read" },
                   new UserAssertion("jwt"))
                   .WithAuthority(AzureCloudInstance.AzurePublic, tenant2)
                   .ExecuteAsync().ConfigureAwait(false);

                // Assert
                Assert.AreEqual(tenant1, result1.TenantId);
                Assert.AreEqual(tenant2, result2.TenantId);

                Assert.AreEqual(2, result1.Account.GetTenantProfiles().Count());
                Assert.AreEqual(2, result2.Account.GetTenantProfiles().Count());
                Assert.AreEqual(result1.Account.HomeAccountId, result2.Account.HomeAccountId);
                Assert.IsNotNull(result1.Account.GetTenantProfiles().Single(t => t.TenantId == tenant1));
                Assert.IsNotNull(result1.Account.GetTenantProfiles().Single(t => t.TenantId == tenant2));

                Assert.AreEqual(tenant1, result1.ClaimsPrincipal.FindFirst("tid").Value);
                Assert.AreEqual(tenant2, result2.ClaimsPrincipal.FindFirst("tid").Value);
            }
        }

        private static IConfidentialClientApplication CreatePcaFromFileWithAuthority(
           MockHttpManager httpManager,
           string authority = null)
        {
            const string clientIdInFile = "1d18b3b0-251b-4714-a02a-9956cec86c2d";
            const string tokenCacheFile = "MultiTenantTokenCache.json";

            var ccaBuilder = ConfidentialClientApplicationBuilder
                .Create(clientIdInFile)
                .WithClientSecret("secret")
                .WithHttpManager(httpManager);

            if (authority != null)
            {
                ccaBuilder = ccaBuilder.WithAuthority(authority);
            }

            var cca = ccaBuilder.BuildConcrete();
            cca.InitializeTokenCacheFromFile(ResourceHelper.GetTestResourceRelativePath(tokenCacheFile), true);
            cca.UserTokenCacheInternal.Accessor.AssertItemCount(3, 2, 3, 3, 1);
            foreach (var at in cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens())
            {
                at.OboCacheKey = "_JPLB-GtkomFJxAOWKHPHR5_ZemiZqb4fzyE_rVBx7M"; // the hash of "jwt"
            }

            cca.UserTokenCacheInternal.Accessor.DeleteAccessToken(
                cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Single(
                    at => at.HomeAccountId == "ae821e4d-f408-451a-af82-882691148603.49f548d0-12b7-4169-a390-bb5304d24462").GetKey());

            return cca;
        }
    }
}
