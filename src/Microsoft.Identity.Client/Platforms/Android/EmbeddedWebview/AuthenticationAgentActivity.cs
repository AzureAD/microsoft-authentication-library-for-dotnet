// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Webkit;
using Android.Widget;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Platforms.Android.EmbeddedWebview
{
    [Activity(Label = "Sign in")]
    internal class AuthenticationAgentActivity : Activity
    {
        private const string AboutBlankUri = "about:blank";
        private CoreWebViewClient _client;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            // Create your application here

            WebView webView = new WebView(ApplicationContext);
            var relativeLayout = new RelativeLayout(ApplicationContext);
            webView.LayoutParameters = new RelativeLayout.LayoutParams(RelativeLayout.LayoutParams.MatchParent, RelativeLayout.LayoutParams.MatchParent);

            relativeLayout.AddView(webView);
            SetContentView(relativeLayout);

            string url = Intent.GetStringExtra("Url");
            WebSettings webSettings = webView.Settings;
            string userAgent = webSettings.UserAgentString;
            webSettings.UserAgentString = userAgent + BrokerConstants.ClientTlsNotSupported;
            // TODO(migration): Figure out how to get logger into this class.  MsalLogger.Default.Verbose("UserAgent:" + webSettings.UserAgentString);

            webSettings.JavaScriptEnabled = true;

            webSettings.LoadWithOverviewMode = true;
            webSettings.DomStorageEnabled = true;
            webSettings.UseWideViewPort = true;
            webSettings.BuiltInZoomControls = true;

            _client = new CoreWebViewClient(Intent.GetStringExtra("Callback"), this);
            webView.SetWebViewClient(_client);
            webView.LoadUrl(url);
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
            public override bool ShouldOverrideUrlLoading(WebView view, string url)
            {
                Uri uri = new Uri(url);
                if (url.StartsWith(BrokerConstants.BrowserExtPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    // TODO(migration): Figure out how to get logger into this class.  MsalLogger.Default.Verbose("It is browser launch request");
                    OpenLinkInBrowser(url, Activity);
                    view.StopLoading();
                    Activity.Finish();
                    return true;
                }

                if (url.StartsWith(BrokerConstants.BrowserExtInstallPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    // TODO(migration): Figure out how to get logger into this class.  MsalLogger.Default.Verbose("It is an azure authenticator install request");
                    view.StopLoading();
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
                    string responseHeader = DeviceAuthHelper.CreateDeviceAuthChallengeResponseAsync(keyPair).Result;
                    Dictionary<string, string> pkeyAuthEmptyResponse = new Dictionary<string, string>
                    {
                        [BrokerConstants.ChallangeResponseHeader] = responseHeader
                    };
                    view.LoadUrl(keyPair["SubmitUrl"], pkeyAuthEmptyResponse);
                    return true;
                }

                if (url.StartsWith(_callback, StringComparison.OrdinalIgnoreCase))
                {
                    Finish(Activity, url);
                    return true;
                }

                if (!url.Equals(AboutBlankUri, StringComparison.OrdinalIgnoreCase) && !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                {
                    UriBuilder errorUri = new UriBuilder(_callback)
                    {
                        Query = string.Format(
                            CultureInfo.InvariantCulture,
                            "error={0}&error_description={1}",
                            MsalError.NonHttpsRedirectNotSupported,
                            MsalErrorMessage.NonHttpsRedirectNotSupported)
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
