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
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using System.Collections.Generic;
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Instance;
using Microsoft.Identity.Core.UI;
using Microsoft.Identity.Core.Telemetry;

namespace Microsoft.Identity.Client
{
#if !DESKTOP && !NET_CORE
#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved
#endif
    /// <summary>
    /// Class to be used to acquire tokens in desktop or mobile applications (Desktop / UWP / Xamarin.iOS / Xamarin.Android).
    /// public client applications are not trusted to safely keep application secrets, and therefore they only access Web APIs in the name of the user only 
    /// (they only support public client flows). For details see https://aka.ms/msal-net-client-applications
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Contrary to <see cref="Microsoft.Identity.Client.ConfidentialClientApplication"/>, public clients are unable to hold configuration time secrets, 
    /// and as a result have no client secret</description></item>
    /// <item><description>the redirect URL is pre-proposed by the library. It does not need to be passed in the constructor</description></item>
    /// <item><description>.NET Core does not support UI, and therefore this platform does not provide the interactive token acquisition methods. Actually
    /// until MSAL.NET supports Windows integrated authentication, Username/Password, and Device Code Flow, the .NET Core platform does not
    /// provide any public client application flows</description></item>
    /// </list>
    /// </remarks>
    public sealed partial class PublicClientApplication : ClientApplicationBase, IPublicClientApplication
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved
    {
        static PublicClientApplication()
        {
            ModuleInitializer.EnsureModuleInitialized();
        }

        /// <summary>
        /// Consutructor of the application. It will use https://login.microsoftonline.com/common as the default authority.
        /// </summary>
        /// <param name="clientId">Client ID (also known as App ID) of the application as registered in the 
        /// application registration portal (https://aka.ms/msal-net-register-app)/. REQUIRED</param>
        public PublicClientApplication(string clientId) : this(clientId, DefaultAuthority)
        {
        }

        /// <summary>
        /// Consutructor of the application.
        /// </summary>
        /// <param name="clientId">Client ID (also named Application ID) of the application as registered in the 
        /// application registration portal (https://aka.ms/msal-net-register-app)/. REQUIRED</param>
        /// <param name="authority">Authority of the security token service (STS) from which MSAL.NET will acquire the tokens.
        /// Usual authorities are:
        /// <list type="bullet">
        /// <item><description><c>https://login.microsoftonline.com/tenant/</c>, where <c>tenant</c> is the tenant ID of the Azure AD tenant
        /// or a domain associated with this Azure AD tenant, in order to sign-in user of a specific organization only</description></item>
        /// <item><description><c>https://login.microsoftonline.com/common/</c> to signing users with any work and school accounts or Microsoft personal account</description></item>
        /// <item><description><c>https://login.microsoftonline.com/organizations/</c> to signing users with any work and school accounts</description></item>
        /// <item><description><c>https://login.microsoftonline.com/consumers/</c> to signing users with only personal Microsoft account (live)</description></item>
        /// </list>
        /// Note that this setting needs to be consistent with what is declared in the application registration portal 
        /// </param>
        public PublicClientApplication(string clientId, string authority)
            : base(clientId, authority, PlatformPlugin.PlatformInformation.GetDefaultRedirectUri(clientId), true)
        {
            UserTokenCache = new TokenCache()
            {
                ClientId = clientId
            };
        }
        // netcoreapp does not support UI at the moment and all the Acquire* methods use UI;
#if !NET_CORE

#if iOS
        private string keychainSecurityGroup;

        /// <summary>
        /// Xamarin iOS specific property enabling the application to share the token cache with other applications sharing the same keychain security group.
        /// If you provide this key, you MUST add the capability to your Application Entitlement.
        /// For more details, please see https://aka.ms/msal-net-sharing-cache-on-ios
        /// </summary>
        /// <remarks>This API may change in future release.</remarks>
        public string KeychainSecurityGroup
        {
            get 
            { 
                return keychainSecurityGroup; 
            }
            set 
            { 
                keychainSecurityGroup = value;
                UserTokenCache.tokenCacheAccessor.SetKeychainSecurityGroup(value);
                UserTokenCache.legacyCachePersistance.SetKeychainSecurityGroup(value);
            }
        }
#endif

#if WINDOWS_APP
        /// <summary>
        /// Flag to enable authentication with the user currently logeed-in in Windows.
        /// When set to true, the application will try to connect to the corporate network using windows integrated authentication.
        /// </summary>
        public bool UseCorporateNetwork { get; set; }
#endif

#if !ANDROID
        /// <summary>
        /// Interactive request to acquire token for the specified scopes. The user is required to select an account
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        /// <remarks>The user will be signed-in interactively if needed,
        /// and will consent to scopes and do multi-factor authentication if such a policy was enabled in the Azure AD tenant.</remarks>
        public async Task<AuthenticationResult> AcquireTokenAsync(IEnumerable<string> scopes)
        {
            Authority authority = Core.Instance.Authority.CreateAuthority(Authority, ValidateAuthority);
            return
                await
                    AcquireTokenForLoginHintCommonAsync(authority, scopes, null, null,
                        UIBehavior.SelectAccount, null, null, ApiEvent.ApiIds.AcquireTokenWithScope).ConfigureAwait(false);
        }

        /// <summary>
        /// Interactive request to acquire token for the specified scopes. The user will need to sign-in but an account will be proposed
        /// based on the <paramref name="loginHint"/>
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="loginHint">Identifier of the user. Generally in UserPrincipalName (UPN) format, e.g. <c>john.doe@contoso.com</c></param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(IEnumerable<string> scopes, string loginHint)
        {
            Authority authority = Core.Instance.Authority.CreateAuthority(Authority, ValidateAuthority);
            return
                await
                    AcquireTokenForLoginHintCommonAsync(authority, scopes, null, loginHint,
                        UIBehavior.SelectAccount, null, null, ApiEvent.ApiIds.AcquireTokenWithScopeHint).ConfigureAwait(false);
        }

        /// <summary>
        /// Interactive request to acquire token for the specified scopes. The user will need to sign-in but an account will be proposed
        /// based on the provided <paramref name="account"/>
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="account">Account to use for the interactive token acquisition. See <see cref="IAccount"/> for ways to get an account</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            IAccount account)
        {
            Authority authority = Core.Instance.Authority.CreateAuthority(Authority, ValidateAuthority);
            return
                await
                    AcquireTokenForUserCommonAsync(authority, scopes, null, account,
                        UIBehavior.SelectAccount, null, null, ApiEvent.ApiIds.AcquireTokenWithScopeUser).ConfigureAwait(false);
        }

