// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if !ANDROID && !iOS && !WINDOWS_APP 
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit
{
    [TestClass]
    [DeploymentItem(@"Resources\local-imds-response.json")]
    public class ConfidentialClientWithRegionTests : TestBase
    {
        private static MockHttpMessageHandler CreateTokenResponseHttpHandler(bool clientCredentialFlow)
        {
            return new MockHttpMessageHandler()
            {
                ExpectedMethod = HttpMethod.Post,
                ResponseMessage = CreateResponse(clientCredentialFlow)
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

        private void SetupMocks(MockHttpManager httpManager)
        {
            httpManager.AddRegionDiscoveryMockHandler(File.ReadAllText(
                        ResourceHelper.GetTestResourceRelativePath("local-imds-response.json")));
        }

        [TestMethod]
        [Description("Test for regional auth with successful instance discovery.")]
        public async Task happyPathAsync()
        {
            using (var harness = CreateTestHarness())
            {
                SetupMocks(harness.HttpManager);

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(new System.Uri(ClientApplicationBase.DefaultAuthority))
                    .WithRedirectUri(TestConstants.RedirectUri)
                    .WithHttpManager(harness.HttpManager)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .BuildConcrete();

                harness.HttpManager.AddMockHandler(CreateTokenResponseHttpHandler(true));

                AuthenticationResult result = await app
                    .AcquireTokenForClient(TestConstants.s_scope)
                    .WithAzureRegion(true)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);
            }
        } 

        [TestMethod]
        [Description("Test when region is received from environment variable")]
        public async Task fetchRegionFromEnvironmentAsync()
        {
            using (var harness = CreateTestHarness())
            {
                Environment.SetEnvironmentVariable("REGION_NAME", "uscentral");

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(new System.Uri(ClientApplicationBase.DefaultAuthority))
                    .WithRedirectUri(TestConstants.RedirectUri)
                    .WithHttpManager(harness.HttpManager)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .BuildConcrete();

                harness.HttpManager.AddMockHandler(CreateTokenResponseHttpHandler(true));

                AuthenticationResult result = await app
                    .AcquireTokenForClient(TestConstants.s_scope)
                    .WithAzureRegion(true)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);
            }

        }

        [TestMethod]
        [Description("Test when the region could not be fetched")]
        public async Task RegionNotFoundAsync()
        {
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddRegionDiscoveryMockHandlerNotFound();

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(new System.Uri(ClientApplicationBase.DefaultAuthority))
                    .WithRedirectUri(TestConstants.RedirectUri)
                    .WithHttpManager(harness.HttpManager)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .BuildConcrete();
                
                try
                {
                    AuthenticationResult result = await app
                    .AcquireTokenForClient(TestConstants.s_scope)
                    .WithAzureRegion(true)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                    Assert.Fail("Exception should be thrown");
                }
                catch(MsalClientException e)
                {
                    Assert.IsNotNull(e);
                    Assert.AreEqual(MsalError.RegionDiscoveryFailed, e.ErrorCode);
                    Assert.AreEqual(MsalErrorMessage.RegionDiscoveryFailed, e.Message);
                }
            }
        }
    }
}
#endif
