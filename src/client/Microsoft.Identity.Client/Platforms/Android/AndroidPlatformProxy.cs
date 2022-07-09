// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.Platforms.Android.Broker;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.Platforms.Android
{
    /// <summary>
    /// Platform / OS specific logic.  No library (ADAL / MSAL) specific code should go in here.
    /// </summary>
#if MAUI
    [Preserve(AllMembers = true)]
#else
    [global::Android.Runtime.Preserve(AllMembers = true)]
#endif
    internal class AndroidPlatformProxy : AbstractPlatformProxy
    {
        internal const string AndroidDefaultRedirectUriTemplate = "msal{0}://auth";
        public AndroidPlatformProxy(ILoggerAdapter logger) : base(logger)
        {
        }

        /// <summary>
        /// Get the user logged in
        /// </summary>
        /// <returns>The username or throws</returns>
        public override Task<string> GetUserPrincipalNameAsync()
        {
            return Task.FromResult(string.Empty);

        }
        protected override string InternalGetProcessorArchitecture()
        {
            if (global::Android.OS.Build.VERSION.SdkInt < global::Android.OS.BuildVersionCodes.Lollipop)
            {
#pragma warning disable CS0618 // For backwards compat only
                return global::Android.OS.Build.CpuAbi;
#pragma warning restore CS0618 
            }
            IList<string> supportedABIs = global::Android.OS.Build.SupportedAbis;
            if (supportedABIs != null && supportedABIs.Count > 0)
            {
                return supportedABIs[0];
            }

            return null;
        }

        protected override string InternalGetOperatingSystem()
        {
            return ((int)global::Android.OS.Build.VERSION.SdkInt)
                .ToString(CultureInfo.InvariantCulture);
        }

        protected override string InternalGetDeviceModel()
        {
            return global::Android.OS.Build.Model;
        }

        /// <inheritdoc />
        public override string GetDefaultRedirectUri(string clientId, bool useRecommendedRedirectUri = false)
        {
            return string.Format(CultureInfo.InvariantCulture, AndroidDefaultRedirectUriTemplate, clientId);
        }

        protected override string InternalGetProductName()
        {
            return "MSAL.Xamarin.Android";
        }

        /// <summary>
        /// Considered PII, ensure that it is hashed.
        /// </summary>
        /// <returns>Name of the calling application</returns>
        protected override string InternalGetCallingApplicationName()
        {
            return global::Android.App.Application.Context.ApplicationInfo?.LoadLabel(global::Android.App.Application.Context.PackageManager);
        }

        /// <summary>
        /// Considered PII, ensure that it is hashed.
        /// </summary>
        /// <returns>Version of the calling application</returns>
        protected override string InternalGetCallingApplicationVersion()
        {
            return global::Android.App.Application.Context.PackageManager.GetPackageInfo(global::Android.App.Application.Context.PackageName, 0)?.VersionName;
        }

        /// <summary>
        /// Considered PII. Please ensure that it is hashed.
        /// </summary>
        /// <returns>Device identifier</returns>
        protected override string InternalGetDeviceId()
        {
            return global::Android.Provider.Settings.Secure.GetString(
                global::Android.App.Application.Context.ContentResolver,
                global::Android.Provider.Settings.Secure.AndroidId);
        }

        /// <inheritdoc />
        public override ILegacyCachePersistence CreateLegacyCachePersistence()
        {
            return new AndroidLegacyCachePersistence(Logger);
        }

        /// <inheritdoc />
        public override ITokenCacheAccessor CreateTokenCacheAccessor(
            CacheOptions cacheOptions, 
            bool isApplicationTokenCache = false)
        {
            return new AndroidTokenCacheAccessor();
        }

        /// <inheritdoc />
        protected override IWebUIFactory CreateWebUiFactory()
        {
            return new AndroidWebUIFactory();
        }

        protected override ICryptographyManager InternalGetCryptographyManager() => new CommonCryptographyManager();

        protected override IPlatformLogger InternalGetPlatformLogger() => new AndroidPlatformLogger();

        protected override IFeatureFlags CreateFeatureFlags() => new AndroidFeatureFlags();     

        public override IBroker CreateBroker(ApplicationConfiguration appConfig, CoreUIParent uiParent)
        {
            return AndroidBrokerFactory.CreateBroker(uiParent, Logger);
        }

        public override IMsalHttpClientFactory CreateDefaultHttpClientFactory()
        {
            return new AndroidHttpClientFactory();
        }

        public override bool LegacyCacheRequiresSerialization => false;
    }
}