        /// <summary>
        /// Interactive request to acquire token for a login with control of the UI behavior and possiblity of passing extra query parameters like additional claims
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="loginHint">Identifier of the user. Generally in UserPrincipalName (UPN) format, e.g. <c>john.doe@contoso.com</c></param>
        /// <param name="behavior">Designed interactive experience for the user.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. 
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(IEnumerable<string> scopes, string loginHint,
            UIBehavior behavior, string extraQueryParameters)
        {
            Authority authority = Core.Instance.Authority.CreateAuthority(Authority, ValidateAuthority);
            return
                await
                    AcquireTokenForLoginHintCommonAsync(authority, scopes, null, loginHint,
                        behavior, extraQueryParameters, null, ApiEvent.ApiIds.AcquireTokenWithScopeHintBehavior).ConfigureAwait(false);
        }

        /// <summary>
        /// Interactive request to acquire token for an account with control of the UI behavior and possiblity of passing extra query parameters like additional claims
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="account">Account to use for the interactive token acquisition. See <see cref="IAccount"/> for ways to get an account</param>
        /// <param name="behavior">Designed interactive experience for the user.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. 
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(IEnumerable<string> scopes, IAccount account,
            UIBehavior behavior, string extraQueryParameters)
        {
            Authority authority = Core.Instance.Authority.CreateAuthority(Authority, ValidateAuthority);
            return
                await
                    AcquireTokenForUserCommonAsync(authority, scopes, null, account, behavior,
                        extraQueryParameters, null, ApiEvent.ApiIds.AcquireTokenWithScopeUserBehavior).ConfigureAwait(false);
        }

