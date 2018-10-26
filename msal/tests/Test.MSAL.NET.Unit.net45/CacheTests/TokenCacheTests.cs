// ------------------------------------------------------------------------------
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
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Cache;
using Microsoft.Identity.Core.Helpers;
using Microsoft.Identity.Core.Instance;
using Microsoft.Identity.Core.OAuth2;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.Microsoft.Identity.Core.Unit;
using Test.Microsoft.Identity.Core.Unit.Mocks;

#if !NET_CORE //TODO: remove the !IF once the bug around token cache is fixed
// https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/656
namespace Test.MSAL.NET.Unit.CacheTests
{
    [TestClass]
    public class TokenCacheTests
    {
        public static long ValidExpiresIn = 3600;

        // Passing a seed to make repro possible
        private static readonly Random Rand = new Random(42);

        // TODO: rename this to _cache, since other tests ALSO use the local variable cache which is confusing.
        private TokenCache _cache;

        [TestInitialize]
        public void TestInitialize()
        {
            _cache = new TokenCache();
            new TestLogger(Guid.Empty);

            AadInstanceDiscovery.Instance.Cache.Clear();
        }

        private void AddHostToInstanceCache(string host)
        {
            AadInstanceDiscovery.Instance.Cache.TryAdd(
                host,
                new InstanceDiscoveryMetadataEntry
                {
                    PreferredNetwork = host,
                    PreferredCache = host,
                    Aliases = new string[]
                    {
                        host
                    }
                });
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _cache.Clear();
        }

      
        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void GetExactScopesMatchedAccessTokenTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var cache = new TokenCache()
                {
                    ClientId = MsalTestConstants.ClientId,
                    HttpManager = httpManager
                };
                var atItem = new MsalAccessTokenCacheItem(
                    MsalTestConstants.ProductionPrefNetworkEnvironment,
                    MsalTestConstants.ClientId,
                    "Bearer",
                    MsalTestConstants.Scope.AsSingleString(),
                    MsalTestConstants.Utid,
                    "",
                    new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)),
                    MockHelpers.CreateClientInfo());

                // create key out of access token cache item and then
                // set it as the value of the access token.
                string atKey = atItem.GetKey().ToString();
                atItem.Secret = atKey;

                cache.tokenCacheAccessor.SaveAccessToken(atItem);
                var item = cache.FindAccessTokenAsync(
                    new AuthenticationRequestParameters()
                    {
                        RequestContext = new RequestContext(new MsalLogger(Guid.Empty, null)),
                        ClientId = MsalTestConstants.ClientId,
                        Authority = Authority.CreateAuthority(MsalTestConstants.AuthorityTestTenant, false),
                        Scope = MsalTestConstants.Scope,
                        Account = MsalTestConstants.User
                    }).Result;

                Assert.IsNotNull(item);
                Assert.AreEqual(atKey.ToString(), item.Secret);
            }
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void GetSubsetScopesMatchedAccessTokenTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var cache = new TokenCache()
                {
                    ClientId = MsalTestConstants.ClientId,
                    HttpManager = httpManager
                };

                var atItem = new MsalAccessTokenCacheItem(
                    MsalTestConstants.ProductionPrefNetworkEnvironment,
                    MsalTestConstants.ClientId,
                    "Bearer",
                    MsalTestConstants.Scope.AsSingleString(),
                    MsalTestConstants.Utid,
                    null,
                    new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromHours(1)),
                    MockHelpers.CreateClientInfo());

                // create key out of access token cache item and then
                // set it as the value of the access token.
                string atKey = atItem.GetKey().ToString();
                atItem.Secret = atKey;

                cache.tokenCacheAccessor.SaveAccessToken(atItem);
                var param = new AuthenticationRequestParameters()
                {
                    RequestContext = new RequestContext(new MsalLogger(Guid.Empty, null)),
                    ClientId = MsalTestConstants.ClientId,
                    Authority = Authority.CreateAuthority(MsalTestConstants.AuthorityTestTenant, false),
                    Scope = new SortedSet<string>(),
                    Account = MsalTestConstants.User
                };

                param.Scope.Add("r1/scope1");
                var item = cache.FindAccessTokenAsync(param).Result;

                Assert.IsNotNull(item);
                Assert.AreEqual(atKey.ToString(), item.Secret);
            }
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void GetIntersectedScopesMatchedAccessTokenTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var cache = new TokenCache()
                {
                    ClientId = MsalTestConstants.ClientId,
                    HttpManager = httpManager
                };

                var atItem = new MsalAccessTokenCacheItem(
                    MsalTestConstants.ProductionPrefNetworkEnvironment,
                    MsalTestConstants.ClientId,
                    "Bearer",
                    MsalTestConstants.Scope.AsSingleString(),
                    MsalTestConstants.Utid,
                    null,
                    new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromHours(1)),
                    MockHelpers.CreateClientInfo());

                // create key out of access token cache item and then
                // set it as the value of the access token.
                string atKey = atItem.GetKey().ToString();
                atItem.Secret = atKey;

                cache.tokenCacheAccessor.SaveAccessToken(atItem);

                var param = new AuthenticationRequestParameters()
                {
                    RequestContext = new RequestContext(new MsalLogger(Guid.Empty, null)),
                    ClientId = MsalTestConstants.ClientId,
                    Authority = Authority.CreateAuthority(MsalTestConstants.AuthorityHomeTenant, false),
                    Scope = new SortedSet<string>(),
                    Account = new Account(MsalTestConstants.UserIdentifier, MsalTestConstants.DisplayableId, null)
                };

                param.Scope.Add(MsalTestConstants.Scope.First());
                param.Scope.Add("non-existent-scopes");
                var item = cache.FindAccessTokenAsync(param).Result;

                //intersected scopes are not returned.
                Assert.IsNull(item);
            }
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void GetExpiredAccessTokenTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                _cache = new TokenCache()
                {
                    ClientId = MsalTestConstants.ClientId,
                    HttpManager = httpManager
                };

                var atItem = new MsalAccessTokenCacheItem(
                    MsalTestConstants.ProductionPrefNetworkEnvironment,
                    MsalTestConstants.ClientId,
                    "Bearer",
                    MsalTestConstants.Scope.AsSingleString(),
                    MsalTestConstants.Utid,
                    null,
                    new DateTimeOffset(DateTime.UtcNow),
                    MockHelpers.CreateClientInfo());

                atItem.Secret = atItem.GetKey().ToString();
                _cache.tokenCacheAccessor.SaveAccessToken(atItem);

                Assert.IsNull(
                    _cache.FindAccessTokenAsync(
                        new AuthenticationRequestParameters()
                        {
                            RequestContext = new RequestContext(new MsalLogger(Guid.Empty, null)),
                            ClientId = MsalTestConstants.ClientId,
                            Authority = Authority.CreateAuthority(MsalTestConstants.AuthorityTestTenant, false),
                            Scope = MsalTestConstants.Scope,
                            Account = new Account(MsalTestConstants.UserIdentifier, MsalTestConstants.DisplayableId, null)
                        }).Result);
            }
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void GetAccessTokenExpiryInRangeTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                _cache = new TokenCache()
                {
                    ClientId = MsalTestConstants.ClientId,
                    HttpManager = httpManager
                };

                var atItem = new MsalAccessTokenCacheItem(
                    MsalTestConstants.ProductionPrefNetworkEnvironment,
                    MsalTestConstants.ClientId,
                    "Bearer",
                    MsalTestConstants.Scope.AsSingleString(),
                    MsalTestConstants.Utid,
                    "",
                    new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromMinutes(4)),
                    MockHelpers.CreateClientInfo());

                atItem.Secret = atItem.GetKey().ToString();
                _cache.tokenCacheAccessor.SaveAccessToken(atItem);

                Assert.IsNull(
                    _cache.FindAccessTokenAsync(
                        new AuthenticationRequestParameters()
                        {
                            RequestContext = new RequestContext(new MsalLogger(Guid.Empty, null)),
                            ClientId = MsalTestConstants.ClientId,
                            Authority = Authority.CreateAuthority(MsalTestConstants.AuthorityTestTenant, false),
                            Scope = MsalTestConstants.Scope,
                            Account = new Account(MsalTestConstants.UserIdentifier, MsalTestConstants.DisplayableId, null)
                        }).Result);
            }
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void GetRefreshTokenTest()
        {
            var cache = new TokenCache()
            {
                ClientId = MsalTestConstants.ClientId
            };

            var rtItem = new MsalRefreshTokenCacheItem(
                MsalTestConstants.ProductionPrefNetworkEnvironment,
                MsalTestConstants.ClientId,
                "someRT",
                MockHelpers.CreateClientInfo());

            string rtKey = rtItem.GetKey().ToString();
            cache.tokenCacheAccessor.SaveRefreshToken(rtItem);
            var authParams = new AuthenticationRequestParameters()
            {
                RequestContext = new RequestContext(new MsalLogger(Guid.Empty, null)),
                ClientId = MsalTestConstants.ClientId,
                Authority = Authority.CreateAuthority(MsalTestConstants.AuthorityHomeTenant, false),
                Scope = MsalTestConstants.Scope,
                Account = MsalTestConstants.User
            };
            Assert.IsNotNull(cache.FindRefreshTokenAsync(authParams));

            // RT is stored by environment, client id and userIdentifier as index.
            // any change to authority (within same environment), uniqueid and displyableid will not 
            // change the outcome of cache look up.
            Assert.IsNotNull(
                cache.FindRefreshTokenAsync(
                    new AuthenticationRequestParameters()
                    {
                        RequestContext = new RequestContext(new MsalLogger(Guid.Empty, null)),
                        ClientId = MsalTestConstants.ClientId,
                        Authority = Authority.CreateAuthority(MsalTestConstants.AuthorityHomeTenant + "more", false),
                        Scope = MsalTestConstants.Scope,
                        Account = MsalTestConstants.User
                    }));
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void GetRefreshTokenDifferentEnvironmentTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var cache = new TokenCache()
                {
                    ClientId = MsalTestConstants.ClientId,
                    HttpManager = httpManager
                };
                var rtItem = new MsalRefreshTokenCacheItem(
                    MsalTestConstants.SovereignEnvironment,
                    MsalTestConstants.ClientId,
                    "someRT",
                    MockHelpers.CreateClientInfo());

                string rtKey = rtItem.GetKey().ToString();
                cache.tokenCacheAccessor.SaveRefreshToken(rtItem);
                var authParams = new AuthenticationRequestParameters()
                {
                    RequestContext = new RequestContext(new MsalLogger(Guid.Empty, null)),
                    ClientId = MsalTestConstants.ClientId,
                    Authority = Authority.CreateAuthority(MsalTestConstants.AuthorityHomeTenant, false),
                    Scope = MsalTestConstants.Scope,
                    Account = MsalTestConstants.User
                };
                var rt = cache.FindRefreshTokenAsync(authParams).Result;
                Assert.IsNull(rt);
            }
        }

