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
using Foundation;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Platforms.AppleShared;
using Microsoft.Identity.Client.UI;
using UIKit;
using WebKit;

namespace Microsoft.Identity.Client.Platforms.iOS.EmbeddedWebview
{
    [Foundation.Register("MsalAuthenticationAgentUIViewController")]
    internal class MsalAuthenticationAgentUIViewController : UIViewController
    {
        private readonly string url;
        public readonly string callback;
        private WKWebView wkWebView;

        public readonly ReturnCodeCallback callbackMethod;

        public delegate void ReturnCodeCallback(AuthorizationResult result);

        public MsalAuthenticationAgentUIViewController(string url, string callback, ReturnCodeCallback callbackMethod)
        {
            this.url = url;
            this.callback = callback;
            this.callbackMethod = callbackMethod;
            NSUrlProtocol.RegisterClass(new ObjCRuntime.Class(typeof(CoreCustomUrlProtocol)));
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            View.BackgroundColor = UIColor.White;

            wkWebView = PrepareWKWebView();

            EvaluateJava();

            this.View.AddSubview(wkWebView);

            this.NavigationItem.LeftBarButtonItem = new UIBarButtonItem(UIBarButtonSystemItem.Cancel,
                CancelAuthentication);

            wkWebView.LoadRequest(new NSUrlRequest(new NSUrl(this.url)));
        }

        protected WKWebView PrepareWKWebView()
        {
            WKWebViewConfiguration wkconfg = new WKWebViewConfiguration() { };

            wkWebView = new WKWebView(View.Bounds, wkconfg)
            {
                UIDelegate = new WKWebNavigationDelegate.WKWebViewUIDelegate(this),
                NavigationDelegate = new WKWebNavigationDelegate(this),
                AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight
            };

            return wkWebView;
        }

        private void EvaluateJava()
        {
            WKJavascriptEvaluationResult handler = HandleWKJavascriptEvaluationResult;

            wkWebView.EvaluateJavaScript((NSString)@"navigator.userAgent", handler);
        }

        private static void HandleWKJavascriptEvaluationResult(NSObject result, NSError err)
        {
            if (err != null)
            {
                MsalLogger.Default.Info(err.LocalizedDescription);
            }
            if (result != null)
            {
                MsalLogger.Default.Info(result.ToString());
            }
            return;
        }

        public void CancelAuthentication(object sender, EventArgs e)
        {
            this.DismissViewController(true, () =>
                    callbackMethod(new AuthorizationResult(AuthorizationStatus.UserCancel, null)));
        }

        public override void DismissViewController(bool animated, Action completionHandler)
        {
            NSUrlProtocol.UnregisterClass(new ObjCRuntime.Class(typeof(CoreCustomUrlProtocol)));
            base.DismissViewController(animated, completionHandler);
        }
    }
}
