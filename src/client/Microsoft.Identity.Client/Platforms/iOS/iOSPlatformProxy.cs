// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Globalization;
using System.Threading.Tasks;
using Foundation;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.UI;
using UIKit;

namespace Microsoft.Identity.Client.Platforms.iOS
{
    /// <summary>
    ///     Platform / OS specific logic.  No library (ADAL / MSAL) specific code should go in here.
    /// </summary>
    internal class iOSPlatformProxy : AbstractPlatformProxyPublic
    {
        internal const string IosDefaultRedirectUriTemplate = "msal{0}://auth";

        public iOSPlatformProxy(ILoggerAdapter logger)
            : base(logger)
        {
        }

        public override Task<string> GetUserPrincipalNameAsync()
        {
            return Task.FromResult(string.Empty);
        }
        internal override string InternalGetProcessorArchitecture()
        {
            return null;
        }

        internal override string InternalGetOperatingSystem()
        {
            return UIDevice.CurrentDevice.SystemVersion;
        }

        internal override string InternalGetDeviceModel()
        {
            return UIDevice.CurrentDevice.Model;
        }

        /// <inheritdoc />
        public override string GetDefaultRedirectUri(string clientId, bool useRecommendedRedirectUri = false)
        {
            return string.Format(CultureInfo.InvariantCulture, IosDefaultRedirectUriTemplate, clientId);
        }

        internal override string InternalGetProductName()
        {
            return "MSAL.Xamarin.iOS";
        }

        /// <summary>
        /// Considered PII, ensure that it is hashed.
        /// </summary>
        /// <returns>Name of the calling application</returns>
        internal override string InternalGetCallingApplicationName()
        {
            return (NSString)NSBundle.MainBundle?.InfoDictionary?["CFBundleName"];
        }

        /// <summary>
        /// Considered PII, ensure that it is hashed.
        /// </summary>
        /// <returns>Version of the calling application</returns>
        internal override string InternalGetCallingApplicationVersion()
        {
            return (NSString)NSBundle.MainBundle?.InfoDictionary?["CFBundleVersion"];
        }

        /// <summary>
        /// Considered PII. Please ensure that it is hashed.
        /// </summary>
        /// <returns>Device identifier</returns>
        internal override string InternalGetDeviceId()
        {
            return UIDevice.CurrentDevice?.IdentifierForVendor?.AsString();
        }

        public override ILegacyCachePersistence CreateLegacyCachePersistence()
        {
            return new iOSLegacyCachePersistence(Logger);
        }

        public override ITokenCacheAccessor CreateTokenCacheAccessor(
            CacheOptions tokenCacheAccessorOptions,
            bool isApplicationTokenCache = false)
        {
            return new iOSTokenCacheAccessor();
        }

        /// <inheritdoc />
        protected override IWebUIFactory CreateWebUiFactory()
        {
            return new IosWebUIFactory();
        }

        internal override ICryptographyManager InternalGetCryptographyManager() => new CommonCryptographyManager();
        internal override IPlatformLogger InternalGetPlatformLogger() => new ConsolePlatformLogger();

        internal override IFeatureFlags CreateFeatureFlags() => new iOSFeatureFlags();

        public override IBroker CreateBroker(ApplicationConfigurationPublic appConfig, CoreUIParent uiParent)
        {
            return new iOSBroker(Logger, CryptographyManager, uiParent);
        }

        public override bool CanBrokerSupportSilentAuth()
        {
            return false;
        }

        public override IMsalHttpClientFactory CreateDefaultHttpClientFactory()
        {
            return new IosHttpClientFactory();
        }

        public override bool LegacyCacheRequiresSerialization => false;

    }
}
