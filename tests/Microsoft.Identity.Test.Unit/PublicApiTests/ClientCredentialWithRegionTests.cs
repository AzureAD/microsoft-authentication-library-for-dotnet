﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Region;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Unit.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit
{
    [TestClass]
    [DeploymentItem(@"Resources\CustomInstanceMetadata.json")]
    public class ConfidentialClientWithRegionTests : TestBase
    {
        public const string EastUsRegion = "eastus";

        [TestMethod]
        // regression for #2837
        public async Task AuthorityOverrideAndRegionalAsync()
        {
            using (new EnvVariableContext())
            {
                const string region = "eastus";
                Environment.SetEnvironmentVariable("REGION_NAME", region);

                var app = ConfidentialClientApplicationBuilder
                                 .Create(TestConstants.ClientId)
                                 .WithAzureRegion(ConfidentialClientApplication.AttemptRegionDiscovery)
                                 .WithClientSecret(TestConstants.ClientSecret)
                                 .Build();

#pragma warning disable CS0618 // Type or member is obsolete
                var ex = await AssertException.TaskThrowsAsync<MsalClientException>(() =>
                    app
                    .AcquireTokenForClient(TestConstants.s_scope)
                    .WithAuthority("https://login.microsoft.com/17b189bc-2b81-4ec5-aa51-3e628cbc931b")
#pragma warning restore CS0618 // Type or member is obsolete
                        .ExecuteAsync())
                    .ConfigureAwait(false);

                Assert.AreEqual(MsalError.RegionalAndAuthorityOverride, ex.ErrorCode);
            }
        }

        [TestMethod]
        // regression for #2837
        public async Task TenantIdOverrideAndRegionalAsync()
        {
            // Arrange

            using (new EnvVariableContext())
            using (var harness = base.CreateTestHarness())
            {
                Environment.SetEnvironmentVariable("REGION_NAME", EastUsRegion);

                var tokenHttpCallHandler = new MockHttpMessageHandler()
                {
                    // Asserts
                    ExpectedUrl = $"https://eastus.login.microsoft.com/17b189bc-2b81-4ec5-aa51-3e628cbc931b/oauth2/v2.0/token",
                    ExpectedMethod = HttpMethod.Post,

                    ResponseMessage = CreateResponse(true)
                };
                harness.HttpManager.AddMockHandler(tokenHttpCallHandler);

                var app = ConfidentialClientApplicationBuilder
                                 .Create(TestConstants.ClientId)
                                 .WithAuthority($"https://login.microsoftonline.com/common", true)
                                 .WithHttpManager(harness.HttpManager)
                                 .WithAzureRegion(ConfidentialClientApplication.AttemptRegionDiscovery)
                                 .WithClientSecret(TestConstants.ClientSecret)
                                 .Build();

                // Act
                var result = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .WithTenantId("17b189bc-2b81-4ec5-aa51-3e628cbc931b")
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(EastUsRegion, result.AuthenticationResultMetadata.RegionDetails.RegionUsed);
                Assert.AreEqual(RegionOutcome.AutodetectSuccess, result.AuthenticationResultMetadata.RegionDetails.RegionOutcome);
            }
        }

        [TestMethod]
        [Description("Test for regional auth with successful instance discovery.")]
        public async Task FetchRegionFromLocalImdsCallAsync()
        {
            const string region = "centralus";

            using (new EnvVariableContext())
            using (var harness = base.CreateTestHarness())
            {
                harness.HttpManager.AddRegionDiscoveryMockHandler(TestConstants.Region);

                IConfidentialClientApplication app = CreateCca(
                    harness.HttpManager,
                    ConfidentialClientApplication.AttemptRegionDiscovery);
                harness.HttpManager.AddMockHandler(CreateTokenResponseHttpHandler(true));

                AuthenticationResult result = await app
                    .AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.AreEqual(TestConstants.Region, result.ApiEvent.RegionUsed);
                Assert.AreEqual(TestConstants.Region, result.ApiEvent.AutoDetectedRegion);
                Assert.AreEqual(RegionAutodetectionSource.Imds, result.ApiEvent.RegionAutodetectionSource);
                Assert.AreEqual(RegionOutcome.AutodetectSuccess, result.ApiEvent.RegionOutcome);
                Assert.AreEqual(
                    "https://centralus.login.microsoft.com/common/oauth2/v2.0/token",
                    result.AuthenticationResultMetadata.TokenEndpoint);
                Assert.AreEqual(region, result.AuthenticationResultMetadata.RegionDetails.RegionUsed);
                Assert.AreEqual(RegionOutcome.AutodetectSuccess, result.AuthenticationResultMetadata.RegionDetails.RegionOutcome);
                Assert.IsNull(result.AuthenticationResultMetadata.RegionDetails.AutoDetectionError);

                // try again, result will be from cache
                result = await app
                    .AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.AreEqual(TestConstants.Region, result.ApiEvent.RegionUsed);
                Assert.AreEqual(TestConstants.Region, result.ApiEvent.AutoDetectedRegion);
                Assert.AreEqual(RegionAutodetectionSource.Cache, result.ApiEvent.RegionAutodetectionSource);
                Assert.AreEqual(RegionOutcome.AutodetectSuccess, result.ApiEvent.RegionOutcome);
                Assert.IsTrue(result.AuthenticationResultMetadata.TokenSource == TokenSource.Cache);
                Assert.AreEqual(region, result.AuthenticationResultMetadata.RegionDetails.RegionUsed);
                Assert.AreEqual(RegionOutcome.AutodetectSuccess, result.AuthenticationResultMetadata.RegionDetails.RegionOutcome);
                Assert.IsNull(result.AuthenticationResultMetadata.RegionDetails.AutoDetectionError);

                // try again, with force refresh, region should be from cache
                harness.HttpManager.AddMockHandler(CreateTokenResponseHttpHandler(true));
                result = await app
                  .AcquireTokenForClient(TestConstants.s_scope)
                  .WithForceRefresh(true)
                  .ExecuteAsync(CancellationToken.None)
                  .ConfigureAwait(false);

                Assert.AreEqual(TestConstants.Region, result.ApiEvent.RegionUsed);
                Assert.AreEqual(TestConstants.Region, result.ApiEvent.AutoDetectedRegion);
                Assert.AreEqual(RegionAutodetectionSource.Cache, result.ApiEvent.RegionAutodetectionSource);
                Assert.AreEqual(RegionOutcome.AutodetectSuccess, result.ApiEvent.RegionOutcome);
                Assert.IsTrue(result.AuthenticationResultMetadata.TokenSource == TokenSource.IdentityProvider);
                Assert.AreEqual(region, result.AuthenticationResultMetadata.RegionDetails.RegionUsed);
                Assert.AreEqual(RegionOutcome.AutodetectSuccess, result.AuthenticationResultMetadata.RegionDetails.RegionOutcome);
                Assert.IsNull(result.AuthenticationResultMetadata.RegionDetails.AutoDetectionError);

                // try again, create a new app, result should still be from cache 
                IConfidentialClientApplication app2 = CreateCca(
                    harness.HttpManager,
                    ConfidentialClientApplication.AttemptRegionDiscovery);

                harness.HttpManager.AddMockHandler(CreateTokenResponseHttpHandler(true));
                result = await app2
                  .AcquireTokenForClient(TestConstants.s_scope)
                  .ExecuteAsync(CancellationToken.None)
                  .ConfigureAwait(false);

                Assert.AreEqual(TestConstants.Region, result.ApiEvent.RegionUsed);
                Assert.AreEqual(TestConstants.Region, result.ApiEvent.AutoDetectedRegion);
                Assert.AreEqual(RegionAutodetectionSource.Cache, result.ApiEvent.RegionAutodetectionSource);
                Assert.AreEqual(RegionOutcome.AutodetectSuccess, result.ApiEvent.RegionOutcome);
                Assert.IsTrue(result.AuthenticationResultMetadata.TokenSource == TokenSource.IdentityProvider);
                Assert.AreEqual(region, result.AuthenticationResultMetadata.RegionDetails.RegionUsed);
                Assert.AreEqual(RegionOutcome.AutodetectSuccess, result.AuthenticationResultMetadata.RegionDetails.RegionOutcome);
                Assert.IsNull(result.AuthenticationResultMetadata.RegionDetails.AutoDetectionError);
            }
        }

        [TestMethod]
        [Description("Tokens between regional and non-regional are interchangeable.")]
        public async Task TokensAreInterchangable_Regional_To_NonRegional_Async()
        {
            using (var harness = base.CreateTestHarness())
            {
                var httpManager = harness.HttpManager;
                httpManager.AddRegionDiscoveryMockHandler(TestConstants.Region);
                httpManager.AddMockHandler(CreateTokenResponseHttpHandler(true));

                IConfidentialClientApplication appWithRegion = CreateCca(
                    httpManager,
                    ConfidentialClientApplication.AttemptRegionDiscovery);
                InMemoryTokenCache memoryTokenCache = new InMemoryTokenCache();
                memoryTokenCache.Bind(appWithRegion.AppTokenCache);

                IConfidentialClientApplication appWithoutRegion = CreateCca(
                    httpManager,
                    null);
                memoryTokenCache.Bind(appWithoutRegion.AppTokenCache);

                AuthenticationResult result = await appWithRegion
                    .AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.AreEqual(TestConstants.Region, result.ApiEvent.RegionUsed);
                Assert.AreEqual(TestConstants.Region, result.ApiEvent.AutoDetectedRegion);
                Assert.AreEqual(RegionAutodetectionSource.Imds, result.ApiEvent.RegionAutodetectionSource);
                Assert.AreEqual(RegionOutcome.AutodetectSuccess, result.ApiEvent.RegionOutcome);
                Assert.AreEqual(TestConstants.Region, result.AuthenticationResultMetadata.RegionDetails.RegionUsed);
                Assert.AreEqual(RegionOutcome.AutodetectSuccess, result.AuthenticationResultMetadata.RegionDetails.RegionOutcome);
                Assert.IsNull(result.AuthenticationResultMetadata.RegionDetails.AutoDetectionError);

                // when switching to non-region, token is found in the cache
                result = await appWithoutRegion
                    .AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.AreEqual(null, result.ApiEvent.RegionUsed);
                Assert.IsNull(result.ApiEvent.AutoDetectedRegion);
                Assert.AreEqual(RegionAutodetectionSource.None, result.ApiEvent.RegionAutodetectionSource);
                Assert.AreEqual(RegionOutcome.None, result.ApiEvent.RegionOutcome);
                Assert.IsTrue(result.AuthenticationResultMetadata.TokenSource == TokenSource.Cache);
                Assert.IsNull(result.AuthenticationResultMetadata.RegionDetails.RegionUsed);
                Assert.AreEqual(RegionOutcome.None, result.AuthenticationResultMetadata.RegionDetails.RegionOutcome);
                Assert.IsNull(result.AuthenticationResultMetadata.RegionDetails.AutoDetectionError);
            }
        }

        [TestMethod]
        [Description("Test when region is received from environment variable")]
        public async Task FetchRegionFromEnvironmentAsync()
        {
            using (new EnvVariableContext())
            using (var harness = base.CreateTestHarness())
            {
                var httpManager = harness.HttpManager;
                Environment.SetEnvironmentVariable("REGION_NAME", TestConstants.Region);
                IConfidentialClientApplication app = CreateCca(
                    httpManager,
                    ConfidentialClientApplication.AttemptRegionDiscovery);

                httpManager.AddMockHandler(CreateTokenResponseHttpHandler(true));

                AuthenticationResult result = await app
                    .AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.AreEqual(TestConstants.Region, result.ApiEvent.RegionUsed);
                Assert.AreEqual(TestConstants.Region, result.ApiEvent.AutoDetectedRegion);
                Assert.AreEqual(RegionAutodetectionSource.EnvVariable, result.ApiEvent.RegionAutodetectionSource);
                Assert.AreEqual(RegionOutcome.AutodetectSuccess, result.ApiEvent.RegionOutcome);
                Assert.AreEqual(TestConstants.Region, result.AuthenticationResultMetadata.RegionDetails.RegionUsed);
                Assert.AreEqual(RegionOutcome.AutodetectSuccess, result.AuthenticationResultMetadata.RegionDetails.RegionOutcome);
                Assert.IsNull(result.AuthenticationResultMetadata.RegionDetails.AutoDetectionError);
                Assert.IsNotNull(result.AccessToken);

            }
        }

        [TestMethod]
        [Description("Test when the region could not be fetched -> fallback to global.")]
        public async Task RegionFallbackToGlobalAsync()
        {
            using (var harness = base.CreateTestHarness())
            {
                harness.ServiceBundle.Config.RetryPolicyFactory = new TestRetryPolicyFactory();
                var httpManager = harness.HttpManager;

                // for simplicity, return 404 so retry is not triggered
                httpManager.AddRegionDiscoveryMockHandlerWithError(HttpStatusCode.NotFound);
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandler(CreateTokenResponseHttpHandler(false));

                IConfidentialClientApplication app = CreateCca(
                     httpManager,
                     ConfidentialClientApplication.AttemptRegionDiscovery);

                AuthenticationResult result = await app
                    .AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);

                Assert.AreEqual(null, result.ApiEvent.RegionUsed);
                Assert.IsNull(result.ApiEvent.AutoDetectedRegion);
                Assert.AreEqual(RegionAutodetectionSource.FailedAutoDiscovery, result.ApiEvent.RegionAutodetectionSource);
                Assert.AreEqual(RegionOutcome.FallbackToGlobal, result.ApiEvent.RegionOutcome);
                Assert.IsNull(result.AuthenticationResultMetadata.RegionDetails.RegionUsed);
                Assert.AreEqual(RegionOutcome.FallbackToGlobal, result.AuthenticationResultMetadata.RegionDetails.RegionOutcome);
                Assert.IsTrue(result.AuthenticationResultMetadata.RegionDetails.AutoDetectionError
                    .Contains(TestConstants.RegionDiscoveryIMDSCallFailedMessage));

            }
        }

        [TestMethod]
        public async Task MsalForceRegionIsSet_RegionIsUsed()
        {
            using (new EnvVariableContext())
            using (var harness = base.CreateTestHarness())
            {
                var httpManager = harness.HttpManager;

                Environment.SetEnvironmentVariable(
                    ConfidentialClientApplicationBuilder.ForceRegionEnvVariable, TestConstants.Region);

                httpManager.AddRegionDiscoveryMockHandler(TestConstants.Region);
                httpManager.AddMockHandler(CreateTokenResponseHttpHandler(expectRegional: true));

                var cca = ConfidentialClientApplicationBuilder
                                 .Create(TestConstants.ClientId)
                                 .WithHttpManager(httpManager)
                                 .WithClientSecret(TestConstants.ClientSecret)
                                 .Build();

                AuthenticationResult result = await cca
                        .AcquireTokenForClient(TestConstants.s_scope)
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                Assert.AreEqual(TestConstants.Region, result.ApiEvent.RegionUsed);
                Assert.AreEqual(RegionAutodetectionSource.Imds, result.ApiEvent.RegionAutodetectionSource);

                Assert.AreEqual(TestConstants.Region, result.AuthenticationResultMetadata.RegionDetails.RegionUsed);
                Assert.AreEqual(RegionOutcome.UserProvidedValid, result.AuthenticationResultMetadata.RegionDetails.RegionOutcome);
                Assert.IsNull(result.AuthenticationResultMetadata.RegionDetails.AutoDetectionError);
            }
        }

        [TestMethod]
        public async Task MsalDisableRegionEnvVariable_API()
        {
            using (new EnvVariableContext())
            using (var harness = base.CreateTestHarness())
            {
                Environment.SetEnvironmentVariable(
                    ConfidentialClientApplicationBuilder.DisableRegionEnvVariable, "1");

                var httpManager = harness.HttpManager;

                var cca = ConfidentialClientApplicationBuilder
                                .Create(TestConstants.ClientId)
                                .WithHttpManager(httpManager)
                                .WithAzureRegion(TestConstants.Region)
                                .WithClientSecret(TestConstants.ClientSecret)
                                .Build();

                await ExpectNoRegionAsync(httpManager, cca).ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task MsalDisableRegionEnvVariable_OtherEnv()
        {
            using (new EnvVariableContext())
            using (var harness = base.CreateTestHarness())
            {
                Environment.SetEnvironmentVariable(
                    ConfidentialClientApplicationBuilder.ForceRegionEnvVariable, "someregion");
                Environment.SetEnvironmentVariable(
                    ConfidentialClientApplicationBuilder.DisableRegionEnvVariable, "1");

                var httpManager = harness.HttpManager;

                var cca = ConfidentialClientApplicationBuilder
                                .Create(TestConstants.ClientId)
                                .WithHttpManager(httpManager)
                                .WithClientSecret(TestConstants.ClientSecret)
                                .Build();

                await ExpectNoRegionAsync(httpManager, cca).ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task MsalDisableRegionEnvVariable_ApiAndOtherEnv()
        {
            using (new EnvVariableContext())
            using (var harness = base.CreateTestHarness())
            {
                Environment.SetEnvironmentVariable(
                    ConfidentialClientApplicationBuilder.DisableRegionEnvVariable, "1");

                Environment.SetEnvironmentVariable(
                    ConfidentialClientApplicationBuilder.ForceRegionEnvVariable, "someregion");

                var httpManager = harness.HttpManager;

                var cca = ConfidentialClientApplicationBuilder
                                .Create(TestConstants.ClientId)
                                .WithHttpManager(httpManager)
                                .WithAzureRegion(TestConstants.Region)
                                .WithClientSecret(TestConstants.ClientSecret)
                                .Build();

                await ExpectNoRegionAsync(httpManager, cca).ConfigureAwait(false);
            }
        }

        private static async Task ExpectNoRegionAsync(MockHttpManager httpManager, IConfidentialClientApplication cca)
        {
            httpManager.AddInstanceDiscoveryMockHandler();
            httpManager.AddMockHandler(CreateTokenResponseHttpHandler(expectRegional: false));

            AuthenticationResult result = await cca
                    .AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

            Assert.IsNull(result.ApiEvent.RegionUsed);
        }

        [TestMethod]
        public async Task MsalForceRegionIsSet_WithRegionIsSet_WithRegionWins()
        {
            using (new EnvVariableContext())
            using (var harness = base.CreateTestHarness())
            {
                var httpManager = harness.HttpManager;

                // this will be ignored, in favor of TestConstants.Region
                Environment.SetEnvironmentVariable(
                    ConfidentialClientApplicationBuilder.ForceRegionEnvVariable, "someOtherRegion");

                httpManager.AddRegionDiscoveryMockHandler(TestConstants.Region);
                httpManager.AddMockHandler(CreateTokenResponseHttpHandler(expectRegional: true));

                var cca = ConfidentialClientApplicationBuilder
                                 .Create(TestConstants.ClientId)
                                 .WithHttpManager(httpManager)
                                 .WithAzureRegion(TestConstants.Region)
                                 .WithClientSecret(TestConstants.ClientSecret)
                                 .Build();

                AuthenticationResult result = await cca
                        .AcquireTokenForClient(TestConstants.s_scope)
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                Assert.AreEqual(TestConstants.Region, result.ApiEvent.RegionUsed);
                Assert.AreEqual(RegionAutodetectionSource.Imds, result.ApiEvent.RegionAutodetectionSource);

                Assert.AreEqual(TestConstants.Region, result.AuthenticationResultMetadata.RegionDetails.RegionUsed);
                Assert.AreEqual(RegionOutcome.UserProvidedValid, result.AuthenticationResultMetadata.RegionDetails.RegionOutcome);
                Assert.IsNull(result.AuthenticationResultMetadata.RegionDetails.AutoDetectionError);
            }
        }

        [TestMethod]
        public async Task MsalForceRegionIsSet_WithRegionIsSetToOptOut_NoRegionIsUsed()
        {
            using (new EnvVariableContext())
            using (var harness = base.CreateTestHarness())
            {
                var httpManager = harness.HttpManager;

                Environment.SetEnvironmentVariable(
                    ConfidentialClientApplicationBuilder.ForceRegionEnvVariable, TestConstants.Region);

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandler(CreateTokenResponseHttpHandler(expectRegional: false));

                var cca = ConfidentialClientApplicationBuilder
                                 .Create(TestConstants.ClientId)
                                 .WithHttpManager(httpManager)
                                 .WithAzureRegion(ConfidentialClientApplicationBuilder.DisableForceRegion)
                                 .WithClientSecret(TestConstants.ClientSecret)
                                 .Build();

                AuthenticationResult result = await cca
                        .AcquireTokenForClient(TestConstants.s_scope)
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                Assert.IsNull(result.ApiEvent.RegionUsed);
            }
        }
        [TestMethod]
        public void WithAzureRegionThrowsOnNullArg()
        {
            AssertException.Throws<ArgumentNullException>(
                () => ConfidentialClientApplicationBuilder
                             .Create(TestConstants.ClientId)
                             .WithAzureRegion(null)
                             .WithClientSecret(TestConstants.ClientSecret)
                             .Build());

            AssertException.Throws<ArgumentNullException>(
               () => ConfidentialClientApplicationBuilder
                            .Create(TestConstants.ClientId)
                            .WithAzureRegion(string.Empty)
                            .WithClientSecret(TestConstants.ClientSecret)
                            .Build());

        }

        [TestMethod]
        // regression: https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2686
        public async Task OtherCloudWithAuthorityValidationAsync()
        {
            const string imdsError = "IMDS call failed with exception";
            const string autoDiscoveryError = "Auto-discovery failed in the past. Not trying again. IMDS call failed";

            using (var harness = base.CreateTestHarness())
            {
                harness.ServiceBundle.Config.RetryPolicyFactory = new TestRetryPolicyFactory();
                var httpManager = harness.HttpManager;

                // for simplicity, return 404 so retry is not triggered
                httpManager.AddRegionDiscoveryMockHandlerWithError(HttpStatusCode.NotFound);

                var discoveryHandler = MockHelpers.CreateInstanceDiscoveryMockHandler(
                     "https://login.microsoftonline.com/common/discovery/instance",
                     TestConstants.DiscoveryJsonResponse);

                var tokenHttpCallHandler = new MockHttpMessageHandler()
                {
                    ExpectedUrl = "https://eastus.login.windows-ppe.org/17b189bc-2b81-4ec5-aa51-3e628cbc931b/oauth2/v2.0/token",
                    ExpectedMethod = HttpMethod.Post,
                    ResponseMessage = CreateResponse(true)
                };

                httpManager.AddMockHandler(discoveryHandler);
                httpManager.AddMockHandler(tokenHttpCallHandler);

                var app = ConfidentialClientApplicationBuilder
                                 .Create(TestConstants.ClientId)
                                 .WithAuthority("https://" + TestConstants.PpeOrgEnvironment + "/common", true) //login.windows-ppe.org is not known to MSAL
                                 .WithHttpManager(httpManager)
                                 .WithAzureRegion(EastUsRegion)
                                 .WithClientSecret(TestConstants.ClientSecret)
                                 .Build();

#pragma warning disable CS0618 // Type or member is obsolete
                AuthenticationResult result = await app
                    .AcquireTokenForClient(TestConstants.s_scope)
                    .WithTenantId("17b189bc-2b81-4ec5-aa51-3e628cbc931b")
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual(EastUsRegion, result.ApiEvent.RegionUsed);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(
                    "https://login.microsoftonline.com/common/discovery/instance?api-version=1.1&authorization_endpoint=https%3A%2F%2Flogin.windows-ppe.org%2F17b189bc-2b81-4ec5-aa51-3e628cbc931b%2Foauth2%2Fv2.0%2Fauthorize",
                    discoveryHandler.ActualRequestMessage.RequestUri.AbsoluteUri,
                    "Authority validation is made on https://login.microsoftonline.com/ and it validates the auth_endpoint of the non-regional authority");
                Assert.AreEqual(EastUsRegion, result.AuthenticationResultMetadata.RegionDetails.RegionUsed);
                Assert.AreEqual(RegionOutcome.UserProvidedAutodetectionFailed, result.AuthenticationResultMetadata.RegionDetails.RegionOutcome);
                Assert.IsTrue(result.AuthenticationResultMetadata.RegionDetails.AutoDetectionError.Contains(imdsError));

                result = await app
                   .AcquireTokenForClient(TestConstants.s_scope)
                   .WithTenantId("17b189bc-2b81-4ec5-aa51-3e628cbc931b")
                   .ExecuteAsync()
                   .ConfigureAwait(false);
#pragma warning restore CS0618 // Type or member is obsolete

                Assert.AreEqual(EastUsRegion, result.ApiEvent.RegionUsed);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(EastUsRegion, result.AuthenticationResultMetadata.RegionDetails.RegionUsed);
                Assert.AreEqual(RegionOutcome.UserProvidedAutodetectionFailed, result.AuthenticationResultMetadata.RegionDetails.RegionOutcome);
                Assert.IsTrue(result.AuthenticationResultMetadata.RegionDetails.AutoDetectionError.Contains(autoDiscoveryError));

            }
        }

        [DataTestMethod]
        [DataRow(true, true)]
        [DataRow(true, false)]
        [DataRow(false, true)]
        [DataRow(false, false)]
        // regression: https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2686
        public async Task OtherCloud_WithValidation_Async(bool validateAuthority, bool authorityIsValid)
        {

            using (new EnvVariableContext())
            using (var harness = base.CreateTestHarness())
            {
                var httpManager = harness.HttpManager;
                Environment.SetEnvironmentVariable("REGION_NAME", "eastus");

                MockHttpMessageHandler discoveryHandler;
                if (authorityIsValid)
                {
                    discoveryHandler = MockHelpers.CreateInstanceDiscoveryMockHandler(
                         "https://login.microsoftonline.com/common/discovery/instance",
                         TestConstants.DiscoveryJsonResponse);
                }
                else
                {
                    discoveryHandler = new MockHttpMessageHandler()
                    {
                        ExpectedUrl = "https://login.microsoftonline.com/common/discovery/instance",
                        ExpectedMethod = HttpMethod.Get,
                        ResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
                        {
                            Content = new StringContent(TestConstants.DiscoveryFailedResponse)
                        }
                    };
                }
                var tokenHttpCallHandler = new MockHttpMessageHandler()
                {
                    ExpectedUrl = "https://eastus.login.windows-ppe.org/17b189bc-2b81-4ec5-aa51-3e628cbc931b/oauth2/v2.0/token",//login.windows-ppe.org is not known to MSAL or AAD
                    ExpectedMethod = HttpMethod.Post,
                    ResponseMessage = CreateResponse(true)
                };

                if (validateAuthority)
                {
                    harness.HttpManager.AddMockHandler(discoveryHandler);
                }

                if (authorityIsValid || !validateAuthority)
                {
                    harness.HttpManager.AddMockHandler(tokenHttpCallHandler);
                }

                var app = ConfidentialClientApplicationBuilder
                                 .Create(TestConstants.ClientId)
                                 .WithAuthority("https://" + TestConstants.PpeOrgEnvironment + "/common", validateAuthority)//login.windows-ppe.org is not known to MSAL or AAD
                                 .WithHttpManager(harness.HttpManager)
                                 .WithAzureRegion(EastUsRegion)
                                 .WithClientSecret(TestConstants.ClientSecret)
                                 .Build();

                if (!authorityIsValid && validateAuthority)
                {
                    var ex = await AssertException.TaskThrowsAsync<MsalServiceException>(() => app
                       .AcquireTokenForClient(TestConstants.s_scope)
                       .WithTenantId("17b189bc-2b81-4ec5-aa51-3e628cbc931b")
                       .ExecuteAsync()).ConfigureAwait(false);

                    Assert.AreEqual(MsalError.InvalidInstance, ex.ErrorCode);
                    var qp = CoreHelpers.ParseKeyValueList(discoveryHandler.ActualRequestMessage.RequestUri.Query.Substring(1), '&', true, null);
                    Assert.AreEqual("https://login.windows-ppe.org/17b189bc-2b81-4ec5-aa51-3e628cbc931b/oauth2/v2.0/authorize", qp["authorization_endpoint"]);//login.windows-ppe.org is not known to MSAL or AAD
                }
                else
                {
                    AuthenticationResult result = await app
                        .AcquireTokenForClient(TestConstants.s_scope)
                        .WithTenantId("17b189bc-2b81-4ec5-aa51-3e628cbc931b").ExecuteAsync()
                        .ConfigureAwait(false);

                    Assert.AreEqual(EastUsRegion, result.ApiEvent.RegionUsed);
                    Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                    Assert.AreEqual(EastUsRegion, result.AuthenticationResultMetadata.RegionDetails.RegionUsed);
                    Assert.AreEqual(RegionOutcome.UserProvidedValid, result.AuthenticationResultMetadata.RegionDetails.RegionOutcome);
                    Assert.AreEqual(null, result.AuthenticationResultMetadata.RegionDetails.AutoDetectionError);

                    if (validateAuthority)
                    {
                        Assert.AreEqual(
                            "https://login.microsoftonline.com/common/discovery/instance?api-version=1.1&authorization_endpoint=https%3A%2F%2Flogin.windows-ppe.org%2F17b189bc-2b81-4ec5-aa51-3e628cbc931b%2Foauth2%2Fv2.0%2Fauthorize",//login.windows-ppe.org is not known to MSAL or AAD
                            discoveryHandler.ActualRequestMessage.RequestUri.AbsoluteUri,
                            "Authority validation is made on https://login.microsoftonline.com/ and it validates the auth_endpoint of the non-regional authority");
                    }

                    result = await app
                       .AcquireTokenForClient(TestConstants.s_scope)
                       .WithTenantId("17b189bc-2b81-4ec5-aa51-3e628cbc931b")
                       .ExecuteAsync()
                       .ConfigureAwait(false);

                    Assert.AreEqual(EastUsRegion, result.ApiEvent.RegionUsed);
                    Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                    Assert.AreEqual(EastUsRegion, result.AuthenticationResultMetadata.RegionDetails.RegionUsed);
                    Assert.AreEqual(null, result.AuthenticationResultMetadata.RegionDetails.AutoDetectionError);
                }
            }

        }

        [DataTestMethod]
        [DataRow("login.partner.microsoftonline.cn", "login.partner.microsoftonline.cn")]
        [DataRow("login.chinacloudapi.cn", "login.partner.microsoftonline.cn")]
        [DataRow("login.microsoftonline.us", "login.microsoftonline.us")]
        [DataRow("login.usgovcloudapi.net", "login.microsoftonline.us")]
        [DataRow("login-us.microsoftonline.com", "login-us.microsoftonline.com")]
        [DataRow("login.windows.net", "login.microsoft.com")]
        [DataRow("login.microsoft.com", "login.microsoft.com")]
        [DataRow("sts.windows.net", "login.microsoft.com")]
        [DataRow("login.microsoftonline.com", "login.microsoft.com")]
        public async Task PublicAndSovereignCloud_UsesPreferredNetwork_AndNoDiscovery_Async(string inputEnv, string expectedEnv)
        {
            using (new EnvVariableContext())
            using (var harness = base.CreateTestHarness())
            {
                Environment.SetEnvironmentVariable("REGION_NAME", EastUsRegion);

                var tokenHttpCallHandler = new MockHttpMessageHandler()
                {
                    ExpectedUrl = $"https://{EastUsRegion}.{expectedEnv}/17b189bc-2b81-4ec5-aa51-3e628cbc931b/oauth2/v2.0/token",
                    ExpectedMethod = HttpMethod.Post,
                    ResponseMessage = CreateResponse(true)
                };
                harness.HttpManager.AddMockHandler(tokenHttpCallHandler);

                var app = ConfidentialClientApplicationBuilder
                                 .Create(TestConstants.ClientId)
                                 .WithAuthority($"https://{inputEnv}/common", true)
                                 .WithHttpManager(harness.HttpManager)
                                 .WithAzureRegion(ConfidentialClientApplication.AttemptRegionDiscovery)
                                 .WithClientSecret(TestConstants.ClientSecret)
                                 .Build();

                AuthenticationResult result = await app
                    .AcquireTokenForClient(TestConstants.s_scope)
                    .WithTenantId($"17b189bc-2b81-4ec5-aa51-3e628cbc931b")
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual("eastus", result.ApiEvent.RegionUsed);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                result = await app
                   .AcquireTokenForClient(TestConstants.s_scope)
                   .WithTenantId($"17b189bc-2b81-4ec5-aa51-3e628cbc931b")
                   .ExecuteAsync()
                   .ConfigureAwait(false);

                Assert.AreEqual(EastUsRegion, result.ApiEvent.RegionUsed);
                Assert.AreEqual(EastUsRegion, result.AuthenticationResultMetadata.RegionDetails.RegionUsed);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(RegionOutcome.AutodetectSuccess, result.AuthenticationResultMetadata.RegionDetails.RegionOutcome);
                Assert.AreEqual(null, result.AuthenticationResultMetadata.RegionDetails.AutoDetectionError);
            }
        }

        [TestMethod]
        [Description("Test with a user configured region.")]
        public async Task UserRegion_DiscoveryHappensOnce_Async()
        {
            using (var harness = base.CreateTestHarness())
            {
                var httpManager = harness.HttpManager;

                httpManager.AddRegionDiscoveryMockHandler(TestConstants.Region);
                httpManager.AddMockHandler(CreateTokenResponseHttpHandler(true));

                IConfidentialClientApplication app = CreateCca(
                     httpManager,
                     TestConstants.Region);

                AuthenticationResult result = await app
                    .AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual(TestConstants.Region, result.ApiEvent.RegionUsed);
                Assert.AreEqual(RegionAutodetectionSource.Imds, result.ApiEvent.RegionAutodetectionSource);
                Assert.AreEqual(RegionOutcome.UserProvidedValid, result.ApiEvent.RegionOutcome);
                Assert.AreEqual(null, result.AuthenticationResultMetadata.RegionDetails.AutoDetectionError);
                Assert.AreEqual(TestConstants.Region, result.AuthenticationResultMetadata.RegionDetails.RegionUsed);
                Assert.AreEqual(RegionOutcome.UserProvidedValid, result.AuthenticationResultMetadata.RegionDetails.RegionOutcome);

                Assert.IsTrue(result.AuthenticationResultMetadata.TokenSource == TokenSource.IdentityProvider);

                result = await app
                    .AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsTrue(result.AuthenticationResultMetadata.TokenSource == TokenSource.Cache);
                Assert.AreEqual(null, result.AuthenticationResultMetadata.RegionDetails.AutoDetectionError);
                Assert.AreEqual(TestConstants.Region, result.AuthenticationResultMetadata.RegionDetails.RegionUsed);
                Assert.AreEqual(RegionOutcome.UserProvidedValid, result.AuthenticationResultMetadata.RegionDetails.RegionOutcome);
            }
        }

        [TestMethod]
        // Test for https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2514
        public async Task AuthorityValidationHappensOnNonRegionalAuthorityAsync()
        {
            using (var harness = base.CreateTestHarness())
            {
                var httpManager = harness.HttpManager;
                var handler = new MockHttpMessageHandler()
                {
                    ExpectedUrl = "https://login.microsoftonline.com/common/discovery/instance",
                    ExpectedMethod = HttpMethod.Get,
                    ResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent(TestConstants.DiscoveryFailedResponse)
                    }
                };

                httpManager.AddMockHandler(handler);

                var app = ConfidentialClientApplicationBuilder
                                 .Create(TestConstants.ClientId)
                                 .WithAuthority("https://invalid.com/common")
                                 .WithRedirectUri(TestConstants.RedirectUri)
                                 .WithHttpManager(httpManager)
                                 .WithClientSecret(TestConstants.ClientSecret)
                                 .Build();

                // Act
                var ex = await AssertException.TaskThrowsAsync<MsalServiceException>(() => app
                    .AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync())
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(MsalError.InvalidInstance, ex.ErrorCode);
                var qp = CoreHelpers.ParseKeyValueList(handler.ActualRequestMessage.RequestUri.Query.Substring(1), '&', true, null);
                Assert.AreEqual("https://invalid.com/common/oauth2/v2.0/authorize", qp["authorization_endpoint"]);
            }
        }

        [TestMethod]
        [Description("Test when region is configured with custom metadata")]
        public void RegionConfiguredWithCustomInstanceDiscoveryThrowsException()
        {
            using (var harness = base.CreateTestHarness())
            {
                var httpManager = harness.HttpManager;

                var ex = Assert.ThrowsException<MsalClientException>(() => CreateCca(
                    httpManager,
                    ConfidentialClientApplication.AttemptRegionDiscovery,
                    hasCustomInstanceMetadata: true));

                Assert.AreEqual(MsalError.RegionDiscoveryWithCustomInstanceMetadata, ex.ErrorCode);
                Assert.AreEqual(MsalErrorMessage.RegionDiscoveryWithCustomInstanceMetadata, ex.Message);
            }
        }

        [TestMethod]
        [Description("Test when region is configured with custom metadata uri")]
        public void RegionConfiguredWithCustomInstanceDiscoveryUriThrowsException()
        {
            using (var harness = base.CreateTestHarness())
            {
                var httpManager = harness.HttpManager;

                var ex = Assert.ThrowsException<MsalClientException>(() => CreateCca(
                    httpManager,
                    ConfidentialClientApplication.AttemptRegionDiscovery,
                    hasCustomInstanceMetadataUri: true));

                Assert.AreEqual(MsalError.RegionDiscoveryWithCustomInstanceMetadata, ex.ErrorCode);
                Assert.AreEqual(MsalErrorMessage.RegionDiscoveryWithCustomInstanceMetadata, ex.Message);
            }
        }

        private static IConfidentialClientApplication CreateCca(MockHttpManager httpManager, string region, bool hasCustomInstanceMetadata = false, bool hasCustomInstanceMetadataUri = false)
        {
            var builder = ConfidentialClientApplicationBuilder
                                 .Create(TestConstants.ClientId)
                                 .WithHttpManager(httpManager)
                                 .WithClientSecret(TestConstants.ClientSecret);

            if (region != null)
            {
                builder = builder.WithAzureRegion(region);
            }

            if (hasCustomInstanceMetadata)
            {
                string instanceMetadataJson = File.ReadAllText(
                ResourceHelper.GetTestResourceRelativePath("CustomInstanceMetadata.json"));
                builder = builder.WithInstanceDiscoveryMetadata(instanceMetadataJson);
            }

            if (hasCustomInstanceMetadataUri)
            {
                Uri customMetadataUri = new Uri("http://login.microsoftonline.com/");
                builder = builder.WithInstanceDiscoveryMetadata(customMetadataUri);
            }

            return builder.Build();
        }
        private static MockHttpMessageHandler CreateTokenResponseHttpHandler(bool expectRegional)
        {
            return new MockHttpMessageHandler()
            {
                ExpectedUrl = expectRegional ?
                    $"https://{TestConstants.Region}.login.microsoft.com/common/oauth2/v2.0/token" :
                    "https://login.microsoftonline.com/common/oauth2/v2.0/token",
                ExpectedMethod = HttpMethod.Post,
                ResponseMessage = CreateResponse(true)
            };
        }

        private static HttpResponseMessage CreateResponse(bool clientCredentialFlow)
        {
            return clientCredentialFlow ?
                MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage(MockHelpers.CreateClientInfo(TestConstants.Uid, TestConstants.Utid)) :
                MockHelpers.CreateSuccessTokenResponseMessage(
                          TestConstants.s_scope.AsSingleString(),
                          MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId),
                          MockHelpers.CreateClientInfo(TestConstants.Uid, TestConstants.Utid));
        }

        [TestMethod]
        public async Task RegionFallbackToGlobal_WhenImdsFailsAndNoEnvVarSet()
        {
            using (var harness = base.CreateTestHarness())
            {
                harness.ServiceBundle.Config.RetryPolicyFactory = new TestRetryPolicyFactory();
                var httpManager = harness.HttpManager;

                // for simplicity, return 404 so retry is not triggered
                httpManager.AddRegionDiscoveryMockHandlerWithError(HttpStatusCode.NotFound);
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandler(CreateTokenResponseHttpHandler(false));

                IConfidentialClientApplication app = CreateCca(
                    httpManager,
                    ConfidentialClientApplication.AttemptRegionDiscovery);

                AuthenticationResult result = await app
                    .AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);
                Assert.IsNull(result.ApiEvent.RegionUsed);
                Assert.AreEqual(RegionOutcome.FallbackToGlobal, result.ApiEvent.RegionOutcome);
                Assert.AreEqual(RegionOutcome.FallbackToGlobal, result.AuthenticationResultMetadata.RegionDetails.RegionOutcome);
            }
        }

        [TestMethod]
        public void RegionDiscoveryThrowsException_WhenCustomMetadataAndRegionDiscoveryEnabled()
        {
            using (var harness = base.CreateTestHarness())
            {
                var httpManager = harness.HttpManager;

                var ex = Assert.ThrowsException<MsalClientException>(() => CreateCca(
                    httpManager,
                    ConfidentialClientApplication.AttemptRegionDiscovery,
                    hasCustomInstanceMetadata: true));

                Assert.AreEqual(MsalError.RegionDiscoveryWithCustomInstanceMetadata, ex.ErrorCode);
                Assert.AreEqual(MsalErrorMessage.RegionDiscoveryWithCustomInstanceMetadata, ex.Message);
            }
        }

    }
}
