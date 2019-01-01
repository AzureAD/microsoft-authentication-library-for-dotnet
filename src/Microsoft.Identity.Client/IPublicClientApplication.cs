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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.CallConfig;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Interface to be used with desktop or mobile applications (Desktop / UWP / Xamarin.iOS / Xamarin.Android).
    /// public client applications are not trusted to safely keep application secrets, and therefore they only access Web APIs in the name of the user only 
    /// (they only support public client flows). For details see https://aka.ms/msal-net-client-applications
    /// </summary>
    public partial interface IPublicClientApplication : IClientApplicationBase
    {

#if !NET_CORE_BUILDTIME

#if iOS
        /// <summary>
        /// Xamarin iOS specific property enabling the application to share the token cache with other applications sharing the same keychain security group.
        /// If you provide this key, you MUST add the capability to your Application Entitlement.
        /// For more details, please see https://aka.ms/msal-net-sharing-cache-on-ios
        /// </summary>
        /// <remarks>This API may change in future release.</remarks>
        string KeychainSecurityGroup {get;set;}
#endif

#if WINDOWS_APP
       /// <summary>
        /// Flag to enable authentication with the user currently logeed-in in Windows.
        /// When set to true, the application will try to connect to the corporate network using windows integrated authentication.
        /// </summary>
        bool UseCorporateNetwork { get; set; }
#endif

        /// <summary>
        /// 
        /// </summary>
        /// <param name="interactiveParameters"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<AuthenticationResult> AcquireTokenAsync(IAcquireTokenInteractiveParameters interactiveParameters, CancellationToken cancellationToken);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="usernamePasswordParameters"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<AuthenticationResult> AcquireTokenAsync(IAcquireTokenWithUsernamePasswordParameters usernamePasswordParameters, CancellationToken cancellationToken);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="integratedWindowsAuthParameters"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<AuthenticationResult> AcquireTokenAsync(IAcquireTokenWithIntegratedWindowsAuthParameters integratedWindowsAuthParameters, CancellationToken cancellationToken);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="deviceCodeParameters"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<AuthenticationResult> AcquireTokenAsync(IAcquireTokenWithDeviceCodeParameters deviceCodeParameters, CancellationToken cancellationToken);

        // expose the interactive API without UIParent only for platforms that 
        // do not need it to operate like desktop, UWP, iOS.
#if !ANDROID_BUILDTIME
        /// <summary>
        /// Interactive request to acquire token for the specified scopes. The user is required to select an account
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        /// <remarks>The user will be signed-in interactively if needed,
        /// and will consent to scopes and do multi-factor authentication if such a policy was enabled in the Azure AD tenant.</remarks>
        Task<AuthenticationResult> AcquireTokenAsync(IEnumerable<string> scopes);

        /// <summary>
        /// Interactive request to acquire token for the specified scopes. The user will need to sign-in but an account will be proposed
        /// based on the <paramref name="loginHint"/>
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="loginHint">Identifier of the user. Generally in UserPrincipalName (UPN) format, e.g. <c>john.doe@contoso.com</c></param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            string loginHint);

        /// <summary>
        /// Interactive request to acquire token for the specified scopes. The user will need to sign-in but an account will be proposed
        /// based on the provided <paramref name="account"/>
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="account">Account to use for the interactive token acquisition. See <see cref="IAccount"/> for ways to get an account</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            IAccount account);

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
        Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            string loginHint,
            UIBehavior behavior,
            string extraQueryParameters);

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
        Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            IAccount account,
            UIBehavior behavior,
            string extraQueryParameters);

        /// <summary>
        /// Interactive request to acquire token for a given login, with the possibility of controlling the user experience, passing extra query
        /// parameters, providing extra scopes that the user can pre-consent to, and overriding the authority pre-configured in the application
        /// </summary>
        /// <param name="scopes">scopes requested to access a protected API</param>
        /// <param name="loginHint">Identifier of the user. Generally in UserPrincipalName (UPN) format, e.g. <c>john.doe@contoso.com</c></param>
        /// <param name="behavior">Designed interactive experience for the user.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. 
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <param name="extraScopesToConsent">scopes that you can request the end user to consent upfront, in addition to the scopes for the protected Web API
        /// for which you want to acquire a security token.</param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured does not change the configured value</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            string loginHint,
            UIBehavior behavior,
            string extraQueryParameters,
            IEnumerable<string> extraScopesToConsent, string authority);

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
        Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            IAccount account,
            UIBehavior behavior,
            string extraQueryParameters,
            IEnumerable<string> extraScopesToConsent,
            string authority);

        // endif !ANDROID
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
#pragma warning disable CS0618 // UIParent is obsolete
        Task<AuthenticationResult> AcquireTokenAsync(IEnumerable<string> scopes, UIParent parent);
