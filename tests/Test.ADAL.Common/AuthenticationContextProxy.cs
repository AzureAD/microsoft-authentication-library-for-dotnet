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
using System.Linq;
using System.Threading.Tasks;

using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Test.ADAL.Common
{    
    internal partial class AuthenticationContextProxy
    {
        private const string FixedCorrelationId = "2ddbba59-1a04-43fb-b363-7fb0ae785030";
        private readonly AuthenticationContext context;


        public async Task<AuthenticationResultProxy> AcquireTokenAsync(string resource, ClientCredential credential)
        {
            return await RunTaskAsync(this.context.AcquireTokenAsync(resource, credential));
        }

        public async Task<AuthenticationResultProxy> AcquireTokenAsync(string resource, IClientAssertionCertificate certificate)
        {
            return await RunTaskAsync(this.context.AcquireTokenAsync(resource, certificate));
        }

        public async Task<AuthenticationResultProxy> AcquireTokenAsync(string resource, ClientAssertion credential)
        {
            return await RunTaskAsync(this.context.AcquireTokenAsync(resource, credential));
        }

#if TEST_ADAL_NET
        public async Task<AuthenticationResultProxy> AcquireTokenAsync(string resource, string clientId, UserCredentialProxy credential)
        {
            return await RunTaskAsync(this.context.AcquireTokenAsync(resource, clientId,
                (credential.Password == null) ?
                new UserCredential(credential.UserId) :
                new UserPasswordCredential(credential.UserId, credential.Password)));
        }
#endif
        public async Task<AuthenticationResultProxy> AcquireTokenSilentAsync(string resource, string clientId)
        {
            return await RunTaskAsync(this.context.AcquireTokenSilentAsync(resource, clientId));
        }

        public async Task<AuthenticationResultProxy> AcquireTokenSilentAsync(string resource, string clientId, UserIdentifier userId)
        {
            return await RunTaskAsync(this.context.AcquireTokenSilentAsync(resource, clientId, userId));
        }

        public async Task<AuthenticationResultProxy> AcquireTokenSilentAsync(string resource, ClientCredential clientCredential, UserIdentifier userId)
        {
            return await RunTaskAsync(this.context.AcquireTokenSilentAsync(resource, clientCredential, userId));
        }

        public async Task<AuthenticationResultProxy> AcquireTokenSilentAsync(string resource, ClientAssertion clientAssertion, UserIdentifier userId)
        {
            return await RunTaskAsync(this.context.AcquireTokenSilentAsync(resource, clientAssertion, userId));
        }

        public async Task<AuthenticationResultProxy> AcquireTokenSilentAsync(string resource, IClientAssertionCertificate clientCertificate, UserIdentifier userId)
        {
            return await RunTaskAsync(this.context.AcquireTokenSilentAsync(resource, clientCertificate, userId));
        }

        public async Task<AuthenticationResultProxy> AcquireTokenByAuthorizationCodeAsync(string authorizationCode, Uri redirectUri, ClientCredential credential)
        {
            return await RunTaskAsync(this.context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, redirectUri, credential));
        }

        public async Task<AuthenticationResultProxy> AcquireTokenByAuthorizationCodeAsync(string authorizationCode, Uri redirectUri, IClientAssertionCertificate certificate)
        {
            return await RunTaskAsync(this.context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, redirectUri, certificate));
        }

        public async Task<AuthenticationResultProxy> AcquireTokenByAuthorizationCodeAsync(string authorizationCode, Uri redirectUri, IClientAssertionCertificate certificate, string resource)
        {
            return await RunTaskAsync(this.context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, redirectUri, certificate, resource));
        }

        public async Task<AuthenticationResultProxy> AcquireTokenByAuthorizationCodeAsync(string authorizationCode, Uri redirectUri, ClientAssertion credential, string resource)
        {
            return await RunTaskAsync(this.context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, redirectUri, credential, resource));
        }

        public async Task<AuthenticationResultProxy> AcquireTokenAsync(string resource, ClientCredential clientCredential, string userAssertion)
        {
            return await RunTaskAsync(this.context.AcquireTokenAsync(resource, clientCredential, (userAssertion == null) ? null : new UserAssertion(userAssertion)));
        }

        public async Task<AuthenticationResultProxy> AcquireTokenAsync(string resource, IClientAssertionCertificate clientCertificate, string userAssertion)
        {
            return await RunTaskAsync(this.context.AcquireTokenAsync(resource, clientCertificate, (userAssertion == null) ? null : new UserAssertion(userAssertion)));
        }

        public async Task<AuthenticationResultProxy> AcquireTokenAsync(string resource, ClientAssertion clientAssertion, string userAssertion)
        {
            return await RunTaskAsync(this.context.AcquireTokenAsync(resource, clientAssertion, (userAssertion == null) ? null : new UserAssertion(userAssertion)));
        }


        internal void VerifySingleItemInCache(AuthenticationResultProxy result, StsType stsType)
        {
            List<TokenCacheItem> items = this.context.TokenCache.ReadItems().ToList();
            Verify.AreEqual(1, items.Count);
            Verify.AreEqual(result.AccessToken, items[0].AccessToken);
            Verify.AreEqual(result.IdToken ?? string.Empty, items[0].IdToken ?? string.Empty);
            Verify.IsTrue(stsType == StsType.ADFS || items[0].IdToken != null);
        }

        private static AuthenticationResultProxy GetAuthenticationResultProxy(AuthenticationResult result)
        {
            return new AuthenticationResultProxy
            {
                Status = AuthenticationStatusProxy.Success,
                AccessToken = result.AccessToken,
                AccessTokenType = result.AccessTokenType,
                ExpiresOn = result.ExpiresOn,
                IdToken = result.IdToken,
                TenantId = result.TenantId,
                UserInfo = result.UserInfo
            };
        }

        private static AuthenticationResultProxy GetAuthenticationResultProxy(Exception ex)
        {
            var output = new AuthenticationResultProxy
            {
                ErrorDescription = ex.Message,
            };

            output.Status = AuthenticationStatusProxy.ClientError;
            if (ex is ArgumentNullException)
            {
                output.Error = AdalError.InvalidArgument;
            }
            else if (ex is ArgumentException)
            {
                output.Error = AdalError.InvalidArgument;
            }
            else if (ex is AdalServiceException)
            {
                output.Error = ((AdalServiceException)ex).ErrorCode;
                output.ExceptionStatusCode = ((AdalServiceException)ex).StatusCode;
                output.ExceptionServiceErrorCodes = ((AdalServiceException)ex).ServiceErrorCodes;
                output.Status = AuthenticationStatusProxy.ServiceError;
            }
            else if (ex is AdalException)
            {
                output.Error = ((AdalException)ex).ErrorCode;
            }
            else
            {
                output.Error = AdalError.AuthenticationFailed;
            }

            output.Exception = ex;

            return output;
        }
    }
}
