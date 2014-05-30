//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Test.ADAL.NET.Friend;

namespace Test.ADAL.Common
{
    internal partial class AdalTests
    {
        public static async Task ConfidentialClientTestAsync(Sts sts)
        {
            SetCredential(sts);
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);

            string authorizationCode = context.AcquireAccessCode(sts.ValidResource, sts.ValidConfidentialClientId, sts.ValidRedirectUriForConfidentialClient, sts.ValidUserId);

            var credential = new ClientCredential(sts.ValidConfidentialClientId, sts.ValidConfidentialClientSecret);

            AuthenticationResultProxy result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, sts.ValidRedirectUriForConfidentialClient, credential);
            if (sts.Type == StsType.ADFS)
            {
                VerifyErrorResult(result, Sts.InvalidAuthorityType, null);
                return;
            }

            VerifySuccessResult(sts, result);

            result = await context.AcquireTokenByRefreshTokenAsync(result.RefreshToken, sts.ValidConfidentialClientId, credential);
            VerifySuccessResult(sts, result, true, false);

            result = await context.AcquireTokenByRefreshTokenAsync(result.RefreshToken, sts.ValidConfidentialClientId, sts.ValidResource);
            VerifyErrorResult(result, "invalid_request", "90014", 400);    // ACS90014: The request body must contain the following parameter: 'client_secret or client_assertion'.

            result = await context.AcquireTokenByAuthorizationCodeAsync(null, sts.ValidRedirectUriForConfidentialClient, credential);
            VerifyErrorResult(result, "invalid_argument", "authorizationCode");

            result = await context.AcquireTokenByAuthorizationCodeAsync(string.Empty, sts.ValidRedirectUriForConfidentialClient, credential);
            VerifyErrorResult(result, "invalid_argument", "authorizationCode");

