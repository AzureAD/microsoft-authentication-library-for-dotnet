// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Executors;
using Microsoft.Identity.Client.WsTrust;

namespace Microsoft.Identity.Client
{
#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved
    /// <summary>
    /// Class to be used to acquire tokens in desktop or mobile applications (Desktop / UWP / Xamarin.iOS / Xamarin.Android).
    /// public client applications are not trusted to safely keep application secrets, and therefore they only access web APIs in the name of the user only.
    /// For details see https://aka.ms/msal-net-client-applications
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Contrary to <see cref="Microsoft.Identity.Client.ConfidentialClientApplication"/>, public clients are unable to hold configuration time secrets,
    /// and as a result have no client secret</description></item>
    /// <item><description>The redirect URL is proposed by the library. It does not need to be passed in the constructor</description></item>
    /// </list>
    /// </remarks>
    public sealed partial class PublicClientApplication : ClientApplicationBase, IPublicClientApplication, IByRefreshToken
    {
        internal PublicClientApplication(ApplicationConfiguration configuration)
            : base(configuration)
        {
        }

        private const string CurrentOSAccountDescriptor = "current_os_account";
        private static IAccount s_currentOsAccount =
            new Account(CurrentOSAccountDescriptor, null, null, null);

        /// <summary>
        /// Currently only the Windows broker is able to login with the current user, see https://aka.ms/msal-net-wam for details.
        /// 
        /// A special account value that indicates that the current Operating System account should be used 
        /// to login the user. Not all operating systems and authentication flows support this concept, in which 
        /// case calling `AcquireTokenSilent` will throw an <see cref="MsalUiRequiredException"/>. 
        /// </summary>
        public static IAccount OperatingSystemAccount
        {
            get
            {
                return s_currentOsAccount;
            }
        }

        internal static bool IsOperatingSystemAccount(IAccount account)
        {
            return string.Equals(account?.HomeAccountId?.Identifier, CurrentOSAccountDescriptor, StringComparison.Ordinal);
        }

        /// <summary>
        /// Returns true if MSAL can use a system browser.
        /// </summary>
        /// <remarks>
        /// On Windows, Mac and Linux a system browser can always be used, except in cases where there is no UI, e.g. SSH connection.
        /// On Android, the browser must support tabs.
        /// </remarks>
        public bool IsSystemWebViewAvailable // TODO MSAL5: consolidate these helpers in the interface
        {
            get
            {
                return ServiceBundle.PlatformProxy.GetWebUiFactory(ServiceBundle.Config).IsSystemWebViewAvailable;
            }
        }

        /// <summary>
        /// Returns true if MSAL can use an embedded web view (browser). 
        /// </summary>
        /// <remarks>
        /// Currently there are no embedded web views on Mac and Linux. On Windows, app developers or users should install 
        /// the WebView2 runtime and this property will inform if the runtime is available, see https://aka.ms/msal-net-webview2
        /// </remarks>
        public bool IsEmbeddedWebViewAvailable()
        {
            return ServiceBundle.PlatformProxy.GetWebUiFactory(ServiceBundle.Config).IsEmbeddedWebViewAvailable;
        }

        /// <summary>
        /// Returns false when the program runs in headless OS, for example when SSH-ed into a Linux machine.
        /// Browsers (web views) and brokers cannot be used if there is no UI support. Instead, please use <see cref="PublicClientApplication.AcquireTokenWithDeviceCode(IEnumerable{string}, Func{DeviceCodeResult, Task})"/>
        /// </summary>
        public bool IsUserInteractive()
        {
            return ServiceBundle.PlatformProxy.GetWebUiFactory(ServiceBundle.Config).IsUserInteractive;
        }

        /// <summary>
        /// Returns <c>true</c> if a broker can be used.
        /// This method is only needed to be used in mobile scenarios which support Mobile Application Management. In other supported scenarios, use <c>WithBroker</c> by itself, which will fall back to use a browser if broker is unavailable.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item><description>On Windows, the broker (WAM) can be used on Windows 10 and is always installed. See https://aka.ms/msal-net-wam </description></item>
        /// <item><description>On Mac, Linux, and older versions of Windows a broker is not available.</description></item>
        /// <item><description>In .NET 5 apps, target <c>net5.0-windows10.0.17763.0</c> for all Windows versions and target <c>net5.0</c> for Linux and Mac.</description></item>
        /// <item><description>In .NET classic or .NET Core 3.1 apps, install Microsoft.Identity.Client.Desktop first and call <c>WithDesktopFeatures()</c>.</description></item>
        /// <item><description>In mobile apps, the device must be Intune joined and Authenticator or Company Portal must be installed. See https://aka.ms/msal-brokers </description></item>
        /// </list>
        /// </remarks>
        public bool IsBrokerAvailable()
        {
            return ServiceBundle.PlatformProxy.CreateBroker(ServiceBundle.Config, null)
                    .IsBrokerInstalledAndInvokable(ServiceBundle.Config.Authority?.AuthorityInfo?.AuthorityType ?? AuthorityType.Aad);
        }

