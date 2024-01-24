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

namespace Microsoft.Identity.Client.Platforms.Features.WebView2WebUi
{
#if NET6_WIN
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
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
          
        }

        public Uri UpdateRedirectUri(Uri redirectUri)
        {
            RedirectUriHelper.Validate(redirectUri, usesSystemBrowser: false);
            return redirectUri;
        }

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

    }
}
