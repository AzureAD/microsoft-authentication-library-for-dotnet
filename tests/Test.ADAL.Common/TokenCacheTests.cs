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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.ADAL.NET.Unit;

namespace Test.ADAL.Common.Unit
{
    [TestClass]
    public class TokenCacheTests
    {
        public static long ValidExpiresIn = 28800;

        private static string[] InvalidResource = new []{ "00000003-0000-0ff1-ce00-000000000001" };

        private static string ValidClientId = "87002806-c87a-41cd-896b-84ca5690d29f";

        private static string[] ValidResource = new[] { "00000003-0000-0ff1-ce00-000000000000" };

        private static string ValidAccessToken =
            "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJhdWQiOiIwMDAwMDAwMy0wMDAwLTBmZjEtY2UwMC0wMDAwMDAwMDAwMDAiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC8wMDAwMDAwMS0wMDAwLTBmZjEtY2UwMC0wMDAwMDAwMDAwMDAvIiwibmJmIjoxMzU4MjIwODkxLCJleHAiOjEzNTgyNDk2OTEsImFjciI6IjEiLCJwcm4iOiI2OWQyNDU0NC1jNDIwLTQ3MjEtYTRiZi0xMDZmMjM3OGQ5ZjYiLCJ0aWQiOiIwMDAwMDAwMS0wMDAwLTBmZjEtY2UwMC0wMDAwMDAwMDAwMDAiLCJpYXQiOiIxMzU4MjIwODkxIiwiYXBwaWQiOiIwMDAwMDAwMC0wMDAwLTAwMDAtMDAwMC0wMDAwMDAwMDAwMDAiLCJhcHBpZGFjciI6IjAiLCJzY3AiOiJzYW1wbGUgc2NvcGVzIiwidiI6IjIifQ.9p6zqloui6PY31Wg6SJpgt2YS-pGWKjHd-0bw_LcuFo";

        // Passing a seed to make repro possible
        private static readonly Random Rand = new Random(42);


        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void LoadFromCacheExpiredToken()
        {
            TokenCache cache = new TokenCache();
            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityCommon,
                TestConstants.DefaultScope, TestConstants.DefaultClientId, TestConstants.DefaultTokenSubjectType,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId,
                TestConstants.DefaultPolicy);
            AuthenticationResultEx ex = new AuthenticationResultEx();
            ex.Result = new AuthenticationResult("Bearer", key.ToString(), new DateTimeOffset(DateTime.UtcNow));
            ex.RefreshToken = "someRT";
            cache.tokenCacheDictionary[key] = ex;

