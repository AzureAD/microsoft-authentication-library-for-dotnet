// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AuthScheme;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CacheTests
{
    [TestClass]
    public class AppTokenCacheReadOptimizationTests : TestBase
    {
        private const string TestTokenType = "test-extension-token";
        private const string TestKeyId = "test-extension-key";

        [TestMethod]
        [DataRow(false, 2)]
        [DataRow(true, 1)]
        public async Task WarmAppTokenRead_OptimizationControlsExternalReadCountAsync(
            bool optimizationEnabled,
            int expectedReadCount)
        {
            // Arrange
            using var httpManager = new MockHttpManager();
            httpManager.AddInstanceDiscoveryMockHandler();
            httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

            var externalCache = new SerializedAppTokenCache();
            ConfidentialClientApplication app = CreateApp(
                httpManager,
                externalCache,
                optimizationEnabled);

            // Act
            AuthenticationResult firstResult = await app
                .AcquireTokenForClient(TestConstants.s_scope)
                .WithTenantId(TestConstants.Utid)
                .ExecuteAsync()
                .ConfigureAwait(false);
            AuthenticationResult secondResult = await app
                .AcquireTokenForClient(TestConstants.s_scope)
                .WithTenantId(TestConstants.Utid)
                .ExecuteAsync()
                .ConfigureAwait(false);

            // Assert
            Assert.AreEqual(TokenSource.IdentityProvider, firstResult.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(TokenSource.Cache, secondResult.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(expectedReadCount, externalCache.ReadCount);
        }

        [TestMethod]
        [Timeout(5000, CooperativeCancellation = true)]
        public async Task ConcurrentExternalHydration_IsSingleFlightedAsync()
        {
            // Arrange
            using var httpManager = new MockHttpManager();
            httpManager.AddInstanceDiscoveryMockHandler();
            httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

            var externalCache = new SerializedAppTokenCache();
            ConfidentialClientApplication app = CreateApp(
                httpManager,
                externalCache,
                optimizationEnabled: true);

            await app.AcquireTokenForClient(TestConstants.s_scope)
                .WithTenantId(TestConstants.Utid)
                .ExecuteAsync()
                .ConfigureAwait(false);

            app.AppTokenCacheInternal.Accessor.Clear();
            int readsBeforeBurst = externalCache.ReadCount;
            externalCache.BlockNextRead();

            Task<AuthenticationResult>[] requests = Enumerable.Range(0, 32)
                .Select(_ => app.AcquireTokenForClient(TestConstants.s_scope)
                    .WithTenantId(TestConstants.Utid)
                    .ExecuteAsync())
                .ToArray();

            await externalCache.ReadStarted.ConfigureAwait(false);
            Assert.AreEqual(
                1,
                app.AppTokenCacheInternal.AppTokenCacheRequestCoordinator.ReadFlightCount);

            // Act
            externalCache.ReleaseRead();
            AuthenticationResult[] results = await Task.WhenAll(requests).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(readsBeforeBurst + 1, externalCache.ReadCount);
            Assert.IsTrue(results.All(result =>
                result.AuthenticationResultMetadata.TokenSource == TokenSource.Cache));
            Assert.AreEqual(
                0,
                app.AppTokenCacheInternal.AppTokenCacheRequestCoordinator.ReadFlightCount);
        }

        [TestMethod]
        [Timeout(5000, CooperativeCancellation = true)]
        public async Task InMemoryRead_DoesNotBypassActiveHydrationAsync()
        {
            // Arrange
            using var httpManager = new MockHttpManager();
            httpManager.AddInstanceDiscoveryMockHandler();
            httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

            var externalCache = new SerializedAppTokenCache();
            ConfidentialClientApplication app = CreateApp(
                httpManager,
                externalCache,
                optimizationEnabled: true);

            await app.AcquireTokenForClient(TestConstants.s_scope)
                .WithTenantId(TestConstants.Utid)
                .ExecuteAsync()
                .ConfigureAwait(false);

            app.AppTokenCacheInternal.Accessor.Clear();
            externalCache.BlockNextRead(afterDeserialization: true);

            Task<AuthenticationResult> leader = app
                .AcquireTokenForClient(TestConstants.s_scope)
                .WithTenantId(TestConstants.Utid)
                .ExecuteAsync();
            await externalCache.ReadStarted.ConfigureAwait(false);

            // Act
            Task<AuthenticationResult> follower = app
                .AcquireTokenForClient(TestConstants.s_scope)
                .WithTenantId(TestConstants.Utid)
                .ExecuteAsync();

            // Assert
            Assert.IsFalse(
                follower.IsCompleted,
                "A local read must join an active hydration instead of consuming intermediate cache state.");

            externalCache.ReleaseRead();
            AuthenticationResult[] results = await Task.WhenAll(leader, follower).ConfigureAwait(false);
            Assert.IsTrue(results.All(result =>
                result.AuthenticationResultMetadata.TokenSource == TokenSource.Cache));
        }

        [TestMethod]
        [Timeout(5000, CooperativeCancellation = true)]
        public async Task WarmAppTokenRead_FormatsEveryRequestIndependentlyAsync()
        {
            // Arrange
            using var httpManager = new MockHttpManager();
            httpManager.AddInstanceDiscoveryMockHandler();
            httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseWithAdditionalParamsMessage(
                tokenType: TestTokenType,
                additionalparams: string.Empty);

            var externalCache = new SerializedAppTokenCache();
            ConfidentialClientApplication app = CreateApp(
                httpManager,
                externalCache,
                optimizationEnabled: true);
            var counter = new FormattingCounter();

            AuthenticationResult firstResult = await AcquireWithSuffixAsync(
                app,
                "first",
                counter).ConfigureAwait(false);
            int readsAfterFirstRequest = externalCache.ReadCount;
            app.AppTokenCacheInternal.Accessor.Clear();
            externalCache.BlockNextRead();

            string[] suffixes = Enumerable.Range(0, 16)
                .Select(index => $"request-{index}")
                .ToArray();

            // Act
            Task<AuthenticationResult>[] requests = suffixes
                .Select(suffix => AcquireWithSuffixAsync(
                    app,
                    suffix,
                    counter))
                .ToArray();
            await externalCache.ReadStarted.ConfigureAwait(false);
            externalCache.ReleaseRead();
            AuthenticationResult[] results = await Task.WhenAll(requests).ConfigureAwait(false);

            // Assert
            StringAssert.EndsWith(firstResult.AccessToken, "first");
            for (int i = 0; i < results.Length; i++)
            {
                StringAssert.EndsWith(results[i].AccessToken, suffixes[i]);
            }

            Assert.AreEqual(1 + suffixes.Length, counter.Count);
            Assert.AreEqual(readsAfterFirstRequest + 1, externalCache.ReadCount);
        }

        [TestMethod]
        [Timeout(5000, CooperativeCancellation = true)]
        public async Task ReadFlight_FollowerCancellationDoesNotCancelLeaderAsync()
        {
            // Arrange
            var coordinator = new AppTokenCacheRequestCoordinator();
            var readStarted = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseRead = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            int readCount = 0;

            async Task<AppTokenCacheReadResult> ReadAsync()
            {
                Interlocked.Increment(ref readCount);
                readStarted.TrySetResult(true);
                await releaseRead.Task.ConfigureAwait(false);
                return new AppTokenCacheReadResult(
                    cacheItem: null,
                    CacheLevel.L1Cache,
                    CacheRefreshReason.NotApplicable,
                    cachedAccessTokenCount: 0,
                    durationInCacheInMs: 0);
            }

            Task<AppTokenCacheReadResponse> leader = coordinator.RunCacheReadAsync(
                "cache-key",
                ReadAsync,
                CancellationToken.None);
            await readStarted.Task.ConfigureAwait(false);

            using var followerCancellation = new CancellationTokenSource();
            Task<AppTokenCacheReadResponse> follower = coordinator.RunCacheReadAsync(
                "cache-key",
                ReadAsync,
                followerCancellation.Token);

            // Act
            followerCancellation.Cancel();
            await AssertException.TaskThrowsAsync<OperationCanceledException>(
                () => follower).ConfigureAwait(false);
            releaseRead.TrySetResult(true);
            AppTokenCacheReadResponse leaderResult = await leader.ConfigureAwait(false);

            // Assert
            Assert.IsTrue(leaderResult.IsLeader);
            Assert.AreEqual(1, readCount);
            Assert.AreEqual(0, coordinator.ReadFlightCount);
        }

        [TestMethod]
        [Timeout(5000, CooperativeCancellation = true)]
        public async Task ReadFlight_LeaderFailureLetsFollowerRetryIndependentlyAsync()
        {
            // Arrange
            var coordinator = new AppTokenCacheRequestCoordinator();
            var readStarted = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseRead = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            int readCount = 0;

            async Task<AppTokenCacheReadResult> ReadAsync()
            {
                Interlocked.Increment(ref readCount);
                readStarted.TrySetResult(true);
                await releaseRead.Task.ConfigureAwait(false);
                throw new InvalidOperationException("cache read failed");
            }

            Task<AppTokenCacheReadResponse> leader = coordinator.RunCacheReadAsync(
                "cache-key",
                ReadAsync,
                CancellationToken.None);
            await readStarted.Task.ConfigureAwait(false);
            Task<AppTokenCacheReadResponse> follower = coordinator.RunCacheReadAsync(
                "cache-key",
                ReadAsync,
                CancellationToken.None);
            await Task.Yield();

            // Act
            releaseRead.TrySetResult(true);
            await AssertException.TaskThrowsAsync<InvalidOperationException>(
                () => leader).ConfigureAwait(false);
            AppTokenCacheReadResponse followerResult = await follower.ConfigureAwait(false);

            // Assert
            Assert.IsFalse(followerResult.IsLeader);
            Assert.IsTrue(followerResult.Result.ShouldRetryIndependently);
            Assert.AreEqual(1, readCount);
            Assert.AreEqual(0, coordinator.ReadFlightCount);
        }

        [TestMethod]
        [Timeout(5000, CooperativeCancellation = true)]
        public async Task IndependentRead_BlocksInMemoryReadUntilCompletionAsync()
        {
            // Arrange
            var coordinator = new AppTokenCacheRequestCoordinator();
            var readStarted = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseRead = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            Task<AppTokenCacheReadResult> read = coordinator.RunIndependentCacheReadAsync(
                async () =>
                {
                    readStarted.TrySetResult(true);
                    await releaseRead.Task.ConfigureAwait(false);
                    return AppTokenCacheReadResult.RetryIndependently();
                });
            await readStarted.Task.ConfigureAwait(false);

            // Act
            bool canReadDuringHydration = coordinator.TryBeginInMemoryRead(out _);
            releaseRead.TrySetResult(true);
            await read.ConfigureAwait(false);
            bool canReadAfterHydration = coordinator.TryBeginInMemoryRead(out _);

            // Assert
            Assert.IsFalse(canReadDuringHydration);
            Assert.IsTrue(canReadAfterHydration);
        }

        [TestMethod]
        public void ProactiveRefreshKey_DifferentKeyIdsAreIsolated()
        {
            // Arrange
            DateTimeOffset now = DateTimeOffset.UtcNow;
            var first = new MsalAccessTokenCacheItem(
                TestConstants.ProductionPrefCacheEnvironment,
                TestConstants.ClientId,
                TestConstants.s_scope.AsSingleString(),
                TestConstants.Utid,
                TestConstants.DefaultAccessToken,
                now,
                now.AddHours(1),
                now.AddHours(1),
                rawClientInfo: null,
                homeAccountId: null,
                keyId: "key-1",
                tokenType: "pop");
            var second = new MsalAccessTokenCacheItem(
                TestConstants.ProductionPrefCacheEnvironment,
                TestConstants.ClientId,
                TestConstants.s_scope.AsSingleString(),
                TestConstants.Utid,
                TestConstants.DefaultAccessToken,
                now,
                now.AddHours(1),
                now.AddHours(1),
                rawClientInfo: null,
                homeAccountId: null,
                keyId: "key-2",
                tokenType: "pop");

            // Act
            string firstKey = CacheKeyFactory.GetAppTokenProactiveRefreshKey(first);
            string secondKey = CacheKeyFactory.GetAppTokenProactiveRefreshKey(second);

            // Assert
            Assert.AreEqual(first.CacheKey, second.CacheKey);
            Assert.AreNotEqual(firstKey, secondKey);
        }

        [TestMethod]
        [Timeout(5000, CooperativeCancellation = true)]
        public async Task ProactiveRefresh_ConcurrentRequestsCallProviderOnceAsync()
        {
            // Arrange
            int providerCallCount = 0;
            var providerStarted = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseProvider = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            using var httpManager = new MockHttpManager();

            ConfidentialClientApplication app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithAuthority(AzureCloudInstance.AzurePublic, TestConstants.Utid)
                .WithHttpManager(httpManager)
                .WithAppTokenProvider(async _ =>
                {
                    Interlocked.Increment(ref providerCallCount);
                    providerStarted.TrySetResult(true);
                    await releaseProvider.Task.ConfigureAwait(false);
                    return new AppTokenProviderResult
                    {
                        AccessToken = TestConstants.DefaultAccessToken + "-refreshed",
                        ExpiresInSeconds = 3600,
                        RefreshInSeconds = 1800
                    };
                })
                .BuildConcrete();

            app.AppTokenCache.SetAppTokenCacheReadOptimization(enabled: true);
            TokenCacheHelper.PopulateCache(app.AppTokenCacheInternal.Accessor, addSecondAt: false);
            TestCommon.UpdateATWithRefreshOn(app.AppTokenCacheInternal.Accessor);

            using var canceledCaller = new CancellationTokenSource();
            canceledCaller.Cancel();
            AuthenticationResult canceledCallerResult = await app
                .AcquireTokenForClient(TestConstants.s_scope)
                .ExecuteAsync(canceledCaller.Token)
                .ConfigureAwait(false);
            Assert.AreEqual(0, providerCallCount);

            Task<AuthenticationResult>[] requests = Enumerable.Range(0, 31)
                .Select(_ => app.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync())
                .ToArray();

            try
            {
                // Act
                AuthenticationResult[] results = await Task.WhenAll(requests).ConfigureAwait(false);
                await providerStarted.Task.ConfigureAwait(false);

                // Assert
                Assert.AreEqual(
                    TokenSource.Cache,
                    canceledCallerResult.AuthenticationResultMetadata.TokenSource);
                Assert.IsTrue(results.All(result =>
                    result.AuthenticationResultMetadata.TokenSource == TokenSource.Cache));
                Assert.AreEqual(1, providerCallCount);
                Assert.AreEqual(
                    1,
                    app.AppTokenCacheInternal.AppTokenCacheRequestCoordinator.ProactiveRefreshFlightCount);
            }
            finally
            {
                releaseProvider.TrySetResult(true);
            }

            Assert.IsTrue(TestCommon.YieldTillSatisfied(() =>
                app.AppTokenCacheInternal.AppTokenCacheRequestCoordinator.ProactiveRefreshFlightCount == 0));
            Assert.AreEqual(1, providerCallCount);
            Assert.AreEqual(
                TestConstants.DefaultAccessToken + "-refreshed",
                app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Single().Secret);
        }

        [TestMethod]
        [Timeout(5000, CooperativeCancellation = true)]
        public async Task ProactiveRefresh_StaleFormattedItemDoesNotStartSecondRefreshAsync()
        {
            // Arrange
            int providerCallCount = 0;
            const string RefreshedToken = "refreshed-token";

            ConfidentialClientApplication app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithAuthority(AzureCloudInstance.AzurePublic, TestConstants.Utid)
                .WithAppTokenProvider(_ =>
                {
                    Interlocked.Increment(ref providerCallCount);
                    return Task.FromResult(new AppTokenProviderResult
                    {
                        AccessToken = RefreshedToken,
                        ExpiresInSeconds = 3600,
                        RefreshInSeconds = 1800
                    });
                })
                .BuildConcrete();

            app.AppTokenCache.SetAppTokenCacheReadOptimization(enabled: true);
            TokenCacheHelper.PopulateCache(app.AppTokenCacheInternal.Accessor, addSecondAt: false);
            TestCommon.UpdateATWithRefreshOn(app.AppTokenCacheInternal.Accessor);

            var formattingStarted = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseFormatting = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var delayedOperation = new BlockingFormattingOperation(
                formattingStarted,
                releaseFormatting);
            var extension = new MsalAuthenticationExtension
            {
                AuthenticationOperation = delayedOperation
            };

            Task<AuthenticationResult> delayedRequest = app
                .AcquireTokenForClient(TestConstants.s_scope)
                .WithAuthenticationExtension(extension)
                .ExecuteAsync();
            await formattingStarted.Task.ConfigureAwait(false);

            try
            {
                // Act
                await app.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.IsTrue(TestCommon.YieldTillSatisfied(() =>
                    providerCallCount == 1 &&
                    app.AppTokenCacheInternal.AppTokenCacheRequestCoordinator.ProactiveRefreshFlightCount == 0 &&
                    string.Equals(
                        RefreshedToken,
                        app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Single().Secret,
                        StringComparison.Ordinal)));
            }
            finally
            {
                releaseFormatting.TrySetResult(true);
            }

            await delayedRequest.ConfigureAwait(false);
            Assert.AreEqual(1, providerCallCount);
        }

        [TestMethod]
        public void SetAppTokenCacheReadOptimization_UserTokenCacheThrows()
        {
            // Arrange
            IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithClientSecret(TestConstants.ClientSecret)
                .Build();

            // Act
            InvalidOperationException exception = AssertException.Throws<InvalidOperationException>(
                () => app.UserTokenCache.SetAppTokenCacheReadOptimization(enabled: true));

            // Assert
            Assert.IsGreaterThanOrEqualTo(
                0,
                exception.Message.IndexOf(
                    nameof(IConfidentialClientApplication.AppTokenCache),
                    StringComparison.Ordinal));
        }

        private static ConfidentialClientApplication CreateApp(
            MockHttpManager httpManager,
            SerializedAppTokenCache externalCache,
            bool optimizationEnabled)
        {
            ConfidentialClientApplication app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithClientSecret(TestConstants.ClientSecret)
                .WithAuthority(TestConstants.AuthorityUtidTenant)
                .WithHttpManager(httpManager)
                .BuildConcrete();

            externalCache.Bind(app.AppTokenCache);
            app.AppTokenCache.SetAppTokenCacheReadOptimization(optimizationEnabled);
            return app;
        }

        private static Task<AuthenticationResult> AcquireWithSuffixAsync(
            ConfidentialClientApplication app,
            string suffix,
            FormattingCounter counter)
        {
            var extension = new MsalAuthenticationExtension
            {
                AuthenticationOperation = new SuffixAuthenticationOperation(
                    suffix,
                    counter)
            };

            return app.AcquireTokenForClient(TestConstants.s_scope)
                .WithTenantId(TestConstants.Utid)
                .WithAuthenticationExtension(extension)
                .ExecuteAsync();
        }

        private sealed class SerializedAppTokenCache
        {
            private byte[] _cacheData;
            private TaskCompletionSource<bool> _readStarted;
            private TaskCompletionSource<bool> _releaseRead;
            private int _blockNextRead;
            private int _blockAfterDeserialization;
            private int _readCount;

            internal int ReadCount => Volatile.Read(ref _readCount);

            internal Task ReadStarted => _readStarted.Task;

            internal void Bind(ITokenCache tokenCache)
            {
                tokenCache.SetBeforeAccessAsync(BeforeAccessAsync);
                tokenCache.SetAfterAccessAsync(AfterAccessAsync);
            }

            internal void BlockNextRead(bool afterDeserialization = false)
            {
                _readStarted = new TaskCompletionSource<bool>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                _releaseRead = new TaskCompletionSource<bool>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                Volatile.Write(
                    ref _blockAfterDeserialization,
                    afterDeserialization ? 1 : 0);
                Volatile.Write(ref _blockNextRead, 1);
            }

            internal void ReleaseRead()
            {
                _releaseRead.TrySetResult(true);
            }

            private async Task BeforeAccessAsync(TokenCacheNotificationArgs args)
            {
                if (!args.HasStateChanged)
                {
                    Interlocked.Increment(ref _readCount);
                    args.TelemetryData.CacheLevel = CacheLevel.L1Cache;

                    bool blockRead = Interlocked.Exchange(ref _blockNextRead, 0) == 1;
                    if (blockRead &&
                        Volatile.Read(ref _blockAfterDeserialization) == 0)
                    {
                        _readStarted.TrySetResult(true);
                        await _releaseRead.Task.ConfigureAwait(false);
                    }

                    args.TokenCache.DeserializeMsalV3(
                        _cacheData,
                        shouldClearExistingCache: true);

                    if (blockRead &&
                        Volatile.Read(ref _blockAfterDeserialization) == 1)
                    {
                        _readStarted.TrySetResult(true);
                        await _releaseRead.Task.ConfigureAwait(false);
                    }

                    return;
                }

                args.TokenCache.DeserializeMsalV3(
                    _cacheData,
                    shouldClearExistingCache: true);
            }

            private Task AfterAccessAsync(TokenCacheNotificationArgs args)
            {
                if (args.HasStateChanged)
                {
                    _cacheData = args.TokenCache.SerializeMsalV3();
                }

                return Task.CompletedTask;
            }
        }

        private sealed class FormattingCounter
        {
            private int _count;

            internal int Count => Volatile.Read(ref _count);

            internal void Increment()
            {
                Interlocked.Increment(ref _count);
            }
        }

        private sealed class SuffixAuthenticationOperation : IAuthenticationOperation
        {
            private readonly string _suffix;
            private readonly FormattingCounter _counter;

            internal SuffixAuthenticationOperation(
                string suffix,
                FormattingCounter counter)
            {
                _suffix = suffix ?? throw new ArgumentNullException(nameof(suffix));
                _counter = counter ?? throw new ArgumentNullException(nameof(counter));
            }

            public int TelemetryTokenType => 5;

            public string AuthorizationHeaderPrefix => "Test";

            public string KeyId => TestKeyId;

            public string AccessTokenType => TestTokenType;

            public IReadOnlyDictionary<string, string> GetTokenRequestParams()
            {
                return new Dictionary<string, string>
                {
                    ["token_type"] = TestTokenType
                };
            }

            public void FormatResult(AuthenticationResult authenticationResult)
            {
                _counter.Increment();
                authenticationResult.AccessToken += _suffix;
            }
        }

        private sealed class BlockingFormattingOperation : IAuthenticationOperation2
        {
            private readonly TaskCompletionSource<bool> _formattingStarted;
            private readonly TaskCompletionSource<bool> _releaseFormatting;

            internal BlockingFormattingOperation(
                TaskCompletionSource<bool> formattingStarted,
                TaskCompletionSource<bool> releaseFormatting)
            {
                _formattingStarted = formattingStarted ??
                    throw new ArgumentNullException(nameof(formattingStarted));
                _releaseFormatting = releaseFormatting ??
                    throw new ArgumentNullException(nameof(releaseFormatting));
            }

            public int TelemetryTokenType => 1;

            public string AuthorizationHeaderPrefix => "Bearer";

            public string KeyId => null;

            public string AccessTokenType => "Bearer";

            public IReadOnlyDictionary<string, string> GetTokenRequestParams()
            {
                return new Dictionary<string, string>();
            }

            public void FormatResult(AuthenticationResult authenticationResult)
            {
            }

            public async Task FormatResultAsync(
                AuthenticationResult authenticationResult,
                CancellationToken cancellationToken = default)
            {
                _formattingStarted.TrySetResult(true);
                await _releaseFormatting.Task.ConfigureAwait(false);
            }

            public Task<bool> ValidateCachedTokenAsync(
                MsalCacheValidationData cachedTokenData)
            {
                return Task.FromResult(true);
            }
        }
    }
}
