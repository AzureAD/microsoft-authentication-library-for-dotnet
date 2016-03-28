//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

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
    public class AuthenticationAgentActivity : Activity
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