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
using System.Collections.Generic;
using Microsoft.Identity.Client.Internal.Telemetry;
using Microsoft.Identity.Core;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Class to be used for native applications (Desktop/UWP/iOS/Android).
    /// </summary>
    public sealed partial class PublicClientApplication : ClientApplicationBase, IPublicClientApplication
    {
        /// <summary>
        /// Consutructor of the application. It will use https://login.microsoftonline.com/common as the default authority.
        /// </summary>
        /// <param name="clientId">Client id of the application</param>
        public PublicClientApplication(string clientId) : this(clientId, DefaultAuthority)
        {
        }

        /// <summary>
        /// Consutructor of the application.
        /// </summary>
        /// <param name="clientId">Client id of the application</param>
        /// <param name="authority">Default authority to be used for the application</param>
        public PublicClientApplication(string clientId, string authority)
            : base(clientId, authority, PlatformPlugin.PlatformInformation.GetDefaultRedirectUri(clientId), true)
        {
            UserTokenCache = new TokenCache()
            {
                ClientId = clientId
            };
        }


#if WINRT
/// <summary>
/// 
/// </summary>
        public bool UseCorporateNetwork { get; set; }
#endif

#if !ANDROID
        /// <summary>
        /// Interactive request to acquire token. 
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <returns>Authentication result containing token of the user</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(IEnumerable<string> scopes)
        {
            Authority authority = Internal.Instance.Authority.CreateAuthority(Authority, ValidateAuthority);
            return
                await
                    AcquireTokenForLoginHintCommonAsync(authority, scopes, null, null,
                        UIBehavior.SelectAccount, null, null, ApiEvent.ApiIds.AcquireTokenWithScope).ConfigureAwait(false);
        }

        /// <summary>
        /// Interactive request to acquire token. 
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="loginHint">Identifier of the user. Generally a UPN.</param>
        /// <returns>Authentication result containing token of the user</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(IEnumerable<string> scopes, string loginHint)
        {
            Authority authority = Internal.Instance.Authority.CreateAuthority(Authority, ValidateAuthority);
            return
                await
                    AcquireTokenForLoginHintCommonAsync(authority, scopes, null, loginHint,
                        UIBehavior.SelectAccount, null, null, ApiEvent.ApiIds.AcquireTokenWithScopeHint).ConfigureAwait(false);
        }

        /// <summary>
        /// Interactive request to acquire token. 
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="user">User object to enforce the same user to be authenticated in the web UI.</param>
        /// <returns>Authentication result containing token of the user</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            IUser user)
        {
            Authority authority = Internal.Instance.Authority.CreateAuthority(Authority, ValidateAuthority);
            return
                await
                    AcquireTokenForUserCommonAsync(authority, scopes, null, user,
                        UIBehavior.SelectAccount, null, null, ApiEvent.ApiIds.AcquireTokenWithScopeUser).ConfigureAwait(false);
        }

        /// <summary>
        /// Interactive request to acquire token. 
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="loginHint">Identifier of the user. Generally a UPN.</param>
        /// <param name="behavior">Enumeration to control UI behavior.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. The parameter can be null.</param>
        /// <returns>Authentication result containing token of the user</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(IEnumerable<string> scopes, string loginHint,
            UIBehavior behavior, string extraQueryParameters)
        {
            Authority authority = Internal.Instance.Authority.CreateAuthority(Authority, ValidateAuthority);
            return
                await
                    AcquireTokenForLoginHintCommonAsync(authority, scopes, null, loginHint,
                        behavior, extraQueryParameters, null, ApiEvent.ApiIds.AcquireTokenWithScopeHintBehavior).ConfigureAwait(false);
        }

        /// <summary>
        /// Interactive request to acquire token. 
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="user">User object to enforce the same user to be authenticated in the web UI.</param>
        /// <param name="behavior">Enumeration to control UI behavior.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. The parameter can be null.</param>
        /// <returns>Authentication result containing token of the user</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(IEnumerable<string> scopes, IUser user,
            UIBehavior behavior, string extraQueryParameters)
        {
            Authority authority = Internal.Instance.Authority.CreateAuthority(Authority, ValidateAuthority);
            return
                await
                    AcquireTokenForUserCommonAsync(authority, scopes, null, user, behavior,
                        extraQueryParameters, null, ApiEvent.ApiIds.AcquireTokenWithScopeUserBehavior).ConfigureAwait(false);
        }

        /// <summary>
        /// Interactive request to acquire token. 
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="loginHint">Identifier of the user. Generally a UPN.</param>
        /// <param name="behavior">Enumeration to control UI behavior.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. The parameter can be null.</param>
        /// <param name="extraScopesToConsent">Array of scopes for which a developer can request consent upfront.</param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured does not change the configured value</param>
        /// <returns>Authentication result containing token of the user</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(IEnumerable<string> scopes, string loginHint,
            UIBehavior behavior, string extraQueryParameters, IEnumerable<string> extraScopesToConsent, string authority)
        {
            Authority authorityInstance = Internal.Instance.Authority.CreateAuthority(authority, ValidateAuthority);
            return
                await
                    AcquireTokenForLoginHintCommonAsync(authorityInstance, scopes, extraScopesToConsent,
                        loginHint, behavior, extraQueryParameters, null, ApiEvent.ApiIds.AcquireTokenWithScopeHintBehaviorAuthority).ConfigureAwait(false);
        }

        /// <summary>
        /// Interactive request to acquire token. 
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="user">User object to enforce the same user to be authenticated in the web UI.</param>
        /// <param name="behavior">Enumeration to control UI behavior.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. The parameter can be null.</param>
        /// <param name="extraScopesToConsent">Array of scopes for which a developer can request consent upfront.</param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured does not change the configured value</param>
        /// <returns>Authentication result containing token of the user</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(IEnumerable<string> scopes, IUser user,
            UIBehavior behavior, string extraQueryParameters, IEnumerable<string> extraScopesToConsent, string authority)
        {
            Authority authorityInstance = Internal.Instance.Authority.CreateAuthority(authority, ValidateAuthority);
            return
                await
                    AcquireTokenForUserCommonAsync(authorityInstance, scopes, extraScopesToConsent, user,
                        behavior, extraQueryParameters, null, ApiEvent.ApiIds.AcquireTokenWithScopeUserBehaviorAuthority).ConfigureAwait(false);
        }
