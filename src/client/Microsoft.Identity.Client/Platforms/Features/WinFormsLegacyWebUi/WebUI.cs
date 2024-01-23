// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Platforms.Features.WinFormsLegacyWebUi
{
    internal abstract class WebUI : IWebUI
    {
        protected Uri RequestUri { get; private set; }
        protected Uri CallbackUri { get; private set; }
        public object OwnerWindow { get; set; }
        protected SynchronizationContext SynchronizationContext { get; set; }

        public RequestContext RequestContext { get; set; }

        public async Task<AuthorizationResult> AcquireAuthorizationAsync(
            Uri authorizationUri,
            Uri redirectUri,
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
            AuthorizationResult authorizationResult = null;

            var authUriBuilder = new UriBuilder(authorizationUri);
            authUriBuilder.AppendOrReplaceQueryParameter("response_mode", "form_post");
            authorizationUri = authUriBuilder.Uri;

            var sendAuthorizeRequest = new Action(() =>
            {
                authorizationResult = Authenticate(authorizationUri, redirectUri, cancellationToken);
            });

            var sendAuthorizeRequestWithTcs = new Action<object>((tcs) =>
            {
                try
                {
                    authorizationResult = Authenticate(authorizationUri, redirectUri, cancellationToken);
                    ((TaskCompletionSource<object>)tcs).TrySetResult(null);
                }
                catch (Exception e)
                {
                    // Need to catch the exception here and put on the TCS which is the task we are waiting on so that
                    // the exception coming out of Authenticate is correctly thrown.
                    ((TaskCompletionSource<object>)tcs).TrySetException(e);
                }
            });

            // If the thread is MTA, it cannot create or communicate with WebBrowser which is a COM control.
            // In this case, we have to create the browser in an STA thread via StaTaskScheduler object.
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.MTA)
            {
                if (SynchronizationContext != null)
                {
                    var tcs = new TaskCompletionSource<object>();
                    SynchronizationContext.Post(new SendOrPostCallback(sendAuthorizeRequestWithTcs), tcs);
                    await tcs.Task.ConfigureAwait(false);
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
                            if (innerException is AggregateException innerAggregateException)
                            {
                                innerException = innerAggregateException.InnerExceptions[0];
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

            return await Task.Factory.StartNew(() => authorizationResult, cancellationToken).ConfigureAwait(false);
        }

        internal AuthorizationResult Authenticate(Uri requestUri, Uri callbackUri, CancellationToken cancellationToken)
        {
            RequestUri = requestUri;
            CallbackUri = callbackUri;

            return OnAuthenticate(cancellationToken);
        }

        protected abstract AuthorizationResult OnAuthenticate(CancellationToken cancellationToken);

        public Uri UpdateRedirectUri(Uri redirectUri)
        {
            RedirectUriHelper.Validate(redirectUri, usesSystemBrowser: false);
            return redirectUri;
        }
    }
}
