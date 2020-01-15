// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Foundation;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Platforms.Shared.Apple;
using Microsoft.Identity.Client.UI;
using UIKit;
using WebKit;

namespace Microsoft.Identity.Client.Platforms.iOS.EmbeddedWebview
{
    [Foundation.Register("MsalAuthenticationAgentUIViewController")]
    internal class MsalAuthenticationAgentUIViewController : UIViewController
    {
        private readonly string _url;
        private WKWebView _wkWebView;

        public MsalAuthenticationAgentUIViewController(string url, string callback, ReturnCodeCallback callbackMethod)
        {
            _url = url;
            Callback = callback;
            CallbackMethod = callbackMethod;
            NSUrlProtocol.RegisterClass(new ObjCRuntime.Class(typeof(CoreCustomUrlProtocol)));
        }

        public delegate void ReturnCodeCallback(AuthorizationResult result);

        public ReturnCodeCallback CallbackMethod { get; }

        public string Callback { get; }


        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            View.BackgroundColor = UIColor.White;

            _wkWebView = PrepareWKWebView();

            EvaluateJava();

            this.View.AddSubview(_wkWebView);

            this.NavigationItem.LeftBarButtonItem = new UIBarButtonItem(UIBarButtonSystemItem.Cancel,
                CancelAuthentication);

            _wkWebView.LoadRequest(new NSUrlRequest(new NSUrl(this._url)));
        }

        protected WKWebView PrepareWKWebView()
        {
            WKWebViewConfiguration wkconfg = new WKWebViewConfiguration() { };

            _wkWebView = new WKWebView(View.Bounds, wkconfg)
            {
                UIDelegate = new WKWebNavigationDelegate.WKWebViewUIDelegate(this),
                NavigationDelegate = new WKWebNavigationDelegate(this),
                AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight
            };

            return _wkWebView;
        }

        private void EvaluateJava()
        {
            WKJavascriptEvaluationResult handler = HandleWKJavascriptEvaluationResult;

            _wkWebView.EvaluateJavaScript((NSString)@"navigator.userAgent", handler);
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
                    CallbackMethod(AuthorizationResult.FromStatus(AuthorizationStatus.UserCancel)));
        }

        public override void DismissViewController(bool animated, Action completionHandler)
        {
            NSUrlProtocol.UnregisterClass(new ObjCRuntime.Class(typeof(CoreCustomUrlProtocol)));
            base.DismissViewController(animated, completionHandler);
        }
    }
}
