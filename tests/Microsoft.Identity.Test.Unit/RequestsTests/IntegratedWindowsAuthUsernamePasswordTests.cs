// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.RequestsTests
{
    [TestClass]
    public class IntegratedWindowsAuthAndUsernamePasswordTests
    {
        private string _password = "x";

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
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

        private MockHttpMessageHandler AddMockHandlerAadSuccess(MockHttpManager httpManager, string authority, IDictionary<string, string> expectedRequestHeaders = null)
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
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                ExpectedRequestHeaders = expectedRequestHeaders
            };
            httpManager.AddMockHandler(handler);

            return handler;
        }

        internal MockHttpMessageHandler AddMockResponseForFederatedAccounts(MockHttpManager httpManager)
        {
            MockHttpMessageHandler realmDiscoveryHandler = AddMockHandlerDefaultUserRealmDiscovery(httpManager);
            AddMockHandlerMex(httpManager);
            AddMockHandlerWsTrustUserName(httpManager);
            AddMockHandlerAadSuccess(httpManager, TestConstants.AuthorityCommonTenant);
            return realmDiscoveryHandler;
        }

        private void AddMockResponseforManagedAccounts(MockHttpManager httpManager)
        {
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
        public async Task AcquireTokenByIntegratedWindowsAuthTest_ManagedUserAsync()
        {
            // Arrange
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                AddMockHandlerDefaultUserRealmDiscovery_ManagedUser(httpManager);

                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                        .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                        .WithHttpManager(httpManager)
                                                        .BuildConcrete();

                // Act
                MsalClientException exception = await AssertException.TaskThrowsAsync<MsalClientException>(
                    async () => await app
                        .AcquireTokenByIntegratedWindowsAuth(TestConstants.s_scope)
                        .WithUsername(TestConstants.s_user.Username)
                        .ExecuteAsync(CancellationToken.None)
                        .ConfigureAwait(false)).ConfigureAwait(false);

                // Assert
                Assert.AreEqual(MsalError.IntegratedWindowsAuthNotSupportedForManagedUser, exception.ErrorCode);
            }
        }

        [TestMethod]
        public async Task AcquireTokenByIntegratedWindowsAuthTest_ManagedUser_DiscoveryFailed_ThrowsExceptionAsync()
        {
            // Arrange
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Get,
                        ResponseMessage = new HttpResponseMessage(HttpStatusCode.NotFound)
                        {
                            Content = new StringContent(
                                "{\"ver\":\"1.0\"," +
                                "\"account_type\":\"Managed\"," +
                                "\"domain_name\":\"some_domain.onmicrosoft.com\"," +
                                "\"cloud_audience_urn\":\"urn:federation:MicrosoftOnline\"," +
                                "\"cloud_instance_name\":\"login.microsoftonline.com\"}")
                        }
                    });

                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                        .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                        .WithHttpManager(httpManager)
                                                        .BuildConcrete();

                // Act
                MsalServiceException exception = await AssertException.TaskThrowsAsync<MsalServiceException>(
                    async () => await app
                        .AcquireTokenByIntegratedWindowsAuth(TestConstants.s_scope)
                        .WithUsername(TestConstants.s_user.Username)
                        .ExecuteAsync(CancellationToken.None)
                        .ConfigureAwait(false)).ConfigureAwait(false);

                // Assert
                Assert.AreEqual(MsalError.UserRealmDiscoveryFailed, exception.ErrorCode);
                Assert.AreEqual(HttpStatusCode.NotFound, (HttpStatusCode)exception.StatusCode);
            }
        }

        [TestMethod]
        public async Task AcquireTokenByIntegratedWindowsAuthTest_UnknownUserAsync()
        {
            // Arrange
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

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

                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                        .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                        .WithHttpManager(httpManager)
                                                        .BuildConcrete();

                // Act
                MsalClientException exception = await AssertException.TaskThrowsAsync<MsalClientException>(
                    async () => await app
                        .AcquireTokenByIntegratedWindowsAuth(TestConstants.s_scope)
                        .WithUsername(TestConstants.s_user.Username)
                        .ExecuteAsync(CancellationToken.None)
                        .ConfigureAwait(false)).ConfigureAwait(false);

                // Assert
                Assert.AreEqual(MsalError.UnknownUserType, exception.ErrorCode);
            }
        }

        [TestMethod]
        [DeploymentItem(@"Resources\TestMex.xml")]
        [DeploymentItem(@"Resources\TestMex3rdParty.xml")]
        [DeploymentItem(@"Resources\WsTrustResponse13.xml")]
        [DataRow("TestMex.xml")]
        [DataRow("TestMex3rdParty.xml")]
        public async Task AcquireTokenByIntegratedWindowsAuth3rdPartyIDPTestAsync(string federationMetadataFilePath)
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();
                var expectedRequestHeaders = new Dictionary<string, string> { { Constants.CcsRoutingHintHeader, CoreHelpers.GetCcsUpnHint(TestConstants.s_user.Username) } };

                MockHttpMessageHandler realmDiscoveryHandler = AddMockHandlerDefaultUserRealmDiscovery(httpManager);
                AddMockHandlerWsTrustWindowsTransport(httpManager);
                MockHttpMessageHandler mockTokenRequestHttpHandler = AddMockHandlerAadSuccess(httpManager, TestConstants.AuthorityCommonTenant, expectedRequestHeaders);
                mockTokenRequestHttpHandler.ExpectedQueryParams = TestConstants.ExtraQueryParameters;
                mockTokenRequestHttpHandler.ExpectedPostData = new Dictionary<string, string> { { OAuth2Parameter.Claims, TestConstants.Claims } };
                string federationMetadata = File.ReadAllText(ResourceHelper.GetTestResourceRelativePath(federationMetadataFilePath));

                //Using 3rd party federation metadata
                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                        .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                        .WithHttpManager(httpManager)
                                                        .WithExtraQueryParameters(TestConstants.ExtraQueryParameters)
                                                        .BuildConcrete();

                AuthenticationResult result = await app
                    .AcquireTokenByIntegratedWindowsAuth(TestConstants.s_scope)
                                                        .WithClaims(TestConstants.Claims)
                                                        .WithUsername(TestConstants.s_user.Username)
                                                        .WithFederationMetadata(federationMetadata)
                                                        .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual("some-access-token", result.AccessToken);
                Assert.IsNotNull(result.Account);
                Assert.AreEqual(TestConstants.DisplayableId, result.Account.Username);
                Assert.IsNotNull(realmDiscoveryHandler.ActualRequestMessage.Headers);
                StringAssert.Contains(realmDiscoveryHandler.ActualRequestMessage.Headers.ToString(), TestConstants.XClientSku,
                    "Client info header should contain " + TestConstants.XClientSku,
                    StringComparison.OrdinalIgnoreCase);
                StringAssert.Contains(realmDiscoveryHandler.ActualRequestMessage.Headers.ToString(), TestConstants.XClientVer,
                    "Client info header should contain " + TestConstants.XClientVer,
                    StringComparison.OrdinalIgnoreCase);
            }
        }

        [DeploymentItem(@"Resources\TestMex.xml")]
        [DeploymentItem(@"Resources\WsTrustResponse13.xml")]
        public async Task AcquireTokenByIntegratedWindowsAuthMetadataTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                MockHttpMessageHandler realmDiscoveryHandler = AddMockHandlerDefaultUserRealmDiscovery(httpManager);
                AddMockHandlerMex(httpManager);
                AddMockHandlerWsTrustWindowsTransport(httpManager);

                MockHttpMessageHandler mockTokenRequestHttpHandler = AddMockHandlerAadSuccess(httpManager, TestConstants.AuthorityCommonTenant);
                mockTokenRequestHttpHandler.ExpectedQueryParams = TestConstants.ExtraQueryParameters;
                mockTokenRequestHttpHandler.ExpectedPostData = new Dictionary<string, string> { { OAuth2Parameter.Claims, TestConstants.Claims } };

                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                        .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                        .WithHttpManager(httpManager)
                                                        .WithExtraQueryParameters(TestConstants.ExtraQueryParameters)
                                                        .BuildConcrete();

                AuthenticationResult result = await app
                    .AcquireTokenByIntegratedWindowsAuth(TestConstants.s_scope)
                                                        .WithClaims(TestConstants.Claims)
                                                        .WithUsername(TestConstants.s_user.Username)
                                                        .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual("some-access-token", result.AccessToken);
                Assert.IsNotNull(result.Account);
                Assert.AreEqual(TestConstants.DisplayableId, result.Account.Username);
                Assert.IsNotNull(realmDiscoveryHandler.ActualRequestMessage.Headers);
                StringAssert.Contains(realmDiscoveryHandler.ActualRequestMessage.Headers.ToString(), TestConstants.XClientSku,
                    "Client info header should contain " + TestConstants.XClientSku,
                    StringComparison.OrdinalIgnoreCase);
                StringAssert.Contains(realmDiscoveryHandler.ActualRequestMessage.Headers.ToString(), TestConstants.XClientVer,
                    "Client info header should contain " + TestConstants.XClientVer,
                    StringComparison.OrdinalIgnoreCase);
            }
        }

        [TestMethod]
        [DeploymentItem(@"Resources\TestMex.xml")]
        [DeploymentItem(@"Resources\WsTrustResponse13.xml")]
        public async Task AcquireTokenByIntegratedWindowsAuthInvalidClientTestAsync()
        {
            IDictionary<string, string> extraQueryParamsAndClaims =
                TestConstants.ExtraQueryParameters.ToDictionary(e => e.Key, e => e.Value);
            extraQueryParamsAndClaims.Add(OAuth2Parameter.Claims, TestConstants.Claims);

            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                MockHttpMessageHandler realmDiscoveryHandler = AddMockHandlerDefaultUserRealmDiscovery(httpManager);
                AddMockHandlerMex(httpManager);
                AddMockHandlerWsTrustWindowsTransport(httpManager);
                httpManager.AddMockHandler(
                   new MockHttpMessageHandler
                   {
                       ExpectedUrl = "https://login.microsoftonline.com/common/oauth2/v2.0/token",
                       ExpectedMethod = HttpMethod.Post,
                       ResponseMessage = MockHelpers.CreateInvalidClientResponseMessage()
                   });

                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                        .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                        .WithHttpManager(httpManager)
                                                        .WithExtraQueryParameters(TestConstants.ExtraQueryParameters)
                                                        .BuildConcrete();

                MsalServiceException result = await AssertException.TaskThrowsAsync<MsalServiceException>(
                    async () => await app.AcquireTokenByIntegratedWindowsAuth(TestConstants.s_scope)
                                                        .WithClaims(TestConstants.Claims)
                                                        .WithUsername(TestConstants.s_user.Username)
                                                        .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                // Check inner exception
                Assert.AreEqual(MsalError.InvalidClient, result.ErrorCode);

                // There should be no cached entries.
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
            }
        }

