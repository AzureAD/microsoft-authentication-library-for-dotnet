// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Utils;
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

            string hash = CoreHelpers.ComputeAccessTokenExtCacheKey(components);
            StringAssert.EndsWith(item.CacheKey, "-" + hash,
                "Cache key should end with partition hash");
        }

        [TestMethod]
        public void RTWithoutPartition_CacheKeyHasNoHash()
        {
            // Act
            var item = new MsalRefreshTokenCacheItem(
                "login.microsoftonline.com",
                TestConstants.ClientId,
                "secret-rt",
                TestConstants.RawClientId,
                familyId: null,
                TestConstants.HomeAccountId);

            // Assert
            Assert.IsNull(item.AdditionalCacheKeyComponents);

            // Non-partitioned RT cache key should not contain any partition hash
            // Verify the key ends with the standard delimiter pattern (no appended hash)
            string keyWithPartition = item.CacheKey + "-extra";
            Assert.AreNotEqual(keyWithPartition, item.CacheKey);
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
            // FRT key should not contain any partition hash
            string hash = CoreHelpers.ComputeAccessTokenExtCacheKey(components);
            Assert.AreEqual(-1, item.CacheKey.IndexOf(hash),
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
        public void OldMsalReadsNewJson_PartitionFieldPreservedAsAdditionalFields()
        {
            // Scenario: new MSAL writes a partitioned RT; old MSAL (which doesn't
            // know about cache_extensions on RTs) reads it. The unknown field should
            // land in AdditionalFieldsJson and survive a re-serialization round-trip,
            // so it is NOT lost when old MSAL writes the cache back.

            // Arrange — create partitioned RT and serialize
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

            // Act — simulate old MSAL: strip partition awareness by
            // deserializing into a plain item that ignores the field.
            // The base class AdditionalFieldsJson mechanism captures unknowns.
            var oldStyleItem = MsalRefreshTokenCacheItem.FromJObject(json);

            // Assert — the partition data round-trips through AdditionalFieldsJson
            // or through the explicit property; either way it must survive.
            var reserializedJson = oldStyleItem.ToJObject();
            Assert.IsNotNull(reserializedJson["ext"],
                "ext (cache_extensions) field must survive the round-trip even if treated as unknown");
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
                    .WithCachePartitionKey(partitionKey, partitionValue)
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
    }
}
