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
using Microsoft.Identity.Client.Internal.Interfaces;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Instance;
using Microsoft.Identity.Client.Internal.Requests;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Class to be used for native applications (Desktop/UWP/iOS/Android).
    /// </summary>
    public sealed partial class PublicClientApplication : ClientApplicationBase, IPublicClientApplication
    {
        private const string DEFAULT_REDIRECT_URI = "urn:ietf:wg:oauth:2.0:oob";

        /// <summary>
        /// .NET specific property that allows configuration of platform specific properties. For example, in iOS/Android it
        /// would include the flag to enable/disable broker.
        /// </summary>
        public IPlatformParameters PlatformParameters { get; set; }

        /// <summary>
        /// Default consutructor of the application. It will use https://login.microsoftonline.com/common as the default authority.
        /// </summary>
        /// <param name="clientId">Client id of the application</param>
        public PublicClientApplication(string clientId) : this(clientId, DefaultAuthority)
        {
        }

        /// <summary>
        /// Default consutructor of the application.
        /// </summary>
        /// <param name="clientId">Client id of the application</param>
        /// <param name="authority">Default authority to be used for the application</param>
        public PublicClientApplication(string clientId, string authority)
            : base(authority, clientId, DEFAULT_REDIRECT_URI, true)
        {
            UserTokenCache = new TokenCache();
        }

        /// <summary>
        /// Interactive request to acquire token. 
        /// </summary>
        /// <param name="scope">Array of scopes requested for resource</param>
        /// <returns>Authentication result containing token of the user</returns>
        public async Task<IAuthenticationResult> AcquireTokenAsync(string[] scope)
        {
            Authority authority = Internal.Instance.Authority.CreateAuthority(Authority, ValidateAuthority);
            return
                await
                    AcquireTokenCommonAsync(authority, scope, null, (string) null,
                        UIBehavior.SelectAccount, null).ConfigureAwait(false);
        }

        /// <summary>
        /// Interactive request to acquire token. 
        /// </summary>
        /// <param name="scope">Array of scopes requested for resource</param>
        /// <param name="loginHint">Identifier of the user. Generally a UPN.</param>
        /// <returns>Authentication result containing token of the user</returns>
        public async Task<IAuthenticationResult> AcquireTokenAsync(string[] scope, string loginHint)
        {
            Authority authority = Internal.Instance.Authority.CreateAuthority(Authority, ValidateAuthority);
            return
                await
                    AcquireTokenCommonAsync(authority, scope, null, loginHint,
                        UIBehavior.SelectAccount, null).ConfigureAwait(false);
        }

        /// <summary>
        /// Interactive request to acquire token. 
        /// </summary>
        /// <param name="scope">Array of scopes requested for resource</param>
        /// <param name="loginHint">Identifier of the user. Generally a UPN.</param>
        /// <param name="behavior">Enumeration to control UI behavior.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. The parameter can be null.</param>
        /// <returns>Authentication result containing token of the user</returns>
        public async Task<IAuthenticationResult> AcquireTokenAsync(string[] scope, string loginHint,
            UIBehavior behavior, string extraQueryParameters)
        {
            Authority authority = Internal.Instance.Authority.CreateAuthority(Authority, ValidateAuthority);
            return
                await
                    AcquireTokenCommonAsync(authority, scope, null, loginHint,
                        behavior, extraQueryParameters).ConfigureAwait(false);
        }

        /// <summary>
        /// Interactive request to acquire token. 
        /// </summary>
        /// <param name="scope">Array of scopes requested for resource</param>
        /// <param name="user">User object to enforce the same user to be authenticated in the web UI.</param>
        /// <param name="behavior">Enumeration to control UI behavior.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. The parameter can be null.</param>
        /// <returns>Authentication result containing token of the user</returns>
        public async Task<IAuthenticationResult> AcquireTokenAsync(string[] scope, User user,
            UIBehavior behavior, string extraQueryParameters)
        {
            Authority authority = Internal.Instance.Authority.CreateAuthority(Authority, ValidateAuthority);
            return
                await
                    AcquireTokenCommonAsync(authority, scope, null, user, behavior,
                        extraQueryParameters).ConfigureAwait(false);
        }

        /// <summary>
        /// Interactive request to acquire token. 
        /// </summary>
        /// <param name="scope">Array of scopes requested for resource</param>
        /// <param name="loginHint">Identifier of the user. Generally a UPN.</param>
        /// <param name="behavior">Enumeration to control UI behavior.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. The parameter can be null.</param>
        /// <param name="additionalScope">Array of scopes for which a developer can request consent upfront.</param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured does not change the configured value</param>
        /// <returns>Authentication result containing token of the user</returns>
        public async Task<IAuthenticationResult> AcquireTokenAsync(string[] scope, string loginHint,
            UIBehavior behavior, string extraQueryParameters, string[] additionalScope, string authority)
        {
            Authority authorityInstance = Internal.Instance.Authority.CreateAuthority(authority, ValidateAuthority);
            return
                await
                    AcquireTokenCommonAsync(authorityInstance, scope, additionalScope,
                        loginHint, behavior, extraQueryParameters).ConfigureAwait(false);
        }

        /// <summary>
        /// Interactive request to acquire token. 
        /// </summary>
        /// <param name="scope">Array of scopes requested for resource</param>
        /// <param name="user">User object to enforce the same user to be authenticated in the web UI.</param>
        /// <param name="behavior">Enumeration to control UI behavior.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. The parameter can be null.</param>
        /// <param name="additionalScope">Array of scopes for which a developer can request consent upfront.</param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured does not change the configured value</param>
        /// <returns>Authentication result containing token of the user</returns>
        public async Task<IAuthenticationResult> AcquireTokenAsync(string[] scope, User user,
            UIBehavior behavior, string extraQueryParameters, string[] additionalScope, string authority)
        {
            Authority authorityInstance = Internal.Instance.Authority.CreateAuthority(authority, ValidateAuthority);
            return
                await
                    AcquireTokenCommonAsync(authorityInstance, scope, additionalScope, user,
                        behavior, extraQueryParameters).ConfigureAwait(false);
        }

        internal IWebUI CreateWebAuthenticationDialog(IPlatformParameters parameters, UIBehavior behavior, RequestContext requestContext)
        {
            return PlatformPlugin.WebUIFactory.CreateAuthenticationDialog(parameters, behavior, requestContext);
        }

        /// <summary>
        /// .NET specific method for intergrated auth.
        /// </summary>
        /// <param name="scope"></param>
        /// <returns></returns>
        internal async Task<AuthenticationResult> AcquireTokenWithIntegratedAuthInternalAsync(string[] scope)
        {
            Authority authority = Internal.Instance.Authority.CreateAuthority(Authority, ValidateAuthority);
            return
                await
                    AcquireTokenUsingIntegratedAuthCommonAsync(authority, scope,
                        new UserCredential()).ConfigureAwait(false);
        }

        /// <summary>
        /// .NET specific method for intergrated auth.
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="authority"></param>
        /// <returns></returns>
        internal async Task<AuthenticationResult> AcquireTokenWithIntegratedAuthInternalAsync(string[] scope,
            string authority)
        {
            Authority authorityInstance = Internal.Instance.Authority.CreateAuthority(authority, ValidateAuthority);
            return
                await
                    AcquireTokenUsingIntegratedAuthCommonAsync(authorityInstance, scope,
                        new UserCredential()).ConfigureAwait(false);
        }

        private async Task<AuthenticationResult> AcquireTokenUsingIntegratedAuthCommonAsync(Authority authority,
            string[] scope, UserCredential userCredential)
        {
/*            var requestParams = this.CreateRequestParameters(authority, scope, policy, this.UserTokenCache);
            var handler = new SilentWebUiRequest(requestParams, userCredential);
            return await handler.RunAsync().ConfigureAwait(false);*/
            await Task.Run(() => { throw new NotImplementedException(); });
            return null;
        }

        private async Task<AuthenticationResult> AcquireTokenCommonAsync(Authority authority, string[] scope,
            string[] additionalScope, string loginHint, UIBehavior behavior,
            string extraQueryParameters)
        {
            var requestParams = CreateRequestParameters(authority, scope, null, UserTokenCache);
            requestParams.ExtraQueryParameters = extraQueryParameters;
            
            var handler =
                new InteractiveRequest(requestParams, additionalScope, loginHint, behavior,
                    CreateWebAuthenticationDialog(PlatformParameters, behavior, requestParams.RequestContext));
            return await handler.RunAsync().ConfigureAwait(false);
        }

        private async Task<AuthenticationResult> AcquireTokenCommonAsync(Authority authority, string[] scope,
            string[] additionalScope, User user, UIBehavior behavior, string extraQueryParameters)
        {

            var requestParams = CreateRequestParameters(authority, scope, user, UserTokenCache);
            requestParams.ExtraQueryParameters = extraQueryParameters;

            var handler =
                new InteractiveRequest(requestParams, additionalScope, behavior,
                    CreateWebAuthenticationDialog(PlatformParameters, behavior, requestParams.RequestContext));
            return await handler.RunAsync().ConfigureAwait(false);
        }

        internal override AuthenticationRequestParameters CreateRequestParameters(Authority authority, string[] scope, User user, TokenCache cache)
        {
            AuthenticationRequestParameters parameters = base.CreateRequestParameters(authority, scope, user, cache);
            parameters.ClientId = ClientId;
            if (PlatformParameters == null)
            {
                PlatformParameters = PlatformPlugin.DefaultPlatformParameters;
            }

            return parameters;
        }
    }
}