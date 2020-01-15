// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Android.App;
using Android.Content;
using Android.OS;
using Microsoft.Identity.Client.Platforms.Android.SystemWebview;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// BrowserTabActivity to get the redirect with code from authorize endpoint. Intent filter has to be declared in the
    /// android manifest for this activity. When chrome custom tab is launched, and we're redirected back with the redirect
    /// uri (redirect_uri has to be unique across apps), the os will fire an intent with the redirect,
    /// and the BrowserTabActivity will be launched.
    /// </summary>
    //[Activity(Name = "microsoft.identity.client.BrowserTabActivity")]
    [CLSCompliant(false)]
    public class BrowserTabActivity : Activity
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="savedInstanceState"></param>
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Intent intent = new Intent(this, typeof (AuthenticationActivity));
            intent.PutExtra(AndroidConstants.CustomTabRedirect, Intent.DataString);
            intent.SetFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
            StartActivity(intent);
        }
    }
}
