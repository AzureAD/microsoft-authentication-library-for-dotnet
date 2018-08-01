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

using Foundation;
using System;
using System.Threading;
using System.Threading.Tasks;
using UIKit;

namespace Microsoft.Identity.Core.UI.EmbeddedWebview
{
    internal class EmbeddedWebUI : WebviewBase, IDisposable
    {
        private nint taskId = UIApplication.BackgroundTaskInvalid;
        private NSObject didEnterBackgroundNotification, willEnterForegroundNotification;
        public RequestContext RequestContext { get; internal set; }
        public CoreUIParent CoreUIParent { get; set; }

        public EmbeddedWebUI()
        {
            this.didEnterBackgroundNotification = NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.DidEnterBackgroundNotification, OnMoveToBackground);
            this.willEnterForegroundNotification = NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.WillEnterForegroundNotification, OnMoveToForeground);
        }

        public async override Task<AuthorizationResult> AcquireAuthorizationAsync(Uri authorizationUri, Uri redirectUri, RequestContext requestContext)
        {
            returnedUriReady = new SemaphoreSlim(0);
            Authenticate(authorizationUri, redirectUri, requestContext);
            await returnedUriReady.WaitAsync().ConfigureAwait(false);

            return authorizationResult;
        }

        public static void SetAuthorizationResult(AuthorizationResult authorizationResultInput)
        {
            authorizationResult = authorizationResultInput;
            returnedUriReady.Release();
        }

        public void Authenticate(Uri authorizationUri, Uri redirectUri, RequestContext requestContext)
        {
            UIViewController viewController = null;
            InvokeOnMainThread(() =>
            {
                UIWindow window = UIApplication.SharedApplication.KeyWindow;
                viewController = CoreUIParent.FindCurrentViewController(window.RootViewController);
            });
            try
            {
                viewController.InvokeOnMainThread(() =>
                {
                    var navigationController =
                        new AuthenticationAgentUINavigationController(authorizationUri.AbsoluteUri,
                            redirectUri.OriginalString, CallbackMethod, CoreUIParent.PreferredStatusBarStyle);

                    navigationController.ModalPresentationStyle = CoreUIParent.ModalPresentationStyle;
                    navigationController.ModalTransitionStyle = CoreUIParent.ModalTransitionStyle;
                    navigationController.TransitioningDelegate = viewController.TransitioningDelegate;

                    viewController.PresentViewController(navigationController, true, null);
                });
            }
            catch (Exception ex)
            {
                throw CoreExceptionFactory.Instance.GetClientException(
                    CoreErrorCodes.AuthenticationUiFailed, 
                    "See inner exception for details", 
                    ex);
            }
        }

        void OnMoveToBackground(NSNotification notification)
        {
            //After iOS 11.3, it is neccesary to keep a background task running while moving an app to the background in order to prevent the system from reclaiming network resources from the app. 
            //This will prevent authentication from failing while the application is moved to the background while waiting for MFA to finish.
            this.taskId = UIApplication.SharedApplication.BeginBackgroundTask(() => {
                if (this.taskId != UIApplication.BackgroundTaskInvalid)
                {
                    UIApplication.SharedApplication.EndBackgroundTask(this.taskId);
                    this.taskId = UIApplication.BackgroundTaskInvalid;
                }
            });
        }

        void OnMoveToForeground(NSNotification notification)
        {
            if (this.taskId != UIApplication.BackgroundTaskInvalid)
            {
                UIApplication.SharedApplication.EndBackgroundTask(this.taskId);
                this.taskId = UIApplication.BackgroundTaskInvalid;
            }
        }

        private static void CallbackMethod(AuthorizationResult result)
        {
            SetAuthorizationResult(result);
        }

        //Hiding NSObject.Dispose() with new to implement IDisposable interface
        public new void Dispose()
        {
            this.didEnterBackgroundNotification.Dispose();
            this.willEnterForegroundNotification.Dispose();
        }
    }
}
