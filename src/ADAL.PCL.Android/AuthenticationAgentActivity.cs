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
using Android.App;
using Android.Content;
using Android.OS;
using Android.Webkit;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    [Activity(Label = "Sign in")]
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
                }
            }

            public override bool ShouldOverrideUrlLoading(WebView view, String url)
            {
                return false;
            }

            public override void OnPageFinished(WebView view, string url)
            {
                if (url.StartsWith(callback))
                {
                    base.OnPageFinished(view, url);
                    this.ReturnIntent = new Intent("Return");
                    this.ReturnIntent.PutExtra("ReturnedUrl", url);
                    ((Activity)view.Context).Finish();
                }

                base.OnPageFinished(view, url);
            }

            public override void OnPageStarted(WebView view, string url, Android.Graphics.Bitmap favicon)
            {
                if (url.StartsWith(callback))
                {
                    base.OnPageStarted(view, url, favicon);
                }

                base.OnPageStarted(view, url, favicon);
            }
        }
    }
}