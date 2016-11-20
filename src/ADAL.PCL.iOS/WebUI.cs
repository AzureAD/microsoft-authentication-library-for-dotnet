//------------------------------------------------------------------------------
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
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class WebUI : IWebUI
    {
        private static SemaphoreSlim returnedUriReady;
        private static AuthorizationResult authorizationResult;
        private readonly PlatformParameters parameters;

        public WebUI(IPlatformParameters parameters)
        {
            this.parameters = parameters as PlatformParameters;
            if (this.parameters == null)
            {
                throw new ArgumentException("parameters should be of type PlatformParameters", "parameters");
            }
        }

        public async Task<AuthorizationResult> AcquireAuthorizationAsync(Uri authorizationUri, Uri redirectUri, CallState callState)
        {
            returnedUriReady = new SemaphoreSlim(0);
            Authenticate(authorizationUri, redirectUri, callState);
            await returnedUriReady.WaitAsync().ConfigureAwait(false);
            return authorizationResult;
        }

        public static void SetAuthorizationResult(AuthorizationResult authorizationResultInput)
        {
            authorizationResult = authorizationResultInput;
            returnedUriReady.Release();
        }

        public void Authenticate(Uri authorizationUri, Uri redirectUri, CallState callState)
        {
            try
            {
                this.parameters.CallerViewController.InvokeOnMainThread(() =>
                {
                    var navigationController =
                        new AuthenticationAgentUINavigationController(authorizationUri.AbsoluteUri,
                            redirectUri.OriginalString, CallbackMethod, this.parameters.PreferredStatusBarStyle);

                    navigationController.ModalPresentationStyle = this.parameters.ModalPresentationStyle;

                    navigationController.ModalTransitionStyle = this.parameters.ModalTransitionStyle;

                    navigationController.TransitioningDelegate = this.parameters.TransitioningDelegate;

                    this.parameters.CallerViewController.PresentViewController(navigationController, true, null);
                });
            }
            catch (Exception ex)
            {
                throw new AdalException(AdalError.AuthenticationUiFailed, ex);
            }
        }

        private void CallbackMethod(AuthorizationResult result)
        {
            SetAuthorizationResult(result);
        }
    }
}