            AuthenticationResultEx resultEx = cache.LoadFromCache(TestConstants.DefaultAuthorityCommon,
                TestConstants.DefaultScope, TestConstants.DefaultClientId, TestConstants.DefaultTokenSubjectType,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId,
                TestConstants.DefaultPolicy, null);
            Assert.IsNotNull(resultEx);
            Assert.IsNotNull(resultEx.Result);
            Assert.AreEqual(resultEx.RefreshToken, "someRT");

        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void LoadFromCacheIntersectingScopeDifferentAuthorities()
        {
            TokenCache cache = new TokenCache();
            loadCacheItems(cache);
            HashSet<string> scope = new HashSet<string>(new[] { "scope1" });

            AuthenticationResultEx resultEx = cache.LoadFromCache(TestConstants.DefaultAuthorityCommon,
                scope, TestConstants.DefaultClientId, TestConstants.DefaultTokenSubjectType,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId,
                TestConstants.DefaultPolicy, null);
            Assert.IsNotNull(resultEx);
            Assert.IsTrue(resultEx.Result.AccessToken.Contains(string.Format("Scope:{0},", TestConstants.DefaultScope.CreateSingleStringFromSet())));

            scope.Add("unique-scope");
            //look for intersection. only RT will be returned for refresh_token grant flow.
            resultEx = cache.LoadFromCache(TestConstants.DefaultAuthorityCommon,
                scope, TestConstants.DefaultClientId, TestConstants.DefaultTokenSubjectType,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId,
                TestConstants.DefaultPolicy, null);
            Assert.IsNotNull(resultEx);
            Assert.IsNotNull(resultEx.Result);
            Assert.AreEqual(resultEx.Result.ExpiresOn, DateTimeOffset.MinValue);
            Assert.AreEqual(resultEx.RefreshToken, "someRT");
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void LoadSingleItemFromCacheMatchingScopeDifferentAuthorities()
        {
            TokenCache cache = new TokenCache();
            loadCacheItems(cache);
            KeyValuePair<TokenCacheKey, AuthenticationResultEx>? item = cache.LoadSingleItemFromCache(TestConstants.DefaultAuthorityCommon,
                TestConstants.DefaultScope, TestConstants.DefaultClientId, TestConstants.DefaultTokenSubjectType,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId,
                TestConstants.DefaultPolicy, null);
            Assert.IsNotNull(item);
            TokenCacheKey key = item.Value.Key;
            AuthenticationResultEx resultEx = item.Value.Value;

            Assert.AreEqual(TestConstants.DefaultAuthorityCommon, key.Authority);
            Assert.AreEqual(TestConstants.DefaultScope, key.Scope);
            Assert.AreEqual(TestConstants.DefaultClientId, key.ClientId);
            Assert.AreEqual(TestConstants.DefaultTokenSubjectType, key.TokenSubjectType);
            Assert.AreEqual(TestConstants.DefaultUniqueId, key.UniqueId);
            Assert.AreEqual(TestConstants.DefaultDisplayableId, key.DisplayableId);
            Assert.AreEqual(TestConstants.DefaultRootId, key.RootId);
            Assert.AreEqual(TestConstants.DefaultPolicy, key.Policy);

            Assert.AreEqual(key.ToString(), resultEx.Result.AccessToken);
        }


        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void LoadSingleItemFromCacheIntersectingScopeDifferentAuthorities()
        {
            TokenCache cache = new TokenCache();
            loadCacheItems(cache);
            HashSet<string> scope = new HashSet<string>(new[] {"scope1"});

            KeyValuePair<TokenCacheKey, AuthenticationResultEx>? item = cache.LoadSingleItemFromCache(TestConstants.DefaultAuthorityCommon,
                scope, TestConstants.DefaultClientId, TestConstants.DefaultTokenSubjectType,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId,
                TestConstants.DefaultPolicy, null);
            Assert.IsNotNull(item);
            TokenCacheKey key = item.Value.Key;
            AuthenticationResultEx resultEx = item.Value.Value;

            Assert.AreEqual(TestConstants.DefaultAuthorityCommon, key.Authority);
            Assert.AreEqual(TestConstants.DefaultScope, key.Scope);
            Assert.AreEqual(TestConstants.DefaultClientId, key.ClientId);
            Assert.AreEqual(TestConstants.DefaultTokenSubjectType, key.TokenSubjectType);
            Assert.AreEqual(TestConstants.DefaultUniqueId, key.UniqueId);
            Assert.AreEqual(TestConstants.DefaultDisplayableId, key.DisplayableId);
            Assert.AreEqual(TestConstants.DefaultRootId, key.RootId);
            Assert.AreEqual(TestConstants.DefaultPolicy, key.Policy);
            Assert.AreEqual(key.ToString(), resultEx.Result.AccessToken);

            scope.Add("unique-scope");
            item = cache.LoadSingleItemFromCache(TestConstants.DefaultAuthorityCommon,
                scope, TestConstants.DefaultClientId, TestConstants.DefaultTokenSubjectType,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId,
                TestConstants.DefaultPolicy, null);
            Assert.IsNotNull(item);
            key = item.Value.Key;
            resultEx = item.Value.Value;

            Assert.AreEqual(TestConstants.DefaultAuthorityCommon, key.Authority);
            Assert.AreEqual(TestConstants.DefaultScope, key.Scope);
            Assert.AreEqual(TestConstants.DefaultClientId, key.ClientId);
            Assert.AreEqual(TestConstants.DefaultTokenSubjectType, key.TokenSubjectType);
            Assert.AreEqual(TestConstants.DefaultUniqueId, key.UniqueId);
            Assert.AreEqual(TestConstants.DefaultDisplayableId, key.DisplayableId);
            Assert.AreEqual(TestConstants.DefaultRootId, key.RootId);
            Assert.AreEqual(TestConstants.DefaultPolicy, key.Policy);
            Assert.AreEqual(key.ToString(), resultEx.Result.AccessToken);


            //invoke multiple tokens error
            TokenCacheKey cacheKey = new TokenCacheKey(TestConstants.DefaultAuthorityCommon,
                TestConstants.DefaultScope, TestConstants.DefaultClientId, TestConstants.DefaultTokenSubjectType,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId+ "more", TestConstants.DefaultRootId,
                TestConstants.DefaultPolicy);
            AuthenticationResultEx ex = new AuthenticationResultEx();
            ex.Result = new AuthenticationResult("Bearer", key.ToString(), new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)));
            ex.RefreshToken = "someRT";
            cache.tokenCacheDictionary[cacheKey] = ex;

            try
            {
                item = cache.LoadSingleItemFromCache(TestConstants.DefaultAuthorityCommon,
                    TestConstants.DefaultScope, TestConstants.DefaultClientId, TestConstants.DefaultTokenSubjectType,
                    TestConstants.DefaultUniqueId, null, TestConstants.DefaultRootId,
                    TestConstants.DefaultPolicy, null);
                Assert.Fail("multiple tokens should have been detected");
            }
            catch (MsalException exception)
            {
                Assert.AreEqual("multiple_matching_tokens_detected", exception.ErrorCode);
            }
        }




        private void loadCacheItems(TokenCache cache)
        {
            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityCommon,
                TestConstants.DefaultScope, TestConstants.DefaultClientId, TestConstants.DefaultTokenSubjectType,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId,
                TestConstants.DefaultPolicy);
            AuthenticationResultEx ex = new AuthenticationResultEx();
            ex.Result = new AuthenticationResult("Bearer", key.ToString(), new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)));
            ex.RefreshToken = "someRT";
            cache.tokenCacheDictionary[key] = ex;

            key = new TokenCacheKey(TestConstants.DefaultAuthorityCommon+"more",
                TestConstants.DefaultScope, TestConstants.DefaultClientId, TestConstants.DefaultTokenSubjectType,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId,
                TestConstants.DefaultPolicy);
            ex = new AuthenticationResultEx();
            ex.Result = new AuthenticationResult("Bearer", key.ToString(), new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)));
            ex.RefreshToken = "someRT";
            cache.tokenCacheDictionary[key] = ex;
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void CrossTenantLookupTest()
        {
            var tokenCache = new TokenCache();
            var cacheDictionary = tokenCache.tokenCacheDictionary;
            tokenCache.Clear();

            TokenCacheKey key = new TokenCacheKey("https://localhost/MockSts/", new HashSet<string>(new[]{ "resource1" }), "client1", TokenSubjectType.User, null, "user1", null);
            AuthenticationResultEx value = CreateCacheValue(null, "user1");

        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void TokenCacheValueSplitTest()
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

        internal AuthenticationResultEx CreateCacheValue(string uniqueId, string displayableId)
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
                resultEx1.IsMultipleScopeRefreshToken == resultEx2.IsMultipleScopeRefreshToken;
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

        internal AuthenticationResultEx GenerateRandomCacheValue(int maxFieldSize)
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
