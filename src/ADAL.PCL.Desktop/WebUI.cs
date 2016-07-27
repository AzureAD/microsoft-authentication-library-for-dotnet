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
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory.Internal
{
    internal abstract class WebUI : IWebUI
    {
        protected Uri RequestUri { get; private set; }

        protected Uri CallbackUri { get; private set; }

        public Object OwnerWindow { get; set; }

        public async Task<AuthorizationResult> AcquireAuthorizationAsync(Uri authorizationUri, Uri redirectUri, CallState callState)
        {
            AuthorizationResult authorizationResult = null;

            var sendAuthorizeRequest = new Action(
                delegate
                {
                    authorizationResult = this.Authenticate(authorizationUri, redirectUri);
                });

            // If the thread is MTA, it cannot create or communicate with WebBrowser which is a COM control.
            // In this case, we have to create the browser in an STA thread via StaTaskScheduler object.
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.MTA)
            {
                using (var staTaskScheduler = new StaTaskScheduler(1))
                {
                    try
                    {
                        Task.Factory.StartNew(sendAuthorizeRequest, CancellationToken.None, TaskCreationOptions.None, staTaskScheduler).Wait();
                    }
                    catch (AggregateException ae)
                    {
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
            this.RequestUri = requestUri;
            this.CallbackUri = callbackUri;

            ThrowOnNetworkDown();
            return this.OnAuthenticate();
        }

        private static void ThrowOnNetworkDown()
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                throw new AdalException(AdalError.NetworkNotAvailable);
            }
        }

        protected abstract AuthorizationResult OnAuthenticate();
    }
}
