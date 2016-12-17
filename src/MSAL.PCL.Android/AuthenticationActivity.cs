//------------------------------------------------------------------------------
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
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Support.CustomTabs;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.OAuth2;
using Uri = Android.Net.Uri;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// </summary>
    [Activity(Label = "Sign In")]
    [CLSCompliant(false)]
    [Android.Runtime.Preserve(AllMembers = true)]
    public class AuthenticationActivity : Activity
    {
        private readonly string _customTabsServiceAction =
            "android.support.customtabs.action.CustomTabsService";
        private readonly string[] _chromePackages =
        {"com.android.chrome", "com.chrome.beta", "com.chrome.dev"};

        private string _requestUrl;
        private int _requestId;
        private bool _restarted;
       // private CustomTabsActivityManager _customTabsActivityManager;

        /// <summary>
        /// </summary>
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // If activity is killed by the os, savedInstance will be the saved bundle.
            if (bundle != null)
            {
                _restarted = true;
                return;
            }

            if (Intent == null)
            {
                SendError(MsalErrorAndroidEx.InvalidRequest, "Received null data intent from caller");
                return;
            }

            _requestUrl = Intent.GetStringExtra(AndroidConstants.RequestUrlKey);
            _requestId = Intent.GetIntExtra(AndroidConstants.RequestId, 0);
            if (string.IsNullOrEmpty(_requestUrl))
            {
                SendError(MsalErrorAndroidEx.InvalidRequest, "Request url is not set on the intent");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="intent"></param>
        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            string url = intent.GetStringExtra(AndroidConstants.CustomTabRedirect);

            Intent resultIntent = new Intent();
            resultIntent.PutExtra(AndroidConstants.AuthorizationFinalUrl, url);
            ReturnToCaller(AndroidConstants.AuthCodeReceived,
                resultIntent);
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void OnResume()
        {
            base.OnResume();

            if (_restarted)
            {
                cancelRequest();
                return;
            }

            _restarted = true;

            /*_customTabsActivityManager = new CustomTabsActivityManager(this);
            _customTabsActivityManager.CustomTabsServiceConnected += delegate { _customTabsActivityManager.LaunchUrl(_requestUrl); };*/
            string chromePackageWithCustomTabSupport = GetChromePackageWithCustomTabSupport(ApplicationContext);

            if (string.IsNullOrEmpty(chromePackageWithCustomTabSupport))
            {
                string chromePackage = GetChromePackage();
                if (string.IsNullOrEmpty(chromePackage))
                {
                    throw new MsalException(MsalErrorAndroidEx.ChromeNotInstalled,
                        "Chrome is not installed on the device, cannot proceed with auth");
                }

                Intent browserIntent = new Intent(Intent.ActionView);
                browserIntent.SetData(Uri.Parse(_requestUrl));
                browserIntent.SetPackage(chromePackage);
                browserIntent.AddCategory(Intent.CategoryBrowsable);
                StartActivity(browserIntent);
            }
            else
            {
                CustomTabsIntent customTabsIntent = new CustomTabsIntent.Builder().Build();
                customTabsIntent.Intent.SetPackage(chromePackageWithCustomTabSupport);
                customTabsIntent.LaunchUrl(this, Uri.Parse(_requestUrl));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="outState"></param>
        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            outState.PutString(AndroidConstants.RequestUrlKey, _requestUrl);
        }

        /**
         * Cancels the auth request.
         */

        private void cancelRequest()
        {
            ReturnToCaller(AndroidConstants.Cancel, new Intent());
        }

        /**
         * Return the error back to caller.
         * @param resultCode The result code to return back.
         * @param data {@link Intent} contains the detailed result.
         */

        private void ReturnToCaller(int resultCode, Intent data)
        {
            data.PutExtra(AndroidConstants.RequestId, _requestId);
            SetResult((Result) resultCode, data);
            this.Finish();
        }

        /**
         * Send error back to caller with the error description.
         * @param errorCode The error code to send back.
         * @param errorDescription The error description to send back.
         */

        private void SendError(string errorCode, string errorDescription)
        {
            Intent errorIntent = new Intent();
            errorIntent.PutExtra(OAuth2ResponseBaseClaim.Error, errorCode);
            errorIntent.PutExtra(OAuth2ResponseBaseClaim.ErrorDescription, errorDescription);
            ReturnToCaller(AndroidConstants.AuthCodeError, errorIntent);
        }

        private string GetChromePackageWithCustomTabSupport(Context context)
        {
            if (context.PackageManager == null)
            {
                return null;
            }

            Intent customTabServiceIntent = new Intent(_customTabsServiceAction);
            IList< ResolveInfo > resolveInfoList = context.PackageManager.QueryIntentServices(
                    customTabServiceIntent, 0);

            // queryIntentServices could return null or an empty list if no matching service existed.
            if (resolveInfoList == null || resolveInfoList.Count == 0)
            {
                return null;
            }

            ISet<string> chromePackage = new HashSet<string>(_chromePackages.CreateSetFromArray());
            foreach (ResolveInfo resolveInfo in resolveInfoList)
            {
                ServiceInfo serviceInfo = resolveInfo.ServiceInfo;
                if (serviceInfo != null && chromePackage.Contains(serviceInfo.PackageName))
                {
                    return serviceInfo.PackageName;
                }
            }

            return null;
        }


        private string GetChromePackage()
        {
            PackageManager packageManager = ApplicationContext.PackageManager;
            if (packageManager == null)
            {
                return null;
            }

            string installedChromePackage = null;
            for (int i = 0; i < _chromePackages.Length; i++)
            {
                try
                {
                    packageManager.GetPackageInfo(_chromePackages[i], PackageInfoFlags.Activities);
                    installedChromePackage = _chromePackages[i];
                    break;
                }
                catch (PackageManager.NameNotFoundException exc)
                {
                    PlatformPlugin.Logger.Error(null, exc);
                    // swallow this exception. If the package does not exist then exception will be thrown.
                }
            }

            return installedChromePackage;
        }
    }
}