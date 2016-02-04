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
        public static long ValidExpiresIn = 28800;

        private static string[] InvalidResource = new []{ "00000003-0000-0ff1-ce00-000000000001" };

        private static string ValidClientId = "87002806-c87a-41cd-896b-84ca5690d29f";

        private static string[] ValidResource = new[] { "00000003-0000-0ff1-ce00-000000000000" };

        private static string ValidAccessToken =
            "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJhdWQiOiIwMDAwMDAwMy0wMDAwLTBmZjEtY2UwMC0wMDAwMDAwMDAwMDAiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC8wMDAwMDAwMS0wMDAwLTBmZjEtY2UwMC0wMDAwMDAwMDAwMDAvIiwibmJmIjoxMzU4MjIwODkxLCJleHAiOjEzNTgyNDk2OTEsImFjciI6IjEiLCJwcm4iOiI2OWQyNDU0NC1jNDIwLTQ3MjEtYTRiZi0xMDZmMjM3OGQ5ZjYiLCJ0aWQiOiIwMDAwMDAwMS0wMDAwLTBmZjEtY2UwMC0wMDAwMDAwMDAwMDAiLCJpYXQiOiIxMzU4MjIwODkxIiwiYXBwaWQiOiIwMDAwMDAwMC0wMDAwLTAwMDAtMDAwMC0wMDAwMDAwMDAwMDAiLCJhcHBpZGFjciI6IjAiLCJzY3AiOiJzYW1wbGUgc2NvcGVzIiwidiI6IjIifQ.9p6zqloui6PY31Wg6SJpgt2YS-pGWKjHd-0bw_LcuFo";

        // Passing a seed to make repro possible
        private static readonly Random Rand = new Random(42);

        public static void DefaultTokenCacheTest()
        {
            var cache = TokenCache.DefaultShared;
            cache.Clear();
            Log.Comment("====== Verifying that cache is empty...");
            VerifyCacheItemCount(cache, 0);

            const string DisplayableId = "testuser@microsoft.com";
            Log.Comment("====== Creating a set of keys and values for the test...");
            TokenCacheKey key = new TokenCacheKey("https://localhost/MockSts", new HashSet<string>(ValidResource), ValidClientId, TokenSubjectType.User, null, DisplayableId, null);
            var value = CreateCacheValue(key.UniqueId, key.DisplayableId);
            Log.Comment(string.Format("Cache Key (with User): {0}", key));
            Log.Comment(string.Format("Cache Value 1: {0}", value));
            TokenCacheKey key2 = new TokenCacheKey("https://localhost/MockSts", new HashSet<string>(InvalidResource), ValidClientId, TokenSubjectType.User, null, DisplayableId);
            var value2 = CreateCacheValue(null, DisplayableId);
            Log.Comment(string.Format("Cache Key (with User): {0}", key));
            Log.Comment(string.Format("Cache Value 2: {0}", value2));
            TokenCacheKey userlessKey = new TokenCacheKey("https://localhost/MockSts", new HashSet<string>(ValidResource), ValidClientId, TokenSubjectType.User, null, null);
            var userlessValue = CreateCacheValue(null, null);
            Log.Comment(string.Format("Cache Key (withoutUser): {0}", userlessKey));
            Log.Comment(string.Format("Cache Value 3: {0}", userlessValue));

            TokenCacheKey incorrectUserKey = new TokenCacheKey("https://localhost/MockSts", new HashSet<string>(InvalidResource), ValidClientId, TokenSubjectType.User, null, "testuser2@microsoft.com");

            Log.Comment("====== Verifying that cache stores the first key/value pair...");
            AddToDictionary(cache, key, value);
            VerifyCacheItems(cache, 1, key);

            Log.Comment("====== Verifying that the only existing value (with user) is retrieved when requested with user and NOT without...");
            Log.Comment("Retrieving with user...");
            var valueInCache = cache.tokenCacheDictionary[key];
            VerifyAuthenticationResultExsAreEqual(value, valueInCache);
            Log.Comment("Retrieving without user...");
            cache.tokenCacheDictionary.TryGetValue(userlessKey, out valueInCache);
            Verify.IsNull(valueInCache);

            Log.Comment("====== Verifying that two entries can exist at the same time, one with user and one without...");
            AddToDictionary(cache, userlessKey, userlessValue);
            VerifyCacheItems(cache, 2, key, userlessKey);

            Log.Comment("====== Verifying that correct values are retrieved when requested with and without user (when two entries exist)...");
            Log.Comment("Retrieving without user...");
            valueInCache = cache.tokenCacheDictionary[userlessKey];
            VerifyAuthenticationResultExsAreEqual(userlessValue, valueInCache);
            Log.Comment("Retrieving with user...");
            valueInCache = cache.tokenCacheDictionary[key];
            VerifyAuthenticationResultExsAreEqual(value, valueInCache);

            Log.Comment("====== Verifying that correct entry is deleted when the key with user is passed...");
            RemoveFromDictionary(cache, key);
            VerifyCacheItems(cache, 1, userlessKey);

            Log.Comment("====== Verifying that correct entry is deleted when the key without user is passed...");
            AddToDictionary(cache, key, value);
            RemoveFromDictionary(cache, userlessKey);
            VerifyCacheItems(cache, 1, key);

            Log.Comment("====== Verifying that correct entry is retrieve and later deleted when the key with user is passed, even if entries are in reverse order...");
            cache.Clear();
            Log.Comment("Storing without user first and then with user...");
            AddToDictionary(cache, userlessKey, userlessValue);
            AddToDictionary(cache, key2, value2);
            valueInCache = cache.tokenCacheDictionary[key2];
            VerifyAuthenticationResultExsAreEqual(value2, valueInCache);
            RemoveFromDictionary(cache, key2);
            VerifyCacheItems(cache, 1, userlessKey);

            Log.Comment("====== Verifying that the userless entry is retrieved ONLY when requested without user...");
            cache.Clear();
            AddToDictionary(cache, userlessKey, value);
            Log.Comment("Retrieving with user...");
            cache.tokenCacheDictionary.TryGetValue(key, out valueInCache);
            Verify.IsNull(valueInCache);
            Log.Comment("Retrieving without user...");
            valueInCache = cache.tokenCacheDictionary[userlessKey];
            VerifyAuthenticationResultExsAreEqual(value, valueInCache);

            Log.Comment("====== Verifying that entry cannot be retrieved with incorrect key...");
            cache.Clear();
            AddToDictionary(cache, key, value);
            Log.Comment("Retrieving with incorrect key...");
            cache.tokenCacheDictionary.TryGetValue(key2, out valueInCache);
            Verify.IsNull(valueInCache);
            Log.Comment("Retrieving with incorrect user...");
            cache.tokenCacheDictionary.TryGetValue(incorrectUserKey, out valueInCache);
            Verify.IsNull(valueInCache);
            Log.Comment("Retrieving with correct user...");
            valueInCache = cache.tokenCacheDictionary[key];
            VerifyAuthenticationResultExsAreEqual(value, valueInCache);

            Log.Comment("====== Verifying that removing items from an empty cache will not throw...");
            Log.Comment("Clearing cache...");
            cache.Clear();
            Log.Comment("Storing an entry...");
            AddToDictionary(cache, key, value);
            VerifyCacheItemCount(cache, 1);
            Log.Comment("Remvoing the only entry...");
            RemoveFromDictionary(cache, key);
            VerifyCacheItemCount(cache, 0);
            Log.Comment("Trying to remove from an empty cache...");
            RemoveFromDictionary(cache, key);
            VerifyCacheItemCount(cache, 0);
        }

        internal static void TokenCacheCrossTenantOperationsTest()
        {
            var tokenCache = new TokenCache();
            var cacheDictionary = tokenCache.tokenCacheDictionary;
            tokenCache.Clear();

            TokenCacheKey key = new TokenCacheKey("https://localhost/MockSts/", new HashSet<string>(new[]{ "resource1" }), "client1", TokenSubjectType.User, null, "user1", null);
            AuthenticationResultEx value = CreateCacheValue(null, "user1");

        }

        internal static void TokenCacheOperationsTest()
        {
            var tokenCache = new TokenCache();
            var cacheDictionary = tokenCache.tokenCacheDictionary;

            tokenCache.Clear();

            TokenCacheKey key = new TokenCacheKey("https://localhost/MockSts/", new HashSet<string>(new[] { "resource1" }), "client1", TokenSubjectType.User, null, "user1");
            TokenCacheKey key2 = new TokenCacheKey("https://localhost/MockSts/", new HashSet<string>(new[] { "resource1" }), "client1", TokenSubjectType.User, null, "user2");
            TokenCacheKey key3 = new TokenCacheKey("https://localhost/MockSts/", new HashSet<string>(new[] { "resource1" }), "client1", TokenSubjectType.UserPlusClient, null, "user1");
            Verify.AreNotEqual(key, key3);

            var value = CreateCacheValue(null, "user1");
            AuthenticationResultEx value2;
            do
            {
                value2 = CreateCacheValue(null, "user2");
            }
            while (value2 == value);

            Verify.AreEqual(0, cacheDictionary.Count);
            AddToDictionary(tokenCache, key, value);
            Verify.AreEqual(1, cacheDictionary.Count);
            var valueInCache = cacheDictionary[key];
            VerifyAuthenticationResultExsAreEqual(valueInCache, value);
            VerifyAuthenticationResultExsAreNotEqual(valueInCache, value2);
            cacheDictionary[key] = value2;
            Verify.AreEqual(1, cacheDictionary.Count);
            valueInCache = cacheDictionary[key];
            VerifyAuthenticationResultExsAreEqual(valueInCache, value2);
            VerifyAuthenticationResultExsAreNotEqual(valueInCache, value);
            try
            {
                AddToDictionary(tokenCache, key, value);
                Verify.Fail("Exception expected due to duplicate key");
            }
            catch (ArgumentException)
            {
                // Expected
            }
            
            Log.Comment("====== Verifying that correct values are retrieved when requested for different tenant with user and without user");
            AuthenticationResultEx resultEx = tokenCache.LoadFromCache("https://localhost/MockSts1", new HashSet<string>(new[] { "resource1" }), "client1", TokenSubjectType.User, null,
                "user1", "root1", null, null);
            Verify.IsNotNull(resultEx);
            

            Verify.IsTrue(RemoveFromDictionary(tokenCache, key));
            Verify.IsFalse(RemoveFromDictionary(tokenCache, key));
            Verify.AreEqual(0, cacheDictionary.Count);

            AddToDictionary(tokenCache, key, value);
            AddToDictionary(tokenCache, key2, value2);
            Verify.AreEqual(2, cacheDictionary.Count);
            Verify.AreEqual(cacheDictionary[key], value);
            Verify.AreEqual(cacheDictionary[key2], value2);

            try
            {
                AddToDictionary(tokenCache, null, value);
                Verify.Fail("Exception expected due to duplicate key");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }

            try
            {
                cacheDictionary[null] = value;
                Verify.Fail("Exception expected due to duplicate key");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }

            Verify.IsFalse(cacheDictionary.IsReadOnly);

            var keys = cacheDictionary.Keys.ToList();
            var values = cacheDictionary.Values.ToList();
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

            Verify.IsTrue(cacheDictionary.ContainsKey(key));
            Verify.IsTrue(cacheDictionary.ContainsKey(key2));
            Verify.IsFalse(cacheDictionary.ContainsKey(key3));

            Verify.IsTrue(cacheDictionary.Contains(new KeyValuePair<TokenCacheKey, AuthenticationResultEx>(key, value)));
            Verify.IsTrue(cacheDictionary.Contains(new KeyValuePair<TokenCacheKey, AuthenticationResultEx>(key2, value2)));
            Verify.IsFalse(cacheDictionary.Contains(new KeyValuePair<TokenCacheKey, AuthenticationResultEx>(key, value2)));
            Verify.IsFalse(cacheDictionary.Contains(new KeyValuePair<TokenCacheKey, AuthenticationResultEx>(key2, value)));

            try
            {
                AddToDictionary(tokenCache, key, value);
                Verify.Fail("Exception expected due to duplicate key");
            }
            catch (ArgumentException)
            {
                // Expected
            }

            AddToDictionary(tokenCache, key3, value);
            Verify.AreEqual(3, cacheDictionary.Keys.Count);
            Verify.IsTrue(cacheDictionary.ContainsKey(key3));

            var cacheItemsCopy = new KeyValuePair<TokenCacheKey, AuthenticationResultEx>[cacheDictionary.Count + 1];
            cacheDictionary.CopyTo(cacheItemsCopy, 1);
            for (int i = 0; i < cacheDictionary.Count; i++)
            {
                Verify.AreEqual(cacheItemsCopy[i + 1].Value, cacheDictionary[cacheItemsCopy[i + 1].Key]);
            }

            try
            {
                cacheDictionary.CopyTo(cacheItemsCopy, 2);
                Verify.Fail("Exception expected");
            }
            catch (ArgumentException)
            {
                // Expected
            }

            try
            {
                cacheDictionary.CopyTo(cacheItemsCopy, -1);
                Verify.Fail("Exception expected");
            }
            catch (ArgumentOutOfRangeException)
            {
                // Expected
            }

            RemoveFromDictionary(tokenCache, key2);
            Verify.AreEqual(2, cacheDictionary.Keys.Count);

            foreach (var kvp in cacheDictionary)
            {
                Verify.IsTrue(kvp.Key.Equals(key) || kvp.Key.Equals(key3));
                Verify.IsTrue(kvp.Value.Equals(value));
            }

            AuthenticationResultEx cacheValue;
            Verify.IsTrue(cacheDictionary.TryGetValue(key, out cacheValue));
            Verify.AreEqual(cacheValue, value);
            Verify.IsTrue(cacheDictionary.TryGetValue(key3, out cacheValue));
            Verify.AreEqual(cacheValue, value);
            Verify.IsFalse(cacheDictionary.TryGetValue(key2, out cacheValue));

            cacheDictionary.Clear();
            Verify.AreEqual(0, cacheDictionary.Keys.Count);
        }

        internal static void TokenCacheValueSplitTest()
        {
            var tokenCache = new TokenCache();
            TokenCacheKey key = new TokenCacheKey("https://localhost/MockSts", new HashSet<string>(new[] { "resourc1" }), "client1", TokenSubjectType.User, null, "user1");

            tokenCache.Clear();
            AddToDictionary(tokenCache, key, null);
            Verify.AreEqual(tokenCache.tokenCacheDictionary[key], null);
            for (int len = 0; len < 3000; len++)
            {
                var value = CreateCacheValue(null, "user1");
                tokenCache.Clear();
                AddToDictionary(tokenCache, key, value);
                Verify.AreEqual(tokenCache.tokenCacheDictionary[key], value);
            }
        }

        public static AuthenticationResultEx CreateCacheValue(string uniqueId, string displayableId)
        {
            string refreshToken = string.Format("RefreshToken{0}", Rand.Next());
            var result = new AuthenticationResult(null, ValidAccessToken, new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)))
                {
                    User = new User { UniqueId = uniqueId, DisplayableId = displayableId }
                };

            return new AuthenticationResultEx
            {
                Result = result,
                RefreshToken = refreshToken
            };
        }
        
        private static void VerifyCacheItemCount(TokenCache cache, int expectedCount)
        {
            Verify.AreEqual(cache.Count, expectedCount);
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
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            char[] str = new char[len];
            for (int i = 0; i < len; i++)
            {
                str[i] = chars[Rand.Next(chars.Length)];
            }

            return new string(str);
        }

        public static string GenerateBase64EncodedRandomString(int len)
        {
            return EncodingHelper.Base64Encode(GenerateRandomString(len)).Substring(0, len);
        }

        private static bool AreEqual(TokenCacheItem item, TokenCacheKey key)
        {
            return item.Match(key);
        }

        private static void VerifyAuthenticationResultExsAreEqual(AuthenticationResultEx resultEx1, AuthenticationResultEx resultEx2)
        {
            Verify.IsTrue(AreAuthenticationResultExsEqual(resultEx1, resultEx2));
        }

        private static void VerifyAuthenticationResultExsAreNotEqual(AuthenticationResultEx resultEx1, AuthenticationResultEx resultEx2)
        {
            Verify.IsFalse(AreAuthenticationResultExsEqual(resultEx1, resultEx2));
        }

        private static void VerifyAuthenticationResultsAreEqual(AuthenticationResult result1, AuthenticationResult result2)
        {
            Verify.IsTrue(AreAuthenticationResultsEqual(result1, result2));
        }

        private static void VerifyAuthenticationResultsAreNotEqual(AuthenticationResult result1, AuthenticationResult result2)
        {
            Verify.IsFalse(AreAuthenticationResultsEqual(result1, result2));
        }

        private static bool AreAuthenticationResultExsEqual(AuthenticationResultEx resultEx1, AuthenticationResultEx resultEx2)
        {
            return AreAuthenticationResultsEqual(resultEx1.Result, resultEx2.Result) &&
                resultEx1.RefreshToken == resultEx2.RefreshToken &&
                resultEx1.IsMultipleResourceRefreshToken == resultEx2.IsMultipleResourceRefreshToken;
        }

        private static bool AreAuthenticationResultsEqual(AuthenticationResult result1, AuthenticationResult result2)
        {
            return (AreStringsEqual(result1.AccessToken, result2.AccessToken)
                    && AreStringsEqual(result1.AccessTokenType, result2.AccessTokenType)
                    && AreStringsEqual(result1.IdToken, result2.IdToken)
                    && AreStringsEqual(result1.TenantId, result2.TenantId)
                    && (result1.User == null || result2.User == null ||
                        (AreStringsEqual(result1.User.DisplayableId, result2.User.DisplayableId)
                        && AreStringsEqual(result1.User.FamilyName, result2.User.FamilyName)
                        && AreStringsEqual(result1.User.GivenName, result2.User.GivenName)
                        && AreStringsEqual(result1.User.IdentityProvider, result2.User.IdentityProvider)
                        && result1.User.PasswordChangeUrl == result2.User.PasswordChangeUrl
                        && result1.User.PasswordExpiresOn == result2.User.PasswordExpiresOn
                        && result1.User.UniqueId == result2.User.UniqueId)));
        }

        private static bool AreStringsEqual(string str1, string str2)
        {
            return (str1 == str2 || string.IsNullOrEmpty(str1) && string.IsNullOrEmpty(str2));
        }

        private static void AddToDictionary(TokenCache tokenCache, TokenCacheKey key, AuthenticationResultEx value)
        {
            tokenCache.OnBeforeAccess(new TokenCacheNotificationArgs { TokenCache = tokenCache });
            tokenCache.OnBeforeWrite(new TokenCacheNotificationArgs { TokenCache = tokenCache });
            tokenCache.tokenCacheDictionary.Add(key, value);
            tokenCache.HasStateChanged = true;
            tokenCache.OnAfterAccess(new TokenCacheNotificationArgs { TokenCache = tokenCache });

        }

        private static bool RemoveFromDictionary(TokenCache tokenCache, TokenCacheKey key)
        {
            tokenCache.OnBeforeAccess(new TokenCacheNotificationArgs { TokenCache = tokenCache });
            tokenCache.OnBeforeWrite(new TokenCacheNotificationArgs { TokenCache = tokenCache });
            bool result = tokenCache.tokenCacheDictionary.Remove(key);
            tokenCache.HasStateChanged = true;
            tokenCache.OnAfterAccess(new TokenCacheNotificationArgs { TokenCache = tokenCache });

            return result;
        }

        public static AuthenticationResultEx GenerateRandomCacheValue(int maxFieldSize)
        {
            return new AuthenticationResultEx
            {
                Result = new AuthenticationResult(
                    null,
                    GenerateRandomString(maxFieldSize),
                    new DateTimeOffset(DateTime.Now + TimeSpan.FromSeconds(ValidExpiresIn)))
                {
                    User = new User { UniqueId = GenerateRandomString(maxFieldSize), DisplayableId = GenerateRandomString(maxFieldSize) }
                },
                RefreshToken = GenerateRandomString(maxFieldSize)
            };
        }
    }
}
