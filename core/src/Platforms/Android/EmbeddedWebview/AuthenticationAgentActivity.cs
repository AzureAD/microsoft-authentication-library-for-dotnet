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
using Android.App;
using Android.Content;
using Android.OS;
using Android.Webkit;
using Android.Widget;
using Microsoft.Identity.Core.Helpers;

namespace Microsoft.Identity.Core.UI.EmbeddedWebview
{
    [Activity(Label = "Sign in")]
    [CLSCompliant(false)]
#pragma warning disable CS3019 // CLS compliance checking will not be performed because it is not visible from outside this assembly
    internal class AuthenticationAgentActivity : Activity
#pragma warning restore CS3019 // CLS compliance checking will not be performed because it is not visible from outside this assembly
    {
        private const string AboutBlankUri = "about:blank";

        private CoreWebViewClient client;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            // Create your application here

            WebView webView = new WebView(ApplicationContext);
            var linearLayout = new LinearLayout(ApplicationContext)
            {
                Orientation = Orientation.Vertical
            };
            linearLayout.AddView(webView);
            SetContentView(linearLayout);

            string url = Intent.GetStringExtra("Url");
            WebSettings webSettings = webView.Settings;
            string userAgent = webSettings.UserAgentString;
            webSettings.UserAgentString = userAgent + BrokerConstants.ClientTlsNotSupported;
            CoreLoggerBase.Default.Verbose("UserAgent:" + webSettings.UserAgentString);

            webSettings.JavaScriptEnabled = true;

            webSettings.LoadWithOverviewMode = true;
            webSettings.DomStorageEnabled = true;
            webSettings.UseWideViewPort = true;
            webSettings.BuiltInZoomControls = true;

            this.client = new CoreWebViewClient(Intent.GetStringExtra("Callback"), this);
            webView.SetWebViewClient(client);
            webView.LoadUrl(url);
        }

        public override void Finish()
        {
            if (this.client.ReturnIntent != null)
            {
                this.SetResult(Result.Ok, this.client.ReturnIntent);
            }
            else
            {
                this.SetResult(Result.Canceled, new Intent("ReturnFromEmbeddedWebview"));
            }
            base.Finish();
        }

        sealed class CoreWebViewClient : WebViewClient
        {
            private readonly string callback;
            private Activity Activity { get; set; }

            public CoreWebViewClient(string callback, Activity activity)
            {
                this.callback = callback;
                this.Activity = activity;
            }

            public Intent ReturnIntent { get; private set; }

            public override void OnLoadResource(WebView view, string url)
            {
                base.OnLoadResource(view, url);

                if (url.StartsWith(callback, StringComparison.OrdinalIgnoreCase))
                {
                    base.OnLoadResource(view, url);
                    this.Finish(Activity, url);
                }

            }

            [Obsolete]
            public override bool ShouldOverrideUrlLoading(WebView view, string url)
            {
                Uri uri = new Uri(url);
                if (url.StartsWith(BrokerConstants.BrowserExtPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    CoreLoggerBase.Default.Verbose("It is browser launch request");
                    OpenLinkInBrowser(url, Activity);
                    view.StopLoading();
                    Activity.Finish();
                    return true;
                }

                if (url.StartsWith(BrokerConstants.BrowserExtInstallPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    CoreLoggerBase.Default.Verbose("It is an azure authenticator install request");
                    view.StopLoading();
                    this.Finish(Activity, url);
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
                    string responseHeader = DeviceAuthHelper.CreateDeviceAuthChallengeResponse(keyPair).Result;
                    Dictionary<string, string> pkeyAuthEmptyResponse = new Dictionary<string, string>();
                    pkeyAuthEmptyResponse[BrokerConstants.ChallangeResponseHeader] = responseHeader;
                    view.LoadUrl(keyPair["SubmitUrl"], pkeyAuthEmptyResponse);
                    return true;
                }

                if (url.StartsWith(callback, StringComparison.OrdinalIgnoreCase))
                {
                    this.Finish(Activity, url);
                    return true;
                }


                if (!url.Equals(AboutBlankUri, StringComparison.OrdinalIgnoreCase) && !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                {
                    UriBuilder errorUri = new UriBuilder(callback);
                    errorUri.Query = string.Format(CultureInfo.InvariantCulture, "error={0}&error_description={1}",
                        MsalError.NonHttpsRedirectNotSupported, MsalErrorMessage.NonHttpsRedirectNotSupported);
                    this.Finish(Activity, errorUri.ToString());
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

                String link = externalBrowserUrlBuilder.Uri.AbsoluteUri;
                Intent intent = new Intent(Intent.ActionView, Android.Net.Uri.Parse(link));
                activity.StartActivity(intent);
            }

            public override void OnPageFinished(WebView view, string url)
            {
                if (url.StartsWith(callback, StringComparison.OrdinalIgnoreCase))
                {
                    base.OnPageFinished(view, url);
                    this.Finish(Activity, url);
                }

                base.OnPageFinished(view, url);
            }

            public override void OnPageStarted(WebView view, string url, Android.Graphics.Bitmap favicon)
            {
                if (url.StartsWith(callback, StringComparison.OrdinalIgnoreCase))
                {
                    base.OnPageStarted(view, url, favicon);
                }

                base.OnPageStarted(view, url, favicon);
            }

            private void Finish(Activity activity, string url)
            {
                this.ReturnIntent = new Intent("ReturnFromEmbeddedWebview");
                this.ReturnIntent.PutExtra("ReturnedUrl", url);
                activity.Finish();
            }
        }
    }
}
