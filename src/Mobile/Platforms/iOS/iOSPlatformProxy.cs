// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#if iOS

using System;
using System.Globalization;
using System.Threading.Tasks;
using Foundation;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.TelemetryCore.Internal;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.UI;
using UIKit;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.PlatformsCommon.Factories;

namespace Microsoft.Identity.Client.Platforms.iOS
{
    /// <summary>
    ///     Platform / OS specific logic.  No library (ADAL / MSAL) specific code should go in here.
    /// </summary>
    internal class iOSPlatformProxy : AbstractSharedPlatformProxy, IPublicClientPlatformProxy
    {
        internal const string IosDefaultRedirectUriTemplate = "msal{0}://auth";

        public iOSPlatformProxy(ICoreLogger logger)
            : base(logger)
        {
        }

        public bool IsSystemWebViewAvailable => true;

        public bool UseEmbeddedWebViewDefault => false;

        /// <summary>
        /// Get the user logged
        /// </summary>
        public Task<string> GetUserPrincipalNameAsync()
        {
            return Task.FromResult(string.Empty);
        }

       
        public override string GetEnvironmentVariable(string variable)
        {
            return null;
        }

        protected override  string InternalGetProcessorArchitecture()
        {
            return null;
        }

        protected override  string InternalGetOperatingSystem()
        {
            return UIDevice.CurrentDevice.SystemVersion;
        }

        protected override  string InternalGetDeviceModel()
        {
            return UIDevice.CurrentDevice.Model;
        }

        /// <inheritdoc />
        public override string GetDefaultRedirectUri(string clientId, bool useRecommendedRedirectUri = false)
        {
            return string.Format(CultureInfo.InvariantCulture, IosDefaultRedirectUriTemplate, clientId);
        }

        protected override  string InternalGetProductName()
        {
            return "MSAL.Xamarin.iOS";
        }

        /// <summary>
        /// Considered PII, ensure that it is hashed.
        /// </summary>
        /// <returns>Name of the calling application</returns>
        protected override  string InternalGetCallingApplicationName()
        {
            return (NSString)NSBundle.MainBundle?.InfoDictionary?["CFBundleName"];
        }

        /// <summary>
        /// Considered PII, ensure that it is hashed.
        /// </summary>
        /// <returns>Version of the calling application</returns>
        protected override  string InternalGetCallingApplicationVersion()
        {
            return (NSString)NSBundle.MainBundle?.InfoDictionary?["CFBundleVersion"];
        }

        /// <summary>
        /// Considered PII. Please ensure that it is hashed.
        /// </summary>
        /// <returns>Device identifier</returns>
        protected override  string InternalGetDeviceId()
        {
            return UIDevice.CurrentDevice?.IdentifierForVendor?.AsString();
        }

        public override ILegacyCachePersistence CreateLegacyCachePersistence()
        {
            return new iOSLegacyCachePersistence(Logger);
        }

        public override ITokenCacheAccessor CreateTokenCacheAccessor()
        {
            return new iOSTokenCacheAccessor();
        }

        /// <inheritdoc />
        protected IWebUIFactory CreateWebUiFactory()
        {
            return new IosWebUIFactory();
        }

        protected override ICryptographyManager InternalGetCryptographyManager() => new iOSCryptographyManager();
        protected override IPlatformLogger InternalGetPlatformLogger() => new ConsolePlatformLogger();

        public override string GetDeviceNetworkState()
        {
            // TODO(mats):
            return string.Empty;
        }

        public override string GetDevicePlatformTelemetryId()
        {
            // TODO(mats):
            return string.Empty;
        }

        public override string GetMatsOsPlatform()
        {
            return MatsConverter.AsString(OsPlatform.Ios);
        }

        public override int GetMatsOsPlatformCode()
        {
            return MatsConverter.AsInt(OsPlatform.Ios);
        }

        protected override IFeatureFlags CreateFeatureFlags() => new iOSFeatureFlags();

        public IBroker CreateBroker(CoreUIParent uiParent)
        {
            if (OverloadBrokerForTest != null)
            {
                return OverloadBrokerForTest;
            }

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

        public IWebUIFactory OverloadWebUiFactory { get; private set; }

        /// <inheritdoc />
        public IWebUIFactory GetWebUiFactory()
        {
            return OverloadWebUiFactory ?? CreateWebUiFactory();
        }

        /// <inheritdoc />
        public void SetWebUiFactory(IWebUIFactory webUiFactory)
        {
            OverloadWebUiFactory = webUiFactory;
        }
    }
}
#endif
