// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
#if __ANDROID_29__
using AndroidX.Browser.CustomTabs;
#else
using Android.Support.CustomTabs;
#endif
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.UI;
using Uri = Android.Net.Uri;

namespace Microsoft.Identity.Client.Platforms.Android.SystemWebview
{
    /// <summary>
    /// </summary>
    [Activity(Name = "microsoft.identity.client.AuthenticationActivity", Exported = false, TaskAffinity = "", ExcludeFromRecents = true)]
    [Preserve(AllMembers = true)]
    internal class AuthenticationActivity : Activity
    {
        internal static SystemWebViewRequest PendingRequest { get; set; }

        /// <summary>
        /// Default Constructor
        /// </summary>
        public AuthenticationActivity()
        { }

        // this is used to check if anything can open custom tabs.
        // Must use the classic support. Leaving the reference AndroidX intent
        private readonly string _customTabsServiceAction =
            "android.support.customtabs.action.CustomTabsService";

        private string _requestUrl;
        private bool _restarted;
        private SystemWebViewRequest _request;

        /// <summary>
        /// </summary>
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            _request = PendingRequest;
            if (!HasPendingRequest() || !_request.MatchesIntent(Intent))
            {
                global::Android.Util.Log.Warn("MSAL", "Ignoring a system browser launch that does not match the pending request.");
                Finish();
                return;
            }

            _requestUrl = _request.AuthorizationUri.AbsoluteUri;
            if (Intent.GetStringExtra(AndroidConstants.CustomTabRedirect) is not null)
            {
                ProcessResponse(Intent);
                return;
            }

            // If activity is killed by the OS, savedInstance will be the saved bundle.
            if (bundle != null)
            {
                _restarted = true;
                return;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="intent"></param>
        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            if (IsFinishing)
            {
                return;
            }

            if (!HasPendingRequest() || !_request.MatchesIntent(intent))
            {
                Finish();
                return;
            }

            ProcessResponse(intent);
        }

        private void ProcessResponse(Intent intent)
        {
            string url = intent?.GetStringExtra(AndroidConstants.CustomTabRedirect);
            if (!_request.IsValidRedirect(url))
            {
                _request.RequestContext.Logger.Warning("Ignoring a system browser response that does not match the pending request.");
                return;
            }

            CompleteRequest(AuthorizationResult.FromUri(url));
        }

        /// <summary>
        ///
        /// </summary>
        protected override void OnResume()
        {
            base.OnResume();

            if (IsFinishing)
            {
                return;
            }

            if (!HasPendingRequest())
            {
                Finish();
                return;
            }

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

                _request.RequestContext.Logger.Warning(
                    "Browser with custom tabs package not available. " +
                    "Launching with alternate browser. See https://aka.ms/msal-net-system-browsers for details.");

                try
                {
                    StartActivity(browserIntent);
                }
                catch (ActivityNotFoundException ex)
                {
                    throw new MsalClientException(
                           MsalError.AndroidActivityNotFound,
                           MsalErrorMessage.AndroidActivityNotFound, ex);
                }
            }
            else
            {
                _request.RequestContext.Logger.Info(() =>
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
            CompleteRequest(AuthorizationResult.FromStatus(AuthorizationStatus.UserCancel));
        }

        /**
         * Return the error back to caller.
         */
        private void CompleteRequest(AuthorizationResult result)
        {
            if (ReferenceEquals(PendingRequest, _request))
            {
                PendingRequest = null;
            }

            _request.RestoreCallerTask();
            Finish();
            _request.Completion.TrySetResult(result);
        }

        private bool HasPendingRequest()
        {
            if (_request is not null && !_request.Completion.Task.IsCompleted && ReferenceEquals(PendingRequest, _request))
            {
                return true;
            }

            global::Android.Util.Log.Warn("MSAL", "Ignoring system browser activity without a pending authentication request.");
            return false;
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
