//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Test.ADAL.Common.Unit
{
    internal class TokenCacheTests
    {
        public const long ValidExpiresIn = 28800;

        private const string InvalidResource = "00000003-0000-0ff1-ce00-000000000001";

        private const string ValidClientId = "87002806-c87a-41cd-896b-84ca5690d29f";

        private const string ValidResource = "00000003-0000-0ff1-ce00-000000000000";

        private const string ValidAccessToken =
            "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJhdWQiOiIwMDAwMDAwMy0wMDAwLTBmZjEtY2UwMC0wMDAwMDAwMDAwMDAiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC8wMDAwMDAwMS0wMDAwLTBmZjEtY2UwMC0wMDAwMDAwMDAwMDAvIiwibmJmIjoxMzU4MjIwODkxLCJleHAiOjEzNTgyNDk2OTEsImFjciI6IjEiLCJwcm4iOiI2OWQyNDU0NC1jNDIwLTQ3MjEtYTRiZi0xMDZmMjM3OGQ5ZjYiLCJ0aWQiOiIwMDAwMDAwMS0wMDAwLTBmZjEtY2UwMC0wMDAwMDAwMDAwMDAiLCJpYXQiOiIxMzU4MjIwODkxIiwiYXBwaWQiOiIwMDAwMDAwMC0wMDAwLTAwMDAtMDAwMC0wMDAwMDAwMDAwMDAiLCJhcHBpZGFjciI6IjAiLCJzY3AiOiJzYW1wbGUgc2NvcGVzIiwidiI6IjIifQ.9p6zqloui6PY31Wg6SJpgt2YS-pGWKjHd-0bw_LcuFo";

        // Passing a seed to make repro possible
        private static readonly Random Rand = new Random(42);

        public static void DefaultTokenCacheTest()
        {
            AuthenticationContext context = new AuthenticationContext("https://login.windows.net/dummy", false);
            var cache = context.TokenCache;
            var cacheStore = cache.TokenCacheStore;
            cacheStore.Clear();
            Log.Comment("====== Verifying that cache is empty...");
            VerifyCacheItemCount(cache, 0);

            const string DisplayableId = "testuser@microsoft.com";
            Log.Comment("====== Creating a set of keys and values for the test...");
            TokenCacheKey key = new TokenCacheKey { Authority = "https://localhost/MockSts", Resource = ValidResource, ClientId = ValidClientId, DisplayableId = DisplayableId };
            Log.Comment(string.Format("Cache Key (with User): {0}", key));
            TokenCacheKey key2 = new TokenCacheKey { Authority = "https://localhost/MockSts", Resource = InvalidResource, ClientId = ValidClientId, DisplayableId = DisplayableId };
            Log.Comment(string.Format("Cache Key (with User): {0}", key));
            TokenCacheKey incorrectUserKey = new TokenCacheKey
                                             {
                                                 Authority = "https://localhost/MockSts",
                                                 Resource = InvalidResource,
                                                 ClientId = ValidClientId,
                                                 DisplayableId = "testuser2@microsoft.com"
                                             };
            Log.Comment(string.Format("Cache Key (with User): {0}", key));
            TokenCacheKey userlessKey = new TokenCacheKey { Authority = "https://localhost/MockSts", Resource = ValidResource, ClientId = ValidClientId };
            Log.Comment(string.Format("Cache Key (withoutUser): {0}", userlessKey));
            string value = CreateCacheValue();
            Log.Comment(string.Format("Cache Value 1: {0}", value));
            string value2 = CreateCacheValue();
            Log.Comment(string.Format("Cache Value 2: {0}", value2));
            string value3 = CreateCacheValue();
            Log.Comment(string.Format("Cache Value 3: {0}", value3));

            Log.Comment("====== Verifying that cache stores the first key/value pair...");
            cacheStore.Add(key, value);
            VerifyCacheItems(cache, 1, key);

            Log.Comment("====== Verifying that the only existing value (with user) is retrieved when requested with user and NOT without...");
            Log.Comment("Retrieving with user...");
            var valueInCache = cacheStore[key];
            VerifyCacheValuesAreEqual(value, valueInCache);
            Log.Comment("Retrieving without user...");
            cacheStore.TryGetValue(userlessKey, out valueInCache);
            Verify.IsNull(valueInCache);

            Log.Comment("====== Verifying that the value can be replaced for an existing key...");
            cacheStore[key] = value2;
            valueInCache = cacheStore[key];
            VerifyCacheValuesAreEqual(value2, valueInCache);
            VerifyCacheItems(cache, 1, key);

            Log.Comment("====== Verifying that two entries can exist at the same time, one with user and one without...");
            cacheStore.Add(userlessKey, value3);
            VerifyCacheItems(cache, 2, key, userlessKey);

            Log.Comment("====== Verifying that correct values are retrieved when requested with and without user (when two entries exist)...");
            Log.Comment("Retrieving without user...");
            valueInCache = cacheStore[userlessKey];
            VerifyCacheValuesAreEqual(value3, valueInCache);
            Log.Comment("Retrieving with user...");
            valueInCache = cacheStore[key];
            VerifyCacheValuesAreEqual(value2, valueInCache);

            Log.Comment("====== Verifying that correct entry is deleted when the key with user is passed...");
            cacheStore.Remove(key);
            VerifyCacheItems(cache, 1, userlessKey);

            Log.Comment("====== Verifying that correct entry is deleted when the key without user is passed...");
            cacheStore.Add(key, value);
            cacheStore.Remove(userlessKey);
            VerifyCacheItems(cache, 1, key);

            Log.Comment("====== Verifying that correct entry is retrieve and later deleted when the key with user is passed, even if entries are in reverse order...");
            cacheStore.Clear();
            Log.Comment("Storing without user first and then with user...");
            cacheStore.Add(userlessKey, value);
            cacheStore.Add(key, value2);
            valueInCache = cacheStore[key];
            VerifyCacheValuesAreEqual(value2, valueInCache);
            cacheStore.Remove(key);
            VerifyCacheItems(cache, 1, userlessKey);

            Log.Comment("====== Verifying that the userless entry is retrieved ONLY when requested without user...");
            cacheStore.Clear();
            cacheStore.Add(userlessKey, value);
            Log.Comment("Retrieving with user...");
            cacheStore.TryGetValue(key, out valueInCache);
            Verify.IsNull(valueInCache);
            Log.Comment("Retrieving without user...");
            valueInCache = cacheStore[userlessKey];
            VerifyCacheValuesAreEqual(value, valueInCache);

            Log.Comment("====== Verifying that entry cannot be retrieved with incorrect key...");
            cacheStore.Clear();
            cacheStore.Add(key, value);
            Log.Comment("Retrieving with incorrect key...");
            cacheStore.TryGetValue(key2, out valueInCache);
            Verify.IsNull(valueInCache);
            Log.Comment("Retrieving with incorrect user...");
            cacheStore.TryGetValue(incorrectUserKey, out valueInCache);
            Verify.IsNull(valueInCache);
            Log.Comment("Retrieving with correct user...");
            valueInCache = cacheStore[key];
            VerifyCacheValuesAreEqual(value, valueInCache);

            Log.Comment("====== Verifying that removing items from an empty cache will not throw...");
            Log.Comment("Clearing cache...");
            cacheStore.Clear();
            Log.Comment("Storing an entry...");
            cacheStore.Add(key, value);
            VerifyCacheItemCount(cache, 1);
            Log.Comment("Remvoing the only entry...");
            cacheStore.Remove(key);
            VerifyCacheItemCount(cache, 0);
            Log.Comment("Trying to remove from an empty cache...");
            cacheStore.Remove(key);
            VerifyCacheItemCount(cache, 0);
        }

        public static async Task TokenCacheKeyTestAsync()
        {
            CheckPublicGetSets();

            string authenticationResult = CreateCacheValue();
            string authority = "https://www.gotJwt.com/";
            string clientId = Guid.NewGuid().ToString();
            string password = Guid.NewGuid().ToString();
            string resource = Guid.NewGuid().ToString();
            string tenantId = Guid.NewGuid().ToString();
            string uniqueId = Guid.NewGuid().ToString();
            string displayableId = Guid.NewGuid().ToString();
            Uri redirectUri = new Uri("https://www.GetJwt.com");

            authority = authority + tenantId + "/";
            UserCredential credential = new UserCredential(displayableId, password);
            AuthenticationContext tempContext = new AuthenticationContext(authority, false);
            var localCache = tempContext.TokenCache;
            IDictionary<TokenCacheKey, string> localCacheStore = localCache.TokenCacheStore;
            localCacheStore.Clear();

            // @Resource, Credential
            TokenCacheKey tokenCacheKey = new TokenCacheKey
                                          {
                                              Authority = authority,
                                              Resource = resource,
                                              ClientId = clientId,
                                              UniqueId = uniqueId,
                                              DisplayableId = displayableId,
                                              ExpiresOn = new DateTimeOffset(DateTime.Now + TimeSpan.FromSeconds(ValidExpiresIn))
                                          };
            localCacheStore.Add(tokenCacheKey, authenticationResult);
            AuthenticationContext acWithLocalCache = new AuthenticationContext(authority, false, localCache);
            AuthenticationResult authenticationResultFromCache = await acWithLocalCache.AcquireTokenAsync(resource, clientId, credential);
            Verify.AreEqual(authenticationResult, TokenCacheEncoding.EncodeCacheValue(authenticationResultFromCache));

            // Duplicate throws error
            localCacheStore.Add(new TokenCacheKey { Authority = authority, Resource = resource, ClientId = clientId, DisplayableId = displayableId }, authenticationResult);

            try
            {
                var result = await acWithLocalCache.AcquireTokenAsync(resource, clientId, credential);
#if TEST_ADAL_WINRT
    // ADAL WinRT does not throw exception. It returns error.
                Verify.AreEqual("multiple_matching_tokens_detected", result.Error);
#else
                Verify.Fail("Exception expected");
#endif
            }
            catch (AdalException adae)
            {
                Verify.IsTrue(adae.ErrorCode == "multiple_matching_tokens_detected" && adae.Message.Contains("The cache contains multiple tokens satisfying the requirements"));
            }

            try
            {
                AuthenticationContext acWithDefaultCache = new AuthenticationContext(authority, false);
                var result = await acWithDefaultCache.AcquireTokenAsync(resource, clientId, credential);
#if TEST_ADAL_WINRT
                Verify.AreEqual("multiple_matching_tokens_detected", result.Error);
#else
                Verify.Fail("Exception expected");
#endif
            }
            catch (AdalException adae)
            {
                Verify.IsTrue(adae.ErrorCode == "multiple_matching_tokens_detected" && adae.Message.Contains("The cache contains multiple tokens satisfying the requirements"));
            }

            // @resource && @clientId
            acWithLocalCache = new AuthenticationContext(authority, false, localCache);
            localCacheStore.Clear();
            var cacheValue = CreateCacheValue();
            resource = Guid.NewGuid().ToString();
            clientId = Guid.NewGuid().ToString();
            uniqueId = Guid.NewGuid().ToString();
            displayableId = Guid.NewGuid().ToString();

            TokenCacheKey tempKey = new TokenCacheKey
                                    {
                                        Authority = authority,
                                        Resource = resource,
                                        ClientId = clientId,
                                        ExpiresOn = new DateTimeOffset(DateTime.Now + TimeSpan.FromSeconds(ValidExpiresIn))
                                    };
            localCacheStore.Add(tempKey, cacheValue);
            localCacheStore.Remove(tempKey);
            Verify.IsFalse(localCacheStore.ContainsKey(tempKey));
            localCacheStore.Add(tempKey, cacheValue);

#if TEST_ADAL_WINRT
            authenticationResultFromCache = await acWithLocalCache.AcquireTokenAsync(resource, clientId, redirectUri);
#else
            authenticationResultFromCache = await acWithLocalCache.AcquireTokenSilentAsync(resource, clientId);
#endif
            Verify.AreEqual(cacheValue, TokenCacheEncoding.EncodeCacheValue(authenticationResultFromCache));

            // @resource && @clientId && userId
            acWithLocalCache = new AuthenticationContext(authority, false, localCache);
            localCacheStore.Clear();
            cacheValue = CreateCacheValue();
            resource = Guid.NewGuid().ToString();
            clientId = Guid.NewGuid().ToString();
            uniqueId = Guid.NewGuid().ToString();
            displayableId = Guid.NewGuid().ToString();
            localCacheStore.Add(
                new TokenCacheKey
                {
                    Authority = authority,
                    Resource = resource,
                    ClientId = clientId,
                    UniqueId = uniqueId,
                    DisplayableId = displayableId,
                    ExpiresOn = new DateTimeOffset(DateTime.Now + TimeSpan.FromSeconds(ValidExpiresIn))
                },
                cacheValue);

            var userId = new UserIdentifier(uniqueId, UserIdentifierType.UniqueId);
            var userIdUpper = new UserIdentifier(displayableId.ToUpper(), UserIdentifierType.RequiredDisplayableId);

#if TEST_ADAL_WINRT
            authenticationResultFromCache = await acWithLocalCache.AcquireTokenAsync(resource, clientId, redirectUri, PromptBehavior.Auto, userId);
#else
            authenticationResultFromCache = await acWithLocalCache.AcquireTokenSilentAsync(resource, clientId, userId);
#endif
            Verify.AreEqual(cacheValue, TokenCacheEncoding.EncodeCacheValue(authenticationResultFromCache));

#if TEST_ADAL_WINRT
            authenticationResultFromCache = await acWithLocalCache.AcquireTokenAsync(resource, clientId, redirectUri, PromptBehavior.Auto, userIdUpper);
#else
            authenticationResultFromCache = await acWithLocalCache.AcquireTokenSilentAsync(resource, clientId, userIdUpper);
#endif
            Verify.AreEqual(cacheValue, TokenCacheEncoding.EncodeCacheValue(authenticationResultFromCache));

#if TEST_ADAL_WINRT
            authenticationResultFromCache = await acWithLocalCache.AcquireTokenAsync(resource, clientId, redirectUri);
#else
            authenticationResultFromCache = await acWithLocalCache.AcquireTokenSilentAsync(resource, clientId);
#endif
            Verify.AreEqual(cacheValue, TokenCacheEncoding.EncodeCacheValue(authenticationResultFromCache));

        }

        internal static void TokenCacheOperationsTest(TokenCache tokenCache)
        {
            var cacheStore = tokenCache.TokenCacheStore;

            cacheStore.Clear();

            DateTimeOffset time = DateTimeOffset.UtcNow;
            TokenCacheKey key = new TokenCacheKey
                                {
                                    Authority = "https://localhost/MockSts",
                                    Resource = "resourc1",
                                    ClientId = "client1",
                                    DisplayableId = "user1",
                                    ExpiresOn = time
                                };
            TokenCacheKey key2 = new TokenCacheKey { Authority = "https://localhost/MockSts", Resource = "resource1", ClientId = "client1", DisplayableId = "user2" };
            TokenCacheKey key3 = new TokenCacheKey
                                 {
                                     Authority = "https://localhost/MockSts",
                                     Resource = "resourc1",
                                     ClientId = "client1",
                                     DisplayableId = "user1",
                                     ExpiresOn = time.AddTicks(1)
                                 };
            Verify.AreNotEqual(key, key3);

            string value = CreateCacheValue();
            string value2;
            do
            {
                value2 = CreateCacheValue();
            }
            while (value2 == value);

            Verify.AreEqual(0, cacheStore.Count);
            cacheStore.Add(key, value);
            Verify.AreEqual(1, cacheStore.Count);
            string valueInCache = cacheStore[key];
            Verify.AreEqual(valueInCache, value);
            Verify.AreNotEqual(valueInCache, value2);
            cacheStore[key] = value2;
            Verify.AreEqual(1, cacheStore.Count);
            valueInCache = cacheStore[key];
            Verify.AreEqual(valueInCache, value2);
            Verify.AreNotEqual(valueInCache, value);
            try
            {
                cacheStore.Add(key, value);
                Verify.Fail("Exception expected due to duplicate key");
            }
            catch (ArgumentException)
            {
                // Expected
            }

            Verify.IsFalse(cacheStore.Remove(new KeyValuePair<TokenCacheKey, string>(key, value)));
            Verify.IsTrue(cacheStore.Remove(new KeyValuePair<TokenCacheKey, string>(key, value2)));
            Verify.IsFalse(cacheStore.Remove(new KeyValuePair<TokenCacheKey, string>(key, value2)));
            Verify.AreEqual(0, cacheStore.Count);

            cacheStore.Add(key, value);
            cacheStore.Add(key2, value2);
            Verify.AreEqual(2, cacheStore.Count);
            Verify.AreEqual(cacheStore[key], value);
            Verify.AreEqual(cacheStore[key2], value2);

            try
            {
                cacheStore.Add(null, value);
                Verify.Fail("Exception expected due to duplicate key");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }

            try
            {
                cacheStore[null] = value;
                Verify.Fail("Exception expected due to duplicate key");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }

            Verify.IsFalse(cacheStore.IsReadOnly);

            var keys = cacheStore.Keys.ToList();
            var values = cacheStore.Values.ToList();
            Verify.AreEqual(2, keys.Count);
            Verify.AreEqual(2, values.Count);
            if (keys[0].Equals(key))
            {
                Verify.AreEqual(keys[1], key2);
                Verify.AreEqual(values[0], value);
                Verify.AreEqual(values[1], value2);
            }
            else
            {
                Verify.AreEqual(keys[0], key2);
                Verify.AreEqual(keys[1], key);
                Verify.AreEqual(values[0], value2);
                Verify.AreEqual(values[1], value);
            }

            Verify.IsTrue(cacheStore.ContainsKey(key));
            Verify.IsTrue(cacheStore.ContainsKey(key2));
            Verify.IsFalse(cacheStore.ContainsKey(key3));

            Verify.IsTrue(cacheStore.Contains(new KeyValuePair<TokenCacheKey, string>(key, value)));
            Verify.IsTrue(cacheStore.Contains(new KeyValuePair<TokenCacheKey, string>(key2, value2)));
            Verify.IsFalse(cacheStore.Contains(new KeyValuePair<TokenCacheKey, string>(key, value2)));
            Verify.IsFalse(cacheStore.Contains(new KeyValuePair<TokenCacheKey, string>(key2, value)));

            try
            {
                cacheStore.Add(new KeyValuePair<TokenCacheKey, string>(key, value));
                Verify.Fail("Exception expected due to duplicate key");
            }
            catch (ArgumentException)
            {
                // Expected
            }

            cacheStore.Add(new KeyValuePair<TokenCacheKey, string>(key3, value));
            Verify.AreEqual(3, cacheStore.Keys.Count);
            Verify.IsTrue(cacheStore.ContainsKey(key3));

            var cacheStoreCopy = new KeyValuePair<TokenCacheKey, string>[cacheStore.Count + 1];
            cacheStore.CopyTo(cacheStoreCopy, 1);
            for (int i = 0; i < cacheStore.Count; i++)
            {
                Verify.AreEqual(cacheStoreCopy[i + 1].Value, cacheStore[cacheStoreCopy[i + 1].Key]);
            }

            try
            {
                cacheStore.CopyTo(cacheStoreCopy, 2);
                Verify.Fail("Exception expected");
            }
            catch (ArgumentException)
            {
                // Expected
            }

            try
            {
                cacheStore.CopyTo(cacheStoreCopy, -1);
                Verify.Fail("Exception expected");
            }
            catch (ArgumentOutOfRangeException)
            {
                // Expected
            }

            cacheStore.Remove(key2);
            Verify.AreEqual(2, cacheStore.Keys.Count);

            foreach (var kvp in cacheStore)
            {
                Verify.IsTrue(kvp.Key.Equals(key) || kvp.Key.Equals(key3));
                Verify.IsTrue(kvp.Value.Equals(value));
            }

            string cacheValue;
            Verify.IsTrue(cacheStore.TryGetValue(key, out cacheValue));
            Verify.AreEqual(cacheValue, value);
            Verify.IsTrue(cacheStore.TryGetValue(key3, out cacheValue));
            Verify.AreEqual(cacheValue, value);
            Verify.IsFalse(cacheStore.TryGetValue(key2, out cacheValue));

            cacheStore.Clear();
            Verify.AreEqual(0, cacheStore.Keys.Count);
        }

        internal static void TokenCacheCapacityTest(TokenCache tokenCache)
        {
            var cacheStore = tokenCache.TokenCacheStore;
            cacheStore.Clear();

            const int MaxItemCount = 100;
            const int MaxFieldSize = 256;
            const int MaxValueSize = 1024 * 20;
            TokenCacheKey[] keys = new TokenCacheKey[MaxItemCount];
            string[] values = new string[MaxItemCount];

            for (int i = 0; i < MaxItemCount; i++)
            {
                keys[i] = new TokenCacheKey
                          {
                              Authority = GenerateRandomString(MaxFieldSize),
                              Resource = GenerateRandomString(MaxFieldSize),
                              ClientId = GenerateRandomString(MaxFieldSize),
                              DisplayableId = GenerateRandomString(MaxFieldSize),
                              ExpiresOn = DateTimeOffset.UtcNow.AddMilliseconds(Rand.Next() % 100000)
                          };

                values[i] = GenerateBase64EncodedRandomString(MaxValueSize);
                cacheStore.Add(keys[i], values[i]);
            }

            Verify.AreEqual(MaxItemCount, cacheStore.Count);

            for (int i = 0; i < MaxItemCount; i++)
            {
                string cacheValue;
                int index = MaxItemCount - i - 1;
                Verify.IsTrue(cacheStore.TryGetValue(keys[index], out cacheValue));
                Verify.AreEqual(values[index], cacheValue);
                cacheStore.Remove(keys[index]);
                Verify.AreEqual(index, cacheStore.Count);
            }

            cacheStore.Clear();
        }

        internal static void TokenCacheValueSplitTest(TokenCache tokenCache)
        {
            var cacheStore = tokenCache.TokenCacheStore;

            TokenCacheKey key = new TokenCacheKey
                                {
                                    Authority = "https://localhost/MockSts",
                                    Resource = "resourc1",
                                    ClientId = "client1",
                                    DisplayableId = "user1",
                                    ExpiresOn = DateTimeOffset.UtcNow
                                };

            cacheStore.Clear();
            cacheStore.Add(key, null);
            Verify.AreEqual(cacheStore[key], null);
            for (int len = 0; len < 3000; len++)
            {
                string value = GenerateRandomString(len);
                cacheStore.Clear();
                cacheStore.Add(key, value);
                Verify.AreEqual(cacheStore[key], value);
            }
        }

        public static string CreateCacheValue()
        {
            string refreshToken = string.Format("RefreshToken{0}", Rand.Next());
            return
                TokenCacheEncoding.EncodeCacheValue(
                    new AuthenticationResult(null, ValidAccessToken, refreshToken, new DateTimeOffset(DateTime.Now + TimeSpan.FromSeconds(ValidExpiresIn))));
        }

        public static void CheckPublicGetSets()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            TokenCacheKey tokenCacheKey = new TokenCacheKey()
                                          {
                                              Authority = "Authority",
                                              ClientId = "ClientId",
                                              ExpiresOn = now,
                                              IsMultipleResourceRefreshToken = false,
                                              Resource = "Resource",
                                              UniqueId = "UniqueId",
                                              DisplayableId = "DisplayableId",
                                              SubjectType = TokenSubjectType.User
                                          };

            Verify.IsTrue(tokenCacheKey.Authority == "Authority");
            Verify.IsTrue(tokenCacheKey.ClientId == "ClientId");
            Verify.IsTrue(tokenCacheKey.ExpiresOn == now);
            Verify.IsTrue(tokenCacheKey.IsMultipleResourceRefreshToken == false);
            Verify.IsTrue(tokenCacheKey.Resource == "Resource");
            Verify.IsTrue(tokenCacheKey.UniqueId == "UniqueId");
            Verify.IsTrue(tokenCacheKey.DisplayableId == "DisplayableId");
            Verify.IsTrue(tokenCacheKey.SubjectType == TokenSubjectType.User);
        }

        private static void VerifyCacheValuesAreEqual(string value1, string value2)
        {
            Verify.AreEqual(value1, value2);
        }

        private static void VerifyCacheItemCount(TokenCache cache, int expectedCount)
        {
            Verify.AreEqual(cache.ReadItems().Count(), expectedCount, null);
        }

        private static void VerifyCacheItems(TokenCache cache, int expectedCount, TokenCacheKey firstKey)
        {
            VerifyCacheItems(cache, expectedCount, firstKey, null);
        }

        private static void VerifyCacheItems(TokenCache cache, int expectedCount, TokenCacheKey firstKey, TokenCacheKey secondKey)
        {
            var items = cache.ReadItems().ToList();
            Verify.AreEqual(expectedCount, items.Count);

            if (firstKey != null)
            {
                Verify.IsTrue(AreEqual(items[0], firstKey) || AreEqual(items[0], secondKey));
            }

            if (secondKey != null)
            {
                Verify.IsTrue(AreEqual(items[1], firstKey) || AreEqual(items[1], secondKey));
            }
        }

        public static bool AreDateTimeOffsetsEqual(DateTimeOffset time1, DateTimeOffset time2)
        {
            return (Math.Abs((time1 - time2).Seconds) < 1.0);
        }

        public static string GenerateRandomString(int len)
        {
            char[] str = new char[len];
            for (int i = 0; i < len; i++)
            {
                str[i] = (char)Rand.Next(0x20, 0x7F);
            }

            return new string(str);
        }

        public static string GenerateBase64EncodedRandomString(int len)
        {
            return EncodingHelper.Base64Encode(GenerateRandomString(len)).Substring(0, len);
        }

        private static bool AreEqual(TokenCacheItem item, TokenCacheKey key)
        {
            return (item.ClientId == key.ClientId &&
                item.Resource == key.Resource &&
                item.Authority == key.Authority &&
                item.DisplayableId == key.DisplayableId &&
                item.ExpiresOn == key.ExpiresOn &&
                item.IsMultipleResourceRefreshToken == key.IsMultipleResourceRefreshToken &&
                item.UniqueId == key.UniqueId);
        }
    }
}
