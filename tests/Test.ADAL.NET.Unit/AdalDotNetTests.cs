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
using System.Globalization;
using System.Threading.Tasks;

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.ADAL.Common;
using Test.ADAL.NET.Unit;

namespace Test.ADAL.NET
{
    [TestClass]
    [DeploymentItem("valid_cert.pfx")]
    [DeploymentItem("invalid_cert.pfx")]
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
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, true);
            AuthenticationResult result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultRedirectUri, platformParameters, sts.ValidUserId);
            VerifySuccessResult(sts, result);
        }
        
        [TestMethod]
        [TestCategory("AdalDotNet")]
        [Description("Positive Test for AcquireToken with missing redirectUri and/or userId")]
        public async Task AcquireTokenPositiveWithoutRedirectUriOrUserIdAsync()
        {
            AuthenticationContext.SetCredentials(TestConstants.DefaultDisplayableId, TestConstants.DefaultPassword);
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant);

            AuthenticationResult result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, platformParameters);
            VerifySuccessResult(sts, result);

            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, platformParameters, null);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "userId");
            VerifyErrorResult(result, Sts.InvalidArgumentError, "UserIdentifier.AnyUser");

            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, platformParameters, UserIdentifier.AnyUser);
            VerifySuccessResult(sts, result);
        }

        [TestMethod]
        [Description("Test for autority validation to AuthenticationContext")]
        [TestCategory("AdalDotNetMock")]
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

            context = new AuthenticationContext("https://login.microsoft0nline.com/common");
            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, platformParameters, sts.ValidUserId);
            VerifyErrorResult(result, Sts.AuthenticationUiFailedError, "authentication dialog");
            context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, false);
            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, platformParameters, sts.ValidUserId);
            VerifySuccessResult(sts, result);

            //whitelisted authority
            context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, true);
            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, platformParameters, sts.ValidUserId);
            VerifySuccessResult(sts, result);

            try
            {
                context = new AuthenticationContext(sts.InvalidAuthority);
                Verify.AreNotEqual(sts.Type, StsType.ADFS);
                result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, platformParameters, sts.ValidUserId);
                VerifyErrorResult(result, TestConstants.DefaultAuthorityCommonTenantNotInValidList, "authority");
            }
            catch (ArgumentException ex)
            {
                Verify.AreEqual(sts.Type, StsType.ADFS);
                Verify.AreEqual(ex.ParamName, "validateAuthority");
            }

            context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant + "/extraPath1/extraPath2", sts.ValidateAuthority);
            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, platformParameters, sts.ValidUserId);
            VerifySuccessResult(sts, result);
        }

        [TestMethod]
        [Description("Test for AcquireToken with redirectUri")]
        [TestCategory("AdalDotNetMock")]
        public async Task AcquireTokenWithRedirectUriTestAsync()
        {
            SetCredential(sts);
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, sts.ValidateAuthority);

            AuthenticationResult result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, sts.InvalidExistingRedirectUri, platformParameters, sts.ValidUserId);
            VerifyErrorResult(result, Sts.AuthenticationCanceledError, null);

            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, sts.InvalidNonExistingRedirectUri, platformParameters, sts.ValidUserId);
            VerifyErrorResult(result, Sts.AuthenticationCanceledError, null);

            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, new Uri(sts.ValidNonExistingRedirectUri.OriginalString + "#fragment"), platformParameters, sts.ValidUserId);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "redirectUri");
            VerifyErrorResult(result, Sts.InvalidArgumentError, "fragment");

            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, null, platformParameters, sts.ValidUserId);
            if (TestType != TestType.WinRT)
            {
                VerifyErrorResult(result, Sts.InvalidArgumentError, "redirectUri");
            }
            else
            {
                // Winrt can send null redirecturi
                VerifySuccessResult(sts, result);
            }

            AuthenticationContext.ClearDefaultCache();
            EndBrowserDialogSession();
            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientIdWithExistingRedirectUri, sts.ValidExistingRedirectUri, platformParameters, sts.ValidUserId);
            VerifySuccessResult(sts, result);

            AuthenticationContext.ClearDefaultCache();

            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, sts.ValidNonExistentRedirectUriClientId, sts.ValidNonExistingRedirectUri, platformParameters, sts.ValidUserId);
            VerifySuccessResult(sts, result);
        }
        
        [TestMethod]
        [Description("Negative Test for AcquireToken with invalid authority")]
        [TestCategory("AdalDotNetMock")]
        public async Task AcquireTokenWithInvalidAuthorityTestAsync()
        {
            SetCredential(sts);
            var context = new AuthenticationContext("https://www.outlook.com/login", false);
            AuthenticationResult result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, platformParameters, sts.ValidUserId);
            VerifyErrorResult(result, Sts.AuthenticationUiFailedError, null);

            context = new AuthenticationContext(sts.InvalidAuthority, false);
            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, platformParameters, sts.ValidUserId);
            VerifyErrorResult(result, Sts.AuthenticationUiFailedError, null);

            if (sts.Type != StsType.ADFS)
            {
                Uri uri = new Uri(TestConstants.DefaultAuthorityCommonTenant);
                context = new AuthenticationContext(string.Format(CultureInfo.CurrentCulture, "{0}://{1}/non_existing_tenant", uri.Scheme, uri.Authority));
                result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, platformParameters, sts.ValidUserId);
                VerifyErrorResult(result, Sts.AuthenticationCanceledError, null);
            }
        }

        [TestMethod]
        [Description("Negative Test for AcquireToken with invalid resource")]
        [TestCategory("AdalDotNetMock")]
        public async Task AcquireTokenWithInvalidResourceTestAsync()
        {
            SetCredential(sts);
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, sts.ValidateAuthority);
            AuthenticationResult result = await context.AcquireTokenAsync(sts.InvalidResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, platformParameters, sts.ValidUserId);
            VerifyErrorResult(result, Sts.InvalidResourceError, "resource");

            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, platformParameters, sts.ValidUserId);
            VerifySuccessResult(sts, result);

            result = await context.AcquireTokenAsync(TestConstants.DefaultResource.ToUpper(), TestConstants.DefaultClientId.ToUpper(), TestConstants.DefaultResource, platformParameters,
                (sts.Type == StsType.AAD) ? new UserIdentifier(sts.ValidUserName, UserIdentifierType.RequiredDisplayableId) : UserIdentifier.AnyUser);
            VerifySuccessResult(sts, result);

            result = await context.AcquireTokenAsync(TestConstants.DefaultResource.ToUpper(), TestConstants.DefaultClientId.ToUpper(), TestConstants.DefaultResource, platformParameters,
                (result.UserInfo != null) ? new UserIdentifier(result.UserInfo.UniqueId, UserIdentifierType.UniqueId) : UserIdentifier.AnyUser);
            VerifySuccessResult(sts, result);
        }

        [TestMethod]
        [Description("Negative Test for AcquireToken with invalid client id")]
        [TestCategory("AdalDotNetMock")]
        public static async Task AcquireTokenWithInvalidClientIdTestAsync()
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, sts.ValidateAuthority);
            AuthenticationResult result = await context.AcquireTokenAsync(TestConstants.DefaultResource, sts.InvalidClientId, TestConstants.DefaultResource, platformParameters, sts.ValidUserId);
            VerifyErrorResult(result, Sts.AuthenticationCanceledError, null);
        }

        [TestMethod]
        [Description("Negative Test for AcquireToken with incorrect user credential")]
        [TestCategory("AdalDotNetMock")]
        public static async Task AcquireTokenWithIncorrectUserCredentialTestAsync()
        {
            AuthenticationContext.SetCredentials(sts.InvalidUserName, "invalid_password");
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, sts.ValidateAuthority);
            AuthenticationResult result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, platformParameters, UserIdentifier.AnyUser, "incorrect_user");
            VerifyErrorResult(result, Sts.AuthenticationCanceledError, "canceled");
        }



        [TestMethod]
        [Description("Positive Test for AcquireToken")]
        [TestCategory("AdalDotNetMock")]
        public async Task ExtraQueryParametersTestAsync()
        {
            SetCredential(sts);
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, sts.ValidateAuthority, TokenCacheType.Null);
            AuthenticationResult result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, platformParameters, sts.ValidUserId, null);
            VerifySuccessResult(sts, result);

            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, platformParameters, sts.ValidUserId, "redirect_uri=123");
            VerifyErrorResult(result, "duplicate_query_parameter", "redirect_uri");

            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, platformParameters, sts.ValidUserId, "resource=123&dummy=dummy_value#$%^@%^^%");
            VerifyErrorResult(result, "duplicate_query_parameter", "resource");

            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, platformParameters, sts.ValidUserId, "client_id=123");
            VerifyErrorResult(result, "duplicate_query_parameter", "client_id");

            EndBrowserDialogSession();
            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, platformParameters, sts.ValidUserId, "login_hint=123");
            VerifyErrorResult(result, "duplicate_query_parameter", "login_hint");

            EndBrowserDialogSession();
            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, platformParameters, sts.ValidUserId, "login_hintx=123");
            VerifySuccessResult(sts, result);

            EndBrowserDialogSession();
            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, platformParameters, UserIdentifier.AnyUser, "login_hint=" + sts.ValidUserName);
            VerifySuccessResult(sts, result);

            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, platformParameters, sts.ValidUserId, string.Empty);
            VerifySuccessResult(sts, result);
        }

        [TestMethod]
        [Description("Negative Test for AcquireToken with user canceling authentication")]
        [TestCategory("AdalDotNetMock")]
        public async Task AcquireTokenWithAuthenticationCanceledTest()
        {
            // ADFS security dialog hang up
            await AdalTests.AcquireTokenWithAuthenticationCanceledTestAsync(Sts);
        }

        [TestMethod]
        [Description("Positive Test for AcquireToken testing default token cache")]
        [TestCategory("AdalDotNetMock")]
        public async Task AcquireTokenPositiveWithDefaultCacheTest()
        {
            await AdalTests.AcquireTokenPositiveWithDefaultCacheTestAsync(Sts);
        }

        [TestMethod]
        [Description("Positive Test for AcquireToken testing custom in memory token cache")]
        [TestCategory("AdalDotNetMock")]
        public async Task AcquireTokenPositiveWithInMemoryCacheTest()
        {
            await AdalTests.AcquireTokenPositiveWithInMemoryCacheTestAsync(Sts);
        }

        [TestMethod]
        [Description("Positive Test for AcquireToken testing default token cache")]
        [TestCategory("AdalDotNetMock")]
        [Ignore]    // Enable once the test bug is fixed.
        public async Task AcquireTokenPositiveWithNullCacheTest()
        {
            await AdalTests.AcquireTokenPositiveWithNullCacheTestAsync(Sts);
        }

        [TestMethod]
        [Description("Test for UserInfo")]
        [TestCategory("AdalDotNetMock")]
        public async Task UserInfoTest()
        {
            await AdalTests.UserInfoTestAsync(Sts);
        }

        [TestMethod]
        [Description("Test for multi resource refresh token")]
        [TestCategory("AdalDotNetMock")]
        public async Task MultiResourceRefreshTokenTest()
        {
            await AdalTests.MultiResourceRefreshTokenTestAsync(Sts);
        }

        [TestMethod]
        [Description("Test for acquring token using tenantless endpoint")]
        [TestCategory("AdalDotNetMock")]
        public async Task TenantlessTest()
        {
            var context = new AuthenticationContext(sts.TenantlessAuthority, sts.ValidateAuthority);
            AuthenticationResult result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, platformParameters, sts.ValidUserId);
            VerifySuccessResult(sts, result);
            Verify.IsNotNullOrEmptyString(result.TenantId);

            AuthenticationContext.SetCredentials(null, null);
            AuthenticationResult result2 = await context.AcquireTokenAsync(
                TestConstants.DefaultResource,
                TestConstants.DefaultClientId,
                TestConstants.DefaultResource,
                platformParameters,
                sts.ValidUserId);

            ValidateAuthenticationResultsAreEqual(result, result2);

            SetCredential(sts);
            context = new AuthenticationContext(sts.TenantlessAuthority.Replace("Common", result.TenantId), sts.ValidateAuthority, TokenCacheType.Null);
            result2 = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, platformParameters, sts.ValidUserId);
            VerifySuccessResult(sts, result2);
        }

        [TestMethod]
        [Description("Test for STS Instance Discovery")]
        [TestCategory("AdalDotNetMock")]
        public async Task InstanceDiscoveryTest()
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, sts.ValidateAuthority);
            AuthenticationResult result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, platformParameters, sts.ValidUserId);
            VerifySuccessResult(sts, result);

            AuthenticationContext.SetEnvironmentVariable("ExtraQueryParameter", string.Empty);

            // PROD discovery endpoint knows about PPE as well, so this passes discovery and fails later as refresh token is invalid for PPE.
            context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant.Replace("windows.net", "windows-ppe.net"), sts.ValidateAuthority);
            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, new ClientCredential(TestConstants.DefaultClientId, sts.ValidPassword));
            VerifyErrorResult(result, "invalid_request", "No service namespace");

            context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant.Replace("windows.net", "windows.unknown"), sts.ValidateAuthority);
            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, platformParameters, sts.ValidUserId);
            VerifyErrorResult(result, "authority_not_in_valid_list", "authority");
        }

        [TestMethod]
        [TestCategory("AdalDotNet")]
        [Description("Test for Force Prompt")]
        public  async Task ForcePromptTestAsync(Sts sts)
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, sts.ValidateAuthority);
            AuthenticationResult result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, platformParameters, sts.ValidUserId);
            VerifySuccessResult(sts, result);

            AuthenticationContext.SetCredentials(null, null);
            AuthenticationResult result2 = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, platformParameters,
                (sts.Type == StsType.ADFS) ? null : sts.ValidUserId);
            VerifySuccessResult(sts, result2);
            Verify.AreEqual(result2.AccessToken, result.AccessToken);

            AuthenticationContext.SetCredentials(sts.ValidUserName, sts.ValidPassword);
            var neverAuthorizationParameters = new platformParameters(PromptBehavior.Always, null);
            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, neverAuthorizationParameters);
            VerifySuccessResult(sts, result);
            Verify.AreNotEqual(result2.AccessToken, result.AccessToken);
        }


