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
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Test.Common;
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
    }
}
