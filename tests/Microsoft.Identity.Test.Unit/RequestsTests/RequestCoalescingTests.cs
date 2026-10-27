// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.RP;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.RequestsTests
{
    [TestClass]
    public class RequestCoalescingTests : TestBase
    {
        [TestMethod]
        [DataRow("client", false, false)]
        [DataRow("client", true, false)]
        [DataRow("provider", true, false)]
        [DataRow("obo", false, false)]
        [DataRow("obo", true, false)]
        [DataRow("long-running", false, false)]
        [DataRow("initiate-search", true, false)]
        [DataRow("silent", false, false)]
        [DataRow("silent", true, false)]
        [DataRow("client", true, true)]
        [DataRow("provider", true, true)]
        [DataRow("obo", true, true)]
        [DataRow("long-running", false, true)]
        [DataRow("silent", false, true)]
        public async Task ConcurrentMisses_PublishOnce_AndReadOwnCacheAsync(string flow, bool shared, bool proactive)
        {
            // Arrange
            using var http = new BlockingTokenEndpoint();
            string clientId = Guid.NewGuid().ToString();
            var backgroundCompleted = new TaskCompletionSource<ExecutionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            int backgroundCallbacks = 0;
            int completionCallbacks = 0;
            ConfidentialClientApplication BuildApp()
            {
                var builder = ConfidentialClientApplicationBuilder.Create(clientId)
                    .WithAuthority(TestConstants.AuthorityTestTenant)
                    .WithInstanceDiscovery(false)
                    .WithHttpClientFactory(http)
                    .WithCacheOptions(new CacheOptions(shared))
                    .OnBackgroundTokenRefreshCompleted(result =>
                    {
                        Interlocked.Increment(ref backgroundCallbacks);
                        backgroundCompleted.TrySetResult(result);
                        return Task.CompletedTask;
                    });
                if (flow == "provider")
                {
                    builder.WithAppTokenProvider(async parameters =>
                    {
                        await http.EnterAsync(parameters.CancellationToken).ConfigureAwait(false);
                        return new AppTokenProviderResult { AccessToken = TestConstants.ATSecret, ExpiresInSeconds = 3600 };
                    });
                }
                else
                {
                    builder.WithClientSecret(TestConstants.ClientSecret);
                }
                var result = builder.BuildConcrete();
                result.ServiceBundle.Config.OnCompletion = (_, _) =>
                {
                    Interlocked.Increment(ref completionCallbacks);
                    return Task.CompletedTask;
                };
                return result;
            }

            var app = BuildApp();
            var other = shared ? BuildApp() : app;
            string session = Guid.NewGuid().ToString();
            IAccount account = null;
            if (flow == "long-running")
            {
                await app.InitiateLongRunningProcessInWebApi(
                    TestConstants.s_scope, TestConstants.DefaultAccessToken, ref session).ExecuteAsync().ConfigureAwait(false);
            }
            else if (flow == "silent")
            {
                var initial = await app.AcquireTokenByAuthorizationCode(TestConstants.s_scope, "code")
                    .ExecuteAsync().ConfigureAwait(false);
                account = initial.Account;
            }

            Task<AuthenticationResult> Acquire(ConfidentialClientApplication target) => flow switch
            {
                "obo" => target.AcquireTokenOnBehalfOf(TestConstants.s_scope,
                    new UserAssertion(TestConstants.DefaultAccessToken)).ExecuteAsync(),
                "long-running" => target.AcquireTokenInLongRunningProcess(TestConstants.s_scope, session).ExecuteAsync(),
                "initiate-search" => target.InitiateLongRunningProcessInWebApi(
                    TestConstants.s_scope, TestConstants.DefaultAccessToken, ref session)
                    .WithSearchInCacheForLongRunningProcess().ExecuteAsync(),
                "silent" => target.AcquireTokenSilent(TestConstants.s_scope, account).ExecuteAsync(),
                _ => target.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync()
            };

            var cache = flow == "client" || flow == "provider" ? app.AppTokenCacheInternal : app.UserTokenCacheInternal;
            if (proactive)
            {
                await Acquire(app).ConfigureAwait(false);
                TestCommon.UpdateATWithRefreshOn(cache.Accessor);
            }
            else if (flow == "long-running" || flow == "silent")
            {
                TokenCacheHelper.ExpireAllAccessTokens(cache);
            }
            int before = http.Calls;
            int completionsBefore = completionCallbacks;
            ReadCoalescingTelemetry(app);
            http.Block = true;

            // Act: every request reaches either admission or HTTP before the owner is released.
            var owner = Acquire(app);
            await AwaitAsync(http.Entered.Task).ConfigureAwait(false);
            var waiters = Enumerable.Range(0, 8).Select(_ => Acquire(other)).ToArray();
            Task<AuthenticationResult> foregroundMiss = null;
            if (proactive)
            {
                // All callers return the still-usable token before the refresh can finish.
                Assert.AreEqual(TaskStatus.RanToCompletion, owner.Status);
                Assert.IsTrue(waiters.All(t => t.Status == TaskStatus.RanToCompletion));
                TokenCacheHelper.ExpireAllAccessTokens(cache);
                foregroundMiss = Acquire(other);
                Assert.IsFalse(foregroundMiss.IsCompleted);
            }
            http.Release.TrySetResult(true);
            var results = await Task.WhenAll(new[] { owner }.Concat(waiters)).ConfigureAwait(false);
            if (proactive)
            {
                Assert.IsTrue((await AwaitAsync(backgroundCompleted.Task).ConfigureAwait(false)).Successful);
                Assert.AreEqual(TokenSource.Cache, (await foregroundMiss.ConfigureAwait(false)).AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(1, backgroundCallbacks);
            }
            var warm = await Acquire(other).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(1, http.Calls - before);
            Assert.AreEqual(proactive ? 0 : 1, results.Count(r => r.AuthenticationResultMetadata.TokenSource == TokenSource.IdentityProvider));
            Assert.AreEqual(proactive ? 9 : 8, results.Count(r => r.AuthenticationResultMetadata.TokenSource == TokenSource.Cache));
            Assert.AreEqual(TokenSource.Cache, warm.AuthenticationResultMetadata.TokenSource);
            Assert.HasCount(9, results.Distinct().ToArray());
            Assert.HasCount(9, results.Select(r => r.CorrelationId).Distinct().ToArray());
            if (flow == "client" || flow == "provider")
            {
                Assert.AreEqual(1, completionCallbacks - completionsBefore);
            }
            var counts = ReadCoalescingTelemetry(app);
            var otherCounts = shared ? ReadCoalescingTelemetry(other) : (0L, 0L);
            Assert.AreEqual(proactive ? 9L : 8L, counts.Coalesced + otherCounts.Item1);
            Assert.AreEqual(1L, counts.Acquired + otherCounts.Item2);
            Assert.AreEqual((0L, 0L), ReadCoalescingTelemetry(app), "Each snapshot is reported only once.");
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task CoalescingTelemetry_ReportsBurstOnNextRequestAsync(bool expired)
        {
            // Arrange
            using var http = new BlockingTokenEndpoint();
            var app = ConfidentialClientApplicationBuilder.Create(Guid.NewGuid().ToString())
                .WithAuthority(TestConstants.AuthorityTestTenant).WithInstanceDiscovery(false)
                .WithClientSecret(TestConstants.ClientSecret).WithHttpClientFactory(http).BuildConcrete();
            Task<AuthenticationResult> Acquire() => app.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync();
            if (expired)
            {
                await Acquire().ConfigureAwait(false);
                TokenCacheHelper.ExpireAllAccessTokens(app.AppTokenCacheInternal);
                ReadCoalescingTelemetry(app);
            }
            http.Block = true;

            // Act
            var owner = Acquire();
            await AwaitAsync(http.Entered.Task).ConfigureAwait(false);
            var waiters = Enumerable.Range(0, 8).Select(_ => Acquire()).ToArray();
            http.Release.TrySetResult(true);
            await Task.WhenAll(waiters.Append(owner)).ConfigureAwait(false);
            await Acquire().ConfigureAwait(false); // An ordinary warm hit must not contribute.
            TokenCacheHelper.ExpireAllAccessTokens(app.AppTokenCacheInternal);
            await Acquire().ConfigureAwait(false);

            // Assert
            string[] fields = http.TelemetryHeaders.Last().Split('|')[2].Split(',');
            Assert.HasCount(8, fields);
            Assert.AreEqual("8", fields[6]);
            Assert.AreEqual("1", fields[7], "The carrying request has not yet completed when its header is sent.");
            Assert.AreEqual((0L, 1L), ReadCoalescingTelemetry(app));
            Assert.AreEqual(expired ? 3 : 2, http.Calls);
        }

        [TestMethod]
        public async Task ProactiveFailure_StillCountsSuppressedRefreshesAsync()
        {
            // Arrange
            using var http = new BlockingTokenEndpoint();
            var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var completed = new TaskCompletionSource<ExecutionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            int providerCalls = 0;
            var app = ConfidentialClientApplicationBuilder.Create(Guid.NewGuid().ToString())
                .WithAuthority(TestConstants.AuthorityTestTenant).WithInstanceDiscovery(false)
                .WithHttpClientFactory(http)
                .WithAppTokenProvider(async _ =>
                {
                    if (Interlocked.Increment(ref providerCalls) == 1)
                    {
                        return new AppTokenProviderResult { AccessToken = "token", ExpiresInSeconds = 3600 };
                    }
                    entered.TrySetResult(true);
                    await release.Task.ConfigureAwait(false);
                    throw new MsalClientException("refresh_failed");
                })
                .OnBackgroundTokenRefreshCompleted(result =>
                {
                    completed.TrySetResult(result);
                    return Task.CompletedTask;
                }).BuildConcrete();
            Task<AuthenticationResult> Acquire() => app.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync();
            await Acquire().ConfigureAwait(false);
            ReadCoalescingTelemetry(app);
            TestCommon.UpdateATWithRefreshOn(app.AppTokenCacheInternal.Accessor);

            // Act
            var owner = await Acquire().ConfigureAwait(false);
            await AwaitAsync(entered.Task).ConfigureAwait(false);
            var followers = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Acquire())).ConfigureAwait(false);
            release.TrySetResult(true);
            var background = await AwaitAsync(completed.Task).ConfigureAwait(false);

            // Assert
            Assert.IsFalse(background.Successful);
            Assert.AreEqual(TokenSource.Cache, owner.AuthenticationResultMetadata.TokenSource);
            Assert.IsTrue(followers.All(r => r.AuthenticationResultMetadata.TokenSource == TokenSource.Cache));
            Assert.AreEqual((8L, 0L), ReadCoalescingTelemetry(app));
            Assert.AreEqual(2, providerCalls);
        }

        [TestMethod]
        [DataRow("client", "force")]
        [DataRow("client", "claims")]
        [DataRow("client", "disabled")]
        [DataRow("obo", "force")]
        [DataRow("obo", "claims")]
        [DataRow("obo", "disabled")]
        [DataRow("long-running", "initiate")]
        public async Task BypassRequests_DoNotCoalesceAsync(string flow, string bypass)
        {
            // Arrange
            using var http = new BlockingTokenEndpoint();
            var app = ConfidentialClientApplicationBuilder.Create(Guid.NewGuid().ToString())
                .WithAuthority(TestConstants.AuthorityTestTenant).WithInstanceDiscovery(false)
                .WithClientSecret(TestConstants.ClientSecret).WithHttpClientFactory(http)
                .WithCacheOptions(bypass == "disabled" ? CacheOptions.DisableInternalCacheOptions : new CacheOptions(false))
                .BuildConcrete();
            string session = Guid.NewGuid().ToString();
            Task<AuthenticationResult> Acquire()
            {
                if (flow == "long-running")
                {
                    return app.InitiateLongRunningProcessInWebApi(
                        TestConstants.s_scope, TestConstants.DefaultAccessToken, ref session).ExecuteAsync();
                }
                if (flow == "obo")
                {
                    var request = app.AcquireTokenOnBehalfOf(TestConstants.s_scope, new UserAssertion(TestConstants.DefaultAccessToken))
                        .WithForceRefresh(bypass == "force");
                    return (bypass == "claims" ? request.WithClaims("{\"access_token\":{\"foo\":null}}") : request).ExecuteAsync();
                }
                var clientRequest = app.AcquireTokenForClient(TestConstants.s_scope).WithForceRefresh(bypass == "force");
                return (bypass == "claims" ? clientRequest.WithClaims("{\"access_token\":{\"foo\":null}}") : clientRequest).ExecuteAsync();
            }
            await Acquire().ConfigureAwait(false);
            int before = http.Calls;
            http.Block = true;

            // Act
            var requests = Enumerable.Range(0, 5).Select(_ => Acquire()).ToArray();
            http.Release.TrySetResult(true);
            var results = await Task.WhenAll(requests).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(5, http.Calls - before);
            Assert.IsTrue(results.All(r => r.AuthenticationResultMetadata.TokenSource == TokenSource.IdentityProvider));
            Assert.AreEqual((0L, 0L), ReadCoalescingTelemetry(app));
        }

        [TestMethod]
        public async Task ClaimsWithRevokedTokenHash_CoalesceOnlyAfterReplacementIsVisibleAsync()
        {
            // Arrange
            using var http = new BlockingTokenEndpoint();
            string token = "revoked";
            var app = ConfidentialClientApplicationBuilder.Create(Guid.NewGuid().ToString())
                .WithAuthority(TestConstants.AuthorityTestTenant).WithInstanceDiscovery(false)
                .WithHttpClientFactory(http)
                .WithAppTokenProvider(async parameters =>
                {
                    await http.EnterAsync(parameters.CancellationToken).ConfigureAwait(false);
                    return new AppTokenProviderResult { AccessToken = token, ExpiresInSeconds = 3600 };
                }).BuildConcrete();
            await app.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync().ConfigureAwait(false);
            string revokedHash = app.ServiceBundle.PlatformProxy.CryptographyManager.CreateSha256HashHex(token);
            token = "replacement";
            http.Block = true;
            Task<AuthenticationResult> Acquire() => app.AcquireTokenForClient(TestConstants.s_scope)
                .WithClaims("{\"access_token\":{\"foo\":null}}")
                .WithAccessTokenSha256ToRefresh(revokedHash).ExecuteAsync();

            // Act
            var owner = Acquire();
            await AwaitAsync(http.Entered.Task).ConfigureAwait(false);
            var waiters = Enumerable.Range(0, 8).Select(_ => Acquire()).ToArray();
            Assert.IsTrue(waiters.All(t => !t.IsCompleted));
            http.Release.TrySetResult(true);
            var results = await Task.WhenAll(new[] { owner }.Concat(waiters)).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(2, http.Calls); // Seed plus exactly one replacement.
            Assert.IsTrue(results.All(r => r.AccessToken == "replacement"));
            Assert.AreEqual(8, results.Count(r => r.AuthenticationResultMetadata.TokenSource == TokenSource.Cache));
        }

        [TestMethod]
        public async Task NestedProvider_DifferentScopes_ProgressIndependentlyAsync()
        {
            // Arrange
            using var http = new BlockingTokenEndpoint();
            ConfidentialClientApplication app = null;
            int calls = 0;
            app = ConfidentialClientApplicationBuilder.Create(Guid.NewGuid().ToString())
                .WithAuthority(TestConstants.AuthorityTestTenant).WithInstanceDiscovery(false)
                .WithHttpClientFactory(http)
                .WithAppTokenProvider(async parameters =>
                {
                    Interlocked.Increment(ref calls);
                    if (parameters.Scopes.Contains("outer/.default"))
                    {
                        await app.AcquireTokenForClient(new[] { "inner/.default" }).ExecuteAsync().ConfigureAwait(false);
                    }
                    return new AppTokenProviderResult { AccessToken = "token", ExpiresInSeconds = 3600 };
                }).BuildConcrete();

            // Act
            await AwaitAsync(app.AcquireTokenForClient(new[] { "outer/.default" }).ExecuteAsync()).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(2, calls);
            Assert.HasCount(2, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
            Assert.AreEqual(0, http.Calls);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task OwnerFailureOrCancellation_DoesNotBecomeWaiterOutcomeAsync(bool cancelOwner)
        {
            // Arrange
            using var http = new BlockingTokenEndpoint();
            using var ownerCancellation = new CancellationTokenSource();
            var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            string clientId = Guid.NewGuid().ToString();
            int goodProviderCalls = 0;
            ConfidentialClientApplication BuildApp(Func<AppTokenProviderParameters, Task<AppTokenProviderResult>> provider) =>
                ConfidentialClientApplicationBuilder.Create(clientId)
                    .WithAuthority(TestConstants.AuthorityTestTenant).WithInstanceDiscovery(false)
                    .WithHttpClientFactory(http).WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
                    .WithAppTokenProvider(provider).BuildConcrete();
            var failing = BuildApp(async parameters =>
            {
                entered.TrySetResult(true);
                if (cancelOwner)
                {
                    await Task.Delay(Timeout.Infinite, parameters.CancellationToken).ConfigureAwait(false);
                }
                await release.Task.ConfigureAwait(false);
                throw new MsalClientException("owner_failed");
            });
            var succeeding = BuildApp(_ =>
            {
                Interlocked.Increment(ref goodProviderCalls);
                return Task.FromResult(new AppTokenProviderResult { AccessToken = "success", ExpiresInSeconds = 3600 });
            });

            // Act
            var owner = failing.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync(ownerCancellation.Token);
            await AwaitAsync(entered.Task).ConfigureAwait(false);
            var waiter = succeeding.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync();
            Assert.IsFalse(waiter.IsCompleted);
            if (cancelOwner)
            {
                ownerCancellation.Cancel();
                await Assert.ThrowsExactlyAsync<TaskCanceledException>(() => owner).ConfigureAwait(false);
            }
            else
            {
                release.TrySetResult(true);
                await Assert.ThrowsExactlyAsync<MsalClientException>(() => owner).ConfigureAwait(false);
            }
            var result = await waiter.ConfigureAwait(false);

            // Assert
            Assert.AreEqual("success", result.AccessToken);
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(1, goodProviderCalls);
            Assert.AreEqual(0, http.Calls);
            Assert.AreEqual((0L, 0L), ReadCoalescingTelemetry(failing));
            Assert.AreEqual((0L, 1L), ReadCoalescingTelemetry(succeeding));
        }

        [TestMethod]
        public async Task CanceledWaiter_DoesNotCancelOwnerOrOtherWaitersAsync()
        {
            // Arrange
            using var http = new BlockingTokenEndpoint { Block = true };
            using var cancellation = new CancellationTokenSource();
            var app = ConfidentialClientApplicationBuilder.Create(Guid.NewGuid().ToString())
                .WithAuthority(TestConstants.AuthorityTestTenant).WithInstanceDiscovery(false)
                .WithClientSecret(TestConstants.ClientSecret).WithHttpClientFactory(http).BuildConcrete();

            // Act
            var owner = app.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync();
            await AwaitAsync(http.Entered.Task).ConfigureAwait(false);
            var canceled = app.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync(cancellation.Token);
            var waiter = app.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync();
            cancellation.Cancel();
            await Assert.ThrowsAsync<OperationCanceledException>(() => canceled).ConfigureAwait(false);
            Assert.IsFalse(owner.IsCompleted);
            http.Release.TrySetResult(true);
            await owner.ConfigureAwait(false);

            // Assert
            Assert.AreEqual(TokenSource.Cache, (await waiter.ConfigureAwait(false)).AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(1, http.Calls);
            Assert.AreEqual((1L, 1L), ReadCoalescingTelemetry(app));
        }

        [TestMethod]
        public async Task GatePool_KeepsQueuedWaitersRegistered_AndReclaimsEntriesAsync()
        {
            // Arrange
            var pool = new TokenRequestCoalescer.GatePool();
            using var cancellation = new CancellationTokenSource();
            var owner = await pool.EnterAsync("same", CancellationToken.None).ConfigureAwait(false);
            var canceled = pool.EnterAsync("same", cancellation.Token);
            var waiter = pool.EnterAsync("same", CancellationToken.None);

            // Act
            cancellation.Cancel();
            await Assert.ThrowsAsync<OperationCanceledException>(() => canceled).ConfigureAwait(false);
            using (await pool.EnterAsync("independent", CancellationToken.None).ConfigureAwait(false))
            {
                Assert.AreEqual(2, pool.Count);
            }
            owner.Dispose();
            using (await waiter.ConfigureAwait(false))
            {
                Assert.IsNull(pool.TryEnter("same"));
                Assert.AreEqual(1, pool.Count);
            }

            // Assert
            Assert.AreEqual(0, pool.Count);
            for (int i = 0; i < 1000; i++)
            {
                using (pool.TryEnter(i.ToString()))
                {
                    Assert.AreEqual(1, pool.Count);
                }
            }
            Assert.AreEqual(0, pool.Count);
        }

        private static (long Coalesced, long Acquired) ReadCoalescingTelemetry(ConfidentialClientApplication app)
        {
            string header = app.ServiceBundle.HttpTelemetryManager.GetCurrentRequestHeader(
                new ApiEvent(Guid.NewGuid()) { CacheInfo = CacheRefreshReason.NoCachedAccessToken });
            string[] fields = header.Split('|')[2].Split(',');
            Assert.HasCount(8, fields);
            return (long.Parse(fields[6], System.Globalization.CultureInfo.InvariantCulture),
                long.Parse(fields[7], System.Globalization.CultureInfo.InvariantCulture));
        }

        private static async Task<T> AwaitAsync<T>(Task<T> task)
        {
            // A watchdog only: ordering and call counts are controlled by gates, never elapsed time.
            Assert.AreSame(task, await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(10))).ConfigureAwait(false),
                "The gated operation did not complete.");
            return await task.ConfigureAwait(false);
        }

        private sealed class BlockingTokenEndpoint : HttpMessageHandler, IMsalHttpClientFactory
        {
            private readonly HttpClient _client;
            private int _calls;
            public bool Block { get; set; }
            public int Calls => Volatile.Read(ref _calls);
            public ConcurrentQueue<string> TelemetryHeaders { get; } = new ConcurrentQueue<string>();
            public TaskCompletionSource<bool> Entered { get; } = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            public TaskCompletionSource<bool> Release { get; } = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            public BlockingTokenEndpoint()
            {
                _client = new HttpClient(this);
            }

            public HttpClient GetHttpClient() => _client;

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    Release.TrySetResult(true);
                }
                base.Dispose(disposing);
            }

            public async Task EnterAsync(CancellationToken cancellationToken)
            {
                Interlocked.Increment(ref _calls);
                if (Block)
                {
                    Entered.TrySetResult(true);
                    await Release.Task.ConfigureAwait(false);
                }
                cancellationToken.ThrowIfCancellationRequested();
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                TelemetryHeaders.Enqueue(request.Headers.GetValues(TelemetryConstants.XClientCurrentTelemetry).Single());
                await EnterAsync(cancellationToken).ConfigureAwait(false);
                string body = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
                return body.Contains("grant_type=client_credentials")
                    ? MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage()
                    : MockHelpers.CreateSuccessTokenResponseMessage();
            }
        }
    }
}
