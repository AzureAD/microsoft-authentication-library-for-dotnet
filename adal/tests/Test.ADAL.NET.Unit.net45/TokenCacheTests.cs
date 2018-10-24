//------------------------------------------------------------------------------
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
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Cache;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Cache;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Helpers;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.ADAL.NET.Common;
using Test.Microsoft.Identity.Core.Unit;
using UserCredential = Microsoft.IdentityModel.Clients.ActiveDirectory.UserCredential;

namespace Test.ADAL.Common.Unit
{
    internal class TokenCacheTests
    {
        public const long ValidExpiresIn = 28800;

        private const string InvalidResource = "00000003-0000-0ff1-ce00-000000000001";

        private const string ValidClientId = "87002806-c87a-41cd-896b-84ca5690d29f";

        private const string ValidResource = "00000003-0000-0ff1-ce00-000000000000";

        private const string ValidAccessToken =
            "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJhdWQiOiIwMDAwMDAwMy0wQ.9p6zqloui6PY31Wg6SJpgt2YS-pGWKjHd-0bw_LcuFo";

        // Passing a seed to make repro possible
        private static readonly Random Rand = new Random(42);

        public static void DefaultTokenCacheTest()
        {
            AuthenticationContext context = new AuthenticationContext("https://login.windows.net/dummy", false);
            var cache = context.TokenCache;
            cache.Clear();
            Log.Comment("====== Verifying that cache is empty...");
            VerifyCacheItemCount(cache, 0);

            const string DisplayableId = "testuser@microsoft.com";
            Log.Comment("====== Creating a set of keys and values for the test...");
            AdalTokenCacheKey key = new AdalTokenCacheKey("https://localhost/MockSts", ValidResource, ValidClientId,
                TokenSubjectType.User, null, DisplayableId);
            var value = CreateCacheValue(key.UniqueId, key.DisplayableId);
            Log.Comment(string.Format(CultureInfo.CurrentCulture, " Cache Key (with User): {0}", key));
            Log.Comment(string.Format(CultureInfo.CurrentCulture, " Cache Value 1: {0}", value));
            AdalTokenCacheKey key2 = new AdalTokenCacheKey("https://localhost/MockSts", InvalidResource, ValidClientId,
                TokenSubjectType.User, null, DisplayableId);
            var value2 = CreateCacheValue(null, DisplayableId);
            Log.Comment(string.Format(CultureInfo.CurrentCulture, " Cache Key (with User): {0}", key));
            Log.Comment(string.Format(CultureInfo.CurrentCulture, " Cache Value 2: {0}", value2));
            AdalTokenCacheKey userlessKey = new AdalTokenCacheKey("https://localhost/MockSts", ValidResource, ValidClientId,
                TokenSubjectType.User, null, null);
            var userlessValue = CreateCacheValue(null, null);
            Log.Comment(string.Format(CultureInfo.CurrentCulture, " Cache Key (withoutUser): {0}", userlessKey));
            Log.Comment(string.Format(CultureInfo.CurrentCulture, " Cache Value 3: {0}", userlessValue));

            AdalTokenCacheKey incorrectUserKey = new AdalTokenCacheKey("https://localhost/MockSts", InvalidResource,
                ValidClientId, TokenSubjectType.User, null, "testuser2@microsoft.com");

            Log.Comment("====== Verifying that cache stores the first key/value pair...");
            AddToDictionary(cache, key, value);
            VerifyCacheItems(cache, 1, key);

            Log.Comment(
                "====== Verifying that the only existing value (with user) is retrieved when requested with user and NOT without...");
            Log.Comment("Retrieving with user...");
            var valueInCache = cache.tokenCacheDictionary[key];
            VerifyAdalResultWrappersAreEqual(value, valueInCache);
            Log.Comment("Retrieving without user...");
            cache.tokenCacheDictionary.TryGetValue(userlessKey, out valueInCache);
            Assert.IsNull(valueInCache);

            Log.Comment("====== Verifying that two entries can exist at the same time, one with user and one without...");
            AddToDictionary(cache, userlessKey, userlessValue);
            VerifyCacheItems(cache, 2, key, userlessKey);

            Log.Comment(
                "====== Verifying that correct values are retrieved when requested with and without user (when two entries exist)...");
            Log.Comment("Retrieving without user...");
            valueInCache = cache.tokenCacheDictionary[userlessKey];
            VerifyAdalResultWrappersAreEqual(userlessValue, valueInCache);
            Log.Comment("Retrieving with user...");
            valueInCache = cache.tokenCacheDictionary[key];
            VerifyAdalResultWrappersAreEqual(value, valueInCache);

            Log.Comment("====== Verifying that correct entry is deleted when the key with user is passed...");
            RemoveFromDictionary(cache, key);
            VerifyCacheItems(cache, 1, userlessKey);

            Log.Comment("====== Verifying that correct entry is deleted when the key without user is passed...");
            AddToDictionary(cache, key, value);
            RemoveFromDictionary(cache, userlessKey);
            VerifyCacheItems(cache, 1, key);

            Log.Comment(
                "====== Verifying that correct entry is retrieve and later deleted when the key with user is passed, even if entries are in reverse order...");
            cache.Clear();
            Log.Comment("Storing without user first and then with user...");
            AddToDictionary(cache, userlessKey, userlessValue);
            AddToDictionary(cache, key2, value2);
            valueInCache = cache.tokenCacheDictionary[key2];
            VerifyAdalResultWrappersAreEqual(value2, valueInCache);
            RemoveFromDictionary(cache, key2);
            VerifyCacheItems(cache, 1, userlessKey);

            Log.Comment("====== Verifying that the userless entry is retrieved ONLY when requested without user...");
            cache.Clear();
            AddToDictionary(cache, userlessKey, value);
            Log.Comment("Retrieving with user...");
            cache.tokenCacheDictionary.TryGetValue(key, out valueInCache);
            Assert.IsNull(valueInCache);
            Log.Comment("Retrieving without user...");
            valueInCache = cache.tokenCacheDictionary[userlessKey];
            VerifyAdalResultWrappersAreEqual(value, valueInCache);

            Log.Comment("====== Verifying that entry cannot be retrieved with incorrect key...");
            cache.Clear();
            AddToDictionary(cache, key, value);
            Log.Comment("Retrieving with incorrect key...");
            cache.tokenCacheDictionary.TryGetValue(key2, out valueInCache);
            Assert.IsNull(valueInCache);
            Log.Comment("Retrieving with incorrect user...");
            cache.tokenCacheDictionary.TryGetValue(incorrectUserKey, out valueInCache);
            Assert.IsNull(valueInCache);
            Log.Comment("Retrieving with correct user...");
            valueInCache = cache.tokenCacheDictionary[key];
            VerifyAdalResultWrappersAreEqual(value, valueInCache);

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

#if !NET_CORE // Platform Behaviour
        /// <summary>
        /// Check when there are multiple users in the cache with the same
        /// authority, clientId, resource but different unique and displayId's that
        /// we can correctly get them from the cache without a multiple token
        /// detected exception.
        /// </summary>
        /// <returns></returns>
        public static async Task TestUniqueIdDisplayableIdLookupAsync()
        {
            string authority = "https://www.gotjwt.com/";
            string tenantId = Guid.NewGuid().ToString();
            string uniqueId = Guid.NewGuid().ToString();
            string displayableId = Guid.NewGuid().ToString();
            Uri redirectUri = new Uri("https://www.GetJwt.com");

            var authenticationResult = CreateCacheValue(uniqueId, displayableId);
            authority = authority + tenantId + "/";
            UserCredential credential = new UserCredential(displayableId);
            AuthenticationContext tempContext = new AuthenticationContext(authority, false);
            var localCache = tempContext.TokenCache;
            localCache.Clear();

            // Add first user into cache
            string resource = Guid.NewGuid().ToString();
            string clientId = Guid.NewGuid().ToString();
            uniqueId = Guid.NewGuid().ToString();
            displayableId = Guid.NewGuid().ToString();
            var cacheValue = CreateCacheValue(uniqueId, displayableId);
            AddToDictionary(localCache,
                new AdalTokenCacheKey(authority, resource, clientId, TokenSubjectType.User, uniqueId, displayableId),
                cacheValue);

            //Add second user into cache
            uniqueId = Guid.NewGuid().ToString();
            displayableId = Guid.NewGuid().ToString();
            cacheValue = CreateCacheValue(uniqueId, displayableId);
            AddToDictionary(localCache,
                new AdalTokenCacheKey(authority, resource, clientId, TokenSubjectType.User, uniqueId, displayableId),
                cacheValue);

            var acWithLocalCache = new AuthenticationContext(authority, false, localCache);
            var userId = new UserIdentifier(uniqueId, UserIdentifierType.UniqueId);
            var userIdUpper = new UserIdentifier(displayableId.ToUpper(CultureInfo.InvariantCulture), UserIdentifierType.RequiredDisplayableId);

            var parameters = new PlatformParameters(PromptBehavior.Auto);
            var authenticationResultFromCache = await acWithLocalCache.AcquireTokenAsync(resource, clientId, redirectUri, parameters, userId).ConfigureAwait(false);
            VerifyAuthenticationResultsAreEqual(new AuthenticationResult(cacheValue.Result), authenticationResultFromCache);

            authenticationResultFromCache = await acWithLocalCache.AcquireTokenAsync(resource, clientId, redirectUri, parameters, userIdUpper).ConfigureAwait(false);
            VerifyAuthenticationResultsAreEqual(new AuthenticationResult(cacheValue.Result), authenticationResultFromCache);

            authenticationResultFromCache = await acWithLocalCache.AcquireTokenSilentAsync(resource, clientId, userId).ConfigureAwait(false);
            VerifyAuthenticationResultsAreEqual(new AuthenticationResult(cacheValue.Result), authenticationResultFromCache);

            authenticationResultFromCache = await acWithLocalCache.AcquireTokenSilentAsync(resource, clientId, userIdUpper).ConfigureAwait(false);
            VerifyAuthenticationResultsAreEqual(new AuthenticationResult(cacheValue.Result), authenticationResultFromCache);
        }

#endif

        public static async Task TokenCacheKeyTestAsync(IPlatformParameters parameters)
        {
            CheckPublicGetSets();

            string authority = "https://www.gotjwt.com/";
            string clientId = Guid.NewGuid().ToString();
            string resource = Guid.NewGuid().ToString();
            string tenantId = Guid.NewGuid().ToString();
            string uniqueId = Guid.NewGuid().ToString();
            string displayableId = Guid.NewGuid().ToString();
            Uri redirectUri = new Uri("https://www.GetJwt.com");

            AdalResultWrapper authenticationResult = CreateCacheValue(uniqueId, displayableId);
            authority = authority + tenantId + "/";
            UserCredential credential = new UserCredential(displayableId);
            AuthenticationContext tempContext = new AuthenticationContext(authority, false);
            var localCache = tempContext.TokenCache;
            localCache.Clear();

            // @Resource, Credential
            AdalTokenCacheKey tokenCacheKey = new AdalTokenCacheKey(authority, resource, clientId, TokenSubjectType.User,
                uniqueId, displayableId);
            AddToDictionary(localCache, tokenCacheKey, authenticationResult);
            AuthenticationContext acWithLocalCache = new AuthenticationContext(authority, false, localCache);
            AuthenticationResult authenticationResultFromCache =
                await acWithLocalCache.AcquireTokenAsync(resource, clientId, credential).ConfigureAwait(false);
            AreAuthenticationResultsEqual(new AuthenticationResult(authenticationResult.Result), authenticationResultFromCache);

            // Duplicate throws error
            authenticationResult.Result.UserInfo.UniqueId = null;
            AddToDictionary(localCache,
                new AdalTokenCacheKey(authority, resource, clientId, TokenSubjectType.User, null, displayableId),
                authenticationResult);


            var adae = AssertException.TaskThrows<AdalException>(() =>
                acWithLocalCache.AcquireTokenAsync(resource, clientId, credential));
            Assert.IsTrue(adae.ErrorCode == "multiple_matching_tokens_detected" &&
                            adae.Message.Contains("The cache contains multiple tokens satisfying the requirements"));


            adae = AssertException.TaskThrows<AdalException>(async () =>
            {
                AuthenticationContext acWithDefaultCache = new AuthenticationContext(authority, false);
                await acWithDefaultCache.AcquireTokenAsync(resource, clientId, credential).ConfigureAwait(false);
                Assert.Fail("Exception expected");
            });
            Assert.IsTrue(adae.ErrorCode == "multiple_matching_tokens_detected" &&
                              adae.Message.Contains("The cache contains multiple tokens satisfying the requirements"));

            // @resource && @clientId
            acWithLocalCache = new AuthenticationContext(authority, false, localCache);
            localCache.Clear();
            var cacheValue = CreateCacheValue(uniqueId, displayableId);
            resource = Guid.NewGuid().ToString();
            clientId = Guid.NewGuid().ToString();

            AdalTokenCacheKey tempKey = new AdalTokenCacheKey(authority, resource, clientId, TokenSubjectType.User, null, null);
            AddToDictionary(localCache, tempKey, cacheValue);
            RemoveFromDictionary(localCache, tempKey);
            Assert.IsFalse(localCache.tokenCacheDictionary.ContainsKey(tempKey));
            AddToDictionary(localCache, tempKey, cacheValue);

            authenticationResultFromCache =
                await acWithLocalCache.AcquireTokenAsync(resource, clientId, redirectUri, parameters).ConfigureAwait(false);
            VerifyAuthenticationResultsAreEqual(new AuthenticationResult(cacheValue.Result), authenticationResultFromCache);

            // @resource && @clientId && userId
            acWithLocalCache = new AuthenticationContext(authority, false, localCache);
            localCache.Clear();
            resource = Guid.NewGuid().ToString();
            clientId = Guid.NewGuid().ToString();
            uniqueId = Guid.NewGuid().ToString();
            displayableId = Guid.NewGuid().ToString();
            cacheValue = CreateCacheValue(uniqueId, displayableId);
            AddToDictionary(localCache,
                new AdalTokenCacheKey(authority, resource, clientId, TokenSubjectType.User, uniqueId, displayableId),
                cacheValue);

            var userId = new UserIdentifier(uniqueId, UserIdentifierType.UniqueId);
            var userIdUpper = new UserIdentifier(displayableId.ToUpper(), UserIdentifierType.RequiredDisplayableId);

            authenticationResultFromCache = await acWithLocalCache.AcquireTokenSilentAsync(resource, clientId, userId).ConfigureAwait(false);
            VerifyAuthenticationResultsAreEqual(new AuthenticationResult(cacheValue.Result), authenticationResultFromCache);

            authenticationResultFromCache =
                await acWithLocalCache.AcquireTokenSilentAsync(resource, clientId, userIdUpper).ConfigureAwait(false);
            VerifyAuthenticationResultsAreEqual(new AuthenticationResult(cacheValue.Result), authenticationResultFromCache);

            authenticationResultFromCache = await acWithLocalCache.AcquireTokenSilentAsync(resource, clientId).ConfigureAwait(false);
            VerifyAuthenticationResultsAreEqual(new AuthenticationResult(cacheValue.Result), authenticationResultFromCache);
        }

        internal static void TokenCacheCrossTenantOperationsTest()
        {
            var tokenCache = new TokenCache();
            var cacheDictionary = tokenCache.tokenCacheDictionary;
            tokenCache.Clear();

            AdalTokenCacheKey key = new AdalTokenCacheKey("https://localhost/MockSts/", "resource1", "client1",
                TokenSubjectType.User, null, "user1");
            AdalResultWrapper value = CreateCacheValue(null, "user1");
        }

        internal static async Task TokenCacheOperationsTestAsync()
        {
            var tokenCache = new TokenCache();
            var cacheDictionary = tokenCache.tokenCacheDictionary;

            tokenCache.Clear();

            AdalTokenCacheKey key = new AdalTokenCacheKey("https://localhost/MockSts/", "resource1", "client1",
                TokenSubjectType.User, null, "user1");
            AdalTokenCacheKey key2 = new AdalTokenCacheKey("https://localhost/MockSts/", "resource1", "client1",
                TokenSubjectType.User, null, "user2");
            AdalTokenCacheKey key3 = new AdalTokenCacheKey("https://localhost/MockSts/", "resource1", "client1",
                TokenSubjectType.UserPlusClient, null, "user1");
            Assert.AreNotEqual(key, key3);

            var value = CreateCacheValue(null, "user1");
            AdalResultWrapper value2;
            do
            {
                value2 = CreateCacheValue(null, "user2");
            } while (value2 == value);

            Assert.AreEqual(0, cacheDictionary.Count);
            AddToDictionary(tokenCache, key, value);
            Assert.AreEqual(1, cacheDictionary.Count);
            var valueInCache = cacheDictionary[key];
            VerifyAdalResultWrappersAreEqual(valueInCache, value);
            VerifyAdalResultWrappersAreNotEqual(valueInCache, value2);
            cacheDictionary[key] = value2;
            Assert.AreEqual(1, cacheDictionary.Count);
            valueInCache = cacheDictionary[key];
            VerifyAdalResultWrappersAreEqual(valueInCache, value2);
            VerifyAdalResultWrappersAreNotEqual(valueInCache, value);

            // Duplicate key -> should fail to add again
            AssertException.Throws<ArgumentException>(() =>
                AddToDictionary(tokenCache, key, value));


            Log.Comment(
                "====== Verifying that correct values are retrieved when requested for different tenant with user and without user");
            CacheQueryData data = new CacheQueryData()
            {
                Authority = "https://localhost/MockSts1",
                Resource = "resource1",
                ClientId = "client1",
                UniqueId = null,
                DisplayableId = "user1",
                SubjectType = TokenSubjectType.User
            };

            AdalResultWrapper resultEx = await tokenCache.LoadFromCacheAsync(data, new RequestContext(new AdalLogger(new Guid()))).ConfigureAwait(false);
            Assert.IsNotNull(resultEx);


            Assert.IsTrue(RemoveFromDictionary(tokenCache, key));
            Assert.IsFalse(RemoveFromDictionary(tokenCache, key));
            Assert.AreEqual(0, cacheDictionary.Count);

            AddToDictionary(tokenCache, key, value);
            AddToDictionary(tokenCache, key2, value2);
            Assert.AreEqual(2, cacheDictionary.Count);
            Assert.AreEqual(cacheDictionary[key], value);
            Assert.AreEqual(cacheDictionary[key2], value2);


            // Null key -> error
            AssertException.Throws<ArgumentNullException>(() =>
                AddToDictionary(tokenCache, null, value));


            // Null key -> error
            AssertException.Throws<ArgumentNullException>(() =>
                AddToDictionary(tokenCache, null, value));


            Assert.IsFalse(cacheDictionary.IsReadOnly);

            var keys = cacheDictionary.Keys.ToList();
            var values = cacheDictionary.Values.ToList();
            Assert.AreEqual(2, keys.Count);
            Assert.AreEqual(2, values.Count);
            if (keys[0].Equals(key))
            {
                Assert.AreEqual(keys[1], key2);
                Assert.AreEqual(values[0], value);
                Assert.AreEqual(values[1], value2);
            }
            else
            {
                Assert.AreEqual(keys[0], key2);
                Assert.AreEqual(keys[1], key);
                Assert.AreEqual(values[0], value2);
                Assert.AreEqual(values[1], value);
            }

            Assert.IsTrue(cacheDictionary.ContainsKey(key));
            Assert.IsTrue(cacheDictionary.ContainsKey(key2));
            Assert.IsFalse(cacheDictionary.ContainsKey(key3));

            Assert.IsTrue(cacheDictionary.Contains(new KeyValuePair<AdalTokenCacheKey, AdalResultWrapper>(key, value)));
            Assert.IsTrue(cacheDictionary.Contains(new KeyValuePair<AdalTokenCacheKey, AdalResultWrapper>(key2, value2)));
            Assert.IsFalse(cacheDictionary.Contains(new KeyValuePair<AdalTokenCacheKey, AdalResultWrapper>(key, value2)));
            Assert.IsFalse(cacheDictionary.Contains(new KeyValuePair<AdalTokenCacheKey, AdalResultWrapper>(key2, value)));


            // Duplicate key -> error
            AssertException.Throws<ArgumentException>(() =>
                AddToDictionary(tokenCache, key, value));


            AddToDictionary(tokenCache, key3, value);
            Assert.AreEqual(3, cacheDictionary.Keys.Count);
            Assert.IsTrue(cacheDictionary.ContainsKey(key3));

            var cacheItemsCopy = new KeyValuePair<AdalTokenCacheKey, AdalResultWrapper>[cacheDictionary.Count + 1];
            cacheDictionary.CopyTo(cacheItemsCopy, 1);
            for (int i = 0; i < cacheDictionary.Count; i++)
            {
                Assert.AreEqual(cacheItemsCopy[i + 1].Value, cacheDictionary[cacheItemsCopy[i + 1].Key]);
            }


            AssertException.Throws<ArgumentException>(() =>
                cacheDictionary.CopyTo(cacheItemsCopy, 2));

            AssertException.Throws<ArgumentOutOfRangeException>(() =>
                cacheDictionary.CopyTo(cacheItemsCopy, -1));


            RemoveFromDictionary(tokenCache, key2);
            Assert.AreEqual(2, cacheDictionary.Keys.Count);

            foreach (var kvp in cacheDictionary)
            {
                Assert.IsTrue(kvp.Key.Equals(key) || kvp.Key.Equals(key3));
                Assert.IsTrue(kvp.Value.Equals(value));
            }

            AdalResultWrapper cacheValue;
            Assert.IsTrue(cacheDictionary.TryGetValue(key, out cacheValue));
            Assert.AreEqual(cacheValue, value);
            Assert.IsTrue(cacheDictionary.TryGetValue(key3, out cacheValue));
            Assert.AreEqual(cacheValue, value);
            Assert.IsFalse(cacheDictionary.TryGetValue(key2, out cacheValue));

            cacheDictionary.Clear();
            Assert.AreEqual(0, cacheDictionary.Keys.Count);
        }

        internal static async Task MultipleUserAssertionHashTestAsync()
        {
            AdalTokenCacheKey key = new AdalTokenCacheKey("https://localhost/MockSts/", "resource1", "client1",
                TokenSubjectType.Client, null, "user1");
            AdalTokenCacheKey key2 = new AdalTokenCacheKey("https://localhost/MockSts/", "resource1", "client1",
                TokenSubjectType.Client, null, "user2");
            AdalResultWrapper value = CreateCacheValue(null, "user1");
            value.UserAssertionHash = "hash1";
            AdalResultWrapper value2 = CreateCacheValue(null, "user2");
            value2.UserAssertionHash = "hash2";

            TokenCache cache = new TokenCache();
            cache.tokenCacheDictionary[key] = value;
            cache.tokenCacheDictionary[key2] = value2;
            CacheQueryData data = new CacheQueryData()
            {
                AssertionHash = "hash1",
                Authority = "https://localhost/MockSts/",
                Resource = "resource1",
                ClientId = "client1",
                SubjectType = TokenSubjectType.Client,
                UniqueId = null,
                DisplayableId = null
            };

            AdalResultWrapper resultEx = await cache.LoadFromCacheAsync(data, new RequestContext(new AdalLogger(new Guid()))).ConfigureAwait(false);
            AreAdalResultWrappersEqual(value, resultEx);

            data.AssertionHash = "hash2";
            resultEx = await cache.LoadFromCacheAsync(data, new RequestContext(new AdalLogger(new Guid()))).ConfigureAwait(false);
            AreAdalResultWrappersEqual(value2, resultEx);

            data.AssertionHash = null;

            // Multiple tokens in cache -> error
            var exc = AssertException.TaskThrows<AdalException>(async () =>
                await cache.LoadFromCacheAsync(data, new RequestContext(new AdalLogger(new Guid()))).ConfigureAwait(false));
            Assert.AreEqual(exc.ErrorCode, AdalError.MultipleTokensMatched);
        }

        internal static void TokenCacheCapacityTest()
        {
            var tokenCache = new TokenCache();
            tokenCache.Clear();

            const int MaxItemCount = 100;
            AdalTokenCacheKey[] keys = new AdalTokenCacheKey[MaxItemCount];
            AdalResultWrapper[] values = new AdalResultWrapper[MaxItemCount];

            for (int i = 0; i < MaxItemCount; i++)
            {
                keys[i] = GenerateRandomTokenCacheKey();

                values[i] = CreateCacheValue(null, null);
                AddToDictionary(tokenCache, keys[i], values[i]);
            }

            Assert.AreEqual(MaxItemCount, tokenCache.Count);

            for (int i = 0; i < MaxItemCount; i++)
            {
                AdalResultWrapper cacheValue;
                int index = MaxItemCount - i - 1;
                Assert.IsTrue(tokenCache.tokenCacheDictionary.TryGetValue(keys[index], out cacheValue));
                Assert.AreEqual(values[index], cacheValue);
                RemoveFromDictionary(tokenCache, keys[index]);
                Assert.AreEqual(index, tokenCache.Count);
            }

            tokenCache.Clear();
        }

        internal static void TokenCacheValueSplitTest()
        {
            var tokenCache = new TokenCache();
            AdalTokenCacheKey key = new AdalTokenCacheKey("https://localhost/MockSts", "resourc1", "client1",
                TokenSubjectType.User, null, "user1");

            tokenCache.Clear();
            AddToDictionary(tokenCache, key, null);
            Assert.AreEqual(tokenCache.tokenCacheDictionary[key], null);
            for (int len = 0; len < 3000; len++)
            {
                var value = CreateCacheValue(null, "user1");
                tokenCache.Clear();
                AddToDictionary(tokenCache, key, value);
                Assert.AreEqual(tokenCache.tokenCacheDictionary[key], value);
            }
        }

        internal static void TokenCacheSerializationTest()
        {
            var context = new AuthenticationContext("https://login.windows.net/common", false);
            var tokenCache = context.TokenCache;
            const int MaxItemCount = 100;
            const int MaxFieldSize = 1024;
            tokenCache.Clear();
            for (int count = 0; count < Rand.Next(1, MaxItemCount); count++)
            {
                AdalTokenCacheKey key = GenerateRandomTokenCacheKey();
                AdalResultWrapper result = GenerateRandomCacheValue(MaxFieldSize, key.UniqueId, key.DisplayableId);
                AddToDictionary(tokenCache, key, result);
            }

            byte[] serializedCache = tokenCache.Serialize();
            TokenCache tokenCache2 = new TokenCache(serializedCache);
            Assert.AreEqual(tokenCache.Count, tokenCache2.Count);
            foreach (TokenCacheItem item in tokenCache.ReadItems())
            {
                var item2 = tokenCache2.ReadItems().FirstOrDefault(it => it.AccessToken == item.AccessToken);
                Assert.IsNotNull(item2);
                double diff = Math.Abs((item.ExpiresOn - item2.ExpiresOn).TotalSeconds);
                Assert.IsTrue((1.0 - diff) >= 0);
            }

            foreach (var key in tokenCache.tokenCacheDictionary.Keys)
            {
                AdalResultWrapper result2 = tokenCache2.tokenCacheDictionary[key];
                VerifyAdalResultWrappersAreEqual(tokenCache.tokenCacheDictionary[key], result2);
            }
        }

        public static AdalResultWrapper CreateCacheValue(string uniqueId, string displayableId)
        {
            string refreshToken = string.Format(CultureInfo.CurrentCulture, " RefreshToken{0}", Rand.Next());
            var result = new AdalResult(null, ValidAccessToken,
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)))
            {
                UserInfo = new AdalUserInfo { UniqueId = uniqueId, DisplayableId = displayableId }
            };

