// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Common.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CacheTests
{
    [TestClass]
    public class UnifiedCacheTests : TestBase
    {
        [TestMethod]
        [Description("Test unified token cache")]
        public void UnifiedCache_MsalStoresToAndReadRtFromAdalCache()
        {
            using (var mocks = new MockHttpAndServiceBundle())
            {
                var httpManager = mocks.HttpManager;
                httpManager.AddInstanceDiscoveryMockHandler();

                var app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                        .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                        .WithHttpManager(httpManager)
                                                        .WithUserTokenLegacyCachePersistenceForTest(
                                                            new TestLegacyCachePersistance())
                                                        .BuildConcrete();
                app.UserTokenCacheInternal.SetBeforeAccess(_ => { });

                app.ServiceBundle.ConfigureMockWebUI(
                    AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));

                HttpResponseMessage response = MockHelpers.CreateSuccessTokenResponseMessageWithUid(
                    TestConstants.Uid,
                    TestConstants.Utid,
                    "testuser@contoso.com");

                httpManager.AddResponseMockHandlerForPost(response);

                AuthenticationResult result = app.AcquireTokenInteractive(TestConstants.s_scope).ExecuteAsync(CancellationToken.None).Result;
                Assert.IsNotNull(result);

                // make sure Msal stored RT in Adal cache
                IDictionary<AdalTokenCacheKey, AdalResultWrapper> adalCacheDictionary =
                    AdalCacheOperations.Deserialize(app.ServiceBundle.ApplicationLogger, app.UserTokenCacheInternal.LegacyPersistence.LoadCache());

                Assert.IsTrue(adalCacheDictionary.Count == 1);

                var requestContext = new RequestContext(app.ServiceBundle, Guid.NewGuid());

                var authority = Microsoft.Identity.Client.Instance.Authority.CreateAuthorityForRequestAsync(
                    requestContext, null).Result;

                AuthenticationRequestParameters reqParams = new AuthenticationRequestParameters(
                    mocks.ServiceBundle,
                    app.UserTokenCacheInternal,
                    new Client.ApiConfig.Parameters.AcquireTokenCommonParameters(),
                    requestContext,
                    authority);

                var accounts = app.UserTokenCacheInternal.GetAccountsAsync(reqParams).Result;
                foreach (IAccount account in accounts)
                {
                    (app.UserTokenCacheInternal as TokenCache).RemoveAccountInternal(account, requestContext);
                }

                Assert.AreEqual(0, httpManager.QueueSize);

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler()
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ExpectedPostData = new Dictionary<string, string>()
                        {
                            {"grant_type", "refresh_token"}
                        },
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                            TestConstants.Uid,
                            TestConstants.DisplayableId,
                            TestConstants.s_scope.ToArray())
                    });

                // Using RT from Adal cache for silent call
                AuthenticationResult result1 = app
                    .AcquireTokenSilent(TestConstants.s_scope, result.Account)
                    .WithTenantId("common")
                    .WithForceRefresh(false)
                    .ExecuteAsync(CancellationToken.None)
                    .Result;

                Assert.IsNotNull(result1);
            }
        }

        [TestMethod]
        [Description("Test that RemoveAccount api is application specific")]
        public void UnifiedCache_RemoveAccountIsApplicationSpecific()
        {
            byte[] data = null;

            using (var httpManager = new MockHttpManager())
            {
                // login to app
                var app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                        .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                        .WithHttpManager(httpManager)
                                                        .BuildConcrete();

                app.UserTokenCache.SetBeforeAccess((TokenCacheNotificationArgs args) =>
                {
                    args.TokenCache.DeserializeMsalV3(data);
                });
                app.UserTokenCache.SetAfterAccess((TokenCacheNotificationArgs args) =>
                {
                    data = args.TokenCache.SerializeMsalV3();
                });

                httpManager.AddInstanceDiscoveryMockHandler();

                app.ServiceBundle.ConfigureMockWebUI(
                    AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));

                httpManager.AddSuccessTokenResponseMockHandlerForPost(ClientApplicationBase.DefaultAuthority);

                AuthenticationResult result = app
                    .AcquireTokenInteractive(TestConstants.s_scope)
                    .ExecuteAsync(CancellationToken.None)
                    .Result;

                Assert.IsNotNull(result);

                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.GetAllAccounts().Count());
                Assert.AreEqual(1, app.GetAccountsAsync().Result.Count());

                // login to app1 with same credentials

                var app1 = PublicClientApplicationBuilder.Create(TestConstants.ClientId2)
                                                         .WithHttpManager(httpManager)
                                                         .WithAuthority(
                                                             new Uri(ClientApplicationBase.DefaultAuthority),
                                                             true).BuildConcrete();

                app1.ServiceBundle.ConfigureMockWebUI(
                    AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));

                app1.UserTokenCache.SetBeforeAccess((TokenCacheNotificationArgs args) =>
                {
                    args.TokenCache.DeserializeMsalV3(data);
                });
                app1.UserTokenCache.SetAfterAccess((TokenCacheNotificationArgs args) =>
                {
                    data = args.TokenCache.SerializeMsalV3();
                });

                httpManager.AddSuccessTokenResponseMockHandlerForPost(ClientApplicationBase.DefaultAuthority);

                result = app1
                    .AcquireTokenInteractive(TestConstants.s_scope)
                    .ExecuteAsync(CancellationToken.None)
                    .Result;

                Assert.IsNotNull(result);

                // make sure that only one account cache entity was created
                Assert.AreEqual(1, app1.GetAccountsAsync().Result.Count());

                Assert.AreEqual(2, app1.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
                Assert.AreEqual(2, app1.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count());
                Assert.AreEqual(2, app1.UserTokenCacheInternal.Accessor.GetAllIdTokens().Count());
                Assert.AreEqual(1, app1.UserTokenCacheInternal.Accessor.GetAllAccounts().Count());

                // remove account from app
                app.RemoveAsync(app.GetAccountsAsync().Result.First()).Wait();

                // make sure account removed from app
                Assert.AreEqual(0, app.GetAccountsAsync().Result.Count());

                // make sure account Not removed from app1
                Assert.AreEqual(1, app1.GetAccountsAsync().Result.Count());
            }
        }

        [TestMethod]
        [Description("Tokens for different resources should not result in multiple entries, because" +
            "refresh tokens are not scope / resource bound.")]
        [TestCategory(TestCategories.Regression)] // https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1815

        public async Task UnifiedCache_ProcessAdalDictionaryForDuplicateKey_Async()
        {
            using (var harness = CreateTestHarness())
            {
                var app = PublicClientApplicationBuilder
                          .Create(TestConstants.ClientId)
                          .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                          .WithUserTokenLegacyCachePersistenceForTest(new TestLegacyCachePersistance())
                          .WithHttpManager(harness.HttpManager)
                          .BuildConcrete();
                app.UserTokenCacheInternal.SetBeforeAccess(_ => { });
                CreateAdalCache(harness.ServiceBundle.ApplicationLogger, app.UserTokenCacheInternal.LegacyPersistence, TestConstants.s_scope.ToString());

                var adalUsers =
                    CacheFallbackOperations.GetAllAdalUsersForMsal(
                        harness.ServiceBundle.ApplicationLogger,
                        app.UserTokenCacheInternal.LegacyPersistence,
                        TestConstants.ClientId);

                CreateAdalCache(harness.ServiceBundle.ApplicationLogger, app.UserTokenCacheInternal.LegacyPersistence, "user.read");

                AdalUsersForMsal adalUsers2 =
                    CacheFallbackOperations.GetAllAdalUsersForMsal(
                        harness.ServiceBundle.ApplicationLogger,
                        app.UserTokenCacheInternal.LegacyPersistence,
                        TestConstants.ClientId);

                Assert.AreEqual(
                    adalUsers.GetUsersWithClientInfo(null).Single().Key,
                    adalUsers2.GetUsersWithClientInfo(null).Single().Key);

                var accounts = await app.GetAccountsAsync().ConfigureAwait(false);

                Assert.AreEqual(1, accounts.Count(), "The 2 RTs belong to the same user");

                // The ADAL cache contains access tokens, but these are NOT usable by MSAL / v2 endpoint. 
                // MSAL will however use the RT from ADAL to fetch new access tokens...
                harness.HttpManager.AddAllMocks(TokenResponseType.Valid_UserFlows);
                var res = await app.AcquireTokenSilent(TestConstants.s_scope, accounts.First()).ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(res);
            }
        }

        [TestMethod]
        [Description("Test for duplicate key in ADAL cache. If ")]
        [TestCategory(TestCategories.Regression)] // https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1815
        public void UnifiedCache_ProcessAdalDictionaryForDuplicateKey_Resources_Test()
        {
            using (var harness = CreateTestHarness())
            {
                var app = PublicClientApplicationBuilder
                          .Create(TestConstants.ClientId)
                          .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                          .WithUserTokenLegacyCachePersistenceForTest(new TestLegacyCachePersistance())
                          .BuildConcrete();

                CreateAdalCache(harness.ServiceBundle.ApplicationLogger, app.UserTokenCacheInternal.LegacyPersistence, TestConstants.s_scope.ToString());

                var adalUsers =
                    CacheFallbackOperations.GetAllAdalUsersForMsal(
                        harness.ServiceBundle.ApplicationLogger,
                        app.UserTokenCacheInternal.LegacyPersistence,
                        TestConstants.ClientId);

                CreateAdalCache(harness.ServiceBundle.ApplicationLogger, app.UserTokenCacheInternal.LegacyPersistence, "user.read");

                var adalUsers2 =
                    CacheFallbackOperations.GetAllAdalUsersForMsal(
                        harness.ServiceBundle.ApplicationLogger,
                        app.UserTokenCacheInternal.LegacyPersistence,
                        TestConstants.ClientId);

                Assert.AreEqual(
                    adalUsers.GetUsersWithClientInfo(null).Single().Key,
                    adalUsers2.GetUsersWithClientInfo(null).Single().Key);

                app.UserTokenCacheInternal.Accessor.ClearAccessTokens();
                app.UserTokenCacheInternal.Accessor.ClearRefreshTokens();
            }
        }

        private void CreateAdalCache(ILoggerAdapter logger, ILegacyCachePersistence legacyCachePersistence, string scopes)
        {
            var key = new AdalTokenCacheKey(
                TestConstants.AuthorityHomeTenant,
                scopes,
                TestConstants.ClientId,
                TestConstants.TokenSubjectTypeUser,
                TestConstants.UniqueId,
                TestConstants.s_user.Username);

            var wrapper = new AdalResultWrapper()
            {
                Result = new AdalResult()
                {
                    UserInfo = new AdalUserInfo()
                    {
                        UniqueId = TestConstants.Uid,
                        DisplayableId = TestConstants.s_user.Username
                    }
                },
                RefreshToken = TestConstants.ClientSecret,
                RawClientInfo = TestConstants.RawClientId,
                ResourceInResponse = scopes
            };

            IDictionary<AdalTokenCacheKey, AdalResultWrapper> dictionary =
                AdalCacheOperations.Deserialize(logger, legacyCachePersistence.LoadCache());
            dictionary[key] = wrapper;
            legacyCachePersistence.WriteCache(AdalCacheOperations.Serialize(logger, dictionary));
        }
    }
}
