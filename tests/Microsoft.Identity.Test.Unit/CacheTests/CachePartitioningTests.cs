// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CacheTests
{
    [TestClass]
    public class CachePartitioningTests : TestBase
    {
        [TestMethod]
        public async Task CCAFlows_CachePartition_TestAsync()
        {
            using (var harness = base.CreateTestHarness())
            {
                var httpManager = harness.HttpManager;
                httpManager.AddInstanceDiscoveryMockHandler();

                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithRedirectUri(TestConstants.RedirectUri)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithHttpManager(httpManager)
                                                              // this will fail if cache partitioning rules are broken (but not when cache serialization is also used)
                                                              .WithCachePartitioningAsserts(harness.ServiceBundle.PlatformProxy)
                                                              .BuildConcrete();

                await RunClientCreds_Async(httpManager, app).ConfigureAwait(false);

                await RunObo_Async(httpManager, app).ConfigureAwait(false);

                await RunAuthCode_Async(httpManager, app).ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task PCAFlows_CachePartition_TestAsync()
        {
            using (var harness = base.CreateTestHarness())
            {
                var httpManager = harness.HttpManager;
                httpManager.AddInstanceDiscoveryMockHandler();

                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithRedirectUri(TestConstants.RedirectUri)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithHttpManager(httpManager)
                                                              // this will fail if cache partitioning rules are broken (but not when cache serialization is also used)                                                              
                                                              .WithCachePartitioningAsserts(harness.ServiceBundle.PlatformProxy)
                                                              .BuildConcrete();

                await RunClientCreds_Async(httpManager, app).ConfigureAwait(false);

                await RunObo_Async(httpManager, app).ConfigureAwait(false);

                await RunAuthCode_Async(httpManager, app).ConfigureAwait(false);
            }
        }

        private static async Task RunAuthCode_Async(MockHttpManager httpManager, ConfidentialClientApplication app)
        {
            httpManager.AddSuccessTokenResponseMockHandlerForPost();
            var result = await app
               .AcquireTokenByAuthorizationCode(TestConstants.s_scope, "some-code")
               .ExecuteAsync(CancellationToken.None)
               .ConfigureAwait(false);
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

            var acc = await app.GetAccountAsync(result.Account.HomeAccountId.Identifier).ConfigureAwait(false);
            result = await app.AcquireTokenSilent(TestConstants.s_scope, acc).ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
        }

        private static async Task RunObo_Async(MockHttpManager httpManager, ConfidentialClientApplication app)
        {
            httpManager.AddSuccessTokenResponseMockHandlerForPost();
            UserAssertion userAssertion = new UserAssertion(TestConstants.DefaultAccessToken);
            var result = await app.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion).ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

            // get AT from cache
            result = await app.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion).ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);

            // get AT via refresh_token flow
            TokenCacheHelper.ExpireAccessTokens(app.UserTokenCacheInternal);
            var handler = httpManager.AddSuccessTokenResponseMockHandlerForPost();
            handler.ExpectedPostData = new Dictionary<string, string> { { "grant_type", "refresh_token" } };
            result = await app.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion).ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
        }

        private static async Task RunClientCreds_Async(MockHttpManager httpManager, ConfidentialClientApplication app)
        {
            httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

            var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray()).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(TestConstants.s_scope.AsSingleString(), result.Scopes.AsSingleString());
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

            result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray()).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
        }
    }
}
