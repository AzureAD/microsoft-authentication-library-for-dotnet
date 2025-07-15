// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Core;
#if WINRT
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Windowing;
using Windows.Graphics;
using Windows.Graphics.Display;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
#endif

namespace Microsoft.Identity.Client.Desktop.WebView2WebUi
{

#if WINRT
    internal class BrowserWindow
    {
        public Task ShowAsync()
        {
            var tcs = new TaskCompletionSource<object>();

            var window = new Window();
            var grid = new Grid();
            window.DispatcherQueue.TryEnqueue(() =>
            {

                var webView = new Microsoft.UI.Xaml.Controls.WebView2
                {
                    Source = new Uri("https://www.google.com"),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };

                grid.Children.Add(webView);
                window.Content = grid;

                //window.Activated += (s, e) =>
                //{

                //    var displayInfo = DisplayInformation.GetForCurrentView();
                //    double scale = displayInfo.RawPixelsPerViewPixel;
                //    var screenHeight = DisplayArea.GetFromWindowId(GetWindowId(window), DisplayAreaFallback.Primary).WorkArea.Height;
                //    int uiHeight = (int)(Math.Max(screenHeight, 160) * 0.7 / scale);

                //    var appWindow = AppWindow.GetFromWindowId(GetWindowId(window));
                //    appWindow.Resize(new SizeInt32(800, uiHeight));
                //};

                window.Activate();
                window.Closed += (s, e) =>
                {
                    tcs.TrySetResult(null);
                };
            });
            return tcs.Task;
        }

        private WindowId GetWindowId(Window window)
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            return Win32Interop.GetWindowIdFromWindow(hwnd);
        }
    }

    internal static class UIThreadHelper
    {
        private static DispatcherQueue _dispatcherQueue;

        /// 

        /// Call this once from the UI thread during app startup (e.g., in App.xaml.cs or MainWindow).
        /// 

        internal static void Initialize(DispatcherQueue dispatcherQueue)
        {
            _dispatcherQueue = dispatcherQueue ?? throw new ArgumentNullException(nameof(dispatcherQueue));
        }

        /// 

        /// Executes the given action on the UI thread.
        /// 

        internal static void RunOnUIThread(Action action)
        {
            if (_dispatcherQueue == null)
            {
                throw new InvalidOperationException("UIThreadHelper is not initialized. Call Initialize() from the UI thread first.");
            }

            if (!_dispatcherQueue.HasThreadAccess)
            {
                _dispatcherQueue.TryEnqueue(() => action());
            }
            else
            {
                action();
            }
        }
    }
    //internal class BrowserWindow : Window
    //{
    //    public BrowserWindow()
    //    {
    //        var window = new Window();
    //        var grid = new Grid();
    //        window.DispatcherQueue.TryEnqueue(() =>
    //        {

    //            var webView = new Microsoft.UI.Xaml.Controls.WebView2
    //            {
    //                Source = new Uri("https://www.google.com"),
    //                HorizontalAlignment = HorizontalAlignment.Stretch,
    //                VerticalAlignment = VerticalAlignment.Stretch
    //            };

    //            grid.Children.Add(webView);
    //            window.Content = grid;

    //            window.Activate();
    //        });
    //    }

    //    private WindowId GetWindowId(Window window)
    //    {
    //        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
    //        return Win32Interop.GetWindowIdFromWindow(hwnd);
    //    }
    //}

#endif

    internal class WebView2WebUi : IWebUI
    {
        private CoreUIParent _parent;
        private RequestContext _requestContext;

        public WebView2WebUi(CoreUIParent parent, RequestContext requestContext)
        {
            _parent = parent;
            _requestContext = requestContext;
        }

        public async Task<AuthorizationResult> AcquireAuthorizationAsync(
            Uri authorizationUri,
            Uri redirectUri,
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
            AuthorizationResult result = null;
            var sendAuthorizeRequest = new Func<Task>(async () =>
            {
                result = await InvokeEmbeddedWebviewAsync(authorizationUri, redirectUri, cancellationToken).ConfigureAwait(false);
            });

            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.MTA)
            {
                if (_parent.SynchronizationContext != null)
                {
                    var sendAuthorizeRequestWithTcs = new Func<object, Task>(async (tcs) =>
                    {
                        try
                        {
                            result = await InvokeEmbeddedWebviewAsync(authorizationUri, redirectUri, cancellationToken).ConfigureAwait(false);
                            ((TaskCompletionSource<object>)tcs).TrySetResult(null);
                        }
                        catch (Exception e)
                        {
                            // Need to catch the exception here and put on the TCS which is the task we are waiting on so that
                            // the exception coming out of Authenticate is correctly thrown.
                            ((TaskCompletionSource<object>)tcs).TrySetException(e);
                        }
                    });

                    var tcs2 = new TaskCompletionSource<object>();

                    _parent.SynchronizationContext.Post(
                        new SendOrPostCallback((state) =>
                        {
                            Task.Run(() => sendAuthorizeRequestWithTcs(state));
                        }), tcs2);
                }
                else
                {
                    using (var staTaskScheduler = new StaTaskScheduler(1))
                    {
                        try
                        {
                            Task.Factory.StartNew(
                                sendAuthorizeRequest,
                                cancellationToken,
                                TaskCreationOptions.None,
                                staTaskScheduler).Wait(cancellationToken);
                        }
                        catch (AggregateException ae)
                        {
                            requestContext.Logger.ErrorPii(ae.InnerException);
                            // Any exception thrown as a result of running task will cause AggregateException to be thrown with
                            // actual exception as inner.
                            Exception innerException = ae.InnerExceptions[0];

                            // In MTA case, AggregateException is two layer deep, so checking the InnerException for that.
                            if (innerException is AggregateException exception)
                            {
                                innerException = exception.InnerExceptions[0];
                            }

                            throw innerException;
                        }
                    }
                }
            }
            else
            {
                await sendAuthorizeRequest().ConfigureAwait(false);
            }

            return result;

        }

        public Uri UpdateRedirectUri(Uri redirectUri)
        {
            RedirectUriHelper.Validate(redirectUri, usesSystemBrowser: false);
            return redirectUri;
        }

        private async Task<AuthorizationResult> InvokeEmbeddedWebviewAsync(Uri startUri, Uri endUri, CancellationToken cancellationToken)
        {
#if !WINRT
            //using (var form = new WinFormsPanelWithWebView2(
            //    _parent.OwnerWindow,
            //    _parent?.EmbeddedWebviewOptions,
            //    _requestContext.Logger,
            //    startUri,
            //    endUri))
            //{
            //    return await form.DisplayDialogAndInterceptUriAsync(cancellationToken).ConfigureAwait(false);
            //}
            await Task.Yield();
            return new AuthorizationResult();
#else
            var browserWindow = new BrowserWindow();
            await browserWindow.ShowAsync().ConfigureAwait(false);

            return new AuthorizationResult();
#endif
        }

    }
}
