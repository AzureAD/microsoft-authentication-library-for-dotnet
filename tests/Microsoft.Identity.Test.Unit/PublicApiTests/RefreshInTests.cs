// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Logger;
using Microsoft.Identity.Client.OAuth2.Throttling;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Test.Common;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class RefreshInTests : TestBase
    {
        #region AcquireTokenSilent tests

        [TestMethod]
        [Description("AT in cache, needs refresh. AAD responds well to Refresh.")]
        [Ignore] // unstable, see https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2918
        public async Task ATS_NonExpired_NeedsRefresh_ValidResponse_Async()
        {
            // Arrange
            using (MockHttpAndServiceBundle harness = base.CreateTestHarness())
            {
                Trace.WriteLine("1. Setup an app with a token cache with one AT");
                PublicClientApplication app = SetupPca(harness);

                Trace.WriteLine("2. Configure AT so that it shows it needs to be refreshed");
                var refreshOn = TestCommon.UpdateATWithRefreshOn(app.UserTokenCacheInternal.Accessor).RefreshOn;
                TokenCacheAccessRecorder cacheAccess = app.UserTokenCache.RecordAccess();

                Trace.WriteLine("3. Configure AAD to respond with valid token to the refresh RT flow");
                harness.HttpManager.AddAllMocks(TokenResponseType.Valid_UserFlows);

                // Act
                Trace.WriteLine("4. ATS - should perform an RT refresh");
                var account = new Account(TestConstants.s_userIdentifier, TestConstants.DisplayableId, null);
                AuthenticationResult result = await app
                    .AcquireTokenSilent(
                        TestConstants.s_scope.ToArray(),
                        account)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                // Assert token is proactively refreshed
                Assert.IsNotNull(result);
                Assert.IsTrue(result.AuthenticationResultMetadata.CacheRefreshReason == CacheRefreshReason.ProactivelyRefreshed);
                Assert.IsTrue(result.AuthenticationResultMetadata.RefreshOn == refreshOn);

                // The following can be indeterministic due to background threading nature
                // So it is verified on check and wait basis

                TestCommon.YieldTillSatisfied(() => harness.HttpManager.QueueSize == 0);

                Assert.AreEqual(0, harness.HttpManager.QueueSize,
                    "MSAL should have refreshed the token because the original AT was marked for refresh");
                cacheAccess.WaitTo_AssertAcessCounts(1, 1);
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
                Assert.IsTrue(result.AuthenticationResultMetadata.RefreshOn == ati.RefreshOn);
                Assert.IsTrue(result.AuthenticationResultMetadata.CacheRefreshReason == CacheRefreshReason.NotApplicable);

                cacheAccess.WaitTo_AssertAcessCounts(2, 1);
            }
        }

        private static PublicClientApplication SetupPca(MockHttpAndServiceBundle harness, LogCallback logCallback = null)
        {
            Trace.WriteLine("1. Setup an app with a token cache with one AT");
            PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                        .WithHttpManager(harness.HttpManager)
                                                                        .WithLogging(logCallback)
                                                                        .BuildConcrete();

            TokenCacheHelper.PopulateCache(app.UserTokenCacheInternal.Accessor, addSecondAt: false);
            return app;
        }

        [TestMethod]
        [Description("AT in cache, needs refresh. AAD is unavailable when refreshing.")]
        [Ignore] // unstable test, keeps failing with Assert.IsTrue failed. Background refresh 2 did not execute. https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2918
        public async Task ATS_NonExpired_NeedsRefresh_AADUnavailableResponse_Async()
        {
            // Arrange
            using (MockHttpAndServiceBundle harness = base.CreateTestHarness())
            {
                Trace.WriteLine("1. Setup an app with a token cache with one AT");
                PublicClientApplication app = SetupPca(harness);

                Trace.WriteLine("2. Configure AT so that it shows it needs to be refreshed");
                TestCommon.UpdateATWithRefreshOn(app.UserTokenCacheInternal.Accessor);
                TokenCacheAccessRecorder cacheAccess = app.UserTokenCache.RecordAccess();

                Trace.WriteLine("3. Configure AAD to respond with a 500 error");
                harness.HttpManager.AddAllMocks(TokenResponseType.Invalid_AADUnavailable503);
                harness.HttpManager.AddTokenResponse(TokenResponseType.Invalid_AADUnavailable503);

                // Act
                var account = new Account(TestConstants.s_userIdentifier, TestConstants.DisplayableId, null);
                AuthenticationResult result = await app
                    .AcquireTokenSilent(
                        TestConstants.s_scope.ToArray(),
                        account)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                // The following can be indeterministic due to background threading nature
                // So it is verified on check and wait basis
                Assert.IsTrue(TestCommon.YieldTillSatisfied(() => harness.HttpManager.QueueSize == 0), "Background refresh 1 did not execute.");

                // Assert
                Assert.AreEqual(0, harness.HttpManager.QueueSize);
                Assert.AreEqual(CacheRefreshReason.ProactivelyRefreshed, result.AuthenticationResultMetadata.CacheRefreshReason);

                cacheAccess.WaitTo_AssertAcessCounts(1, 0); // the refresh failed, no new data is written to the cache

                // reset throttling, otherwise MSAL would block similar requests for 2 minutes 
                // and we would still get a cached response
                SingletonThrottlingManager.GetInstance().ResetCache();

                // Now let AAD respond with tokens
                harness.HttpManager.AddTokenResponse(TokenResponseType.Valid_UserFlows);

                result = await app
                    .AcquireTokenSilent(
                        TestConstants.s_scope.ToArray(),
                        account)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.AreEqual(CacheRefreshReason.ProactivelyRefreshed, result.AuthenticationResultMetadata.CacheRefreshReason);

                Assert.IsTrue(TestCommon.YieldTillSatisfied(
                    () => harness.HttpManager.QueueSize == 0),
                    "Background refresh 2 did not execute.");
                Assert.IsTrue(
                    TestCommon.YieldTillSatisfied(() => cacheAccess.AfterAccessTotalCount == 3),
                    "The background refresh executed, but the cache was not updated");

                cacheAccess.WaitTo_AssertAcessCounts(2, 1); // new tokens written to cache
            }
        }

        [TestMethod]
        [Description("AT in cache, needs refresh. AAD fails but is available when refreshing.")]
        public async Task ATS_NonExpired_NeedsRefresh_AADInvalidResponse_Async()
        {
            bool wasErrorLogged = false;
            // Arrange
            using (MockHttpAndServiceBundle harness = base.CreateTestHarness())
            {
                Trace.WriteLine("1. Setup an app with a token cache with one AT");
                PublicClientApplication app = SetupPca(harness, LocalLogCallback);

                Trace.WriteLine("2. Configure AT so that it shows it needs to be refreshed");
                TestCommon.UpdateATWithRefreshOn(app.UserTokenCacheInternal.Accessor);
                TokenCacheAccessRecorder cacheAccess = app.UserTokenCache.RecordAccess();

                Trace.WriteLine("3. Configure AAD to respond with the typical Invalid Grant error");
                harness.HttpManager.AddAllMocks(TokenResponseType.InvalidGrant);

                var account = new Account(TestConstants.s_userIdentifier, TestConstants.DisplayableId, null);

                await app.AcquireTokenSilent(
                        TestConstants.s_scope.ToArray(),
                        account)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsTrue(TestCommon.YieldTillSatisfied(() => wasErrorLogged == true));
            }

            void LocalLogCallback(LogLevel level, string message, bool containsPii)
            {
                if (level == LogLevel.Error &&
                    message.Contains(SilentRequestHelper.ProactiveRefreshServiceError))
                {
                    wasErrorLogged = true;
                }
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
                TestCommon.UpdateATWithRefreshOn(app.UserTokenCacheInternal.Accessor, expired: true);
                TokenCacheAccessRecorder cacheAccess = app.UserTokenCache.RecordAccess();

                Trace.WriteLine("3. Configure AAD to be unavailable");
                harness.HttpManager.AddAllMocks(TokenResponseType.Invalid_AADUnavailable503);
                harness.HttpManager.AddTokenResponse(TokenResponseType.Invalid_AADUnavailable503);

                var account = new Account(TestConstants.s_userIdentifier, TestConstants.DisplayableId, null);

                // Act
                MsalServiceException ex = await AssertException.TaskThrowsAsync<MsalServiceException>(() => app
                    .AcquireTokenSilent(
                        TestConstants.s_scope.ToArray(),
                        account)
                    .ExecuteAsync())
                    .ConfigureAwait(false);

                Assert.IsFalse(ex is MsalUiRequiredException, "5xx exceptions do not translate to MsalUIRequired");
                Assert.AreEqual(503, ex.StatusCode);
            }
        }

        [TestMethod]
        public void JitterIsAddedToRefreshOn()
        {
            var at = TokenCacheHelper.CreateAccessTokenItem();
            var refreshOnFromCache = DateTimeOffset.UtcNow - TimeSpan.FromMinutes(10);
            at = at.WithRefreshOn(refreshOnFromCache);

            List<DateTimeOffset?> refreshOnWithJitterList = new List<DateTimeOffset?>();
            for (int i = 1; i <= 10; i++)
            {
                SilentRequestHelper.NeedsRefresh(at, out DateTimeOffset? refreshOnWithJitter);
                refreshOnWithJitterList.Add(refreshOnWithJitter);

                Assert.IsTrue(refreshOnWithJitter.HasValue);
                CoreAssert.IsWithinRange(
                    refreshOnFromCache, 
                    refreshOnWithJitter.Value, 
                    TimeSpan.FromSeconds(Constants.DefaultJitterRangeInSeconds));
            }
            Assert.IsTrue(refreshOnWithJitterList.Distinct().Count() >= 8, "Jitter is random, so we can only have 1-2 identical values");
        }

        [TestMethod]
        public async Task ATS_ProactiveRefresh_CancelsSuccessfully_Async()
        {
            bool wasErrorLogged = false;

            // Arrange
            using MockHttpAndServiceBundle harness = base.CreateTestHarness();
            harness.HttpManager.AddInstanceDiscoveryMockHandler();

            PublicClientApplication app = SetupPca(harness, LocalLogCallback);
            TestCommon.UpdateATWithRefreshOn(app.UserTokenCacheInternal.Accessor);

            var account = new Account(TestConstants.s_userIdentifier, TestConstants.DisplayableId, null);

            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            cts.Cancel();
            cts.Dispose();

            // Act
            await app.AcquireTokenSilent(
                    TestConstants.s_scope.ToArray(),
                    account)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            Assert.IsTrue(TestCommon.YieldTillSatisfied(() => wasErrorLogged));

            void LocalLogCallback(LogLevel level, string message, bool containsPii)
            {
                if (level == LogLevel.Warning &&
                    message.Contains(SilentRequestHelper.ProactiveRefreshCancellationError))
                {
                    wasErrorLogged = true;
                }
            }
        }

        #endregion

        #region Client Credentials

        [TestMethod]
        [Description("AT in cache, needs refresh. AAD responds well to Refresh.")]
        public async Task ClientCredentials_NonExpired_NeedsRefresh_ValidResponse_Async()
        {
            // Arrange
            using (MockHttpAndServiceBundle harness = base.CreateTestHarness())
            {
                Trace.WriteLine("1. Setup an app");
                ConfidentialClientApplication app = SetupCca(harness);

                Trace.WriteLine("2. Configure AT so that it shows it needs to be refreshed");
                var refreshOn = TestCommon.UpdateATWithRefreshOn(app.AppTokenCacheInternal.Accessor).RefreshOn;

                TokenCacheAccessRecorder cacheAccess = app.AppTokenCache.RecordAccess();

                Trace.WriteLine("3. Configure AAD to respond with valid token to the refresh RT flow");
                harness.HttpManager.AddAllMocks(TokenResponseType.Valid_ClientCredentials);

                // Act
                Trace.WriteLine("4. ATS - should perform an RT refresh");
                AuthenticationResult result = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                TestCommon.YieldTillSatisfied(() => harness.HttpManager.QueueSize == 0);
                Assert.IsNotNull(result);
                Assert.AreEqual(0, harness.HttpManager.QueueSize,
                    "MSAL should have refreshed the token because the original AT was marked for refresh");
                cacheAccess.WaitTo_AssertAcessCounts(1, 1);
                Assert.IsTrue(result.AuthenticationResultMetadata.CacheRefreshReason == CacheRefreshReason.ProactivelyRefreshed);
                Assert.IsTrue(result.AuthenticationResultMetadata.RefreshOn == refreshOn);

                result = await app.AcquireTokenForClient(TestConstants.s_scope)
                                    .ExecuteAsync()
                                    .ConfigureAwait(false);

                Assert.IsTrue(result.AuthenticationResultMetadata.CacheRefreshReason == CacheRefreshReason.NotApplicable);
            }
        }

        [TestMethod]
        [Description("AT in cache, needs refresh. AAD responds well to Refresh.")]
        public async Task ClientCredentials_OnBehalfOf_NonExpired_NeedsRefresh_ValidResponse_Async()
        {
            // Arrange
            using (MockHttpAndServiceBundle harness = base.CreateTestHarness())
            {
                Trace.WriteLine("1. Setup an app with a token cache with one AT");
                ConfidentialClientApplication app = SetupCca(harness);

                Trace.WriteLine("2. Configure AT so that it shows it needs to be refreshed");
                var refreshOn = TestCommon.UpdateATWithRefreshOn(app.UserTokenCacheInternal.Accessor).RefreshOn;
                TokenCacheAccessRecorder cacheAccess = app.UserTokenCache.RecordAccess();

                Trace.WriteLine("3. Configure AAD to respond with valid token to the refresh RT flow");
                harness.HttpManager.AddAllMocks(TokenResponseType.Valid_UserFlows);

                // Act
                Trace.WriteLine("4. ATS - should perform an RT refresh");
                AuthenticationResult result = await app.AcquireTokenOnBehalfOf(TestConstants.s_scope, new UserAssertion(TestConstants.UserAssertion, "assertiontype"))
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                TestCommon.YieldTillSatisfied(() => harness.HttpManager.QueueSize == 0);
                Assert.IsNotNull(result);
                Assert.AreEqual(0, harness.HttpManager.QueueSize,
                    "MSAL should have refreshed the token because the original AT was marked for refresh");
                cacheAccess.WaitTo_AssertAcessCounts(1, 1);
                Assert.IsTrue(result.AuthenticationResultMetadata.CacheRefreshReason == CacheRefreshReason.ProactivelyRefreshed);
                Assert.IsTrue(result.AuthenticationResultMetadata.RefreshOn == refreshOn);

                result = await app.AcquireTokenOnBehalfOf(TestConstants.s_scope, new UserAssertion(TestConstants.UserAssertion, "assertiontype"))
                    .ExecuteAsync()
                    .ConfigureAwait(false);
                Assert.IsTrue(result.AuthenticationResultMetadata.CacheRefreshReason == CacheRefreshReason.NotApplicable);
            }
        }

        private static ConfidentialClientApplication SetupCca(MockHttpAndServiceBundle harness, LogCallback logCallback = null)
        {
            ConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                          .WithAuthority(AzureCloudInstance.AzurePublic, TestConstants.Utid)
                                                          .WithClientSecret(TestConstants.ClientSecret)
                                                          .WithHttpManager(harness.HttpManager)
                                                          .WithLogging(logCallback)
                                                          .BuildConcrete();

            TokenCacheHelper.PopulateCache(app.AppTokenCacheInternal.Accessor, addSecondAt: false);
            TokenCacheHelper.PopulateCache(app.UserTokenCacheInternal.Accessor, addSecondAt: false, userAssertion: TestConstants.UserAssertion);
            return app;
        }

        [TestMethod]
        [Description("AT in cache, needs refresh. AAD is unavailable when refreshing.")]
        public async Task ClientCredentials_NonExpired_NeedsRefresh_AadUnavailableResponse_Async()
        {
            // Arrange
            using (MockHttpAndServiceBundle harness = base.CreateTestHarness())
            {
                Trace.WriteLine("1. Setup an app ");
                ConfidentialClientApplication app = SetupCca(harness);

                Trace.WriteLine("2. Configure AT so that it shows it needs to be refreshed");

                TestCommon.UpdateATWithRefreshOn(app.AppTokenCacheInternal.Accessor);

                TokenCacheAccessRecorder cacheAccess = app.AppTokenCache.RecordAccess();

                Trace.WriteLine("3. Configure AAD to respond with an error");
                harness.HttpManager.AddAllMocks(TokenResponseType.Invalid_AADUnavailable503);
                harness.HttpManager.AddTokenResponse(TokenResponseType.Invalid_AADUnavailable503);

                // Act
                AuthenticationResult result = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(result, "ClientCredentials should still succeeds even though AAD is unavailable");
                TestCommon.YieldTillSatisfied(() => harness.HttpManager.QueueSize == 0);
                Assert.AreEqual(0, harness.HttpManager.QueueSize);
                cacheAccess.WaitTo_AssertAcessCounts(1, 0); // the refresh failed, no new data is written to the cache

                // Now let AAD respond with tokens
                harness.HttpManager.AddTokenResponse(TokenResponseType.Valid_ClientCredentials);

                result = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync()
                    .ConfigureAwait(false);
                Assert.IsNotNull(result);

                cacheAccess.WaitTo_AssertAcessCounts(2, 1); // new tokens written to cache
            }
        }

        [TestMethod]
        public async Task ClientCredentials_NonExpired_NeedsRefresh_AadInvalidResponse_Async()
        {
            bool wasErrorLogged = false;
            // Arrange
            using (MockHttpAndServiceBundle harness = base.CreateTestHarness())
            {
                Trace.WriteLine("1. Setup an app");
                ConfidentialClientApplication app = SetupCca(harness, LocalLogCallback);

                Trace.WriteLine("2. Configure AT so that it shows it needs to be refreshed");

                TestCommon.UpdateATWithRefreshOn(app.AppTokenCacheInternal.Accessor);

                TokenCacheAccessRecorder cacheAccess = app.AppTokenCache.RecordAccess();

                Trace.WriteLine("3. Configure AAD to respond with the typical Invalid Grant error");
                harness.HttpManager.AddAllMocks(TokenResponseType.InvalidGrant);

                // Act
                await app.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsTrue(TestCommon.YieldTillSatisfied(() => wasErrorLogged == true));
                cacheAccess.WaitTo_AssertAcessCounts(1, 0);
            }

            void LocalLogCallback(LogLevel level, string message, bool containsPii)
            {
                if (level == LogLevel.Error &&
                    message.Contains(SilentRequestHelper.ProactiveRefreshServiceError))
                {
                    wasErrorLogged = true;
                }
            }
        }

        [TestMethod]
        [Description("AT expired. AAD fails but is available when refreshing.")]
        public async Task ClientCredentials_Expired_NeedsRefresh_AADInvalidResponse_Async()
        {
            // Arrange
            using (MockHttpAndServiceBundle harness = base.CreateTestHarness())
            {
                Trace.WriteLine("1. Setup an app with a token cache with one AT = expired and needs refresh");
                ConfidentialClientApplication app = SetupCca(harness);
                TestCommon.UpdateATWithRefreshOn(app.AppTokenCacheInternal.Accessor, expired: true);

                TokenCacheAccessRecorder cacheAccess = app.AppTokenCache.RecordAccess();

                Trace.WriteLine("2. Configure AAD to be unavailable");
                harness.HttpManager.AddAllMocks(TokenResponseType.Invalid_AADUnavailable503);
                harness.HttpManager.AddTokenResponse(TokenResponseType.Invalid_AADUnavailable503);

                // Act
                MsalServiceException ex = await AssertException.TaskThrowsAsync<MsalServiceException>(() => app
                   .AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync())
                    .ConfigureAwait(false);

                Assert.IsFalse(ex is MsalUiRequiredException, "5xx exceptions do not translate to MsalUIRequired");
                Assert.AreEqual(503, ex.StatusCode);
                cacheAccess.WaitTo_AssertAcessCounts(1, 0);
            }
        }

        [TestMethod]
        public async Task ClientCredentials_ProactiveRefresh_CancelsSuccessfully_Async()
        {
            bool wasErrorLogged = false;

            // Arrange
            using MockHttpAndServiceBundle harness = CreateTestHarness();
            harness.HttpManager.AddInstanceDiscoveryMockHandler();

            ConfidentialClientApplication app = SetupCca(harness, LocalLogCallback);
            TestCommon.UpdateATWithRefreshOn(app.AppTokenCacheInternal.Accessor);

            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            cts.Cancel();
            cts.Dispose();

            // Act
            await app.AcquireTokenForClient(TestConstants.s_scope)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            Assert.IsTrue(TestCommon.YieldTillSatisfied(() => wasErrorLogged));

            void LocalLogCallback(LogLevel level, string message, bool containsPii)
            {
                if (level == LogLevel.Warning &&
                    message.Contains(SilentRequestHelper.ProactiveRefreshCancellationError))
                {
                    wasErrorLogged = true;
                }
            }
        }
        #endregion
    }
}
