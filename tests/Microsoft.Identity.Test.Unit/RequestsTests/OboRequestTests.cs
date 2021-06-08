// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.RequestsTests
{
    [TestClass]
    public class OboRequestTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
        }

        private MockHttpMessageHandler AddMockHandlerAadSuccess(MockHttpManager httpManager, string authority)
        {
            var handler = new MockHttpMessageHandler
            {
                ExpectedUrl = authority + "oauth2/v2.0/token",
                ExpectedMethod = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
            };
            httpManager.AddMockHandler(handler);

            return handler;
        }
        
        [TestMethod]
        public async Task AcquireTokenByOboAccessTokenExpiredRefreshTokenAvailableAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                AddMockHandlerAadSuccess(httpManager, TestConstants.AuthorityCommonTenant);

                var cca = ConfidentialClientApplicationBuilder
                                                         .Create(TestConstants.ClientId)
                                                         .WithClientSecret(TestConstants.ClientSecret)
                                                         .WithAuthority(TestConstants.AuthorityCommonTenant)
                                                         .WithHttpManager(httpManager)
                                                         .BuildConcrete();

                UserAssertion userAssertion = new UserAssertion(TestConstants.FormattedAccessToken);
                var result = await cca.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion).ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual("some-access-token", result.AccessToken);

                //Expire access tokens
                TokenCacheHelper.ExpireAccessTokens(cca.UserTokenCacheInternal);

                MockHttpMessageHandler mockTokenRequestHttpHandlerRefresh = AddMockHandlerAadSuccess(httpManager, TestConstants.AuthorityCommonTenant);
                mockTokenRequestHttpHandlerRefresh.ExpectedPostData = new Dictionary<string, string> { { "grant_type", "refresh_token" } };

                result = await cca.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion).ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual("some-access-token", result.AccessToken);
            }
        }

        [TestMethod]
        public async Task AcquireTokenByOboMissMatchUserAssertionsAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                AddMockHandlerAadSuccess(httpManager, TestConstants.AuthorityCommonTenant);

                var cca = ConfidentialClientApplicationBuilder
                                                         .Create(TestConstants.ClientId)
                                                         .WithClientSecret(TestConstants.ClientSecret)
                                                         .WithAuthority(TestConstants.AuthorityCommonTenant)
                                                         .WithHttpManager(httpManager)
                                                         .BuildConcrete();

                UserAssertion userAssertion = new UserAssertion(TestConstants.FormattedAccessToken);
                var result = await cca.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion).ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual("some-access-token", result.AccessToken);

                //Update user assertions
                TokenCacheHelper.UpdateUserAssertions(cca);

                MockHttpMessageHandler mockTokenRequestHttpHandlerRefresh = AddMockHandlerAadSuccess(httpManager, TestConstants.AuthorityCommonTenant);

                //Access and refresh tokens are have a different user assertion so MSAL should perform OBO.
                result = await cca.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion).ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual("some-access-token", result.AccessToken);
                Assert.AreEqual(result.AuthenticationResultMetadata.TokenSource, TokenSource.IdentityProvider);
            }
        }

        [TestMethod]
        public async Task AcquireTokenByOboAccessTokenInCacheTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                AddMockHandlerAadSuccess(httpManager, TestConstants.AuthorityCommonTenant);

                var cca = ConfidentialClientApplicationBuilder
                                                         .Create(TestConstants.ClientId)
                                                         .WithClientSecret(TestConstants.ClientSecret)
                                                         .WithAuthority(TestConstants.AuthorityCommonTenant)
                                                         .WithHttpManager(httpManager)
                                                         .BuildConcrete();

                UserAssertion userAssertion = new UserAssertion(TestConstants.FormattedAccessToken);
                var result = await cca.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion).ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual("some-access-token", result.AccessToken);

                result = await cca.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion).ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(result.AuthenticationResultMetadata.TokenSource, TokenSource.Cache);
                Assert.AreEqual("some-access-token", result.AccessToken);
            }
        }
    }
}
