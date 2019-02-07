//----------------------------------------------------------------------
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
using CoreFoundation;
using Foundation;
using Microsoft.Identity.Client.Exceptions;
using Microsoft.Identity.Client.Platforms.AppleShared;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Utils;
using UIKit;
using WebKit;

namespace Microsoft.Identity.Client.Platforms.iOS.EmbeddedWebview
{
    internal class WKWebNavigationDelegate : WKNavigationDelegate
    {
        private const string AboutBlankUri = "about:blank";
        private MsalAuthenticationAgentUIViewController AuthenticationAgentUIViewController = null;

        public WKWebNavigationDelegate(MsalAuthenticationAgentUIViewController AuthUIViewController)
        {
            AuthenticationAgentUIViewController = AuthUIViewController;
            return;
        }

        public override void DecidePolicy(WKWebView webView, WKNavigationAction navigationAction, Action<WKNavigationActionPolicy> decisionHandler)
        {
            string requestUrlString = navigationAction.Request.Url.ToString();
            // If the URL has the browser:// scheme then this is a request to open an external browser
            if (requestUrlString.StartsWith(iOSBrokerConstants.BrowserExtPrefix, StringComparison.OrdinalIgnoreCase))
            {
                DispatchQueue.MainQueue.DispatchAsync(() => AuthenticationAgentUIViewController.CancelAuthentication(null, null));

                // Build the HTTPS URL for launching with an external browser
                var httpsUrlBuilder = new UriBuilder(requestUrlString)
                {
                    Scheme = Uri.UriSchemeHttps
                };
                requestUrlString = httpsUrlBuilder.Uri.AbsoluteUri;

                DispatchQueue.MainQueue.DispatchAsync(
                    () => UIApplication.SharedApplication.OpenUrl(new NSUrl(requestUrlString)));
                AuthenticationAgentUIViewController.DismissViewController(true, null);
                decisionHandler(WKNavigationActionPolicy.Cancel);
                return;
            }

            if (requestUrlString.StartsWith(AuthenticationAgentUIViewController.callback, StringComparison.OrdinalIgnoreCase) ||
                   requestUrlString.StartsWith(iOSBrokerConstants.BrowserExtInstallPrefix, StringComparison.OrdinalIgnoreCase))
            {
                AuthenticationAgentUIViewController.DismissViewController(true, () =>
                    AuthenticationAgentUIViewController.callbackMethod(new AuthorizationResult(AuthorizationStatus.Success, requestUrlString)));
                decisionHandler(WKNavigationActionPolicy.Cancel);
                return;
            }

            if (requestUrlString.StartsWith(iOSBrokerConstants.DeviceAuthChallengeRedirect, StringComparison.OrdinalIgnoreCase))
            {
                Uri uri = new Uri(requestUrlString);
                string query = uri.Query;
                if (query.StartsWith("?", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Substring(1);
                }

                Dictionary<string, string> keyPair = CoreHelpers.ParseKeyValueList(query, '&', true, false, null);
                string responseHeader = DeviceAuthHelper.CreateDeviceAuthChallengeResponseAsync(keyPair).Result;

                NSMutableUrlRequest newRequest = (NSMutableUrlRequest)navigationAction.Request.MutableCopy();
                newRequest.Url = new NSUrl(keyPair["SubmitUrl"]);
                newRequest[iOSBrokerConstants.ChallengeResponseHeader] = responseHeader;
                webView.LoadRequest(newRequest);
                decisionHandler(WKNavigationActionPolicy.Cancel);
                return;
            }

            if (!navigationAction.Request.Url.AbsoluteString.Equals(AboutBlankUri, StringComparison.OrdinalIgnoreCase)
                && !navigationAction.Request.Url.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                AuthorizationResult result = new AuthorizationResult(AuthorizationStatus.ErrorHttp)
                {
                    Error = CoreErrorCodes.NonHttpsRedirectNotSupported,
                    ErrorDescription = CoreErrorMessages.NonHttpsRedirectNotSupported
                };
                AuthenticationAgentUIViewController.DismissViewController(true, () => AuthenticationAgentUIViewController.callbackMethod(result));
                decisionHandler(WKNavigationActionPolicy.Cancel);
                return;
            }
            decisionHandler(WKNavigationActionPolicy.Allow);
            return;
        }

        internal class WKWebViewUIDelegate : WKUIDelegate
        {
            private readonly MsalAuthenticationAgentUIViewController _controller = null;

            public WKWebViewUIDelegate(MsalAuthenticationAgentUIViewController c)
            {
                _controller = c;
                return;
            }
        }
    }
}