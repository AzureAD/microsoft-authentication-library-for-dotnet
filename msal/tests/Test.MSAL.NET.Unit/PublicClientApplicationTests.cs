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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Core.Cache;
using Microsoft.Identity.Core.Helpers;
using Microsoft.Identity.Core.Http;
using Microsoft.Identity.Core.Instance;
using Microsoft.Identity.Core.OAuth2;
using Microsoft.Identity.Core.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Microsoft.Identity.Core.Telemetry;
using Test.MSAL.NET.Unit.Mocks;
using Test.Microsoft.Identity.Core.Unit.Mocks;

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
            ModuleInitializer.ForceModuleInitializationTestOnly();

            cache = new TokenCache();
            Authority.ValidatedAuthorities.Clear();
            HttpClientFactory.ReturnHttpClientForMocks = true;
            HttpMessageHandlerFactory.ClearMockHandlers();
            Telemetry.GetInstance().RegisterReceiver(_myReceiver.OnEvents);

            AadInstanceDiscovery.Instance.Cache.Clear();
            AddMockResponseForInstanceDisovery();
        }

        internal void AddMockResponseForInstanceDisovery()
        {
            HttpMessageHandlerFactory.AddMockHandler(
                MockHelpers.CreateInstanceDiscoveryMockHandler(
                    TestConstants.GetDiscoveryEndpoint(TestConstants.AuthorityCommonTenant)));
        }

        [TestCleanup]
        public void TestCleanup()
        {
            cache.tokenCacheAccessor.AccessTokenCacheDictionary.Clear();
            cache.tokenCacheAccessor.RefreshTokenCacheDictionary.Clear();
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
        public async Task NoStateReturnedTest()
        {
            PublicClientApplication app = new PublicClientApplication(TestConstants.ClientId);

            MockWebUI ui = new MockWebUI()
            {
                AddStateInAuthorizationResult = false,
                MockResult = new AuthorizationResult(AuthorizationStatus.Success,
                    TestConstants.AuthorityHomeTenant + "?code=some-code")
            };

            MsalMockHelpers.ConfigureMockWebUI(ui);

            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(TestConstants.AuthorityHomeTenant)
            });

            try
            {
                AuthenticationResult result = await app.AcquireTokenAsync(TestConstants.Scope);
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
        public async Task DifferentStateReturnedTest()
        {
            PublicClientApplication app = new PublicClientApplication(TestConstants.ClientId);

            MockWebUI ui = new MockWebUI()
            {
                AddStateInAuthorizationResult = false,
                MockResult = new AuthorizationResult(AuthorizationStatus.Success,
                    TestConstants.AuthorityHomeTenant + "?code=some-code&state=mistmatched")
            };

            MsalMockHelpers.ConfigureMockWebUI(ui);

            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(TestConstants.AuthorityHomeTenant)
            });

            try
            {
                AuthenticationResult result = await app.AcquireTokenAsync(TestConstants.Scope);
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
        public async Task AcquireTokenNoClientInfoReturnedTest()
        {
            PublicClientApplication app = new PublicClientApplication(TestConstants.ClientId);

            MockWebUI ui = new MockWebUI()
            {
                MockResult = new AuthorizationResult(AuthorizationStatus.Success,
                    TestConstants.AuthorityHomeTenant + "?code=some-code")
            };

            MsalMockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
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

            MsalMockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
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
            Assert.IsNotNull(result.Account);
            Assert.AreEqual(TestConstants.UniqueId, result.UniqueId);
            Assert.AreEqual(TestConstants.CreateUserIdentifer(), result.Account.HomeAccountId);
            Assert.AreEqual(TestConstants.DisplayableId, result.Account.Username);

            // repeat interactive call and pass in the same user
            MsalMockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
                app.RedirectUri + "?code=some-code"));

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
            });

            result = app.AcquireTokenAsync(TestConstants.Scope).Result;
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Account);
            Assert.AreEqual(TestConstants.UniqueId, result.UniqueId);
            Assert.AreEqual(TestConstants.CreateUserIdentifer(), result.Account.HomeAccountId);
            Assert.AreEqual(TestConstants.DisplayableId, result.Account.Username);

            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        [DeploymentItem(@"Resources\TestMex.xml", "MsalResource")]
        [DeploymentItem(@"Resources\WsTrustResponse13.xml", "MsalResource")]
        public async Task AcquireTokenByWindowsIntegratedAuthTest()
        {
            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(TestConstants.AuthorityHomeTenant)
            });

            // user realm discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"ver\":\"1.0\",\"account_type\":\"federated\",\"domain_name\":\"microsoft.com\"," +
                                                "\"federation_protocol\":\"WSTrust\",\"federation_metadata_url\":" +
                                                "\"https://msft.sts.microsoft.com/adfs/services/trust/mex\"," +
                                                "\"federation_active_auth_url\":\"https://msft.sts.microsoft.com/adfs/services/trust/2005/usernamemixed\"" +
                                                ",\"cloud_instance_name\":\"login.microsoftonline.com\"}")
                }
            });

            // MEX
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Url = "https://msft.sts.microsoft.com/adfs/services/trust/mex",
                Method = HttpMethod.Get,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(File.ReadAllText(@"MsalResource\TestMex.xml"))
                }
            });

            // WsTrust
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Url = "https://msft.sts.microsoft.com/adfs/services/trust/13/windowstransport",
                Method = HttpMethod.Post,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(File.ReadAllText(@"MsalResource\WsTrustResponse13.xml"))
                }
            });

            // AAD
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Url = "https://login.microsoftonline.com/home/oauth2/v2.0/token",
                Method = HttpMethod.Post,
                PostData = new Dictionary<string, string>()
                {
                    {"grant_type", "urn:ietf:params:oauth:grant-type:saml1_1-bearer"},
                    {"scope", "offline_access openid profile r1/scope1 r1/scope2"}
                },
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
            });

            PublicClientApplication app = new PublicClientApplication(TestConstants.ClientId);
            AuthenticationResult result = await app.AcquireTokenByWindowsIntegratedAuthAsync(TestConstants.Scope, TestConstants.UserIdentifier).ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.AreEqual("some-access-token", result.AccessToken);
            Assert.IsNotNull(result.User);
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

            MsalMockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
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
            Assert.IsNotNull(result.Account);
            Assert.AreEqual(TestConstants.UniqueId, result.UniqueId);
            Assert.AreEqual(TestConstants.CreateUserIdentifer(), result.Account.HomeAccountId);
            Assert.AreEqual(TestConstants.DisplayableId, result.Account.Username);
            Assert.AreEqual(TestConstants.Utid, result.TenantId);

            // repeat interactive call and pass in the same user
            MsalMockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
                app.RedirectUri + "?code=some-code"));

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(TestConstants.Scope.ToString(),
                    MockHelpers.CreateIdToken(TestConstants.UniqueId + "more", TestConstants.DisplayableId + "more",
                        TestConstants.Utid + "more"),
                    MockHelpers.CreateClientInfo(TestConstants.Uid + "more",
                        TestConstants.Utid + "more"))
            });

            result = app.AcquireTokenAsync(TestConstants.Scope).Result;
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Account);
            Assert.AreEqual(TestConstants.UniqueId + "more", result.UniqueId);
            Assert.AreEqual(TestConstants.CreateUserIdentifer(TestConstants.Uid + "more",
                TestConstants.Utid + "more"), result.Account.HomeAccountId);
            Assert.AreEqual(TestConstants.DisplayableId + "more", result.Account.Username);
            Assert.AreEqual(TestConstants.Utid + "more", result.TenantId);

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

            MsalMockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
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
            Assert.IsNotNull(result.Account);
            Assert.AreEqual(TestConstants.UniqueId, result.UniqueId);
            Assert.AreEqual(TestConstants.CreateUserIdentifer(), result.Account.HomeAccountId);
            Assert.AreEqual(TestConstants.DisplayableId, result.Account.Username);

            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");

            var dict = new Dictionary<string, string>();
            dict[OAuth2Parameter.DomainReq] = TestConstants.Utid;
            dict[OAuth2Parameter.LoginReq] = TestConstants.Uid;

            // repeat interactive call and pass in the same user
            MsalMockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
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
                result = app.AcquireTokenAsync(TestConstants.Scope, result.Account, UIBehavior.SelectAccount, null).Result;
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

            var users = app.GetAccountsAsync().Result;
            Assert.AreEqual(1, users.Count());
            Assert.AreEqual(1, cache.tokenCacheAccessor.AccessTokenCacheDictionary.Count);

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

            MsalMockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
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
            Assert.IsNotNull(result.Account);
            Assert.AreEqual(TestConstants.UniqueId, result.UniqueId);
            Assert.AreEqual(TestConstants.CreateUserIdentifer(), result.Account.HomeAccountId);
            Assert.AreEqual(TestConstants.DisplayableId, result.Account.Username);
            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");

            // repeat interactive call and pass in the same user
            MsalMockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
                app.RedirectUri + "?code=some-code"));

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(TestConstants.Scope.AsSingleString(),
                    MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId),
                    MockHelpers.CreateClientInfo(TestConstants.Uid, TestConstants.Utid + "more"))
            });

            result = app.AcquireTokenAsync(TestConstants.Scope, (IAccount)null, UIBehavior.SelectAccount, null).Result;
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Account);
            Assert.AreEqual(TestConstants.UniqueId, result.UniqueId);
            Assert.AreEqual(TestConstants.CreateUserIdentifer(TestConstants.Uid, TestConstants.Utid + "more"),
                result.Account.HomeAccountId);
            Assert.AreEqual(TestConstants.DisplayableId, result.Account.Username);
            var users = app.GetAccountsAsync().Result;
            Assert.AreEqual(2, users.Count());
            Assert.AreEqual(2, cache.tokenCacheAccessor.AccessTokenCacheDictionary.Count);
            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void GetUsersTest()
        {
            PublicClientApplication app = new PublicClientApplication(TestConstants.ClientId);
            IEnumerable<IAccount> users = app.GetAccountsAsync().Result;
            Assert.IsNotNull(users);
            Assert.IsFalse(users.Any());
            cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };

            app.UserTokenCache = cache;
            TokenCacheHelper.PopulateCache(cache.tokenCacheAccessor);
            users = app.GetAccountsAsync().Result;
            Assert.IsNotNull(users);
            Assert.AreEqual(1, users.Count());

            var atItem = new MsalAccessTokenCacheItem(
                TestConstants.ProductionPrefNetworkEnvironment,
                TestConstants.ClientId,
                "Bearer",
                TestConstants.Scope.AsSingleString(),
                TestConstants.Utid,
                null,
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(3600)),
                MockHelpers.CreateClientInfo());

            atItem.Secret = atItem.GetKey().ToString();
            cache.tokenCacheAccessor.AccessTokenCacheDictionary[atItem.GetKey().ToString()] =
                JsonHelper.SerializeToJson(atItem);


            // another cache entry for different uid. user count should be 2.

            MsalRefreshTokenCacheItem rtItem = new MsalRefreshTokenCacheItem
                (TestConstants.ProductionPrefNetworkEnvironment, TestConstants.ClientId, "someRT", MockHelpers.CreateClientInfo("uId1", "uTId1"));

            cache.tokenCacheAccessor.RefreshTokenCacheDictionary[rtItem.GetKey().ToString()] =
                JsonHelper.SerializeToJson(rtItem);

            MsalIdTokenCacheItem idTokenCacheItem = new MsalIdTokenCacheItem(
                TestConstants.ProductionPrefNetworkEnvironment,
                TestConstants.ClientId,
                MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId),
                MockHelpers.CreateClientInfo("uId1", "uTId1"),
                "uTId1");

            cache.tokenCacheAccessor.IdTokenCacheDictionary[idTokenCacheItem.GetKey().ToString()] = JsonHelper.SerializeToJson(idTokenCacheItem);


            MsalAccountCacheItem accountCacheItem = new MsalAccountCacheItem
                (TestConstants.ProductionPrefNetworkEnvironment, null, MockHelpers.CreateClientInfo("uId1", "uTId1"), null, null, "uTId1");

            cache.tokenCacheAccessor.AccountCacheDictionary[accountCacheItem.GetKey().ToString()] = JsonHelper.SerializeToJson(accountCacheItem);


            Assert.AreEqual(2, cache.tokenCacheAccessor.RefreshTokenCacheDictionary.Count);
            users = app.GetAccountsAsync().Result;
            Assert.IsNotNull(users);
            Assert.AreEqual(2, users.Count());

            // another cache entry for different environment. user count should still be 2. Sovereign cloud user must not be returned
            rtItem = new MsalRefreshTokenCacheItem(TestConstants.SovereignEnvironment, TestConstants.ClientId, "someRT",
                MockHelpers.CreateClientInfo(TestConstants.Uid + "more1", TestConstants.Utid));

            cache.tokenCacheAccessor.RefreshTokenCacheDictionary[rtItem.GetKey().ToString()] =
                JsonHelper.SerializeToJson(rtItem);
            Assert.AreEqual(3, cache.tokenCacheAccessor.RefreshTokenCacheDictionary.Count);
            users = app.GetAccountsAsync().Result;
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
            TokenCacheHelper.PopulateCache(cache.tokenCacheAccessor);

            foreach (var user in app.GetAccountsAsync().Result)
            {
                app.RemoveAsync(user).Wait();
            }

            Assert.AreEqual(0, app.UserTokenCache.tokenCacheAccessor.AccessTokenCacheDictionary.Count);
            Assert.AreEqual(0, app.UserTokenCache.tokenCacheAccessor.RefreshTokenCacheDictionary.Count);
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public async Task AcquireTokenSilentScopeAndEmptyCacheTest()
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
                        new Account(TestConstants.UserIdentifier, TestConstants.DisplayableId, null)).ConfigureAwait(false);
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
        public async Task AcquireTokenSilentScopeAndUserMultipleTokensFoundTest()
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
            TokenCacheHelper.PopulateCache(cache.tokenCacheAccessor);
            try
            {
                AuthenticationResult result = await app.AcquireTokenSilentAsync(TestConstants.Scope.ToArray(),
                        new Account(TestConstants.UserIdentifier, TestConstants.DisplayableId, null)).ConfigureAwait(false);
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
            TokenCacheHelper.PopulateCache(cache.tokenCacheAccessor);
            cache.tokenCacheAccessor.AccessTokenCacheDictionary.Remove(new MsalAccessTokenCacheKey(
                TestConstants.ProductionPrefNetworkEnvironment,
                TestConstants.Utid,
                TestConstants.UserIdentifier.Identifier,
                TestConstants.ClientId,
                TestConstants.ScopeForAnotherResourceStr).ToString());

            Task<AuthenticationResult> task = app.AcquireTokenSilentAsync(TestConstants.ScopeForAnotherResource.ToArray(),
                new Account(TestConstants.UserIdentifier, TestConstants.DisplayableId, null));

            AuthenticationResult result = task.Result;
            Assert.IsNotNull(result);
            Assert.AreEqual(TestConstants.DisplayableId, result.Account.Username);
            Assert.AreEqual(TestConstants.ScopeForAnotherResource.AsSingleString(), result.Scopes.AsSingleString());
            Assert.AreEqual(2, cache.tokenCacheAccessor.GetAllAccessTokensAsString().Count());
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
            TokenCacheHelper.PopulateCache(cache.tokenCacheAccessor);
            cache.tokenCacheAccessor.AccessTokenCacheDictionary.Remove(new MsalAccessTokenCacheKey(
                TestConstants.ProductionPrefNetworkEnvironment,
                TestConstants.Utid,
                TestConstants.UserIdentifier.Identifier,
                TestConstants.ClientId,
                TestConstants.ScopeForAnotherResourceStr).ToString());

            Task<AuthenticationResult> task = app.AcquireTokenSilentAsync(TestConstants.Scope.ToArray(),
                new Account(TestConstants.UserIdentifier, TestConstants.DisplayableId, null));

            AuthenticationResult result = task.Result;
            Assert.IsNotNull(result);
            Assert.AreEqual(TestConstants.DisplayableId, result.Account.Username);
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
            TokenCacheHelper.PopulateCache(cache.tokenCacheAccessor);
            cache.tokenCacheAccessor.AccessTokenCacheDictionary.Remove(new MsalAccessTokenCacheKey(
                TestConstants.ProductionPrefNetworkEnvironment,
                TestConstants.Utid,
                TestConstants.UserIdentifier.Identifier,
                TestConstants.ClientId,
                TestConstants.ScopeForAnotherResourceStr).ToString());

            Task<AuthenticationResult> task = app.AcquireTokenSilentAsync(TestConstants.Scope.ToArray(),
                new Account(TestConstants.UserIdentifier, TestConstants.DisplayableId, null));
            AuthenticationResult result = task.Result;
            Assert.IsNotNull(result);
            Assert.AreEqual(TestConstants.DisplayableId, result.Account.Username);
            Assert.AreEqual(TestConstants.Scope.AsSingleString(), result.Scopes.AsSingleString());
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void AcquireTokenSilentCacheOnlyLookupTest()
        {
            PublicClientApplication app =
                new PublicClientApplication(TestConstants.ClientId, TestConstants.AuthorityTestTenant)
                {
                    ValidateAuthority = false
                };

            cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };

            app.UserTokenCache = cache;
            TokenCacheHelper.PopulateCache(cache.tokenCacheAccessor);
            cache.tokenCacheAccessor.AccessTokenCacheDictionary.Remove(new MsalAccessTokenCacheKey(
                TestConstants.ProductionPrefNetworkEnvironment,
                TestConstants.Utid,
                TestConstants.UserIdentifier.Identifier,
                TestConstants.ClientId,
                TestConstants.ScopeForAnotherResourceStr).ToString());

            Task<AuthenticationResult> task = app.AcquireTokenSilentAsync(TestConstants.Scope.ToArray(),
                new Account(TestConstants.UserIdentifier, TestConstants.DisplayableId, null), app.Authority, false);

            AuthenticationResult result = task.Result;
            Assert.IsNotNull(result);
            Assert.AreEqual(TestConstants.DisplayableId, result.Account.Username);
            Assert.AreEqual(TestConstants.Scope.AsSingleString(), result.Scopes.AsSingleString());
            Assert.IsNotNull(_myReceiver.EventsReceived.Find(anEvent =>  // Expect finding such an event
                anEvent[EventBase.EventNameKey].EndsWith("api_event") && anEvent[ApiEvent.WasSuccessfulKey] == "true"
                && anEvent[ApiEvent.ApiIdKey] == "31"));
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void AcquireTokenSilentForceRefreshTest()
        {
            HttpMessageHandlerFactory.ClearMockHandlers();

            PublicClientApplication app = new PublicClientApplication(TestConstants.ClientId)
            {
                ValidateAuthority = false
            };

            cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };

            app.UserTokenCache = cache;
            TokenCacheHelper.PopulateCache(cache.tokenCacheAccessor);

            HttpMessageHandlerFactory.AddMockHandler(
                MockHelpers.CreateInstanceDiscoveryMockHandler(
                    TestConstants.GetDiscoveryEndpoint(TestConstants.AuthorityCommonTenant)));

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
                        TestConstants.Scope.ToArray())
            });

            Task<AuthenticationResult> task = app.AcquireTokenSilentAsync(TestConstants.Scope.ToArray(),
                new Account(TestConstants.UserIdentifier, TestConstants.DisplayableId, null), null, true);

            AuthenticationResult result = task.Result;
            Assert.IsNotNull(result);
            Assert.AreEqual(TestConstants.DisplayableId, result.Account.Username);
            Assert.AreEqual(
                TestConstants.Scope.ToArray().AsSingleString(),
                result.Scopes.AsSingleString());

            Assert.AreEqual(2, cache.tokenCacheAccessor.AccessTokenCacheDictionary.Count);
            Assert.AreEqual(1, cache.tokenCacheAccessor.RefreshTokenCacheDictionary.Count);

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
            TokenCacheHelper.PopulateCache(cache.tokenCacheAccessor);

            MockHttpMessageHandler mockHandler = new MockHttpMessageHandler
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateInvalidGrantTokenResponseMessage()
            };
            HttpMessageHandlerFactory.AddMockHandler(mockHandler);
            try
            {
                Task<AuthenticationResult> task =
                    app.AcquireTokenSilentAsync(TestConstants.CacheMissScope,
                        new Account(TestConstants.UserIdentifier, TestConstants.DisplayableId, null),
                        app.Authority, false);
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

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public async Task AuthUiFailedExceptionTest()
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
            MsalMockHelpers.ConfigureMockWebUI(new MockWebUI()
            {
                ExceptionToThrow =
                    new MsalClientException(MsalClientException.AuthenticationUiFailedError,
                        "Failed to invoke webview", new Exception("some-inner-Exception"))
            });

            try
            {
                AuthenticationResult result = await app.AcquireTokenAsync(TestConstants.Scope);
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
            var users = app.GetAccountsAsync().Result;
            Assert.IsNotNull(users);
            // no users in the cache
            Assert.AreEqual(0, users.Count());

            var fetchedUser = app.GetAccountAsync(null).Result;
            Assert.IsNull(fetchedUser);

            fetchedUser = app.GetAccountAsync("").Result;
            Assert.IsNull(fetchedUser);

            TokenCacheHelper.AddRefreshTokenToCache(app.UserTokenCache.tokenCacheAccessor, TestConstants.Uid,
                TestConstants.Utid, TestConstants.Name);
            TokenCacheHelper.AddAccountToCache(app.UserTokenCache.tokenCacheAccessor, TestConstants.Uid,
                TestConstants.Utid);

            TokenCacheHelper.AddRefreshTokenToCache(app.UserTokenCache.tokenCacheAccessor, TestConstants.Uid + "1",
                TestConstants.Utid, TestConstants.Name + "1");
            TokenCacheHelper.AddAccountToCache(app.UserTokenCache.tokenCacheAccessor, TestConstants.Uid + "1",
                TestConstants.Utid);

            users = app.GetAccountsAsync().Result;
            Assert.IsNotNull(users);
            // two users in the cache
            Assert.AreEqual(2, users.Count());

            var userToFind = users.First();

            fetchedUser = app.GetAccountAsync(userToFind.HomeAccountId.Identifier).Result;

            Assert.AreEqual(userToFind.Username, fetchedUser.Username);
            Assert.AreEqual(userToFind.HomeAccountId, fetchedUser.HomeAccountId);
            Assert.AreEqual(userToFind.Environment, fetchedUser.Environment);
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

            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(TestConstants.AuthorityHomeTenant)
            });

            MsalMockHelpers.ConfigureMockWebUI(ui);

            try
            {
                AuthenticationResult result = await app.AcquireTokenAsync(TestConstants.Scope).ConfigureAwait(false);
            }
            catch (MsalClientException exc)
            {
                Assert.IsNotNull(exc);
                Assert.AreEqual("authentication_canceled", exc.ErrorCode);
                Assert.IsNotNull(_myReceiver.EventsReceived.Find(anEvent =>  // Expect finding such an event
                anEvent[EventBase.EventNameKey].EndsWith("ui_event") && anEvent[UiEvent.UserCancelledKey] == "true"));
                return;
            }
            finally
            {
                Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
            }

            Assert.Fail("Should not reach here. Exception was not thrown.");
        }

        [TestMethod]
        [Description("Test for AcquireToken with access denied error. This error will occur if" +
            "user cancels authentication with embedded webview")]
        public async Task AcquireTokenWithAccessDeniedErrorTestAsync()
        {
            PublicClientApplication app = new PublicClientApplication(TestConstants.ClientId);

            // Interactive call and authentication fails with access denied
            MockWebUI ui = new MockWebUI()
            {
                MockResult = new AuthorizationResult(AuthorizationStatus.ProtocolError,
                    TestConstants.AuthorityHomeTenant + "?error=access_denied")
            };

            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(TestConstants.AuthorityHomeTenant)
            });

            MsalMockHelpers.ConfigureMockWebUI(ui);

            try
            {
                AuthenticationResult result = await app.AcquireTokenAsync(TestConstants.Scope).ConfigureAwait(false);
            }
            catch (MsalServiceException exc)
            {
                Assert.IsNotNull(exc);
                Assert.AreEqual("access_denied", exc.ErrorCode);
                Assert.IsNotNull(_myReceiver.EventsReceived.Find(anEvent =>  // Expect finding such an event
                anEvent[EventBase.EventNameKey].EndsWith("ui_event") && anEvent[UiEvent.AccessDeniedKey] == "true"));
                return;
            }
            finally
            {
                Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
            }

            Assert.Fail("Should not reach here. Exception was not thrown.");
        }

        [TestMethod]
        [Description("ClientApplicationBase.GetAuthoriy tests")]
        public void GetAuthority_AccountWithNullIdPassed_CommonAuthorityUsed()
        {
            PublicClientApplication app = new PublicClientApplication(TestConstants.ClientId);

            var authoriy = app.GetAuthority(new Account(null, TestConstants.Name, TestConstants.ProductionPrefNetworkEnvironment));
            Assert.AreEqual(ClientApplicationBase.DefaultAuthority, authoriy.CanonicalAuthority);
        }

        [TestMethod]
        [Description("ClientApplicationBase.GetAuthoriy tests")]
        public void GetAuthority_AccountWithIdPassed_TenantedAuthorityUsed()
        {
            PublicClientApplication app = new PublicClientApplication(TestConstants.ClientId);

            var authoriy = app.GetAuthority(
                new Account(
                    new AccountId("objectId." + TestConstants.Utid, "objectId", TestConstants.Utid),
                    TestConstants.Name,
                    TestConstants.ProductionPrefNetworkEnvironment));

            Assert.AreEqual(TestConstants.AuthorityTestTenant, authoriy.CanonicalAuthority);
        }

        [TestCategory("PublicClientApplicationTests")]
        public async Task AcquireTokenSilentNullAccountErrorTest()
        {
            PublicClientApplication app = new PublicClientApplication(TestConstants.ClientId)
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
                AuthenticationResult result = await app.AcquireTokenSilentAsync(TestConstants.Scope.ToArray(), null).ConfigureAwait(false);
            }
            catch (MsalUiRequiredException exc)
            {
                Assert.IsTrue(exc is MsalUiRequiredException);
                Assert.AreEqual("user_null", MsalUiRequiredException.UserNullError);
            }
        }
    }
}