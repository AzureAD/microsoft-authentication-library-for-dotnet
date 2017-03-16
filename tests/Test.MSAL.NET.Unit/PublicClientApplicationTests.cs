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
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Cache;
using Microsoft.Identity.Client.Internal.Http;
using Microsoft.Identity.Client.Internal.Instance;
using Microsoft.Identity.Client.Internal.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Test.MSAL.NET.Unit.Mocks;

namespace Test.MSAL.NET.Unit
{
    [TestClass]
    public class PublicClientApplicationTests
    {
        TokenCache cache;

        [TestInitialize]
        public void TestInitialize()
        {
            cache = new TokenCache();
            Authority.ValidatedAuthorities.Clear();
            HttpClientFactory.ReturnHttpClientForMocks = true;
            HttpMessageHandlerFactory.ClearMockHandlers();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            cache.TokenCacheAccessor.TokenCacheDictionary.Clear();
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void ConstructorsTest()
        {
            PublicClientApplication app = new PublicClientApplication(TestConstants.ClientId);
            Assert.IsNotNull(app);
            Assert.AreEqual("https://login.microsoftonline.com/common/", app.Authority);
            Assert.AreEqual(TestConstants.ClientId, app.ClientId);
            Assert.AreEqual("urn:ietf:wg:oauth:2.0:oob", app.RedirectUri);
            Assert.IsTrue(app.ValidateAuthority);

            app = new PublicClientApplication(TestConstants.ClientId, TestConstants.AuthorityGuestTenant);
            Assert.IsNotNull(app);
            Assert.AreEqual(TestConstants.AuthorityGuestTenant, app.Authority);
            Assert.AreEqual(TestConstants.ClientId, app.ClientId);
            Assert.AreEqual("urn:ietf:wg:oauth:2.0:oob", app.RedirectUri);
            Assert.IsTrue(app.ValidateAuthority);


            app = new PublicClientApplication(TestConstants.ClientId, "https://login.microsoftonline.com/tfp/vibrob2c.onmicrosoft.com/B2C_1_B2C_Signup_Signin_Policy/oauth2/v2.0");
            Assert.IsNotNull(app);
            Assert.AreEqual("https://login.microsoftonline.com/tfp/vibrob2c.onmicrosoft.com/b2c_1_b2c_signup_signin_policy/", app.Authority);
            Assert.AreEqual(TestConstants.ClientId, app.ClientId);
            Assert.AreEqual("urn:ietf:wg:oauth:2.0:oob", app.RedirectUri);
            Assert.IsTrue(app.ValidateAuthority);
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void GetUsersTest()
        {
            PublicClientApplication app = new PublicClientApplication(TestConstants.ClientId);
            IEnumerable<User> users = app.Users;
            Assert.IsNotNull(users);
            Assert.IsFalse(users.Any());
            cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };

            app.UserTokenCache = cache;
            TokenCacheHelper.PopulateCache(cache.TokenCacheAccessor);
            users = app.Users;
            Assert.IsNotNull(users);
            Assert.AreEqual(1, users.Count());
            foreach (var user in users)
            {
                Assert.AreEqual(TestConstants.ClientId, user.ClientId);
                Assert.IsNotNull(user.TokenCache);
            }

            // another cache entry for different home object id. user count should be 2.
            TokenCacheKey key = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                TestConstants.ScopeForAnotherResource, TestConstants.ClientId,
                TestConstants.HomeObjectId + "more");


            AccessTokenCacheItem item = new AccessTokenCacheItem()
            {
                AccessToken = key.ToString(),
                TokenType = "Bearer",
                ExpiresOnUnixTimestamp = MsalHelpers.DateTimeToUnixTimestamp((DateTime.UtcNow + TimeSpan.FromSeconds(3600))),
                User = new User
                {
                    DisplayableId = TestConstants.DisplayableId,
                    UniqueId = TestConstants.UniqueId + "more",
                    HomeObjectId = TestConstants.HomeObjectId
                },
                Scope = TestConstants.Scope
            };
            cache.TokenCacheAccessor.TokenCacheDictionary[key.ToString()] = JsonHelper.SerializeToJson(item);


            // another cache entry for different home object id. user count should be 2.
            TokenCacheKey rtKey = new TokenCacheKey(null, null, TestConstants.ClientId,
                TestConstants.HomeObjectId + "more");
            
            RefreshTokenCacheItem rtItem = new RefreshTokenCacheItem()
            {
                ClientId = TestConstants.ClientId,
                RefreshToken = "someRT",
                RawIdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId + "more", TestConstants.DisplayableId, TestConstants.HomeObjectId + "more"),
                User = new User
                {
                    DisplayableId = TestConstants.DisplayableId,
                    UniqueId = TestConstants.UniqueId + "more",
                    HomeObjectId = TestConstants.HomeObjectId + "more"
                }
            };
            cache.TokenCacheAccessor.TokenCacheDictionary[rtKey.ToString()] = JsonHelper.SerializeToJson(rtItem);


            users = app.Users;
            Assert.IsNotNull(users);
            Assert.AreEqual(2, users.Count());
            foreach (var user in users)
            {
                Assert.AreEqual(TestConstants.ClientId, user.ClientId);
                Assert.IsNotNull(user.TokenCache);
            }
        }


        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void GetUsersAndSignThemOutTest()
        {
            PublicClientApplication app = new PublicClientApplication(TestConstants.ClientId);
            app.UserTokenCache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };
            TokenCacheHelper.PopulateCache(cache.TokenCacheAccessor);

