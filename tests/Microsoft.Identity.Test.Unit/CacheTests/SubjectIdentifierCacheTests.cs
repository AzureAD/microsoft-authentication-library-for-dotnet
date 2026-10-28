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
            PublicClientApplication app, MockHttpManager http, string subject)
        {
            http.AddMockHandler(new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Post,
                ResponseMessage = CreateTokenResponse(subject, true)
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

        private static ITokenCacheAccessor CreateFlatAccessor()
        {
            // Android persists JSON by CacheKey and ignores optional partition keys on enumeration.
            var accessor = Substitute.For<ITokenCacheAccessor>();
            var accessTokens = new Dictionary<string, string>(StringComparer.Ordinal);
            var refreshTokens = new Dictionary<string, string>(StringComparer.Ordinal);
            var idTokens = new Dictionary<string, string>(StringComparer.Ordinal);
            var accounts = new Dictionary<string, string>(StringComparer.Ordinal);
            var metadata = new Dictionary<string, MsalAppMetadataCacheItem>(StringComparer.Ordinal);

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
                accounts[item.CacheKey] = item.ToJObject().ToJsonString();
            });
            accessor.When(a => a.SaveAppMetadata(Arg.Any<MsalAppMetadataCacheItem>())).Do(c =>
            {
                var item = c.Arg<MsalAppMetadataCacheItem>();
                metadata[item.CacheKey] = item;
            });
            accessor.GetAllAccessTokens().ReturnsForAnyArgs(_ => accessTokens.Values.Select(MsalAccessTokenCacheItem.FromJsonString).ToList());
            accessor.GetAllRefreshTokens().ReturnsForAnyArgs(_ => refreshTokens.Values.Select(MsalRefreshTokenCacheItem.FromJsonString).ToList());
            accessor.GetAllIdTokens().ReturnsForAnyArgs(_ => idTokens.Values.Select(MsalIdTokenCacheItem.FromJsonString).ToList());
            accessor.GetAllAccounts().ReturnsForAnyArgs(_ => accounts.Values.Select(MsalAccountCacheItem.FromJsonString).ToList());
            accessor.GetAllAppMetadata().Returns(_ => metadata.Values.ToList());
            accessor.GetIdToken(Arg.Any<MsalAccessTokenCacheItem>()).Returns(c =>
                idTokens.TryGetValue(c.Arg<MsalAccessTokenCacheItem>().GetIdTokenItem().CacheKey, out string json)
                    ? MsalIdTokenCacheItem.FromJsonString(json) : null);
            accessor.GetAccount(Arg.Any<MsalAccountCacheItem>()).Returns(c =>
                accounts.TryGetValue(c.Arg<MsalAccountCacheItem>().CacheKey, out string json)
                    ? MsalAccountCacheItem.FromJsonString(json) : null);
            accessor.GetAppMetadata(Arg.Any<MsalAppMetadataCacheItem>()).Returns(c =>
                metadata.TryGetValue(c.Arg<MsalAppMetadataCacheItem>().CacheKey, out var item) ? item : null);
            accessor.When(a => a.DeleteAccessToken(Arg.Any<MsalAccessTokenCacheItem>())).Do(c => accessTokens.Remove(c.Arg<MsalAccessTokenCacheItem>().CacheKey));
            accessor.When(a => a.DeleteRefreshToken(Arg.Any<MsalRefreshTokenCacheItem>())).Do(c => refreshTokens.Remove(c.Arg<MsalRefreshTokenCacheItem>().CacheKey));
            accessor.When(a => a.DeleteIdToken(Arg.Any<MsalIdTokenCacheItem>())).Do(c => idTokens.Remove(c.Arg<MsalIdTokenCacheItem>().CacheKey));
            accessor.When(a => a.DeleteAccount(Arg.Any<MsalAccountCacheItem>())).Do(c => accounts.Remove(c.Arg<MsalAccountCacheItem>().CacheKey));
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