        /// <summary>
        /// Interactive request to acquire token for a given login, with the possibility of controlling the user experience, passing extra query
        /// parameters, providing extra scopes that the user can pre-consent to, and overriding the authority pre-configured in the application
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="loginHint">Identifier of the user. Generally in UserPrincipalName (UPN) format, e.g. <c>john.doe@contoso.com</c></param>
        /// <param name="behavior">Designed interactive experience for the user.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. 
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <param name="extraScopesToConsent">Scopes that you can request the end user to consent upfront, in addition to the scopes for the protected Web API
        /// for which you want to acquire a security token.</param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured does not change the configured value</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(IEnumerable<string> scopes, string loginHint,
            UIBehavior behavior, string extraQueryParameters, IEnumerable<string> extraScopesToConsent, string authority)
        {
            Authority authorityInstance = Core.Instance.Authority.CreateAuthority(authority, ValidateAuthority);
            return
                await
                    AcquireTokenForLoginHintCommonAsync(authorityInstance, scopes, extraScopesToConsent,
                        loginHint, behavior, extraQueryParameters, null, ApiEvent.ApiIds.AcquireTokenWithScopeHintBehaviorAuthority).ConfigureAwait(false);
        }

        /// <summary>
        /// Interactive request to acquire token for a given account, with the possibility of controlling the user experience, passing extra query
        /// parameters, providing extra scopes that the user can pre-consent to, and overriding the authority pre-configured in the application
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="account">Account to use for the interactive token acquisition. See <see cref="IAccount"/> for ways to get an account</param>
        /// <param name="behavior">Designed interactive experience for the user.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. 
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <param name="extraScopesToConsent">Scopes that you can request the end user to consent upfront, in addition to the scopes for the protected Web API
        /// for which you want to acquire a security token.</param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured does not change the configured value</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(IEnumerable<string> scopes, IAccount account,
            UIBehavior behavior, string extraQueryParameters, IEnumerable<string> extraScopesToConsent, string authority)
        {
            Authority authorityInstance = Core.Instance.Authority.CreateAuthority(authority, ValidateAuthority);
            return
                await
                    AcquireTokenForUserCommonAsync(authorityInstance, scopes, extraScopesToConsent, account,
                        behavior, extraQueryParameters, null, ApiEvent.ApiIds.AcquireTokenWithScopeUserBehaviorAuthority).ConfigureAwait(false);
        }
