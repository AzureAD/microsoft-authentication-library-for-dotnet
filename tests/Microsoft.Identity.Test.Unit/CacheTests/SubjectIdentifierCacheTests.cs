// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Android.Content;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Platforms.Android;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Common.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using MsalError = Microsoft.Identity.Client.MsalError;

namespace Microsoft.Identity.Test.Unit.CacheTests
{
    [TestClass]
    public class SubjectIdentifierCacheTests : TestBase
    {
        private const string Authority = "https://localhost/adfs";
        private static readonly string[] s_scopes = { "api.read" };

        [TestMethod]
        [DataRow(false, false, false)]
        [DataRow(true, false, false)]
        [DataRow(false, true, false)]
        [DataRow(true, true, false)]
        [DataRow(false, false, true)]
        [DataRow(true, false, true)]
        public async Task AndroidMixedIdentities_RejectAmbiguousAccountsAsync(
            bool subjectFirst, bool identicalIdentifier, bool genericAuthority)
        {
            // Arrange
            using var http = new MockHttpManager();
            var app = CreateApplication(http, genericAuthority);
            var cache = (TokenCache)app.UserTokenCacheInternal;
            var stores = new Dictionary<string, Dictionary<string, object>>(StringComparer.Ordinal);
            cache.Accessor = CreateAccessor(stores);
            string clientInfo = Base64UrlHelpers.Encode("{\"uid\":\"member\",\"utid\":\"tenant-id\"}");
            string subjectId = identicalIdentifier ? "member.tenant-id" : "Member.tenant-id";

            // Act
            var first = subjectFirst
                ? await SignInAsync(app, http, subjectId).ConfigureAwait(false)
                : await SignInAsync(app, http, "regular", clientInfo).ConfigureAwait(false);
            Assert.AreEqual(4, stores.Values.Sum(store => store.Count));
            var second = subjectFirst
                ? await SignInAsync(app, http, "regular", clientInfo).ConfigureAwait(false)
                : await SignInAsync(app, http, subjectId).ConfigureAwait(false);

            // Assert
            // Neither sign-in may overwrite or evict the other identity's credentials.
            Assert.IsTrue(stores.Values.All(store => store.Count == 1));
            var unrelated = await SignInAsync(app, http, "unrelated").ConfigureAwait(false);
            cache.Accessor = CreateAccessor(stores);
            AssertStoredSubjects(cache.Accessor, "unrelated");
            Assert.HasCount(1, await app.GetAccountsAsync().ConfigureAwait(false));
            Assert.IsNull(await app.GetAccountAsync(subjectId).ConfigureAwait(false));

            foreach (var account in new[] { first.Account, second.Account })
            {
                foreach (bool forceRefresh in new[] { false, true })
                {
                    // No HTTP handler is queued: an ambiguous credential must never reach the endpoint.
                    var error = await Assert.ThrowsExactlyAsync<MsalUiRequiredException>(() =>
                        app.AcquireTokenSilent(s_scopes, account).WithForceRefresh(forceRefresh).ExecuteAsync())
                        .ConfigureAwait(false);
                    Assert.AreEqual(MsalError.NoTokensFoundError, error.ErrorCode);
                }

                await app.RemoveAsync(account).ConfigureAwait(false);
                AssertStoredSubjects(cache.Accessor, "unrelated");
                Assert.AreEqual(12, stores.Values.Sum(store => store.Count));
            }

            foreach (var store in stores.Where(pair => pair.Key.StartsWith(
                "com.microsoft.identity.client.accessToken", StringComparison.Ordinal)))
            {
                foreach (string json in store.Value.Values)
                {
                    var accessToken = MsalAccessTokenCacheItem.FromJsonString(json);
                    if (accessToken.HomeAccountId != "unrelated")
                    {
                        Assert.IsNull(cache.Accessor.GetIdToken(accessToken));
                    }
                }
            }

            await AssertCachedTokenAsync(app, unrelated.Account, "unrelated").ConfigureAwait(false);
            cache.Accessor.Clear();
            var fresh = await SignInAsync(app, http, subjectId).ConfigureAwait(false);
            await AssertCachedTokenAsync(app, fresh.Account, subjectId).ConfigureAwait(false);
        }

