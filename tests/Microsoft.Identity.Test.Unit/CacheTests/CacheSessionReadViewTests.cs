// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.CacheTests
{
    [TestClass]
    [DoNotParallelize]
    public class CacheSessionReadViewTests
    {
        private const string Setting = TokenCache.DisableCacheReadIsolationSwitch;
        private const string ClientId = "22222222-2222-2222-2222-222222222222";
        private const string HomeId = "user.11111111-1111-1111-1111-111111111111";
        private bool _previousDisabled;

        [TestInitialize]
        public void Initialize()
        {
            AppContext.TryGetSwitch(Setting, out _previousDisabled);
            AppContext.SetSwitch(Setting, false);
        }

        [TestCleanup]
        public void Cleanup()
        {
            AppContext.SetSwitch(Setting, _previousDisabled);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task SessionPinsItsReadState(bool isolated)
        {
            SetIsolation(isolated);
            var cache = CreateCache();
            cache.SetBeforeAccess(args => args.TokenCache.DeserializeMsalV3(Blob("before"), true));
            var session = CreateSession(cache);
            Assert.AreEqual("before", (await session.GetAccountsAsync().ConfigureAwait(false)).Single().Username);
            cache.DeserializeMsalV3(Blob("after"), true);
            Assert.AreEqual(isolated ? "before" : "after",
                (await session.GetAccountsAsync().ConfigureAwait(false)).Single().Username);
        }

        [TestMethod]
        public async Task PrivateViewPreservesUnknownJson()
        {
            var cache = CreateCache();
            cache.SetBeforeAccess(args => args.TokenCache.DeserializeMsalV3(Blob("before"), true));
            var session = CreateSession(cache);
            Assert.AreEqual(1, (await session.GetAccountsAsync().ConfigureAwait(false)).Count());
            var field = typeof(CacheSessionManager).GetField("_readView", BindingFlags.Instance | BindingFlags.NonPublic);
            var view = (ITokenCacheInternal)field.GetValue(session);
            using var json = JsonDocument.Parse(view.SerializeMsalV3());
            Assert.AreEqual(42, json.RootElement.GetProperty("future_node").GetProperty("value").GetInt32());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task UnrelatedCacheSerializationIsAllowed(bool isolated)
        {
            SetIsolation(isolated);
            var cache = CreateCache();
            var unrelated = CreateCache();
            cache.SetBeforeAccess(args =>
            {
                args.TokenCache.DeserializeMsalV3(Blob("before"), true);
                unrelated.SerializeMsalV3();
            });
            Assert.AreEqual(1, (await CreateSession(cache).GetAccountsAsync().ConfigureAwait(false)).Count());
        }

        [TestMethod]
        public async Task NestedUnrelatedAppCacheDoesNotValidateTheParentsPendingHydration()
        {
            var cache = CreateCache();
            var unrelated = new TokenCache(((TokenCache)CreateCache()).ServiceBundle, true);
            unrelated.SetBeforeAccess(args => args.TokenCache.DeserializeMsalV3(null, true));
            cache.SetBeforeAccessAsync(async args =>
            {
                Assert.AreEqual(0, (await CreateSession(unrelated).GetAccountsAsync().ConfigureAwait(false)).Count());
                args.TokenCache.DeserializeMsalV3(Blob("before"), true);
            });
            Assert.AreEqual(1, (await CreateSession(cache).GetAccountsAsync().ConfigureAwait(false)).Count());
        }

        [TestMethod]
        [DataRow("SerializeAdalV3")]
        [DataRow("DeserializeAdalV3")]
        [DataRow("SerializeMsalV2")]
        [DataRow("DeserializeMsalV2")]
        [DataRow("SerializeMsalV3")]
        [DataRow("DeserializeMsalV3")]
        [DataRow("CreateSnapshot")]
        [DataRow("RestoreSnapshot")]
        public async Task AllCapturedRootSerializerEntryPointsAreGuarded(string operation)
        {
            var cache = CreateCache();
            cache.SetBeforeAccess(args =>
            {
                args.TokenCache.DeserializeMsalV3(Blob("before"), true);
#pragma warning disable CS0618
                switch (operation)
                {
                    case "SerializeAdalV3": cache.SerializeAdalV3(); break;
                    case "DeserializeAdalV3": cache.DeserializeAdalV3(Array.Empty<byte>()); break;
                    case "SerializeMsalV2": cache.SerializeMsalV2(); break;
                    case "DeserializeMsalV2": cache.DeserializeMsalV2(Array.Empty<byte>()); break;
                    case "SerializeMsalV3": cache.SerializeMsalV3(); break;
                    case "DeserializeMsalV3": cache.DeserializeMsalV3(null, true); break;
                    case "CreateSnapshot": ((ITokenCacheSnapshotProvider)cache).CreateSnapshot(); break;
                    case "RestoreSnapshot": ((ITokenCacheSnapshotProvider)cache).RestoreSnapshot(null); break;
                    default: Assert.Fail("Unknown test operation."); break;
                }
#pragma warning restore CS0618
            });
            await ExpectUnsupported(() => CreateSession(cache).GetAccountsAsync(), "captured/shared").ConfigureAwait(false);
        }

        [TestMethod]
        public async Task IncrementalHydrationCannotSilentlyUseAnEmptyPrivateBase()
        {
            var cache = CreateCache();
            cache.SetBeforeAccess(args => args.TokenCache.DeserializeMsalV3(Blob("before"), false));
            await ExpectUnsupported(() => CreateSession(cache).GetAccountsAsync(), "clear-existing").ConfigureAwait(false);
        }

        [TestMethod]
        [DataRow("msal-v3")]
        [DataRow("snapshot")]
        [DataRow("empty")]
        public async Task AwaitedHydrationDoesNotRequireExecutionContextFlow(string shape)
        {
            var cache = CreateCache();
            var seed = CreateCache();
            seed.DeserializeMsalV3(Blob("before"), true);
            var snapshot = ((ITokenCacheSnapshotProvider)seed).CreateSnapshot();
            cache.SetBeforeAccessAsync(async args =>
            {
                Task hydration;
                using (ExecutionContext.SuppressFlow())
                {
                    hydration = Task.Run(() =>
                    {
                        switch (shape)
                        {
                            case "msal-v3": args.TokenCache.DeserializeMsalV3(Blob("before"), true); break;
                            case "snapshot": ((ITokenCacheSnapshotProvider)args.TokenCache).RestoreSnapshot(snapshot); break;
                            case "empty": args.TokenCache.DeserializeMsalV3(null, true); break;
                            default: Assert.Fail("Unknown hydration shape."); break;
                        }
                    });
                }
                await hydration.ConfigureAwait(false);
            });

            var accounts = (await CreateSession(cache).GetAccountsAsync().ConfigureAwait(false)).ToArray();
            Assert.HasCount(shape == "empty" ? 0 : 1, accounts);
            if (accounts.Length != 0)
                Assert.AreEqual("before", accounts[0].Username);
            Assert.IsEmpty(cache.Accessor.GetAllAccounts());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task SuppressedFlowStillRejectsIncompleteHydration(bool legacyFormat)
        {
            var cache = CreateCache();
            var seed = CreateCache();
            seed.DeserializeMsalV3(Blob("before"), true);
#pragma warning disable CS0618
            byte[] legacyBytes = seed.SerializeMsalV2();
#pragma warning restore CS0618
            cache.SetBeforeAccessAsync(async args =>
            {
                Task hydration;
                using (ExecutionContext.SuppressFlow())
                {
                    hydration = Task.Run(() =>
                    {
                        if (legacyFormat)
                        {
#pragma warning disable CS0618
                            args.TokenCache.DeserializeMsalV2(legacyBytes);
#pragma warning restore CS0618
                        }
                        else
                        {
                            args.TokenCache.DeserializeMsalV3(Blob("before"), false);
                        }
                    });
                }
                await hydration.ConfigureAwait(false);
            });
            await ExpectUnsupported(() => CreateSession(cache).GetAccountsAsync(), "clear-existing").ConfigureAwait(false);
        }

        [TestMethod]
        public async Task RetriedHydrationCannotReuseTheFailedAttemptsCompletion()
        {
            var cache = CreateCache();
            bool failPublication = true;
            cache.SetBeforeAccess(args =>
            {
                if (failPublication)
                    args.TokenCache.DeserializeMsalV3(Blob("before"), true);
            });
            cache.SetAfterAccess(_ =>
            {
                if (failPublication)
                    throw new InvalidOperationException("synthetic-publication");
            });
            var session = CreateSession(cache);
            bool failed = false;
            try { await session.GetAccountsAsync().ConfigureAwait(false); }
            catch (InvalidOperationException exception) when (exception.Message == "synthetic-publication") { failed = true; }
            Assert.IsTrue(failed);
            failPublication = false;
            await ExpectUnsupported(() => session.GetAccountsAsync(), "hydration").ConfigureAwait(false);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task CacheWithoutNotificationsKeepsItsWarmState(bool isolated)
        {
            SetIsolation(isolated);
            var cache = CreateCache();
            cache.DeserializeMsalV3(Blob("before"), true);
            Assert.AreEqual(1, (await CreateSession(cache).GetAccountsAsync().ConfigureAwait(false)).Count());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task CapturedMsalV2TargetCannotSilentlyLoseTheAccount(bool isolated)
        {
            SetIsolation(isolated);
            var cache = CreateCache();
            cache.DeserializeMsalV3(Blob("before"), true);
#pragma warning disable CS0618
            var serialized = cache.SerializeMsalV2();
            cache.SetBeforeAccess(args => cache.DeserializeMsalV2(serialized));
#pragma warning restore CS0618
            var session = CreateSession(cache);
            if (isolated)
                await ExpectUnsupported(() => session.GetAccountsAsync(), "captured/shared").ConfigureAwait(false);
            else
                Assert.AreEqual(1, (await session.GetAccountsAsync().ConfigureAwait(false)).Count());
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(true, false)]
        [DataRow(false, true)]
        [DataRow(true, true)]
        public async Task MissingHydrationCannotSilentlyLoseWarmState(bool isolated, bool beforeCallback)
        {
            SetIsolation(isolated);
            var cache = CreateCache();
            cache.DeserializeMsalV3(Blob("before"), true);
            if (beforeCallback)
                cache.SetBeforeAccess(_ => { });
            else
                cache.SetAfterAccess(_ => { });
            var session = CreateSession(cache);
            if (isolated)
                await ExpectUnsupported(() => session.GetAccountsAsync(), "hydration").ConfigureAwait(false);
            else
                Assert.AreEqual(1, (await session.GetAccountsAsync().ConfigureAwait(false)).Count());
        }

        [TestMethod]
        public async Task AfterAccessFailureAllowsTheSameSessionToRetry()
        {
            var cache = CreateCache();
            bool fail = true;
            cache.SetBeforeAccess(args => args.TokenCache.DeserializeMsalV3(Blob("before"), true));
            cache.SetAfterAccess(_ =>
            {
                if (fail)
                    throw new InvalidOperationException("synthetic-after-access");
            });
            var session = CreateSession(cache);
            bool observed = false;
            try { await session.GetAccountsAsync().ConfigureAwait(false); }
            catch (InvalidOperationException exception) when (exception.Message == "synthetic-after-access") { observed = true; }
            Assert.IsTrue(observed);
            fail = false;
            Assert.AreEqual(1, (await session.GetAccountsAsync().ConfigureAwait(false)).Count());
        }

        [TestMethod]
        public async Task CancelledSharedGateWaitDoesNotReleaseTheOwner()
        {
            SetIsolation(false);
            var cache = CreateCache();
            cache.SetBeforeAccess(args => args.TokenCache.DeserializeMsalV3(Blob("before"), true));
            using var cancellation = new CancellationTokenSource();
            await cache.Semaphore.WaitAsync(CancellationToken.None).ConfigureAwait(false);
            try
            {
                var waiting = CreateSession(cache, cancellation.Token).GetAccountsAsync();
                cancellation.Cancel();
                bool observed = false;
                try { await waiting.ConfigureAwait(false); }
                catch (OperationCanceledException) { observed = true; }
                Assert.IsTrue(observed);
            }
            finally { cache.Semaphore.Release(); }
            Assert.AreEqual(1, (await CreateSession(cache).GetAccountsAsync().ConfigureAwait(false)).Count());
        }

        [TestMethod]
        public async Task SuccessfulWriteInvalidatesTheSameSessionsView()
        {
            var cache = CreateCache();
            byte[] persisted = Blob("before");
            cache.SetBeforeAccess(args => args.TokenCache.DeserializeMsalV3(persisted, true));
            cache.SetAfterAccess(args =>
            {
                if (args.HasStateChanged)
                    persisted = Blob("after");
            });
            var session = CreateSession(cache);
            Assert.AreEqual("before", (await session.GetAccountsAsync().ConfigureAwait(false)).Single().Username);
            await session.SaveTokenResponseAsync(TestConstants.CreateMsalTokenResponse()).ConfigureAwait(false);
            Assert.AreEqual("after", (await session.GetAccountsAsync().ConfigureAwait(false)).Single().Username);
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public async Task RefreshPreservesPersistedTenantProfiles(bool isolated, bool changedBeforeWrite)
        {
            SetIsolation(isolated);
            const string homeTenant = "11111111-1111-1111-1111-111111111111";
            const string guestTenant = "33333333-3333-3333-3333-333333333333";
            const string addedTenant = "44444444-4444-4444-4444-444444444444";
            const string authority = "https://login.microsoftonline.com/" + homeTenant;
            var cache = CreateCache();
            cache.DeserializeMsalV3(Blob("before"), true);
            foreach (string tenant in new[] { homeTenant, guestTenant })
            {
                var seed = TestConstants.CreateMsalTokenResponse(tenant);
                cache.Accessor.SaveIdToken(new MsalIdTokenCacheItem(
                    "login.microsoftonline.com", ClientId, seed, tenant, HomeId));
            }
            byte[] persisted = cache.SerializeMsalV3();
            cache.Accessor.SaveIdToken(new MsalIdTokenCacheItem(
                "login.microsoftonline.com", ClientId, TestConstants.CreateMsalTokenResponse(addedTenant), addedTenant, HomeId));
            byte[] changed = cache.SerializeMsalV3();
            cache.DeserializeMsalV3(null, true);
            cache.SetBeforeAccess(args => args.TokenCache.DeserializeMsalV3(
                changedBeforeWrite && args.HasStateChanged ? changed : persisted, true));
            cache.SetAfterAccess(args =>
            {
                if (args.HasStateChanged)
                    persisted = args.TokenCache.SerializeMsalV3();
            });
            var session = CreateSession(cache, authority: authority);
            var before = (await session.GetAccountsAsync().ConfigureAwait(false)).Single();
            Assert.AreEqual(2, before.GetTenantProfiles().Count());
            Assert.IsNotNull(await session.FindRefreshTokenAsync().ConfigureAwait(false));
            if (isolated)
                Assert.IsEmpty(cache.Accessor.GetAllIdTokens(HomeId));

            var response = TestConstants.CreateMsalTokenResponse(homeTenant);
            response.ClientInfo = TestConstants.CreateClientInfo("user", homeTenant);
            response.IdToken = TestConstants.CreateIdToken("updated-user", "updated@example.test", homeTenant);
            var result = await session.SaveTokenResponseAsync(response).ConfigureAwait(false);

            var profiles = result.Item3.GetTenantProfiles().ToArray();
            var expectedTenants = changedBeforeWrite
                ? new[] { homeTenant, guestTenant, addedTenant }
                : new[] { homeTenant, guestTenant };
            CollectionAssert.AreEquivalent(expectedTenants,
                profiles.Select(profile => profile.TenantId).ToArray());
            Assert.AreEqual("updated-user", profiles.Single(profile => profile.TenantId == homeTenant)
                .ClaimsPrincipal.FindFirst("oid").Value);
            Assert.AreEqual(expectedTenants.Length, (await CreateSession(cache, authority: authority).GetAccountsAsync().ConfigureAwait(false))
                .Single().GetTenantProfiles().Count());
        }

        [TestMethod]
        public async Task SharedReadersDelayWritersAndQueuedWritersBlockNewReaders()
        {
            var gate = new OptionalSemaphoreSlim(true, allowSharedReaders: true);
            using var first = await gate.AcquireReadAsync(CancellationToken.None).ConfigureAwait(false);
            using var second = await gate.AcquireReadAsync(CancellationToken.None).ConfigureAwait(false);
            var writer = gate.WaitAsync(CancellationToken.None);
            var lateReader = gate.AcquireReadAsync(CancellationToken.None);
            Assert.IsFalse(writer.IsCompleted);
            Assert.IsFalse(lateReader.IsCompleted);
            first.Dispose();
            Assert.IsFalse(writer.IsCompleted);
            second.Dispose();
            await Complete(writer).ConfigureAwait(false);
            try { Assert.IsFalse(lateReader.IsCompleted); }
            finally { gate.Release(); }
            await Complete(lateReader).ConfigureAwait(false);
            using var last = await lateReader.ConfigureAwait(false);
        }

        [TestMethod]
        public async Task CancelledWriterPreservesReadersAndReleasesItsTurnstile()
        {
            var gate = new OptionalSemaphoreSlim(true, allowSharedReaders: true);
            using var first = await gate.AcquireReadAsync(CancellationToken.None).ConfigureAwait(false);
            using var cancellation = new CancellationTokenSource();
            var writer = gate.WaitAsync(cancellation.Token);
            cancellation.Cancel();
            await ExpectCancellation(writer).ConfigureAwait(false);
            var next = gate.AcquireReadAsync(CancellationToken.None);
            await Complete(next).ConfigureAwait(false);
            using var second = await next.ConfigureAwait(false);
            first.Dispose();
            Assert.AreEqual(0, gate.CurrentCount);
            second.Dispose();
            second.Dispose();
            Assert.AreEqual(1, gate.CurrentCount);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task CancelledReaderPreservesSynchronousAndAsyncWriterOwnership(bool synchronous)
        {
            var gate = new OptionalSemaphoreSlim(true, allowSharedReaders: true);
            if (synchronous)
                gate.Wait();
            else
                await gate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
            try
            {
                using var cancellation = new CancellationTokenSource();
                var reader = gate.AcquireReadAsync(cancellation.Token);
                cancellation.Cancel();
                await ExpectCancellation(reader).ConfigureAwait(false);
                Assert.AreEqual(0, gate.CurrentCount);
            }
            finally { gate.Release(); }
            using var recovered = await gate.AcquireReadAsync(CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task MixedReadersAndWritersNeverOverlap(bool synchronousWriters)
        {
            var gate = new OptionalSemaphoreSlim(true, allowSharedReaders: true);
            int readers = 0;
            int writers = 0;
            var workers = Enumerable.Range(0, 20).Select(index => Task.Run(async () =>
            {
                for (int iteration = 0; iteration < 32; iteration++)
                {
                    if (index < 4)
                    {
                        if (synchronousWriters)
                            gate.Wait();
                        else
                            await gate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
                        try
                        {
                            Assert.AreEqual(0, Volatile.Read(ref readers));
                            Assert.AreEqual(1, Interlocked.Increment(ref writers));
                            await Task.Yield();
                            Assert.AreEqual(0, Volatile.Read(ref readers));
                            Interlocked.Decrement(ref writers);
                        }
                        finally { gate.Release(); }
                    }
                    else
                    {
                        using var lease = await gate.AcquireReadAsync(CancellationToken.None).ConfigureAwait(false);
                        Interlocked.Increment(ref readers);
                        try
                        {
                            Assert.AreEqual(0, Volatile.Read(ref writers));
                            await Task.Yield();
                            Assert.AreEqual(0, Volatile.Read(ref writers));
                        }
                        finally { Interlocked.Decrement(ref readers); }
                    }
                }
            })).ToArray();
            await Complete(Task.WhenAll(workers)).ConfigureAwait(false);
            Assert.AreEqual(1, gate.CurrentCount);
        }

        [TestMethod]
        public async Task CancelledIsolatedHydrationWaitReleasesItsPrivateGate()
        {
            var cache = CreateCache();
            cache.SetBeforeAccess(args => args.TokenCache.DeserializeMsalV3(Blob("before"), true));
            using var cancellation = new CancellationTokenSource();
            var session = CreateSession(cache, cancellation.Token);
            await cache.Semaphore.WaitAsync(CancellationToken.None).ConfigureAwait(false);
            try
            {
                var read = session.GetAccountsAsync();
                cancellation.Cancel();
                await ExpectCancellation(read).ConfigureAwait(false);
                Assert.AreEqual(0, cache.Semaphore.CurrentCount);
                var field = typeof(CacheSessionManager).GetField("_readView", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.AreEqual(1, ((ITokenCacheInternal)field.GetValue(session)).Semaphore.CurrentCount);
            }
            finally { cache.Semaphore.Release(); }
            Assert.AreEqual(1, (await CreateSession(cache).GetAccountsAsync().ConfigureAwait(false)).Count());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task CallbackFailureReleasesTheRootReadLease(bool afterAccess)
        {
            var cache = CreateCache();
            bool fail = true;
            cache.SetBeforeAccess(args =>
            {
                if (fail && !afterAccess)
                    throw new InvalidOperationException("synthetic-callback");
                args.TokenCache.DeserializeMsalV3(Blob("before"), true);
            });
            cache.SetAfterAccess(_ =>
            {
                if (fail && afterAccess)
                    throw new InvalidOperationException("synthetic-callback");
            });
            var session = CreateSession(cache);
            bool observed = false;
            try { await session.GetAccountsAsync().ConfigureAwait(false); }
            catch (InvalidOperationException exception) when (exception.Message == "synthetic-callback") { observed = true; }
            Assert.IsTrue(observed);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await cache.Semaphore.WaitAsync(timeout.Token).ConfigureAwait(false);
            cache.Semaphore.Release();
            fail = false;
            Assert.AreEqual(1, (await session.GetAccountsAsync().ConfigureAwait(false)).Count());
        }

        [TestMethod]
        public async Task RootTokenWriteWaitsForTheEntireIsolatedHydration()
        {
            var cache = CreateCache();
            byte[] persisted = Blob("before");
            var actor = new AsyncLocal<bool>();
            var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var resume = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            cache.SetBeforeAccessAsync(async args =>
            {
                byte[] captured = persisted;
                if (actor.Value)
                {
                    entered.TrySetResult(true);
                    await resume.Task.ConfigureAwait(false);
                }
                args.TokenCache.DeserializeMsalV3(captured, true);
            });
            cache.SetAfterAccess(args =>
            {
                if (args.HasStateChanged)
                    persisted = Blob("after");
            });
            var read = Task.Run(async () =>
            {
                actor.Value = true;
                return await CreateSession(cache).GetAccountsAsync().ConfigureAwait(false);
            });
            await Complete(entered.Task).ConfigureAwait(false);
            var write = CreateSession(cache).SaveTokenResponseAsync(TestConstants.CreateMsalTokenResponse());
            try { Assert.IsFalse(write.IsCompleted); }
            finally { resume.TrySetResult(true); }
            await Complete(read).ConfigureAwait(false);
            await Complete(write).ConfigureAwait(false);
            Assert.AreEqual("before", (await read.ConfigureAwait(false)).Single().Username);
            Assert.AreEqual("after", (await CreateSession(cache).GetAccountsAsync().ConfigureAwait(false)).Single().Username);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task SynchronizationDisabledRetainsExistingBehavior(bool callbacks)
        {
            var cache = CreateCache(synchronize: false);
            cache.DeserializeMsalV3(Blob("before"), true);
            if (callbacks)
            {
                cache.SetBeforeAccess(args => args.TokenCache.DeserializeMsalV3(Blob("before"), true));
                Assert.AreEqual(1, (await CreateSession(cache).GetAccountsAsync().ConfigureAwait(false)).Count());
            }
            else
            {
                Assert.AreEqual(1, (await CreateSession(cache).GetAccountsAsync().ConfigureAwait(false)).Count());
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task IsolationChoiceIsCapturedAtCacheConstruction(bool isolated)
        {
            SetIsolation(isolated);
            var cache = CreateCache();
            SetIsolation(!isolated);
            cache.SetBeforeAccess(args => args.TokenCache.DeserializeMsalV3(Blob("before"), true));
            var session = CreateSession(cache);
            Assert.AreEqual("before", (await session.GetAccountsAsync().ConfigureAwait(false)).Single().Username);
            cache.DeserializeMsalV3(Blob("after"), true);
            Assert.AreEqual(isolated ? "before" : "after",
                (await session.GetAccountsAsync().ConfigureAwait(false)).Single().Username);
        }

        [TestMethod]
        public async Task NestedHydrationCannotSerializeAnOuterSourceCache()
        {
            var parent = CreateCache();
            var parentView = ((TokenCache)parent).CreateReadView();
            using (parentView.BeginReadHydration(parent))
            {
                var child = CreateCache();
                var childView = ((TokenCache)child).CreateReadView();
                using (childView.BeginReadHydration(child))
                {
                    await Task.Yield();
                    await ExpectUnsupported(() =>
                    {
                        parent.SerializeMsalV3();
                        return Task.CompletedTask;
                    }, "captured/shared").ConfigureAwait(false);
                    CreateCache().SerializeMsalV3();
                }
                ((ITokenCacheSerializer)parentView).DeserializeMsalV3(Blob("before"), true);
                parentView.ValidateReadHydration();
            }
            parent.SerializeMsalV3();
        }

        [TestMethod]
        public async Task HydrationGuardEndsBeforePostReleaseLogging()
        {
            ITokenCacheInternal cache = null;
            bool loggedRelease = false;
            NotSupportedException unexpectedGuard = null;
            cache = CreateCache(logCallback: (_, message, __) =>
            {
                if (message.Contains("[Cache Session Manager] Released cache semaphore", StringComparison.Ordinal))
                {
                    loggedRelease = true;
                    try { cache.SerializeMsalV3(); }
                    catch (NotSupportedException exception) { unexpectedGuard = exception; }
                }
            });
            cache.SetBeforeAccess(args => args.TokenCache.DeserializeMsalV3(Blob("before"), true));
            cache.SetAfterAccessAsync(_ => ExpectUnsupported(() =>
            {
                cache.SerializeMsalV3();
                return Task.CompletedTask;
            }, "captured/shared"));

            Assert.AreEqual(1, (await CreateSession(cache).GetAccountsAsync().ConfigureAwait(false)).Count());
            Assert.IsTrue(loggedRelease);
            Assert.IsNull(unexpectedGuard, "The hydration-only guard leaked into post-release logging.");
        }

        [TestMethod]
        public async Task CapturedExecutionContextDoesNotExtendHydrationLifetime()
        {
            // Arrange
            var cache = CreateCache();
            ExecutionContext captured = null;
            cache.SetBeforeAccess(args =>
            {
                args.TokenCache.DeserializeMsalV3(Blob("private"), true);
                captured = ExecutionContext.Capture();
            });

            // Act
            Assert.AreEqual(1, (await CreateSession(cache).GetAccountsAsync().ConfigureAwait(false)).Count());

            // Assert
            Assert.IsNotNull(captured);
            ExecutionContext.Run(captured, _ => cache.SerializeMsalV3(), null);
        }

        [TestMethod]
        public async Task CapturedChildContextRetainsAnActiveAncestorGuard()
        {
            // Arrange
            var parent = CreateCache();
            var parentView = ((TokenCache)parent).CreateReadView();
            using (parentView.BeginReadHydration(parent))
            {
                var child = CreateCache();
                var childView = ((TokenCache)child).CreateReadView();
                ExecutionContext captured;
                using (childView.BeginReadHydration(child))
                {
                    captured = ExecutionContext.Capture();
                }

                // Act
                Task guarded = null;
                ExecutionContext.Run(captured, _ => guarded = ExpectUnsupported(() =>
                    Task.FromResult(parent.SerializeMsalV3()), "captured/shared"), null);

                // Assert
                await guarded.ConfigureAwait(false);
            }
        }

        [TestMethod]
        [DataRow("captured-root")]
        [DataRow("incremental")]
        [DataRow("load-once")]
        [DataRow("after-only")]
        [DataRow("no-op")]
        [DataRow("msal-v2")]
        [DataRow("structured")]
        public async Task OptOutPreservesExistingSerializerContracts(string shape)
        {
            AppContext.SetSwitch(Setting, true);
            var cache = CreateCache();
            cache.DeserializeMsalV3(Blob("before"), true);
#pragma warning disable CS0618
            byte[] v2 = cache.SerializeMsalV2();
#pragma warning restore CS0618
            var snapshot = ((ITokenCacheSnapshotProvider)cache).CreateSnapshot();
            int calls = 0;
            TokenCacheCallback callback = args =>
            {
                Assert.AreSame(cache, args.TokenCache);
                calls++;
                switch (shape)
                {
                    case "captured-root": cache.DeserializeMsalV3(Blob("before"), true); break;
                    case "incremental": args.TokenCache.DeserializeMsalV3(Blob("before"), false); break;
                    case "load-once":
                        if (calls == 1) args.TokenCache.DeserializeMsalV3(Blob("before"), true);
                        break;
                    case "after-only": args.TokenCache.DeserializeMsalV3(Blob("before"), true); break;
                    case "no-op": break;
#pragma warning disable CS0618
                    case "msal-v2": args.TokenCache.DeserializeMsalV2(v2); break;
#pragma warning restore CS0618
                    case "structured": ((ITokenCacheSnapshotProvider)args.TokenCache).RestoreSnapshot(snapshot); break;
                    default: Assert.Fail("Unknown serializer shape."); break;
                }
            };
            if (shape == "after-only")
                cache.SetAfterAccess(callback);
            else
                cache.SetBeforeAccess(callback);

            var session = CreateSession(cache);
            Assert.AreEqual("before", (await session.GetAccountsAsync().ConfigureAwait(false)).Single().Username);
            cache.DeserializeMsalV3(Blob("after"), true);
            Assert.AreEqual("after", (await session.GetAccountsAsync().ConfigureAwait(false)).Single().Username);
            Assert.AreEqual(1, calls, "An already refreshed operation must not invoke its serializer again.");
            Assert.AreEqual("after", cache.Accessor.GetAllAccounts().Single().PreferredUsername);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task DefaultIsolationHasAConstructionTimeOptOut(bool disabled)
        {
            AppContext.SetSwitch(Setting, disabled);
            var cache = CreateCache();
            AppContext.SetSwitch(Setting, !disabled);
            cache.SetBeforeAccess(args => args.TokenCache.DeserializeMsalV3(Blob("before"), true));
            var session = CreateSession(cache);
            Assert.AreEqual("before", (await session.GetAccountsAsync().ConfigureAwait(false)).Single().Username);
            cache.DeserializeMsalV3(Blob("after"), true);
            Assert.AreEqual(disabled ? "after" : "before",
                (await session.GetAccountsAsync().ConfigureAwait(false)).Single().Username);
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(false, true)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public async Task OnlyKnownConfidentialPartitionsUsePrivateHydration(bool publicClient, bool knownAccount)
        {
            AppContext.SetSwitch(Setting, false);
            var cache = publicClient
                ? (ITokenCacheInternal)PublicClientApplicationBuilder.Create(ClientId)
                    .WithAuthority("https://login.microsoftonline.com/11111111-1111-1111-1111-111111111111")
                    .WithInstanceDiscovery(false)
                    .WithLegacyCacheCompatibility(false)
                    .BuildConcrete().UserTokenCache
                : CreateCache();
            int calls = 0;
            cache.SetBeforeAccess(args =>
            {
                if (!publicClient && knownAccount)
                    Assert.AreNotSame(cache, args.TokenCache);
                else
                    Assert.AreSame(cache, args.TokenCache);
                args.TokenCache.DeserializeMsalV3(Blob(++calls == 1 ? "before" : "after"), true);
            });
            var session = CreateSession(cache, knownAccount: knownAccount);
            Assert.AreEqual("before", (await session.GetAccountsAsync().ConfigureAwait(false)).Single().Username);
            Assert.AreEqual("after", (await CreateSession(cache, knownAccount: knownAccount)
                .GetAccountsAsync().ConfigureAwait(false)).Single().Username);
            Assert.AreEqual(!publicClient && knownAccount ? "before" : "after",
                (await session.GetAccountsAsync().ConfigureAwait(false)).Single().Username);
            Assert.AreEqual(2, calls);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task ConfidentialSilentAcquisitionPreservesLoginHintResolution(bool loginHint)
        {
            using var httpManager = new MockHttpManager();
            var application = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithAuthority("https://login.microsoftonline.com/" + TestConstants.Utid)
                .WithClientSecret("synthetic-not-a-credential")
                .WithInstanceDiscovery(false)
                .WithLegacyCacheCompatibility(false)
                .WithHttpManager(httpManager)
                .BuildConcrete();
            var cache = (ITokenCacheInternal)application.UserTokenCache;
            TokenCacheHelper.PopulateCache(cache.Accessor, environment: "login.microsoftonline.com", addSecondAt: false);
            var seed = cache.Accessor.GetAllAccounts().Single();
            var account = new Account(seed.HomeAccountId, seed.PreferredUsername, seed.Environment);
            byte[] persisted = cache.SerializeMsalV3();
            cache.DeserializeMsalV3(null, true);
            int hydrations = 0;
            cache.SetBeforeAccess(args =>
            {
                Assert.AreEqual(loginHint, ReferenceEquals(cache, args.TokenCache));
                hydrations++;
                args.TokenCache.DeserializeMsalV3(persisted, true);
            });

            var builder = loginHint
                ? application.AcquireTokenSilent(TestConstants.s_scope, account.Username)
                : application.AcquireTokenSilent(TestConstants.s_scope, account);
            var result = await builder.ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(account.HomeAccountId.Identifier, result.Account.HomeAccountId.Identifier);
            Assert.AreEqual(1, hydrations);
        }

        [TestMethod]
        public async Task DefaultHydrationDoesNotCopyOrReplaceTheRoot()
        {
            var cache = CreateCache();
            cache.DeserializeMsalV3(Blob("root"), true);
            cache.SetBeforeAccess(args =>
            {
                Assert.AreNotSame(cache, args.TokenCache);
                var readerMutex = typeof(OptionalSemaphoreSlim).GetField("_readerMutex", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(readerMutex.GetValue(cache.Semaphore));
                Assert.IsNull(readerMutex.GetValue(((ITokenCacheInternal)args.TokenCache).Semaphore),
                    "A private target must not allocate another shared-reader coordinator.");
                args.TokenCache.DeserializeMsalV3(Blob("private"), true);
            });
            var session = CreateSession(cache);
            Assert.AreEqual("private", (await session.GetAccountsAsync().ConfigureAwait(false)).Single().Username);
            Assert.AreEqual("root", cache.Accessor.GetAllAccounts().Single().PreferredUsername);
        }

        [TestMethod]
        public async Task ConcurrentReadsInOneSessionPublishOnlyOneHydratedView()
        {
            // Arrange
            var cache = (TokenCache)CreateCache();
            var session = CreateSession(cache);
            var originalProxy = cache.ServiceBundle.PlatformProxy;
            var proxy = Substitute.For<IPlatformProxy>();
            using var secondConstructorEntered = new ManualResetEventSlim();
            using var firstCallbackEntered = new ManualResetEventSlim();
            using var secondConstructorReturning = new ManualResetEventSlim();
            var finishHydration = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            int constructions = 0;
            int callbacks = 0;
            proxy.GetFeatureFlags().Returns(originalProxy.GetFeatureFlags());
            proxy.CreateLegacyCachePersistence().Returns(originalProxy.CreateLegacyCachePersistence());
            proxy.CreateTokenCacheAccessor(Arg.Any<CacheOptions>(), false).Returns(_ =>
            {
                if (Interlocked.Increment(ref constructions) == 1)
                {
                    Assert.IsTrue(secondConstructorEntered.Wait(TimeSpan.FromSeconds(5)));
                }
                else
                {
                    secondConstructorEntered.Set();
                    Assert.IsTrue(firstCallbackEntered.Wait(TimeSpan.FromSeconds(5)));
                    secondConstructorReturning.Set();
                }
                return originalProxy.CreateTokenCacheAccessor(null, false);
            });
            cache.ServiceBundle.SetPlatformProxyForTest(proxy);
            cache.SetBeforeAccessAsync(async args =>
            {
                Interlocked.Increment(ref callbacks);
                args.TokenCache.DeserializeMsalV3(Blob("private"), true);
                firstCallbackEntered.Set();
                await finishHydration.Task.ConfigureAwait(false);
            });
            // Act
            var first = Task.Run(() => session.GetAccountsAsync());
            var second = Task.Run(() => session.GetAccountsAsync());
            try
            {
                Assert.IsTrue(await Task.Run(() => secondConstructorReturning.Wait(TimeSpan.FromSeconds(5))).ConfigureAwait(false));
            }
            finally
            {
                finishHydration.TrySetResult(true);
            }
            var results = await Task.WhenAll(first, second).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(2, constructions, "Both constructors must overlap before publication.");
            Assert.AreEqual(1, callbacks, "One session must hydrate only its published view.");
            Assert.IsTrue(results.All(accounts => accounts.Single().Username == "private"));
        }

        [TestMethod]
        public async Task LegacyEnabledCachesRetainExistingBehavior()
        {
            AppContext.SetSwitch(Setting, false);
            var application = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithAuthority("https://login.microsoftonline.com/common")
                .WithClientSecret("synthetic-not-a-credential")
                .WithInstanceDiscovery(false)
                .WithLegacyCacheCompatibility(true)
                .BuildConcrete();
            var persistence = new InMemoryLegacyCachePersistence();
            LegacyTokenCacheHelper.PopulateLegacyCache(
                NSubstitute.Substitute.For<Microsoft.Identity.Client.Core.ILoggerAdapter>(), persistence);
            var cache = new TokenCache(application.ServiceBundle, false, persistence);
            cache.SetBeforeAccess(_ => { });
            var session = CreateSession(cache);
            var before = (await session.GetAccountsAsync().ConfigureAwait(false)).Select(account => account.Username).OrderBy(name => name).ToArray();
            Assert.IsNotEmpty(before, "The fixture must exercise real ADAL fallback entries.");
            persistence.WriteCache(null);
            Assert.AreEqual(0, (await session.GetAccountsAsync().ConfigureAwait(false)).Count());
            Assert.AreEqual(0, (await CreateSession(cache).GetAccountsAsync().ConfigureAwait(false)).Count());
        }

        [TestMethod]
        public async Task ConcurrentHydrationAcceptsStructuredSnapshots()
        {
            var cache = CreateCache();
            cache.DeserializeMsalV3(Blob("before"), true);
            var snapshot = ((ITokenCacheSnapshotProvider)cache).CreateSnapshot();
            cache.SetBeforeAccess(args => ((ITokenCacheSnapshotProvider)args.TokenCache).RestoreSnapshot(snapshot));
            var session = CreateSession(cache);
            Assert.AreEqual("before", (await session.GetAccountsAsync().ConfigureAwait(false)).Single().Username);
        }

        [TestMethod]
        public async Task FailedPersistenceInvalidatesTheOperationsOldView()
        {
            AppContext.SetSwitch(Setting, false);
            var cache = CreateCache();
            byte[] persisted = Blob("before");
            cache.SetBeforeAccess(args => args.TokenCache.DeserializeMsalV3(persisted, true));
            cache.SetAfterAccess(args =>
            {
                if (args.HasStateChanged)
                {
                    persisted = Blob("written");
                    throw new InvalidOperationException("synthetic-persistence");
                }
            });
            var session = CreateSession(cache);
            Assert.AreEqual("before", (await session.GetAccountsAsync().ConfigureAwait(false)).Single().Username);
            bool failed = false;
            try { await session.SaveTokenResponseAsync(TestConstants.CreateMsalTokenResponse()).ConfigureAwait(false); }
            catch (InvalidOperationException exception) when (exception.Message == "synthetic-persistence") { failed = true; }
            Assert.IsTrue(failed);
            Assert.AreEqual("written", (await session.GetAccountsAsync().ConfigureAwait(false)).Single().Username);
        }

        private static void SetIsolation(bool enabled)
        {
            AppContext.SetSwitch(Setting, !enabled);
        }

        private static async Task Complete(Task task)
        {
            Assert.AreSame(task, await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(5))).ConfigureAwait(false),
                "Cache coordination did not complete within the bounded wait.");
            await task.ConfigureAwait(false);
        }

        private static async Task ExpectCancellation(Task task)
        {
            bool observed = false;
            try { await Complete(task).ConfigureAwait(false); }
            catch (OperationCanceledException) { observed = true; }
            Assert.IsTrue(observed);
        }

        private static ITokenCacheInternal CreateCache(bool synchronize = true, LogCallback logCallback = null)
        {
            var builder = ConfidentialClientApplicationBuilder.Create(ClientId)
                .WithAuthority("https://login.microsoftonline.com/11111111-1111-1111-1111-111111111111")
                .WithClientSecret("synthetic-not-a-credential")
                .WithInstanceDiscovery(false)
                .WithLegacyCacheCompatibility(false)
                .WithCacheSynchronization(synchronize);
            if (logCallback != null)
                builder.WithLogging(logCallback, LogLevel.Verbose);
            return (ITokenCacheInternal)builder.BuildConcrete().UserTokenCache;
        }

        private static CacheSessionManager CreateSession(ITokenCacheInternal cache, CancellationToken cancellationToken = default,
            bool knownAccount = true, string authority = null)
        {
            var service = ((TokenCache)cache).ServiceBundle;
            var context = new RequestContext(service, Guid.NewGuid(), null, cancellationToken)
            {
                ApiEvent = new ApiEvent(Guid.NewGuid())
            };
            var request = TestCommon.CreateAuthenticationRequestParameters(service,
                authority: authority == null ? null : Authority.CreateAuthority(authority), requestContext: context,
                apiID: ApiEvent.ApiIds.AcquireTokenSilent);
            request.Account = knownAccount ? new Account(HomeId, "before", "login.microsoftonline.com") : null;
            return new CacheSessionManager(cache, request);
        }

        private static byte[] Blob(string username)
        {
            return Encoding.UTF8.GetBytes("{\"Account\":{\"synthetic-account\":{\"home_account_id\":\"" + HomeId +
                "\",\"environment\":\"login.microsoftonline.com\",\"realm\":\"11111111-1111-1111-1111-111111111111\"," +
                "\"local_account_id\":\"user\",\"username\":\"" + username + "\",\"authority_type\":\"MSSTS\"}}," +
                "\"RefreshToken\":{\"synthetic-rt\":{\"home_account_id\":\"" + HomeId +
                "\",\"environment\":\"login.microsoftonline.com\",\"client_id\":\"" + ClientId +
                "\",\"credential_type\":\"RefreshToken\",\"secret\":\"synthetic\"}},\"future_node\":{\"value\":42}}");
        }

        private static async Task ExpectUnsupported(Func<Task> action, string reason)
        {
            bool observed = false;
            try { await action().ConfigureAwait(false); }
            catch (NotSupportedException exception)
            {
                Assert.IsTrue(exception.Message.Contains(reason, StringComparison.Ordinal));
                Assert.IsTrue(exception.Message.Contains(Setting, StringComparison.Ordinal),
                    "An incompatible serializer must receive actionable opt-out guidance.");
                observed = true;
            }
            Assert.IsTrue(observed, "Expected an explicit unsupported-contract failure, not a silent cache miss.");
        }
    }
}
