//----------------------------------------------------------------------
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
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Android specific UI properties for interactive flows, such as the parent activity and
    /// which browser to use
    /// </summary>
    public sealed class UIParent
    {
        private const string ChromePackage = "com.android.chrome";
        private const string CustomTabService = "android.support.customtabs.action.CustomTabsService";

        static UIParent()
        {
            ModuleInitializer.EnsureModuleInitialized();
        }


        /// <summary>
        /// Default constructor. Should not be used on Android.
        /// </summary>
        [Obsolete("This constructor should not be used because this object requires a parameters of type Activity. ")]
        public UIParent() // do not delete this ctor because it exists on NetStandard
        {
            throw new MsalClientException(MsalError.ActivityRequired, MsalErrorMessage.ActivityRequired);
        }

        /// <summary>
        /// Initializes an instance for a provided activity.
        /// </summary>
        /// <param name="activity">parent activity for the call. REQUIRED.</param>
        [CLSCompliant(false)]
        public UIParent(Activity activity)
        {
            CoreUIParent = new CoreUIParent(activity);
        }

        /// <summary>
        /// Initializes an instance for a provided activity with flag directing the application
        /// to use the embedded webview instead of the system browser. See https://aka.ms/msal-net-uses-web-browser
        /// </summary>
        [CLSCompliant(false)]
        public UIParent(Activity activity, bool useEmbeddedWebview) : this(activity)
        {
            CoreUIParent.UseEmbeddedWebview = useEmbeddedWebview;
        }

        #if ANDROID_RUNTIME
        /// <summary>
        /// Platform agnostic constructor that allows building an UIParent from a NetStandard assembly.
        /// On Android, the parent is expected to be an Activity.
        /// </summary>
        /// <remarks>This constructor is only avaiable at runtime, to provide support for NetStandard</remarks>
        /// <param name="parent">Android Activity on which to parent the web UI. Cannot be null.</param>
        /// <param name="useEmbeddedWebview">Flag to determine between embedded vs system browser. See https://aka.ms/msal-net-uses-web-browser </param>
        public UIParent(object parent, bool useEmbeddedWebview)
        : this(ValidateParentObject(parent), useEmbeddedWebview)
        {
        }

        #endif

        private static Activity ValidateParentObject(object parent)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(
                    nameof(parent) +
                    " cannot be null on Android platforms. Please pass in an Activity to which to attach a web UI.");
            }

            Activity parentActivity = parent as Activity;
            if (parentActivity == null)
            {
                throw new ArgumentException(nameof(parent) +
                                            " is expected to be of type Android.App.Activity but is of type " +
                                            parent.GetType());
            }

            return parentActivity;
        }

        internal CoreUIParent CoreUIParent { get; }

        /// <summary>
        /// Checks Android device for chrome packages.
        /// Returns true if chrome package for launching system webview is enabled on device.
        /// Returns false if chrome package is not found.
        /// </summary>
        /// <example>
        /// The following code decides, in a Xamarin.Forms app, which browser to use based on the presence of the
        /// required packages.
        /// <code>
        /// bool useSystemBrowser = UIParent.IsSystemWebviewAvailable();
        /// App.UIParent = new UIParent(Xamarin.Forms.Forms.Context as Activity, !useSystemBrowser);
        /// </code>
        /// </example>
        public static bool IsSystemWebviewAvailable() // This is part of the NetStandard "interface" 
        {
            bool isBrowserWithCustomTabSupportAvailable = IsBrowserWithCustomTabSupportAvailable();
            return (isBrowserWithCustomTabSupportAvailable || IsChromeEnabled()) &&
                   isBrowserWithCustomTabSupportAvailable;
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
            ApplicationInfo applicationInfo = Application.Context.PackageManager.GetApplicationInfo(ChromePackage, 0);

            // Chrome is difficult to uninstall on an Android device. Most users will disable it, but the package will still
            // show up, therefore need to check application.Enabled is false
            return string.IsNullOrEmpty(ChromePackage) || applicationInfo.Enabled;
        }
    }
}