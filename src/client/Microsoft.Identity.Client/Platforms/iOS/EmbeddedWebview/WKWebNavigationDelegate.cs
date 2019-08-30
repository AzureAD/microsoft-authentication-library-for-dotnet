// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using CoreFoundation;
using Foundation;
using Microsoft.Identity.Client.Platforms.Shared.Apple;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Utils;
using UIKit;
using WebKit;

namespace Microsoft.Identity.Client.Platforms.iOS.EmbeddedWebview
{
    internal class WKWebNavigationDelegate : WKNavigationDelegate
    {
        private const string AboutBlankUri = "about:blank";
        private MsalAuthenticationAgentUIViewController _authenticationAgentUIViewController = null;

        public WKWebNavigationDelegate(MsalAuthenticationAgentUIViewController authUIViewController)
        {
            _authenticationAgentUIViewController = authUIViewController;
            return;
        }

        public override void DecidePolicy(WKWebView webView, WKNavigationAction navigationAction, Action<WKNavigationActionPolicy> decisionHandler)
        {
            string requestUrlString = navigationAction.Request.Url.ToString();
            // If the URL has the browser:// scheme then this is a request to open an external browser
            if (requestUrlString.StartsWith(iOSBrokerConstants.BrowserExtPrefix, StringComparison.OrdinalIgnoreCase))
            {
                DispatchQueue.MainQueue.DispatchAsync(() => _authenticationAgentUIViewController.CancelAuthentication(null, null));

                // Build the HTTPS URL for launching with an external browser
                var httpsUrlBuilder = new UriBuilder(requestUrlString)
                {
                    Scheme = Uri.UriSchemeHttps
                };
                requestUrlString = httpsUrlBuilder.Uri.AbsoluteUri;

                DispatchQueue.MainQueue.DispatchAsync(
                    () => UIApplication.SharedApplication.OpenUrl(new NSUrl(requestUrlString)));
                _authenticationAgentUIViewController.DismissViewController(true, null);
                decisionHandler(WKNavigationActionPolicy.Cancel);
                return;
            }

            if (requestUrlString.StartsWith(_authenticationAgentUIViewController.Callback, StringComparison.OrdinalIgnoreCase) ||
                   requestUrlString.StartsWith(iOSBrokerConstants.BrowserExtInstallPrefix, StringComparison.OrdinalIgnoreCase))
            {
                _authenticationAgentUIViewController.DismissViewController(true, () =>
                    _authenticationAgentUIViewController.CallbackMethod(AuthorizationResult.FromUri(requestUrlString)));
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
                AuthorizationResult result = AuthorizationResult.FromStatus(
                    AuthorizationStatus.ErrorHttp,
                    MsalError.NonHttpsRedirectNotSupported,
                    MsalErrorMessage.NonHttpsRedirectNotSupported);

                _authenticationAgentUIViewController.DismissViewController(true, () => _authenticationAgentUIViewController.CallbackMethod(result));
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
