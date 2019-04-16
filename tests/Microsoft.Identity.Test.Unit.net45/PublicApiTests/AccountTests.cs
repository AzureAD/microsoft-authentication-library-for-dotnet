// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class AccountTests
    {
        // Some tests load the TokenCache from a file and use this clientId
        private const string ClientIdInFile = "0615b6ca-88d4-4884-8729-b178178f7c27";

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
        }

        [TestMethod]
        public void Constructor_IdIsNotRequired()
        {
            // 1. Id is not required
            new Account(null, "d", "n");

            // 2. Other properties are optional too
            new Account("a.b", null, null);
        }

        [TestMethod]
        public void Constructor_PropertiesSet()
        {
            Account actual = new Account("a.b", "disp", "env");

            Assert.AreEqual("a.b", actual.HomeAccountId.Identifier);
            Assert.AreEqual("a", actual.HomeAccountId.ObjectId);
            Assert.AreEqual("b", actual.HomeAccountId.TenantId);
            Assert.AreEqual("disp", actual.Username);
            Assert.AreEqual("env", actual.Environment);
        }

        [TestMethod]
        [DeploymentItem(@"Resources\SingleCloudTokenCache.json")]
        public async Task CallsToPublicCloudDoNotHitTheNetworkAsync()
        {
            IMsalHttpClientFactory factoryThatThrows = Substitute.For<IMsalHttpClientFactory>();
            factoryThatThrows.When(x => x.GetHttpClient()).Do(x => { Assert.Fail("A network call is being performed"); });

            // Arrange
            PublicClientApplication pca = PublicClientApplicationBuilder
                .Create(ClientIdInFile)
                .WithAuthority(AzureCloudInstance.AzurePublic, AadAuthorityAudience.PersonalMicrosoftAccount)
                .WithHttpClientFactory(factoryThatThrows)
                .BuildConcrete();

            pca.InitializeTokenCacheFromFile(ResourceHelper.GetTestResourceRelativePath("SingleCloudTokenCache.json"));
            pca.UserTokenCacheInternal.Accessor.AssertItemCount(2, 2, 2, 2, 1);

            // Act
            var accounts = await pca.GetAccountsAsync().ConfigureAwait(false);

            // Assert
            Assert.AreEqual(2, accounts.Count());
            Assert.IsTrue(accounts.All(a => a.Environment == "login.microsoftonline.com"));
        }


        // Bug https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1030
        [TestMethod]
        [DeploymentItem(@"Resources\MultiCloudTokenCache.json")]
        public async Task MultiCloudEnvAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                // Arrange
                httpManager.AddInstanceDiscoveryMockHandler();

                const string TokenCacheFile = "MultiCloudTokenCache.json";
                var pcaGlobal = InitPcaForCloud(AzureCloudInstance.AzurePublic, httpManager, TokenCacheFile);
                var pcaDe = InitPcaForCloud(AzureCloudInstance.AzureGermany, httpManager, TokenCacheFile);
                var pcaCn = InitPcaForCloud(AzureCloudInstance.AzureChina, httpManager, TokenCacheFile);

                // Act
                var accountsGlobal = await pcaGlobal.GetAccountsAsync().ConfigureAwait(false);
                var accountsDe = await pcaDe.GetAccountsAsync().ConfigureAwait(false);
                var accountsCn = await pcaCn.GetAccountsAsync().ConfigureAwait(false);

                // Assert
                Assert.AreEqual("login.microsoftonline.com", accountsGlobal.Single().Environment);
                Assert.AreEqual("login.microsoftonline.de", accountsDe.Single().Environment);
                Assert.AreEqual("login.chinacloudapi.cn", accountsCn.Single().Environment);
            }
        }

        // Bug https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1030
        [TestMethod]
        [DeploymentItem(@"Resources\MultiCloudTokenCache.json")]
        public async Task GermanCloudNoNetworkCallAsync()
        {
            // if a network call is made, this test will fail
            IMsalHttpClientFactory factoryThatThrows = Substitute.For<IMsalHttpClientFactory>();
            factoryThatThrows.When(x => x.GetHttpClient()).Do(x => { Assert.Fail("A network call is being performed"); });

            var pcaDe = PublicClientApplicationBuilder
              .Create(ClientIdInFile)
              .WithAuthority(AzureCloudInstance.AzureGermany, AadAuthorityAudience.PersonalMicrosoftAccount)
              .WithHttpClientFactory(factoryThatThrows)
              .BuildConcrete();

            pcaDe.InitializeTokenCacheFromFile(ResourceHelper.GetTestResourceRelativePath("MultiCloudTokenCache.json"));

            // remove all but the German account
            pcaDe.UserTokenCacheInternal.Accessor.GetAllAccounts()
                .Where(a => a.Environment != "login.microsoftonline.de")
                .ToList()
                .ForEach(a => pcaDe.UserTokenCacheInternal.Accessor.DeleteAccount(a.GetKey()));

            // Act
            var accountsDe = await pcaDe.GetAccountsAsync().ConfigureAwait(false);

            // Assert
            Assert.AreEqual("login.microsoftonline.de", accountsDe.Single().Environment);
        }

        private PublicClientApplication InitPcaForCloud(AzureCloudInstance cloud, HttpManager httpManager, string tokenCacheFile)
        {
            PublicClientApplication pca = PublicClientApplicationBuilder
                  .Create(ClientIdInFile)
                  .WithAuthority(cloud, AadAuthorityAudience.PersonalMicrosoftAccount)
                  .WithHttpManager(httpManager)
                  .BuildConcrete();

            pca.InitializeTokenCacheFromFile(ResourceHelper.GetTestResourceRelativePath(tokenCacheFile));
            pca.UserTokenCacheInternal.Accessor.AssertItemCount(3, 3, 3, 3, 1);

            return pca;
        }
    }
}
