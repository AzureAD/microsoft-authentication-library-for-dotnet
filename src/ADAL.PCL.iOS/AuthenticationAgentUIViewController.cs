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
using CoreFoundation;
using CoreGraphics;
using Foundation;
using UIKit;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    [Foundation.Register("AuthenticationAgentUIViewController")]
    internal class AuthenticationAgentUIViewController : UIViewController
    {
        private UIWebView webView;

        private readonly string url;
        private readonly string callback;

        private readonly ReturnCodeCallback callbackMethod;

        public delegate void ReturnCodeCallback(AuthorizationResult result);

        public AuthenticationAgentUIViewController(string url, string callback, ReturnCodeCallback callbackMethod)
        {
            this.url = url;
            this.callback = callback;
            this.callbackMethod = callbackMethod;
            NSUrlProtocol.RegisterClass(new ObjCRuntime.Class(typeof(AdalCustomUrlProtocol)));
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            View.BackgroundColor = UIColor.White;

            webView = new UIWebView((CGRect) View.Bounds);
            webView.ShouldStartLoad = (wView, request, navType) =>
            {
                if (request == null)
                {
                    return true;
                }

                string requestUrlString = request.Url.ToString();
                
                if (requestUrlString.StartsWith(BrokerConstants.BrowserExtPrefix))
                {
                    DispatchQueue.MainQueue.DispatchAsync(() => CancelAuthentication(null, null));
                    requestUrlString = requestUrlString.Replace(BrokerConstants.BrowserExtPrefix, "https://");
                    DispatchQueue.MainQueue.DispatchAsync(
                        () => UIApplication.SharedApplication.OpenUrl(new NSUrl(requestUrlString)));
                    this.DismissViewController(true, null);
                    return false;
                }

                if (requestUrlString.ToLower().StartsWith(callback.ToLower()) || requestUrlString.StartsWith(BrokerConstants.BrowserExtInstallPrefix))
                {
                    callbackMethod(new AuthorizationResult(AuthorizationStatus.Success, request.Url.ToString()));
                    this.DismissViewController(true, null);
                    return false;
                }

                if (requestUrlString.StartsWith(BrokerConstants.DeviceAuthChallengeRedirect, StringComparison.CurrentCultureIgnoreCase))
                {
                    Uri uri = new Uri(requestUrlString);
                    string query = uri.Query;
                    if (query.StartsWith("?"))
                    {
                        query = query.Substring(1);
                    }

                    Dictionary<string, string> keyPair = EncodingHelper.ParseKeyValueList(query, '&', true, false, null);
                    string responseHeader = PlatformPlugin.DeviceAuthHelper.CreateDeviceAuthChallengeResponse(keyPair).Result;
                    
                    NSMutableUrlRequest newRequest = (NSMutableUrlRequest)request.MutableCopy();
                    newRequest.Url = new NSUrl(keyPair["SubmitUrl"]);
                    newRequest[BrokerConstants.ChallengeResponseHeader] = responseHeader;
                    wView.LoadRequest(newRequest);
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

            this.NavigationItem.LeftBarButtonItem = new UIBarButtonItem(UIBarButtonSystemItem.Cancel,
                this.CancelAuthentication);

            webView.LoadRequest(new NSUrlRequest(new NSUrl(this.url)));

            // if this is false, page will be 'zoomed in' to normal size
            //webView.ScalesPageToFit = true;
        }

        private void CancelAuthentication(object sender, EventArgs e)
        {
            callbackMethod(new AuthorizationResult(AuthorizationStatus.UserCancel, null));
            this.DismissViewController(true, null);
        }

        public override void DismissViewController(bool animated, Action completionHandler)
        {
            NSUrlProtocol.UnregisterClass(new ObjCRuntime.Class(typeof(AdalCustomUrlProtocol)));
            base.DismissViewController(animated, completionHandler);
        }
    }
}