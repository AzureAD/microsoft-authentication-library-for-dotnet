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
using Microsoft.Identity.Client.Internal.OAuth2;
using Microsoft.Identity.Client.Internal.Telemetry;

namespace Test.MSAL.NET.Unit
{
    [TestClass]
    public class PublicClientApplicationTests
    {
        TokenCache cache;
        private MyReceiver _myReceiver = new MyReceiver();

        [TestInitialize]
        public void TestInitialize()
        {
            cache = new TokenCache();
            Authority.ValidatedAuthorities.Clear();
            HttpClientFactory.ReturnHttpClientForMocks = true;
            HttpMessageHandlerFactory.ClearMockHandlers();
            Telemetry.GetInstance().RegisterReceiver(_myReceiver.OnEvents);
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
            var mockResult = Substitute.For<AuthenticationResult>();
            mockResult.IdToken.Returns("id token");
            mockResult.Scopes.Returns(new string[] { "scope1" });

            var mockApp = Substitute.For<IPublicClientApplication>();
            mockApp.AcquireTokenAsync(new string[] { "scope1", "scope2" }).ReturnsForAnyArgs(mockResult);

            // Now call the substitute with the args to get the substitute result
            AuthenticationResult actualResult = mockApp.AcquireTokenAsync(new string[] { "scope1" }).Result;
            Assert.IsNotNull(actualResult);
            Assert.AreEqual("id token", actualResult.IdToken, "Mock result failed to return the expected id token");

            // Check the users properties returns the dummy users
            IEnumerable<string> scopes = actualResult.Scopes;
            Assert.IsNotNull(scopes);
            CollectionAssert.AreEqual(new string[] { "scope1" }, actualResult.Scopes.ToArray());
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
            MsalServiceException ex =
                AssertException.Throws<MsalServiceException>(() => mockApp.AcquireTokenAsync(new string[] { "scope1" }));
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

            app = new PublicClientApplication(TestConstants.ClientId,
                "https://login.microsoftonline.com/tfp/vibrob2c.onmicrosoft.com/B2C_1_B2C_Signup_Signin_Policy/oauth2/v2.0");
            Assert.IsNotNull(app);
            Assert.AreEqual(
                "https://login.microsoftonline.com/tfp/vibrob2c.onmicrosoft.com/b2c_1_b2c_signup_signin_policy/",
                app.Authority);
            Assert.AreEqual(TestConstants.ClientId, app.ClientId);
            Assert.AreEqual("urn:ietf:wg:oauth:2.0:oob", app.RedirectUri);
            Assert.IsTrue(app.ValidateAuthority);
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public async Task NoStateReturnedTestAsync()
        {
            PublicClientApplication app = new PublicClientApplication(TestConstants.ClientId);

            MockWebUI ui = new MockWebUI()
            {
                AddStateInAuthorizationResult = false,
                MockResult = new AuthorizationResult(AuthorizationStatus.Success,
                    TestConstants.AuthorityHomeTenant + "?code=some-code")
            };

            MockHelpers.ConfigureMockWebUI(ui);

            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(TestConstants.AuthorityHomeTenant)
            });

            try
            {
                AuthenticationResult result = await app.AcquireTokenAsync(TestConstants.Scope).ConfigureAwait(false);
                Assert.Fail("API should have failed here");
            }
            catch (MsalClientException exc)
            {
                Assert.IsNotNull(exc);
                Assert.AreEqual(MsalClientException.StateMismatchError, exc.ErrorCode);
            }
            finally
            {
                Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
            }
            Assert.IsNotNull(_myReceiver.EventsReceived.Find(anEvent =>  // Expect finding such an event
                anEvent[EventBase.EventNameKey].EndsWith("api_event") && anEvent[ApiEvent.ApiIdKey] == "170"
                && anEvent[ApiEvent.WasSuccessfulKey] == "false" && anEvent[ApiEvent.ApiErrorCodeKey] == "state_mismatch"
                ));
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public async Task DifferentStateReturnedTestAsync()
        {
            PublicClientApplication app = new PublicClientApplication(TestConstants.ClientId);

            MockWebUI ui = new MockWebUI()
            {
                AddStateInAuthorizationResult = false,
                MockResult = new AuthorizationResult(AuthorizationStatus.Success,
                    TestConstants.AuthorityHomeTenant + "?code=some-code&state=mistmatched")
            };

            MockHelpers.ConfigureMockWebUI(ui);

            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(TestConstants.AuthorityHomeTenant)
            });

