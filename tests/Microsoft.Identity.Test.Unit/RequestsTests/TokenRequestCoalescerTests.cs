// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.AuthScheme;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.ClientCredential;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.RequestsTests
{
    [TestClass]
    public class TokenRequestCoalescerTests : TestBase
    {
        [TestMethod]
        [DataRow("scope-order", false)]
        [DataRow("shared-cache", false)]
        [DataRow("scope", true)]
        [DataRow("tenant", true)]
        [DataRow("assertion", true)]
        [DataRow("session", true)]
        [DataRow("binding", true)]
        [DataRow("extra-key", true)]
        [DataRow("cache", true)]
        [DataRow("flow", true)]
        public void Admission_UsesCompleteOperationAndCacheIdentity(string difference, bool independent)
        {
            // Arrange
            var service = TestCommon.CreateDefaultServiceBundle();
            service.Config.AccessorOptions = new CacheOptions(difference == "shared-cache");
            var cache = new TokenCache(service, false);
            var api = difference == "assertion" || difference == "session"
                ? ApiEvent.ApiIds.AcquireTokenOnBehalfOf
                : ApiEvent.ApiIds.AcquireTokenForClient;
            AuthenticationRequestParameters CreateParameters(bool other)
            {
                var common = new AcquireTokenCommonParameters
                {
                    ApiId = api,
                    Scopes = other && difference == "scope" ? new[] { "a", "c" } :
                        other ? new[] { "b", "a" } : new[] { "a", "b" }
                };
                var context = new RequestContext(service, Guid.NewGuid(), null)
                {
                    ApiEvent = new ApiEvent(Guid.NewGuid())
                };
                var parameters = new AuthenticationRequestParameters(
                    service,
                    other && (difference == "cache" || difference == "shared-cache") ? new TokenCache(service, false) : cache,
                    common, context,
                    Authority.CreateAuthority(other && difference == "tenant"
                        ? "https://login.microsoftonline.com/other-tenant"
                        : TestConstants.AuthorityTestTenant),
                    cacheKeyComponents: new SortedList<string, string>
                    {
                        ["component"] = other && difference == "extra-key" ? "other" : "same"
                    })
                {
                    UserAssertion = new UserAssertion(other && difference == "assertion" ? "other-assertion" : "assertion"),
                    LongRunningOboCacheKey = difference == "session" ? (other ? "other-session" : "session") : null
                };
                if (other && difference == "binding")
                {
                    var scheme = Substitute.For<IAuthenticationOperation>();
                    scheme.AccessTokenType.Returns("pop");
                    scheme.KeyId.Returns("key");
                    parameters.AuthenticationScheme = scheme;
                }
                return parameters;
            }

            var owner = new TokenRequestCoalescer(CreateParameters(false), "flow",
                () => throw new AssertFailedException("Admission must not read the cache."),
                (_, _) => throw new AssertFailedException("Admission must not create results."));
            var contender = new TokenRequestCoalescer(CreateParameters(true), difference == "flow" ? "other-flow" : "flow",
                () => throw new AssertFailedException("Admission must not read the cache."),
                (_, _) => throw new AssertFailedException("Admission must not create results."));

            // Act / Assert
            using var held = owner.TryEnter();
            Assert.IsNotNull(held);
            using var admitted = contender.TryEnter();
            Assert.AreEqual(independent, admitted is not null);
        }

        [TestMethod]
        [DataRow("provider", false, false, false)]
        [DataRow("provider", true, false, true)]
        [DataRow("provider", false, true, true)]
        [DataRow("certificate", false, false, false)]
        [DataRow("certificate", true, false, true)]
        [DataRow("certificate", false, true, true)]
        [DataRow("assertion", false, false, false)]
        [DataRow("assertion", true, false, true)]
        [DataRow("assertion", false, true, true)]
        [DataRow("signed-assertion", false, false, false)]
        [DataRow("signed-assertion", true, false, true)]
        [DataRow("signed-assertion", false, true, true)]
        [DataRow("failure", false, false, false)]
        [DataRow("failure", true, false, true)]
        [DataRow("failure", false, true, true)]
        [DataRow("request", false, false, false)]
        [DataRow("request", true, false, true)]
        [DataRow("request", false, true, true)]
        [DataRow("request-order", false, true, true)]
        [DataRow("request-absent", false, true, true)]
        [DataRow("completion", true, true, false)]
        [DataRow("background-completion", true, true, false)]
        [DataRow("telemetry-observer", true, true, false)]
        [DataRow("secret", true, true, false)]
        [DataRow("static-assertion", true, true, false)]
        [DataRow("static-certificate", true, true, false)]
        [DataRow("static-claims", true, true, false)]
        public void CallbackAdmission_UsesApplicationAndConfiguredStrategyIdentity(
            string kind, bool differentApplication, bool differentStrategy, bool independent)
        {
            // Arrange
            string clientId = Guid.NewGuid().ToString();
            var service = TestCommon.CreateDefaultServiceBundle();
            var otherService = differentApplication ? TestCommon.CreateDefaultServiceBundle() : service;
            Func<AppTokenProviderParameters, Task<AppTokenProviderResult>> CreateProvider(string token) =>
                _ => Task.FromResult(new AppTokenProviderResult { AccessToken = token });
            Func<OnBeforeTokenRequestData, Task> CreateHandler(string value) =>
                _ => Task.FromResult(value);
            var firstProvider = CreateProvider("first");
            var secondProvider = CreateProvider("second");
            var firstHandler = CreateHandler("first");
            var secondHandler = CreateHandler("second");
            Assert.AreEqual(firstProvider.Method, secondProvider.Method);
            Assert.AreEqual(firstHandler.Method, secondHandler.Method);
            Func<AssertionRequestOptions, ExecutionResult, Task<bool>> firstFailure = (_, _) => Task.FromResult(false);
            Func<AssertionRequestOptions, ExecutionResult, Task<bool>> secondFailure = (_, _) => Task.FromResult(true);
            IClientCredential CreateCredential() => kind switch
            {
                "certificate" => new DynamicCertificateClientCredential(_ => Task.FromResult(CertHelper.GetOrCreateTestCert())),
                "assertion" => new ClientAssertionStringDelegateCredential((_, _) => Task.FromResult("assertion")),
                "signed-assertion" => new ClientAssertionDelegateCredential((_, _) =>
                    Task.FromResult(new ClientSignedAssertion { Assertion = "assertion" })),
                "static-assertion" => new SignedAssertionClientCredential("assertion"),
                "static-certificate" => new CertificateClientCredential(CertHelper.GetOrCreateTestCert()),
                "static-claims" => new CertificateAndClaimsClientCredential(
                    _ => Task.FromResult(CertHelper.GetOrCreateTestCert()), new Dictionary<string, string>(), true,
                    CertHelper.GetOrCreateTestCert()),
                _ => new ClientSecretCredential(TestConstants.ClientSecret)
            };
            var firstCredential = CreateCredential();
            var secondCredential = CreateCredential();
            AuthenticationRequestParameters CreateParameters(IServiceBundle bundle, bool alternative)
            {
                bundle.Config.ClientId = clientId;
                bundle.Config.AccessorOptions = CacheOptions.EnableSharedCacheOptions;
                bundle.Config.ClientCredential = alternative ? secondCredential : firstCredential;
                if (kind == "provider")
                {
                    bundle.Config.AppTokenProvider = alternative ? secondProvider : firstProvider;
                }
                if (kind == "failure")
                {
                    bundle.Config.OnMsalServiceFailure = alternative ? secondFailure : firstFailure;
                }
                if (kind == "completion")
                {
                    bundle.Config.OnCompletion = (_, _) => Task.CompletedTask;
                }
                if (kind == "background-completion")
                {
                    bundle.Config.OnBackgroundTokenRefreshCompleted = _ => Task.CompletedTask;
                }
                var common = new AcquireTokenCommonParameters
                {
                    ApiId = ApiEvent.ApiIds.AcquireTokenForClient,
                    Scopes = TestConstants.s_scope
                };
                if (kind == "telemetry-observer")
                {
                    common.OtelTagsEnricher = (_, _) => { };
                }
                if (kind == "request" || kind == "request-order" || kind == "request-absent")
                {
                    common.OnBeforeTokenRequestHandler = kind == "request-order"
                        ? new List<Func<OnBeforeTokenRequestData, Task>>(alternative
                            ? new[] { secondHandler, firstHandler } : new[] { firstHandler, secondHandler })
                        : kind == "request-absent" && alternative ? null
                        : new List<Func<OnBeforeTokenRequestData, Task>> { alternative ? secondHandler : firstHandler };
                }
                return new AuthenticationRequestParameters(bundle, new TokenCache(bundle, false), common,
                    new RequestContext(bundle, Guid.NewGuid(), null) { ApiEvent = new ApiEvent(Guid.NewGuid()) },
                    Authority.CreateAuthority(TestConstants.AuthorityTestTenant));
            }
            TokenRequestCoalescer CreateCoalescer(AuthenticationRequestParameters parameters) =>
                new TokenRequestCoalescer(parameters, "flow",
                    () => throw new AssertFailedException("Admission must not read the cache."),
                    (_, _) => throw new AssertFailedException("Admission must not create results."));
            var owner = CreateCoalescer(CreateParameters(service, false));
            var contender = CreateCoalescer(CreateParameters(otherService, differentStrategy));

            // Act / Assert
            using var held = owner.TryEnter();
            Assert.IsNotNull(held);
            using var admitted = contender.TryEnter();
            Assert.AreEqual(independent, admitted is not null);
            Assert.AreEqual(kind == "certificate" || kind == "assertion" || kind == "signed-assertion",
                firstCredential.HasRequestCallback);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task LateObservation_DoesNotRefreshNewGeneration_EvenWhenRefreshIsStillDueAsync(bool sameSecret)
        {
            // Arrange
            var service = TestCommon.CreateDefaultServiceBundle();
            var parameters = TestCommon.CreateAuthenticationRequestParameters(service);
            parameters.RequestContext.ApiEvent.CacheInfo = CacheRefreshReason.ProactivelyRefreshed;
            var now = DateTimeOffset.UtcNow;
            MsalAccessTokenCacheItem CreateToken(string secret, DateTimeOffset cachedAt) =>
                new MsalAccessTokenCacheItem("login.microsoftonline.com", service.Config.ClientId, "scope", "tenant",
                    secret, cachedAt, now.AddHours(1), now.AddHours(1), null, null, refreshOn: now.AddHours(-1));
            var observed = CreateToken("old", now.AddMinutes(-10));
            var published = CreateToken(sameSecret ? "old" : "new", sameSecret ? now : observed.CachedAt);
            var current = observed;
            int acquisitions = 0;
            Task<AuthenticationResult> CreateResult(MsalAccessTokenCacheItem token, TokenSource source) =>
                AuthenticationResult.CreateAsync(token, null, parameters.AuthenticationScheme,
                    parameters.RequestContext.CorrelationId, source, parameters.RequestContext.ApiEvent,
                    null, null, null, CancellationToken.None);
            var coalescer = new TokenRequestCoalescer(parameters, "flow",
                () => Task.FromResult(current),
                (_, _) => throw new AssertFailedException(
                    "A skipped background refresh must not create another cache result."));
            Task<AuthenticationResult> Acquire()
            {
                acquisitions++;
                current = published;
                return CreateResult(current, TokenSource.IdentityProvider);
            }

            // Act: the second caller made its refresh decision before the first publication.
            using (coalescer.TryEnter())
            {
                var owner = await coalescer.ExecuteUnderGateAsync(Acquire, observed, CancellationToken.None).ConfigureAwait(false);
                Assert.AreEqual(TokenSource.IdentityProvider, owner.AuthenticationResultMetadata.TokenSource);
            }
            Assert.IsTrue(SilentRequestHelper.NeedsRefresh(published));
            using (coalescer.TryEnter())
            {
                var late = await coalescer.ExecuteUnderGateAsync(Acquire, observed, CancellationToken.None).ConfigureAwait(false);
                Assert.IsNull(late, "A newer cached generation must suppress the background acquisition.");
            }

            // Assert
            Assert.AreEqual(1, acquisitions);
            string header = service.HttpTelemetryManager.GetCurrentRequestHeader(
                new ApiEvent(Guid.NewGuid()) { CacheInfo = CacheRefreshReason.ProactivelyRefreshed });
            Assert.IsTrue(header.EndsWith(",1,1", StringComparison.Ordinal));
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(true, false)]
        [DataRow(false, true)]
        [DataRow(true, true)]
        public async Task ProactiveRecheck_AsyncValidation_DoesNotMutateForegroundTelemetryAsync(
            bool newerGeneration, bool rejectRecheckedToken)
        {
            // Arrange
            var service = TestCommon.CreateDefaultServiceBundle();
            var parameters = TestCommon.CreateAuthenticationRequestParameters(service);
            var foregroundEvent = parameters.RequestContext.ApiEvent;
            foregroundEvent.CacheInfo = CacheRefreshReason.ProactivelyRefreshed;
            foregroundEvent.IsAccessTokenCacheHit = true;
            var now = DateTimeOffset.UtcNow;
            MsalAccessTokenCacheItem CreateToken(DateTimeOffset cachedAt) =>
                new MsalAccessTokenCacheItem("login.microsoftonline.com", service.Config.ClientId, "scope", "tenant",
                    "token", cachedAt, now.AddHours(1), now.AddHours(1), null, null, refreshOn: now.AddHours(-1));
            var observed = CreateToken(now.AddMinutes(-10));
            var current = newerGeneration ? CreateToken(now) : observed;
            var validating = new TaskCompletionSource<ApiEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var completed = new TaskCompletionSource<ExecutionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            var operation = Substitute.For<IAuthenticationOperation2>();
            operation.AccessTokenType.Returns("bearer");
            operation.GetTokenRequestParams().Returns(new Dictionary<string, string>());
            operation.FormatResultAsync(Arg.Any<AuthenticationResult>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);
            int validations = 0;
            operation.ValidateCachedTokenAsync(Arg.Any<MsalCacheValidationData>()).Returns(async _ =>
            {
                if (Interlocked.Increment(ref validations) == 1)
                {
                    validating.TrySetResult(parameters.RequestContext.ApiEvent);
                    await release.Task.ConfigureAwait(false);
                    return !rejectRecheckedToken;
                }
                return true;
            });
            parameters.AuthenticationScheme = operation;
            service.Config.OnBackgroundTokenRefreshCompleted = result =>
            {
                completed.TrySetResult(result);
                return Task.CompletedTask;
            };
            Task<AuthenticationResult> CreateResult(TokenSource source) =>
                AuthenticationResult.CreateAsync(current, null, parameters.AuthenticationScheme,
                    parameters.RequestContext.CorrelationId, source, parameters.RequestContext.ApiEvent,
                    null, null, null, CancellationToken.None);
            int lookups = 0;
            int acquisitions = 0;
            var coalescer = new TokenRequestCoalescer(parameters, "flow",
                () =>
                {
                    if (Interlocked.Increment(ref lookups) == 1)
                    {
                        parameters.RequestContext.ApiEvent.CacheInfo = CacheRefreshReason.NotApplicable;
                    }
                    return Task.FromResult(current);
                }, (_, _) => CreateResult(TokenSource.Cache));
            var foreground = await CreateResult(TokenSource.Cache).ConfigureAwait(false);
            string foregroundHeader = service.HttpTelemetryManager.GetCurrentRequestHeader(foregroundEvent);

            // Act
            SilentRequestHelper.ProcessFetchInBackground(observed, () =>
            {
                Interlocked.Increment(ref acquisitions);
                parameters.RequestContext.ApiEvent.DurationInHttpInMs = 123;
                return CreateResult(TokenSource.IdentityProvider);
            }, parameters.RequestContext.Logger, service, foregroundEvent, null, null, coalescer: coalescer);
            var backgroundEvent = await AwaitAsync(validating.Task).ConfigureAwait(false);
            try
            {
                RequestBase.PopulateSuccessMetadata(foreground.AuthenticationResultMetadata,
                    foregroundEvent, 1, CacheLevel.None);

                // Assert while validation is suspended, before background code can restore its reason.
                Assert.AreNotSame(foregroundEvent, backgroundEvent);
                Assert.AreEqual(CacheRefreshReason.NotApplicable, backgroundEvent.CacheInfo);
                Assert.AreEqual(CacheRefreshReason.ProactivelyRefreshed, foregroundEvent.CacheInfo);
                Assert.AreEqual(CacheRefreshReason.ProactivelyRefreshed,
                    foreground.AuthenticationResultMetadata.CacheRefreshReason);
                Assert.AreEqual(0L, foreground.AuthenticationResultMetadata.DurationInHttpInMs);
                Assert.AreEqual(foregroundHeader, service.HttpTelemetryManager.GetCurrentRequestHeader(foregroundEvent),
                    "The suspended background recheck must not change foreground HTTP telemetry.");
            }
            finally
            {
                release.TrySetResult(true);
            }

            // Waiting for admission also observes completion when a newer generation suppresses refresh.
            await AwaitAsync(coalescer.ExecuteAsync(
                () => throw new AssertFailedException("The waiter must reuse the cached token."),
                CancellationToken.None)).ConfigureAwait(false);
            bool shouldAcquire = !newerGeneration || rejectRecheckedToken;
            if (shouldAcquire)
            {
                var background = await AwaitAsync(completed.Task).ConfigureAwait(false);
                Assert.IsTrue(background.Successful);
                Assert.AreEqual(CacheRefreshReason.ProactivelyRefreshed,
                    background.Result.AuthenticationResultMetadata.CacheRefreshReason);
                Assert.AreEqual(123L, background.Result.AuthenticationResultMetadata.DurationInHttpInMs);
            }
            Assert.AreEqual(shouldAcquire ? 1 : 0, acquisitions);
            Assert.AreEqual(CacheRefreshReason.ProactivelyRefreshed, foregroundEvent.CacheInfo);
            Assert.AreEqual(0L, foregroundEvent.DurationInHttpInMs);
            Assert.IsTrue(foregroundEvent.IsAccessTokenCacheHit);
        }

        private static async Task<T> AwaitAsync<T>(Task<T> task)
        {
            Assert.AreSame(task, await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(10))).ConfigureAwait(false),
                "The gated operation did not complete.");
            return await task.ConfigureAwait(false);
        }

        [TestMethod]
        [DataRow(CacheRefreshReason.NoCachedAccessToken, CacheRefreshReason.NotApplicable, false, false, CacheRefreshReason.NoCachedAccessToken)]
        [DataRow(CacheRefreshReason.Expired, CacheRefreshReason.NotApplicable, false, false, CacheRefreshReason.Expired)]
        [DataRow(CacheRefreshReason.NoCachedAccessToken, CacheRefreshReason.Expired, false, false, CacheRefreshReason.Expired)]
        [DataRow(CacheRefreshReason.ProactivelyRefreshed, CacheRefreshReason.NotApplicable, false, true, CacheRefreshReason.ProactivelyRefreshed)]
        [DataRow(CacheRefreshReason.NoCachedAccessToken, CacheRefreshReason.NotApplicable, true, false, CacheRefreshReason.NotApplicable)]
        public async Task CacheRecheck_RestoresOnlyLostForegroundReason_AndPreservesHitsAndProactiveRefreshAsync(
            CacheRefreshReason initial, CacheRefreshReason recheck, bool cacheHit, bool proactive, CacheRefreshReason expected)
        {
            // Arrange
            var service = TestCommon.CreateDefaultServiceBundle();
            var parameters = TestCommon.CreateAuthenticationRequestParameters(service);
            var apiEvent = parameters.RequestContext.ApiEvent;
            apiEvent.CacheInfo = initial;
            var now = DateTimeOffset.UtcNow;
            var token = new MsalAccessTokenCacheItem("login.microsoftonline.com", service.Config.ClientId, "scope", "tenant",
                "token", now.AddMinutes(-10), now.AddHours(1), now.AddHours(1), null, null, refreshOn: now.AddHours(-1));
            Task<AuthenticationResult> CreateResult(TokenSource source) =>
                AuthenticationResult.CreateAsync(token, null, parameters.AuthenticationScheme,
                    parameters.RequestContext.CorrelationId, source, apiEvent, null, null, null, CancellationToken.None);
            var coalescer = new TokenRequestCoalescer(parameters, "flow",
                () =>
                {
                    apiEvent.CacheInfo = recheck;
                    return Task.FromResult(cacheHit || proactive ? token : null);
                },
                (_, _) => CreateResult(TokenSource.Cache));
            int acquisitions = 0;

            // Act
            var result = await coalescer.ExecuteUnderGateAsync(() =>
            {
                acquisitions++;
                Assert.AreEqual(expected, apiEvent.CacheInfo);
                return CreateResult(TokenSource.IdentityProvider);
            }, proactive ? token : null, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(expected, apiEvent.CacheInfo);
            Assert.AreEqual(cacheHit ? 0 : 1, acquisitions);
            Assert.AreEqual(cacheHit ? TokenSource.Cache : TokenSource.IdentityProvider,
                result.AuthenticationResultMetadata.TokenSource);
        }
    }
}
