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
using Android.OS;
using Android.Webkit;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    [Activity(Label = "Sign in")]
    [CLSCompliant(false)]
    internal class AuthenticationAgentActivity : Activity
    {
        private AdalWebViewClient client;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Create your application here

            SetContentView(Resource.Layout.WebAuthenticationBroker);

            string url = Intent.GetStringExtra("Url");

            WebView webView = FindViewById<WebView>(Resource.Id.agentWebView);
            WebSettings webSettings = webView.Settings;
            string userAgent = webSettings.UserAgentString;
            webSettings.UserAgentString = 
                    userAgent + BrokerConstants.ClientTlsNotSupported;
            PlatformPlugin.Logger.Verbose(null, "UserAgent:" + webSettings.UserAgentString);

            webSettings.JavaScriptEnabled = true;

            webSettings.LoadWithOverviewMode = true;
            webSettings.DomStorageEnabled = true;
            webSettings.UseWideViewPort = true;
            webSettings.BuiltInZoomControls = true;

            this.client = new AdalWebViewClient(Intent.GetStringExtra("Callback"));
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
                this.SetResult(Result.Canceled, new Intent("Return"));
            }
            base.Finish();
        }

        sealed class AdalWebViewClient : WebViewClient
        {
            private readonly string callback;

            public AdalWebViewClient(string callback)
            {
                this.callback = callback;
            }

            public Intent ReturnIntent { get; private set; }

            public override void OnLoadResource(WebView view, string url)
            {
                base.OnLoadResource(view, url);

                if (url.StartsWith(callback))
                {
                    base.OnLoadResource(view, url);
                    this.Finish(view, url);
                }
            }

            public override bool ShouldOverrideUrlLoading(WebView view, string url)
            {
                Uri uri = new Uri(url);
                if (url.StartsWith(BrokerConstants.BrowserExtPrefix))
                {
                    PlatformPlugin.Logger.Verbose(null, "It is browser launch request");
                    OpenLinkInBrowser(url, ((Activity)view.Context));
                    view.StopLoading();
                    ((Activity)view.Context).Finish();
                    return true;
                }

                if (url.StartsWith(BrokerConstants.BrowserExtInstallPrefix))
                {
                    PlatformPlugin.Logger.Verbose(null, "It is an azure authenticator install request");
                    view.StopLoading();
                    this.Finish(view, url);
                    return true;
                }

                if (url.StartsWith(BrokerConstants.ClientTlsRedirect, StringComparison.CurrentCultureIgnoreCase))
                {
                    string query = uri.Query;
                    if (query.StartsWith("?"))
                    {
                        query = query.Substring(1);
                    }

                    Dictionary<string, string> keyPair = EncodingHelper.ParseKeyValueList(query, '&', true, false, null);
                    string responseHeader = PlatformPlugin.DeviceAuthHelper.CreateDeviceAuthChallengeResponse(keyPair).Result;
                    Dictionary<string, string> pkeyAuthEmptyResponse = new Dictionary<string, string>();
                    pkeyAuthEmptyResponse[BrokerConstants.ChallangeResponseHeader] = responseHeader;
                    view.LoadUrl(keyPair["SubmitUrl"], pkeyAuthEmptyResponse);
                    return true;
                }

                if (url.StartsWith(callback, StringComparison.OrdinalIgnoreCase))
                {
                    this.Finish(view, url);
                    return true;
                }


                if (!url.Equals("about:blank", StringComparison.CurrentCultureIgnoreCase) && !uri.Scheme.Equals("https", StringComparison.CurrentCultureIgnoreCase))
                {
                    UriBuilder errorUri = new UriBuilder(callback);
                    errorUri.Query = string.Format("error={0}&error_description={1}",
                        AdalError.NonHttpsRedirectNotSupported, AdalErrorMessage.NonHttpsRedirectNotSupported);
                    this.Finish(view, errorUri.ToString());
                    return true;
                }


                return false;
            }

            private void OpenLinkInBrowser(string url, Activity activity)
            {
                String link = url
                        .Replace(BrokerConstants.BrowserExtPrefix, "https://");
                Intent intent = new Intent(Intent.ActionView, Android.Net.Uri.Parse(link));
                activity.StartActivity(intent);
            }

            public override void OnPageFinished(WebView view, string url)
            {
                if (url.StartsWith(callback, StringComparison.OrdinalIgnoreCase))
                {
                    base.OnPageFinished(view, url);
                    this.Finish(view, url);
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

            private void Finish(WebView view, string url)
            {
                var activity = ((Activity)view.Context);
                if (activity != null && !activity.IsFinishing)
                {
                    this.ReturnIntent = new Intent("Return");
                    this.ReturnIntent.PutExtra("ReturnedUrl", url);
                    ((Activity)view.Context).Finish();
                }
            }

        }
    }
}
