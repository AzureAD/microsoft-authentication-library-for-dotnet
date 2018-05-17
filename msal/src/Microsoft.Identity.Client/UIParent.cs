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

#if ANDROID
using Android.App;
using Android.Content.PM;
#endif

using Microsoft.Identity.Core.UI;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class UIParent
    {
        internal CoreUIParent CoreUIParent { get; private set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public UIParent()
        {
            CoreUIParent = new CoreUIParent();
        }

#if iOS
        public UIParent(bool useEmbeddedWebview)
        {
            CoreUIParent.UseEmbeddedWebview = useEmbeddedWebview;
        }
#endif

#if ANDROID
        private Activity Activity { get; set; }

        private static readonly string[] _chromePackages =
        {"com.android.chrome", "com.chrome.beta", "com.chrome.dev"};

        /// <summary>
        /// Initializes an instance for a provided activity.
        /// </summary>
        /// <param name="activity">parent activity for the call. REQUIRED.</param>
        public UIParent(Activity activity)
        {
            Activity = activity;
            CoreUIParent = new CoreUIParent(Activity);
        }

        public UIParent(Activity activity, bool useEmbeddedWebview) :this(activity)
        {
            CoreUIParent.UseEmbeddedWebview = useEmbeddedWebview;
        }

        public static bool IsSystemWebviewAvailable()
        {
            PackageManager packageManager = Application.Context.PackageManager;

            string installedChromePackage = null;
            try
            {
                for (int i = 0; i < _chromePackages.Length; i++)
                {
                    packageManager.GetPackageInfo(_chromePackages[i], PackageInfoFlags.Activities);
                    installedChromePackage = _chromePackages[i];
                }
            }
            catch (PackageManager.NameNotFoundException)
            {
                return false;
            }

            return true;
        }
#endif

#if DESKTOP || WINRT
        //hidden webview can be used in both WinRT and desktop applications.
        internal bool UseHiddenBrowser { get { return CoreUIParent.UseHiddenBrowser; } set { CoreUIParent.UseHiddenBrowser = value; } }

#if WINRT
        internal bool UseCorporateNetwork { get { return CoreUIParent.UseCorporateNetwork; } set { CoreUIParent.UseCorporateNetwork = value; } }

#endif

#if DESKTOP
        internal object OwnerWindow { get; set; }

        /// <summary>
        /// Initializes an instance for a provided parent window.
        /// </summary>
        /// <param name="ownerWindow">Parent window object reference. OPTIONAL.</param>
        public UIParent(object ownerWindow)
        {
            CoreUIParent = new CoreUIParent(ownerWindow);
        }
#endif
#endif
    }
}