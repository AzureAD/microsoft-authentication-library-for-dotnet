// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CoreTests.InstanceTests
{
    [TestClass]
    public class DstsAuthorityTests : TestBase
    {
        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();
        }

        private static MockHttpMessageHandler CreateTokenResponseHttpHandler(string authority)
        {
            IDictionary<string, string> expectedRequestBody = new Dictionary<string, string>
            {
                { "scope", TestConstants.ScopeStr },
                { "grant_type", "client_credentials" },
                { "client_id", TestConstants.ClientId },
                { "client_secret", TestConstants.ClientSecret }
            };

            return new MockHttpMessageHandler()
            {
                ExpectedUrl = $"{authority}/oauth2/v2.0/token",
                ExpectedMethod = HttpMethod.Post,
                ExpectedPostData = expectedRequestBody,
                ResponseMessage = MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage(MockHelpers.CreateClientInfo(TestConstants.Uid, TestConstants.Utid))
            };
        }

        [DataTestMethod]
        [DataRow(TestConstants.DstsAuthorityCommon)]
        [DataRow(TestConstants.DstsAuthorityTenanted)]
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
        [DataRow(TestConstants.DstsAuthorityCommon)]
        [DataRow(TestConstants.DstsAuthorityTenanted)]
        public void DstsEndpointsTest(string authority)
        {
            var instance = Authority.CreateAuthority(authority);
            var _harness = base.CreateTestHarness();
            var _testRequestContext = new RequestContext(
                _harness.ServiceBundle,
                Guid.NewGuid());

            Assert.AreEqual($"{authority}/oauth2/v2.0/token", instance.GetTokenEndpointAsync(_testRequestContext).Result);
            Assert.AreEqual($"{authority}/oauth2/v2.0/authorize", instance.GetAuthorizationEndpointAsync(_testRequestContext).Result);
            Assert.AreEqual($"{authority}/oauth2/v2.0/devicecode", instance.GetDeviceCodeEndpointAsync(_testRequestContext).Result);
            Assert.AreEqual($"https://some.url.dsts.core.azure-test.net/dstsv2/common/userrealm/", instance.AuthorityInfo.UserRealmUriPrefix);
        }

        [TestMethod]
        public void Validate_MinNumberOfSegments()
        {
            try
            {
                var instance = Authority.CreateAuthority(TestConstants.DstsAuthorityTenantless);

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
            
            Authority authority = AuthorityTestHelper.CreateAuthorityFromUrl(TestConstants.DstsAuthorityTenanted);
            Assert.AreEqual("tenantid", authority.TenantId);
            
            string updatedAuthority = authority.GetTenantedAuthority("tenant2");            

            Assert.AreEqual(
                TestConstants.DstsAuthorityTenanted,
                updatedAuthority.TrimEnd('/'),
                "Not changed, original authority already has tenant id");

            string updatedAuthority2 = authority.GetTenantedAuthority("tenant2", true);
            Assert.AreEqual(
                "https://some.url.dsts.core.azure-test.net/dstsv2/tenant2/",
                updatedAuthority2);
        }

        [TestMethod]
        public void TenantlessAuthorityChanges()
        {
            string commonAuth = TestConstants.DstsAuthorityTenantless + "common/";
            Authority authority = AuthorityTestHelper.CreateAuthorityFromUrl(
                commonAuth);

            Assert.AreEqual("common", authority.TenantId);
        }
    }
}
