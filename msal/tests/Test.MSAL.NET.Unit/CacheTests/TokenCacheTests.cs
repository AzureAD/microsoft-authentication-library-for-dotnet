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
using System.IO;
using System.Linq;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Cache;
using Microsoft.Identity.Core.Helpers;
using Microsoft.Identity.Core.Instance;
using Microsoft.Identity.Core.OAuth2;
using Microsoft.Identity.Core.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.Microsoft.Identity.Core.Unit;
using Test.Microsoft.Identity.Core.Unit.Mocks;
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
            new TestPlatformInformation();
            cache = new TokenCache();
            new TestLogger(Guid.Empty);

            HttpClientFactory.ReturnHttpClientForMocks = true;
            HttpMessageHandlerFactory.ClearMockHandlers();

            AadInstanceDiscovery.Instance.InstanceCache.Clear();

            HttpMessageHandlerFactory.AddMockHandler(
                MockHelpers.CreateInstanceDiscoveryMockHandler(
                    TestConstants.GetDiscoveryEndpoint(TestConstants.AuthorityCommonTenant)));
        }
        
        void AddHostToInstanceCache(String host)
        {
            AadInstanceDiscovery.Instance.InstanceCache.TryAdd(host, new InstanceDiscoveryMetadataEntry
            {
                PreferredNetwork = host,
                PreferredCache = host,
                Aliases = new string[] { host }
            });
        }

        [TestCleanup]
        public void TestCleanup()
        {
            cache.Clear();
        }


        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void GetExactScopesMatchedAccessTokenTest()
        {
            TokenCache cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };
            var atItem = new MsalAccessTokenCacheItem(
                TestConstants.ProductionPrefNetworkEnvironment,
                TestConstants.ClientId,
                "Bearer",
                TestConstants.Scope.AsSingleString(),
                TestConstants.Utid,
                "",
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)),
                MockHelpers.CreateClientInfo());

            // create key out of access token cache item and then
            // set it as the value of the access token.
            string atKey = atItem.GetKey().ToString();
            atItem.Secret = atKey;

            cache.tokenCacheAccessor.AccessTokenCacheDictionary[atKey] = JsonHelper.SerializeToJson(atItem);
            MsalAccessTokenCacheItem item = cache.FindAccessTokenAsync(new AuthenticationRequestParameters()
            {
                RequestContext = new RequestContext(new MsalLogger(Guid.Empty, null)),
                ClientId = TestConstants.ClientId,
                Authority = Authority.CreateAuthority(TestConstants.AuthorityTestTenant, false),
                Scope = TestConstants.Scope,
                Account = TestConstants.User
            }).Result;

            Assert.IsNotNull(item);
            Assert.AreEqual(atKey.ToString(), item.Secret);
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void GetSubsetScopesMatchedAccessTokenTest()
        {
            TokenCache cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };

            var atItem = new MsalAccessTokenCacheItem(
                TestConstants.ProductionPrefNetworkEnvironment,
                TestConstants.ClientId,
                "Bearer",
                TestConstants.Scope.AsSingleString(),
                TestConstants.Utid,
                null,
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromHours(1)),
                MockHelpers.CreateClientInfo());

            // create key out of access token cache item and then
            // set it as the value of the access token.
            string atKey = atItem.GetKey().ToString();
            atItem.Secret = atKey;

            cache.tokenCacheAccessor.AccessTokenCacheDictionary[atKey] = JsonHelper.SerializeToJson(atItem);
            var param = new AuthenticationRequestParameters()
            {
                RequestContext = new RequestContext(new MsalLogger(Guid.Empty, null)),
                ClientId = TestConstants.ClientId,
                Authority = Authority.CreateAuthority(TestConstants.AuthorityTestTenant, false),
                Scope = new SortedSet<string>(),
                Account = TestConstants.User
            };

            param.Scope.Add("r1/scope1");
            MsalAccessTokenCacheItem item = cache.FindAccessTokenAsync(param).Result;

            Assert.IsNotNull(item);
            Assert.AreEqual(atKey.ToString(), item.Secret);
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void GetIntersectedScopesMatchedAccessTokenTest()
        {
            TokenCache cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };

            var atItem = new MsalAccessTokenCacheItem(
                TestConstants.ProductionPrefNetworkEnvironment,
                TestConstants.ClientId,
                "Bearer",
                TestConstants.Scope.AsSingleString(),
                TestConstants.Utid,
                null,
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromHours(1)),
                MockHelpers.CreateClientInfo());

            // create key out of access token cache item and then
            // set it as the value of the access token.
            string atKey = atItem.GetKey().ToString();
            atItem.Secret = atKey;

            cache.tokenCacheAccessor.AccessTokenCacheDictionary[atKey] = JsonHelper.SerializeToJson(atItem);

            var param = new AuthenticationRequestParameters()
            {
                RequestContext = new RequestContext(new MsalLogger(Guid.Empty, null)),
                ClientId = TestConstants.ClientId,
                Authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false),
                Scope = new SortedSet<string>(),
                Account =
                    new Account()
                    {
                        Username = TestConstants.DisplayableId,
                        HomeAccountId = TestConstants.UserIdentifier
                    }
            };

            param.Scope.Add(TestConstants.Scope.First());
            param.Scope.Add("non-existant-scopes");
            MsalAccessTokenCacheItem item = cache.FindAccessTokenAsync(param).Result;

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

            var atItem = new MsalAccessTokenCacheItem(
                TestConstants.ProductionPrefNetworkEnvironment,
                TestConstants.ClientId,
                "Bearer",
                TestConstants.Scope.AsSingleString(),
                TestConstants.Utid,
                null,
                new DateTimeOffset(DateTime.UtcNow),
                MockHelpers.CreateClientInfo());

            atItem.Secret = atItem.GetKey().ToString();
            cache.tokenCacheAccessor.AccessTokenCacheDictionary[atItem.GetKey().ToString()] =
                JsonHelper.SerializeToJson(atItem);

            Assert.IsNull(cache.FindAccessTokenAsync(new AuthenticationRequestParameters()
            {
                RequestContext = new RequestContext(new MsalLogger(Guid.Empty, null)),
                ClientId = TestConstants.ClientId,
                Authority = Authority.CreateAuthority(TestConstants.AuthorityTestTenant, false),
                Scope = TestConstants.Scope,
                Account =
                    new Account()
                    {
                        Username = TestConstants.DisplayableId,
                        HomeAccountId = TestConstants.UserIdentifier
                    }
            }).Result);
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void GetAccessTokenExpiryInRangeTest()
        {
            cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };

            var atItem = new MsalAccessTokenCacheItem(
                TestConstants.ProductionPrefNetworkEnvironment,
                TestConstants.ClientId,
                "Bearer",
                TestConstants.Scope.AsSingleString(),
                TestConstants.Utid,
                "",
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromMinutes(4)),
                MockHelpers.CreateClientInfo());

            atItem.Secret = atItem.GetKey().ToString();
            cache.tokenCacheAccessor.AccessTokenCacheDictionary[atItem.GetKey().ToString()] =
                JsonHelper.SerializeToJson(atItem);

            Assert.IsNull(cache.FindAccessTokenAsync(new AuthenticationRequestParameters()
            {
                RequestContext = new RequestContext(new MsalLogger(Guid.Empty, null)),
                ClientId = TestConstants.ClientId,
                Authority = Authority.CreateAuthority(TestConstants.AuthorityTestTenant, false),
                Scope = TestConstants.Scope,
                Account =
                    new Account()
                    {
                        Username = TestConstants.DisplayableId,
                        HomeAccountId = TestConstants.UserIdentifier
                    }
            }).Result);
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void GetRefreshTokenTest()
        {
            TokenCache cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };

            MsalRefreshTokenCacheItem rtItem = new MsalRefreshTokenCacheItem
                (TestConstants.ProductionPrefNetworkEnvironment, TestConstants.ClientId, "someRT", MockHelpers.CreateClientInfo());

            string rtKey = rtItem.GetKey().ToString();
            cache.tokenCacheAccessor.RefreshTokenCacheDictionary[rtKey] = JsonHelper.SerializeToJson(rtItem);
            var authParams = new AuthenticationRequestParameters()
            {
                RequestContext = new RequestContext(new MsalLogger(Guid.Empty, null)),
                ClientId = TestConstants.ClientId,
                Authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false),
                Scope = TestConstants.Scope,
                Account = TestConstants.User
            };
            Assert.IsNotNull(cache.FindRefreshTokenAsync(authParams));

            // RT is stored by environment, client id and userIdentifier as index.
            // any change to authority (within same environment), uniqueid and displyableid will not 
            // change the outcome of cache look up.
            Assert.IsNotNull(cache.FindRefreshTokenAsync(new AuthenticationRequestParameters()
            {
                RequestContext = new RequestContext(new MsalLogger(Guid.Empty, null)),
                ClientId = TestConstants.ClientId,
                Authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant + "more", false),
                Scope = TestConstants.Scope,
                Account = TestConstants.User
            }));
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void GetRefreshTokenDifferentEnvironmentTest()
        {
            TokenCache cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };
            MsalRefreshTokenCacheItem rtItem = new MsalRefreshTokenCacheItem
                (TestConstants.SovereignEnvironment, TestConstants.ClientId, "someRT", MockHelpers.CreateClientInfo());

            string rtKey = rtItem.GetKey().ToString();
            cache.tokenCacheAccessor.RefreshTokenCacheDictionary[rtKey] = JsonHelper.SerializeToJson(rtItem);
            var authParams = new AuthenticationRequestParameters()
            {
                RequestContext = new RequestContext(new MsalLogger(Guid.Empty, null)),
                ClientId = TestConstants.ClientId,
                Authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false),
                Scope = TestConstants.Scope,
                Account = TestConstants.User
            };
            var rt = cache.FindRefreshTokenAsync(authParams).Result;
            Assert.IsNull(rt);
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void GetAppTokenFromCacheTest()
        {
            cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };

            var atItem = new MsalAccessTokenCacheItem(
                TestConstants.ProductionPrefNetworkEnvironment,
                TestConstants.ClientId,
                "Bearer",
                TestConstants.Scope.AsSingleString(),
                TestConstants.Utid,
                null,
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)),
                MockHelpers.CreateClientInfo());

            string atKey = atItem.GetKey().ToString();
            atItem.Secret = atKey;

            cache.tokenCacheAccessor.AccessTokenCacheDictionary[atItem.GetKey().ToString()] =
                JsonHelper.SerializeToJson(atItem);

            MsalAccessTokenCacheItem cacheItem = cache.FindAccessTokenAsync(new AuthenticationRequestParameters()
            {
                IsClientCredentialRequest = true,
                RequestContext = new RequestContext(new MsalLogger(Guid.Empty, null)),
                Authority = Authority.CreateAuthority(TestConstants.AuthorityTestTenant, false),
                ClientId = TestConstants.ClientId,
                ClientCredential = TestConstants.CredentialWithSecret,
                Scope = TestConstants.Scope
            }).Result;

            Assert.IsNotNull(cacheItem);
            Assert.AreEqual(atItem.GetKey().ToString(), cacheItem.GetKey().ToString());
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void GetAccessTokenNoUserAssertionInCacheTest()
        {
            TokenCache cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };
        
            var atItem = new MsalAccessTokenCacheItem(
                TestConstants.ProductionPrefNetworkEnvironment,
                TestConstants.ClientId,
                "Bearer",
                TestConstants.Scope.AsSingleString(),
                TestConstants.Utid,
                null,
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromHours(1)),
                MockHelpers.CreateClientInfo());

            // create key out of access token cache item and then
            // set it as the value of the access token.
            string atKey = atItem.GetKey().ToString();
            atItem.Secret = atKey;

            cache.tokenCacheAccessor.AccessTokenCacheDictionary[atKey] = JsonHelper.SerializeToJson(atItem);
            var param = new AuthenticationRequestParameters()
            {
                RequestContext = new RequestContext(new MsalLogger(Guid.Empty, null)),
                ClientId = TestConstants.ClientId,
                Authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false),
                Scope = TestConstants.Scope,
                UserAssertion = new UserAssertion(CoreCryptographyHelpers.CreateBase64UrlEncodedSha256Hash(atKey.ToString()))
            };

            MsalAccessTokenCacheItem item = cache.FindAccessTokenAsync(param).Result;

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

            var atItem = new MsalAccessTokenCacheItem(
                TestConstants.ProductionPrefNetworkEnvironment,
                TestConstants.ClientId,
                "Bearer",
                TestConstants.Scope.AsSingleString(),
                TestConstants.Utid,
                null,
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromHours(1)),
                MockHelpers.CreateClientInfo());

            // create key out of access token cache item and then
            // set it as the value of the access token.
            string atKey = atItem.GetKey().ToString();
            atItem.Secret = atKey;

            atItem.UserAssertionHash = CoreCryptographyHelpers.CreateBase64UrlEncodedSha256Hash(atKey);

            cache.tokenCacheAccessor.AccessTokenCacheDictionary[atKey.ToString()] = JsonHelper.SerializeToJson(atItem);
            var param = new AuthenticationRequestParameters()
            {
                RequestContext = new RequestContext(new MsalLogger(Guid.Empty, null)),
                ClientId = TestConstants.ClientId,
                Authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false),
                Scope = TestConstants.Scope,
                UserAssertion = new UserAssertion(atItem.UserAssertionHash + "-random")
            };

            MsalAccessTokenCacheItem item = cache.FindAccessTokenAsync(param).Result;

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

            var atItem = new MsalAccessTokenCacheItem(
                TestConstants.ProductionPrefNetworkEnvironment,
                TestConstants.ClientId,
                "Bearer",
                TestConstants.Scope.AsSingleString(),
                TestConstants.Utid,
                null,
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromHours(1)),
                MockHelpers.CreateClientInfo());

            // create key out of access token cache item and then
            // set it as the value of the access token.
            string atKey = atItem.GetKey().ToString();
            atItem.Secret = atKey;
            atItem.UserAssertionHash = CoreCryptographyHelpers.CreateBase64UrlEncodedSha256Hash(atKey);

            cache.tokenCacheAccessor.AccessTokenCacheDictionary[atKey.ToString()] = JsonHelper.SerializeToJson(atItem);
            var param = new AuthenticationRequestParameters()
            {
                RequestContext = new RequestContext(new MsalLogger(Guid.Empty, null)),
                ClientId = TestConstants.ClientId,
                Authority = Authority.CreateAuthority(TestConstants.AuthorityTestTenant, false),
                Scope = TestConstants.Scope,
                UserAssertion = new UserAssertion(atKey.ToString())
            };

            cache.AfterAccess = AfterAccessNoChangeNotification;
            MsalAccessTokenCacheItem item = cache.FindAccessTokenAsync(param).Result;

            Assert.IsNotNull(item);
            Assert.AreEqual(atKey.ToString(), item.Secret);
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void SaveAccessAndRefreshTokenWithEmptyCacheTest()
        {
            TokenCache cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };

            MsalTokenResponse response = new MsalTokenResponse();
            response.IdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId);
            response.AccessToken = "access-token";
            response.ClientInfo = MockHelpers.CreateClientInfo();
            response.ExpiresIn = 3599;
            response.CorrelationId = "correlation-id";
            response.RefreshToken = "refresh-token";
            response.Scope = TestConstants.Scope.AsSingleString();
            response.TokenType = "Bearer";
            AuthenticationRequestParameters requestParams = new AuthenticationRequestParameters()
            {
                RequestContext = new RequestContext(new MsalLogger(Guid.Empty, null)),
                Authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false),
                ClientId = TestConstants.ClientId,
                TenantUpdatedCanonicalAuthority = TestConstants.AuthorityTestTenant
            };

            AddHostToInstanceCache(TestConstants.ProductionPrefNetworkEnvironment);

            cache.SaveAccessAndRefreshToken(requestParams, response);

            Assert.AreEqual(1, cache.tokenCacheAccessor.RefreshTokenCacheDictionary.Count);
            Assert.AreEqual(1, cache.tokenCacheAccessor.AccessTokenCacheDictionary.Count);
        }


        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void SaveAccessAndRefreshTokenWithMoreScopesTest()
        {
            TokenCache cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };

            MsalTokenResponse response = new MsalTokenResponse();
            response.IdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId);
            response.ClientInfo = MockHelpers.CreateClientInfo();
            response.AccessToken = "access-token";
            response.ExpiresIn = 3599;
            response.CorrelationId = "correlation-id";
            response.RefreshToken = "refresh-token";
            response.Scope = TestConstants.Scope.AsSingleString();
            response.TokenType = "Bearer";

            RequestContext requestContext = new RequestContext(new MsalLogger(Guid.NewGuid(), null));
            AuthenticationRequestParameters requestParams = new AuthenticationRequestParameters()
            {
                RequestContext = requestContext,
                Authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false),
                ClientId = TestConstants.ClientId,
                TenantUpdatedCanonicalAuthority = TestConstants.AuthorityTestTenant
            };

            AddHostToInstanceCache(TestConstants.ProductionPrefNetworkEnvironment);

            cache.SaveAccessAndRefreshToken(requestParams, response);

            Assert.AreEqual(1, cache.tokenCacheAccessor.RefreshTokenCacheDictionary.Count);
            Assert.AreEqual(1, cache.tokenCacheAccessor.AccessTokenCacheDictionary.Count);

            response = new MsalTokenResponse();
            response.IdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId);
            response.ClientInfo = MockHelpers.CreateClientInfo();
            response.AccessToken = "access-token-2";
            response.ExpiresIn = 3599;
            response.CorrelationId = "correlation-id";
            response.RefreshToken = "refresh-token-2";
            response.Scope = TestConstants.Scope.AsSingleString() + " another-scope";
            response.TokenType = "Bearer";

            cache.SaveAccessAndRefreshToken(requestParams, response);

            Assert.AreEqual(1, cache.tokenCacheAccessor.RefreshTokenCacheDictionary.Count);
            Assert.AreEqual(1, cache.tokenCacheAccessor.AccessTokenCacheDictionary.Count);

            Assert.AreEqual("refresh-token-2", cache.GetAllRefreshTokensForClient(requestContext).First().Secret);
            Assert.AreEqual("access-token-2", cache.GetAllAccessTokensForClient(requestContext).First().Secret);
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void SaveAccessAndRefreshTokenWithLessScopesTest()
        {
            TokenCache cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };

            MsalTokenResponse response = new MsalTokenResponse();
            response.IdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId);
            response.ClientInfo = MockHelpers.CreateClientInfo();
            response.AccessToken = "access-token";
            response.ExpiresIn = 3599;
            response.CorrelationId = "correlation-id";
            response.RefreshToken = "refresh-token";
            response.Scope = TestConstants.Scope.AsSingleString();
            response.TokenType = "Bearer";

            RequestContext requestContext = new RequestContext(new MsalLogger(Guid.NewGuid(), null));
            AuthenticationRequestParameters requestParams = new AuthenticationRequestParameters()
            {
                RequestContext = requestContext,
                Authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false),
                ClientId = TestConstants.ClientId,
                TenantUpdatedCanonicalAuthority = TestConstants.AuthorityTestTenant
            };

            AddHostToInstanceCache(TestConstants.ProductionPrefNetworkEnvironment);

            cache.SaveAccessAndRefreshToken(requestParams, response);

            response = new MsalTokenResponse();
            response.IdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId);
            response.ClientInfo = MockHelpers.CreateClientInfo();
            response.AccessToken = "access-token-2";
            response.ExpiresIn = 3599;
            response.CorrelationId = "correlation-id";
            response.RefreshToken = "refresh-token-2";
            response.Scope = TestConstants.Scope.First();
            response.TokenType = "Bearer";

            cache.SaveAccessAndRefreshToken(requestParams, response);

            Assert.AreEqual(1, cache.tokenCacheAccessor.RefreshTokenCacheDictionary.Count);
            Assert.AreEqual(1, cache.tokenCacheAccessor.AccessTokenCacheDictionary.Count);
            Assert.AreEqual("refresh-token-2", cache.GetAllRefreshTokensForClient(requestContext).First().Secret);
            Assert.AreEqual("access-token-2", cache.GetAllAccessTokensForClient(requestContext).First().Secret);
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void SaveAccessAndRefreshTokenWithIntersectingScopesTest()
        {
            TokenCache cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };

            MsalTokenResponse response = new MsalTokenResponse
            {
                IdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId),
                AccessToken = "access-token",
                ClientInfo = MockHelpers.CreateClientInfo(),
                ExpiresIn = 3599,
                CorrelationId = "correlation-id",
                RefreshToken = "refresh-token",
                Scope = TestConstants.Scope.AsSingleString(),
                TokenType = "Bearer"
            };

            RequestContext requestContext = new RequestContext(new MsalLogger(Guid.NewGuid(), null));
            AuthenticationRequestParameters requestParams = new AuthenticationRequestParameters()
            {
                RequestContext = requestContext,
                Authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false),
                ClientId = TestConstants.ClientId,
                TenantUpdatedCanonicalAuthority = TestConstants.AuthorityTestTenant
            };

            AddHostToInstanceCache(TestConstants.ProductionPrefNetworkEnvironment);

            cache.SaveAccessAndRefreshToken(requestParams, response);

            response = new MsalTokenResponse
            {
                IdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId),
                ClientInfo = MockHelpers.CreateClientInfo(),
                AccessToken = "access-token-2",
                ExpiresIn = 3599,
                CorrelationId = "correlation-id",
                RefreshToken = "refresh-token-2",
                Scope = TestConstants.Scope.AsSingleString() + " random-scope",
                TokenType = "Bearer"
            };

            cache.SaveAccessAndRefreshToken(requestParams, response);

            Assert.AreEqual(1, cache.tokenCacheAccessor.RefreshTokenCacheDictionary.Count);
            Assert.AreEqual(1, cache.tokenCacheAccessor.AccessTokenCacheDictionary.Count);

            Assert.AreEqual("refresh-token-2", cache.GetAllRefreshTokensForClient(requestContext).First().Secret);
            Assert.AreEqual("access-token-2", cache.GetAllAccessTokensForClient(requestContext).First().Secret);
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void SaveAccessAndRefreshTokenWithDifferentAuthoritySameUserTest()
        {
            TokenCache cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };

            MsalTokenResponse response = new MsalTokenResponse();
            response.IdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId);
            response.ClientInfo = MockHelpers.CreateClientInfo();
            response.AccessToken = "access-token";
            response.ExpiresIn = 3599;
            response.CorrelationId = "correlation-id";
            response.RefreshToken = "refresh-token";
            response.Scope = TestConstants.Scope.AsSingleString();
            response.TokenType = "Bearer";

            RequestContext requestContext = new RequestContext(new MsalLogger(Guid.NewGuid(), null));
            AuthenticationRequestParameters requestParams = new AuthenticationRequestParameters()
            {
                RequestContext = requestContext,
                Authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false),
                ClientId = TestConstants.ClientId,
                TenantUpdatedCanonicalAuthority = TestConstants.AuthorityHomeTenant
            };

            AddHostToInstanceCache(TestConstants.ProductionPrefNetworkEnvironment);

            cache.SaveAccessAndRefreshToken(requestParams, response);

            response = new MsalTokenResponse();
            response.IdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId);
            response.ClientInfo = MockHelpers.CreateClientInfo();
            response.AccessToken = "access-token-2";
            response.ExpiresIn = 3599;
            response.CorrelationId = "correlation-id";
            response.RefreshToken = "refresh-token-2";
            response.Scope = TestConstants.Scope.AsSingleString() + " another-scope";
            response.TokenType = "Bearer";


            requestContext = new RequestContext(new MsalLogger(Guid.NewGuid(), null));
            requestParams = new AuthenticationRequestParameters()
            {
                RequestContext = requestContext,
                Authority = Authority.CreateAuthority(TestConstants.AuthorityGuestTenant, false),
                ClientId = TestConstants.ClientId,
                TenantUpdatedCanonicalAuthority = TestConstants.AuthorityGuestTenant
            };

            cache.SetAfterAccess(AfterAccessChangedNotification);
            cache.SaveAccessAndRefreshToken(requestParams, response);
            Assert.IsFalse(cache.HasStateChanged);

            Assert.AreEqual(1, cache.tokenCacheAccessor.RefreshTokenCacheDictionary.Count);
            Assert.AreEqual(2, cache.tokenCacheAccessor.AccessTokenCacheDictionary.Count);

            Assert.AreEqual("refresh-token-2", cache.GetAllRefreshTokensForClient(requestContext).First().Secret);
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

            MsalTokenResponse response = new MsalTokenResponse();
            response.IdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId);
            response.ClientInfo = MockHelpers.CreateClientInfo();
            response.AccessToken = "access-token";
            response.ExpiresIn = 3599;
            response.CorrelationId = "correlation-id";
            response.RefreshToken = "refresh-token";
            response.Scope = TestConstants.Scope.AsSingleString();
            response.TokenType = "Bearer";

            RequestContext requestContext = new RequestContext(new MsalLogger(Guid.NewGuid(), null));
            AuthenticationRequestParameters requestParams = new AuthenticationRequestParameters()
            {
                RequestContext = requestContext,
                Authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false),
                ClientId = TestConstants.ClientId,
                TenantUpdatedCanonicalAuthority = TestConstants.AuthorityTestTenant
            };

            AddHostToInstanceCache(TestConstants.ProductionPrefNetworkEnvironment);

            cache.SaveAccessAndRefreshToken(requestParams, response);
            byte[] serializedCache = cache.Serialize();
            cache.tokenCacheAccessor.AccessTokenCacheDictionary.Clear();
            cache.tokenCacheAccessor.RefreshTokenCacheDictionary.Clear();

            Assert.AreEqual(0, cache.tokenCacheAccessor.RefreshTokenCacheDictionary.Count);
            Assert.AreEqual(0, cache.tokenCacheAccessor.AccessTokenCacheDictionary.Count);

            cache.Deserialize(serializedCache);

            Assert.AreEqual(1, cache.tokenCacheAccessor.RefreshTokenCacheDictionary.Count);
            Assert.AreEqual(1, cache.tokenCacheAccessor.AccessTokenCacheDictionary.Count);

            serializedCache = cache.Serialize();
            cache.Deserialize(serializedCache);
            //item count should not change because old cache entries should have
            //been overriden

            Assert.AreEqual(1, cache.tokenCacheAccessor.RefreshTokenCacheDictionary.Count);
            Assert.AreEqual(1, cache.tokenCacheAccessor.AccessTokenCacheDictionary.Count);

            MsalAccessTokenCacheItem atItem = cache.GetAllAccessTokensForClient(requestContext).First();
            Assert.AreEqual(response.AccessToken, atItem.Secret);
            Assert.AreEqual(TestConstants.AuthorityTestTenant, atItem.Authority);
            Assert.AreEqual(TestConstants.ClientId, atItem.ClientId);
            Assert.AreEqual(response.TokenType, atItem.TokenType);
            Assert.AreEqual(response.Scope, atItem.ScopeSet.AsSingleString());

            // todo add test for idToken serialization
            //Assert.AreEqual(response.IdToken, atItem.RawIdToken);

            MsalRefreshTokenCacheItem rtItem = cache.GetAllRefreshTokensForClient(requestContext).First();
            Assert.AreEqual(response.RefreshToken, rtItem.Secret);
            Assert.AreEqual(TestConstants.ClientId, rtItem.ClientId);
            Assert.AreEqual(TestConstants.UserIdentifier.Identifier, rtItem.HomeAccountId);
            Assert.AreEqual(TestConstants.ProductionPrefNetworkEnvironment, rtItem.Environment);
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void FindAccessToken_ScopeCaseInsensitive()
        {
            var tokenCache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };

            TokenCacheHelper.PopulateCache(tokenCache.tokenCacheAccessor);

            var param = new AuthenticationRequestParameters()
            {
                RequestContext = new RequestContext(new MsalLogger(Guid.Empty, null)),
                ClientId = TestConstants.ClientId,
                Authority = Authority.CreateAuthority(TestConstants.AuthorityTestTenant, false),
                Scope = new SortedSet<string>(),
                Account = TestConstants.User
            };

            var scopeInCache = TestConstants.Scope.FirstOrDefault();

            var upperCaseScope = scopeInCache.ToUpper();
            param.Scope.Add(upperCaseScope);

            var item = tokenCache.FindAccessTokenAsync(param).Result;

            Assert.IsNotNull(item);
            Assert.IsTrue(item.ScopeSet.Contains(scopeInCache));
        }

        /*
        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void DeserializeCacheItemWithNoVersion()
        {
            string noVersionCacheEntry = "{\"client_id\":\"client_id\",\"client_info\":\"eyJ1aWQiOiJteS1VSUQiLCJ1dGlkIjoibXktVVRJRCJ9\",\"access_token\":\"access-token\",\"authority\":\"https:\\\\/\\\\/login.microsoftonline.com\\\\/home\\\\/\",\"expires_on\":1494025355,\"id_token\":\"someheader.eyJhdWQiOiAiZTg1NGE0YTctNmMzNC00NDljLWIyMzctZmM3YTI4MDkzZDg0IiwiaXNzIjogImh0dHBzOi8vbG9naW4ubWljcm9zb2Z0b25saW5lLmNvbS82YzNkNTFkZC1mMGU1LTQ5NTktYjRlYS1hODBjNGUzNmZlNWUvdjIuMC8iLCJpYXQiOiAxNDU1ODMzODI4LCJuYmYiOiAxNDU1ODMzODI4LCJleHAiOiAxNDU1ODM3NzI4LCJpcGFkZHIiOiAiMTMxLjEwNy4xNTkuMTE3IiwibmFtZSI6ICJNYXJycnJyaW8gQm9zc3kiLCJvaWQiOiAidW5pcXVlX2lkIiwicHJlZmVycmVkX3VzZXJuYW1lIjogImRpc3BsYXlhYmxlQGlkLmNvbSIsInN1YiI6ICJLNF9TR0d4S3FXMVN4VUFtaGc2QzFGNlZQaUZ6Y3gtUWQ4MGVoSUVkRnVzIiwidGlkIjogIm15LWlkcCIsInZlciI6ICIyLjAifQ.somesignature\",\"scope\":\"r1\\\\/scope1 r1\\\\/scope2\",\"token_type\":\"Bearer\",\"user_assertion_hash\":null}";

            TokenCache cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };
            cache.AddAccessTokenCacheItem(JsonHelper.DeserializeFromJson<MsalAccessTokenCacheItem>(noVersionCacheEntry));
            ICollection<MsalAccessTokenCacheItem> items = cache.GetAllAccessTokensForClient(new RequestContext(new MsalLogger(Guid.NewGuid(), null)));
            Assert.AreEqual(1, items.Count);
            MsalAccessTokenCacheItem item = items.First();
            Assert.AreEqual(0, item.Version);
        }
        
        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void DeserializeCacheItemWithDifferentVersion()
        {
            string differentVersionEntry = "{\"client_id\":\"client_id\",\"client_info\":\"eyJ1aWQiOiJteS1VSUQiLCJ1dGlkIjoibXktVVRJRCJ9\",\"ver\":5,\"access_token\":\"access-token\",\"authority\":\"https:\\\\/\\\\/login.microsoftonline.com\\\\/home\\\\/\",\"expires_on\":1494025355,\"id_token\":\"someheader.eyJhdWQiOiAiZTg1NGE0YTctNmMzNC00NDljLWIyMzctZmM3YTI4MDkzZDg0IiwiaXNzIjogImh0dHBzOi8vbG9naW4ubWljcm9zb2Z0b25saW5lLmNvbS82YzNkNTFkZC1mMGU1LTQ5NTktYjRlYS1hODBjNGUzNmZlNWUvdjIuMC8iLCJpYXQiOiAxNDU1ODMzODI4LCJuYmYiOiAxNDU1ODMzODI4LCJleHAiOiAxNDU1ODM3NzI4LCJpcGFkZHIiOiAiMTMxLjEwNy4xNTkuMTE3IiwibmFtZSI6ICJNYXJycnJyaW8gQm9zc3kiLCJvaWQiOiAidW5pcXVlX2lkIiwicHJlZmVycmVkX3VzZXJuYW1lIjogImRpc3BsYXlhYmxlQGlkLmNvbSIsInN1YiI6ICJLNF9TR0d4S3FXMVN4VUFtaGc2QzFGNlZQaUZ6Y3gtUWQ4MGVoSUVkRnVzIiwidGlkIjogIm15LWlkcCIsInZlciI6ICIyLjAifQ.somesignature\",\"scope\":\"r1\\\\/scope1 r1\\\\/scope2\",\"token_type\":\"Bearer\",\"user_assertion_hash\":null}";
           
            TokenCache cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };
            cache.AddAccessTokenCacheItem(JsonHelper.DeserializeFromJson<MsalAccessTokenCacheItem>(differentVersionEntry));
            ICollection<MsalAccessTokenCacheItem> items = cache.GetAllAccessTokensForClient(new RequestContext(new MsalLogger(Guid.NewGuid(), null)));
            Assert.AreEqual(1, items.Count);
            MsalAccessTokenCacheItem item = items.First();
            Assert.AreEqual(5, item.Version);
        }
        */
    }
}
