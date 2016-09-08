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

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// </summary>
    [Activity(Label = "Sign In")]
    [CLSCompliant(false)]
    public class AuthenticationAgentActivity : Activity
    {
        private readonly ISet<string> _chromePackages = new string[]
        {
            "com.android.chrome",
            "com.chrome.beta",
            "com.chrome.dev"
        }.CreateSetFromArray();

        private string _requestUrl;
        private int _requestId;
        private bool _restarted;

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

        /**
         * OnNewIntent will be called before onResume.
         * @param intent
         */

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            string url = intent.GetStringExtra(AndroidConstants.CUSTOM_TAB_REDIRECT);

            Intent resultIntent = new Intent();
            resultIntent.PutExtra(AndroidConstants.AUTHORIZATION_FINAL_URL, url);
            ReturnToCaller(AndroidConstants.AuthCodeReceived,
                resultIntent);
        }


        protected override void OnResume()
        {
            base.OnResume();

            if (_restarted)
            {
                cancelRequest();
                return;
            }

            _restarted = true;

            string chromePackageWithCustomTabSupport = GetChromePackageWithCustomTabSupport(ApplicationContext);
            _requestUrl = Intent.GetStringExtra(AndroidConstants.RequestUrlKey);

            if (chromePackageWithCustomTabSupport != null)
            {
                CustomTabsIntent customTabsIntent = new CustomTabsIntent.Builder().Build();
                customTabsIntent.Intent.SetPackage(GetChromePackageWithCustomTabSupport(this));
                customTabsIntent.LaunchUrl(this, Android.Net.Uri.Parse(_requestUrl));
            }
            else
            {
                //TODO throw chrome tab missing exception
            }
        }

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
            SetResult(resultCode, data);
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

        ///<summary>
        /// Check if the chrome package with custom tab support is available on the device, 
        /// and return the package name if available.
        /// </summary>
        private string GetChromePackageWithCustomTabSupport(Context context)
        {
            if (context.PackageManager == null)
            {
                return null;
            }

            Intent customTabServiceIntent = new Intent("android.support.customtabs.action.CustomTabsService");
            IList<ResolveInfo> resolveInfoList = context.PackageManager.QueryIntentServices(
                customTabServiceIntent, 0);

            // queryIntentServices could return null or an empty list if no matching service existed.
            if (resolveInfoList == null || resolveInfoList.Count == 0)
            {
                // TODO: add logs
                return null;
            }

            foreach (ResolveInfo resolveInfo in resolveInfoList)
            {
                ServiceInfo serviceInfo = resolveInfo.ServiceInfo;
                if (serviceInfo != null && _chromePackages.Contains(serviceInfo.PackageName))
                {
                    return serviceInfo.PackageName;
                }
            }

            return null;
        }
    }
}