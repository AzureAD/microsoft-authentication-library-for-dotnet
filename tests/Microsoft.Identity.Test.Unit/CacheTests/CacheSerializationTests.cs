// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.Utils;
#if SUPPORTS_SYSTEM_TEXT_JSON
using System.Text.Json;
using System.Text.Json.Nodes;
using JObject = System.Text.Json.Nodes.JsonObject;
#else
using Microsoft.Identity.Json.Linq;
#endif
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.CacheTests
{
    [TestClass]
    public class CacheSerializationTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
        }

        private static readonly IEnumerable<string> s_appMetadataKeys = new[]
        {
            StorageJsonKeys.ClientId ,
            StorageJsonKeys.Environment,
            StorageJsonKeys.FamilyId
        };

#if SUPPORTS_SYSTEM_TEXT_JSON
        private static readonly JsonDocumentOptions _documentOptions = new JsonDocumentOptions
        {
            AllowTrailingCommas = true
        };
#endif

        private MsalAccessTokenCacheItem CreateAccessTokenItem(
            string kid = null,
            string tokenType = "Bearer",
            DateTimeOffset? cachedAt = null,
            DateTimeOffset? expiresOn = null,
            DateTimeOffset? extendedExpiresOn = null)
        {
            var item = new MsalAccessTokenCacheItem(
                preferredCacheEnv: "env",
                clientId: TestConstants.ClientId,
                scopes: TestConstants.ScopeStr,
                tenantId: "the_tenant_id",
                secret: "access_token_secret",
                cachedAt: cachedAt.HasValue ? cachedAt.Value : DateTimeOffset.UtcNow,
                expiresOn: expiresOn.HasValue ? expiresOn.Value : DateTimeOffset.UtcNow + TimeSpan.FromHours(1),
                extendedExpiresOn: extendedExpiresOn.HasValue ? extendedExpiresOn.Value : DateTimeOffset.UtcNow + TimeSpan.FromHours(1),
                rawClientInfo: string.Empty,
                homeAccountId: TestConstants.HomeAccountId,
                keyId: kid,
                tokenType: tokenType,
                oboCacheKey: "assertion_hash");

            return item;
        }

        private MsalRefreshTokenCacheItem CreateRefreshTokenItem(bool isFrt = false)
        {
            MsalRefreshTokenCacheItem item;

            if (isFrt)
            {
                item = new MsalRefreshTokenCacheItem
                {
                    ClientId = TestConstants.ClientId,
                    Environment = "env",
                    HomeAccountId = TestConstants.HomeAccountId,
                    Secret = "access_token_secret",
                    RawClientInfo = string.Empty
                };
            }
            else
            {
                item = new MsalRefreshTokenCacheItem
                {
                    ClientId = TestConstants.ClientId,
                    Environment = "env",
                    HomeAccountId = TestConstants.HomeAccountId,
                    Secret = "access_token_secret",
                    RawClientInfo = string.Empty,
                    OboCacheKey = "assertion_hash"
                };
            }

            item.InitCacheKey();
            return item;
        }

        private MsalIdTokenCacheItem CreateIdTokenItem()
        {
            var item = new MsalIdTokenCacheItem
            {
                ClientId = TestConstants.ClientId,
                Environment = "env",
                HomeAccountId = TestConstants.HomeAccountId,
                Secret = "access_token_secret",
                TenantId = "the_tenant_id",
                RawClientInfo = string.Empty,
            };

            item.InitCacheKey();
            return item;
        }

        private MsalAccountCacheItem CreateAccountItem()
        {
            var item = new MsalAccountCacheItem
            {
                Environment = "env",
                HomeAccountId = TestConstants.HomeAccountId,
                TenantId = "the_tenant_id",
                AuthorityType = "authority type",
                RawClientInfo = string.Empty,
                LocalAccountId = TestConstants.LocalAccountId,
                Name = TestConstants.Name,
                GivenName = TestConstants.GivenName,
                FamilyName = TestConstants.FamilyName,
                PreferredUsername = TestConstants.Username
            };

            item.InitCacheKey();
            return item;
        }

        private ITokenCacheAccessor CreateTokenCacheAccessor()
        {
            const int NumAccessTokens = 5;
            const int NumRefreshTokens = 3;
            const int NumIdTokens = 3;
            const int NumAccounts = 3;

            return CreateTokenCacheAccessorWithKeyPrefix(
                string.Empty,
                NumAccessTokens,
                NumRefreshTokens,
                NumIdTokens,
                NumAccounts);
        }

        private ITokenCacheAccessor CreateTokenCacheAccessorWithKeyPrefix(
            string keyPrefix,
            int numAccessTokens,
            int numRefreshTokens,
            int numIdTokens,
            int numAccounts)
        {
            var accessor = new InMemoryPartitionedUserTokenCacheAccessor(Substitute.For<ILoggerAdapter>(), null);

            for (int i = 1; i <= numAccessTokens; i++)
            {
                var item = CreateAccessTokenItem(
                    cachedAt: DateTimeHelpers.UnixTimestampToDateTime(34567),
                    expiresOn: DateTimeHelpers.UnixTimestampToDateTime(12345),
                    extendedExpiresOn: DateTimeHelpers.UnixTimestampToDateTime(23456));

                item.Environment += $"_{keyPrefix}{i}"; // ensure we get unique cache keys
                item.InitCacheKey();
                accessor.SaveAccessToken(item);
            }

            for (int i = 1; i <= numRefreshTokens; i++)
            {
                var item = CreateRefreshTokenItem();
                item.Environment += $"_{keyPrefix}{i}"; // ensure we get unique cache keys
                item.InitCacheKey();
                accessor.SaveRefreshToken(item);
            }

            // Create an FRT
            var frt = CreateRefreshTokenItem(true);
            frt.FamilyId = "1";
            frt.InitCacheKey();
            accessor.SaveRefreshToken(frt);

            for (int i = 1; i <= numIdTokens; i++)
            {
                var item = CreateIdTokenItem();
                item.Environment += $"_{keyPrefix}{i}"; // ensure we get unique cache keys
                item.InitCacheKey();
                accessor.SaveIdToken(item);
            }

            for (int i = 1; i <= numAccounts; i++)
            {
                var item = CreateAccountItem();
                item.Environment += $"_{keyPrefix}{i}"; // ensure we get unique cache keys
                item.InitCacheKey();
                accessor.SaveAccount(item);
            }

            accessor.SaveAppMetadata(new MsalAppMetadataCacheItem(TestConstants.ClientId, "env_1", "1"));
            accessor.SaveAppMetadata(new MsalAppMetadataCacheItem(TestConstants.ClientId, "env_2", ""));
            accessor.SaveAppMetadata(new MsalAppMetadataCacheItem(TestConstants.ClientId2, "env_1", "another_family"));

            return accessor;
        }

        #region ACCESS TOKEN TESTS

        [TestMethod]
        public void TestSerializeMsalAccessTokenCacheItem()
        {
            var item = CreateAccessTokenItem();
            Assert.AreEqual(StorageJsonValues.TokenTypeBearer, item.TokenType);

            string asJson = item.ToJsonString();
            var item2 = MsalAccessTokenCacheItem.FromJsonString(asJson);

            AssertAccessTokenCacheItemsAreEqual(item, item2);
        }

        [TestMethod]
        public void TestSerializeMsalAccessTokenCacheItemWithAdditionalFields()
        {
            var item = CreateAccessTokenItem();

            // Add an unknown field into the json
            var asJObject = item.ToJObject();
            asJObject["unsupported_field_name"] = "this is a value";

            // Ensure unknown field remains in the AdditionalFieldsJson block
            var item2 = MsalAccessTokenCacheItem.FromJObject(asJObject);
            Assert.AreEqual("{\r\n  \"unsupported_field_name\": \"this is a value\"\r\n}", item2.AdditionalFieldsJson);

            // Ensure additional fields make the round trip into json
            asJObject = item2.ToJObject();
            AssertAccessTokenHasJObjectFields(
                asJObject,
                new List<string>
                {
                    "unsupported_field_name"
                });
        }

        [TestMethod]
        public void TestSerializeMsalAccessTokenCacheItem_WithRefreshOn()
        {
            var item = CreateAccessTokenItem();

            item.WithRefreshOn(DateTimeOffset.UtcNow + TimeSpan.FromMinutes(30));
            string asJson = item.ToJsonString();
            var item2 = MsalAccessTokenCacheItem.FromJsonString(asJson);

            AssertAccessTokenCacheItemsAreEqual(item, item2, "123"); // TODO: fix this
        }

        [TestMethod]
        public void TestSerializeMsalAccessTokenCacheItem_WithKidAndTokenType()
        {
            var item = CreateAccessTokenItem();
            Assert.AreEqual(StorageJsonValues.TokenTypeBearer, item.TokenType);

            var item2 = CreateAccessTokenItem("kid", "pop");

            string asJson = item2.ToJsonString();
            var item3 = MsalAccessTokenCacheItem.FromJsonString(asJson);

            AssertAccessTokenCacheItemsAreEqual(item2, item3);
        }

        [TestMethod]
        public void TestMsalAccessTokenCacheItem_HasProperJObjectFields()
        {
            var item = CreateAccessTokenItem();
            var asJObject = item.ToJObject();

            AssertAccessTokenHasJObjectFields(asJObject);
        }

        #endregion // ACCESS TOKEN TESTS

        #region REFRESH TOKEN TESTS

        [TestMethod]
        public void TestSerializeMsalRefreshTokenCacheItem()
        {
            var item = CreateRefreshTokenItem();
            string asJson = item.ToJsonString();
            var item2 = MsalRefreshTokenCacheItem.FromJsonString(asJson);

            AssertRefreshTokenCacheItemsAreEqual(item, item2);
        }

        [TestMethod]
        public void Test_FRT_SerializeDeserialize()
        {
            var item1 = CreateRefreshTokenItem();
            item1.FamilyId = null;
            var item2 = CreateRefreshTokenItem();
            item2.FamilyId = "";
            var item3 = CreateRefreshTokenItem();
            item3.FamilyId = "1";

            var json1 = item1.ToJsonString();
            var json2 = item2.ToJsonString();
            var json3 = item3.ToJsonString();

            var reserialized1 = MsalRefreshTokenCacheItem.FromJsonString(json1);
            var reserialized2 = MsalRefreshTokenCacheItem.FromJsonString(json2);
            var reserialized3 = MsalRefreshTokenCacheItem.FromJsonString(json3);

            AssertRefreshTokenCacheItemsAreEqual(item1, reserialized1);
            AssertRefreshTokenCacheItemsAreEqual(item2, reserialized2);
            AssertRefreshTokenCacheItemsAreEqual(item3, reserialized3);
        }

        [TestMethod]
        public void TestSerializeMsalRefreshTokenCacheItemWithAdditionalFields()
        {
            var item = CreateRefreshTokenItem();

            // Add an unknown field into the json
            var asJObject = item.ToJObject();
            asJObject["unsupported_field_name"] = "this is a value";

            // Ensure unknown field remains in the AdditionalFieldsJson block
            var item2 = MsalRefreshTokenCacheItem.FromJObject(asJObject);
            Assert.AreEqual("{\r\n  \"unsupported_field_name\": \"this is a value\"\r\n}", item2.AdditionalFieldsJson);

            // Ensure additional fields make the round trip into json
            asJObject = item2.ToJObject();
            AssertRefreshTokenHasJObjectFields(
                asJObject,
                new List<string>
                {
                    "unsupported_field_name"
                });
        }

        [TestMethod]
        public void TestMsalRefreshTokenCacheItem_HasProperJObjectFields()
        {
            var item = CreateRefreshTokenItem();
            var asJObject = item.ToJObject();

            AssertRefreshTokenHasJObjectFields(asJObject);
        }

        #endregion // REFRESH TOKEN TESTS

        #region ID TOKEN TESTS

        [TestMethod]
        public void TestSerializeMsalIdTokenCacheItem()
        {
            var item = CreateIdTokenItem();
            string asJson = item.ToJsonString();
            var item2 = MsalIdTokenCacheItem.FromJsonString(asJson);

            AssertIdTokenCacheItemsAreEqual(item, item2);
        }

        [TestMethod]
        public void TestSerializeMsalIdTokenCacheItemWithAdditionalFields()
        {
            var item = CreateIdTokenItem();

            // Add an unknown field into the json
            var asJObject = item.ToJObject();
            asJObject["unsupported_field_name"] = "this is a value";

            // Ensure unknown field remains in the AdditionalFieldsJson block
            var item2 = MsalIdTokenCacheItem.FromJObject(asJObject);
            Assert.AreEqual("{\r\n  \"unsupported_field_name\": \"this is a value\"\r\n}", item2.AdditionalFieldsJson);

            // Ensure additional fields make the round trip into json
            asJObject = item2.ToJObject();
            AssertIdTokenHasJObjectFields(
                asJObject,
                new List<string>
                {
                    "unsupported_field_name"
                });
        }

        [TestMethod]
        public void TestMsalIdTokenCacheItem_HasProperJObjectFields()
        {
            var item = CreateIdTokenItem();
            var asJObject = item.ToJObject();

            AssertIdTokenHasJObjectFields(asJObject);
        }

        #endregion // ID TOKEN TESTS

        #region ACCOUNT TESTS

        [TestMethod]
        public void TestSerializeMsalAccountCacheItem()
        {
            var item = CreateAccountItem();
            string asJson = item.ToJsonString();
            var item2 = MsalAccountCacheItem.FromJsonString(asJson);

            AssertAccountCacheItemsAreEqual(item, item2);
        }

        [TestMethod]
        public void TestSerializeAccountWithWamId()
        {
            MsalAccountCacheItem item = CreateAccountItem();
            item.WamAccountIds = new Dictionary<string, string>() { { "client_id_1", "wam_id_1" }, { "client_id_2", "wam_id_2" } };
            string asJson = item.ToJsonString();

            Assert.IsTrue(asJson.Contains(@" ""wam_account_ids"": {
    ""client_id_1"": ""wam_id_1"",
    ""client_id_2"": ""wam_id_2""
  }"));

            var item2 = MsalAccountCacheItem.FromJsonString(asJson);
            AssertAccountCacheItemsAreEqual(item, item2);
        }

        [TestMethod]
        public void TestSerializeMsalAccountCacheItemWithAdditionalFields()
        {
            var item = CreateAccountItem();

            // Add an unknown field into the json
            var asJObject = item.ToJObject();
            asJObject["unsupported_field_name"] = "this is a value";

            // Ensure unknown field remains in the AdditionalFieldsJson block
            var item2 = MsalAccountCacheItem.FromJObject(asJObject);
            Assert.AreEqual("{\r\n  \"unsupported_field_name\": \"this is a value\"\r\n}", item2.AdditionalFieldsJson);

            // Ensure additional fields make the round trip into json
            asJObject = item2.ToJObject();
            AssertAccountHasJObjectFields(
                asJObject,
                new List<string>
                {
                    "unsupported_field_name"
                });
        }

        [TestMethod]
        public void TestMsalAccountCacheItem_HasProperJObjectFields()
        {
            var item = CreateAccountItem();
            var asJObject = item.ToJObject();

            AssertAccountHasJObjectFields(asJObject);
        }

        #endregion // ACCOUNT TESTS

        #region APP METADATA TESTS

        [TestMethod]
        public void TestAppMetadata_SerializeDeserialize()
        {
            var item = new MsalAppMetadataCacheItem(TestConstants.ClientId, "env", "1");
            string asJson = item.ToJsonString();
            var item2 = MsalAppMetadataCacheItem.FromJsonString(asJson);

            Assert.AreEqual(item, item2);
        }

        [TestMethod]
        public void TestAppMetadata_Supports_AdditionalFields()
        {
            var item = new MsalAppMetadataCacheItem(TestConstants.ClientId, "env", "1");

            // Add an unknown field into the json
            var asJObject = item.ToJObject();
            AssertContainsKeys(asJObject, s_appMetadataKeys);

            asJObject["unsupported_field_name"] = "this is a value";

            // Ensure unknown field remains in the AdditionalFieldsJson block
            var item2 = MsalAppMetadataCacheItem.FromJObject(asJObject);
            Assert.AreEqual("{\r\n  \"unsupported_field_name\": \"this is a value\"\r\n}", item2.AdditionalFieldsJson);

            // Ensure additional fields make the round trip into json
            asJObject = item2.ToJObject();
            AssertContainsKeys(asJObject, s_appMetadataKeys);
            AssertContainsKeys(asJObject, new[] { "unsupported_field_name" });
        }

        #endregion // APP METADATA TESTS

        #region DICTIONARY SERIALIZATION TESTS

        [TestMethod]
        public void TestDictionarySerialization()
        {
            var accessor = CreateTokenCacheAccessor();

            var s1 = new TokenCacheDictionarySerializer(accessor);
            byte[] bytes = s1.Serialize(null);
            string json = new UTF8Encoding().GetString(bytes);

            var otherAccessor = new InMemoryPartitionedUserTokenCacheAccessor(Substitute.For<ILoggerAdapter>(), null);
            var s2 = new TokenCacheDictionarySerializer(otherAccessor);
            s2.Deserialize(bytes, false);

            AssertAccessorsAreEqual(accessor, otherAccessor);
        }

        #endregion // DICTIONARY SERIALIZTION TESTS

        #region JSON SERIALIZATION TESTS

        [TestMethod]
        [DeploymentItem(@"Resources\CacheFromTheFuture.json")]
        public async Task UnknownNodesTestAsync()
        {
            string jsonFilePath = ResourceHelper.GetTestResourceRelativePath("CacheFromTheFuture.json");
            string jsonContent = File.ReadAllText(jsonFilePath);
            byte[] cache = Encoding.UTF8.GetBytes(jsonContent);

            var tokenCache = new TokenCache(TestCommon.CreateDefaultServiceBundle(), false);
            tokenCache.SetBeforeAccess(notificationArgs =>
            {
                notificationArgs.TokenCache.DeserializeMsalV3(cache);
            });
            tokenCache.SetAfterAccess(notificationArgs =>
            {
                cache = notificationArgs.TokenCache.SerializeMsalV3();
            });

            var notification = new TokenCacheNotificationArgs(tokenCache, null, null, false, false, null, false, null, default, default, default, default, null, default);

            await (tokenCache as ITokenCacheInternal).OnBeforeAccessAsync(notification).ConfigureAwait(false);
            await (tokenCache as ITokenCacheInternal).OnAfterAccessAsync(notification).ConfigureAwait(false);
            (tokenCache as ITokenCacheInternal).Accessor.AssertItemCount(5, 4, 3, 3, 3);

            await (tokenCache as ITokenCacheInternal).OnBeforeAccessAsync(notification).ConfigureAwait(false);
            (tokenCache as ITokenCacheInternal).Accessor.AssertItemCount(5, 4, 3, 3, 3);

            await (tokenCache as ITokenCacheInternal).OnAfterAccessAsync(notification).ConfigureAwait(false);
            (tokenCache as ITokenCacheInternal).Accessor.AssertItemCount(5, 4, 3, 3, 3);

#if SUPPORTS_SYSTEM_TEXT_JSON
            var finalJson = JsonNode.Parse(Encoding.UTF8.GetString(cache), documentOptions: _documentOptions).AsObject();

            var originalJson = JsonNode.Parse(jsonContent, documentOptions: _documentOptions).AsObject();
            Assert.IsTrue(DeepEquals(originalJson, finalJson));
#else
            var finalJson = JObject.Parse(Encoding.UTF8.GetString(cache));

            var originalJson = JObject.Parse(jsonContent);
            Assert.IsTrue(JToken.DeepEquals(originalJson, finalJson));
#endif
        }

        [TestMethod]
        [DeploymentItem(@"Resources\ExpectedTokenCache.json")]
        public void TestJsonSerialization()
        {
            string expectedJson = File.ReadAllText(ResourceHelper.GetTestResourceRelativePath("ExpectedTokenCache.json"));
            var accessor = CreateTokenCacheAccessor();

            var s1 = new TokenCacheJsonSerializer(accessor);
            byte[] bytes = s1.Serialize(null);
            string actualJson = new UTF8Encoding().GetString(bytes);

#if SUPPORTS_SYSTEM_TEXT_JSON
            Assert.IsTrue(DeepEquals(
                JsonNode.Parse(actualJson, documentOptions: _documentOptions).AsObject(),
                JsonNode.Parse(expectedJson, documentOptions: _documentOptions).AsObject()));
#else
            Assert.IsTrue(JToken.DeepEquals(JObject.Parse(actualJson), JObject.Parse(expectedJson)));
#endif
            var otherAccessor = new InMemoryPartitionedUserTokenCacheAccessor(Substitute.For<ILoggerAdapter>(), null);
            var s2 = new TokenCacheJsonSerializer(otherAccessor);
            s2.Deserialize(bytes, false);

            AssertAccessorsAreEqual(accessor, otherAccessor);

            // serialize again to detect errors that come from deserialization
            byte[] bytes2 = s2.Serialize(null);
            string actualJson2 = new UTF8Encoding().GetString(bytes2);
#if SUPPORTS_SYSTEM_TEXT_JSON
            Assert.IsTrue(DeepEquals(
                    JsonNode.Parse(actualJson2, documentOptions: _documentOptions).AsObject(),
                JsonNode.Parse(expectedJson, documentOptions: _documentOptions).AsObject()));
#else
            Assert.IsTrue(JToken.DeepEquals(JObject.Parse(actualJson2), JObject.Parse(expectedJson)));
#endif
        }

        [TestMethod]
        public void TestSerializeContainsNoNulls()
        {
            var prefix = $"_SOMERANDOMPREFIX";
            var accessor = CreateTokenCacheAccessor();

            // Create a refresh token with a null family id in it
            var item = CreateRefreshTokenItem();
            item.FamilyId = null;
            item.Environment += prefix; // ensure we get unique cache keys
            accessor.SaveRefreshToken(item);

            var s1 = new TokenCacheJsonSerializer(accessor);
            byte[] bytes = s1.Serialize(null);
            string json = CoreHelpers.ByteArrayToString(bytes);

            Assert.IsTrue(json.Contains(prefix));
            Assert.IsFalse(json.ToLowerInvariant().Contains("null"));
        }

        [TestMethod]
        [DeploymentItem(@"Resources\ExpectedTokenCache.json")]
        public void TestDeserializeWithClearCache()
        {
            // Create a token accessor with keys in it that are NOT in the expected token cache
            var originalAccessor = CreateTokenCacheAccessorWithKeyPrefix("FAKE", 7, 6, 5, 4);
            var s1 = new TokenCacheJsonSerializer(originalAccessor);
            byte[] originalBytes = s1.Serialize(null);

            var differentAccessor = CreateTokenCacheAccessor();
            var s2 = new TokenCacheJsonSerializer(differentAccessor);
            byte[] differentBytes = s2.Serialize(null);

            // Assert that they have different counts of items...
            Assert.AreNotEqual(originalAccessor.GetAllAccessTokens().Count, differentAccessor.GetAllAccessTokens().Count);
            Assert.AreNotEqual(originalAccessor.GetAllRefreshTokens().Count, differentAccessor.GetAllRefreshTokens().Count);
            Assert.AreNotEqual(originalAccessor.GetAllIdTokens().Count, differentAccessor.GetAllIdTokens().Count);
            Assert.AreNotEqual(originalAccessor.GetAllAccounts().Count, differentAccessor.GetAllAccounts().Count);

            // Now, deserialize differentBytes into originalAccessor with cacheFlush = true
            // This means we should destroy the contents of originalAccessor and replace them with the
            // contents of the different cache

            s1.Deserialize(differentBytes, true);

            AssertAccessorsAreEqual(differentAccessor, originalAccessor);

            string expectedJson = File.ReadAllText(ResourceHelper.GetTestResourceRelativePath("ExpectedTokenCache.json"));
            // serialize again to detect errors that come from deserialization
            byte[] bytes2 = s1.Serialize(null);
            string actualJson2 = new UTF8Encoding().GetString(bytes2);
#if SUPPORTS_SYSTEM_TEXT_JSON
            Assert.IsTrue(DeepEquals(
                JsonNode.Parse(actualJson2, documentOptions: _documentOptions).AsObject(),
                JsonNode.Parse(expectedJson, documentOptions: _documentOptions).AsObject()));
#else
            Assert.IsTrue(JToken.DeepEquals(JObject.Parse(actualJson2), JObject.Parse(expectedJson)));
#endif
        }

        [TestMethod]
        public void TestDeserializeWithNoClearCache()
        {
            // Create a token accessor with keys in it that are NOT in the expected token cache
            var originalAccessor = CreateTokenCacheAccessorWithKeyPrefix("FAKE", 7, 6, 5, 4);
            var s1 = new TokenCacheJsonSerializer(originalAccessor);
            byte[] originalBytes = s1.Serialize(null);

            var differentAccessor = CreateTokenCacheAccessor();
            var s2 = new TokenCacheJsonSerializer(differentAccessor);
            byte[] differentBytes = s2.Serialize(null);

            // Assert that they have different counts of items...

            int originalAccessTokenCount = originalAccessor.GetAllAccessTokens().Count;
            int originalRefreshTokenCount = originalAccessor.GetAllRefreshTokens().Count;
            int originalIdTokenCount = originalAccessor.GetAllIdTokens().Count;
            int originalAccountsCount = originalAccessor.GetAllAccounts().Count;

            Assert.AreNotEqual(originalAccessTokenCount, differentAccessor.GetAllAccessTokens().Count);
            Assert.AreNotEqual(originalRefreshTokenCount, differentAccessor.GetAllRefreshTokens().Count);
            Assert.AreNotEqual(originalIdTokenCount, differentAccessor.GetAllIdTokens().Count);
            Assert.AreNotEqual(originalAccountsCount, differentAccessor.GetAllAccounts().Count);

            // Now, deserialize differentBytes into originalAccessor with cacheFlush = false
            // This means we should merge the contents of originalAccessor and the
            // contents of the different cache

            s1.Deserialize(differentBytes, false);

            Assert.AreEqual(originalAccessor.GetAllAccessTokens().Count, differentAccessor.GetAllAccessTokens().Count + originalAccessTokenCount);

            // This is -1 because the PRT FOCI refresh token will not duplicate since it has the same key.
            Assert.AreEqual(originalAccessor.GetAllRefreshTokens().Count, differentAccessor.GetAllRefreshTokens().Count + originalRefreshTokenCount - 1);
            Assert.AreEqual(originalAccessor.GetAllIdTokens().Count, differentAccessor.GetAllIdTokens().Count + originalIdTokenCount);
            Assert.AreEqual(originalAccessor.GetAllAccounts().Count, differentAccessor.GetAllAccounts().Count + originalAccountsCount);
        }

        #endregion // JSON SERIALIZATION TESTS

        [TestMethod]
        [DeploymentItem(@"Resources\cachecompat_dotnet_dictionary.bin")]
        public void TestMsalNet2XCacheSerializationInterop()
        {
            var accessor = new InMemoryPartitionedUserTokenCacheAccessor(Substitute.For<ILoggerAdapter>(), null);
            var s = new TokenCacheDictionarySerializer(accessor);
            string binFilePath = ResourceHelper.GetTestResourceRelativePath("cachecompat_dotnet_dictionary.bin");
            byte[] bytes = File.ReadAllBytes(binFilePath);
            s.Deserialize(bytes, false);

            Assert.AreEqual(1, accessor.GetAllAccessTokens().Count);
            Assert.AreEqual(1, accessor.GetAllRefreshTokens().Count);
            Assert.AreEqual(1, accessor.GetAllIdTokens().Count);
            Assert.AreEqual(1, accessor.GetAllAccounts().Count);
            Assert.AreEqual(0, accessor.GetAllAppMetadata().Count);

            MsalAccessTokenCacheItem expectedAccessTokenItem = new MsalAccessTokenCacheItem(
               "login.windows.net",
               "b945c513-3946-4ecd-b179-6499803a2167",
               "User.Read User.ReadBasic.All profile openid email",
               "26039cce-489d-4002-8293-5b0c5134eacb",
               secret: "",
               cachedAt: DateTimeHelpers.UnixTimestampToDateTime(1548803419),
               expiresOn: DateTimeHelpers.UnixTimestampToDateTime(1548846619),
               extendedExpiresOn: DateTimeHelpers.UnixTimestampToDateTime(1548846619),
               rawClientInfo: string.Empty,
               homeAccountId: "13dd2c19-84cd-416a-ae7d-49573e425619.26039cce-489d-4002-8293-5b0c5134eacb"
               );

            expectedAccessTokenItem.AdditionalFieldsJson = "{\r\n  \"access_token_type\": \"Bearer\"\r\n}";

            AssertAccessTokenCacheItemsAreEqual(expectedAccessTokenItem, accessor.GetAllAccessTokens().First());

            var expectedRefreshTokenItem = new MsalRefreshTokenCacheItem
            {
                Environment = "login.windows.net",
                HomeAccountId = "13dd2c19-84cd-416a-ae7d-49573e425619.26039cce-489d-4002-8293-5b0c5134eacb",
                RawClientInfo = string.Empty,
                ClientId = "b945c513-3946-4ecd-b179-6499803a2167",
                OboCacheKey = string.Empty
            };
            AssertRefreshTokenCacheItemsAreEqual(expectedRefreshTokenItem, accessor.GetAllRefreshTokens().First());

            var expectedIdTokenItem = new MsalIdTokenCacheItem
            {
                Environment = "login.windows.net",
                HomeAccountId = "13dd2c19-84cd-416a-ae7d-49573e425619.26039cce-489d-4002-8293-5b0c5134eacb",
                RawClientInfo = string.Empty,
                ClientId = "b945c513-3946-4ecd-b179-6499803a2167",
                TenantId = "26039cce-489d-4002-8293-5b0c5134eacb"
            };
            AssertIdTokenCacheItemsAreEqual(expectedIdTokenItem, accessor.GetAllIdTokens().First());

            var expectedAccountItem = new MsalAccountCacheItem
            {
                Environment = "login.windows.net",
                HomeAccountId = "13dd2c19-84cd-416a-ae7d-49573e425619.26039cce-489d-4002-8293-5b0c5134eacb",
                RawClientInfo = "eyJ1aWQiOiIxM2RkMmMxOS04NGNkLTQxNmEtYWU3ZC00OTU3M2U0MjU2MTkiLCJ1dGlkIjoiMjYwMzljY2UtNDg5ZC00MDAyLTgyOTMtNWIwYzUxMzRlYWNiIn0",
                PreferredUsername = "abhi@ddobalianoutlook.onmicrosoft.com",
                Name = "Abhi Test",
                GivenName = string.Empty,
                FamilyName = string.Empty,
                LocalAccountId = "13dd2c19-84cd-416a-ae7d-49573e425619",
                TenantId = "26039cce-489d-4002-8293-5b0c5134eacb"
            };
            AssertAccountCacheItemsAreEqual(expectedAccountItem, accessor.GetAllAccounts().First());
        }

        private void AssertAccessorsAreEqual(ITokenCacheAccessor expected, ITokenCacheAccessor actual)
        {
            Assert.AreEqual(expected.GetAllAccessTokens().Count, actual.GetAllAccessTokens().Count);
            Assert.AreEqual(expected.GetAllRefreshTokens().Count, actual.GetAllRefreshTokens().Count);
            Assert.AreEqual(expected.GetAllIdTokens().Count, actual.GetAllIdTokens().Count);
            Assert.AreEqual(expected.GetAllAccounts().Count, actual.GetAllAccounts().Count);
        }

        private void AssertContainsKey(JObject j, string key)
        {
            Assert.IsTrue(j.ContainsKey(key), $"JObject should contain key: {key}");
        }

        private void AssertContainsKeys(JObject j, IEnumerable<string> keys)
        {
            if (keys != null)
            {
                foreach (string key in keys)
                {
                    AssertContainsKey(j, key);
                }
            }
        }

        private void AddBaseJObjectFields(List<string> fields)
        {
            fields.AddRange(new List<string> { "home_account_id", "environment" });
        }

        private void AddBaseCredentialJObjectFields(List<string> fields)
        {
            AddBaseJObjectFields(fields);
            fields.AddRange(new List<string> { "client_id", "secret", "credential_type" });
        }

        private void AssertAccessTokenHasJObjectFields(JObject j, IEnumerable<string> additionalKeys = null)
        {
            var keys = new List<string>
            {
                "realm",
                "target",
                "cached_at",
                "expires_on",
                "extended_expires_on",
                "cached_at"
            };

            AddBaseCredentialJObjectFields(keys);

            AssertContainsKeys(j, keys);
            AssertContainsKeys(j, additionalKeys);
        }

        private void AssertRefreshTokenHasJObjectFields(JObject j, IEnumerable<string> additionalKeys = null)
        {
            var keys = new List<string>
            {
            };

            AddBaseCredentialJObjectFields(keys);

            AssertContainsKeys(j, keys);
            AssertContainsKeys(j, additionalKeys);
        }

        private void AssertIdTokenHasJObjectFields(JObject j, IEnumerable<string> additionalKeys = null)
        {
            var keys = new List<string>
            {
                "realm",
            };

            AddBaseCredentialJObjectFields(keys);

            AssertContainsKeys(j, keys);
            AssertContainsKeys(j, additionalKeys);
        }

        private void AssertAccountHasJObjectFields(JObject j, IEnumerable<string> additionalKeys = null)
        {
            var keys = new List<string>
            {
                "username",
                "given_name",
                "family_name",
                //"middle_name",  todo(cache): we don't support middle name
                "local_account_id",
                "authority_type",
            };

            AddBaseJObjectFields(keys);

            AssertContainsKeys(j, keys);
            AssertContainsKeys(j, additionalKeys);
        }

        private void AssertCacheItemBaseItemsAreEqual(MsalCacheItemBase expected, MsalCacheItemBase actual)
        {
            Assert.AreEqual(expected.AdditionalFieldsJson, actual.AdditionalFieldsJson, nameof(actual.AdditionalFieldsJson));
            Assert.AreEqual(expected.Environment, actual.Environment, nameof(actual.Environment));
            Assert.AreEqual(expected.HomeAccountId, actual.HomeAccountId, nameof(actual.HomeAccountId));
            Assert.AreEqual(expected.RawClientInfo, actual.RawClientInfo, nameof(actual.RawClientInfo));
        }

        private void AssertCredentialCacheItemBaseItemsAreEqual(MsalCredentialCacheItemBase expected, MsalCredentialCacheItemBase actual)
        {
            AssertCacheItemBaseItemsAreEqual(expected, actual);

            Assert.AreEqual(expected.ClientId, actual.ClientId, nameof(actual.ClientId));
            Assert.AreEqual(expected.CredentialType, actual.CredentialType, nameof(actual.CredentialType));
        }

        private void AssertAccessTokenCacheItemsAreEqual(
            MsalAccessTokenCacheItem expected,
            MsalAccessTokenCacheItem actual,
            string refreshOnTimeStamp = "")
        {
            AssertCredentialCacheItemBaseItemsAreEqual(expected, actual);

            Assert.AreEqual(expected.Environment, actual.Environment, nameof(actual.Environment));

            // since MSAL has to transform unix timestamp (i.e. time in seconds since 1 jan 1970) to datetime, the precision is 1 second
            CoreAssert.AreWithinOneSecond(expected.ExpiresOn, actual.ExpiresOn, nameof(actual.ExpiresOn));
            CoreAssert.AreWithinOneSecond(expected.ExtendedExpiresOn, actual.ExtendedExpiresOn, nameof(actual.ExtendedExpiresOn));
            CoreAssert.AreWithinOneSecond(expected.CachedAt, actual.CachedAt, nameof(actual.CachedAt));

            Assert.AreEqual(expected.IsExtendedLifeTimeToken, actual.IsExtendedLifeTimeToken, nameof(actual.IsExtendedLifeTimeToken));
            Assert.AreEqual(expected.CacheKey, actual.CacheKey);
            CollectionAssert.AreEqual(expected.ScopeSet.ToList(), actual.ScopeSet.ToList(), nameof(actual.ScopeSet));
            Assert.AreEqual(expected.TenantId, actual.TenantId, nameof(actual.TenantId));
            Assert.AreEqual(expected.OboCacheKey, actual.OboCacheKey, nameof(actual.OboCacheKey));
            Assert.AreEqual(expected.RefreshOn, actual.RefreshOn, nameof(actual.RefreshOn));
            Assert.AreEqual(expected.KeyId, actual.KeyId, nameof(actual.KeyId));
            Assert.AreEqual(expected.TokenType, actual.TokenType, nameof(actual.TokenType));

            if (actual.RefreshOn != null)
            {
                var timeDiff = int.Parse(refreshOnTimeStamp) - ((DateTimeOffset)actual.RefreshOn).ToUnixTimeSeconds();
                Assert.IsTrue(Constants.DefaultJitterRangeInSeconds >= timeDiff && timeDiff >= -Constants.DefaultJitterRangeInSeconds);
            }
        }

        private void AssertRefreshTokenCacheItemsAreEqual(MsalRefreshTokenCacheItem expected, MsalRefreshTokenCacheItem actual)
        {
            AssertCredentialCacheItemBaseItemsAreEqual(expected, actual);

            if (string.IsNullOrEmpty(expected.FamilyId))
            {
                Assert.IsTrue(string.IsNullOrEmpty(actual.FamilyId));
            }
            else
            {
                Assert.AreEqual(expected.FamilyId, actual.FamilyId);
            }

            if (string.IsNullOrEmpty(expected.OboCacheKey))
            {
                Assert.IsTrue(string.IsNullOrEmpty(actual.OboCacheKey));
            }
            else
            {
                Assert.AreEqual(expected.OboCacheKey, actual.OboCacheKey);
            }
        }

        private void AssertIdTokenCacheItemsAreEqual(MsalIdTokenCacheItem expected, MsalIdTokenCacheItem actual)
        {
            AssertCredentialCacheItemBaseItemsAreEqual(expected, actual);
            Assert.AreEqual(expected.TenantId, actual.TenantId, nameof(actual.TenantId));
        }

        private void AssertAccountCacheItemsAreEqual(MsalAccountCacheItem expected, MsalAccountCacheItem actual)
        {
            AssertCacheItemBaseItemsAreEqual(expected, actual);

            Assert.AreEqual(expected.PreferredUsername, actual.PreferredUsername, nameof(actual.PreferredUsername));
            Assert.AreEqual(expected.Name, actual.Name, nameof(actual.Name));
            Assert.AreEqual(expected.GivenName, actual.GivenName, nameof(actual.GivenName));
            Assert.AreEqual(expected.FamilyName, actual.FamilyName, nameof(actual.FamilyName));
            Assert.AreEqual(expected.LocalAccountId, actual.LocalAccountId, nameof(actual.LocalAccountId));
            Assert.AreEqual(expected.AuthorityType, actual.AuthorityType, nameof(actual.AuthorityType));
            Assert.AreEqual(expected.TenantId, actual.TenantId, nameof(actual.TenantId));
            CoreAssert.AssertDictionariesAreEqual(expected.WamAccountIds, actual.WamAccountIds, StringComparer.Ordinal);
        }

#if SUPPORTS_SYSTEM_TEXT_JSON
        private bool DeepEquals(JsonNode a, JsonNode b)
        {
            if (a == null && b == null)
            {
                return true;
            }
            else if (a is JsonValue aVal && b is JsonValue bVal)
            {
                return aVal.ToJsonString() == bVal.ToJsonString();
            }
            else if (a is JsonObject aObj && b is JsonObject bObj)
            {
                return ObjectEquals(aObj, bObj);
            }
            else if (a is JsonArray aArray && b is JsonArray bArray)
            {
                return ArrayEquals(aArray, bArray);
            }

            return false;
        }

        private bool ArrayEquals(JsonArray arr1, JsonArray arr2)
        {
            if (arr1.Count != arr2.Count)
                return false;

            for (int i = 0; i < arr1.Count; i++)
            {
                var item1 = arr1[i];
                var item2 = arr2[i];
                if (!DeepEquals(item1, item2))
                    return false;
            }
            return true;
        }

        private bool ObjectEquals(JsonObject obj1, JsonObject obj2)
        {
            if (obj1.Count != obj2.Count)
                return false;

            for (int i = 0; i < obj1.Count; i++)
            {
                var keyPair = obj1.ElementAt(i);
                var item1 = obj1[keyPair.Key];
                if (!obj2.ContainsKey(keyPair.Key))
                {
                    return false;
                }

                var item2 = obj2[keyPair.Key];
                if (!DeepEquals(item1, item2))
                    return false;
            }
            return true;
        }
#endif
    }
}
