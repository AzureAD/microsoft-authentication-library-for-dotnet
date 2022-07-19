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
        private const string TenantlessDstsAuthority = "https://some.url.dsts.core.azure-test.net/dstsv2/";
        
        private const string TenantedDstsAuthority = "https://some.url.dsts.core.azure-test.net/dstsv2/tenantid";
        private const string CommonAuthority = "https://some.url.dsts.core.azure-test.net/dstsv2/common";

        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();
        }

        private static MockHttpMessageHandler CreateTokenResponseHttpHandler(string authority)
        {
            IDictionary<string, string> expectedRequestBody = new Dictionary<string, string>();
            expectedRequestBody.Add("scope", TestConstants.ScopeStr);
            expectedRequestBody.Add("grant_type", "client_credentials");
            expectedRequestBody.Add("client_id", TestConstants.ClientId);
            expectedRequestBody.Add("client_secret", TestConstants.ClientSecret);

            return new MockHttpMessageHandler()
            {
                ExpectedUrl = $"{authority}/oauth2/v2.0/token",
                ExpectedMethod = HttpMethod.Post,
                ExpectedPostData = expectedRequestBody,
                ResponseMessage = MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage(MockHelpers.CreateClientInfo(TestConstants.Uid, TestConstants.Utid))
            };
        }

        [DataTestMethod]
        [DataRow(CommonAuthority)]
        [DataRow(TenantedDstsAuthority)]
        public async Task DstsClientCredentialSuccessfulTestAsync(string authority)
        {
            using (var httpManager = new MockHttpManager())
            {
                IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithHttpManager(httpManager)
                    .WithAuthority(authority)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .Build();

                Assert.AreEqual(authority + "/", app.Authority);
                var confidentailClientApp = (ConfidentialClientApplication)app;
                Assert.AreEqual(AuthorityType.Dsts, confidentailClientApp.AuthorityInfo.AuthorityType);

                httpManager.AddMockHandler(CreateTokenResponseHttpHandler(authority));

                AuthenticationResult result = await app
                    .AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                result = await app
                    .AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [DataTestMethod]
        [DataRow(CommonAuthority)]
        [DataRow(TenantedDstsAuthority)]
        public void DstsEndpointsTest(string authority)
        {
            var instance = Authority.CreateAuthority(authority);

            Assert.AreEqual($"{authority}/oauth2/v2.0/token", instance.GetTokenEndpoint());
            Assert.AreEqual($"{authority}/oauth2/v2.0/authorize", instance.GetAuthorizationEndpoint());
            Assert.AreEqual($"{authority}/oauth2/v2.0/devicecode", instance.GetDeviceCodeEndpoint());
            Assert.AreEqual($"https://some.url.dsts.core.azure-test.net/dstsv2/common/userrealm/", instance.AuthorityInfo.UserRealmUriPrefix);
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
            Assert.AreEqual("https://some.url.dsts.azure-test.net/other_tenant_id/", updatedAuthority2, "Not changed with forced flag");
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
