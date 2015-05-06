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
using CoreGraphics;

using CoreFoundation;
using UIKit;
using Foundation;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    [Foundation.Register("AuthenticationAgentUIViewController")]
    internal class AuthenticationAgentUIViewController : UIViewController
    {
		UIWebView webView;

        private string url;
        private string callback;

        private ReturnCodeCallback callbackMethod;

        public delegate void ReturnCodeCallback(AuthorizationResult result);

        public AuthenticationAgentUIViewController(string url, string callback, ReturnCodeCallback callbackMethod)
        {
            this.url = url;
            this.callback = callback;
            this.callbackMethod = callbackMethod;
        }

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();

            View.BackgroundColor = UIColor.White;

            webView = new UIWebView((CGRect)View.Bounds);
		    webView.ShouldStartLoad = (wView, request, navType) =>
		    {
		        if (request == null)
		        {
		            return true;
		        }

		        string requestUrl = request.Url.ToString().ToLower();
                if (requestUrl.StartsWith(callback.ToLower()))
                {
                    callbackMethod(new AuthorizationResult(AuthorizationStatus.Success, request.Url.ToString()));
                    this.DismissViewController(true, null);
                    return false;
                }

                return true;
		    };

		    webView.LoadFinished += delegate
		    {
                // If the title is too long, iOS automatically truncates it and adds ...
                this.Title = webView.EvaluateJavascript(@"document.title") ?? "Sign in";
		    };

			View.AddSubview(webView);

            this.NavigationItem.LeftBarButtonItem = new UIBarButtonItem(UIBarButtonSystemItem.Cancel, this.CancelAuthentication);

			webView.LoadRequest (new NSUrlRequest (new NSUrl (this.url)));
			
			// if this is false, page will be 'zoomed in' to normal size
			//webView.ScalesPageToFit = true;
		}

        private void CancelAuthentication(object sender, EventArgs e)
        {
            callbackMethod(new AuthorizationResult(AuthorizationStatus.UserCancel, null));
            this.DismissViewController(true, null);
        }
    }
}