        [TestMethod]
        [DataRow("Account", false)]
        [DataRow("Account", true)]
        [DataRow("accessToken", false)]
        [DataRow("refreshToken", false)]
        [DataRow("idToken", false)]
        public async Task AndroidLegacyMixedIdentities_RequireCacheClearAsync(string legacyType, bool hasVersionedSubject)
        {
            // Arrange
            using var http = new MockHttpManager();
            var app = CreateApplication(http, false);
            var cache = (TokenCache)app.UserTokenCacheInternal;
            var stores = new Dictionary<string, Dictionary<string, object>>(StringComparer.Ordinal);
            cache.Accessor = CreateAccessor(stores);
            var subject = await SignInAsync(app, http, "Member.tenant-id").ConfigureAwait(false);
            MsalCacheItemBase legacyItem = legacyType switch
            {
                "accessToken" => cache.Accessor.GetAllAccessTokens().Single(),
                "refreshToken" => cache.Accessor.GetAllRefreshTokens().Single(),
                "idToken" => cache.Accessor.GetAllIdTokens().Single(),
                "Account" => cache.Accessor.GetAllAccounts().Single(),
                _ => throw new ArgumentOutOfRangeException(nameof(legacyType))
            };
            cache.Accessor.Clear();
            string clientInfo = Base64UrlHelpers.Encode("{\"uid\":\"member\",\"utid\":\"tenant-id\"}");
            var regular = await SignInAsync(app, http, "regular", clientInfo).ConfigureAwait(false);
            var regularAccessToken = cache.Accessor.GetAllAccessTokens().Single();
            var regularAccount = cache.Accessor.GetAllAccounts().Single();
            AuthenticationResult unrelated = hasVersionedSubject
                ? await SignInAsync(app, http, "unrelated").ConfigureAwait(false)
                : null;

            // Model a pre-upgrade cache with only one surviving subject record.
            SeedLegacyStore(stores, legacyType, legacyItem);
            var originalEntries = stores.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
            cache.Accessor = CreateAccessor(stores);

            // Act
            foreach (var account in new[] { subject.Account, regular.Account })
            {
                foreach (bool forceRefresh in new[] { false, true })
                {
                    // No HTTP handler is queued: neither identity's credential may be redeemed.
                    var error = await Assert.ThrowsExactlyAsync<MsalUiRequiredException>(() =>
                        app.AcquireTokenSilent(s_scopes, account).WithForceRefresh(forceRefresh).ExecuteAsync())
                        .ConfigureAwait(false);
                    Assert.AreEqual(MsalError.NoTokensFoundError, error.ErrorCode);
                }

                await app.RemoveAsync(account).ConfigureAwait(false);
                foreach (var store in stores)
                {
                    CollectionAssert.AreEquivalent(originalEntries[store.Key], store.Value.ToArray());
                }
            }

            // Assert
            Assert.IsNull(cache.Accessor.GetIdToken(regularAccessToken));
            Assert.IsNull(cache.Accessor.GetAccount(regularAccount));
            Assert.IsNull(await app.GetAccountAsync(subject.Account.HomeAccountId.Identifier).ConfigureAwait(false));
            Assert.IsNull(await app.GetAccountAsync(regular.Account.HomeAccountId.Identifier).ConfigureAwait(false));
            Assert.HasCount(hasVersionedSubject ? 1 : 0, await app.GetAccountsAsync().ConfigureAwait(false));
            AssertStoredSubjects(cache.Accessor, hasVersionedSubject ? new[] { "unrelated" } : Array.Empty<string>());
            if (unrelated is not null)
            {
                await AssertCachedTokenAsync(app, unrelated.Account, "unrelated").ConfigureAwait(false);
            }

            cache.Accessor.Clear();
            var fresh = await SignInAsync(app, http, "Member.tenant-id").ConfigureAwait(false);
            await AssertCachedTokenAsync(app, fresh.Account, "Member.tenant-id").ConfigureAwait(false);
        }

        [TestMethod]
        [DataRow("accessToken", "Member.tenant-id")]
        [DataRow("refreshToken", "Member.tenant-id")]
        [DataRow("idToken", "Member.tenant-id")]
        [DataRow("Account", "member.tenant-id")]
        public async Task AndroidLegacySubject_RejectsConflictingWriteAsync(string legacyType, string subjectId)
        {
            // Arrange
            using var http = new MockHttpManager();
            var app = CreateApplication(http, false);
            var cache = (TokenCache)app.UserTokenCacheInternal;
            var stores = new Dictionary<string, Dictionary<string, object>>(StringComparer.Ordinal);
            cache.Accessor = CreateAccessor(stores);
            var subject = await SignInAsync(app, http, subjectId).ConfigureAwait(false);
            MsalCacheItemBase remnant = legacyType switch
            {
                "accessToken" => cache.Accessor.GetAllAccessTokens().Single(),
                "refreshToken" => cache.Accessor.GetAllRefreshTokens().Single(),
                "idToken" => cache.Accessor.GetAllIdTokens().Single(),
                "Account" => cache.Accessor.GetAllAccounts().Single(),
                _ => throw new ArgumentOutOfRangeException(nameof(legacyType))
            };
            cache.Accessor.Clear();
            SeedLegacyStore(stores, legacyType, remnant);
            var originalEntry = stores["com.microsoft.identity.client." + legacyType].Single();

            // Act
            var error = await Assert.ThrowsExactlyAsync<MsalClientException>(() =>
                SignInAsync(app, http, "regular", MockHelpers.CreateClientInfo("member", "tenant-id")))
                .ConfigureAwait(false);

            // Assert
            Assert.AreEqual(MsalError.UserMismatch, error.ErrorCode);
            Assert.AreEqual(originalEntry.Value, stores["com.microsoft.identity.client." + legacyType][originalEntry.Key]);
            cache.Accessor = CreateAccessor(stores);
            foreach (bool forceRefresh in new[] { false, true })
            {
                await Assert.ThrowsExactlyAsync<MsalUiRequiredException>(() =>
                    app.AcquireTokenSilent(s_scopes, subject.Account).WithForceRefresh(forceRefresh).ExecuteAsync())
                    .ConfigureAwait(false);
            }

            await app.RemoveAsync(subject.Account).ConfigureAwait(false);
            Assert.AreEqual(originalEntry.Value, stores["com.microsoft.identity.client." + legacyType][originalEntry.Key]);
            cache.Accessor.Clear();
            var fresh = await SignInAsync(app, http, subjectId).ConfigureAwait(false);
            await AssertCachedTokenAsync(app, fresh.Account, subjectId).ConfigureAwait(false);
        }

