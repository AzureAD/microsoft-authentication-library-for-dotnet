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

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Http;
using Microsoft.Identity.Core.Instance;
using Microsoft.Identity.Core.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security;
using System.Threading.Tasks;
using Test.Microsoft.Identity.Core.Unit.Mocks;
using Test.MSAL.NET.Unit.Mocks;

namespace Test.MSAL.NET.Unit
{
    [TestClass]
    public class IWAUsernamePasswordTests
    {
        TokenCache cache;
        private MyReceiver _myReceiver = new MyReceiver();
        SecureString secureString;

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
            CreateSecureString();
        }

        internal void AddMockResponseForInstanceDisovery()
        {
            HttpMessageHandlerFactory.AddMockHandler(
                MockHelpers.CreateInstanceDiscoveryMockHandler(
                    TestConstants.GetDiscoveryEndpoint(TestConstants.AuthorityCommonTenant)));
        }

        internal void CreateSecureString()
        {
            secureString = null;
            SecureString str = new SecureString();
            str.AppendChar('x');
            str.MakeReadOnly();
            secureString = str;
        }

        internal void AddMockResponseForFederatedAccounts()
        {
            MockWebUI ui = new MockWebUI()
            {
                MockResult = new AuthorizationResult(AuthorizationStatus.Success,
                TestConstants.AuthorityOrganizationsTenant + "?code=some-code")
            };

            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(TestConstants.AuthorityOrganizationsTenant)
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
                Url = "https://msft.sts.microsoft.com/adfs/services/trust/2005/usernamemixed",
                Method = HttpMethod.Post,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(File.ReadAllText(@"MsalResource\WsTrustResponse.xml"))
                }
            });

            // AAD
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Url = "https://login.microsoftonline.com/organizations/oauth2/v2.0/token",
                Method = HttpMethod.Post,
                PostData = new Dictionary<string, string>()
                {
                    {"grant_type", "urn:ietf:params:oauth:grant-type:saml1_1-bearer"},
                    {"scope", "openid offline_access profile r1/scope1 r1/scope2"}
                },
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
            });
        }

        private void AddMockResponseforManagedAccounts()
        {
            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(TestConstants.AuthorityOrganizationsTenant)
            });

            // user realm discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content =
                        new StringContent("{\"ver\":\"1.0\",\"account_type\":\"Managed\",\"domain_name\":\"id.com\"}")
                },
                QueryParams = new Dictionary<string, string>()
                {
                    {"api-version", "1.0"}
                }
            });
        }

        [TestCleanup]
        public void TestCleanup()
        {
            cache.tokenCacheAccessor.AccessTokenCacheDictionary.Clear();
            cache.tokenCacheAccessor.RefreshTokenCacheDictionary.Clear();
        }

        [TestMethod]
        [TestCategory("IWAUsernamePasswordTests")]
        [DeploymentItem(@"Resources\TestMex.xml", "MsalResource")]
        [DeploymentItem(@"Resources\WsTrustResponse13.xml", "MsalResource")]
        public async Task AcquireTokenByIntegratedWindowsAuthTest()
        {
            MockWebUI ui = new MockWebUI()
            {
                MockResult = new AuthorizationResult(AuthorizationStatus.Success,
                  TestConstants.AuthorityHomeTenant + "?code=some-code")
            };

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
            AuthenticationResult result = await app.AcquireTokenByIntegratedWindowsAuthAsync(TestConstants.Scope, TestConstants.User.Username).ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.AreEqual("some-access-token", result.AccessToken);
            Assert.IsNotNull(result.Account);
            Assert.AreEqual(TestConstants.DisplayableId, result.Account.Username);

            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }

        [TestMethod]
        [TestCategory("IWAUsernamePasswordTests")]
        [DeploymentItem(@"Resources\TestMex.xml", "MsalResource")]
        [DeploymentItem(@"Resources\WsTrustResponse.xml", "MsalResource")]
        public async Task FederatedUsernamePasswordWithSecureStringAcquireTokenTest()
        {
            AddMockResponseForFederatedAccounts();

            PublicClientApplication app = new PublicClientApplication(TestConstants.ClientId);

            AuthenticationResult result = await app.AcquireTokenByUsernamePasswordAsync(TestConstants.Scope, TestConstants.User.Username, secureString).ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.AreEqual("some-access-token", result.AccessToken);
            Assert.IsNotNull(result.Account);
            Assert.AreEqual(TestConstants.User.Username, result.Account.Username);

            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }

        [TestMethod]
        [TestCategory("IWAUsernamePasswordTests")]
        [DeploymentItem(@"Resources\TestMex.xml", "MsalResource")]
        public void MexEndpointFailsToResolveTest()
        {
            MockWebUI ui = new MockWebUI()
            {
                MockResult = new AuthorizationResult(AuthorizationStatus.Success,
                 TestConstants.AuthorityOrganizationsTenant + "?code=some-code")
            };

            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(TestConstants.AuthorityOrganizationsTenant)
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
                    Content = new StringContent(File.ReadAllText(@"MsalResource\TestMex.xml").Replace("<wsp:All>", " "))
                }
            });

            cache.ClientId = TestConstants.ClientId;
            PublicClientApplication app = new PublicClientApplication(TestConstants.ClientId)
            {
                UserTokenCache = cache
            };

            // Call aquire token, Mex parser fails
            var result = AssertException.TaskThrows<MsalException>(async () =>
                await app.AcquireTokenByUsernamePasswordAsync(TestConstants.Scope, TestConstants.User.Username, secureString).ConfigureAwait(false));

            // Check exception message
            Assert.AreEqual("Parsing WS metadata exchange failed", result.Message);
            Assert.AreEqual("parsing_ws_metadata_exchange_failed", result.ErrorCode);

            // There should be no cached entries.
            Assert.AreEqual(0, cache.tokenCacheAccessor.AccessTokenCacheDictionary.Count);

            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }

        [TestMethod]
        [TestCategory("IWAUsernamePasswordTests")]
        [DeploymentItem(@"Resources\TestMex.xml", "MsalResource")]
        public void MexDoesNotReturnAuthEndpointTest()
        {
            MockWebUI ui = new MockWebUI()
            {
                MockResult = new AuthorizationResult(AuthorizationStatus.Success,
                 TestConstants.AuthorityOrganizationsTenant + "?code=some-code")
            };

            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(TestConstants.AuthorityOrganizationsTenant)
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

            // Mex does not return integrated auth endpoint (../13/windowstransport)
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Url = "https://msft.sts.microsoft.com/adfs/services/trust/13/windowstransport",
                Method = HttpMethod.Post,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent("Not found")
                }
            });

            cache.ClientId = TestConstants.ClientId;
            PublicClientApplication app = new PublicClientApplication(TestConstants.ClientId)
            {
                UserTokenCache = cache
            };

            // Call aquire token, endpoint not found
            var result = AssertException.TaskThrows<MsalException>(async () =>
                await app.AcquireTokenByUsernamePasswordAsync(TestConstants.Scope, TestConstants.User.Username, secureString).ConfigureAwait(false));

            // Check exception message
            Assert.AreEqual(CoreErrorCodes.ParsingWsTrustResponseFailed, result.ErrorCode);

            // There should be no cached entries.
            Assert.AreEqual(0, cache.tokenCacheAccessor.AccessTokenCacheDictionary.Count);

            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }

        [TestMethod]
        [TestCategory("IWAUsernamePasswordTests")]
        public void MexParsingFailsTest()
        {
            MockWebUI ui = new MockWebUI()
            {
                MockResult = new AuthorizationResult(AuthorizationStatus.Success,
                 TestConstants.AuthorityOrganizationsTenant + "?code=some-code")
            };

            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(TestConstants.AuthorityOrganizationsTenant)
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
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent("Not Found")
                }
            });

            cache.ClientId = TestConstants.ClientId;
            PublicClientApplication app = new PublicClientApplication(TestConstants.ClientId)
            {
                UserTokenCache = cache
            };

            // Call aquire token
            var result = AssertException.TaskThrows<MsalException>(async () =>
                await app.AcquireTokenByUsernamePasswordAsync(TestConstants.Scope, TestConstants.User.Username, secureString).ConfigureAwait(false));

            // Check inner exception
            Assert.AreEqual("Response status code does not indicate success: 404 (NotFound).", result.Message);

            // There should be no cached entries.
            Assert.AreEqual(0, cache.tokenCacheAccessor.AccessTokenCacheDictionary.Count);

            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }

        [TestMethod]
        [TestCategory("IWAUsernamePasswordTests")]
        [DeploymentItem(@"Resources\TestMex.xml", "MsalResource")]
        [DeploymentItem(@"Resources\WsTrustResponse.xml", "MsalResource")]
        public void FederatedUsernameNullPasswordTest()
        {
            MockWebUI ui = new MockWebUI()
            {
                MockResult = new AuthorizationResult(AuthorizationStatus.Success,
                TestConstants.AuthorityOrganizationsTenant + "?code=some-code")
            };

            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(TestConstants.AuthorityOrganizationsTenant)
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

            // Mex does not return integrated auth endpoint (.../13/windowstransport)
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Url = "https://msft.sts.microsoft.com/adfs/services/trust/13/windowstransport",
                Method = HttpMethod.Post,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("Not found")
                }
            });

            cache.ClientId = TestConstants.ClientId;
            PublicClientApplication app = new PublicClientApplication(TestConstants.ClientId)
            {
                UserTokenCache = cache
            };

            SecureString str = null;

            // Call acquire token
            var result = AssertException.TaskThrows<MsalException>(async () =>
                await app.AcquireTokenByUsernamePasswordAsync(TestConstants.Scope, TestConstants.User.Username, str).ConfigureAwait(false));

            // Check inner exception
            Assert.AreEqual(CoreErrorCodes.ParsingWsTrustResponseFailed, result.ErrorCode);

            // There should be no cached entries.
            Assert.AreEqual(0, cache.tokenCacheAccessor.AccessTokenCacheDictionary.Count);

            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }

        [TestMethod]
        [TestCategory("IWAUsernamePasswordTests")]
        public async Task ManagedUsernameSecureStringPasswordAcquireTokenTest()
        {
            AddMockResponseforManagedAccounts();

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                PostDataObject = new Dictionary<string, object>()
                {
                    {"grant_type", "password"},
                    {"username", TestConstants.User.Username},
                    {"password", secureString}
                }
            });

            PublicClientApplication app = new PublicClientApplication(TestConstants.ClientId);

            AuthenticationResult result = await app.AcquireTokenByUsernamePasswordAsync(TestConstants.Scope, TestConstants.User.Username, secureString).ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.AreEqual("some-access-token", result.AccessToken);
            Assert.IsNotNull(result.Account);
            Assert.AreEqual(TestConstants.User.Username, result.Account.Username);

            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }

        [TestMethod]
        [TestCategory("IWAUsernamePasswordTests")]
        public void ManagedUsernameNoPasswordAcquireTokenTest()
        {
            AddMockResponseforManagedAccounts();

            cache.ClientId = TestConstants.ClientId;
            PublicClientApplication app = new PublicClientApplication(TestConstants.ClientId)
            {
                UserTokenCache = cache
            };

            SecureString str = null;

            // Call acquire token
            var result = AssertException.TaskThrows<MsalException>(async () =>
                await app.AcquireTokenByUsernamePasswordAsync(TestConstants.Scope, TestConstants.User.Username, str).ConfigureAwait(false));

            // Check error code
            Assert.AreEqual(MsalError.PasswordRequiredForManagedUserError, result.ErrorCode);

            // There should be no cached entries.
            Assert.AreEqual(0, cache.tokenCacheAccessor.AccessTokenCacheDictionary.Count);

            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }

        [TestMethod]
        [TestCategory("IWAUsernamePasswordTests")]
        public void ManagedUsernameIncorrectPasswordAcquireTokenTest()
        {
            AddMockResponseforManagedAccounts();

            SecureString str = new SecureString();
            str.AppendChar('y');
            str.MakeReadOnly();

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateInvalidGrantTokenResponseMessage(),
                PostDataObject = new Dictionary<string, object>()
                {
                    {"grant_type", "password"},
                    {"username", TestConstants.User.Username},
                    {"password", secureString}
                }
            });

            cache.ClientId = TestConstants.ClientId;
            PublicClientApplication app = new PublicClientApplication(TestConstants.ClientId)
            {
                UserTokenCache = cache
            };

            // Call acquire token
            var result = AssertException.TaskThrows<MsalException>(async () =>
                await app.AcquireTokenByUsernamePasswordAsync(TestConstants.Scope, TestConstants.User.Username, str).ConfigureAwait(false));

            // Check error code
            Assert.AreEqual(CoreErrorCodes.InvalidGrantError, result.ErrorCode);

            // There should be no cached entries.
            Assert.AreEqual(0, cache.tokenCacheAccessor.AccessTokenCacheDictionary.Count);

            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }
    }
}
