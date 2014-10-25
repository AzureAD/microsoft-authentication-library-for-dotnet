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
using System.Drawing;

using MonoTouch.CoreFoundation;
using MonoTouch.UIKit;
using MonoTouch.Foundation;

namespace ADAL
{
    [Register("AuthenticationAgentUIViewController")]
    public class AuthenticationAgentUIViewController : UIViewController
    {
		UIWebView webView;

        private string url;
        private string callback;

        private ReturnCode callbackMethod;

        public delegate void ReturnCode(string result);

        public AuthenticationAgentUIViewController(string url, string callback, ReturnCode callbackMethod)
        {
            this.url = url;
            this.callback = callback;
            this.callbackMethod = callbackMethod;
        }

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			
			Title = "WebView";
			View.BackgroundColor = UIColor.White;

            webView = new UIWebView(View.Bounds);
		    webView.ShouldStartLoad = (wView, request, navType) =>
		    {
                if (request != null && request.Url.ToString().StartsWith(callback))
                {
                    callbackMethod(request.Url.ToString());
                    this.DismissViewController(true, null);
                    return false;
                }

                return true;
		    };

			View.AddSubview(webView);

			webView.LoadRequest (new NSUrlRequest (new NSUrl (this.url)));
			
			// if this is false, page will be 'zoomed in' to normal size
			//webView.ScalesPageToFit = true;
		}
    }
}