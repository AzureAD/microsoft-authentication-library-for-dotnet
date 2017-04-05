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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Interfaces;
using SafariServices;
using UIKit;

namespace Microsoft.Identity.Client
{
    internal class WebUI : NSObject, IWebUI, ISFSafariViewControllerDelegate
    {
        private static SemaphoreSlim returnedUriReady;
        private static AuthorizationResult authorizationResult;
        private SFSafariViewController safariViewController;
        
        public RequestContext RequestContext { get; set; }

        public async Task<AuthorizationResult> AcquireAuthorizationAsync(Uri authorizationUri, Uri redirectUri,
            RequestContext requestContext)
        {
            var window = UIApplication.SharedApplication.KeyWindow;
            var vc = FindCurrentViewController(window.RootViewController);
            
            returnedUriReady = new SemaphoreSlim(0);
            Authenticate(authorizationUri, redirectUri, vc, requestContext);
            await returnedUriReady.WaitAsync().ConfigureAwait(false);
            //dismiss safariviewcontroller
            vc.InvokeOnMainThread(() =>
            { safariViewController.DismissViewController(false, null);
            });

            return authorizationResult;
        }

        public static void SetAuthorizationResult(AuthorizationResult authorizationResultInput)
        {
            if (returnedUriReady != null)
            {
                authorizationResult = authorizationResultInput;
                returnedUriReady.Release();
            }
        }

        public void Authenticate(Uri authorizationUri, Uri redirectUri, UIViewController vc, RequestContext requestContext)
        {
            try
            {
                safariViewController = new SFSafariViewController(new NSUrl(authorizationUri.AbsoluteUri), false);
                safariViewController.Delegate = this;
                vc.InvokeOnMainThread(() =>
                {
                    vc.PresentViewController(safariViewController, false, null);
                });
            }
            catch (Exception ex)
            {
                requestContext.Logger.Error(ex);
                throw new MsalException(MsalError.AuthenticationUiFailed, "Failed to invoke SFSafariViewController", ex);
            }
        }

        [Foundation.Export("safariViewControllerDidFinish:")]
        public void DidFinish(SFSafariViewController controller)
        {
            controller.DismissViewController(true, null);
            SetAuthorizationResult(new AuthorizationResult(AuthorizationStatus.UserCancel, null));
        }

        private UIViewController FindCurrentViewController(UIViewController rootViewController)
        {
            if (rootViewController is UITabBarController)
            {
                UITabBarController tabBarController = (UITabBarController)rootViewController;
                return FindCurrentViewController(tabBarController.SelectedViewController);
            }
            else if (rootViewController is UINavigationController)
            {
                UINavigationController navigationController = (UINavigationController)rootViewController;
                return FindCurrentViewController(navigationController.VisibleViewController);
            }
            else if (rootViewController.PresentedViewController != null)
            {
                UIViewController presentedViewController = rootViewController.PresentedViewController;
                return FindCurrentViewController(presentedViewController);
            }
            else
            {
                return rootViewController;
            }
        }

    }
}