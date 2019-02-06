// ------------------------------------------------------------------------------
// 
// Copyright (c) Microsoft Corporation.
// All rights reserved.
// 
// This code is licensed under the MIT License.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// ------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Json.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CacheTests
{
    [TestClass]
    public class CacheSerializationTests
    {
        private MsalAccessTokenCacheItem CreateAccessTokenItem()
        {
            return new MsalAccessTokenCacheItem
            {
                ClientId = MsalTestConstants.ClientId,
                Environment = "env",
                ExpiresOnUnixTimestamp = "12345",
                ExtendedExpiresOnUnixTimestamp = "23456",
                CachedAt = "34567",
                HomeAccountId = MsalTestConstants.HomeAccountId,
                IsExtendedLifeTimeToken = false,
                NormalizedScopes = MsalTestConstants.ScopeStr,
                Secret = "access_token_secret",
                TenantId = "the_tenant_id",
                RawClientInfo = null, // todo(cache): what should this be?
                UserAssertionHash = "assertion hash", // todo(cache): what should this be
            };
        }

        private MsalRefreshTokenCacheItem CreateRefreshTokenItem()
        {
            return new MsalRefreshTokenCacheItem
            {
                ClientId = MsalTestConstants.ClientId,
                Environment = "env",
                HomeAccountId = MsalTestConstants.HomeAccountId,
                Secret = "access_token_secret",
                RawClientInfo = null, // todo(cache): what should this be?
            };
        }

        private MsalIdTokenCacheItem CreateIdTokenItem()
        {
            return new MsalIdTokenCacheItem
            {
                ClientId = MsalTestConstants.ClientId,
                Environment = "env",
                HomeAccountId = MsalTestConstants.HomeAccountId,
                Secret = "access_token_secret",
                TenantId = "the_tenant_id",
                RawClientInfo = null, // todo(cache): what should this be?
            };
        }

        private MsalAccountCacheItem CreateAccountItem()
        {
            return new MsalAccountCacheItem
            {
                Environment = "env",
                HomeAccountId = MsalTestConstants.HomeAccountId,
                TenantId = "the_tenant_id",
                AuthorityType = "authority type", // todo(cache): what should this be?
                RawClientInfo = null, // "raw client info", // todo(cache): what should this be?
                LocalAccountId = MsalTestConstants.LocalAccountId,
                Name = MsalTestConstants.Name,
                GivenName = MsalTestConstants.GivenName,
                FamilyName = MsalTestConstants.FamilyName,
                PreferredUsername = MsalTestConstants.Username
            };
        }

        private ITokenCacheAccessor CreateTokenCacheAccessor()
        {
            var accessor = new TokenCacheAccessor();

            const int NumAccessTokens = 5;
            const int NumRefreshTokens = 3;
            const int NumIdTokens = 3;
            const int NumAccounts = 3;

            for (int i = 1; i <= NumAccessTokens; i++)
            {
                var item = CreateAccessTokenItem();
                item.Environment = item.Environment + $"_{i}"; // ensure we get unique cache keys
                accessor.AccessTokenCacheDictionary[item.GetKey().ToString()] = item;
            }

            for (int i = 1; i <= NumRefreshTokens; i++)
            {
                var item = CreateRefreshTokenItem();
                item.Environment = item.Environment + $"_{i}"; // ensure we get unique cache keys
                accessor.RefreshTokenCacheDictionary[item.GetKey().ToString()] = item;
            }

            for (int i = 1; i <= NumIdTokens; i++)
            {
                var item = CreateIdTokenItem();
                item.Environment = item.Environment + $"_{i}"; // ensure we get unique cache keys
                accessor.IdTokenCacheDictionary[item.GetKey().ToString()] = item;
            }

            for (int i = 1; i <= NumAccounts; i++)
            {
                var item = CreateAccountItem();
                item.Environment = item.Environment + $"_{i}"; // ensure we get unique cache keys
                accessor.AccountCacheDictionary[item.GetKey().ToString()] = item;
            }

            return accessor;
        }

        #region ACCESS TOKEN TESTS

        [TestMethod]
        public void TestSerializeMsalAccessTokenCacheItem()
        {
            var item = CreateAccessTokenItem();
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

        #region DICTIONARY SERIALIZATION TESTS

        [TestMethod]
        public void TestDictionarySerialization()
        {
            var accessor = CreateTokenCacheAccessor();

            var s1 = new TokenCacheDictionarySerializer(accessor);
            byte[] bytes = s1.Serialize();
            string json = new UTF8Encoding().GetString(bytes);

            // TODO(cache): assert json value?  or look at JObject?

            var otherAccessor = new TokenCacheAccessor();
            var s2 = new TokenCacheDictionarySerializer(otherAccessor);
            s2.Deserialize(bytes);

            AssertAccessorsAreEqual(accessor, otherAccessor);
        }

        #endregion // DICTIONARY SERIALIZTION TESTS

        #region JSON SERIALIZATION TESTS

        [TestMethod]
        public void TestJsonSerialization()
        {
            var accessor = CreateTokenCacheAccessor();

            var s1 = new TokenCacheJsonSerializer(accessor);
            byte[] bytes = s1.Serialize();
            string json = new UTF8Encoding().GetString(bytes);

            var otherAccessor = new TokenCacheAccessor();
            var s2 = new TokenCacheJsonSerializer(otherAccessor);
            s2.Deserialize(bytes);

            AssertAccessorsAreEqual(accessor, otherAccessor);
        }

        #endregion // JSON SERIALIZATION TESTS

        [TestMethod]
        [Ignore("Waiting on python to update target to be space-delimited strings instead of array")]
        [DeploymentItem(@"Resources\cachecompat_python.bin")]
        public void TestPythonCacheSerializationInterop()
        {
            var accessor = new TokenCacheAccessor();
            var s = new TokenCacheJsonSerializer(accessor);
            string pythonBinFilePath = ResourceHelper.GetTestResourceRelativePath("cachecompat_python.bin");
            byte[] bytes = File.ReadAllBytes(pythonBinFilePath);
            s.Deserialize(bytes);

            Assert.AreEqual(0, accessor.AccessTokenCount, nameof(accessor.AccessTokenCount));
            Assert.AreEqual(0, accessor.RefreshTokenCount, nameof(accessor.RefreshTokenCount));
            Assert.AreEqual(0, accessor.IdTokenCount, nameof(accessor.IdTokenCount));
            Assert.AreEqual(0, accessor.AccountCount, nameof(accessor.AccountCount));
        }

        [TestMethod]
        [DeploymentItem(@"Resources\cachecompat_dotnet_dictionary.bin")]
        public void TestMsalNet2XCacheSerializationInterop()
        {
            var accessor = new TokenCacheAccessor();
            var s = new TokenCacheDictionarySerializer(accessor);
            string binFilePath = ResourceHelper.GetTestResourceRelativePath("cachecompat_dotnet_dictionary.bin");
            byte[] bytes = File.ReadAllBytes(binFilePath);
            s.Deserialize(bytes);

            Assert.AreEqual(1, accessor.AccessTokenCount, nameof(accessor.AccessTokenCount));
            Assert.AreEqual(1, accessor.RefreshTokenCount, nameof(accessor.RefreshTokenCount));
            Assert.AreEqual(1, accessor.IdTokenCount, nameof(accessor.IdTokenCount));
            Assert.AreEqual(1, accessor.AccountCount, nameof(accessor.AccountCount));

            var expectedAccessTokenItem = new MsalAccessTokenCacheItem
            {
                AdditionalFieldsJson = "{\r\n  \"access_token_type\": \"Bearer\"\r\n}",
                Environment = "login.windows.net",
                HomeAccountId = "13dd2c19-84cd-416a-ae7d-49573e425619.26039cce-489d-4002-8293-5b0c5134eacb",
                RawClientInfo = string.Empty,
                ClientId = "b945c513-3946-4ecd-b179-6499803a2167",
                TenantId = "26039cce-489d-4002-8293-5b0c5134eacb",
                CachedAt = "1548803419",
                ExpiresOnUnixTimestamp = "1548846619",
                ExtendedExpiresOnUnixTimestamp = "1548846619",
                NormalizedScopes = "User.Read User.ReadBasic.All profile openid email",
                UserAssertionHash = string.Empty
            };
            AssertAccessTokenCacheItemsAreEqual(expectedAccessTokenItem, accessor.AccessTokenCacheDictionary.Values.First());

            var expectedRefreshTokenItem = new MsalRefreshTokenCacheItem
            {
                Environment = "login.windows.net",
                HomeAccountId = "13dd2c19-84cd-416a-ae7d-49573e425619.26039cce-489d-4002-8293-5b0c5134eacb",
                RawClientInfo = string.Empty,
                ClientId = "b945c513-3946-4ecd-b179-6499803a2167"
            };
            AssertRefreshTokenCacheItemsAreEqual(expectedRefreshTokenItem, accessor.RefreshTokenCacheDictionary.Values.First());

            var expectedIdTokenItem = new MsalIdTokenCacheItem
            {
                Environment = "login.windows.net",
                HomeAccountId = "13dd2c19-84cd-416a-ae7d-49573e425619.26039cce-489d-4002-8293-5b0c5134eacb",
                RawClientInfo = string.Empty,
                ClientId = "b945c513-3946-4ecd-b179-6499803a2167",
                TenantId = "26039cce-489d-4002-8293-5b0c5134eacb"
            };
            AssertIdTokenCacheItemsAreEqual(expectedIdTokenItem, accessor.IdTokenCacheDictionary.Values.First());

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
            AssertAccountCacheItemsAreEqual(expectedAccountItem, accessor.AccountCacheDictionary.Values.First());
        }

        private void AssertAccessorsAreEqual(ITokenCacheAccessor expected, ITokenCacheAccessor actual)
        {
            Assert.AreEqual(expected.AccessTokenCount, actual.AccessTokenCount);
            Assert.AreEqual(expected.RefreshTokenCount, actual.RefreshTokenCount);
            Assert.AreEqual(expected.IdTokenCount, actual.IdTokenCount);
            Assert.AreEqual(expected.AccountCount, actual.AccountCount);
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
            fields.AddRange(new List<string> {"home_account_id", "environment", "client_info" });
        }

        private void AddBaseCredentialJObjectFields(List<string> fields)
        {
            AddBaseJObjectFields(fields);
            fields.AddRange(new List<string> {"client_id", "secret", "credential_type" });
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

        private void AssertAccessTokenCacheItemsAreEqual(MsalAccessTokenCacheItem expected, MsalAccessTokenCacheItem actual)
        {
            AssertCredentialCacheItemBaseItemsAreEqual(expected, actual);

            Assert.AreEqual(expected.Authority, actual.Authority, nameof(actual.Authority));
            Assert.AreEqual(expected.ExpiresOnUnixTimestamp, actual.ExpiresOnUnixTimestamp, nameof(actual.ExpiresOnUnixTimestamp));
            Assert.AreEqual(expected.ExtendedExpiresOnUnixTimestamp, actual.ExtendedExpiresOnUnixTimestamp, nameof(actual.ExtendedExpiresOnUnixTimestamp));
            Assert.AreEqual(expected.CachedAt, actual.CachedAt, nameof(actual.CachedAt));
            Assert.AreEqual(expected.ExpiresOn, actual.ExpiresOn, nameof(actual.ExpiresOn));
            Assert.AreEqual(expected.ExtendedExpiresOn, actual.ExtendedExpiresOn, nameof(actual.ExtendedExpiresOn));
            Assert.AreEqual(expected.IsExtendedLifeTimeToken, actual.IsExtendedLifeTimeToken, nameof(actual.IsExtendedLifeTimeToken));
            Assert.AreEqual(expected.NormalizedScopes, actual.NormalizedScopes, nameof(actual.NormalizedScopes));
            CollectionAssert.AreEqual(expected.ScopeSet, actual.ScopeSet, nameof(actual.ScopeSet));
            Assert.AreEqual(expected.TenantId, actual.TenantId, nameof(actual.TenantId));
            Assert.AreEqual(expected.UserAssertionHash, actual.UserAssertionHash, nameof(actual.UserAssertionHash));
        }

        private void AssertRefreshTokenCacheItemsAreEqual(MsalRefreshTokenCacheItem expected, MsalRefreshTokenCacheItem actual)
        {
            AssertCredentialCacheItemBaseItemsAreEqual(expected, actual);
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
        }
    }
}