// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Common.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CoreTests.InstanceTests
{
    [TestClass]
    public class PublicApiInstanceMetadataTests : TestBase
    {
        [TestMethod]
        [DeploymentItem(@"Resources\CustomInstanceMetadata.json")]
        public async Task AcquireTokenInterative_WithValidCustomInstanceMetadata_Async()
        {
            string instanceMetadataJson = File.ReadAllText(
                ResourceHelper.GetTestResourceRelativePath("CustomInstanceMetadata.json"));

            using (var harness = CreateTestHarness())
            {
                // No instance discovery is made - it is important to not have this mock handler added
                // harness.HttpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = PublicClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(new Uri("https://login.windows.net/common/"), false)
                    .WithInstanceDiscoveryMetadata(instanceMetadataJson)
                    .WithHttpManager(harness.HttpManager)
                    .BuildConcrete();

                app.ServiceBundle.ConfigureMockWebUI(
                    AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));

                // the rest of the communication with AAD happens on the preferred_network alias, not on login.windows.net
                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(TestConstants.AuthorityCommonTenant);

                AuthenticationResult result = await app
                    .AcquireTokenInteractive(TestConstants.s_scope)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
                Assert.AreEqual(TestConstants.UniqueId, result.UniqueId);
                Assert.AreEqual(TestConstants.CreateUserIdentifier(), result.Account.HomeAccountId.Identifier);
                Assert.AreEqual(TestConstants.DisplayableId, result.Account.Username);
            }
        }

        [TestMethod]
        [DeploymentItem(@"Resources\CustomInstanceMetadata.json")]
        public async Task AcquireTokenInterative_WithBadCustomInstanceMetadata_Async()
        {
            string instanceMetadataJson = File.ReadAllText(
                ResourceHelper.GetTestResourceRelativePath("CustomInstanceMetadata.json"));

            using (var harness = CreateTestHarness())
            {
                // No instance discovery is made - it is important to not have this mock handler added
                // harness.HttpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = PublicClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(new Uri(@"https://sts.windows.net/common/"), false)
                    .WithInstanceDiscoveryMetadata(instanceMetadataJson)
                    .WithHttpManager(harness.HttpManager)
                    .BuildConcrete();

                var ex = await Assert.ThrowsExceptionAsync<MsalClientException>(() => app
                    .AcquireTokenInteractive(TestConstants.s_scope)
                    .ExecuteAsync(CancellationToken.None))
                    .ConfigureAwait(false);

                Assert.AreEqual(MsalError.InvalidUserInstanceMetadata, ex.ErrorCode);
            }
        }

        [TestMethod]
        [DeploymentItem(@"Resources\CustomInstanceMetadata.json")]
        public async Task CustomInstanceDiscoveryMetadataUri_Async()
        {
            using (var harness = CreateTestHarness())
            {
                var customMetadataUri = new Uri("https://custom.instance.discovery.uri");
                var customAuthrority = "https://my.custom.authority/common/";

                harness.HttpManager.AddInstanceDiscoveryMockHandler(
                    customAuthrority,
                    customMetadataUri,
                    TestConstants.DiscoveryJsonResponse.Replace("login.microsoftonline.com", "my.custom.authority"));

                PublicClientApplication app = PublicClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(customAuthrority, false)
                    .WithInstanceDiscoveryMetadata(customMetadataUri)
                    .WithHttpManager(harness.HttpManager)
                    .BuildConcrete();

                app.ServiceBundle.ConfigureMockWebUI(
                    AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));

                // the rest of the communication with AAD happens on the preferred_network alias, not on login.windows.net
                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(customAuthrority);

                AuthenticationResult result = await app
                    .AcquireTokenInteractive(TestConstants.s_scope)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
                Assert.AreEqual(TestConstants.UniqueId, result.UniqueId);
                Assert.AreEqual(TestConstants.CreateUserIdentifier(), result.Account.HomeAccountId.Identifier);
                Assert.AreEqual(TestConstants.DisplayableId, result.Account.Username);
            }
        }
    }
}
