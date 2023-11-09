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
    /// Component used to acquire tokens in desktop and mobile applications. Public client applications are not trusted to safely keep application secrets and therefore they can only access web APIs in the name of the authenticating user.
    /// For more details on differences between public and confidential clients, refer to <see href="https://aka.ms/msal-net-client-applications">our documentation</see>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Unlike <see cref="Microsoft.Identity.Client.IConfidentialClientApplication"/>, public clients are unable to securely store secrets on a client device and as a result do not require the use of a client secret.
    /// </para>
    /// <para>
    /// The redirect URI needed for interactive authentication is automatically determined by the library. It does not need to be passed explicitly in the constructor. Depending
    /// on the authentication strategy (e.g., through the <see href="https://learn.microsoft.com/entra/msal/dotnet/acquiring-tokens/desktop-mobile/wam?view=msal-dotnet-latest">Web Account Manager</see>, the Authenticator app, web browser, etc.), different redirect URIs will be used by MSAL. Redirect URIs must always be configured for the application in the Azure Portal.
    /// </para>
    /// </remarks>
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
        /// Interactive request to acquire a token for the specified scopes. Either a system browser, an embedded browser or a broker will 
        /// handle this request, depending on the version of .NET framework used and on configuration. 
        /// For Microsoft Entra applications, a broker is recommended. See https://aka.ms/msal-net-wam
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API.</param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request.</returns>
        /// <remarks>The user will be signed-in interactively and will consent to scopes, as well as perform a multi-factor authentication step if such a policy was enabled in the Azure AD tenant.
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
        /// </remarks>
        AcquireTokenWithDeviceCodeParameterBuilder AcquireTokenWithDeviceCode(
            IEnumerable<string> scopes,
            Func<DeviceCodeResult, Task> deviceCodeResultCallback);

        /// <summary>
        /// This API is no longer recommended and will be deprecated in future versions in favor of similar functionality via the Windows broker (WAM).
        /// See https://aka.ms/msal-net-wam
        /// WAM does not require any setup for desktop apps to login with the Windows account.
        /// 
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
        /// </remarks>
        AcquireTokenByUsernamePasswordParameterBuilder AcquireTokenByUsernamePassword(
            IEnumerable<string> scopes,
            string username,
            string password);
    }
}
