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
                new UserCredential(credential.UserId, credential.Password)));
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
