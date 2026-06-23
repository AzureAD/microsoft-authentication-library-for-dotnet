// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CacheTests
{
    [TestClass]
    public class RefreshTokenCachePartitionTests
    {
        [TestMethod]
        public void RTWithPartition_CacheKeyIncludesHash()
        {
            // Arrange
            var components = new SortedList<string, string>
            {
                { "pk", "pv" }
            };

            // Act
            var item = new MsalRefreshTokenCacheItem(
                "login.microsoftonline.com",
                TestConstants.ClientId,
                "secret-rt",
                TestConstants.RawClientId,
                familyId: null,
                TestConstants.HomeAccountId,
                components);

            // Assert
            Assert.IsNotNull(item.AdditionalCacheKeyComponents);
            Assert.HasCount(1, item.AdditionalCacheKeyComponents);
            Assert.AreEqual("pv", item.AdditionalCacheKeyComponents["pk"]);

            // The hash is passed through GetCredentialKey which lower-cases the entire key
            string hash = CoreHelpers.ComputeAccessTokenExtCacheKey(components).ToLowerInvariant();
            StringAssert.EndsWith(item.CacheKey, "-" + hash,
                "Cache key should end with lower-cased partition hash");

            // The entire cache key must be lower-case (consistent with MSAL convention)
            Assert.AreEqual(item.CacheKey, item.CacheKey.ToLowerInvariant(),
                "Cache key must be fully lower-cased");
        }

        [TestMethod]
        public void RTWithoutPartition_CacheKeyHasNoHash()
        {
            // Arrange
            var components = new SortedList<string, string> { { "pk", "pv" } };

            // Act
            var nonPartitioned = new MsalRefreshTokenCacheItem(
                "login.microsoftonline.com",
                TestConstants.ClientId,
                "secret-rt",
                TestConstants.RawClientId,
                familyId: null,
                TestConstants.HomeAccountId);

            var partitioned = new MsalRefreshTokenCacheItem(
                "login.microsoftonline.com",
                TestConstants.ClientId,
                "secret-rt",
                TestConstants.RawClientId,
                familyId: null,
                TestConstants.HomeAccountId,
                components);

            // Assert
            Assert.IsNull(nonPartitioned.AdditionalCacheKeyComponents);

            // Non-partitioned key must be shorter (no hash suffix)
            Assert.IsGreaterThan(nonPartitioned.CacheKey.Length, partitioned.CacheKey.Length,
                "Partitioned key should be longer than non-partitioned key");

            // The partition hash must not appear in the non-partitioned key
            string hash = CoreHelpers.ComputeAccessTokenExtCacheKey(components).ToLowerInvariant();
            Assert.DoesNotContain(hash, nonPartitioned.CacheKey,
                "Non-partitioned key must not contain the partition hash");
        }

        [TestMethod]
        public void FRT_NeverGetsPartitioned()
        {
            // Arrange
            var components = new SortedList<string, string>
            {
                { "pk", "pv" }
            };

            // Act — create an FRT with partition components
            var item = new MsalRefreshTokenCacheItem(
                "login.microsoftonline.com",
                TestConstants.ClientId,
                "secret-rt",
                TestConstants.RawClientId,
                familyId: "1",
                TestConstants.HomeAccountId,
                components);

            // Assert — partition should be ignored for FRTs
            Assert.IsNull(item.AdditionalCacheKeyComponents);
            Assert.IsTrue(item.IsFRT);

            // FRT key should not contain any partition hash (case-insensitive check
            // since both the hash and the key are lower-cased by convention)
            string hash = CoreHelpers.ComputeAccessTokenExtCacheKey(components).ToLowerInvariant();
            Assert.DoesNotContain(hash, item.CacheKey,
                "FRT cache key must not contain partition hash");
        }

        [TestMethod]
        public void RTPartition_SerializationRoundTrip()
        {
            // Arrange
            var components = new SortedList<string, string>
            {
                { "pk", "pv" }
            };

            var original = new MsalRefreshTokenCacheItem(
                "login.microsoftonline.com",
                TestConstants.ClientId,
                "secret-rt",
                TestConstants.RawClientId,
                familyId: null,
                TestConstants.HomeAccountId,
                components);

            // Act — round-trip through JSON
            string json = original.ToJsonString();
            var deserialized = MsalRefreshTokenCacheItem.FromJsonString(json);

            // Assert
            Assert.IsNotNull(deserialized.AdditionalCacheKeyComponents);
            Assert.HasCount(1, deserialized.AdditionalCacheKeyComponents);
            Assert.AreEqual("pv", deserialized.AdditionalCacheKeyComponents["pk"]);
            Assert.AreEqual(original.CacheKey, deserialized.CacheKey);
        }

        [TestMethod]
        public void RTPartition_DeserializationWithoutPartition_HasNoComponents()
        {
            // Arrange — create without partition
            var original = new MsalRefreshTokenCacheItem(
                "login.microsoftonline.com",
                TestConstants.ClientId,
                "secret-rt",
                TestConstants.RawClientId,
                familyId: null,
                TestConstants.HomeAccountId);

            // Act — round-trip through JSON
            string json = original.ToJsonString();
            var deserialized = MsalRefreshTokenCacheItem.FromJsonString(json);

            // Assert — backward compatible: no components
            Assert.IsNull(deserialized.AdditionalCacheKeyComponents);
            Assert.AreEqual(original.CacheKey, deserialized.CacheKey);
        }

        [TestMethod]
        public void FRT_SerializationRoundTrip_NoPartition()
        {
            // Arrange — FRT with components (should be ignored)
            var components = new SortedList<string, string>
            {
                { "pk", "pv" }
            };

            var original = new MsalRefreshTokenCacheItem(
                "login.microsoftonline.com",
                TestConstants.ClientId,
                "secret-rt",
                TestConstants.RawClientId,
                familyId: "1",
                TestConstants.HomeAccountId,
                components);

            // Act
            string json = original.ToJsonString();
            var deserialized = MsalRefreshTokenCacheItem.FromJsonString(json);

            // Assert — FRT never gets partition
            Assert.IsNull(deserialized.AdditionalCacheKeyComponents);
            Assert.IsTrue(deserialized.IsFRT);
        }

        [TestMethod]
        public void OldMsalReadsNewJson_PartitionFieldPreservedViaAdditionalFieldsJson()
        {
            // Scenario: new MSAL writes a partitioned RT. Old MSAL (which doesn't
            // know about the "ext" field on RTs) reads it. The unknown field lands
            // in AdditionalFieldsJson and must survive re-serialization.

            // Arrange — build JSON as new MSAL would write it
            var components = new SortedList<string, string> { { "pk", "pv" } };
            var partitioned = new MsalRefreshTokenCacheItem(
                "login.microsoftonline.com",
                TestConstants.ClientId,
                "secret-rt",
                TestConstants.RawClientId,
                familyId: null,
                TestConstants.HomeAccountId,
                components);

            var json = partitioned.ToJObject();

            // Verify the ext field is present in the serialized JSON
            Assert.IsNotNull(json["ext"], "New MSAL must serialize the ext field");

            // Act — simulate old MSAL (which doesn't know about the 'ext' field on RTs) by
            // renaming it so the current parser treats it as unknown and moves it into AdditionalFieldsJson.
            var extValue = json["ext"];
            json.Remove("ext");
            json["unknown_ext"] = extValue;

            var oldStyleItem = MsalRefreshTokenCacheItem.FromJObject(json);

            // Assert — the renamed field should be captured in AdditionalFieldsJson
            // because the parser doesn't know about it (simulating old MSAL not knowing "ext")
            Assert.IsNotNull(oldStyleItem.AdditionalFieldsJson,
                "Unknown fields must be captured in AdditionalFieldsJson");
            Assert.Contains("unknown_ext", oldStyleItem.AdditionalFieldsJson,
                "Renamed ext field must survive as an additional field");
            Assert.IsNull(oldStyleItem.AdditionalCacheKeyComponents,
                "Old MSAL must not populate partition components from renamed field");

            // Re-serialize and verify the unknown field is preserved
            var reserializedJson = oldStyleItem.ToJObject();
            Assert.IsNotNull(reserializedJson["unknown_ext"],
                "Unknown field must survive old MSAL round-trip via AdditionalFieldsJson");
        }

        [TestMethod]
        public void NewMsalReadsOldJson_MissingPartitionFieldYieldsNullComponents()
        {
            // Scenario: old MSAL wrote an RT without cache_extensions.
            // New MSAL reads it and should treat it as non-partitioned.

            // Arrange — manually build JSON without cache_extensions
            var json = new System.Text.Json.Nodes.JsonObject
            {
                ["home_account_id"] = TestConstants.HomeAccountId,
                ["environment"] = "login.microsoftonline.com",
                ["credential_type"] = "RefreshToken",
                ["client_id"] = TestConstants.ClientId,
                ["secret"] = "old-rt-secret",
                ["family_id"] = ""
            };

            // Act
            var item = MsalRefreshTokenCacheItem.FromJObject(json);

            // Assert
            Assert.IsNull(item.AdditionalCacheKeyComponents);
            Assert.AreEqual("old-rt-secret", item.Secret);
        }

        [TestMethod]
        public async Task AcquireTokenByAuthCode_WithPartition_StoresPartitionedRT_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                // Arrange
                const string partitionKey = "transfer_id";
                const string partitionValue = "abc123";

                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithAuthority(new System.Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(httpManager)
                    .WithInstanceDiscovery(false)
                    .BuildConcrete();

                httpManager.AddSuccessTokenResponseMockHandlerForPost();

                // Act — acquire with partition
                var result = await app.AcquireTokenByAuthorizationCode(TestConstants.s_scope, TestConstants.DefaultAuthorizationCode)
                    .WithCachePartitionKey(partitionKey, partitionValue, partitionRefreshToken: true)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                // Verify RT was stored with partition
                var rtItems = app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens();
                Assert.AreNotEqual(0, rtItems.Count, "At least one RT should be in cache");

                var partitionedRt = rtItems.FirstOrDefault(rt => rt.AdditionalCacheKeyComponents != null);
                Assert.IsNotNull(partitionedRt, "Expected a partitioned RT in the cache");
                Assert.AreEqual(partitionValue, partitionedRt.AdditionalCacheKeyComponents[partitionKey]);
            }
        }

        [TestMethod]
        public async Task AcquireTokenByAuthCode_WithoutPartition_StoresNonPartitionedRT_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                // Arrange
                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithAuthority(new System.Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(httpManager)
                    .WithInstanceDiscovery(false)
                    .BuildConcrete();

                httpManager.AddSuccessTokenResponseMockHandlerForPost();

                // Act — acquire without partition
                var result = await app.AcquireTokenByAuthorizationCode(TestConstants.s_scope, TestConstants.DefaultAuthorizationCode)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(result);

                var rtItems = app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens();
                Assert.AreNotEqual(0, rtItems.Count, "At least one RT should be in cache");
                Assert.IsTrue(rtItems.All(rt => rt.AdditionalCacheKeyComponents is null),
                    "No RT should have partition components when acquired without partition");
            }
        }

        [TestMethod]
        public async Task AcquireTokenSilent_WithPartition_FindsPartitionedRT_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                // Arrange
                const string partitionKey = "session_type";
                const string partitionValue = "transfer";

                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithAuthority(new System.Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(httpManager)
                    .WithInstanceDiscovery(false)
                    .BuildConcrete();

                // Acquire with partition to seed both AT and RT in cache
                httpManager.AddSuccessTokenResponseMockHandlerForPost();
                var result = await app.AcquireTokenByAuthorizationCode(TestConstants.s_scope, TestConstants.DefaultAuthorizationCode)
                    .WithCachePartitionKey(partitionKey, partitionValue, partitionRefreshToken: true)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                // Expire all ATs so silent must use the RT
                TokenCacheHelper.ExpireAllAccessTokens(app.UserTokenCacheInternal);

                // Mock the refresh token grant response
                var handler = httpManager.AddSuccessTokenResponseMockHandlerForPost(
                    TestConstants.AuthorityUtidTenant);
                handler.ExpectedPostData = new Dictionary<string, string>
                {
                    { OAuth2Parameter.GrantType, OAuth2GrantType.RefreshToken }
                };

                // Act: silent acquire with partition should find the partitioned RT
                var account = await app.GetAccountAsync(result.Account.HomeAccountId.Identifier).ConfigureAwait(false);
                var silentResult = await app.AcquireTokenSilent(TestConstants.s_scope, account)
                    .WithCachePartitionKey(partitionKey, partitionValue, partitionRefreshToken: true)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert: token came from IDP via RT refresh
                Assert.AreEqual(TokenSource.IdentityProvider, silentResult.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public async Task AcquireTokenSilent_WithoutRtPartition_FindsPartitionedRT_BackwardCompat_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                // Arrange: a partitioned RT exists in cache, but the silent caller
                // does not opt into RT partitioning. The RT filter should not engage,
                // so the partitioned RT is still found (backward compat).
                const string partitionKey = "session_type";
                const string partitionValue = "transfer";

                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithAuthority(new System.Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(httpManager)
                    .WithInstanceDiscovery(false)
                    .BuildConcrete();

                // Seed a partitioned RT
                httpManager.AddSuccessTokenResponseMockHandlerForPost();
                var result = await app.AcquireTokenByAuthorizationCode(TestConstants.s_scope, TestConstants.DefaultAuthorizationCode)
                    .WithCachePartitionKey(partitionKey, partitionValue, partitionRefreshToken: true)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Expire all ATs so silent must use the RT
                TokenCacheHelper.ExpireAllAccessTokens(app.UserTokenCacheInternal);

                // Mock the refresh token grant response
                var handler = httpManager.AddSuccessTokenResponseMockHandlerForPost(
                    TestConstants.AuthorityUtidTenant);
                handler.ExpectedPostData = new Dictionary<string, string>
                {
                    { OAuth2Parameter.GrantType, OAuth2GrantType.RefreshToken }
                };

                // Act: silent acquire WITHOUT partitionRefreshToken should still find the RT
                var account = await app.GetAccountAsync(result.Account.HomeAccountId.Identifier).ConfigureAwait(false);
                var silentResult = await app.AcquireTokenSilent(TestConstants.s_scope, account)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert: token came from IDP via RT refresh (filter was not engaged)
                Assert.AreEqual(TokenSource.IdentityProvider, silentResult.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public async Task AcquireTokenSilent_WithDifferentPartition_DoesNotFindPartitionedRT_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                // Arrange: only a partitioned RT exists in cache
                const string partitionKey = "session_type";
                const string partitionValue = "transfer";

                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithAuthority(new System.Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(httpManager)
                    .WithInstanceDiscovery(false)
                    .BuildConcrete();

                // Seed a partitioned RT
                httpManager.AddSuccessTokenResponseMockHandlerForPost();
                var result = await app.AcquireTokenByAuthorizationCode(TestConstants.s_scope, TestConstants.DefaultAuthorizationCode)
                    .WithCachePartitionKey(partitionKey, partitionValue, partitionRefreshToken: true)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Expire all ATs
                TokenCacheHelper.ExpireAllAccessTokens(app.UserTokenCacheInternal);

                // Act: silent acquire with partitionRefreshToken but a DIFFERENT value should NOT find it
                var account = await app.GetAccountAsync(result.Account.HomeAccountId.Identifier).ConfigureAwait(false);
                var ex = await AssertException.TaskThrowsAsync<MsalUiRequiredException>(
                    () => app.AcquireTokenSilent(TestConstants.s_scope, account)
                        .WithCachePartitionKey(partitionKey, "different_value", partitionRefreshToken: true)
                        .ExecuteAsync())
                    .ConfigureAwait(false);

                // Assert: interaction required because no matching RT was found
                Assert.AreEqual(MsalError.NoTokensFoundError, ex.ErrorCode);
            }
        }

        [TestMethod]
        public async Task AcquireTokenSilent_MixedCache_PartitionIsolatesCorrectRT_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                // Arrange: both a partitioned and non-partitioned RT in the same cache
                const string partitionKey = "session_type";
                const string partitionValue = "transfer";

                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithAuthority(new System.Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(httpManager)
                    .WithInstanceDiscovery(false)
                    .BuildConcrete();

                // Seed non-partitioned RT (regular OIDC session)
                httpManager.AddSuccessTokenResponseMockHandlerForPost();
                var regularResult = await app.AcquireTokenByAuthorizationCode(TestConstants.s_scope, TestConstants.DefaultAuthorizationCode)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Seed partitioned RT (transfer token session)
                httpManager.AddSuccessTokenResponseMockHandlerForPost();
                var partitionedResult = await app.AcquireTokenByAuthorizationCode(TestConstants.s_scope, "transfer-code")
                    .WithCachePartitionKey(partitionKey, partitionValue, partitionRefreshToken: true)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Verify both RTs exist
                var rtItems = app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens();
                var nonPartitioned = rtItems.Where(rt => rt.AdditionalCacheKeyComponents is null).ToList();
                var partitioned = rtItems.Where(rt => rt.AdditionalCacheKeyComponents != null).ToList();
                Assert.AreNotEqual(0, nonPartitioned.Count, "Non-partitioned RT expected");
                Assert.AreNotEqual(0, partitioned.Count, "Partitioned RT expected");

                // Expire all ATs so silent must go through RT
                TokenCacheHelper.ExpireAllAccessTokens(app.UserTokenCacheInternal);

                // Silent with partition: should use partitioned RT
                var partitionHandler = httpManager.AddSuccessTokenResponseMockHandlerForPost(
                    TestConstants.AuthorityUtidTenant);
                partitionHandler.ExpectedPostData = new Dictionary<string, string>
                {
                    { OAuth2Parameter.GrantType, OAuth2GrantType.RefreshToken }
                };

                var account = await app.GetAccountAsync(regularResult.Account.HomeAccountId.Identifier).ConfigureAwait(false);
                var silentPartitioned = await app.AcquireTokenSilent(TestConstants.s_scope, account)
                    .WithCachePartitionKey(partitionKey, partitionValue, partitionRefreshToken: true)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, silentPartitioned.AuthenticationResultMetadata.TokenSource);

                // Expire ATs again
                TokenCacheHelper.ExpireAllAccessTokens(app.UserTokenCacheInternal);

                // Silent without partition: should use non-partitioned RT
                var regularHandler = httpManager.AddSuccessTokenResponseMockHandlerForPost(
                    TestConstants.AuthorityUtidTenant);
                regularHandler.ExpectedPostData = new Dictionary<string, string>
                {
                    { OAuth2Parameter.GrantType, OAuth2GrantType.RefreshToken }
                };

                var silentRegular = await app.AcquireTokenSilent(TestConstants.s_scope, account)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, silentRegular.AuthenticationResultMetadata.TokenSource);
            }
        }
    }
}
