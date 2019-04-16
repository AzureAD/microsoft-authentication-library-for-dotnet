// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
                // TODO(migration): figure out how to get logger into this class: MsalLogger.Default.Info(err.LocalizedDescription);
            }
            if (result != null)
            {
                // TODO(migration): figure out how to get logger into this class: MsalLogger.Default.Info(result.ToString());
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