        [TestMethod]
        [DataRow("login.microsoftonline.com", "login.windows.net", true)]
        [DataRow("login.partner.microsoftonline.cn", "login.chinacloudapi.cn", true)]
        [DataRow("authority.example.test", "alias.example.test", true)]
        [DataRow("login.microsoftonline.com", "login.microsoftonline.us", false)]
        public async Task AndroidLegacyMixedIdentities_RespectAliasesAsync(string legacyHost, string cacheHost, bool aliasesMatch)
        {
            // Arrange
            using var http = new MockHttpManager();
            var metadata = new JsonObject
            {
                ["metadata"] = new JsonArray(new JsonObject
                {
                    ["preferred_network"] = cacheHost,
                    ["preferred_cache"] = cacheHost,
                    ["aliases"] = aliasesMatch ? new JsonArray(legacyHost, cacheHost) : new JsonArray(cacheHost)
                })
            };
            var app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithAuthority("https://" + cacheHost + "/tenant-id", false)
                .WithInstanceDiscoveryMetadata(metadata.ToJsonString())
                .WithLegacyCacheCompatibility(false)
                .WithRedirectUri(TestConstants.RedirectUri)
                .WithHttpManager(http)
                .BuildConcrete();
            app.ServiceBundle.ConfigureMockWebUI();
            var cache = (TokenCache)app.UserTokenCacheInternal;
            var stores = new Dictionary<string, Dictionary<string, object>>(StringComparer.Ordinal);
            cache.Accessor = CreateAccessor(stores);
            var subject = await SignInAsync(app, http, "Member.tenant-id").ConfigureAwait(false);
            var remnant = new MsalRefreshTokenCacheItem(legacyHost, TestConstants.ClientId,
                "legacy-subject-token", null, null, subject.Account.HomeAccountId.Identifier);
            cache.Accessor.Clear();
            SeedLegacyStore(stores, "refreshToken", remnant);

            // Act
            var regular = await SignInAsync(app, http, "regular", MockHelpers.CreateClientInfo("member", "tenant-id"))
                .ConfigureAwait(false);
            var originalEntries = stores.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
            cache.Accessor = CreateAccessor(stores);

            // Assert
            if (!aliasesMatch)
            {
                var result = await app.AcquireTokenSilent(s_scopes, regular.Account).ExecuteAsync().ConfigureAwait(false);
                Assert.AreEqual(regular.AccessToken, result.AccessToken);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(regular.Account.HomeAccountId.Identifier, result.Account.HomeAccountId.Identifier);
                Assert.HasCount(1, await app.GetAccountsAsync().ConfigureAwait(false));
                return;
            }

            foreach (var account in new[] { subject.Account, regular.Account })
            {
                foreach (bool forceRefresh in new[] { false, true })
                {
                    await Assert.ThrowsExactlyAsync<MsalUiRequiredException>(() =>
                        app.AcquireTokenSilent(s_scopes, account).WithForceRefresh(forceRefresh).ExecuteAsync())
                        .ConfigureAwait(false);
                }

                await app.RemoveAsync(account).ConfigureAwait(false);
                foreach (var store in stores)
                {
                    CollectionAssert.AreEquivalent(originalEntries[store.Key], store.Value.ToArray());
                }
            }

            Assert.IsEmpty(await app.GetAccountsAsync().ConfigureAwait(false));
        }

