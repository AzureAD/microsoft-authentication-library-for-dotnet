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
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Cache;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Http;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.OAuth2;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.ADAL.NET.Common;
using Test.ADAL.NET.Common.Mocks;

namespace Test.ADAL.NET.Unit
{
#if !NET_CORE // Enable when bug https://IdentityDivision.visualstudio.com/_workitems/edit/573878 is fixed

    /// <summary>
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
        private readonly DateTimeOffset _expirationTime = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(30);
        private static readonly string[] _cacheNoise = { "", "different" };
        private ICryptographyManager _crypto;

        [TestInitialize]
        public void TestInitialize()
        {
            ModuleInitializer.ForceModuleInitializationTestOnly();
            AdalHttpMessageHandlerFactory.InitializeMockProvider();
            ResetInstanceDiscovery();
            _crypto = PlatformProxyFactory.GetPlatformProxy().CryptographyManager;
        }

        public void ResetInstanceDiscovery()
        {
            InstanceDiscovery.InstanceCache.Clear();
            AdalHttpMessageHandlerFactory.AddMockHandler(MockHelpers.CreateInstanceDiscoveryMockHandler(AdalTestConstants.GetDiscoveryEndpoint(AdalTestConstants.DefaultAuthorityCommonTenant)));
        }

        [TestMethod]
        [TestCategory("OboFlowTests")]
        public async Task MultiUserNoHashInCacheNoUsernamePassedInAssertionTestAsync()
        {
            var context = new AuthenticationContext(AdalTestConstants.DefaultAuthorityHomeTenant, new TokenCache());
            string accessToken = "access-token";

            foreach (var cachenoise in _cacheNoise)
            {
                //cache entry has no user assertion hash
                await context.TokenCache.StoreToCacheAsync(new AdalResultWrapper
                {
                    RefreshToken = cachenoise + "some-rt",
                    ResourceInResponse = AdalTestConstants.DefaultResource,
                    Result = new AdalResult("Bearer", cachenoise + "some-token-in-cache", _expirationTime)
                    {
                        UserInfo =
                            new AdalUserInfo()
                            {
                                DisplayableId = cachenoise + AdalTestConstants.DefaultDisplayableId,
                                UniqueId = cachenoise + AdalTestConstants.DefaultUniqueId
                            },
                        IdToken = MockHelpers.CreateIdToken(AdalTestConstants.DefaultUniqueId, AdalTestConstants.DefaultDisplayableId)
                    },
                },
                AdalTestConstants.DefaultAuthorityHomeTenant, AdalTestConstants.DefaultResource, AdalTestConstants.DefaultClientId, TokenSubjectType.User,
               new RequestContext(new AdalLogger(new Guid()))).ConfigureAwait(false);
            }
            ResetInstanceDiscovery();

            ClientCredential clientCredential = new ClientCredential(AdalTestConstants.DefaultClientId,
                AdalTestConstants.DefaultClientSecret);
            AdalHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(AdalTestConstants.GetTokenEndpoint(AdalTestConstants.DefaultAuthorityHomeTenant))
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                PostData = new Dictionary<string, string>()
                {
                    {"client_id", AdalTestConstants.DefaultClientId},
                    {"grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"}
                }
            });

            // call acquire token with no username. this will result in a network call because cache entry with no assertion hash is
            // treated as a cache miss.
            var result =
                await
                    context.AcquireTokenAsync(AdalTestConstants.DefaultResource, clientCredential,
                        new UserAssertion(accessToken)).ConfigureAwait(false);
            Assert.AreEqual(0, AdalHttpMessageHandlerFactory.MockHandlersCount(), "all mocks should have been consumed");
            Assert.IsNotNull(result.AccessToken);
            Assert.AreEqual("some-access-token", result.AccessToken);
            Assert.AreEqual(AdalTestConstants.DefaultDisplayableId, result.UserInfo.DisplayableId);

            //there should be only one cache entry.
            Assert.AreEqual(2, context.TokenCache.Count);

            //assertion hash should be stored in the cache entry.
            Assert.AreEqual(_crypto.CreateSha256Hash(accessToken),
                context.TokenCache.tokenCacheDictionary.Values.First(x => x.UserAssertionHash != null).UserAssertionHash);
        }

        [TestMethod]
        [TestCategory("OboFlowTests")]
        public async Task MultiUserNoHashInCacheMatchingUsernamePassedInAssertionTestAsync()
        {
            var context = new AuthenticationContext(AdalTestConstants.DefaultAuthorityHomeTenant, new TokenCache());
            string accessToken = "access-token";

            foreach (var cachenoise in _cacheNoise)
            {
                //cache entry has no user assertion hash
                await context.TokenCache.StoreToCacheAsync(new AdalResultWrapper
                {
                    RefreshToken = cachenoise + "some-rt",
                    ResourceInResponse = AdalTestConstants.DefaultResource,
                    Result = new AdalResult("Bearer", cachenoise + "some-token-in-cache", _expirationTime)
                    {
                        UserInfo =
                            new AdalUserInfo()
                            {
                                DisplayableId = cachenoise + AdalTestConstants.DefaultDisplayableId,
                                UniqueId = cachenoise + AdalTestConstants.DefaultUniqueId
                            },
                        IdToken = MockHelpers.CreateIdToken(AdalTestConstants.DefaultUniqueId, AdalTestConstants.DefaultDisplayableId)
                    },
                },
                AdalTestConstants.DefaultAuthorityHomeTenant, AdalTestConstants.DefaultResource, AdalTestConstants.DefaultClientId, TokenSubjectType.User,
               new RequestContext(new AdalLogger(new Guid()))).ConfigureAwait(false);
            }
            ResetInstanceDiscovery();

            ClientCredential clientCredential = new ClientCredential(AdalTestConstants.DefaultClientId,
                AdalTestConstants.DefaultClientSecret);
            AdalHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(AdalTestConstants.GetTokenEndpoint(AdalTestConstants.DefaultAuthorityHomeTenant))
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                PostData = new Dictionary<string, string>()
                {
                    {"client_id", AdalTestConstants.DefaultClientId},
                    {"grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"}
                }
            });

            // call acquire token with matching username from cache entry. this will result in a network call 
            // because cache entry with no assertion hash is treated as a cache miss.

            var result =
                await
                    context.AcquireTokenAsync(AdalTestConstants.DefaultResource, clientCredential,
                        new UserAssertion(accessToken, OAuthGrantType.JwtBearer, AdalTestConstants.DefaultDisplayableId)).ConfigureAwait(false);
            Assert.AreEqual(0, AdalHttpMessageHandlerFactory.MockHandlersCount(), "all mocks should have been consumed");
            Assert.IsNotNull(result.AccessToken);
            Assert.AreEqual("some-access-token", result.AccessToken);
            Assert.AreEqual(AdalTestConstants.DefaultDisplayableId, result.UserInfo.DisplayableId);

            //there should be only one cache entry.
            Assert.AreEqual(2, context.TokenCache.Count);

            //assertion hash should be stored in the cache entry.
            Assert.AreEqual(_crypto.CreateSha256Hash(accessToken),
                context.TokenCache.tokenCacheDictionary.Values.First(x => x.UserAssertionHash != null).UserAssertionHash);
        }

        [TestMethod]
        [TestCategory("OboFlowTests")]
        public async Task MultiUserNoHashInCacheDifferentUsernamePassedInAssertionTestAsync()
        {
            var context = new AuthenticationContext(AdalTestConstants.DefaultAuthorityHomeTenant, new TokenCache());
            string accessToken = "access-token";

            foreach (var cachenoise in _cacheNoise)
            {
                AdalTokenCacheKey key = new AdalTokenCacheKey(AdalTestConstants.DefaultAuthorityHomeTenant,
                    AdalTestConstants.DefaultResource, AdalTestConstants.DefaultClientId, TokenSubjectType.User,
                    cachenoise + AdalTestConstants.DefaultUniqueId, cachenoise + AdalTestConstants.DefaultDisplayableId);
                //cache entry has no user assertion hash
                context.TokenCache.tokenCacheDictionary[key] = new AdalResultWrapper
                {
                    RefreshToken = cachenoise + "some-rt",
                    ResourceInResponse = AdalTestConstants.DefaultResource,
                    Result = new AdalResult("Bearer", cachenoise + "some-token-in-cache", _expirationTime)
                    {
                        UserInfo =
                            new AdalUserInfo()
                            {
                                DisplayableId = cachenoise + AdalTestConstants.DefaultDisplayableId,
                                UniqueId = cachenoise + AdalTestConstants.DefaultUniqueId
                            }
                    },
                };
            }

            ClientCredential clientCredential = new ClientCredential(AdalTestConstants.DefaultClientId,
                AdalTestConstants.DefaultClientSecret);

            string displayableId2 = "extra" + AdalTestConstants.DefaultDisplayableId;
            string uniqueId2 = "extra" + AdalTestConstants.DefaultUniqueId;

            AdalHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(AdalTestConstants.GetTokenEndpoint(AdalTestConstants.DefaultAuthorityHomeTenant))
            {
                Method = HttpMethod.Post,
                ResponseMessage =
                    MockHelpers.CreateSuccessTokenResponseMessage(uniqueId2, displayableId2,
                        AdalTestConstants.DefaultResource),
                PostData = new Dictionary<string, string>()
                {
                    {"client_id", AdalTestConstants.DefaultClientId},
                    {"grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"}
                }
            });

            // call acquire token with diferent username from cache entry. this will result in a network call
            // because cache lookup failed for non-existant user
            var result =
                await
                    context.AcquireTokenAsync(AdalTestConstants.DefaultResource, clientCredential,
                        new UserAssertion(accessToken, OAuthGrantType.JwtBearer,
                            "non-existant" + AdalTestConstants.DefaultDisplayableId)).ConfigureAwait(false);
            Assert.AreEqual(0, AdalHttpMessageHandlerFactory.MockHandlersCount(), "all mocks should have been consumed");
            Assert.IsNotNull(result.AccessToken);
            Assert.AreEqual("some-access-token", result.AccessToken);
            Assert.AreEqual(displayableId2, result.UserInfo.DisplayableId);

            //there should be only one cache entry.
            Assert.AreEqual(3, context.TokenCache.Count);

            //assertion hash should be stored in the cache entry.
            Assert.AreEqual(_crypto.CreateSha256Hash(accessToken),
                context.TokenCache.tokenCacheDictionary.Values.First(x => x.UserAssertionHash != null)
                    .UserAssertionHash);
        }

        [TestMethod]
        [TestCategory("OboFlowTests")]
        public async Task MultiUserWithHashInCacheNoUsernameAndMatchingAssertionTestAsync()
        {
            var context = new AuthenticationContext(AdalTestConstants.DefaultAuthorityHomeTenant, new TokenCache());
            string accessToken = "access-token";
            //cache entry has user assertion hash
            string tokenInCache = "obo-access-token";

            foreach (var cachenoise in _cacheNoise)
            {
                AdalTokenCacheKey key = new AdalTokenCacheKey(AdalTestConstants.DefaultAuthorityHomeTenant,
                    AdalTestConstants.DefaultResource, AdalTestConstants.DefaultClientId, TokenSubjectType.User,
                    cachenoise + AdalTestConstants.DefaultUniqueId, cachenoise + AdalTestConstants.DefaultDisplayableId);

                context.TokenCache.tokenCacheDictionary[key] = new AdalResultWrapper
                {
                    RefreshToken = cachenoise + "some-rt",
                    ResourceInResponse = AdalTestConstants.DefaultResource,
                    Result =
                        new AdalResult("Bearer", cachenoise + tokenInCache, _expirationTime)
                        {
                            UserInfo =
                                new AdalUserInfo()
                                {
                                    DisplayableId = cachenoise + AdalTestConstants.DefaultDisplayableId,
                                    UniqueId = cachenoise + AdalTestConstants.DefaultUniqueId
                                }
                        },
                    UserAssertionHash = _crypto.CreateSha256Hash(cachenoise + accessToken)
                };
            }

            ClientCredential clientCredential = new ClientCredential(AdalTestConstants.DefaultClientId,
                AdalTestConstants.DefaultClientSecret);

            // call acquire token with no username and matching assertion hash. this will result in a cache
            // hit.
            var result =
                await
                    context.AcquireTokenAsync(AdalTestConstants.DefaultResource, clientCredential,
                        new UserAssertion(accessToken)).ConfigureAwait(false);
            Assert.IsNotNull(result.AccessToken);
            Assert.AreEqual(tokenInCache, result.AccessToken);
            Assert.AreEqual(AdalTestConstants.DefaultDisplayableId, result.UserInfo.DisplayableId);

            //there should be only one cache entry.
            Assert.AreEqual(2, context.TokenCache.Count);
        }

        [TestMethod]
        [TestCategory("OboFlowTests")]
        public async Task MultiUserWithHashInCacheNoUsernameAndDifferentAssertionTestAsync()
        {
            var context = new AuthenticationContext(AdalTestConstants.DefaultAuthorityHomeTenant, new TokenCache());
            string accessToken = "access-token";
            //cache entry has user assertion hash
            string tokenInCache = "obo-access-token";

            foreach (var cachenoise in _cacheNoise)
            {
                await context.TokenCache.StoreToCacheAsync(new AdalResultWrapper
                {
                    RefreshToken = cachenoise + "some-rt",
                    ResourceInResponse = AdalTestConstants.DefaultResource,
                    Result =
                        new AdalResult("Bearer", cachenoise + tokenInCache, _expirationTime)
                        {
                            UserInfo =
                                new AdalUserInfo()
                                {
                                    DisplayableId = cachenoise + AdalTestConstants.DefaultDisplayableId,
                                    UniqueId = cachenoise + AdalTestConstants.DefaultUniqueId
                                },
                            IdToken = MockHelpers.CreateIdToken(AdalTestConstants.DefaultUniqueId, AdalTestConstants.DefaultDisplayableId)
                        },
                    UserAssertionHash = _crypto.CreateSha256Hash(cachenoise + accessToken)
                },
                AdalTestConstants.DefaultAuthorityHomeTenant, AdalTestConstants.DefaultResource, AdalTestConstants.DefaultClientId, TokenSubjectType.User,
               new RequestContext(new AdalLogger(new Guid()))).ConfigureAwait(false);
            }
            ResetInstanceDiscovery();

            AdalHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(AdalTestConstants.GetTokenEndpoint(AdalTestConstants.DefaultAuthorityHomeTenant))
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                PostData = new Dictionary<string, string>()
                {
                    {"client_id", AdalTestConstants.DefaultClientId},
                    {"grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"}
                }
            });

            ClientCredential clientCredential = new ClientCredential(AdalTestConstants.DefaultClientId,
                AdalTestConstants.DefaultClientSecret);

            // call acquire token with no username and different assertion hash. this will result in a cache
            // hit.
            var result =
                await
                    context.AcquireTokenAsync(AdalTestConstants.DefaultResource, clientCredential,
                        new UserAssertion("non-existant" + accessToken)).ConfigureAwait(false);
            Assert.AreEqual(0, AdalHttpMessageHandlerFactory.MockHandlersCount(), "all mocks should have been consumed");
            Assert.IsNotNull(result.AccessToken);
            Assert.AreEqual("some-access-token", result.AccessToken);
            Assert.AreEqual(AdalTestConstants.DefaultDisplayableId, result.UserInfo.DisplayableId);

            //there should be only one cache entry.
            Assert.AreEqual(2, context.TokenCache.Count);
        }

        [TestMethod]
        [TestCategory("OboFlowTests")]
        public async Task MultiUserWithHashInCacheMatchingUsernameAndMatchingAssertionTestAsync()
        {
            var context = new AuthenticationContext(AdalTestConstants.DefaultAuthorityHomeTenant, new TokenCache());
            string accessToken = "access-token";
            //cache entry has user assertion hash
            string tokenInCache = "obo-access-token";
            foreach (var cachenoise in _cacheNoise)
            {
                AdalTokenCacheKey key = new AdalTokenCacheKey(AdalTestConstants.DefaultAuthorityHomeTenant,
                    AdalTestConstants.DefaultResource, AdalTestConstants.DefaultClientId, TokenSubjectType.User,
                    cachenoise + AdalTestConstants.DefaultUniqueId, cachenoise + AdalTestConstants.DefaultDisplayableId);

                context.TokenCache.tokenCacheDictionary[key] = new AdalResultWrapper
                {
                    RefreshToken = cachenoise + "some-rt",
                    ResourceInResponse = AdalTestConstants.DefaultResource,
                    Result =
                        new AdalResult("Bearer", cachenoise + tokenInCache, _expirationTime)
                        {
                            UserInfo =
                                new AdalUserInfo()
                                {
                                    DisplayableId = cachenoise + AdalTestConstants.DefaultDisplayableId,
                                    UniqueId = cachenoise + AdalTestConstants.DefaultUniqueId
                                }
                        },
                    UserAssertionHash = _crypto.CreateSha256Hash(cachenoise + accessToken)
                };
            }

            ClientCredential clientCredential = new ClientCredential(AdalTestConstants.DefaultClientId,
                AdalTestConstants.DefaultClientSecret);

            // call acquire token with matching username and matching assertion hash. this will result in a cache
            // hit.
            var result =
                await
                    context.AcquireTokenAsync(AdalTestConstants.DefaultResource, clientCredential,
                        new UserAssertion(accessToken, OAuthGrantType.JwtBearer, AdalTestConstants.DefaultDisplayableId)).ConfigureAwait(false);
            Assert.IsNotNull(result.AccessToken);
            Assert.AreEqual(tokenInCache, result.AccessToken);
            Assert.AreEqual(AdalTestConstants.DefaultDisplayableId, result.UserInfo.DisplayableId);

            //there should be only one cache entry.
            Assert.AreEqual(2, context.TokenCache.Count);

            //assertion hash should be stored in the cache entry.
            var expectedHash = _crypto.CreateSha256Hash(accessToken);

            Assert.IsTrue(context.TokenCache.tokenCacheDictionary.Values.Any(v => v.UserAssertionHash == expectedHash));
        }

        [TestMethod]
        [TestCategory("OboFlowTests")]
        public async Task MultiUserWithHashInCacheMatchingUsernameAndDifferentAssertionTestAsync()
        {
            var context = new AuthenticationContext(AdalTestConstants.DefaultAuthorityHomeTenant, new TokenCache());
            string accessToken = "access-token";
            //cache entry has user assertion hash
            string tokenInCache = "obo-access-token";
            foreach (var cachenoise in _cacheNoise)
            {
                await context.TokenCache.StoreToCacheAsync(new AdalResultWrapper
                {
                    RefreshToken = cachenoise + "some-rt",
                    ResourceInResponse = AdalTestConstants.DefaultResource,
                    Result =
                        new AdalResult("Bearer", cachenoise + tokenInCache, _expirationTime)
                        {
                            UserInfo =
                                new AdalUserInfo()
                                {
                                    DisplayableId = cachenoise + AdalTestConstants.DefaultDisplayableId,
                                    UniqueId = cachenoise + AdalTestConstants.DefaultUniqueId
                                },
                            IdToken = MockHelpers.CreateIdToken(AdalTestConstants.DefaultUniqueId, AdalTestConstants.DefaultDisplayableId)
                        },
                    UserAssertionHash = _crypto.CreateSha256Hash(cachenoise + accessToken)
                },
                AdalTestConstants.DefaultAuthorityHomeTenant, AdalTestConstants.DefaultResource, AdalTestConstants.DefaultClientId, TokenSubjectType.User,
               new RequestContext(new AdalLogger(new Guid()))).ConfigureAwait(false);
            }
            ResetInstanceDiscovery();

            AdalHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(AdalTestConstants.GetTokenEndpoint(AdalTestConstants.DefaultAuthorityHomeTenant))
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                PostData = new Dictionary<string, string>()
                {
                    {"client_id", AdalTestConstants.DefaultClientId},
                    {"grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"}
                }
            });

            ClientCredential clientCredential = new ClientCredential(AdalTestConstants.DefaultClientId,
                AdalTestConstants.DefaultClientSecret);

            // call acquire token with matching username and different assertion hash. this will result in a cache
            // hit.
            var result =
                await
                    context.AcquireTokenAsync(AdalTestConstants.DefaultResource, clientCredential,
                        new UserAssertion("non-existant" + accessToken, OAuthGrantType.JwtBearer,
                            AdalTestConstants.DefaultDisplayableId)).ConfigureAwait(false);
            Assert.AreEqual(0, AdalHttpMessageHandlerFactory.MockHandlersCount(), "all mocks should have been consumed");
            Assert.IsNotNull(result.AccessToken);
            Assert.AreEqual("some-access-token", result.AccessToken);
            Assert.AreEqual(AdalTestConstants.DefaultDisplayableId, result.UserInfo.DisplayableId);

            //there should be only one cache entry.
            Assert.AreEqual(2, context.TokenCache.Count);
        }

        [TestMethod]
        [TestCategory("OboFlowTests")]
        public async Task SingleUserNoHashInCacheNoUsernamePassedInAssertionTestAsync()
        {
            var context = new AuthenticationContext(AdalTestConstants.DefaultAuthorityHomeTenant, new TokenCache());
            string accessToken = "access-token";
            await context.TokenCache.StoreToCacheAsync(new AdalResultWrapper
            {
                RefreshToken = "some-rt",
                ResourceInResponse = AdalTestConstants.DefaultResource,
                Result = new AdalResult("Bearer", "some-token-in-cache", _expirationTime)
                {
                    UserInfo =
                        new AdalUserInfo()
                        {
                            DisplayableId = AdalTestConstants.DefaultDisplayableId,
                            UniqueId = AdalTestConstants.DefaultUniqueId
                        },
                    IdToken = MockHelpers.CreateIdToken(AdalTestConstants.DefaultUniqueId, AdalTestConstants.DefaultDisplayableId)
                },
                //cache entry has no user assertion hash
            },
            AdalTestConstants.DefaultAuthorityHomeTenant, AdalTestConstants.DefaultResource, AdalTestConstants.DefaultClientId, TokenSubjectType.User,
           new RequestContext(new AdalLogger(new Guid()))).ConfigureAwait(false);
            ResetInstanceDiscovery();

            ClientCredential clientCredential = new ClientCredential(AdalTestConstants.DefaultClientId,
                AdalTestConstants.DefaultClientSecret);
            AdalHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(AdalTestConstants.GetTokenEndpoint(AdalTestConstants.DefaultAuthorityHomeTenant))
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                PostData = new Dictionary<string, string>()
                {
                    {"client_id", AdalTestConstants.DefaultClientId},
                    {"grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"}
                }
            });

            // call acquire token with no username. this will result in a network call because cache entry with no assertion hash is
            // treated as a cache miss.
            var result =
                await
                    context.AcquireTokenAsync(AdalTestConstants.DefaultResource, clientCredential,
                        new UserAssertion(accessToken)).ConfigureAwait(false);
            Assert.AreEqual(0, AdalHttpMessageHandlerFactory.MockHandlersCount(), "all mocks should have been consumed");
            Assert.IsNotNull(result.AccessToken);
            Assert.AreEqual("some-access-token", result.AccessToken);
            Assert.AreEqual(AdalTestConstants.DefaultDisplayableId, result.UserInfo.DisplayableId);

            //there should be only one cache entry.
            Assert.AreEqual(1, context.TokenCache.Count);

            //assertion hash should be stored in the cache entry.
            Assert.AreEqual(_crypto.CreateSha256Hash(accessToken),
                context.TokenCache.tokenCacheDictionary.Values.First().UserAssertionHash);
        }

        [TestMethod]
        [TestCategory("OboFlowTests")]
        public async Task SingleUserNoHashInCacheMatchingUsernamePassedInAssertionTestAsync()
        {
            var context = new AuthenticationContext(AdalTestConstants.DefaultAuthorityHomeTenant, new TokenCache());
            string accessToken = "access-token";
            await context.TokenCache.StoreToCacheAsync(new AdalResultWrapper
            {
                RefreshToken = "some-rt",
                ResourceInResponse = AdalTestConstants.DefaultResource,
                Result = new AdalResult("Bearer", "some-token-in-cache", _expirationTime)
                {
                    UserInfo =
                        new AdalUserInfo()
                        {
                            DisplayableId = AdalTestConstants.DefaultDisplayableId,
                            UniqueId = AdalTestConstants.DefaultUniqueId
                        },
                    IdToken = MockHelpers.CreateIdToken(AdalTestConstants.DefaultUniqueId, AdalTestConstants.DefaultDisplayableId)
                },
                //cache entry has no user assertion hash
            },
            AdalTestConstants.DefaultAuthorityHomeTenant, AdalTestConstants.DefaultResource, AdalTestConstants.DefaultClientId, TokenSubjectType.User,
           new RequestContext(new AdalLogger(new Guid()))).ConfigureAwait(false);
            ResetInstanceDiscovery();

            ClientCredential clientCredential = new ClientCredential(AdalTestConstants.DefaultClientId,
                AdalTestConstants.DefaultClientSecret);
            AdalHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(AdalTestConstants.GetTokenEndpoint(AdalTestConstants.DefaultAuthorityHomeTenant))
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                PostData = new Dictionary<string, string>()
                {
                    {"client_id", AdalTestConstants.DefaultClientId},
                    {"grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"}
                }
            });

            // call acquire token with matching username from cache entry. this will result in a network call 
            // because cache entry with no assertion hash is treated as a cache miss.

            var result =
                await
                    context.AcquireTokenAsync(AdalTestConstants.DefaultResource, clientCredential,
                        new UserAssertion(accessToken, OAuthGrantType.JwtBearer, AdalTestConstants.DefaultDisplayableId)).ConfigureAwait(false);
            Assert.AreEqual(0, AdalHttpMessageHandlerFactory.MockHandlersCount(), "all mocks should have been consumed");
            Assert.IsNotNull(result.AccessToken);
            Assert.AreEqual("some-access-token", result.AccessToken);
            Assert.AreEqual(AdalTestConstants.DefaultDisplayableId, result.UserInfo.DisplayableId);

            //there should be only one cache entry.
            Assert.AreEqual(1, context.TokenCache.Count);

            //assertion hash should be stored in the cache entry.
            Assert.AreEqual(_crypto.CreateSha256Hash(accessToken),
                context.TokenCache.tokenCacheDictionary.Values.First().UserAssertionHash);
        }

        [TestMethod]
        [TestCategory("OboFlowTests")]
        public async Task SingleUserNoHashInCacheDifferentUsernamePassedInAssertionTestAsync()
        {
            var context = new AuthenticationContext(AdalTestConstants.DefaultAuthorityHomeTenant, new TokenCache());
            string accessToken = "access-token";
            AdalTokenCacheKey key = new AdalTokenCacheKey(AdalTestConstants.DefaultAuthorityHomeTenant,
                AdalTestConstants.DefaultResource, AdalTestConstants.DefaultClientId, TokenSubjectType.User,
                AdalTestConstants.DefaultUniqueId, AdalTestConstants.DefaultDisplayableId);
            //cache entry has no user assertion hash
            context.TokenCache.tokenCacheDictionary[key] = new AdalResultWrapper
            {
                RefreshToken = "some-rt",
                ResourceInResponse = AdalTestConstants.DefaultResource,
                Result = new AdalResult("Bearer", "some-token-in-cache", _expirationTime)
                {
                    UserInfo =
                        new AdalUserInfo()
                        {
                            DisplayableId = AdalTestConstants.DefaultDisplayableId,
                            UniqueId = AdalTestConstants.DefaultUniqueId
                        }
                },
            };

            ClientCredential clientCredential = new ClientCredential(AdalTestConstants.DefaultClientId,
                AdalTestConstants.DefaultClientSecret);

            string displayableId2 = "extra" + AdalTestConstants.DefaultDisplayableId;
            string uniqueId2 = "extra" + AdalTestConstants.DefaultUniqueId;

            AdalHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(AdalTestConstants.GetTokenEndpoint(AdalTestConstants.DefaultAuthorityHomeTenant))
            {
                Method = HttpMethod.Post,
                ResponseMessage =
                    MockHelpers.CreateSuccessTokenResponseMessage(uniqueId2, displayableId2,
                        AdalTestConstants.DefaultResource),
                PostData = new Dictionary<string, string>()
                {
                    {"client_id", AdalTestConstants.DefaultClientId},
                    {"grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"}
                }
            });

            // call acquire token with diferent username from cache entry. this will result in a network call
            // because cache lookup failed for non-existant user
            var result =
                await
                    context.AcquireTokenAsync(AdalTestConstants.DefaultResource, clientCredential,
                        new UserAssertion(accessToken, OAuthGrantType.JwtBearer, displayableId2
                            )).ConfigureAwait(false);
            Assert.AreEqual(0, AdalHttpMessageHandlerFactory.MockHandlersCount(), "all mocks should have been consumed");
            Assert.IsNotNull(result.AccessToken);
            Assert.AreEqual("some-access-token", result.AccessToken);
            Assert.AreEqual(displayableId2, result.UserInfo.DisplayableId);

            //there should be only one cache entry.
            Assert.AreEqual(2, context.TokenCache.Count);

            //assertion hash should be stored in the cache entry.
            Assert.AreEqual(_crypto.CreateSha256Hash(accessToken),
                context.TokenCache.tokenCacheDictionary.Values.First(
                    s => s.Result.UserInfo != null && s.Result.UserInfo.DisplayableId.Equals(displayableId2, StringComparison.OrdinalIgnoreCase))
                    .UserAssertionHash);
        }

        [TestMethod]
        [TestCategory("OboFlowTests")]
        public async Task SingleUserWithHashInCacheNoUsernameAndMatchingAssertionTestAsync()
        {
            var context = new AuthenticationContext(AdalTestConstants.DefaultAuthorityHomeTenant, new TokenCache());
            string accessToken = "access-token";
            AdalTokenCacheKey key = new AdalTokenCacheKey(AdalTestConstants.DefaultAuthorityHomeTenant,
                AdalTestConstants.DefaultResource, AdalTestConstants.DefaultClientId, TokenSubjectType.User,
                AdalTestConstants.DefaultUniqueId, AdalTestConstants.DefaultDisplayableId);

            //cache entry has user assertion hash
            string tokenInCache = "obo-access-token";
            context.TokenCache.tokenCacheDictionary[key] = new AdalResultWrapper
            {
                RefreshToken = "some-rt",
                ResourceInResponse = AdalTestConstants.DefaultResource,
                Result =
                    new AdalResult("Bearer", tokenInCache, _expirationTime)
                    {
                        UserInfo =
                            new AdalUserInfo()
                            {
                                DisplayableId = AdalTestConstants.DefaultDisplayableId,
                                UniqueId = AdalTestConstants.DefaultUniqueId
                            }
                    },
                UserAssertionHash = _crypto.CreateSha256Hash(accessToken)
            };

            ClientCredential clientCredential = new ClientCredential(AdalTestConstants.DefaultClientId,
                AdalTestConstants.DefaultClientSecret);

            // call acquire token with no username and matching assertion hash. this will result in a cache
            // hit.
            var result =
                await
                    context.AcquireTokenAsync(AdalTestConstants.DefaultResource, clientCredential,
                        new UserAssertion(accessToken)).ConfigureAwait(false);
            Assert.IsNotNull(result.AccessToken);
            Assert.AreEqual(tokenInCache, result.AccessToken);
            Assert.AreEqual(AdalTestConstants.DefaultDisplayableId, result.UserInfo.DisplayableId);

            //there should be only one cache entry.
            Assert.AreEqual(1, context.TokenCache.Count);

            //assertion hash should be stored in the cache entry.
            Assert.AreEqual(_crypto.CreateSha256Hash(accessToken),
                context.TokenCache.tokenCacheDictionary.Values.First().UserAssertionHash);
        }

        [TestMethod]
        [TestCategory("OboFlowTests")]
        public async Task SingleUserWithHashInCacheNoUsernameAndDifferentAssertionTestAsync()
        {
            var context = new AuthenticationContext(AdalTestConstants.DefaultAuthorityHomeTenant, new TokenCache());
            string accessToken = "access-token";
            string tokenInCache = "obo-access-token";
            await context.TokenCache.StoreToCacheAsync(new AdalResultWrapper
            {
                RefreshToken = "some-rt",
                ResourceInResponse = AdalTestConstants.DefaultResource,
                Result =
                    new AdalResult("Bearer", tokenInCache, _expirationTime)
                    {
                        UserInfo =
                            new AdalUserInfo()
                            {
                                DisplayableId = AdalTestConstants.DefaultDisplayableId,
                                UniqueId = AdalTestConstants.DefaultUniqueId
                            },
                        IdToken = MockHelpers.CreateIdToken(AdalTestConstants.DefaultUniqueId, AdalTestConstants.DefaultDisplayableId)
                    },
                UserAssertionHash = _crypto.CreateSha256Hash(accessToken + "different")
            },
            AdalTestConstants.DefaultAuthorityHomeTenant, AdalTestConstants.DefaultResource, AdalTestConstants.DefaultClientId, TokenSubjectType.User,
           new RequestContext(new AdalLogger(new Guid()))).ConfigureAwait(false);
            ResetInstanceDiscovery();

            AdalHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(AdalTestConstants.GetTokenEndpoint(AdalTestConstants.DefaultAuthorityHomeTenant))
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                PostData = new Dictionary<string, string>()
                {
                    {"client_id", AdalTestConstants.DefaultClientId},
                    {"grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"}
                }
            });

            ClientCredential clientCredential = new ClientCredential(AdalTestConstants.DefaultClientId,
                AdalTestConstants.DefaultClientSecret);

            // call acquire token with no username and different assertion hash. this will result in a cache
            // hit.
            var result =
                await
                    context.AcquireTokenAsync(AdalTestConstants.DefaultResource, clientCredential,
                        new UserAssertion(accessToken)).ConfigureAwait(false);
            Assert.AreEqual(0, AdalHttpMessageHandlerFactory.MockHandlersCount(), "all mocks should have been consumed");
            Assert.IsNotNull(result.AccessToken);
            Assert.AreEqual("some-access-token", result.AccessToken);
            Assert.AreEqual(AdalTestConstants.DefaultDisplayableId, result.UserInfo.DisplayableId);

            //there should be only one cache entry.
            Assert.AreEqual(1, context.TokenCache.Count);

            //assertion hash should be stored in the cache entry.
            Assert.AreEqual(_crypto.CreateSha256Hash(accessToken),
                context.TokenCache.tokenCacheDictionary.Values.First().UserAssertionHash);
        }


        [TestMethod]
        [TestCategory("OboFlowTests")]
        public async Task SingleUserWithHashInCacheMatchingUsernameAndMatchingAssertionTestAsync()
        {
            var context = new AuthenticationContext(AdalTestConstants.DefaultAuthorityHomeTenant, new TokenCache());
            string accessToken = "access-token";
            AdalTokenCacheKey key = new AdalTokenCacheKey(AdalTestConstants.DefaultAuthorityHomeTenant,
                AdalTestConstants.DefaultResource, AdalTestConstants.DefaultClientId, TokenSubjectType.User,
                AdalTestConstants.DefaultUniqueId, AdalTestConstants.DefaultDisplayableId);

            //cache entry has user assertion hash
            string tokenInCache = "obo-access-token";
            context.TokenCache.tokenCacheDictionary[key] = new AdalResultWrapper
            {
                RefreshToken = "some-rt",
                ResourceInResponse = AdalTestConstants.DefaultResource,
                Result =
                    new AdalResult("Bearer", tokenInCache, _expirationTime)
                    {
                        UserInfo =
                            new AdalUserInfo()
                            {
                                DisplayableId = AdalTestConstants.DefaultDisplayableId,
                                UniqueId = AdalTestConstants.DefaultUniqueId
                            }
                    },
                UserAssertionHash = _crypto.CreateSha256Hash(accessToken)
            };

            ClientCredential clientCredential = new ClientCredential(AdalTestConstants.DefaultClientId,
                AdalTestConstants.DefaultClientSecret);

            // call acquire token with matching username and matching assertion hash. this will result in a cache
            // hit.
            var result =
                await
                    context.AcquireTokenAsync(AdalTestConstants.DefaultResource, clientCredential,
                        new UserAssertion(accessToken)).ConfigureAwait(false);
            Assert.IsNotNull(result.AccessToken);
            Assert.AreEqual(tokenInCache, result.AccessToken);
            Assert.AreEqual(AdalTestConstants.DefaultDisplayableId, result.UserInfo.DisplayableId);

            //there should be only one cache entry.
            Assert.AreEqual(1, context.TokenCache.Count);

            //assertion hash should be stored in the cache entry.
            Assert.AreEqual(_crypto.CreateSha256Hash(accessToken),
                context.TokenCache.tokenCacheDictionary.Values.First().UserAssertionHash);
        }

        [TestMethod]
        [TestCategory("OboFlowTests")]
        public async Task SingleUserWithHashInCacheMatchingUsernameAndDifferentAssertionTestAsync()
        {
            var context = new AuthenticationContext(AdalTestConstants.DefaultAuthorityHomeTenant, new TokenCache());
            string accessToken = "access-token";
            string tokenInCache = "obo-access-token";
            await context.TokenCache.StoreToCacheAsync(new AdalResultWrapper
            {
                RefreshToken = "some-rt",
                ResourceInResponse = AdalTestConstants.DefaultResource,
                Result =
                    new AdalResult("Bearer", tokenInCache, _expirationTime)
                    {
                        UserInfo =
                            new AdalUserInfo()
                            {
                                DisplayableId = AdalTestConstants.DefaultDisplayableId,
                                UniqueId = AdalTestConstants.DefaultUniqueId
                            },
                        IdToken = MockHelpers.CreateIdToken(AdalTestConstants.DefaultUniqueId, AdalTestConstants.DefaultDisplayableId)
                    },
                UserAssertionHash = _crypto.CreateSha256Hash(accessToken + "different")
            },
            AdalTestConstants.DefaultAuthorityHomeTenant, AdalTestConstants.DefaultResource, AdalTestConstants.DefaultClientId, TokenSubjectType.User,
           new RequestContext(new AdalLogger(new Guid()))).ConfigureAwait(false);
            ResetInstanceDiscovery();

            AdalHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(AdalTestConstants.GetTokenEndpoint(AdalTestConstants.DefaultAuthorityHomeTenant))
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                PostData = new Dictionary<string, string>()
                {
                    {"client_id", AdalTestConstants.DefaultClientId},
                    {"grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"}
                }
            });

            ClientCredential clientCredential = new ClientCredential(AdalTestConstants.DefaultClientId,
                AdalTestConstants.DefaultClientSecret);

            // call acquire token with matching username and different assertion hash. this will result in a cache
            // hit.
            var result =
                await
                    context.AcquireTokenAsync(AdalTestConstants.DefaultResource, clientCredential,
                        new UserAssertion(accessToken)).ConfigureAwait(false);
            Assert.AreEqual(0, AdalHttpMessageHandlerFactory.MockHandlersCount(), "all mocks should have been consumed");
            Assert.IsNotNull(result.AccessToken);
            Assert.AreEqual("some-access-token", result.AccessToken);
            Assert.AreEqual(AdalTestConstants.DefaultDisplayableId, result.UserInfo.DisplayableId);

            //there should be only one cache entry.
            Assert.AreEqual(1, context.TokenCache.Count);

            //assertion hash should be stored in the cache entry.
            Assert.AreEqual(_crypto.CreateSha256Hash(accessToken),
                context.TokenCache.tokenCacheDictionary.Values.First().UserAssertionHash);
        }

        [TestMethod]
        [TestCategory("OboFlowTests")]
        public async Task SingleUserWithHashInCacheMatchingUsernameAndMatchingAssertionDifferentResourceTestAsync()
        {
            var context = new AuthenticationContext(AdalTestConstants.DefaultAuthorityHomeTenant, new TokenCache());
            string accessToken = "access-token";
            AdalTokenCacheKey key = new AdalTokenCacheKey(AdalTestConstants.DefaultAuthorityHomeTenant,
                AdalTestConstants.DefaultResource, AdalTestConstants.DefaultClientId, TokenSubjectType.UserPlusClient,
                AdalTestConstants.DefaultUniqueId, AdalTestConstants.DefaultDisplayableId);

            //cache entry has user assertion hash
            string tokenInCache = "obo-access-token";
            context.TokenCache.tokenCacheDictionary[key] = new AdalResultWrapper
            {
                RefreshToken = "some-rt",
                ResourceInResponse = AdalTestConstants.DefaultResource,
                Result =
                    new AdalResult("Bearer", tokenInCache, _expirationTime)
                    {
                        UserInfo =
                            new AdalUserInfo()
                            {
                                DisplayableId = AdalTestConstants.DefaultDisplayableId,
                                UniqueId = AdalTestConstants.DefaultUniqueId
                            }
                    },
                UserAssertionHash = _crypto.CreateSha256Hash(accessToken)
            };
            AdalHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(AdalTestConstants.GetTokenEndpoint(AdalTestConstants.DefaultAuthorityHomeTenant))
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(AdalTestConstants.AnotherResource,
                    AdalTestConstants.DefaultDisplayableId, AdalTestConstants.DefaultUniqueId),
                PostData = new Dictionary<string, string>()
                {
                    {"client_id", AdalTestConstants.DefaultClientId},
                    {"grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"}
                }
            });

            ClientCredential clientCredential = new ClientCredential(AdalTestConstants.DefaultClientId,
                AdalTestConstants.DefaultClientSecret);

            // call acquire token with matching username and different assertion hash. this will result in a cache
            // hit.
            var result =
                await
                    context.AcquireTokenAsync(AdalTestConstants.AnotherResource, clientCredential,
                        new UserAssertion(accessToken, OAuthGrantType.JwtBearer, AdalTestConstants.DefaultDisplayableId)).ConfigureAwait(false);
            Assert.AreEqual(0, AdalHttpMessageHandlerFactory.MockHandlersCount(), "all mocks should have been consumed");
            Assert.IsNotNull(result.AccessToken);
            Assert.AreEqual("some-access-token", result.AccessToken);
            Assert.AreEqual(AdalTestConstants.DefaultDisplayableId, result.UserInfo.DisplayableId);

            //there should be only one cache entry.
            Assert.AreEqual(2, context.TokenCache.Count);

            //assertion hash should be stored in the cache entry.
            foreach (var value in context.TokenCache.tokenCacheDictionary.Values)
            {
                Assert.AreEqual(_crypto.CreateSha256Hash(accessToken), value.UserAssertionHash);
            }
        }
    }
#endif
}
