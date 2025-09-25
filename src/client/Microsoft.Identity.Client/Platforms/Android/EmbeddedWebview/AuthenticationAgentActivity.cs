// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Webkit;
using Android.Widget;
using Microsoft.Identity.Client.PlatformsCommon;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.Utils;
#if __ANDROID_30__
using AndroidX.Core.View;
using AndroidX.Core.Graphics;
#endif

namespace Microsoft.Identity.Client.Platforms.Android.EmbeddedWebview
{
    [Activity(ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize, Exported=false)]
    internal class AuthenticationAgentActivity : Activity
    {
        private const string AboutBlankUri = "about:blank";
        private CoreWebViewClient _client;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            
            // Enable edge-to-edge for Android API 30+
            EnableEdgeToEdge();
            
            // Create your application here
            WebView webView = new WebView(this);
            var relativeLayout = new RelativeLayout(this);
            webView.LayoutParameters = new RelativeLayout.LayoutParams(RelativeLayout.LayoutParams.MatchParent, RelativeLayout.LayoutParams.MatchParent);

            relativeLayout.AddView(webView);
            SetContentView(relativeLayout);
            
            // Apply window insets for edge-to-edge layout
            ApplyWindowInsets(relativeLayout, webView);

            string url = Intent.GetStringExtra("Url");

            WebSettings webSettings = webView.Settings;
            string userAgent = webSettings.UserAgentString;
            webSettings.UserAgentString = userAgent + BrokerConstants.ClientTlsNotSupported;

            webSettings.JavaScriptEnabled = true;
            webSettings.LoadWithOverviewMode = true;
            webSettings.DomStorageEnabled = true;
            webSettings.UseWideViewPort = true;
            webSettings.BuiltInZoomControls = true;            

            _client = new CoreWebViewClient(Intent.GetStringExtra("Callback"), this);
            webView.SetWebViewClient(_client);
            webView.LoadUrl(url);
        }

        private void EnableEdgeToEdge()
        {
#if __ANDROID_30__
            if (Build.VERSION.SdkInt >= BuildVersionCodes.R) // API 30+
            {
                // Enable edge-to-edge
                Window.SetDecorFitsSystemWindows(false);
                
                // For API 35+, ensure proper edge-to-edge behavior
                if (Build.VERSION.SdkInt >= BuildVersionCodes.VanillaIceCream) // API 35
                {
                    // Additional API 35 specific configurations
                    Window.StatusBarColor = global::Android.Graphics.Color.Transparent;
                    Window.NavigationBarColor = global::Android.Graphics.Color.Transparent;
                }
            }
#endif
        }

        private void ApplyWindowInsets(RelativeLayout parentLayout, WebView webView)
        {
#if __ANDROID_30__
            if (Build.VERSION.SdkInt >= BuildVersionCodes.R) // API 30+
            {
                ViewCompat.SetOnApplyWindowInsetsListener(parentLayout, (v, insets) =>
                {
                    var systemBarsInsets = insets.GetInsets(WindowInsetsCompat.Type.SystemBars());
                    var imeInsets = insets.GetInsets(WindowInsetsCompat.Type.Ime());
                    
                    // Apply padding to avoid system UI overlap
                    var layoutParams = webView.LayoutParameters as RelativeLayout.LayoutParams;
                    if (layoutParams != null)
                    {
                        layoutParams.SetMargins(
                            systemBarsInsets.Left,
                            systemBarsInsets.Top,
                            systemBarsInsets.Right,
                            Math.Max(systemBarsInsets.Bottom, imeInsets.Bottom)
                        );
                        webView.LayoutParameters = layoutParams;
                    }
                    
                    return WindowInsetsCompat.Consumed;
                });
            }
#endif
        }

        public override void Finish()
        {
            if (_client.ReturnIntent != null)
            {
                SetResult(Result.Ok, _client.ReturnIntent);
            }
            else
            {
                SetResult(Result.Canceled, new Intent("ReturnFromEmbeddedWebview"));
            }
            base.Finish();
        }