#endif

        /// <summary>
        /// Interactive request to acquire token for the specified scopes. The interactive window will be parented to the specified
        /// window. The user will be required to select an account
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="parent">Object containing a reference to the parent window/activity. REQUIRED for Xamarin.Android only.</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        /// <remarks>The user will be signed-in interactively if needed,
        /// and will consent to scopes and do multi-factor authentication if such a policy was enabled in the Azure AD tenant.</remarks>
        public async Task<AuthenticationResult> AcquireTokenAsync(IEnumerable<string> scopes, UIParent parent)
        {
            Authority authority = Core.Instance.Authority.CreateAuthority(Authority, ValidateAuthority);
            return
                await
                    AcquireTokenForLoginHintCommonAsync(authority, scopes, null, null,
                        UIBehavior.SelectAccount, null, parent, ApiEvent.ApiIds.AcquireTokenWithScope).ConfigureAwait(false);
        }

        /// <summary>
        /// Interactive request to acquire token for the specified scopes. The interactive window will be parented to the specified
        /// window. The user will need to sign-in but an account will be proposed
        /// based on the <paramref name="loginHint"/>
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="loginHint">Identifier of the user. Generally in UserPrincipalName (UPN) format, e.g. <c>john.doe@contoso.com</c></param>
        /// <param name="parent">Object containing a reference to the parent window/activity. REQUIRED for Xamarin.Android only.</param>
        /// <returns>Authentication result containing a token for the requested scopes and login</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(IEnumerable<string> scopes, string loginHint, UIParent parent)
        {
            Authority authority = Core.Instance.Authority.CreateAuthority(Authority, ValidateAuthority);
            return
                await
                    AcquireTokenForLoginHintCommonAsync(authority, scopes, null, loginHint,
                        UIBehavior.SelectAccount, null, parent, ApiEvent.ApiIds.AcquireTokenWithScopeHint).ConfigureAwait(false);
        }

        /// <summary>
        /// Interactive request to acquire token for the specified scopes. The user will need to sign-in but an account will be proposed
        /// based on the provided <paramref name="account"/>
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="account">Account to use for the interactive token acquisition. See <see cref="IAccount"/> for ways to get an account</param>
        /// <param name="parent">Object containing a reference to the parent window/activity. REQUIRED for Xamarin.Android only.</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            IAccount account, UIParent parent)
        {
            Authority authority = Core.Instance.Authority.CreateAuthority(Authority, ValidateAuthority);
            return
                await
                    AcquireTokenForUserCommonAsync(authority, scopes, null, account,
                        UIBehavior.SelectAccount, null, parent, ApiEvent.ApiIds.AcquireTokenWithScopeUser).ConfigureAwait(false);
        }

        /// <summary>
        /// Interactive request to acquire token for a login with control of the UI behavior and possiblity of passing extra query parameters like additional claims
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="loginHint">Identifier of the user. Generally in UserPrincipalName (UPN) format, e.g. <c>john.doe@contoso.com</c></param>
        /// <param name="behavior">Designed interactive experience for the user.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. 
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <param name="parent">Object containing a reference to the parent window/activity. REQUIRED for Xamarin.Android only.</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(IEnumerable<string> scopes, string loginHint,
            UIBehavior behavior, string extraQueryParameters, UIParent parent)
        {
            Authority authority = Core.Instance.Authority.CreateAuthority(Authority, ValidateAuthority);
            return
                await
                    AcquireTokenForLoginHintCommonAsync(authority, scopes, null, loginHint,
                        behavior, extraQueryParameters, parent, ApiEvent.ApiIds.AcquireTokenWithScopeHintBehavior).ConfigureAwait(false);
        }

        /// <summary>
        /// Interactive request to acquire token for an account with control of the UI behavior and possiblity of passing extra query parameters like additional claims
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="account">Account to use for the interactive token acquisition. See <see cref="IAccount"/> for ways to get an account</param>
        /// <param name="behavior">Designed interactive experience for the user.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. 
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <param name="parent">Object containing a reference to the parent window/activity. REQUIRED for Xamarin.Android only.</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(IEnumerable<string> scopes, IAccount account,
            UIBehavior behavior, string extraQueryParameters, UIParent parent)
        {
            Authority authority = Core.Instance.Authority.CreateAuthority(Authority, ValidateAuthority);
            return
                await
                    AcquireTokenForUserCommonAsync(authority, scopes, null, account, behavior,
                        extraQueryParameters, parent, ApiEvent.ApiIds.AcquireTokenWithScopeUserBehavior).ConfigureAwait(false);
        }

        /// <summary>
        /// Interactive request to acquire token for a given login, with the possibility of controlling the user experience, passing extra query
        /// parameters, providing extra scopes that the user can pre-consent to, and overriding the authority pre-configured in the application
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="loginHint">Identifier of the user. Generally in UserPrincipalName (UPN) format, e.g. <c>john.doe@contoso.com</c></param>
        /// <param name="behavior">Designed interactive experience for the user.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. 
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <param name="extraScopesToConsent">scopes that you can request the end user to consent upfront, in addition to the scopes for the protected Web API
        /// for which you want to acquire a security token.</param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured does not change the configured value</param>
        /// <param name="parent">Object containing a reference to the parent window/activity. REQUIRED for Xamarin.Android only.</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(IEnumerable<string> scopes, string loginHint,
            UIBehavior behavior, string extraQueryParameters, IEnumerable<string> extraScopesToConsent, string authority, UIParent parent)
        {
            Authority authorityInstance = Core.Instance.Authority.CreateAuthority(authority, ValidateAuthority);
            return
                await
                    AcquireTokenForLoginHintCommonAsync(authorityInstance, scopes, extraScopesToConsent,
                        loginHint, behavior, extraQueryParameters, parent, ApiEvent.ApiIds.AcquireTokenWithScopeHintBehaviorAuthority).ConfigureAwait(false);
        }

        /// <summary>
        /// Interactive request to acquire token for a given account, with the possibility of controlling the user experience, passing extra query
        /// parameters, providing extra scopes that the user can pre-consent to, and overriding the authority pre-configured in the application
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="account">Account to use for the interactive token acquisition. See <see cref="IAccount"/> for ways to get an account</param>
        /// <param name="behavior">Designed interactive experience for the user.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. 
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <param name="extraScopesToConsent">scopes that you can request the end user to consent upfront, in addition to the scopes for the protected Web API
        /// for which you want to acquire a security token.</param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured does not change the configured value</param>
        /// <param name="parent">Object containing a reference to the parent window/activity. REQUIRED for Xamarin.Android only.</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(IEnumerable<string> scopes, IAccount account,
        UIBehavior behavior, string extraQueryParameters, IEnumerable<string> extraScopesToConsent, string authority, UIParent parent)
        {
            Authority authorityInstance = Core.Instance.Authority.CreateAuthority(authority, ValidateAuthority);
            return
                await
                    AcquireTokenForUserCommonAsync(authorityInstance, scopes, extraScopesToConsent, account,
                        behavior, extraQueryParameters, parent, ApiEvent.ApiIds.AcquireTokenWithScopeUserBehaviorAuthority).ConfigureAwait(false);
        }

        /// <summary>
        /// Non-interactive request to acquire token via Windows Integrated Authentication.
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <returns>Authentication result containing token of the current login user</returns>
        public async Task<AuthenticationResult> AcquireTokenByWindowsIntegratedAuthAsync(IEnumerable<string> scopes)
        {
            return await AcquireTokenByUserCredentialAsync(scopes, new UserCredential()).ConfigureAwait(false);
        }

        /// <summary>
        /// Non-interactive request to acquire token via Windows Integrated Authentication.
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="username">Username</param>
        /// <returns>Authentication result containing token of the specified login user</returns>
        public async Task<AuthenticationResult> AcquireTokenByWindowsIntegratedAuthAsync(IEnumerable<string> scopes, string username)
        {
            return await AcquireTokenByUserCredentialAsync(scopes, new UserCredential(username)).ConfigureAwait(false);
        }

        /// <summary>
        /// Non-interactive request to acquire token via Windows Integrated Authentication.
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="userCredential">A UserCredential representing the user account</param>
        /// <returns>Authentication result containing token</returns>
        private async Task<AuthenticationResult> AcquireTokenByUserCredentialAsync(IEnumerable<string> scopes, UserCredential userCredential)
        {
            Authority authority = Core.Instance.Authority.CreateAuthority(Authority, ValidateAuthority);
            var requestParams = CreateRequestParameters(authority, scopes, null, UserTokenCache);
            var handler = new NonInteractiveRequest(requestParams, userCredential) { ApiId = ApiEvent.ApiIds.AcquireTokenWithScopeUser };
            return await handler.RunAsync().ConfigureAwait(false);
        }

        internal IWebUI CreateWebAuthenticationDialog(UIParent parent, UIBehavior behavior, RequestContext requestContext)
        {
            //create instance of UIParent and assign useCorporateNetwork to UIParent 
            if (parent == null)
            {
                parent = new UIParent();
            }

#if WINDOWS_APP || DESKTOP
            //hidden webview can be used in both WinRT and desktop applications.
            parent.UseHiddenBrowser = behavior.Equals(UIBehavior.Never);
#if WINDOWS_APP
            parent.UseCorporateNetwork = UseCorporateNetwork;
#endif
#endif

            return PlatformPlugin.WebUIFactory.CreateAuthenticationDialog(parent.CoreUIParent, requestContext);
        }

        private async Task<AuthenticationResult> AcquireTokenForLoginHintCommonAsync(Authority authority, IEnumerable<string> scopes,
            IEnumerable<string> extraScopesToConsent, string loginHint, UIBehavior behavior,
            string extraQueryParameters, UIParent parent, ApiEvent.ApiIds apiId)
        {
            var requestParams = CreateRequestParameters(authority, scopes, null, UserTokenCache);
            requestParams.ExtraQueryParameters = extraQueryParameters;

#if iOS || ANDROID
            if (!parent.CoreUIParent.UseEmbeddedWebview)
            {
                PlatformPlugin.PlatformInformation.ValidateRedirectUri(requestParams.RedirectUri, requestParams.RequestContext);
            }
#endif

            var handler =
                new InteractiveRequest(requestParams, extraScopesToConsent, loginHint, behavior,
                    CreateWebAuthenticationDialog(parent, behavior, requestParams.RequestContext))
                { ApiId = apiId };
            return await handler.RunAsync().ConfigureAwait(false);
        }

        private async Task<AuthenticationResult> AcquireTokenForUserCommonAsync(Authority authority, IEnumerable<string> scopes,
            IEnumerable<string> extraScopesToConsent, IAccount user, UIBehavior behavior, string extraQueryParameters, UIParent parent, ApiEvent.ApiIds apiId)
        {
            var requestParams = CreateRequestParameters(authority, scopes, user, UserTokenCache);
            requestParams.ExtraQueryParameters = extraQueryParameters;

#if iOS || ANDROID
            if (!parent.CoreUIParent.UseEmbeddedWebview)
            {
                PlatformPlugin.PlatformInformation.ValidateRedirectUri(requestParams.RedirectUri, requestParams.RequestContext);
            }
#endif

            var handler =
                new InteractiveRequest(requestParams, extraScopesToConsent, behavior,
                    CreateWebAuthenticationDialog(parent, behavior, requestParams.RequestContext))
                { ApiId = apiId };
            return await handler.RunAsync().ConfigureAwait(false);
        }

        internal override AuthenticationRequestParameters CreateRequestParameters(Authority authority,
            IEnumerable<string> scopes, IAccount user, TokenCache cache)
        {
            AuthenticationRequestParameters parameters = base.CreateRequestParameters(authority, scopes, user, cache);
            return parameters;
        }

// endif for !NET_CORE
#endif
    }
}