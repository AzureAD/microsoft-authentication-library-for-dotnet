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

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Exceptions;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Test.Unit.RequestsTests
{
    [TestClass]
    public class IntegratedWindowsAuthAndUsernamePasswordTests
    {
        private SecureString _secureString;

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetStateAndInitMsal();

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

        private MockHttpMessageHandler AddMockHandlerDefaultUserRealmDiscovery(MockHttpManager httpManager)
        {
            var handler = new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Get,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                            "{\"ver\":\"1.0\",\"account_type\":\"federated\",\"domain_name\":\"microsoft.com\"," +
                            "\"federation_protocol\":\"WSTrust\",\"federation_metadata_url\":" +
                            "\"https://msft.sts.microsoft.com/adfs/services/trust/mex\"," +
                            "\"federation_active_auth_url\":\"https://msft.sts.microsoft.com/adfs/services/trust/2005/usernamemixed\"" +
                            ",\"cloud_instance_name\":\"login.microsoftonline.com\"}")
                }
            };

            // user realm discovery
            httpManager.AddMockHandler(handler);
            return handler;
        }

        private void AddMockHandlerDefaultUserRealmDiscovery_ManagedUser(MockHttpManager httpManager)
        {
            // user realm discovery
            httpManager.AddMockHandler(
                new MockHttpMessageHandler
                {
                    ExpectedMethod = HttpMethod.Get,
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
                    ExpectedUrl = "https://msft.sts.microsoft.com/adfs/services/trust/mex",
                    ExpectedMethod = HttpMethod.Get,
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
                    ExpectedUrl = "https://msft.sts.microsoft.com/adfs/services/trust/2005/usernamemixed",
                    ExpectedMethod = HttpMethod.Post,
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
                    ExpectedUrl = "https://msft.sts.microsoft.com/adfs/services/trust/13/windowstransport",
                    ExpectedMethod = HttpMethod.Post,
                    ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            File.ReadAllText(ResourceHelper.GetTestResourceRelativePath("WsTrustResponse13.xml")))
                    }
                });
        }

        private MockHttpMessageHandler AddMockHandlerAadSuccess(MockHttpManager httpManager, string authority)
        {
            var handler = new MockHttpMessageHandler
            {
                ExpectedUrl = authority + "oauth2/v2.0/token",
                ExpectedMethod = HttpMethod.Post,
                ExpectedPostData = new Dictionary<string, string>
                    {
                        {"grant_type", "urn:ietf:params:oauth:grant-type:saml1_1-bearer"},
                        {"scope", "openid offline_access profile r1/scope1 r1/scope2"}
                    },
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
            };
            httpManager.AddMockHandler(handler);

            return handler;
        }

        internal MockHttpMessageHandler AddMockResponseForFederatedAccounts(MockHttpManager httpManager)
        {
            httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityCommonTenant);
            MockHttpMessageHandler realmDiscoveryHandler = AddMockHandlerDefaultUserRealmDiscovery(httpManager);
            AddMockHandlerMex(httpManager);
            AddMockHandlerWsTrustUserName(httpManager);
            AddMockHandlerAadSuccess(httpManager, MsalTestConstants.AuthorityCommonTenant);
            return realmDiscoveryHandler;
        }

        private void AddMockResponseforManagedAccounts(MockHttpManager httpManager)
        {
            httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityOrganizationsTenant);

            // user realm discovery
            httpManager.AddMockHandler(
                new MockHttpMessageHandler
                {
                    ExpectedMethod = HttpMethod.Get,
                    ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            "{\"ver\":\"1.0\",\"account_type\":\"Managed\",\"domain_name\":\"id.com\"}")
                    },
                    ExpectedQueryParams = new Dictionary<string, string>
                    {
                        {"api-version", "1.0"}
                    }
                });
        }

        [TestMethod]
        [TestCategory("IntegratedWindowsAuthAndUsernamePasswordTests")]
        public void AcquireTokenByIntegratedWindowsAuthTest_ManagedUser()
        {
            // Arrange
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityCommonTenant);
                AddMockHandlerDefaultUserRealmDiscovery_ManagedUser(httpManager);

                var app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                        .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                        .WithHttpManager(httpManager)
                                                        .BuildConcrete();

                // Act
                var exception = AssertException.TaskThrows<MsalClientException>(
                    async () => await app
                        .AcquireTokenByIntegratedWindowsAuth(MsalTestConstants.Scope)
                        .WithUsername(MsalTestConstants.User.Username)
                        .ExecuteAsync(CancellationToken.None)
                        .ConfigureAwait(false));

                // Assert
                Assert.AreEqual(MsalClientException.IntegratedWindowsAuthNotSupportedForManagedUser, exception.ErrorCode);
            }
        }

        [TestMethod]
        [TestCategory("IntegratedWindowsAuthAndUsernamePasswordTests")]
        public void AcquireTokenByIntegratedWindowsAuthTest_UnknownUser()
        {
            // Arrange
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityCommonTenant);

                // user realm discovery - unknown user type
                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Get,
                        ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                "{\"ver\":\"1.0\"," +
                                "\"account_type\":\"Bogus\"}")
                        }
                    });

                var app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                        .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                        .WithHttpManager(httpManager)
                                                        .BuildConcrete();

                // Act
                var exception = AssertException.TaskThrows<MsalClientException>(
                    async () => await app
                        .AcquireTokenByIntegratedWindowsAuth(MsalTestConstants.Scope)
                        .WithUsername(MsalTestConstants.User.Username)
                        .ExecuteAsync(CancellationToken.None)
                        .ConfigureAwait(false));

                // Assert
                Assert.AreEqual(MsalClientException.UnknownUserType, exception.ErrorCode);
            }
        }

        [TestMethod]
        [TestCategory("IntegratedWindowsAuthAndUsernamePasswordTests")]
        [DeploymentItem(@"Resources\TestMex.xml")]
        [DeploymentItem(@"Resources\WsTrustResponse13.xml")]
        public async Task AcquireTokenByIntegratedWindowsAuthTestAsync()
        {
            IDictionary<string, string> extraQueryParamsAndClaims =
                MsalTestConstants.ExtraQueryParams.ToDictionary(e => e.Key, e => e.Value);
            extraQueryParamsAndClaims.Add(OAuth2Parameter.Claims, MsalTestConstants.Claims);

            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityCommonTenant);
                MockHttpMessageHandler realmDiscoveryHandler = AddMockHandlerDefaultUserRealmDiscovery(httpManager);
                AddMockHandlerMex(httpManager);
                AddMockHandlerWsTrustWindowsTransport(httpManager);
                var mockTokenRequestHttpHandler = AddMockHandlerAadSuccess(httpManager, MsalTestConstants.AuthorityCommonTenant);
                mockTokenRequestHttpHandler.ExpectedQueryParams = extraQueryParamsAndClaims;

                var app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                        .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                        .WithHttpManager(httpManager)
                                                        .WithExtraQueryParameters(MsalTestConstants.ExtraQueryParams)
                                                        .BuildConcrete();

                var result = await app
                    .AcquireTokenByIntegratedWindowsAuth(MsalTestConstants.Scope)
                                                        .WithClaims(MsalTestConstants.Claims)
                                                        .WithUsername(MsalTestConstants.User.Username)
                                                        .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual("some-access-token", result.AccessToken);
                Assert.IsNotNull(result.Account);
                Assert.AreEqual(MsalTestConstants.DisplayableId, result.Account.Username);
                Assert.IsNotNull(realmDiscoveryHandler.ActualRequestMessge.Headers);
                StringAssert.Contains(realmDiscoveryHandler.ActualRequestMessge.Headers.ToString(), MsalTestConstants.XClientSku,
                    "Client info header should contain " + MsalTestConstants.XClientSku,
                    StringComparison.OrdinalIgnoreCase);
                StringAssert.Contains(realmDiscoveryHandler.ActualRequestMessge.Headers.ToString(), MsalTestConstants.XClientVer,
                    "Client info header should contain " + MsalTestConstants.XClientVer,
                    StringComparison.OrdinalIgnoreCase);
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
                httpManager.AddInstanceDiscoveryMockHandler();
                MockHttpMessageHandler realmDiscoveryHandler = AddMockResponseForFederatedAccounts(httpManager);

                var app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                        .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                        .WithHttpManager(httpManager)
                                                        .BuildConcrete();

                var result = await app.AcquireTokenByUsernamePassword(
                    MsalTestConstants.Scope,
                    MsalTestConstants.User.Username,
                    _secureString).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual("some-access-token", result.AccessToken);
                Assert.IsNotNull(result.Account);
                Assert.AreEqual(MsalTestConstants.User.Username, result.Account.Username);
                Assert.IsNotNull(realmDiscoveryHandler.ActualRequestMessge.Headers);
                StringAssert.Contains(realmDiscoveryHandler.ActualRequestMessge.Headers.ToString(), MsalTestConstants.XClientSku,
                    "Client info header should contain " + MsalTestConstants.XClientSku,
                    StringComparison.OrdinalIgnoreCase);
                StringAssert.Contains(realmDiscoveryHandler.ActualRequestMessge.Headers.ToString(), MsalTestConstants.XClientVer,
                    "Client info header should contain " + MsalTestConstants.XClientVer,
                    StringComparison.OrdinalIgnoreCase);
            }
        }

        [TestMethod]
        [TestCategory("IntegratedWindowsAuthAndUsernamePasswordTests")]
        [DeploymentItem(@"Resources\TestMex.xml")]
        public void MexEndpointFailsToResolveTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityOrganizationsTenant);
                AddMockHandlerDefaultUserRealmDiscovery(httpManager);

                // MEX
                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedUrl = "https://msft.sts.microsoft.com/adfs/services/trust/mex",
                        ExpectedMethod = HttpMethod.Get,
                        ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                File.ReadAllText(ResourceHelper.GetTestResourceRelativePath("TestMex.xml"))
                                    .Replace("<wsp:All>", " "))
                        }
                    });

                var app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                        .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs)
                                                        .WithHttpManager(httpManager)
                                                        .BuildConcrete();

                // Call acquire token, Mex parser fails
                var result = AssertException.TaskThrows<MsalException>(
                    async () => await app.AcquireTokenByUsernamePassword(
                        MsalTestConstants.Scope,
                        MsalTestConstants.User.Username,
                        _secureString).ExecuteAsync(CancellationToken.None).ConfigureAwait(false));

                // Check exception message
                Assert.AreEqual("Parsing WS metadata exchange failed", result.Message);
                Assert.AreEqual("parsing_ws_metadata_exchange_failed", result.ErrorCode);

                // There should be no cached entries.
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
            }
        }

        [TestMethod]
        [TestCategory("IntegratedWindowsAuthAndUsernamePasswordTests")]
        [DeploymentItem(@"Resources\TestMex.xml")]
        public void MexDoesNotReturnAuthEndpointTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityCommonTenant);
                AddMockHandlerDefaultUserRealmDiscovery(httpManager);
                AddMockHandlerMex(httpManager);

                // Mex does not return integrated auth endpoint (../13/windowstransport)
                httpManager.AddMockHandlerContentNotFound(HttpMethod.Post,
                    "https://msft.sts.microsoft.com/adfs/services/trust/13/windowstransport");

                var app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                        .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                        .WithHttpManager(httpManager)
                                                        .BuildConcrete();

                // Call acquire token, endpoint not found
                var result = AssertException.TaskThrows<MsalException>(
                    async () => await app.AcquireTokenByUsernamePassword(
                        MsalTestConstants.Scope,
                        MsalTestConstants.User.Username,
                        _secureString).ExecuteAsync(CancellationToken.None).ConfigureAwait(false));

                // Check exception message
                Assert.AreEqual(MsalClientException.ParsingWsTrustResponseFailed, result.ErrorCode);

                // There should be no cached entries.
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
            }
        }

        [TestMethod]
        [TestCategory("IntegratedWindowsAuthAndUsernamePasswordTests")]
        public void MexParsingFailsTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityCommonTenant);
                AddMockHandlerDefaultUserRealmDiscovery(httpManager);

                // MEX
                httpManager.AddMockHandlerContentNotFound(HttpMethod.Get,
                    "https://msft.sts.microsoft.com/adfs/services/trust/mex");

                var app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                        .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                        .WithHttpManager(httpManager)
                                                        .BuildConcrete();

                // Call acquire token
                var result = AssertException.TaskThrows<MsalException>(
                    async () => await app.AcquireTokenByUsernamePassword(
                        MsalTestConstants.Scope,
                        MsalTestConstants.User.Username,
                        _secureString).ExecuteAsync(CancellationToken.None).ConfigureAwait(false));

                // Check inner exception
                Assert.AreEqual("Response status code does not indicate success: 404 (NotFound).", result.Message);

                // There should be no cached entries.
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
            }
        }

        [TestMethod]
        [TestCategory("IntegratedWindowsAuthAndUsernamePasswordTests")]
        [DeploymentItem(@"Resources\TestMex.xml")]
        [DeploymentItem(@"Resources\WsTrustResponse.xml")]
        public void FederatedUsernameNullPasswordTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityCommonTenant);
                AddMockHandlerDefaultUserRealmDiscovery(httpManager);
                AddMockHandlerMex(httpManager);

                // Mex does not return integrated auth endpoint (.../13/windowstransport)
                httpManager.AddMockHandlerContentNotFound(HttpMethod.Post,
                    "https://msft.sts.microsoft.com/adfs/services/trust/13/windowstransport");

                var app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                        .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                        .WithHttpManager(httpManager)
                                                        .BuildConcrete();

                SecureString str = null;

                // Call acquire token
                var result = AssertException.TaskThrows<MsalException>(
                    async () => await app.AcquireTokenByUsernamePassword(
                        MsalTestConstants.Scope,
                        MsalTestConstants.User.Username,
                        str).ExecuteAsync(CancellationToken.None).ConfigureAwait(false));

                // Check inner exception
                Assert.AreEqual(MsalClientException.ParsingWsTrustResponseFailed, result.ErrorCode);

                // There should be no cached entries.
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
            }
        }

        [TestMethod]
        [TestCategory("IntegratedWindowsAuthAndUsernamePasswordWithCommonTests")]
        [DeploymentItem(@"Resources\TestMex.xml")]
        [DeploymentItem(@"Resources\WsTrustResponse.xml")]
        public void FederatedUsernamePasswordCommonAuthorityTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityCommonTenant);
                AddMockHandlerDefaultUserRealmDiscovery(httpManager);
                AddMockHandlerMex(httpManager);
                AddMockHandlerWsTrustUserName(httpManager);

                // AAD
                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedUrl = "https://login.microsoftonline.com/common/oauth2/v2.0/token",
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateInvalidRequestTokenResponseMessage()
                    });

                var app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                        .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                        .WithHttpManager(httpManager)
                                                        .BuildConcrete();

                // Call acquire token
                var result = AssertException.TaskThrows<MsalException>(
                    async () => await app.AcquireTokenByUsernamePassword(
                        MsalTestConstants.Scope,
                        MsalTestConstants.User.Username,
                        _secureString).ExecuteAsync(CancellationToken.None).ConfigureAwait(false));

                // Check inner exception
                Assert.AreEqual(MsalError.InvalidRequest, result.ErrorCode);

                // There should be no cached entries.
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
            }
        }

        [TestMethod]
        [TestCategory("IntegratedWindowsAuthAndUsernamePasswordWithCommonTests")]
        public void ManagedUsernamePasswordCommonAuthorityTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityCommonTenant);

                // user realm discovery
                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Get,
                        ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                "{\"ver\":\"1.0\",\"account_type\":\"Managed\",\"domain_name\":\"id.com\"}")
                        },
                        ExpectedQueryParams = new Dictionary<string, string>
                        {
                            {"api-version", "1.0"}
                        }
                    });

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateInvalidRequestTokenResponseMessage()
                    });

                var app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                        .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                        .WithHttpManager(httpManager)
                                                        .BuildConcrete();

                // Call acquire token
                var result = AssertException.TaskThrows<MsalException>(
                    async () => await app.AcquireTokenByUsernamePassword(
                        MsalTestConstants.Scope,
                        MsalTestConstants.User.Username,
                        _secureString).ExecuteAsync(CancellationToken.None).ConfigureAwait(false));

                // Check inner exception
                Assert.AreEqual(MsalError.InvalidRequest, result.ErrorCode);

                // There should be no cached entries.
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
            }
        }

        [TestMethod]
        [TestCategory("IntegratedWindowsAuthAndUsernamePasswordTests")]
        public async Task ManagedUsernameSecureStringPasswordAcquireTokenTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();
                AddMockResponseforManagedAccounts(httpManager);

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                        ExpectedPostDataObject = new Dictionary<string, object>
                        {
                            {"grant_type", "password"},
                            {"username", MsalTestConstants.User.Username},
                            {"password", _secureString}
                        }
                    });

                var app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                        .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs)
                                                        .WithHttpManager(httpManager)
                                                        .BuildConcrete();

                var result = await app.AcquireTokenByUsernamePassword(
                    MsalTestConstants.Scope,
                    MsalTestConstants.User.Username,
                    _secureString).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

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
                httpManager.AddInstanceDiscoveryMockHandler();
                AddMockResponseforManagedAccounts(httpManager);

                var app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                        .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs)
                                                        .WithHttpManager(httpManager)
                                                        .BuildConcrete();

                SecureString str = null;

                // Call acquire token
                var result = AssertException.TaskThrows<MsalException>(
                    async () => await app.AcquireTokenByUsernamePassword(
                        MsalTestConstants.Scope,
                        MsalTestConstants.User.Username,
                        str).ExecuteAsync(CancellationToken.None).ConfigureAwait(false));

                // Check error code
                Assert.AreEqual(MsalClientException.PasswordRequiredForManagedUserError, result.ErrorCode);

                // There should be no cached entries.
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
            }
        }

        [TestMethod]
        [TestCategory("IntegratedWindowsAuthAndUsernamePasswordTests")]
        public void ManagedUsernameIncorrectPasswordAcquireTokenTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();
                AddMockResponseforManagedAccounts(httpManager);

                var str = new SecureString();
                str.AppendChar('y');
                str.MakeReadOnly();

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateInvalidGrantTokenResponseMessage(),
                        ExpectedPostDataObject = new Dictionary<string, object>
                        {
                            {"grant_type", "password"},
                            {"username", MsalTestConstants.User.Username},
                            {"password", _secureString}
                        }
                    });

                var app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                        .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs)
                                                        .WithHttpManager(httpManager)
                                                        .BuildConcrete();

                // Call acquire token
                var result = AssertException.TaskThrows<MsalException>(
                    async () => await app.AcquireTokenByUsernamePassword(
                        MsalTestConstants.Scope,
                        MsalTestConstants.User.Username,
                        str).ExecuteAsync(CancellationToken.None).ConfigureAwait(false));

                // Check error code
                Assert.AreEqual(MsalUiRequiredException.InvalidGrantError, result.ErrorCode);

                // There should be no cached entries.
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
            }
        }
#endif
    }
}