#if !WINDOWS_APP && !ANDROID && !iOS // Confidential Client N/A
        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void GetAppTokenFromCacheTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                _cache = new TokenCache()
                {
                    ClientId = MsalTestConstants.ClientId,
                    HttpManager = httpManager
                };

                var atItem = new MsalAccessTokenCacheItem(
                    MsalTestConstants.ProductionPrefNetworkEnvironment,
                    MsalTestConstants.ClientId,
                    "Bearer",
                    MsalTestConstants.Scope.AsSingleString(),
                    MsalTestConstants.Utid,
                    null,
                    new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)),
                    MockHelpers.CreateClientInfo());

                string atKey = atItem.GetKey().ToString();
                atItem.Secret = atKey;

                _cache.tokenCacheAccessor.SaveAccessToken(atItem);

                var cacheItem = _cache.FindAccessTokenAsync(
                    new AuthenticationRequestParameters()
                    {
                        IsClientCredentialRequest = true,
                        RequestContext = new RequestContext(new MsalLogger(Guid.Empty, null)),
                        Authority = Authority.CreateAuthority(MsalTestConstants.AuthorityTestTenant, false),
                        ClientId = MsalTestConstants.ClientId,
                        ClientCredential = MsalTestConstants.CredentialWithSecret,
                        Scope = MsalTestConstants.Scope
                    }).Result;

                Assert.IsNotNull(cacheItem);
                Assert.AreEqual(atItem.GetKey().ToString(), cacheItem.GetKey().ToString());
            }
        }
