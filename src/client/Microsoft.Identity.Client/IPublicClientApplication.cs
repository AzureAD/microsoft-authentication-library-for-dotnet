// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Component to be used with public client applications like desktop or mobile applications.
    /// </summary>
    public partial interface IPublicClientApplication : IClientApplicationBase
    {
        /// <summary>
        /// Tells if the application can use the system web browser, therefore enabling single-sign-on with web applications.
        /// By default, MSAL will try to use a system browser on the mobile platforms, if it is available.
        /// See <see href="https://aka.ms/msal-net-uses-web-browser">our documentation</see> for more details.
        /// </summary>
        /// <remarks>
        /// On Windows, macOS, and Linux a system browser can always be used, except in cases where there is no UI (e.g., a SSH session).
        /// On Android, the browser must support tabs.
        /// </remarks>
        /// <returns>Returns <c>true</c> if MSAL can use the system web browser.</returns>
        bool IsSystemWebViewAvailable { get; }

        /// <summary>
        /// Interactive request to acquire a token for the specified scopes. The interactive window will be parented to an application
        /// window specified through a handle. The user will be required to select an account.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API.</param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request.</returns>
        /// <remarks>The user will be signed-in interactively and will consent to scopes, as well as perform a multi-factor authentication step if such a policy was enabled in the Azure AD tenant.
        ///
        /// You can also pass optional parameters by calling:
        /// <list type="bullet">         
        /// <item><description><see cref="AcquireTokenInteractiveParameterBuilder.WithPrompt(Prompt)"/> to specify the user experience when signing-in.</description></item>
        /// <item><description><see cref="AcquireTokenInteractiveParameterBuilder.WithUseEmbeddedWebView(bool)"/> to specify if you want to use the embedded web browser or the default system browser.</description></item>
        /// <item><description><see cref="AcquireTokenInteractiveParameterBuilder.WithSystemWebViewOptions(SystemWebViewOptions)"/> to configure the user experience when using the default system browser.</description></item>
        /// <item><description><see cref="AcquireTokenInteractiveParameterBuilder.WithAccount(IAccount)"/> or <see cref="AcquireTokenInteractiveParameterBuilder.WithLoginHint(string)"/> to prevent the account selection dialog from appearing if you want to sign-in a specific account.</description></item>
        /// <item><description><see cref="AcquireTokenInteractiveParameterBuilder.WithExtraScopesToConsent(IEnumerable{string})"/> if you want to let the user pre-consent to additional scopes (which won't be returned in the access token).</description></item>
        /// <item><description><see cref="AbstractAcquireTokenParameterBuilder{T}.WithExtraQueryParameters(Dictionary{string, string})"/> to pass additional query parameters to the authentication service.</description></item>
        /// <item><description>One of the overrides of <see cref="AbstractAcquireTokenParameterBuilder{T}.WithAuthority(string, bool)"/> to override the default authority set at the application construction. Note that the overriding authority needs to be part of the known authorities added to the application constructor.</description></item>
        /// </list>
        /// </remarks>
        AcquireTokenInteractiveParameterBuilder AcquireTokenInteractive(IEnumerable<string> scopes);

        /// <summary>
        /// Acquires a security token on a device without a web browser, by letting the user authenticate on
        /// another device.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="deviceCodeResultCallback">Callback containing information to show the user about how to authenticate and enter the device code.</param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request</returns>
        /// <remarks>
        /// The token acquisition is done in two steps:
        /// <list type="bullet">
        /// <item><description>The method first acquires a device code from the authority and returns it to the caller via
        /// the <paramref name="deviceCodeResultCallback"/>. This callback takes care of interacting with the user
        /// to direct them to authenticate (i.e., to a specific URL, with a code)</description></item>
        /// <item><description>The method then proceeds to poll for the security
        /// token which is granted upon successful login by the user based on the device code information.</description></item>
        /// </list>
        /// See <see href="https://aka.ms/msal-device-code-flow">our documentation</see> for additional context.
        /// You can also pass optional parameters by calling <see cref="AbstractAcquireTokenParameterBuilder{T}.WithExtraQueryParameters(Dictionary{string, string})"/>
        /// and one of the overrides of <see cref="AbstractAcquireTokenParameterBuilder{T}.WithAuthority(string, bool)"/>
        /// in order to override the default authority. Note that the overriding authority needs to be part
        /// of the known authorities added to the application constructor.
        /// </remarks>
        AcquireTokenWithDeviceCodeParameterBuilder AcquireTokenWithDeviceCode(
            IEnumerable<string> scopes,
            Func<DeviceCodeResult, Task> deviceCodeResultCallback);

        /// <summary>
        /// Non-interactive request to acquire a security token for the signed-in user in Windows,
        /// via Integrated Windows Authentication.
        /// The account used in this overrides is pulled from the operating system as the current user principal name.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request</returns>
        /// <remarks>
        /// See <see href="https://aka.ms/msal-net-iwa">our documentation</see> for more details.
        /// You can pass optional parameters by calling <see cref="AcquireTokenByIntegratedWindowsAuthParameterBuilder.WithUsername(string)"/> to pass the identifier
        /// of the user account for which to acquire a token with Integrated Windows Authentication. This is generally in
        /// User Principal Name (UPN) format (e.g. john.doe@contoso.com). This is normally not needed, but some Windows administrators
        /// set policies preventing applications from looking up the signed-in user and in that case the username needs to be passed.
        /// You can also chain with <see cref="AbstractAcquireTokenParameterBuilder{T}.WithExtraQueryParameters(Dictionary{string, string})"/> to pass
        /// additional query parameters to the authentication service, along with one of the overrides of <see cref="AbstractAcquireTokenParameterBuilder{T}.WithAuthority(string, bool)"/>
        /// in order to override the default authority. Note that the overriding authority needs to be part
        /// of the known authorities added to the application constructor.
        /// </remarks>
        AcquireTokenByIntegratedWindowsAuthParameterBuilder AcquireTokenByIntegratedWindowsAuth(
            IEnumerable<string> scopes);

        /// <summary>
        /// Non-interactive request to acquire a token via username and password authentication.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="username">Identifier of the user application requests token on behalf.
        /// Generally in UserPrincipalName (UPN) format, e.g. <c>john.doe@contoso.com</c></param>
        /// <param name="password">User password as a secure string.</param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request</returns>
        /// <remarks>
        /// Available only for .NET Framework and .NET Core applications. See <see href="https://aka.ms/msal-net-up">our documentation</see> for details.
        /// You can also pass optional parameters by chaining the builder with <see cref="AbstractAcquireTokenParameterBuilder{T}.WithExtraQueryParameters(Dictionary{string, string})"/>
        /// and one of the overrides of <see cref="AbstractAcquireTokenParameterBuilder{T}.WithAuthority(string, bool)"/>
        /// to override the default authority. Note that the overriding authority needs to be part
        /// of the known authorities added to the application constructor.
        /// .NET no longer recommends using SecureString and MSAL puts the plaintext value of the password on the wire, as required by the OAuth protocol. See <see href="https://docs.microsoft.com/dotnet/api/system.security.securestring?view=net-6.0#remarks">SecureString documentation</see> for details.
        /// </remarks>
        [Obsolete("Using SecureString is not recommended. Use AcquireTokenByUsernamePassword(IEnumerable<string> scopes, string username, string password) instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        AcquireTokenByUsernamePasswordParameterBuilder AcquireTokenByUsernamePassword(
            IEnumerable<string> scopes,
            string username,
            SecureString password);

        /// <summary>
        /// Non-interactive request to acquire a token via username and password authentication.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="username">Identifier of the user application requests token on behalf.
        /// Generally in UserPrincipalName (UPN) format, e.g. <c>john.doe@contoso.com</c></param>
        /// <param name="password">User password as a string.</param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request</returns>
        /// <remarks>
        /// Available only for .NET Framework and .NET Core applications. See <see href="https://aka.ms/msal-net-up">our documentation</see> for details.
        /// You can also pass optional parameters by chaining the builder with <see cref="AbstractAcquireTokenParameterBuilder{T}.WithExtraQueryParameters(Dictionary{string, string})"/>
        /// and one of the overrides of <see cref="AbstractAcquireTokenParameterBuilder{T}.WithAuthority(string, bool)"/>
        /// to override the default authority. Note that the overriding authority needs to be part
        /// of the known authorities added to the application constructor.
        /// </remarks>
        AcquireTokenByUsernamePasswordParameterBuilder AcquireTokenByUsernamePassword(
            IEnumerable<string> scopes,
            string username,
            string password);
    }
}
