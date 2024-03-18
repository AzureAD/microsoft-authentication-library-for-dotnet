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
    /// <inheritdoc cref="IPublicClientApplication"/>
    public sealed partial class PublicClientApplication : ClientApplicationBase, IPublicClientApplication, IByRefreshToken
    {
        internal PublicClientApplication(ApplicationConfiguration configuration)
            : base(configuration)
        {
        }

        private const string CurrentOSAccountDescriptor = "current_os_account";
        private static readonly IAccount s_currentOsAccount =
            new Account(CurrentOSAccountDescriptor, null, null, null);

        /// <summary>
        /// A special account value that indicates that the current operating system account should be used 
        /// to log the user in. Not all operating systems and authentication flows support this concept, in which 
        /// case calling <see cref="IClientApplicationBase.AcquireTokenSilent(IEnumerable{string}, IAccount)"/> will throw an <see cref="MsalUiRequiredException"/>. 
        /// </summary>
        /// <remarks>
        /// Currently only the Windows broker is able to login with the current operating system user. For additional details, see <see href="https://aka.ms/msal-net-wam">the documentation on the Windows broker</see>.
        /// </remarks>
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

        /// <inheritdoc/>
        public bool IsSystemWebViewAvailable // TODO MSAL5: consolidate these helpers in the interface
        {
            get
            {
                return ServiceBundle.PlatformProxy.GetWebUiFactory(ServiceBundle.Config).IsSystemWebViewAvailable;
            }
        }

        /// <summary>
        /// Returns <c>true</c> if MSAL can use an embedded web view (web browser).
        /// </summary>
        /// <remarks>
        /// All .NET Framework applications will use the legacy web view. .NET 6 and .NET Core applications must use the <see href="https://www.nuget.org/packages/Microsoft.Identity.Client.Desktop">Microsoft.Identity.Client.Desktop</see> package with WebView2. .NET 6 for Windows comes with WebView2 by default.
        /// WebView2 UI is only shown for non-AAD authorities.
        /// Refer to <see href="https://aka.ms/msal-net-webview2">our documentation</see> for additional details.
        /// </remarks>
        public bool IsEmbeddedWebViewAvailable()
        {
            return ServiceBundle.PlatformProxy.GetWebUiFactory(ServiceBundle.Config).IsEmbeddedWebViewAvailable;
        }

        /// <summary>
        /// Returns <c>false</c> when the application runs in headless mode (e.g., when SSH-d into a Linux machine).
        /// Browsers (web views) and brokers cannot be used if there is no UI support. For those scenarios, use <see cref="PublicClientApplication.AcquireTokenWithDeviceCode(IEnumerable{string}, Func{DeviceCodeResult, Task})"/>.
        /// </summary>
        public bool IsUserInteractive()
        {
            return ServiceBundle.PlatformProxy.GetWebUiFactory(ServiceBundle.Config).IsUserInteractive;
        }

        /// <summary>
        /// Returns <c>true</c> if an authentication broker can be used.
        /// This method is only needed for mobile scenarios which support Mobile Application Management (MAM). In other cases, use <c>WithBroker</c>, which will fall back to use a browser if an authentication broker is unavailable.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item><description>On Windows, the broker (WAM) can be used on Windows 10 and is always installed. See https://aka.ms/msal-net-wam </description></item>
        /// <item><description>On Mac, Linux, and older versions of Windows a broker is not available.</description></item>
        /// <item><description>In .NET 6 apps, target <c>net6.0-windows10.0.17763.0</c> for all Windows versions and target <c>net6.0</c> for Linux and Mac.</description></item>
        /// <item><description>In .NET classic or .NET Core 3.1 apps, install Microsoft.Identity.Client.Desktop first and call <c>WithDesktopFeatures()</c>.</description></item>
        /// <item><description>In mobile apps, the device must be Intune joined and Authenticator or Company Portal must be installed. See https://aka.ms/msal-brokers </description></item>
        /// </list>
        /// </remarks>
#if ANDROID || iOS || WINDOWS_APP
        [Obsolete("This method is obsolete. Applications should rely on the library automatically falling back to a browser if the broker is not available. ", false)]
#endif
        public bool IsBrokerAvailable()
        {
            return ServiceBundle.PlatformProxy.CreateBroker(ServiceBundle.Config, null)
                    .IsBrokerInstalledAndInvokable(ServiceBundle.Config.Authority?.AuthorityInfo?.AuthorityType ?? AuthorityType.Aad);
        }

        /// <inheritdoc/>
        [CLSCompliant(false)]
        public AcquireTokenInteractiveParameterBuilder AcquireTokenInteractive(
            IEnumerable<string> scopes)
        {
            return AcquireTokenInteractiveParameterBuilder
                .Create(ClientExecutorFactory.CreatePublicClientExecutor(this), scopes)
                .WithParentActivityOrWindowFunc(ServiceBundle.Config.ParentActivityOrWindowFunc);
        }

        /// <inheritdoc/>
        public AcquireTokenWithDeviceCodeParameterBuilder AcquireTokenWithDeviceCode(
            IEnumerable<string> scopes,
            Func<DeviceCodeResult, Task> deviceCodeResultCallback)
        {
            return AcquireTokenWithDeviceCodeParameterBuilder.Create(
                ClientExecutorFactory.CreatePublicClientExecutor(this),
                scopes,
                deviceCodeResultCallback);
        }

        /// <inheritdoc/>
        AcquireTokenByRefreshTokenParameterBuilder IByRefreshToken.AcquireTokenByRefreshToken(
            IEnumerable<string> scopes,
            string refreshToken)
        {
            return AcquireTokenByRefreshTokenParameterBuilder.Create(
                ClientExecutorFactory.CreateClientApplicationBaseExecutor(this),
                scopes,
                refreshToken);
        }

        /// <inheritdoc/>
        public AcquireTokenByIntegratedWindowsAuthParameterBuilder AcquireTokenByIntegratedWindowsAuth(
            IEnumerable<string> scopes)
        {
            return AcquireTokenByIntegratedWindowsAuthParameterBuilder.Create(
                ClientExecutorFactory.CreatePublicClientExecutor(this),
                scopes);
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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
