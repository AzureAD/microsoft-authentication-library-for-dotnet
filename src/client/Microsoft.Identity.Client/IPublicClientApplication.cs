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
    /// Represents public client applications - desktop and mobile applications.
    /// </summary>
    /// <remarks>
    /// Public client applications are not trusted to safely keep application secrets and therefore they can only access web APIs in the name of the authenticating user.
    /// See <see href="https://aka.ms/msal-net-client-applications">Client Applications</see>.
    /// <para>
    /// Unlike <see cref="Microsoft.Identity.Client.IConfidentialClientApplication"/>, public clients are unable to securely store secrets on a client device and as a result do not require the use of a client secret.
    /// </para>
    /// <para>
    /// The redirect URI needed for interactive authentication is automatically determined by the library. It does not need to be passed explicitly in the constructor. Depending
    /// on the authentication strategy (e.g., through the <see href="https://aka.ms/msal-net-wam">Web Account Manager</see>, the Authenticator app, web browser, etc.), different redirect URIs will be used by MSAL. Redirect URIs must always be configured for the application in the Azure Portal.
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
        /// Acquires a token interactively for the specified scopes. Either a system browser, an embedded browser, or a broker will 
        /// handle this request, depending on the version of .NET framework used and on configuration. 
        /// For Microsoft Entra applications, a broker is recommended. See <see href="https://aka.ms/msal-net-wam">Windows Broker</see>.
        /// This method does not look in the token cache, but stores the result in it. Before calling this method, use other methods 
        /// such as <see cref="IClientApplicationBase.AcquireTokenSilent(IEnumerable{string}, IAccount)"/> to check the token cache.
        /// See <see href="https://aka.ms/adal-to-msal-net/interactive">Interactive Authentication</see>.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API.</param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request.</returns>
        /// <remarks>The user will be signed-in interactively and will consent to scopes, as well as perform a multi-factor authentication step if such a policy was enabled in the Azure AD tenant.
        /// </remarks>
        AcquireTokenInteractiveParameterBuilder AcquireTokenInteractive(IEnumerable<string> scopes);

        /// <summary>
        /// Acquires a token on a device without a web browser by letting the user authenticate on
        /// another device.
        /// This method does not look in the token cache, but stores the result in it. Before calling this method, use other methods 
        /// such as <see cref="IClientApplicationBase.AcquireTokenSilent(IEnumerable{string}, IAccount)"/> to check the token cache.
        /// </summary>
        /// <remarks>
        /// The token acquisition is done in two steps:
        /// <list type="bullet">
        /// <item><description>The method first acquires a device code from the authority and returns it to the caller via
        /// the <paramref name="deviceCodeResultCallback"/>. This callback takes care of interacting with the user
        /// to direct them to authenticate (i.e., to a specific URL, with a code).</description></item>
        /// <item><description>The method then proceeds to poll for the security
        /// token which is granted upon successful login by the user based on the device code information.</description></item>
        /// </list>
        /// See <see href="https://aka.ms/msal-device-code-flow">Device Code Flow</see>.
        /// </remarks>
        /// <param name="scopes">Scopes requested to access a protected API.</param>
        /// <param name="deviceCodeResultCallback">Callback containing information to show the user about how to authenticate and enter the device code.</param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request.</returns>
        AcquireTokenWithDeviceCodeParameterBuilder AcquireTokenWithDeviceCode(
            IEnumerable<string> scopes,
            Func<DeviceCodeResult, Task> deviceCodeResultCallback);

        /// <summary>
        /// <para>
        /// This API is no longer recommended and will be deprecated in future versions in favor of 
        /// similar functionality via <see href="https://aka.ms/msal-net-wam">the Windows broker (WAM)</see>.
        /// WAM does not require any setup for desktop apps to login with the Windows account.
        /// </para>
        /// <para>
        /// Acquires a token non-interactively for the signed-in user in Windows
        /// via Integrated Windows Authentication.
        /// The account used in this overrides is pulled from the operating system as the current user principal name.
        /// This method does not look in the token cache, but stores the result in it. Before calling this method, use other methods 
        /// such as <see cref="IClientApplicationBase.AcquireTokenSilent(IEnumerable{string}, IAccount)"/> to check the token cache.
        /// </para>
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API.</param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request.</returns>
        /// <remarks>
        /// See <see href="https://aka.ms/msal-net-iwa">our documentation</see> for more details.
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
        /// Acquires a token without user interaction using username and password authentication.
        /// This method does not look in the token cache, but stores the result in it. Before calling this method, use other methods 
        /// such as <see cref="IClientApplicationBase.AcquireTokenSilent(IEnumerable{string}, IAccount)"/> to check the token cache.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API.</param>
        /// <param name="username">Identifier of the user application requests token on behalf.
        /// Generally in UserPrincipalName (UPN) format, e.g. <c>john.doe@contoso.com</c></param>
        /// <param name="password">User password as a string.</param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request.</returns>
        /// <remarks>
        /// Available only for .NET Framework and .NET Core applications. See <see href="https://aka.ms/msal-net-up">our documentation</see> for details.
        /// </remarks>
        AcquireTokenByUsernamePasswordParameterBuilder AcquireTokenByUsernamePassword(
            IEnumerable<string> scopes,
            string username,
            string password);
    }
}
