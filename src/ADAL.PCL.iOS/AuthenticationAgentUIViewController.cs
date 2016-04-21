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
                
                if (requestUrlString.StartsWith(BrokerConstants.BrowserExtPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    DispatchQueue.MainQueue.DispatchAsync(() => CancelAuthentication(null, null));
                    requestUrlString = requestUrlString.Replace(BrokerConstants.BrowserExtPrefix, "https://");
                    DispatchQueue.MainQueue.DispatchAsync(
                        () => UIApplication.SharedApplication.OpenUrl(new NSUrl(requestUrlString)));
                    this.DismissViewController(true, null);
                    return false;
                }

                if (requestUrlString.ToLower(CultureInfo.InvariantCulture).StartsWith(callback.ToLower(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase) || 
                    requestUrlString.StartsWith(BrokerConstants.BrowserExtInstallPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    callbackMethod(new AuthorizationResult(AuthorizationStatus.Success, request.Url.ToString()));
                    this.DismissViewController(true, null);
                    return false;
                }

                if (requestUrlString.StartsWith(BrokerConstants.DeviceAuthChallengeRedirect, StringComparison.CurrentCultureIgnoreCase))
                {
                    Uri uri = new Uri(requestUrlString);
                    string query = uri.Query;
                    if (query.StartsWith("?", StringComparison.OrdinalIgnoreCase))
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
                
                if (!request.Url.AbsoluteString.Equals("about:blank", StringComparison.CurrentCultureIgnoreCase) && !request.Url.Scheme.Equals("https", StringComparison.CurrentCultureIgnoreCase))
                {
                    AuthorizationResult result = new AuthorizationResult(AuthorizationStatus.ErrorHttp);
                    result.Error = AdalError.NonHttpsRedirectNotSupported;
                    result.ErrorDescription = AdalErrorMessage.NonHttpsRedirectNotSupported;
                    callbackMethod(result);
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
