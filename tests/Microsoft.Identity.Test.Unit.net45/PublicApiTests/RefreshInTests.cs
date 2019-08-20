// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.


using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Cache.Keys;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.TelemetryCore.Internal.Constants;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

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
            using (var harness = base.CreateTestHarness())
            {
                Trace.WriteLine("1. Setup an app with a token cache with one AT");
                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithHttpManager(harness.HttpManager)
                                                                            .BuildConcrete();

                var tokenCacheHelper = new TokenCacheHelper();
                tokenCacheHelper.PopulateCache(app.UserTokenCacheInternal.Accessor, addSecondAt: false);

                Trace.WriteLine("2. Configure AT so that it shows it needs to be refreshed");
                UpdateATWithRefreshOn(app, DateTime.UtcNow - TimeSpan.FromMinutes(1));
                var cacheAccess = app.UserTokenCache.RecordAccess();

                Trace.WriteLine("3. Configure AAD to respond with valid token to the refresh RT flow");
                AddHttpMocks(TokenResponseType.Valid, harness.HttpManager);

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
                var ati = app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Single();
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
            }
        }

        [TestMethod]
        [Description("AT in cache, needs refresh. AAD in unavaible when refreshing.")]
        public async Task ATS_NonExpired_NeedsRefresh_AADUnavailableResponse_Async()
        {
            // Arrange
            using (var harness = base.CreateTestHarness())
            {
                Trace.WriteLine("1. Setup an app with a token cache with one AT");
                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithHttpManager(harness.HttpManager)
                                                                            .BuildConcrete();

                var tokenCacheHelper = new TokenCacheHelper();
                tokenCacheHelper.PopulateCache(app.UserTokenCacheInternal.Accessor, addSecondAt: false);

                Trace.WriteLine("2. Configure AT so that it shows it needs to be refreshed");
                UpdateATWithRefreshOn(app, DateTime.UtcNow - TimeSpan.FromMinutes(1));
                var cacheAccess = app.UserTokenCache.RecordAccess();


                Trace.WriteLine("3. Configure AAD to respond with a 500 error");
                AddHttpMocks(TokenResponseType.Invalid_AADUnavailable, harness.HttpManager);

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
            using (var harness = base.CreateTestHarness())
            {
                Trace.WriteLine("1. Setup an app with a token cache with one AT");
                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithHttpManager(harness.HttpManager)
                                                                            .BuildConcrete();

                var tokenCacheHelper = new TokenCacheHelper();
                tokenCacheHelper.PopulateCache(app.UserTokenCacheInternal.Accessor, addSecondAt: false);

                Trace.WriteLine("2. Configure AT so that it shows it needs to be refreshed");
                UpdateATWithRefreshOn(app, DateTime.UtcNow - TimeSpan.FromMinutes(1));
                var cacheAccess = app.UserTokenCache.RecordAccess();


                Trace.WriteLine("3. Configure AAD to respond with the typical Invalid Grant error");
                AddHttpMocks(TokenResponseType.Invalid_AADAvailable, harness.HttpManager);
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
        [Description("AT in cache, needs refresh. AAD fails but is available when refreshing.")]
        public async Task ATS_Expired_NeedsRefresh_AADInvalidResponse_Async()
        {
            // Arrange
            using (var harness = base.CreateTestHarness())
            {
                Trace.WriteLine("1. Setup an app with a token cache with one AT");
                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithHttpManager(harness.HttpManager)
                                                                            .BuildConcrete();

                var tokenCacheHelper = new TokenCacheHelper();
                tokenCacheHelper.PopulateCache(app.UserTokenCacheInternal.Accessor, addSecondAt: false);

                Trace.WriteLine("2. Configure AT so that it shows it needs to be refreshed, but is also expired");
                UpdateATWithRefreshOn(app, DateTime.UtcNow - TimeSpan.FromMinutes(1), true);
                var cacheAccess = app.UserTokenCache.RecordAccess();

                Trace.WriteLine("3. Configure AAD to be unavaiable");
                AddHttpMocks(TokenResponseType.Invalid_AADUnavailable, harness.HttpManager);
                var account = new Account(TestConstants.s_userIdentifier, TestConstants.DisplayableId, null);

                // Act
               var ex = await AssertException.TaskThrowsAsync<MsalServiceException>(() => app
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
            using (var harness = base.CreateTestHarness())
            {
                Trace.WriteLine("1. Setup an app with a token cache with one AT");
                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithHttpManager(harness.HttpManager)
                                                                            .BuildConcrete();

                var tokenCacheHelper = new TokenCacheHelper();
                tokenCacheHelper.PopulateCache(app.UserTokenCacheInternal.Accessor, addSecondAt: false);

                Trace.WriteLine("2. Configure AT so that it shows it needs to be refreshed");
                UpdateATWithRefreshOn(app, DateTime.UtcNow - TimeSpan.FromMinutes(1));
                var cacheAccess = app.UserTokenCache.RecordAccess();

                Trace.WriteLine("3. Configure AAD to respond with valid token to the refresh RT flow");
                AddHttpMocks(TokenResponseType.Valid, harness.HttpManager);

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
                var ati = app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Single();
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
            }
        }
        #endregion

        private static void AddHttpMocks(TokenResponseType aadResponse, MockHttpManager httpManager)
        {
            httpManager.AddInstanceDiscoveryMockHandler();
            httpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityUtidTenant);

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
            PublicClientApplication app, 
            DateTimeOffset refreshOn, 
            bool expired = false)
        {
            MsalAccessTokenCacheItem atItem = app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Single();

            // past date on refresh on
            atItem.RefreshOnUnixTimestamp = CoreHelpers.DateTimeToUnixTimestamp(refreshOn);

            Assert.IsTrue(atItem.ExpiresOn > DateTime.UtcNow + TimeSpan.FromMinutes(10));

            if (expired)
            {
                atItem.ExpiresOnUnixTimestamp = CoreHelpers.DateTimeToUnixTimestamp(DateTime.UtcNow - TimeSpan.FromMinutes(1));
            }

            app.UserTokenCacheInternal.Accessor.SaveAccessToken(atItem);

            return atItem;
        }
    }
}
