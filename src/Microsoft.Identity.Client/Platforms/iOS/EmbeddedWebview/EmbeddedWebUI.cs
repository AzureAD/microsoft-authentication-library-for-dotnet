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
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Exceptions;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.UI;
using UIKit;

namespace Microsoft.Identity.Client.Platforms.iOS.EmbeddedWebview
{
    internal class EmbeddedWebUI : WebviewBase, IDisposable
    {
        private readonly RequestContext _requestContext;
        private readonly CoreUIParent _coreUiParent;

        public EmbeddedWebUI(RequestContext requestContext, CoreUIParent coreUiParent)
        {
            _requestContext = requestContext;
            _coreUiParent = coreUiParent;
        }

        public override async Task<AuthorizationResult> AcquireAuthorizationAsync(Uri authorizationUri, Uri redirectUri, RequestContext requestContext)
        {
            returnedUriReady = new SemaphoreSlim(0);
            Authenticate(authorizationUri, redirectUri, requestContext);
            await returnedUriReady.WaitAsync().ConfigureAwait(false);
            return authorizationResult;
        }

        private static void SetAuthorizationResult(AuthorizationResult authorizationResultInput)
        {
            authorizationResult = authorizationResultInput;
            returnedUriReady.Release();
        }

        private void Authenticate(Uri authorizationUri, Uri redirectUri, RequestContext requestContext)
        {
            viewController = null;
            InvokeOnMainThread(() =>
            {
                UIWindow window = UIApplication.SharedApplication.KeyWindow;
                viewController = CoreUIParent.FindCurrentViewController(window.RootViewController);
            });
            try
            {
                viewController.InvokeOnMainThread(() =>
                {
                    var navigationController = new AuthenticationAgentUINavigationController(
                        _requestContext,
                        authorizationUri.AbsoluteUri,
                        redirectUri.OriginalString,
                        CallbackMethod,
                        _coreUiParent.PreferredStatusBarStyle)
                    {
                        ModalPresentationStyle = _coreUiParent.ModalPresentationStyle,
                        ModalTransitionStyle = _coreUiParent.ModalTransitionStyle,
                        TransitioningDelegate = viewController.TransitioningDelegate
                    };


                    viewController.PresentViewController(navigationController, true, null);
                });
            }
            catch (Exception ex)
            {
                throw MsalExceptionFactory.GetClientException(
                    CoreErrorCodes.AuthenticationUiFailed,
                    "See inner exception for details",
                    ex);
            }
        }

        private static void CallbackMethod(AuthorizationResult result)
        {
            SetAuthorizationResult(result);
        }

        public override void ValidateRedirectUri(Uri redirectUri)
        {
            RedirectUriHelper.Validate(redirectUri, usesSystemBrowser: false);
        }
    }
}
