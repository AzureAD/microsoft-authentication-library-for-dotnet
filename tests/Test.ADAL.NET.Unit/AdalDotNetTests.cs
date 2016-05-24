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
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.ADAL.Common;
using Test.ADAL.NET.Unit.Mocks;

namespace Test.ADAL.NET.Unit
{
    [TestClass]
    [DeploymentItem("valid_cert.pfx")]
    public class AdalDotNetTests
    {
        private PlatformParameters platformParameters;

        [TestInitialize]
        public void Initialize()
        {
            HttpMessageHandlerFactory.ClearMockHandlers();
            platformParameters = new PlatformParameters(PromptBehavior.Auto);
        }

        [TestMethod]
        [Description("Positive Test for AcquireToken")]
        [TestCategory("AdalDotNet")]
        public async Task SmokeTest()
        {
            MockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
                TestConstants.DefaultRedirectUri + "?code=some-code"));
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
            });

            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, true);
            AuthenticationResult result =
                await
                    context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId,
                        TestConstants.DefaultRedirectUri, platformParameters);
            Assert.IsNotNull(result);
            Assert.IsTrue(context.Authenticator.Authority.EndsWith("/some-tenant-id/"));
            Assert.AreEqual(result.AccessToken, "some-access-token");
            Assert.IsNotNull(result.UserInfo);
            Assert.AreEqual(TestConstants.DefaultDisplayableId, result.UserInfo.DisplayableId);
            Assert.AreEqual(TestConstants.DefaultUniqueId, result.UserInfo.UniqueId);
        }

        [TestMethod]
        [Description("Positive Test for AcquireToken with missing redirectUri and/or userId")]
        public async Task AcquireTokenPositiveWithoutUserIdAsync()
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, new TokenCache());
            MockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
                TestConstants.DefaultRedirectUri + "?code=some-code"));
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
            });

            AuthenticationResult result =
                await
                    context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId,
                        TestConstants.DefaultRedirectUri, platformParameters);
            Assert.IsNotNull(result);
            Assert.AreEqual(result.AccessToken, "some-access-token");
            try
            {
                result =
                    await
                        context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId,
                            TestConstants.DefaultRedirectUri, platformParameters, null);
            }
            catch (ArgumentException exc)
            {
                Assert.IsTrue(exc.Message.StartsWith(AdalErrorMessage.SpecifyAnyUser));
            }

            // this should hit the cache
            result =
                await
                    context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId,
                        TestConstants.DefaultRedirectUri, platformParameters, UserIdentifier.AnyUser);
            Assert.IsNotNull(result);
            Assert.AreEqual(result.AccessToken, "some-access-token");
        }

        [TestMethod]
        [Description("Test for autority validation to AuthenticationContext")]
        public async Task AuthenticationContextAuthorityValidationTestAsync()
        {
            AuthenticationContext context = null;
            AuthenticationResult result = null;
            try
            {
                context = new AuthenticationContext("https://login.contoso.com/adfs");
                await
                    context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId,
                        TestConstants.DefaultRedirectUri, platformParameters);
            }
            catch (ArgumentException ex)
            {
                Assert.AreEqual(ex.ParamName, "validateAuthority");
            }

            MockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
                TestConstants.DefaultRedirectUri + "?code=some-code"));
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
            });

            //whitelisted authority
            context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, true);
            result =
                await
                    context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId,
                        TestConstants.DefaultRedirectUri, platformParameters,
                        new UserIdentifier(TestConstants.DefaultDisplayableId, UserIdentifierType.RequiredDisplayableId));
            Assert.IsNotNull(result);
            Assert.AreEqual(result.AccessToken, "some-access-token");
            Assert.IsNotNull(result.UserInfo);
            //add handler to return failed discovery response
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Get,
                ResponseMessage =
                    MockHelpers.CreateFailureResponseMessage(
                        "{\"error\":\"invalid_instance\",\"error_description\":\"AADSTS70002: Error in validating authority.\"}")
            });

            try
            {
                context = new AuthenticationContext("https://login.microsoft0nline.com/common");
                result =
                    await
                        context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId,
                            TestConstants.DefaultRedirectUri, platformParameters);
            }
            catch (AdalException ex)
            {
                Assert.AreEqual(ex.ErrorCode, AdalError.AuthorityNotInValidList);
            }
        }

        [TestMethod]
        [Description("Negative Test for AcquireToken with invalid resource")]
        public async Task AcquireTokenWithInvalidResourceTestAsync()
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, new TokenCache());
            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultResource, TestConstants.DefaultClientId, TokenSubjectType.User,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId);
            context.TokenCache.tokenCacheDictionary[key] = new AuthenticationResultEx
            {
                RefreshToken = "some-rt",
                ResourceInResponse = TestConstants.DefaultResource,
                Result = new AuthenticationResult("Bearer", "some-access-token", DateTimeOffset.UtcNow)
            };

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateInvalidRequestTokenResponseMessage()
            });

            try
            {
                await context.AcquireTokenSilentAsync("random-resource", TestConstants.DefaultClientId);
            }
            catch (AdalServiceException exc)
            {
                Assert.AreEqual(AdalError.FailedToRefreshToken, exc.ErrorCode);
            }
        }

        [TestMethod]
        [Description("Negative Test for AcquireToken with user canceling authentication")]
        public async Task AcquireTokenWithAuthenticationCanceledTest()
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, new TokenCache());
            MockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.UserCancel,
                TestConstants.DefaultRedirectUri + "?error=user_cancelled"));
            try
            {
                await
                    context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId,
                        TestConstants.DefaultRedirectUri, platformParameters);
            }
            catch (AdalServiceException ex)
            {
                Assert.AreEqual(ex.ErrorCode, AdalError.AuthenticationCanceled);
            }
        }

        [TestMethod]
        [Description("Positive Test for AcquireToken testing default token cache")]
        public async Task AcquireTokenPositiveWithNullCacheTest()
        {
            MockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
                TestConstants.DefaultRedirectUri + "?code=some-code"));
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
            });

            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, null);
            AuthenticationResult result =
                await
                    context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId,
                        TestConstants.DefaultRedirectUri, platformParameters);
            Assert.IsNotNull(result);
            Assert.AreEqual(result.AccessToken, "some-access-token");
            Assert.IsNotNull(result.UserInfo);
        }


        [TestMethod]
        [Description("Test for simple refresh token")]
        public async Task SimpleRefreshTokenTest()
        {
            var context = new AuthenticationContext(TestConstants.DefaultAdfsAuthorityTenant, false, new TokenCache());
            //add simple RT to cache
            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAdfsAuthorityTenant,
                TestConstants.DefaultResource, TestConstants.DefaultClientId, TokenSubjectType.User, null, null);
            context.TokenCache.tokenCacheDictionary[key] = new AuthenticationResultEx
            {
                RefreshToken = "some-rt",
                Result = new AuthenticationResult("Bearer", "some-access-token", DateTimeOffset.UtcNow)
            };

            //token request for some other resource should fail.
            try
            {
                await context.AcquireTokenSilentAsync("random-resource", TestConstants.DefaultClientId);
            }
            catch (AdalSilentTokenAcquisitionException exc)
            {
                Assert.AreEqual(AdalError.FailedToAcquireTokenSilently, exc.ErrorCode);
            }
        }

        [TestMethod]
        [Description("Test for acquring token using tenant specific endpoint")]
        public async Task TenantSpecificAuthorityTest()
        {
            MockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
                TestConstants.DefaultRedirectUri + "?code=some-code"));
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
            });

            var context = new AuthenticationContext(TestConstants.DefaultAuthorityHomeTenant, true);
            AuthenticationResult result =
                await
                    context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId,
                        TestConstants.DefaultRedirectUri, platformParameters);
            Assert.IsNotNull(result);
            Assert.AreEqual(TestConstants.DefaultAuthorityHomeTenant, context.Authenticator.Authority);
            Assert.AreEqual(result.AccessToken, "some-access-token");
            Assert.IsNotNull(result.UserInfo);
            Assert.AreEqual(TestConstants.DefaultDisplayableId, result.UserInfo.DisplayableId);
            Assert.AreEqual(TestConstants.DefaultUniqueId, result.UserInfo.UniqueId);
        }

        [TestMethod]
        [Description("Test for Force Prompt")]
        public async Task ForcePromptTestAsync()
        {
            MockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
                TestConstants.DefaultRedirectUri + "?code=some-code"));
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                PostData = new Dictionary<string, string>()
                {
                    {"grant_type", "authorization_code"}
                }
            });

            var context = new AuthenticationContext(TestConstants.DefaultAuthorityHomeTenant, true);
            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultResource, TestConstants.DefaultClientId, TokenSubjectType.User,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId);
            context.TokenCache.tokenCacheDictionary[key] = new AuthenticationResultEx
            {
                RefreshToken = "some-rt",
                ResourceInResponse = TestConstants.DefaultResource,
                Result = new AuthenticationResult("Bearer", "existing-access-token", DateTimeOffset.UtcNow)
            };

            AuthenticationResult result =
                await
                    context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId,
                        TestConstants.DefaultRedirectUri, new PlatformParameters(PromptBehavior.Always));
            Assert.IsNotNull(result);
            Assert.AreEqual(TestConstants.DefaultAuthorityHomeTenant, context.Authenticator.Authority);
            Assert.AreEqual(result.AccessToken, "some-access-token");
            Assert.IsNotNull(result.UserInfo);
            Assert.AreEqual(TestConstants.DefaultDisplayableId, result.UserInfo.DisplayableId);
            Assert.AreEqual(TestConstants.DefaultUniqueId, result.UserInfo.UniqueId);
        }

        [TestMethod]
        [Description("Positive Test for AcquireToken non-interactive")]
        public async Task AcquireTokenNonInteractivePositiveTestAsync()
        {
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
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

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                PostData = new Dictionary<string, string>()
                {
                    {"grant_type", "password"},
                    {"username", TestConstants.DefaultDisplayableId},
                    {"password", TestConstants.DefaultPassword}
                }
            });

            var context = new AuthenticationContext(TestConstants.DefaultAuthorityHomeTenant, true);
            AuthenticationResult result =
                await
                    context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId,
                        new UserPasswordCredential(TestConstants.DefaultDisplayableId, TestConstants.DefaultPassword));
            Assert.IsNotNull(result);
            Assert.AreEqual(TestConstants.DefaultAuthorityHomeTenant, context.Authenticator.Authority);
            Assert.AreEqual(result.AccessToken, "some-access-token");
            Assert.IsNotNull(result.UserInfo);
            Assert.AreEqual(TestConstants.DefaultDisplayableId, result.UserInfo.DisplayableId);
            Assert.AreEqual(TestConstants.DefaultUniqueId, result.UserInfo.UniqueId);
        }

        [TestMethod]
        [Description("Positive Test for Confidential Client")]
        public async Task ConfidentialClientWithX509Test()
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, new TokenCache());
            var certificate = new ClientAssertionCertificate(TestConstants.DefaultClientId, new X509Certificate2("valid_cert.pfx", TestConstants.DefaultPassword));

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                PostData = new Dictionary<string, string>()
                {
                    {"client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer"}
                }
            });

            AuthenticationResult result =
                await
                    context.AcquireTokenByAuthorizationCodeAsync("some-code", TestConstants.DefaultRedirectUri,
                        certificate, TestConstants.DefaultResource);
            Assert.IsNotNull(result.AccessToken);

            try
            {
                await
                    context.AcquireTokenByAuthorizationCodeAsync(null, TestConstants.DefaultRedirectUri, certificate,
                        TestConstants.DefaultResource);
            }
            catch (ArgumentNullException exc)
            {
                Assert.AreEqual(exc.ParamName, "authorizationCode");
            }

            try
            {
                await
                    context.AcquireTokenByAuthorizationCodeAsync(string.Empty, TestConstants.DefaultRedirectUri,
                        certificate,
                        TestConstants.DefaultResource);
            }
            catch (ArgumentNullException exc)
            {
                Assert.AreEqual(exc.ParamName, "authorizationCode");
            }

            try
            {
                // Send null for redirect
                await
                    context.AcquireTokenByAuthorizationCodeAsync("some-code", null, certificate,
                        TestConstants.DefaultResource);
            }
            catch (ArgumentNullException exc)
            {
                Assert.AreEqual(exc.ParamName, "redirectUri");
            }

            try
            {
                await
                    context.AcquireTokenByAuthorizationCodeAsync("some-code", TestConstants.DefaultRedirectUri,
                        (ClientAssertionCertificate) null, TestConstants.DefaultResource);
            }
            catch (ArgumentNullException exc)
            {
                Assert.AreEqual(exc.ParamName, "clientCertificate");
            }
        }
        [TestMethod]
        [Description("Test for Client credential")]
        public async Task ClientCredentialTestAsync()
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, new TokenCache());
            var credential = new ClientCredential(TestConstants.DefaultClientId, TestConstants.DefaultClientSecret);

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"access_token\":\"some-access-token\"}")
                },
                PostData = new Dictionary<string, string>()
                {
                    {"client_id", TestConstants.DefaultClientId},
                    {"client_secret", TestConstants.DefaultClientSecret},
                    {"grant_type", "client_credentials"}
                }
            });

            AuthenticationResult result = await context.AcquireTokenAsync(TestConstants.DefaultResource, credential);
            Assert.IsNotNull(result.AccessToken);

            // cache look up
            var result2 = await context.AcquireTokenAsync(TestConstants.DefaultResource, credential);
            Assert.AreEqual(result.AccessToken, result2.AccessToken);

            try
            {
                await context.AcquireTokenAsync(null, credential);
            }
            catch (ArgumentNullException exc)
            {
                Assert.AreEqual(exc.ParamName, "resource");
            }

            try
            {
                await context.AcquireTokenAsync(TestConstants.DefaultResource, (ClientCredential)null);
            }
            catch (ArgumentNullException exc)
            {
                Assert.AreEqual(exc.ParamName, "clientCredential");
            }
        }

        [TestMethod]
        [Description("Test for Client assertion with X509")]
        public async Task ClientAssertionWithX509Test()
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, new TokenCache());
            var certificate = new ClientAssertionCertificate(TestConstants.DefaultClientId, new X509Certificate2("valid_cert.pfx", TestConstants.DefaultPassword));

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"access_token\":\"some-access-token\"}")
                },
                PostData = new Dictionary<string, string>()
                {
                    {"client_id", TestConstants.DefaultClientId},
                    {"grant_type", "client_credentials"}
                }
            });

            AuthenticationResult result = await context.AcquireTokenAsync(TestConstants.DefaultResource, certificate);
            Assert.IsNotNull(result.AccessToken);

            try
            {
                await context.AcquireTokenAsync(null, certificate);
            }
            catch (ArgumentNullException exc)
            {
                Assert.AreEqual(exc.ParamName, "resource");
            }


            try
            {
                await context.AcquireTokenAsync(TestConstants.DefaultResource, (ClientAssertionCertificate)null);
            }
            catch (ArgumentNullException exc)
            {
                Assert.AreEqual(exc.ParamName, "clientCertificate");
            }
        }

        
        [TestMethod]
        [Description("Test for Confidential Client with self signed jwt")]
        public async Task ConfidentialClientWithJwtTest()
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, new TokenCache());
            
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                PostData = new Dictionary<string, string>()
                {
                    {"client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer"}
                }
            });

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                PostData = new Dictionary<string, string>()
                {
                    {"client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer"}
                }
            });

            ClientAssertion assertion = new ClientAssertion(TestConstants.DefaultClientId, "some-assertion");
            AuthenticationResult result = await context.AcquireTokenByAuthorizationCodeAsync("some-code", TestConstants.DefaultRedirectUri, assertion, TestConstants.DefaultResource);
            Assert.IsNotNull(result.AccessToken);

            result = await context.AcquireTokenByAuthorizationCodeAsync("some-code", TestConstants.DefaultRedirectUri, assertion, null);
            Assert.IsNotNull(result.AccessToken);
            
            try
            {
                await context.AcquireTokenByAuthorizationCodeAsync(string.Empty, TestConstants.DefaultRedirectUri, assertion, TestConstants.DefaultResource);
            }
            catch (ArgumentNullException exc)
            {
                Assert.AreEqual(exc.ParamName, "authorizationCode");
            }

            try
            {
                await context.AcquireTokenByAuthorizationCodeAsync(null, TestConstants.DefaultRedirectUri, assertion, TestConstants.DefaultResource);
            }
            catch (ArgumentNullException exc)
            {
                Assert.AreEqual(exc.ParamName, "authorizationCode");
            }

            try
            {
                await context.AcquireTokenByAuthorizationCodeAsync("some-code", null, assertion, TestConstants.DefaultResource);
            }
            catch (ArgumentNullException exc)
            {
                Assert.AreEqual(exc.ParamName, "redirectUri");
            }

            try
            {
                await context.AcquireTokenByAuthorizationCodeAsync("some-code", TestConstants.DefaultRedirectUri, (ClientAssertion)null, TestConstants.DefaultResource);
            }
            catch (ArgumentNullException exc)
            {
                Assert.AreEqual(exc.ParamName, "clientAssertion");
            }
        }

        [TestMethod]
        [Description("Positive Test for AcquireTokenOnBehalf with client credential")]
        public async Task AcquireTokenOnBehalfAndClientCredentialTest()
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, new TokenCache());
            string accessToken = "some-access-token";
            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultResource, TestConstants.DefaultClientId, TokenSubjectType.User,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId);
            context.TokenCache.tokenCacheDictionary[key] = new AuthenticationResultEx
            {
                RefreshToken = "some-rt",
                ResourceInResponse = TestConstants.DefaultResource,
                Result = new AuthenticationResult("Bearer", accessToken, DateTimeOffset.UtcNow)
            };

            ClientCredential clientCredential = new ClientCredential(TestConstants.DefaultClientId, TestConstants.DefaultClientSecret);
            try
            {
                await context.AcquireTokenAsync(null, clientCredential, new UserAssertion(accessToken));
            }
            catch (ArgumentNullException exc)
            {
                Assert.AreEqual(exc.ParamName, "resource");
            }

            try
            {
                await context.AcquireTokenAsync(TestConstants.DefaultResource, clientCredential, null);
            }
            catch (ArgumentNullException exc)
            {
                Assert.AreEqual(exc.ParamName, "userAssertion");
            }

            try
            {
                await context.AcquireTokenAsync(TestConstants.DefaultResource, (ClientCredential)null, new UserAssertion(accessToken));
            }
            catch (ArgumentNullException exc)
            {
                Assert.AreEqual(exc.ParamName, "clientCredential");
            }

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"access_token\":\"some-other-token\"}")
                },
                PostData = new Dictionary<string, string>()
                {
                    {"client_id", TestConstants.DefaultClientId},
                    {"grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"}
                }
            });

            var result = await context.AcquireTokenAsync(TestConstants.AnotherResource, clientCredential, new UserAssertion(accessToken));
            Assert.IsNotNull(result.AccessToken);
        }

        [TestMethod]
        [Description("Positive Test for AcquireTokenOnBehalf with client credential")]
        public async Task AcquireTokenOnBehalfAndClientCertificateCredentialTest()
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, new TokenCache());
            string accessToken = "some-access-token";
            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultResource, TestConstants.DefaultClientId, TokenSubjectType.User,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId);
            context.TokenCache.tokenCacheDictionary[key] = new AuthenticationResultEx
            {
                RefreshToken = "some-rt",
                ResourceInResponse = TestConstants.DefaultResource,
                Result = new AuthenticationResult("Bearer", accessToken, DateTimeOffset.UtcNow)
            };

            ClientAssertionCertificate clientCredential = new ClientAssertionCertificate(TestConstants.DefaultClientId, new X509Certificate2("valid_cert.pfx", TestConstants.DefaultPassword));
            try
            {
                await context.AcquireTokenAsync(null, clientCredential, new UserAssertion(accessToken));
            }
            catch (ArgumentNullException exc)
            {
                Assert.AreEqual(exc.ParamName, "resource");
            }

            try
            {
                await context.AcquireTokenAsync(TestConstants.DefaultResource, clientCredential, null);
            }
            catch (ArgumentNullException exc)
            {
                Assert.AreEqual(exc.ParamName, "userAssertion");
            }

            try
            {
                await context.AcquireTokenAsync(TestConstants.DefaultResource, (ClientAssertionCertificate)null, new UserAssertion(accessToken));
            }
            catch (ArgumentNullException exc)
            {
                Assert.AreEqual(exc.ParamName, "clientCertificate");
            }

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"access_token\":\"some-other-token\"}")
                },
                PostData = new Dictionary<string, string>()
                {
                    {"client_id", TestConstants.DefaultClientId},
                    {"grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"}
                }
            });

            var result = await context.AcquireTokenAsync(TestConstants.AnotherResource, clientCredential, new UserAssertion(accessToken));
            Assert.IsNotNull(result.AccessToken);
        }

        [TestMethod]
        [Description("Test for GetAuthorizationRequestURL")]
        public async Task GetAuthorizationRequestUrlTest()
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant);
            Uri uri = null;

            try
            {
                uri = await context.GetAuthorizationRequestUrlAsync(null, TestConstants.DefaultClientId, TestConstants.DefaultRedirectUri, new UserIdentifier(TestConstants.DefaultDisplayableId, UserIdentifierType.RequiredDisplayableId), "extra=123");
            }
            catch (ArgumentNullException ex)
            {
                Assert.AreEqual(ex.ParamName, "resource");
            }
            
            uri = await context.GetAuthorizationRequestUrlAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultRedirectUri, new UserIdentifier(TestConstants.DefaultDisplayableId, UserIdentifierType.RequiredDisplayableId), "extra=123");
            Assert.IsNotNull(uri);
            Assert.IsTrue(uri.AbsoluteUri.Contains("login_hint"));
            Assert.IsTrue(uri.AbsoluteUri.Contains("extra=123"));
            uri = await context.GetAuthorizationRequestUrlAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultRedirectUri, UserIdentifier.AnyUser, null);
            Assert.IsNotNull(uri);
            Assert.IsFalse(uri.AbsoluteUri.Contains("login_hint"));
            Assert.IsFalse(uri.AbsoluteUri.Contains("client-request-id="));
            context.CorrelationId = Guid.NewGuid();
            uri = await context.GetAuthorizationRequestUrlAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultRedirectUri, new UserIdentifier(TestConstants.DefaultDisplayableId, UserIdentifierType.RequiredDisplayableId), "extra");
            Assert.IsNotNull(uri);
            Assert.IsTrue(uri.AbsoluteUri.Contains("client-request-id="));
        }
    }
}
