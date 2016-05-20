/*
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
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Test.ADAL.Common;
using Test.ADAL.NET.Unit;

namespace Test.ADAL.NET.Unit
{
    internal class AdalTests
    {
        public static async Task ConfidentialClientTestAsync()
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, sts.ValidateAuthority);

            string authorizationCode = await context.AcquireAccessCodeAsync(TestConstants.DefaultResource, sts.ValidConfidentialClientId, sts.ValidRedirectUriForConfidentialClient, sts.ValidUserId);

            var credential = new ClientCredential(sts.ValidConfidentialClientId, sts.ValidConfidentialClientSecret);

            AuthenticationResult result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, sts.ValidRedirectUriForConfidentialClient, credential);
            VerifySuccessResult(sts, result);

            AuthenticationContext.Delay(2000);   // 2 seconds delay
            context.SetCorrelationId(new Guid("2ddbba59-1a04-43fb-b363-7fb0ae785031"));

            // Test cache usage in AcquireTokenByAuthorizationCodeAsync
            // There is no cache lookup, so the results should be different.
            AuthenticationResult result2 = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, sts.ValidRedirectUriForConfidentialClient, credential);
            VerifySuccessResult(sts, result2);
            Verify.AreNotEqual(result.AccessToken, result2.AccessToken);
            AuthenticationContext.ClearDefaultCache();

            result = await context.AcquireTokenByAuthorizationCodeAsync(null, sts.ValidRedirectUriForConfidentialClient, credential);
            VerifyErrorResult(result, "invalid_argument", "authorizationCode");

            result = await context.AcquireTokenByAuthorizationCodeAsync(string.Empty, sts.ValidRedirectUriForConfidentialClient, credential);
            VerifyErrorResult(result, "invalid_argument", "authorizationCode");

            result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode + "x", sts.ValidRedirectUriForConfidentialClient, credential);
            VerifyErrorResult(result, "invalid_grant", "authorization code");

            result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, new Uri(sts.ValidRedirectUriForConfidentialClient.OriginalString + "x"), credential);

            VerifyErrorResult(result, "invalid_grant", "does not match the reply address", 400, "70002");

            result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, sts.ValidRedirectUriForConfidentialClient, (ClientCredential)null);
            VerifyErrorResult(result, "invalid_argument", "credential");

            var invalidCredential = new ClientCredential(sts.ValidConfidentialClientId, sts.ValidConfidentialClientSecret + "x");
            result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, sts.ValidRedirectUriForConfidentialClient, invalidCredential);
            VerifyErrorResult(result, "invalid_client", "client secret", 401);
        }



        public static async Task ClientCredentialTestAsync(Sts sts)
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, sts.ValidateAuthority);

            AuthenticationResult result = null;

            var credential = new ClientCredential(sts.ValidConfidentialClientId, sts.ValidConfidentialClientSecret);
            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, credential);
            Verify.IsNotNullOrEmptyString(result.AccessToken);
            AuthenticationContext.Delay(2000);   // 2 seconds delay
            var result2 = await context.AcquireTokenAsync(TestConstants.DefaultResource, credential);
            Verify.IsNotNullOrEmptyString(result2.AccessToken);
            VerifyExpiresOnAreEqual(result, result2);

            result = await context.AcquireTokenAsync(null, credential);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "resource");

            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, (ClientCredential)null);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "clientCredential");

            context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, sts.ValidateAuthority, TokenCacheType.Null);
            var invalidCredential = new ClientCredential(sts.ValidConfidentialClientId, sts.ValidConfidentialClientSecret + "x");
            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, invalidCredential);
            VerifyErrorResult(result, Sts.InvalidClientError, "70002");

            invalidCredential = new ClientCredential(sts.ValidConfidentialClientId.Replace("0", "1"), sts.ValidConfidentialClientSecret + "x");
            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, invalidCredential);
            VerifyErrorResult(result, Sts.UnauthorizedClient, "70001", 400);
        }

        public static async Task ClientAssertionWithX509TestAsync(Sts sts)
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, sts.ValidateAuthority, TokenCacheType.Null);

            AuthenticationResult result = null;

            var certificate = new ClientAssertionCertificate(sts.ValidConfidentialClientId, CreateX509Certificate(sts.ConfidentialClientCertificateName, sts.ConfidentialClientCertificatePassword));
            RecorderJwtId.JwtIdIndex = 2;
            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, certificate);
            Verify.IsNotNullOrEmptyString(result.AccessToken);

            result = await context.AcquireTokenAsync(null, certificate);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "resource");

            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, (ClientAssertionCertificate)null);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "clientCertificate");

            var invalidCertificate = new ClientAssertionCertificate(sts.ValidConfidentialClientId, CreateX509Certificate(sts.InvalidConfidentialClientCertificateName, sts.ConfidentialClientCertificatePassword));
            RecorderJwtId.JwtIdIndex = 3;
            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, invalidCertificate);
            VerifyErrorResult(result, Sts.InvalidClientError, null, 0, "50012");

            invalidCertificate = new ClientAssertionCertificate(sts.InvalidClientId, CreateX509Certificate(sts.ConfidentialClientCertificateName, sts.ConfidentialClientCertificatePassword));
            RecorderJwtId.JwtIdIndex = 4;
            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, invalidCertificate);
            VerifyErrorResult(result, Sts.UnauthorizedClient, null, 400, "70001");
        }

        public static async Task ConfidentialClientWithJwtTestAsync(Sts sts)
        {
            SetCredential(sts);
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, sts.ValidateAuthority);

            AuthenticationResult result = null;

            string authorizationCode = await context.AcquireAccessCodeAsync(TestConstants.DefaultResource, sts.ValidConfidentialClientId, sts.ValidRedirectUriForConfidentialClient, sts.ValidUserId);
            RecorderJwtId.JwtIdIndex = 9;
            ClientAssertion assertion = CreateClientAssertion(TestConstants.DefaultAuthorityCommonTenant, sts.ValidConfidentialClientId, sts.ConfidentialClientCertificateName, sts.ConfidentialClientCertificatePassword);
            result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, sts.ValidRedirectUriForConfidentialClient, assertion, TestConstants.DefaultResource);
            VerifySuccessResult(sts, result);

            result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, sts.ValidRedirectUriForConfidentialClient, assertion, null);
            VerifySuccessResult(sts, result);

            result = await context.AcquireTokenByAuthorizationCodeAsync(string.Empty, sts.ValidRedirectUriForConfidentialClient, assertion, TestConstants.DefaultResource);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "authorizationCode");

            result = await context.AcquireTokenByAuthorizationCodeAsync(null, sts.ValidRedirectUriForConfidentialClient, assertion, TestConstants.DefaultResource);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "authorizationCode");

            // Send null for redirect
            result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, null, assertion, TestConstants.DefaultResource);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "redirectUri");

            result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, sts.ValidRedirectUriForConfidentialClient, (ClientAssertion)null, TestConstants.DefaultResource);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "clientAssertion");
        }

        public static async Task ClientAssertionWithSelfSignedJwtTestAsync(Sts sts)
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, sts.ValidateAuthority, TokenCacheType.Null);

            AuthenticationResult result = null;
            RecorderJwtId.JwtIdIndex = 10;
            ClientAssertion validCredential = CreateClientAssertion(TestConstants.DefaultAuthorityCommonTenant, sts.ValidConfidentialClientId, sts.ConfidentialClientCertificateName, sts.ConfidentialClientCertificatePassword);
            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, validCredential);
            Verify.IsNotNullOrEmptyString(result.AccessToken);

            result = await context.AcquireTokenAsync(null, validCredential);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "resource");

            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, (ClientAssertion)null);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "clientAssertion");

            RecorderJwtId.JwtIdIndex = 11;
            ClientAssertion invalidCredential = CreateClientAssertion(TestConstants.DefaultAuthorityCommonTenant, sts.ValidConfidentialClientId, sts.InvalidConfidentialClientCertificateName, sts.InvalidConfidentialClientCertificatePassword);
            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, invalidCredential);
            VerifyErrorResult(result, Sts.InvalidClientError, "50012", 401); // AADSTS50012: Client assertion contains an invalid signature.

            result = await context.AcquireTokenAsync(sts.InvalidResource, validCredential);
            VerifyErrorResult(result, Sts.InvalidResourceError, "50001", 400);   // ACS50001: Resource not found.

            RecorderJwtId.JwtIdIndex = 12;
            invalidCredential = CreateClientAssertion(TestConstants.DefaultAuthorityCommonTenant, sts.InvalidClientId, sts.InvalidConfidentialClientCertificateName, sts.InvalidConfidentialClientCertificatePassword);
            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, invalidCredential);
            VerifyErrorResult(result, Sts.UnauthorizedClient, "70001", 400); // AADSTS70001: Application '87002806-c87a-41cd-896b-84ca5690d29e' is not registered for the account.
        }

        public static async Task AcquireTokenFromCacheTestAsync(Sts sts)
        {
            AuthenticationContext context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, sts.ValidateAuthority);

            try
            {
                await context.AcquireTokenSilentAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, sts.ValidUserId);
                Verify.Fail("AdalSilentTokenAcquisitionException was expected");
            }
            catch (AdalSilentTokenAcquisitionException ex)
            {
                Verify.AreEqual(AdalError.FailedToAcquireTokenSilently, ex.ErrorCode);
            }
            catch
            {
                Verify.Fail("AdalSilentTokenAcquisitionException was expected");                
            }

            AuthenticationContext.SetCredentials(sts.Type == StsType.ADFS ? sts.ValidUserName : null, sts.ValidPassword);
            var contextProxy = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, sts.ValidateAuthority);
            AuthenticationResult resultProxy = await contextProxy.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, platformParameters, sts.ValidUserId);
            VerifySuccessResult(sts, resultProxy);

            AuthenticationResult result = await context.AcquireTokenSilentAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, (sts.Type == StsType.ADFS) ? UserIdentifier.AnyUser : sts.ValidUserId);
            VerifySuccessResult(result);

            result = await context.AcquireTokenSilentAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId);
            VerifySuccessResult(result);
        }

        internal static async Task CacheExpirationMarginTestAsync(Sts sts)
        {
            SetCredential(sts);
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, sts.ValidateAuthority);
            AuthenticationResult result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, platformParameters, sts.ValidUserId);
            VerifySuccessResult(sts, result);

            AuthenticationContext.Delay(2000);   // 2 seconds delay

            AuthenticationContext.SetCredentials(null, null);

            var userId = (result.UserInfo != null) ? new UserIdentifier(result.UserInfo.DisplayableId, UserIdentifierType.OptionalDisplayableId) : UserIdentifier.AnyUser;

            AuthenticationResult result2 = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, platformParameters, userId, SecondCallExtraQueryParameter);
            VerifySuccessResult(sts, result2);
            VerifyExpiresOnAreEqual(result, result2);

            var dummyContext = new AuthenticationContext("https://dummy/dummy", false);
            AdalFriend.UpdateTokenExpiryOnTokenCache(dummyContext.TokenCache, DateTime.UtcNow + TimeSpan.FromSeconds(4 * 60 + 50));

            result2 = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, platformParameters, userId);
            VerifySuccessResult(sts, result2);
            Verify.AreNotEqual(result.AccessToken, result2.AccessToken);
        }
        
        internal static async Task AcquireTokenOnBehalfAndClientCredentialTestAsync(Sts sts)
        {
            SetCredential(sts);

            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, sts.ValidateAuthority);
            AuthenticationResult result = await context.AcquireTokenAsync(sts.ValidConfidentialClientId, TestConstants.DefaultClientId, TestConstants.DefaultResource, platformParameters, sts.ValidUserId);
            VerifySuccessResult(sts, result);

            ClientCredential clientCredential = new ClientCredential(sts.ValidConfidentialClientId, sts.ValidConfidentialClientSecret);

            AuthenticationResult result2 = await context.AcquireTokenAsync(null, clientCredential, result.AccessToken);
            VerifyErrorResult(result2, Sts.InvalidArgumentError, "resource");

            result2 = await context.AcquireTokenAsync(TestConstants.DefaultResource, clientCredential, null);
            VerifyErrorResult(result2, Sts.InvalidArgumentError, "userAssertion");

            result2 = await context.AcquireTokenAsync(TestConstants.DefaultResource, (ClientCredential)null, result.AccessToken);
            VerifyErrorResult(result2, Sts.InvalidArgumentError, "clientCredential");

            result2 = await context.AcquireTokenAsync(TestConstants.DefaultResource + "x", clientCredential, result.AccessToken);
            VerifyErrorResult(result2, Sts.InvalidResourceError, null);

            result2 = await context.AcquireTokenAsync(TestConstants.DefaultResource, clientCredential, result.AccessToken);
            VerifySuccessResult(sts, result2, true, false);

            // Testing cache
            AuthenticationContext.Delay(2000);   // 2 seconds delay
            AuthenticationResult result3 = await context.AcquireTokenAsync(TestConstants.DefaultResource, clientCredential, result.AccessToken);
            VerifySuccessResult(sts, result3, true, false);
            VerifyExpiresOnAreEqual(result2, result3);

            // Using MRRT in cached token to acquire token for a different resource
            AuthenticationResult result4 = await context.AcquireTokenAsync(TestConstants.DefaultResource2, clientCredential, result.AccessToken + "x");
            VerifySuccessResult(sts, result4, true, false);

            AuthenticationContext.ClearDefaultCache();

            result2 = await context.AcquireTokenAsync(TestConstants.DefaultResource, clientCredential, result.AccessToken + "x");
            VerifyErrorResult(result2, "invalid_grant", "invalid signature");

            ClientCredential invalidClientCredential = new ClientCredential(sts.ValidConfidentialClientId, sts.ValidConfidentialClientSecret + "x");
            result2 = await context.AcquireTokenAsync(TestConstants.DefaultResource, invalidClientCredential, result.AccessToken);
            VerifyErrorResult(result2, Sts.InvalidClientError, "Invalid client secret");

            result2 = await context.AcquireTokenAsync(TestConstants.DefaultResource, clientCredential, result.AccessToken);
            VerifySuccessResult(sts, result2, true, false);

            // Using MRRT in cached token to acquire token for a different resource
            result3 = await context.AcquireTokenSilentAsync(TestConstants.DefaultResource2, sts.ValidConfidentialClientId);
            VerifyErrorResult(result3, AdalError.FailedToAcquireTokenSilently, null);

            // Using MRRT in cached token to acquire token for a different resource
            result3 = await context.AcquireTokenSilentAsync(TestConstants.DefaultResource2, clientCredential, UserIdentifier.AnyUser);
            VerifySuccessResult(sts, result3, true, false);
        }

        internal static async Task AcquireTokenOnBehalfAndClientCertificateTestAsync(Sts sts)
        {
            SetCredential(sts);

            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, sts.ValidateAuthority);
            AuthenticationResult result = await context.AcquireTokenAsync(sts.ValidConfidentialClientId, TestConstants.DefaultClientId, TestConstants.DefaultResource, platformParameters, sts.ValidUserId);
            VerifySuccessResult(sts, result);

            var clientCertificate = new ClientAssertionCertificate(sts.ValidConfidentialClientId, CreateX509Certificate(sts.ConfidentialClientCertificateName, sts.ConfidentialClientCertificatePassword));
            RecorderJwtId.JwtIdIndex = 5;
            AuthenticationResult result2 = await context.AcquireTokenAsync(null, clientCertificate, result.AccessToken);
            VerifyErrorResult(result2, Sts.InvalidArgumentError, "resource");

            result2 = await context.AcquireTokenAsync(TestConstants.DefaultResource, clientCertificate, null);
            VerifyErrorResult(result2, Sts.InvalidArgumentError, "userAssertion");

            result2 = await context.AcquireTokenAsync(TestConstants.DefaultResource, (ClientAssertionCertificate)null, result.AccessToken);
            RecorderJwtId.JwtIdIndex = 6;
            VerifyErrorResult(result2, Sts.InvalidArgumentError, "clientCertificate");

            result2 = await context.AcquireTokenAsync(TestConstants.DefaultResource, clientCertificate, result.AccessToken);
            VerifySuccessResult(sts, result2, true, false);

            // Testing cache
            AuthenticationContext.Delay(2000);   // 2 seconds delay
            AuthenticationResult result3 = await context.AcquireTokenAsync(TestConstants.DefaultResource, clientCertificate, result.AccessToken);
            VerifySuccessResult(sts, result3, true, false);
            VerifyExpiresOnAreEqual(result2, result3);

            // Using MRRT in cached token to acquire token for a different resource
            AuthenticationResult result4 = await context.AcquireTokenAsync(TestConstants.DefaultResource2, clientCertificate, result.AccessToken + "x");
            VerifySuccessResult(sts, result4, true, false);

            AuthenticationContext.ClearDefaultCache();

            result2 = await context.AcquireTokenAsync(TestConstants.DefaultResource + "x", clientCertificate, result.AccessToken);
            VerifyErrorResult(result2, Sts.InvalidResourceError, null);

            result2 = await context.AcquireTokenAsync(TestConstants.DefaultResource, clientCertificate, result.AccessToken + "x");
            VerifyErrorResult(result2, "invalid_grant", "invalid signature");

            var invalidClientCredential = new ClientAssertionCertificate(sts.ValidConfidentialClientId.Replace('1', '2'), CreateX509Certificate(sts.ConfidentialClientCertificateName, sts.ConfidentialClientCertificatePassword));
            RecorderJwtId.JwtIdIndex = 7;
            result2 = await context.AcquireTokenAsync(TestConstants.DefaultResource, invalidClientCredential, result.AccessToken);
            VerifyErrorResult(result2, Sts.UnauthorizedClient, "not found");

            result2 = await context.AcquireTokenAsync(TestConstants.DefaultResource, clientCertificate, result.AccessToken);
            VerifySuccessResult(sts, result2, true, false);

            // Using MRRT in cached token to acquire token for a different resource
            result3 = await context.AcquireTokenSilentAsync(TestConstants.DefaultResource2, sts.ValidConfidentialClientId);
            VerifyErrorResult(result3, AdalError.FailedToAcquireTokenSilently, null);

            // Using MRRT in cached token to acquire token for a different resource
            result3 = await context.AcquireTokenSilentAsync(TestConstants.DefaultResource2, clientCertificate, UserIdentifier.AnyUser);
            VerifySuccessResult(sts, result3, true, false);
        }

        internal static async Task AcquireTokenOnBehalfAndClientAssertionTestAsync(Sts sts)
        {
            SetCredential(sts);

            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, sts.ValidateAuthority);
            AuthenticationResult result = await context.AcquireTokenAsync(sts.ValidConfidentialClientId, TestConstants.DefaultClientId, TestConstants.DefaultResource, platformParameters, sts.ValidUserId);
            VerifySuccessResult(sts, result);

            RecorderJwtId.JwtIdIndex = 13;
            ClientAssertion clientAssertion = CreateClientAssertion(TestConstants.DefaultAuthorityCommonTenant, sts.ValidConfidentialClientId, sts.ConfidentialClientCertificateName, sts.ConfidentialClientCertificatePassword);

            AuthenticationResult result2 = await context.AcquireTokenAsync(null, clientAssertion, result.AccessToken);
            VerifyErrorResult(result2, Sts.InvalidArgumentError, "resource");

            result2 = await context.AcquireTokenAsync(TestConstants.DefaultResource, clientAssertion, null);
            VerifyErrorResult(result2, Sts.InvalidArgumentError, "userAssertion");

            result2 = await context.AcquireTokenAsync(TestConstants.DefaultResource, (ClientAssertion)null, result.AccessToken);
            VerifyErrorResult(result2, Sts.InvalidArgumentError, "clientAssertion");

            result2 = await context.AcquireTokenAsync(TestConstants.DefaultResource, clientAssertion, result.AccessToken);
            VerifySuccessResult(sts, result2, true, false);

            // Testing cache
            AuthenticationContext.Delay(2000);   // 2 seconds delay
            AuthenticationResult result3 = await context.AcquireTokenAsync(TestConstants.DefaultResource, clientAssertion, result.AccessToken);
            VerifySuccessResult(sts, result3, true, false);
            VerifyExpiresOnAreEqual(result2, result3);

            // Using MRRT in cached token to acquire token for a different resource
            AuthenticationResult result4 = await context.AcquireTokenAsync(TestConstants.DefaultResource2, clientAssertion, result.AccessToken);
            VerifySuccessResult(sts, result4, true, false);

            AuthenticationContext.ClearDefaultCache();

            result2 = await context.AcquireTokenAsync(TestConstants.DefaultResource, clientAssertion, result.AccessToken);
            VerifySuccessResult(sts, result2, true, false);

            // Using MRRT in cached token to acquire token for a different resource
            result3 = await context.AcquireTokenSilentAsync(TestConstants.DefaultResource2, clientAssertion, UserIdentifier.AnyUser);
            VerifySuccessResult(sts, result3, true, false);
        }

        internal static async Task MultiThreadedClientAssertionWithX509TestAsync(Sts sts)
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, sts.ValidateAuthority);

            const int ParallelCount = 20;
            AuthenticationResult[] result = new AuthenticationResult[ParallelCount];

            var certificate = new ClientAssertionCertificate(sts.ValidConfidentialClientId, CreateX509Certificate(sts.ConfidentialClientCertificateName, sts.ConfidentialClientCertificatePassword));
            RecorderJwtId.JwtIdIndex = 8;

            Parallel.For(0, ParallelCount, async (i) =>
            {
                result[i] = await context.AcquireTokenAsync(TestConstants.DefaultResource, certificate);
                Log.Comment("Error: " + result[i].Error);
                Log.Comment("Error Description: " + result[i].ErrorDescription);
                Verify.IsNotNullOrEmptyString(result[i].AccessToken);
            });

            result[0] = await context.AcquireTokenAsync(TestConstants.DefaultResource, certificate);
            Log.Comment("Error: " + result[0].Error);
            Log.Comment("Error Description: " + result[0].ErrorDescription);
            Verify.IsNotNullOrEmptyString(result[0].AccessToken);
        }


        internal static async Task AcquireTokenWithPromptBehaviorNeverTestAsync(Sts sts)
        {
            // Should not be able to get a token silently on first try.
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, sts.ValidateAuthority);
            var neverAuthorizationParameters = new platformParameters(PromptBehavior.Never, null);
            AuthenticationResult result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, neverAuthorizationParameters);
            VerifyErrorResult(result, Sts.UserInteractionRequired, null);

            AuthenticationContext.SetCredentials(sts.ValidUserName, sts.ValidPassword);
            // Obtain a token interactively.
            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, platformParameters, sts.ValidUserId);
            VerifySuccessResult(sts, result);

            AuthenticationContext.SetCredentials(null, null);
            // Now there should be a token available in the cache so token should be available silently.
            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, neverAuthorizationParameters);
            VerifySuccessResult(sts, result);

            // Clear the cache and silent auth should work via session cookies.
            AuthenticationContext.ClearDefaultCache();
            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, neverAuthorizationParameters);
            VerifySuccessResult(sts, result);

            // Clear the cache and cookies and silent auth should fail.
            AuthenticationContext.ClearDefaultCache();
            EndBrowserDialogSession();
            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, neverAuthorizationParameters);
            VerifyErrorResult(result, Sts.UserInteractionRequired, null);
        }

        internal static async Task SwitchUserTestAsync(Sts sts)
        {
            Log.Comment("Acquire token for user1 interactively");
            AuthenticationContext.SetCredentials(null, sts.ValidPassword);
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, sts.ValidateAuthority);
            AuthenticationResult result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, platformParameters, sts.ValidUserId);
            VerifySuccessResultAndTokenContent(sts, result);
            Verify.AreEqual(sts.ValidUserName, result.UserInfo.DisplayableId);

            Log.Comment("Acquire token via cookie for user1 without user");
            AuthenticationResult result2 = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, platformParameters);
            VerifySuccessResultAndTokenContent(sts, result2);
            Verify.AreEqual(sts.ValidUserName, result2.UserInfo.DisplayableId);

            Log.Comment("Acquire token for user2 via force prompt and user");
            AuthenticationContext.SetCredentials(sts.ValidUserName2, sts.ValidPassword2);
            var alwaysAuthorizationParameters = new platformParameters(PromptBehavior.Always, null);
            result2 = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, alwaysAuthorizationParameters, sts.ValidRequiredUserId2);
            VerifySuccessResultAndTokenContent(sts, result2);
            Verify.AreEqual(sts.ValidUserName2, result2.UserInfo.DisplayableId);

            Log.Comment("Acquire token for user2 via force prompt");
            AuthenticationContext.SetCredentials(sts.ValidUserName2, sts.ValidPassword2);
            result2 = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, alwaysAuthorizationParameters);
            VerifySuccessResultAndTokenContent(sts, result2);
            Verify.AreEqual(sts.ValidUserName2, result2.UserInfo.DisplayableId);

            Log.Comment("Fail to acquire token without user while tokens for two users in the cache");
            result2 = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, platformParameters);
            VerifyErrorResult(result2, "multiple_matching_tokens_detected", null);
        }
        
        internal static async Task TokenSubjectTypeTestAsync(Sts sts)
        {
            SetCredential(sts);
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, sts.ValidateAuthority);

            string authorizationCode = await context.AcquireAccessCodeAsync(TestConstants.DefaultResource, sts.ValidConfidentialClientId, sts.ValidRedirectUriForConfidentialClient, sts.ValidUserId);

            var credential = new ClientCredential(sts.ValidConfidentialClientId, sts.ValidConfidentialClientSecret);

            AuthenticationResult result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, sts.ValidRedirectUriForConfidentialClient, credential);
            VerifySuccessResult(sts, result);

            AuthenticationResult result2 = await context.AcquireTokenSilentAsync(TestConstants.DefaultResource, credential, sts.ValidUserId);
            VerifySuccessResult(sts, result2);
            VerifyExpiresOnAreEqual(result, result2);

            AuthenticationResult result3 = await context.AcquireTokenAsync(TestConstants.DefaultResource, credential);
            VerifySuccessResult(sts, result3, false, false);

            AuthenticationResult result4 = await context.AcquireTokenAsync(TestConstants.DefaultResource, credential);
            VerifySuccessResult(sts, result4, false, false);
            VerifyExpiresOnAreEqual(result3, result4);
            VerifyExpiresOnAreNotEqual(result, result3);

            var cacheItems = TokenCache.DefaultShared.ReadItems().ToList();
            Verify.AreEqual(cacheItems.Count, 2);
        }

        internal static async Task GetAuthorizationRequestURLTestAsync(Sts sts)
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, sts.ValidateAuthority);
            Uri uri = null;

            try
            {
                uri = await context.GetAuthorizationRequestUrlAsync(null, TestConstants.DefaultClientId, TestConstants.DefaultResource, sts.ValidUserId, "extra=123");
            }
            catch (ArgumentNullException ex)
            {
                Verify.AreEqual(ex.ParamName, "resource");
            }
            
            uri = await context.GetAuthorizationRequestUrlAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, sts.ValidUserId, "extra=123");
            Verify.IsNotNull(uri);
            Verify.IsTrue(uri.AbsoluteUri.Contains("login_hint"));
            Verify.IsTrue(uri.AbsoluteUri.Contains("extra=123"));
            uri = await context.GetAuthorizationRequestUrlAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, UserIdentifier.AnyUser, null);
            Verify.IsNotNull(uri);
            Verify.IsFalse(uri.AbsoluteUri.Contains("login_hint"));
            Verify.IsFalse(uri.AbsoluteUri.Contains("client-request-id="));
            context.CorrelationId = Guid.NewGuid();
            uri = await context.GetAuthorizationRequestUrlAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, sts.ValidUserId, "extra");
            Verify.IsNotNull(uri);
            Verify.IsTrue(uri.AbsoluteUri.Contains("client-request-id="));
        }

        internal static async Task LoggerTestAsync(Sts sts)
        {
            var eventListener = new SampleEventListener();
            eventListener.EnableEvents(AdalOption.AdalEventSource, EventLevel.Verbose);

            Trace.TraceInformation("$$$$$");

            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, sts.ValidateAuthority, TokenCacheType.Null);

            var credential = new ClientCredential(sts.ValidConfidentialClientId, sts.ValidConfidentialClientSecret);
            await context.AcquireTokenAsync(TestConstants.DefaultResource, credential);
            var invalidCredential = new ClientCredential(sts.ValidConfidentialClientId, sts.ValidConfidentialClientSecret + "x");
            await context.AcquireTokenAsync(TestConstants.DefaultResource, invalidCredential);

            Verify.IsTrue(eventListener.TraceBuffer.IndexOf("$$") < 0);

            eventListener.TraceBuffer = string.Empty;

            credential = new ClientCredential(sts.ValidConfidentialClientId, sts.ValidConfidentialClientSecret);
            await context.AcquireTokenAsync(TestConstants.DefaultResource, credential);
            invalidCredential = new ClientCredential(sts.ValidConfidentialClientId, sts.ValidConfidentialClientSecret + "x");
            await context.AcquireTokenAsync(TestConstants.DefaultResource, invalidCredential);

            Verify.IsFalse(string.IsNullOrEmpty(eventListener.TraceBuffer));

            eventListener.TraceBuffer = string.Empty;
            eventListener.DisableEvents(AdalOption.AdalEventSource);

            credential = new ClientCredential(sts.ValidConfidentialClientId, sts.ValidConfidentialClientSecret);
            await context.AcquireTokenAsync(TestConstants.DefaultResource, credential);
            invalidCredential = new ClientCredential(sts.ValidConfidentialClientId, sts.ValidConfidentialClientSecret + "x");
            await context.AcquireTokenAsync(TestConstants.DefaultResource, invalidCredential);

            Verify.IsTrue(string.IsNullOrEmpty(eventListener.TraceBuffer));
        }

        internal static async Task MsaTestAsync()
        {
            AadSts sts = new AadSts();

            string liveIdtoken = StsLoginFlow.TryGetSamlToken("https://login.live.com", sts.MsaUserName, sts.MsaPassword, "urn:federation:MicrosoftOnline");
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, sts.ValidateAuthority);

            try
            {
                var result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, new UserAssertion(liveIdtoken, "urn:ietf:params:oauth:grant-type:saml1_1-bearer"));
                VerifySuccessResult(result);


                var result2 = await context.AcquireTokenSilentAsync(TestConstants.DefaultResource2, TestConstants.DefaultClientId, new UserIdentifier(sts.MsaUserName, UserIdentifierType.OptionalDisplayableId));
                VerifySuccessResult(result2);

                AuthenticationContext.Delay(2000);   // 2 seconds delay

                var result3 = await context.AcquireTokenSilentAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, new UserIdentifier(sts.MsaUserName, UserIdentifierType.OptionalDisplayableId));
                VerifySuccessResult(result3);
                Verify.IsTrue(AreDateTimeOffsetsEqual(result.ExpiresOn, result3.ExpiresOn));
            }
            catch (Exception ex)
            {
                Verify.Fail("Unexpected exception: " + ex);
            }

            try
            {
                context.TokenCache.Clear();
                var result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, new UserAssertion("x", "urn:ietf:params:oauth:grant-type:saml1_1-bearer"));
                Verify.Fail("Exception expected");
                VerifySuccessResult(result);
            }
            catch (AdalServiceException ex)
            {
                Verify.AreEqual(ex.ErrorCode, "invalid_grant");
                Verify.AreEqual(ex.StatusCode, 400);
                Verify.IsTrue(ex.ServiceErrorCodes.Contains("50008"));
            }
        }

        private static void VerifySuccessResult(AuthenticationResult result)
        {
            Log.Comment("Verifying success result...");

            Verify.IsNotNull(result);
            Verify.IsNotNullOrEmptyString(result.AccessToken, "AuthenticationResult.AccessToken");
            long expiresIn = (long)(result.ExpiresOn - DateTime.UtcNow).TotalSeconds;
            Log.Comment("Verifying token expiration...");
            Verify.IsGreaterThanOrEqual(expiresIn, (long)0, "Token Expiration");
        }

        public static ClientAssertion CreateClientAssertion(string authority, string clientId, string certificateName, string certificatePassword)
        {
            string audience = authority.Replace("login", "sts");

            // Test fails with out this
            if (!audience.EndsWith(@"/",StringComparison.OrdinalIgnoreCase))
            {
                audience += @"/";
            }

            ClientAssertion assertion = AdalFriend.CreateJwt(CreateX509Certificate(certificateName, certificatePassword), certificatePassword, clientId, audience);
            return new ClientAssertion(clientId, assertion.Assertion);
        }

        private static X509Certificate2 CreateX509Certificate(string filename, string password)
        {
            return new X509Certificate2(filename, password);
        }
    }
}
*/
