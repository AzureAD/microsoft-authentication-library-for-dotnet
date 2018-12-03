//----------------------------------------------------------------------
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
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.Internal.UI
{
    internal abstract class WebUI : IWebUI
    {
        protected Uri RequestUri { get; private set; }
        protected Uri CallbackUri { get; private set; }
        public Object OwnerWindow { get; set; }

        public RequestContext RequestContext { get; set; }

        public async Task<AuthorizationResult> AcquireAuthorizationAsync(Uri authorizationUri, Uri redirectUri,
            RequestContext requestContext)
        {
            AuthorizationResult authorizationResult = null;

            var sendAuthorizeRequest = new Action(
                delegate { authorizationResult = Authenticate(authorizationUri, redirectUri); });

            // If the thread is MTA, it cannot create or communicate with WebBrowser which is a COM control.
            // In this case, we have to create the browser in an STA thread via StaTaskScheduler object.
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.MTA)
            {
                using (var staTaskScheduler = new StaTaskScheduler(1))
                {
                    try
                    {
                        Task.Factory.StartNew(sendAuthorizeRequest, CancellationToken.None, TaskCreationOptions.None,
                            staTaskScheduler).Wait();
                    }
                    catch (AggregateException ae)
                    {
                        requestContext.Logger.ErrorPii(ae.InnerException);
                        // Any exception thrown as a result of running task will cause AggregateException to be thrown with 
                        // actual exception as inner.
                        Exception innerException = ae.InnerExceptions[0];

                        // In MTA case, AggregateException is two layer deep, so checking the InnerException for that.
                        if (innerException is AggregateException)
                        {
                            innerException = ((AggregateException)innerException).InnerExceptions[0];
                        }

                        throw innerException;
                    }
                }
            }
            else
            {
                sendAuthorizeRequest();
            }

            return await Task.Factory.StartNew(() => authorizationResult).ConfigureAwait(false);
        }

        internal AuthorizationResult Authenticate(Uri requestUri, Uri callbackUri)
        {
            RequestUri = requestUri;
            CallbackUri = callbackUri;

            ThrowOnNetworkDown();
            return OnAuthenticate();
        }

        private static void ThrowOnNetworkDown()
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                throw new MsalClientException(MsalClientException.NetworkNotAvailableError, MsalErrorMessage.NetworkNotAvailable);
            }
        }

        protected abstract AuthorizationResult OnAuthenticate();

        public void ValidateRedirectUri(Uri redirectUri)
        {
            RedirectUriHelper.Validate(redirectUri, usesSystemBrowser: false);
        }
    }
}