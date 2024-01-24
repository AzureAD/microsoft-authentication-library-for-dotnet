// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Kerberos;
using Microsoft.Identity.Client.PlatformsCommon.Factories;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using System.Runtime.CompilerServices;
using Microsoft.Identity.Client.AppConfig;

#if iOS
using UIKit;
#endif

#if ANDROID
using Android.App;
#endif

#if NETFRAMEWORK || NET6_WIN
using System.Windows.Forms;
#endif

#if MAC
using AppKit;
#endif

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// </summary>
    public sealed class PublicClientApplicationBuilder : AbstractApplicationBuilder<PublicClientApplicationBuilder>
    {
        /// <inheritdoc/>
        internal PublicClientApplicationBuilder(ApplicationConfiguration configuration)
            : base(configuration)
        {
        }

        /// <summary>
        /// Creates a PublicClientApplicationBuilder from public client application
        /// configuration options. See https://aka.ms/msal-net-application-configuration
        /// </summary>
        /// <param name="options">Public client applications configuration options</param>
        /// <returns>A <see cref="PublicClientApplicationBuilder"/> from which to set more
        /// parameters, and to create a public client application instance</returns>
        public static PublicClientApplicationBuilder CreateWithApplicationOptions(PublicClientApplicationOptions options)
        {
            var config = new ApplicationConfiguration(MsalClientType.PublicClient);
            return new PublicClientApplicationBuilder(config)
                .WithOptions(options)
                .WithKerberosTicketClaim(options.KerberosServicePrincipalName, options.TicketContainer);
        }

        /// <summary>
        /// Creates a PublicClientApplicationBuilder from a clientID.
        /// See https://aka.ms/msal-net-application-configuration
        /// </summary>
        /// <param name="clientId">Client ID (also known as App ID) of the application as registered in the
        /// application registration portal (https://aka.ms/msal-net-register-app)/.</param>
        /// <returns>A <see cref="PublicClientApplicationBuilder"/> from which to set more
        /// parameters, and to create a public client application instance</returns>
        public static PublicClientApplicationBuilder Create(string clientId)
        {
            var config = new ApplicationConfiguration(MsalClientType.PublicClient);
            return new PublicClientApplicationBuilder(config).WithClientId(clientId);
        }

        internal PublicClientApplicationBuilder WithUserTokenLegacyCachePersistenceForTest(ILegacyCachePersistence legacyCachePersistence)
        {
            Config.UserTokenLegacyCachePersistenceForTest = legacyCachePersistence;
            return this;
        }

        /// <summary>
        /// Configures the public client application to use the recommended reply URI for the platform. 
        /// See https://aka.ms/msal-net-default-reply-uri.
        /// <list type="table">
        /// <listheader>
        /// <term>Platform</term>
        /// <Description>Default Reply URI</Description>
        /// </listheader>
        /// <item>
        /// <term>.NET desktop</term>
        /// <Description><c>https://login.microsoftonline.com/common/oauth2/nativeclient</c></Description>
        /// </item>
        /// <item>
        /// <term>UWP</term>
        /// <Description>value of <c>WebAuthenticationBroker.GetCurrentApplicationCallbackUri()</c></Description>
        /// </item>
        /// <item>
        /// <term>For system browser on .NET Core</term>
        /// <Description><c>http://localhost</c></Description>
        /// </item>
        /// </list>
        /// NOTE:There will be an update to the default redirect URI in the future to accommodate for system browsers on the 
        /// .NET desktop and .NET Core platforms.
        /// </summary>
        /// <returns>A <see cref="PublicClientApplicationBuilder"/> from which to set more
        /// parameters, and to create a public client application instance</returns>
        public PublicClientApplicationBuilder WithDefaultRedirectUri()
        {
            Config.UseRecommendedDefaultRedirectUri = true;
            return this;
        }

        /// <summary>
        /// Enables multi cloud support for this instance of public client application.
        /// It enables applications to use in a global public cloud authority to the library and can still get tokens for resources from sovereign clouds.
        /// </summary>
        /// <param name="enableMultiCloudSupport">Enable or disable multi cloud support.</param>
        /// <returns>A <see cref="PublicClientApplicationBuilder"/> from which to set more
        /// parameters, and to create a public client application instance</returns>
        /// <remarks>This feature is available to Microsoft applications, which have the same client id across all clouds</remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public PublicClientApplicationBuilder WithMultiCloudSupport(bool enableMultiCloudSupport)
        {
            Config.MultiCloudSupportEnabled = enableMultiCloudSupport;
            return this;
        }

        /// <summary>
        /// You can specify a Keychain Access Group to use for persisting the token cache across multiple applications.
        /// This enables you to share the token cache between several applications having the same Keychain access group.
        /// Sharing the token cache allows single sign-on between all of the applications that use the same Keychain access Group.
        /// See https://aka.ms/msal-net-ios-keychain-security-group for more information.
        /// </summary>
        /// <param name="keychainSecurityGroup"></param>
        /// <returns>A <see cref="PublicClientApplicationBuilder"/> from which to set more
        /// parameters, and to create a public client application instance</returns>
#if !iOS
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
        public PublicClientApplicationBuilder WithIosKeychainSecurityGroup(string keychainSecurityGroup)
        {
#if iOS
            Config.IosKeychainSecurityGroup = keychainSecurityGroup;
#endif // iOS
            return this;
        }

        /// <summary>
        /// Brokers enable Single-Sign-On, device identification,
        /// and application identification verification. To enable one of these features,
        /// you need to set the WithBroker() parameters to true. See https://aka.ms/msal-net-wam 
        /// for more information on platform specific settings required to enable the broker.
        /// 
        /// On iOS and Android, Authenticator and Company Portal serve as brokers.
        /// On Windows, WAM (Windows Account Manager) serves as broker. See https://aka.ms/msal-net-wam
        /// </summary>
        /// <param name="enableBroker">Determines whether or not to use broker with the default set to true.</param>
        /// <returns>A <see cref="PublicClientApplicationBuilder"/> from which to set more
        /// parameters, and to create a public client application instance</returns>
        /// <remarks>If your app uses .NET classic or .NET, and you wish to use the Windows broker, 
        /// please install the NuGet package Microsoft.Identity.Client.Broker and call .WithBroker(BrokerOptions).
        /// This is not needed if for MAUI apps or for apps targeting net6-windows or higher.</remarks>
        public PublicClientApplicationBuilder WithBroker(bool enableBroker = true)
        {
#pragma warning disable CS0162 // Unreachable code detected

#if NET462
            if (Config.BrokerCreatorFunc == null)
            {
                throw new PlatformNotSupportedException(
                    "The Windows broker is not directly available on MSAL for .NET Framework. " +
                    "\n\rTo use it, install the NuGet package named Microsoft.Identity.Client.Broker " +
                    "and call the extension method .WithBroker(BrokerOptions) " +                    
                    "\n\rFor details see https://aka.ms/msal-net-wam ");
            }
#endif

#if NET_CORE && !NET6_WIN
            if (Config.BrokerCreatorFunc == null)
            {
                throw new PlatformNotSupportedException(
                    "The desktop broker is not directly available in the MSAL package. "+
                    "\n\rInstall the NuGet package Microsoft.Identity.Client.Broker and call the extension method .WithBroker(BrokerOptions). " +
                    "\n\rFor details, see https://aka.ms/msal-net-wam");
            }
#endif

#if NET6_WIN
            Config.BrokerOptions = new BrokerOptions(enableBroker ? BrokerOptions.OperatingSystems.Windows : BrokerOptions.OperatingSystems.None);
#endif

            Config.IsBrokerEnabled = enableBroker;
            return this;
#pragma warning restore CS0162 // Unreachable code detected
        }

        /// <summary>
        /// Allows customization of the Windows 10 Broker experience. 
        /// </summary>
#if !SUPPORTS_BROKER || __MOBILE__
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
#if !NET6_WIN && !WINDOWS_APP && !__MOBILE__
        [Obsolete("This API has been replaced with WithBroker(BrokerOptions), which can be found in Microsoft.Identity.Client.Broker package. See https://aka.ms/msal-net-wam for details.", false)]
#endif
#if NET6_WIN
        [Obsolete("This API has been replaced with WithBroker(BrokerOptions). See https://aka.ms/msal-net-wam for details.", false)]
#endif
        public PublicClientApplicationBuilder WithWindowsBrokerOptions(WindowsBrokerOptions options)
        {
            WindowsBrokerOptions.ValidatePlatformAvailability();
            var newOptions = BrokerOptions.CreateFromWindowsOptions(options);
            Config.BrokerOptions = newOptions; 
            return this;
        }

#if NET6_WIN
        /// <summary>
        /// Brokers enable Single-Sign-On, device identification, and enhanced security.
        /// Use this API to enable brokers on desktop platforms.
        /// 
        /// See https://aka.ms/msal-net-wam for more information on platform specific settings required to enable the broker such as redirect URIs.
        /// 
        /// </summary>
        /// <param name="brokerOptions">This provides cross platform options for broker.</param>
        /// <returns>A <see cref="PublicClientApplicationBuilder"/> from which to set more
        /// parameters, and to create a public client application instance</returns>
        public PublicClientApplicationBuilder WithBroker(BrokerOptions brokerOptions)
        {
            Config.BrokerOptions = brokerOptions;
            Config.IsBrokerEnabled = brokerOptions.IsBrokerEnabledOnCurrentOs();

            return this;
        }
#endif

#if WINDOWS_APP
        /// <summary>
        /// Flag to enable authentication with the user currently signed-in on Windows.
        /// </summary>
        /// <param name="useCorporateNetwork">When set to true, the application will try to connect to the corporate network using Windows Integrated Authentication.</param>
        /// <returns>A <see cref="PublicClientApplicationBuilder"/> from which to set more
        /// parameters, and to create a public client application instance</returns>
        public PublicClientApplicationBuilder WithUseCorporateNetwork(bool useCorporateNetwork)
        {
            Config.UseCorporateNetwork = useCorporateNetwork;
            return this;
        }
#endif

        /// <summary>
        ///  Sets a reference to the ViewController (if using Xamarin.iOS), Activity (if using Xamarin.Android)
        ///  IWin32Window or IntPtr (if using .Net Framework). Used for invoking the browser.
        /// </summary>
        /// <remarks>
        /// Mandatory only on Android to be set either from here or from AcquireTokenInteractive builder.
        /// See https://aka.ms/msal-net-android-activity for further documentation and details.
        /// </remarks>
        /// <param name="parentActivityOrWindowFunc">The parent as an object, so that it can be used from shared NetStandard assemblies</param>
        /// <returns>The builder to chain the .With methods</returns>
        /// 
#if !NETSTANDARD
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)] // hide everywhere but NetStandard
#endif
        public PublicClientApplicationBuilder WithParentActivityOrWindow(Func<object> parentActivityOrWindowFunc)
        {
            return WithParentFunc(parentActivityOrWindowFunc);
        }

        private PublicClientApplicationBuilder WithParentFunc(Func<object> parentFunc)
        {
            Config.ParentActivityOrWindowFunc = parentFunc;
            return this;
        }

