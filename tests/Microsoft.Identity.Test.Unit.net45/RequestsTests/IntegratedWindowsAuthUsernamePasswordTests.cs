using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Exceptions;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Common.Mocks;
using Microsoft.Identity.Test.Unit.PublicApiTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.RequestsTests
{
    [TestClass]
    public class IntegratedWindowsAuthAndUsernamePasswordTests
    {
        private readonly MyReceiver _myReceiver = new MyReceiver();
        private TokenCache _cache;
        private SecureString _secureString;
        private ITelemetryManager _telemetryManager;

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetStateAndInitMsal();
            _cache = new TokenCache();
            _telemetryManager = new TelemetryManager(_myReceiver);

            new AadInstanceDiscovery(null, _telemetryManager, true);
            new ValidatedAuthoritiesCache(true);

            CreateSecureString();
        }

        internal void CreateSecureString()
        {
            _secureString = null;
            var str = new SecureString();
            str.AppendChar('x');
            str.MakeReadOnly();
            _secureString = str;
        }

        private void AddMockHandlerDefaultUserRealmDiscovery(MockHttpManager httpManager)
        {
            // user realm discovery
            httpManager.AddMockHandler(
                new MockHttpMessageHandler
                {
                    Method = HttpMethod.Get,
                    ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            "{\"ver\":\"1.0\",\"account_type\":\"federated\",\"domain_name\":\"microsoft.com\"," +
                            "\"federation_protocol\":\"WSTrust\",\"federation_metadata_url\":" +
                            "\"https://msft.sts.microsoft.com/adfs/services/trust/mex\"," +
                            "\"federation_active_auth_url\":\"https://msft.sts.microsoft.com/adfs/services/trust/2005/usernamemixed\"" +
                            ",\"cloud_instance_name\":\"login.microsoftonline.com\"}")
                    }
                });
        }

        private void AddMockHandlerDefaultUserRealmDiscovery_ManagedUser(MockHttpManager httpManager)
        {
            // user realm discovery
            httpManager.AddMockHandler(
                new MockHttpMessageHandler
                {
                    Method = HttpMethod.Get,
                    ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            "{\"ver\":\"1.0\"," +
                            "\"account_type\":\"Managed\"," +
                            "\"domain_name\":\"some_domain.onmicrosoft.com\"," +
                            "\"cloud_audience_urn\":\"urn:federation:MicrosoftOnline\"," +
                            "\"cloud_instance_name\":\"login.microsoftonline.com\"}")
                    }
                });
        }

        private void AddMockHandlerMex(MockHttpManager httpManager)
        {
            // MEX
            httpManager.AddMockHandler(
                new MockHttpMessageHandler
                {
                    Url = "https://msft.sts.microsoft.com/adfs/services/trust/mex",
                    Method = HttpMethod.Get,
                    ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            File.ReadAllText(ResourceHelper.GetTestResourceRelativePath("TestMex.xml")))
                    }
                });
        }

        private void AddMockHandlerWsTrustUserName(MockHttpManager httpManager)
        {
            httpManager.AddMockHandler(
                new MockHttpMessageHandler
                {
                    Url = "https://msft.sts.microsoft.com/adfs/services/trust/2005/usernamemixed",
                    Method = HttpMethod.Post,
                    ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            File.ReadAllText(ResourceHelper.GetTestResourceRelativePath(@"WsTrustResponse.xml")))
                    }
                });
        }

        private void AddMockHandlerWsTrustWindowsTransport(MockHttpManager httpManager)
        {
            httpManager.AddMockHandler(
                new MockHttpMessageHandler
                {
                    Url = "https://msft.sts.microsoft.com/adfs/services/trust/13/windowstransport",
                    Method = HttpMethod.Post,
                    ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            File.ReadAllText(ResourceHelper.GetTestResourceRelativePath("WsTrustResponse13.xml")))
                    }
                });
        }

        private void AddMockHandlerAadSuccess(MockHttpManager httpManager, string authority)
        {
            httpManager.AddMockHandler(
                new MockHttpMessageHandler
                {
                    Url = authority + "oauth2/v2.0/token",
                    Method = HttpMethod.Post,
                    PostData = new Dictionary<string, string>
                    {
                        {"grant_type", "urn:ietf:params:oauth:grant-type:saml1_1-bearer"},
                        {"scope", "openid offline_access profile r1/scope1 r1/scope2"}
                    },
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
                });
        }

        internal void AddMockResponseForFederatedAccounts(MockHttpManager httpManager)
        {
            var ui = new MockWebUI
            {
                MockResult = new AuthorizationResult(
                    AuthorizationStatus.Success,
                    MsalTestConstants.AuthorityOrganizationsTenant + "?code=some-code")
            };

            httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityOrganizationsTenant);
            AddMockHandlerDefaultUserRealmDiscovery(httpManager);
            AddMockHandlerMex(httpManager);
            AddMockHandlerWsTrustUserName(httpManager);
            AddMockHandlerAadSuccess(httpManager, MsalTestConstants.AuthorityOrganizationsTenant);
        }

        private void AddMockResponseforManagedAccounts(MockHttpManager httpManager)
        {
            httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityOrganizationsTenant);

            // user realm discovery
            httpManager.AddMockHandler(
                new MockHttpMessageHandler
                {
                    Method = HttpMethod.Get,
                    ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            "{\"ver\":\"1.0\",\"account_type\":\"Managed\",\"domain_name\":\"id.com\"}")
                    },
                    QueryParams = new Dictionary<string, string>
                    {
                        {"api-version", "1.0"}
                    }
                });
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _cache.TokenCacheAccessor.ClearAccessTokens();
            _cache.TokenCacheAccessor.ClearRefreshTokens();
        }

        [TestMethod]
        [TestCategory("IntegratedWindowsAuthAndUsernamePasswordTests")]
        public void AcquireTokenByIntegratedWindowsAuthTest_ManagedUser()
        {
            // Arrange
            using (var httpManager = new MockHttpManager())
            {
                var serviceBundle = ServiceBundle.CreateWithCustomHttpManager(httpManager);
                httpManager.AddInstanceDiscoveryMockHandler();

                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityHomeTenant);
                AddMockHandlerDefaultUserRealmDiscovery_ManagedUser(httpManager);

                var app =
                    new PublicClientApplication(serviceBundle, MsalTestConstants.ClientId,
                        ClientApplicationBase.DefaultAuthority);

                // Act
                var exception = AssertException.TaskThrows<MsalClientException>(
                    async () => await app.AcquireTokenByIntegratedWindowsAuthAsync(
                            MsalTestConstants.Scope,
                            MsalTestConstants.User.Username)
                        .ConfigureAwait(false));

                // Assert
                Assert.AreEqual(MsalError.IntegratedWindowsAuthNotSupportedForManagedUser, exception.ErrorCode);
            }
        }

        [TestMethod]
        [TestCategory("IntegratedWindowsAuthAndUsernamePasswordTests")]
        public void AcquireTokenByIntegratedWindowsAuthTest_UnknownUser()
        {
            // Arrange
            using (var httpManager = new MockHttpManager())
            {
                var serviceBundle = ServiceBundle.CreateWithCustomHttpManager(httpManager);
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityHomeTenant);

                // user realm discovery - unknown user type
                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        Method = HttpMethod.Get,
                        ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                "{\"ver\":\"1.0\"," +
                                "\"account_type\":\"Bogus\"}")
                        }
                    });

                var app =
                    new PublicClientApplication(serviceBundle, MsalTestConstants.ClientId,
                        ClientApplicationBase.DefaultAuthority);

                // Act
                var exception = AssertException.TaskThrows<MsalClientException>(
                    async () => await app.AcquireTokenByIntegratedWindowsAuthAsync(
                            MsalTestConstants.Scope,
                            MsalTestConstants.User.Username)
                        .ConfigureAwait(false));

                // Assert
                Assert.AreEqual(MsalError.UnknownUserType, exception.ErrorCode);
            }
        }


        [TestMethod]
        [TestCategory("IntegratedWindowsAuthAndUsernamePasswordTests")]
        [DeploymentItem(@"Resources\TestMex.xml")]
        [DeploymentItem(@"Resources\WsTrustResponse13.xml")]
        public async Task AcquireTokenByIntegratedWindowsAuthTestAsync()
        {
            var ui = new MockWebUI
            {
                MockResult = new AuthorizationResult(
                    AuthorizationStatus.Success,
                    MsalTestConstants.AuthorityHomeTenant + "?code=some-code")
            };

            using (var httpManager = new MockHttpManager())
            {
                var serviceBundle = ServiceBundle.CreateWithCustomHttpManager(httpManager);
                httpManager.AddInstanceDiscoveryMockHandler();

                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityHomeTenant);
                AddMockHandlerDefaultUserRealmDiscovery(httpManager);
                AddMockHandlerMex(httpManager);
                AddMockHandlerWsTrustWindowsTransport(httpManager);
                AddMockHandlerAadSuccess(httpManager, MsalTestConstants.AuthorityHomeTenant);

                var app =
                    new PublicClientApplication(serviceBundle, MsalTestConstants.ClientId,
                        ClientApplicationBase.DefaultAuthority);
                var result = await app
                    .AcquireTokenByIntegratedWindowsAuthAsync(MsalTestConstants.Scope, MsalTestConstants.User.Username)
                    .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual("some-access-token", result.AccessToken);
                Assert.IsNotNull(result.Account);
                Assert.AreEqual(MsalTestConstants.DisplayableId, result.Account.Username);
            }
        }

