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

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Cache;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Cache;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Platform;
using Test.ADAL.NET.Common;
using Test.ADAL.NET.Common.Mocks;
using AuthenticationContext = Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext;
using HttpMessageHandlerFactory = Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Http.AdalHttpMessageHandlerFactory;
using CoreHttpClientFactory = Microsoft.Identity.Core.Http.HttpClientFactory;
using UserCredential = Microsoft.IdentityModel.Clients.ActiveDirectory.UserCredential;
using Test.Microsoft.Identity.Core.Unit;

namespace Test.ADAL.NET.Integration
{

    [TestClass]
    [DeploymentItem("Resources\\TestMex.xml")]
    [DeploymentItem("Resources\\WsTrustResponse.xml")]
    [DeploymentItem("Resources\\WsTrustResponse13.xml")]
    public class FederatedUserCredentialTests
    {
        [TestInitialize]
        public void Initialize()
        {
            TokenCache.DefaultShared.Clear();

            ResetInstanceDiscovery();
            CoreExceptionFactory.Instance = new AdalExceptionFactory();
        }

        public void ResetInstanceDiscovery()
        {
            InstanceDiscovery.InstanceCache.Clear();
            HttpMessageHandlerFactory.InitializeMockProvider();
            HttpMessageHandlerFactory.AddMockHandler(MockHelpers.CreateInstanceDiscoveryMockHandler(AdalTestConstants.GetDiscoveryEndpoint(AdalTestConstants.DefaultAuthorityCommonTenant)));
        }

#if DESKTOP // UserPasswordCredential only supported on net45
        [TestMethod]
        [Description("Test for AcquireToken with empty cache")]
        public async Task AcquireTokenWithEmptyCache_GetsTokenFromServiceTestAsync()
        {
            using (var httpManager = new Microsoft.Identity.Core.Unit.Mocks.MockHttpManager())
            {
                AuthenticationContext context = new AuthenticationContext(
                    httpManager,
                    AdalTestConstants.DefaultAuthorityCommonTenant,
                    AuthorityValidationType.NotProvided,
                    new TokenCache());
                await context.Authenticator.UpdateFromTemplateAsync(null).ConfigureAwait(false);

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler()
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
                        },
                        QueryParams = new Dictionary<string, string>()
                        {
                            {"api-version", "1.0"}
                        }
                    });

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler("https://msft.sts.microsoft.com/adfs/services/trust/mex")
                    {
                        Method = HttpMethod.Get,
                        ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(File.ReadAllText("TestMex.xml"))
                        }
                    });

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler("https://msft.sts.microsoft.com/adfs/services/trust/2005/usernamemixed")
                    {
                        Method = HttpMethod.Post,
                        ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(File.ReadAllText("WsTrustResponse.xml"))
                        }
                    });

                HttpMessageHandlerFactory.AddMockHandler(
                    new MockHttpMessageHandler("https://login.microsoftonline.com/common/oauth2/token")
                    {
                        Method = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                        PostData = new Dictionary<string, string>()
                        {
                            {"grant_type", "urn:ietf:params:oauth:grant-type:saml1_1-bearer"},
                            {"scope", "openid"}
                        }
                    });

                // Call acquire token
                AuthenticationResult result = await context.AcquireTokenAsync(
                                                  AdalTestConstants.DefaultResource,
                                                  AdalTestConstants.DefaultClientId,
                                                  new UserPasswordCredential(
                                                      AdalTestConstants.DefaultDisplayableId,
                                                      AdalTestConstants.DefaultPassword)).ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual("some-access-token", result.AccessToken);
                Assert.AreEqual(AdalTestConstants.DefaultDisplayableId, result.UserInfo.DisplayableId);

                // There should be one cached entry.
                Assert.AreEqual(1, context.TokenCache.Count);

                // All mocks are consumed
                Assert.AreEqual(0, HttpMessageHandlerFactory.MockHandlersCount());
            }
        }