        [TestMethod]
        [DataRow("member", "Member", false, true)]
        [DataRow("Member", "member", false, false)]
        [DataRow("member.tenant-id", "Member.tenant-id", true, true)]
        [DataRow("member", "Member", true, false)]
        [DataRow("member", "other", false, true)]
        [DataRow("6:member", "6:Member", false, false)]
        public async Task AndroidSubjects_RemainIsolatedAsync(
            string firstSubject, string secondSubject, bool genericAuthority, bool refreshIncludesIdToken)
        {
            // Arrange
            using var http = new MockHttpManager();
            var app = CreateApplication(http, genericAuthority);
            var cache = (TokenCache)app.UserTokenCacheInternal;
            var stores = new Dictionary<string, Dictionary<string, object>>(StringComparer.Ordinal);
            cache.Accessor = CreateAccessor(stores);

            // Act
            var first = await SignInAsync(app, http, firstSubject).ConfigureAwait(false);
            AssertStoredSubjects(cache.Accessor, firstSubject);
            var second = await SignInAsync(app, http, secondSubject).ConfigureAwait(false);
            AssertStoredSubjects(cache.Accessor, firstSubject, secondSubject);

            // Assert
            Assert.HasCount(2, await app.GetAccountsAsync().ConfigureAwait(false));
            Assert.AreEqual(firstSubject, (await app.GetAccountAsync(firstSubject).ConfigureAwait(false)).HomeAccountId.Identifier);
            Assert.AreEqual(secondSubject, (await app.GetAccountAsync(secondSubject).ConfigureAwait(false)).HomeAccountId.Identifier);
            await AssertCachedTokenAsync(app, first.Account, firstSubject).ConfigureAwait(false);
            await AssertCachedTokenAsync(app, second.Account, secondSubject).ConfigureAwait(false);
            Assert.IsTrue(stores.Where(pair => !pair.Key.EndsWith(".subject.v1", StringComparison.Ordinal))
                .All(pair => pair.Value.Count == 0));
            Assert.IsTrue(stores.Where(pair => pair.Key.EndsWith(".subject.v1", StringComparison.Ordinal))
                .All(pair => pair.Value.Count == 2));

            // Reopen the same persisted stores, without serializing through the unsupported Android V3 API.
            cache.Accessor = CreateAccessor(stores);
            await AssertCachedTokenAsync(app, first.Account, firstSubject).ConfigureAwait(false);
            http.AddMockHandler(new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Post,
                ExpectedPostData = new Dictionary<string, string> { ["refresh_token"] = "rt-" + firstSubject },
                ResponseMessage = CreateTokenResponse(firstSubject, refreshIncludesIdToken, refreshed: true)
            });
            var refreshed = await app.AcquireTokenSilent(s_scopes, first.Account)
                .WithForceRefresh(true).ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual("at-refreshed-" + firstSubject, refreshed.AccessToken);
            Assert.AreEqual(firstSubject, refreshed.Account.HomeAccountId.Identifier);
            AssertStoredSubjects(cache.Accessor, firstSubject, secondSubject);
            await AssertCachedTokenAsync(app, second.Account, secondSubject).ConfigureAwait(false);

            await app.RemoveAsync(first.Account).ConfigureAwait(false);
            AssertStoredSubjects(cache.Accessor, secondSubject);
            Assert.IsNull(await app.GetAccountAsync(firstSubject).ConfigureAwait(false));
            await Assert.ThrowsExactlyAsync<MsalUiRequiredException>(
                () => app.AcquireTokenSilent(s_scopes, first.Account).ExecuteAsync()).ConfigureAwait(false);
            await AssertCachedTokenAsync(app, second.Account, secondSubject).ConfigureAwait(false);
            cache.Accessor.Clear();
            Assert.IsTrue(stores.All(pair => pair.Value.Count == 0));
        }

