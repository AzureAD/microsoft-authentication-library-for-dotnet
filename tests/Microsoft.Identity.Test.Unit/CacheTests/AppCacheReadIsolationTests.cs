// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Test.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CacheTests
{
    [TestClass]
    [DoNotParallelize]
    public class AppCacheReadIsolationTests
    {
        private const string ClientId = "22222222-2222-2222-2222-222222222222";
        private const string TenantA = "11111111-1111-1111-1111-111111111111";
        private const string TenantB = "33333333-3333-3333-3333-333333333333";
        private const string ScopeA = "api://resource-a/.default";
        private const string ScopeB = "api://resource-b/.default";
        private bool _previousAppIsolation;
        private bool _previousDisabled;

        [TestInitialize]
        public void Initialize()
        {
            AppContext.TryGetSwitch(TokenCache.EnableAppCacheReadIsolationSwitch, out _previousAppIsolation);
            AppContext.TryGetSwitch(TokenCache.DisableCacheReadIsolationSwitch, out _previousDisabled);
            AppContext.SetSwitch(TokenCache.EnableAppCacheReadIsolationSwitch, true);
            AppContext.SetSwitch(TokenCache.DisableCacheReadIsolationSwitch, false);
        }

        [TestCleanup]
        public void Cleanup()
        {
            AppContext.SetSwitch(TokenCache.EnableAppCacheReadIsolationSwitch, _previousAppIsolation);
            AppContext.SetSwitch(TokenCache.DisableCacheReadIsolationSwitch, _previousDisabled);
        }

        [TestMethod]
        [DataRow(false, true, false, false, false)]
        [DataRow(true, true, false, false, true)]
        [DataRow(true, false, false, false, false)]
        [DataRow(true, true, true, false, false)]
        [DataRow(true, true, false, true, false)]
        public async Task IsolationRequiresExplicitOptInAndSupportedConfiguration(
            bool optIn, bool synchronize, bool legacy, bool globallyDisabled, bool isolated)
        {
            // Arrange
            AppContext.SetSwitch(TokenCache.EnableAppCacheReadIsolationSwitch, optIn);
            AppContext.SetSwitch(TokenCache.DisableCacheReadIsolationSwitch, globallyDisabled);
            var application = CreateApplication(synchronize, legacy);
            var cache = (ITokenCacheInternal)application.AppTokenCache;
            byte[] payload = Payload(TenantA, ScopeA, "token-a");
            bool usedOriginal = false;
            cache.SetBeforeAccess(args =>
            {
                usedOriginal = ReferenceEquals(cache, args.TokenCache);
                args.TokenCache.DeserializeMsalV3(payload, true);
            });

            // Act
            var result = await application.AcquireTokenForClient(new[] { ScopeA }).ExecuteAsync().ConfigureAwait(false);

            // Assert
            Assert.AreEqual("token-a", result.AccessToken);
            Assert.AreEqual(!isolated, usedOriginal);
            Assert.HasCount(isolated ? 0 : 1, cache.Accessor.GetAllAccessTokens());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task AsyncReadsOverlapAndKeepTheirOwnPartition(bool useSnapshot)
        {
            // Arrange
            var application = CreateApplication();
            var cache = (ITokenCacheInternal)application.AppTokenCache;
            byte[] blobA = Payload(TenantA, ScopeA, "token-a");
            byte[] blobB = Payload(TenantB, ScopeA, "token-b");
            TokenCacheSnapshot snapshotA = useSnapshot ? Snapshot(blobA) : null;
            TokenCacheSnapshot snapshotB = useSnapshot ? Snapshot(blobB) : null;
            string keyA = CacheKeyFactory.GetAppTokenCacheItemKey(ClientId, TenantA, null);
            var bothEntered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var resume = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            int entered = 0;
            int completed = 0;
            cache.SetBeforeAccessAsync(async args =>
            {
                Assert.AreNotSame(cache, args.TokenCache);
                if (Interlocked.Increment(ref entered) == 2)
                {
                    bothEntered.TrySetResult(true);
                }
                await resume.Task.ConfigureAwait(false);
                bool isA = string.Equals(keyA, args.SuggestedCacheKey, StringComparison.Ordinal);
                if (useSnapshot)
                {
                    ((ITokenCacheSnapshotProvider)args.TokenCache).RestoreSnapshot(isA ? snapshotA : snapshotB);
                }
                else
                {
                    args.TokenCache.DeserializeMsalV3(isA ? blobA : blobB, true);
                }
            });
            cache.SetAfterAccessAsync(args =>
            {
                Assert.AreNotSame(cache, args.TokenCache);
                Assert.IsFalse(args.HasStateChanged);
                using var json = JsonDocument.Parse(args.TokenCache.SerializeMsalV3());
                string tenant = json.RootElement.GetProperty("future_node").GetProperty("tenant").GetString();
                Assert.AreEqual(string.Equals(keyA, args.SuggestedCacheKey, StringComparison.Ordinal) ? TenantA : TenantB, tenant);
                Interlocked.Increment(ref completed);
                return Task.CompletedTask;
            });

            // Act
            var first = application.AcquireTokenForClient(new[] { ScopeA }).WithTenantId(TenantA).ExecuteAsync();
            var second = application.AcquireTokenForClient(new[] { ScopeA }).WithTenantId(TenantB).ExecuteAsync();
            try
            {
                await Complete(bothEntered.Task).ConfigureAwait(false);
                Assert.AreEqual(2, Volatile.Read(ref entered));
            }
            finally
            {
                resume.TrySetResult(true);
            }
            var results = await Complete(Task.WhenAll(first, second)).ConfigureAwait(false);

            // Assert
            Assert.AreEqual("token-a", results[0].AccessToken);
            Assert.AreEqual("token-b", results[1].AccessToken);
            Assert.AreEqual(TokenSource.Cache, results[0].AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(TokenSource.Cache, results[1].AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(2, completed);
            Assert.IsEmpty(cache.Accessor.GetAllAccessTokens(), "Read-only hydration must not accumulate state in the root.");
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(false, true)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public async Task ReadKeepsItsPartitionAfterAnotherReadCompletes(bool useSnapshot, bool asyncCallbacks)
        {
            // Arrange
            using var resume = new ManualResetEventSlim();
            var firstReleased = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            int releases = 0;
            var application = CreateApplication(logCallback: (_, message, _) =>
            {
                if (message.IndexOf("[Cache Session Manager] Released cache semaphore", StringComparison.Ordinal) >= 0 &&
                    Interlocked.Increment(ref releases) == 1)
                {
                    firstReleased.TrySetResult(true);
                    if (!resume.Wait(TimeSpan.FromSeconds(15)))
                    {
                        throw new TimeoutException("Timed out waiting to resume the first cache read.");
                    }
                }
            });
            var cache = (ITokenCacheInternal)application.AppTokenCache;
            byte[] blobA = Payload(TenantA, ScopeA, "token-a");
            byte[] blobB = Payload(TenantB, ScopeA, "token-b");
            TokenCacheSnapshot snapshotA = useSnapshot ? Snapshot(blobA) : null;
            TokenCacheSnapshot snapshotB = useSnapshot ? Snapshot(blobB) : null;
            string keyA = CacheKeyFactory.GetAppTokenCacheItemKey(ClientId, TenantA, null);
            void BeforeAccess(TokenCacheNotificationArgs args)
            {
                bool isA = string.Equals(keyA, args.SuggestedCacheKey, StringComparison.Ordinal);
                if (useSnapshot)
                {
                    ((ITokenCacheSnapshotProvider)args.TokenCache).RestoreSnapshot(isA ? snapshotA : snapshotB);
                }
                else
                {
                    args.TokenCache.DeserializeMsalV3(isA ? blobA : blobB, true);
                }
            }
            if (asyncCallbacks)
            {
                cache.SetBeforeAccessAsync(args => { BeforeAccess(args); return Task.CompletedTask; });
            }
            else
            {
                cache.SetBeforeAccess(BeforeAccess);
            }

            // Act
            var first = Task.Run(() => application.AcquireTokenForClient(new[] { ScopeA })
                .WithTenantId(TenantA).ExecuteAsync());
            try
            {
                await Complete(firstReleased.Task).ConfigureAwait(false);
                var second = await Complete(application.AcquireTokenForClient(new[] { ScopeA })
                    .WithTenantId(TenantB).ExecuteAsync()).ConfigureAwait(false);
                Assert.AreEqual("token-b", second.AccessToken);
            }
            finally
            {
                resume.Set();
            }

            // Assert
            var firstResult = await Complete(first).ConfigureAwait(false);
            Assert.AreEqual("token-a", firstResult.AccessToken);
            Assert.AreEqual(TokenSource.Cache, firstResult.AuthenticationResultMetadata.TokenSource);
            Assert.IsEmpty(cache.Accessor.GetAllAccessTokens());
        }

        [TestMethod]
        public async Task SynchronousReadCallbacksRemainSequential()
        {
            // Arrange
            var cache = (ITokenCacheInternal)CreateApplication().AppTokenCache;
            byte[] payload = Payload(TenantA, ScopeA, "token");
            var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var resume = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            int beforeCalls = 0;
            cache.SetBeforeAccess(args =>
            {
                args.TokenCache.DeserializeMsalV3(payload, true);
                if (Interlocked.Increment(ref beforeCalls) == 1)
                {
                    entered.TrySetResult(true);
                }
            });
            cache.SetAfterAccessAsync(async _ =>
            {
                if (Volatile.Read(ref beforeCalls) == 1)
                {
                    await resume.Task.ConfigureAwait(false);
                }
            });
            var first = Session(cache, TenantA).FindAccessTokenAsync();
            await Complete(entered.Task).ConfigureAwait(false);

            // Act
            var second = Session(cache, TenantA).FindAccessTokenAsync();
            try
            {
                Assert.AreEqual(1, Volatile.Read(ref beforeCalls));
                Assert.IsFalse(second.IsCompleted);
            }
            finally
            {
                resume.TrySetResult(true);
            }
            var results = await Complete(Task.WhenAll(first, second)).ConfigureAwait(false);

            // Assert
            Assert.AreEqual("token", results[0]?.Secret);
            Assert.AreEqual("token", results[1]?.Secret);
            Assert.AreEqual(2, beforeCalls);
        }

        [TestMethod]
        public async Task ReadNotificationHasTokensDescribesThePrivateCache()
        {
            // Arrange
            var cache = (ITokenCacheInternal)CreateApplication().AppTokenCache;
            byte[] payload = Payload(TenantA, ScopeA, "token");
            ((ITokenCacheSerializer)cache).DeserializeMsalV3(payload, true);
            bool? beforeHasTokens = null;
            bool? afterHasTokens = null;
            cache.SetBeforeAccess(args =>
            {
                beforeHasTokens = args.HasTokens;
                args.TokenCache.DeserializeMsalV3(payload, true);
            });
            cache.SetAfterAccess(args => afterHasTokens = args.HasTokens);

            // Act
            var result = await Complete(Session(cache, TenantA).FindAccessTokenAsync()).ConfigureAwait(false);

            // Assert
            Assert.AreEqual("token", result?.Secret);
            Assert.IsFalse(beforeHasTokens);
            Assert.IsTrue(afterHasTokens);
            Assert.HasCount(1, cache.Accessor.GetAllAccessTokens());
        }

        [TestMethod]
        public async Task SharedCacheWithoutCallbacksRetainsExistingBehavior()
        {
            // Arrange
            var application = CreateApplication(shared: true);
            var cache = (ITokenCacheInternal)application.AppTokenCache;
            var now = DateTimeOffset.UtcNow;
            const string sharedScope = "api://shared-cache-isolation/.default";
            var token = new MsalAccessTokenCacheItem(
                "login.microsoftonline.com", ClientId, sharedScope, TenantA, "shared-token",
                now, now.AddHours(1), now.AddHours(1), null, "");
            cache.Accessor.SaveAccessToken(token);

            try
            {
                // Act
                var result = await Complete(application.AcquireTokenForClient(new[] { sharedScope }).ExecuteAsync()).ConfigureAwait(false);

                // Assert
                Assert.AreEqual("shared-token", result.AccessToken);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                Assert.Contains(token, cache.Accessor.GetAllAccessTokens());
            }
            finally
            {
                cache.Accessor.DeleteAccessToken(token);
            }
        }

        [TestMethod]
        public async Task ReusedBuilderDoesNotShareThePrivateAccessor()
        {
            // Arrange
            var builder = ConfidentialClientApplicationBuilder.Create(ClientId)
                .WithAuthority("https://login.microsoftonline.com/" + TenantA)
                .WithClientSecret("synthetic-not-a-credential")
                .WithLegacyCacheCompatibility(false);
            var cache = (ITokenCacheInternal)builder.BuildConcrete().AppTokenCache;
            byte[] payload = Payload(TenantA, ScopeA, "private-token");
            cache.SetBeforeAccess(args => args.TokenCache.DeserializeMsalV3(payload, true));

            var sharedCache = (ITokenCacheInternal)CreateApplication(shared: true).AppTokenCache;
            var now = DateTimeOffset.UtcNow;
            var sentinel = new MsalAccessTokenCacheItem(
                "login.microsoftonline.com", ClientId, "api://shared-sentinel/.default", TenantB, "sentinel",
                now, now.AddHours(1), now.AddHours(1), null, "");
            sharedCache.Accessor.SaveAccessToken(sentinel);
            builder.WithCacheOptions(CacheOptions.EnableSharedCacheOptions);

            try
            {
                // Act
                var result = await Complete(Session(cache, TenantA).FindAccessTokenAsync()).ConfigureAwait(false);

                // Assert
                Assert.AreEqual("private-token", result?.Secret);
                Assert.Contains(sentinel, sharedCache.Accessor.GetAllAccessTokens());
            }
            finally
            {
                sharedCache.Accessor.DeleteAccessToken(sentinel);
                foreach (var item in sharedCache.Accessor.GetAllAccessTokens())
                {
                    if (item.Secret == "private-token")
                    {
                        sharedCache.Accessor.DeleteAccessToken(item);
                    }
                }
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task IncompatibleProviderFailsWithOptOutGuidance(bool useCapturedRoot)
        {
            // Arrange
            var cache = (ITokenCacheInternal)CreateApplication().AppTokenCache;
            byte[] payload = Payload(TenantA, ScopeA, "token");
            cache.SetBeforeAccessAsync(args =>
            {
                if (useCapturedRoot)
                {
                    cache.DeserializeMsalV3(payload, true);
                }
                return Task.CompletedTask;
            });

            // Act
            var exception = await Assert.ThrowsExactlyAsync<NotSupportedException>(
                () => Session(cache, TenantA).FindAccessTokenAsync()).ConfigureAwait(false);

            // Assert
            StringAssert.Contains(exception.Message, TokenCache.EnableAppCacheReadIsolationSwitch);
        }

        [TestMethod]
        public async Task FailedReadCanRetryWithoutPublishingToTheRoot()
        {
            // Arrange
            var cache = (ITokenCacheInternal)CreateApplication().AppTokenCache;
            cache.DeserializeMsalV3(Payload(TenantA, ScopeA, "root"), true);
            byte[] payload = Payload(TenantA, ScopeA, "private");
            bool fail = true;
            cache.SetBeforeAccess(args => args.TokenCache.DeserializeMsalV3(payload, true));
            cache.SetAfterAccess(_ =>
            {
                if (fail)
                {
                    throw new InvalidOperationException("synthetic callback failure");
                }
            });
            var session = Session(cache, TenantA);

            // Act
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                () => session.FindAccessTokenAsync()).ConfigureAwait(false);
            Assert.AreEqual("root", cache.Accessor.GetAllAccessTokens()[0].Secret);
            fail = false;
            var result = await Complete(session.FindAccessTokenAsync()).ConfigureAwait(false);

            // Assert
            Assert.AreEqual("private", result?.Secret);
            Assert.AreEqual("root", cache.Accessor.GetAllAccessTokens()[0].Secret);
        }

        [TestMethod]
        public async Task CancelledReadReleasesItsWaitAndAllowsLaterReads()
        {
            // Arrange
            var application = CreateApplication();
            var cache = (ITokenCacheInternal)application.AppTokenCache;
            byte[] payload = Payload(TenantA, ScopeA, "token");
            cache.SetBeforeAccessAsync(args =>
            {
                args.TokenCache.DeserializeMsalV3(payload, true);
                return Task.CompletedTask;
            });
            using var cancellation = new CancellationTokenSource();
            await cache.Semaphore.WaitAsync(CancellationToken.None).ConfigureAwait(false);

            try
            {
                // Act
                var blocked = application.AcquireTokenForClient(new[] { ScopeA }).ExecuteAsync(cancellation.Token);
                Assert.IsFalse(blocked.IsCompleted);
                cancellation.Cancel();
                await Assert.ThrowsAsync<OperationCanceledException>(() => Complete(blocked)).ConfigureAwait(false);
            }
            finally
            {
                cache.Semaphore.Release();
            }

            // Assert
            var result = await Complete(application.AcquireTokenForClient(new[] { ScopeA }).ExecuteAsync()).ConfigureAwait(false);
            Assert.AreEqual("token", result.AccessToken);
        }

        [TestMethod]
        public async Task WriteHydratesRootAndInvalidatesThePriorReadView()
        {
            // Arrange
            var cache = (ITokenCacheInternal)CreateApplication().AppTokenCache;
            byte[] persisted = Payload(TenantA, ScopeA, "old");
            cache.SetBeforeAccessAsync(args =>
            {
                args.TokenCache.DeserializeMsalV3(persisted, true);
                return Task.CompletedTask;
            });
            cache.SetAfterAccessAsync(args =>
            {
                if (args.HasStateChanged)
                {
                    Assert.AreSame(cache, args.TokenCache);
                    persisted = args.TokenCache.SerializeMsalV3();
                }
                return Task.CompletedTask;
            });
            var request = Request(cache, TenantA);
            var session = new CacheSessionManager(cache, request);
            var before = await session.FindAccessTokenAsync().ConfigureAwait(false);
            Assert.IsEmpty(cache.Accessor.GetAllAccessTokens());

            // Act
            await session.SaveTokenResponseAsync(Response(ScopeB, "new")).ConfigureAwait(false);
            request.Scope.Clear();
            request.Scope.Add(ScopeB);
            var after = await session.FindAccessTokenAsync().ConfigureAwait(false);

            // Assert
            Assert.AreEqual("old", before?.Secret);
            Assert.AreEqual("new", after?.Secret);
            Assert.HasCount(2, cache.Accessor.GetAllAccessTokens());
            using var json = JsonDocument.Parse(persisted);
            Assert.AreEqual(TenantA, json.RootElement.GetProperty("future_node").GetProperty("tenant").GetString());
        }

        [TestMethod]
        public async Task AsyncReadCallbackIsCoordinatedWithWrites()
        {
            // Arrange
            var cache = (ITokenCacheInternal)CreateApplication().AppTokenCache;
            byte[] persisted = Payload(TenantA, ScopeA, "old");
            var readEntered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var resume = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            bool writeEntered = false;
            cache.SetBeforeAccessAsync(async args =>
            {
                args.TokenCache.DeserializeMsalV3(persisted, true);
                if (args.HasStateChanged)
                {
                    writeEntered = true;
                }
                else
                {
                    readEntered.TrySetResult(true);
                    await resume.Task.ConfigureAwait(false);
                }
            });
            cache.SetAfterAccessAsync(args =>
            {
                if (args.HasStateChanged)
                {
                    persisted = args.TokenCache.SerializeMsalV3();
                }
                return Task.CompletedTask;
            });
            var read = Session(cache, TenantA).FindAccessTokenAsync();
            await Complete(readEntered.Task).ConfigureAwait(false);

            // Act
            var write = Session(cache, TenantA).SaveTokenResponseAsync(Response(ScopeB, "new"));
            try
            {
                Assert.IsFalse(write.IsCompleted);
                Assert.IsFalse(writeEntered);
            }
            finally
            {
                resume.TrySetResult(true);
            }
            await Complete(Task.WhenAll(read, write)).ConfigureAwait(false);

            // Assert
            Assert.IsTrue(writeEntered);
            Assert.HasCount(2, cache.Accessor.GetAllAccessTokens());
        }

        private static ConfidentialClientApplication CreateApplication(
            bool synchronize = true, bool legacy = false, bool shared = false, LogCallback logCallback = null)
        {
            var builder = ConfidentialClientApplicationBuilder.Create(ClientId)
                .WithAuthority("https://login.microsoftonline.com/" + TenantA)
                .WithClientSecret("synthetic-not-a-credential")
                .WithInstanceDiscovery(false)
                .WithHttpClientFactory(new NoNetworkFactory())
                .WithLegacyCacheCompatibility(legacy)
                .WithCacheSynchronization(synchronize);
            if (shared)
            {
                builder.WithCacheOptions(CacheOptions.EnableSharedCacheOptions);
            }
            if (logCallback is not null)
            {
                builder.WithLogging(logCallback, LogLevel.Verbose);
            }
            return builder.BuildConcrete();
        }

        private static AuthenticationRequestParameters Request(ITokenCacheInternal cache, string tenant)
        {
            return TestCommon.CreateAuthenticationRequestParameters(
                ((TokenCache)cache).ServiceBundle,
                Authority.CreateAuthority("https://login.microsoftonline.com/" + tenant),
                new HashSet<string> { ScopeA },
                apiID: ApiEvent.ApiIds.AcquireTokenForClient);
        }

        private static CacheSessionManager Session(ITokenCacheInternal cache, string tenant)
        {
            return new CacheSessionManager(cache, Request(cache, tenant));
        }

        private static byte[] Payload(string tenant, string scope, string secret)
        {
            var cache = (ITokenCacheInternal)CreateApplication().AppTokenCache;
            var now = DateTimeOffset.UtcNow;
            cache.Accessor.SaveAccessToken(new MsalAccessTokenCacheItem(
                "login.microsoftonline.com", ClientId, scope, tenant, secret,
                now, now.AddHours(1), now.AddHours(1), null, ""));
            string json = Encoding.UTF8.GetString(cache.SerializeMsalV3());
            return Encoding.UTF8.GetBytes(json.Substring(0, json.Length - 1) +
                ",\"future_node\":{\"tenant\":\"" + tenant + "\"}}");
        }

        private static TokenCacheSnapshot Snapshot(byte[] payload)
        {
            var cache = (ITokenCacheInternal)CreateApplication().AppTokenCache;
            cache.DeserializeMsalV3(payload, true);
            return ((ITokenCacheSnapshotProvider)cache).CreateSnapshot();
        }

        private static MsalTokenResponse Response(string scope, string accessToken)
        {
            return new MsalTokenResponse { AccessToken = accessToken, Scope = scope, ExpiresIn = 3600, TokenType = "Bearer" };
        }

        private static async Task<T> Complete<T>(Task<T> task)
        {
            using var timeout = new CancellationTokenSource();
            var delay = Task.Delay(TimeSpan.FromSeconds(10), timeout.Token);
            Assert.AreSame(task, await Task.WhenAny(task, delay).ConfigureAwait(false),
                "The cache operation did not complete within the bounded wait.");
            timeout.Cancel();
            return await task.ConfigureAwait(false);
        }

        private static async Task Complete(Task task)
        {
            using var timeout = new CancellationTokenSource();
            var delay = Task.Delay(TimeSpan.FromSeconds(10), timeout.Token);
            Assert.AreSame(task, await Task.WhenAny(task, delay).ConfigureAwait(false),
                "The cache operation did not complete within the bounded wait.");
            timeout.Cancel();
            await task.ConfigureAwait(false);
        }

        private sealed class NoNetworkFactory : IMsalHttpClientFactory
        {
            public HttpClient GetHttpClient()
            {
                throw new InvalidOperationException("The cached-token tests must not access the network.");
            }
        }
    }
}
