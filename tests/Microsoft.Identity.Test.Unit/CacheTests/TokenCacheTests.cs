// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Castle.Core.Internal;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.CacheTests
{
    [TestClass]
    public class TokenCacheTests : TestBase
    {
        public static long ValidExpiresIn = 3600;
        public static long ValidExtendedExpiresIn = 7200;

        private string _clientInfo;
        private string _homeAccountId;

        [TestInitialize]
        public override void TestInitialize()
        {
            _clientInfo = MockHelpers.CreateClientInfo();
            _homeAccountId = ClientInfo.CreateFromJson(_clientInfo).ToAccountIdentifier();

            base.TestInitialize();
        }

        [DataTestMethod]
        [DataRow(true, true, true)]
        [DataRow(true, false, false)]
        [DataRow(false, true, false)]
        [DataRow(false, true, false)]
        public async Task WithLegacyCacheCompatibilityTest_Async(
            bool enableLegacyCacheCompatibility,
            bool serializeCache,
            bool expectToCallAdalLegacyCache)
        {
            // Arrange
            var legacyCachePersistence = Substitute.For<ILegacyCachePersistence>();
            var serviceBundle = TestCommon.CreateServiceBundleWithCustomHttpManager(null, isLegacyCacheEnabled: enableLegacyCacheCompatibility);
            var requestContext = new RequestContext(serviceBundle, Guid.NewGuid());
            var response = TestConstants.CreateMsalTokenResponse();

            ITokenCacheInternal cache = new TokenCache(serviceBundle, false);
            ((TokenCache)cache).LegacyCachePersistence = legacyCachePersistence;
            if (serializeCache) // no point in invoking the Legacy ADAL cache if you're only keeping it memory
            {
                cache.SetBeforeAccess((_) => { });
            }

            var requestParams = TestCommon.CreateAuthenticationRequestParameters(serviceBundle);
            requestParams.AuthorityManager = new AuthorityManager(
                requestContext,
                Authority.CreateAuthorityWithTenant(
                    requestParams.AuthorityInfo,
                    TestConstants.Utid));
            requestParams.Account = new Account(TestConstants.s_userIdentifier, $"1{TestConstants.DisplayableId}", TestConstants.ProductionPrefNetworkEnvironment);

            // Act
            await cache.FindRefreshTokenAsync(requestParams).ConfigureAwait(true);
            await cache.SaveTokenResponseAsync(requestParams, response).ConfigureAwait(true);
            await cache.GetAccountsAsync(requestParams).ConfigureAwait(true);
            await cache.RemoveAccountAsync(requestParams.Account, requestParams).ConfigureAwait(true);

            // Assert
            if (expectToCallAdalLegacyCache)
            {
                legacyCachePersistence.ReceivedWithAnyArgs().LoadCache();
                legacyCachePersistence.ReceivedWithAnyArgs().WriteCache(Arg.Any<byte[]>());
            }
            else
            {
                legacyCachePersistence.DidNotReceiveWithAnyArgs().LoadCache();
                legacyCachePersistence.DidNotReceiveWithAnyArgs().WriteCache(Arg.Any<byte[]>());
            }
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task WithMultiCloudSupportTest_Async(
            bool multiCloudSupportEnabled)
        {
            // Arrange
            var serviceBundle = TestCommon.CreateServiceBundleWithCustomHttpManager(null, isMultiCloudSupportEnabled: multiCloudSupportEnabled);
            var requestContext = new RequestContext(serviceBundle, Guid.NewGuid());
            var response = TestConstants.CreateMsalTokenResponse();

            ITokenCacheInternal cache = new TokenCache(serviceBundle, false);

            var requestParams = TestCommon.CreateAuthenticationRequestParameters(serviceBundle);
            requestParams.AuthorityManager = new AuthorityManager(
                requestContext,
                Authority.CreateAuthorityWithTenant(
                    requestParams.AuthorityInfo,
                    TestConstants.Utid));
            requestParams.Account = new Account(TestConstants.s_userIdentifier, $"1{TestConstants.DisplayableId}", TestConstants.ProductionPrefNetworkEnvironment);

            var res = await cache.SaveTokenResponseAsync(requestParams, response).ConfigureAwait(true);

            IEnumerable<IAccount> accounts = await cache.GetAccountsAsync(requestParams).ConfigureAwait(true);
            Assert.IsNotNull(accounts);
            Assert.IsNotNull(accounts.Single());

            MsalRefreshTokenCacheItem refreshToken = await cache.FindRefreshTokenAsync(requestParams).ConfigureAwait(true);
            Assert.IsNotNull(refreshToken);

            MsalIdTokenCacheItem idToken = cache.GetIdTokenCacheItem(res.Item1);
            Assert.IsNotNull(idToken);

            await cache.RemoveAccountAsync(requestParams.Account, requestParams).ConfigureAwait(true);
            accounts = await cache.GetAccountsAsync(requestParams).ConfigureAwait(true);
            Assert.IsNotNull(accounts);
            Assert.IsTrue(accounts.IsNullOrEmpty());
        }

        [TestMethod]
        public void GetExactScopesMatchedAccessTokenTest()
        {
            using (var harness = CreateTestHarness())
            {
                ITokenCacheInternal cache = new TokenCache(harness.ServiceBundle, false);
                var atItem = TokenCacheHelper.CreateAccessTokenItem();

                cache.Accessor.SaveAccessToken(atItem);
                var item = cache.FindAccessTokenAsync(
                    harness.CreateAuthenticationRequestParameters(
                        TestConstants.AuthorityTestTenant,
                        TestConstants.s_scope,
                        cache,
                        account: TestConstants.s_user)).Result;

                Assert.IsNotNull(item);
            }
        }

        [TestMethod]
        public void GetSubsetScopesMatchedAccessTokenTest()
        {
            using (var harness = CreateTestHarness())
            {
                ITokenCacheInternal cache = new TokenCache(harness.ServiceBundle, false);
                var atItem = TokenCacheHelper.CreateAccessTokenItem("r1/scope1 r1/scope2");

                cache.Accessor.SaveAccessToken(atItem);
                var param = harness.CreateAuthenticationRequestParameters(
                    TestConstants.AuthorityTestTenant,
                    new SortedSet<string>(),
                    cache,
                    account: TestConstants.s_user);

                param.Scope.Add("r1/scope1");
                var item = cache.FindAccessTokenAsync(param).Result;

                Assert.IsNotNull(item);
            }
        }

        [TestMethod]
        [WorkItem(1548)] //https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1548
        public void TokenCacheHitTest()
        {
            VerifyAccessTokenIsFound("openid profile user.read", new[] { "User.Read" });
            VerifyAccessTokenIsFound("openid profile User.Read", new[] { "User.Read", "offline_access" });
            VerifyAccessTokenIsFound("openid profile User.Read", new[] { "offline_access" });
            VerifyAccessTokenIsFound("non_graph_scope", new[] { "profile" });
            VerifyAccessTokenIsFound("non_graph_scope", new[] { "openid" });
            VerifyAccessTokenIsFound("non_graph_scope", new[] { "offline_access" });
            VerifyAccessTokenIsFound("non_graph_scope", new[] { "OFFline_access" });
            VerifyAccessTokenIsFound("non_graph_scope", new[] { "non_graph_scope", "offline_access" }); // regression
            VerifyAccessTokenIsFound("non_graph_scope", new[] { "offline_access", "profile" });
            VerifyAccessTokenIsFound("non_graph_scope", new[] { "offline_access", "profile", "openid" });
            VerifyAccessTokenIsFound("non_graph_scope", new[] { "non_graph_scope", "offline_access", "profile", "openid" });

            VerifyAccessTokenIsFound("", new string[0]);
            VerifyAccessTokenIsFound(null, new string[0]);
            VerifyAccessTokenIsFound("non_graph_scope", new string[0]);
            VerifyAccessTokenIsFound("openid profile User.Read", new string[0]);
            VerifyAccessTokenIsFound("openid profile User.Read", new[] { "User.Read" });
            VerifyAccessTokenIsFound("", new[] { "" });

            VerifyAccessTokenIsNotFound("openid profile user.read", new[] { "non_graph_scope" });
            VerifyAccessTokenIsNotFound("openid profile user.read", new[] { "email" });
            VerifyAccessTokenIsNotFound("openid profile user.read", new[] { "user.read", "email" });
        }

        private void VerifyAccessTokenIsFound(string cachedAtScopes, string[] queryScopes, bool expectFind = true)
        {
            using (var harness = CreateTestHarness())
            {
                ITokenCacheInternal cache = new TokenCache(harness.ServiceBundle, false);

                var atItem = new MsalAccessTokenCacheItem(
                    TestConstants.ProductionPrefNetworkEnvironment,
                    TestConstants.ClientId,
                    cachedAtScopes,
                    TestConstants.Utid,
                    null,
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow + TimeSpan.FromHours(1),
                    DateTimeOffset.UtcNow + TimeSpan.FromHours(2),
                    _clientInfo,
                    _homeAccountId);

                cache.Accessor.SaveAccessToken(atItem);

                var param = harness.CreateAuthenticationRequestParameters(
                    TestConstants.AuthorityTestTenant,
                    queryScopes,
                    cache,
                    account: TestConstants.s_user);

                var item = cache.FindAccessTokenAsync(param).Result;

                if (expectFind == true)
                    Assert.IsNotNull(item);
                else
                    Assert.IsNull(item);
            }
        }

        private void VerifyAccessTokenIsNotFound(string cachedAtScopes, string[] queryScopes)
        {
            VerifyAccessTokenIsFound(cachedAtScopes, queryScopes, false);
        }

        [TestMethod]
        public void GetIntersectedScopesMatchedAccessTokenTest()
        {
            using (var harness = CreateTestHarness())
            {
                ITokenCacheInternal cache = new TokenCache(harness.ServiceBundle, false);

                var atItem = TokenCacheHelper.CreateAccessTokenItem();
                cache.Accessor.SaveAccessToken(atItem);

                var param = harness.CreateAuthenticationRequestParameters(
                    TestConstants.AuthorityHomeTenant,
                    new SortedSet<string>(),
                    cache,
                    account: new Account(TestConstants.s_userIdentifier, TestConstants.DisplayableId, null));

                param.Scope.Add(TestConstants.s_scope.First());
                param.Scope.Add("non-existent-scopes");
                var item = cache.FindAccessTokenAsync(param).Result;

                // intersected scopes are not returned.
                Assert.IsNull(item);
            }
        }

        [TestMethod]
        public void AccessToken_WithRefresh_FromMsalResponseJson()
        {
            // Arrange
            string json = TestConstants.TokenResponseJson;
            json = JsonTestUtils.AddKeyValue(json, "refresh_in", "1800");

            var tokenResponse = JsonHelper.DeserializeFromJson<MsalTokenResponse>(json);
            var homeAccountId = ClientInfo.CreateFromJson(tokenResponse.ClientInfo).ToAccountIdentifier();

            // Act
            MsalAccessTokenCacheItem at = new MsalAccessTokenCacheItem(
                TestConstants.ProductionPrefNetworkEnvironment,
                    TestConstants.ClientId,
                    tokenResponse,
                    TestConstants.TenantId,
                    homeAccountId);

            // Assert
            Assert.AreEqual(1800, tokenResponse.RefreshIn);
            Assert.AreEqual(tokenResponse.TokenType, at.TokenType);
            Assert.IsNull(at.KeyId);
            Assert.IsTrue(at.RefreshOn.HasValue);
            CoreAssert.IsWithinRange(
                at.RefreshOn.Value,
                (at.CachedAt + TimeSpan.FromSeconds(1800)),
                TimeSpan.FromSeconds(Constants.DefaultJitterRangeInSeconds));
        }

        [TestMethod]
        public void AccessToken_WithKidAndType_FromMsalResponseJson()
        {
            // Arrange
            string json = TestConstants.TokenResponseJson;
            json = JsonTestUtils.AddKeyValue(json, StorageJsonKeys.TokenType, "pop");

            var tokenResponse = JsonHelper.DeserializeFromJson<MsalTokenResponse>(json);
            var homeAccountId = ClientInfo.CreateFromJson(tokenResponse.ClientInfo).ToAccountIdentifier();

            // Act
            MsalAccessTokenCacheItem at = new MsalAccessTokenCacheItem(
                TestConstants.ProductionPrefNetworkEnvironment,
                TestConstants.ClientId,
                tokenResponse,
                TestConstants.TenantId,
                homeAccountId,
                keyId: "kid1");

            // Assert
            Assert.AreEqual("kid1", at.KeyId);
            CoreAssert.AreEqual(tokenResponse.TokenType, at.TokenType, "pop");
        }

        [TestMethod]
        public void AccessToken_WithNoRefresh_FromMsalResponseJson()
        {
            // Arrange
            string json = TestConstants.TokenResponseJson;
            var tokenResponse = JsonHelper.DeserializeFromJson<MsalTokenResponse>(json);
            var homeAccountId = ClientInfo.CreateFromJson(tokenResponse.ClientInfo).ToAccountIdentifier();

            // Act
            MsalAccessTokenCacheItem at = new MsalAccessTokenCacheItem(
                TestConstants.ProductionPrefNetworkEnvironment,
                TestConstants.ClientId,
                tokenResponse,
                homeAccountId,
                TestConstants.TenantId);

            // Assert
            Assert.IsNull(tokenResponse.RefreshIn);
            Assert.IsFalse(at.RefreshOn.HasValue);
        }

        [TestMethod]
        public void GetExpiredAccessTokenTest()
        {
            using (var harness = CreateTestHarness())
            {
                ITokenCacheInternal cache = new TokenCache(harness.ServiceBundle, false);

                var atItem = new MsalAccessTokenCacheItem(
                    TestConstants.ProductionPrefNetworkEnvironment,
                    TestConstants.ClientId,
                    TestConstants.s_scope.AsSingleString(),
                    TestConstants.Utid,
                    null,
                    DateTimeOffset.UtcNow - TimeSpan.FromMinutes(30),
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow + TimeSpan.FromHours(2),
                    _clientInfo,
                    _homeAccountId);

                cache.Accessor.SaveAccessToken(atItem);

                var param = harness.CreateAuthenticationRequestParameters(
                    TestConstants.AuthorityTestTenant,
                    new SortedSet<string>(),
                    cache,
                    account: new Account(TestConstants.s_userIdentifier, TestConstants.DisplayableId, null));

                Assert.IsNull(cache.FindAccessTokenAsync(param).Result);
            }
        }

        [TestMethod]
        // Regression test for https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1806
        public void GetInvalidExpirationAccessTokenTest()
        {
            using (var harness = CreateTestHarness())
            {
                ITokenCacheInternal cache = new TokenCache(harness.ServiceBundle, false);

                var atItem = new MsalAccessTokenCacheItem(
                    TestConstants.ProductionPrefNetworkEnvironment,
                    TestConstants.ClientId,
                    TestConstants.s_scope.AsSingleString(),
                    TestConstants.Utid,
                    null,
                    cachedAt: DateTimeOffset.UtcNow,
                    expiresOn: new DateTimeOffset(
                        DateTime.UtcNow +
                        TimeSpan.FromDays(TokenCache.ExpirationTooLongInDays) +
                        TimeSpan.FromMinutes(5)),
                    extendedExpiresOn: DateTimeOffset.UtcNow,
                    _clientInfo,
                    _homeAccountId);

                atItem.Secret = atItem.CacheKey;
                cache.Accessor.SaveAccessToken(atItem);

                var param = harness.CreateAuthenticationRequestParameters(
                    TestConstants.AuthorityTestTenant,
                    new SortedSet<string>(),
                    cache,
                    account: new Account(TestConstants.s_userIdentifier, TestConstants.DisplayableId, null));

                Assert.IsNull(cache.FindAccessTokenAsync(param).Result);
            }
        }

        [TestMethod]
        public void GetExpiredAccessToken_WithExtendedExpireStillValid_Test()
        {
            using (var harness = CreateTestHarness(isExtendedTokenLifetimeEnabled: true))
            {
                ITokenCacheInternal cache = new TokenCache(harness.ServiceBundle, false);

                var atItem = new MsalAccessTokenCacheItem(
                    TestConstants.ProductionPrefNetworkEnvironment,
                    TestConstants.ClientId,
                    TestConstants.s_scope.AsSingleString(),
                    TestConstants.Utid,
                    null,
                    new DateTimeOffset(DateTime.UtcNow),
                    new DateTimeOffset(DateTime.UtcNow),
                    new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromHours(2)),
                    _clientInfo,
                    _homeAccountId);

                atItem.Secret = atItem.CacheKey;
                cache.Accessor.SaveAccessToken(atItem);

                var param = harness.CreateAuthenticationRequestParameters(
                    TestConstants.AuthorityTestTenant,
                    new SortedSet<string>(),
                    cache,
                    account: new Account(TestConstants.s_userIdentifier, TestConstants.DisplayableId, null));

                var cacheItem = cache.FindAccessTokenAsync(param).Result;

                Assert.IsNotNull(cacheItem);
                Assert.AreEqual(atItem.CacheKey, cacheItem.CacheKey);
                Assert.IsTrue(cacheItem.IsExtendedLifeTimeToken);
            }
        }

        [TestMethod]
        [TestCategory(TestCategories.TokenCacheTests)]
        public void GetAccessTokenExpiryInRangeTest()
        {
            using (var harness = CreateTestHarness())
            {
                ITokenCacheInternal cache = new TokenCache(harness.ServiceBundle, false);

                var atItem = new MsalAccessTokenCacheItem(
                    TestConstants.ProductionPrefNetworkEnvironment,
                    TestConstants.ClientId,
                    TestConstants.s_scope.AsSingleString(),
                    TestConstants.Utid,
                    "",
                    new DateTimeOffset(DateTime.UtcNow),
                    new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromMinutes(4)),
                    new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromHours(2)),
                    _clientInfo,
                    _homeAccountId);

                atItem.Secret = atItem.CacheKey;
                cache.Accessor.SaveAccessToken(atItem);

                var param = harness.CreateAuthenticationRequestParameters(
                    TestConstants.AuthorityTestTenant,
                    new SortedSet<string>(),
                    cache,
                    account: new Account(TestConstants.s_userIdentifier, TestConstants.DisplayableId, null));

                Assert.IsNull(cache.FindAccessTokenAsync(param).Result);
            }
        }

        [TestMethod]
        // regression for https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3130
        public void ExpiryNoTokens()
        {
            using (var harness = CreateTestHarness())
            {
                // Arrange
                ITokenCacheInternal appTokenCache = new TokenCache(harness.ServiceBundle, true);
                ITokenCacheInternal userTokenCache = new TokenCache(harness.ServiceBundle, false);
                var logger = Substitute.For<ILoggerAdapter>();

                // Act
                var appAccessorExpiration = TokenCache.CalculateSuggestedCacheExpiry(appTokenCache.Accessor, logger);
                var userAccessorExpiration = TokenCache.CalculateSuggestedCacheExpiry(userTokenCache.Accessor, logger);

                // Assert
                Assert.IsNull(appAccessorExpiration);
                Assert.IsNull(userAccessorExpiration);
                Assert.IsFalse(appTokenCache.Accessor.HasAccessOrRefreshTokens());
                Assert.IsFalse(userTokenCache.Accessor.HasAccessOrRefreshTokens());
            }
        }

        [TestMethod]
        // regression for https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3130
        public void TokensCloseToExpiry_NoTokens()
        {
            using (var harness = CreateTestHarness())
            {
                // Arrange
                ITokenCacheInternal appTokenCache = new TokenCache(harness.ServiceBundle, true);
                ITokenCacheInternal userTokenCache = new TokenCache(harness.ServiceBundle, false);
                var logger = Substitute.For<ILoggerAdapter>();

                var t1 = TokenCacheHelper.CreateAccessTokenItem(isExpired: true);
                var t2 = TokenCacheHelper.CreateAccessTokenItem(isExpired: true);
                // token that expires in less than 5 min and is seen as expired by msal
                var t3 = TokenCacheHelper.CreateAccessTokenItem(exiresIn: Constants.AccessTokenExpirationBuffer - TimeSpan.FromSeconds(1));

                appTokenCache.Accessor.SaveAccessToken(t1);
                appTokenCache.Accessor.SaveAccessToken(t2);
                appTokenCache.Accessor.SaveAccessToken(t3);
                userTokenCache.Accessor.SaveAccessToken(t1);
                userTokenCache.Accessor.SaveAccessToken(t2);
                userTokenCache.Accessor.SaveAccessToken(t3);

                // Act
                var appAccessorExpiration = TokenCache.CalculateSuggestedCacheExpiry(appTokenCache.Accessor, logger);
                var userAccessorExpiration = TokenCache.CalculateSuggestedCacheExpiry(userTokenCache.Accessor, logger);

                // Assert
                Assert.IsNull(appAccessorExpiration);
                Assert.IsNull(userAccessorExpiration);
                Assert.IsFalse(appTokenCache.Accessor.HasAccessOrRefreshTokens());
                Assert.IsFalse(userTokenCache.Accessor.HasAccessOrRefreshTokens());

                // Arrange - token that is not seen as expired
                var t4 = TokenCacheHelper.CreateAccessTokenItem(exiresIn: Constants.AccessTokenExpirationBuffer + TimeSpan.FromMinutes(1));
                appTokenCache.Accessor.SaveAccessToken(t4);
                userTokenCache.Accessor.SaveAccessToken(t4);

                // Act
                appAccessorExpiration = TokenCache.CalculateSuggestedCacheExpiry(appTokenCache.Accessor, logger);
                userAccessorExpiration = TokenCache.CalculateSuggestedCacheExpiry(userTokenCache.Accessor, logger);

                // Assert
                CoreAssert.IsWithinRange(t4.ExpiresOn, appAccessorExpiration.Value, TimeSpan.FromSeconds(3));
                CoreAssert.IsWithinRange(t4.ExpiresOn, userAccessorExpiration.Value, TimeSpan.FromSeconds(3));
                Assert.IsTrue(appTokenCache.Accessor.HasAccessOrRefreshTokens());
                Assert.IsTrue(userTokenCache.Accessor.HasAccessOrRefreshTokens());

            }
        }

        [TestMethod]
        [TestCategory(TestCategories.TokenCacheTests)]
        public void GetRefreshTokenTest()
        {
            using (var harness = CreateTestHarness())
            {
                ITokenCacheInternal cache = new TokenCache(harness.ServiceBundle, false);

                var rtItem = new MsalRefreshTokenCacheItem(
                    TestConstants.ProductionPrefNetworkEnvironment,
                    TestConstants.ClientId,
                    "someRT",
                    _clientInfo,
                    null,
                    _homeAccountId);

                string rtKey = rtItem.CacheKey;
                cache.Accessor.SaveRefreshToken(rtItem);

                var authParams = harness.CreateAuthenticationRequestParameters(
                    TestConstants.AuthorityTestTenant,
                    TestConstants.s_scope,
                    cache,
                    account: TestConstants.s_user);

                Assert.IsNotNull(cache.FindRefreshTokenAsync(authParams));

                // RT is stored by environment, client id and userIdentifier as index.
                // any change to authority (within same environment), uniqueid and displyableid will not
                // change the outcome of cache look up.
                Assert.IsNotNull(cache.FindRefreshTokenAsync(
                                     harness.CreateAuthenticationRequestParameters(
                                         TestConstants.AuthorityHomeTenant + "more",
                                         TestConstants.s_scope,
                                         cache,
                                         account: TestConstants.s_user)));
            }
        }

        [TestMethod]
        public void GetRefreshTokenDifferentEnvironmentTest()
        {
            using (var harness = CreateTestHarness())
            {
                ITokenCacheInternal cache = new TokenCache(harness.ServiceBundle, false);
                var rtItem = new MsalRefreshTokenCacheItem(
                    TestConstants.SovereignNetworkEnvironmentDE,
                    TestConstants.ClientId,
                    "someRT",
                    _clientInfo,
                    null,
                    _homeAccountId);

                string rtKey = rtItem.CacheKey;
                cache.Accessor.SaveRefreshToken(rtItem);

                var authParams = harness.CreateAuthenticationRequestParameters(
                    TestConstants.AuthorityTestTenant,
                    TestConstants.s_scope,
                    cache,
                    account: TestConstants.s_user);

                var rt = cache.FindRefreshTokenAsync(authParams).Result;
                Assert.IsNull(rt);
            }
        }

        [TestMethod]
        [TestCategory(TestCategories.TokenCacheTests)]
        public async Task GetAppTokenFromCacheTestAsync()
        {
            using (var harness = CreateTestHarness())
            {
                ITokenCacheInternal cache = new TokenCache(harness.ServiceBundle, true);

                var atItem = new MsalAccessTokenCacheItem(
                    TestConstants.ProductionPrefNetworkEnvironment,
                    TestConstants.ClientId,
                    TestConstants.s_scope.AsSingleString(),
                    TestConstants.Utid,
                    null,
                    new DateTimeOffset(DateTime.UtcNow),
                    new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)),
                    new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExtendedExpiresIn)),
                    _clientInfo,
                    _homeAccountId);

                string atKey = atItem.CacheKey;
                atItem.Secret = atKey;

                cache.Accessor.SaveAccessToken(atItem);

                var authParams = harness.CreateAuthenticationRequestParameters(
                    TestConstants.AuthorityTestTenant,
                    TestConstants.s_scope,
                    cache,
                    apiId: ApiEvent.ApiIds.AcquireTokenForClient);

                var cacheItem = await cache.FindAccessTokenAsync(authParams).ConfigureAwait(false);

                Assert.IsNotNull(cacheItem);
                Assert.AreEqual(atItem.CacheKey, cacheItem.CacheKey);
            }
        }

        [TestMethod]
        [TestCategory(TestCategories.TokenCacheTests)]
        public async Task DoNotSaveRefreshTokenInAdalCacheForMsalB2CAuthorityTestAsync()
        {
            var appConfig = new ApplicationConfiguration(MsalClientType.ConfidentialClient)
            {
                ClientId = TestConstants.ClientId,
                RedirectUri = TestConstants.RedirectUri,
                Authority = Authority.CreateAuthority(TestConstants.B2CAuthority, false)
            };

            var serviceBundle = ServiceBundle.Create(appConfig);
            ITokenCacheInternal cache = new TokenCache(serviceBundle, false);

            MsalTokenResponse response = TestConstants.CreateMsalTokenResponse();

            var authority = Authority.CreateAuthority(TestConstants.B2CAuthority);
            authority = Authority.CreateAuthorityWithTenant(
                authority.AuthorityInfo,
                TestConstants.Utid);

            var requestParams = TestCommon.CreateAuthenticationRequestParameters(
                serviceBundle,
                authority);

            AddHostToInstanceCache(serviceBundle, TestConstants.ProductionPrefNetworkEnvironment);

            await cache.SaveTokenResponseAsync(requestParams, response).ConfigureAwait(false);

            Assert.AreEqual(1, cache.Accessor.GetAllRefreshTokens().Count());
            Assert.AreEqual(1, cache.Accessor.GetAllAccessTokens().Count());

            IDictionary<AdalTokenCacheKey, AdalResultWrapper> dictionary =
                AdalCacheOperations.Deserialize(serviceBundle.ApplicationLogger, cache.LegacyPersistence.LoadCache());
            cache.LegacyPersistence.WriteCache(AdalCacheOperations.Serialize(serviceBundle.ApplicationLogger, dictionary));

            // ADAL cache is empty because B2C scenario is only for MSAL
            Assert.AreEqual(0, dictionary.Count);
        }

        [TestMethod]
        [TestCategory(TestCategories.TokenCacheTests)]
        public void GetAccessAndRefreshTokenNoUserAssertionInCacheTest()
        {
            using (var harness = CreateTestHarness())
            {
                ITokenCacheInternal cache = new TokenCache(harness.ServiceBundle, false);

                var atItem = new MsalAccessTokenCacheItem(
                    TestConstants.ProductionPrefNetworkEnvironment,
                    TestConstants.ClientId,
                    TestConstants.s_scope.AsSingleString(),
                    TestConstants.Utid,
                    null,
                    new DateTimeOffset(DateTime.UtcNow),
                    new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromHours(1)),
                    new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromHours(2)),
                    _clientInfo,
                    _homeAccountId);

                // create key out of access token cache item and then
                // set it as the value of the access token.
                string atKey = atItem.CacheKey;
                atItem.Secret = atKey;
                cache.Accessor.SaveAccessToken(atItem);

                var rtItem = new MsalRefreshTokenCacheItem(
                    TestConstants.ProductionPrefNetworkEnvironment,
                    TestConstants.ClientId,
                    null,
                    _clientInfo,
                    null,
                    _homeAccountId);

                string rtKey = rtItem.CacheKey;
                rtItem.Secret = rtKey;
                cache.Accessor.SaveRefreshToken(rtItem);

                var authParams = harness.CreateAuthenticationRequestParameters(
                    TestConstants.AuthorityTestTenant,
                    TestConstants.s_scope,
                    cache,
                    apiId: ApiEvent.ApiIds.AcquireTokenOnBehalfOf);
                authParams.UserAssertion = new UserAssertion(
                    harness.ServiceBundle.PlatformProxy.CryptographyManager.CreateBase64UrlEncodedSha256Hash(atKey));

                var item = cache.FindAccessTokenAsync(authParams).Result;

                // cache lookup should fail because there was no userassertion hash in the matched
                // token cache item.

                Assert.IsNull(item);
            }
        }

        [TestMethod]
        [TestCategory(TestCategories.TokenCacheTests)]
        public void GetAccessAndRefreshTokenUserAssertionMismatchInCacheTest()
        {
            using (var harness = CreateTestHarness())
            {
                ITokenCacheInternal cache = new TokenCache(harness.ServiceBundle, false);
                string assertion = harness.ServiceBundle.PlatformProxy.CryptographyManager.CreateBase64UrlEncodedSha256Hash("T");

                var atItem = new MsalAccessTokenCacheItem(
                    TestConstants.ProductionPrefNetworkEnvironment,
                    TestConstants.ClientId,
                    TestConstants.s_scope.AsSingleString(),
                    TestConstants.Utid,
                    null,
                    new DateTimeOffset(DateTime.UtcNow),
                    new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromHours(1)),
                    new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromHours(2)),
                    _clientInfo,
                    _homeAccountId,
                    oboCacheKey: assertion);

                cache.Accessor.SaveAccessToken(atItem);

                var rtItem = new MsalRefreshTokenCacheItem(
                    TestConstants.ProductionPrefNetworkEnvironment,
                    TestConstants.ClientId,
                    null,
                    _clientInfo,
                    null,
                    _homeAccountId);

                string rtKey = rtItem.CacheKey;
                rtItem.Secret = rtKey;
                rtItem.OboCacheKey = assertion;
                cache.Accessor.SaveRefreshToken(rtItem);

                var authParams = harness.CreateAuthenticationRequestParameters(
                    TestConstants.AuthorityTestTenant,
                    TestConstants.s_scope,
                    cache,
                    apiId: ApiEvent.ApiIds.AcquireTokenOnBehalfOf);
                authParams.UserAssertion = new UserAssertion(atItem.OboCacheKey + "-random");

                var itemAT = cache.FindAccessTokenAsync(authParams).Result;
                var itemRT = cache.FindRefreshTokenAsync(authParams).Result;

                // cache lookup should fail because there was user assertion hash did not match the one
                // stored in token cache item.
                Assert.IsNull(itemAT);
                Assert.IsNull(itemRT);
            }
        }

        [TestMethod]
        [TestCategory(TestCategories.TokenCacheTests)]
        public void GetAccessAndRefreshTokenMatchedUserAssertionInCacheTest()
        {
            using (var harness = CreateTestHarness())
            {
                ITokenCacheInternal cache = new TokenCache(harness.ServiceBundle, false);
                string assertionHash = harness.ServiceBundle.PlatformProxy.CryptographyManager.CreateBase64UrlEncodedSha256Hash("T");
                var atItem = new MsalAccessTokenCacheItem(
                    TestConstants.ProductionPrefNetworkEnvironment,
                    TestConstants.ClientId,
                    TestConstants.s_scope.AsSingleString(),
                    TestConstants.Utid,
                    null,
                    new DateTimeOffset(DateTime.UtcNow),
                    new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromHours(1)),
                    new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromHours(2)),
                    _clientInfo,
                    _homeAccountId,
                    oboCacheKey: assertionHash);

                cache.Accessor.SaveAccessToken(atItem);

                var rtItem = new MsalRefreshTokenCacheItem(
                    TestConstants.ProductionPrefNetworkEnvironment,
                    TestConstants.ClientId,
                    null,
                    _clientInfo,
                    null,
                    _homeAccountId);

                rtItem.OboCacheKey = assertionHash;
                cache.Accessor.SaveRefreshToken(rtItem);

                var authParams = harness.CreateAuthenticationRequestParameters(
                    TestConstants.AuthorityTestTenant,
                    TestConstants.s_scope,
                    cache,
                    apiId: ApiEvent.ApiIds.AcquireTokenOnBehalfOf,
                    account: new Account(_homeAccountId, null, TestConstants.ProductionPrefNetworkEnvironment));
                authParams.UserAssertion = new UserAssertion("T");

                ((TokenCache)cache).AfterAccess = AfterAccessNoChangeNotification;
                var itemAT = cache.FindAccessTokenAsync(authParams).Result;
                var itemRT = cache.FindRefreshTokenAsync(authParams).Result;

                Assert.IsNotNull(itemAT);
                Assert.IsNotNull(itemRT);
            }
        }

        [TestMethod]
        [TestCategory(TestCategories.TokenCacheTests)]
        public async Task SaveAccessAndRefreshTokenWithEmptyCacheTestAsync()
        {
            var serviceBundle = TestCommon.CreateDefaultServiceBundle();
            ITokenCacheInternal cache = new TokenCache(serviceBundle, false);

            MsalTokenResponse response = TestConstants.CreateMsalTokenResponse();

            var requestParams = TestCommon.CreateAuthenticationRequestParameters(serviceBundle);
            requestParams.AuthorityManager = new AuthorityManager(
                requestParams.RequestContext,
                Authority.CreateAuthorityWithTenant(
                    requestParams.AuthorityInfo,
                    TestConstants.Utid));

            AddHostToInstanceCache(serviceBundle, TestConstants.ProductionPrefNetworkEnvironment);

            await cache.SaveTokenResponseAsync(requestParams, response).ConfigureAwait(false);

            cache.Accessor.AssertItemCount(
                expectedAtCount: 1,
                expectedRtCount: 1,
                expectedAccountCount: 1,
                expectedIdtCount: 1,
                expectedAppMetadataCount: 1);

            var metadata = cache.Accessor.GetAllAppMetadata().First();
            Assert.AreEqual(TestConstants.ClientId, metadata.ClientId);
            Assert.AreEqual(TestConstants.ProductionPrefNetworkEnvironment, metadata.Environment);
            Assert.IsNull(metadata.FamilyId);
        }

        [TestMethod]
        public async Task NoAppMetadata_WhenFociIsDisabledAsync()
        {
            using (var harness = CreateTestHarness())
            {
                // Arrange
                var testFlags = Substitute.For<IFeatureFlags>();
                testFlags.IsFociEnabled.Returns(false);

                harness.ServiceBundle.PlatformProxy.SetFeatureFlags(testFlags);

                ITokenCacheInternal cache = new TokenCache(harness.ServiceBundle, false);
                MsalTokenResponse response = TestConstants.CreateMsalTokenResponse();
                var requestParams = TestCommon.CreateAuthenticationRequestParameters(harness.ServiceBundle);
                requestParams.AuthorityManager = new AuthorityManager(
                  requestParams.RequestContext,
                  Authority.CreateAuthorityWithTenant(
                      requestParams.AuthorityInfo,
                      TestConstants.Utid));
                AddHostToInstanceCache(harness.ServiceBundle, TestConstants.ProductionPrefNetworkEnvironment);

                // Act
                await cache.SaveTokenResponseAsync(requestParams, response).ConfigureAwait(false);

                // Assert
                cache.Accessor.AssertItemCount(
                    expectedAtCount: 1,
                    expectedRtCount: 1,
                    expectedAccountCount: 1,
                    expectedIdtCount: 1,
                    expectedAppMetadataCount: 0);

                // Don't save RT as an FRT if FOCI is disabled
                Assert.IsTrue(string.IsNullOrEmpty(cache.Accessor.GetAllRefreshTokens().First().FamilyId));
            }
        }

        [TestMethod]
        public async Task SaveMultipleAppmetadataAsync()
        {
            var serviceBundle = TestCommon.CreateDefaultServiceBundle();
            ITokenCacheInternal cache = new TokenCache(serviceBundle, false);

            MsalTokenResponse response = TestConstants.CreateMsalTokenResponse();
            MsalTokenResponse response2 = TestConstants.CreateMsalTokenResponse();
            response2.FamilyId = "1";

            var requestParams = TestCommon.CreateAuthenticationRequestParameters(serviceBundle);
            requestParams.AuthorityManager = new AuthorityManager(
              requestParams.RequestContext,
              Authority.CreateAuthorityWithTenant(
                  requestParams.AuthorityInfo,
                  TestConstants.Utid));
            AddHostToInstanceCache(serviceBundle, TestConstants.ProductionPrefNetworkEnvironment);

            await cache.SaveTokenResponseAsync(requestParams, response).ConfigureAwait(false);
            await cache.SaveTokenResponseAsync(requestParams, response2).ConfigureAwait(false);

            cache.Accessor.AssertItemCount(
                expectedAtCount: 1,
                expectedRtCount: 2, // a normal RT and an FRT
                expectedAccountCount: 1,
                expectedIdtCount: 1,
                expectedAppMetadataCount: 1);

            var metadata = cache.Accessor.GetAllAppMetadata().First();
            Assert.AreEqual(TestConstants.ClientId, metadata.ClientId);
            Assert.AreEqual(TestConstants.ProductionPrefNetworkEnvironment, metadata.Environment);
            Assert.AreEqual(TestConstants.FamilyId, metadata.FamilyId);

            Assert.IsTrue(cache.Accessor.GetAllRefreshTokens().Any(rt => rt.FamilyId == "1"));
            Assert.IsTrue(cache.Accessor.GetAllRefreshTokens().Any(rt => string.IsNullOrEmpty(rt.FamilyId)));
        }

        [TestMethod]
        public void CreateFrtFromTokenResponse()
        {
            MsalTokenResponse response = TestConstants.CreateMsalTokenResponse();
            response.FamilyId = "1";
            var homeAccountId = ClientInfo.CreateFromJson(response.ClientInfo).ToAccountIdentifier();

            var frt = new MsalRefreshTokenCacheItem(
                "env",
                TestConstants.ClientId,
                response,
                homeAccountId);

            Assert.AreEqual("1", frt.FamilyId);
        }

        [TestMethod]
        [TestCategory(TestCategories.TokenCacheTests)]
        public async Task SaveAccessAndRefreshTokenWithMoreScopesTestAsync()
        {
            var serviceBundle = TestCommon.CreateDefaultServiceBundle();
            ITokenCacheInternal cache = new TokenCache(serviceBundle, false);
            MsalTokenResponse response = TestConstants.CreateMsalTokenResponse();

            var requestParams = TestCommon.CreateAuthenticationRequestParameters(serviceBundle);
            requestParams.AuthorityManager = new AuthorityManager(
              requestParams.RequestContext,
              Authority.CreateAuthorityWithTenant(
                  requestParams.AuthorityInfo,
                  TestConstants.Utid));
            AddHostToInstanceCache(serviceBundle, TestConstants.ProductionPrefNetworkEnvironment);

            await cache.SaveTokenResponseAsync(requestParams, response).ConfigureAwait(false);

            Assert.AreEqual(1, cache.Accessor.GetAllRefreshTokens().Count());
            Assert.AreEqual(1, cache.Accessor.GetAllAccessTokens().Count());

            Assert.IsNull(cache.Accessor.GetAllRefreshTokens().First().FamilyId);

            response = TestConstants.CreateMsalTokenResponse();
            response.Scope = TestConstants.s_scope.AsSingleString() + " another-scope";
            response.AccessToken = "access-token-2";
            response.RefreshToken = "refresh-token-2";

            await cache.SaveTokenResponseAsync(requestParams, response).ConfigureAwait(false);

            Assert.AreEqual(1, cache.Accessor.GetAllRefreshTokens().Count());
            Assert.AreEqual(1, cache.Accessor.GetAllAccessTokens().Count());

            Assert.AreEqual("refresh-token-2", (cache.Accessor.GetAllRefreshTokens()).First().Secret);
            Assert.AreEqual("access-token-2", (cache.Accessor.GetAllAccessTokens()).First().Secret);
        }

        [TestMethod]
        [TestCategory(TestCategories.TokenCacheTests)]
        public async Task SaveAccessAndRefreshTokenWithLessScopesTestAsync()
        {
            var serviceBundle = TestCommon.CreateDefaultServiceBundle();
            ITokenCacheInternal cache = new TokenCache(serviceBundle, false);
            MsalTokenResponse response = TestConstants.CreateMsalTokenResponse();

            var requestParams = TestCommon.CreateAuthenticationRequestParameters(serviceBundle);
            requestParams.AuthorityManager = new AuthorityManager(
              requestParams.RequestContext,
              Authority.CreateAuthorityWithTenant(
                  requestParams.AuthorityInfo,
                  TestConstants.Utid));
            AddHostToInstanceCache(serviceBundle, TestConstants.ProductionPrefNetworkEnvironment);

            await cache.SaveTokenResponseAsync(requestParams, response).ConfigureAwait(false);

            response = TestConstants.CreateMsalTokenResponse();
            response.Scope = TestConstants.s_scope.First();
            response.AccessToken = "access-token-2";
            response.RefreshToken = "refresh-token-2";

            await cache.SaveTokenResponseAsync(requestParams, response).ConfigureAwait(false);

            Assert.AreEqual(1, cache.Accessor.GetAllRefreshTokens().Count());
            Assert.AreEqual(1, cache.Accessor.GetAllAccessTokens().Count());
            Assert.AreEqual("refresh-token-2", (cache.Accessor.GetAllRefreshTokens()).First().Secret);
            Assert.AreEqual("access-token-2", (cache.Accessor.GetAllAccessTokens()).First().Secret);
        }

        [TestMethod]
        [TestCategory(TestCategories.TokenCacheTests)]
        public async Task SaveAccessAndRefreshTokenWithIntersectingScopesTestAsync()
        {
            var serviceBundle = TestCommon.CreateDefaultServiceBundle();
            ITokenCacheInternal cache = new TokenCache(serviceBundle, false);
            MsalTokenResponse response = TestConstants.CreateMsalTokenResponse();

            var authority = Authority.CreateAuthority(TestConstants.AuthorityUtidTenant);
            var requestParams = TestCommon.CreateAuthenticationRequestParameters(serviceBundle, authority);

            AddHostToInstanceCache(serviceBundle, TestConstants.ProductionPrefNetworkEnvironment);

            await cache.SaveTokenResponseAsync(requestParams, response).ConfigureAwait(false);

            response = TestConstants.CreateMsalTokenResponse();
            response.Scope = TestConstants.s_scope.AsSingleString() + " random-scope";
            response.AccessToken = "access-token-2";
            response.RefreshToken = "refresh-token-2";

            await cache.SaveTokenResponseAsync(requestParams, response).ConfigureAwait(false);

            Assert.AreEqual(1, cache.Accessor.GetAllRefreshTokens().Count());
            Assert.AreEqual(1, cache.Accessor.GetAllAccessTokens().Count());

            Assert.AreEqual("refresh-token-2", (cache.Accessor.GetAllRefreshTokens()).First().Secret);
            Assert.AreEqual("access-token-2", (cache.Accessor.GetAllAccessTokens()).First().Secret);
        }

        [TestMethod]
        [TestCategory(TestCategories.TokenCacheTests)]
        public void CacheAdfsTokenTest()
        {
            using (var harness = CreateTestHarness())
            {
                var serviceBundle = harness.ServiceBundle;
                ITokenCacheInternal adfsCache = new TokenCache(serviceBundle, false);
                var authority = Authority.CreateAuthority(TestConstants.OnPremiseAuthority);

                MsalTokenResponse response = new MsalTokenResponse();

                response.IdToken = MockHelpers.CreateIdToken(string.Empty, TestConstants.FabrikamDisplayableId, null);
                response.ClientInfo = null;
                response.AccessToken = "access-token";
                response.ExpiresIn = 3599;
                response.CorrelationId = "correlation-id";
                response.RefreshToken = "refresh-token";
                response.Scope = TestConstants.s_scope.AsSingleString();
                response.TokenType = "Bearer";

                RequestContext requestContext = new RequestContext(serviceBundle, new Guid());
                var requestParams = TestCommon.CreateAuthenticationRequestParameters(serviceBundle, authority);

                adfsCache.SaveTokenResponseAsync(requestParams, response);

                Assert.AreEqual(1, adfsCache.Accessor.GetAllRefreshTokens().Count());
                Assert.AreEqual(1, adfsCache.Accessor.GetAllAccessTokens().Count());
            }
        }

        private void AfterAccessChangedNotification(TokenCacheNotificationArgs args)
        {
            Assert.IsTrue(args.HasStateChanged);
        }

        private void AfterAccessNoChangeNotification(TokenCacheNotificationArgs args)
        {
            Assert.IsFalse(args.HasStateChanged);
        }

        [TestMethod]
        [TestCategory(TestCategories.TokenCacheTests)]
        public async Task SaveAccessAndRefreshTokenWithDifferentAuthoritySameUserTestAsync()
        {
            var serviceBundle = TestCommon.CreateDefaultServiceBundle();
            ITokenCacheInternal cache = new TokenCache(serviceBundle, false);
            MsalTokenResponse response = TestConstants.CreateMsalTokenResponse("home");

            var requestParams = TestCommon.CreateAuthenticationRequestParameters(
                serviceBundle,
                Authority.CreateAuthority(TestConstants.AuthorityHomeTenant));

            AddHostToInstanceCache(serviceBundle, TestConstants.ProductionPrefNetworkEnvironment);

            await cache.SaveTokenResponseAsync(requestParams, response).ConfigureAwait(false);
            response = TestConstants.CreateMsalTokenResponse("guest");
            response.Scope = TestConstants.s_scope.AsSingleString() + " another-scope";
            response.AccessToken = "access-token-2";
            response.RefreshToken = "refresh-token-2";

            requestParams = TestCommon.CreateAuthenticationRequestParameters(
                serviceBundle,
                Authority.CreateAuthority(TestConstants.AuthorityGuestTenant));

            cache.SetAfterAccess(AfterAccessChangedNotification);
            await cache.SaveTokenResponseAsync(requestParams, response).ConfigureAwait(false);
#pragma warning disable CS0618 // Type or member is obsolete
            Assert.IsFalse(((TokenCache)cache).HasStateChanged);
#pragma warning restore CS0618 // Type or member is obsolete

            Assert.AreEqual(1, cache.Accessor.GetAllRefreshTokens().Count());
            Assert.AreEqual(2, cache.Accessor.GetAllAccessTokens().Count());

            Assert.AreEqual("refresh-token-2", (cache.Accessor.GetAllRefreshTokens()).First().Secret);
        }

        [TestMethod]
        [TestCategory(TestCategories.TokenCacheTests)]
        public void CanDeserializeTokenCacheInNet462()
        {
            var tokenCache = new TokenCache(TestCommon.CreateDefaultServiceBundle(), false)
            {
                AfterAccess = args => { Assert.IsFalse(args.HasStateChanged); }
            };
            ((ITokenCacheSerializer)tokenCache).DeserializeMsalV3(null);
#pragma warning disable CS0618 // Type or member is obsolete
            Assert.IsFalse(tokenCache.HasStateChanged, "State should not have changed when deserializing nothing.");
#pragma warning restore CS0618 // Type or member is obsolete
        }

        [TestMethod]
        [TestCategory(TestCategories.TokenCacheTests)]
        public async Task SerializeDeserializeCacheTestAsync()
        {
            var serviceBundle = TestCommon.CreateDefaultServiceBundle();
            ITokenCacheInternal cache = new TokenCache(serviceBundle, false);

            MsalTokenResponse response = TestConstants.CreateMsalTokenResponse();

            var requestContext = new RequestContext(serviceBundle, Guid.NewGuid());
            var requestParams = TestCommon.CreateAuthenticationRequestParameters(
                serviceBundle,
                authority: Authority.CreateAuthority(TestConstants.AuthorityUtidTenant),
                requestContext: requestContext);
            AddHostToInstanceCache(serviceBundle, TestConstants.ProductionPrefNetworkEnvironment);

            await cache.SaveTokenResponseAsync(requestParams, response).ConfigureAwait(false);
            byte[] serializedCache = ((ITokenCacheSerializer)cache).SerializeMsalV3();

            cache.Accessor.ClearAccessTokens();
            cache.Accessor.ClearRefreshTokens();

            Assert.AreEqual(0, cache.Accessor.GetAllRefreshTokens().Count());
            Assert.AreEqual(0, cache.Accessor.GetAllAccessTokens().Count());

            ((ITokenCacheSerializer)cache).DeserializeMsalV3(serializedCache);

            Assert.AreEqual(1, cache.Accessor.GetAllRefreshTokens().Count());
            Assert.AreEqual(1, cache.Accessor.GetAllAccessTokens().Count());

            serializedCache = ((ITokenCacheSerializer)cache).SerializeMsalV3();
            ((ITokenCacheSerializer)cache).DeserializeMsalV3(serializedCache);
            // item count should not change because old cache entries should have
            // been overriden

            Assert.AreEqual(1, cache.Accessor.GetAllRefreshTokens().Count());
            Assert.AreEqual(1, cache.Accessor.GetAllAccessTokens().Count());

            var atItem = (cache.Accessor.GetAllAccessTokens()).First();
            Assert.AreEqual(response.AccessToken, atItem.Secret);
            Assert.AreEqual(TestConstants.Utid, atItem.TenantId);
            Assert.AreEqual(TestConstants.ProductionPrefNetworkEnvironment, atItem.Environment);
            Assert.AreEqual(TestConstants.ClientId, atItem.ClientId);
            Assert.AreEqual(response.Scope, atItem.ScopeSet.AsSingleString());

            // todo add test for idToken serialization
            // Assert.AreEqual(response.IdToken, atItem.RawIdToken);

            var rtItem = (cache.Accessor.GetAllRefreshTokens()).First();
            Assert.AreEqual(response.RefreshToken, rtItem.Secret);
            Assert.AreEqual(TestConstants.ClientId, rtItem.ClientId);
            Assert.AreEqual(TestConstants.s_userIdentifier, rtItem.HomeAccountId);
            Assert.AreEqual(TestConstants.ProductionPrefNetworkEnvironment, rtItem.Environment);
        }

        [TestMethod]
        [TestCategory(TestCategories.TokenCacheTests)]
        public async Task SerializeDeserializeCache_ClearCacheTrueWithNoSerializedCache_TestAsync()
        {
            var serviceBundle = TestCommon.CreateDefaultServiceBundle();
            ITokenCacheInternal cache = new TokenCache(serviceBundle, false);

            MsalTokenResponse response = TestConstants.CreateMsalTokenResponse();

            var requestContext = new RequestContext(serviceBundle, Guid.NewGuid());
            var requestParams = TestCommon.CreateAuthenticationRequestParameters(
                 serviceBundle,
                 authority: Authority.CreateAuthority(TestConstants.AuthorityUtidTenant),
                 requestContext: requestContext);

            AddHostToInstanceCache(serviceBundle, TestConstants.ProductionPrefNetworkEnvironment);

            await cache.SaveTokenResponseAsync(requestParams, response).ConfigureAwait(false);
            byte[] serializedCache = ((ITokenCacheSerializer)cache).SerializeMsalV3();

            Assert.AreEqual(1, cache.Accessor.GetAllRefreshTokens().Count());
            Assert.AreEqual(1, cache.Accessor.GetAllAccessTokens().Count());

            ((ITokenCacheSerializer)cache).DeserializeMsalV3(serializedCache);

            Assert.AreEqual(1, cache.Accessor.GetAllRefreshTokens().Count());
            Assert.AreEqual(1, cache.Accessor.GetAllAccessTokens().Count());

            ((ITokenCacheSerializer)cache).DeserializeMsalV3(null, true);

            Assert.AreEqual(0, cache.Accessor.GetAllRefreshTokens().Count());
            Assert.AreEqual(0, cache.Accessor.GetAllAccessTokens().Count());
        }

        [TestMethod]
        [TestCategory(TestCategories.TokenCacheTests)]
        public void FindAccessToken_ScopeCaseInsensitive()
        {
            var serviceBundle = TestCommon.CreateDefaultServiceBundle();
            ITokenCacheInternal cache = new TokenCache(serviceBundle, false);

            TokenCacheHelper.PopulateCacheWithOneAccessToken(cache.Accessor);

            var requestParams = TestCommon.CreateAuthenticationRequestParameters(
                serviceBundle,
                scopes: new HashSet<string>());
            requestParams.Account = TestConstants.s_user;
            requestParams.RequestContext.ApiEvent = new ApiEvent(Guid.NewGuid());

            string scopeInCache = TestConstants.s_scope.FirstOrDefault();

            string upperCaseScope = scopeInCache.ToUpperInvariant();
            requestParams.Scope.Add(upperCaseScope);

            var item = cache.FindAccessTokenAsync(requestParams).Result;

            Assert.IsNotNull(item);
            Assert.IsTrue(item.ScopeSet.Contains(scopeInCache));
        }

        [TestMethod]
        [TestCategory(TestCategories.TokenCacheTests)]
        public void CacheB2CTokenTest()
        {
            using (var harness = CreateTestHarness())
            {
                ITokenCacheInternal cache = new TokenCache(harness.ServiceBundle, false);

                string tenantID = "someTenantID";
                Authority authority = Authority.CreateAuthority(
                    $"https://login.microsoftonline.com/tfp/{tenantID}/somePolicy/oauth2/v2.0/authorize");

                // creating IDToken with empty tenantID and displayableID/PreferredUserName for B2C scenario
                MsalTokenResponse response = TestConstants.CreateMsalTokenResponse();

                var requestContext = new RequestContext(harness.ServiceBundle, Guid.NewGuid());

                var requestParams = TestCommon.CreateAuthenticationRequestParameters(
                  harness.ServiceBundle,
                  authority: authority,
                  requestContext: requestContext);

                cache.SaveTokenResponseAsync(requestParams, response);

                Assert.AreEqual(1, cache.Accessor.GetAllRefreshTokens().Count());
                Assert.AreEqual(1, cache.Accessor.GetAllAccessTokens().Count());
            }
        }

        [TestMethod]
        public void TestIsFociMember()
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                ITokenCacheInternal cache = new TokenCache(harness.ServiceBundle, false);
                AuthenticationRequestParameters requestParams = harness.CreateAuthenticationRequestParameters(
                    TestConstants.AuthorityTestTenant,
                    TestConstants.s_scope,
                    cache,
                    account: TestConstants.s_user);

                // Act
                bool? result = cache.IsFociMemberAsync(requestParams, "1").Result;

                // Assert
                Assert.IsNull(result, "No app metadata, should return null which indicates <uknown>");

                ValidateIsFociMember(cache, requestParams,
                    metadataFamilyId: "1",
                    expectedResult: true, // checks for familyId "1"
                    errMessage: "Valid app metadata, should return true because family Id matches");

                ValidateIsFociMember(cache, requestParams,
                    metadataFamilyId: "2",
                    expectedResult: false, // checks for familyId "1"
                    errMessage: "Valid app metadata, should return false because family Id does not match");

                ValidateIsFociMember(cache, requestParams,
                    metadataFamilyId: null,
                    expectedResult: false, // checks for familyId "1"
                    errMessage: "Valid app metadata showing that the app is not member of any family");
            }
        }

        [TestMethod]
        public void TestIsFociMember_EnvAlias()
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                ITokenCacheInternal cache = new TokenCache(harness.ServiceBundle, false);
                AuthenticationRequestParameters requestParams = harness.CreateAuthenticationRequestParameters(
                    TestConstants.AuthorityTestTenant,
                    TestConstants.s_scope,
                    cache,
                    account: TestConstants.s_user);

                cache.Accessor.SaveAppMetadata(
                    new MsalAppMetadataCacheItem(
                        TestConstants.ClientId,
                        TestConstants.ProductionNotPrefEnvironmentAlias,
                        "1"));

                // Act
                bool? result = cache.IsFociMemberAsync(requestParams, "1").Result; //request params uses ProductionPrefEnvAlias

                // Assert
                Assert.AreEqual(true, result.Value);
            }
        }

        [TestMethod]
        [TestCategory(TestCategories.TokenCacheTests)]
        public async Task ValidateTokenCacheContentsAreLogged_TestAsync()
        {
            using MockHttpAndServiceBundle harness = CreateTestHarness();

            //Arrange
            harness.HttpManager.AddInstanceDiscoveryMockHandler();

            string logs = string.Empty;
            LogCallback logCallback = (LogLevel level, string message, bool _) =>
                                    {
                                        if (level == LogLevel.Verbose)
                                        {
                                            logs += message;
                                        }
                                    };

            var serviceBundle = TestCommon.CreateServiceBundleWithCustomHttpManager(harness.HttpManager, logCallback: logCallback);
            ITokenCacheInternal cache = new TokenCache(serviceBundle, false);
            cache.SetAfterAccess((_) => { return; });

            TokenCacheHelper.PopulateCacheWithAccessTokens(cache.Accessor, 11);

            var requestParams = TestCommon.CreateAuthenticationRequestParameters(
                serviceBundle,
                scopes: new HashSet<string>());
            requestParams.Account = TestConstants.s_user;
            requestParams.RequestContext.ApiEvent = new ApiEvent(Guid.NewGuid());

            var response = TokenCacheHelper.CreateMsalTokenResponse(true);

            //Act
            await cache.SaveTokenResponseAsync(requestParams, response).ConfigureAwait(false);

            //Assert
            Assert.IsTrue(logs != string.Empty);
            Assert.IsTrue(logs.Contains("Total number of access tokens in the cache: 12"));
            Assert.IsTrue(logs.Contains("Total number of refresh tokens in the cache: 12"));
            Assert.IsTrue(logs.Contains("First 10 access token cache keys:"));
            Assert.IsTrue(logs.Contains("First 10 refresh token cache keys:"));

            var accessTokens = cache.Accessor.GetAllAccessTokens().ToList();
            var refreshTokens = cache.Accessor.GetAllRefreshTokens().ToList();
            for (int i = 0; i < 10; i++)
            {
                Assert.IsTrue(logs.Contains(accessTokens[i].ToLogString()));
                Assert.IsTrue(logs.Contains(refreshTokens[i].ToLogString()));
            }
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void AccessTokenCacheItem_ToLogString_UsesPiiFlag_Test(bool enablePii)
        {
            var accessTokenCacheItem = TokenCacheHelper.CreateAccessTokenItem();

            var log = accessTokenCacheItem.ToLogString(enablePii);

            Assert.AreEqual(enablePii, log.Contains(accessTokenCacheItem.HomeAccountId));
            Assert.AreNotEqual(enablePii, log.Contains(accessTokenCacheItem.HomeAccountId.GetHashCode().ToString()));
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void RefreshTokenCacheItem_ToLogString_UsesPiiFlag_Test(bool enablePii)
        {
            var refreshTokenCacheItem = TokenCacheHelper.CreateRefreshTokenItem();

            var log = refreshTokenCacheItem.ToLogString(enablePii);

            Assert.AreEqual(enablePii, log.Contains(refreshTokenCacheItem.HomeAccountId));
            Assert.AreNotEqual(enablePii, log.Contains(refreshTokenCacheItem.HomeAccountId.GetHashCode().ToString()));
        }

        private void ValidateIsFociMember(
            ITokenCacheInternal cache,
            AuthenticationRequestParameters requestParams,
            string metadataFamilyId,
            bool? expectedResult,
            string errMessage)
        {
            // Arrange
            var metadata = new MsalAppMetadataCacheItem(TestConstants.ClientId, TestConstants.ProductionPrefCacheEnvironment, metadataFamilyId);
            cache.Accessor.SaveAppMetadata(metadata);

            // Act
            var result = cache.IsFociMemberAsync(requestParams, "1").Result;

            // Assert
            Assert.AreEqual(expectedResult, result, errMessage);
            Assert.AreEqual(1, cache.Accessor.GetAllAppMetadata().Count());
        }

        private void AddHostToInstanceCache(IServiceBundle serviceBundle, string host)
        {
            (serviceBundle.InstanceDiscoveryManager as InstanceDiscoveryManager)
                .AddTestValueToStaticProvider(
                    host,
                    new InstanceDiscoveryMetadataEntry
                    {
                        PreferredNetwork = host,
                        PreferredCache = host,
                        Aliases = new string[]
                        {
                            host
                        }
                    });
        }
    }
}
