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
using System.Linq;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Cache;
using Microsoft.Identity.Client.Internal.Instance;
using Microsoft.Identity.Client.Internal.OAuth2;
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

        TokenCache cache;

        [TestInitialize]
        public void TestInitialize()
        {
            cache = new TokenCache();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            cache.TokenCacheAccessor.TokenCacheDictionary.Clear();
        }


        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void GetExactScopesMatchedAccessTokenTest()
        {
            TokenCache cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };
            AccessTokenCacheItem atItem = new AccessTokenCacheItem()
            {
                Authority = TestConstants.AuthorityHomeTenant,
                ClientId = TestConstants.ClientId,
                TokenType = "Bearer",
                Scope = TestConstants.Scope,
                ExpiresOnUnixTimestamp = MsalHelpers.DateTimeToUnixTimestamp(DateTime.UtcNow+TimeSpan.FromHours(1)),
                RawIdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId, TestConstants.HomeObjectId)
            };

            // create key out of access token cache item and then
            // set it as the value of the access token.
            TokenCacheKey atKey = atItem.GetTokenCacheKey();
            atItem.AccessToken = atKey.ToString();

            cache.TokenCacheAccessor.TokenCacheDictionary[atKey.ToString()] = JsonHelper.SerializeToJson(atItem);
            AccessTokenCacheItem item = cache.FindAccessToken(new AuthenticationRequestParameters()
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
            });

            Assert.IsNotNull(item);
            Assert.AreEqual(atKey.ToString(), item.AccessToken);
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void GetSubsetScopesMatchedAccessTokenTest()
        {
            TokenCache cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };
            AccessTokenCacheItem atItem = new AccessTokenCacheItem()
            {
                Authority = TestConstants.AuthorityHomeTenant,
                ClientId = TestConstants.ClientId,
                TokenType = "Bearer",
                Scope = TestConstants.Scope,
                ExpiresOnUnixTimestamp = MsalHelpers.DateTimeToUnixTimestamp(DateTime.UtcNow + TimeSpan.FromHours(1)),
                RawIdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId, TestConstants.HomeObjectId)
            };

            // create key out of access token cache item and then
            // set it as the value of the access token.
            TokenCacheKey atKey = atItem.GetTokenCacheKey();
            atItem.AccessToken = atKey.ToString();

            cache.TokenCacheAccessor.TokenCacheDictionary[atKey.ToString()] = JsonHelper.SerializeToJson(atItem);
            var param = new AuthenticationRequestParameters()
            {
                ClientId = TestConstants.ClientId,
                Authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false),
                Scope = new SortedSet<string>(),
                User =
                    new User()
                    {
                        UniqueId = TestConstants.UniqueId,
                        DisplayableId = TestConstants.DisplayableId,
                        HomeObjectId = TestConstants.HomeObjectId
                    }
            };

            param.Scope.Add("r1/scope1");
            AccessTokenCacheItem item = cache.FindAccessToken(param);

            Assert.IsNotNull(item);
            Assert.AreEqual(atKey.ToString(), item.AccessToken);
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void GetIntersectedScopesMatchedAccessTokenTest()
        {
            TokenCache cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };
            AccessTokenCacheItem atItem = new AccessTokenCacheItem()
            {
                Authority = TestConstants.AuthorityHomeTenant,
                ClientId = TestConstants.ClientId,
                TokenType = "Bearer",
                Scope = TestConstants.Scope,
                ExpiresOnUnixTimestamp = MsalHelpers.DateTimeToUnixTimestamp(DateTime.UtcNow + TimeSpan.FromHours(1)),
                RawIdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId, TestConstants.HomeObjectId)
            };

            // create key out of access token cache item and then
            // set it as the value of the access token.
            TokenCacheKey atKey = atItem.GetTokenCacheKey();
            atItem.AccessToken = atKey.ToString();
            cache.TokenCacheAccessor.TokenCacheDictionary[atKey.ToString()] = JsonHelper.SerializeToJson(atItem);

            var param = new AuthenticationRequestParameters()
            {
                ClientId = TestConstants.ClientId,
                Authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false),
                Scope = new SortedSet<string>(),
                User =
                    new User()
                    {
                        UniqueId = TestConstants.UniqueId,
                        DisplayableId = TestConstants.DisplayableId,
                        HomeObjectId = TestConstants.HomeObjectId
                    }
            };

            param.Scope.Add(TestConstants.Scope.First());
            param.Scope.Add("non-existant-scopes");
            AccessTokenCacheItem item = cache.FindAccessToken(param);

            //intersected scopes are not returned.
            Assert.IsNull(item);
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void GetExpiredAccessTokenTest()
        {
            TokenCache cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };
            TokenCacheKey atKey = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                TestConstants.Scope, TestConstants.ClientId, TestConstants.HomeObjectId);

            AccessTokenCacheItem atItem = new AccessTokenCacheItem()
            {
                TokenType = "Bearer",
                AccessToken = atKey.ToString(),
                ExpiresOnUnixTimestamp = MsalHelpers.DateTimeToUnixTimestamp(DateTime.UtcNow)
            };
            cache.TokenCacheAccessor.TokenCacheDictionary[atKey.ToString()] = JsonHelper.SerializeToJson(atItem);

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
        public void GetAccessTokenExpiryInRangeTest()
        {
            TokenCache cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };
            TokenCacheKey atKey = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                TestConstants.Scope, TestConstants.ClientId, TestConstants.HomeObjectId);

            AccessTokenCacheItem atItem = new AccessTokenCacheItem()
            {
                TokenType = "Bearer",
                AccessToken = atKey.ToString(),
                ExpiresOnUnixTimestamp = MsalHelpers.DateTimeToUnixTimestamp(DateTime.UtcNow + TimeSpan.FromMinutes(4))
            };
            cache.TokenCacheAccessor.TokenCacheDictionary[atKey.ToString()] = JsonHelper.SerializeToJson(atItem);

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
            TokenCache cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };
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
            cache.TokenCacheAccessor.TokenCacheDictionary[rtKey.ToString()] = JsonHelper.SerializeToJson(rtItem);
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
            cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };
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
            cache.TokenCacheAccessor.TokenCacheDictionary[item.GetTokenCacheKey().ToString()] = JsonHelper.SerializeToJson(item);

            AccessTokenCacheItem cacheItem = cache.FindAccessToken(new AuthenticationRequestParameters()
            {
                Authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false),
                ClientId = TestConstants.ClientId,
                ClientCredential= TestConstants.CredentialWithSecret,
                Scope = TestConstants.Scope
            });

            Assert.IsNotNull(cacheItem);
            Assert.AreEqual(item.GetTokenCacheKey(), cacheItem.GetTokenCacheKey());
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void GetAccessTokenNoUserAssertionInCacheTest()
        {
            TokenCache cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };
            AccessTokenCacheItem atItem = new AccessTokenCacheItem()
            {
                Authority = TestConstants.AuthorityHomeTenant,
                ClientId = TestConstants.ClientId,
                TokenType = "Bearer",
                Scope = TestConstants.Scope,
                ExpiresOnUnixTimestamp = MsalHelpers.DateTimeToUnixTimestamp(DateTime.UtcNow + TimeSpan.FromHours(1)),
                RawIdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId, TestConstants.HomeObjectId)
            };

            // create key out of access token cache item and then
            // set it as the value of the access token.
            TokenCacheKey atKey = atItem.GetTokenCacheKey();
            atItem.AccessToken = atKey.ToString();

            cache.TokenCacheAccessor.TokenCacheDictionary[atKey.ToString()] = JsonHelper.SerializeToJson(atItem);
            var param = new AuthenticationRequestParameters()
            {
                ClientId = TestConstants.ClientId,
                Authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false),
                Scope = TestConstants.Scope,
                UserAssertion = new UserAssertion(new CryptographyHelper().CreateSha256Hash(atKey.ToString()))
            };
            
            AccessTokenCacheItem item = cache.FindAccessToken(param);

            //cache lookup should fail because there was no userassertion hash in the matched
            //token cache item.

            Assert.IsNull(item);
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void GetAccessTokenUserAssertionMismatchInCacheTest()
        {
            TokenCache cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };
            AccessTokenCacheItem atItem = new AccessTokenCacheItem()
            {
                Authority = TestConstants.AuthorityHomeTenant,
                ClientId = TestConstants.ClientId,
                TokenType = "Bearer",
                Scope = TestConstants.Scope,
                ExpiresOnUnixTimestamp = MsalHelpers.DateTimeToUnixTimestamp(DateTime.UtcNow + TimeSpan.FromHours(1)),
                RawIdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId, TestConstants.HomeObjectId)
            };

            // create key out of access token cache item and then
            // set it as the value of the access token.
            TokenCacheKey atKey = atItem.GetTokenCacheKey();
            atItem.AccessToken = atKey.ToString();
            atItem.UserAssertionHash = new CryptographyHelper().CreateSha256Hash(atKey.ToString());

            cache.TokenCacheAccessor.TokenCacheDictionary[atKey.ToString()] = JsonHelper.SerializeToJson(atItem);
            var param = new AuthenticationRequestParameters()
            {
                ClientId = TestConstants.ClientId,
                Authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false),
                Scope = TestConstants.Scope,
                UserAssertion = new UserAssertion(atItem.UserAssertionHash + "-random")
            };

            AccessTokenCacheItem item = cache.FindAccessToken(param);

            // cache lookup should fail because there was userassertion hash did not match the one
            // stored in token cache item.
            Assert.IsNull(item);
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void GetAccessTokenMatchedUserAssertionInCacheTest()
        {
            TokenCache cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };
            AccessTokenCacheItem atItem = new AccessTokenCacheItem()
            {
                Authority = TestConstants.AuthorityHomeTenant,
                ClientId = TestConstants.ClientId,
                TokenType = "Bearer",
                Scope = TestConstants.Scope,
                ExpiresOnUnixTimestamp = MsalHelpers.DateTimeToUnixTimestamp(DateTime.UtcNow + TimeSpan.FromHours(1)),
                RawIdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId, TestConstants.HomeObjectId)
            };

            // create key out of access token cache item and then
            // set it as the value of the access token.
            TokenCacheKey atKey = atItem.GetTokenCacheKey();
            atItem.AccessToken = atKey.ToString();
            atItem.UserAssertionHash = PlatformPlugin.CryptographyHelper.CreateSha256Hash(atKey.ToString());

            cache.TokenCacheAccessor.TokenCacheDictionary[atKey.ToString()] = JsonHelper.SerializeToJson(atItem);
            var param = new AuthenticationRequestParameters()
            {
                ClientId = TestConstants.ClientId,
                Authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false),
                Scope = TestConstants.Scope,
                UserAssertion = new UserAssertion(atKey.ToString())
            };

            AccessTokenCacheItem item = cache.FindAccessToken(param);

            Assert.IsNotNull(item);
            Assert.AreEqual(atKey.ToString(), item.AccessToken);
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void SaveAccessAndRefreshTokenWithEmptyCacheTest()
        {
            TokenCache cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };

            TokenResponse response = new TokenResponse();
            response.IdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId,
                TestConstants.HomeObjectId);
            response.AccessToken = "access-token";
            response.ExpiresIn = 3599;
            response.CorrelationId = "correlation-id";
            response.RefreshToken = "refresh-token";
            response.Scope = TestConstants.Scope.AsSingleString();
            response.TokenType = "Bearer";
            AuthenticationRequestParameters requestParams = new AuthenticationRequestParameters()
            {
                Authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false),
                ClientId = TestConstants.ClientId
            };

            cache.SaveAccessAndRefreshToken(requestParams, response);

            Assert.AreEqual(1, cache.RefreshTokenCount);
            Assert.AreEqual(1, cache.AccessTokenCount);
        }


        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void SaveAccessAndRefreshTokenWithMoreScopesTest()
        {
            TokenCache cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };

            TokenResponse response = new TokenResponse();
            response.IdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId,
                TestConstants.HomeObjectId);
            response.AccessToken = "access-token";
            response.ExpiresIn = 3599;
            response.CorrelationId = "correlation-id";
            response.RefreshToken = "refresh-token";
            response.Scope = TestConstants.Scope.AsSingleString();
            response.TokenType = "Bearer";

            AuthenticationRequestParameters requestParams = new AuthenticationRequestParameters()
            {
                Authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false),
                ClientId = TestConstants.ClientId
            };

            cache.SaveAccessAndRefreshToken(requestParams, response);

            Assert.AreEqual(1, cache.RefreshTokenCount);
            Assert.AreEqual(1, cache.AccessTokenCount);

            response = new TokenResponse();
            response.IdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId,
                TestConstants.HomeObjectId);
            response.AccessToken = "access-token-2";
            response.ExpiresIn = 3599;
            response.CorrelationId = "correlation-id";
            response.RefreshToken = "refresh-token-2";
            response.Scope = TestConstants.Scope.AsSingleString() + " another-scope";
            response.TokenType = "Bearer";

            cache.SaveAccessAndRefreshToken(requestParams, response);

            Assert.AreEqual(1, cache.RefreshTokenCount);
            Assert.AreEqual(1, cache.AccessTokenCount);

            Assert.AreEqual("refresh-token-2", cache.GetAllRefreshTokens().First().RefreshToken);
            Assert.AreEqual("access-token-2", cache.GetAllAccessTokens().First().AccessToken);
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void SaveAccessAndRefreshTokenWithLessScopesTest()
        {
            TokenCache cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };

            TokenResponse response = new TokenResponse();
            response.IdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId,
                TestConstants.HomeObjectId);
            response.AccessToken = "access-token";
            response.ExpiresIn = 3599;
            response.CorrelationId = "correlation-id";
            response.RefreshToken = "refresh-token";
            response.Scope = TestConstants.Scope.AsSingleString();
            response.TokenType = "Bearer";
            AuthenticationRequestParameters requestParams = new AuthenticationRequestParameters()
            {
                Authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false),
                ClientId = TestConstants.ClientId
            };

            cache.SaveAccessAndRefreshToken(requestParams, response);

            response = new TokenResponse();
            response.IdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId,
                TestConstants.HomeObjectId);
            response.AccessToken = "access-token-2";
            response.ExpiresIn = 3599;
            response.CorrelationId = "correlation-id";
            response.RefreshToken = "refresh-token-2";
            response.Scope = TestConstants.Scope.First();
            response.TokenType = "Bearer";
            
            cache.SaveAccessAndRefreshToken(requestParams, response);

            Assert.AreEqual(1, cache.RefreshTokenCount);
            Assert.AreEqual(1, cache.AccessTokenCount);

            Assert.AreEqual("refresh-token-2", cache.GetAllRefreshTokens().First().RefreshToken);
            Assert.AreEqual("access-token-2", cache.GetAllAccessTokens().First().AccessToken);
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void SaveAccessAndRefreshTokenWithIntersectingScopesTest()
        {
            TokenCache cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };

            TokenResponse response = new TokenResponse();
            response.IdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId,
                TestConstants.HomeObjectId);
            response.AccessToken = "access-token";
            response.ExpiresIn = 3599;
            response.CorrelationId = "correlation-id";
            response.RefreshToken = "refresh-token";
            response.Scope = TestConstants.Scope.AsSingleString();
            response.TokenType = "Bearer";
            
            AuthenticationRequestParameters requestParams = new AuthenticationRequestParameters()
            {
                Authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false),
                ClientId = TestConstants.ClientId
            };

            cache.SaveAccessAndRefreshToken(requestParams, response);

            response = new TokenResponse();
            response.IdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId,
                TestConstants.HomeObjectId);
            response.AccessToken = "access-token-2";
            response.ExpiresIn = 3599;
            response.CorrelationId = "correlation-id";
            response.RefreshToken = "refresh-token-2";
            response.Scope = TestConstants.Scope.First() + " random-scope";
            response.TokenType = "Bearer";

            cache.SaveAccessAndRefreshToken(requestParams, response);

            Assert.AreEqual(1, cache.RefreshTokenCount);
            Assert.AreEqual(1, cache.AccessTokenCount);

            Assert.AreEqual("refresh-token-2", cache.GetAllRefreshTokens().First().RefreshToken);
            Assert.AreEqual("access-token-2", cache.GetAllAccessTokens().First().AccessToken);
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void SaveAccessAndRefreshTokenWithDifferentAuthoritySameUserTest()
        {
            TokenCache cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };

            TokenResponse response = new TokenResponse();
            response.IdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId,
                TestConstants.HomeObjectId);
            response.AccessToken = "access-token";
            response.ExpiresIn = 3599;
            response.CorrelationId = "correlation-id";
            response.RefreshToken = "refresh-token";
            response.Scope = TestConstants.Scope.AsSingleString();
            response.TokenType = "Bearer";
            
            AuthenticationRequestParameters requestParams = new AuthenticationRequestParameters()
            {
                Authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false),
                ClientId = TestConstants.ClientId
            };

            cache.SaveAccessAndRefreshToken(requestParams, response);

            response = new TokenResponse();
            response.IdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId,
                TestConstants.HomeObjectId);
            response.AccessToken = "access-token-2";
            response.ExpiresIn = 3599;
            response.CorrelationId = "correlation-id";
            response.RefreshToken = "refresh-token-2";
            response.Scope = TestConstants.Scope.AsSingleString() + " another-scope";
            response.TokenType = "Bearer";

            requestParams = new AuthenticationRequestParameters()
            {
                Authority = Authority.CreateAuthority(TestConstants.AuthorityGuestTenant, false),
                ClientId = TestConstants.ClientId
            };

            cache.SaveAccessAndRefreshToken(requestParams, response);

            Assert.AreEqual(1, cache.RefreshTokenCount);
            Assert.AreEqual(2, cache.AccessTokenCount);

            Assert.AreEqual("refresh-token-2", cache.GetAllRefreshTokens().First().RefreshToken);
        }


        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void SerializeDeserializeCacheTest()
        {
            TokenCache cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };

            TokenResponse response = new TokenResponse();
            response.IdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId,
                TestConstants.HomeObjectId);
            response.AccessToken = "access-token";
            response.ExpiresIn = 3599;
            response.CorrelationId = "correlation-id";
            response.RefreshToken = "refresh-token";
            response.Scope = TestConstants.Scope.AsSingleString();
            response.TokenType = "Bearer";

            AuthenticationRequestParameters requestParams = new AuthenticationRequestParameters()
            {
                Authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false),
                ClientId = TestConstants.ClientId
            };

            cache.SaveAccessAndRefreshToken(requestParams, response);
            byte[] serializedCache = cache.Serialize();
            
            cache.TokenCacheAccessor.TokenCacheDictionary.Clear();
            Assert.AreEqual(0, cache.AccessTokenCount);
            Assert.AreEqual(0, cache.RefreshTokenCount);

            cache.Deserialize(serializedCache);
            Assert.AreEqual(1, cache.AccessTokenCount);
            Assert.AreEqual(1, cache.RefreshTokenCount);

            serializedCache = cache.Serialize();
            cache.Deserialize(serializedCache);
            //item count should not change because old cache entries should have
            //been overriden

            Assert.AreEqual(1, cache.AccessTokenCount);
            Assert.AreEqual(1, cache.RefreshTokenCount);

            AccessTokenCacheItem atItem = cache.GetAllAccessTokens().First();
            Assert.AreEqual(response.AccessToken, atItem.AccessToken);
            Assert.AreEqual(TestConstants.AuthorityHomeTenant, atItem.Authority);
            Assert.AreEqual(TestConstants.ClientId, atItem.ClientId);
            Assert.AreEqual(response.TokenType, atItem.TokenType);
            Assert.AreEqual(response.Scope, atItem.Scope.AsSingleString());
            Assert.AreEqual(TestConstants.UniqueId, atItem.UniqueId);
            Assert.AreEqual(TestConstants.DisplayableId, atItem.DisplayableId);
            Assert.AreEqual(TestConstants.HomeObjectId, atItem.HomeObjectId);
            Assert.AreEqual(response.IdToken, atItem.RawIdToken);

            RefreshTokenCacheItem rtItem = cache.GetAllRefreshTokens().First();
            Assert.AreEqual(response.RefreshToken, rtItem.RefreshToken);
            Assert.AreEqual(response.IdToken, rtItem.RawIdToken);
            Assert.AreEqual(TestConstants.ClientId, rtItem.ClientId);
            Assert.AreEqual(TestConstants.HomeObjectId, rtItem.HomeObjectId);
            Assert.IsNull(rtItem.Authority);
        }
    }
}