#endif

        [TestMethod]
        [Description("Integrated auth using upn of federated user.")]
        public async Task IntegratedAuthUsingUpn_GetsTokenFromServiceTestAsync()
        {
            using (var httpManager = new Microsoft.Identity.Core.Unit.Mocks.MockHttpManager())
            {
                AuthenticationContext context = new AuthenticationContext(
                    httpManager,
                    AdalTestConstants.DefaultAuthorityCommonTenant,
                    AuthorityValidationType.NotProvided,
                    new TokenCache());
                await context.Authenticator.UpdateFromTemplateAsync(null).ConfigureAwait(false);

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler()
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
                        },
                        QueryParams = new Dictionary<string, string>()
                        {
                            {"api-version", "1.0"}
                        }
                    });

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler("https://msft.sts.microsoft.com/adfs/services/trust/mex")
                    {
                        Method = HttpMethod.Get,
                        ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                File.ReadAllText(ResourceHelper.GetTestResourceRelativePath("TestMex.xml")))
                        }
                    });

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler("https://msft.sts.microsoft.com/adfs/services/trust/13/windowstransport")
                    {
                        Method = HttpMethod.Post,
                        ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                File.ReadAllText(ResourceHelper.GetTestResourceRelativePath("WsTrustResponse13.xml")))
                        }
                    });

                HttpMessageHandlerFactory.AddMockHandler(
                    new MockHttpMessageHandler("https://login.microsoftonline.com/common/oauth2/token")
                    {
                        Method = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                        PostData = new Dictionary<string, string>()
                        {
                            {"grant_type", "urn:ietf:params:oauth:grant-type:saml1_1-bearer"},
                            {"scope", "openid"}
                        }
                    });

                // Call acquire token
                AuthenticationResult result = await context.AcquireTokenAsync(
                                                  AdalTestConstants.DefaultResource,
                                                  AdalTestConstants.DefaultClientId,
                                                  new UserCredential(AdalTestConstants.DefaultDisplayableId)).ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual("some-access-token", result.AccessToken);
                Assert.AreEqual(AdalTestConstants.DefaultDisplayableId, result.UserInfo.DisplayableId);

                // There should be one cached entry.
                Assert.AreEqual(1, context.TokenCache.Count);

                // All mocks are consumed
                Assert.AreEqual(0, HttpMessageHandlerFactory.MockHandlersCount());
            }
        }


        [TestMethod]
        [Description("Integrated auth missing mex and fails parsing")]
        public async Task IntegratedAuthMissingMex_FailsMexParsingTestAsync()
        {
            using (var httpManager = new Microsoft.Identity.Core.Unit.Mocks.MockHttpManager())
            {
                AuthenticationContext context = new AuthenticationContext(
                    httpManager,
                    AdalTestConstants.DefaultAuthorityCommonTenant,
                    AuthorityValidationType.NotProvided,
                    new TokenCache());
                await context.Authenticator.UpdateFromTemplateAsync(null).ConfigureAwait(false);

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler()
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
                        },
                        QueryParams = new Dictionary<string, string>()
                        {
                            {"api-version", "1.0"}
                        }
                    });

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler("https://msft.sts.microsoft.com/adfs/services/trust/mex")
                    {
                        Method = HttpMethod.Get,
                        ResponseMessage = new HttpResponseMessage(HttpStatusCode.NotFound)
                        {
                            Content = new StringContent("Not found")
                        }
                    });

                // Call acquire token
                var result = AssertException.TaskThrows<AdalServiceException>(
                    () => context.AcquireTokenAsync(
                        AdalTestConstants.DefaultResource,
                        AdalTestConstants.DefaultClientId,
                        new UserCredential(AdalTestConstants.DefaultDisplayableId)));

                // Check inner exception
                Assert.AreEqual("Response status code does not indicate success: 404 (NotFound).", result.Message);

                // There should be no cached entries.
                Assert.AreEqual(0, context.TokenCache.Count);

                // All mocks are consumed
                Assert.AreEqual(0, HttpMessageHandlerFactory.MockHandlersCount());
            }
        }

        [TestMethod]
        [Description("Test for AcquireToken with valid token in cache")]
        public async Task AcquireTokenWithValidTokenInCache_ReturnsCachedTokenTestAsync()
        {
            AuthenticationContext context = new AuthenticationContext(AdalTestConstants.DefaultAuthorityCommonTenant, new TokenCache());
            await context.Authenticator.UpdateFromTemplateAsync(null).ConfigureAwait(false);

            AdalTokenCacheKey key = new AdalTokenCacheKey(AdalTestConstants.DefaultAuthorityCommonTenant,
                AdalTestConstants.DefaultResource, AdalTestConstants.DefaultClientId, TokenSubjectType.User,
                AdalTestConstants.DefaultUniqueId, AdalTestConstants.DefaultDisplayableId);
            context.TokenCache.tokenCacheDictionary[key] = new AdalResultWrapper
            {
                RefreshToken = "some-rt",
                ResourceInResponse = AdalTestConstants.DefaultResource,
                Result = new AdalResult("Bearer", "existing-access-token",
                    DateTimeOffset.UtcNow + TimeSpan.FromMinutes(100))
            };

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler("https://login.microsoftonline.com/common/oauth2/token")
            {
                Method = HttpMethod.Get,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("No network call is expected")
                }
            });

            // Call acquire token
            AuthenticationResult result = await context.AcquireTokenAsync(AdalTestConstants.DefaultResource, AdalTestConstants.DefaultClientId,
                new UserCredential(AdalTestConstants.DefaultDisplayableId)).ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.AreEqual("existing-access-token", result.AccessToken);

            // There should be one cached entry.
            Assert.AreEqual(1, context.TokenCache.Count);
            Assert.IsNotNull(result.UserInfo);
        }

        [TestMethod]
        [Description("Test for expired access token and valid refresh token in cache")]
        public async Task IntegratedAuthWithExpiredTokenInCache_UsesRefreshTokenTestAsync()
        {
            AuthenticationContext context = new AuthenticationContext(AdalTestConstants.DefaultAuthorityCommonTenant, new TokenCache());
            await context.Authenticator.UpdateFromTemplateAsync(null).ConfigureAwait(false);

            await context.TokenCache.StoreToCacheAsync(new AdalResultWrapper
            {
                RefreshToken = "some-rt",
                ResourceInResponse = AdalTestConstants.DefaultResource,
                Result = new AdalResult("Bearer", "existing-access-token", DateTimeOffset.UtcNow)
                {
                    UserInfo =
                        new AdalUserInfo()
                        {
                            DisplayableId = AdalTestConstants.DefaultDisplayableId,
                            UniqueId = AdalTestConstants.DefaultUniqueId
                        },
                    IdToken = MockHelpers.CreateIdToken(AdalTestConstants.DefaultUniqueId, AdalTestConstants.DefaultDisplayableId)
                },
            },
            AdalTestConstants.DefaultAuthorityCommonTenant, AdalTestConstants.DefaultResource, AdalTestConstants.DefaultClientId, TokenSubjectType.User,
            new RequestContext(new AdalLogger(new Guid()))).ConfigureAwait(false);
            ResetInstanceDiscovery();

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                PostData = new Dictionary<string, string>()
                {
                    {"grant_type", "refresh_token"}
                }
            });

            // Call acquire token
            AuthenticationResult result = await context.AcquireTokenAsync(AdalTestConstants.DefaultResource, AdalTestConstants.DefaultClientId,
                new UserCredential(AdalTestConstants.DefaultDisplayableId)).ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.AreEqual("some-access-token", result.AccessToken);

            // There should be one cached entry.
            Assert.AreEqual(1, context.TokenCache.Count);

            // Cache entry updated with new access token
            var entry = await context.TokenCache.LoadFromCacheAsync(new CacheQueryData
            {
                Authority = AdalTestConstants.DefaultAuthorityCommonTenant,
                Resource = AdalTestConstants.DefaultResource,
                ClientId = AdalTestConstants.DefaultClientId,
                SubjectType = TokenSubjectType.User,
                UniqueId = AdalTestConstants.DefaultUniqueId,
                DisplayableId = AdalTestConstants.DefaultDisplayableId
            },
            new RequestContext(new AdalLogger(new Guid()))).ConfigureAwait(false);
            Assert.AreEqual("some-access-token", entry.Result.AccessToken);

            Assert.IsNotNull(result.UserInfo);
            Assert.AreEqual(1, context.TokenCache.Count);

            // All mocks are consumed
            Assert.AreEqual(0, HttpMessageHandlerFactory.MockHandlersCount());
        }

        [TestMethod]
        [Description("Mex endpoint fails to resolve and results in a 404")]
        public async Task MexEndpointFailsToResolveTestAsync()
        {
            using (var httpManager = new Microsoft.Identity.Core.Unit.Mocks.MockHttpManager())
            {
                AuthenticationContext context = new AuthenticationContext(
                    httpManager,
                    AdalTestConstants.DefaultAuthorityCommonTenant,
                    AuthorityValidationType.NotProvided,
                    new TokenCache());
                await context.Authenticator.UpdateFromTemplateAsync(null).ConfigureAwait(false);

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler()
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
                        },
                        QueryParams = new Dictionary<string, string>()
                        {
                            {"api-version", "1.0"}
                        }
                    });

                // Malformed Mex returned
                httpManager.AddMockHandler(
                    new MockHttpMessageHandler("https://msft.sts.microsoft.com/adfs/services/trust/mex")
                    {
                        Method = HttpMethod.Get,
                        ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                File.ReadAllText(ResourceHelper.GetTestResourceRelativePath("TestMex.xml"))
                                .Replace("<wsp:All>", " "))
                        }
                    });

                // Call acquire token, Mex parser fails
                var result = AssertException.TaskThrows<AdalException>(
                    () => context.AcquireTokenAsync(
                        AdalTestConstants.DefaultResource,
                        AdalTestConstants.DefaultClientId,
                        new UserCredential(AdalTestConstants.DefaultDisplayableId)));

                // Check exception message
                Assert.AreEqual("Parsing WS metadata exchange failed", result.Message);
                Assert.AreEqual("parsing_ws_metadata_exchange_failed", result.ErrorCode);

                // There should be no cached entries.
                Assert.AreEqual(0, context.TokenCache.Count);

                // All mocks are consumed
                Assert.AreEqual(0, HttpMessageHandlerFactory.MockHandlersCount());
            }
        }

        [TestMethod]
        [Description("Integrated auth using upn of federated user and Mex does not return integrated auth endpoint")]
        public async Task IntegratedAuthUsingUpn_MexDoesNotReturnAuthEndpointTestAsync()
        {
            using (var httpManager = new Microsoft.Identity.Core.Unit.Mocks.MockHttpManager())
            {
                AuthenticationContext context = new AuthenticationContext(
                    httpManager,
                    AdalTestConstants.DefaultAuthorityCommonTenant,
                    AuthorityValidationType.NotProvided,
                    new TokenCache());
                await context.Authenticator.UpdateFromTemplateAsync(null).ConfigureAwait(false);

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler()
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
                        },
                        QueryParams = new Dictionary<string, string>()
                        {
                            {"api-version", "1.0"}
                        }
                    });

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler("https://msft.sts.microsoft.com/adfs/services/trust/mex")
                    {
                        Method = HttpMethod.Get,
                        ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                File.ReadAllText(ResourceHelper.GetTestResourceRelativePath("TestMex.xml")))
                        }
                    });

                // Mex does not return integrated auth endpoint (.../13/windowstransport)
                httpManager.AddMockHandler(
                    new MockHttpMessageHandler("https://msft.sts.microsoft.com/adfs/services/trust/13/windowstransport")
                    {
                        Method = HttpMethod.Post,
                        ResponseMessage = new HttpResponseMessage(HttpStatusCode.NotFound)
                        {
                            Content = new StringContent("Not found")
                        }
                    });

                // Call acquire token, endpoint not found
                var result = AssertException.TaskThrows<AdalException>(
                    () => context.AcquireTokenAsync(
                        AdalTestConstants.DefaultResource,
                        AdalTestConstants.DefaultClientId,
                        new UserCredential(AdalTestConstants.DefaultDisplayableId)));

                // Check exception message
                Assert.AreEqual(
                    "Federated service at https://msft.sts.microsoft.com/adfs/services/trust/13/windowstransport returned error: Not found",
                    result.Message);

                // There should be no cached entries.
                Assert.AreEqual(0, context.TokenCache.Count);

                // All mocks are consumed
                Assert.AreEqual(0, HttpMessageHandlerFactory.MockHandlersCount());
            }
        }

      
    }
}