#if !WINDOWS_APP // U/P flow not enabled on UWP
        [TestMethod]
        [DeploymentItem(@"Resources\TestMex.xml")]
        [DeploymentItem(@"Resources\WsTrustResponse.xml")]
        public async Task FederatedUsernamePasswordAcquireTokenTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();
                MockHttpMessageHandler realmDiscoveryHandler = AddMockResponseForFederatedAccounts(httpManager);

                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                        .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                        .WithHttpManager(httpManager)
                                                        .BuildConcrete();

                AuthenticationResult result = await app.AcquireTokenByUsernamePassword(
                    TestConstants.s_scope,
                    TestConstants.s_user.Username,
                    _password).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual("some-access-token", result.AccessToken);
                Assert.IsNotNull(result.Account);
                Assert.AreEqual(TestConstants.s_user.Username, result.Account.Username);
                Assert.IsNotNull(realmDiscoveryHandler.ActualRequestMessage.Headers);
                StringAssert.Contains(realmDiscoveryHandler.ActualRequestMessage.Headers.ToString(), TestConstants.XClientSku,
                    "Client info header should contain " + TestConstants.XClientSku,
                    StringComparison.OrdinalIgnoreCase);
                StringAssert.Contains(realmDiscoveryHandler.ActualRequestMessage.Headers.ToString(), TestConstants.XClientVer,
                    "Client info header should contain " + TestConstants.XClientVer,
                    StringComparison.OrdinalIgnoreCase);
            }
        }

        [TestMethod]
        [DeploymentItem(@"Resources\TestMex.xml")]
        public async Task MexEndpointFailsToResolveTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();
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

                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                        .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs)
                                                        .WithHttpManager(httpManager)
                                                        .BuildConcrete();

                // Call acquire token, Mex parser fails
                MsalClientException result = await AssertException.TaskThrowsAsync<MsalClientException>(
                    async () => await app.AcquireTokenByUsernamePassword(
                        TestConstants.s_scope,
                        TestConstants.s_user.Username,
                        _password).ExecuteAsync(CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false);

                // Check exception message
                Assert.AreEqual(MsalErrorMessage.ParsingMetadataDocumentFailed, result.Message);
                Assert.AreEqual("parsing_ws_metadata_exchange_failed", result.ErrorCode);

                // There should be no cached entries.
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
            }
        }

        [TestMethod]
        [DeploymentItem(@"Resources\TestMex.xml")]
        public async Task MexDoesNotReturnAuthEndpointTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();
                AddMockHandlerDefaultUserRealmDiscovery(httpManager);
                AddMockHandlerMex(httpManager);

                // Mex does not return integrated auth endpoint (../13/windowstransport)
                httpManager.AddMockHandlerContentNotFound(HttpMethod.Post,
                    "https://msft.sts.microsoft.com/adfs/services/trust/13/windowstransport");

                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                        .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                        .WithHttpManager(httpManager)
                                                        .BuildConcrete();

                // Call acquire token, endpoint not found
                MsalClientException result = await AssertException.TaskThrowsAsync<MsalClientException>(
                    async () => await app.AcquireTokenByUsernamePassword(
                        TestConstants.s_scope,
                        TestConstants.s_user.Username,
                        _password).ExecuteAsync(CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false);

                // Check exception message
                Assert.AreEqual(MsalError.ParsingWsTrustResponseFailed, result.ErrorCode);

                // There should be no cached entries.
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
            }
        }

        [TestMethod]
        public async Task MexParsingFailsTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();
                AddMockHandlerDefaultUserRealmDiscovery(httpManager);

                // MEX
                httpManager.AddMockHandlerContentNotFound(HttpMethod.Get,
                    "https://msft.sts.microsoft.com/adfs/services/trust/mex");

                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                        .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                        .WithHttpManager(httpManager)
                                                        .BuildConcrete();

                // Call acquire token
                MsalServiceException result = await AssertException.TaskThrowsAsync<MsalServiceException>(
                    async () => await app.AcquireTokenByUsernamePassword(
                        TestConstants.s_scope,
                        TestConstants.s_user.Username,
                        _password).ExecuteAsync(CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false);

                // Check inner exception
                string expectedError = string.Format(CultureInfo.InvariantCulture,
                    MsalErrorMessage.HttpRequestUnsuccessful + "See https://aka.ms/msal-net-ropc for more information. ",
                    "404", "NotFound");
                Assert.AreEqual(expectedError, result.Message);

                // There should be no cached entries.
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
            }
        }

        [TestMethod]
        [DeploymentItem(@"Resources\TestMex.xml")]
        [DeploymentItem(@"Resources\WsTrustResponse.xml")]
        public async Task FederatedUsernameNullPasswordTestAsync()
        {
            string password = null;
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();
                AddMockHandlerDefaultUserRealmDiscovery(httpManager);
                AddMockHandlerMex(httpManager);

                // Mex does not return integrated auth endpoint (.../13/windowstransport)
                httpManager.AddMockHandlerContentNotFound(HttpMethod.Post,
                    "https://msft.sts.microsoft.com/adfs/services/trust/13/windowstransport");

                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                        .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                        .WithHttpManager(httpManager)
                                                        .BuildConcrete();

                // Call acquire token
                MsalClientException result = await AssertException.TaskThrowsAsync<MsalClientException>(
                    async () => await app.AcquireTokenByUsernamePassword(
                        TestConstants.s_scope,
                        TestConstants.s_user.Username,
                        password).ExecuteAsync(CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false);

                // Check inner exception
                Assert.AreEqual(MsalError.ParsingWsTrustResponseFailed, result.ErrorCode);

                // There should be no cached entries.
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
            }
        }

        [TestMethod]
        [DeploymentItem(@"Resources\TestMex.xml")]
        [DeploymentItem(@"Resources\WsTrustResponse.xml")]
        public async Task FederatedUsernamePasswordCommonAuthorityTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();
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

                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                        .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                        .WithHttpManager(httpManager)
                                                        .BuildConcrete();

                // Call acquire token
                MsalServiceException result = await AssertException.TaskThrowsAsync<MsalServiceException>(
                    async () => await app.AcquireTokenByUsernamePassword(
                        TestConstants.s_scope,
                        TestConstants.s_user.Username,
                        _password).ExecuteAsync(CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false);

                // Check inner exception
                Assert.AreEqual(MsalError.InvalidRequest, result.ErrorCode);

                // There should be no cached entries.
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
            }
        }

        [TestMethod]
        public async Task ManagedUsernamePasswordCommonAuthorityTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

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

                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                        .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                        .WithHttpManager(httpManager)
                                                        .BuildConcrete();

                // Call acquire token
                MsalServiceException result = await AssertException.TaskThrowsAsync<MsalServiceException>(
                    async () => await app.AcquireTokenByUsernamePassword(
                        TestConstants.s_scope,
                        TestConstants.s_user.Username,
                        _password).ExecuteAsync(CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false);

                // Check inner exception
                Assert.AreEqual(MsalError.InvalidRequest, result.ErrorCode);

                // There should be no cached entries.
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
            }
        }

        [TestMethod]

        public async Task ManagedUsernamePasswordAcquireTokenTestAsync()
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
                        ExpectedPostData = new Dictionary<string, string>
                        {
                            {"grant_type", "password"},
                            {"username", TestConstants.s_user.Username},
                        }
                    });

                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                        .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs)
                                                        .WithHttpManager(httpManager)
                                                        .BuildConcrete();

                AuthenticationResult result = await app.AcquireTokenByUsernamePassword(
                    TestConstants.s_scope,
                    TestConstants.s_user.Username,
                    _password).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual("some-access-token", result.AccessToken);
                Assert.IsNotNull(result.Account);
                Assert.AreEqual(TestConstants.s_user.Username, result.Account.Username);
            }
        }

        [TestMethod]
        public async Task ManagedUsernameNoPasswordAcquireTokenTestAsync()
        {
            string password = null;
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();
                AddMockResponseforManagedAccounts(httpManager);

                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                        .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs)
                                                        .WithHttpManager(httpManager)
                                                        .BuildConcrete();

                // Call acquire token
                MsalClientException result = await AssertException.TaskThrowsAsync<MsalClientException>(
                    async () => await app.AcquireTokenByUsernamePassword(
                        TestConstants.s_scope,
                        TestConstants.s_user.Username,
                        password).ExecuteAsync(CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false);

                // Check error code
                Assert.AreEqual(MsalError.PasswordRequiredForManagedUserError, result.ErrorCode);

                // There should be no cached entries.
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
            }
        }

        [TestMethod]
        public async Task ManagedUsernameIncorrectPasswordAcquireTokenTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();
                AddMockResponseforManagedAccounts(httpManager);

                string str = "y";

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateInvalidGrantTokenResponseMessage(),
                        ExpectedPostData = new Dictionary<string, string>
                        {
                            {"grant_type", "password"},
                            {"username", TestConstants.s_user.Username},
                            {"password", "y"}
                        }
                    });

                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                        .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs)
                                                        .WithHttpManager(httpManager)
                                                        .BuildConcrete();

                // Call acquire token
                MsalUiRequiredException result = await Assert.ThrowsExceptionAsync<MsalUiRequiredException>(
                    () => app.AcquireTokenByUsernamePassword(
                        TestConstants.s_scope,
                        TestConstants.s_user.Username,
                        str).ExecuteAsync(CancellationToken.None)).ConfigureAwait(false);

                // Check error code
                Assert.AreEqual(MsalError.InvalidGrantError, result.ErrorCode);

                // There should be no cached entries.
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
            }
        }

        [TestMethod]
        public async Task UsernamePasswordInvalidClientTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

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

                // AAD
                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedUrl = "https://login.microsoftonline.com/common/oauth2/v2.0/token",
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateInvalidClientResponseMessage()
                    });

                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                        .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                        .WithHttpManager(httpManager)
                                                        .BuildConcrete();

                // Call acquire token
                MsalServiceException result = await Assert.ThrowsExceptionAsync<MsalServiceException>(
                    () => app.AcquireTokenByUsernamePassword(
                        TestConstants.s_scope,
                        TestConstants.s_user.Username,
                        _password).ExecuteAsync()).ConfigureAwait(false);

                // Check inner exception
                Assert.AreEqual(MsalError.InvalidClient, result.ErrorCode);

                // There should be no cached entries.
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
            }
        }
#endif
    }
}