            result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode + "x", sts.ValidRedirectUriForConfidentialClient, credential);
            VerifyErrorResult(result, "invalid_grant", "authorization code");

            result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, new Uri(sts.ValidRedirectUriForConfidentialClient.AbsoluteUri + "x"), credential);

            // TODO: Update status code to 400 once AAD returns it.
            VerifyErrorResult(result, "invalid_grant", "access grant is invalid or malformed", (sts.Type == StsType.ADFS) ? 400 : 401);

            result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, sts.ValidRedirectUriForConfidentialClient, (ClientCredential)null);
            VerifyErrorResult(result, "invalid_argument", "credential");

            var invalidCredential = new ClientCredential(sts.ValidConfidentialClientId, sts.ValidConfidentialClientSecret + "x");
            result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, sts.ValidRedirectUriForConfidentialClient, invalidCredential);
            VerifyErrorResult(result, "invalid_client", "client secret", 401);
        }

        public static async Task ConfidentialClientWithX509TestAsync(Sts sts)
        {
            SetCredential(sts);
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority, TokenCacheStoreType.Null);

            string authorizationCode = context.AcquireAccessCode(sts.ValidResource, sts.ValidConfidentialClientId, sts.ValidRedirectUriForConfidentialClient, sts.ValidUserId);
            var credential = new X509CertificateCredentialProxy(sts.ValidConfidentialClientId, sts.ConfidentialClientCertificateName, sts.ConfidentialClientCertificatePassword);
            AuthenticationResultProxy result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, sts.ValidRedirectUriForConfidentialClient, credential, sts.ValidResource);
            if (sts.Type == StsType.ADFS)
            {
                VerifyErrorResult(result, Sts.InvalidAuthorityType, null);
                return;
            }

            VerifySuccessResult(sts, result);

            result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, sts.ValidRedirectUriForConfidentialClient, credential);
            VerifySuccessResult(sts, result);

            result = await context.AcquireTokenByRefreshTokenAsync(result.RefreshToken, sts.ValidConfidentialClientId, credential, sts.ValidResource);
            VerifySuccessResult(sts, result, true, false);

            result = await context.AcquireTokenByRefreshTokenAsync(result.RefreshToken, sts.ValidConfidentialClientId, sts.ValidResource);
            VerifyErrorResult(result, Sts.InvalidRequest, "90014", 400);   // The request body must contain the following parameter: 'client_secret or client_assertion'.

            result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, sts.ValidRedirectUriForConfidentialClient, credential, null);
            VerifySuccessResult(sts, result);

            result = await context.AcquireTokenByAuthorizationCodeAsync(null, sts.ValidRedirectUriForConfidentialClient, credential, sts.ValidResource);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "authorizationCode");

            result = await context.AcquireTokenByAuthorizationCodeAsync(string.Empty, sts.ValidRedirectUriForConfidentialClient, credential, sts.ValidResource);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "authorizationCode");

            // Send null for redirect
            result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, null, credential, sts.ValidResource);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "redirectUri");

            result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, sts.ValidRedirectUriForConfidentialClient, (X509CertificateCredentialProxy)null, sts.ValidResource);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "clientCertificate");
        }

        public static async Task ClientCredentialTestAsync(Sts sts)
        {
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority, TokenCacheStoreType.Null);

            AuthenticationResultProxy result = null;

            var credential = new ClientCredential(sts.ValidConfidentialClientId, sts.ValidConfidentialClientSecret);
            result = await context.AcquireTokenAsync(sts.ValidResource, credential);
            if (sts.Type == StsType.ADFS)
            {
                VerifyErrorResult(result, Sts.InvalidAuthorityType, null);
                return;
            }

            Verify.IsNotNull(result.AccessToken);

            result = await context.AcquireTokenAsync(null, credential);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "resource");

            result = await context.AcquireTokenAsync(sts.ValidResource, (ClientCredential)null);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "clientCredential");

            var invalidCredential = new ClientCredential(sts.ValidConfidentialClientId, sts.ValidConfidentialClientSecret + "x");
            result = await context.AcquireTokenAsync(sts.ValidResource, invalidCredential);
            VerifyErrorResult(result, Sts.InvalidClientError, "50012");

            invalidCredential = new ClientCredential(sts.ValidConfidentialClientId.Replace("0", "1"), sts.ValidConfidentialClientSecret + "x");
            result = await context.AcquireTokenAsync(sts.ValidResource, invalidCredential);
            VerifyErrorResult(result, Sts.UnauthorizedClient, "70001", 401);
        }

        public static async Task ClientAssertionWithX509TestAsync(Sts sts)
        {
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority, TokenCacheStoreType.Null);

            AuthenticationResultProxy result = null;

            var credential = new X509CertificateCredentialProxy(sts.ValidConfidentialClientId, sts.ConfidentialClientCertificateName, sts.ConfidentialClientCertificatePassword);
            result = await context.AcquireTokenAsync(sts.ValidResource, credential);
            if (sts.Type == StsType.ADFS)
            {
                VerifyErrorResult(result, Sts.InvalidAuthorityType, null);
                return;
            }

            Verify.IsNotNull(result.AccessToken);

            result = await context.AcquireTokenAsync(null, credential);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "resource");

            result = await context.AcquireTokenAsync(sts.ValidResource, (X509CertificateCredentialProxy)null);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "clientCertificate");

            var invalidCredential = new X509CertificateCredentialProxy(sts.ValidConfidentialClientId, sts.InvalidConfidentialClientCertificateName, sts.InvalidConfidentialClientCertificatePassword);
            result = await context.AcquireTokenAsync(sts.ValidResource, invalidCredential);
            VerifyErrorResult(result, Sts.InvalidClientError, "50012"); // AADSTS50012: Client assertion contains an invalid signature.

            invalidCredential = new X509CertificateCredentialProxy(sts.InvalidClientId, sts.ConfidentialClientCertificateName, sts.ConfidentialClientCertificatePassword);
            result = await context.AcquireTokenAsync(sts.ValidResource, invalidCredential);
            VerifyErrorResult(result, Sts.UnauthorizedClient, "70001"); // AADSTS70001: Application '87002806-c87a-41cd-896b-84ca5690d29e' is not registered for the account.
        }

        public static async Task ConfidentialClientWithJwtTestAsync(Sts sts)
        {
            SetCredential(sts);
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);

            AuthenticationResultProxy result = null;

            string authorizationCode = context.AcquireAccessCode(sts.ValidResource, sts.ValidConfidentialClientId, sts.ValidRedirectUriForConfidentialClient, sts.ValidUserId);
            ClientAssertion credential = CreateClientAssertion(sts.Authority, sts.ValidConfidentialClientId, sts.ConfidentialClientCertificateName, sts.ConfidentialClientCertificatePassword);

            result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, sts.ValidRedirectUriForConfidentialClient, credential, sts.ValidResource);
            if (sts.Type == StsType.ADFS)
            {
                VerifyErrorResult(result, Sts.InvalidAuthorityType, null);
                return;
            }

            VerifySuccessResult(sts, result);

            result = await context.AcquireTokenByRefreshTokenAsync(result.RefreshToken, sts.ValidConfidentialClientId, credential, sts.ValidResource);
            VerifySuccessResult(sts, result, true, false);

            result = await context.AcquireTokenByRefreshTokenAsync(result.RefreshToken, sts.ValidConfidentialClientId, sts.ValidResource);
            VerifyErrorResult(result, Sts.InvalidRequest, "90014");   // The request body must contain the following parameter: 'client_secret or client_assertion'.

            result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, sts.ValidRedirectUriForConfidentialClient, credential, null);
            VerifySuccessResult(sts, result);

            result = await context.AcquireTokenByAuthorizationCodeAsync(string.Empty, sts.ValidRedirectUriForConfidentialClient, credential, sts.ValidResource);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "authorizationCode");

            result = await context.AcquireTokenByAuthorizationCodeAsync(null, sts.ValidRedirectUriForConfidentialClient, credential, sts.ValidResource);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "authorizationCode");

            // Send null for redirect
            result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, null, credential, sts.ValidResource);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "redirectUri");

            result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, sts.ValidRedirectUriForConfidentialClient, (ClientAssertion)null, sts.ValidResource);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "clientAssertion");
        }

        public static async Task ClientAssertionWithSelfSignedJwtTestAsync(Sts sts)
        {
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority, TokenCacheStoreType.Null);

            AuthenticationResultProxy result = null;
            ClientAssertion validCredential = CreateClientAssertion(sts.Authority, sts.ValidConfidentialClientId, sts.ConfidentialClientCertificateName, sts.ConfidentialClientCertificatePassword);
            result = await context.AcquireTokenAsync(sts.ValidResource, validCredential);
            if (sts.Type == StsType.ADFS)
            {
                VerifyErrorResult(result, Sts.InvalidAuthorityType, null);
                return;
            }

            Verify.IsNotNull(result.AccessToken);

            result = await context.AcquireTokenAsync(null, validCredential);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "resource");

            result = await context.AcquireTokenAsync(sts.ValidResource, (ClientAssertion)null);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "clientAssertion");

            ClientAssertion invalidCredential = CreateClientAssertion(sts.Authority, sts.ValidConfidentialClientId, sts.InvalidConfidentialClientCertificateName, sts.InvalidConfidentialClientCertificatePassword);
            result = await context.AcquireTokenAsync(sts.ValidResource, invalidCredential);
            VerifyErrorResult(result, Sts.InvalidClientError, "50012", 401); // AADSTS50012: Client assertion contains an invalid signature.

            result = await context.AcquireTokenAsync(sts.InvalidResource, validCredential);
            VerifyErrorResult(result, Sts.InvalidResourceError, "50001", 400);   // ACS50001: Resource not found.

            invalidCredential = CreateClientAssertion(sts.Authority, sts.InvalidClientId, sts.InvalidConfidentialClientCertificateName, sts.InvalidConfidentialClientCertificatePassword);
            result = await context.AcquireTokenAsync(sts.ValidResource, invalidCredential);
            VerifyErrorResult(result, Sts.UnauthorizedClient, "70001", 401); // AADSTS70001: Application '87002806-c87a-41cd-896b-84ca5690d29e' is not registered for the account.
        }

        public static async Task AcquireTokenFromCacheTestAsync(Sts sts)
        {
            AuthenticationContext context = new AuthenticationContext(sts.Authority, sts.ValidateAuthority);

            try
            {
                await context.AcquireTokenSilentAsync(sts.ValidResource, sts.ValidClientId, sts.ValidUserId);
            }
            catch (AdalException ex)
            {
                Verify.AreEqual(AdalError.FailedToAcquireTokenSilently, ex.ErrorCode);
            }

            AuthenticationContextProxy.SetCredentials(sts.Type == StsType.ADFS ? sts.ValidUserName : null, sts.ValidPassword);
            var contextProxy = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);
            AuthenticationResultProxy resultProxy = contextProxy.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, sts.ValidUserId);
            VerifySuccessResult(sts, resultProxy);

            AuthenticationResult result = await context.AcquireTokenSilentAsync(sts.ValidResource, sts.ValidClientId, (sts.Type == StsType.ADFS) ? null : sts.ValidUserId);
            VerifySuccessResult(result);

            result = await context.AcquireTokenSilentAsync(sts.ValidResource, sts.ValidClientId);
            VerifySuccessResult(result);
        }

        internal static void CacheExpirationMarginTest(Sts sts)
        {
            SetCredential(sts);
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);
            AuthenticationResultProxy result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, sts.ValidUserId);
            VerifySuccessResult(sts, result);

            AuthenticationContextProxy.Delay(2000);   // 2 seconds delay

            AuthenticationContextProxy.SetCredentials(null, null);

            var userId = (result.UserInfo != null) ? new UserIdentifier(result.UserInfo.DisplayableId, UserIdentifierType.OptionalDisplayableId) : null;

            AuthenticationResultProxy result2 = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, userId, SecondCallExtraQueryParameter);
            VerifySuccessResult(sts, result2);
            VerifyExpiresOnAreEqual(result, result2);

            var dummyContext = new AuthenticationContext("https://dummy/dummy", false);
            var cache = dummyContext.TokenCacheStore;
            var key = cache.Keys.First();
            key.ExpiresOn = DateTime.UtcNow + TimeSpan.FromSeconds(4 * 60 + 50);  // less than 5 minues expiration margin
            var value = cache.Values.First();
            cache.Clear();
            cache.Add(key, value);

            result2 = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, userId);
            VerifySuccessResult(sts, result2);
            Verify.AreNotEqual(result.AccessToken, result2.AccessToken);
        }
        
        internal static async Task AcquireTokenOnBehalfAndClientCredentialTestAsync(Sts sts)
        {
            SetCredential(sts);

            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);
            AuthenticationResultProxy result = context.AcquireToken(sts.ValidConfidentialClientId, sts.ValidClientId, sts.ValidDefaultRedirectUri, sts.ValidUserId);
            VerifySuccessResult(sts, result);

            ClientCredential clientCredential = new ClientCredential(sts.ValidConfidentialClientId, sts.ValidConfidentialClientSecret);

            AuthenticationResultProxy result2 = await context.AcquireTokenAsync(null, result.AccessToken, clientCredential);
            VerifyErrorResult(result2, Sts.InvalidArgumentError, "resource");

            result2 = await context.AcquireTokenAsync(sts.ValidResource, null, clientCredential);
            VerifyErrorResult(result2, Sts.InvalidArgumentError, "userAssertion");

            result2 = await context.AcquireTokenAsync(sts.ValidResource, result.AccessToken, (ClientCredential)null);
            VerifyErrorResult(result2, Sts.InvalidArgumentError, "clientCredential");

            result2 = await context.AcquireTokenAsync(sts.ValidResource + "x", result.AccessToken, clientCredential);
            VerifyErrorResult(result2, Sts.InvalidResourceError, null);

            result2 = await context.AcquireTokenAsync(sts.ValidResource, result.AccessToken, clientCredential);
            VerifySuccessResult(sts, result2, false, false);

            // Testing cache
            AuthenticationContextProxy.Delay(2000);   // 2 seconds delay
            AuthenticationResultProxy result3 = await context.AcquireTokenAsync(sts.ValidResource, result.AccessToken, clientCredential);
            VerifySuccessResult(sts, result3, false, false);
            VerifyExpiresOnAreEqual(result2, result3);

            AuthenticationContextProxy.ClearDefaultCache();

            result2 = await context.AcquireTokenAsync(sts.ValidResource, result.AccessToken + "x", clientCredential);
            VerifyErrorResult(result2, "invalid_grant", "invalid signature");

            ClientCredential invalidClientCredential = new ClientCredential(sts.ValidConfidentialClientId, sts.ValidConfidentialClientSecret + "x");
            result2 = await context.AcquireTokenAsync(sts.ValidResource, result.AccessToken, invalidClientCredential);
            VerifyErrorResult(result2, Sts.InvalidClientError, "Invalid client secret");
        }

        internal static async Task AcquireTokenOnBehalfAndClientCertificateTestAsync(Sts sts)
        {
            SetCredential(sts);

            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);
            AuthenticationResultProxy result = context.AcquireToken(sts.ValidConfidentialClientId, sts.ValidClientId, sts.ValidDefaultRedirectUri, sts.ValidUserId);
            VerifySuccessResult(sts, result);

            X509CertificateCredentialProxy clientCertificate = new X509CertificateCredentialProxy(sts.ValidConfidentialClientId, sts.ConfidentialClientCertificateName, sts.ConfidentialClientCertificatePassword);
            AuthenticationResultProxy result2 = await context.AcquireTokenAsync(null, result.AccessToken, clientCertificate);
            VerifyErrorResult(result2, Sts.InvalidArgumentError, "resource");

            result2 = await context.AcquireTokenAsync(sts.ValidResource, null, clientCertificate);
            VerifyErrorResult(result2, Sts.InvalidArgumentError, "userAssertion");

            result2 = await context.AcquireTokenAsync(sts.ValidResource, result.AccessToken, (X509CertificateCredentialProxy)null);
            VerifyErrorResult(result2, Sts.InvalidArgumentError, "clientCertificate");

            result2 = await context.AcquireTokenAsync(sts.ValidResource, result.AccessToken, clientCertificate);
            VerifySuccessResult(sts, result2, false, false);

            // Testing cache
            AuthenticationContextProxy.Delay(2000);   // 2 seconds delay
            AuthenticationResultProxy result3 = await context.AcquireTokenAsync(sts.ValidResource, result.AccessToken, clientCertificate);
            VerifySuccessResult(sts, result3, false, false);
            VerifyExpiresOnAreEqual(result2, result3);

            AuthenticationContextProxy.ClearDefaultCache();

            result2 = await context.AcquireTokenAsync(sts.ValidResource + "x", result.AccessToken, clientCertificate);
            VerifyErrorResult(result2, Sts.InvalidResourceError, null);

            result2 = await context.AcquireTokenAsync(sts.ValidResource, result.AccessToken + "x", clientCertificate);
            VerifyErrorResult(result2, "invalid_grant", "invalid signature");

            X509CertificateCredentialProxy invalidClientCredential = new X509CertificateCredentialProxy(sts.ValidConfidentialClientId.Replace('1', '2'), sts.ConfidentialClientCertificateName, sts.ConfidentialClientCertificatePassword);
            result2 = await context.AcquireTokenAsync(sts.ValidResource, result.AccessToken, invalidClientCredential);
            VerifyErrorResult(result2, Sts.UnauthorizedClient, "not registered");
        }

        internal static async Task AcquireTokenOnBehalfAndClientAssertionTestAsync(Sts sts)
        {
            SetCredential(sts);

            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);
            AuthenticationResultProxy result = context.AcquireToken(sts.ValidConfidentialClientId, sts.ValidClientId, sts.ValidDefaultRedirectUri, sts.ValidUserId);
            VerifySuccessResult(sts, result);

            ClientAssertion clientAssertion = CreateClientAssertion(sts.Authority, sts.ValidConfidentialClientId, sts.ConfidentialClientCertificateName, sts.ConfidentialClientCertificatePassword);

            AuthenticationResultProxy result2 = await context.AcquireTokenAsync(null, result.AccessToken, clientAssertion);
            VerifyErrorResult(result2, Sts.InvalidArgumentError, "resource");

            result2 = await context.AcquireTokenAsync(sts.ValidResource, null, clientAssertion);
            VerifyErrorResult(result2, Sts.InvalidArgumentError, "userAssertion");

            result2 = await context.AcquireTokenAsync(sts.ValidResource, result.AccessToken, (ClientAssertion)null);
            VerifyErrorResult(result2, Sts.InvalidArgumentError, "clientAssertion");

            result2 = await context.AcquireTokenAsync(sts.ValidResource, result.AccessToken, clientAssertion);
            VerifySuccessResult(sts, result2, false, false);

            // Testing cache
            AuthenticationContextProxy.Delay(2000);   // 2 seconds delay
            AuthenticationResultProxy result3 = await context.AcquireTokenAsync(sts.ValidResource, result.AccessToken, clientAssertion);
            VerifySuccessResult(sts, result3, false, false);
            VerifyExpiresOnAreEqual(result2, result3);
        }

        private static void VerifySuccessResult(AuthenticationResult result)
        {
            Log.Comment("Verifying success result...");

            Verify.IsNotNull(result);
            Verify.IsNotNull(result.AccessToken, "AuthenticationResult.AccessToken");
            Verify.IsNotNull(result.RefreshToken, "AuthenticationResult.RefreshToken");
            long expiresIn = (long)(result.ExpiresOn - DateTime.UtcNow).TotalSeconds;
            Log.Comment("Verifying token expiration...");
            Verify.IsGreaterThanOrEqual(expiresIn, (long)0, "Token Expiration");
        }

        public static ClientAssertion CreateClientAssertion(string authority, string clientId, string certificateName, string certificatePassword)
        {
            authority = authority.Replace("login", "sts");

            // Test fails with out this
            if (!authority.EndsWith(@"/"))
            {
                authority += @"/";
            }

            ClientAssertion credential = AdalFriend.CreateJwt(new X509Certificate2(certificateName + ".pfx", certificatePassword), clientId, authority);
            return new ClientAssertion(credential.Assertion);
        }
    }
}