        /// <summary>
        /// Interactive request to acquire a token for the specified scopes. The interactive window will be parented to the specified
        /// window. The user will be required to select an account.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request</returns>
        /// <remarks>The user will be signed-in interactively if needed,
        /// and will consent to scopes and do multi-factor authentication if such a policy was enabled in the Azure AD tenant.
        ///
        /// You can also pass optional parameters by calling:
        ///         
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
        [CLSCompliant(false)]
        public AcquireTokenInteractiveParameterBuilder AcquireTokenInteractive(
            IEnumerable<string> scopes)
        {
            return AcquireTokenInteractiveParameterBuilder
                .Create(ClientExecutorFactory.CreatePublicClientExecutor(this), scopes)
                .WithParentActivityOrWindowFunc(ServiceBundle.Config.ParentActivityOrWindowFunc);
        }
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved

        /// <summary>
        /// Acquires a security token on a device without a web browser, by letting the user authenticate on
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
        public AcquireTokenWithDeviceCodeParameterBuilder AcquireTokenWithDeviceCode(
            IEnumerable<string> scopes,
            Func<DeviceCodeResult, Task> deviceCodeResultCallback)
        {
            return AcquireTokenWithDeviceCodeParameterBuilder.Create(
                ClientExecutorFactory.CreatePublicClientExecutor(this),
                scopes,
                deviceCodeResultCallback);
        }

        AcquireTokenByRefreshTokenParameterBuilder IByRefreshToken.AcquireTokenByRefreshToken(
            IEnumerable<string> scopes,
            string refreshToken)
        {
            return AcquireTokenByRefreshTokenParameterBuilder.Create(
                ClientExecutorFactory.CreateClientApplicationBaseExecutor(this),
                scopes,
                refreshToken);
        }

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
        public AcquireTokenByIntegratedWindowsAuthParameterBuilder AcquireTokenByIntegratedWindowsAuth(
            IEnumerable<string> scopes)
        {
            return AcquireTokenByIntegratedWindowsAuthParameterBuilder.Create(
                ClientExecutorFactory.CreatePublicClientExecutor(this),
                scopes);
        }

        /// <summary>
        /// Non-interactive request to acquire a security token from the authority, via Username/Password Authentication.
        /// See https://aka.ms/msal-net-up for details.
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
        /// .NET no longer recommends using SecureString and MSAL puts the plaintext value of the password on the wire, as required by the OAuth protocol. See <see href="https://docs.microsoft.com/en-us/dotnet/api/system.security.securestring?view=net-6.0#remarks">SecureString documentation</see>.
        /// </remarks>
        [Obsolete("Using SecureString is not recommended. Use AcquireTokenByUsernamePassword(IEnumerable<string> scopes, string username, string password) instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public AcquireTokenByUsernamePasswordParameterBuilder AcquireTokenByUsernamePassword(
            IEnumerable<string> scopes,
            string username,
            SecureString password)
        {
            return AcquireTokenByUsernamePasswordParameterBuilder.Create(
                ClientExecutorFactory.CreatePublicClientExecutor(this),
                scopes,
                username,
                new string(password.PasswordToCharArray()));
        }

        /// <summary>
        /// Non-interactive request to acquire a security token from the authority, via Username/Password Authentication.
        /// See https://aka.ms/msal-net-up for details.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="username">Identifier of the user application requests token on behalf.
        /// Generally in UserPrincipalName (UPN) format, e.g. <c>john.doe@contoso.com</c></param>
        /// <param name="password">User password as a string.</param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request</returns>
        /// <remarks>You can also pass optional parameters by chaining the builder with:
        /// <see cref="AbstractAcquireTokenParameterBuilder{T}.WithExtraQueryParameters(Dictionary{string, string})"/> to pass
        /// additional query parameters to the Azure AD, and one of the overrides of <see cref="AbstractAcquireTokenParameterBuilder{T}.WithAuthority(string, bool)"/>
        /// in order to override the default authority set at the application construction. Note that the overriding authority needs to be part
        /// of the known authorities added to the application construction.
        /// </remarks>
        public AcquireTokenByUsernamePasswordParameterBuilder AcquireTokenByUsernamePassword(
            IEnumerable<string> scopes,
            string username,
            string password)
        {
            return AcquireTokenByUsernamePasswordParameterBuilder.Create(
                ClientExecutorFactory.CreatePublicClientExecutor(this),
                scopes,
                username,
                password);
        }

        /// <summary>
        /// Used to determine if the currently available broker is able to perform Proof-of-Possession.
        /// </summary>
        /// <returns>Boolean indicating if Proof-of-Possession is supported</returns>
        public bool IsProofOfPossessionSupportedByClient()
        {
            if (ServiceBundle.Config.IsBrokerEnabled)
            {
                var broker = ServiceBundle.PlatformProxy.CreateBroker(ServiceBundle.Config, null);

                if (broker.IsBrokerInstalledAndInvokable(ServiceBundle.Config.Authority.AuthorityInfo.AuthorityType))
                {
                    return broker.IsPopSupported;
                }
            }

            return false;
        }
    }
}
