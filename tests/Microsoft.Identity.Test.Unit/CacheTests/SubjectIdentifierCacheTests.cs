// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Common.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

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
        public async Task CaseDistinctSubjects_RemainIsolatedAsync(
            string firstSubject, string secondSubject, bool genericAuthority, bool refreshIncludesIdToken)
        {
            // Arrange
            using var http = new MockHttpManager();
            var app = CreateApplication(http, genericAuthority);
            var cache = (TokenCache)app.UserTokenCacheInternal;
            cache.Accessor = CreateFlatAccessor();

            // Act
            var first = await SignInAsync(app, http, firstSubject).ConfigureAwait(false);
            AssertStoredSubjects(cache.Accessor, firstSubject);
            var second = await SignInAsync(app, http, secondSubject).ConfigureAwait(false);
            AssertStoredSubjects(cache.Accessor, firstSubject, secondSubject);

            // Assert
            var accounts = (await app.GetAccountsAsync().ConfigureAwait(false)).ToList();
            Assert.HasCount(2, accounts);
            Assert.AreEqual(firstSubject, (await app.GetAccountAsync(firstSubject).ConfigureAwait(false)).HomeAccountId.Identifier);
            Assert.AreEqual(secondSubject, (await app.GetAccountAsync(secondSubject).ConfigureAwait(false)).HomeAccountId.Identifier);
            await AssertCachedTokenAsync(app, first.Account, firstSubject).ConfigureAwait(false);
            await AssertCachedTokenAsync(app, second.Account, secondSubject).ConfigureAwait(false);

            var serializer = new TokenCacheJsonSerializer(cache.Accessor);
            byte[] serialized = serializer.Serialize(null);
            cache.Accessor = CreateFlatAccessor();
            new TokenCacheJsonSerializer(cache.Accessor).Deserialize(serialized, true);
            AssertStoredSubjects(cache.Accessor, firstSubject, secondSubject);
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
            Assert.AreEqual(TokenSource.IdentityProvider, refreshed.AuthenticationResultMetadata.TokenSource);
            AssertStoredSubjects(cache.Accessor, firstSubject, secondSubject);
            await AssertCachedTokenAsync(app, second.Account, secondSubject).ConfigureAwait(false);

            await app.RemoveAsync(first.Account).ConfigureAwait(false);
            AssertStoredSubjects(cache.Accessor, secondSubject);
            Assert.IsNull(await app.GetAccountAsync(firstSubject).ConfigureAwait(false));
            await Assert.ThrowsExactlyAsync<MsalUiRequiredException>(
                () => app.AcquireTokenSilent(s_scopes, first.Account).ExecuteAsync()).ConfigureAwait(false);
            await AssertCachedTokenAsync(app, second.Account, secondSubject).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task RefreshWithoutIdentityMetadata_PreservesClientInfoKeysAsync()
        {
            // Arrange
            using var http = new MockHttpManager();
            var app = CreateApplication(http, false);
            var cache = (TokenCache)app.UserTokenCacheInternal;
            cache.Accessor = CreateFlatAccessor();
            string clientInfo = MockHelpers.CreateClientInfo();
            http.AddMockHandler(new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Post,
                ResponseMessage = CreateTokenResponse("member", true, clientInfo: clientInfo)
            });
            var signedIn = await app.AcquireTokenInteractive(s_scopes).ExecuteAsync().ConfigureAwait(false);
            string accessTokenKey = cache.Accessor.GetAllAccessTokens().Single().CacheKey;
            string refreshTokenKey = cache.Accessor.GetAllRefreshTokens().Single().CacheKey;
            http.AddMockHandler(new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Post,
                ExpectedPostData = new Dictionary<string, string> { ["refresh_token"] = "rt-member" },
                ResponseMessage = CreateTokenResponse("member", false, refreshed: true)
            });

            // Act
            var account = new Account(signedIn.Account.HomeAccountId.Identifier.ToUpperInvariant(),
                signedIn.Account.Username, signedIn.Account.Environment);
            var refreshed = await app.AcquireTokenSilent(s_scopes, account)
                .WithForceRefresh(true).ExecuteAsync().ConfigureAwait(false);

            // Assert
            Assert.AreEqual("at-refreshed-member", refreshed.AccessToken);
            Assert.AreEqual(accessTokenKey, cache.Accessor.GetAllAccessTokens().Single().CacheKey);
            Assert.AreEqual(refreshTokenKey, cache.Accessor.GetAllRefreshTokens().Single().CacheKey);
            Assert.AreEqual(clientInfo, cache.Accessor.GetAllAccessTokens().Single().RawClientInfo);
            Assert.AreEqual(clientInfo, cache.Accessor.GetAllRefreshTokens().Single().RawClientInfo);
        }

        [TestMethod]
        public async Task LegacyIdToken_CannotAliasVersionedSubjectAsync()
        {
            // Arrange
            using var http = new MockHttpManager();
            var app = CreateApplication(http, false);
            var cache = (TokenCache)app.UserTokenCacheInternal;
            cache.Accessor = CreateFlatAccessor();
            var signedIn = await SignInAsync(app, http, "member").ConfigureAwait(false);
            var accessToken = cache.Accessor.GetAllAccessTokens().Single();
            var legacyJson = cache.Accessor.GetAllIdTokens().Single().ToJObject();
            legacyJson[StorageJsonKeys.HomeAccountId] = accessToken.GetHomeAccountIdKey();
            legacyJson.Remove(StorageJsonKeys.SubjectKeyVersion);
            var legacyIdToken = MsalIdTokenCacheItem.FromJObject(legacyJson).CloneForSnapshot();
            Assert.IsTrue(legacyIdToken.IsLegacySubjectEntry);
            Assert.AreEqual(accessToken.GetIdTokenItem().CacheKey, legacyIdToken.CacheKey);
            cache.Accessor.SaveIdToken(legacyIdToken);

            // Act
            var result = await app.AcquireTokenSilent(s_scopes, signedIn.Account).ExecuteAsync().ConfigureAwait(false);

            // Assert
            Assert.AreEqual("at-member", result.AccessToken);
            Assert.IsNull(result.IdToken);
        }

        [TestMethod]
        [DataRow("member", false)]
        [DataRow("Member", false)]
        [DataRow("member", true)]
        [DataRow("Member", true)]
        public async Task LegacySubjectCredentials_RequireInteractionAsync(string requestedSubject, bool forceRefresh)
        {
            // Arrange
            using var http = new MockHttpManager();
            var app = CreateApplication(http, false);
            var cache = (TokenCache)app.UserTokenCacheInternal;
            cache.Accessor = CreateFlatAccessor();
            var signedIn = await SignInAsync(app, http, "Member").ConfigureAwait(false);
            var legacyAccessToken = cache.Accessor.GetAllAccessTokens().Single().ToJObject();
            var legacyRefreshToken = cache.Accessor.GetAllRefreshTokens().Single().ToJObject();
            legacyAccessToken.Remove(StorageJsonKeys.SubjectKeyVersion);
            legacyRefreshToken.Remove(StorageJsonKeys.SubjectKeyVersion);
            cache.Accessor.DeleteAccessToken(cache.Accessor.GetAllAccessTokens().Single());
            cache.Accessor.DeleteRefreshToken(cache.Accessor.GetAllRefreshTokens().Single());
            cache.Accessor.SaveAccessToken(MsalAccessTokenCacheItem.FromJObject(legacyAccessToken));
            cache.Accessor.SaveRefreshToken(MsalRefreshTokenCacheItem.FromJObject(legacyRefreshToken));
            var account = new Account(requestedSubject, signedIn.Account.Username, signedIn.Account.Environment);

            // Act
            byte[] serialized = new TokenCacheJsonSerializer(cache.Accessor).Serialize(null);
            cache.Accessor = CreateFlatAccessor();
            new TokenCacheJsonSerializer(cache.Accessor).Deserialize(serialized, true);
            var exception = await Assert.ThrowsExactlyAsync<MsalUiRequiredException>(
                () => app.AcquireTokenSilent(s_scopes, account).WithForceRefresh(forceRefresh).ExecuteAsync())
                .ConfigureAwait(false);

            // Assert
            Assert.AreEqual(MsalError.NoTokensFoundError, exception.ErrorCode);
            Assert.IsTrue(cache.Accessor.GetAllAccessTokens().Single().IsLegacySubjectEntry);
            Assert.IsTrue(cache.Accessor.GetAllRefreshTokens().Single().IsLegacySubjectEntry);
            Assert.IsTrue(cache.Accessor.GetAllAccessTokens().Single().CloneForSnapshot().IsLegacySubjectEntry);
            Assert.IsTrue(cache.Accessor.GetAllRefreshTokens().Single().CloneForSnapshot().IsLegacySubjectEntry);
            Assert.IsEmpty(await app.GetAccountsAsync().ConfigureAwait(false));
            // No HTTP handler is queued: any attempt to redeem the legacy RT fails this test.

            var recovered = await SignInAsync(app, http, requestedSubject).ConfigureAwait(false);
            await AssertCachedTokenAsync(app, recovered.Account, requestedSubject).ConfigureAwait(false);
            Assert.HasCount(1, await app.GetAccountsAsync().ConfigureAwait(false));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task IosAccountAfterSignIn_UsesFreshMetadataAsync(bool hasClientInfo)
        {
            // Arrange
            using var http = new MockHttpManager();
            var app = CreateApplication(http, false);
            var cache = (TokenCache)app.UserTokenCacheInternal;
            cache.Accessor = CreateFlatAccessor(useIosAccountKeys: true);
            string clientInfo = hasClientInfo ? MockHelpers.CreateClientInfo() : null;
            await SignInAsync(app, http, "member", clientInfo).ConfigureAwait(false);
            var originalAccount = cache.Accessor.GetAllAccounts().Single();
            var legacyJson = originalAccount.ToJObject();
            legacyJson.Remove(StorageJsonKeys.SubjectKeyVersion);
            legacyJson[StorageJsonKeys.Username] = "old@example.test";
            cache.Accessor.DeleteAccount(originalAccount);
            cache.Accessor.SaveAccount(MsalAccountCacheItem.FromJObject(legacyJson));
            Assert.AreEqual("old@example.test", cache.Accessor.GetAllAccounts().Single().PreferredUsername);

            // Act
            var signedIn = await SignInAsync(app, http, "member", clientInfo).ConfigureAwait(false);
            var storedAccounts = cache.Accessor.GetAllAccounts();
            Assert.HasCount(hasClientInfo ? 1 : 2, storedAccounts);
            if (!hasClientInfo)
            {
                Assert.IsTrue(storedAccounts[0].IsLegacySubjectEntry);
                Assert.AreEqual("old@example.test", storedAccounts[0].PreferredUsername);
            }

            var accounts = (await app.GetAccountsAsync().ConfigureAwait(false)).ToList();
            var selectedAccount = await app.GetAccountAsync(signedIn.Account.HomeAccountId.Identifier).ConfigureAwait(false);

            // Assert
            Assert.HasCount(1, accounts);
            Assert.AreEqual("member@example.test", signedIn.Account.Username);
            Assert.AreEqual(signedIn.Account.Username, accounts[0].Username);
            Assert.IsNotNull(selectedAccount);
            Assert.AreEqual(signedIn.Account.Username, selectedAccount.Username);
            var cached = await app.AcquireTokenSilent(s_scopes, selectedAccount).ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual(signedIn.Account.Username, cached.Account.Username);
            Assert.AreEqual("at-member", cached.AccessToken);
            Assert.AreEqual(TokenSource.Cache, cached.AuthenticationResultMetadata.TokenSource);
        }

        [TestMethod]
        [DataRow(StorageJsonValues.AccountRootKey)]
        [DataRow(StorageJsonValues.CredentialTypeRefreshToken)]
        public async Task SharedV3Cache_PreservesClientInfoIdentityAsync(string clientInfoSection)
        {
            // Arrange
            using var http = new MockHttpManager();
            var app = CreateApplication(http, false);
            var cache = (TokenCache)app.UserTokenCacheInternal;
            cache.Accessor = CreateFlatAccessor();
            string clientInfo = MockHelpers.CreateClientInfo();
            var signedIn = await SignInAsync(app, http, "member", clientInfo).ConfigureAwait(false);
            string accessTokenKey = cache.Accessor.GetAllAccessTokens().Single().CacheKey;
            var root = CreateSharedV3Cache(cache, clientInfoSection);

            // Act
            ((ITokenCacheSerializer)app.UserTokenCache).DeserializeMsalV3(root.ToJsonString().ToByteArray(), true);
            var accounts = (await app.GetAccountsAsync().ConfigureAwait(false)).ToList();

            // Assert
            Assert.HasCount(1, accounts);
            Assert.AreEqual(signedIn.Account.HomeAccountId.Identifier, accounts[0].HomeAccountId.Identifier);
            Assert.AreEqual(accessTokenKey, cache.Accessor.GetAllAccessTokens().Single().CacheKey);
            var cached = await app.AcquireTokenSilent(s_scopes, accounts[0]).ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual("at-member", cached.AccessToken);
            Assert.AreEqual(TokenSource.Cache, cached.AuthenticationResultMetadata.TokenSource);
            Assert.IsNotNull(cached.IdToken);

            http.AddMockHandler(new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Post,
                ExpectedPostData = new Dictionary<string, string> { ["refresh_token"] = "rt-member" },
                ResponseMessage = CreateTokenResponse("member", false, refreshed: true)
            });
            var refreshed = await app.AcquireTokenSilent(s_scopes, accounts[0])
                .WithForceRefresh(true).ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual("at-refreshed-member", refreshed.AccessToken);
            Assert.AreEqual(accessTokenKey, cache.Accessor.GetAllAccessTokens().Single().CacheKey);
            Assert.AreEqual(clientInfo, cache.Accessor.GetAllAccessTokens().Single().RawClientInfo);
            Assert.AreEqual(clientInfo, cache.Accessor.GetAllRefreshTokens().Single().RawClientInfo);
        }

        [TestMethod]
        [DataRow("case")]
        [DataRow("environment")]
        [DataRow("mismatchedClientInfo")]
        [DataRow("conflictingClientInfo")]
        [DataRow("subjectVersion")]
        [DataRow("unknownVersion")]
        public async Task SharedV3Cache_DoesNotInferAmbiguousIdentityAsync(string scenario)
        {
            // Arrange
            using var http = new MockHttpManager();
            var app = CreateApplication(http, false);
            var cache = (TokenCache)app.UserTokenCacheInternal;
            cache.Accessor = CreateFlatAccessor();
            var signedIn = await SignInAsync(app, http, "member", MockHelpers.CreateClientInfo()).ConfigureAwait(false);
            var root = CreateSharedV3Cache(cache, StorageJsonValues.AccountRootKey);
            var account = root[StorageJsonValues.AccountRootKey].AsObject().Single().Value.AsObject();
            var idToken = root[StorageJsonValues.CredentialTypeIdToken].AsObject().Single().Value.AsObject();
            string differentClientInfo = Base64UrlHelpers.Encode("{\"uid\":\"other\",\"utid\":\"tenant\"}");
            switch (scenario)
            {
                case "case":
                    account[StorageJsonKeys.HomeAccountId] = signedIn.Account.HomeAccountId.Identifier.ToUpperInvariant();
                    break;
                case "environment":
                    account[StorageJsonKeys.Environment] = "other.example.test";
                    break;
                case "mismatchedClientInfo":
                    account[StorageJsonKeys.ClientInfo] = differentClientInfo;
                    break;
                case "conflictingClientInfo":
                    idToken[StorageJsonKeys.ClientInfo] = differentClientInfo;
                    break;
                case "subjectVersion":
                    idToken[StorageJsonKeys.SubjectKeyVersion] = "1";
                    break;
                case "unknownVersion":
                    idToken[StorageJsonKeys.SubjectKeyVersion] = "2";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(scenario));
            }

            // Act
            ((ITokenCacheSerializer)app.UserTokenCache).DeserializeMsalV3(root.ToJsonString().ToByteArray(), true);

            // Assert
            Assert.IsTrue(cache.Accessor.GetAllAccessTokens().Single().IsLegacySubjectEntry);
            Assert.IsTrue(cache.Accessor.GetAllRefreshTokens().Single().IsLegacySubjectEntry);
            Assert.IsEmpty(await app.GetAccountsAsync().ConfigureAwait(false));
            var exception = await Assert.ThrowsExactlyAsync<MsalUiRequiredException>(
                () => app.AcquireTokenSilent(s_scopes, signedIn.Account).ExecuteAsync()).ConfigureAwait(false);
            Assert.AreEqual(MsalError.NoTokensFoundError, exception.ErrorCode);
        }

        private static JsonObject CreateSharedV3Cache(TokenCache cache, string clientInfoSection)
        {
            var root = JsonNode.Parse(new TokenCacheJsonSerializer(cache.Accessor).Serialize(null)).AsObject();
            foreach (var section in root.Where(section => section.Key != clientInfoSection))
            {
                foreach (var entry in section.Value.AsObject())
                {
                    entry.Value.AsObject().Remove(StorageJsonKeys.ClientInfo);
                }
            }

            return root;
        }

        private static PublicClientApplication CreateApplication(MockHttpManager http, bool genericAuthority)
        {
            var builder = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithHttpManager(http)
                .WithRedirectUri(TestConstants.RedirectUri)
                .WithInstanceDiscovery(false);
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
            string subject, bool includeIdToken, bool refreshed = false, string clientInfo = null)
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

            return MockHelpers.CreateSuccessResponseMessage(response.ToJsonString());
        }

        private static async Task AssertCachedTokenAsync(PublicClientApplication app, IAccount account, string subject)
        {
            var result = await app.AcquireTokenSilent(s_scopes, account).ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual("at-" + subject, result.AccessToken);
            Assert.AreEqual(subject, result.Account.HomeAccountId.Identifier);
            Assert.AreEqual(subject, result.ClaimsPrincipal.FindFirst("sub").Value);
            Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
        }

        private static void AssertStoredSubjects(ITokenCacheAccessor accessor, params string[] subjects)
        {
            CollectionAssert.AreEquivalent(subjects, accessor.GetAllAccessTokens().Select(t => t.HomeAccountId).ToArray());
            CollectionAssert.AreEquivalent(subjects, accessor.GetAllRefreshTokens().Select(t => t.HomeAccountId).ToArray());
            CollectionAssert.AreEquivalent(subjects, accessor.GetAllIdTokens().Select(t => t.HomeAccountId).ToArray());
            CollectionAssert.AreEquivalent(subjects, accessor.GetAllAccounts().Select(t => t.HomeAccountId).ToArray());
            Assert.HasCount(subjects.Length, accessor.GetAllAccessTokens().Select(t => t.iOSCacheKey.iOSAccount).Distinct(StringComparer.OrdinalIgnoreCase));
            Assert.HasCount(subjects.Length, accessor.GetAllRefreshTokens().Select(t => t.iOSCacheKey.iOSAccount).Distinct(StringComparer.OrdinalIgnoreCase));
            Assert.HasCount(subjects.Length, accessor.GetAllIdTokens().Select(t => t.iOSCacheKey.iOSAccount).Distinct(StringComparer.OrdinalIgnoreCase));
            Assert.HasCount(subjects.Length, accessor.GetAllAccounts().Select(t => t.iOSCacheKey.iOSAccount).Distinct(StringComparer.OrdinalIgnoreCase));
        }

        private static ITokenCacheAccessor CreateFlatAccessor(bool useIosAccountKeys = false)
        {
            // Android persists JSON by CacheKey and ignores optional partition keys on enumeration.
            var accessor = Substitute.For<ITokenCacheAccessor>();
            var accessTokens = new Dictionary<string, string>(StringComparer.Ordinal);
            var refreshTokens = new Dictionary<string, string>(StringComparer.Ordinal);
            var idTokens = new Dictionary<string, string>(StringComparer.Ordinal);
            var accounts = new Dictionary<(string Account, string Service), string>();
            var metadata = new Dictionary<string, MsalAppMetadataCacheItem>(StringComparer.Ordinal);

            (string Account, string Service) GetAccountKey(MsalAccountCacheItem item) =>
                useIosAccountKeys ? (item.iOSCacheKey.iOSAccount, item.iOSCacheKey.iOSService) : (item.CacheKey, null);

            accessor.When(a => a.SaveAccessToken(Arg.Any<MsalAccessTokenCacheItem>())).Do(c =>
            {
                var item = c.Arg<MsalAccessTokenCacheItem>();
                accessTokens[item.CacheKey] = item.ToJObject().ToJsonString();
            });
            accessor.When(a => a.SaveRefreshToken(Arg.Any<MsalRefreshTokenCacheItem>())).Do(c =>
            {
                var item = c.Arg<MsalRefreshTokenCacheItem>();
                refreshTokens[item.CacheKey] = item.ToJObject().ToJsonString();
            });
            accessor.When(a => a.SaveIdToken(Arg.Any<MsalIdTokenCacheItem>())).Do(c =>
            {
                var item = c.Arg<MsalIdTokenCacheItem>();
                idTokens[item.CacheKey] = item.ToJObject().ToJsonString();
            });
            accessor.When(a => a.SaveAccount(Arg.Any<MsalAccountCacheItem>())).Do(c =>
            {
                var item = c.Arg<MsalAccountCacheItem>();
                accounts[GetAccountKey(item)] = item.ToJObject().ToJsonString();
            });
            accessor.When(a => a.SaveAppMetadata(Arg.Any<MsalAppMetadataCacheItem>())).Do(c =>
            {
                var item = c.Arg<MsalAppMetadataCacheItem>();
                metadata[item.CacheKey] = item;
            });
            accessor.GetAllAccessTokens().ReturnsForAnyArgs(_ => accessTokens.Values.Select(MsalAccessTokenCacheItem.FromJsonString).ToList());
            accessor.GetAllRefreshTokens().ReturnsForAnyArgs(_ => refreshTokens.Values.Select(MsalRefreshTokenCacheItem.FromJsonString).ToList());
            accessor.GetAllIdTokens().ReturnsForAnyArgs(_ => idTokens.Values.Select(MsalIdTokenCacheItem.FromJsonString).ToList());
            // Exercise the iOS upgrade join with legacy account records enumerated first.
            accessor.GetAllAccounts().ReturnsForAnyArgs(_ => accounts.Values.Select(MsalAccountCacheItem.FromJsonString)
                .OrderBy(item => item.SubjectKeyVersion).ToList());
            accessor.GetAllAppMetadata().Returns(_ => metadata.Values.ToList());
            accessor.GetIdToken(Arg.Any<MsalAccessTokenCacheItem>()).Returns(c =>
                idTokens.TryGetValue(c.Arg<MsalAccessTokenCacheItem>().GetIdTokenItem().CacheKey, out string json)
                    ? MsalIdTokenCacheItem.FromJsonString(json) : null);
            accessor.GetAccount(Arg.Any<MsalAccountCacheItem>()).Returns(c =>
                accounts.TryGetValue(GetAccountKey(c.Arg<MsalAccountCacheItem>()), out string json)
                    ? MsalAccountCacheItem.FromJsonString(json) : null);
            accessor.GetAppMetadata(Arg.Any<MsalAppMetadataCacheItem>()).Returns(c =>
                metadata.TryGetValue(c.Arg<MsalAppMetadataCacheItem>().CacheKey, out var item) ? item : null);
            accessor.When(a => a.DeleteAccessToken(Arg.Any<MsalAccessTokenCacheItem>())).Do(c => accessTokens.Remove(c.Arg<MsalAccessTokenCacheItem>().CacheKey));
            accessor.When(a => a.DeleteRefreshToken(Arg.Any<MsalRefreshTokenCacheItem>())).Do(c => refreshTokens.Remove(c.Arg<MsalRefreshTokenCacheItem>().CacheKey));
            accessor.When(a => a.DeleteIdToken(Arg.Any<MsalIdTokenCacheItem>())).Do(c => idTokens.Remove(c.Arg<MsalIdTokenCacheItem>().CacheKey));
            accessor.When(a => a.DeleteAccount(Arg.Any<MsalAccountCacheItem>())).Do(c => accounts.Remove(GetAccountKey(c.Arg<MsalAccountCacheItem>())));
            accessor.When(a => a.Clear()).Do(_ =>
            {
                accessTokens.Clear();
                refreshTokens.Clear();
                idTokens.Clear();
                accounts.Clear();
                metadata.Clear();
            });
            accessor.HasAccessOrRefreshTokens().Returns(_ => accessTokens.Count + refreshTokens.Count > 0);
            accessor.EntryCount.Returns(_ => accessTokens.Count + refreshTokens.Count + idTokens.Count + accounts.Count + metadata.Count);
            return accessor;
        }
    }
}