#if ANDROID
        /// <summary>
        /// Sets a reference to the current Activity that triggers the browser to be shown. Required
        /// for MSAL to be able to show the browser when using Xamarin.Android
        /// </summary>
        /// <param name="activityFunc">A function to return the current Activity</param>
        /// <returns>The builder to chain the .With methods</returns>
        [CLSCompliant(false)]
        public PublicClientApplicationBuilder WithParentActivityOrWindow(Func<Activity> activityFunc)
        {
            if (activityFunc == null)
            {
                throw new ArgumentNullException(nameof(activityFunc));
            }

            return WithParentFunc(() => (object)activityFunc());
        }
#endif

#if iOS
        /// <summary>
        /// Sets a reference to the current ViewController that triggers the browser to be shown. 
        /// </summary>
        /// <param name="viewControllerFunc">A function to return the current ViewController</param>
        /// <returns>The builder to chain the .With methods</returns>
        [CLSCompliant(false)]
        public PublicClientApplicationBuilder WithParentActivityOrWindow(Func<UIViewController> viewControllerFunc)
        {
            if (viewControllerFunc == null)
            {
                throw new ArgumentNullException(nameof(viewControllerFunc));
            }

            return WithParentFunc(() => (object)viewControllerFunc());
        }
#endif

#if NETFRAMEWORK || NET6_WIN
        /// <summary>
        /// Sets a reference to the current IWin32Window that triggers the browser to be shown.
        /// Used to center the browser that pop-up onto this window.
        /// </summary>
        /// <param name="windowFunc">A function to return the current window</param>
        /// <returns>The builder to chain the .With methods</returns>
        [CLSCompliant(false)]
        public PublicClientApplicationBuilder WithParentActivityOrWindow(Func<IWin32Window> windowFunc)
        {
            if (windowFunc == null)
            {
                throw new ArgumentNullException(nameof(windowFunc));
            }

            return WithParentFunc(windowFunc);
        }
