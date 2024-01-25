// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
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
            factoryThatThrows.When(x => x.GetHttpClient()).Do(_ => { Assert.Fail("A network call is being performed"); });

            // Arrange
            PublicClientApplication pca = PublicClientApplicationBuilder
                .Create(ClientIdInFile)
                .WithAuthority(AzureCloudInstance.AzurePublic, AadAuthorityAudience.PersonalMicrosoftAccount)
                .WithHttpClientFactory(factoryThatThrows)
                .BuildConcrete();

            pca.InitializeTokenCacheFromFile(ResourceHelper.GetTestResourceRelativePath("SingleCloudTokenCache.json"));
            pca.UserTokenCacheInternal.Accessor.AssertItemCount(2, 2, 2, 2, 1);
            var cacheAccessRecorder = pca.UserTokenCache.RecordAccess();

            // Act
            var accounts = await pca.GetAccountsAsync().ConfigureAwait(false);

            // Assert
            Assert.AreEqual(2, accounts.Count());
            Assert.IsTrue(accounts.All(a => a.Environment == "login.microsoftonline.com"));
            cacheAccessRecorder.AssertAccessCounts(1, 0);
        }

        // Bug https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1030
        [TestMethod]
        [DeploymentItem(@"Resources\MultiCloudTokenCache.json")]
        public async Task MultiCloudEnvAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                // Arrange
                // no discovery made because all environments are known

                const string TokenCacheFile = "MultiCloudTokenCache.json";
                var pcaGlobal = InitPcaFromCacheFile(AzureCloudInstance.AzurePublic, httpManager, TokenCacheFile);
                var pcaDe = InitPcaFromCacheFile(AzureCloudInstance.AzureGermany, httpManager, TokenCacheFile);
                var pcaCn = InitPcaFromCacheFile(AzureCloudInstance.AzureChina, httpManager, TokenCacheFile);

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

        [TestMethod]
        [DeploymentItem(@"Resources\MultiCloudTokenCache.json")]
        public async Task GetAccounts_PerformsNetworkInstanceDiscovery_IfUnknownAccountEnvironment_Async()
        {
            // Arrange - modify an existing account to have an unknown environment
            string tokenCacheAsString = File.ReadAllText(
                ResourceHelper.GetTestResourceRelativePath("MultiCloudTokenCache.json"));
            var cacheJson = JObject.Parse(tokenCacheAsString);

            JEnumerable<JToken> tokens = cacheJson["Account"].Children();
            foreach (JToken token in tokens)
            {
                var obj = token.Children().Single() as JObject;

                if (string.Equals(
                    obj["environment"].ToString(),
                    "login.microsoftonline.de",
                    StringComparison.InvariantCulture))
                {
                    obj["environment"] = new Uri(TestConstants.AuthorityNotKnownTenanted).Host;
                }
            }

            tokenCacheAsString = cacheJson.ToString();

            await ValidateGetAccountsWithDiscoveryAsync(tokenCacheAsString).ConfigureAwait(false);
        }

        [TestMethod]
        [DeploymentItem(@"Resources\MultiCloudTokenCache.json")]
        public async Task GetAccounts_PerformsNetworkInstanceDiscovery_IfUnknownRtEnvironment_Async()
        {
            // Arrange - modify an existing account to have an unknown environment
            string tokenCacheAsString = File.ReadAllText(
                ResourceHelper.GetTestResourceRelativePath("MultiCloudTokenCache.json"));
            var cacheJson = JObject.Parse(tokenCacheAsString);

            JEnumerable<JToken> tokens = cacheJson["RefreshToken"].Children();
            foreach (JToken token in tokens)
            {
                var obj = token.Children().Single() as JObject;

                if (string.Equals(
                    obj["environment"].ToString(),
                    "login.microsoftonline.de",
                    StringComparison.InvariantCulture))
                {
                    obj["environment"] = new Uri(TestConstants.AuthorityNotKnownTenanted).Host;
                }
            }

            tokenCacheAsString = cacheJson.ToString();

            await ValidateGetAccountsWithDiscoveryAsync(tokenCacheAsString).ConfigureAwait(false);
        }

        // Bug https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1030
        [TestMethod]
        [DeploymentItem(@"Resources\MultiCloudTokenCache.json")]
        public async Task GermanCloudNoNetworkCallAsync()
        {
            // if a network call is made, this test will fail
            IMsalHttpClientFactory factoryThatThrows = Substitute.For<IMsalHttpClientFactory>();
            factoryThatThrows.When(x => x.GetHttpClient()).Do(_ => { Assert.Fail("A network call is being performed"); });

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
                .ForEach(a => pcaDe.UserTokenCacheInternal.Accessor.DeleteAccount(a));
            var cacheAccessRecorder = pcaDe.UserTokenCache.RecordAccess();

            // Act
            var accountsDe = await pcaDe.GetAccountsAsync().ConfigureAwait(false);

            // Assert
            Assert.AreEqual("login.microsoftonline.de", accountsDe.Single().Environment);
            cacheAccessRecorder.AssertAccessCounts(1, 0);
        }

        [TestMethod]
        public void TestGetAccounts()
        {
            using (var httpManager = new MockHttpManager())
            {
                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithHttpManager(httpManager)
                                                                            .BuildConcrete();

                IEnumerable<IAccount> accounts = app.GetAccountsAsync().Result;
                Assert.IsNotNull(accounts);
                Assert.IsFalse(accounts.Any());
                TokenCacheHelper.PopulateCache(app.UserTokenCacheInternal.Accessor);

                accounts = app.GetAccountsAsync().Result;
                Assert.IsNotNull(accounts);
                Assert.AreEqual(1, accounts.Count());
                string clientInfo = MockHelpers.CreateClientInfo();
                string homeAccountId = ClientInfo.CreateFromJson(clientInfo).ToAccountIdentifier();
                var atItem = new MsalAccessTokenCacheItem(
                    TestConstants.ProductionPrefCacheEnvironment,
                    TestConstants.ClientId,
                    TestConstants.s_scope.AsSingleString(),
                    TestConstants.Utid,
                    null,
                    DateTimeOffset.UtcNow,
                    new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(3600)),
                    new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(7200)),
                    clientInfo,
                    homeAccountId);

                atItem.Secret = atItem.CacheKey;
                app.UserTokenCacheInternal.Accessor.SaveAccessToken(atItem);

                // another cache entry for different uid. user count should be 2.

                string clientInfo2 = MockHelpers.CreateClientInfo("uId1", "uTId1");
                string homeAccountId2 = ClientInfo.CreateFromJson(clientInfo2).ToAccountIdentifier();

                MsalRefreshTokenCacheItem rtItem = new MsalRefreshTokenCacheItem(
                    TestConstants.ProductionPrefCacheEnvironment,
                    TestConstants.ClientId,
                    "someRT",
                    clientInfo2,
                    null,
                    homeAccountId2);

                app.UserTokenCacheInternal.Accessor.SaveRefreshToken(rtItem);

                MsalIdTokenCacheItem idTokenCacheItem = new MsalIdTokenCacheItem(
                    TestConstants.ProductionPrefCacheEnvironment,
                    TestConstants.ClientId,
                    MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId),
                    clientInfo2,
                    homeAccountId: homeAccountId2,
                    tenantId: "uTId1");

                app.UserTokenCacheInternal.Accessor.SaveIdToken(idTokenCacheItem);

                MsalAccountCacheItem accountCacheItem = new MsalAccountCacheItem(
                    TestConstants.ProductionPrefCacheEnvironment,
                    null,
                    clientInfo2,
                    homeAccountId2,
                    null,
                    null,
                    "uTId1",
                    null,
                    null,
                    null);

                app.UserTokenCacheInternal.Accessor.SaveAccount(accountCacheItem);

                Assert.AreEqual(2, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count);
                accounts = app.GetAccountsAsync().Result;
                Assert.IsNotNull(accounts);
                Assert.AreEqual(2, accounts.Count()); // scoped by env

                // another cache entry for different environment. user count should still be 2. Sovereign cloud user must not be returned
                string clientInfo3 = MockHelpers.CreateClientInfo(TestConstants.Uid + "more1", TestConstants.Utid);
                string homeAccountId3 = ClientInfo.CreateFromJson(clientInfo3).ToAccountIdentifier();

                rtItem = new MsalRefreshTokenCacheItem(
                    TestConstants.SovereignNetworkEnvironmentDE,
                    TestConstants.ClientId,
                    "someRT",
                    clientInfo3,
                    null,
                    homeAccountId3);

                app.UserTokenCacheInternal.Accessor.SaveRefreshToken(rtItem);
                Assert.AreEqual(3, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count);
                accounts = app.GetAccountsAsync().Result;
                Assert.IsNotNull(accounts);
                Assert.AreEqual(2, accounts.Count());
            }
        }

        [TestMethod]
        public async Task TestAccountAcrossMultipleClientIdsAsync()
        {
            // Arrange

            PublicClientApplication app = PublicClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .BuildConcrete();

            // Populate with tokens tied to ClientId2
            TokenCacheHelper.PopulateCache(app.UserTokenCacheInternal.Accessor, clientId: TestConstants.ClientId2);
            var cacheAccessRecorder = app.UserTokenCache.RecordAccess();

            app.UserTokenCacheInternal.Accessor.AssertItemCount(
                expectedAtCount: 2,
                expectedRtCount: 1,
                expectedAccountCount: 1,
                expectedIdtCount: 1,
                expectedAppMetadataCount: 1);

            // Act
            var accounts = await app.GetAccountsAsync().ConfigureAwait(false);

            // Assert 
            Assert.IsFalse(accounts.Any(), "No accounts should be returned because the existing account to a different client");
            cacheAccessRecorder.AssertAccessCounts(1, 0);

            // Arrange

            // Populate for clientid2
            TokenCacheHelper.PopulateCache(app.UserTokenCacheInternal.Accessor, clientId: TestConstants.ClientId);

            app.UserTokenCacheInternal.Accessor.AssertItemCount(
                expectedAtCount: 4,
                expectedRtCount: 2,
                expectedAccountCount: 1, // still 1 account
                expectedIdtCount: 2,
                expectedAppMetadataCount: 2);

            // Act
            accounts = await app.GetAccountsAsync().ConfigureAwait(false);
            cacheAccessRecorder.AssertAccessCounts(2, 0);
            Assert.IsTrue(cacheAccessRecorder.LastAfterAccessNotificationArgs.HasTokens);

            await app.RemoveAsync(accounts.Single()).ConfigureAwait(false);

            // Assert
            cacheAccessRecorder.AssertAccessCounts(2, 1);
            app.UserTokenCacheInternal.Accessor.AssertItemCount(
               expectedAtCount: 2,
               expectedRtCount: 1,
               expectedAccountCount: 0,
               expectedIdtCount: 1,
               expectedAppMetadataCount: 2); // app metadata is never deleted
        }

        [TestMethod]
        public void GetAccountsAndSignThemOutTest()
        {
            PublicClientApplication app = PublicClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .BuildConcrete();

            TokenCacheHelper.PopulateCache(app.UserTokenCacheInternal.Accessor);

            foreach (var user in app.GetAccountsAsync().Result)
            {
                app.RemoveAsync(user).Wait();
            }

            Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
            Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count);
        }

        [TestMethod]
        public void ToClaimsPrincipal_Success()
        {
            // copied from https://github.com/AzureAD/microsoft-identity-web/blob/master/src/Microsoft.Identity.Web/AccountExtensions.cs#L13
            static ClaimsPrincipal ToClaimsPrincipal(IAccount account)
            {
                ClaimsIdentity identity = new ClaimsIdentity(new[]
                    {
                    new Claim(ClaimTypes.Upn, account.Username),
                });

                if (!string.IsNullOrEmpty(account.HomeAccountId?.ObjectId))
                {
                    identity.AddClaim(new Claim("oid", account.HomeAccountId.ObjectId));
                }

                if (!string.IsNullOrEmpty(account.HomeAccountId?.TenantId))
                {
                    identity.AddClaim(new Claim("tid", account.HomeAccountId.TenantId));
                }

                return new ClaimsPrincipal(identity);
            }

            var username = "username@test.com";
            var oid = "objectId";
            var tid = "tenantId";

            IAccount account = Substitute.For<IAccount>();
            account.Username.Returns(username);
            var accId = new AccountId($"{oid}.{tid}", oid, tid);
            account.HomeAccountId.Returns(accId);

            var claimsIdentityResult = ToClaimsPrincipal(account).Identity as ClaimsIdentity;

            Assert.IsNotNull(claimsIdentityResult, "The ClaimsIdentity should not be null.");
            Assert.AreEqual(3, claimsIdentityResult.Claims.Count(), "Expected 3 claims in the ClaimsIdentity.");
            Assert.AreEqual(username, claimsIdentityResult.FindFirst(ClaimTypes.Upn)?.Value, "UPN claim value should match.");
            Assert.AreEqual(oid, claimsIdentityResult.FindFirst("oid")?.Value, "OID claim value should match.");
            Assert.AreEqual(tid, claimsIdentityResult.FindFirst("tid")?.Value, "TID claim value should match.");
        }

        private async Task ValidateGetAccountsWithDiscoveryAsync(string tokenCacheAsString)
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var pcaGlobal = InitPcaFromCacheString(AzureCloudInstance.AzurePublic, httpManager, tokenCacheAsString);
                var pcaDe = InitPcaFromCacheString(AzureCloudInstance.AzureGermany, httpManager, tokenCacheAsString);
                var pcaCn = InitPcaFromCacheString(AzureCloudInstance.AzureChina, httpManager, tokenCacheAsString);

                // Act
                var accountsGlobal = await pcaGlobal.GetAccountsAsync().ConfigureAwait(false);
                var accountsDe = await pcaDe.GetAccountsAsync().ConfigureAwait(false);
                var accountsCn = await pcaCn.GetAccountsAsync().ConfigureAwait(false);

                // Assert
                Assert.AreEqual("login.microsoftonline.com", accountsGlobal.Single().Environment);
                Assert.IsTrue(!accountsDe.Any());
                Assert.AreEqual("login.chinacloudapi.cn", accountsCn.Single().Environment);
            }
        }

        private PublicClientApplication InitPcaFromCacheFile(
            AzureCloudInstance cloud,
            IHttpManager httpManager,
            string tokenCacheFile)
        {
            return InitPcaFromCacheString(
                cloud,
                httpManager,
                File.ReadAllText(
                    ResourceHelper.GetTestResourceRelativePath(tokenCacheFile)));
        }

        private PublicClientApplication InitPcaFromCacheString(
            AzureCloudInstance cloud,
            IHttpManager httpManager,
            string tokenCacheString)
        {
            PublicClientApplication pca = PublicClientApplicationBuilder
                  .Create(ClientIdInFile)
                  .WithAuthority(cloud, AadAuthorityAudience.PersonalMicrosoftAccount)
                  .WithHttpManager(httpManager)
                  .BuildConcrete();

            pca.InitializeTokenCacheFromString(tokenCacheString);
            pca.UserTokenCacheInternal.Accessor.AssertItemCount(3, 3, 3, 3, 1);

            return pca;
        }
    }
}
