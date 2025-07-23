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
    internal class WebView2WebUi : IWebUI
    {
        private CoreUIParent _parent;
        private RequestContext _requestContext;

        public WebView2WebUi(CoreUIParent parent, RequestContext requestContext)
        {
            _parent = parent;
            _requestContext = requestContext;
        }

        //         public async Task<AuthorizationResult> AcquireAuthorizationAsync(
        //             Uri authorizationUri,
        //             Uri redirectUri,
        //             RequestContext requestContext,
        //             CancellationToken cancellationToken)
        //         {
        //             AuthorizationResult result = null;

        // #if WINRT
        //             var sendAuthorizeRequest = new Func<Task>(async () =>
        //             {
        //                 result = await InvokeEmbeddedWebviewAsync(authorizationUri, redirectUri, cancellationToken).ConfigureAwait(false);
        //             });
        // #else
        //             var sendAuthorizeRequest = new Action(() =>
        //             {
        //                 result = InvokeEmbeddedWebview(authorizationUri, redirectUri, cancellationToken);
        //             });
        // #endif

        //             if (Thread.CurrentThread.GetApartmentState() == ApartmentState.MTA)
        //             {
        //                 if (_parent.SynchronizationContext != null)
        //                 {
        // #if WINRT
        //                     var sendAuthorizeRequestWithTcs = new Func<object, Task>(async (tcs) =>
        //                     {
        //                         try
        //                         {
        //                             result = await InvokeEmbeddedWebviewAsync(authorizationUri, redirectUri, cancellationToken).ConfigureAwait(false);
        // #else
        //                     var sendAuthorizeRequestWithTcs = new Action<object>((tcs) =>
        //                     {
        //                         try
        //                         {
        //                             result = InvokeEmbeddedWebview(authorizationUri, redirectUri, cancellationToken);
        // #endif
        //                             ((TaskCompletionSource<object>)tcs).TrySetResult(null);
        //                         }
        //                         catch (Exception e)
        //                         {
        //                             // Need to catch the exception here and put on the TCS which is the task we are waiting on so that
        //                             // the exception coming out of Authenticate is correctly thrown.
        //                             ((TaskCompletionSource<object>)tcs).TrySetException(e);
        //                         }
        //                     });

        //                     var tcs2 = new TaskCompletionSource<object>();

        //                     _parent.SynchronizationContext.Post(
        // #if WINRT
        //                         new SendOrPostCallback((state) =>
        //                         {
        //                             Task.Run(() => sendAuthorizeRequestWithTcs(state));
        //                         }), tcs2);
        // #else
        //                         new SendOrPostCallback(sendAuthorizeRequestWithTcs), tcs2);
        //                     await tcs2.Task.ConfigureAwait(false);
        // #endif
        //                 }
        //                 else
        //                 {
        //                     using (var staTaskScheduler = new StaTaskScheduler(1))
        //                     {
        //                         try
        //                         {
        //                             Task.Factory.StartNew(
        //                                 sendAuthorizeRequest,
        //                                 cancellationToken,
        //                                 TaskCreationOptions.None,
        //                                 staTaskScheduler).Wait(cancellationToken);
        //                         }
        //                         catch (AggregateException ae)
        //                         {
        //                             requestContext.Logger.ErrorPii(ae.InnerException);
        //                             // Any exception thrown as a result of running task will cause AggregateException to be thrown with
        //                             // actual exception as inner.
        //                             Exception innerException = ae.InnerExceptions[0];

        //                             // In MTA case, AggregateException is two layer deep, so checking the InnerException for that.
        //                             if (innerException is AggregateException exception)
        //                             {
        //                                 innerException = exception.InnerExceptions[0];
        //                             }

        //                             throw innerException;
        //                         }
        //                     }
        //                 }
        //             }
        //             else
        //             {
        // #if WINRT
        //                 await sendAuthorizeRequest().ConfigureAwait(false);
        // #else
        //                 sendAuthorizeRequest();
        // #endif
        //             }

        //             return result;

        //         }

        public async Task<AuthorizationResult> AcquireAuthorizationAsync(
            Uri authorizationUri,
            Uri redirectUri,
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
            AuthorizationResult result = null;

        #if WINRT
            var sendAuthorizeRequest = new Func<Task>(async () =>
            {
                result = await InvokeEmbeddedWebviewAsync(authorizationUri, redirectUri, cancellationToken).ConfigureAwait(false);
            });
        #else
            var sendAuthorizeRequest = new Action(() =>
            {
                result = InvokeEmbeddedWebview(authorizationUri, redirectUri, cancellationToken);
            });
        #endif

            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.MTA)
            {
                if (_parent.SynchronizationContext != null)
                {
        #if WINRT
                    // For WINRT/WinUI3, use ContinueWith pattern for async operations
                    var tcs = new TaskCompletionSource<AuthorizationResult>();

                    _parent.SynchronizationContext.Post((state) =>
                    {
                        var taskCompletionSource = (TaskCompletionSource<AuthorizationResult>)state;

                        // Start the async operation on the UI thread (this call itself is synchronous)
                        var asyncOperation = InvokeEmbeddedWebviewAsync(authorizationUri, redirectUri, cancellationToken);

                        // Handle the completion asynchronously
                        asyncOperation.ContinueWith(task =>
                        {
                            if (task.IsFaulted)
                            {
                                var exception = task.Exception?.InnerException ?? task.Exception;
                                taskCompletionSource.TrySetException(exception);
                            }
                            else if (task.IsCanceled)
                            {
                                taskCompletionSource.TrySetCanceled();
                            }
                            else
                            {
                                taskCompletionSource.TrySetResult(task.Result);
                            }
                        }, TaskContinuationOptions.ExecuteSynchronously);

                    }, tcs);

                    return await tcs.Task.ConfigureAwait(false);
        #else
                    // For non-WINRT, keep the existing synchronous pattern
                    var sendAuthorizeRequestWithTcs = new Action<object>((tcs) =>
                    {
                        try
                        {
                            var authResult = InvokeEmbeddedWebview(authorizationUri, redirectUri, cancellationToken);
                            ((TaskCompletionSource<AuthorizationResult>)tcs).TrySetResult(authResult);
                        }
                        catch (Exception e)
                        {
                            ((TaskCompletionSource<AuthorizationResult>)tcs).TrySetException(e);
                        }
                    });

                    var tcs2 = new TaskCompletionSource<AuthorizationResult>();
                    _parent.SynchronizationContext.Post(new SendOrPostCallback(sendAuthorizeRequestWithTcs), tcs2);
                    return await tcs2.Task.ConfigureAwait(false);
        #endif
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
                            Exception innerException = ae.InnerExceptions[0];

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
        #if WINRT
                await sendAuthorizeRequest().ConfigureAwait(false);
        #else
                sendAuthorizeRequest();
        #endif
            }

            return result;
        }
    

    // public async Task<AuthorizationResult> AcquireAuthorizationAsync(
        //     Uri authorizationUri,
        //     Uri redirectUri,
        //     RequestContext requestContext,
        //     CancellationToken cancellationToken)
        // {
        //     AuthorizationResult result = null;

        // #if WINRT
        //     // For WinUI3, get the dispatcher queue from the current context
        //     var currentDispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        //     // Try to get the main window's dispatcher if available
        //     DispatcherQueue mainDispatcher = null;
        //     try
        //     {
        //         // In WinUI3, we need to get the dispatcher from the main window or current context
        //         mainDispatcher = currentDispatcher ?? 
        //                         Microsoft.UI.Xaml.Window.Current?.DispatcherQueue ??
        //                         (_parent.OwnerWindow as Microsoft.UI.Xaml.Window)?.DispatcherQueue;
        //     }
        //     catch
        //     {
        //         // Fallback to current thread dispatcher
        //         mainDispatcher = currentDispatcher;
        //     }

        //     if (mainDispatcher != null && mainDispatcher.HasThreadAccess)
        //     {
        //         // We're already on the correct UI thread - create window directly
        //         return await InvokeEmbeddedWebviewAsync(authorizationUri, redirectUri, cancellationToken).ConfigureAwait(false);
        //     }
        //     else if (mainDispatcher != null)
        //     {
        //         // We need to marshal to the UI thread
        //         var tcs = new TaskCompletionSource<AuthorizationResult>();

        //         mainDispatcher.TryEnqueue(() =>
        //         {
        //             try
        //             {
        //                 var asyncOperation = InvokeEmbeddedWebviewAsync(authorizationUri, redirectUri, cancellationToken);
        //                 asyncOperation.ContinueWith(task =>
        //                 {
        //                     if (task.IsFaulted)
        //                     {
        //                         var exception = task.Exception?.InnerException ?? task.Exception;
        //                         tcs.TrySetException(exception);
        //                     }
        //                     else if (task.IsCanceled)
        //                     {
        //                         tcs.TrySetCanceled();
        //                     }
        //                     else
        //                     {
        //                         tcs.TrySetResult(task.Result);
        //                     }
        //                 }, TaskContinuationOptions.ExecuteSynchronously);
        //             }
        //             catch (Exception ex)
        //             {
        //                 tcs.TrySetException(ex);
        //             }
        //         });

        //         return await tcs.Task.ConfigureAwait(false);
        //     }
        //     else
        //     {
        //         // Fallback: No UI dispatcher available, use the traditional approach
        //         var sendAuthorizeRequest = new Func<Task>(async () =>
        //         {
        //             result = await InvokeEmbeddedWebviewAsync(authorizationUri, redirectUri, cancellationToken).ConfigureAwait(false);
        //         });
        // #else
        //     var sendAuthorizeRequest = new Action(() =>
        //     {
        //         result = InvokeEmbeddedWebview(authorizationUri, redirectUri, cancellationToken);
        //     });
        // #endif

        //         if (Thread.CurrentThread.GetApartmentState() == ApartmentState.MTA)
        //         {
        //             if (_parent.SynchronizationContext != null)
        //             {
        // #if WINRT
        //                 // This is the fallback case for WinUI3 when no UI dispatcher is available
        //                 var tcs = new TaskCompletionSource<AuthorizationResult>();

        //                 _parent.SynchronizationContext.Post((state) =>
        //                 {
        //                     var taskCompletionSource = (TaskCompletionSource<AuthorizationResult>)state;

        //                     var asyncOperation = InvokeEmbeddedWebviewAsync(authorizationUri, redirectUri, cancellationToken);
        //                     asyncOperation.ContinueWith(task =>
        //                     {
        //                         if (task.IsFaulted)
        //                         {
        //                             var exception = task.Exception?.InnerException ?? task.Exception;
        //                             taskCompletionSource.TrySetException(exception);
        //                         }
        //                         else if (task.IsCanceled)
        //                         {
        //                             taskCompletionSource.TrySetCanceled();
        //                         }
        //                         else
        //                         {
        //                             taskCompletionSource.TrySetResult(task.Result);
        //                         }
        //                     }, TaskContinuationOptions.ExecuteSynchronously);

        //                 }, tcs);

        //                 return await tcs.Task.ConfigureAwait(false);
        // #else
        //                 // For non-WINRT, keep the existing synchronous pattern
        //                 var sendAuthorizeRequestWithTcs = new Action<object>((tcs) =>
        //                 {
        //                     try
        //                     {
        //                         var authResult = InvokeEmbeddedWebview(authorizationUri, redirectUri, cancellationToken);
        //                         ((TaskCompletionSource<AuthorizationResult>)tcs).TrySetResult(authResult);
        //                     }
        //                     catch (Exception e)
        //                     {
        //                         ((TaskCompletionSource<AuthorizationResult>)tcs).TrySetException(e);
        //                     }
        //                 });

        //                 var tcs2 = new TaskCompletionSource<AuthorizationResult>();
        //                 _parent.SynchronizationContext.Post(new SendOrPostCallback(sendAuthorizeRequestWithTcs), tcs2);
        //                 return await tcs2.Task.ConfigureAwait(false);
        // #endif
        //             }
        //             else
        //             {
        //                 using (var staTaskScheduler = new StaTaskScheduler(1))
        //                 {
        //                     try
        //                     {
        //                         Task.Factory.StartNew(
        //                             sendAuthorizeRequest,
        //                             cancellationToken,
        //                             TaskCreationOptions.None,
        //                             staTaskScheduler).Wait(cancellationToken);
        //                     }
        //                     catch (AggregateException ae)
        //                     {
        //                         requestContext.Logger.ErrorPii(ae.InnerException);
        //                         Exception innerException = ae.InnerExceptions[0];

        //                         if (innerException is AggregateException exception)
        //                         {
        //                             innerException = exception.InnerExceptions[0];
        //                         }

        //                         throw innerException;
        //                     }
        //                 }
        //             }
        //         }
        //         else
        //         {
        // #if WINRT
        //             await sendAuthorizeRequest().ConfigureAwait(false);
        // #else
        //             sendAuthorizeRequest();
        // #endif
        //         }

        //         return result;
        // #if WINRT
        //     }
        // #endif
        // }

        public Uri UpdateRedirectUri(Uri redirectUri)
        {
            RedirectUriHelper.Validate(redirectUri, usesSystemBrowser: false);
            return redirectUri;
        }

#if WINRT
        private async Task<AuthorizationResult> InvokeEmbeddedWebviewAsync(Uri startUri, Uri endUri, CancellationToken cancellationToken)
        {
            var window = new WinUI3WindowWithWebView2(
                _parent.OwnerWindow,
                _parent?.EmbeddedWebviewOptions,
                _requestContext.Logger,
                startUri,
                endUri);

            return await window.DisplayDialogAndInterceptUriAsync(cancellationToken).ConfigureAwait(false);
        }
#else
        private AuthorizationResult InvokeEmbeddedWebview(Uri startUri, Uri endUri, CancellationToken cancellationToken)
        {
            using (var form = new WinFormsPanelWithWebView2(
                _parent.OwnerWindow,
                _parent?.EmbeddedWebviewOptions,
                _requestContext.Logger,
                startUri,
                endUri))
            {
                return form.DisplayDialogAndInterceptUri(cancellationToken);
            }
        }
#endif
    }
}
