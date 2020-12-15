// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.PlatformsCommon.Factories;

#if iOS
using UIKit;
#endif

#if ANDROID
using Android.App;
#endif

#if DESKTOP || NET5_WIN
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
        /// <inheritdoc />
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
            var config = new ApplicationConfiguration();
            return new PublicClientApplicationBuilder(config).WithOptions(options);
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
            var config = new ApplicationConfiguration();
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
        /// <term>Default Reply URI</term>
        /// </listheader>
        /// <item>
        /// <term>.NET desktop</term>
        /// <term><c>https://login.microsoftonline.com/common/oauth2/nativeclient</c></term>
        /// </item>
        /// <item>
        /// <term>UWP</term>
        /// <term>value of <c>WebAuthenticationBroker.GetCurrentApplicationCallbackUri()</c></term>
        /// </item>
        /// <item>
        /// <term>For system browser on .NET Core</term>
        /// <term><c>http://localhost</c></term>
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
        /// you need to set the WithBroker() parameters to true. See https://aka.ms/msal-net-brokers 
        /// for more information on platform specific settings required to enable the broker.
        /// 
        /// On iOS and Android, Autheticator and Company Portal serve as brokers.
        /// On Windows, WAM (Windows Account Manager) serves as broker. See https://aka.ms/msal-net-wam
        /// </summary>
        /// <param name="enableBroker">Determines whether or not to use broker with the default set to true.</param>
        /// <returns>A <see cref="PublicClientApplicationBuilder"/> from which to set more
        /// parameters, and to create a public client application instance</returns>
        public PublicClientApplicationBuilder WithBroker(bool enableBroker = true)
        {
#if NET45
            throw new PlatformNotSupportedException("The Windows broker is not available on .NET Framework 4.5, please use at least .NET Framework 4.6.2");
#endif

#if NET461 || NET_CORE
            if (Config.BrokerCreatorFunc == null)
            {
                throw new PlatformNotSupportedException(
                    "The Windows broker is not directly available on MSAL for .NET Framework or .NET Core 3.x" +
                    " To use it, please install the nuget package named Microsoft.Identity.Client.Desktop " +
                    "and call the extension method .WithWindowsBroker(true)");
            }
#endif
#if NET461 || WINDOWS_APP || NET5_WIN
            if (!Config.ExperimentalFeaturesEnabled)
            {
                throw new MsalClientException(
                    MsalError.ExperimentalFeature,
                    MsalErrorMessage.ExperimentalFeature(nameof(WithBroker)));
            }
#endif

#pragma warning disable CS0162 // Unreachable code detected
            Config.IsBrokerEnabled = enableBroker;
            return this;
#pragma warning restore CS0162 // Unreachable code detected
        }

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

#if DESKTOP || NET5_WIN
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

            return WithParentFunc(() => (object)windowFunc());
        }
#endif

#if DESKTOP || NET5_WIN || NET_CORE
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

            return WithParentFunc(() => (object)windowFunc());
        }
#endif

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public IPublicClientApplication Build()
        {
            return BuildConcrete();
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        internal PublicClientApplication BuildConcrete()
        {
            return new PublicClientApplication(BuildConfiguration());
        }

        /// <inheritdoc />
        internal override void Validate()
        {
            base.Validate();

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
