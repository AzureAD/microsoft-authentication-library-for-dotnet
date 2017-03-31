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

namespace Test.MSAL.NET.Unit.CacheTests
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
            cache.TokenCacheAccessor.AccessTokenCacheDictionary.Clear();
            cache.TokenCacheAccessor.RefreshTokenCacheDictionary.Clear();
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
                RawIdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId),
                RawClientInfo = MockHelpers.DefaultClientInfo,
            };
            atItem.IdToken = IdToken.Parse(atItem.RawIdToken);
            atItem.ClientInfo = ClientInfo.Parse(atItem.RawClientInfo);

            // create key out of access token cache item and then
            // set it as the value of the access token.
            AccessTokenCacheKey atKey = atItem.GetAccessTokenItemKey();
            atItem.AccessToken = atKey.ToString();

            cache.TokenCacheAccessor.AccessTokenCacheDictionary[atKey.ToString()] = JsonHelper.SerializeToJson(atItem);
            AccessTokenCacheItem item = cache.FindAccessToken(new AuthenticationRequestParameters()
            {
                ClientId = TestConstants.ClientId,
                Authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false),
                Scope = TestConstants.Scope,
                User = TestConstants.User
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
                RawIdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId),
                RawClientInfo = MockHelpers.DefaultClientInfo,
            };
            atItem.IdToken = IdToken.Parse(atItem.RawIdToken);
            atItem.ClientInfo = ClientInfo.Parse(atItem.RawClientInfo);

            // create key out of access token cache item and then
            // set it as the value of the access token.
            AccessTokenCacheKey atKey = atItem.GetAccessTokenItemKey();
            atItem.AccessToken = atKey.ToString();

            cache.TokenCacheAccessor.AccessTokenCacheDictionary[atKey.ToString()] = JsonHelper.SerializeToJson(atItem);
            var param = new AuthenticationRequestParameters()
            {
                ClientId = TestConstants.ClientId,
                Authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false),
                Scope = new SortedSet<string>(),
                User = TestConstants.User
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
                RawIdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId)
            };

            // create key out of access token cache item and then
            // set it as the value of the access token.
            AccessTokenCacheKey atKey = atItem.GetAccessTokenItemKey();
            atItem.AccessToken = atKey.ToString();
            cache.TokenCacheAccessor.AccessTokenCacheDictionary[atKey.ToString()] = JsonHelper.SerializeToJson(atItem);

            var param = new AuthenticationRequestParameters()
            {
                ClientId = TestConstants.ClientId,
                Authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false),
                Scope = new SortedSet<string>(),
                User =
                    new User()
                    {
                        DisplayableId = TestConstants.DisplayableId,
                        Identifier = TestConstants.UserIdentifier
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
            cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };

            AccessTokenCacheItem item = new AccessTokenCacheItem()
            {
                Authority = TestConstants.AuthorityHomeTenant,
                ClientId = TestConstants.ClientId,
                TokenType = "Bearer",
                ExpiresOnUnixTimestamp = MsalHelpers.DateTimeToUnixTimestamp(DateTime.UtcNow),
                RawIdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId),
                RawClientInfo = MockHelpers.DefaultClientInfo,
                Scope = TestConstants.Scope
            };
            item.IdToken = IdToken.Parse(item.RawIdToken);
            item.ClientInfo = ClientInfo.Parse(item.RawClientInfo);
            item.AccessToken = item.GetAccessTokenItemKey().ToString();
            cache.TokenCacheAccessor.AccessTokenCacheDictionary[item.GetAccessTokenItemKey().ToString()] = JsonHelper.SerializeToJson(item);

            cache.TokenCacheAccessor.AccessTokenCacheDictionary[item.GetAccessTokenItemKey().ToString()] = JsonHelper.SerializeToJson(item);

            Assert.IsNull(cache.FindAccessToken(new AuthenticationRequestParameters()
            {
                ClientId = TestConstants.ClientId,
                Authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false),
                Scope = TestConstants.Scope,
                User =
                    new User()
                    {
                        DisplayableId = TestConstants.DisplayableId,
                        Identifier = TestConstants.UserIdentifier
                    }
            }));
        }


        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void GetAccessTokenExpiryInRangeTest()
        {
            cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };

            AccessTokenCacheItem atItem = new AccessTokenCacheItem()
            {
                Authority = TestConstants.AuthorityHomeTenant,
                ClientId = TestConstants.ClientId,
                Scope = TestConstants.Scope,
                RawIdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId),
                TokenType = "Bearer",
                ExpiresOnUnixTimestamp = MsalHelpers.DateTimeToUnixTimestamp(DateTime.UtcNow + TimeSpan.FromMinutes(4))
            };

            atItem.AccessToken = atItem.GetAccessTokenItemKey().ToString();
            cache.TokenCacheAccessor.AccessTokenCacheDictionary[atItem.GetAccessTokenItemKey().ToString()] = JsonHelper.SerializeToJson(atItem);

            Assert.IsNull(cache.FindAccessToken(new AuthenticationRequestParameters()
            {
                ClientId = TestConstants.ClientId,
                Authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false),
                Scope = TestConstants.Scope,
                User =
                    new User()
                    {
                        DisplayableId = TestConstants.DisplayableId,
                        Identifier = TestConstants.UserIdentifier
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
                Environment = TestConstants.ProductionEnvironment,
                ClientId = TestConstants.ClientId,
                RefreshToken = "someRT",
                Uid = TestConstants.Uid,
                Utid = TestConstants.Utid,
                DisplayableId = TestConstants.DisplayableId,
                IdentityProvider = TestConstants.IdentityProvider,
                Name = TestConstants.Name
            };

            RefreshTokenCacheKey rtKey = rtItem.GetRefreshTokenItemKey();
            cache.TokenCacheAccessor.RefreshTokenCacheDictionary[rtKey.ToString()] = JsonHelper.SerializeToJson(rtItem);
            var authParams = new AuthenticationRequestParameters()
            {
                ClientId = TestConstants.ClientId,
                Authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false),
                Scope = TestConstants.Scope,
                User = TestConstants.User
            };
            Assert.IsNotNull(cache.FindRefreshToken(authParams));

            // RT is stored by environment, client id and userIdentifier as index.
            // any change to authority (within same environment), uniqueid and displyableid will not 
            // change the outcome of cache look up.
            Assert.IsNotNull(cache.FindRefreshToken(new AuthenticationRequestParameters()
            {
                ClientId = TestConstants.ClientId,
                Authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant + "more", false),
                Scope = TestConstants.Scope,
                User = TestConstants.User
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
                RawClientInfo = null,
                User = null,
                Scope = TestConstants.Scope
            };
            item.AccessToken = item.GetAccessTokenItemKey().ToString();
            cache.TokenCacheAccessor.AccessTokenCacheDictionary[item.GetAccessTokenItemKey().ToString()] = JsonHelper.SerializeToJson(item);

            AccessTokenCacheItem cacheItem = cache.FindAccessToken(new AuthenticationRequestParameters()
            {
                Authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false),
                ClientId = TestConstants.ClientId,
                ClientCredential= TestConstants.CredentialWithSecret,
                Scope = TestConstants.Scope
            });

            Assert.IsNotNull(cacheItem);
            Assert.AreEqual(item.GetAccessTokenItemKey(), cacheItem.GetAccessTokenItemKey());
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
                RawIdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId)
            };

            // create key out of access token cache item and then
            // set it as the value of the access token.
            AccessTokenCacheKey atKey = atItem.GetAccessTokenItemKey();
            atItem.AccessToken = atKey.ToString();

            cache.TokenCacheAccessor.AccessTokenCacheDictionary[atKey.ToString()] = JsonHelper.SerializeToJson(atItem);
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
                RawIdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId)
            };

            // create key out of access token cache item and then
            // set it as the value of the access token.
            AccessTokenCacheKey atKey = atItem.GetAccessTokenItemKey();
            atItem.AccessToken = atKey.ToString();
            atItem.UserAssertionHash = new CryptographyHelper().CreateSha256Hash(atKey.ToString());

            cache.TokenCacheAccessor.AccessTokenCacheDictionary[atKey.ToString()] = JsonHelper.SerializeToJson(atItem);
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
                RawIdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId)
            };

            // create key out of access token cache item and then
            // set it as the value of the access token.
            AccessTokenCacheKey atKey = atItem.GetAccessTokenItemKey();
            atItem.AccessToken = atKey.ToString();
            atItem.UserAssertionHash = PlatformPlugin.CryptographyHelper.CreateSha256Hash(atKey.ToString());

            cache.TokenCacheAccessor.AccessTokenCacheDictionary[atKey.ToString()] = JsonHelper.SerializeToJson(atItem);
            var param = new AuthenticationRequestParameters()
            {
                ClientId = TestConstants.ClientId,
                Authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false),
                Scope = TestConstants.Scope,
                UserAssertion = new UserAssertion(atKey.ToString())
            };

            cache.AfterAccess = AfterAccessNoChangeNotification;
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
            response.IdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId);
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

            Assert.AreEqual(1, cache.TokenCacheAccessor.RefreshTokenCacheDictionary.Count);
            Assert.AreEqual(1, cache.TokenCacheAccessor.AccessTokenCacheDictionary.Count);
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
            response.IdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId);
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

            Assert.AreEqual(1, cache.TokenCacheAccessor.RefreshTokenCacheDictionary.Count);
            Assert.AreEqual(1, cache.TokenCacheAccessor.AccessTokenCacheDictionary.Count);

            response = new TokenResponse();
            response.IdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId);
            response.AccessToken = "access-token-2";
            response.ExpiresIn = 3599;
            response.CorrelationId = "correlation-id";
            response.RefreshToken = "refresh-token-2";
            response.Scope = TestConstants.Scope.AsSingleString() + " another-scope";
            response.TokenType = "Bearer";

            cache.SaveAccessAndRefreshToken(requestParams, response);

            Assert.AreEqual(1, cache.TokenCacheAccessor.RefreshTokenCacheDictionary.Count);
            Assert.AreEqual(1, cache.TokenCacheAccessor.AccessTokenCacheDictionary.Count);

            Assert.AreEqual("refresh-token-2", cache.GetAllRefreshTokensForClient().First().RefreshToken);
            Assert.AreEqual("access-token-2", cache.GetAllAccessTokensForClient().First().AccessToken);
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
            response.IdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId);
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
            response.IdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId);
            response.AccessToken = "access-token-2";
            response.ExpiresIn = 3599;
            response.CorrelationId = "correlation-id";
            response.RefreshToken = "refresh-token-2";
            response.Scope = TestConstants.Scope.First();
            response.TokenType = "Bearer";
            
            cache.SaveAccessAndRefreshToken(requestParams, response);

            Assert.AreEqual(1, cache.TokenCacheAccessor.RefreshTokenCacheDictionary.Count);
            Assert.AreEqual(1, cache.TokenCacheAccessor.AccessTokenCacheDictionary.Count);
            Assert.AreEqual("refresh-token-2", cache.GetAllRefreshTokensForClient().First().RefreshToken);
            Assert.AreEqual("access-token-2", cache.GetAllAccessTokensForClient().First().AccessToken);
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void SaveAccessAndRefreshTokenWithIntersectingScopesTest()
        {
            TokenCache cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };

            TokenResponse response = new TokenResponse
            {
                IdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId),
                AccessToken = "access-token",
                ClientInfo = MockHelpers.DefaultClientInfo,
                ExpiresIn = 3599,
                CorrelationId = "correlation-id",
                RefreshToken = "refresh-token",
                Scope = TestConstants.Scope.AsSingleString(),
                TokenType = "Bearer"
            };

            AuthenticationRequestParameters requestParams = new AuthenticationRequestParameters()
            {
                Authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false),
                ClientId = TestConstants.ClientId
            };

            cache.SaveAccessAndRefreshToken(requestParams, response);

            response = new TokenResponse
            {
                IdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId),
                ClientInfo = MockHelpers.DefaultClientInfo,
                AccessToken = "access-token-2",
                ExpiresIn = 3599,
                CorrelationId = "correlation-id",
                RefreshToken = "refresh-token-2",
                Scope = TestConstants.Scope.First() + " random-scope",
                TokenType = "Bearer"
            };

            cache.SaveAccessAndRefreshToken(requestParams, response);

            Assert.AreEqual(1, cache.TokenCacheAccessor.RefreshTokenCacheDictionary.Count);
            Assert.AreEqual(1, cache.TokenCacheAccessor.AccessTokenCacheDictionary.Count);

            Assert.AreEqual("refresh-token-2", cache.GetAllRefreshTokensForClient().First().RefreshToken);
            Assert.AreEqual("access-token-2", cache.GetAllAccessTokensForClient().First().AccessToken);
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
            response.IdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId);
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
            response.IdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId);
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

            cache.SetAfterAccess(AfterAccessChangedNotification);
            cache.SaveAccessAndRefreshToken(requestParams, response);
            Assert.IsFalse(cache.HasStateChanged);

            Assert.AreEqual(1, cache.TokenCacheAccessor.RefreshTokenCacheDictionary.Count);
            Assert.AreEqual(2, cache.TokenCacheAccessor.AccessTokenCacheDictionary.Count);

            Assert.AreEqual("refresh-token-2", cache.GetAllRefreshTokensForClient().First().RefreshToken);
        }
        
        private void AfterAccessChangedNotification(TokenCacheNotificationArgs args)
        {
            Assert.IsTrue(args.TokenCache.HasStateChanged);
        }
        private void AfterAccessNoChangeNotification(TokenCacheNotificationArgs args)
        {
            Assert.IsFalse(args.TokenCache.HasStateChanged);
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
            response.IdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId);
            response.ClientInfo = MockHelpers.DefaultClientInfo;
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
            
            cache.TokenCacheAccessor.AccessTokenCacheDictionary.Clear();
            cache.TokenCacheAccessor.RefreshTokenCacheDictionary.Clear();

            Assert.AreEqual(0, cache.TokenCacheAccessor.RefreshTokenCacheDictionary.Count);
            Assert.AreEqual(0, cache.TokenCacheAccessor.AccessTokenCacheDictionary.Count);

            cache.Deserialize(serializedCache);

            Assert.AreEqual(1, cache.TokenCacheAccessor.RefreshTokenCacheDictionary.Count);
            Assert.AreEqual(1, cache.TokenCacheAccessor.AccessTokenCacheDictionary.Count);

            serializedCache = cache.Serialize();
            cache.Deserialize(serializedCache);
            //item count should not change because old cache entries should have
            //been overriden


            Assert.AreEqual(1, cache.TokenCacheAccessor.RefreshTokenCacheDictionary.Count);
            Assert.AreEqual(1, cache.TokenCacheAccessor.AccessTokenCacheDictionary.Count);

            AccessTokenCacheItem atItem = cache.GetAllAccessTokensForClient().First();
            Assert.AreEqual(response.AccessToken, atItem.AccessToken);
            Assert.AreEqual(TestConstants.AuthorityHomeTenant, atItem.Authority);
            Assert.AreEqual(TestConstants.ClientId, atItem.ClientId);
            Assert.AreEqual(response.TokenType, atItem.TokenType);
            Assert.AreEqual(response.Scope, atItem.Scope.AsSingleString());
            Assert.AreEqual(response.IdToken, atItem.RawIdToken);

            RefreshTokenCacheItem rtItem = cache.GetAllRefreshTokensForClient().First();
            Assert.AreEqual(response.RefreshToken, rtItem.RefreshToken);
            Assert.AreEqual(TestConstants.ClientId, rtItem.ClientId);
            Assert.AreEqual(TestConstants.UserIdentifier, rtItem.GetUserIdentifier());
            Assert.AreEqual(TestConstants.ProductionEnvironment, rtItem.Environment);
        }
    }
}