#if TEST_ADAL_NET
        [TestMethod]
        [Description("Positive Test for AcquireToken non-interactive")]
        [TestCategory("AdalDotNet")]
        public async Task AcquireTokenNonInteractivePositiveTestAsync()
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, sts.ValidateAuthority);
            UserCredentialProxy credential = new UserCredentialProxy(sts.ValidUserName, sts.ValidPassword);
            AuthenticationResult result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, credential);
            VerifySuccessResult(sts, result);
            Verify.IsNotNull(result.UserInfo);
            Verify.IsNotNullOrEmptyString(result.UserInfo.UniqueId);
            Verify.IsNotNullOrEmptyString(result.UserInfo.DisplayableId);

            AuthenticationContext.Delay(2000);

            // Test token cache
            AuthenticationResult result2 = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, credential);
            VerifySuccessResult(sts, result2);
            VerifyExpiresOnAreEqual(result, result2);
        }
#endif


        [TestMethod]
        [Description("Positive Test for AcquireToken using federated tenant and then refreshing the session")]
        [TestCategory("AdalDotNet")]
        public async Task AcquireTokenAndRefreshSessionTest()
        {
            var userId = sts.ValidUserId;

            AuthenticationContext.SetCredentials(userId.Id, sts.ValidPassword);
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, false, TokenCacheType.InMemory);
            AuthenticationResult result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, platformParameters, userId);
            VerifySuccessResult(sts, result);
            AuthenticationContext.Delay(2000);
            var refreshSessionAuthorizationParameters = new platformParameters(PromptBehavior.RefreshSession, null);
            AuthenticationResult result2 = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, refreshSessionAuthorizationParameters, userId);
            VerifySuccessResult(sts, result2);
            Verify.AreNotEqual(result.AccessToken, result2.AccessToken);
        }

        [TestMethod]
        [Description("Positive Test for AcquireToken using federated tenant")]
        [TestCategory("AdalDotNet")]
        public async Task AcquireTokenPositiveWithFederatedTenantTest()
        {
            var userId = sts.ValidUserId;

            AuthenticationContext.SetCredentials(userId.Id, sts.ValidPassword);
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, false, TokenCacheType.Null);
            AuthenticationResult result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, platformParameters, userId);
            VerifySuccessResult(sts, result);

            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, platformParameters, UserIdentifier.AnyUser);
            VerifySuccessResult(sts, result);
        }

        [TestMethod]
        [Description("Correlation Id test")]
        [TestCategory("AdalDotNet")]
        public async Task CorrelationIdTest()
        {
            await AdalTests.CorrelationIdTestAsync(Sts);
        }

        [TestMethod]
        [Description("Test for reading WebException as inner exception")]
        [TestCategory("AdalDotNetMock")]
        public async Task InnerExceptionAccessTest()
        {
            SetCredential(sts);
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, sts.ValidateAuthority);
            AuthenticationResult result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, platformParameters, sts.ValidUserId);
            VerifySuccessResult(sts, result);
            result = await context.AcquireTokenSilentAsync(TestConstants.DefaultResource, sts.InvalidClientId);
            VerifyErrorResult(result, "failed_to_acquire_token_silently", null);
            Verify.IsNotNull(result.Exception);
        }

        [TestMethod]
        [Description("Positive Test for Confidential Client")]
        [TestCategory("AdalDotNetMock")]
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
        [Description("Positive Test for AcquireToken from cache only")]
        [TestCategory("AdalDotNetMock")]
        public async Task AcquireTokenFromCacheTest()
        {
            await AdalTests.AcquireTokenFromCacheTestAsync(Sts);
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

        [TestMethod]
        [Description("Test for logging in ADAL")]
        [TestCategory("AdalDotNetMock")]
        public async Task LoggerTest()
        {
            await AdalTests.LoggerTestAsync(Sts);
        }

        [TestMethod]
        [Description("Test for non-interactive federation with MSA")]
        [TestCategory("AdalDotNet")]
        public async Task MsaTest()
        {
            await AdalTests.MsaTestAsync();
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
        }


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
