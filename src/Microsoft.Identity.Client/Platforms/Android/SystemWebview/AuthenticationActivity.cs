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
using System.Globalization;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Support.CustomTabs;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Exceptions;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.OAuth2;
using Uri = Android.Net.Uri;

namespace Microsoft.Identity.Client.Platforms.Android.SystemWebview
{
    /// <summary>
    /// </summary>
    [Activity(Name = "microsoft.identity.client.AuthenticationActivity")]
    [global::Android.Runtime.Preserve(AllMembers = true)]
    internal class AuthenticationActivity : Activity
    {
        internal static RequestContext RequestContext { get; set; }

        /// <summary>
        /// Default Constructor
        /// </summary>
        public AuthenticationActivity()
        { }

        private readonly string _customTabsServiceAction =
            "android.support.customtabs.action.CustomTabsService";

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
                SendError(
                    MsalError.UnresolvableIntentError,
                    "Received null data intent from caller");
                return;
            }

            _requestUrl = Intent.GetStringExtra(AndroidConstants.RequestUrlKey);
            _requestId = Intent.GetIntExtra(AndroidConstants.RequestId, 0);
            if (string.IsNullOrEmpty(_requestUrl))
            {
                SendError(MsalError.InvalidRequest, "Request url is not set on the intent");
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

            string chromePackageWithCustomTabSupport = GetChromePackageWithCustomTabSupport(ApplicationContext);

            if (string.IsNullOrEmpty(chromePackageWithCustomTabSupport))
            {
                Intent browserIntent = new Intent(Intent.ActionView, Uri.Parse(_requestUrl));
                browserIntent.AddCategory(Intent.CategoryBrowsable);

                RequestContext.Logger.Warning(
                    "Browser with custom tabs package not available. " +
                    "Launching with alternate browser. See https://aka.ms/msal-net-system-browsers for details.");

                try
                {
                    StartActivity(browserIntent);
                }
                catch (ActivityNotFoundException ex)
                {
                    throw MsalExceptionFactory.GetClientException(
                           MsalError.AndroidActivityNotFound,
                           MsalErrorMessage.AndroidActivityNotFound, ex);
                }
            }
            else
            {
                RequestContext.Logger.Info(
                    string.Format(
                    CultureInfo.CurrentCulture,
                    "Browser with custom tabs package available. Using {0}. ",
                    chromePackageWithCustomTabSupport));

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
            SetResult((Result)resultCode, data);
            Finish();
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

            IEnumerable<ResolveInfo> resolveInfoListWithCustomTabs = context.PackageManager.QueryIntentServices(
                    customTabServiceIntent, PackageInfoFlags.MatchAll);

            // queryIntentServices could return null or an empty list if no matching service existed.
            if (resolveInfoListWithCustomTabs == null || !resolveInfoListWithCustomTabs.Any())
            {
                return null;
            }

            foreach (ResolveInfo resolveInfo in resolveInfoListWithCustomTabs)
            {
                ServiceInfo serviceInfo = resolveInfo.ServiceInfo;
                if (serviceInfo != null)
                {
                    return serviceInfo.PackageName;
                }
            }

            return null;
        }
    }
}