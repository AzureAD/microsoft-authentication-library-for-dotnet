// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Interface to be used with desktop or mobile applications (Desktop / UWP / Xamarin.iOS / Xamarin.Android).
    /// public client applications are not trusted to safely keep application secrets, and therefore they only access Web APIs in the name of the user only.
    /// For details see https://aka.ms/msal-net-client-applications.
    /// </summary>
    public partial interface IPublicClientApplication : IClientApplicationBase
    {
        /// <summary>
        /// Tells if the application can use the system web browser, therefore getting single-sign-on with web applications.
        /// By default, MSAL will try to use a system browser on the mobile platforms, if it is available.
        /// See https://aka.ms/msal-net-uses-web-browser.
        /// </summary>
        bool IsSystemWebViewAvailable { get; }

        /// <summary>
        /// Interactive request to acquire a token for the specified scopes. The interactive window will be parented to the specified
        /// window. The user will be required to select an account
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request</returns>
        /// <remarks>The user will be signed-in interactively if needed,
        /// and will consent to scopes and do multi-factor authentication if such a policy was enabled in the Azure AD tenant.
        ///
        /// You can also pass optional parameters by calling:
        /// <see cref="AcquireTokenInteractiveParameterBuilder.WithPrompt(Prompt)"/> to specify the user experience
        /// when signing-in, <see cref="AcquireTokenInteractiveParameterBuilder.WithUseEmbeddedWebView(bool)"/> to specify
        /// if you want to use the embedded web browser or the system default browser,
        /// <see cref="AcquireTokenInteractiveParameterBuilder.WithSystemWebViewOptions(SystemWebViewOptions)"/> to configure
        /// the user experience when using the Default browser,
        /// <see cref="AcquireTokenInteractiveParameterBuilder.WithAccount(IAccount)"/> or <see cref="AcquireTokenInteractiveParameterBuilder.WithLoginHint(string)"/>
        /// to prevent the select account dialog from appearing in the case you want to sign-in a specific account,
        /// <see cref="AcquireTokenInteractiveParameterBuilder.WithExtraScopesToConsent(IEnumerable{string})"/> if you want to let the
        /// user pre-consent to additional scopes (which won't be returned in the access token),
        /// <see cref="AbstractAcquireTokenParameterBuilder{T}.WithExtraQueryParameters(Dictionary{string, string})"/> to pass
        /// additional query parameters to the STS, and one of the overrides of <see cref="AbstractAcquireTokenParameterBuilder{T}.WithAuthority(string, bool)"/>
        /// in order to override the default authority set at the application construction. Note that the overriding authority needs to be part
        /// of the known authorities added to the application construction.
        /// </remarks>
        AcquireTokenInteractiveParameterBuilder AcquireTokenInteractive(IEnumerable<string> scopes);

        /// <summary>
        /// Acquires a security token on a device without a Web browser, by letting the user authenticate on
        /// another device. This is done in two steps:
        /// <list type="bullet">
        /// <item><description>The method first acquires a device code from the authority and returns it to the caller via
        /// the <paramref name="deviceCodeResultCallback"/>. This callback takes care of interacting with the user
        /// to direct them to authenticate (to a specific URL, with a code)</description></item>
        /// <item><description>The method then proceeds to poll for the security
        /// token which is granted upon successful login by the user based on the device code information</description></item>
        /// </list>
        /// See https://aka.ms/msal-device-code-flow.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="deviceCodeResultCallback">Callback containing information to show the user about how to authenticate and enter the device code.</param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request</returns>
        /// <remarks>
        /// You can also pass optional parameters by calling:
        /// <see cref="AbstractAcquireTokenParameterBuilder{T}.WithExtraQueryParameters(Dictionary{string, string})"/> to pass
        /// additional query parameters to the STS, and one of the overrides of <see cref="AbstractAcquireTokenParameterBuilder{T}.WithAuthority(string, bool)"/>
        /// in order to override the default authority set at the application construction. Note that the overriding authority needs to be part
        /// of the known authorities added to the application construction.
        /// </remarks>
        AcquireTokenWithDeviceCodeParameterBuilder AcquireTokenWithDeviceCode(
            IEnumerable<string> scopes,
            Func<DeviceCodeResult, Task> deviceCodeResultCallback);

        /// <summary>
        /// Non-interactive request to acquire a security token for the signed-in user in Windows,
        /// via Integrated Windows Authentication. See https://aka.ms/msal-net-iwa.
        /// The account used in this overrides is pulled from the operating system as the current user principal name.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request</returns>
        /// <remarks>
        /// You can also pass optional parameters by calling:
        /// <see cref="AcquireTokenByIntegratedWindowsAuthParameterBuilder.WithUsername(string)"/> to pass the identifier
        /// of the user account for which to acquire a token with Integrated Windows authentication. This is generally in
        /// UserPrincipalName (UPN) format, e.g. john.doe@contoso.com. This is normally not needed, but some Windows administrators
        /// set policies preventing applications from looking-up the signed-in user in Windows, and in that case the username
        /// needs to be passed.
        /// You can also chain with
        /// <see cref="AbstractAcquireTokenParameterBuilder{T}.WithExtraQueryParameters(Dictionary{string, string})"/> to pass
        /// additional query parameters to the STS, and one of the overrides of <see cref="AbstractAcquireTokenParameterBuilder{T}.WithAuthority(string, bool)"/>
        /// in order to override the default authority set at the application construction. Note that the overriding authority needs to be part
        /// of the known authorities added to the application construction.
        /// </remarks>
        AcquireTokenByIntegratedWindowsAuthParameterBuilder AcquireTokenByIntegratedWindowsAuth(
            IEnumerable<string> scopes);

        /// <summary>
        /// Non-interactive request to acquire a security token from the authority, via Username/Password Authentication.
        /// Available only on .net desktop and .net core. See https://aka.ms/msal-net-up for details.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="username">Identifier of the user application requests token on behalf.
        /// Generally in UserPrincipalName (UPN) format, e.g. <c>john.doe@contoso.com</c></param>
        /// <param name="password">User password as a secure string.</param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request</returns>
        /// <remarks>You can also pass optional parameters by chaining the builder with:
        /// <see cref="AbstractAcquireTokenParameterBuilder{T}.WithExtraQueryParameters(Dictionary{string, string})"/> to pass
        /// additional query parameters to the STS, and one of the overrides of <see cref="AbstractAcquireTokenParameterBuilder{T}.WithAuthority(string, bool)"/>
        /// in order to override the default authority set at the application construction. Note that the overriding authority needs to be part
        /// of the known authorities added to the application construction.
        /// </remarks>
        AcquireTokenByUsernamePasswordParameterBuilder AcquireTokenByUsernamePassword(
            IEnumerable<string> scopes,
            string username,
            SecureString password);
    }
}