#endif

#if NETFRAMEWORK || NET6_WIN || NET_CORE || NETSTANDARD
        /// <summary>
        /// Sets a reference to the IntPtr to a window that triggers the browser to be shown.
        /// Used to center the browser that pop-up onto this window.
        /// </summary>
        /// <param name="windowFunc">A function to return the current window</param>
        /// <returns>The builder to chain the .With methods</returns>
        [CLSCompliant(false)]
        public PublicClientApplicationBuilder WithParentActivityOrWindow(Func<IntPtr> windowFunc)
        {
            if (windowFunc == null)
            {
                throw new ArgumentNullException(nameof(windowFunc));
            }

            return WithParentFunc(() => windowFunc());
        }
#endif

        /// <summary>
        /// Sets the parameters required to get a Kerberos Ticket from Azure AD service.
        /// </summary>
        /// <param name="servicePrincipalName">Service principal name to get Kerberos Service Ticket.</param>
        /// <param name="ticketContainer">Container to use for Kerberos Ticket.</param>
        /// <returns>The builder to chain the .With methods</returns>
        public PublicClientApplicationBuilder WithKerberosTicketClaim(string servicePrincipalName, KerberosTicketContainer ticketContainer)
        {
            Config.KerberosServicePrincipalName = servicePrincipalName;
            Config.TicketContainer = ticketContainer;
            return this;
        }

        /// <summary>
        /// Returns <c>true</c> if a broker can be used.
        /// This method is only needed to be used in mobile scenarios which support Mobile Application Management. In other supported scenarios, use <c>WithBroker</c> by itself, which will fall back to use a browser if broker is unavailable.
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
            return PlatformProxyFactory.CreatePlatformProxy(null)
                    .CreateBroker(Config, null).IsBrokerInstalledAndInvokable(Config.Authority?.AuthorityInfo?.AuthorityType ?? AuthorityType.Aad);
        }

        /// <summary>
        /// Builds an instance of <see cref="IPublicClientApplication"/> 
        /// from the parameters set in the <see cref="PublicClientApplicationBuilder"/>.
        /// </summary>
        /// <exception cref="MsalClientException">Thrown when errors occur locally in the library itself (for example, because of incorrect configuration).</exception>
        /// <returns>An instance of <see cref="IPublicClientApplication"/></returns>
        public IPublicClientApplication Build()
        {
            return BuildConcrete();
        }

        internal PublicClientApplication BuildConcrete()
        {
            return new PublicClientApplication(BuildConfiguration());
        }

        /// <inheritdoc/>
        internal override void Validate()
        {
            base.Validate();

            //ADFS does not require client id to be in the form of a GUID.
            if (Config.Authority.AuthorityInfo?.AuthorityType != AuthorityType.Adfs && !Guid.TryParse(Config.ClientId, out _))
            {
                throw new MsalClientException(MsalError.ClientIdMustBeAGuid, MsalErrorMessage.ClientIdMustBeAGuid);
            }

#if IS_XAMARIN_OR_UWP
            if (Config.IsBrokerEnabled && Config.MultiCloudSupportEnabled)
            {
                // TODO: https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3139
                throw new NotSupportedException(MsalErrorMessage.MultiCloudSupportUnavailable);
            }
#endif
            if (string.IsNullOrWhiteSpace(Config.RedirectUri))
            {
                Config.RedirectUri = PlatformProxyFactory.CreatePlatformProxy(null)
                                                         .GetDefaultRedirectUri(Config.ClientId, Config.UseRecommendedDefaultRedirectUri);
            }

            if (!Uri.TryCreate(Config.RedirectUri, UriKind.Absolute, out Uri uriResult))
            {
                throw new InvalidOperationException(MsalErrorMessage.InvalidRedirectUriReceived(Config.RedirectUri));
            }
        }
    }
}
