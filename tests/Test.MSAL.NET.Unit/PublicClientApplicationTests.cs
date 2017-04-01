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
            cache.TokenCacheAccessor.AccessTokenCacheDictionary.Clear();
            cache.TokenCacheAccessor.RefreshTokenCacheDictionary.Clear();
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        [Description("Tests the public interfaces can be mocked")]
        public void MockPublicClientApplication()
        {
            // Setup up a public client application that returns a dummy result
            // The caller asks for two scopes, but only one is returned
            var mockResult = Substitute.For<IAuthenticationResult>();
            mockResult.IdToken.Returns("id token");
            mockResult.Scope.Returns(new string[] { "scope1" });

            var mockApp = Substitute.For<IPublicClientApplication>();
            mockApp.AcquireTokenAsync(new string[] { "scope1", "scope2" }).ReturnsForAnyArgs(mockResult);

            // Now call the substitute with the args to get the substitute result
            IAuthenticationResult actualResult = mockApp.AcquireTokenAsync(new string[] { "scope1" }).Result;
            Assert.IsNotNull(actualResult);
            Assert.AreEqual("id token", actualResult.IdToken, "Mock result failed to return the expected id token");

            // Check the users properties returns the dummy users
            IEnumerable<string> scopes = actualResult.Scope;
            Assert.IsNotNull(scopes);
            CollectionAssert.AreEqual(new string[] { "scope1" }, actualResult.Scope.ToArray());
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        [Description("Tests the public application interfaces can be mocked to throw MSAL exceptions")]
        public void MockPublicClientApplication_Exception()
        {
            // Setup up a confidential client application that returns throws
            var mockApp = Substitute.For<IPublicClientApplication>();
            mockApp
                .WhenForAnyArgs(x => x.AcquireTokenAsync(Arg.Any<string[]>()))
                .Do(x => { throw new MsalServiceException("my error code", "my message"); });


            // Now call the substitute and check the exception is thrown
            MsalServiceException ex = AssertException.Throws<MsalServiceException>(() => mockApp.AcquireTokenAsync(new string[] { "scope1" }));
            Assert.AreEqual("my error code", ex.ErrorCode);
            Assert.AreEqual("my message", ex.Message);
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
            
            AccessTokenCacheItem item = new AccessTokenCacheItem()
            {
                Authority = TestConstants.AuthorityHomeTenant,
                ClientId = TestConstants.ClientId,
                TokenType = "Bearer",
                ExpiresOnUnixTimestamp = MsalHelpers.DateTimeToUnixTimestamp((DateTime.UtcNow + TimeSpan.FromSeconds(3600))),
                RawIdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId),
                RawClientInfo = MockHelpers.CreateClientInfo(),
                ScopeSet = TestConstants.Scope
            };
            item.IdToken = IdToken.Parse(item.RawIdToken);
            item.ClientInfo = ClientInfo.Parse(item.RawClientInfo);
            item.AccessToken = item.GetAccessTokenItemKey().ToString();
            cache.TokenCacheAccessor.AccessTokenCacheDictionary[item.GetAccessTokenItemKey().ToString()] = JsonHelper.SerializeToJson(item);


            // another cache entry for different uid. user count should be 2.
            RefreshTokenCacheItem rtItem = new RefreshTokenCacheItem()
            {
                Environment = TestConstants.ProductionEnvironment,
                ClientId = TestConstants.ClientId,
                RefreshToken = "someRT",
                RawClientInfo = MockHelpers.CreateClientInfo(TestConstants.Uid + "more", TestConstants.Utid),
                DisplayableId = TestConstants.DisplayableId,
                IdentityProvider = TestConstants.IdentityProvider,
                Name = TestConstants.Name
            };
            rtItem.ClientInfo = ClientInfo.Parse(rtItem.RawClientInfo);
            cache.TokenCacheAccessor.RefreshTokenCacheDictionary[rtItem.GetRefreshTokenItemKey().ToString()] = JsonHelper.SerializeToJson(rtItem);
            Assert.AreEqual(2, cache.TokenCacheAccessor.RefreshTokenCacheDictionary.Count);
            users = app.Users;
            Assert.IsNotNull(users);
            Assert.AreEqual(2, users.Count());

            // another cache entry for different environment. user count should still be 2. Sovereign cloud user must not be returned
            rtItem = new RefreshTokenCacheItem()
            {
                Environment = TestConstants.SovereignEnvironment,
                ClientId = TestConstants.ClientId,
                RefreshToken = "someRT",
                RawClientInfo = MockHelpers.CreateClientInfo(TestConstants.Uid + "more1", TestConstants.Utid),
                DisplayableId = TestConstants.DisplayableId,
                IdentityProvider = TestConstants.IdentityProvider,
                Name = TestConstants.Name
            };
            rtItem.ClientInfo = ClientInfo.Parse(rtItem.RawClientInfo);
            cache.TokenCacheAccessor.RefreshTokenCacheDictionary[rtItem.GetRefreshTokenItemKey().ToString()] = JsonHelper.SerializeToJson(rtItem);
            Assert.AreEqual(3, cache.TokenCacheAccessor.RefreshTokenCacheDictionary.Count);
            users = app.Users;
            Assert.IsNotNull(users);
            Assert.AreEqual(2, users.Count());
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
                app.Remove(user);
            }

            Assert.AreEqual(0, app.UserTokenCache.TokenCacheAccessor.AccessTokenCacheDictionary.Count);
            Assert.AreEqual(0, app.UserTokenCache.TokenCacheAccessor.RefreshTokenCacheDictionary.Count);
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
            cache.TokenCacheAccessor.AccessTokenCacheDictionary.Remove(new AccessTokenCacheKey(TestConstants.AuthorityGuestTenant,
                TestConstants.ScopeForAnotherResource, TestConstants.ClientId,
                TestConstants.UserIdentifier).ToString());

            Task<IAuthenticationResult> task = app.AcquireTokenSilentAsync(TestConstants.Scope.ToArray(), new User()
            {
                DisplayableId = TestConstants.DisplayableId,
                Identifier = TestConstants.UserIdentifier,
            }, app.Authority, false);
            IAuthenticationResult result = task.Result;
            Assert.IsNotNull(result);
            Assert.AreEqual(TestConstants.DisplayableId, result.User.DisplayableId);
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
                        TestConstants.DisplayableId,
                        TestConstants.Scope.Union(TestConstants.ScopeForAnotherResource).ToArray())
            });

            Task<IAuthenticationResult> task = app.AcquireTokenSilentAsync(TestConstants.Scope.ToArray(),
                new User()
                {
                    DisplayableId = TestConstants.DisplayableId,
                    Identifier = TestConstants.UserIdentifier,
                }, app.Authority, true);
            IAuthenticationResult result = task.Result;
            Assert.IsNotNull(result);
            Assert.AreEqual(TestConstants.DisplayableId, result.User.DisplayableId);
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
                Task<IAuthenticationResult> task =
                    app.AcquireTokenSilentAsync(TestConstants.ScopeForAnotherResource.ToArray(),
                        new User()
                        {
                            DisplayableId = TestConstants.DisplayableId,
                            Identifier = TestConstants.UserIdentifier,
                        }, app.Authority, false);
                IAuthenticationResult result = task.Result;
                Assert.Fail("AdalSilentTokenAcquisitionException was expected");
            }
            catch (AggregateException ex)
            {
                Assert.IsNotNull(ex.InnerException);
                Assert.IsTrue(ex.InnerException is MsalServiceException);
                var msalExc = (MsalServiceException)ex.InnerException;
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