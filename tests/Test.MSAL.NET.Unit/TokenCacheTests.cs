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
        public static long ValidExpiresIn = 3600;
        // Passing a seed to make repro possible
        private static readonly Random Rand = new Random(42);

        private TokenCachePlugin _tokenCachePlugin;

        [TestInitialize]
        public void TestInitialize()
        {
            _tokenCachePlugin = (TokenCachePlugin) PlatformPlugin.TokenCachePlugin;
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
                TestConstants.Scope, TestConstants.ClientId, TestConstants.HomeObjectId);

            AccessTokenCacheItem atItem = new AccessTokenCacheItem()
            {
                TokenType = "Bearer",
                AccessToken = atKey.ToString(),
                ExpiresOnUnixTimestamp = MsalHelpers.DateTimeToUnixTimestamp(DateTime.UtcNow)
            };
            _tokenCachePlugin.TokenCacheDictionary[atKey.ToString()] = JsonHelper.SerializeToJson(atItem);

            Assert.IsNull(cache.FindAccessToken(new AuthenticationRequestParameters()
            {
                ClientId = TestConstants.ClientId,
                Authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false),
                Scope = TestConstants.Scope,
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
            RefreshTokenCacheItem rtItem = new RefreshTokenCacheItem()
            {
                Authority = TestConstants.AuthorityHomeTenant,
                ClientId = TestConstants.ClientId,
                RefreshToken = "someRT",
                RawIdToken = MockHelpers.DefaultIdToken,
                User = new User
                {
                    DisplayableId = TestConstants.DisplayableId,
                    UniqueId = TestConstants.UniqueId,
                    HomeObjectId = TestConstants.HomeObjectId
                }
            };

            TokenCacheKey rtKey = rtItem.GetTokenCacheKey();
            _tokenCachePlugin.TokenCacheDictionary[rtKey.ToString()] = JsonHelper.SerializeToJson(rtItem);
            Assert.IsNotNull(cache.FindRefreshToken(new AuthenticationRequestParameters()
            {
                ClientId = TestConstants.ClientId,
                Authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false),
                Scope = TestConstants.Scope,
                User =
                    new User()
                    {
                        UniqueId = TestConstants.UniqueId,
                        DisplayableId = TestConstants.DisplayableId,
                        HomeObjectId = TestConstants.HomeObjectId
                    }
            }));

            // RT is stored only by client id and home object id as index.
            // any change to authority, uniqueid and displyableid will not 
            // outcome of cache look up.
            Assert.IsNotNull(cache.FindRefreshToken(new AuthenticationRequestParameters()
            {
                ClientId = TestConstants.ClientId,
                Authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant + "more", false),
                Scope = TestConstants.Scope,
                User =
                    new User()
                    {
                        UniqueId = TestConstants.UniqueId + "more",
                        DisplayableId = TestConstants.DisplayableId + "more",
                        HomeObjectId = TestConstants.HomeObjectId
                    }
            }));
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void GetAppTokenFromCacheTest()
        {
            TokenCache tokenCache = new TokenCache(TestConstants.ClientId);
            AccessTokenCacheItem item = new AccessTokenCacheItem()
            {
                Authority = TestConstants.AuthorityHomeTenant,
                ClientId = TestConstants.ClientId,
                TokenType = "Bearer",
                ExpiresOnUnixTimestamp =
                    MsalHelpers.DateTimeToUnixTimestamp(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)),
                RawIdToken = null,
                User = null,
                Scope = TestConstants.Scope
            };
            item.AccessToken = item.GetTokenCacheKey().ToString();
            _tokenCachePlugin.TokenCacheDictionary[item.GetTokenCacheKey().ToString()] = JsonHelper.SerializeToJson(item);

            AccessTokenCacheItem cacheItem = tokenCache.FindAccessToken(new AuthenticationRequestParameters()
            {
                Authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false),
                ClientId = TestConstants.ClientId,
                ClientCredential= TestConstants.CredentialWithSecret,
                Scope = TestConstants.Scope
            });

            Assert.IsNotNull(cacheItem);
            Assert.AreEqual(item.GetTokenCacheKey(), cacheItem.GetTokenCacheKey());
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
