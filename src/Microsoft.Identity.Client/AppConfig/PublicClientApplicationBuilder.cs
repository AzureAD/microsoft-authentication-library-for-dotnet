// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.PlatformsCommon.Factories;

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
        /// <term><c>https://localhost</c></term>
        /// </item>
        /// </list>
        /// NOTE:There will be an update to the default rediect uri in the future to accomodate for system browsers on the 
        /// .NET desktop and .NET Core platforms.
        /// </summary>
        /// <returns>A <see cref="PublicClientApplicationBuilder"/> from which to set more
        /// parameters, and to create a public client application instance</returns>
        public PublicClientApplicationBuilder WithDefaultRedirectUri()
        {
            Config.UseRecommendedDefaultRedirectUri = true;
            return this;
        }

#if !ANDROID_BUILDTIME && !WINDOWS_APP_BUILDTIME && !NET_CORE_BUILDTIME && !DESKTOP_BUILDTIME && !MAC_BUILDTIME
        /// <summary>
        /// You can specify a Keychain Access Group to use for persisting the token cache across multiple applications.
        /// This enables you to share the token cache between several applications having the same keychain access group.
        /// Sharing the token cache allows single sign-on between all of the applications that use the same Keychain access Group.
        /// See https://aka.ms/msal-net-ios-keychain-security-group for more information.
        /// </summary>
        /// <param name="keychainSecurityGroup"></param>
        /// <returns>A <see cref="PublicClientApplicationBuilder"/> from which to set more
        /// parameters, and to create a public client application instance</returns>
        public PublicClientApplicationBuilder WithIosKeychainSecurityGroup(string keychainSecurityGroup)
        {
#if iOS
            Config.IosKeychainSecurityGroup = keychainSecurityGroup;
#endif // iOS
            return this;
        }

        /// <summary>
        /// On Android and iOS, brokers enabled Single-Sign-On, device identification,
        /// and application identification verificatoin. To enable one of these features,
        /// you need to set the WithBroker() parameters to true. See https://aka.ms/msal-net-brokers 
        /// for more information on platform specific settings required to enable the broker.
        /// </summary>
        /// <param name="enableBroker"></param>
        /// <returns>A <see cref="PublicClientApplicationBuilder"/> from which to set more
        /// parameters, and to create a public client application instance</returns>
        public PublicClientApplicationBuilder WithBroker(bool enableBroker)
        {
#if iOS
            Config.IsBrokerEnabled = enableBroker;
#endif // iOS
            return this;
        }
#endif // !ANDROID_BUILDTIME && !WINDOWS_APP_BUILDTIME && !NET_CORE_BUILDTIME && !DESKTOP_BUILDTIME && !MAC_BUILDTIME

#if WINDOWS_APP
        /// <summary>
        /// Flag to enable authentication with the user currently logeed-in in Windows.
        /// </summary>
        /// <param name="useCorporateNetwork">When set to true, the application will try to connect to the corporate network using windows integrated authentication.</param>
        /// <returns>A <see cref="PublicClientApplicationBuilder"/> from which to set more
        /// parameters, and to create a public client application instance</returns>
        public PublicClientApplicationBuilder WithUseCorporateNetwork(bool useCorporateNetwork)
        {
            Config.UseCorporateNetwork = useCorporateNetwork;
            return this;
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
