/*
//----------------------------------------------------------------------
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
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal.Cache;
using Microsoft.Identity.Client.Internal.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.MSAL.NET.Unit.Mocks;

namespace Test.MSAL.NET.Unit.RequestsTests
{
    class OboFlowTests
    {    /// <summary>
         /// This test class executes and validates OBO scenarios where token cache may or may not 
         /// contain entries with user assertion hash. It accounts for cases where there is
         /// a single user and when there are multiple users in the cache.
         /// user assertion hash exists so that the API can deterministically identify the user
         /// in the cache when a usernae is not passed in. It also allows the API to acquire
         /// new token when a different assertion is passed for the user. this is needed because
         /// the user may have authenticated with updated claims like MFA/device auth on the client.
         /// </summary>
        [TestClass]
        public class OboFlowTests
        {
            private readonly CryptographyHelper _cryptoHelper = new CryptographyHelper();
            private readonly DateTimeOffset _expirationTime = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(30);
            private static readonly string[] _cacheNoise = { "", "different" };

            [TestInitialize]
            public void TestInitialize()
            {
                HttpMessageHandlerFactory.ClearMockHandlers();
            }

            [TestMethod]
            [TestCategory("OboFlowTests")]
            public async Task MultiUserNoHashInCacheNoUsernamePassedInAssertionTest()
            {
                var context = new ConfidentialClientApplication(TestConstants.AuthorityHomeTenant, new TokenCache(TestConstants.ClientId));
                string accessToken = "access-token";

                foreach (var cachenoise in _cacheNoise)
                {
                    TokenCacheKey key = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                        TestConstants.DefaultResource, TestConstants.ClientId, TokenSubjectType.User,
                        cachenoise + TestConstants.UniqueId, cachenoise + TestConstants.DisplayableId);
                    //cache entry has no user assertion hash
                    context.TokenCache.tokenCacheDictionary[key] = new AuthenticationResultEx
                    {
                        RefreshToken = cachenoise + "some-rt",
                        ResourceInResponse = TestConstants.DefaultResource,
                        Result = new AuthenticationResult("Bearer", cachenoise + "some-token-in-cache", _expirationTime)
                        {
                            UserInfo =
                                new UserInfo()
                                {
                                    DisplayableId = cachenoise + TestConstants.DisplayableId,
                                    UniqueId = cachenoise + TestConstants.UniqueId
                                }
                        },
                    };
                }

                ClientCredential clientCredential = new ClientCredential(TestConstants.ClientId,
                    TestConstants.ClientSecret);
                HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
                {
                    Method = HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                    PostData = new Dictionary<string, string>()
                {
                    {"client_id", TestConstants.ClientId},
                    {"grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"}
                }
                });

                // call acquire token with no username. this will result in a network call because cache entry with no assertion hash is
                // treated as a cache miss.
                var result =
                    await
                        context.AcquireTokenAsync(TestConstants.DefaultResource, clientCredential,
                            new UserAssertion(accessToken));
                Assert.AreEqual(0, HttpMessageHandlerFactory.MockHandlersCount(), "all mocks should have been consumed");
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual("some-access-token", result.AccessToken);
                Assert.AreEqual(TestConstants.DisplayableId, result.UserInfo.DisplayableId);

                //there should be only one cache entry.
                Assert.AreEqual(2, context.TokenCache.Count);

                //assertion hash should be stored in the cache entry.
                Assert.AreEqual(_cryptoHelper.CreateSha256Hash(accessToken),
                    context.TokenCache.tokenCacheDictionary.Values.First(x => x.UserAssertionHash != null).UserAssertionHash);
            }

            [TestMethod]
            [TestCategory("OboFlowTests")]
            public async Task MultiUserNoHashInCacheMatchingUsernamePassedInAssertionTest()
            {
                var context = new AuthenticationContext(TestConstants.AuthorityHomeTenant, new TokenCache());
                string accessToken = "access-token";

                foreach (var cachenoise in _cacheNoise)
                {
                    TokenCacheKey key = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                        TestConstants.DefaultResource, TestConstants.ClientId, TokenSubjectType.User,
                        cachenoise + TestConstants.UniqueId, cachenoise + TestConstants.DisplayableId);
                    //cache entry has no user assertion hash
                    context.TokenCache.tokenCacheDictionary[key] = new AuthenticationResultEx
                    {
                        RefreshToken = cachenoise + "some-rt",
                        ResourceInResponse = TestConstants.DefaultResource,
                        Result = new AuthenticationResult("Bearer", cachenoise + "some-token-in-cache", _expirationTime)
                        {
                            UserInfo =
                                new UserInfo()
                                {
                                    DisplayableId = cachenoise + TestConstants.DisplayableId,
                                    UniqueId = cachenoise + TestConstants.UniqueId
                                }
                        },
                    };
                }

                ClientCredential clientCredential = new ClientCredential(TestConstants.ClientId,
                    TestConstants.ClientSecret);
                HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
                {
                    Method = HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                    PostData = new Dictionary<string, string>()
                {
                    {"client_id", TestConstants.ClientId},
                    {"grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"}
                }
                });

                // call acquire token with matching username from cache entry. this will result in a network call 
                // because cache entry with no assertion hash is treated as a cache miss.

                var result =
                    await
                        context.AcquireTokenAsync(TestConstants.DefaultResource, clientCredential,
                            new UserAssertion(accessToken, OAuthGrantType.JwtBearer, TestConstants.DisplayableId));
                Assert.AreEqual(0, HttpMessageHandlerFactory.MockHandlersCount(), "all mocks should have been consumed");
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual("some-access-token", result.AccessToken);
                Assert.AreEqual(TestConstants.DisplayableId, result.UserInfo.DisplayableId);

                //there should be only one cache entry.
                Assert.AreEqual(2, context.TokenCache.Count);

                //assertion hash should be stored in the cache entry.
                Assert.AreEqual(_cryptoHelper.CreateSha256Hash(accessToken),
                    context.TokenCache.tokenCacheDictionary.Values.First(x => x.UserAssertionHash != null).UserAssertionHash);
            }

            [TestMethod]
            [TestCategory("OboFlowTests")]
            public async Task MultiUserNoHashInCacheDifferentUsernamePassedInAssertionTest()
            {
                var context = new AuthenticationContext(TestConstants.AuthorityHomeTenant, new TokenCache());
                string accessToken = "access-token";

                foreach (var cachenoise in _cacheNoise)
                {
                    TokenCacheKey key = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                        TestConstants.DefaultResource, TestConstants.ClientId, TokenSubjectType.User,
                        cachenoise + TestConstants.UniqueId, cachenoise + TestConstants.DisplayableId);
                    //cache entry has no user assertion hash
                    context.TokenCache.tokenCacheDictionary[key] = new AuthenticationResultEx
                    {
                        RefreshToken = cachenoise + "some-rt",
                        ResourceInResponse = TestConstants.DefaultResource,
                        Result = new AuthenticationResult("Bearer", cachenoise + "some-token-in-cache", _expirationTime)
                        {
                            UserInfo =
                                new UserInfo()
                                {
                                    DisplayableId = cachenoise + TestConstants.DisplayableId,
                                    UniqueId = cachenoise + TestConstants.UniqueId
                                }
                        },
                    };
                }

                ClientCredential clientCredential = new ClientCredential(TestConstants.ClientId,
                    TestConstants.ClientSecret);

                string displayableId2 = "extra" + TestConstants.DisplayableId;
                string uniqueId2 = "extra" + TestConstants.UniqueId;

                HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
                {
                    Method = HttpMethod.Post,
                    ResponseMessage =
                        MockHelpers.CreateSuccessTokenResponseMessage(uniqueId2, displayableId2,
                            TestConstants.DefaultResource),
                    PostData = new Dictionary<string, string>()
                {
                    {"client_id", TestConstants.ClientId},
                    {"grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"}
                }
                });

                // call acquire token with diferent username from cache entry. this will result in a network call
                // because cache lookup failed for non-existant user
                var result =
                    await
                        context.AcquireTokenAsync(TestConstants.DefaultResource, clientCredential,
                            new UserAssertion(accessToken, OAuthGrantType.JwtBearer,
                                "non-existant" + TestConstants.DisplayableId));
                Assert.AreEqual(0, HttpMessageHandlerFactory.MockHandlersCount(), "all mocks should have been consumed");
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual("some-access-token", result.AccessToken);
                Assert.AreEqual(displayableId2, result.UserInfo.DisplayableId);

                //there should be only one cache entry.
                Assert.AreEqual(3, context.TokenCache.Count);

                //assertion hash should be stored in the cache entry.
                Assert.AreEqual(_cryptoHelper.CreateSha256Hash(accessToken),
                    context.TokenCache.tokenCacheDictionary.Values.First(x => x.UserAssertionHash != null)
                        .UserAssertionHash);
            }

            [TestMethod]
            [TestCategory("OboFlowTests")]
            public async Task MultiUserWithHashInCacheNoUsernameAndMatchingAssertionTest()
            {
                var context = new AuthenticationContext(TestConstants.AuthorityHomeTenant, new TokenCache());
                string accessToken = "access-token";
                //cache entry has user assertion hash
                string tokenInCache = "obo-access-token";

                foreach (var cachenoise in _cacheNoise)
                {
                    TokenCacheKey key = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                        TestConstants.DefaultResource, TestConstants.ClientId, TokenSubjectType.User,
                        cachenoise + TestConstants.UniqueId, cachenoise + TestConstants.DisplayableId);

                    context.TokenCache.tokenCacheDictionary[key] = new AuthenticationResultEx
                    {
                        RefreshToken = cachenoise + "some-rt",
                        ResourceInResponse = TestConstants.DefaultResource,
                        Result =
                            new AuthenticationResult("Bearer", cachenoise + tokenInCache, _expirationTime)
                            {
                                UserInfo =
                                    new UserInfo()
                                    {
                                        DisplayableId = cachenoise + TestConstants.DisplayableId,
                                        UniqueId = cachenoise + TestConstants.UniqueId
                                    }
                            },
                        UserAssertionHash = _cryptoHelper.CreateSha256Hash(cachenoise + accessToken)
                    };
                }

                ClientCredential clientCredential = new ClientCredential(TestConstants.ClientId,
                    TestConstants.ClientSecret);

                // call acquire token with no username and matching assertion hash. this will result in a cache
                // hit.
                var result =
                    await
                        context.AcquireTokenAsync(TestConstants.DefaultResource, clientCredential,
                            new UserAssertion(accessToken));
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(tokenInCache, result.AccessToken);
                Assert.AreEqual(TestConstants.DisplayableId, result.UserInfo.DisplayableId);

                //there should be only one cache entry.
                Assert.AreEqual(2, context.TokenCache.Count);
            }

            [TestMethod]
            [TestCategory("OboFlowTests")]
            public async Task MultiUserWithHashInCacheNoUsernameAndDifferentAssertionTest()
            {
                var context = new AuthenticationContext(TestConstants.AuthorityHomeTenant, new TokenCache());
                string accessToken = "access-token";
                //cache entry has user assertion hash
                string tokenInCache = "obo-access-token";

                foreach (var cachenoise in _cacheNoise)
                {
                    TokenCacheKey key = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                        TestConstants.DefaultResource, TestConstants.ClientId, TokenSubjectType.User,
                        cachenoise + TestConstants.UniqueId, cachenoise + TestConstants.DisplayableId);
                    context.TokenCache.tokenCacheDictionary[key] = new AuthenticationResultEx
                    {
                        RefreshToken = cachenoise + "some-rt",
                        ResourceInResponse = TestConstants.DefaultResource,
                        Result =
                            new AuthenticationResult("Bearer", cachenoise + tokenInCache, _expirationTime)
                            {
                                UserInfo =
                                    new UserInfo()
                                    {
                                        DisplayableId = cachenoise + TestConstants.DisplayableId,
                                        UniqueId = cachenoise + TestConstants.UniqueId
                                    }
                            },
                        UserAssertionHash = _cryptoHelper.CreateSha256Hash(cachenoise + accessToken)
                    };
                }

                HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
                {
                    Method = HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                    PostData = new Dictionary<string, string>()
                {
                    {"client_id", TestConstants.ClientId},
                    {"grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"}
                }
                });

                ClientCredential clientCredential = new ClientCredential(TestConstants.ClientId,
                    TestConstants.ClientSecret);

                // call acquire token with no username and different assertion hash. this will result in a cache
                // hit.
                var result =
                    await
                        context.AcquireTokenAsync(TestConstants.DefaultResource, clientCredential,
                            new UserAssertion("non-existant" + accessToken));
                Assert.AreEqual(0, HttpMessageHandlerFactory.MockHandlersCount(), "all mocks should have been consumed");
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual("some-access-token", result.AccessToken);
                Assert.AreEqual(TestConstants.DisplayableId, result.UserInfo.DisplayableId);

                //there should be only one cache entry.
                Assert.AreEqual(2, context.TokenCache.Count);
            }


            [TestMethod]
            [TestCategory("OboFlowTests")]
            public async Task MultiUserWithHashInCacheMatchingUsernameAndMatchingAssertionTest()
            {
                var context = new AuthenticationContext(TestConstants.AuthorityHomeTenant, new TokenCache());
                string accessToken = "access-token";
                //cache entry has user assertion hash
                string tokenInCache = "obo-access-token";
                foreach (var cachenoise in _cacheNoise)
                {
                    TokenCacheKey key = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                        TestConstants.DefaultResource, TestConstants.ClientId, TokenSubjectType.User,
                        cachenoise + TestConstants.UniqueId, cachenoise + TestConstants.DisplayableId);

                    context.TokenCache.tokenCacheDictionary[key] = new AuthenticationResultEx
                    {
                        RefreshToken = cachenoise + "some-rt",
                        ResourceInResponse = TestConstants.DefaultResource,
                        Result =
                            new AuthenticationResult("Bearer", cachenoise + tokenInCache, _expirationTime)
                            {
                                UserInfo =
                                    new UserInfo()
                                    {
                                        DisplayableId = cachenoise + TestConstants.DisplayableId,
                                        UniqueId = cachenoise + TestConstants.UniqueId
                                    }
                            },
                        UserAssertionHash = _cryptoHelper.CreateSha256Hash(cachenoise + accessToken)
                    };
                }

                ClientCredential clientCredential = new ClientCredential(TestConstants.ClientId,
                    TestConstants.ClientSecret);

                // call acquire token with matching username and matching assertion hash. this will result in a cache
                // hit.
                var result =
                    await
                        context.AcquireTokenAsync(TestConstants.DefaultResource, clientCredential,
                            new UserAssertion(accessToken, OAuthGrantType.JwtBearer, TestConstants.DisplayableId));
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(tokenInCache, result.AccessToken);
                Assert.AreEqual(TestConstants.DisplayableId, result.UserInfo.DisplayableId);

                //there should be only one cache entry.
                Assert.AreEqual(2, context.TokenCache.Count);

                //assertion hash should be stored in the cache entry.
                Assert.AreEqual(_cryptoHelper.CreateSha256Hash(accessToken),
                    context.TokenCache.tokenCacheDictionary.Values.First().UserAssertionHash);
            }

            [TestMethod]
            [TestCategory("OboFlowTests")]
            public async Task MultiUserWithHashInCacheMatchingUsernameAndDifferentAssertionTest()
            {
                var context = new AuthenticationContext(TestConstants.AuthorityHomeTenant, new TokenCache());
                string accessToken = "access-token";
                //cache entry has user assertion hash
                string tokenInCache = "obo-access-token";
                foreach (var cachenoise in _cacheNoise)
                {
                    TokenCacheKey key = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                        TestConstants.DefaultResource, TestConstants.ClientId, TokenSubjectType.User,
                        cachenoise + TestConstants.UniqueId, cachenoise + TestConstants.DisplayableId);

                    context.TokenCache.tokenCacheDictionary[key] = new AuthenticationResultEx
                    {
                        RefreshToken = cachenoise + "some-rt",
                        ResourceInResponse = TestConstants.DefaultResource,
                        Result =
                            new AuthenticationResult("Bearer", cachenoise + tokenInCache, _expirationTime)
                            {
                                UserInfo =
                                    new UserInfo()
                                    {
                                        DisplayableId = cachenoise + TestConstants.DisplayableId,
                                        UniqueId = cachenoise + TestConstants.UniqueId
                                    }
                            },
                        UserAssertionHash = _cryptoHelper.CreateSha256Hash(cachenoise + accessToken)
                    };
                }

                HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
                {
                    Method = HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                    PostData = new Dictionary<string, string>()
                {
                    {"client_id", TestConstants.ClientId},
                    {"grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"}
                }
                });

                ClientCredential clientCredential = new ClientCredential(TestConstants.ClientId,
                    TestConstants.ClientSecret);

                // call acquire token with matching username and different assertion hash. this will result in a cache
                // hit.
                var result =
                    await
                        context.AcquireTokenAsync(TestConstants.DefaultResource, clientCredential,
                            new UserAssertion("non-existant" + accessToken, OAuthGrantType.JwtBearer,
                                TestConstants.DisplayableId));
                Assert.AreEqual(0, HttpMessageHandlerFactory.MockHandlersCount(), "all mocks should have been consumed");
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual("some-access-token", result.AccessToken);
                Assert.AreEqual(TestConstants.DisplayableId, result.UserInfo.DisplayableId);

                //there should be only one cache entry.
                Assert.AreEqual(2, context.TokenCache.Count);
            }

            [TestMethod]
            [TestCategory("OboFlowTests")]
            public async Task SingleUserNoHashInCacheNoUsernamePassedInAssertionTest()
            {
                var context = new AuthenticationContext(TestConstants.AuthorityHomeTenant, new TokenCache());
                string accessToken = "access-token";
                TokenCacheKey key = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                    TestConstants.DefaultResource, TestConstants.ClientId, TokenSubjectType.User,
                    TestConstants.UniqueId, TestConstants.DisplayableId);
                //cache entry has no user assertion hash
                context.TokenCache.tokenCacheDictionary[key] = new AuthenticationResultEx
                {
                    RefreshToken = "some-rt",
                    ResourceInResponse = TestConstants.DefaultResource,
                    Result = new AuthenticationResult("Bearer", "some-token-in-cache", _expirationTime)
                    {
                        UserInfo =
                            new UserInfo()
                            {
                                DisplayableId = TestConstants.DisplayableId,
                                UniqueId = TestConstants.UniqueId
                            }
                    },
                };

                ClientCredential clientCredential = new ClientCredential(TestConstants.ClientId,
                    TestConstants.ClientSecret);
                HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
                {
                    Method = HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                    PostData = new Dictionary<string, string>()
                {
                    {"client_id", TestConstants.ClientId},
                    {"grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"}
                }
                });

                // call acquire token with no username. this will result in a network call because cache entry with no assertion hash is
                // treated as a cache miss.
                var result =
                    await
                        context.AcquireTokenAsync(TestConstants.DefaultResource, clientCredential,
                            new UserAssertion(accessToken));
                Assert.AreEqual(0, HttpMessageHandlerFactory.MockHandlersCount(), "all mocks should have been consumed");
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual("some-access-token", result.AccessToken);
                Assert.AreEqual(TestConstants.DisplayableId, result.UserInfo.DisplayableId);

                //there should be only one cache entry.
                Assert.AreEqual(1, context.TokenCache.Count);

                //assertion hash should be stored in the cache entry.
                Assert.AreEqual(_cryptoHelper.CreateSha256Hash(accessToken),
                    context.TokenCache.tokenCacheDictionary.Values.First().UserAssertionHash);
            }

            [TestMethod]
            [TestCategory("OboFlowTests")]
            public async Task SingleUserNoHashInCacheMatchingUsernamePassedInAssertionTest()
            {
                var context = new AuthenticationContext(TestConstants.AuthorityHomeTenant, new TokenCache());
                string accessToken = "access-token";
                TokenCacheKey key = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                    TestConstants.DefaultResource, TestConstants.ClientId, TokenSubjectType.User,
                    TestConstants.UniqueId, TestConstants.DisplayableId);
                //cache entry has no user assertion hash
                context.TokenCache.tokenCacheDictionary[key] = new AuthenticationResultEx
                {
                    RefreshToken = "some-rt",
                    ResourceInResponse = TestConstants.DefaultResource,
                    Result = new AuthenticationResult("Bearer", "some-token-in-cache", _expirationTime)
                    {
                        UserInfo =
                            new UserInfo()
                            {
                                DisplayableId = TestConstants.DisplayableId,
                                UniqueId = TestConstants.UniqueId
                            }
                    },
                };

                ClientCredential clientCredential = new ClientCredential(TestConstants.ClientId,
                    TestConstants.ClientSecret);
                HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
                {
                    Method = HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                    PostData = new Dictionary<string, string>()
                {
                    {"client_id", TestConstants.ClientId},
                    {"grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"}
                }
                });

                // call acquire token with matching username from cache entry. this will result in a network call 
                // because cache entry with no assertion hash is treated as a cache miss.

                var result =
                    await
                        context.AcquireTokenAsync(TestConstants.DefaultResource, clientCredential,
                            new UserAssertion(accessToken, OAuthGrantType.JwtBearer, TestConstants.DisplayableId));
                Assert.AreEqual(0, HttpMessageHandlerFactory.MockHandlersCount(), "all mocks should have been consumed");
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual("some-access-token", result.AccessToken);
                Assert.AreEqual(TestConstants.DisplayableId, result.UserInfo.DisplayableId);

                //there should be only one cache entry.
                Assert.AreEqual(1, context.TokenCache.Count);

                //assertion hash should be stored in the cache entry.
                Assert.AreEqual(_cryptoHelper.CreateSha256Hash(accessToken),
                    context.TokenCache.tokenCacheDictionary.Values.First().UserAssertionHash);
            }

            [TestMethod]
            [TestCategory("OboFlowTests")]
            public async Task SingleUserNoHashInCacheDifferentUsernamePassedInAssertionTest()
            {
                var context = new AuthenticationContext(TestConstants.AuthorityHomeTenant, new TokenCache());
                string accessToken = "access-token";
                TokenCacheKey key = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                    TestConstants.DefaultResource, TestConstants.ClientId, TokenSubjectType.User,
                    TestConstants.UniqueId, TestConstants.DisplayableId);
                //cache entry has no user assertion hash
                context.TokenCache.tokenCacheDictionary[key] = new AuthenticationResultEx
                {
                    RefreshToken = "some-rt",
                    ResourceInResponse = TestConstants.DefaultResource,
                    Result = new AuthenticationResult("Bearer", "some-token-in-cache", _expirationTime)
                    {
                        UserInfo =
                            new UserInfo()
                            {
                                DisplayableId = TestConstants.DisplayableId,
                                UniqueId = TestConstants.UniqueId
                            }
                    },
                };

                ClientCredential clientCredential = new ClientCredential(TestConstants.ClientId,
                    TestConstants.ClientSecret);

                string displayableId2 = "extra" + TestConstants.DisplayableId;
                string uniqueId2 = "extra" + TestConstants.UniqueId;

                HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
                {
                    Method = HttpMethod.Post,
                    ResponseMessage =
                        MockHelpers.CreateSuccessTokenResponseMessage(uniqueId2, displayableId2,
                            TestConstants.DefaultResource),
                    PostData = new Dictionary<string, string>()
                {
                    {"client_id", TestConstants.ClientId},
                    {"grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"}
                }
                });

                // call acquire token with diferent username from cache entry. this will result in a network call
                // because cache lookup failed for non-existant user
                var result =
                    await
                        context.AcquireTokenAsync(TestConstants.DefaultResource, clientCredential,
                            new UserAssertion(accessToken, OAuthGrantType.JwtBearer, displayableId2
                                ));
                Assert.AreEqual(0, HttpMessageHandlerFactory.MockHandlersCount(), "all mocks should have been consumed");
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual("some-access-token", result.AccessToken);
                Assert.AreEqual(displayableId2, result.UserInfo.DisplayableId);

                //there should be only one cache entry.
                Assert.AreEqual(2, context.TokenCache.Count);

                //assertion hash should be stored in the cache entry.
                Assert.AreEqual(_cryptoHelper.CreateSha256Hash(accessToken),
                    context.TokenCache.tokenCacheDictionary.Values.First(
                        s => s.Result.UserInfo != null && s.Result.UserInfo.DisplayableId.Equals(displayableId2))
                        .UserAssertionHash);
            }

            [TestMethod]
            [TestCategory("OboFlowTests")]
            public async Task SingleUserWithHashInCacheNoUsernameAndMatchingAssertionTest()
            {
                var context = new AuthenticationContext(TestConstants.AuthorityHomeTenant, new TokenCache());
                string accessToken = "access-token";
                TokenCacheKey key = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                    TestConstants.DefaultResource, TestConstants.ClientId, TokenSubjectType.User,
                    TestConstants.UniqueId, TestConstants.DisplayableId);

                //cache entry has user assertion hash
                string tokenInCache = "obo-access-token";
                context.TokenCache.tokenCacheDictionary[key] = new AuthenticationResultEx
                {
                    RefreshToken = "some-rt",
                    ResourceInResponse = TestConstants.DefaultResource,
                    Result =
                        new AuthenticationResult("Bearer", tokenInCache, _expirationTime)
                        {
                            UserInfo =
                                new UserInfo()
                                {
                                    DisplayableId = TestConstants.DisplayableId,
                                    UniqueId = TestConstants.UniqueId
                                }
                        },
                    UserAssertionHash = _cryptoHelper.CreateSha256Hash(accessToken)
                };

                ClientCredential clientCredential = new ClientCredential(TestConstants.ClientId,
                    TestConstants.ClientSecret);

                // call acquire token with no username and matching assertion hash. this will result in a cache
                // hit.
                var result =
                    await
                        context.AcquireTokenAsync(TestConstants.DefaultResource, clientCredential,
                            new UserAssertion(accessToken));
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(tokenInCache, result.AccessToken);
                Assert.AreEqual(TestConstants.DisplayableId, result.UserInfo.DisplayableId);

                //there should be only one cache entry.
                Assert.AreEqual(1, context.TokenCache.Count);

                //assertion hash should be stored in the cache entry.
                Assert.AreEqual(_cryptoHelper.CreateSha256Hash(accessToken),
                    context.TokenCache.tokenCacheDictionary.Values.First().UserAssertionHash);
            }

            [TestMethod]
            [TestCategory("OboFlowTests")]
            public async Task SingleUserWithHashInCacheNoUsernameAndDifferentAssertionTest()
            {
                var context = new AuthenticationContext(TestConstants.AuthorityHomeTenant, new TokenCache());
                string accessToken = "access-token";
                TokenCacheKey key = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                    TestConstants.DefaultResource, TestConstants.ClientId, TokenSubjectType.User,
                    TestConstants.UniqueId, TestConstants.DisplayableId);

                //cache entry has user assertion hash
                string tokenInCache = "obo-access-token";
                context.TokenCache.tokenCacheDictionary[key] = new AuthenticationResultEx
                {
                    RefreshToken = "some-rt",
                    ResourceInResponse = TestConstants.DefaultResource,
                    Result =
                        new AuthenticationResult("Bearer", tokenInCache, _expirationTime)
                        {
                            UserInfo =
                                new UserInfo()
                                {
                                    DisplayableId = TestConstants.DisplayableId,
                                    UniqueId = TestConstants.UniqueId
                                }
                        },
                    UserAssertionHash = _cryptoHelper.CreateSha256Hash(accessToken + "different")
                };

                HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
                {
                    Method = HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                    PostData = new Dictionary<string, string>()
                {
                    {"client_id", TestConstants.ClientId},
                    {"grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"}
                }
                });

                ClientCredential clientCredential = new ClientCredential(TestConstants.ClientId,
                    TestConstants.ClientSecret);

                // call acquire token with no username and different assertion hash. this will result in a cache
                // hit.
                var result =
                    await
                        context.AcquireTokenAsync(TestConstants.DefaultResource, clientCredential,
                            new UserAssertion(accessToken));
                Assert.AreEqual(0, HttpMessageHandlerFactory.MockHandlersCount(), "all mocks should have been consumed");
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual("some-access-token", result.AccessToken);
                Assert.AreEqual(TestConstants.DisplayableId, result.UserInfo.DisplayableId);

                //there should be only one cache entry.
                Assert.AreEqual(1, context.TokenCache.Count);

                //assertion hash should be stored in the cache entry.
                Assert.AreEqual(_cryptoHelper.CreateSha256Hash(accessToken),
                    context.TokenCache.tokenCacheDictionary.Values.First().UserAssertionHash);
            }


            [TestMethod]
            [TestCategory("OboFlowTests")]
            public async Task SingleUserWithHashInCacheMatchingUsernameAndMatchingAssertionTest()
            {
                var context = new AuthenticationContext(TestConstants.AuthorityHomeTenant, new TokenCache());
                string accessToken = "access-token";
                TokenCacheKey key = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                    TestConstants.DefaultResource, TestConstants.ClientId, TokenSubjectType.User,
                    TestConstants.UniqueId, TestConstants.DisplayableId);

                //cache entry has user assertion hash
                string tokenInCache = "obo-access-token";
                context.TokenCache.tokenCacheDictionary[key] = new AuthenticationResultEx
                {
                    RefreshToken = "some-rt",
                    ResourceInResponse = TestConstants.DefaultResource,
                    Result =
                        new AuthenticationResult("Bearer", tokenInCache, _expirationTime)
                        {
                            UserInfo =
                                new UserInfo()
                                {
                                    DisplayableId = TestConstants.DisplayableId,
                                    UniqueId = TestConstants.UniqueId
                                }
                        },
                    UserAssertionHash = _cryptoHelper.CreateSha256Hash(accessToken)
                };

                ClientCredential clientCredential = new ClientCredential(TestConstants.ClientId,
                    TestConstants.ClientSecret);

                // call acquire token with matching username and matching assertion hash. this will result in a cache
                // hit.
                var result =
                    await
                        context.AcquireTokenAsync(TestConstants.DefaultResource, clientCredential,
                            new UserAssertion(accessToken));
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(tokenInCache, result.AccessToken);
                Assert.AreEqual(TestConstants.DisplayableId, result.UserInfo.DisplayableId);

                //there should be only one cache entry.
                Assert.AreEqual(1, context.TokenCache.Count);

                //assertion hash should be stored in the cache entry.
                Assert.AreEqual(_cryptoHelper.CreateSha256Hash(accessToken),
                    context.TokenCache.tokenCacheDictionary.Values.First().UserAssertionHash);
            }

            [TestMethod]
            [TestCategory("OboFlowTests")]
            public async Task SingleUserWithHashInCacheMatchingUsernameAndDifferentAssertionTest()
            {
                var context = new AuthenticationContext(TestConstants.AuthorityHomeTenant, new TokenCache());
                string accessToken = "access-token";
                TokenCacheKey key = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                    TestConstants.DefaultResource, TestConstants.ClientId, TokenSubjectType.User,
                    TestConstants.UniqueId, TestConstants.DisplayableId);

                //cache entry has user assertion hash
                string tokenInCache = "obo-access-token";
                context.TokenCache.tokenCacheDictionary[key] = new AuthenticationResultEx
                {
                    RefreshToken = "some-rt",
                    ResourceInResponse = TestConstants.DefaultResource,
                    Result =
                        new AuthenticationResult("Bearer", tokenInCache, _expirationTime)
                        {
                            UserInfo =
                                new UserInfo()
                                {
                                    DisplayableId = TestConstants.DisplayableId,
                                    UniqueId = TestConstants.UniqueId
                                }
                        },
                    UserAssertionHash = _cryptoHelper.CreateSha256Hash(accessToken + "different")
                };

                HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
                {
                    Method = HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                    PostData = new Dictionary<string, string>()
                {
                    {"client_id", TestConstants.ClientId},
                    {"grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"}
                }
                });

                ClientCredential clientCredential = new ClientCredential(TestConstants.ClientId,
                    TestConstants.ClientSecret);

                // call acquire token with matching username and different assertion hash. this will result in a cache
                // hit.
                var result =
                    await
                        context.AcquireTokenAsync(TestConstants.DefaultResource, clientCredential,
                            new UserAssertion(accessToken));
                Assert.AreEqual(0, HttpMessageHandlerFactory.MockHandlersCount(), "all mocks should have been consumed");
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual("some-access-token", result.AccessToken);
                Assert.AreEqual(TestConstants.DisplayableId, result.UserInfo.DisplayableId);

                //there should be only one cache entry.
                Assert.AreEqual(1, context.TokenCache.Count);

                //assertion hash should be stored in the cache entry.
                Assert.AreEqual(_cryptoHelper.CreateSha256Hash(accessToken),
                    context.TokenCache.tokenCacheDictionary.Values.First().UserAssertionHash);
            }

            [TestMethod]
            [TestCategory("OboFlowTests")]
            public async Task SingleUserWithHashInCacheMatchingUsernameAndMatchingAssertionDifferentResourceTest()
            {
                var context = new AuthenticationContext(TestConstants.AuthorityHomeTenant, new TokenCache());
                string accessToken = "access-token";
                TokenCacheKey key = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                    TestConstants.DefaultResource, TestConstants.ClientId, TokenSubjectType.UserPlusClient,
                    TestConstants.UniqueId, TestConstants.DisplayableId);

                //cache entry has user assertion hash
                string tokenInCache = "obo-access-token";
                context.TokenCache.tokenCacheDictionary[key] = new AuthenticationResultEx
                {
                    RefreshToken = "some-rt",
                    ResourceInResponse = TestConstants.DefaultResource,
                    Result =
                        new AuthenticationResult("Bearer", tokenInCache, _expirationTime)
                        {
                            UserInfo =
                                new UserInfo()
                                {
                                    DisplayableId = TestConstants.DisplayableId,
                                    UniqueId = TestConstants.UniqueId
                                }
                        },
                    UserAssertionHash = _cryptoHelper.CreateSha256Hash(accessToken)
                };
                HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
                {
                    Method = HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(TestConstants.AnotherResource,
                        TestConstants.DisplayableId, TestConstants.UniqueId),
                    PostData = new Dictionary<string, string>()
                {
                    {"client_id", TestConstants.ClientId},
                    {"grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"}
                }
                });

                ClientCredential clientCredential = new ClientCredential(TestConstants.ClientId,
                    TestConstants.ClientSecret);

                // call acquire token with matching username and different assertion hash. this will result in a cache
                // hit.
                var result =
                    await
                        context.AcquireTokenAsync(TestConstants.AnotherResource, clientCredential,
                            new UserAssertion(accessToken, OAuthGrantType.JwtBearer, TestConstants.DisplayableId));
                Assert.AreEqual(0, HttpMessageHandlerFactory.MockHandlersCount(), "all mocks should have been consumed");
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual("some-access-token", result.AccessToken);
                Assert.AreEqual(TestConstants.DisplayableId, result.UserInfo.DisplayableId);

                //there should be only one cache entry.
                Assert.AreEqual(2, context.TokenCache.Count);

                //assertion hash should be stored in the cache entry.
                foreach (var value in context.TokenCache.tokenCacheDictionary.Values)
                {
                    Assert.AreEqual(_cryptoHelper.CreateSha256Hash(accessToken), value.UserAssertionHash);
                }
            }
        }

    }
}
*/
