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
        [DataRow(false)]
        [DataRow(true)]
        public async Task AndroidClientInfo_PreservesLegacyKeysAndRefreshAsync(bool emptyIdToken)
        {
            // Arrange
            using var http = new MockHttpManager();
            var app = CreateApplication(http, false);
            var cache = (TokenCache)app.UserTokenCacheInternal;
            var stores = new Dictionary<string, Dictionary<string, object>>(StringComparer.Ordinal);
            cache.Accessor = CreateAccessor(stores);
            string clientInfo = MockHelpers.CreateClientInfo();
            var signedIn = await SignInAsync(app, http, "member", clientInfo).ConfigureAwait(false);
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
                ExpectedPostData = new Dictionary<string, string> { ["refresh_token"] = "rt-member" },
                ResponseMessage = CreateTokenResponse("member", false, refreshed: true, emptyIdToken: emptyIdToken)
            });
            var refreshed = await app.AcquireTokenSilent(s_scopes, account)
                .WithForceRefresh(true).ExecuteAsync().ConfigureAwait(false);

            // Assert
            Assert.AreEqual("at-member", cached.AccessToken);
            Assert.AreEqual("member", cached.ClaimsPrincipal.FindFirst("sub").Value);
            Assert.AreEqual("member@example.test", cached.Account.Username);
            Assert.AreEqual("at-refreshed-member", refreshed.AccessToken);
            Assert.AreEqual(clientInfo, cache.Accessor.GetAllAccessTokens().Single().RawClientInfo);
            Assert.AreEqual(clientInfo, cache.Accessor.GetAllRefreshTokens().Single().RawClientInfo);
            foreach (var store in stores)
            {
                CollectionAssert.AreEquivalent(originalKeys[store.Key], store.Value.Keys.ToArray());
            }
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
            PublicClientApplication app, MockHttpManager http, string subject, string clientInfo = null)
        {
            http.AddMockHandler(new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Post,
                ResponseMessage = CreateTokenResponse(subject, true, clientInfo: clientInfo)
            });
            return await app.AcquireTokenInteractive(s_scopes).ExecuteAsync().ConfigureAwait(false);
        }

        private static HttpResponseMessage CreateTokenResponse(
            string subject, bool includeIdToken, bool refreshed = false, string clientInfo = null, bool emptyIdToken = false)
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
                    ["preferred_username"] = subject + "@example.test",
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