        [TestMethod]
        [DataRow("member", false)]
        [DataRow("Member", true)]
        public async Task AndroidLegacySubjects_RequireSignInAsync(string requestedSubject, bool forceRefresh)
        {
            // Arrange
            using var http = new MockHttpManager();
            var app = CreateApplication(http, false);
            var cache = (TokenCache)app.UserTokenCacheInternal;
            var stores = new Dictionary<string, Dictionary<string, object>>(StringComparer.Ordinal);
            cache.Accessor = CreateAccessor(stores);
            var oldAccount = await SignInAsync(app, http, "Member").ConfigureAwait(false);
            SeedLegacyStore(stores, "accessToken", cache.Accessor.GetAllAccessTokens().Single());
            SeedLegacyStore(stores, "refreshToken", cache.Accessor.GetAllRefreshTokens().Single());
            SeedLegacyStore(stores, "idToken", cache.Accessor.GetAllIdTokens().Single());
            SeedLegacyStore(stores, "Account", cache.Accessor.GetAllAccounts().Single());
            foreach (var store in stores.Where(pair => pair.Key.EndsWith(".subject.v1", StringComparison.Ordinal)))
            {
                store.Value.Clear();
            }

            cache.Accessor = CreateAccessor(stores);
            var account = new Account(requestedSubject, oldAccount.Account.Username, oldAccount.Account.Environment);

            // Act
            var exception = await Assert.ThrowsExactlyAsync<MsalUiRequiredException>(
                () => app.AcquireTokenSilent(s_scopes, account).WithForceRefresh(forceRefresh).ExecuteAsync()).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(MsalError.NoTokensFoundError, exception.ErrorCode);
            Assert.IsEmpty(await app.GetAccountsAsync().ConfigureAwait(false));
            AssertStoredSubjects(cache.Accessor);
            // No token endpoint handler was queued: neither legacy credential may be used.
            var fresh = await SignInAsync(app, http, requestedSubject).ConfigureAwait(false);
            Assert.HasCount(1, await app.GetAccountsAsync().ConfigureAwait(false));
            await AssertCachedTokenAsync(app, fresh.Account, requestedSubject).ConfigureAwait(false);
            cache.Accessor.Clear();
            Assert.IsTrue(stores.All(pair => pair.Value.Count == 0));
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(true, false)]
        [DataRow(false, true)]
        public async Task AndroidClientInfo_PreservesLegacyKeysAndRefreshAsync(bool emptyIdToken, bool includeIdToken)
        {
            // Arrange
            using var http = new MockHttpManager();
            var app = CreateApplication(http, false);
            var cache = (TokenCache)app.UserTokenCacheInternal;
            var stores = new Dictionary<string, Dictionary<string, object>>(StringComparer.Ordinal);
            cache.Accessor = CreateAccessor(stores);
            string clientInfo = MockHelpers.CreateClientInfo();
            string subject = includeIdToken ? ClientInfo.CreateFromJson(clientInfo).ToAccountIdentifier() : "member";
            var signedIn = await SignInAsync(app, http, subject, clientInfo).ConfigureAwait(false);
            var originalKeys = stores.ToDictionary(pair => pair.Key, pair => pair.Value.Keys.ToArray());
            Assert.IsTrue(stores.Where(pair => pair.Key.EndsWith(".subject.v1", StringComparison.Ordinal))
                .All(pair => pair.Value.Count == 0));
            Assert.IsTrue(stores.Where(pair => !pair.Key.EndsWith(".subject.v1", StringComparison.Ordinal))
                .All(pair => pair.Value.Count == 1));
            cache.Accessor = CreateAccessor(stores);
            var account = new Account(signedIn.Account.HomeAccountId.Identifier.ToUpperInvariant(),
                signedIn.Account.Username, signedIn.Account.Environment);

            // Act
            var cached = await app.AcquireTokenSilent(s_scopes, account).ExecuteAsync().ConfigureAwait(false);
            http.AddMockHandler(new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Post,
                ExpectedPostData = new Dictionary<string, string> { ["refresh_token"] = "rt-" + subject },
                ResponseMessage = CreateTokenResponse(subject, includeIdToken, refreshed: true, emptyIdToken: emptyIdToken)
            });
            var refreshed = await app.AcquireTokenSilent(s_scopes, account)
                .WithForceRefresh(true).ExecuteAsync().ConfigureAwait(false);

            // Assert
            Assert.AreEqual("at-" + subject, cached.AccessToken);
            Assert.AreEqual(subject, cached.ClaimsPrincipal.FindFirst("sub").Value);
            Assert.AreEqual(subject + "@example.test", cached.Account.Username);
            Assert.AreEqual("at-refreshed-" + subject, refreshed.AccessToken);
            Assert.AreEqual(clientInfo, cache.Accessor.GetAllAccessTokens().Single().RawClientInfo);
            Assert.AreEqual(clientInfo, cache.Accessor.GetAllRefreshTokens().Single().RawClientInfo);
            foreach (var store in stores)
            {
                CollectionAssert.AreEquivalent(originalKeys[store.Key], store.Value.Keys.ToArray());
            }

