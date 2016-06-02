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
using AppKit;
using WebKit;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    [Register("AuthenticationAgentNSWindowController")]
    class AuthenticationAgentNSWindowController
        : NSWindowController, IWebPolicyDelegate, IWebFrameLoadDelegate, INSWindowDelegate
    {
        const int DEFAULT_WINDOW_WIDTH = 420;
        const int DEFAULT_WINDOW_HEIGHT = 650;

        WebView webView;
        NSProgressIndicator progressIndicator;

        NSWindow callerWindow;

        readonly string url;
        readonly string callback;

        readonly ReturnCodeCallback callbackMethod;

        public delegate void ReturnCodeCallback(AuthorizationResult result);

        public AuthenticationAgentNSWindowController(string url, string callback, ReturnCodeCallback callbackMethod)
            : base ("PlaceholderNibNameToForceWindowLoad")
        {
            this.url = url;
            this.callback = callback;
            this.callbackMethod = callbackMethod;
            NSUrlProtocol.RegisterClass(new ObjCRuntime.Class(typeof(AdalCustomUrlProtocol)));
        }

        [Export("windowWillClose:")]
        public void WillClose(NSNotification notification)
        {
            NSApplication.SharedApplication.StopModal ();

            NSUrlProtocol.UnregisterClass(new ObjCRuntime.Class(typeof(AdalCustomUrlProtocol)));
        }

        public void Run(NSWindow callerWindow)
        {
            this.callerWindow = callerWindow;

            RunModal();
        }

        //webview only works on main runloop, not nested, so set up manual modal runloop
        void RunModal()
        {
            var window = Window;
            IntPtr session = NSApplication.SharedApplication.BeginModalSession(window);
            NSRunResponse result = NSRunResponse.Continues;

            while (result == NSRunResponse.Continues)
            {
                using (var pool = new NSAutoreleasePool())
                {
                    var nextEvent = NSApplication.SharedApplication.NextEvent(NSEventMask.AnyEvent, NSDate.DistantFuture, NSRunLoop.NSDefaultRunLoopMode, true);

                    //discard events that are for other windows, else they remain somewhat interactive
                    if (nextEvent.Window != null && nextEvent.Window != window)
                    {
                        continue;
                    }

                    NSApplication.SharedApplication.SendEvent(nextEvent);

                    // Run the window modally until there are no events to process
                    result = (NSRunResponse)(long)NSApplication.SharedApplication.RunModalSession(session);

                    // Give the main loop some time
                    NSRunLoop.Current.LimitDateForMode(NSRunLoopMode.Default);
                }
            }

            NSApplication.SharedApplication.EndModalSession(session);
        }

        //largely ported from azure-activedirectory-library-for-objc
        //ADAuthenticationViewController.m
        public override void LoadWindow()
        {
            var parentWindow = callerWindow ?? NSApplication.SharedApplication.MainWindow;

            CGRect windowRect;
            if (parentWindow != null)
            {
                windowRect = parentWindow.Frame;
            }
            else
            {
                // If we didn't get a parent window then center it in the screen
                windowRect = NSScreen.MainScreen.Frame;
            }

            // Calculate the center of the current main window so we can position our window in the center of it
            CGRect centerRect = CenterRect(windowRect, new CGRect(0, 0, DEFAULT_WINDOW_WIDTH, DEFAULT_WINDOW_HEIGHT));

            var window = new NSWindow(centerRect, NSWindowStyle.Titled | NSWindowStyle.Closable | NSWindowStyle.Resizable, NSBackingStore.Buffered, true)
            {
                BackgroundColor = NSColor.Red,
                WeakDelegate = this,
                AccessibilityIdentifier = "ADAL_SIGN_IN_WINDOW"
            };

            var contentView = window.ContentView;
            contentView.AutoresizesSubviews = true;

            webView = new WebView(contentView.Frame, null, null)
            {
                FrameLoadDelegate = this,
                PolicyDelegate = this,
                AutoresizingMask = NSViewResizingMask.HeightSizable | NSViewResizingMask.WidthSizable,
                AccessibilityIdentifier = "ADAL_SIGN_IN_WEBVIEW"
            };

            contentView.AddSubview(webView);

            progressIndicator = new NSProgressIndicator(new CGRect(DEFAULT_WINDOW_WIDTH / 2 - 16, DEFAULT_WINDOW_HEIGHT / 2 - 16, 32, 32))
            {
                Style = NSProgressIndicatorStyle.Spinning,
                // Keep the item centered in the window even if it's resized.
                AutoresizingMask = NSViewResizingMask.MinXMargin | NSViewResizingMask.MaxXMargin | NSViewResizingMask.MinYMargin | NSViewResizingMask.MaxYMargin
            };

            // On OS X there's a noticable lag between the window showing and the page loading, so starting with the spinner
            // at least make it looks like something is happening.
            progressIndicator.Hidden = false;
            progressIndicator.StartAnimation(null);

            contentView.AddSubview(progressIndicator);

            Window = window;

            webView.MainFrameUrl = url;
        }

        static CGRect CenterRect(CGRect rect1, CGRect rect2)
        {
            nfloat x = rect1.X + ((rect1.Width - rect2.Width) / 2);
            nfloat y = rect1.Y + ((rect1.Height - rect2.Height) / 2);

            x = x < 0 ? 0 : x;
            y = y < 0 ? 0 : y;

            rect2.X = x;
            rect2.X = y;

            return rect2;
        }

        [Export("webView:decidePolicyForNavigationAction:request:frame:decisionListener:")]
        void DecidePolicyForNavigation(WebView webView, NSDictionary actionInformation, NSUrlRequest request, WebFrame frame, NSObject decisionToken)
        {
            if (request == null)
            {
                WebView.DecideUse(decisionToken);
                return;
            }

            string requestUrlString = request.Url.ToString();

            if (requestUrlString.StartsWith(BrokerConstants.BrowserExtPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var result = new AuthorizationResult(AuthorizationStatus.ProtocolError)
                {
                    Error = "Unsupported request",
                    ErrorDescription = "Server is redirecting client to browser. This behavior is not yet defined on Mac OS X."
                };
                callbackMethod(result);
                WebView.DecideIgnore(decisionToken);
                Close();
                return;
            }

            if (requestUrlString.ToLower(CultureInfo.InvariantCulture).StartsWith(callback.ToLower(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase) ||
                requestUrlString.StartsWith(BrokerConstants.BrowserExtInstallPrefix, StringComparison.OrdinalIgnoreCase))
            {
                callbackMethod(new AuthorizationResult(AuthorizationStatus.Success, request.Url.ToString()));
                WebView.DecideIgnore(decisionToken);
                Close();
                return;
            }

            if (requestUrlString.StartsWith(BrokerConstants.DeviceAuthChallengeRedirect, StringComparison.CurrentCultureIgnoreCase))
            {
                var uri = new Uri(requestUrlString);
                string query = uri.Query;
                if (query.StartsWith("?", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Substring(1);
                }

                Dictionary<string, string> keyPair = EncodingHelper.ParseKeyValueList(query, '&', true, false, null);
                string responseHeader = PlatformPlugin.DeviceAuthHelper.CreateDeviceAuthChallengeResponse(keyPair).Result;

                var newRequest = (NSMutableUrlRequest)request.MutableCopy();
                newRequest.Url = new NSUrl(keyPair["SubmitUrl"]);
                newRequest[BrokerConstants.ChallengeResponseHeader] = responseHeader;
                webView.MainFrame.LoadRequest(newRequest);
                WebView.DecideIgnore(decisionToken);
                return;
            }

            if (!request.Url.AbsoluteString.Equals("about:blank", StringComparison.CurrentCultureIgnoreCase) && !request.Url.Scheme.Equals("https", StringComparison.CurrentCultureIgnoreCase))
            {
                var result = new AuthorizationResult(AuthorizationStatus.ErrorHttp);
                result.Error = AdalError.NonHttpsRedirectNotSupported;
                result.ErrorDescription = AdalErrorMessage.NonHttpsRedirectNotSupported;
                callbackMethod(result);
                WebView.DecideIgnore(decisionToken);
                Close();
            }

            WebView.DecideUse(decisionToken);
        }

        [Export("webView:didFinishLoadForFrame:")]
        public void FinishedLoad(WebView sender, WebFrame forFrame)
        {
            Window.Title = webView.MainFrameTitle ?? "Sign in";

            progressIndicator.Hidden = true;
            progressIndicator.StopAnimation(null);
        }

        void CancelAuthentication()
        {
            callbackMethod(new AuthorizationResult(AuthorizationStatus.UserCancel, null));
        }

        [Export("windowShouldClose:")]
        public bool WindowShouldClose(NSObject sender)
        {
            CancelAuthentication();
            return true;
        }
    }
}