            foreach (var user in app.Users)
            {
                user.SignOut();
            }

            Assert.AreEqual(0, app.UserTokenCache.AccessTokenCount);
            Assert.AreEqual(0, app.UserTokenCache.RefreshTokenCount);
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void AcquireTokenSilentCacheOnlyLookupTest()
        {
            PublicClientApplication app = new PublicClientApplication(TestConstants.ClientId, TestConstants.AuthorityHomeTenant)
            {
                ValidateAuthority = false
            };

            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(TestConstants.AuthorityHomeTenant)
            });

            cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };

            app.UserTokenCache = cache;
            TokenCacheHelper.PopulateCache(cache.TokenCacheAccessor);
            cache.TokenCacheAccessor.TokenCacheDictionary.Remove(new TokenCacheKey(TestConstants.AuthorityGuestTenant,
                TestConstants.ScopeForAnotherResource, TestConstants.ClientId,
                TestConstants.HomeObjectId).ToString());

            Task<AuthenticationResult> task = app.AcquireTokenSilentAsync(TestConstants.Scope.ToArray(), new User()
            {
                UniqueId = TestConstants.UniqueId,
                DisplayableId = TestConstants.DisplayableId,
                HomeObjectId = TestConstants.HomeObjectId,
            }, app.Authority, false);
            AuthenticationResult result = task.Result;
            Assert.IsNotNull(result);
            Assert.AreEqual(TestConstants.DisplayableId, result.User.DisplayableId);
            Assert.AreEqual(TestConstants.UniqueId, result.User.UniqueId);
            Assert.AreEqual(TestConstants.Scope.AsSingleString(), result.Scope.AsSingleString());

            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }
        
        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void AcquireTokenSilentForceRefreshTest()
        {
            PublicClientApplication app = new PublicClientApplication(TestConstants.ClientId)
            {
                ValidateAuthority = false
            };

            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(TestConstants.AuthorityHomeTenant)
            });

            cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };

            app.UserTokenCache = cache;
            TokenCacheHelper.PopulateCache(cache.TokenCacheAccessor);

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage =
                    MockHelpers.CreateSuccessTokenResponseMessage(TestConstants.UniqueId,
                        TestConstants.DisplayableId, TestConstants.HomeObjectId,
                        TestConstants.Scope.Union(TestConstants.ScopeForAnotherResource).ToArray())
            });

            Task<AuthenticationResult> task = app.AcquireTokenSilentAsync(TestConstants.Scope.ToArray(),
                new User()
                {
                    UniqueId = TestConstants.UniqueId,
                    DisplayableId = TestConstants.DisplayableId,
                    HomeObjectId = TestConstants.HomeObjectId,
                }, app.Authority, true);
            AuthenticationResult result = task.Result;
            Assert.IsNotNull(result);
            Assert.AreEqual(TestConstants.DisplayableId, result.User.DisplayableId);
            Assert.AreEqual(TestConstants.UniqueId, result.User.UniqueId);
            Assert.AreEqual(
                TestConstants.Scope.Union(TestConstants.ScopeForAnotherResource).ToArray().AsSingleString(),
                result.Scope.AsSingleString());

            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void AcquireTokenSilentServiceErrorTest()
        {
            PublicClientApplication app = new PublicClientApplication(TestConstants.ClientId)
            {
                ValidateAuthority = false
            };

            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(TestConstants.AuthorityHomeTenant)
            });

            //populate cache
            cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };

            app.UserTokenCache = cache;
            TokenCacheHelper.PopulateCache(cache.TokenCacheAccessor);

            MockHttpMessageHandler mockHandler = new MockHttpMessageHandler();
            mockHandler.Method = HttpMethod.Post;
            mockHandler.ResponseMessage = MockHelpers.CreateInvalidGrantTokenResponseMessage();
            HttpMessageHandlerFactory.AddMockHandler(mockHandler);
            try
            {
                Task<AuthenticationResult> task =
                    app.AcquireTokenSilentAsync(TestConstants.ScopeForAnotherResource.ToArray(),
                        new User()
                        {
                            UniqueId = TestConstants.UniqueId,
                            DisplayableId = TestConstants.DisplayableId,
                            HomeObjectId = TestConstants.HomeObjectId,
                        }, app.Authority, false);
                AuthenticationResult result = task.Result;
                Assert.Fail("AdalSilentTokenAcquisitionException was expected");
            }
            catch (AggregateException ex)
            {
                Assert.IsNotNull(ex.InnerException);
                Assert.IsTrue(ex.InnerException is MsalServiceException);
                var msalExc = (MsalServiceException) ex.InnerException;
                Assert.AreEqual(msalExc.ErrorCode, "invalid_grant");
            }

            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        [ExpectedException(typeof(HttpRequestException), "Cannot write more bytes to the buffer than the configured maximum buffer size: 1048576.")]
        public async Task HttpRequestExceptionIsNotSuppressed()
        {
            var app = new PublicClientApplication(TestConstants.ClientId);

            // add mock response bigger than 1MB for Http Client
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(new string(new char[1048577]))
                }
            });

            await app.AcquireTokenAsync(TestConstants.Scope.ToArray());
        }
    }
}
