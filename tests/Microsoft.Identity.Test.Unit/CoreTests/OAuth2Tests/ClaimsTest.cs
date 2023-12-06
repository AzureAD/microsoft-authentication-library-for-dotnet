// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Common.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Test.Common;

namespace Microsoft.Identity.Test.Unit.CoreTests.OAuth2Tests
{
    [TestClass]
    public class ClaimsTest : TestBase
    {
        // https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1639
        [TestMethod]
        public async Task AcquireTokenSilent_BypassesCacheForAccessTokens_IfClaimsUsed_Async()
        {
            using (var harness = CreateTestHarness())
            {
                Trace.WriteLine("1. Setup an app with a token cache with a valid AT");
                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithHttpManager(harness.HttpManager)
                                                                            .BuildConcrete();

                TokenCacheHelper.PopulateCache(app.UserTokenCacheInternal.Accessor, addSecondAt: false);

                Trace.WriteLine("2. Silent Request without claims returns the AT from the cache");
                var account = (await app.GetAccountsAsync().ConfigureAwait(false)).Single();
                var result = await app.AcquireTokenSilent(TestConstants.s_scope, account)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result, "A result is obtained from the cache, i.e. without HTTP calls");

                Trace.WriteLine("3. Silent Request + WithClaims does not return an AT from the cache");
                harness.HttpManager.AddInstanceDiscoveryMockHandler(TestConstants.AuthorityUtidTenant);
                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(TestConstants.AuthorityUtidTenant);

                result = await app.AcquireTokenSilent(TestConstants.s_scope, account)
                    .WithClaims(TestConstants.Claims)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result, "An access token is no longer obtained from the cache. The RT is used to get one from the STS.");
            }
        }

        // https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1639
        [TestMethod]
        public async Task AcquireTokenForClient_BypassesCacheForAccessTokens_IfClaimsUsed_Async()
        {
            // Arrange
            using (MockHttpAndServiceBundle harness = base.CreateTestHarness())
            {
                Trace.WriteLine("1. Setup an app with a token cache with one AT");
                ConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                           .WithAuthority(AzureCloudInstance.AzurePublic, TestConstants.Utid)
                                                           .WithClientSecret(TestConstants.ClientSecret)
                                                           .WithHttpManager(harness.HttpManager)
                                                           .BuildConcrete();

                TokenCacheHelper.PopulateCache(app.AppTokenCacheInternal.Accessor, addSecondAt: false);

                Trace.WriteLine("2. AcquireTokenForClient returns from the cache ");
                AuthenticationResult result = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync()
                    .ConfigureAwait(false);
                Assert.IsNotNull(result, "A result is obtained from the cache, i.e. without HTTP calls");

                harness.HttpManager.AddInstanceDiscoveryMockHandler();
                var refreshHandler = new MockHttpMessageHandler()
                {
                    ExpectedMethod = HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage()
                };
                harness.HttpManager.AddMockHandler(refreshHandler);

                // Act
                Trace.WriteLine("3. AcquireTokenForClient + Claims does not return AT from cache");
                await app.AcquireTokenForClient(TestConstants.s_scope)
                    .WithClaims(TestConstants.Claims)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result, "An access token is no longer obtained from the cache. The RT is used to get one from the STS.");
            }
        }

        // https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1639
        [TestMethod]
        public async Task AcquireTokenSilent_NoCacheBypass_IfClientCapabilitiesAreUsed_Async()
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                Trace.WriteLine("1. Setup an app with a token cache with a valid AT + Client Capabilities");
                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithHttpManager(harness.HttpManager)
                                                                            .WithClientCapabilities(TestConstants.ClientCapabilities)
                                                                            .BuildConcrete();

                TokenCacheHelper.PopulateCache(app.UserTokenCacheInternal.Accessor, addSecondAt: false);

                Trace.WriteLine("2. Silent Request returns the AT from the cache");
                var account = (await app.GetAccountsAsync().ConfigureAwait(false)).Single();
                var result = await app.AcquireTokenSilent(TestConstants.s_scope, account)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result, "A result is obtained from the cache, i.e. without HTTP calls");
            }
        }

        [TestMethod]
        public async Task ClaimsAreSentTo_AuthorizationEndpoint_And_TokenEndpoint_Async()
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                                            .WithHttpManager(harness.HttpManager)
                                                                            .BuildConcrete();

                var mockUi =
                     app.ServiceBundle.ConfigureMockWebUI(
                        AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));

                mockUi.QueryParamsToValidate = new Dictionary<string, string> { { OAuth2Parameter.Claims, TestConstants.Claims } };

                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(
                    TestConstants.AuthorityCommonTenant,
                    bodyParameters: mockUi.QueryParamsToValidate);

                AuthenticationResult result = await app
                    .AcquireTokenInteractive(TestConstants.s_scope)
                    .WithClaims(TestConstants.Claims)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(result.Account);
            }
        }

        [TestMethod]
        public async Task ClientCapabilities_AreSentTo_AuthorizationEndpoint_And_TokenEndpoint_Async()
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                var app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                              .WithHttpManager(harness.HttpManager)
                              .WithClientCapabilities(TestConstants.ClientCapabilities)
                              .BuildConcrete();

                var mockUi =
                     app.ServiceBundle.ConfigureMockWebUI(
                        AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));

                mockUi.QueryParamsToValidate = new Dictionary<string, string> { {
                        OAuth2Parameter.Claims,
                        TestConstants.ClientCapabilitiesJson } };

                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(
                    TestConstants.AuthorityCommonTenant,
                    bodyParameters: mockUi.QueryParamsToValidate);

                AuthenticationResult result = await app
                    .AcquireTokenInteractive(TestConstants.s_scope)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(result.Account);
            }
        }

        [TestMethod]
        public async Task ClaimsAndClientCapabilities_AreMerged_And_AreSentTo_AuthorizationEndpoint_And_TokenEndpoint_Async()
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                var app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                .WithHttpManager(harness.HttpManager)
                                .WithClientCapabilities(TestConstants.ClientCapabilities)
                                .BuildConcrete();

                var mockUi =
                     app.ServiceBundle.ConfigureMockWebUI(
                        AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));

                mockUi.QueryParamsToValidate = new Dictionary<string, string> {
                    { OAuth2Parameter.Claims,
                        TestConstants.ClientCapabilitiesAndClaimsJson } };

                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(
                    TestConstants.AuthorityCommonTenant,
                    bodyParameters: mockUi.QueryParamsToValidate);

                AuthenticationResult result = await app
                    .AcquireTokenInteractive(TestConstants.s_scope)
                    .WithClaims(TestConstants.Claims)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(result.Account);
            }
        }

        [TestMethod]
        public async Task Claims_Fail_WhenClaimsIsNotJson_Async()
        {
            var app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                            .WithClientCapabilities(TestConstants.ClientCapabilities)
                            .BuildConcrete();

            var ex = await AssertException.TaskThrowsAsync<MsalClientException>(
                () => app
                    .AcquireTokenInteractive(TestConstants.s_scope)
                    .WithClaims("claims_that_are_not_json")
                    .ExecuteAsync(CancellationToken.None))
                .ConfigureAwait(false);

            Assert.AreEqual(MsalError.InvalidJsonClaimsFormat, ex.ErrorCode);
        }

        [DataTestMethod]
        [DynamicData(nameof(TestData.GetClaimsAndCapabilities), typeof(TestData), DynamicDataSourceType.Method)]
        public void ClaimsMerge_Test(string claims, string[] capabilities, string expectedMergedJson)
        {
            var mergedJson = ClaimsHelper.GetMergedClaimsAndClientCapabilities(claims, capabilities);
            Assert.AreEqual(expectedMergedJson, mergedJson);
        }
    }
}