#pragma warning restore CS0618 // UIParent is obsolete

        /// <summary>
        /// Interactive request to acquire token for the specified scopes. The interactive window will be parented to the specified
        /// window. . The user will need to sign-in but an account will be proposed
        /// based on the <paramref name="loginHint"/>
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="loginHint">Identifier of the user. Generally in UserPrincipalName (UPN) format, e.g. <c>john.doe@contoso.com</c></param>
        /// <param name="parent">Object containing a reference to the parent window/activity. REQUIRED for Xamarin.Android only.</param>
        /// <returns>Authentication result containing a token for the requested scopes and login</returns>
#pragma warning disable CS0618 // UIParent is obsolete
        Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            string loginHint, UIParent parent);
#pragma warning restore CS0618 // UIParent is obsolete

        /// <summary>
        /// Interactive request to acquire token for the specified scopes. The user will need to sign-in but an account will be proposed
        /// based on the provided <paramref name="account"/>
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="account">Account to use for the interactive token acquisition. See <see cref="IAccount"/> for ways to get an account</param>
        /// <param name="parent">Object containing a reference to the parent window/activity. REQUIRED for Xamarin.Android only.</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
#pragma warning disable CS0618 // UIParent is obsolete
        Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            IAccount account, UIParent parent);
#pragma warning restore CS0618 // UIParent is obsolete

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
#pragma warning disable CS0618 // UIParent is obsolete
        Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            string loginHint,
            UIBehavior behavior,
            string extraQueryParameters, UIParent parent);
#pragma warning restore CS0618 // UIParent is obsolete

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
#pragma warning disable CS0618 // UIParent is obsolete
        Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            IAccount account,
            UIBehavior behavior,
            string extraQueryParameters, UIParent parent);
#pragma warning restore CS0618 // UIParent is obsolete

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
        /// <param name="parent">Object containing a reference to the parent window/activity. REQUIRED for Xamarin.Android only.</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
#pragma warning disable CS0618 // UIParent is obsolete
        Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            string loginHint,
            UIBehavior behavior,
            string extraQueryParameters,
            IEnumerable<string> extraScopesToConsent, string authority, UIParent parent);
#pragma warning restore CS0618 // UIParent is obsolete

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
        /// <param name="parent">Object containing a reference to the parent window/activity. REQUIRED for Xamarin.Android only.</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
#pragma warning disable CS0618 // UIParent is obsolete
        Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            IAccount account,
            UIBehavior behavior,
            string extraQueryParameters,
            IEnumerable<string> extraScopesToConsent,
            string authority, UIParent parent);
#pragma warning restore CS0618 // UIParent is obsolete
        // endif !NOT_CORE
