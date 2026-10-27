// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.AuthScheme;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Http.Retry;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2.Throttling;
using Microsoft.Identity.Client.RP;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Unit.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

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
            var other = shared && flow != "provider" ? BuildApp() : app;
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
        public async Task ServiceFailureCallback_DifferentStrategyProgresses_WhileSameStrategyWaitsAsync()
        {
            // Arrange
            using var http = new BlockingTokenEndpoint { ResponseFactory = () => CreateFailureResponse(400) };
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            string clientId = Guid.NewGuid().ToString();
            var app = CreateDirectApp(http, clientId, shared: true);
            var fallback = CreateDirectApp(http, clientId, shared: true);
            var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            AuthenticationResult fallbackResult = null;
            int callbacks = 0;
            app.ServiceBundle.Config.OnMsalServiceFailure = async (_, _) =>
            {
                Interlocked.Increment(ref callbacks);
                http.ResponseFactory = null;
                entered.TrySetResult(true);
                await release.Task.ConfigureAwait(false);
                fallbackResult = await fallback.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync(cancellation.Token).ConfigureAwait(false);
                return false;
            };

            // Act
            var owner = app.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync(cancellation.Token);
            await AwaitAsync(entered.Task).ConfigureAwait(false);
            var waiters = Enumerable.Range(0, 4).Select(_ =>
                app.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync(cancellation.Token)).ToArray();
            try
            {
                Assert.IsTrue(waiters.All(t => !t.IsCompleted));
                var independent = await AwaitAsync(fallback.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync(cancellation.Token)).ConfigureAwait(false);
                Assert.AreEqual(TokenSource.IdentityProvider, independent.AuthenticationResultMetadata.TokenSource);
                Assert.IsFalse(owner.IsCompleted, "A different strategy must progress while the callback is suspended.");
                Assert.HasCount(1, fallback.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
            }
            finally
            {
                release.TrySetResult(true);
            }
            var error = await AwaitAsync(CaptureServiceFailureAsync(owner)).ConfigureAwait(false);
            var followers = await Task.WhenAll(waiters).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(400, error.StatusCode);
            Assert.AreEqual(1, callbacks);
            Assert.IsNotNull(fallbackResult);
            Assert.AreEqual(TokenSource.Cache, fallbackResult.AuthenticationResultMetadata.TokenSource);
            Assert.IsTrue(followers.All(r => r.AuthenticationResultMetadata.TokenSource == TokenSource.Cache));
            Assert.IsTrue(followers.All(r => r.AccessToken == fallbackResult.AccessToken));
            Assert.AreEqual(2, http.Calls);
            Assert.HasCount(1, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
        }

        [TestMethod]
        [DataRow("certificate", TokenSource.IdentityProvider, 1, 2)]
        [DataRow("assertion", TokenSource.IdentityProvider, 1, 2)]
        [DataRow("signed-assertion", TokenSource.Cache, 6, 1)]
        public async Task CredentialProvider_DifferentStrategyProgresses_WhileResolutionWaitsAsync(
            string credential, TokenSource expectedTokenSource, int expectedProviderCalls, int expectedHttpCalls)
        {
            // Arrange
            using var http = new BlockingTokenEndpoint();
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            string clientId = Guid.NewGuid().ToString();
            var bootstrap = CreateDirectApp(http, clientId, shared: true);
            var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            int providerCalls = 0;
            AuthenticationResult bootstrapResult = null;
            async Task BootstrapAsync()
            {
                Interlocked.Increment(ref providerCalls);
                entered.TrySetResult(true);
                await release.Task.ConfigureAwait(false);
                bootstrapResult = await bootstrap.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync(cancellation.Token).ConfigureAwait(false);
                Assert.HasCount(1, bootstrap.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
            }
            var builder = ConfidentialClientApplicationBuilder.Create(clientId)
                .WithAuthority(TestConstants.AuthorityTestTenant).WithInstanceDiscovery(false)
                .WithHttpClientFactory(http).WithCacheOptions(CacheOptions.EnableSharedCacheOptions);
            if (credential == "certificate")
            {
                builder.WithCertificate(async _ =>
                {
                    await BootstrapAsync().ConfigureAwait(false);
                    return CertHelper.GetOrCreateTestCert();
                }, new CertificateOptions());
            }
            else if (credential == "assertion")
            {
                builder.WithClientAssertion(async (AssertionRequestOptions _) =>
                {
                    await BootstrapAsync().ConfigureAwait(false);
                    return "assertion";
                });
            }
            else
            {
                builder.WithClientAssertion(async (_, _) =>
                {
                    await BootstrapAsync().ConfigureAwait(false);
                    return new ClientSignedAssertion { Assertion = "assertion" };
                });
            }
            var app = builder.BuildConcrete();

            // Act
            var owner = app.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync(cancellation.Token);
            await AwaitAsync(entered.Task).ConfigureAwait(false);
            var waiters = Enumerable.Range(0, 4).Select(_ =>
                app.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync(cancellation.Token)).ToArray();
            try
            {
                Assert.IsTrue(waiters.All(t => !t.IsCompleted));
                // Signed assertions resolve before cache lookup for every caller; other providers run under admission.
                Assert.AreEqual(credential == "signed-assertion" ? 5 : 1, providerCalls);
                Assert.AreEqual(0, http.Calls);
                Assert.HasCount(0, bootstrap.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
                var independent = await AwaitAsync(bootstrap.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync(cancellation.Token)).ConfigureAwait(false);
                Assert.AreEqual(TokenSource.IdentityProvider, independent.AuthenticationResultMetadata.TokenSource);
                Assert.IsFalse(owner.IsCompleted, "A different strategy must progress while credential resolution is suspended.");
                Assert.AreEqual(1, http.Calls);
                Assert.HasCount(1, bootstrap.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
            }
            finally
            {
                release.TrySetResult(true);
            }
            var result = await AwaitAsync(owner).ConfigureAwait(false);
            var followers = await Task.WhenAll(waiters).ConfigureAwait(false);
            var warm = await app.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync().ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(bootstrapResult);
            Assert.AreEqual(TokenSource.Cache, bootstrapResult.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(expectedTokenSource, result.AuthenticationResultMetadata.TokenSource);
            Assert.IsTrue(followers.All(r => r.AuthenticationResultMetadata.TokenSource == TokenSource.Cache));
            Assert.AreEqual(TokenSource.Cache, warm.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(expectedProviderCalls, providerCalls);
            Assert.AreEqual(expectedHttpCalls, http.Calls);
            Assert.HasCount(1, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(400)]
        [DataRow(429)]
        [DataRow(503)]
        public async Task CompletionCallback_ReleasesAdmissionBeforeAwaitingIndependentAcquisitionAsync(int status)
        {
            // Arrange
            using var http = new BlockingTokenEndpoint();
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            if (status != 0)
            {
                http.ResponseFactory = () => CreateFailureResponse(status);
            }
            string clientId = Guid.NewGuid().ToString();
            var app = CreateDirectApp(http, clientId, shared: true);
            var other = CreateDirectApp(http, clientId, shared: true);
            AuthenticationResult nested = null;
            int callbacks = 0;
            int failuresSeenByObserver = -1;
            app.ServiceBundle.Config.OnCompletion = async (_, _) =>
            {
                Interlocked.Increment(ref callbacks);
                failuresSeenByObserver = TokenRequestCoalescer.GetFailureCache(app.ServiceBundle).CacheForTest.Count;
                http.ResponseFactory = null;
                http.Block = true;
                TokenCacheHelper.ExpireAllAccessTokens(app.AppTokenCacheInternal);
                Task<AuthenticationResult> acquisition;
                using (ExecutionContext.SuppressFlow())
                {
                    acquisition = Task.Run(() => other.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync(cancellation.Token));
                }
                nested = await acquisition.ConfigureAwait(false);
            };

            // Act
            var owner = app.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync(cancellation.Token);
            await AwaitAsync(http.Entered.Task).ConfigureAwait(false);
            var follower = app.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync(cancellation.Token);
            try
            {
                Assert.IsFalse(owner.IsCompleted, "The completion observer must still be awaiting the nested acquisition.");
                Assert.IsFalse(follower.IsCompleted, "Independent callers must wait for the nested acquisition's publication.");
                Assert.AreEqual(status == 503 ? 3 : 2, http.Calls);
            }
            finally
            {
                http.Release.TrySetResult(true);
            }
            if (status != 0)
            {
                await AwaitAsync(CaptureServiceFailureAsync(owner)).ConfigureAwait(false);
            }
            else
            {
                await AwaitAsync(owner).ConfigureAwait(false);
            }
            var followed = await AwaitAsync(follower).ConfigureAwait(false);
            Assert.AreEqual(TokenSource.Cache, followed.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(nested.AccessToken, followed.AccessToken);
            var warm = await other.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync().ConfigureAwait(false);

            // Assert
            Assert.AreEqual(1, callbacks);
            Assert.AreEqual(status == 429 || status == 503 ? 1 : 0, failuresSeenByObserver);
            Assert.IsNotNull(nested);
            Assert.AreEqual(TokenSource.IdentityProvider, nested.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(TokenSource.Cache, warm.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(status == 503 ? 3 : 2, http.Calls);
            Assert.HasCount(1, other.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
        }

        [TestMethod]
        public async Task SharedCacheProvider_ForwardingToSameScopeDirectRequest_DoesNotDeadlockAsync()
        {
            // Arrange
            using var http = new BlockingTokenEndpoint { Block = true };
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            string clientId = Guid.NewGuid().ToString();
            var direct = CreateDirectApp(http, clientId, shared: true);
            int providerCalls = 0;
            var provider = ConfidentialClientApplicationBuilder.Create(clientId)
                .WithAuthority(TestConstants.AuthorityTestTenant).WithInstanceDiscovery(false)
                .WithHttpClientFactory(http).WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
                .WithAppTokenProvider(async parameters =>
                {
                    Interlocked.Increment(ref providerCalls);
                    var result = await direct.AcquireTokenForClient(parameters.Scopes)
                        .ExecuteAsync(parameters.CancellationToken).ConfigureAwait(false);
                    Assert.HasCount(1, direct.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
                    return new AppTokenProviderResult { AccessToken = result.AccessToken, ExpiresInSeconds = 3600 };
                }).BuildConcrete();

            // Act
            var owner = provider.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync(cancellation.Token);
            await AwaitAsync(http.Entered.Task).ConfigureAwait(false);
            var followers = Enumerable.Range(0, 8).Select(_ =>
                provider.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync(cancellation.Token)).ToArray();
            var directFollowers = Enumerable.Range(0, 8).Select(_ =>
                direct.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync(cancellation.Token)).ToArray();
            Assert.IsTrue(followers.All(t => !t.IsCompleted));
            Assert.IsTrue(directFollowers.All(t => !t.IsCompleted));
            Assert.AreEqual(1, providerCalls);
            Assert.AreEqual(1, http.Calls);
            Assert.HasCount(0, direct.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
            http.Release.TrySetResult(true);
            var results = await AwaitAsync(Task.WhenAll(followers.Append(owner))).ConfigureAwait(false);
            var directResults = await AwaitAsync(Task.WhenAll(directFollowers)).ConfigureAwait(false);
            var providerWarm = await provider.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync().ConfigureAwait(false);
            var directWarm = await direct.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync().ConfigureAwait(false);

            // Assert
            Assert.AreEqual(1, providerCalls);
            Assert.AreEqual(1, http.Calls);
            Assert.AreEqual(8, results.Count(r => r.AuthenticationResultMetadata.TokenSource == TokenSource.Cache));
            Assert.IsTrue(directResults.All(r => r.AuthenticationResultMetadata.TokenSource == TokenSource.Cache));
            Assert.IsTrue(results.Concat(directResults).All(r => r.AccessToken == directWarm.AccessToken));
            Assert.AreEqual(TokenSource.Cache, providerWarm.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(TokenSource.Cache, directWarm.AuthenticationResultMetadata.TokenSource);
            Assert.HasCount(1, provider.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
            Assert.AreEqual(directWarm.AccessToken, providerWarm.AccessToken);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task RejectedCacheRecheck_RestoresMissMetadataAndEmitsBufferedTelemetryAsync(bool validation)
        {
            // Arrange
            using var http = new BlockingTokenEndpoint();
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var app = CreateDirectApp(http);
            var initial = await app.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync().ConfigureAwait(false);
            Assert.HasCount(1, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
            string hash = app.ServiceBundle.PlatformProxy.CryptographyManager.CreateSha256HashHex(initial.AccessToken);
            var validating = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            int validations = 0;
            var operation = Substitute.For<IAuthenticationOperation2>();
            operation.AccessTokenType.Returns("bearer");
            operation.GetTokenRequestParams().Returns(new Dictionary<string, string>());
            operation.FormatResultAsync(Arg.Any<AuthenticationResult>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);
            operation.ValidateCachedTokenAsync(Arg.Any<MsalCacheValidationData>()).Returns(async _ =>
            {
                if (Interlocked.Increment(ref validations) == 2)
                {
                    validating.TrySetResult(true);
                    await release.Task.ConfigureAwait(false);
                }
                return false;
            });
            app.ServiceBundle.HttpTelemetryManager.RecordCoalescedRequest(CacheRefreshReason.NoCachedAccessToken);

            // Act
            var request = app.AcquireTokenForClient(TestConstants.s_scope);
            request = validation
                ? request.WithAuthenticationOperation(operation)
                : request.WithAccessTokenSha256ToRefresh(hash);
            var acquisition = request.ExecuteAsync(cancellation.Token);
            if (validation)
            {
                try
                {
                    await AwaitAsync(validating.Task).ConfigureAwait(false);
                    Assert.IsFalse(acquisition.IsCompleted);
                    Assert.AreEqual(1, http.Calls, "The post-admission validator must finish before a replacement is acquired.");
                    Assert.HasCount(1, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
                }
                finally
                {
                    release.TrySetResult(true);
                }
            }
            var result = await AwaitAsync(acquisition).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(2, http.Calls);
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(CacheRefreshReason.NoCachedAccessToken, result.AuthenticationResultMetadata.CacheRefreshReason);
            Assert.HasCount(1, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
            string[] header = http.TelemetryHeaders.Last().Split('|');
            Assert.AreEqual(((int)CacheRefreshReason.NoCachedAccessToken).ToString(
                System.Globalization.CultureInfo.InvariantCulture), header[1].Split(',')[1]);
            Assert.HasCount(8, header[2].Split(','));
            Assert.IsTrue(header[2].EndsWith(",1,1", StringComparison.Ordinal),
                "The rejected recheck must not leave the prior burst counters buffered.");
            Assert.AreEqual((0L, 1L), ReadCoalescingTelemetry(app));
            if (validation)
            {
                await operation.Received(2).ValidateCachedTokenAsync(Arg.Any<MsalCacheValidationData>()).ConfigureAwait(false);
            }
        }

        [TestMethod]
        [DataRow("client", 429, 1)]
        [DataRow("client", 503, 2)]
        [DataRow("obo", 429, 1)]
        [DataRow("obo", 503, 2)]
        [DataRow("long-running", 503, 2)]
        [DataRow("silent", 503, 2)]
        public async Task DirectServiceFailureBurst_ThrottlesFollowers_UntilExpirationAsync(string flow, int status, int attempts)
        {
            // Arrange
            using var http = new BlockingTokenEndpoint();
            var app = CreateDirectApp(http);
            string publishedCorrelationId = null;
            app.ServiceBundle.Config.OnCompletion = (_, result) =>
            {
                if (result.Exception is not null)
                {
                    publishedCorrelationId = result.Exception.CorrelationId;
                }
                return Task.CompletedTask;
            };
            var acquire = await CreateAcquisitionAsync(app, flow).ConfigureAwait(false);
            if (flow == "silent" || flow == "long-running")
            {
                TokenCacheHelper.ExpireAllAccessTokens(app.UserTokenCacheInternal);
            }
            int before = http.Calls;
            http.Block = true;
            http.ResponseFactory = () => CreateFailureResponse(status);

            // Act
            var owner = acquire();
            await AwaitAsync(http.Entered.Task).ConfigureAwait(false);
            var followers = Enumerable.Range(0, 8).Select(_ => CaptureServiceFailureAsync(acquire())).ToArray();
            http.Release.TrySetResult(true);
            var original = await CaptureServiceFailureAsync(owner).ConfigureAwait(false);
            var failures = await Task.WhenAll(followers).ConfigureAwait(false);

            // Assert
            Assert.IsNotInstanceOfType<MsalThrottledServiceException>(original);
            Assert.IsFalse(string.IsNullOrWhiteSpace(original.CorrelationId));
            if (flow == "client")
            {
                Assert.AreEqual(original.CorrelationId, publishedCorrelationId,
                    "The owner's correlation ID must be assigned before completion observers see the published failure.");
            }
            Assert.AreEqual(attempts, http.Calls - before, "Only the owner's logical HTTP acquisition and retries may run.");
            Assert.HasCount(8, failures.Distinct().ToArray());
            foreach (var failure in failures)
            {
                Assert.IsInstanceOfType<MsalThrottledServiceException>(failure);
                var throttled = (MsalThrottledServiceException)failure;
                Assert.AreSame(original, throttled.OriginalServiceException);
                Assert.AreEqual(original.ResponseBody, throttled.ResponseBody);
                Assert.AreSame(original.Headers, throttled.Headers);
                Assert.AreEqual(original.CorrelationId, throttled.CorrelationId);
                Assert.AreEqual(status, throttled.StatusCode);
            }
            var cache = TokenRequestCoalescer.GetFailureCache(app.ServiceBundle);
            Assert.HasCount(1, cache.CacheForTest);
            var entry = cache.CacheForTest.Single();
            Assert.AreEqual(TimeSpan.FromSeconds(60), entry.Value.ExpirationTime - entry.Value.CreationTime);
            cache.CacheForTest[entry.Key] = new ThrottlingCacheEntry(original,
                DateTimeOffset.UtcNow.AddMinutes(-2), DateTimeOffset.UtcNow.AddMinutes(-1));
            http.ResponseFactory = null;
            var recovered = await acquire().ConfigureAwait(false);
            Assert.AreEqual(TokenSource.IdentityProvider, recovered.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(attempts + 1, http.Calls - before);
            Assert.IsTrue(cache.IsEmpty());
            Assert.AreEqual(TokenSource.Cache, (await acquire().ConfigureAwait(false)).AuthenticationResultMetadata.TokenSource);
        }

        [TestMethod]
        [DataRow("application")]
        [DataRow("scope")]
        [DataRow("flow")]
        [DataRow("assertion")]
        [DataRow("binding")]
        [DataRow("request-callback")]
        public async Task DirectServiceFailure_IsIsolatedByApplicationAndOperationAsync(string difference)
        {
            // Arrange
            using var http = new BlockingTokenEndpoint { ResponseFactory = () => CreateFailureResponse(429) };
            string clientId = Guid.NewGuid().ToString();
            var app = CreateDirectApp(http, clientId, shared: true);
            var other = difference == "application" ? CreateDirectApp(http, clientId, shared: true) : app;
            Func<OnBeforeTokenRequestData, Task> firstCallback = _ => Task.CompletedTask;
            Func<OnBeforeTokenRequestData, Task> secondCallback = _ => Task.CompletedTask;
            IAuthenticationOperation Operation(string key)
            {
                var operation = Substitute.For<IAuthenticationOperation>();
                operation.AccessTokenType.Returns("bearer");
                operation.KeyId.Returns(key);
                operation.GetTokenRequestParams().Returns(new Dictionary<string, string>());
                return operation;
            }
            Task<AuthenticationResult> Acquire(bool independent)
            {
                var target = independent ? other : app;
                if (difference == "assertion" || (difference == "flow" && independent))
                {
                    return target.AcquireTokenOnBehalfOf(TestConstants.s_scope,
                        new UserAssertion(independent ? "other-assertion" : "assertion")).ExecuteAsync();
                }
                var request = target.AcquireTokenForClient(
                    independent && difference == "scope" ? new[] { "other/.default" } : TestConstants.s_scope);
                if (difference == "request-callback")
                {
                    request.OnBeforeTokenRequest(independent ? secondCallback : firstCallback);
                }
                return (difference == "binding"
                    ? request.WithAuthenticationOperation(Operation(independent ? "other" : "original"))
                    : request).ExecuteAsync();
            }

            // Act
            await CaptureServiceFailureAsync(Acquire(false)).ConfigureAwait(false);
            await Assert.ThrowsExactlyAsync<MsalThrottledServiceException>(() => Acquire(false)).ConfigureAwait(false);
            http.ResponseFactory = null;
            var result = await Acquire(true).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(2, http.Calls);
            Assert.HasCount(1, TokenRequestCoalescer.GetFailureCache(app.ServiceBundle).CacheForTest);
            if (difference == "application")
            {
                Assert.IsTrue(TokenRequestCoalescer.GetFailureCache(other.ServiceBundle).IsEmpty());
            }
        }

        [TestMethod]
        [DataRow("client")]
        [DataRow("obo")]
        [DataRow("long-running")]
        [DataRow("silent")]
        public async Task ProactiveServiceFailure_PreservesUsableToken_AndThrottlesFurtherRefreshesAsync(string flow)
        {
            // Arrange
            using var http = new BlockingTokenEndpoint();
            var app = CreateDirectApp(http);
            var completed = new TaskCompletionSource<ExecutionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            app.ServiceBundle.Config.OnBackgroundTokenRefreshCompleted = result =>
            {
                completed.TrySetResult(result);
                return Task.CompletedTask;
            };
            var acquire = await CreateAcquisitionAsync(app, flow).ConfigureAwait(false);
            await acquire().ConfigureAwait(false);
            int before = http.Calls;
            var tokenCache = flow == "client" ? app.AppTokenCacheInternal : app.UserTokenCacheInternal;
            TestCommon.UpdateATWithRefreshOn(tokenCache.Accessor);
            http.Block = true;
            http.ResponseFactory = () => CreateFailureResponse(503);

            // Act
            var usable = await acquire().ConfigureAwait(false);
            await AwaitAsync(http.Entered.Task).ConfigureAwait(false);
            http.Release.TrySetResult(true);
            var firstRefresh = await AwaitAsync(completed.Task).ConfigureAwait(false);
            completed = new TaskCompletionSource<ExecutionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            var stillUsable = await acquire().ConfigureAwait(false);
            var secondRefresh = await AwaitAsync(completed.Task).ConfigureAwait(false);
            TokenCacheHelper.ExpireAllAccessTokens(tokenCache);
            var foreground = await CaptureServiceFailureAsync(acquire()).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(TokenSource.Cache, usable.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(TokenSource.Cache, stillUsable.AuthenticationResultMetadata.TokenSource);
            Assert.IsFalse(firstRefresh.Successful);
            Assert.IsFalse(secondRefresh.Successful);
            Assert.IsInstanceOfType<MsalThrottledServiceException>(secondRefresh.Exception);
            Assert.IsInstanceOfType<MsalThrottledServiceException>(foreground);
            Assert.AreEqual(2, http.Calls - before, "Only one proactive HTTP/retry cycle may run.");
            Assert.HasCount(1, TokenRequestCoalescer.GetFailureCache(app.ServiceBundle).CacheForTest);
        }

        [TestMethod]
        [DataRow("force")]
        [DataRow("claims")]
        [DataRow("disabled")]
        public async Task BypassServiceFailures_DoNotReadOrPopulateCoalescedFailureCacheAsync(string bypass)
        {
            // Arrange
            using var http = new BlockingTokenEndpoint { ResponseFactory = () => CreateFailureResponse(429) };
            var app = CreateDirectApp(http);
            if (bypass == "disabled")
            {
                app.ServiceBundle.Config.AccessorOptions = CacheOptions.DisableInternalCacheOptions;
            }
            Task<AuthenticationResult> Acquire()
            {
                var request = app.AcquireTokenForClient(TestConstants.s_scope).WithForceRefresh(bypass == "force");
                return (bypass == "claims" ? request.WithClaims("{\"access_token\":{\"foo\":null}}") : request).ExecuteAsync();
            }

            // Act
            await CaptureServiceFailureAsync(Acquire()).ConfigureAwait(false);
            await CaptureServiceFailureAsync(Acquire()).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(2, http.Calls);
            Assert.IsTrue(TokenRequestCoalescer.GetFailureCache(app.ServiceBundle).IsEmpty());
        }

        [TestMethod]
        [DataRow("force")]
        [DataRow("claims")]
        [DataRow("disabled")]
        public async Task BypassRequests_IgnoreExistingCoalescedFailureAsync(string bypass)
        {
            // Arrange
            using var http = new BlockingTokenEndpoint { ResponseFactory = () => CreateFailureResponse(429) };
            var app = CreateDirectApp(http);
            await CaptureServiceFailureAsync(app.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync()).ConfigureAwait(false);
            http.ResponseFactory = null;
            if (bypass == "disabled")
            {
                app.ServiceBundle.Config.AccessorOptions = CacheOptions.DisableInternalCacheOptions;
            }

            // Act
            var request = app.AcquireTokenForClient(TestConstants.s_scope).WithForceRefresh(bypass == "force");
            var result = await (bypass == "claims"
                ? request.WithClaims("{\"access_token\":{\"foo\":null}}") : request).ExecuteAsync().ConfigureAwait(false);

            // Assert
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(2, http.Calls);
            Assert.HasCount(1, TokenRequestCoalescer.GetFailureCache(app.ServiceBundle).CacheForTest);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task ServiceFailureRetryCallback_OnlyFinalFailureIsCachedAsync(bool recover)
        {
            // Arrange
            using var http = new BlockingTokenEndpoint { ResponseFactory = () => CreateFailureResponse(429) };
            var app = CreateDirectApp(http);
            int callbacks = 0;
            app.ServiceBundle.Config.OnMsalServiceFailure = (_, _) =>
            {
                if (++callbacks == 1)
                {
                    if (recover)
                    {
                        http.ResponseFactory = null;
                    }
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            };
            Task<AuthenticationResult> Acquire() => app.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync();

            // Act / Assert
            if (recover)
            {
                Assert.AreEqual(TokenSource.IdentityProvider,
                    (await Acquire().ConfigureAwait(false)).AuthenticationResultMetadata.TokenSource);
                Assert.IsTrue(TokenRequestCoalescer.GetFailureCache(app.ServiceBundle).IsEmpty());
            }
            else
            {
                var final = await CaptureServiceFailureAsync(Acquire()).ConfigureAwait(false);
                var throttled = await Assert.ThrowsExactlyAsync<MsalThrottledServiceException>(() => Acquire()).ConfigureAwait(false);
                Assert.AreSame(final, throttled.OriginalServiceException);
                Assert.AreEqual(2, callbacks, "Throttled followers must not re-enter the acquisition's retry callback.");
            }
            Assert.AreEqual(2, http.Calls);
        }

        [TestMethod]
        [DataRow(400, "invalid_client", false)]
        [DataRow(503, "invalid_client", false)]
        [DataRow(400, "invalid_grant", false)]
        [DataRow(503, "invalid_grant", false)]
        [DataRow(429, "temporarily_unavailable", true)]
        public async Task CredentialAndClaimsErrors_AreNotCachedAsync(int status, string error, bool claims)
        {
            // Arrange
            using var http = new BlockingTokenEndpoint { ResponseFactory = () => CreateFailureResponse(status, error, claims) };
            var app = CreateDirectApp(http);

            // Act
            var first = await CaptureServiceFailureAsync(app.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync()).ConfigureAwait(false);
            var second = await CaptureServiceFailureAsync(app.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync()).ConfigureAwait(false);

            // Assert
            Assert.IsNotInstanceOfType<MsalThrottledServiceException>(first);
            Assert.IsNotInstanceOfType<MsalThrottledServiceException>(second);
            Assert.AreEqual(status >= 500 ? 4 : 2, http.Calls);
            Assert.IsTrue(TokenRequestCoalescer.GetFailureCache(app.ServiceBundle).IsEmpty());
        }

        [TestMethod]
        public async Task RetryAfterServiceFailure_UsesExistingProvider_NotCoalescedFailureCacheAsync()
        {
            // Arrange
            using var http = new BlockingTokenEndpoint
            {
                ResponseFactory = () =>
                {
                    var response = CreateFailureResponse(503);
                    response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(120));
                    return response;
                }
            };
            var app = CreateDirectApp(http);

            // Act
            var first = await CaptureServiceFailureAsync(app.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync()).ConfigureAwait(false);
            var second = await CaptureServiceFailureAsync(app.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync()).ConfigureAwait(false);
            var bypass = await CaptureServiceFailureAsync(app.AcquireTokenForClient(TestConstants.s_scope)
                .WithForceRefresh(true).ExecuteAsync()).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(1, http.Calls, "Retry-After still prevents HTTP retries and applies to bypass requests.");
            Assert.IsInstanceOfType<MsalThrottledServiceException>(second);
            Assert.IsInstanceOfType<MsalThrottledServiceException>(bypass);
            Assert.AreSame(first, ((MsalThrottledServiceException)second).OriginalServiceException);
            Assert.IsTrue(TokenRequestCoalescer.GetFailureCache(app.ServiceBundle).IsEmpty());
        }

        [TestMethod]
        [DataRow("provider")]
        [DataRow("before-request")]
        [DataRow("timeout")]
        [DataRow("network")]
        [DataRow("cancellation")]
        public async Task NonServiceResponseFailures_AreNotCachedAsync(string failure)
        {
            // Arrange
            using var http = new BlockingTokenEndpoint();
            using var cancellation = new CancellationTokenSource();
            var app = CreateDirectApp(http);
            int callbackCalls = 0;
            if (failure == "provider")
            {
                app.ServiceBundle.Config.AppTokenProvider = _ =>
                {
                    callbackCalls++;
                    throw new MsalServiceException("provider_failure", "Not an ESTS response", 503);
                };
            }
            if (failure == "timeout" || failure == "network" || failure == "cancellation")
            {
                http.ResponseFactory = () =>
                {
                    if (failure == "cancellation")
                    {
                        cancellation.Cancel();
                    }
                    if (failure == "network")
                    {
                        throw new HttpRequestException("network_failure");
                    }
                    throw new TaskCanceledException("http_timeout_or_cancellation");
                };
            }
            Task<AuthenticationResult> Acquire()
            {
                var request = app.AcquireTokenForClient(TestConstants.s_scope);
                if (failure == "before-request")
                {
                    request.OnBeforeTokenRequest(_ =>
                    {
                        callbackCalls++;
                        throw new MsalServiceException("callback_failure", "Not an ESTS response", 503);
                    });
                }
                return request.ExecuteAsync(cancellation.Token);
            }

            // Act / Assert
            for (int i = 0; i < 2; i++)
            {
                if (failure == "network")
                {
                    await Assert.ThrowsAsync<HttpRequestException>(() => Acquire()).ConfigureAwait(false);
                }
                else if (failure == "cancellation")
                {
                    await Assert.ThrowsAsync<OperationCanceledException>(() => Acquire()).ConfigureAwait(false);
                }
                else
                {
                    Assert.IsNotInstanceOfType<MsalThrottledServiceException>(
                        await CaptureServiceFailureAsync(Acquire()).ConfigureAwait(false));
                }
            }
            Assert.IsTrue(TokenRequestCoalescer.GetFailureCache(app.ServiceBundle).IsEmpty());
            Assert.AreEqual(failure == "timeout" ? 4 : failure == "network" ? 2 : failure == "cancellation" ? 1 : 0, http.Calls);
            Assert.AreEqual(failure == "provider" || failure == "before-request" ? 2 : 0, callbackCalls);
        }

        [TestMethod]
        public async Task RequestCallbacks_DifferentStrategiesProgress_WhileSameStrategyWaitsAsync()
        {
            // Arrange
            using var http = new BlockingTokenEndpoint();
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var app = CreateDirectApp(http);
            var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            int ownerCallbacks = 0;
            int otherCallbacks = 0;
            Func<OnBeforeTokenRequestData, Task> ownerCallback = async _ =>
            {
                Interlocked.Increment(ref ownerCallbacks);
                entered.TrySetResult(true);
                await release.Task.ConfigureAwait(false);
            };
            Func<OnBeforeTokenRequestData, Task> otherCallback = _ =>
            {
                Interlocked.Increment(ref otherCallbacks);
                return Task.CompletedTask;
            };
            Task<AuthenticationResult> Acquire(Func<OnBeforeTokenRequestData, Task> callback) =>
                app.AcquireTokenForClient(TestConstants.s_scope).OnBeforeTokenRequest(callback)
                    .ExecuteAsync(cancellation.Token);

            // Act
            var owner = Acquire(ownerCallback);
            await AwaitAsync(entered.Task).ConfigureAwait(false);
            var waiters = Enumerable.Range(0, 4).Select(_ => Acquire(ownerCallback)).ToArray();
            try
            {
                Assert.IsTrue(waiters.All(t => !t.IsCompleted));
                var independent = await AwaitAsync(Acquire(otherCallback)).ConfigureAwait(false);
                Assert.AreEqual(TokenSource.IdentityProvider, independent.AuthenticationResultMetadata.TokenSource);
                Assert.IsFalse(owner.IsCompleted);
                Assert.HasCount(1, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
            }
            finally
            {
                release.TrySetResult(true);
            }
            var result = await AwaitAsync(owner).ConfigureAwait(false);
            var followers = await Task.WhenAll(waiters).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            Assert.IsTrue(followers.All(r => r.AuthenticationResultMetadata.TokenSource == TokenSource.Cache));
            Assert.AreEqual(1, ownerCallbacks);
            Assert.AreEqual(1, otherCallbacks);
            Assert.AreEqual(2, http.Calls);
            Assert.HasCount(1, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
        }

        [TestMethod]
        public async Task NestedProvider_SameStrategy_DoesNotBypassLiveAdmissionAsync()
        {
            // Arrange
            using var http = new BlockingTokenEndpoint();
            ConfidentialClientApplication app = null;
            int providerCalls = 0;
            app = ConfidentialClientApplicationBuilder.Create(Guid.NewGuid().ToString())
                .WithAuthority(TestConstants.AuthorityTestTenant).WithInstanceDiscovery(false)
                .WithHttpClientFactory(http)
                .WithAppTokenProvider(async parameters =>
                {
                    if (Interlocked.Increment(ref providerCalls) == 1)
                    {
                        using var cancellation = new CancellationTokenSource();
                        var nested = app.AcquireTokenForClient(parameters.Scopes).ExecuteAsync(cancellation.Token);
                        Assert.IsFalse(nested.IsCompleted, "Nesting must not bypass the active strategy's admission.");
                        cancellation.Cancel();
                        await Assert.ThrowsAsync<OperationCanceledException>(() => nested).ConfigureAwait(false);
                    }
                    return new AppTokenProviderResult { AccessToken = "token", ExpiresInSeconds = 3600 };
                }).BuildConcrete();

            // Act
            var result = await AwaitAsync(app.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync()).ConfigureAwait(false);
            var warm = await app.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync().ConfigureAwait(false);

            // Assert
            Assert.AreEqual(1, providerCalls);
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(TokenSource.Cache, warm.AuthenticationResultMetadata.TokenSource);
            Assert.HasCount(1, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
            Assert.AreEqual(0, http.Calls);
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
        public async Task DifferentApplicationProvider_ProgressesBeforeOwnerFailureOrCancellationAsync(bool cancelOwner)
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
            var result = await AwaitAsync(waiter).ConfigureAwait(false);
            Assert.IsFalse(owner.IsCompleted);
            Assert.HasCount(1, succeeding.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
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

        private static ConfidentialClientApplication CreateDirectApp(
            BlockingTokenEndpoint http, string clientId = null, bool shared = false)
        {
            var app = ConfidentialClientApplicationBuilder.Create(clientId ?? Guid.NewGuid().ToString())
                .WithAuthority(TestConstants.AuthorityTestTenant).WithInstanceDiscovery(false)
                .WithClientSecret(TestConstants.ClientSecret).WithHttpClientFactory(http)
                .WithCacheOptions(new CacheOptions(shared)).BuildConcrete();
            var retryFactory = Substitute.For<IRetryPolicyFactory>();
            retryFactory.GetRetryPolicy(Arg.Any<RequestType>()).Returns(new TestDefaultRetryPolicy(RequestType.STS));
            app.ServiceBundle.Config.RetryPolicyFactory = retryFactory;
            return app;
        }

        private static async Task<Func<Task<AuthenticationResult>>> CreateAcquisitionAsync(
            ConfidentialClientApplication app, string flow)
        {
            string session = Guid.NewGuid().ToString();
            IAccount account = null;
            if (flow == "long-running")
            {
                await app.InitiateLongRunningProcessInWebApi(TestConstants.s_scope,
                    TestConstants.DefaultAccessToken, ref session).ExecuteAsync().ConfigureAwait(false);
            }
            else if (flow == "silent")
            {
                account = (await app.AcquireTokenByAuthorizationCode(TestConstants.s_scope, "code")
                    .ExecuteAsync().ConfigureAwait(false)).Account;
            }
            return () => flow switch
            {
                "obo" => app.AcquireTokenOnBehalfOf(TestConstants.s_scope,
                    new UserAssertion(TestConstants.DefaultAccessToken)).ExecuteAsync(),
                "long-running" => app.AcquireTokenInLongRunningProcess(TestConstants.s_scope, session).ExecuteAsync(),
                "silent" => app.AcquireTokenSilent(TestConstants.s_scope, account).ExecuteAsync(),
                _ => app.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync()
            };
        }

        private static HttpResponseMessage CreateFailureResponse(int status, string error = "temporarily_unavailable", bool claims = false)
        {
            return new HttpResponseMessage((HttpStatusCode)status)
            {
                Content = new StringContent("{\"error\":\"" + error + "\",\"error_description\":\"test failure\"" +
                    (claims ? ",\"claims\":\"challenge\"" : "") + "}")
            };
        }

        private static Task<MsalServiceException> CaptureServiceFailureAsync(Task<AuthenticationResult> request) =>
            Assert.ThrowsAsync<MsalServiceException>(() => request);

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
            public Func<HttpResponseMessage> ResponseFactory { get; set; }
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
                if (ResponseFactory is not null)
                {
                    return ResponseFactory();
                }
                string body = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
                return body.Contains("grant_type=client_credentials")
                    ? MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage()
                    : MockHelpers.CreateSuccessTokenResponseMessage();
            }
        }
    }
}
