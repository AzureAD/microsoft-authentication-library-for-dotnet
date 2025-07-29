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
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Windowing;
using Windows.Graphics;
using Windows.Graphics.Display;
using Microsoft.UI;
using Microsoft.UI.Dispatching;

namespace Microsoft.Identity.Client.Desktop.WinUI3.WebView2WebUi
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

            var sendAuthorizeRequest = new Func<Task>(async () =>
            {
                result = await InvokeEmbeddedWebviewAsync(authorizationUri, redirectUri, cancellationToken).ConfigureAwait(false);
            });

            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.MTA)
            {
                if (_parent.SynchronizationContext != null)
                {
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
            var window = new WinUI3WindowWithWebView2(
                _parent.OwnerWindow,
                _parent?.EmbeddedWebviewOptions,
                _requestContext.Logger,
                startUri,
                endUri);

            return await window.DisplayDialogAndInterceptUriAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