#endif

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void DoNotSaveRefreshTokenInAdalCacheForMsalB2CAuthorityTest()
        {
            var cache = new TokenCache()
            {
                ClientId = MsalTestConstants.ClientId
            };

            var response = new MsalTokenResponse
            {
                IdToken = MockHelpers.CreateIdToken(MsalTestConstants.UniqueId, MsalTestConstants.DisplayableId),
                AccessToken = "access-token",
                ClientInfo = MockHelpers.CreateClientInfo(),
                ExpiresIn = 3599,
                CorrelationId = "correlation-id",
                RefreshToken = "refresh-token",
                Scope = MsalTestConstants.Scope.AsSingleString(),
                TokenType = "Bearer"
            };
            var requestParams = new AuthenticationRequestParameters()
            {
                RequestContext = new RequestContext(new MsalLogger(Guid.Empty, null)),
                Authority = Authority.CreateAuthority(MsalTestConstants.B2CAuthority, false),
                ClientId = MsalTestConstants.ClientId,
                TenantUpdatedCanonicalAuthority = MsalTestConstants.AuthorityTestTenant
            };

            AddHostToInstanceCache(MsalTestConstants.ProductionPrefNetworkEnvironment);

            cache.SaveAccessAndRefreshToken(requestParams, response);

            Assert.AreEqual(1, cache.tokenCacheAccessor.RefreshTokenCount);
            Assert.AreEqual(1, cache.tokenCacheAccessor.AccessTokenCount);

            IDictionary<AdalTokenCacheKey, AdalResultWrapper> dictionary =
                AdalCacheOperations.Deserialize(cache.legacyCachePersistence.LoadCache());
            cache.legacyCachePersistence.WriteCache(AdalCacheOperations.Serialize(dictionary));

            // ADAL cache is empty because B2C scenario is only for MSAL
            Assert.AreEqual(0, dictionary.Count);
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void GetAccessTokenNoUserAssertionInCacheTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var cache = new TokenCache()
                {
                    ClientId = MsalTestConstants.ClientId,
                    HttpManager = httpManager
                };

                var atItem = new MsalAccessTokenCacheItem(
                    MsalTestConstants.ProductionPrefNetworkEnvironment,
                    MsalTestConstants.ClientId,
                    "Bearer",
                    MsalTestConstants.Scope.AsSingleString(),
                    MsalTestConstants.Utid,
                    null,
                    new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromHours(1)),
                    MockHelpers.CreateClientInfo());

                // create key out of access token cache item and then
                // set it as the value of the access token.
                string atKey = atItem.GetKey().ToString();
                atItem.Secret = atKey;

                cache.tokenCacheAccessor.SaveAccessToken(atItem);
                var param = new AuthenticationRequestParameters()
                {
                    RequestContext = new RequestContext(new MsalLogger(Guid.Empty, null)),
                    ClientId = MsalTestConstants.ClientId,
                    Authority = Authority.CreateAuthority(MsalTestConstants.AuthorityHomeTenant, false),
                    Scope = MsalTestConstants.Scope,
                    UserAssertion = new UserAssertion(PlatformProxyFactory.GetPlatformProxy().CryptographyManager.CreateBase64UrlEncodedSha256Hash(atKey.ToString()))
                };

                var item = cache.FindAccessTokenAsync(param).Result;

                //cache lookup should fail because there was no userassertion hash in the matched
                //token cache item.

                Assert.IsNull(item);
            }
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void GetAccessTokenUserAssertionMismatchInCacheTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var cache = new TokenCache()
                {
                    ClientId = MsalTestConstants.ClientId,
                    HttpManager = httpManager
                };

                var atItem = new MsalAccessTokenCacheItem(
                    MsalTestConstants.ProductionPrefNetworkEnvironment,
                    MsalTestConstants.ClientId,
                    "Bearer",
                    MsalTestConstants.Scope.AsSingleString(),
                    MsalTestConstants.Utid,
                    null,
                    new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromHours(1)),
                    MockHelpers.CreateClientInfo());

                // create key out of access token cache item and then
                // set it as the value of the access token.
                string atKey = atItem.GetKey().ToString();
                atItem.Secret = atKey;

                atItem.UserAssertionHash = PlatformProxyFactory.GetPlatformProxy().CryptographyManager.CreateBase64UrlEncodedSha256Hash(atKey);

                cache.tokenCacheAccessor.SaveAccessToken(atItem);
                var param = new AuthenticationRequestParameters()
                {
                    RequestContext = new RequestContext(new MsalLogger(Guid.Empty, null)),
                    ClientId = MsalTestConstants.ClientId,
                    Authority = Authority.CreateAuthority(MsalTestConstants.AuthorityHomeTenant, false),
                    Scope = MsalTestConstants.Scope,
                    UserAssertion = new UserAssertion(atItem.UserAssertionHash + "-random")
                };

                var item = cache.FindAccessTokenAsync(param).Result;

                // cache lookup should fail because there was userassertion hash did not match the one
                // stored in token cache item.
                Assert.IsNull(item);
            }
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void GetAccessTokenMatchedUserAssertionInCacheTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var cache = new TokenCache()
                {
                    ClientId = MsalTestConstants.ClientId,
                    HttpManager = httpManager
                };

                var atItem = new MsalAccessTokenCacheItem(
                    MsalTestConstants.ProductionPrefNetworkEnvironment,
                    MsalTestConstants.ClientId,
                    "Bearer",
                    MsalTestConstants.Scope.AsSingleString(),
                    MsalTestConstants.Utid,
                    null,
                    new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromHours(1)),
                    MockHelpers.CreateClientInfo());

                // create key out of access token cache item and then
                // set it as the value of the access token.
                string atKey = atItem.GetKey().ToString();
                atItem.Secret = atKey;
                atItem.UserAssertionHash = PlatformProxyFactory.GetPlatformProxy().CryptographyManager.CreateBase64UrlEncodedSha256Hash(atKey);

                cache.tokenCacheAccessor.SaveAccessToken(atItem);
                var param = new AuthenticationRequestParameters()
                {
                    RequestContext = new RequestContext(new MsalLogger(Guid.Empty, null)),
                    ClientId = MsalTestConstants.ClientId,
                    Authority = Authority.CreateAuthority(MsalTestConstants.AuthorityTestTenant, false),
                    Scope = MsalTestConstants.Scope,
                    UserAssertion = new UserAssertion(atKey.ToString())
                };

                cache.AfterAccess = AfterAccessNoChangeNotification;
                var item = cache.FindAccessTokenAsync(param).Result;

                Assert.IsNotNull(item);
                Assert.AreEqual(atKey.ToString(), item.Secret);
            }
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void SaveAccessAndRefreshTokenWithEmptyCacheTest()
        {
            var cache = new TokenCache()
            {
                ClientId = MsalTestConstants.ClientId,
            };

            var response = new MsalTokenResponse
            {
                IdToken = MockHelpers.CreateIdToken(MsalTestConstants.UniqueId, MsalTestConstants.DisplayableId),
                AccessToken = "access-token",
                ClientInfo = MockHelpers.CreateClientInfo(),
                ExpiresIn = 3599,
                CorrelationId = "correlation-id",
                RefreshToken = "refresh-token",
                Scope = MsalTestConstants.Scope.AsSingleString(),
                TokenType = "Bearer"
            };
            var requestParams = new AuthenticationRequestParameters()
            {
                RequestContext = new RequestContext(new MsalLogger(Guid.Empty, null)),
                Authority = Authority.CreateAuthority(MsalTestConstants.AuthorityHomeTenant, false),
                ClientId = MsalTestConstants.ClientId,
                TenantUpdatedCanonicalAuthority = MsalTestConstants.AuthorityTestTenant
            };

            AddHostToInstanceCache(MsalTestConstants.ProductionPrefNetworkEnvironment);

            cache.SaveAccessAndRefreshToken(requestParams, response);

            Assert.AreEqual(1, cache.tokenCacheAccessor.RefreshTokenCount);
            Assert.AreEqual(1, cache.tokenCacheAccessor.AccessTokenCount);
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void SaveAccessAndRefreshTokenWithMoreScopesTest()
        {
            var cache = new TokenCache()
            {
                ClientId = MsalTestConstants.ClientId
            };

            var response = new MsalTokenResponse
            {
                IdToken = MockHelpers.CreateIdToken(MsalTestConstants.UniqueId, MsalTestConstants.DisplayableId),
                ClientInfo = MockHelpers.CreateClientInfo(),
                AccessToken = "access-token",
                ExpiresIn = 3599,
                CorrelationId = "correlation-id",
                RefreshToken = "refresh-token",
                Scope = MsalTestConstants.Scope.AsSingleString(),
                TokenType = "Bearer"
            };

            var requestContext = new RequestContext(new MsalLogger(Guid.NewGuid(), null));
            var requestParams = new AuthenticationRequestParameters()
            {
                RequestContext = requestContext,
                Authority = Authority.CreateAuthority(MsalTestConstants.AuthorityHomeTenant, false),
                ClientId = MsalTestConstants.ClientId,
                TenantUpdatedCanonicalAuthority = MsalTestConstants.AuthorityTestTenant
            };

            AddHostToInstanceCache(MsalTestConstants.ProductionPrefNetworkEnvironment);

            cache.SaveAccessAndRefreshToken(requestParams, response);

            Assert.AreEqual(1, cache.tokenCacheAccessor.RefreshTokenCount);
            Assert.AreEqual(1, cache.tokenCacheAccessor.AccessTokenCount);

            response = new MsalTokenResponse();
            response.IdToken = MockHelpers.CreateIdToken(MsalTestConstants.UniqueId, MsalTestConstants.DisplayableId);
            response.ClientInfo = MockHelpers.CreateClientInfo();
            response.AccessToken = "access-token-2";
            response.ExpiresIn = 3599;
            response.CorrelationId = "correlation-id";
            response.RefreshToken = "refresh-token-2";
            response.Scope = MsalTestConstants.Scope.AsSingleString() + " another-scope";
            response.TokenType = "Bearer";

            cache.SaveAccessAndRefreshToken(requestParams, response);

            Assert.AreEqual(1, cache.tokenCacheAccessor.RefreshTokenCount);
            Assert.AreEqual(1, cache.tokenCacheAccessor.AccessTokenCount);

            Assert.AreEqual("refresh-token-2", cache.GetAllRefreshTokensForClient(requestContext).First().Secret);
            Assert.AreEqual("access-token-2", cache.GetAllAccessTokensForClient(requestContext).First().Secret);
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void SaveAccessAndRefreshTokenWithLessScopesTest()
        {
            var cache = new TokenCache()
            {
                ClientId = MsalTestConstants.ClientId
            };

            var response = new MsalTokenResponse
            {
                IdToken = MockHelpers.CreateIdToken(MsalTestConstants.UniqueId, MsalTestConstants.DisplayableId),
                ClientInfo = MockHelpers.CreateClientInfo(),
                AccessToken = "access-token",
                ExpiresIn = 3599,
                CorrelationId = "correlation-id",
                RefreshToken = "refresh-token",
                Scope = MsalTestConstants.Scope.AsSingleString(),
                TokenType = "Bearer"
            };

            var requestContext = new RequestContext(new MsalLogger(Guid.NewGuid(), null));
            var requestParams = new AuthenticationRequestParameters()
            {
                RequestContext = requestContext,
                Authority = Authority.CreateAuthority(MsalTestConstants.AuthorityHomeTenant, false),
                ClientId = MsalTestConstants.ClientId,
                TenantUpdatedCanonicalAuthority = MsalTestConstants.AuthorityTestTenant
            };

            AddHostToInstanceCache(MsalTestConstants.ProductionPrefNetworkEnvironment);

            cache.SaveAccessAndRefreshToken(requestParams, response);

            response = new MsalTokenResponse
            {
                IdToken = MockHelpers.CreateIdToken(MsalTestConstants.UniqueId, MsalTestConstants.DisplayableId),
                ClientInfo = MockHelpers.CreateClientInfo(),
                AccessToken = "access-token-2",
                ExpiresIn = 3599,
                CorrelationId = "correlation-id",
                RefreshToken = "refresh-token-2",
                Scope = MsalTestConstants.Scope.First(),
                TokenType = "Bearer"
            };

            cache.SaveAccessAndRefreshToken(requestParams, response);

            Assert.AreEqual(1, cache.tokenCacheAccessor.RefreshTokenCount);
            Assert.AreEqual(1, cache.tokenCacheAccessor.AccessTokenCount);
            Assert.AreEqual("refresh-token-2", cache.GetAllRefreshTokensForClient(requestContext).First().Secret);
            Assert.AreEqual("access-token-2", cache.GetAllAccessTokensForClient(requestContext).First().Secret);
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void SaveAccessAndRefreshTokenWithIntersectingScopesTest()
        {
            var cache = new TokenCache()
            {
                ClientId = MsalTestConstants.ClientId
            };

            var response = new MsalTokenResponse
            {
                IdToken = MockHelpers.CreateIdToken(MsalTestConstants.UniqueId, MsalTestConstants.DisplayableId),
                AccessToken = "access-token",
                ClientInfo = MockHelpers.CreateClientInfo(),
                ExpiresIn = 3599,
                CorrelationId = "correlation-id",
                RefreshToken = "refresh-token",
                Scope = MsalTestConstants.Scope.AsSingleString(),
                TokenType = "Bearer"
            };

            var requestContext = new RequestContext(new MsalLogger(Guid.NewGuid(), null));
            var requestParams = new AuthenticationRequestParameters()
            {
                RequestContext = requestContext,
                Authority = Authority.CreateAuthority(MsalTestConstants.AuthorityHomeTenant, false),
                ClientId = MsalTestConstants.ClientId,
                TenantUpdatedCanonicalAuthority = MsalTestConstants.AuthorityTestTenant
            };

            AddHostToInstanceCache(MsalTestConstants.ProductionPrefNetworkEnvironment);

            cache.SaveAccessAndRefreshToken(requestParams, response);

            response = new MsalTokenResponse
            {
                IdToken = MockHelpers.CreateIdToken(MsalTestConstants.UniqueId, MsalTestConstants.DisplayableId),
                ClientInfo = MockHelpers.CreateClientInfo(),
                AccessToken = "access-token-2",
                ExpiresIn = 3599,
                CorrelationId = "correlation-id",
                RefreshToken = "refresh-token-2",
                Scope = MsalTestConstants.Scope.AsSingleString() + " random-scope",
                TokenType = "Bearer"
            };

            cache.SaveAccessAndRefreshToken(requestParams, response);

            Assert.AreEqual(1, cache.tokenCacheAccessor.RefreshTokenCount);
            Assert.AreEqual(1, cache.tokenCacheAccessor.AccessTokenCount);

            Assert.AreEqual("refresh-token-2", cache.GetAllRefreshTokensForClient(requestContext).First().Secret);
            Assert.AreEqual("access-token-2", cache.GetAllAccessTokensForClient(requestContext).First().Secret);
        }

        private void AfterAccessChangedNotification(TokenCacheNotificationArgs args)
        {
            Assert.IsTrue(args.TokenCache.HasStateChanged);
        }

        private void AfterAccessNoChangeNotification(TokenCacheNotificationArgs args)
        {
            Assert.IsFalse(args.TokenCache.HasStateChanged);
        }


#if !WINDOWS_APP && !ANDROID && !iOS // Token Cache Serialization N/A

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void SaveAccessAndRefreshTokenWithDifferentAuthoritySameUserTest()
        {
            var cache = new TokenCache()
            {
                ClientId = MsalTestConstants.ClientId
            };

            var response = new MsalTokenResponse
            {
                IdToken = MockHelpers.CreateIdToken(MsalTestConstants.UniqueId, MsalTestConstants.DisplayableId),
                ClientInfo = MockHelpers.CreateClientInfo(),
                AccessToken = "access-token",
                ExpiresIn = 3599,
                CorrelationId = "correlation-id",
                RefreshToken = "refresh-token",
                Scope = MsalTestConstants.Scope.AsSingleString(),
                TokenType = "Bearer"
            };

            var requestContext = new RequestContext(new MsalLogger(Guid.NewGuid(), null));
            var requestParams = new AuthenticationRequestParameters()
            {
                RequestContext = requestContext,
                Authority = Authority.CreateAuthority(MsalTestConstants.AuthorityHomeTenant, false),
                ClientId = MsalTestConstants.ClientId,
                TenantUpdatedCanonicalAuthority = MsalTestConstants.AuthorityHomeTenant
            };

            AddHostToInstanceCache(MsalTestConstants.ProductionPrefNetworkEnvironment);

            cache.SaveAccessAndRefreshToken(requestParams, response);

            response = new MsalTokenResponse
            {
                IdToken = MockHelpers.CreateIdToken(MsalTestConstants.UniqueId, MsalTestConstants.DisplayableId),
                ClientInfo = MockHelpers.CreateClientInfo(),
                AccessToken = "access-token-2",
                ExpiresIn = 3599,
                CorrelationId = "correlation-id",
                RefreshToken = "refresh-token-2",
                Scope = MsalTestConstants.Scope.AsSingleString() + " another-scope",
                TokenType = "Bearer"
            };

            requestContext = new RequestContext(new MsalLogger(Guid.NewGuid(), null));
            requestParams = new AuthenticationRequestParameters()
            {
                RequestContext = requestContext,
                Authority = Authority.CreateAuthority(MsalTestConstants.AuthorityGuestTenant, false),
                ClientId = MsalTestConstants.ClientId,
                TenantUpdatedCanonicalAuthority = MsalTestConstants.AuthorityGuestTenant
            };

            cache.SetAfterAccess(AfterAccessChangedNotification);
            cache.SaveAccessAndRefreshToken(requestParams, response);
            Assert.IsFalse(cache.HasStateChanged);

            Assert.AreEqual(1, cache.tokenCacheAccessor.RefreshTokenCount);
            Assert.AreEqual(2, cache.tokenCacheAccessor.AccessTokenCount);

            Assert.AreEqual("refresh-token-2", cache.GetAllRefreshTokensForClient(requestContext).First().Secret);
        }

      
        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void CanDeserializeTokenCacheInNet462()
        {
            var previousLogLevel = Logger.Level;
            // Setting LogLevel.Verbose causes certain static dependencies to load
            Logger.Level = LogLevel.Verbose;
            var tokenCache = new TokenCache();
            tokenCache.Deserialize(null);
            Assert.IsFalse(tokenCache.HasStateChanged, "State should not have changed when deserializing nothing.");
            Logger.Level = previousLogLevel;
        }
        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void SerializeDeserializeCacheTest()
        {
            var cache = new TokenCache()
            {
                ClientId = MsalTestConstants.ClientId
            };

            var response = new MsalTokenResponse
            {
                IdToken = MockHelpers.CreateIdToken(MsalTestConstants.UniqueId, MsalTestConstants.DisplayableId),
                ClientInfo = MockHelpers.CreateClientInfo(),
                AccessToken = "access-token",
                ExpiresIn = 3599,
                CorrelationId = "correlation-id",
                RefreshToken = "refresh-token",
                Scope = MsalTestConstants.Scope.AsSingleString(),
                TokenType = "Bearer"
            };

            var requestContext = new RequestContext(new MsalLogger(Guid.NewGuid(), null));
            var requestParams = new AuthenticationRequestParameters()
            {
                RequestContext = requestContext,
                Authority = Authority.CreateAuthority(MsalTestConstants.AuthorityHomeTenant, false),
                ClientId = MsalTestConstants.ClientId,
                TenantUpdatedCanonicalAuthority = MsalTestConstants.AuthorityTestTenant
            };

            AddHostToInstanceCache(MsalTestConstants.ProductionPrefNetworkEnvironment);

            cache.SaveAccessAndRefreshToken(requestParams, response);
            byte[] serializedCache = cache.Serialize();
            cache.tokenCacheAccessor.ClearAccessTokens();
            cache.tokenCacheAccessor.ClearRefreshTokens();

            Assert.AreEqual(0, cache.tokenCacheAccessor.RefreshTokenCount);
            Assert.AreEqual(0, cache.tokenCacheAccessor.AccessTokenCount);

            cache.Deserialize(serializedCache);

            Assert.AreEqual(1, cache.tokenCacheAccessor.RefreshTokenCount);
            Assert.AreEqual(1, cache.tokenCacheAccessor.AccessTokenCount);

            serializedCache = cache.Serialize();
            cache.Deserialize(serializedCache);
            //item count should not change because old cache entries should have
            //been overriden

            Assert.AreEqual(1, cache.tokenCacheAccessor.RefreshTokenCount);
            Assert.AreEqual(1, cache.tokenCacheAccessor.AccessTokenCount);

            var atItem = cache.GetAllAccessTokensForClient(requestContext).First();
            Assert.AreEqual(response.AccessToken, atItem.Secret);
            Assert.AreEqual(MsalTestConstants.AuthorityTestTenant, atItem.Authority);
            Assert.AreEqual(MsalTestConstants.ClientId, atItem.ClientId);
            Assert.AreEqual(response.TokenType, atItem.TokenType);
            Assert.AreEqual(response.Scope, atItem.ScopeSet.AsSingleString());

            // todo add test for idToken serialization
            //Assert.AreEqual(response.IdToken, atItem.RawIdToken);

            var rtItem = cache.GetAllRefreshTokensForClient(requestContext).First();
            Assert.AreEqual(response.RefreshToken, rtItem.Secret);
            Assert.AreEqual(MsalTestConstants.ClientId, rtItem.ClientId);
            Assert.AreEqual(MsalTestConstants.UserIdentifier.Identifier, rtItem.HomeAccountId);
            Assert.AreEqual(MsalTestConstants.ProductionPrefNetworkEnvironment, rtItem.Environment);
        }
#endif

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void FindAccessToken_ScopeCaseInsensitive()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var tokenCache = new TokenCache()
                {
                    ClientId = MsalTestConstants.ClientId,
                    HttpManager = httpManager
                };

                TokenCacheHelper.PopulateCache(tokenCache.tokenCacheAccessor);

                var param = new AuthenticationRequestParameters()
                {
                    RequestContext = new RequestContext(new MsalLogger(Guid.Empty, null)),
                    ClientId = MsalTestConstants.ClientId,
                    Authority = Authority.CreateAuthority(MsalTestConstants.AuthorityTestTenant, false),
                    Scope = new SortedSet<string>(),
                    Account = MsalTestConstants.User
                };

                string scopeInCache = MsalTestConstants.Scope.FirstOrDefault();

                string upperCaseScope = scopeInCache.ToUpper();
                param.Scope.Add(upperCaseScope);

                var item = tokenCache.FindAccessTokenAsync(param).Result;

                Assert.IsNotNull(item);
                Assert.IsTrue(item.ScopeSet.Contains(scopeInCache));
            }
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void CacheB2CTokenTest()
        {
            var B2CCache = new TokenCache();
            string tenantID = "someTenantID";
            var authority = Authority.CreateAuthority(
                $"https://login.microsoftonline.com/tfp/{tenantID}/somePolicy/oauth2/v2.0/authorize",
                false);

            // creating IDToken with empty tenantID and displayableID/PreferedUserName for B2C scenario
            var response = new MsalTokenResponse
            {
                IdToken = MockHelpers.CreateIdToken(string.Empty, string.Empty, string.Empty),
                ClientInfo = MockHelpers.CreateClientInfo(),
                AccessToken = "access-token",
                ExpiresIn = 3599,
                CorrelationId = "correlation-id",
                RefreshToken = "refresh-token",
                Scope = MsalTestConstants.Scope.AsSingleString(),
                TokenType = "Bearer"
            };

            var requestContext = new RequestContext(new MsalLogger(Guid.NewGuid(), null));
            var requestParams = new AuthenticationRequestParameters()
            {
                RequestContext = requestContext,
                Authority = authority,
                ClientId = MsalTestConstants.ClientId,
                TenantUpdatedCanonicalAuthority = MsalTestConstants.AuthorityTestTenant
            };

            B2CCache.SaveAccessAndRefreshToken(requestParams, response);

            Assert.AreEqual(1, B2CCache.tokenCacheAccessor.RefreshTokenCount);
            Assert.AreEqual(1, B2CCache.tokenCacheAccessor.AccessTokenCount);
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
#endif