            try
            {
                AuthenticationResult result = await app.AcquireTokenAsync(TestConstants.Scope).ConfigureAwait(false);
                Assert.Fail("API should have failed here");
            }
            catch (MsalClientException exc)
            {
                Assert.IsNotNull(exc);
                Assert.AreEqual(MsalClientException.StateMismatchError, exc.ErrorCode);
            }
            finally
            {
                Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
            }
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public async Task AcquireTokenNoClientInfoReturnedTestAsync()
        {
            PublicClientApplication app = new PublicClientApplication(TestConstants.ClientId);

            MockWebUI ui = new MockWebUI()
            {
                MockResult = new AuthorizationResult(AuthorizationStatus.Success,
                    TestConstants.AuthorityHomeTenant + "?code=some-code")
            };

            MockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
                app.RedirectUri + "?code=some-code"));

            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(TestConstants.AuthorityHomeTenant)
            });

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage("some-scope1 some-scope2",
                    MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId), string.Empty)
            });

            try
            {
                AuthenticationResult result = await app.AcquireTokenAsync(TestConstants.Scope).ConfigureAwait(false);
            }
            catch (MsalClientException exc)
            {
                Assert.IsNotNull(exc);
                Assert.AreEqual(MsalClientException.JsonParseError, exc.ErrorCode);
                Assert.AreEqual("client info is null", exc.Message);
            }
            finally
            {
                Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
            }
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void AcquireTokenSameUserTest()
        {
            PublicClientApplication app = new PublicClientApplication(TestConstants.ClientId);

            MockWebUI ui = new MockWebUI()
            {
                MockResult = new AuthorizationResult(AuthorizationStatus.Success,
                    TestConstants.AuthorityHomeTenant + "?code=some-code")
            };

            MockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
                app.RedirectUri + "?code=some-code"));

            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(TestConstants.AuthorityHomeTenant)
            });

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
            });

            AuthenticationResult result = app.AcquireTokenAsync(TestConstants.Scope).Result;
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.User);
            Assert.AreEqual(TestConstants.UniqueId, result.UniqueId);
            Assert.AreEqual(TestConstants.CreateUserIdentifer(), result.User.Identifier);
            Assert.AreEqual(TestConstants.DisplayableId, result.User.DisplayableId);

            // repeat interactive call and pass in the same user
            MockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
                app.RedirectUri + "?code=some-code"));

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
            });

            result = app.AcquireTokenAsync(TestConstants.Scope).Result;
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.User);
            Assert.AreEqual(TestConstants.UniqueId, result.UniqueId);
            Assert.AreEqual(TestConstants.CreateUserIdentifer(), result.User.Identifier);
            Assert.AreEqual(TestConstants.DisplayableId, result.User.DisplayableId);

            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void AcquireTokenAddTwoUsersTest()
        {
            PublicClientApplication app = new PublicClientApplication(TestConstants.ClientId);

            MockWebUI ui = new MockWebUI()
            {
                MockResult = new AuthorizationResult(AuthorizationStatus.Success,
                    TestConstants.AuthorityHomeTenant + "?code=some-code")
            };

            MockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
                app.RedirectUri + "?code=some-code"));

            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(TestConstants.AuthorityHomeTenant)
            });

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
            });

            AuthenticationResult result = app.AcquireTokenAsync(TestConstants.Scope).Result;
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.User);
            Assert.AreEqual(TestConstants.UniqueId, result.UniqueId);
            Assert.AreEqual(TestConstants.CreateUserIdentifer(), result.User.Identifier);
            Assert.AreEqual(TestConstants.DisplayableId, result.User.DisplayableId);
            Assert.AreEqual(TestConstants.IdentityProvider, result.TenantId);

            // repeat interactive call and pass in the same user
            MockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
                app.RedirectUri + "?code=some-code"));

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(TestConstants.Scope.ToString(),
                    MockHelpers.CreateIdToken(TestConstants.UniqueId + "more", TestConstants.DisplayableId + "more",
                        TestConstants.IdentityProvider + "more"),
                    MockHelpers.CreateClientInfo(TestConstants.Uid + "more",
                        TestConstants.IdentityProvider + "more"))
            });

            result = app.AcquireTokenAsync(TestConstants.Scope).Result;
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.User);
            Assert.AreEqual(TestConstants.UniqueId + "more", result.UniqueId);
            Assert.AreEqual(TestConstants.CreateUserIdentifer(TestConstants.Uid + "more",
                TestConstants.IdentityProvider + "more"), result.User.Identifier);
            Assert.AreEqual(TestConstants.DisplayableId + "more", result.User.DisplayableId);
            Assert.AreEqual(TestConstants.IdentityProvider + "more", result.TenantId);

            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void AcquireTokenDifferentUserReturnedFromServiceTest()
        {
            cache.ClientId = TestConstants.ClientId;
            PublicClientApplication app = new PublicClientApplication(TestConstants.ClientId)
            {
                UserTokenCache = cache
            };

            MockWebUI ui = new MockWebUI()
            {
                MockResult = new AuthorizationResult(AuthorizationStatus.Success,
                    TestConstants.AuthorityHomeTenant + "?code=some-code")
            };

            MockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
                app.RedirectUri + "?code=some-code"));

            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(TestConstants.AuthorityHomeTenant)
            });

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
            });

            AuthenticationResult result = app.AcquireTokenAsync(TestConstants.Scope).Result;
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.User);
            Assert.AreEqual(TestConstants.UniqueId, result.UniqueId);
            Assert.AreEqual(TestConstants.CreateUserIdentifer(), result.User.Identifier);
            Assert.AreEqual(TestConstants.DisplayableId, result.User.DisplayableId);

            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");

            var dict = new Dictionary<string, string>();
            dict[OAuth2Parameter.DomainReq] = TestConstants.Utid;
            dict[OAuth2Parameter.LoginReq] = TestConstants.Uid;

            // repeat interactive call and pass in the same user
            MockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
                app.RedirectUri + "?code=some-code"), dict);

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(TestConstants.Scope.AsSingleString(),
                    MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId),
                    MockHelpers.CreateClientInfo(TestConstants.Uid, TestConstants.Utid + "more"))
            });

            try
            {
                result = app.AcquireTokenAsync(TestConstants.Scope, result.User, UIBehavior.SelectAccount, null).Result;
                Assert.Fail("API should have failed here");
            }
            catch (AggregateException ex)
            {
                MsalServiceException exc = (MsalServiceException)ex.InnerException;
                Assert.IsNotNull(exc);
                Assert.AreEqual("user_mismatch", exc.ErrorCode);
            }
            Assert.IsNotNull(_myReceiver.EventsReceived.Find(anEvent =>  // Expect finding such an event
                anEvent[EventBase.EventNameKey].EndsWith("api_event") && anEvent[ApiEvent.ApiIdKey] == "174"
                && anEvent[ApiEvent.WasSuccessfulKey] == "false" && anEvent[ApiEvent.ApiErrorCodeKey] == "user_mismatch"
                ));

            Assert.AreEqual(1, app.Users.Count());
            Assert.AreEqual(1, cache.TokenCacheAccessor.AccessTokenCacheDictionary.Count);

            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void AcquireTokenNullUserPassedInAndNewUserReturnedFromServiceTest()
        {
            cache.ClientId = TestConstants.ClientId;
            PublicClientApplication app = new PublicClientApplication(TestConstants.ClientId)
            {
                UserTokenCache = cache
            };

            MockWebUI ui = new MockWebUI()
            {
                MockResult = new AuthorizationResult(AuthorizationStatus.Success,
                    TestConstants.AuthorityHomeTenant + "?code=some-code")
            };

            MockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
                app.RedirectUri + "?code=some-code"));

            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(TestConstants.AuthorityHomeTenant)
            });

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
            });

            AuthenticationResult result = app.AcquireTokenAsync(TestConstants.Scope).Result;
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.User);
            Assert.AreEqual(TestConstants.UniqueId, result.UniqueId);
            Assert.AreEqual(TestConstants.CreateUserIdentifer(), result.User.Identifier);
            Assert.AreEqual(TestConstants.DisplayableId, result.User.DisplayableId);
            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");

            // repeat interactive call and pass in the same user
            MockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
                app.RedirectUri + "?code=some-code"));

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(TestConstants.Scope.AsSingleString(),
                    MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId),
                    MockHelpers.CreateClientInfo(TestConstants.Uid, TestConstants.Utid + "more"))
            });

            result = app.AcquireTokenAsync(TestConstants.Scope, (IUser)null, UIBehavior.SelectAccount, null).Result;
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.User);
            Assert.AreEqual(TestConstants.UniqueId, result.UniqueId);
            Assert.AreEqual(TestConstants.CreateUserIdentifer(TestConstants.Uid, TestConstants.Utid + "more"),
                result.User.Identifier);
            Assert.AreEqual(TestConstants.DisplayableId, result.User.DisplayableId);
            Assert.AreEqual(2, app.Users.Count());
            Assert.AreEqual(2, cache.TokenCacheAccessor.AccessTokenCacheDictionary.Count);
            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void GetUsersTest()
        {
            PublicClientApplication app = new PublicClientApplication(TestConstants.ClientId);
            IEnumerable<IUser> users = app.Users;
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
                ExpiresOnUnixTimestamp =
                    MsalHelpers.DateTimeToUnixTimestamp((DateTime.UtcNow + TimeSpan.FromSeconds(3600))),
                RawIdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId),
                RawClientInfo = MockHelpers.CreateClientInfo(),
                ScopeSet = TestConstants.Scope
            };
            item.IdToken = IdToken.Parse(item.RawIdToken);
            item.ClientInfo = ClientInfo.CreateFromJson(item.RawClientInfo);
            item.AccessToken = item.GetAccessTokenItemKey().ToString();
            cache.TokenCacheAccessor.AccessTokenCacheDictionary[item.GetAccessTokenItemKey().ToString()] =
                JsonHelper.SerializeToJson(item);


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
            rtItem.ClientInfo = ClientInfo.CreateFromJson(rtItem.RawClientInfo);
            cache.TokenCacheAccessor.RefreshTokenCacheDictionary[rtItem.GetRefreshTokenItemKey().ToString()] =
                JsonHelper.SerializeToJson(rtItem);
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
            rtItem.ClientInfo = ClientInfo.CreateFromJson(rtItem.RawClientInfo);
            cache.TokenCacheAccessor.RefreshTokenCacheDictionary[rtItem.GetRefreshTokenItemKey().ToString()] =
                JsonHelper.SerializeToJson(rtItem);
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
        public async Task AcquireTokenSilentScopeAndEmptyCacheTestAsync()
        {
            PublicClientApplication app =
                new PublicClientApplication(TestConstants.ClientId)
                {
                    ValidateAuthority = false
                };

            cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };

            app.UserTokenCache = cache;
            try
            {
                AuthenticationResult result = await app.AcquireTokenSilentAsync(TestConstants.Scope.ToArray(),
                        new User()
                        {
                            DisplayableId = TestConstants.DisplayableId,
                            Identifier = TestConstants.UserIdentifier,
                        })
                    .ConfigureAwait(false);
            }
            catch (MsalUiRequiredException exc)
            {
                Assert.AreEqual(MsalUiRequiredException.NoTokensFoundError, exc.ErrorCode);
            }
            Assert.IsNotNull(_myReceiver.EventsReceived.Find(anEvent =>  // Expect finding such an event
                anEvent[EventBase.EventNameKey].EndsWith("api_event") && anEvent[ApiEvent.ApiIdKey] == "30"
                && anEvent[ApiEvent.WasSuccessfulKey] == "false" && anEvent[ApiEvent.ApiErrorCodeKey] == "no_tokens_found"
                ));
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public async Task AcquireTokenSilentScopeAndUserMultipleTokensFoundTestAsync()
        {
            PublicClientApplication app =
                new PublicClientApplication(TestConstants.ClientId)
                {
                    ValidateAuthority = false
                };

            cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };

            app.UserTokenCache = cache;
            TokenCacheHelper.PopulateCache(cache.TokenCacheAccessor);
            try
            {
                AuthenticationResult result = await app.AcquireTokenSilentAsync(TestConstants.Scope.ToArray(),
                        new User()
                        {
                            DisplayableId = TestConstants.DisplayableId,
                            Identifier = TestConstants.UserIdentifier,
                        })
                    .ConfigureAwait(false);
            }
            catch (MsalClientException exc)
            {
                Assert.AreEqual(MsalClientException.MultipleTokensMatchedError, exc.ErrorCode);
            }
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void AcquireTokenSilentScopeAndUserOverloadWithNoMatchingScopesInCacheTest()
        {
            // this test ensures that the API can
            // get authority (if unique) from the cache entries where scope does not match.
            // it should only happen for case where no authority is passed.
            PublicClientApplication app =
                new PublicClientApplication(TestConstants.ClientId)
                {
                    ValidateAuthority = false
                };

            cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };

            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(TestConstants.AuthorityHomeTenant)
            });
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage =
                    MockHelpers.CreateSuccessTokenResponseMessage(TestConstants.UniqueId,
                        TestConstants.DisplayableId,
                        TestConstants.ScopeForAnotherResource.ToArray())
            });

            app.UserTokenCache = cache;
            TokenCacheHelper.PopulateCache(cache.TokenCacheAccessor);
            cache.TokenCacheAccessor.AccessTokenCacheDictionary.Remove(new AccessTokenCacheKey(
                TestConstants.AuthorityGuestTenant,
                TestConstants.ScopeForAnotherResource, TestConstants.ClientId,
                TestConstants.UserIdentifier).ToString());

            Task<AuthenticationResult> task = app.AcquireTokenSilentAsync(TestConstants.ScopeForAnotherResource.ToArray(), new User()
            {
                DisplayableId = TestConstants.DisplayableId,
                Identifier = TestConstants.UserIdentifier,
            });

            AuthenticationResult result = task.Result;
            Assert.IsNotNull(result);
            Assert.AreEqual(TestConstants.DisplayableId, result.User.DisplayableId);
            Assert.AreEqual(TestConstants.ScopeForAnotherResource.AsSingleString(), result.Scopes.AsSingleString());
            Assert.AreEqual(2, cache.TokenCacheAccessor.GetAllAccessTokensAsString().Count());
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void AcquireTokenSilentScopeAndUserOverloadDefaultAuthorityTest()
        {
            PublicClientApplication app =
                new PublicClientApplication(TestConstants.ClientId)
                {
                    ValidateAuthority = false
                };

            cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };

            app.UserTokenCache = cache;
            TokenCacheHelper.PopulateCache(cache.TokenCacheAccessor);
            cache.TokenCacheAccessor.AccessTokenCacheDictionary.Remove(new AccessTokenCacheKey(
                TestConstants.AuthorityGuestTenant,
                TestConstants.ScopeForAnotherResource, TestConstants.ClientId,
                TestConstants.UserIdentifier).ToString());

            Task<AuthenticationResult> task = app.AcquireTokenSilentAsync(TestConstants.Scope.ToArray(), new User()
            {
                DisplayableId = TestConstants.DisplayableId,
                Identifier = TestConstants.UserIdentifier,
            });
            AuthenticationResult result = task.Result;
            Assert.IsNotNull(result);
            Assert.AreEqual(TestConstants.DisplayableId, result.User.DisplayableId);
            Assert.AreEqual(TestConstants.Scope.AsSingleString(), result.Scopes.AsSingleString());
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void AcquireTokenSilentScopeAndUserOverloadTenantSpecificAuthorityTest()
        {
            PublicClientApplication app =
                new PublicClientApplication(TestConstants.ClientId, TestConstants.AuthorityGuestTenant)
                {
                    ValidateAuthority = false
                };

            cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };

            app.UserTokenCache = cache;
            TokenCacheHelper.PopulateCache(cache.TokenCacheAccessor);
            cache.TokenCacheAccessor.AccessTokenCacheDictionary.Remove(new AccessTokenCacheKey(
                TestConstants.AuthorityGuestTenant,
                TestConstants.ScopeForAnotherResource, TestConstants.ClientId,
                TestConstants.UserIdentifier).ToString());

            Task<AuthenticationResult> task = app.AcquireTokenSilentAsync(TestConstants.Scope.ToArray(), new User()
            {
                DisplayableId = TestConstants.DisplayableId,
                Identifier = TestConstants.UserIdentifier,
            });
            AuthenticationResult result = task.Result;
            Assert.IsNotNull(result);
            Assert.AreEqual(TestConstants.DisplayableId, result.User.DisplayableId);
            Assert.AreEqual(TestConstants.Scope.AsSingleString(), result.Scopes.AsSingleString());
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void AcquireTokenSilentCacheOnlyLookupTest()
        {
            PublicClientApplication app =
                new PublicClientApplication(TestConstants.ClientId, TestConstants.AuthorityHomeTenant)
                {
                    ValidateAuthority = false
                };

            cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };

            app.UserTokenCache = cache;
            TokenCacheHelper.PopulateCache(cache.TokenCacheAccessor);
            cache.TokenCacheAccessor.AccessTokenCacheDictionary.Remove(new AccessTokenCacheKey(
                TestConstants.AuthorityGuestTenant,
                TestConstants.ScopeForAnotherResource, TestConstants.ClientId,
                TestConstants.UserIdentifier).ToString());

            Task<AuthenticationResult> task = app.AcquireTokenSilentAsync(TestConstants.Scope.ToArray(), new User()
            {
                DisplayableId = TestConstants.DisplayableId,
                Identifier = TestConstants.UserIdentifier,
            }, app.Authority, false);

            AuthenticationResult result = task.Result;
            Assert.IsNotNull(result);
            Assert.AreEqual(TestConstants.DisplayableId, result.User.DisplayableId);
            Assert.AreEqual(TestConstants.Scope.AsSingleString(), result.Scopes.AsSingleString());
            Assert.IsNotNull(_myReceiver.EventsReceived.Find(anEvent =>  // Expect finding such an event
                anEvent[EventBase.EventNameKey].EndsWith("api_event") && anEvent[ApiEvent.WasSuccessfulKey] == "true"
                && anEvent[ApiEvent.ApiIdKey] == "31"));
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
                        TestConstants.Scope.ToArray())
            });

            Task<AuthenticationResult> task = app.AcquireTokenSilentAsync(TestConstants.Scope.ToArray(),
                new User()
                {
                    DisplayableId = TestConstants.DisplayableId,
                    Identifier = TestConstants.UserIdentifier,
                }, null, true);
            AuthenticationResult result = task.Result;
            Assert.IsNotNull(result);
            Assert.AreEqual(TestConstants.DisplayableId, result.User.DisplayableId);
            Assert.AreEqual(
                TestConstants.Scope.ToArray().AsSingleString(),
                result.Scopes.AsSingleString());

            Assert.AreEqual(2, cache.TokenCacheAccessor.AccessTokenCacheDictionary.Count);
            Assert.AreEqual(1, cache.TokenCacheAccessor.RefreshTokenCacheDictionary.Count);

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
                            DisplayableId = TestConstants.DisplayableId,
                            Identifier = TestConstants.UserIdentifier,
                        }, app.Authority, false);
                AuthenticationResult result = task.Result;
                Assert.Fail("MsalUiRequiredException was expected");
            }
            catch (AggregateException ex)
            {
                Assert.IsNotNull(ex.InnerException);
                Assert.IsTrue(ex.InnerException is MsalUiRequiredException);
                var msalExc = (MsalUiRequiredException)ex.InnerException;
                Assert.AreEqual(msalExc.ErrorCode, MsalUiRequiredException.InvalidGrantError);
            }

            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        [ExpectedException(typeof(HttpRequestException),
            "Cannot write more bytes to the buffer than the configured maximum buffer size: 1048576.")]
        public async Task HttpRequestExceptionIsNotSuppressedAsync()
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

            await app.AcquireTokenAsync(TestConstants.Scope.ToArray()).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public async Task AuthUiFailedExceptionTestAsync()
        {
            cache.ClientId = TestConstants.ClientId;
            PublicClientApplication app = new PublicClientApplication(TestConstants.ClientId)
            {
                UserTokenCache = cache
            };

            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(TestConstants.AuthorityHomeTenant)
            });

            // repeat interactive call and pass in the same user
            MockHelpers.ConfigureMockWebUI(new MockWebUI()
            {
                ExceptionToThrow =
                    new MsalClientException(MsalClientException.AuthenticationUiFailedError,
                        "Failed to invoke webview", new Exception("some-inner-Exception"))
            });

            try
            {
                AuthenticationResult result = await app.AcquireTokenAsync(TestConstants.Scope).ConfigureAwait(false);
                Assert.Fail("API should have failed here");
            }
            catch (MsalClientException exc)
            {
                Assert.IsNotNull(exc);
                Assert.AreEqual(MsalClientException.AuthenticationUiFailedError, exc.ErrorCode);
                Assert.AreEqual("some-inner-Exception", exc.InnerException.Message);
            }
            finally
            {
                Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
            }
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void GetUserTest()
        {
            var app = new PublicClientApplication(TestConstants.ClientId);
            var users = app.Users;
            Assert.IsNotNull(users);
            // no users in the cache
            Assert.AreEqual(0, users.Count());

            var fetchedUser = app.GetUser(null);
            Assert.IsNull(fetchedUser);

            fetchedUser = app.GetUser("");
            Assert.IsNull(fetchedUser);

            TokenCacheHelper.AddRefreshTokenToCache(app.UserTokenCache.TokenCacheAccessor, TestConstants.Uid,
                TestConstants.Utid, TestConstants.Name);
            TokenCacheHelper.AddRefreshTokenToCache(app.UserTokenCache.TokenCacheAccessor, TestConstants.Uid + "1",
                TestConstants.Utid, TestConstants.Name + "1");

            users = app.Users;
            Assert.IsNotNull(users);
            // two users in the cache
            Assert.AreEqual(2, users.Count());

            var userToFind = users.First();

            fetchedUser = app.GetUser(userToFind.Identifier);

            Assert.AreEqual(userToFind.DisplayableId, fetchedUser.DisplayableId);
            Assert.AreEqual(userToFind.Identifier, fetchedUser.Identifier);
            Assert.AreEqual(userToFind.IdentityProvider, fetchedUser.IdentityProvider);
            Assert.AreEqual(userToFind.Name, fetchedUser.Name);
        }

        [TestMethod]
        [Description("Test for AcquireToken with user canceling authentication")]
        public async Task AcquireTokenWithAuthenticationCanceledTestAsync()
        {
            PublicClientApplication app = new PublicClientApplication(TestConstants.ClientId);

            // Interactive call and user cancels authentication
            MockWebUI ui = new MockWebUI()
            {
                MockResult = new AuthorizationResult(AuthorizationStatus.UserCancel,
                    TestConstants.AuthorityHomeTenant + "?error=user_canceled")
            };

            MockHelpers.ConfigureMockWebUI(ui);

            try
            {
                AuthenticationResult result = await app.AcquireTokenAsync(TestConstants.Scope).ConfigureAwait(false);
            }
            catch (MsalClientException exc)
            {
                Assert.IsNotNull(exc);
                Assert.AreEqual("authentication_canceled", exc.ErrorCode);
                Assert.AreEqual("User canceled authentication", exc.Message);
            }
            finally
            {
                Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
            }
        }

        [TestMethod]
        [Description("Test for AcquireToken with access denied error")]
        public async Task AcquireTokenWithAccessDeniedErrorTestAsync()
        {
            PublicClientApplication app = new PublicClientApplication(TestConstants.ClientId);

            // Interactive call and authentication fails with access denied
            MockWebUI ui = new MockWebUI()
            {
                MockResult = new AuthorizationResult(AuthorizationStatus.ProtocolError,
                    TestConstants.AuthorityHomeTenant + "?error=access_denied")
            };

            MockHelpers.ConfigureMockWebUI(ui);

            try
            {
                AuthenticationResult result = await app.AcquireTokenAsync(TestConstants.Scope).ConfigureAwait(false);
            }
            catch (MsalServiceException exc)
            {
                Assert.IsNotNull(exc);
                Assert.AreEqual("access_denied", exc.ErrorCode);
            }
            finally
            {
                Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
            }
        }
    }
}