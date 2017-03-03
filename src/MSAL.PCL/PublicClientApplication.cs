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
    /// Native applications (desktop/phone/iOS/Android).
    /// </summary>
    public sealed class PublicClientApplication : ClientApplicationBase
    {
        private const string DEFAULT_REDIRECT_URI = "urn:ietf:wg:oauth:2.0:oob";

        /// <summary>
        /// .NET specific property that allows configuration of platform specific properties. For example, in iOS/Android it
        /// would include the flag to enable/disable broker.
        /// </summary>
        public IPlatformParameters PlatformParameters { get; set; }

        /// <summary>
        /// Default consutructor of the application.
        /// </summary>
        public PublicClientApplication(string clientId) : this(clientId, DefaultAuthority)
        {
        }

        /// <summary>
        /// </summary>
        public PublicClientApplication(string clientId, string authority)
            : base(authority, clientId, DEFAULT_REDIRECT_URI, true)
        {
            this.UserTokenCache = new TokenCache(clientId);
        }

        /// <summary>
        /// </summary>
        /// <param name="scope"></param>
        /// <returns></returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(string[] scope)
        {
            Authority authority = Internal.Instance.Authority.CreateAuthority(this.Authority, this.ValidateAuthority);
            return
                await
                    this.AcquireTokenCommonAsync(authority, scope, null, (string) null,
                        UiOptions.SelectAccount, null).ConfigureAwait(false);
        }

        /// <summary>
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="loginHint"></param>
        /// <returns></returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(string[] scope, string loginHint)
        {
            Authority authority = Internal.Instance.Authority.CreateAuthority(this.Authority, this.ValidateAuthority);
            return
                await
                    this.AcquireTokenCommonAsync(authority, scope, null, loginHint,
                        UiOptions.SelectAccount, null).ConfigureAwait(false);
        }

        /// <summary>
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="loginHint"></param>
        /// <param name="extraQueryParameters"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(string[] scope, string loginHint,
            UiOptions options, string extraQueryParameters)
        {
            Authority authority = Internal.Instance.Authority.CreateAuthority(this.Authority, this.ValidateAuthority);
            return
                await
                    this.AcquireTokenCommonAsync(authority, scope, null, loginHint,
                        options, extraQueryParameters).ConfigureAwait(false);
        }

        /// <summary>
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="user"></param>
        /// <param name="options"></param>
        /// <param name="extraQueryParameters"></param>
        /// <returns></returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(string[] scope, User user,
            UiOptions options, string extraQueryParameters)
        {
            Authority authority = Internal.Instance.Authority.CreateAuthority(this.Authority, this.ValidateAuthority);
            return
                await
                    this.AcquireTokenCommonAsync(authority, scope, null, user, options,
                        extraQueryParameters).ConfigureAwait(false);
        }

        /// <summary>
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="loginHint"></param>
        /// <param name="extraQueryParameters"></param>
        /// <param name="options"></param>
        /// <param name="additionalScope"></param>
        /// <param name="authority"></param>
        /// <returns></returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(string[] scope, string loginHint,
            UiOptions options, string extraQueryParameters, string[] additionalScope, string authority)
        {
            Authority authorityInstance = Internal.Instance.Authority.CreateAuthority(authority, this.ValidateAuthority);
            return
                await
                    this.AcquireTokenCommonAsync(authorityInstance, scope, additionalScope,
                        loginHint, options, extraQueryParameters).ConfigureAwait(false);
        }

        /// <summary>
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="user"></param>
        /// <param name="options"></param>
        /// <param name="extraQueryParameters"></param>
        /// <param name="additionalScope"></param>
        /// <param name="authority"></param>
        /// <returns></returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(string[] scope, User user,
            UiOptions options, string extraQueryParameters, string[] additionalScope, string authority)
        {
            Authority authorityInstance = Internal.Instance.Authority.CreateAuthority(authority, this.ValidateAuthority);
            return
                await
                    this.AcquireTokenCommonAsync(authorityInstance, scope, additionalScope, user,
                        options, extraQueryParameters).ConfigureAwait(false);
        }

        internal IWebUI CreateWebAuthenticationDialog(IPlatformParameters parameters)
        {
            return PlatformPlugin.WebUIFactory.CreateAuthenticationDialog(parameters);
        }

        /// <summary>
        /// .NET specific method for intergrated auth. To support Xamarin, we would need to move these to platform specific
        /// libraries.
        /// </summary>
        /// <param name="scope"></param>
        /// <returns></returns>
        internal async Task<AuthenticationResult> AcquireTokenWithIntegratedAuthInternalAsync(string[] scope)
        {
            Authority authority = Internal.Instance.Authority.CreateAuthority(this.Authority, this.ValidateAuthority);
            return
                await
                    this.AcquireTokenUsingIntegratedAuthCommonAsync(authority, scope,
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
            Authority authorityInstance = Internal.Instance.Authority.CreateAuthority(authority, this.ValidateAuthority);
            return
                await
                    this.AcquireTokenUsingIntegratedAuthCommonAsync(authorityInstance, scope,
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
            string[] additionalScope, string loginHint, UiOptions uiOptions,
            string extraQueryParameters)
        {
            var requestParams = this.CreateRequestParameters(authority, scope, null, this.UserTokenCache);
            requestParams.ExtraQueryParameters = extraQueryParameters;
            
            var handler =
                new InteractiveRequest(requestParams, additionalScope, loginHint, uiOptions,
                    this.CreateWebAuthenticationDialog(this.PlatformParameters));
            return await handler.RunAsync().ConfigureAwait(false);
        }

        private async Task<AuthenticationResult> AcquireTokenCommonAsync(Authority authority, string[] scope,
            string[] additionalScope, User user, UiOptions uiOptions, string extraQueryParameters)
        {

            var requestParams = this.CreateRequestParameters(authority, scope, user, this.UserTokenCache);
            requestParams.ExtraQueryParameters = extraQueryParameters;

            var handler =
                new InteractiveRequest(requestParams, additionalScope, uiOptions,
                    this.CreateWebAuthenticationDialog(this.PlatformParameters));
            return await handler.RunAsync().ConfigureAwait(false);
        }

        internal override AuthenticationRequestParameters CreateRequestParameters(Authority authority, string[] scope, User user, TokenCache cache)
        {
            AuthenticationRequestParameters parameters = base.CreateRequestParameters(authority, scope, user, cache);
            parameters.ClientId = this.ClientId;
            if (this.PlatformParameters == null)
            {
                this.PlatformParameters = PlatformPlugin.DefaultPlatformParameters;
            }

            return parameters;
        }
    }
}