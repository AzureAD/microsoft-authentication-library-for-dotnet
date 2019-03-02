// ------------------------------------------------------------------------------
// 
// Copyright (c) Microsoft Corporation.
// All rights reserved.
// 
// This code is licensed under the MIT License.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// ------------------------------------------------------------------------------

using System;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.PlatformsCommon.Factories;

namespace Microsoft.Identity.Client.AppConfig
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

#if !ANDROID_BUILDTIME && !WINDOWS_APP_BUILDTIME && !NET_CORE_BUILDTIME && !DESKTOP_BUILDTIME && !MAC_BUILDTIME
        /// <summary>
        /// 
        /// </summary>
        /// <param name="keychainSecurityGroup"></param>
        /// <returns></returns>
        public PublicClientApplicationBuilder WithIosKeychainSecurityGroup(string keychainSecurityGroup)
        {
#if iOS
            Config.IosKeychainSecurityGroup = keychainSecurityGroup;
#endif // iOS 
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="enableBroker"></param>
        /// <returns></returns>
        private PublicClientApplicationBuilder WithBroker(bool enableBroker)
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
                                                         .GetDefaultRedirectUri(Config.ClientId);
            }

            if (!Uri.TryCreate(Config.RedirectUri, UriKind.Absolute, out Uri uriResult))
            {
                throw new InvalidOperationException(CoreErrorMessages.InvalidRedirectUriReceived(Config.RedirectUri));
            }
        }
    }
}