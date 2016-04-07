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
using Microsoft.Identity.Client.Handlers;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client
{
   public sealed class ConfidentialClientApplication : AbstractClientApplication
   {
       /// <summary>
       /// 
       /// </summary>
       public ClientCredential ClientCredential { get; private set; }

       public TokenCache AppTokenCache { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="redirectUri"></param>
        /// <param name="clientCredential"></param>
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
       public ConfidentialClientApplication(string authority, string clientId, string redirectUri, ClientCredential clientCredential, TokenCache userTokenCache) :base(authority, clientId, redirectUri, true)
        {
            this.ClientCredential = clientCredential;
            this.UserTokenCache = userTokenCache;
            this.AppTokenCache = TokenCache.DefaultSharedAppTokenCache;
        }

        public async Task<AuthenticationResult> AcquireTokenOnBehalfOfAsync(string[] scope, UserAssertion userAssertion)
        {
            Authenticator authenticator = new Authenticator(this.Authority, this.ValidateAuthority, this.CorrelationId);
            return
                await
                    this.AcquireTokenOnBehalfCommonAsync(authenticator, scope, userAssertion, null)
                        .ConfigureAwait(false);
        }
    

        public async Task<AuthenticationResult> AcquireTokenOnBehalfOfAsync(string[] scope, UserAssertion userAssertion, string authority, string policy)
        {
            Authenticator authenticator = new Authenticator(authority, this.ValidateAuthority, this.CorrelationId);
            return
                await
                    this.AcquireTokenOnBehalfCommonAsync(authenticator, scope, userAssertion, policy)
                        .ConfigureAwait(false);
        }


        public async Task<AuthenticationResult> AcquireTokenByAuthorizationCodeAsync(string[] scope, string authorizationCode)
        {
            return
                await
                    this.AcquireTokenByAuthorizationCodeCommonAsync(authorizationCode, scope, new Uri(this.RedirectUri), null).ConfigureAwait(false);
        }

        public async Task<AuthenticationResult> AcquireTokenByAuthorizationCodeAsync(string[] scope, string authorizationCode, string policy)
        {
            return
                await
                    this.AcquireTokenByAuthorizationCodeCommonAsync(authorizationCode, scope, new Uri(this.RedirectUri), policy).ConfigureAwait(false);
        }


       public async Task<AuthenticationResult> AcquireTokenForClient(string[] scope, string policy)
        {
           return
               await
                   this.AcquireTokenForClientCommonAsync(scope, policy).ConfigureAwait(false);
        }

        private async Task<AuthenticationResult> AcquireTokenForClientCommonAsync(string[] scope, string policy)
        {
            Authenticator authenticator = new Authenticator(this.Authority, this.ValidateAuthority, this.CorrelationId);
            HandlerData data = this.GetHandlerData(authenticator, scope, policy, this.AppTokenCache);
            data.RestrictToSingleUser = false;
            var handler = new AcquireTokenForClientHandler(data);
            return await handler.RunAsync();
        }

        private async Task<AuthenticationResult> AcquireTokenOnBehalfCommonAsync(Authenticator authenticator, string[] scope, UserAssertion userAssertion, string policy)
        {
            var handler = new AcquireTokenOnBehalfHandler(this.GetHandlerData(authenticator, scope, policy, this.UserTokenCache), userAssertion);
            return await handler.RunAsync();
        }

        private async Task<AuthenticationResult> AcquireTokenByAuthorizationCodeCommonAsync(string authorizationCode, string[] scope, Uri redirectUri, string policy)
        {
            Authenticator authenticator = new Authenticator(this.Authority, this.ValidateAuthority, this.CorrelationId);
            var handler = new AcquireTokenByAuthorizationCodeHandler(this.GetHandlerData(authenticator, scope, policy, this.UserTokenCache), authorizationCode, redirectUri);
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
            HandlerData data =
                this.GetHandlerData(authenticator, scope, null, this.UserTokenCache);
            data.ClientKey = new ClientKey(this.ClientId);
            var handler =
                new AcquireTokenInteractiveHandler(data, null,
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
            HandlerData data = this.GetHandlerData(authenticator, scope, policy, this.UserTokenCache);
            data.ClientKey = new ClientKey(this.ClientId);
            var handler =
                new AcquireTokenInteractiveHandler(data, additionalScope,
                    new Uri(redirectUri), null, loginHint, null, extraQueryParameters, null);
            return await handler.CreateAuthorizationUriAsync(this.CorrelationId).ConfigureAwait(false);
        }
        
        internal override HandlerData GetHandlerData(Authenticator authenticator, string[] scope, string policy,
            TokenCache cache)
        {
            HandlerData data = base.GetHandlerData(authenticator, scope, policy, cache);
            data.ClientKey = new ClientKey(this.ClientId, this.ClientCredential, authenticator);

            return data;
        }
    }
}
