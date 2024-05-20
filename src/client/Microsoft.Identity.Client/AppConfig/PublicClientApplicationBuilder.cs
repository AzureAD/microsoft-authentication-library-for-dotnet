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
using Microsoft.Identity.Client.Instance;

#if iOS
using UIKit;
#endif

#if ANDROID
using Android.App;
#endif

#if NETFRAMEWORK 
using System.Windows.Forms;
#endif

#if MAC
using AppKit;
#endif

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// </summary>
    public sealed class PublicClientApplicationBuilder : 
        AbstractApplicationBuilder<PublicClientApplicationBuilder>
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
        /// <Description><c>`https://login.microsoftonline.com/common/oauth2/nativeclient`</c></Description>
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

#if ANDROID || iOS
        /// <summary>
        /// Brokers (Microsoft Authenticator, Intune Company Portal) enable Single-Sign-On, device identification,

        /// and application identification verification. To enable one of these features,
        /// you need to set the WithBroker(bool) parameters to true on Android and iOS. 
        /// On desktop platforms, install the NuGet package Microsoft.Identity.Client.Broker and call the extension method .WithBroker(BrokerOptions)
        /// See https://aka.ms/msal-net-wam for desktop platforms.
        /// </summary>
        /// <param name="enableBroker">Determines whether or not to use broker with the default set to true.</param>
        /// <returns>A <see cref="PublicClientApplicationBuilder"/> from which to set more
        /// parameters, and to create a public client application instance</returns>
        /// <remarks>On desktop (.NET, .NET Framework) install the NuGet package Microsoft.Identity.Client.Broker and call .WithBroker(BrokerOptions).
        /// This is not needed for MAUI apps.</remarks>
        public PublicClientApplicationBuilder WithBroker(bool enableBroker = true)
        {
            Config.IsBrokerEnabled = enableBroker;
            return this;
        }
#else
        /// <summary>
        /// 
        /// </summary>
        /// <param name="enableBroker"></param>
        /// <returns></returns>
        /// <exception cref="PlatformNotSupportedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("The desktop broker is not directly available in the MSAL package. Install the NuGet package Microsoft.Identity.Client.Broker and call the extension method .WithBroker(BrokerOptions). For details, see https://aka.ms/msal-net-wam", true)]
        public PublicClientApplicationBuilder WithBroker(bool enableBroker = true)
        {
            throw new PlatformNotSupportedException(
                  "The desktop broker is not directly available in the Microsoft.Identity.Client package. " +
                  "\n\rTo use it, install the NuGet package named Microsoft.Identity.Client.Broker " +
                  "and call the extension method .WithBroker(BrokerOptions) from namespace Microsoft.Identity.Client.Broker" +
                  "\n\rFor details see https://aka.ms/msal-net-wam ");
        }
#endif

        /// <summary>
        /// Allows customization of the Windows 10 Broker experience. 
        /// </summary>
#if !SUPPORTS_BROKER || __MOBILE__
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
#if !__MOBILE__
        [Obsolete("This API has been replaced with WithBroker(BrokerOptions), which can be found in Microsoft.Identity.Client.Broker package. See https://aka.ms/msal-net-wam for details.", false)]
#endif

        public PublicClientApplicationBuilder WithWindowsBrokerOptions(WindowsBrokerOptions options)
        {
            WindowsBrokerOptions.ValidatePlatformAvailability();
            var newOptions = BrokerOptions.CreateFromWindowsOptions(options);
            Config.BrokerOptions = newOptions; 
            return this;
        }

        /// <summary>
        ///  Sets a reference to the ViewController (if using iOS), Activity (if using Android)
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

        /// <summary>
        /// Adds a known authority corresponding to a generic OpenIdConnect Identity Provider. 
        /// MSAL will append ".well-known/openid-configuration" to the authority and retrieve the OIDC 
        /// metadata from there, to figure out the endpoints.
        /// See https://openid.net/specs/openid-connect-core-1_0.html#Terminology
        /// </summary>
        /// <remarks>
        /// Experimental on public clients.
        /// Do not use this method with Entra ID authorities (e.g. https://login.microsfoftonline.com/common).
        /// Use WithAuthority(string) instead.
        /// </remarks>
        public PublicClientApplicationBuilder WithOidcAuthority(string authorityUri) 
        {
            ValidateUseOfExperimentalFeature("WithOidcAuthority");

            var authorityInfo = AuthorityInfo.FromGenericAuthority(authorityUri);
            Config.Authority = Authority.CreateAuthority(authorityInfo);

            return this;
        }

#if ANDROID
        /// <summary>
        /// Sets a reference to the current Activity that triggers the browser to be shown. Required
        /// for MSAL to be able to show the browser when using Android
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

#if NETFRAMEWORK
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

#if NETFRAMEWORK || NET_CORE || NETSTANDARD
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
        /// <param name="ticketContainer">Specify where the Kerberos ticket will be returned - as a claim in the ID token or as a claim in the access token. 
        /// If the ticket is for the client application, use the ID token. If the ticket is for the downstream API, use the access token.</param>
        /// <remarks>
        /// The expiry of the Kerberos ticket is tied to the expiry of the token that contains it.
        /// MSAL provides several helper APIs to read and write Kerberos tickets from the Windows Ticket Cache - see <see cref="KerberosSupplementalTicketManager"/>.
        /// </remarks>
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
        /// <item><description>In .NET classic or .NET, install Microsoft.Identity.Client.Desktop first and call <c>WithDesktopFeatures()</c>.</description></item>
        /// <item><description>In mobile apps, the device must be Intune joined and Authenticator or Company Portal must be installed. See https://aka.ms/msal-brokers </description></item>
        /// </list>
        /// </remarks>
#if ANDROID || iOS
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

#if __MOBILE__
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

            if (!Uri.TryCreate(Config.RedirectUri, UriKind.Absolute, out Uri _))
            {
                throw new InvalidOperationException(MsalErrorMessage.InvalidRedirectUriReceived(Config.RedirectUri));
            }
        }
    }
}
