//------------------------------------------------------------------------------
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
using System.Threading.Tasks;
using Microsoft.Identity.Client.Requests;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// ConfidentialClientApplication
    /// </summary>
    public sealed class ConfidentialClientApplication : AbstractClientApplication
   {
        /// <summary>
        /// ClientCredential
        /// </summary>
        public ClientCredential ClientCredential { get; private set; }

        /// <summary>
        /// AppTokenCache
        /// </summary>
        public TokenCache AppTokenCache { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="redirectUri"></param>
        /// <param name="clientCredential"></param>
        /// <param name="userTokenCache"></param>
        public ConfidentialClientApplication(string clientId, string redirectUri,
           ClientCredential clientCredential, TokenCache userTokenCache):this(DefaultAuthority, clientId, redirectUri, clientCredential, userTokenCache)
       {
       }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="authority"></param>
        /// <param name="clientId"></param>
        /// <param name="redirectUri"></param>
        /// <param name="clientCredential"></param>
        /// <param name="userTokenCache"></param>
        public ConfidentialClientApplication(string authority, string clientId, string redirectUri, ClientCredential clientCredential, TokenCache userTokenCache) :base(authority, clientId, redirectUri, true)
        {
            this.ClientCredential = clientCredential;
            this.UserTokenCache = userTokenCache;
            this.AppTokenCache = TokenCache.DefaultSharedAppTokenCache;
        }
        /// <summary>
        /// AcquireTokenOnBehalfOfAsync
        /// </summary> 
        public async Task<AuthenticationResult> AcquireTokenOnBehalfOfAsync(string[] scope, UserAssertion userAssertion)
        {
            Authenticator authenticator = new Authenticator(this.Authority, this.ValidateAuthority, this.CorrelationId);
            return
                await
                    this.AcquireTokenOnBehalfCommonAsync(authenticator, scope, userAssertion, null)
                        .ConfigureAwait(false);
        }

        /// <summary>
        /// AcquireTokenOnBehalfOfAsync
        /// </summary>
        public async Task<AuthenticationResult> AcquireTokenOnBehalfOfAsync(string[] scope, UserAssertion userAssertion, string authority, string policy)
        {
            Authenticator authenticator = new Authenticator(authority, this.ValidateAuthority, this.CorrelationId);
            return
                await
                    this.AcquireTokenOnBehalfCommonAsync(authenticator, scope, userAssertion, policy)
                        .ConfigureAwait(false);
        }

        /// <summary>
        ///AcquireTokenByAuthorizationCodeAsync
        /// </summary>
        public async Task<AuthenticationResult> AcquireTokenByAuthorizationCodeAsync(string[] scope, string authorizationCode)
        {
            return
                await
                    this.AcquireTokenByAuthorizationCodeCommonAsync(authorizationCode, scope, new Uri(this.RedirectUri), null).ConfigureAwait(false);
        }
        /// <summary>
        ///AcquireTokenByAuthorizationCodeAsync
        /// </summary>
        public async Task<AuthenticationResult> AcquireTokenByAuthorizationCodeAsync(string[] scope, string authorizationCode, string policy)
        {
            return
                await
                    this.AcquireTokenByAuthorizationCodeCommonAsync(authorizationCode, scope, new Uri(this.RedirectUri), policy).ConfigureAwait(false);
        }

        /// <summary>
        ///AcquireTokenForClient
        /// </summary>
        public async Task<AuthenticationResult> AcquireTokenForClient(string[] scope, string policy)
        {
           return
               await
                   this.AcquireTokenForClientCommonAsync(scope, policy).ConfigureAwait(false);
        }

        private async Task<AuthenticationResult> AcquireTokenForClientCommonAsync(string[] scope, string policy)
        {
            Authenticator authenticator = new Authenticator(this.Authority, this.ValidateAuthority, this.CorrelationId);
            AuthenticationRequestParameters parameters = this.GetHandlerData(authenticator, scope, policy, this.AppTokenCache);
            parameters.RestrictToSingleUser = false;
            var handler = new ClientCredentialRequest(parameters);
            return await handler.RunAsync();
        }

        private async Task<AuthenticationResult> AcquireTokenOnBehalfCommonAsync(Authenticator authenticator, string[] scope, UserAssertion userAssertion, string policy)
        {
            var handler = new OnBehalfOfRequest(this.GetHandlerData(authenticator, scope, policy, this.UserTokenCache), userAssertion);
            return await handler.RunAsync();
        }

        private async Task<AuthenticationResult> AcquireTokenByAuthorizationCodeCommonAsync(string authorizationCode, string[] scope, Uri redirectUri, string policy)
        {
            Authenticator authenticator = new Authenticator(this.Authority, this.ValidateAuthority, this.CorrelationId);
            var handler = new AuthorizationCodeRequest(this.GetHandlerData(authenticator, scope, policy, this.UserTokenCache), authorizationCode, redirectUri);
            return await handler.RunAsync();
        }

        /// <summary>
        /// Gets URL of the authorize endpoint including the query parameters.
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="loginHint"></param>
        /// <param name="extraQueryParameters"></param>
        /// <returns>URL of the authorize endpoint including the query parameters.</returns>
        public async Task<Uri> GetAuthorizationRequestUrlAsync(string[] scope, string loginHint, string extraQueryParameters)
        {
            Authenticator authenticator = new Authenticator(this.Authority, this.ValidateAuthority, this.CorrelationId);
            AuthenticationRequestParameters parameters =
                this.GetHandlerData(authenticator, scope, null, this.UserTokenCache);
            parameters.ClientKey = new ClientKey(this.ClientId);
            var handler =
                new InteractiveRequest(parameters, null,
                    new Uri(this.RedirectUri), null, loginHint, null, extraQueryParameters, null);
            return await handler.CreateAuthorizationUriAsync(this.CorrelationId).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets URL of the authorize endpoint including the query parameters.
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="redirectUri"></param>
        /// <param name="loginHint"></param>
        /// <param name="extraQueryParameters"></param>
        /// <param name="additionalScope"></param>
        /// <param name="authority"></param>
        /// <param name="policy"></param>
        /// <returns>URL of the authorize endpoint including the query parameters.</returns>
        public async Task<Uri> GetAuthorizationRequestUrlAsync(string[] scope, string redirectUri, string loginHint, string extraQueryParameters, string[] additionalScope, string authority, string policy)
        {
            Authenticator authenticator = new Authenticator(authority, this.ValidateAuthority, this.CorrelationId);
            AuthenticationRequestParameters parameters = this.GetHandlerData(authenticator, scope, policy, this.UserTokenCache);
            parameters.ClientKey = new ClientKey(this.ClientId);
            var handler =
                new InteractiveRequest(parameters, additionalScope,
                    new Uri(redirectUri), null, loginHint, null, extraQueryParameters, null);
            return await handler.CreateAuthorizationUriAsync(this.CorrelationId).ConfigureAwait(false);
        }
        
        internal override AuthenticationRequestParameters GetHandlerData(Authenticator authenticator, string[] scope, string policy,
            TokenCache cache)
        {
            AuthenticationRequestParameters parameters = base.GetHandlerData(authenticator, scope, policy, cache);
            parameters.ClientKey = new ClientKey(this.ClientId, this.ClientCredential, authenticator);

            return parameters;
        }
    }
}
