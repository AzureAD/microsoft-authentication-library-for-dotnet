// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if !ANDROID && !iOS && !WINDOWS_APP 
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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit
{
    [TestClass]
    [DeploymentItem(@"Resources\local-imds-response.json")]
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
                Assert.AreEqual((int)RegionAutodetectionSource.Imds, result.ApiEvent.RegionSource);
                Assert.AreEqual((int)RegionOutcome.AutodetectSuccess, result.ApiEvent.RegionOutcome);

                // try again, result will be from cache
                result = await app
                    .AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.AreEqual(TestConstants.Region, result.ApiEvent.RegionUsed);
                Assert.AreEqual((int)RegionAutodetectionSource.Cache, result.ApiEvent.RegionSource);
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
                Assert.AreEqual((int)RegionAutodetectionSource.Cache, result.ApiEvent.RegionSource);
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
                Assert.AreEqual((int)RegionAutodetectionSource.Cache, result.ApiEvent.RegionSource);
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
                Assert.AreEqual((int)RegionAutodetectionSource.Imds, result.ApiEvent.RegionSource);
                Assert.AreEqual((int)RegionOutcome.AutodetectSuccess, result.ApiEvent.RegionOutcome);

                // when switching to non-region, token is found in the cache
                result = await appWithoutRegion
                    .AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.AreEqual(null, result.ApiEvent.RegionUsed);
                Assert.AreEqual((int)RegionAutodetectionSource.None, result.ApiEvent.RegionSource);
                Assert.AreEqual((int)RegionOutcome.None, result.ApiEvent.RegionOutcome);

                Assert.IsTrue(result.AuthenticationResultMetadata.TokenSource == TokenSource.Cache);
            }
        }

        [TestMethod]
        [Ignore] //  https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2512
        [Description("Tokens between non-regional and regional are interchangable.")]
        public async Task TokensAreInterchangable_NonRegional_To_Regional_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandler(CreateTokenResponseHttpHandler(false));

                IConfidentialClientApplication appWithRegion = CreateCca(
                    httpManager,
                    ConfidentialClientApplication.AttemptRegionDiscovery);
                InMemoryTokenCache memoryTokenCache = new InMemoryTokenCache();
                memoryTokenCache.Bind(appWithRegion.AppTokenCache);

                IConfidentialClientApplication appWithoutRegion = CreateCca(
                    httpManager,
                    null);
                memoryTokenCache.Bind(appWithoutRegion.AppTokenCache);

                AuthenticationResult result = await appWithoutRegion
                    .AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
                Assert.IsTrue(result.AuthenticationResultMetadata.TokenSource == TokenSource.IdentityProvider);

                httpManager.AddRegionDiscoveryMockHandler("uscentral");

                // when switching to non-region, token is found in the cache
                result = await appWithRegion
                    .AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsTrue(result.AuthenticationResultMetadata.TokenSource == TokenSource.Cache);
            }
        }


        [TestMethod]
        [Description("Test when region is received from environment variable")]
        public async Task FetchRegionFromEnvironmentAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                try
                {
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
                    Assert.AreEqual((int)RegionAutodetectionSource.EnvVariable, result.ApiEvent.RegionSource);
                    Assert.AreEqual((int)RegionOutcome.AutodetectSuccess, result.ApiEvent.RegionOutcome);

                    Assert.IsNotNull(result.AccessToken);

                }
                finally
                {
                    Environment.SetEnvironmentVariable("REGION_NAME", null);
                }
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
                    Assert.AreEqual((int)RegionAutodetectionSource.None, result.ApiEvent.RegionSource);
                    Assert.AreEqual((int)RegionOutcome.FallbackToGlobal, result.ApiEvent.RegionOutcome);
                }
                catch (MsalServiceException)
                {
                    Assert.Fail("Fallback to global failed.");
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
                Assert.AreEqual((int)RegionAutodetectionSource.Imds, result.ApiEvent.RegionSource);
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

    }
}
#endif
