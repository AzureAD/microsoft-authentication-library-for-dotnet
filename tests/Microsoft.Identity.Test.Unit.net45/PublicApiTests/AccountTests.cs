// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.Utils;
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

     

        [TestMethod]
        public void TestGetAccounts()
        {
            var tokenCacheHelper = new TokenCacheHelper();

            using (var httpManager = new MockHttpManager())
            {
                PublicClientApplication app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                                            .WithHttpManager(httpManager)
                                                                            .BuildConcrete();

                IEnumerable<IAccount> accounts = app.GetAccountsAsync().Result;
                Assert.IsNotNull(accounts);
                Assert.IsFalse(accounts.Any());
                tokenCacheHelper.PopulateCache(app.UserTokenCacheInternal.Accessor);
                accounts = app.GetAccountsAsync().Result;
                Assert.IsNotNull(accounts);
                Assert.AreEqual(1, accounts.Count());

                var atItem = new MsalAccessTokenCacheItem(
                    MsalTestConstants.ProductionPrefCacheEnvironment,
                    MsalTestConstants.ClientId,
                    MsalTestConstants.Scope.AsSingleString(),
                    MsalTestConstants.Utid,
                    null,
                    new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(3600)),
                    new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(7200)),
                    MockHelpers.CreateClientInfo());

                atItem.Secret = atItem.GetKey().ToString();
                app.UserTokenCacheInternal.Accessor.SaveAccessToken(atItem);

                // another cache entry for different uid. user count should be 2.

                MsalRefreshTokenCacheItem rtItem = new MsalRefreshTokenCacheItem(
                    MsalTestConstants.ProductionPrefCacheEnvironment,
                    MsalTestConstants.ClientId,
                    "someRT",
                    MockHelpers.CreateClientInfo("uId1", "uTId1"));

                app.UserTokenCacheInternal.Accessor.SaveRefreshToken(rtItem);

                MsalIdTokenCacheItem idTokenCacheItem = new MsalIdTokenCacheItem(
                    MsalTestConstants.ProductionPrefCacheEnvironment,
                    MsalTestConstants.ClientId,
                    MockHelpers.CreateIdToken(MsalTestConstants.UniqueId, MsalTestConstants.DisplayableId),
                    MockHelpers.CreateClientInfo("uId1", "uTId1"),
                    "uTId1");

                app.UserTokenCacheInternal.Accessor.SaveIdToken(idTokenCacheItem);

                MsalAccountCacheItem accountCacheItem = new MsalAccountCacheItem(
                    MsalTestConstants.ProductionPrefCacheEnvironment,
                    null,
                    MockHelpers.CreateClientInfo("uId1", "uTId1"),
                    null,
                    null,
                    "uTId1",
                    null,
                    null);

                app.UserTokenCacheInternal.Accessor.SaveAccount(accountCacheItem);

                Assert.AreEqual(2, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count());
                accounts = app.GetAccountsAsync().Result;
                Assert.IsNotNull(accounts);
                Assert.AreEqual(2, accounts.Count()); // scoped by env

                // another cache entry for different environment. user count should still be 2. Sovereign cloud user must not be returned
                rtItem = new MsalRefreshTokenCacheItem(
                    MsalTestConstants.SovereignNetworkEnvironment,
                    MsalTestConstants.ClientId,
                    "someRT",
                    MockHelpers.CreateClientInfo(MsalTestConstants.Uid + "more1", MsalTestConstants.Utid));

                app.UserTokenCacheInternal.Accessor.SaveRefreshToken(rtItem);
                Assert.AreEqual(3, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count());
                accounts = app.GetAccountsAsync().Result;
                Assert.IsNotNull(accounts);
                Assert.AreEqual(2, accounts.Count());
            }
        }

        [TestMethod]
        public async Task TestAccountAcrossMultipleClientIdsAsync()
        {
            // Arrange
            var tokenCacheHelper = new TokenCacheHelper();
            PublicClientApplication app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId).BuildConcrete();

            // Populate with tokens tied to ClientId2
            tokenCacheHelper.PopulateCache(app.UserTokenCacheInternal.Accessor, clientId: MsalTestConstants.ClientId2);

            app.UserTokenCacheInternal.Accessor.AssertItemCount(
                expectedAtCount: 2,
                expectedRtCount: 1,
                expectedAccountCount: 1,
                expectedIdtCount: 1,
                expectedAppMetadataCount: 1);

            // Act
            var accounts = await app.GetAccountsAsync().ConfigureAwait(false);

            // Assert - an account is returned even if app is scoped to ClientId1
            Assert.AreEqual(1, accounts.Count());

            // Arrange

            // Populate for clientid2
            tokenCacheHelper.PopulateCache(app.UserTokenCacheInternal.Accessor, clientId: MsalTestConstants.ClientId);

            app.UserTokenCacheInternal.Accessor.AssertItemCount(
                expectedAtCount: 4,
                expectedRtCount: 2,
                expectedAccountCount: 1, // still 1 account
                expectedIdtCount: 2,
                expectedAppMetadataCount: 2);

            // Act
            await app.RemoveAsync(accounts.Single()).ConfigureAwait(false);

            // Assert
            app.UserTokenCacheInternal.Accessor.AssertItemCount(
               expectedAtCount: 0,
               expectedRtCount: 0,
               expectedAccountCount: 0,
               expectedIdtCount: 0,
               expectedAppMetadataCount: 2); // app metadata is never deleted

        }

        [TestMethod]
        public void GetAccountsAndSignThemOutTest()
        {
            PublicClientApplication app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId).BuildConcrete();
            var tokenCacheHelper = new TokenCacheHelper();
            tokenCacheHelper.PopulateCache(app.UserTokenCacheInternal.Accessor);

            foreach (var user in app.GetAccountsAsync().Result)
            {
                app.RemoveAsync(user).Wait();
            }

            Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
            Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count());
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
