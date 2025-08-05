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

#if WINUI3
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

        public async Task<AuthorizationResult> AcquireAuthorizationAsync(
            Uri authorizationUri,
            Uri redirectUri,
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
            AuthorizationResult result = null;

#if WINUI3
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
#if WINUI3
                    var tcs = new TaskCompletionSource<AuthorizationResult>();

                    _parent.SynchronizationContext.Post((state) =>
                    {
                        var taskCompletionSource = (TaskCompletionSource<AuthorizationResult>)state;

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
#endif
                }
                else
                {
                    using (var staTaskScheduler = new StaTaskScheduler(1))
                    {
                        try
                        {
#if WINUI3
                            await Task.Factory.StartNew(
                                sendAuthorizeRequest,
                                cancellationToken,
                                TaskCreationOptions.None,
                                staTaskScheduler).Unwrap().ConfigureAwait(false);
#else
                            Task.Factory.StartNew(
                                sendAuthorizeRequest,
                                cancellationToken,
                                TaskCreationOptions.None,
                                staTaskScheduler).Wait(cancellationToken);
#endif
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
#if WINUI3
                await sendAuthorizeRequest().ConfigureAwait(false);
#else
                sendAuthorizeRequest();
#endif
            }

            return result;
        }

        public Uri UpdateRedirectUri(Uri redirectUri)
        {
            RedirectUriHelper.Validate(redirectUri, usesSystemBrowser: false);
            return redirectUri;
        }

#if WINUI3
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
