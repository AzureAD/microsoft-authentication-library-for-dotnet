// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.


using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class RefreshInTests : TestBase
    {
        private enum TokenResponseType
        {
            Valid,
            Invalid_AADUnavailable,
            Invalid_AADAvailable
        }

        #region AcquireTokenSilent tests

        [TestMethod]
        [Description("AT in cache, needs refresh. AAD responds well to Refresh.")]
        public async Task ATS_NonExpired_NeedsRefresh_ValidResponse_Async()
        {
            // Arrange
            using (MockHttpAndServiceBundle harness = base.CreateTestHarness())
            {
                Trace.WriteLine("1. Setup an app with a token cache with one AT");
                PublicClientApplication app = SetupPca(harness);

                Trace.WriteLine("2. Configure AT so that it shows it needs to be refreshed");
                UpdateATWithRefreshOn(app.UserTokenCacheInternal.Accessor, DateTime.UtcNow - TimeSpan.FromMinutes(1));
                TokenCacheAccessRecorder cacheAccess = app.UserTokenCache.RecordAccess();

                Trace.WriteLine("3. Configure AAD to respond with valid token to the refresh RT flow");
                AddHttpMocks(TokenResponseType.Valid, harness.HttpManager, pca: true);

                // Act
                Trace.WriteLine("4. ATS - should perform an RT refresh");
                var account = new Account(TestConstants.s_userIdentifier, TestConstants.DisplayableId, null);
                AuthenticationResult result = await app
                    .AcquireTokenSilent(
                        TestConstants.s_scope.ToArray(),
                        account)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual(0, harness.HttpManager.QueueSize,
                    "MSAL should have refreshed the token because the original AT was marked for refresh");
                cacheAccess.AssertAccessCounts(1, 1);
                MsalAccessTokenCacheItem ati = app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Single();
                Assert.IsTrue(ati.RefreshOn > DateTime.UtcNow + TimeSpan.FromMinutes(10));

                // New ATS does not refresh the AT because RefreshIn is in the future
                Trace.WriteLine("5. ATS - should fetch from the cache only");
                result = await app
                  .AcquireTokenSilent(
                      TestConstants.s_scope.ToArray(),
                      account)
                  .ExecuteAsync(CancellationToken.None)
                  .ConfigureAwait(false);
                Assert.IsNotNull(result);

                cacheAccess.AssertAccessCounts(2, 1);
            }
        }

        private static PublicClientApplication SetupPca(MockHttpAndServiceBundle harness)
        {
            Trace.WriteLine("1. Setup an app with a token cache with one AT");
            PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                        .WithHttpManager(harness.HttpManager)
                                                                        .BuildConcrete();

            var tokenCacheHelper = new TokenCacheHelper();
            tokenCacheHelper.PopulateCache(app.UserTokenCacheInternal.Accessor, addSecondAt: false);
            return app;
        }

        [TestMethod]
        [Description("AT in cache, needs refresh. AAD in unavaible when refreshing.")]
        public async Task ATS_NonExpired_NeedsRefresh_AADUnavailableResponse_Async()
        {
            // Arrange
            using (MockHttpAndServiceBundle harness = base.CreateTestHarness())
            {
                Trace.WriteLine("1. Setup an app with a token cache with one AT");
                PublicClientApplication app = SetupPca(harness);

                Trace.WriteLine("2. Configure AT so that it shows it needs to be refreshed");
                UpdateATWithRefreshOn(app.UserTokenCacheInternal.Accessor, DateTime.UtcNow - TimeSpan.FromMinutes(1));
                TokenCacheAccessRecorder cacheAccess = app.UserTokenCache.RecordAccess();


                Trace.WriteLine("3. Configure AAD to respond with a 500 error");
                AddHttpMocks(TokenResponseType.Invalid_AADUnavailable, harness.HttpManager, pca: true);

                // Act
                var account = new Account(TestConstants.s_userIdentifier, TestConstants.DisplayableId, null);
                AuthenticationResult result = await app
                    .AcquireTokenSilent(
                        TestConstants.s_scope.ToArray(),
                        account)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(result, "ATS still succeeds even though AAD is unavaible");
                Assert.AreEqual(0, harness.HttpManager.QueueSize);
                cacheAccess.AssertAccessCounts(1, 0); // the refresh failed, no new data is written to the cache

                // Now let AAD respond with tokens
                AddTokenResponse(TokenResponseType.Valid, harness.HttpManager);

                result = await app
                  .AcquireTokenSilent(
                      TestConstants.s_scope.ToArray(),
                      account)
                  .ExecuteAsync(CancellationToken.None)
                  .ConfigureAwait(false);
                Assert.IsNotNull(result);
                cacheAccess.AssertAccessCounts(2, 1); // new tokens written to cache
            }
        }

        [TestMethod]
        [Description("AT in cache, needs refresh. AAD fails but is available when refreshing.")]
        public async Task ATS_NonExpired_NeedsRefresh_AADInvalidResponse_Async()
        {
            // Arrange
            using (MockHttpAndServiceBundle harness = base.CreateTestHarness())
            {
                Trace.WriteLine("1. Setup an app with a token cache with one AT");
                PublicClientApplication app = SetupPca(harness);

                Trace.WriteLine("2. Configure AT so that it shows it needs to be refreshed");
                UpdateATWithRefreshOn(app.UserTokenCacheInternal.Accessor, DateTime.UtcNow - TimeSpan.FromMinutes(1));
                TokenCacheAccessRecorder cacheAccess = app.UserTokenCache.RecordAccess();


                Trace.WriteLine("3. Configure AAD to respond with the typical Invalid Grant error");
                AddHttpMocks(TokenResponseType.Invalid_AADAvailable, harness.HttpManager, pca: true);
                var account = new Account(TestConstants.s_userIdentifier, TestConstants.DisplayableId, null);

                // Act
                await AssertException.TaskThrowsAsync<MsalUiRequiredException>(() => app
                    .AcquireTokenSilent(
                        TestConstants.s_scope.ToArray(),
                        account)
                    .ExecuteAsync())
                    .ConfigureAwait(false);
            }
        }

        [TestMethod]
        [Description("AT in cache, expired. AAD unavailable.")]
        public async Task ATS_Expired_NeedsRefresh_AADInvalidResponse_Async()
        {
            // Arrange
            using (MockHttpAndServiceBundle harness = base.CreateTestHarness())
            {
                Trace.WriteLine("1. Setup an app with a token cache with one AT");
                PublicClientApplication app = SetupPca(harness);

                Trace.WriteLine("2. Configure AT so that it shows it needs to be refreshed, but is also expired");
                UpdateATWithRefreshOn(app.UserTokenCacheInternal.Accessor, DateTime.UtcNow - TimeSpan.FromMinutes(1), true);
                TokenCacheAccessRecorder cacheAccess = app.UserTokenCache.RecordAccess();

                Trace.WriteLine("3. Configure AAD to be unavaiable");
                AddHttpMocks(TokenResponseType.Invalid_AADUnavailable, harness.HttpManager, pca: true);
                var account = new Account(TestConstants.s_userIdentifier, TestConstants.DisplayableId, null);

                // Act
                MsalServiceException ex = await AssertException.TaskThrowsAsync<MsalServiceException>(() => app
                    .AcquireTokenSilent(
                        TestConstants.s_scope.ToArray(),
                        account)
                    .ExecuteAsync())
                    .ConfigureAwait(false);

                Assert.IsFalse(ex is MsalUiRequiredException, "5xx exceptions do not translate to MsalUIRequired");
                Assert.AreEqual(504, ex.StatusCode);
            }
        }
        #endregion

        #region Client Creds

        [TestMethod]
        [Description("AT in cache, needs refresh. AAD responds well to Refresh.")]
        public async Task ClientCreds_NonExpired_NeedsRefresh_ValidResponse_Async()
        {
            // Arrange
            using (MockHttpAndServiceBundle harness = base.CreateTestHarness())
            {
                Trace.WriteLine("1. Setup an app with a token cache with one AT");
                ConfidentialClientApplication app = SetupCca(harness);

                Trace.WriteLine("2. Configure AT so that it shows it needs to be refreshed");
                UpdateATWithRefreshOn(app.AppTokenCacheInternal.Accessor, DateTime.UtcNow - TimeSpan.FromMinutes(1));
                TokenCacheAccessRecorder cacheAccess = app.AppTokenCache.RecordAccess();

                Trace.WriteLine("3. Configure AAD to respond with valid token to the refresh RT flow");
                AddHttpMocks(TokenResponseType.Valid, harness.HttpManager, pca: false);

                // Act
                Trace.WriteLine("4. ATS - should perform an RT refresh");
                AuthenticationResult result = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual(0, harness.HttpManager.QueueSize,
                    "MSAL should have refreshed the token because the original AT was marked for refresh");
                cacheAccess.AssertAccessCounts(1, 1);
            }
        }

        private static ConfidentialClientApplication SetupCca(MockHttpAndServiceBundle harness)
        {
            ConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                          .WithAuthority(AzureCloudInstance.AzurePublic, TestConstants.TenantId)
                                                          .WithClientSecret(TestConstants.ClientSecret)
                                                          .WithHttpManager(harness.HttpManager)
                                                          .BuildConcrete();

            var tokenCacheHelper = new TokenCacheHelper();
            tokenCacheHelper.PopulateCache(app.AppTokenCacheInternal.Accessor, addSecondAt: false);
            return app;
        }

        [TestMethod]
        [Description("AT in cache, needs refresh. AAD in unavaible when refreshing.")]
        public async Task ClientCreds_NonExpired_NeedsRefresh_AADUnavailableResponse_Async()
        {
            // Arrange
            using (MockHttpAndServiceBundle harness = base.CreateTestHarness())
            {
                Trace.WriteLine("1. Setup an app with a token cache with one AT");
                ConfidentialClientApplication app = SetupCca(harness);

                Trace.WriteLine("2. Configure AT so that it shows it needs to be refreshed");
                UpdateATWithRefreshOn(app.AppTokenCacheInternal.Accessor, DateTime.UtcNow - TimeSpan.FromMinutes(1));
                TokenCacheAccessRecorder cacheAccess = app.AppTokenCache.RecordAccess();

                Trace.WriteLine("3. Configure AAD to respond with an error");
                AddHttpMocks(TokenResponseType.Invalid_AADUnavailable, harness.HttpManager, pca: false);

                // Act
                AuthenticationResult result = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(result, "ClientCreds should still succeeds even though AAD is unavaible");
                Assert.AreEqual(0, harness.HttpManager.QueueSize);
                cacheAccess.AssertAccessCounts(1, 0); // the refresh failed, no new data is written to the cache

                // Now let AAD respond with tokens
                AddTokenResponse(TokenResponseType.Valid, harness.HttpManager);

                result = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync()
                    .ConfigureAwait(false);
                Assert.IsNotNull(result);
                cacheAccess.AssertAccessCounts(2, 1); // new tokens written to cache
            }
        }

        [TestMethod]
        public async Task ClientCreds_NonExpired_NeedsRefresh_AADInvalidResponse_Async()
        {
            // Arrange
            using (MockHttpAndServiceBundle harness = base.CreateTestHarness())
            {
                Trace.WriteLine("1. Setup an app with a token cache with one AT");
                ConfidentialClientApplication app = SetupCca(harness);

                Trace.WriteLine("2. Configure AT so that it shows it needs to be refreshed");
                UpdateATWithRefreshOn(app.AppTokenCacheInternal.Accessor, DateTime.UtcNow - TimeSpan.FromMinutes(1));
                TokenCacheAccessRecorder cacheAccess = app.AppTokenCache.RecordAccess();

                Trace.WriteLine("3. Configure AAD to respond with the typical Invalid Grant error");
                AddHttpMocks(TokenResponseType.Invalid_AADAvailable, harness.HttpManager, pca: false);

                // Act
                await AssertException.TaskThrowsAsync<MsalUiRequiredException>(() => 
                   app.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync())
                    .ConfigureAwait(false);
                cacheAccess.AssertAccessCounts(1, 0);
            }
        }

        [TestMethod]
        [Description("AT expiredh. AAD fails but is available when refreshing.")]
        public async Task ClientCreds_Expired_NeedsRefresh_AADInvalidResponse_Async()
        {
            // Arrange
            using (MockHttpAndServiceBundle harness = base.CreateTestHarness())
            {
                Trace.WriteLine("1. Setup an app with a token cache with one AT");
                ConfidentialClientApplication app = SetupCca(harness);

                Trace.WriteLine("2. Configure AT so that it shows it needs to be refreshed, but is also expired");
                UpdateATWithRefreshOn(app.AppTokenCacheInternal.Accessor, DateTime.UtcNow - TimeSpan.FromMinutes(1), true);
                TokenCacheAccessRecorder cacheAccess = app.AppTokenCache.RecordAccess();

                Trace.WriteLine("3. Configure AAD to be unavaiable");
                AddHttpMocks(TokenResponseType.Invalid_AADUnavailable, harness.HttpManager, pca: false);

                // Act
                MsalServiceException ex = await AssertException.TaskThrowsAsync<MsalServiceException>(() => app
                   .AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync())
                    .ConfigureAwait(false);

                Assert.IsFalse(ex is MsalUiRequiredException, "5xx exceptions do not translate to MsalUIRequired");
                Assert.AreEqual(504, ex.StatusCode);
                cacheAccess.AssertAccessCounts(1, 0);
            }
        }

        #endregion

        private static void AddHttpMocks(TokenResponseType aadResponse, MockHttpManager httpManager, bool pca)
        {
            httpManager.AddInstanceDiscoveryMockHandler();
            httpManager.AddMockHandlerForTenantEndpointDiscovery(
                pca ? TestConstants.AuthorityUtidTenant : TestConstants.AadAuthorityWithTestTenantId);

            AddTokenResponse(aadResponse, httpManager);
        }

        private static void AddTokenResponse(TokenResponseType aadResponse, MockHttpManager httpManager)
        {
            switch (aadResponse)
            {
                case TokenResponseType.Valid:
                    var refreshHandler = new MockHttpMessageHandler()
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                       TestConstants.UniqueId,
                       TestConstants.DisplayableId,
                       TestConstants.s_scope.ToArray())
                    };
                    httpManager.AddMockHandler(refreshHandler);
                    break;
                case TokenResponseType.Invalid_AADUnavailable:
                    var refreshHandler1 = new MockHttpMessageHandler()
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateFailureMessage(
                            System.Net.HttpStatusCode.GatewayTimeout, "gateway timeout")
                    };
                    var refreshHandler2 = new MockHttpMessageHandler()
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateFailureMessage(
                           System.Net.HttpStatusCode.GatewayTimeout, "gateway timeout")
                    };

                    // MSAL retries once for errors in the 500 - 600 range
                    httpManager.AddMockHandler(refreshHandler1);
                    httpManager.AddMockHandler(refreshHandler2);
                    break;
                case TokenResponseType.Invalid_AADAvailable:
                    var handler = new MockHttpMessageHandler()
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateInvalidGrantTokenResponseMessage()
                    };

                    httpManager.AddMockHandler(handler);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private static MsalAccessTokenCacheItem UpdateATWithRefreshOn(
            ITokenCacheAccessor accessor,
            DateTimeOffset refreshOn,
            bool expired = false)
        {
            MsalAccessTokenCacheItem atItem = accessor.GetAllAccessTokens().Single();

            // past date on refresh on
            atItem.RefreshOnUnixTimestamp = CoreHelpers.DateTimeToUnixTimestamp(refreshOn);

            Assert.IsTrue(atItem.ExpiresOn > DateTime.UtcNow + TimeSpan.FromMinutes(10));

            if (expired)
            {
                atItem.ExpiresOnUnixTimestamp = CoreHelpers.DateTimeToUnixTimestamp(DateTime.UtcNow - TimeSpan.FromMinutes(1));
            }

            accessor.SaveAccessToken(atItem);

            return atItem;
        }
    }
}