            return new AdalResultWrapper
            {
                Result = result,
                RefreshToken = refreshToken
            };
        }

        public static void CheckPublicGetSets()
        {
            AdalTokenCacheKey tokenCacheKey = new AdalTokenCacheKey("Authority", "Resource", "ClientId", TokenSubjectType.User,
                "UniqueId", "DisplayableId");

            Assert.IsTrue(tokenCacheKey.Authority == "Authority");
            Assert.IsTrue(tokenCacheKey.ClientId == "ClientId");
            Assert.IsTrue(tokenCacheKey.Resource == "Resource");
            Assert.IsTrue(tokenCacheKey.UniqueId == "UniqueId");
            Assert.IsTrue(tokenCacheKey.DisplayableId == "DisplayableId");
            Assert.IsTrue(tokenCacheKey.TokenSubjectType == TokenSubjectType.User);
        }

        private static void VerifyCacheItemCount(TokenCache cache, int expectedCount)
        {
            Assert.AreEqual(cache.Count, expectedCount);
        }

        private static void VerifyCacheItems(TokenCache cache, int expectedCount, AdalTokenCacheKey firstKey)
        {
            VerifyCacheItems(cache, expectedCount, firstKey, null);
        }

        private static void VerifyCacheItems(TokenCache cache, int expectedCount, AdalTokenCacheKey firstKey,
            AdalTokenCacheKey secondKey)
        {
            var items = cache.ReadItems().ToList();
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

        private static bool AreEqual(TokenCacheItem item, AdalTokenCacheKey key)
        {
            return item.Match(key);
        }

        private static void VerifyAdalResultWrappersAreEqual(AdalResultWrapper resultEx1,
            AdalResultWrapper resultEx2)
        {
            Assert.IsTrue(AreAdalResultWrappersEqual(resultEx1, resultEx2));
        }

        private static void VerifyAdalResultWrappersAreNotEqual(AdalResultWrapper resultEx1,
            AdalResultWrapper resultEx2)
        {
            Assert.IsFalse(AreAdalResultWrappersEqual(resultEx1, resultEx2));
        }

        private static void VerifyAuthenticationResultsAreEqual(AuthenticationResult result1,
            AuthenticationResult result2)
        {
            Assert.IsTrue(AreAuthenticationResultsEqual(result1, result2));
        }

        private static void VerifyAuthenticationResultsAreNotEqual(AuthenticationResult result1,
            AuthenticationResult result2)
        {
            Assert.IsFalse(AreAuthenticationResultsEqual(result1, result2));
        }

        private static bool AreAdalResultWrappersEqual(AdalResultWrapper resultEx1,
            AdalResultWrapper resultEx2)
        {
            return AreAuthenticationResultsEqual(new AuthenticationResult(resultEx1.Result), new AuthenticationResult(resultEx2.Result)) &&
                   resultEx1.RefreshToken == resultEx2.RefreshToken &&
                   resultEx1.IsMultipleResourceRefreshToken == resultEx2.IsMultipleResourceRefreshToken &&
                   resultEx1.UserAssertionHash == resultEx2.UserAssertionHash;
        }

        private static bool AreAuthenticationResultsEqual(AuthenticationResult result1, AuthenticationResult result2)
        {
            return (AreStringsEqual(result1.AccessToken, result2.AccessToken)
                    && AreStringsEqual(result1.AccessTokenType, result2.AccessTokenType)
                    && AreStringsEqual(result1.IdToken, result2.IdToken)
                    && AreStringsEqual(result1.TenantId, result2.TenantId)
                    && (result1.UserInfo == null || result2.UserInfo == null ||
                        (AreStringsEqual(result1.UserInfo.DisplayableId, result2.UserInfo.DisplayableId)
                         && AreStringsEqual(result1.UserInfo.FamilyName, result2.UserInfo.FamilyName)
                         && AreStringsEqual(result1.UserInfo.GivenName, result2.UserInfo.GivenName)
                         && AreStringsEqual(result1.UserInfo.IdentityProvider, result2.UserInfo.IdentityProvider)
                         && result1.UserInfo.PasswordChangeUrl == result2.UserInfo.PasswordChangeUrl
                         && result1.UserInfo.PasswordExpiresOn == result2.UserInfo.PasswordExpiresOn
                         && result1.UserInfo.UniqueId == result2.UserInfo.UniqueId)));
        }

        private static bool AreStringsEqual(string str1, string str2)
        {
            return (str1 == str2 || string.IsNullOrEmpty(str1) && string.IsNullOrEmpty(str2));
        }

        private static void AddToDictionary(TokenCache tokenCache, AdalTokenCacheKey key, AdalResultWrapper value)
        {
            tokenCache.OnBeforeAccess(new TokenCacheNotificationArgs { TokenCache = tokenCache });
            tokenCache.OnBeforeWrite(new TokenCacheNotificationArgs { TokenCache = tokenCache });
            tokenCache.tokenCacheDictionary.Add(key, value);
            tokenCache.HasStateChanged = true;
            tokenCache.OnAfterAccess(new TokenCacheNotificationArgs { TokenCache = tokenCache });
        }

        private static bool RemoveFromDictionary(TokenCache tokenCache, AdalTokenCacheKey key)
        {
            tokenCache.OnBeforeAccess(new TokenCacheNotificationArgs { TokenCache = tokenCache });
            tokenCache.OnBeforeWrite(new TokenCacheNotificationArgs { TokenCache = tokenCache });
            bool result = tokenCache.tokenCacheDictionary.Remove(key);
            tokenCache.HasStateChanged = true;
            tokenCache.OnAfterAccess(new TokenCacheNotificationArgs { TokenCache = tokenCache });

            return result;
        }

        private static AdalTokenCacheKey GenerateRandomTokenCacheKey()
        {
            return new AdalTokenCacheKey(Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                TokenSubjectType.User,
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString());
        }

        public static AdalResultWrapper GenerateRandomCacheValue(int maxFieldSize, string uniqueId,
            string displayableId)
        {
            return new AdalResultWrapper
            {
                Result = new AdalResult(
                    null,
                    GenerateRandomString(maxFieldSize),
                    new DateTimeOffset(DateTime.Now + TimeSpan.FromSeconds(ValidExpiresIn)))
                {
                    UserInfo = new AdalUserInfo { UniqueId = uniqueId, DisplayableId = displayableId }
                },
                RefreshToken = GenerateRandomString(maxFieldSize),
                UserAssertionHash = Guid.NewGuid().ToString()
            };
        }

        public static void TokenCacheBackCompatTest(byte[] oldcache)
        {
            TokenCache cache = new TokenCache(oldcache);
            Assert.IsNotNull(cache);
            foreach (var value in cache.tokenCacheDictionary.Values)
            {
                Assert.IsNull(value.UserAssertionHash);
            }
        }

        public static void ParallelStorePositiveTest(byte[] oldcache)
        {
            TokenCache cache = new TokenCache(oldcache);
            cache.BeforeAccess = DoBefore;
            cache.AfterAccess = DoAfter;
            Task readTask = Task.Run(() => cache.ReadItems());
            Task writeTask = Task.Run(() => cache.Clear());
            readTask.Wait();
            writeTask.Wait();
        }

        private static int _count = 0;

        private static void DoBefore(TokenCacheNotificationArgs args)
        {
            _count++;
        }

        private static void DoAfter(TokenCacheNotificationArgs args)
        {
            Assert.AreEqual(1, _count);
            _count--;
        }

        public static void TokenCacheClearTest(byte[] oldcache)
        {
            CoreLoggerBase.Default = null;
            TokenCache cache = new TokenCache();
            //Verifying default constructor sets CoreLoggerBase.Default
            cache.Clear();

            CoreLoggerBase.Default = null;
            TokenCache cache2 = new TokenCache(oldcache);
            //Verifying overloaded constructor sets CoreLoggerBase.Default
            cache.Clear();
        }
    }
}
