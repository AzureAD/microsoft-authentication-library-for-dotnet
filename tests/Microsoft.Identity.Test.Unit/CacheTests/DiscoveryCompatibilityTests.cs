// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CacheTests
{
    [TestClass]
    [DoNotParallelize]
    public class DiscoveryCompatibilityTests
    {
        private bool _previousDisabled;

        [TestInitialize]
        public void Initialize()
        {
            AppContext.TryGetSwitch(TokenCache.DisableCacheReadIsolationSwitch, out _previousDisabled);
            AppContext.SetSwitch(TokenCache.DisableCacheReadIsolationSwitch, false);
        }

        [TestCleanup]
        public void Cleanup()
        {
            AppContext.SetSwitch(TokenCache.DisableCacheReadIsolationSwitch, _previousDisabled);
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(false, true)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public async Task AccountCacheHitThenIndependentLoginHintPreservesProfiles(bool clear, bool synchronous)
        {
            using var http = new MockHttpManager();
            var application = CreateApplication(http);
            var seed = Seed("discovery-a", "alice@example.test", profiles: true);
            var provider = new PartitionProvider { Clear = clear };
            provider.Payloads[seed.Account.HomeAccountId.Identifier] = seed.Payload;
            provider.Register(application.UserTokenCache, synchronous);

            var first = await application.AcquireTokenSilent(TestConstants.s_scope, seed.Account).ExecuteAsync().ConfigureAwait(false);
            var second = await application.AcquireTokenSilent(TestConstants.s_scope, seed.Account.Username).ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, first.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(TokenSource.Cache, second.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(seed.Account.HomeAccountId.Identifier, second.Account.HomeAccountId.Identifier);
            CollectionAssert.AreEquivalent(new[] { TestConstants.Utid, "33333333-3333-3333-3333-333333333333" },
                second.Account.GetTenantProfiles().Select(profile => profile.TenantId).ToArray());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task ReturnedClaimsCannotMutateAnotherDiscoveryOperation(bool resultClaims)
        {
            using var http = new MockHttpManager();
            var application = CreateApplication(http);
            var seed = Seed("discovery-claims", "alice@example.test");
            var provider = new PartitionProvider { Clear = true };
            provider.Payloads[seed.Account.HomeAccountId.Identifier] = seed.Payload;
            provider.Register(application.UserTokenCache);

            var first = await application.AcquireTokenSilent(TestConstants.s_scope, seed.Account)
                .ExecuteAsync().ConfigureAwait(false);
            var principal = resultClaims ? first.ClaimsPrincipal : first.Account.GetTenantProfiles().Single().ClaimsPrincipal;
            principal.Claims.First().Properties["discovery-probe"] = "caller mutation";

            var second = (await application.GetAccountsAsync().ConfigureAwait(false)).Single();
            Assert.IsFalse(second.GetTenantProfiles().Single().ClaimsPrincipal.Claims
                .Any(claim => claim.Properties.ContainsKey("discovery-probe")),
                "Mutable claims exposed to one caller must not change another operation's published discovery state.");
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task DiscoveryPreservesMergeVersusReplacementAndAmbiguity(bool clear)
        {
            using var http = new MockHttpManager();
            var application = CreateApplication(http);
            var first = Seed("discovery-a", "same@example.test");
            var second = Seed("discovery-b", "same@example.test");
            var provider = new PartitionProvider { Clear = clear };
            provider.Payloads[first.Account.HomeAccountId.Identifier] = first.Payload;
            provider.Payloads[second.Account.HomeAccountId.Identifier] = second.Payload;
            provider.Register(application.UserTokenCache);
            await application.AcquireTokenSilent(TestConstants.s_scope, first.Account).ExecuteAsync().ConfigureAwait(false);
            await application.AcquireTokenSilent(TestConstants.s_scope, second.Account).ExecuteAsync().ConfigureAwait(false);

            var accounts = (await application.GetAccountsAsync().ConfigureAwait(false)).ToArray();
            Assert.HasCount(clear ? 1 : 2, accounts);
            if (clear)
            {
                var result = await application.AcquireTokenSilent(TestConstants.s_scope, "same@example.test").ExecuteAsync().ConfigureAwait(false);
                Assert.AreEqual(second.Account.HomeAccountId.Identifier, result.Account.HomeAccountId.Identifier);
            }
            else
            {
                bool ambiguous = false;
                try { await application.AcquireTokenSilent(TestConstants.s_scope, "same@example.test").ExecuteAsync().ConfigureAwait(false); }
                catch (MsalUiRequiredException exception) when (exception.ErrorCode == MsalError.MultipleAccountsForLoginHint) { ambiguous = true; }
                Assert.IsTrue(ambiguous, "Multiple matching accounts must not become a silently selected identity.");
            }
        }

        [TestMethod]
        public async Task FailedReadPublicationDoesNotPublishDiscovery()
        {
            using var http = new MockHttpManager();
            var application = CreateApplication(http);
            var seed = Seed("discovery-a", "alice@example.test");
            var provider = new PartitionProvider { FailReadPublication = true };
            provider.Payloads[seed.Account.HomeAccountId.Identifier] = seed.Payload;
            provider.Register(application.UserTokenCache);
            bool failed = false;
            try { await application.AcquireTokenSilent(TestConstants.s_scope, seed.Account).ExecuteAsync().ConfigureAwait(false); }
            catch (InvalidOperationException exception) when (exception.Message == "synthetic-publication") { failed = true; }
            Assert.IsTrue(failed);
            provider.FailReadPublication = false;
            Assert.IsEmpty(await application.GetAccountsAsync().ConfigureAwait(false));
        }

        [TestMethod]
        public async Task RemovingAnAccountInvalidatesDiscoveryAndDoesNotReplayIt()
        {
            using var http = new MockHttpManager();
            var application = CreateApplication(http);
            var seed = Seed("discovery-a", "alice@example.test");
            var provider = new PartitionProvider();
            provider.Payloads[seed.Account.HomeAccountId.Identifier] = seed.Payload;
            provider.Register(application.UserTokenCache);
            await application.AcquireTokenSilent(TestConstants.s_scope, seed.Account).ExecuteAsync().ConfigureAwait(false);
            Assert.HasCount(1, (await application.GetAccountsAsync().ConfigureAwait(false)).ToArray());

            await application.RemoveAsync(seed.Account).ConfigureAwait(false);
            Assert.IsEmpty(await application.GetAccountsAsync().ConfigureAwait(false));
        }

        [TestMethod]
        public async Task ExplicitEmptyUnkeyedHydrationSuppressesDiscoveryFallback()
        {
            using var http = new MockHttpManager();
            var application = CreateApplication(http);
            var seed = Seed("discovery-a", "alice@example.test");
            var provider = new PartitionProvider();
            provider.Payloads[seed.Account.HomeAccountId.Identifier] = seed.Payload;
            provider.Register(application.UserTokenCache);
            await application.AcquireTokenSilent(TestConstants.s_scope, seed.Account).ExecuteAsync().ConfigureAwait(false);
            provider.EmptyUnkeyed = true;

            Assert.IsEmpty(await application.GetAccountsAsync().ConfigureAwait(false),
                "An explicit empty restore must not resurrect previously discovered accounts.");
        }

        [TestMethod]
        public async Task UnkeyedDiscoverySessionPinsItsStateAcrossLaterPublication()
        {
            using var http = new MockHttpManager();
            var application = CreateApplication(http);
            var seed = Seed("discovery-a", "before@example.test");
            var changed = Seed("discovery-a", "after@example.test");
            var provider = new PartitionProvider();
            provider.Payloads[seed.Account.HomeAccountId.Identifier] = seed.Payload;
            provider.Register(application.UserTokenCache);
            await application.AcquireTokenSilent(TestConstants.s_scope, seed.Account).ExecuteAsync().ConfigureAwait(false);
            var cache = (ITokenCacheInternal)application.UserTokenCache;
            var session = CreateUnkeyedSession(cache);
            Assert.AreEqual("before@example.test", (await session.GetAccountsAsync().ConfigureAwait(false)).Single().Username);

            provider.Payloads[seed.Account.HomeAccountId.Identifier] = changed.Payload;
            await application.AcquireTokenSilent(TestConstants.s_scope, seed.Account).ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual("before@example.test", (await session.GetAccountsAsync().ConfigureAwait(false)).Single().Username);
            Assert.AreEqual("after@example.test", (await application.GetAccountsAsync().ConfigureAwait(false)).Single().Username);
        }

        [TestMethod]
        public async Task DiscoveryFiltersForeignClientsAndEnvironments()
        {
            using var http = new MockHttpManager();
            var application = CreateApplication(http);
            var seed = Seed("discovery-a", "alice@example.test", foreignEntries: true);
            var provider = new PartitionProvider();
            provider.Payloads[seed.Account.HomeAccountId.Identifier] = seed.Payload;
            provider.Register(application.UserTokenCache);
            await application.AcquireTokenSilent(TestConstants.s_scope, seed.Account).ExecuteAsync().ConfigureAwait(false);

            var accounts = (await application.GetAccountsAsync().ConfigureAwait(false)).ToArray();
            Assert.HasCount(1, accounts);
            Assert.AreEqual(seed.Account.HomeAccountId.Identifier, accounts[0].HomeAccountId.Identifier);
        }

        [TestMethod]
        public async Task AccountRecordsWithoutRefreshTokenEligibilityAreNotDiscoverable()
        {
            using var http = new MockHttpManager();
            var application = CreateApplication(http);
            var seed = Seed("discovery-a", "alice@example.test", withoutRefreshToken: true);
            var provider = new PartitionProvider();
            provider.Payloads[seed.Account.HomeAccountId.Identifier] = seed.Payload;
            provider.Register(application.UserTokenCache);
            var result = await application.AcquireTokenSilent(TestConstants.s_scope, seed.Account).ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);

            Assert.IsEmpty(await application.GetAccountsAsync().ConfigureAwait(false));
        }

        [TestMethod]
        public async Task FreshUnkeyedProviderDataTakesPrecedenceOverPriorDiscovery()
        {
            using var http = new MockHttpManager();
            var application = CreateApplication(http);
            var first = Seed("discovery-a", "alice@example.test");
            var second = Seed("discovery-b", "bob@example.test");
            var provider = new PartitionProvider();
            provider.Payloads[first.Account.HomeAccountId.Identifier] = first.Payload;
            provider.Register(application.UserTokenCache);
            await application.AcquireTokenSilent(TestConstants.s_scope, first.Account).ExecuteAsync().ConfigureAwait(false);
            provider.Payloads.Clear();
            provider.Payloads[second.Account.HomeAccountId.Identifier] = second.Payload;
            provider.HydrateUnkeyed = true;

            var accounts = (await application.GetAccountsAsync().ConfigureAwait(false)).ToArray();
            Assert.HasCount(1, accounts);
            Assert.AreEqual(second.Account.HomeAccountId.Identifier, accounts[0].HomeAccountId.Identifier);
        }

        private static ConfidentialClientApplication CreateApplication(MockHttpManager http)
        {
            return ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithAuthority("https://login.microsoftonline.com/" + TestConstants.Utid)
                .WithClientSecret("synthetic-not-a-credential")
                .WithInstanceDiscovery(false)
                .WithLegacyCacheCompatibility(false)
                .WithHttpManager(http)
                .BuildConcrete();
        }

        private static (byte[] Payload, IAccount Account) Seed(string uid, string username,
            bool profiles = false, bool foreignEntries = false, bool withoutRefreshToken = false)
        {
            using var http = new MockHttpManager();
            var application = CreateApplication(http);
            var cache = (ITokenCacheInternal)application.UserTokenCache;
            TokenCacheHelper.PopulateCache(cache.Accessor, uid: uid, displayableId: username,
                environment: "login.microsoftonline.com", addSecondAt: false);
            var entry = cache.Accessor.GetAllAccounts().Single();
            var account = new Account(entry.HomeAccountId, entry.PreferredUsername, entry.Environment);
            if (profiles)
            {
                const string guest = "33333333-3333-3333-3333-333333333333";
                var response = TestConstants.CreateMsalTokenResponse(guest);
                response.IdToken = TestConstants.CreateIdToken(uid, username, guest);
                cache.Accessor.SaveIdToken(new MsalIdTokenCacheItem("login.microsoftonline.com",
                    TestConstants.ClientId, response, guest, account.HomeAccountId.Identifier));
            }
            if (foreignEntries)
            {
                TokenCacheHelper.PopulateCache(cache.Accessor, uid: "foreign-client",
                    clientId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", environment: "login.microsoftonline.com",
                    displayableId: "foreign@example.test", addSecondAt: false);
                TokenCacheHelper.PopulateCache(cache.Accessor, uid: "foreign-environment",
                    environment: "login.microsoftonline.us", displayableId: "foreign@example.test", addSecondAt: false);
            }
            if (withoutRefreshToken)
            {
                foreach (var token in cache.Accessor.GetAllRefreshTokens())
                    cache.Accessor.DeleteRefreshToken(token);
            }
            return (cache.SerializeMsalV3(), account);
        }

        private static CacheSessionManager CreateUnkeyedSession(ITokenCacheInternal cache)
        {
            var service = ((TokenCache)cache).ServiceBundle;
            var context = new RequestContext(service, Guid.NewGuid(), null, CancellationToken.None)
            {
                ApiEvent = new ApiEvent(Guid.NewGuid())
            };
            var request = TestCommon.CreateAuthenticationRequestParameters(service, requestContext: context,
                apiID: ApiEvent.ApiIds.GetAccounts);
            request.Account = null;
            return new CacheSessionManager(cache, request);
        }

        private sealed class PartitionProvider
        {
            internal readonly ConcurrentDictionary<string, byte[]> Payloads = new ConcurrentDictionary<string, byte[]>();
            internal bool Clear = true;
            internal bool FailReadPublication;
            internal bool EmptyUnkeyed;
            internal bool HydrateUnkeyed;

            internal void Register(ITokenCache cache, bool synchronous = false)
            {
                if (synchronous)
                {
                    cache.SetBeforeAccess(Before);
                    cache.SetAfterAccess(After);
                }
                else
                {
                    cache.SetBeforeAccessAsync(args => { Before(args); return Task.CompletedTask; });
                    cache.SetAfterAccessAsync(args => { After(args); return Task.CompletedTask; });
                }
            }

            private void Before(TokenCacheNotificationArgs args)
            {
                if (string.IsNullOrEmpty(args.SuggestedCacheKey))
                {
                    if (EmptyUnkeyed || HydrateUnkeyed)
                        args.TokenCache.DeserializeMsalV3(null, true);
                    if (HydrateUnkeyed)
                    {
                        foreach (var payload in Payloads.OrderBy(pair => pair.Key).Select(pair => pair.Value))
                            args.TokenCache.DeserializeMsalV3(payload, false);
                    }
                    return;
                }
                Payloads.TryGetValue(args.SuggestedCacheKey, out byte[] bytes);
                args.TokenCache.DeserializeMsalV3(bytes, Clear);
            }

            private void After(TokenCacheNotificationArgs args)
            {
                if (args.HasStateChanged)
                    Payloads[args.SuggestedCacheKey] = args.TokenCache.SerializeMsalV3();
                else if (FailReadPublication && !string.IsNullOrEmpty(args.SuggestedCacheKey))
                    throw new InvalidOperationException("synthetic-publication");
            }
        }
    }
}