#endif

        /// <summary>
        /// Interactive request to acquire token. 
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="parent">Object contains reference to parent window/activity. REQUIRED for Xamarin.Android only.</param>
        /// <returns>Authentication result containing token of the user</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(IEnumerable<string> scopes, UIParent parent)
        {
            Authority authority = Internal.Instance.Authority.CreateAuthority(Authority, ValidateAuthority);
            return
                await
                    AcquireTokenForLoginHintCommonAsync(authority, scopes, null, null,
                        UIBehavior.SelectAccount, null, parent, ApiEvent.ApiIds.AcquireTokenWithScope).ConfigureAwait(false);
        }

        /// <summary>
        /// Interactive request to acquire token. 
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="loginHint">Identifier of the user. Generally a UPN.</param>
        /// <param name="parent">Object contains reference to parent window/activity. REQUIRED for Xamarin.Android only.</param>
        /// <returns>Authentication result containing token of the user</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(IEnumerable<string> scopes, string loginHint, UIParent parent)
        {
            Authority authority = Internal.Instance.Authority.CreateAuthority(Authority, ValidateAuthority);
            return
                await
                    AcquireTokenForLoginHintCommonAsync(authority, scopes, null, loginHint,
                        UIBehavior.SelectAccount, null, parent, ApiEvent.ApiIds.AcquireTokenWithScopeHint).ConfigureAwait(false);
        }

        /// <summary>
        /// Interactive request to acquire token. 
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="user">User object to enforce the same user to be authenticated in the web UI.</param>
        /// <param name="parent">Object contains reference to parent window/activity. REQUIRED for Xamarin.Android only.</param>
        /// <returns>Authentication result containing token of the user</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            IUser user, UIParent parent)
        {
            Authority authority = Internal.Instance.Authority.CreateAuthority(Authority, ValidateAuthority);
            return
                await
                    AcquireTokenForUserCommonAsync(authority, scopes, null, user,
                        UIBehavior.SelectAccount, null, parent, ApiEvent.ApiIds.AcquireTokenWithScopeUser).ConfigureAwait(false);
        }

        /// <summary>
        /// Interactive request to acquire token. 
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="loginHint">Identifier of the user. Generally a UPN.</param>
        /// <param name="behavior">Enumeration to control UI behavior.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. The parameter can be null.</param>
        /// <param name="parent">Object contains reference to parent window/activity. REQUIRED for Xamarin.Android only.</param>
        /// <returns>Authentication result containing token of the user</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(IEnumerable<string> scopes, string loginHint,
            UIBehavior behavior, string extraQueryParameters, UIParent parent)
        {
            Authority authority = Internal.Instance.Authority.CreateAuthority(Authority, ValidateAuthority);
            return
                await
                    AcquireTokenForLoginHintCommonAsync(authority, scopes, null, loginHint,
                        behavior, extraQueryParameters, parent, ApiEvent.ApiIds.AcquireTokenWithScopeHintBehavior).ConfigureAwait(false);
        }

        /// <summary>
        /// Interactive request to acquire token. 
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="user">User object to enforce the same user to be authenticated in the web UI.</param>
        /// <param name="behavior">Enumeration to control UI behavior.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. The parameter can be null.</param>
        /// <param name="parent">Object contains reference to parent window/activity. REQUIRED for Xamarin.Android only.</param>
        /// <returns>Authentication result containing token of the user</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(IEnumerable<string> scopes, IUser user,
            UIBehavior behavior, string extraQueryParameters, UIParent parent)
        {
            Authority authority = Internal.Instance.Authority.CreateAuthority(Authority, ValidateAuthority);
            return
                await
                    AcquireTokenForUserCommonAsync(authority, scopes, null, user, behavior,
                        extraQueryParameters, parent, ApiEvent.ApiIds.AcquireTokenWithScopeUserBehavior).ConfigureAwait(false);
        }

        /// <summary>
        /// Interactive request to acquire token. 
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="loginHint">Identifier of the user. Generally a UPN.</param>
        /// <param name="behavior">Enumeration to control UI behavior.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. The parameter can be null.</param>
        /// <param name="extraScopesToConsent">Array of scopes for which a developer can request consent upfront.</param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured does not change the configured value</param>
        /// <param name="parent">Object contains reference to parent window/activity. REQUIRED for Xamarin.Android only.</param>
        /// <returns>Authentication result containing token of the user</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(IEnumerable<string> scopes, string loginHint,
            UIBehavior behavior, string extraQueryParameters, IEnumerable<string> extraScopesToConsent, string authority, UIParent parent)
        {
            Authority authorityInstance = Internal.Instance.Authority.CreateAuthority(authority, ValidateAuthority);
            return
                await
                    AcquireTokenForLoginHintCommonAsync(authorityInstance, scopes, extraScopesToConsent,
                        loginHint, behavior, extraQueryParameters, parent, ApiEvent.ApiIds.AcquireTokenWithScopeHintBehaviorAuthority).ConfigureAwait(false);
        }

        /// <summary>
        /// Interactive request to acquire token. 
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="user">User object to enforce the same user to be authenticated in the web UI.</param>
        /// <param name="behavior">Enumeration to control UI behavior.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. The parameter can be null.</param>
        /// <param name="extraScopesToConsent">Array of scopes for which a developer can request consent upfront.</param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured does not change the configured value</param>
        /// <param name="parent">Object contains reference to parent window/activity. REQUIRED for Xamarin.Android only.</param>
        /// <returns>Authentication result containing token of the user</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(IEnumerable<string> scopes, IUser user,
            UIBehavior behavior, string extraQueryParameters, IEnumerable<string> extraScopesToConsent, string authority, UIParent parent)
        {
            Authority authorityInstance = Internal.Instance.Authority.CreateAuthority(authority, ValidateAuthority);
            return
                await
                    AcquireTokenForUserCommonAsync(authorityInstance, scopes, extraScopesToConsent, user,
                        behavior, extraQueryParameters, parent, ApiEvent.ApiIds.AcquireTokenWithScopeUserBehaviorAuthority).ConfigureAwait(false);
        }



        internal IWebUI CreateWebAuthenticationDialog(UIParent parent, UIBehavior behavior, RequestContext requestContext)
        {
            //create instance of UIParent and assign useCorporateNetwork to UIParent 
            if (parent == null)
            {
                parent = new UIParent();
            }

#if WINRT || DESKTOP
            //hidden webview can be used in both WinRT and desktop applications.
            parent.UseHiddenBrowser = behavior.Equals(UIBehavior.Never);
#if WINRT
            parent.UseCorporateNetwork = UseCorporateNetwork;
#endif
#endif

            return PlatformPlugin.WebUIFactory.CreateAuthenticationDialog(parent, requestContext);
        }

        private async Task<AuthenticationResult> AcquireTokenForLoginHintCommonAsync(Authority authority, IEnumerable<string> scopes,
            IEnumerable<string> extraScopesToConsent, string loginHint, UIBehavior behavior,
            string extraQueryParameters, UIParent parent, ApiEvent.ApiIds apiId)
        {
            var requestParams = CreateRequestParameters(authority, scopes, null, UserTokenCache);
            requestParams.ExtraQueryParameters = extraQueryParameters;
            var handler =
                new InteractiveRequest(requestParams, extraScopesToConsent, loginHint, behavior,
                    CreateWebAuthenticationDialog(parent, behavior, requestParams.RequestContext)){ApiId = apiId};
            return await handler.RunAsync().ConfigureAwait(false);
        }

        private async Task<AuthenticationResult> AcquireTokenForUserCommonAsync(Authority authority, IEnumerable<string> scopes,
            IEnumerable<string> extraScopesToConsent, IUser user, UIBehavior behavior, string extraQueryParameters, UIParent parent, ApiEvent.ApiIds apiId)
        {

            var requestParams = CreateRequestParameters(authority, scopes, user, UserTokenCache);
            requestParams.ExtraQueryParameters = extraQueryParameters;

            var handler =
                new InteractiveRequest(requestParams, extraScopesToConsent, behavior,
                    CreateWebAuthenticationDialog(parent, behavior, requestParams.RequestContext)){ApiId = apiId};
            return await handler.RunAsync().ConfigureAwait(false);
        }

        internal override AuthenticationRequestParameters CreateRequestParameters(Authority authority,
            IEnumerable<string> scopes, IUser user, TokenCache cache)
        {
            AuthenticationRequestParameters parameters = base.CreateRequestParameters(authority, scopes, user, cache);
            return parameters;
        }
    }
}