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
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Cache;
using Microsoft.Identity.Client.Internal.Instance;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.MSAL.NET.Unit.Mocks;

namespace Test.MSAL.NET.Unit
{
    [TestClass]
    public class TokenCacheTests
    {
        public static long ValidExpiresIn = 28800;
        // Passing a seed to make repro possible
        private static readonly Random Rand = new Random(42);

        private TokenCachePlugin _tokenCachePlugin;

        [TestInitialize]
        public void TestInitialize()
        {
            _tokenCachePlugin = (TokenCachePlugin)PlatformPlugin.TokenCachePlugin;
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _tokenCachePlugin.TokenCacheDictionary.Clear();
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void GetExpiredAccessTokenTest()
        {
            TokenCache cache = new TokenCache(TestConstants.ClientId);
            TokenCacheKey atKey = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                TestConstants.Scope, TestConstants.ClientId,
                TestConstants.UniqueId, TestConstants.DisplayableId, TestConstants.HomeObjectId,
                TestConstants.Policy);

            TokenCacheItem atItem = new TokenCacheItem()
            {
                TokenType = "Bearer",
                Token = atKey.ToString(),
                ExpiresOn = new DateTimeOffset(DateTime.UtcNow)
            };
            _tokenCachePlugin.TokenCacheDictionary[atKey.ToString()] = JsonHelper.SerializeToJson(atItem);

            Assert.IsNull(cache.FindAccessToken(new AuthenticationRequestParameters()
            {
                Authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false),
                Scope = TestConstants.Scope,
                Policy = TestConstants.Policy,
                User =
                    new User()
                    {
                        UniqueId = TestConstants.UniqueId,
                        DisplayableId = TestConstants.DisplayableId,
                        HomeObjectId = TestConstants.HomeObjectId
                    }
            }));
        }
        
        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void GetRefreshTokenTest()
        {

            TokenCache cache = new TokenCache(TestConstants.ClientId);
            TokenCacheKey rtKey = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                TestConstants.Scope, TestConstants.ClientId,
                TestConstants.UniqueId, TestConstants.DisplayableId, TestConstants.HomeObjectId,
                TestConstants.Policy);

            RefreshTokenCacheItem rtItem = new RefreshTokenCacheItem()
            {
                RefreshToken = "someRT"
            };
            _tokenCachePlugin.TokenCacheDictionary[rtKey.ToString()] = JsonHelper.SerializeToJson(rtItem);

            Assert.IsNull(cache.FindRefreshToken(new AuthenticationRequestParameters()
            {
                Authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false),
                Scope = TestConstants.Scope,
                Policy = TestConstants.Policy,
                User =
                    new User()
                    {
                        UniqueId = TestConstants.UniqueId,
                        DisplayableId = TestConstants.DisplayableId,
                        HomeObjectId = TestConstants.HomeObjectId
                    }
            }));
        }
        
        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void ClearCacheTest()
        {

            TokenCacheHelper.PopulateCache(_tokenCachePlugin);
            TokenCache tokenCache = new TokenCache(TestConstants.ClientId);

            TokenCacheKey key = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                TestConstants.Scope, TestConstants.ClientId + "more",
                TestConstants.UniqueId, TestConstants.DisplayableId, TestConstants.HomeObjectId,
                TestConstants.Policy);
            _tokenCachePlugin.TokenCacheDictionary[key.ToString()] = JsonHelper.SerializeToJson(new TokenCacheItem());
            tokenCache.Clear();
            Assert.AreEqual(1, _tokenCachePlugin.TokenCacheDictionary.Count);
            Assert.AreEqual(key, _tokenCachePlugin.TokenCacheDictionary.ContainsKey(key.ToString()));
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void GetAppTokenFromCacheTest()
        {
            TokenCache tokenCache = new TokenCache(TestConstants.ClientId);

            TokenCacheKey key = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
    TestConstants.Scope, TestConstants.ClientId,
    null, null, null, null);
            TokenCacheItem item = new TokenCacheItem()
            {
                Token = key.ToString(),
                TokenType = "Bearer",
                ExpiresOn = new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)),
                Scope = TestConstants.Scope
            };
            _tokenCachePlugin.TokenCacheDictionary[key.ToString()] = JsonHelper.SerializeToJson(item);

            Assert.AreEqual(key.ToString(),
                tokenCache.FindAccessToken(new AuthenticationRequestParameters()
                {
                    Authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false),
                    Scope = TestConstants.Scope
                }).Token);
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
    }
}
