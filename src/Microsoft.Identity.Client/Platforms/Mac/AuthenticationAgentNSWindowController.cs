// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using CoreGraphics;
using Foundation;
using AppKit;
using WebKit;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Client.Platforms.Shared.Apple;

namespace Microsoft.Identity.Client.Platforms.Mac
{
    [Register("AuthenticationAgentNSWindowController")]
    internal class AuthenticationAgentNSWindowController
        : NSWindowController, IWebPolicyDelegate, IWebFrameLoadDelegate, INSWindowDelegate
    {
        private const int DEFAULT_WINDOW_WIDTH = 420;
        private const int DEFAULT_WINDOW_HEIGHT = 650;

        private WebView _webView;
        private NSProgressIndicator _progressIndicator;
        private NSWindow _callerWindow;

        private readonly string _url;
        private readonly string _callback;
        private readonly ReturnCodeCallback _callbackMethod;

        public delegate void ReturnCodeCallback(AuthorizationResult result);

        public AuthenticationAgentNSWindowController(string url, string callback, ReturnCodeCallback callbackMethod)
            : base("PlaceholderNibNameToForceWindowLoad")
        {
            _url = url;
            _callback = callback;
            _callbackMethod = callbackMethod;
            NSUrlProtocol.RegisterClass(new ObjCRuntime.Class(typeof(CoreCustomUrlProtocol)));
        }

        [Export("windowWillClose:")]
        public void WillClose(NSNotification notification)
        {
            NSApplication.SharedApplication.StopModal();

            NSUrlProtocol.UnregisterClass(new ObjCRuntime.Class(typeof(CoreCustomUrlProtocol)));
        }

        public void Run(NSWindow callerWindow)
        {
            _callerWindow = callerWindow;

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
                    var nextEvent = NSApplication.SharedApplication.NextEvent(
                        NSEventMask.AnyEvent,
                        NSDate.DistantFuture,
                        NSRunLoopMode.Default,
                        true);

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
            var parentWindow = _callerWindow ?? NSApplication.SharedApplication.MainWindow;

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

            var window = new NSWindow(centerRect, NSWindowStyle.Titled | NSWindowStyle.Closable, NSBackingStore.Buffered, true)
            {
                BackgroundColor = NSColor.WindowBackground,
                WeakDelegate = this,
                AccessibilityIdentifier = "SIGN_IN_WINDOW"
            };

            var contentView = window.ContentView;
            contentView.AutoresizesSubviews = true;

            _webView = new WebView(contentView.Frame, null, null)
            {
                FrameLoadDelegate = this,
                PolicyDelegate = this,
                AutoresizingMask = NSViewResizingMask.HeightSizable | NSViewResizingMask.WidthSizable,
                AccessibilityIdentifier = "SIGN_IN_WEBVIEW"
            };

            contentView.AddSubview(_webView);

            // On macOS there's a noticeable lag between the window showing and the page loading, so starting with the spinner
            // at least make it looks like something is happening.
            _progressIndicator = new NSProgressIndicator(
                new CGRect(
                    (DEFAULT_WINDOW_WIDTH / 2) - 16,
                    (DEFAULT_WINDOW_HEIGHT / 2) - 16,
                    32,
                    32))
            {
                Style = NSProgressIndicatorStyle.Spinning,
                // Keep the item centered in the window even if it's resized.
                AutoresizingMask = NSViewResizingMask.MinXMargin | NSViewResizingMask.MaxXMargin | NSViewResizingMask.MinYMargin | NSViewResizingMask.MaxYMargin
            };

            _progressIndicator.Hidden = false;
            _progressIndicator.StartAnimation(null);

            contentView.AddSubview(_progressIndicator);

            Window = window;

            _webView.MainFrameUrl = _url;
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
                var result = AuthorizationResult.FromStatus(
                    AuthorizationStatus.ProtocolError,
                    "Unsupported request",
                    "Server is redirecting client to browser. This behavior is not yet defined on Mac OS X.");
                _callbackMethod(result);
                WebView.DecideIgnore(decisionToken);
                Close();
                return;
            }

            if (requestUrlString.ToLower(CultureInfo.InvariantCulture).StartsWith(_callback.ToLower(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase) ||
                requestUrlString.StartsWith(BrokerConstants.BrowserExtInstallPrefix, StringComparison.OrdinalIgnoreCase))
            {
                _callbackMethod(AuthorizationResult.FromUri(request.Url.ToString()));
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

                Dictionary<string, string> keyPair = CoreHelpers.ParseKeyValueList(query, '&', true, false, null);
                string responseHeader = DeviceAuthHelper.CreateDeviceAuthChallengeResponseAsync(keyPair).Result;

                var newRequest = (NSMutableUrlRequest)request.MutableCopy();
                newRequest.Url = new NSUrl(keyPair["SubmitUrl"]);
                newRequest[BrokerConstants.ChallengeResponseHeader] = responseHeader;
                webView.MainFrame.LoadRequest(newRequest);
                WebView.DecideIgnore(decisionToken);
                return;
            }

            if (!request.Url.AbsoluteString.Equals("about:blank", StringComparison.CurrentCultureIgnoreCase) &&
                !request.Url.Scheme.Equals("https", StringComparison.CurrentCultureIgnoreCase))
            {
                var result = AuthorizationResult.FromStatus(
                    AuthorizationStatus.ErrorHttp,
                    MsalError.NonHttpsRedirectNotSupported,
                    MsalErrorMessage.NonHttpsRedirectNotSupported);

                _callbackMethod(result);
                WebView.DecideIgnore(decisionToken);
                Close();
            }

            WebView.DecideUse(decisionToken);
        }

        [Export("webView:didFinishLoadForFrame:")]
        public void FinishedLoad(WebView sender, WebFrame forFrame)
        {
            Window.Title = _webView.MainFrameTitle ?? "Sign in";

            _progressIndicator.Hidden = true;
            _progressIndicator.StopAnimation(null);
        }

        void CancelAuthentication()
        {
            _callbackMethod(AuthorizationResult.FromStatus(AuthorizationStatus.UserCancel));
        }

        [Export("windowShouldClose:")]
        public bool WindowShouldClose(NSObject sender)
        {
            CancelAuthentication();
            return true;
        }
    }
}
