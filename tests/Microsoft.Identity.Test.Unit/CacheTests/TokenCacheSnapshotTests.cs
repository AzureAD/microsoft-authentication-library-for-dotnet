// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Cache.Keys;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CacheTests
{
    [TestClass]
    public class TokenCacheSnapshotTests : TestBase
    {
        // If a clone coverage test fails after changing a cache item or a base class, update
        // CloneForSnapshot to handle the member, then update the fixture and expected members.
        [TestMethod]
        public void AccessTokenSnapshotCloneAccountsForEveryInstanceMember()
        {
            // Arrange
            var time = DateTimeOffset.FromUnixTimeSeconds(1800000000);
            var source = new MsalAccessTokenCacheItem(
                "environment", "snapshot-tests", "scope-a scope-b", "tenant", "synthetic-access-token",
                time, time.AddHours(1), time.AddHours(2), "client-info", "account",
                "key", time.AddMinutes(30), "pop", "obo",
                new SortedList<string, string> { ["partition"] = "one" })
            {
                AdditionalFieldsJson = "{\"future_item\":{\"values\":[1,2]}}",
                IsExtendedLifeTimeToken = true
            };
            source.AddAdditionalCacheParameters(new Dictionary<string, string> { ["extra"] = "value" });
            var expectedProperties = new[]
            {
                nameof(source.TenantId),
                nameof(source.OboCacheKey),
                nameof(source.KeyId),
                nameof(source.TokenType),
                nameof(source.ScopeSet),
                nameof(source.ScopeString),
                nameof(source.ExpiresOn),
                nameof(source.ExtendedExpiresOn),
                nameof(source.RefreshOn),
                nameof(source.CachedAt),
                nameof(source.IsExtendedLifeTimeToken),
                nameof(source.CacheKey),
                nameof(source.AdditionalCacheKeyComponents),
                nameof(source.PersistedCacheParameters),
                nameof(source.iOSCacheKey),
                nameof(source.CredentialType),
                nameof(source.ClientId),
                nameof(source.Secret),
                nameof(source.HomeAccountId),
                nameof(source.Environment),
                nameof(source.RawClientInfo),
                nameof(source.IsAndroidCacheItem),
                nameof(source.IsAndroidSubject),
                nameof(source.AdditionalFieldsJson)
            };
            // Act
            var clone = source.CloneForSnapshot();

            // Assert
            AssertSnapshotClone(source, clone, expectedProperties,
                new[] { "_extraKeyParts", "_credentialDescriptor", "iOSCacheKeyLazy" });
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void RefreshTokenSnapshotCloneAccountsForEveryInstanceMember(bool familyToken)
        {
            // Arrange
            var source = new MsalRefreshTokenCacheItem(
                "environment", "snapshot-tests", "synthetic-refresh-token", "client-info",
                familyToken ? "1" : "", "account",
                familyToken ? null : new SortedList<string, string> { ["partition"] = "one" })
            {
                OboCacheKey = "obo",
                AdditionalFieldsJson = "{\"future_item\":{\"values\":[1,2]}}"
            };
            var expectedProperties = new[]
            {
                nameof(source.FamilyId),
                nameof(source.OboCacheKey),
                nameof(source.IsFRT),
                nameof(source.AdditionalCacheKeyComponents),
                nameof(source.CacheKey),
                nameof(source.iOSCacheKey),
                nameof(source.CredentialType),
                nameof(source.ClientId),
                nameof(source.Secret),
                nameof(source.HomeAccountId),
                nameof(source.Environment),
                nameof(source.RawClientInfo),
                nameof(source.IsAndroidCacheItem),
                nameof(source.IsAndroidSubject),
                nameof(source.AdditionalFieldsJson)
            };

            // Act
            var clone = source.CloneForSnapshot();

            // Assert
            AssertSnapshotClone(source, clone, expectedProperties, new[] { "iOSCacheKeyLazy" });
        }

        [TestMethod]
        public void IdTokenSnapshotCloneAccountsForEveryInstanceMember()
        {
            // Arrange
            var source = new MsalIdTokenCacheItem(
                "environment", "snapshot-tests",
                "header." + Base64UrlHelpers.Encode(
                    "{\"sub\":\"subject\",\"tid\":\"tenant\",\"name\":\"Test User\",\"preferred_username\":\"user@example.com\"}"),
                "client-info", "account", "tenant")
            {
                AdditionalFieldsJson = "{\"future_item\":{\"values\":[1,2]}}"
            };
            var expectedProperties = new[]
            {
                nameof(source.TenantId),
                nameof(source.IdToken),
                nameof(source.CacheKey),
                nameof(source.iOSCacheKey),
                nameof(source.CredentialType),
                nameof(source.ClientId),
                nameof(source.Secret),
                nameof(source.HomeAccountId),
                nameof(source.Environment),
                nameof(source.RawClientInfo),
                nameof(source.IsAndroidCacheItem),
                nameof(source.IsAndroidSubject),
                nameof(source.AdditionalFieldsJson)
            };

            // Act
            var clone = source.CloneForSnapshot();

            // Assert
            AssertSnapshotClone(source, clone, expectedProperties, new[] { "idTokenLazy", "iOSCacheKeyLazy" });
        }

        [TestMethod]
        public void AccountSnapshotCloneAccountsForEveryInstanceMember()
        {
            // Arrange
            var source = new MsalAccountCacheItem(
                "environment", "local-account", "client-info", "account", "account-source",
                "Test User", "user@example.com", "tenant", "Test", "User",
                new Dictionary<string, string> { ["app"] = "wam-account" })
            {
                AuthorityType = "B2C",
                AdditionalFieldsJson = "{\"future_item\":{\"values\":[1,2]}}"
            };
            var expectedProperties = new[]
            {
                nameof(source.TenantId),
                nameof(source.PreferredUsername),
                nameof(source.Name),
                nameof(source.GivenName),
                nameof(source.FamilyName),
                nameof(source.LocalAccountId),
                nameof(source.AccountSource),
                nameof(source.AuthorityType),
                nameof(source.WamAccountIds),
                nameof(source.CacheKey),
                nameof(source.iOSCacheKey),
                nameof(source.HomeAccountId),
                nameof(source.Environment),
                nameof(source.RawClientInfo),
                nameof(source.IsAndroidCacheItem),
                nameof(source.IsAndroidSubject),
                nameof(source.AdditionalFieldsJson)
            };

            // Act
            var clone = source.CloneForSnapshot();

            // Assert
            AssertSnapshotClone(source, clone, expectedProperties, new[] { "iOSCacheKeyLazy" });
        }

        [TestMethod]
        public void AppMetadataSnapshotCloneAccountsForEveryInstanceMember()
        {
            // Arrange
            var source = new MsalAppMetadataCacheItem("snapshot-tests", "environment", "1")
            {
                AdditionalFieldsJson = "{\"future_item\":{\"values\":[1,2]}}"
            };
            var expectedProperties = new[]
            {
                nameof(source.ClientId),
                nameof(source.Environment),
                nameof(source.FamilyId),
                nameof(source.CacheKey),
                nameof(source.iOSCacheKey),
                nameof(source.AdditionalFieldsJson)
            };

            // Act
            var clone = source.CloneForSnapshot();

            // Assert
            AssertSnapshotClone(source, clone, expectedProperties, new[] { "iOSCacheKeyLazy" });
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void SnapshotPreservesV3State(bool applicationCache)
        {
            // Arrange
            var source = CreateCache(applicationCache);
            Populate(source, applicationCache);
            byte[] expected = source.SerializeMsalV3();

            // Act
            var snapshot = ((ITokenCacheSnapshotProvider)source).CreateSnapshot();
            var target = CreateCache(applicationCache);
            ((ITokenCacheSnapshotProvider)target).RestoreSnapshot(snapshot);

            // Assert
            AssertJsonEqual(expected, snapshot.SerializeMsalV3());
            AssertJsonEqual(expected, target.SerializeMsalV3());
        }

        [TestMethod]
        public void SnapshotIsolatedFromSourceAndEveryRestore()
        {
            // Arrange
            var source = CreateCache(false);
            Populate(source, false);
            var snapshot = ((ITokenCacheSnapshotProvider)source).CreateSnapshot();
            byte[] expected = snapshot.SerializeMsalV3();

            // Act
            Mutate(source);
            var first = CreateCache(false);
            var second = CreateCache(false);
            ((ITokenCacheSnapshotProvider)first).RestoreSnapshot(snapshot);
            Mutate(first);
            ((ITokenCacheSnapshotProvider)second).RestoreSnapshot(snapshot);

            // Assert
            AssertJsonEqual(expected, snapshot.SerializeMsalV3());
            AssertJsonEqual(expected, second.SerializeMsalV3());
            var token = ((ITokenCacheInternal)second).Accessor.GetAllAccessTokens().Single();
            Assert.IsFalse(token.IsExtendedLifeTimeToken);
            Assert.DoesNotContain("injected", token.ScopeSet);
        }

        [TestMethod]
        public async Task ConcurrentRestoresDoNotShareMutableState()
        {
            // Arrange
            var source = CreateCache(false);
            Populate(source, false);
            var snapshot = ((ITokenCacheSnapshotProvider)source).CreateSnapshot();
            byte[] expected = snapshot.SerializeMsalV3();

            // Act
            await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => Task.Run(() =>
            {
                var destination = CreateCache(false);
                ((ITokenCacheSnapshotProvider)destination).RestoreSnapshot(snapshot);
                AssertJsonEqual(expected, destination.SerializeMsalV3());
                Mutate(destination);
            }))).ConfigureAwait(false);

            // Assert
            AssertJsonEqual(expected, snapshot.SerializeMsalV3());
        }

        [TestMethod]
        public void SwitchingAndClearingSnapshotsMatchesBytePath()
        {
            // Arrange
            var a = CreateCache(true);
            var b = CreateCache(true);
            Populate(a, true, "tenant-a");
            Populate(b, true, "tenant-b");
            var target = CreateCache(true);
            var legacy = CreateCache(true);

            // Act / Assert
            foreach (var source in new[] { a, b, a })
            {
                ((ITokenCacheSnapshotProvider)target).RestoreSnapshot(((ITokenCacheSnapshotProvider)source).CreateSnapshot());
                legacy.DeserializeMsalV3(source.SerializeMsalV3(), true);
                AssertJsonEqual(legacy.SerializeMsalV3(), target.SerializeMsalV3());
                Assert.HasCount(1, ((ITokenCacheInternal)target).Accessor.GetAllAccessTokens());
            }
            ((ITokenCacheSnapshotProvider)target).RestoreSnapshot(null);
            legacy.DeserializeMsalV3(null, true);
            AssertJsonEqual(legacy.SerializeMsalV3(), target.SerializeMsalV3());
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void WrongCacheTypeIsRejectedWithoutClearingDestination(bool applicationCache)
        {
            // Arrange
            var source = CreateCache(!applicationCache);
            var target = CreateCache(applicationCache);
            Populate(target, applicationCache);
            byte[] expected = target.SerializeMsalV3();
            var snapshot = ((ITokenCacheSnapshotProvider)source).CreateSnapshot();

            // Act / Assert
            Assert.ThrowsExactly<ArgumentException>(() => ((ITokenCacheSnapshotProvider)target).RestoreSnapshot(snapshot));
            AssertJsonEqual(expected, target.SerializeMsalV3());
        }

        [TestMethod]
        public void UnknownFieldsSurviveSnapshotAndLegacyReader()
        {
            // Arrange
            var source = CreateCache(true);
            Populate(source, true);
            var json = JsonNode.Parse(source.SerializeMsalV3()).AsObject();
            json["future"] = JsonNode.Parse("{\"nested\":[1,null,true,false,1.25,123456789012345678901234567890,{\"value\":\"preserved\"}]}");
            source.DeserializeMsalV3(Encoding.UTF8.GetBytes(json.ToJsonString()), true);

            // Act
            var snapshot = ((ITokenCacheSnapshotProvider)source).CreateSnapshot();
            var legacy = CreateCache(true);
            legacy.DeserializeMsalV3(snapshot.SerializeMsalV3(), true);
            var destination = CreateCache(true);
            ((ITokenCacheSnapshotProvider)destination).RestoreSnapshot(snapshot);

            // Assert
            AssertJsonEqual(source.SerializeMsalV3(), legacy.SerializeMsalV3());
            AssertJsonEqual(source.SerializeMsalV3(), destination.SerializeMsalV3());
        }

        [TestMethod]
        public void UnknownNodesAreIsolatedOnCaptureAndEveryRestore()
        {
            // Arrange
            var source = CreateCache(false);
            var nodes = new Dictionary<string, JsonNode>
            {
                ["future"] = JsonNode.Parse("{\"nested\":[{\"value\":\"original\"},null,true,123456789012345678901234567890]}")
            };
            var snapshot = new TokenCacheSnapshot(source.Accessor, nodes, false);
            byte[] expected = snapshot.SerializeMsalV3();

            // Act
            nodes["future"]["nested"][0]["value"] = "source changed";
            var first = snapshot.Restore(CreateCache(false).Accessor);
            first["future"]["nested"][0]["value"] = "destination changed";
            var second = snapshot.Restore(CreateCache(false).Accessor);

            // Assert
            Assert.AreEqual("original", second["future"]["nested"][0]["value"].GetValue<string>());
            AssertJsonEqual(expected, snapshot.SerializeMsalV3());
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void RestoreRetainsDestinationMetadataLikeV3(bool applicationCache)
        {
            // Arrange
            var source = CreateCache(applicationCache);
            Populate(source, applicationCache);
            var destination = CreateCache(applicationCache);
            var legacy = CreateCache(applicationCache);
            foreach (var cache in new[] { destination, legacy })
            {
                cache.Accessor.SaveAppMetadata(new MsalAppMetadataCacheItem("other-client", "other-environment", "1"));
            }

            // Act
            ((ITokenCacheSnapshotProvider)destination).RestoreSnapshot(((ITokenCacheSnapshotProvider)source).CreateSnapshot());
            legacy.DeserializeMsalV3(source.SerializeMsalV3(), true);

            // Assert
            Assert.HasCount(2, destination.Accessor.GetAllAppMetadata());
            AssertJsonEqual(legacy.SerializeMsalV3(), destination.SerializeMsalV3());

            // Act
            ((ITokenCacheSnapshotProvider)destination).RestoreSnapshot(null);
            legacy.DeserializeMsalV3(null, true);

            // Assert
            Assert.HasCount(2, destination.Accessor.GetAllAppMetadata());
            Assert.IsEmpty(destination.Accessor.GetAllAccessTokens());
            Assert.IsEmpty(destination.Accessor.GetAllRefreshTokens());
            Assert.IsEmpty(destination.Accessor.GetAllIdTokens());
            Assert.IsEmpty(destination.Accessor.GetAllAccounts());
            AssertJsonEqual(legacy.SerializeMsalV3(), destination.SerializeMsalV3());
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void EmptySnapshotReplacesTokensLikeV3(bool applicationCache)
        {
            // Arrange
            var empty = CreateCache(applicationCache);
            var destination = CreateCache(applicationCache);
            Populate(destination, applicationCache);
            var legacy = CreateCache(applicationCache);
            legacy.DeserializeMsalV3(destination.SerializeMsalV3(), true);

            // Act
            ((ITokenCacheSnapshotProvider)destination).RestoreSnapshot(((ITokenCacheSnapshotProvider)empty).CreateSnapshot());
            legacy.DeserializeMsalV3(empty.SerializeMsalV3(), true);

            // Assert
            Assert.IsEmpty(destination.Accessor.GetAllAccessTokens());
            AssertJsonEqual(legacy.SerializeMsalV3(), destination.SerializeMsalV3());
        }

        [TestMethod]
        public void FamilyRefreshTokensAndUnknownItemFieldsSurvive()
        {
            // Arrange
            var source = CreateCache(false);
            Populate(source, false);
            source.Accessor.SaveRefreshToken(new MsalRefreshTokenCacheItem(
                "environment", "other-client", "synthetic-family-token", null, "1", "account"));
            var json = JsonNode.Parse(source.SerializeMsalV3());
            foreach (var category in new[] { "RefreshToken", "IdToken", "Account", "AppMetadata" })
            {
                foreach (var entry in json[category].AsObject())
                {
                    entry.Value["future_item"] = JsonNode.Parse("{\"nested\":[null,true,{\"value\":123456789012345678901234567890}]}");
                }
            }
            source.DeserializeMsalV3(Encoding.UTF8.GetBytes(json.ToJsonString()), true);
            byte[] expected = source.SerializeMsalV3();

            // Act
            var snapshot = ((ITokenCacheSnapshotProvider)source).CreateSnapshot();
            var destination = CreateCache(false);
            ((ITokenCacheSnapshotProvider)destination).RestoreSnapshot(snapshot);

            // Assert
            Assert.HasCount(2, destination.Accessor.GetAllRefreshTokens());
            Assert.AreEqual("1", destination.Accessor.GetAllRefreshTokens().Single(token => token.IsFRT).FamilyId);
            AssertJsonEqual(expected, snapshot.SerializeMsalV3());
            AssertJsonEqual(expected, destination.SerializeMsalV3());
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void InternalCacheOptionsRejectSnapshotOperations(bool applicationCache)
        {
            // Arrange
            var source = CreateCache(applicationCache);
            var snapshot = ((ITokenCacheSnapshotProvider)source).CreateSnapshot();
            var app = ConfidentialClientApplicationBuilder.Create("snapshot-tests")
                .WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
                .Build();
            var provider = (ITokenCacheSnapshotProvider)(applicationCache ? app.AppTokenCache : app.UserTokenCache);

            // Act / Assert
            var captureError = Assert.ThrowsExactly<MsalClientException>(() => provider.CreateSnapshot());
            Assert.AreEqual(MsalError.StaticCacheWithExternalSerialization, captureError.ErrorCode);
            var restoreError = Assert.ThrowsExactly<MsalClientException>(() => provider.RestoreSnapshot(snapshot));
            Assert.AreEqual(MsalError.StaticCacheWithExternalSerialization, restoreError.ErrorCode);
            var clearError = Assert.ThrowsExactly<MsalClientException>(() => provider.RestoreSnapshot(null));
            Assert.AreEqual(MsalError.StaticCacheWithExternalSerialization, clearError.ErrorCode);
        }

        [TestMethod]
        public void PreEpochTimestampsMatchV3Truncation()
        {
            // Arrange
            var source = CreateCache(true);
            var time = DateTimeOffset.FromUnixTimeSeconds(-1).AddMilliseconds(500);
            source.Accessor.SaveAccessToken(new MsalAccessTokenCacheItem(
                "environment", "snapshot-tests", "scope", "tenant", "synthetic-token",
                time, time, time, null, null, refreshOn: time));
            var legacy = CreateCache(true);
            legacy.DeserializeMsalV3(source.SerializeMsalV3(), true);

            // Act
            var snapshot = ((ITokenCacheSnapshotProvider)source).CreateSnapshot();
            var destination = CreateCache(true);
            ((ITokenCacheSnapshotProvider)destination).RestoreSnapshot(snapshot);

            // Assert
            AssertJsonEqual(legacy.SerializeMsalV3(), snapshot.SerializeMsalV3());
            AssertJsonEqual(legacy.SerializeMsalV3(), destination.SerializeMsalV3());
        }

        private static ITokenCacheInternal CreateCache(bool applicationCache)
        {
            var app = ConfidentialClientApplicationBuilder.Create("snapshot-tests").Build();
            return (ITokenCacheInternal)(applicationCache ? app.AppTokenCache : app.UserTokenCache);
        }

        [TestMethod]
        public void EmptyExtensionsAndDefaultsMatchJsonRoundTrip()
        {
            // Arrange
            var source = CreateCache(true);
            Populate(source, true);
            var json = JsonNode.Parse(source.SerializeMsalV3());
            var token = json["AccessToken"].AsObject().First().Value;
            token["cache_extensions"] = new JsonObject();
            token["token_type"] = "bearer";
            token["refresh_on"] = "0";
            source.DeserializeMsalV3(Encoding.UTF8.GetBytes(json.ToJsonString()), true);
            byte[] expected = source.SerializeMsalV3();

            // Act
            var snapshot = ((ITokenCacheSnapshotProvider)source).CreateSnapshot();
            var target = CreateCache(true);
            ((ITokenCacheSnapshotProvider)target).RestoreSnapshot(snapshot);

            // Assert
            AssertJsonEqual(expected, snapshot.SerializeMsalV3());
            AssertJsonEqual(expected, target.SerializeMsalV3());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task AppAcquisitionReturnsCachedTokenWithoutNetwork(bool useSnapshot)
        {
            // Arrange
            const string Tenant = "11111111-1111-1111-1111-111111111111";
            const string Scope = "https://resource/.default";
            var app = ConfidentialClientApplicationBuilder.Create("snapshot-tests")
                .WithClientSecret("synthetic-test-credential")
                .WithAuthority($"https://login.microsoftonline.com/{Tenant}")
                .WithInstanceDiscovery(false)
                .WithHttpClientFactory(new NoNetworkFactory())
                .Build();
            var cache = (ITokenCacheInternal)app.AppTokenCache;
            var now = DateTimeOffset.UtcNow;
            cache.Accessor.SaveAccessToken(new MsalAccessTokenCacheItem(
                "login.microsoftonline.com", "snapshot-tests", Scope, Tenant, "synthetic-access-token",
                now, now.AddHours(1), now.AddHours(2), null, ""));
            var snapshot = ((ITokenCacheSnapshotProvider)cache).CreateSnapshot();
            byte[] bytes = cache.SerializeMsalV3();
            cache.Accessor.Clear();
            app.AppTokenCache.SetBeforeAccess(args =>
            {
                if (useSnapshot)
                {
                    ((ITokenCacheSnapshotProvider)args.TokenCache).RestoreSnapshot(snapshot);
                }
                else
                {
                    args.TokenCache.DeserializeMsalV3(bytes, true);
                }
            });

            // Act
            var result = await app.AcquireTokenForClient(new[] { Scope }).ExecuteAsync().ConfigureAwait(false);

            // Assert
            Assert.AreEqual("synthetic-access-token", result.AccessToken);
            Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(Tenant, result.TenantId);
        }

        [TestMethod]
        public async Task CacheCallbacksCaptureWritesAndRestoreForNextAcquisition()
        {
            // Arrange
            using var httpManager = new MockHttpManager();
            httpManager.AddInstanceDiscoveryMockHandler();
            httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
            var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithClientSecret(TestConstants.ClientSecret)
                .WithAuthority(TestConstants.AuthorityUtidTenant)
                .WithHttpManager(httpManager)
                .Build();
            TokenCacheSnapshot stored = null;
            int writes = 0;
            int reads = 0;
            app.AppTokenCache.SetBeforeAccess(args =>
            {
                reads++;
                ((ITokenCacheSnapshotProvider)args.TokenCache).RestoreSnapshot(stored);
            });
            app.AppTokenCache.SetAfterAccess(args =>
            {
                if (args.HasStateChanged)
                {
                    writes++;
                    stored = ((ITokenCacheSnapshotProvider)args.TokenCache).CreateSnapshot();
                }
            });

            // Act
            var first = await app.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync().ConfigureAwait(false);

            // Assert
            Assert.AreEqual(TokenSource.IdentityProvider, first.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(1, writes);
            Assert.AreEqual(2, reads);
            Assert.IsNotNull(stored);
            var persisted = CreateCache(true);
            persisted.DeserializeMsalV3(stored.SerializeMsalV3(), true);
            Assert.HasCount(1, persisted.Accessor.GetAllAccessTokens());

            // Act
            ((ITokenCacheInternal)app.AppTokenCache).Accessor.Clear();
            var second = await app.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync().ConfigureAwait(false);

            // Assert
            Assert.AreEqual(TokenSource.Cache, second.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(first.AccessToken, second.AccessToken);
            Assert.AreEqual(3, reads);
            Assert.AreEqual(1, writes);
            Assert.HasCount(1, ((ITokenCacheInternal)app.AppTokenCache).Accessor.GetAllAccessTokens());
            Assert.AreEqual(0, httpManager.QueueSize);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task SilentUserAcquisitionRestoresCacheWithoutNetwork(bool useSnapshot)
        {
            // Arrange
            var app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithAuthority(TestConstants.AuthorityUtidTenant)
                .WithInstanceDiscovery(false)
                .WithLegacyCacheCompatibility(false)
                .WithHttpClientFactory(new NoNetworkFactory())
                .Build();
            var cache = (ITokenCacheInternal)app.UserTokenCache;
            TokenCacheHelper.PopulateCache(cache.Accessor,
                environment: TestConstants.ProductionPrefNetworkEnvironment, addSecondAt: false);
            cache.Accessor.GetAllAccessTokens().Single().Secret = "cached-user-token";
            var store = new UserFlowCacheStore(app.UserTokenCache, useSnapshot);
            store.Save(TestConstants.HomeAccountId, cache);
            byte[] expected = store.Export(TestConstants.HomeAccountId);
            var account = new Account(TestConstants.HomeAccountId, TestConstants.DisplayableId,
                TestConstants.ProductionPrefNetworkEnvironment);

            // Act / Assert
            for (int i = 0; i < 2; i++)
            {
                cache.Accessor.Clear();
                var result = await app.AcquireTokenSilent(TestConstants.s_scope, account)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual("cached-user-token", result.AccessToken);
                Assert.AreEqual(TestConstants.HomeAccountId, result.Account.HomeAccountId.Identifier);
                Assert.AreEqual(TestConstants.Utid, result.TenantId);
                CollectionAssert.AreEquivalent(TestConstants.s_scope.ToArray(), result.Scopes.ToArray());
                Assert.HasCount(1, cache.Accessor.GetAllAccessTokens());
                Assert.HasCount(1, cache.Accessor.GetAllRefreshTokens());
                Assert.HasCount(1, cache.Accessor.GetAllIdTokens());
                Assert.HasCount(1, cache.Accessor.GetAllAccounts());
                AssertJsonEqual(expected, cache.SerializeMsalV3());
                Assert.HasCount(i + 1, store.Reads);
                Assert.AreEqual(TestConstants.HomeAccountId, store.Reads.Last());
                Assert.AreEqual(0, store.Writes);
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task SilentRefreshPersistsRotatedTokenAndRestoresNextCacheHit(bool useSnapshot)
        {
            // Arrange
            using var httpManager = new MockHttpManager();
            httpManager.AddInstanceDiscoveryMockHandler();
            httpManager.AddSuccessTokenResponseMockHandlerForPost(
                authority: TestConstants.AuthorityUtidTenant,
                bodyParameters: new Dictionary<string, string>
                {
                    [OAuth2Parameter.GrantType] = OAuth2GrantType.RefreshToken,
                    [OAuth2Parameter.RefreshToken] = TestConstants.RTSecret
                },
                responseMessage: MockHelpers.CreateSuccessTokenResponseMessage(
                    accessToken: "renewed-user-token", refreshToken: "rotated-refresh-token"));
            var app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithAuthority(TestConstants.AuthorityUtidTenant)
                .WithLegacyCacheCompatibility(false)
                .WithHttpManager(httpManager)
                .Build();
            var cache = (ITokenCacheInternal)app.UserTokenCache;
            TokenCacheHelper.PopulateCache(cache.Accessor, expiredAccessTokens: true, addSecondAt: false);
            var store = new UserFlowCacheStore(app.UserTokenCache, useSnapshot);
            store.Save(TestConstants.HomeAccountId, cache);
            byte[] original = store.Export(TestConstants.HomeAccountId);
            var account = new Account(TestConstants.HomeAccountId, TestConstants.DisplayableId,
                TestConstants.ProductionPrefCacheEnvironment);
            cache.Accessor.Clear();

            // Act
            var refreshed = await app.AcquireTokenSilent(TestConstants.s_scope, account)
                .ExecuteAsync().ConfigureAwait(false);

            // Assert
            Assert.AreEqual(TokenSource.IdentityProvider, refreshed.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual("renewed-user-token", refreshed.AccessToken);
            Assert.AreEqual(TestConstants.HomeAccountId, refreshed.Account.HomeAccountId.Identifier);
            Assert.AreEqual(TestConstants.Utid, refreshed.TenantId);
            CollectionAssert.AreEquivalent(TestConstants.s_scope.ToArray(), refreshed.Scopes.ToArray());
            Assert.AreEqual(0, httpManager.QueueSize);
            Assert.AreEqual(1, store.Writes);
            var persisted = CreateCache(false);
            persisted.DeserializeMsalV3(store.Export(TestConstants.HomeAccountId), true);
            Assert.AreEqual("renewed-user-token", persisted.Accessor.GetAllAccessTokens().Single().Secret);
            Assert.AreEqual("rotated-refresh-token", persisted.Accessor.GetAllRefreshTokens().Single().Secret);
            Assert.HasCount(1, persisted.Accessor.GetAllIdTokens());
            Assert.HasCount(1, persisted.Accessor.GetAllAccounts());
            var previous = CreateCache(false);
            previous.DeserializeMsalV3(original, true);
            Assert.AreEqual(TestConstants.RTSecret, previous.Accessor.GetAllRefreshTokens().Single().Secret);
            int readsBeforeCacheHit = store.Reads.Count;
            cache.Accessor.Clear();

            // Act
            var cached = await app.AcquireTokenSilent(TestConstants.s_scope, account)
                .ExecuteAsync().ConfigureAwait(false);

            // Assert
            Assert.AreEqual(TokenSource.Cache, cached.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(refreshed.AccessToken, cached.AccessToken);
            Assert.HasCount(readsBeforeCacheHit + 1, store.Reads);
            Assert.IsTrue(store.Reads.All(key => key == TestConstants.HomeAccountId));
            Assert.AreEqual(1, store.Writes);
            Assert.AreEqual("rotated-refresh-token", cache.Accessor.GetAllRefreshTokens().Single().Secret);
            Assert.AreEqual(0, httpManager.QueueSize);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task OboCallbacksKeepAssertionPartitionsIsolated(bool useSnapshot)
        {
            // Arrange
            using var httpManager = new MockHttpManager();
            httpManager.AddInstanceDiscoveryMockHandler();
            var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithClientSecret(TestConstants.ClientSecret)
                .WithAuthority(TestConstants.AuthorityUtidTenant)
                .WithLegacyCacheCompatibility(false)
                .WithHttpManager(httpManager)
                .Build();
            var cache = (ITokenCacheInternal)app.UserTokenCache;
            var store = new UserFlowCacheStore(app.UserTokenCache, useSnapshot);
            var assertions = new[] { new UserAssertion("assertion-a"), new UserAssertion("assertion-b") };
            var tokens = new[] { "obo-token-a", "obo-token-b" };

            // Act / Assert
            for (int i = 0; i < assertions.Length; i++)
            {
                httpManager.AddSuccessTokenResponseMockHandlerForPost(
                    authority: TestConstants.AuthorityUtidTenant,
                    bodyParameters: new Dictionary<string, string>
                    {
                        [OAuth2Parameter.GrantType] = OAuth2GrantType.JwtBearer,
                        [OAuth2Parameter.Assertion] = assertions[i].Assertion
                    },
                    responseMessage: MockHelpers.CreateSuccessTokenResponseMessage(accessToken: tokens[i]));
                cache.Accessor.Clear();
                var result = await app.AcquireTokenOnBehalfOf(TestConstants.s_scope, assertions[i])
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(tokens[i], result.AccessToken);
                Assert.AreEqual(i + 1, store.Writes);
                Assert.AreEqual(assertions[i].AssertionHash, store.Reads.Last());
                var persisted = CreateCache(false);
                persisted.DeserializeMsalV3(store.Export(assertions[i].AssertionHash), true);
                var token = persisted.Accessor.GetAllAccessTokens().Single();
                Assert.AreEqual(tokens[i], token.Secret);
                Assert.AreEqual(assertions[i].AssertionHash, token.OboCacheKey);
                Assert.IsEmpty(persisted.Accessor.GetAllRefreshTokens());
                Assert.AreEqual(0, httpManager.QueueSize);
            }

            foreach (int partition in new[] { 0, 1, 0 })
            {
                cache.Accessor.Clear();
                int readsBefore = store.Reads.Count;
                var result = await app.AcquireTokenOnBehalfOf(TestConstants.s_scope, assertions[partition])
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(tokens[partition], result.AccessToken);
                Assert.AreEqual(TestConstants.Utid, result.TenantId);
                CollectionAssert.AreEquivalent(TestConstants.s_scope.ToArray(), result.Scopes.ToArray());
                Assert.HasCount(readsBefore + 1, store.Reads);
                Assert.AreEqual(assertions[partition].AssertionHash, store.Reads.Last());
                Assert.AreEqual(2, store.Writes);
                var token = cache.Accessor.GetAllAccessTokens().Single();
                Assert.AreEqual(tokens[partition], token.Secret);
                Assert.AreEqual(assertions[partition].AssertionHash, token.OboCacheKey);
                Assert.AreEqual(0, httpManager.QueueSize);
            }
        }

        private sealed class UserFlowCacheStore
        {
            private readonly bool _useSnapshot;
            private readonly Dictionary<string, TokenCacheSnapshot> _snapshots = new Dictionary<string, TokenCacheSnapshot>(StringComparer.Ordinal);
            private readonly Dictionary<string, byte[]> _bytes = new Dictionary<string, byte[]>(StringComparer.Ordinal);

            public List<string> Reads { get; } = new List<string>();
            public int Writes { get; private set; }

            public UserFlowCacheStore(ITokenCache cache, bool useSnapshot)
            {
                _useSnapshot = useSnapshot;
                cache.SetBeforeAccess(args =>
                {
                    Assert.IsFalse(args.IsApplicationCache);
                    Assert.IsFalse(string.IsNullOrEmpty(args.SuggestedCacheKey));
                    Reads.Add(args.SuggestedCacheKey);
                    if (_useSnapshot)
                    {
                        _snapshots.TryGetValue(args.SuggestedCacheKey, out var snapshot);
                        ((ITokenCacheSnapshotProvider)args.TokenCache).RestoreSnapshot(snapshot);
                    }
                    else
                    {
                        _bytes.TryGetValue(args.SuggestedCacheKey, out var bytes);
                        args.TokenCache.DeserializeMsalV3(bytes, true);
                    }
                });
                cache.SetAfterAccess(args =>
                {
                    if (args.HasStateChanged)
                    {
                        Writes++;
                        Save(args.SuggestedCacheKey, args.TokenCache);
                    }
                });
            }

            public void Save(string key, ITokenCacheSerializer cache)
            {
                if (_useSnapshot)
                {
                    _snapshots[key] = ((ITokenCacheSnapshotProvider)cache).CreateSnapshot();
                }
                else
                {
                    _bytes[key] = cache.SerializeMsalV3();
                }
            }

            public byte[] Export(string key)
            {
                return _useSnapshot ? _snapshots[key].SerializeMsalV3() : _bytes[key];
            }
        }

        private sealed class NoNetworkFactory : IMsalHttpClientFactory
        {
            public HttpClient GetHttpClient()
            {
                throw new InvalidOperationException("A warm-cache test must not access the network.");
            }
        }

        private static void Populate(ITokenCache cache, bool applicationCache, string tenant = "tenant")
        {
            var accessor = ((ITokenCacheInternal)cache).Accessor;
            var time = DateTimeOffset.FromUnixTimeSeconds(1800000000).AddTicks(1234);
            var at = new MsalAccessTokenCacheItem(
                "environment", "snapshot-tests", "scope-a scope-b", tenant, "synthetic-access-token",
                time, time.AddHours(1), time.AddHours(2), null, "account",
                "key", time.AddMinutes(30), "pop", "obo",
                new SortedList<string, string> { ["partition"] = "one" })
            {
                AdditionalFieldsJson = "{\"future_item\":{\"values\":[1,2]}}",
                IsExtendedLifeTimeToken = true
            };
            at.AddAdditionalCacheParameters(new Dictionary<string, string> { ["extra"] = "value" });
            accessor.SaveAccessToken(at);
            accessor.SaveAppMetadata(new MsalAppMetadataCacheItem("snapshot-tests", "environment", null));
            if (!applicationCache)
            {
                accessor.SaveRefreshToken(new MsalRefreshTokenCacheItem(
                    "environment", "snapshot-tests", "synthetic-refresh-token", null, null, "account",
                    new SortedList<string, string> { ["partition"] = "one" }) { OboCacheKey = "obo" });
                accessor.SaveIdToken(new MsalIdTokenCacheItem(
                    "environment", "snapshot-tests", "synthetic-id-token", null, "account", tenant));
                accessor.SaveAccount(new MsalAccountCacheItem(
                    "environment", "local", null, "account", null, null, null, tenant, null, null,
                    new Dictionary<string, string> { ["app"] = "wam" }));
            }
        }

        private static void Mutate(ITokenCache cache)
        {
            var accessor = ((ITokenCacheInternal)cache).Accessor;
            var at = accessor.GetAllAccessTokens().Single();
            at.Secret = "changed";
            at.ScopeSet.Add("injected");
            at.AdditionalCacheKeyComponents["partition"] = "changed";
            at.PersistedCacheParameters["extra"] = "changed";
            at.IsExtendedLifeTimeToken = true;
            accessor.GetAllRefreshTokens().Single().AdditionalCacheKeyComponents["partition"] = "changed";
            accessor.GetAllAccounts().Single().WamAccountIds["app"] = "changed";
            accessor.GetAllIdTokens().Single().Secret = "changed";
        }

        private static void AssertSnapshotClone<T>(
            T source, T clone, string[] expectedProperties, string[] expectedFields)
            where T : MsalItemWithAdditionalFields
        {
            const BindingFlags InstanceMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var properties = new List<PropertyInfo>();
            var fields = new List<FieldInfo>();
            for (var type = typeof(T); type != typeof(object); type = type.BaseType)
            {
                properties.AddRange(type.GetProperties(InstanceMembers | BindingFlags.DeclaredOnly));
                fields.AddRange(type.GetFields(InstanceMembers | BindingFlags.DeclaredOnly)
                    .Where(field => !field.IsDefined(typeof(CompilerGeneratedAttribute), false)));
            }

            Assert.AreNotSame(source, clone);
            CollectionAssert.AreEquivalent(expectedProperties, properties.Select(property => property.Name).ToArray(),
                $"Update the fixture and clone assertions whenever a {typeof(T).Name} property is added.");
            CollectionAssert.AreEquivalent(expectedFields, fields.Select(field => field.Name).ToArray(),
                $"Update the fixture and clone assertions whenever a {typeof(T).Name} field is added.");

            foreach (var property in properties)
            {
                object expected = property.GetValue(source);
                object actual = property.GetValue(clone);
                if (source is MsalAccessTokenCacheItem &&
                    property.Name == nameof(MsalAccessTokenCacheItem.IsExtendedLifeTimeToken))
                {
                    // Request-only state is deliberately not persisted in a snapshot.
                    Assert.IsTrue((bool)expected);
                    Assert.IsFalse((bool)actual);
                }
                else
                {
                    AssertSnapshotMemberEqual(expected, actual, $"{typeof(T).Name}.{property.Name}");
                }
            }

            foreach (var field in fields)
            {
                AssertSnapshotMemberEqual(field.GetValue(source), field.GetValue(clone), $"{typeof(T).Name}.{field.Name}");
            }
        }

        private static void AssertSnapshotMemberEqual(object expected, object actual, string member)
        {
            if (expected is HashSet<string> scopes)
            {
                Assert.AreNotSame(expected, actual, member);
                CollectionAssert.AreEquivalent(scopes.ToArray(), ((HashSet<string>)actual).ToArray(), member);
            }
            else if (expected is IEnumerable<string> strings)
            {
                Assert.AreNotSame(expected, actual, member);
                CollectionAssert.AreEqual(strings.ToArray(), ((IEnumerable<string>)actual).ToArray(), member);
            }
            else if (expected is IEnumerable<KeyValuePair<string, string>> entries)
            {
                Assert.AreNotSame(expected, actual, member);
                CollectionAssert.AreEquivalent(entries.ToArray(),
                    ((IEnumerable<KeyValuePair<string, string>>)actual).ToArray(), member);
            }
            else if (expected is IiOSKey key)
            {
                var clonedKey = (IiOSKey)actual;
                Assert.AreNotSame(key, clonedKey, member);
                Assert.AreEqual(key.iOSAccount, clonedKey.iOSAccount, member);
                Assert.AreEqual(key.iOSGeneric, clonedKey.iOSGeneric, member);
                Assert.AreEqual(key.iOSService, clonedKey.iOSService, member);
                Assert.AreEqual(key.iOSType, clonedKey.iOSType, member);
            }
            else if (expected is IdToken token)
            {
                var clonedToken = (IdToken)actual;
                Assert.AreNotSame(token, clonedToken, member);
                Assert.AreNotSame(token.ClaimsPrincipal, clonedToken.ClaimsPrincipal, member);
                CollectionAssert.AreEquivalent(
                    token.ClaimsPrincipal.Claims.Select(claim =>
                        (claim.Type, claim.Value, claim.ValueType, claim.Issuer, claim.OriginalIssuer)).ToArray(),
                    clonedToken.ClaimsPrincipal.Claims.Select(claim =>
                        (claim.Type, claim.Value, claim.ValueType, claim.Issuer, claim.OriginalIssuer)).ToArray(), member);
            }
            else if (expected is Lazy<IiOSKey> || expected is Lazy<IdToken>)
            {
                Assert.AreNotSame(expected, actual, member);
            }
            else
            {
                Assert.AreEqual(expected, actual, member);
            }
        }

        private static void AssertJsonEqual(byte[] expected, byte[] actual)
        {
            JsonTestUtils.AssertJsonDeepEquals(Encoding.UTF8.GetString(expected), Encoding.UTF8.GetString(actual));
        }
    }
}