        private sealed class CoreWebViewClient : WebViewClient
        {
            private readonly string _callback;
            private Activity Activity { get; set; }

            public CoreWebViewClient(string callback, Activity activity)
            {
                _callback = callback;
                Activity = activity;
            }

            public Intent ReturnIntent { get; private set; }

            public override void OnLoadResource(WebView view, string url)
            {
                base.OnLoadResource(view, url);

                if (url.StartsWith(_callback, StringComparison.OrdinalIgnoreCase))
                {
                    base.OnLoadResource(view, url);
                    Finish(Activity, url);
                }

            }

            [Obsolete] // because parent is obsolete
#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
            public override bool ShouldOverrideUrlLoading(WebView view, string url)
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member
            {
                Uri uri = new Uri(url);
                if (url.StartsWith(BrokerConstants.BrowserExtPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    OpenLinkInBrowser(url, Activity);
                    view.StopLoading();
                    Activity.Finish();
                    return true;
                }

                if (url.StartsWith(BrokerConstants.BrowserExtInstallPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    view.StopLoading();
                    Finish(Activity, url);
                    return true;
                }

                if (url.StartsWith(_callback, StringComparison.OrdinalIgnoreCase))
                {
                    Finish(Activity, url);
                    return true;
                }

                if (url.StartsWith(BrokerConstants.ClientTlsRedirect, StringComparison.OrdinalIgnoreCase))
                {
                    string query = uri.Query;
                    if (query.StartsWith("?", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Substring(1);
                    }

                    Dictionary<string, string> keyPair = CoreHelpers.ParseKeyValueList(query, '&', true, false, null);
                    string responseHeader = DeviceAuthHelper.GetBypassChallengeResponse(keyPair);
                    Dictionary<string, string> pkeyAuthEmptyResponse = new Dictionary<string, string>();
                    pkeyAuthEmptyResponse[BrokerConstants.ChallengeResponseHeader] = responseHeader;

                    view.LoadUrl(keyPair["SubmitUrl"], pkeyAuthEmptyResponse);

                    return true;
                }

                if (!url.Equals(AboutBlankUri, StringComparison.OrdinalIgnoreCase) && 
                    !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                {
                    UriBuilder errorUri = new UriBuilder(_callback)
                    {
                        Query = string.Format(
                            CultureInfo.InvariantCulture,
                            "error={0}&error_description={1}",
                            MsalError.NonHttpsRedirectNotSupported,
                            MsalErrorMessage.NonHttpsRedirectNotSupported + " - " + CoreHelpers.UrlEncode(uri.AbsoluteUri))
                    };
                    Finish(Activity, errorUri.ToString());
                    return true;
                }

                return false;
            }

            private void OpenLinkInBrowser(string url, Activity activity)
            {
                // Construct URL to launch external browser (use HTTPS)
                var externalBrowserUrlBuilder = new UriBuilder(url)
                {
                    Scheme = Uri.UriSchemeHttps
                };

                string link = externalBrowserUrlBuilder.Uri.AbsoluteUri;
                Intent intent = new Intent(Intent.ActionView, global::Android.Net.Uri.Parse(link));
                activity.StartActivity(intent);
            }

            public override void OnPageFinished(WebView view, string url)
            {
                if (url.StartsWith(_callback, StringComparison.OrdinalIgnoreCase))
                {
                    base.OnPageFinished(view, url);
                    Finish(Activity, url);
                }

                base.OnPageFinished(view, url);
            }

            public override void OnPageStarted(WebView view, string url, global::Android.Graphics.Bitmap favicon)
            {
                if (url.StartsWith(_callback, StringComparison.OrdinalIgnoreCase))
                {
                    base.OnPageStarted(view, url, favicon);
                }

                base.OnPageStarted(view, url, favicon);
            }

            private void Finish(Activity activity, string url)
            {
                ReturnIntent = new Intent("ReturnFromEmbeddedWebview");
                ReturnIntent.PutExtra("ReturnedUrl", url);
                activity.Finish();
            }
        }
    }
}
