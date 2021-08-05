// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if !ANDROID && !iOS && !WINDOWS_APP 
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Region;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit
{
    [TestClass]
    public class ConfidentialClientWithRegionTests : TestBase
    {

        [TestMethod]
        [Description("Test for regional auth with successful instance discovery.")]
        public async Task FetchRegionFromLocalImdsCallAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddRegionDiscoveryMockHandler(TestConstants.Region);

                IConfidentialClientApplication app = CreateCca(
                    httpManager,
                    ConfidentialClientApplication.AttemptRegionDiscovery);
                httpManager.AddMockHandler(CreateTokenResponseHttpHandler(true));

                AuthenticationResult result = await app
                    .AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.AreEqual(TestConstants.Region, result.ApiEvent.RegionUsed);
                Assert.AreEqual((int)RegionAutodetectionSource.Imds, result.ApiEvent.RegionAutodetectionSource);
                Assert.AreEqual((int)RegionOutcome.AutodetectSuccess, result.ApiEvent.RegionOutcome);

                // try again, result will be from cache
                result = await app
                    .AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.AreEqual(TestConstants.Region, result.ApiEvent.RegionUsed);
                Assert.AreEqual((int)RegionAutodetectionSource.Cache, result.ApiEvent.RegionAutodetectionSource);
                Assert.AreEqual((int)RegionOutcome.AutodetectSuccess, result.ApiEvent.RegionOutcome);
                Assert.IsTrue(result.AuthenticationResultMetadata.TokenSource == TokenSource.Cache);

                // try again, with force refresh, region should be from cache
                httpManager.AddMockHandler(CreateTokenResponseHttpHandler(true));
                result = await app
                  .AcquireTokenForClient(TestConstants.s_scope)
                  .WithForceRefresh(true)
                  .ExecuteAsync(CancellationToken.None)
                  .ConfigureAwait(false);

                Assert.AreEqual(TestConstants.Region, result.ApiEvent.RegionUsed);
                Assert.AreEqual((int)RegionAutodetectionSource.Cache, result.ApiEvent.RegionAutodetectionSource);
                Assert.AreEqual((int)RegionOutcome.AutodetectSuccess, result.ApiEvent.RegionOutcome);
                Assert.IsTrue(result.AuthenticationResultMetadata.TokenSource == TokenSource.IdentityProvider);

                // try again, create a new app, result should still be from cache 
                IConfidentialClientApplication app2 = CreateCca(
                    httpManager,
                    ConfidentialClientApplication.AttemptRegionDiscovery);

                httpManager.AddMockHandler(CreateTokenResponseHttpHandler(true));
                result = await app2
                  .AcquireTokenForClient(TestConstants.s_scope)
                  .ExecuteAsync(CancellationToken.None)
                  .ConfigureAwait(false);

                Assert.AreEqual(TestConstants.Region, result.ApiEvent.RegionUsed);
                Assert.AreEqual((int)RegionAutodetectionSource.Cache, result.ApiEvent.RegionAutodetectionSource);
                Assert.AreEqual((int)RegionOutcome.AutodetectSuccess, result.ApiEvent.RegionOutcome);
                Assert.IsTrue(result.AuthenticationResultMetadata.TokenSource == TokenSource.IdentityProvider);
            }
        }

        [TestMethod]
        [Description("Tokens between regional and non-regional are interchangable.")]
        public async Task TokensAreInterchangable_Regional_To_NonRegional_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
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
                Assert.AreEqual((int)RegionAutodetectionSource.Imds, result.ApiEvent.RegionAutodetectionSource);
                Assert.AreEqual((int)RegionOutcome.AutodetectSuccess, result.ApiEvent.RegionOutcome);

                // when switching to non-region, token is found in the cache
                result = await appWithoutRegion
                    .AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.AreEqual(null, result.ApiEvent.RegionUsed);
                Assert.AreEqual((int)RegionAutodetectionSource.None, result.ApiEvent.RegionAutodetectionSource);
                Assert.AreEqual((int)RegionOutcome.None, result.ApiEvent.RegionOutcome);

                Assert.IsTrue(result.AuthenticationResultMetadata.TokenSource == TokenSource.Cache);
            }
        }

        [TestMethod]
        [Description("Test when the region could not be fetched -> fallback to global.")]
        public async Task RegionFallbackToGlobalAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddRegionDiscoveryMockHandlerNotFound();
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandler(CreateTokenResponseHttpHandler(false));

                IConfidentialClientApplication app = CreateCca(
                     httpManager,
                     ConfidentialClientApplication.AttemptRegionDiscovery);

                try
                {
                    AuthenticationResult result = await app
                        .AcquireTokenForClient(TestConstants.s_scope)
                        .ExecuteAsync(CancellationToken.None)
                        .ConfigureAwait(false);

                    Assert.IsNotNull(result.AccessToken);

                    Assert.AreEqual(null, result.ApiEvent.RegionUsed);
                    Assert.AreEqual((int)RegionAutodetectionSource.FailedAutoDiscovery, result.ApiEvent.RegionAutodetectionSource);
                    Assert.AreEqual((int)RegionOutcome.FallbackToGlobal, result.ApiEvent.RegionOutcome);
                }
                catch (MsalServiceException)
                {
                    Assert.Fail("Fallback to global failed.");
                }
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
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddRegionDiscoveryMockHandlerNotFound();

                var discoveryHandler = MockHelpers.CreateInstanceDiscoveryMockHandler(
                     "https://login.microsoftonline.com/common/discovery/instance",
                     TestConstants.DiscoveryJsonResponse);

                var tokenHttpCallHandler = new MockHttpMessageHandler()
                {
                    ExpectedUrl = "https://eastus.login.windows-ppe.net/17b189bc-2b81-4ec5-aa51-3e628cbc931b/oauth2/v2.0/token",
                    ExpectedMethod = HttpMethod.Post,
                    ResponseMessage = CreateResponse(true)
                };

                httpManager.AddMockHandler(discoveryHandler);
                httpManager.AddMockHandler(tokenHttpCallHandler);

                var app = ConfidentialClientApplicationBuilder
                                 .Create(TestConstants.ClientId)
                                 .WithAuthority("https://login.windows-ppe.net/common", true)
                                 .WithHttpManager(httpManager)
                                 .WithAzureRegion("eastus")
                                 .WithClientSecret(TestConstants.ClientSecret)
                                 .Build();

                AuthenticationResult result = await app
                    .AcquireTokenForClient(TestConstants.s_scope)
                    .WithAuthority("https://login.windows-ppe.net/17b189bc-2b81-4ec5-aa51-3e628cbc931b")
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual("eastus", result.ApiEvent.RegionUsed);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(
                    "https://login.microsoftonline.com/common/discovery/instance?api-version=1.1&authorization_endpoint=https%3A%2F%2Flogin.windows-ppe.net%2F17b189bc-2b81-4ec5-aa51-3e628cbc931b%2Foauth2%2Fv2.0%2Fauthorize",
                    discoveryHandler.ActualRequestMessage.RequestUri.AbsoluteUri,
                    "Authority validation is made on https://login.microsoftonline.com/ and it validates the auth_endpoint of the non-regional authority");

                result = await app
                   .AcquireTokenForClient(TestConstants.s_scope)
                   .WithAuthority("https://login.windows-ppe.net/17b189bc-2b81-4ec5-aa51-3e628cbc931b")
                   .ExecuteAsync()
                   .ConfigureAwait(false);

                Assert.AreEqual("eastus", result.ApiEvent.RegionUsed);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);

            }
        }

        [TestMethod]
        // regression: https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2686
        public async Task OtherCloud_WithValidation_Async()
        {
            await RunPpeTestAsync(validateAuthority: true, authorityIsValid: true).ConfigureAwait(false);
            await RunPpeTestAsync(validateAuthority: true, authorityIsValid: false).ConfigureAwait(false);
            await RunPpeTestAsync(validateAuthority: false, authorityIsValid: true).ConfigureAwait(false);
            await RunPpeTestAsync(validateAuthority: false, authorityIsValid: false).ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow("login.partner.microsoftonline.cn", "login.partner.microsoftonline.cn")]
        [DataRow("login.chinacloudapi.cn", "login.partner.microsoftonline.cn")]
        [DataRow("login.microsoftonline.us", "login.microsoftonline.us")]
        [DataRow("login.usgovcloudapi.net", "login.microsoftonline.us")]
        [DataRow("login-us.microsoftonline.com", "login-us.microsoftonline.com")]
        [DataRow("login.windows.net", "r.login.microsoftonline.com")]
        [DataRow("login.microsoft.com", "r.login.microsoftonline.com")]
        [DataRow("sts.windows.net", "r.login.microsoftonline.com")]
        [DataRow("login.microsoftonline.com", "r.login.microsoftonline.com")]
        public async Task PublicAndSovereignCloud_UsesPreferredNetwork_AndNoDiscovery_Async(string inputEnv, string expectedEnv)
        {
            const string region = "eastus";

            using (var harness = new MockHttpAndServiceBundle())
            {
                harness.HttpManager.AddRegionDiscoveryMockHandlerNotFound();
                var tokenHttpCallHandler = new MockHttpMessageHandler()
                {
                    ExpectedUrl = $"https://eastus.{expectedEnv}/17b189bc-2b81-4ec5-aa51-3e628cbc931b/oauth2/v2.0/token",
                    ExpectedMethod = HttpMethod.Post,
                    ResponseMessage = CreateResponse(true)
                };
                harness.HttpManager.AddMockHandler(tokenHttpCallHandler);

                var app = ConfidentialClientApplicationBuilder
                                 .Create(TestConstants.ClientId)
                                 .WithAuthority($"https://{inputEnv}/common", true)
                                 .WithHttpManager(harness.HttpManager)
                                 .WithAzureRegion(region)
                                 .WithClientSecret(TestConstants.ClientSecret)
                                 .Build();

                AuthenticationResult result = await app
                    .AcquireTokenForClient(TestConstants.s_scope)
                    .WithAuthority($"https://{inputEnv}/17b189bc-2b81-4ec5-aa51-3e628cbc931b")
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual("eastus", result.ApiEvent.RegionUsed);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                result = await app
                   .AcquireTokenForClient(TestConstants.s_scope)
                   .WithAuthority($"https://{inputEnv}/17b189bc-2b81-4ec5-aa51-3e628cbc931b")
                   .ExecuteAsync()
                   .ConfigureAwait(false);

                Assert.AreEqual("eastus", result.ApiEvent.RegionUsed);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        private static async Task RunPpeTestAsync(bool validateAuthority, bool authorityIsValid)
        {
            using (var harness = new MockHttpAndServiceBundle())
            {
                MockHttpMessageHandler discoveryHandler = null;
                if (authorityIsValid)
                {
                    harness.HttpManager.AddRegionDiscoveryMockHandler(TestConstants.Region);
                    discoveryHandler = MockHelpers.CreateInstanceDiscoveryMockHandler(
                         "https://login.microsoftonline.com/common/discovery/instance",
                         TestConstants.DiscoveryJsonResponse);
                }
                else
                {
                    harness.HttpManager.AddRegionDiscoveryMockHandler(TestConstants.Region);
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
                    ExpectedUrl = "https://eastus.login.windows-ppe.net/17b189bc-2b81-4ec5-aa51-3e628cbc931b/oauth2/v2.0/token",
                    ExpectedMethod = HttpMethod.Post,
                    ResponseMessage = CreateResponse(true)
                };


                if (authorityIsValid || !validateAuthority) // no calls because authority validation will fail
                {
                    harness.HttpManager.AddMockHandler(discoveryHandler);
                    harness.HttpManager.AddMockHandler(tokenHttpCallHandler);
                }
                else
                {
                    harness.HttpManager.AddMockHandler(discoveryHandler);
                }

                var app = ConfidentialClientApplicationBuilder
                                 .Create(TestConstants.ClientId)
                                 .WithAuthority("https://login.windows-ppe.net/common", validateAuthority)
                                 .WithHttpManager(harness.HttpManager)
                                 .WithAzureRegion("eastus")
                                 .WithClientSecret(TestConstants.ClientSecret)
                                 .Build();

                if (!authorityIsValid && validateAuthority)
                {
                    var ex = await AssertException.TaskThrowsAsync<MsalServiceException>(() => app
                   .AcquireTokenForClient(TestConstants.s_scope)
                   .WithAuthority("https://login.windows-ppe.net/17b189bc-2b81-4ec5-aa51-3e628cbc931b")
                   .ExecuteAsync()).ConfigureAwait(false);

                    Assert.AreEqual(MsalError.InvalidInstance, ex.ErrorCode);
                    var qp = CoreHelpers.ParseKeyValueList(discoveryHandler.ActualRequestMessage.RequestUri.Query.Substring(1), '&', true, null);
                    Assert.AreEqual("https://login.windows-ppe.net/17b189bc-2b81-4ec5-aa51-3e628cbc931b/oauth2/v2.0/authorize", qp["authorization_endpoint"]);
                }
                else
                {
                    AuthenticationResult result = await app
                        .AcquireTokenForClient(TestConstants.s_scope)
                        .WithAuthority("https://login.windows-ppe.net/17b189bc-2b81-4ec5-aa51-3e628cbc931b")
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    Assert.AreEqual("eastus", result.ApiEvent.RegionUsed);
                    Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                    Assert.AreEqual(
                        "https://login.microsoftonline.com/common/discovery/instance?api-version=1.1&authorization_endpoint=https%3A%2F%2Flogin.windows-ppe.net%2F17b189bc-2b81-4ec5-aa51-3e628cbc931b%2Foauth2%2Fv2.0%2Fauthorize",
                        discoveryHandler.ActualRequestMessage.RequestUri.AbsoluteUri,
                        "Authority validation is made on https://login.microsoftonline.com/ and it validates the auth_endpoint of the non-regional authority");

                    result = await app
                       .AcquireTokenForClient(TestConstants.s_scope)
                       .WithAuthority("https://login.windows-ppe.net/17b189bc-2b81-4ec5-aa51-3e628cbc931b")
                       .ExecuteAsync()
                       .ConfigureAwait(false);

                    Assert.AreEqual("eastus", result.ApiEvent.RegionUsed);
                    Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                }
            }
        }

        [TestMethod]
        [Description("Test with a user configured region.")]
        public async Task UserRegion_DiscoveryHappensOnce_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
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
                Assert.AreEqual((int)RegionAutodetectionSource.Imds, result.ApiEvent.RegionAutodetectionSource);
                Assert.AreEqual((int)RegionOutcome.UserProvidedValid, result.ApiEvent.RegionOutcome);

                Assert.IsTrue(result.AuthenticationResultMetadata.TokenSource == TokenSource.IdentityProvider);

                result = await app
                    .AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsTrue(result.AuthenticationResultMetadata.TokenSource == TokenSource.Cache);
            }
        }

        [TestMethod]
        // Test for https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2514
        public async Task AuthorityValidationHappensOnNonRegionalAuthorityAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
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

        private static IConfidentialClientApplication CreateCca(MockHttpManager httpManager, string region)
        {
            var builder = ConfidentialClientApplicationBuilder
                                 .Create(TestConstants.ClientId)
                                 .WithAuthority(new System.Uri(ClientApplicationBase.DefaultAuthority))
                                 .WithRedirectUri(TestConstants.RedirectUri)
                                 .WithHttpManager(httpManager)
                                 .WithClientSecret(TestConstants.ClientSecret);

            if (region != null)
            {
                builder = builder.WithAzureRegion(region);
            }

            return builder.Build();
        }
        private static MockHttpMessageHandler CreateTokenResponseHttpHandler(bool expectRegional)
        {
            return new MockHttpMessageHandler()
            {
                ExpectedUrl = expectRegional ?
                    $"https://{TestConstants.Region}.r.login.microsoftonline.com/common/oauth2/v2.0/token" :
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

    }
}
#endif
