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
        // Passing a seed to make repro possible
        private static readonly Random Rand = new Random(42);


        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void LoadFromCacheExpiredToken()
        {
            TokenCache cache = new TokenCache();
            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultScope, TestConstants.DefaultClientId, TestConstants.DefaultTokenSubjectType,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId,
                TestConstants.DefaultPolicy);
            AuthenticationResultEx ex = new AuthenticationResultEx();
            ex.Result = new AuthenticationResult("Bearer", key.ToString(), new DateTimeOffset(DateTime.UtcNow));
            ex.RefreshToken = "someRT";
            cache.tokenCacheDictionary[key] = ex;

            AuthenticationResultEx resultEx = cache.LoadFromCache(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultScope, TestConstants.DefaultClientId, TestConstants.DefaultTokenSubjectType,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId,
                TestConstants.DefaultPolicy, null);
            Assert.IsNotNull(resultEx);
            Assert.IsNotNull(resultEx.Result);
            Assert.IsNull(resultEx.Result.AccessToken);
            Assert.AreEqual(resultEx.RefreshToken, "someRT");

        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void LoadFromCacheIntersectingScopeDifferentAuthorities()
        {
            TokenCache cache = new TokenCache();
            loadCacheItems(cache);
            HashSet<string> scope = new HashSet<string>(new[] { "r1/scope1" });

            AuthenticationResultEx resultEx = cache.LoadFromCache(TestConstants.DefaultAuthorityHomeTenant,
                scope, TestConstants.DefaultClientId, TestConstants.DefaultTokenSubjectType,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId,
                TestConstants.DefaultPolicy, null);
            Assert.IsNotNull(resultEx);
            Assert.IsTrue(resultEx.Result.AccessToken.Contains(string.Format("Scope:{0},", TestConstants.DefaultScope.CreateSingleStringFromSet())));

            scope.Add("r1/unique-scope");
            //look for intersection. only RT will be returned for refresh_token grant flow.
            resultEx = cache.LoadFromCache(TestConstants.DefaultAuthorityHomeTenant,
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
        public void LoadFromCacheFamilyOfClientIdToken()
        {
            //this test will result only in a RT and no access token returned.
            var tokenCache = new TokenCache();
            loadCacheItems(tokenCache);

            AuthenticationResultEx resultEx =
                tokenCache.LoadFromCache(TestConstants.DefaultAuthorityGuestTenant + "non-existant",
                    new HashSet<string>(new[] { "r1/scope1"}),
                    TestConstants.DefaultClientId, TestConstants.DefaultTokenSubjectType, null, null,
                    TestConstants.DefaultRootId, TestConstants.DefaultPolicy, null);
            Assert.IsNotNull(resultEx);
            Assert.IsNotNull(resultEx.Result);
            Assert.IsNull(resultEx.Result.AccessToken);
            Assert.AreEqual(resultEx.Result.ExpiresOn, DateTimeOffset.MinValue);
            Assert.AreEqual(resultEx.RefreshToken, "someRT");
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void LoadFromCacheCrossTenantToken()
        {
            //this test will result only in a RT and no access token returned.
            var tokenCache = new TokenCache();
            loadCacheItems(tokenCache);

            AuthenticationResultEx resultEx =
                tokenCache.LoadFromCache(TestConstants.DefaultAuthorityGuestTenant,
                    new HashSet<string>(new[] { "r1/scope1", "random-scope" }),
                    TestConstants.DefaultClientId+"more", TestConstants.DefaultTokenSubjectType, null, null,
                    TestConstants.DefaultRootId, TestConstants.DefaultPolicy, null);
            Assert.IsNotNull(resultEx);
            Assert.IsNotNull(resultEx.Result);
            Assert.IsNull(resultEx.Result.AccessToken);
            Assert.AreEqual(resultEx.Result.ExpiresOn, DateTimeOffset.MinValue);
            Assert.AreEqual(resultEx.RefreshToken, "someRT");
        }


        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void LoadSingleItemFromCacheMatchingScopeDifferentAuthorities()
        {
            TokenCache cache = new TokenCache();
            loadCacheItems(cache);
            KeyValuePair<TokenCacheKey, AuthenticationResultEx>? item = cache.LoadSingleItemFromCache(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultScope, TestConstants.DefaultClientId, TestConstants.DefaultTokenSubjectType,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId,
                TestConstants.DefaultPolicy, null);
            Assert.IsNotNull(item);
            TokenCacheKey key = item.Value.Key;
            AuthenticationResultEx resultEx = item.Value.Value;

            Assert.AreEqual(TestConstants.DefaultAuthorityHomeTenant, key.Authority);
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
        public void LoadSingleItemFromCacheFamilyOfClientIdTest()
        {
            TokenCache cache = new TokenCache();
            loadCacheItems(cache);

            //lookup is for guest tenant authority, but the RT will be returned for home tenant authority because it is participating in FoCI feature.
            KeyValuePair<TokenCacheKey, AuthenticationResultEx>? item =
                cache.LoadSingleItemFromCache(TestConstants.DefaultAuthorityGuestTenant,
                    TestConstants.DefaultScope, TestConstants.DefaultClientId+"more", TestConstants.DefaultTokenSubjectType,
                    TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId,
                    TestConstants.DefaultPolicy, null);

            Assert.IsNotNull(item);
            TokenCacheKey key = item.Value.Key;
            AuthenticationResultEx resultEx = item.Value.Value;

            Assert.AreEqual(TestConstants.DefaultAuthorityHomeTenant, key.Authority);
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
        public void LoadSingleItemFromCacheNonExistantScopeDifferentAuthorities()
        {
            TokenCache cache = new TokenCache();
            loadCacheItems(cache);
            HashSet<string> scope = new HashSet<string>(new[] { "nonexistant-scope" });

            KeyValuePair<TokenCacheKey, AuthenticationResultEx>? item = cache.LoadSingleItemFromCache(TestConstants.DefaultAuthorityHomeTenant,
                scope, TestConstants.DefaultClientId, TestConstants.DefaultTokenSubjectType,
                null, null, TestConstants.DefaultRootId,
                TestConstants.DefaultPolicy, null);
            Assert.IsNotNull(item);
            TokenCacheKey key = item.Value.Key;
            AuthenticationResultEx resultEx = item.Value.Value;

            Assert.AreEqual(TestConstants.DefaultAuthorityHomeTenant, key.Authority);
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
            HashSet<string> scope = new HashSet<string>(new[] {"r1/scope1"});

            KeyValuePair<TokenCacheKey, AuthenticationResultEx>? item = cache.LoadSingleItemFromCache(TestConstants.DefaultAuthorityHomeTenant,
                scope, TestConstants.DefaultClientId, TestConstants.DefaultTokenSubjectType,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId,
                TestConstants.DefaultPolicy, null);
            Assert.IsNotNull(item);
            TokenCacheKey key = item.Value.Key;
            AuthenticationResultEx resultEx = item.Value.Value;

            Assert.AreEqual(TestConstants.DefaultAuthorityHomeTenant, key.Authority);
            Assert.AreEqual(TestConstants.DefaultScope, key.Scope);
            Assert.AreEqual(TestConstants.DefaultClientId, key.ClientId);
            Assert.AreEqual(TestConstants.DefaultTokenSubjectType, key.TokenSubjectType);
            Assert.AreEqual(TestConstants.DefaultUniqueId, key.UniqueId);
            Assert.AreEqual(TestConstants.DefaultDisplayableId, key.DisplayableId);
            Assert.AreEqual(TestConstants.DefaultRootId, key.RootId);
            Assert.AreEqual(TestConstants.DefaultPolicy, key.Policy);
            Assert.AreEqual(key.ToString(), resultEx.Result.AccessToken);

            scope.Add("unique-scope");
            item = cache.LoadSingleItemFromCache(TestConstants.DefaultAuthorityHomeTenant,
                scope, TestConstants.DefaultClientId, TestConstants.DefaultTokenSubjectType,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId,
                TestConstants.DefaultPolicy, null);

            Assert.IsNotNull(item);
            key = item.Value.Key;
            resultEx = item.Value.Value;

            Assert.AreEqual(TestConstants.DefaultAuthorityHomeTenant, key.Authority);
            Assert.AreEqual(TestConstants.DefaultScope, key.Scope); //default scope contains r1/scope1
            Assert.AreEqual(TestConstants.DefaultClientId, key.ClientId);
            Assert.AreEqual(TestConstants.DefaultTokenSubjectType, key.TokenSubjectType);
            Assert.AreEqual(TestConstants.DefaultUniqueId, key.UniqueId);
            Assert.AreEqual(TestConstants.DefaultDisplayableId, key.DisplayableId);
            Assert.AreEqual(TestConstants.DefaultRootId, key.RootId);
            Assert.AreEqual(TestConstants.DefaultPolicy, key.Policy);
            Assert.AreEqual(key.ToString(), resultEx.Result.AccessToken);


            //invoke multiple tokens error
            TokenCacheKey cacheKey = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultScope, TestConstants.DefaultClientId, TestConstants.DefaultTokenSubjectType,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId+ "more", TestConstants.DefaultRootId,
                TestConstants.DefaultPolicy);
            AuthenticationResultEx ex = new AuthenticationResultEx();
            ex.Result = new AuthenticationResult("Bearer", key.ToString(), new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)));
            ex.RefreshToken = "someRT";
            cache.tokenCacheDictionary[cacheKey] = ex;

            try
            {
                item = cache.LoadSingleItemFromCache(TestConstants.DefaultAuthorityHomeTenant,
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
            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultScope, TestConstants.DefaultClientId, TestConstants.DefaultTokenSubjectType,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId,
                TestConstants.DefaultPolicy);
            AuthenticationResultEx ex = new AuthenticationResultEx();
            ex.Result = new AuthenticationResult("Bearer", key.ToString(), new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)));
            ex.Result.User = new User { DisplayableId = TestConstants.DefaultDisplayableId, UniqueId = TestConstants.DefaultUniqueId, RootId = TestConstants.DefaultRootId };
            ex.Result.FamilyId = "1";
            ex.RefreshToken = "someRT";
            cache.tokenCacheDictionary[key] = ex;

            key = new TokenCacheKey(TestConstants.DefaultAuthorityGuestTenant,
                TestConstants.ScopeForAnotherResource, TestConstants.DefaultClientId, TestConstants.DefaultTokenSubjectType,
                TestConstants.DefaultUniqueId + "more", TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId,
                TestConstants.DefaultPolicy);
            ex = new AuthenticationResultEx();
            ex.Result = new AuthenticationResult("Bearer", key.ToString(), new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)));
            ex.Result.User = new User{ DisplayableId = TestConstants.DefaultDisplayableId, UniqueId = TestConstants.DefaultUniqueId + "more", RootId = TestConstants.DefaultRootId };
            ex.RefreshToken = "someRT";
            cache.tokenCacheDictionary[key] = ex;
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void LoadSingleItemFromCacheCrossTenantLookupTest()
        {
            var tokenCache = new TokenCache();
            loadCacheItems(tokenCache);

            //cross-tenant works by default. search cache using non-existant authority
            //using root id. Code will find multiple results with the same root id. it can return any.
            KeyValuePair<TokenCacheKey, AuthenticationResultEx>? item =
                tokenCache.LoadSingleItemFromCache(TestConstants.DefaultAuthorityGuestTenant + "non-existant",
                    new HashSet<string>(new[] {"scope1", "random-scope"}),
                    TestConstants.DefaultClientId, TestConstants.DefaultTokenSubjectType, null, null,
                    TestConstants.DefaultRootId, TestConstants.DefaultPolicy, null);
            Assert.IsNotNull(item);
            TokenCacheKey key = item.Value.Key;
            AuthenticationResultEx resultEx = item.Value.Value;

            Assert.AreEqual(TestConstants.DefaultAuthorityHomeTenant, key.Authority);
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
        public void TokenCacheValueSplitTest()
        {
            var tokenCache = new TokenCache();
            TokenCacheKey key = new TokenCacheKey("https://localhost/MockSts", new HashSet<string>(new[] { "resourc1" }), "client1", TokenSubjectType.User, null, "user1");

            tokenCache.Clear("client1");
            AddToDictionary(tokenCache, key, null);
            Assert.AreEqual(tokenCache.tokenCacheDictionary[key], null);
            for (int len = 0; len < 3000; len++)
            {
                var value = CreateCacheValue(null, "user1");
                tokenCache.Clear("client1");
                AddToDictionary(tokenCache, key, value);
                Assert.AreEqual(tokenCache.tokenCacheDictionary[key], value);
            }
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void ReadItemsTest()
        {
            var tokenCache = new TokenCache();
            loadCacheItems(tokenCache);
            IEnumerable<TokenCacheItem> items = tokenCache.ReadItems(TestConstants.DefaultClientId);
            Assert.AreEqual(2, items.Count());
            Assert.AreEqual(TestConstants.DefaultUniqueId, items.Where(item => item.Authority.Equals(TestConstants.DefaultAuthorityHomeTenant)).First().UniqueId);
            Assert.AreEqual(TestConstants.DefaultUniqueId + "more", items.Where(item => item.Authority.Equals(TestConstants.DefaultAuthorityGuestTenant)).First().UniqueId);
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void DeleteItemTest()
        {
            var tokenCache = new TokenCache();
            loadCacheItems(tokenCache);
            try
            {
                tokenCache.DeleteItem(null);
                Assert.Fail("ArgumentNullException should have been thrown");
            }
            catch (ArgumentNullException)
            {
                
            }
            KeyValuePair<TokenCacheKey, AuthenticationResultEx>? kvp =
                tokenCache.LoadSingleItemFromCache(TestConstants.DefaultAuthorityHomeTenant,
                    TestConstants.DefaultScope, TestConstants.DefaultClientId, TestConstants.DefaultTokenSubjectType,
                    TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId,
                    TestConstants.DefaultPolicy, null);

            TokenCacheItem item = new TokenCacheItem(kvp.Value.Key, kvp.Value.Value.Result);
            tokenCache.DeleteItem(item);
            Assert.AreEqual(1, tokenCache.Count);

            IEnumerable<TokenCacheItem> items = tokenCache.ReadItems(TestConstants.DefaultClientId);
            Assert.AreEqual(TestConstants.DefaultUniqueId + "more", items.Where(entry => entry.Authority.Equals(TestConstants.DefaultAuthorityGuestTenant)).First().UniqueId);
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void SerializationDeserializationTest()
        {
            var tokenCache1 = new TokenCache();
            loadCacheItems(tokenCache1);
            byte[] cacheBytes = tokenCache1.Serialize();
            Assert.IsNotNull(cacheBytes);
            Assert.IsTrue(cacheBytes.Length > 0);

            var tokenCache2 = new TokenCache(cacheBytes);
            Assert.AreEqual(tokenCache1.Count, tokenCache2.Count);
            
            foreach(TokenCacheKey key in tokenCache1.tokenCacheDictionary.Keys)
            {
                Assert.IsTrue(tokenCache2.tokenCacheDictionary.ContainsKey(key));
                AuthenticationResultEx result1 = tokenCache1.tokenCacheDictionary[key];
                AuthenticationResultEx result2 = tokenCache2.tokenCacheDictionary[key];

                Assert.AreEqual(result1.RefreshToken, result2.RefreshToken);
                Assert.AreEqual(result1.Exception, result2.Exception);
                Assert.AreEqual(result1.IsMultipleScopeRefreshToken, result2.IsMultipleScopeRefreshToken);
                Assert.AreEqual(result1.ScopeInResponse, result2.ScopeInResponse);
                Assert.AreEqual(result1.Result.AccessToken, result2.Result.AccessToken);
                Assert.AreEqual(result1.Result.FamilyId, result2.Result.FamilyId);
                Assert.AreEqual(result1.Result.AccessTokenType, result2.Result.AccessTokenType);
                Assert.AreEqual(result1.Result.IdToken, result2.Result.IdToken);
                Assert.AreEqual(result1.Result.User.DisplayableId, result2.Result.User.DisplayableId);
                Assert.AreEqual(result1.Result.User.UniqueId, result2.Result.User.UniqueId);
                Assert.AreEqual(result1.Result.User.RootId, result2.Result.User.RootId);
                Assert.AreEqual(result1.Result.User.PasswordChangeUrl, result2.Result.User.PasswordChangeUrl);
                Assert.AreEqual(result1.Result.User.IdentityProvider, result2.Result.User.IdentityProvider);
                Assert.IsTrue(AreDateTimeOffsetsEqual(result1.Result.ExpiresOn, result2.Result.ExpiresOn));
            }
        }



        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void DeserializationNullAndEmptyBlobTest()
        {
            var tokenCache = new TokenCache(null);
            Assert.IsNotNull(tokenCache);
            Assert.IsNotNull(tokenCache.Count);

            tokenCache = new TokenCache(new byte[] {});
            Assert.IsNotNull(tokenCache);
            Assert.IsNotNull(tokenCache.Count);
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void StoreToCacheIntersectingScopesTest()
        {
            var tokenCache = new TokenCache();
            loadCacheItems(tokenCache);

            //save result with intersecting scopes
            var result = new AuthenticationResult("Bearer", "some-access-token", new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)))
            {
                User = new User { UniqueId = TestConstants.DefaultUniqueId, DisplayableId = TestConstants.DefaultDisplayableId }
            };

            AuthenticationResultEx resultEx = new AuthenticationResultEx
            {
                Result = result,
                RefreshToken = "someRT",
                ScopeInResponse = new HashSet<string>(new string[] { "r1/scope1", "r1/scope5" })
            };

            tokenCache.StoreToCache(resultEx, TestConstants.DefaultAuthorityHomeTenant, TestConstants.DefaultClientId,
                TestConstants.DefaultTokenSubjectType, TestConstants.DefaultPolicy, null);

            Assert.AreEqual(2, tokenCache.Count);
            AuthenticationResultEx resultExOut = 
                tokenCache.LoadFromCache(TestConstants.DefaultAuthorityHomeTenant,
                new HashSet<string>(new string[] {"r1/scope5"}), TestConstants.DefaultClientId, 
                TestConstants.DefaultTokenSubjectType, null, null, null, TestConstants.DefaultPolicy, null);

            Assert.AreEqual(resultEx.RefreshToken, resultExOut.RefreshToken);
            Assert.AreEqual(resultEx.Result.AccessToken, resultExOut.Result.AccessToken);
            Assert.AreEqual(resultEx.Result.AccessTokenType, resultExOut.Result.AccessTokenType);
            Assert.AreEqual(resultEx.Result.User.UniqueId, resultExOut.Result.User.UniqueId);
            Assert.AreEqual(resultEx.Result.User.DisplayableId, resultExOut.Result.User.DisplayableId);
            Assert.AreEqual(resultEx.Result.User.RootId, resultExOut.Result.User.RootId);
        }


        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void StoreToCacheClientCredentialTest()
        {
            var tokenCache = new TokenCache();
            loadCacheItems(tokenCache);
            
            var result = new AuthenticationResult("Bearer", "some-access-token", new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)))
            {
                User = null
            };

            AuthenticationResultEx resultEx = new AuthenticationResultEx
            {
                Result = result,
                RefreshToken = null,
                ScopeInResponse = new HashSet<string>(new string[] { "r1/scope1" })
            };

            //scope should not intersect with existing entry because it is a different token subject type.
            tokenCache.StoreToCache(resultEx, TestConstants.DefaultAuthorityHomeTenant, TestConstants.DefaultClientId,
                TokenSubjectType.Client, TestConstants.DefaultPolicy, null);

            Assert.AreEqual(3, tokenCache.Count);
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void StoreToCacheUniqueScopesTest()
        {
            var tokenCache = new TokenCache();
            tokenCache.AfterAccess = null;
            tokenCache.BeforeAccess = null;
            tokenCache.BeforeWrite = null;
            loadCacheItems(tokenCache);

            //save result with intersecting scopes
            var result = new AuthenticationResult("Bearer", "some-access-token", new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)))
            {
                User = new User { UniqueId = TestConstants.DefaultUniqueId, DisplayableId = TestConstants.DefaultDisplayableId }
            };

            AuthenticationResultEx resultEx = new AuthenticationResultEx
            {
                Result = result,
                RefreshToken = "someRT",
                ScopeInResponse = new HashSet<string>(new string[] { "r1/scope5", "r1/scope7" })
            };

            tokenCache.StoreToCache(resultEx, TestConstants.DefaultAuthorityHomeTenant, TestConstants.DefaultClientId,
                TestConstants.DefaultTokenSubjectType, TestConstants.DefaultPolicy, null);

            Assert.AreEqual(3, tokenCache.Count);
            AuthenticationResultEx resultExOut =
                tokenCache.LoadFromCache(TestConstants.DefaultAuthorityHomeTenant,
                new HashSet<string>(new string[] { "r1/scope5" }), TestConstants.DefaultClientId,
                TestConstants.DefaultTokenSubjectType, null, null, null, TestConstants.DefaultPolicy, null);

            Assert.AreEqual(resultEx.RefreshToken, resultExOut.RefreshToken);
            Assert.AreEqual(resultEx.Result.AccessToken, resultExOut.Result.AccessToken);
            Assert.AreEqual(resultEx.Result.AccessTokenType, resultExOut.Result.AccessTokenType);
            Assert.AreEqual(resultEx.Result.User.UniqueId, resultExOut.Result.User.UniqueId);
            Assert.AreEqual(resultEx.Result.User.DisplayableId, resultExOut.Result.User.DisplayableId);
            Assert.AreEqual(resultEx.Result.User.RootId, resultExOut.Result.User.RootId);
        }

        internal AuthenticationResultEx CreateCacheValue(string uniqueId, string displayableId)
        {
            string refreshToken = string.Format("RefreshToken{0}", Rand.Next());
            var result = new AuthenticationResult(null, "some-access-token", new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)))
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
            Assert.AreEqual(cache.Count, expectedCount);
        }

        private static void VerifyCacheItems(TokenCache cache, int expectedCount, TokenCacheKey firstKey)
        {
            VerifyCacheItems(cache, expectedCount, firstKey, null);
        }

        private static void VerifyCacheItems(TokenCache cache, int expectedCount, TokenCacheKey firstKey, TokenCacheKey secondKey)
        {
            var items = cache.ReadItems(TestConstants.DefaultClientId).ToList();
            Assert.AreEqual(expectedCount, items.Count);

            if (firstKey != null)
            {
                Assert.IsTrue(AreEqual(items[0], firstKey) || AreEqual(items[0], secondKey));
            }

            if (secondKey != null)
            {
                Assert.IsTrue(AreEqual(items[1], firstKey) || AreEqual(items[1], secondKey));
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
            Assert.IsTrue(AreAuthenticationResultExsEqual(resultEx1, resultEx2));
        }

        private static void VerifyAuthenticationResultExsAreNotEqual(AuthenticationResultEx resultEx1, AuthenticationResultEx resultEx2)
        {
            Assert.IsFalse(AreAuthenticationResultExsEqual(resultEx1, resultEx2));
        }

        private static void VerifyAuthenticationResultsAreEqual(AuthenticationResult result1, AuthenticationResult result2)
        {
            Assert.IsTrue(AreAuthenticationResultsEqual(result1, result2));
        }

        private static void VerifyAuthenticationResultsAreNotEqual(AuthenticationResult result1, AuthenticationResult result2)
        {
            Assert.IsFalse(AreAuthenticationResultsEqual(result1, result2));
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
                        && AreStringsEqual(result1.User.Name, result2.User.Name)
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
