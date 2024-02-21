// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.RequestsTests
{
    [TestClass]
    public class OnBehalfOfTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
        }

        private MockHttpMessageHandler AddMockHandlerAadSuccess(
            MockHttpManager httpManager,
            string authority = TestConstants.AuthorityCommonTenant,
            IList<string> unexpectedHeaders = null,
            HttpResponseMessage responseMessage = null,
            IDictionary<string, string> expectedPostData = null)
        {
            var handler = new MockHttpMessageHandler
            {
                ExpectedUrl = authority + "oauth2/v2.0/token",
                ExpectedMethod = HttpMethod.Post,
                ResponseMessage = responseMessage ?? MockHelpers.CreateSuccessTokenResponseMessage(),
                UnexpectedRequestHeaders = unexpectedHeaders,
            };
            if (expectedPostData != null)
            {
                handler.ExpectedPostData = expectedPostData;
            }
            httpManager.AddMockHandler(handler);

            return handler;
        }

        [TestMethod]
        public async Task OboSkipsRegional_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                AddMockHandlerAadSuccess(httpManager);

                var cca = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithAzureRegion("eastus1")
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                UserAssertion userAssertion = new UserAssertion(TestConstants.DefaultAccessToken);
                var result = await cca.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion)
                                      .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(
                    TokenSource.IdentityProvider,
                    result.AuthenticationResultMetadata.TokenSource);

                Assert.AreEqual(
                    "https://login.microsoftonline.com/common/oauth2/v2.0/token", // no region
                    result.AuthenticationResultMetadata.TokenEndpoint);
            }
        }

        [TestMethod]
        public async Task AccessTokenExpiredRefreshTokenNotAvailable_TestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                AddMockHandlerAadSuccess(httpManager);

                var cca = BuildCCA(httpManager);

                UserAssertion userAssertion = new UserAssertion(TestConstants.DefaultAccessToken);
                var result = await cca.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion).ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TestConstants.ATSecret, result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                //Expire access tokens
                TokenCacheHelper.ExpireAllAccessTokens(cca.UserTokenCacheInternal);

                AddMockHandlerAadSuccess(
                    httpManager,
                    expectedPostData: new Dictionary<string, string> { { OAuth2Parameter.GrantType, OAuth2GrantType.JwtBearer } });

                result = await cca.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion).ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TestConstants.ATSecret, result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public async Task MissMatchUserAssertions_TestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                AddMockHandlerAadSuccess(httpManager);

                var cca = BuildCCA(httpManager);

                UserAssertion userAssertion = new UserAssertion(TestConstants.DefaultAccessToken);
                var result = await cca.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion).ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TestConstants.ATSecret, result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                //Update user assertions
                TokenCacheHelper.UpdateUserAssertions(cca);

                AddMockHandlerAadSuccess(httpManager);

                //Access and refresh tokens have a different user assertion so MSAL should perform OBO.
                result = await cca.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion).ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TestConstants.ATSecret, result.AccessToken);
                Assert.AreEqual(result.AuthenticationResultMetadata.TokenSource, TokenSource.IdentityProvider);
            }
        }

        [TestMethod]
        public async Task AccessTokenInCache_TestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                AddMockHandlerAadSuccess(httpManager);

                var cca = BuildCCA(httpManager);

                var userCacheAccess = cca.UserTokenCache.RecordAccess();

                UserAssertion userAssertion = new UserAssertion(TestConstants.DefaultAccessToken);
                var result = await cca.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion).ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TestConstants.ATSecret, result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                userCacheAccess.AssertAccessCounts(1, 1);

                result = await cca.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion).ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(TestConstants.ATSecret, result.AccessToken);
                userCacheAccess.AssertAccessCounts(2, 1);

                MsalAccessTokenCacheItem cachedAccessToken = cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Single();
                Assert.AreEqual(userAssertion.AssertionHash, cachedAccessToken.OboCacheKey);
                Assert.AreEqual(0, cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count);
            }
        }

        [TestMethod]
        public async Task NullCcs_TestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();
                var extraUnexpectedHeaders = new List<string>() { { Constants.CcsRoutingHintHeader } };
                AddMockHandlerAadSuccess(httpManager, unexpectedHeaders: extraUnexpectedHeaders);

                var cca = BuildCCA(httpManager);

                UserAssertion userAssertion = new UserAssertion(TestConstants.DefaultAccessToken);
                var result = await cca.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion)
                                      .WithCcsRoutingHint("")
                                      .WithCcsRoutingHint("", "")
                                      .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TestConstants.ATSecret, result.AccessToken);

                result = await cca.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion)
                      .WithCcsRoutingHint("")
                      .WithCcsRoutingHint("", "")
                      .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TestConstants.ATSecret, result.AccessToken);
            }
        }

        [TestMethod]
        public async Task SuggestedCacheExpiry_ShouldExist_TestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();
                AddMockHandlerAadSuccess(httpManager);

                var app = BuildCCA(httpManager);

                InMemoryTokenCache cache = new InMemoryTokenCache();
                cache.Bind(app.UserTokenCache);

                (app.UserTokenCache as TokenCache).AfterAccess += (args) =>
                {
                    if (args.HasStateChanged)
                    {
                        Assert.IsTrue(args.SuggestedCacheExpiry.HasValue);
                    }
                };

                UserAssertion userAssertion = new UserAssertion(TestConstants.DefaultAccessToken);
                await app.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion).ExecuteAsync().ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task ClaimsChallengeErrorHandling_TestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateInvalidGrantTokenResponseMessage(claims: TestConstants.ClaimsChallenge)
                    });

                var cca = BuildCCA(httpManager);
                UserAssertion userAssertion = new UserAssertion(TestConstants.DefaultAccessToken);

                //Throw exception with claims for OBO:
                var ex = await Assert.ThrowsExceptionAsync<MsalClaimsChallengeException>(async () =>
                {
                    await cca.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion).ExecuteAsync().ConfigureAwait(false);
                }).ConfigureAwait(false);

                Assert.AreEqual(TestConstants.ClaimsChallenge, ex.Claims);
                Assert.IsTrue(ex.Message.Contains(MsalErrorMessage.ClaimsChallenge));

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateInvalidGrantTokenResponseMessage(claims: TestConstants.ClaimsChallenge)
                    });

                //Throw exception with claims without OBO:
                ex = await Assert.ThrowsExceptionAsync<MsalClaimsChallengeException>(async () =>
                {
                    await cca.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync().ConfigureAwait(false);
                }).ConfigureAwait(false);

                Assert.AreEqual(TestConstants.ClaimsChallenge, ex.Claims);
                Assert.IsTrue(ex.Message.Contains(MsalErrorMessage.ClaimsChallenge));
            }
        }

        private ConfidentialClientApplication BuildCCA(IHttpManager httpManager)
        {
            return ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithAuthority(TestConstants.AuthorityCommonTenant)
                    .WithHttpManager(httpManager)
                    .BuildConcrete();
        }

        [TestMethod]
        [DeploymentItem(@"Resources\MultiTenantOBOTokenCache.json")]
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
                    .WithTenantId(tenant1)
                    .ExecuteAsync().ConfigureAwait(false);

                var result2 = await cca.AcquireTokenOnBehalfOf(
                   new[] { "User.Read" },
                   new UserAssertion("jwt"))
                   .WithTenantId(tenant2)
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
            const string tokenCacheFile = "MultiTenantOBOTokenCache.json";

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
            cca.UserTokenCacheInternal.Accessor.AssertItemCount(2, 2, 3, 3, 1);

            return cca;
        }
    }
}