#endif

        #region DEVICE CODE

        /// <summary>
        /// Acquires a security token on a device without a Web browser, by letting the user authenticate on 
        /// another device. This is done in two steps:
        /// <list type="bullet">
        /// <item><description>the method first acquires a device code from the authority and returns it to the caller via
        /// the <paramref name="deviceCodeResultCallback"/>. This callback takes care of interacting with the user
        /// to direct them to authenticate (to a specific URL, with a code)</description></item>
        /// <item><description>The method then proceeds to poll for the security
        /// token which is granted upon successful login by the user based on the device code information</description></item>
        /// </list>
        /// See https://aka.ms/msal-device-code-flow.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="deviceCodeResultCallback">Callback containing information to show the user about how to authenticate and enter the device code.</param>
        /// <returns>Authentication result containing a token for the requested scopes and for the user who has authenticated on another device with the code</returns>

        Task<AuthenticationResult> AcquireTokenWithDeviceCodeAsync(
            IEnumerable<string> scopes,
            Func<DeviceCodeResult, Task> deviceCodeResultCallback);

        /// <summary>
        /// Acquires a security token on a device without a Web browser, by letting the user authenticate on 
        /// another device, with possibility of passing extra parameters. This is done in two steps:
        /// <list type="bullet">
        /// <item><description>the method first acquires a device code from the authority and returns it to the caller via
        /// the <paramref name="deviceCodeResultCallback"/>. This callback takes care of interacting with the user
        /// to direct them to authenticate (to a specific URL, with a code)</description></item>
        /// <item><description>The method then proceeds to poll for the security
        /// token which is granted upon successful login by the user based on the device code information</description></item>
        /// </list>
        /// See https://aka.ms/msal-device-code-flow.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. 
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <param name="deviceCodeResultCallback">Callback containing information to show the user about how to authenticate and enter the device code.</param>
        /// <returns>Authentication result containing a token for the requested scopes and for the user who has authenticated on another device with the code</returns>

        Task<AuthenticationResult> AcquireTokenWithDeviceCodeAsync(
            IEnumerable<string> scopes,
            string extraQueryParameters,
            Func<DeviceCodeResult, Task> deviceCodeResultCallback);

        /// <summary>
        /// Acquires a security token on a device without a Web browser, by letting the user authenticate on 
        /// another device, with possibility of cancelling the token acquisition before it times out. This is done in two steps:
        /// <list type="bullet">
        /// <item><description>the method first acquires a device code from the authority and returns it to the caller via
        /// the <paramref name="deviceCodeResultCallback"/>. This callback takes care of interacting with the user
        /// to direct them to authenticate (to a specific URL, with a code)</description></item>
        /// <item><description>The method then proceeds to poll for the security
        /// token which is granted upon successful login by the user based on the device code information. This step is cancelable</description></item>
        /// </list>
        /// See https://aka.ms/msal-device-code-flow.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="deviceCodeResultCallback">The callback containing information to show the user about how to authenticate and enter the device code.</param>
        /// <param name="cancellationToken">A CancellationToken which can be triggered to cancel the operation in progress.</param>
        /// <returns>Authentication result containing a token for the requested scopes and for the user who has authenticated on another device with the code</returns>
        Task<AuthenticationResult> AcquireTokenWithDeviceCodeAsync(
            IEnumerable<string> scopes,
            Func<DeviceCodeResult, Task> deviceCodeResultCallback,
            CancellationToken cancellationToken);

        /// <summary>
        /// Acquires a security token on a device without a Web browser, by letting the user authenticate on 
        /// another device, with possibility of passing extra query parameters and cancelling the token acquisition before it times out. This is done in two steps:
        /// <list type="bullet">
        /// <item><description>the method first acquires a device code from the authority and returns it to the caller via
        /// the <paramref name="deviceCodeResultCallback"/>. This callback takes care of interacting with the user
        /// to direct them to authenticate (to a specific URL, with a code)</description></item>
        /// <item><description>The method then proceeds to poll for the security
        /// token which is granted upon successful login by the user based on the device code information. This step is cancelable</description></item>
        /// </list>
        /// See https://aka.ms/msal-device-code-flow.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. 
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <param name="deviceCodeResultCallback">The callback containing information to show the user about how to authenticate and enter the device code.</param>
        /// <param name="cancellationToken">A CancellationToken which can be triggered to cancel the operation in progress.</param>
        /// <returns>Authentication result containing a token for the requested scopes and for the user who has authenticated on another device with the code</returns>
        Task<AuthenticationResult> AcquireTokenWithDeviceCodeAsync(
            IEnumerable<string> scopes,
            string extraQueryParameters,
            Func<DeviceCodeResult, Task> deviceCodeResultCallback,
            CancellationToken cancellationToken);

        #endregion // DEVICE CODE

        #region INTEGRATED WINDOWS AUTH
#if !ANDROID_BUILDTIME && !iOS_BUILDTIME

#if !NET_CORE_BUILDTIME

        /// <summary>
        /// Non-interactive request to acquire a security token for the signed-in user in Windows, via Integrated Windows Authentication.
        /// See https://aka.ms/msal-net-iwa.
        /// The account used in this overrides is pulled from the operating system as the current user principal name
        /// </summary>
        /// <remarks>
        /// On Windows Universal Platform, the following capabilities need to be provided:
        /// Enterprise Authentication, Private Networks (Client and Server), User Account Information
        /// </remarks>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <returns>Authentication result containing a token for the requested scopes and for the currently logged-in user in Windows</returns>
        Task<AuthenticationResult> AcquireTokenByIntegratedWindowsAuthAsync(IEnumerable<string> scopes);
#endif

        /// <summary>
        /// Non-interactive request to acquire a security token for the signed-in user in Windows, via Integrated Windows Authentication.
        /// See https://aka.ms/msal-net-iwa.
        /// The account used in this overrides is pulled from the operating system as the current user principal name
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="username">Identifier of the user account for which to acquire a token with Integrated Windows authentication. 
        /// Generally in UserPrincipalName (UPN) format, e.g. john.doe@contoso.com</param>
        /// <returns>Authentication result containing a token for the requested scopes and for the currently logged-in user in Windows</returns>
        Task<AuthenticationResult> AcquireTokenByIntegratedWindowsAuthAsync(
            IEnumerable<string> scopes,
            string username);
#endif

        #endregion // INTEGRATED WINDOWS AUTH

        #region USERNAME PASSWORD

        /// <summary>
        /// Non-interactive request to acquire a security token from the authority, via Username/Password Authentication.
        /// See https://aka.ms/msal-net-up.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="username">Identifier of the user application requests token on behalf.
        /// Generally in UserPrincipalName (UPN) format, e.g. john.doe@contoso.com</param>
        /// <param name="password">User password.</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        Task<AuthenticationResult> AcquireTokenByUsernamePasswordAsync(
            IEnumerable<string> scopes,
            string username,
            string password);

        #endregion // USERNAME PASSWORD

    }
}