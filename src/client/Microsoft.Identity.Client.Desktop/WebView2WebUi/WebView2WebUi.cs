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

namespace Microsoft.Identity.Client.Desktop.WebView2WebUi
{
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
// #if WINRT
//         // Check if we're on the UI thread
//         if (Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread() != null)
//         {
//             // We're on the UI thread, call directly
//             result = await InvokeWinUIEmbeddedWebviewAsync(authorizationUri, redirectUri, cancellationToken);
//         }
//         else
//         {
//             // We're on a background thread, marshal to UI thread
//             var tcs = new TaskCompletionSource<AuthorizationResult>();

//             // Get the main window's dispatcher queue
//             var mainDispatcher = GetMainDispatcherQueue();
            
//             mainDispatcher.TryEnqueue(async () =>
//             {
//                 try
//                 {
//                     var authResult = await InvokeWinUIEmbeddedWebviewAsync(authorizationUri, redirectUri, cancellationToken);
//                     tcs.SetResult(authResult);
//                 }
//                 catch (Exception ex)
//                 {
//                     tcs.SetException(ex);
//                 }
//             });

//             result = await tcs.Task.ConfigureAwait(false);
//         }

//         return result;
// #else           
            var sendAuthorizeRequest = new Action(() =>
            {
                result = InvokeEmbeddedWebview(authorizationUri, redirectUri, cancellationToken);
            });

            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.MTA)
            {
                if (_parent.SynchronizationContext != null)
                {
                    var sendAuthorizeRequestWithTcs = new Action<object>((tcs) =>
                    {
                        try
                        {
                            result = InvokeEmbeddedWebview(authorizationUri, redirectUri, cancellationToken);
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
                        new SendOrPostCallback(sendAuthorizeRequestWithTcs), tcs2);
                    await tcs2.Task.ConfigureAwait(false);
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
                sendAuthorizeRequest();
            }

            return result;
// #endif
        }

        public Uri UpdateRedirectUri(Uri redirectUri)
        {
            RedirectUriHelper.Validate(redirectUri, usesSystemBrowser: false);
            return redirectUri;
        }

// #if WINRT
//         private async Task<AuthorizationResult> InvokeWinUIEmbeddedWebviewAsync(Uri startUri, Uri endUri, CancellationToken cancellationToken)
//         {
//             var window = new WinUI3WindowWithWebView2(
//                 _parent.OwnerWindow as Window,
//                 _parent?.EmbeddedWebviewOptions,
//                 _requestContext.Logger,
//                 startUri,
//                 endUri);

//             try
//             {
//                 return await window.DisplayDialogAndInterceptUriAsync(cancellationToken);
//             }
//             finally
//             {
//                 // Ensure window is properly closed
//                 window?.Close();
//             }
//         }

//         private Microsoft.UI.Dispatching.DispatcherQueue GetMainDispatcherQueue()
//         {
//             // Try to get dispatcher from parent window first
//             if (_parent.OwnerWindow is Window parentWindow)
//             {
//                 return parentWindow.DispatcherQueue;
//             }

//             // If no parent window, we'll need to create the auth window on a UI thread
//             // The simplest approach is to let the caller ensure they're on the UI thread
//             // or handle this at a higher level in your application
//             throw new InvalidOperationException(
//                 "No parent window available and not on UI thread. " +
//                 "Ensure AcquireAuthorizationAsync is called from the UI thread or provide a parent window.");
//         }
// #else
        private AuthorizationResult InvokeEmbeddedWebview(Uri startUri, Uri endUri, CancellationToken cancellationToken)
        {
// #if WINRT
//             throw new NotSupportedException("WebView2 is not supported in WinRT. Use WinUI3WebView2WebUi instead.");
// #else
            using (var form = new WinFormsPanelWithWebView2(
                _parent.OwnerWindow,
                _parent?.EmbeddedWebviewOptions,
                _requestContext.Logger,
                startUri,
                endUri))
            {
                return form.DisplayDialogAndInterceptUri(cancellationToken);
            }
// #endif
        }

// #endif
    }
}
