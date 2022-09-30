// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Platforms.Android.EmbeddedWebview;
using Microsoft.Identity.Client.Platforms.Android.SystemWebview;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.Platforms.Android
{
#if MAUI
    [Preserve(AllMembers = true)]
#else
    [global::Android.Runtime.Preserve(AllMembers = true)]
#endif
    internal class AndroidWebUIFactory : IWebUIFactory
    {
        private const string ChromePackage = "com.android.chrome";
        // this is used to check if anything can open custom tabs.
        // Must use the classic support. Leaving the reference androidx intent
        //#if __ANDROID_29__
        //        private const string CustomTabService = "androidx.browser.customtabs.action.CustomTabsService";
        //#else
        private const string CustomTabService = "android.support.customtabs.action.CustomTabsService";
        //#endif

        public IWebUI CreateAuthenticationDialog(
            CoreUIParent coreUIParent,
            WebViewPreference useEmbeddedWebView,
            RequestContext requestContext)
        {
            if (useEmbeddedWebView == WebViewPreference.Embedded)
            {
                return new EmbeddedWebUI(coreUIParent)
                {
                    RequestContext = requestContext
                };
            }

            return new SystemWebUI(coreUIParent)
            {
                RequestContext = requestContext
            };
        }

        public bool IsSystemWebViewAvailable
        {
            get
            {
                bool isBrowserWithCustomTabSupportAvailable = IsBrowserWithCustomTabSupportAvailable();
                return (isBrowserWithCustomTabSupportAvailable || IsChromeEnabled()) &&
                       isBrowserWithCustomTabSupportAvailable;
            }
        }

        public bool IsUserInteractive => true;

        public bool IsEmbeddedWebViewAvailable => true;

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

    }
}
