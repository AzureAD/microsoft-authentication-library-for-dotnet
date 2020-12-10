// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.TelemetryCore.Internal;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Platforms.Android.EmbeddedWebview;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.Platforms.Android.Broker;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Platforms.Android
{
    /// <summary>
    /// Platform / OS specific logic.  No library (ADAL / MSAL) specific code should go in here.
    /// </summary>
    [global::Android.Runtime.Preserve(AllMembers = true)]
    internal class AndroidPlatformProxy : AbstractPlatformProxy
    {
        internal const string AndroidDefaultRedirectUriTemplate = "msal{0}://auth";
        private const string ChromePackage = "com.android.chrome";
        // this is used to check if anything can open custom tabs.
        // Must use the classic support. Leaving the reference androidx intent
        //#if __ANDROID_29__
        //        private const string CustomTabService = "androidx.browser.customtabs.action.CustomTabsService";
        //#else
        private const string CustomTabService = "android.support.customtabs.action.CustomTabsService";
        //#endif
        public AndroidPlatformProxy(ICoreLogger logger) : base(logger)
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
        public override ITokenCacheAccessor CreateTokenCacheAccessor()
        {
            return new AndroidTokenCacheAccessor();
        }

        /// <inheritdoc />
        protected override IWebUIFactory CreateWebUiFactory()
        {
            return new AndroidWebUIFactory();
        }

        protected override ICryptographyManager InternalGetCryptographyManager() => new AndroidCryptographyManager();
        protected override IPlatformLogger InternalGetPlatformLogger() => new AndroidPlatformLogger();

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
            return MatsConverter.AsString(OsPlatform.Android);
        }

        public override int GetMatsOsPlatformCode()
        {
            return MatsConverter.AsInt(OsPlatform.Android);
        }
        protected override IFeatureFlags CreateFeatureFlags() => new AndroidFeatureFlags();

        public override bool IsSystemWebViewAvailable
        {
            get
            {
                bool isBrowserWithCustomTabSupportAvailable = IsBrowserWithCustomTabSupportAvailable();
                return (isBrowserWithCustomTabSupportAvailable || IsChromeEnabled()) &&
                       isBrowserWithCustomTabSupportAvailable;
            }
        }

        private static bool IsBrowserWithCustomTabSupportAvailable()
        {
            Intent customTabServiceIntent = new Intent(CustomTabService);

            IEnumerable<ResolveInfo> resolveInfoListWithCustomTabs =
                Application.Context.PackageManager.QueryIntentServices(
                    customTabServiceIntent, PackageInfoFlags.MatchAll);

            // queryIntentServices could return null or an empty list if no matching service existed.
            if (resolveInfoListWithCustomTabs == null || !resolveInfoListWithCustomTabs.Any())
            {
                return false;
            }

            return true;
        }

        private static bool IsChromeEnabled()
        {
            try
            {
                ApplicationInfo applicationInfo = Application.Context.PackageManager.GetApplicationInfo(ChromePackage, 0);

                // Chrome is difficult to uninstall on an Android device. Most users will disable it, but the package will still
                // show up, therefore need to check application.Enabled is false
                return applicationInfo.Enabled;
            }
            catch (PackageManager.NameNotFoundException)
            {
                // In case Chrome is actually uninstalled, GetApplicationInfo will throw
                return false;
            }
        }

        public override bool UseEmbeddedWebViewDefault => false;

        public override IBroker CreateBroker(IAppConfigInternal appConfig, CoreUIParent uiParent)
        {
            return new AndroidBroker(uiParent, Logger);
        }

        public override bool CanBrokerSupportSilentAuth()
        {
            IBroker broker = CreateBroker(null, null);

            if (broker.IsBrokerInstalledAndInvokable())
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override IMsalHttpClientFactory CreateDefaultHttpClientFactory()
        {
            return new AndroidHttpClientFactory();
        }
    }
}