#if !WINDOWS_APP // U/P flow not enabled on UWP
        [TestMethod]
        [TestCategory("IntegratedWindowsAuthAndUsernamePasswordTests")]
        [DeploymentItem(@"Resources\TestMex.xml")]
        [DeploymentItem(@"Resources\WsTrustResponse.xml")]
        public async Task FederatedUsernamePasswordWithSecureStringAcquireTokenTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                var serviceBundle = ServiceBundle.CreateWithCustomHttpManager(httpManager);
                httpManager.AddInstanceDiscoveryMockHandler();
                AddMockResponseForFederatedAccounts(httpManager);

                var app =
                    new PublicClientApplication(serviceBundle, MsalTestConstants.ClientId,
                        ClientApplicationBase.DefaultAuthority);

                var result = await app.AcquireTokenByUsernamePasswordAsync(
                    MsalTestConstants.Scope,
                    MsalTestConstants.User.Username,
                    _secureString).ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual("some-access-token", result.AccessToken);
                Assert.IsNotNull(result.Account);
                Assert.AreEqual(MsalTestConstants.User.Username, result.Account.Username);
            }
        }

        [TestMethod]
        [TestCategory("IntegratedWindowsAuthAndUsernamePasswordTests")]
        [DeploymentItem(@"Resources\TestMex.xml")]
        public void MexEndpointFailsToResolveTest()
        {
            var ui = new MockWebUI
            {
                MockResult = new AuthorizationResult(
                    AuthorizationStatus.Success,
                    MsalTestConstants.AuthorityOrganizationsTenant + "?code=some-code")
            };

            using (var httpManager = new MockHttpManager())
            {
                var serviceBundle = ServiceBundle.CreateWithCustomHttpManager(httpManager);
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityOrganizationsTenant);
                AddMockHandlerDefaultUserRealmDiscovery(httpManager);

                // MEX
                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        Url = "https://msft.sts.microsoft.com/adfs/services/trust/mex",
                        Method = HttpMethod.Get,
                        ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                File.ReadAllText(ResourceHelper.GetTestResourceRelativePath("TestMex.xml"))
                                    .Replace("<wsp:All>", " "))
                        }
                    });

                _cache.ClientId = MsalTestConstants.ClientId;
                var app = new PublicClientApplication(serviceBundle, MsalTestConstants.ClientId,
                    ClientApplicationBase.DefaultAuthority)
                {
                    UserTokenCache = _cache
                };

                // Call acquire token, Mex parser fails
                var result = AssertException.TaskThrows<MsalException>(
                    async () => await app.AcquireTokenByUsernamePasswordAsync(
                        MsalTestConstants.Scope,
                        MsalTestConstants.User.Username,
                        _secureString).ConfigureAwait(false));

                // Check exception message
                Assert.AreEqual("Parsing WS metadata exchange failed", result.Message);
                Assert.AreEqual("parsing_ws_metadata_exchange_failed", result.ErrorCode);

                // There should be no cached entries.
                Assert.AreEqual(0, _cache.TokenCacheAccessor.AccessTokenCount);
            }
        }

        [TestMethod]
        [TestCategory("IntegratedWindowsAuthAndUsernamePasswordTests")]
        [DeploymentItem(@"Resources\TestMex.xml")]
        public void MexDoesNotReturnAuthEndpointTest()
        {
            var ui = new MockWebUI
            {
                MockResult = new AuthorizationResult(
                    AuthorizationStatus.Success,
                    MsalTestConstants.AuthorityOrganizationsTenant + "?code=some-code")
            };

            using (var httpManager = new MockHttpManager())
            {
                var serviceBundle = ServiceBundle.CreateWithCustomHttpManager(httpManager);
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityOrganizationsTenant);
                AddMockHandlerDefaultUserRealmDiscovery(httpManager);
                AddMockHandlerMex(httpManager);

                // Mex does not return integrated auth endpoint (../13/windowstransport)
                httpManager.AddMockHandlerContentNotFound(HttpMethod.Post,
                    "https://msft.sts.microsoft.com/adfs/services/trust/13/windowstransport");

                _cache.ClientId = MsalTestConstants.ClientId;
                var app = new PublicClientApplication(serviceBundle, MsalTestConstants.ClientId,
                    ClientApplicationBase.DefaultAuthority)
                {
                    UserTokenCache = _cache
                };

                // Call acquire token, endpoint not found
                var result = AssertException.TaskThrows<MsalException>(
                    async () => await app.AcquireTokenByUsernamePasswordAsync(
                        MsalTestConstants.Scope,
                        MsalTestConstants.User.Username,
                        _secureString).ConfigureAwait(false));

                // Check exception message
                Assert.AreEqual(CoreErrorCodes.ParsingWsTrustResponseFailed, result.ErrorCode);

                // There should be no cached entries.
                Assert.AreEqual(0, _cache.TokenCacheAccessor.AccessTokenCount);
            }
        }

        [TestMethod]
        [TestCategory("IntegratedWindowsAuthAndUsernamePasswordTests")]
        public void MexParsingFailsTest()
        {
            var ui = new MockWebUI
            {
                MockResult = new AuthorizationResult(
                    AuthorizationStatus.Success,
                    MsalTestConstants.AuthorityOrganizationsTenant + "?code=some-code")
            };

            using (var httpManager = new MockHttpManager())
            {
                var serviceBundle = ServiceBundle.CreateWithCustomHttpManager(httpManager);
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityOrganizationsTenant);
                AddMockHandlerDefaultUserRealmDiscovery(httpManager);

                // MEX
                httpManager.AddMockHandlerContentNotFound(HttpMethod.Get,
                    "https://msft.sts.microsoft.com/adfs/services/trust/mex");

                _cache.ClientId = MsalTestConstants.ClientId;

                var app = new PublicClientApplication(serviceBundle, MsalTestConstants.ClientId,
                    ClientApplicationBase.DefaultAuthority)
                {
                    UserTokenCache = _cache
                };

                // Call acquire token
                var result = AssertException.TaskThrows<MsalException>(
                    async () => await app.AcquireTokenByUsernamePasswordAsync(
                        MsalTestConstants.Scope,
                        MsalTestConstants.User.Username,
                        _secureString).ConfigureAwait(false));

                // Check inner exception
                Assert.AreEqual("Response status code does not indicate success: 404 (NotFound).", result.Message);

                // There should be no cached entries.
                Assert.AreEqual(0, _cache.TokenCacheAccessor.AccessTokenCount);
            }
        }

        [TestMethod]
        [TestCategory("IntegratedWindowsAuthAndUsernamePasswordTests")]
        [DeploymentItem(@"Resources\TestMex.xml")]
        [DeploymentItem(@"Resources\WsTrustResponse.xml")]
        public void FederatedUsernameNullPasswordTest()
        {
            var ui = new MockWebUI
            {
                MockResult = new AuthorizationResult(
                    AuthorizationStatus.Success,
                    MsalTestConstants.AuthorityOrganizationsTenant + "?code=some-code")
            };

            using (var httpManager = new MockHttpManager())
            {
                var serviceBundle = ServiceBundle.CreateWithCustomHttpManager(httpManager);
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityOrganizationsTenant);
                AddMockHandlerDefaultUserRealmDiscovery(httpManager);
                AddMockHandlerMex(httpManager);

                // Mex does not return integrated auth endpoint (.../13/windowstransport)
                httpManager.AddMockHandlerContentNotFound(HttpMethod.Post,
                    "https://msft.sts.microsoft.com/adfs/services/trust/13/windowstransport");

                _cache.ClientId = MsalTestConstants.ClientId;
                var app = new PublicClientApplication(serviceBundle, MsalTestConstants.ClientId,
                    ClientApplicationBase.DefaultAuthority)
                {
                    UserTokenCache = _cache
                };

                SecureString str = null;

                // Call acquire token
                var result = AssertException.TaskThrows<MsalException>(
                    async () => await app.AcquireTokenByUsernamePasswordAsync(
                        MsalTestConstants.Scope,
                        MsalTestConstants.User.Username,
                        str).ConfigureAwait(false));

                // Check inner exception
                Assert.AreEqual(CoreErrorCodes.ParsingWsTrustResponseFailed, result.ErrorCode);

                // There should be no cached entries.
                Assert.AreEqual(0, _cache.TokenCacheAccessor.AccessTokenCount);
            }
        }

        [TestMethod]
        [TestCategory("IntegratedWindowsAuthAndUsernamePasswordWithCommonTests")]
        [DeploymentItem(@"Resources\TestMex.xml")]
        [DeploymentItem(@"Resources\WsTrustResponse.xml")]
        public void FederatedUsernamePasswordCommonAuthorityTest()
        {
            var ui = new MockWebUI
            {
                MockResult = new AuthorizationResult(
                    AuthorizationStatus.Success,
                    MsalTestConstants.AuthorityCommonTenant + "?code=some-code")
            };

            using (var httpManager = new MockHttpManager())
            {
                var serviceBundle = ServiceBundle.CreateWithCustomHttpManager(httpManager);
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityCommonTenant);
                AddMockHandlerDefaultUserRealmDiscovery(httpManager);
                AddMockHandlerMex(httpManager);
                AddMockHandlerWsTrustUserName(httpManager);

                // AAD
                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        Url = "https://login.microsoftonline.com/common/oauth2/v2.0/token",
                        Method = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateInvalidRequestTokenResponseMessage()
                    });

                _cache.ClientId = MsalTestConstants.ClientId;
                var app = new PublicClientApplication(serviceBundle, MsalTestConstants.ClientId,
                    ClientApplicationBase.DefaultAuthority)
                {
                    UserTokenCache = _cache
                };

                // Call acquire token
                var result = AssertException.TaskThrows<MsalException>(
                    async () => await app.AcquireTokenByUsernamePasswordAsync(
                        MsalTestConstants.Scope,
                        MsalTestConstants.User.Username,
                        _secureString).ConfigureAwait(false));

                // Check inner exception
                Assert.AreEqual(CoreErrorCodes.InvalidRequest, result.ErrorCode);

                // There should be no cached entries.
                Assert.AreEqual(0, _cache.TokenCacheAccessor.AccessTokenCount);
            }
        }

        [TestMethod]
        [TestCategory("IntegratedWindowsAuthAndUsernamePasswordWithCommonTests")]
        public void ManagedUsernamePasswordCommonAuthorityTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                var serviceBundle = ServiceBundle.CreateWithCustomHttpManager(httpManager);
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityCommonTenant);

                // user realm discovery
                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        Method = HttpMethod.Get,
                        ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                "{\"ver\":\"1.0\",\"account_type\":\"Managed\",\"domain_name\":\"id.com\"}")
                        },
                        QueryParams = new Dictionary<string, string>
                        {
                            {"api-version", "1.0"}
                        }
                    });

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        Method = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateInvalidRequestTokenResponseMessage()
                    });

                _cache.ClientId = MsalTestConstants.ClientId;
                var app = new PublicClientApplication(serviceBundle, MsalTestConstants.ClientId,
                    ClientApplicationBase.DefaultAuthority)
                {
                    UserTokenCache = _cache
                };

                // Call acquire token
                var result = AssertException.TaskThrows<MsalException>(
                    async () => await app.AcquireTokenByUsernamePasswordAsync(
                        MsalTestConstants.Scope,
                        MsalTestConstants.User.Username,
                        _secureString).ConfigureAwait(false));

                // Check inner exception
                Assert.AreEqual(CoreErrorCodes.InvalidRequest, result.ErrorCode);

                // There should be no cached entries.
                Assert.AreEqual(0, _cache.TokenCacheAccessor.AccessTokenCount);
            }
        }

        [TestMethod]
        [TestCategory("IntegratedWindowsAuthAndUsernamePasswordTests")]
        public async Task ManagedUsernameSecureStringPasswordAcquireTokenTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                var serviceBundle = ServiceBundle.CreateWithCustomHttpManager(httpManager);
                httpManager.AddInstanceDiscoveryMockHandler();
                AddMockResponseforManagedAccounts(httpManager);

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        Method = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                        PostDataObject = new Dictionary<string, object>
                        {
                            {"grant_type", "password"},
                            {"username", MsalTestConstants.User.Username},
                            {"password", _secureString}
                        }
                    });

                var app = new PublicClientApplication(
                    serviceBundle,
                    MsalTestConstants.ClientId,
                    ClientApplicationBase.DefaultAuthority);

                var result = await app.AcquireTokenByUsernamePasswordAsync(
                    MsalTestConstants.Scope,
                    MsalTestConstants.User.Username,
                    _secureString).ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual("some-access-token", result.AccessToken);
                Assert.IsNotNull(result.Account);
                Assert.AreEqual(MsalTestConstants.User.Username, result.Account.Username);
            }
        }

        [TestMethod]
        [TestCategory("IntegratedWindowsAuthAndUsernamePasswordTests")]
        public void ManagedUsernameNoPasswordAcquireTokenTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                var serviceBundle = ServiceBundle.CreateWithCustomHttpManager(httpManager);
                httpManager.AddInstanceDiscoveryMockHandler();
                AddMockResponseforManagedAccounts(httpManager);

                _cache.ClientId = MsalTestConstants.ClientId;
                var app = new PublicClientApplication(serviceBundle, MsalTestConstants.ClientId,
                    ClientApplicationBase.DefaultAuthority)
                {
                    UserTokenCache = _cache
                };

                SecureString str = null;

                // Call acquire token
                var result = AssertException.TaskThrows<MsalException>(
                    async () => await app.AcquireTokenByUsernamePasswordAsync(
                        MsalTestConstants.Scope,
                        MsalTestConstants.User.Username,
                        str).ConfigureAwait(false));

                // Check error code
                Assert.AreEqual(MsalError.PasswordRequiredForManagedUserError, result.ErrorCode);

                // There should be no cached entries.
                Assert.AreEqual(0, _cache.TokenCacheAccessor.AccessTokenCount);
            }
        }

        [TestMethod]
        [TestCategory("IntegratedWindowsAuthAndUsernamePasswordTests")]
        public void ManagedUsernameIncorrectPasswordAcquireTokenTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                var serviceBundle = ServiceBundle.CreateWithCustomHttpManager(httpManager);
                httpManager.AddInstanceDiscoveryMockHandler();
                AddMockResponseforManagedAccounts(httpManager);

                var str = new SecureString();
                str.AppendChar('y');
                str.MakeReadOnly();

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        Method = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateInvalidGrantTokenResponseMessage(),
                        PostDataObject = new Dictionary<string, object>
                        {
                            {"grant_type", "password"},
                            {"username", MsalTestConstants.User.Username},
                            {"password", _secureString}
                        }
                    });

                _cache.ClientId = MsalTestConstants.ClientId;
                var app = new PublicClientApplication(serviceBundle, MsalTestConstants.ClientId,
                    ClientApplicationBase.DefaultAuthority)
                {
                    UserTokenCache = _cache
                };

                // Call acquire token
                var result = AssertException.TaskThrows<MsalException>(
                    async () => await app.AcquireTokenByUsernamePasswordAsync(
                        MsalTestConstants.Scope,
                        MsalTestConstants.User.Username,
                        str).ConfigureAwait(false));

                // Check error code
                Assert.AreEqual(CoreErrorCodes.InvalidGrantError, result.ErrorCode);

                // There should be no cached entries.
                Assert.AreEqual(0, _cache.TokenCacheAccessor.AccessTokenCount);
            }
        }
#endif
    }
}