            var next = await app.AcquireTokenSilent(s_scopes, signedIn.Account).ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual(refreshed.AccessToken, next.AccessToken);
            Assert.AreEqual(TokenSource.Cache, next.AuthenticationResultMetadata.TokenSource);
            Assert.HasCount(1, await app.GetAccountsAsync().ConfigureAwait(false));
            await app.RemoveAsync(signedIn.Account).ConfigureAwait(false);
            Assert.IsTrue(stores.Values.All(store => store.Count == 0));
        }

        [TestMethod]
        [DataRow(false, false, false, false, false)]
        [DataRow(true, false, false, false, false)]
        [DataRow(false, true, false, false, false)]
        [DataRow(false, false, false, true, false)]
        [DataRow(true, false, true, false, false)]
        [DataRow(false, true, true, true, false)]
        [DataRow(false, true, true, false, true)]
        public async Task AndroidMixedIdentities_ExcludeAdalFallbackAsync(
            bool adalHasClientInfo, bool orphaned, bool adalOnly, bool idOnly, bool refreshTokenOnly)
        {
            // Arrange
            using var http = new MockHttpManager();
            var legacyCache = new InMemoryLegacyCachePersistence();
            var app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithAuthority(TestConstants.AuthorityHomeTenant, false)
                .WithInstanceDiscovery(false)
                .WithRedirectUri(TestConstants.RedirectUri)
                .WithHttpManager(http)
                .WithUserTokenLegacyCachePersistenceForTest(legacyCache)
                .BuildConcrete();
            app.ServiceBundle.ConfigureMockWebUI();
            // Enable desktop legacy persistence while exercising the source-linked Android accessor.
            app.UserTokenCacheInternal.SetBeforeAccess(_ => { });
            var cache = (TokenCache)app.UserTokenCacheInternal;
            var stores = new Dictionary<string, Dictionary<string, object>>(StringComparer.Ordinal);
            cache.Accessor = CreateAccessor(stores);
            string clientInfo = MockHelpers.CreateClientInfo("member", "tenant-id");
            string unrelatedClientInfo = MockHelpers.CreateClientInfo("other", "tenant-id");
            var regular = await SignInAsync(app, http, "member", clientInfo).ConfigureAwait(false);
            var subject = await SignInAsync(app, http, "Member.tenant-id",
                username: regular.Account.Username).ConfigureAwait(false);
            var unrelated = await SignInAsync(app, http, "other", unrelatedClientInfo).ConfigureAwait(false);
            cache.RemoveAccountInternal(unrelated.Account, new RequestContext(app.ServiceBundle, Guid.NewGuid(), null));
            if (adalOnly)
            {
                var subjectRefreshToken = MsalRefreshTokenCacheItem.FromJsonString(
                    (string)stores["com.microsoft.identity.client.refreshToken.subject.v1"].Values.Single());
                var subjectIdToken = MsalIdTokenCacheItem.FromJsonString(
                    (string)stores["com.microsoft.identity.client.idToken.subject.v1"].Values.Single());
                cache.Accessor.Clear();
                SeedLegacyStore(stores, "refreshToken", subjectRefreshToken);
                SeedLegacyStore(stores, "idToken", subjectIdToken);
            }

            if (orphaned)
            {
                stores["com.microsoft.identity.client.Account"].Clear();
                stores["com.microsoft.identity.client.Account.subject.v1"].Clear();
            }

            if (refreshTokenOnly)
            {
                stores["com.microsoft.identity.client.idToken"].Clear();
                stores["com.microsoft.identity.client.idToken.subject.v1"].Clear();
            }

            cache.Accessor = CreateAccessor(stores);

            var adalEntries = AdalCacheOperations.Deserialize(app.ServiceBundle.ApplicationLogger, legacyCache.LoadCache());
            Assert.HasCount(2, adalEntries);
            if (!adalHasClientInfo)
            {
                adalEntries.Single(pair => pair.Value.RawClientInfo == clientInfo).Value.RawClientInfo = null;
                legacyCache.WriteCache(AdalCacheOperations.Serialize(app.ServiceBundle.ApplicationLogger, adalEntries));
            }

            byte[] originalAdalCache = legacyCache.LoadCache();
            Assert.AreEqual("rt-member", CacheFallbackOperations.GetRefreshToken(
                app.ServiceBundle.ApplicationLogger, legacyCache, new[] { regular.Account.Environment },
                TestConstants.ClientId, subject.Account).Secret);

            // Act
            var usernameOnlyAccount = new Account(null, regular.Account.Username, regular.Account.Environment);
            var accountsToTest = idOnly
                ? new[] { new Account("member.other-tenant", null, regular.Account.Environment) }
                : new[] { subject.Account, regular.Account, usernameOnlyAccount };
            foreach (var account in accountsToTest)
            {
                foreach (bool forceRefresh in new[] { false, true })
                {
                    // No HTTP handler is queued: ADAL must not redeem the excluded identity.
                    var error = await Assert.ThrowsExactlyAsync<MsalUiRequiredException>(() =>
                        app.AcquireTokenSilent(s_scopes, account).WithForceRefresh(forceRefresh).ExecuteAsync())
                        .ConfigureAwait(false);
                    Assert.AreEqual(MsalError.NoTokensFoundError, error.ErrorCode);
                }

                await app.RemoveAsync(account).ConfigureAwait(false);
                CollectionAssert.AreEqual(originalAdalCache, legacyCache.LoadCache());
            }

            // Assert
            var accounts = await app.GetAccountsAsync().ConfigureAwait(false);
            Assert.HasCount(1, accounts);
            Assert.AreEqual(unrelated.Account.HomeAccountId.Identifier, accounts.Single().HomeAccountId.Identifier);
            Assert.IsNull(await app.GetAccountAsync(subject.Account.HomeAccountId.Identifier).ConfigureAwait(false));
            http.AddMockHandler(new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Post,
                ExpectedPostData = new Dictionary<string, string> { ["refresh_token"] = "rt-other" },
                ResponseMessage = CreateTokenResponse("other", true, refreshed: true)
            });
            var refreshed = await app.AcquireTokenSilent(s_scopes, unrelated.Account)
                .WithForceRefresh(true).ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual("at-refreshed-other", refreshed.AccessToken);
            Assert.AreEqual(unrelated.Account.HomeAccountId.Identifier, refreshed.Account.HomeAccountId.Identifier);
            await app.RemoveAsync(unrelated.Account).ConfigureAwait(false);
            Assert.AreEqual("rt-member", AdalCacheOperations.Deserialize(
                app.ServiceBundle.ApplicationLogger, legacyCache.LoadCache()).Single().Value.RefreshToken);
        }

        [TestMethod]
        [DataRow(StorageJsonValues.AccountRootKey)]
        [DataRow(StorageJsonValues.CredentialTypeRefreshToken)]
        public async Task SharedV3Cache_RemainsUnchangedAsync(string clientInfoSection)
        {
            // Arrange
            using var http = new MockHttpManager();
            var app = CreateApplication(http, false);
            var account = (await SignInAsync(app, http, "member", MockHelpers.CreateClientInfo()).ConfigureAwait(false)).Account;
            var serializer = (ITokenCacheSerializer)app.UserTokenCache;
            var root = JsonNode.Parse(serializer.SerializeMsalV3()).AsObject();
            foreach (var section in root.Where(section => section.Key != clientInfoSection))
            {
                foreach (var entry in section.Value.AsObject())
                {
                    entry.Value.AsObject().Remove(StorageJsonKeys.ClientInfo);
                }
            }

            // Act
            serializer.DeserializeMsalV3(root.ToJsonString().ToByteArray(), true);
            var result = await app.AcquireTokenSilent(s_scopes, account).ExecuteAsync().ConfigureAwait(false);

            // Assert
            Assert.HasCount(1, await app.GetAccountsAsync().ConfigureAwait(false));
            Assert.AreEqual("at-member", result.AccessToken);
            Assert.AreEqual("member", result.ClaimsPrincipal.FindFirst("sub").Value);
            Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
            var accessToken = ((TokenCache)app.UserTokenCacheInternal).Accessor.GetAllAccessTokens().Single();
            Assert.IsTrue(string.IsNullOrEmpty(accessToken.RawClientInfo));
            Assert.IsFalse(accessToken.IsAndroidCacheItem);
        }

        private static void SeedLegacyStore(Dictionary<string, Dictionary<string, object>> stores,
            string type, MsalCacheItemBase item)
        {
            string key = type switch
            {
                "accessToken" => ((MsalAccessTokenCacheItem)item).CacheKey,
                "refreshToken" => ((MsalRefreshTokenCacheItem)item).CacheKey,
                "idToken" => ((MsalIdTokenCacheItem)item).CacheKey,
                "Account" => ((MsalAccountCacheItem)item).CacheKey,
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
            stores["com.microsoft.identity.client." + type][key] = item.ToJObject().ToString();
        }

        private sealed class TestAndroidAccessor : AndroidTokenCacheAccessor, ITokenCacheAccessor
        {
            private readonly Dictionary<string, MsalAppMetadataCacheItem> _metadata = new Dictionary<string, MsalAppMetadataCacheItem>();

            internal TestAndroidAccessor(Func<string, ISharedPreferences> getPreferences) : base(getPreferences, null) { }

            // Desktop token acquisition calls these APIs; Android token acquisition does not.
            public new void SaveAppMetadata(MsalAppMetadataCacheItem item) => _metadata[item.CacheKey] = item;
            public new List<MsalAppMetadataCacheItem> GetAllAppMetadata() => _metadata.Values.ToList();
            public new MsalAppMetadataCacheItem GetAppMetadata(MsalAppMetadataCacheItem item) =>
                _metadata.TryGetValue(item.CacheKey, out var metadata) ? metadata : null;
            public new bool HasAccessOrRefreshTokens() => GetAllAccessTokens().Count + GetAllRefreshTokens().Count > 0;
        }

        private static ITokenCacheAccessor CreateAccessor(Dictionary<string, Dictionary<string, object>> stores)
        {
            return new TestAndroidAccessor(name =>
            {
                if (!stores.TryGetValue(name, out var values))
                {
                    values = new Dictionary<string, object>(StringComparer.Ordinal);
                    stores.Add(name, values);
                }

                var preferences = Substitute.For<ISharedPreferences>();
                var editor = Substitute.For<ISharedPreferencesEditor>();
                preferences.All.Returns(_ => values);
                preferences.GetString(Arg.Any<string>(), Arg.Any<string>()).Returns(call =>
                    values.TryGetValue(call.ArgAt<string>(0), out var value) ? (string)value : call.ArgAt<string>(1));
                preferences.Edit().Returns(editor);
                editor.PutString(Arg.Any<string>(), Arg.Any<string>()).Returns(call =>
                {
                    values[call.ArgAt<string>(0)] = call.ArgAt<string>(1);
                    return editor;
                });
                editor.Remove(Arg.Any<string>()).Returns(call => { values.Remove(call.Arg<string>()); return editor; });
                editor.Clear().Returns(_ => { values.Clear(); return editor; });
                return preferences;
            });
        }

        private static PublicClientApplication CreateApplication(MockHttpManager http, bool genericAuthority)
        {
            var builder = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithHttpManager(http).WithRedirectUri(TestConstants.RedirectUri).WithInstanceDiscovery(false);
            if (genericAuthority)
            {
                builder.WithExperimentalFeatures().WithOidcAuthority(Authority);
                http.AddMockHandler(new MockHttpMessageHandler
                {
                    ExpectedMethod = HttpMethod.Get,
                    ResponseMessage = MockHelpers.CreateSuccessResponseMessage(TestConstants.GetOidcResponse(Authority))
                });
            }
            else
            {
                builder.WithAdfsAuthority(Authority, false);
            }

            var app = builder.BuildConcrete();
            app.ServiceBundle.ConfigureMockWebUI();
            return app;
        }

        private static async Task<AuthenticationResult> SignInAsync(
            PublicClientApplication app, MockHttpManager http, string subject, string clientInfo = null, string username = null)
        {
            http.AddMockHandler(new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Post,
                ResponseMessage = CreateTokenResponse(subject, true, clientInfo: clientInfo, username: username)
            });
            return await app.AcquireTokenInteractive(s_scopes).ExecuteAsync().ConfigureAwait(false);
        }

        private static HttpResponseMessage CreateTokenResponse(
            string subject, bool includeIdToken, bool refreshed = false, string clientInfo = null, bool emptyIdToken = false,
            string username = null)
        {
            var response = new JsonObject
            {
                ["access_token"] = (refreshed ? "at-refreshed-" : "at-") + subject,
                ["refresh_token"] = "rt-" + subject,
                ["token_type"] = "Bearer",
                ["expires_in"] = 3600,
                ["scope"] = string.Join(" ", s_scopes)
            };
            if (clientInfo is not null)
            {
                response["client_info"] = clientInfo;
            }

            if (includeIdToken)
            {
                var claims = new JsonObject
                {
                    ["sub"] = subject,
                    ["preferred_username"] = username ?? subject + "@example.test",
                    ["iss"] = Authority,
                    ["aud"] = TestConstants.ClientId
                };
                response["id_token"] = Base64UrlHelpers.Encode("{\"alg\":\"none\"}") + "." +
                    Base64UrlHelpers.Encode(claims.ToJsonString()) + ".";
            }
            else if (emptyIdToken)
            {
                response["id_token"] = string.Empty;
            }

            return MockHelpers.CreateSuccessResponseMessage(response.ToJsonString());
        }

        private static async Task AssertCachedTokenAsync(PublicClientApplication app, IAccount account, string subject)
        {
            var result = await app.AcquireTokenSilent(s_scopes, account).ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual("at-" + subject, result.AccessToken);
            Assert.AreEqual(subject, result.Account.HomeAccountId.Identifier);
            Assert.AreEqual(subject, result.ClaimsPrincipal.FindFirst("sub").Value);
            Assert.AreEqual(subject + "@example.test", result.Account.Username);
            Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
        }

        private static void AssertStoredSubjects(ITokenCacheAccessor accessor, params string[] subjects)
        {
            CollectionAssert.AreEquivalent(subjects, accessor.GetAllAccessTokens().Select(t => t.HomeAccountId).ToArray());
            CollectionAssert.AreEquivalent(subjects, accessor.GetAllRefreshTokens().Select(t => t.HomeAccountId).ToArray());
            CollectionAssert.AreEquivalent(subjects, accessor.GetAllIdTokens().Select(t => t.HomeAccountId).ToArray());
            CollectionAssert.AreEquivalent(subjects, accessor.GetAllAccounts().Select(t => t.HomeAccountId).ToArray());
        }
    }
}
