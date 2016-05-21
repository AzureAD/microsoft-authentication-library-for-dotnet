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
            AuthenticationResult result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultRedirectUri, platformParameters);
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

            AuthenticationResult result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultRedirectUri, platformParameters);
            Verify.IsNotNull(result);
            Verify.AreEqual(result.AccessToken, "some-access-token");
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
            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultRedirectUri, platformParameters, UserIdentifier.AnyUser);
            Verify.IsNotNull(result);
            Verify.AreEqual(result.AccessToken, "some-access-token");
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
                await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultRedirectUri, platformParameters);
            }
            catch (ArgumentException ex)
            {
                Verify.AreEqual(ex.ParamName, "validateAuthority");
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
            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultRedirectUri, platformParameters, new UserIdentifier(TestConstants.DefaultDisplayableId, UserIdentifierType.RequiredDisplayableId));
            Assert.IsNotNull(result);
            Assert.AreEqual(result.AccessToken, "some-access-token");
            Assert.IsNotNull(result.UserInfo);
            //add handler to return failed discovery response
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateFailureResponseMessage("{\"error\":\"invalid_instance\",\"error_description\":\"AADSTS70002: Error in validating authority.\"}")
                });

            try
            {
                context = new AuthenticationContext("https://login.microsoft0nline.com/common");
                result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultRedirectUri, platformParameters);
            }
            catch (AdalException ex)
            {
                Verify.AreEqual(ex.ErrorCode, AdalError.AuthorityNotInValidList);
            }
        }
        
        [TestMethod]
        [Description("Negative Test for AcquireToken with invalid resource")]
        public async Task AcquireTokenWithInvalidResourceTestAsync()
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, new TokenCache());
            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant, TestConstants.DefaultResource, TestConstants.DefaultClientId, TokenSubjectType.User, TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId);
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
                await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultRedirectUri, platformParameters);
            }
            catch (AdalServiceException ex)
            {
                Verify.AreEqual(ex.ErrorCode, AdalError.AuthenticationCanceled);
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
            AuthenticationResult result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultRedirectUri, platformParameters);
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
            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAdfsAuthorityTenant, TestConstants.DefaultResource, TestConstants.DefaultClientId, TokenSubjectType.User, null, null);
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
            AuthenticationResult result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultRedirectUri, platformParameters);
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
            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant, TestConstants.DefaultResource, TestConstants.DefaultClientId, TokenSubjectType.User, TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId);
            context.TokenCache.tokenCacheDictionary[key] = new AuthenticationResultEx
            {
                RefreshToken = "some-rt",
                ResourceInResponse = TestConstants.DefaultResource,
                Result = new AuthenticationResult("Bearer", "existing-access-token", DateTimeOffset.UtcNow)
            };

            AuthenticationResult result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultRedirectUri, new PlatformParameters(PromptBehavior.Always));
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
                    Content = new StringContent("{\"ver\":\"1.0\",\"account_type\":\"Managed\",\"domain_name\":\"id.com\"}")
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
            AuthenticationResult result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, new UserPasswordCredential(TestConstants.DefaultDisplayableId, TestConstants.DefaultPassword));
            Assert.IsNotNull(result);
            Assert.AreEqual(TestConstants.DefaultAuthorityHomeTenant, context.Authenticator.Authority);
            Assert.AreEqual(result.AccessToken, "some-access-token");
            Assert.IsNotNull(result.UserInfo);
            Assert.AreEqual(TestConstants.DefaultDisplayableId, result.UserInfo.DisplayableId);
            Assert.AreEqual(TestConstants.DefaultUniqueId, result.UserInfo.UniqueId);
        }
        /*
        [TestMethod]
        [Description("Positive Test for Confidential Client")]
        public async Task ConfidentialClientWithX509Test()
        {
            SetCredential(sts);
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, sts.ValidateAuthority, TokenCacheType.Null);

            string authorizationCode = await context.AcquireAccessCodeAsync(TestConstants.DefaultResource, sts.ValidConfidentialClientId, sts.ValidRedirectUriForConfidentialClient, sts.ValidUserId);
            var certificate = new ClientAssertionCertificate(sts.ValidConfidentialClientId, CreateX509Certificate(sts.ConfidentialClientCertificateName, sts.ConfidentialClientCertificatePassword));
            RecorderJwtId.JwtIdIndex = 1;
            AuthenticationResult result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, sts.ValidRedirectUriForConfidentialClient, certificate, TestConstants.DefaultResource);
            VerifySuccessResult(sts, result);

            result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, sts.ValidRedirectUriForConfidentialClient, certificate);
            VerifySuccessResult(sts, result);

            result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, sts.ValidRedirectUriForConfidentialClient, certificate, null);
            VerifySuccessResult(sts, result);

            result = await context.AcquireTokenByAuthorizationCodeAsync(null, sts.ValidRedirectUriForConfidentialClient, certificate, TestConstants.DefaultResource);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "authorizationCode");

            result = await context.AcquireTokenByAuthorizationCodeAsync(string.Empty, sts.ValidRedirectUriForConfidentialClient, certificate, TestConstants.DefaultResource);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "authorizationCode");

            // Send null for redirect
            result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, null, certificate, TestConstants.DefaultResource);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "redirectUri");

            result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, sts.ValidRedirectUriForConfidentialClient, (ClientAssertionCertificate)null, TestConstants.DefaultResource);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "clientCertificate");
        }

        /*
        [TestMethod]
        [Description("Test for Client credential")]
        [TestCategory("AdalDotNetMock")]
        public async Task ClientCredentialTestAsync()
        {
            await AdalTests.ClientCredentialTestAsync(Sts);
        }

        [TestMethod]
        [Description("Test for Client assertion with X509")]
        [TestCategory("AdalDotNetMock")]
        public async Task ClientAssertionWithX509Test()
        {
            await AdalTests.ClientAssertionWithX509TestAsync(Sts);
        }

        [TestMethod]
        [Description("Test for Confidential Client with self signed jwt")]
        [TestCategory("AdalDotNetMock")]
        public async Task ConfidentialClientWithJwtTest()
        {
            await AdalTests.ConfidentialClientWithJwtTestAsync(Sts);
        }

        [TestMethod]
        [Description("Positive Test for Client assertion with self signed Jwt")]
        [TestCategory("AdalDotNetMock")]
        public async Task ClientAssertionWithSelfSignedJwtTest()
        {
            await AdalTests.ClientAssertionWithSelfSignedJwtTestAsync(Sts);
        }

        [TestMethod]
        [Description("Positive Test for Confidential Client")]
        [TestCategory("AdalDotNetMock")]
        public async Task ConfidentialClientTest()
        {
            await AdalTests.ConfidentialClientTestAsync(Sts);
        }

        [TestMethod]
        [TestCategory("AdalDotNet")]
        [Description("Negative Test for AcquireToken with PromptBehavior.Never")]
        public async Task AcquireTokenWithPromptBehaviorNeverTestAsync()
        {
            await AdalTests.AcquireTokenWithPromptBehaviorNeverTestAsync(Sts);
        }

        [TestMethod]
        [Description("Positive Test for AcquireTokenOnBehalf with client credential")]
        [TestCategory("AdalDotNetMock")]
        public async Task AcquireTokenOnBehalfAndClientCredentialTest()
        {
            await AdalTests.AcquireTokenOnBehalfAndClientCredentialTestAsync(Sts);
        }

        [TestMethod]
        [Description("Positive Test for AcquireTokenOnBehalf with client certificate")]
        [TestCategory("AdalDotNetMock")]
        public async Task AcquireTokenOnBehalfAndClientCertificateTest()
        {
            await AdalTests.AcquireTokenOnBehalfAndClientCertificateTestAsync(Sts);
        }

        [TestMethod]
        [Description("Positive Test for AcquireTokenOnBehalf with client assertion")]
        [TestCategory("AdalDotNetMock")]
        public async Task AcquireTokenOnBehalfAndClientAssertionTest()
        {
            await AdalTests.AcquireTokenOnBehalfAndClientAssertionTestAsync(Sts);
        }

        [TestMethod]
        [TestCategory("AdalDotNet")]
        [Description("Test for cache in multi user scenario")]
        public async Task MultiUserCacheTest()
        {
            await AdalTests.MultiUserCacheTestAsync(Sts);
        }

        [TestMethod]
        [TestCategory("AdalDotNet")]
        [Description("Test for switching user in multi user scenario")]
        public async Task SwitchUserTest()
        {
            await AdalTests.SwitchUserTestAsync(Sts);
        }

        [TestMethod]
        [Description("Test for cache expiration margin")]
        [TestCategory("AdalDotNetMock")]
        public async Task CacheExpirationMarginTest()
        {
            await AdalTests.CacheExpirationMarginTestAsync(Sts);
        }

        [TestMethod]
        [Description("Test for client assertion in multi threaded scenario")]
        [TestCategory("AdalDotNet")]
        public async Task MultiThreadedClientAssertionWithX509Test()
        {
            await AdalTests.MultiThreadedClientAssertionWithX509TestAsync(Sts);
        }

        [TestMethod]
        [Description("Test for token cache usage in AcquireTokenByAuthorizationCode")]
        [TestCategory("AdalDotNetMock")]
        public async Task AcquireTokenByAuthorizationCodeWithCacheTest()
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, sts.ValidateAuthority);
            var credential = new ClientCredential(sts.ValidConfidentialClientId, sts.ValidConfidentialClientSecret);

            AuthenticationResult result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, sts.ValidRedirectUriForConfidentialClient, credential);
            AuthenticationContext.Delay(2000);
            AuthenticationResult result2 = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode2, sts.ValidRedirectUriForConfidentialClient, credential);
            VerifySuccessResult(sts, result, true, false);
            VerifySuccessResult(sts, result2, true, false);
            VerifyExpiresOnAreNotEqual(result, result2);

            AuthenticationResult result3 = await context.AcquireTokenSilentAsync(TestConstants.DefaultResource, credential, UserIdentifier.AnyUser);
            VerifyErrorResult(result3, "multiple_matching_tokens_detected", null);

            AuthenticationResult result4 = await context.AcquireTokenSilentAsync(TestConstants.DefaultResource, credential, sts.ValidUserId);
            AuthenticationResult result5 = await context.AcquireTokenSilentAsync(TestConstants.DefaultResource, credential, sts.ValidRequiredUserId2);
            VerifySuccessResult(sts, result4, true, false);
            VerifySuccessResult(sts, result5, true, false);
            VerifyExpiresOnAreEqual(result4, result);
            VerifyExpiresOnAreEqual(result5, result2);
            VerifyExpiresOnAreNotEqual(result4, result5);
        }

        [TestMethod]
        [Description("Test for token refresh for confidnetial client using Multi Resource Refresh Token (MRRT) in cache")]
        [TestCategory("AdalDotNetMock")]
        public async Task ConfidentialClientTokenRefreshWithMrrtTest()
        {
            SetCredential(sts);
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, sts.ValidateAuthority);

            string authorizationCode = await context.AcquireAccessCodeAsync(TestConstants.DefaultResource, sts.ValidConfidentialClientId, sts.ValidRedirectUriForConfidentialClient, sts.ValidUserId);

            var credential = new ClientCredential(sts.ValidConfidentialClientId, sts.ValidConfidentialClientSecret);

            AuthenticationResult result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, sts.ValidRedirectUriForConfidentialClient, credential);
            VerifySuccessResult(sts, result);

            AuthenticationResult result2 = await context.AcquireTokenSilentAsync(TestConstants.DefaultResource2, credential, UserIdentifier.AnyUser);
            VerifySuccessResult(sts, result2, true, false);

            AuthenticationContext.ClearDefaultCache();

            result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, sts.ValidRedirectUriForConfidentialClient, credential);
            VerifySuccessResult(sts, result);

            result2 = await context.AcquireTokenSilentAsync(TestConstants.DefaultResource, credential, UserIdentifier.AnyUser);
            VerifySuccessResult(sts, result2, true, false);

            result2 = await context.AcquireTokenSilentAsync(TestConstants.DefaultResource2, sts.ValidConfidentialClientId);
            VerifyErrorResult(result2, AdalError.FailedToAcquireTokenSilently, null);

            result2 = await context.AcquireTokenSilentAsync(TestConstants.DefaultResource2, credential, UserIdentifier.AnyUser);
            VerifySuccessResult(sts, result2, true, false);
        }

        [TestMethod]
        [Description("Test for different token subject types (Client, User, ClientPlusUser)")]
        [TestCategory("AdalDotNetMock")]
        public async Task TokenSubjectTypeTest()
        {
            await AdalTests.TokenSubjectTypeTestAsync(Sts);
        }

        [TestMethod]
        [Description("Test for GetAuthorizationRequestURL")]
        [TestCategory("AdalDotNetMock")]
        public async Task GetAuthorizationRequestUrlTest()
        {
            await AdalTests.GetAuthorizationRequestURLTestAsync(Sts);
        }

#if TEST_ADAL_NET
        [TestMethod]
        [Description("Test for mixed case username and cache")]
        [TestCategory("AdalDotNetMock")]
        public async Task MixedCaseUserNameTest()
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, sts.ValidateAuthority);
            UserCredentialProxy credential = new UserCredentialProxy(sts.ValidUserName3, sts.ValidPassword3);
            AuthenticationResult result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, credential);
            VerifySuccessResult(sts, result);
            Verify.IsNotNull(result.UserInfo);
            Verify.AreNotEqual(result.UserInfo.DisplayableId, result.UserInfo.DisplayableId.ToLower());
            AuthenticationContext.Delay(2000);   // 2 seconds delay
            AuthenticationResult result2 = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, credential);
            VerifySuccessResult(sts, result2);
            Verify.IsTrue(AreDateTimeOffsetsEqual(result.ExpiresOn, result2.ExpiresOn));
        }
        
        [TestMethod]
        [Description("Positive Test for AcquireToken with valid user credentials")]
        [TestCategory("AdalDotNet")]
        public async Task ResourceOwnerCredentialsTest()
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, sts.ValidateAuthority);
            UserCredentialProxy credential = new UserCredentialProxy(sts.ValidUserName, sts.ValidPassword);
            AuthenticationResult result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, credential);
            VerifySuccessResult(sts, result);
            Verify.IsNotNull(result.UserInfo);

            // TODO: Figure out if we should we use mixed case user name to run tests?
            // Verify.AreNotEqual(result.UserInfo.DisplayableId, result.UserInfo.DisplayableId.ToLower());

            AuthenticationContext.Delay(2000);   // 2 seconds delay
            AuthenticationResult result2 = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, credential);
            VerifySuccessResult(sts, result2);
            Verify.IsTrue(AreDateTimeOffsetsEqual(result.ExpiresOn, result2.ExpiresOn));
        }
#endif

        private static void VerifyErrorDescriptionContains(string errorDescription, string keyword)
        {
            Log.Comment(string.Format(CultureInfo.CurrentCulture, " Verifying error description '{0}'...", errorDescription));
            Verify.IsGreaterThanOrEqual(errorDescription.IndexOf(keyword, StringComparison.OrdinalIgnoreCase), 0);
        }

        private static void ValidateAuthenticationResultsAreEqual(AuthenticationResult result, AuthenticationResult result2)
        {
            Verify.AreEqual(result.AccessToken, result2.AccessToken, "AuthenticationResult.AccessToken");
            Verify.AreEqual(result.UserInfo.UniqueId, result2.UserInfo.UniqueId);
            Verify.AreEqual(result.UserInfo.DisplayableId, result2.UserInfo.DisplayableId);
            Verify.AreEqual(result.UserInfo.GivenName, result2.UserInfo.GivenName);
            Verify.AreEqual(result.UserInfo.FamilyName, result2.UserInfo.FamilyName);
            Verify.AreEqual(result.TenantId, result2.TenantId);
        }

        public static void VerifySuccessResult(AuthenticationResult result, bool supportRefreshToken = true, bool supportUserInfo = true)
        {
            Log.Comment("Verifying success result...");
            if (result.Status != AuthenticationStatusProxy.Success)
            {
                Log.Comment(string.Format(CultureInfo.CurrentCulture, " Unexpected '{0}' error from service: {1}", result.Error, result.ErrorDescription));
            }

            Verify.AreEqual(AuthenticationStatusProxy.Success, result.Status, "AuthenticationResult.Status");
            Verify.IsNotNullOrEmptyString(result.AccessToken, "AuthenticationResult.AccessToken");

            Verify.IsNullOrEmptyString(result.Error, "AuthenticationResult.Error");
            Verify.IsNullOrEmptyString(result.ErrorDescription, "AuthenticationResult.ErrorDescription");

            if (sts.Type != StsType.ADFS && supportUserInfo)
            {
                Action<string, string, bool> ValidateUserInfo = (string field, string caption, bool required) =>
                {
                    if (string.IsNullOrEmpty(field))
                    {
                        if (required)
                        {
                            Log.Error("No " + caption);
                        }
                        else
                        {
                            Log.Warning("No " + caption);
                        }
                    }
                    else
                    {
                        Log.Comment(field, caption);
                    }
                };

                ValidateUserInfo(result.TenantId, "tenant id", true);
                ValidateUserInfo(result.UserInfo.UniqueId, "user unique id", true);
                ValidateUserInfo(result.UserInfo.DisplayableId, "user displayable id", true);
                ValidateUserInfo(result.UserInfo.IdentityProvider, "identity provider", true);
                ValidateUserInfo(result.UserInfo.GivenName, "given name", false);
                ValidateUserInfo(result.UserInfo.FamilyName, "family name", false);
            }

            long expiresIn = (long)(result.ExpiresOn - DateTime.UtcNow).TotalSeconds;
            Log.Comment("Verifying token expiration...");
            Verify.IsGreaterThanOrEqual(expiresIn, (long)0, "Token ExpiresOn");
        }*/


        public static void VerifyExpiresOnAreEqual(AuthenticationResult result, AuthenticationResult result2)
        {
            bool equal = AreDateTimeOffsetsEqual(result.ExpiresOn, result2.ExpiresOn);

            if (!equal)
            {
                Log.Comment(result.ExpiresOn.ToString("R", CultureInfo.InvariantCulture) + " <> " + result2.ExpiresOn.ToString("R", CultureInfo.InvariantCulture));
            }

            Verify.IsTrue(equal, "AuthenticationResult.ExpiresOn");
        }

        public static void VerifyExpiresOnAreNotEqual(AuthenticationResult result, AuthenticationResult result2)
        {
            bool equal = AreDateTimeOffsetsEqual(result.ExpiresOn, result2.ExpiresOn);

            if (equal)
            {
                Log.Comment(result.ExpiresOn.ToString("R", CultureInfo.InvariantCulture) + " <> " + result2.ExpiresOn.ToString("R", CultureInfo.InvariantCulture));
            }

            Verify.IsFalse(equal, "AuthenticationResult.ExpiresOn");
        }

        public static bool AreDateTimeOffsetsEqual(DateTimeOffset time1, DateTimeOffset time2)
        {
            bool equal = (time1.Ticks / 10000 == time2.Ticks / 10000);
            if (!equal)
            {
                Log.Comment("DateTimeOffsets with ticks {0} and {1} are not equal", time1.Ticks, time2.Ticks);
            }

            return equal;
        }
    }
}
