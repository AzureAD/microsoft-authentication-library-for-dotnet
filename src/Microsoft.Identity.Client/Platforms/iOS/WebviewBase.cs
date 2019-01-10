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

using System;
using System.Threading;
using System.Threading.Tasks;
using AuthenticationServices;
using Foundation;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.UI;
using SafariServices;
using UIKit;

namespace Microsoft.Identity.Client.Platforms.iOS
{
    internal abstract class WebviewBase : NSObject, IWebUI, ISFSafariViewControllerDelegate
    {
        protected static SemaphoreSlim returnedUriReady;
        protected static AuthorizationResult authorizationResult;
        protected static UIViewController viewController;
        protected SFSafariViewController safariViewController;
        protected SFAuthenticationSession sfAuthenticationSession;
        protected ASWebAuthenticationSession asWebAuthenticationSession;
        protected nint taskId = UIApplication.BackgroundTaskInvalid;
        protected NSObject didEnterBackgroundNotification;
        protected NSObject willEnterForegroundNotification;

        public WebviewBase()
        {
            didEnterBackgroundNotification = NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.DidEnterBackgroundNotification, OnMoveToBackground);
            willEnterForegroundNotification = NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.WillEnterForegroundNotification, OnMoveToForeground);
        }

        public abstract Task<AuthorizationResult> AcquireAuthorizationAsync(Uri authorizationUri, Uri redirectUri,
            RequestContext requestContext);

        public static bool ContinueAuthentication(string url)
        {
            if (returnedUriReady == null)
            {
                return false;
            }

            viewController.InvokeOnMainThread(() =>
            {
                authorizationResult = new AuthorizationResult(AuthorizationStatus.Success, url);
                returnedUriReady.Release();
            });

            return true;
        }

        protected void OnMoveToBackground(NSNotification notification)
        {
            // After iOS 11.3, it is neccesary to keep a background task running while moving an app to the background 
            // in order to prevent the system from reclaiming network resources from the app. 
            // This will prevent authentication from failing while the application is moved to the background while waiting for MFA to finish.
            taskId = UIApplication.SharedApplication.BeginBackgroundTask(() =>
            {
                if (taskId != UIApplication.BackgroundTaskInvalid)
                {
                    UIApplication.SharedApplication.EndBackgroundTask(taskId);
                    taskId = UIApplication.BackgroundTaskInvalid;
                }
            });
        }

        protected void OnMoveToForeground(NSNotification notification)
        {
            if (taskId != UIApplication.BackgroundTaskInvalid)
            {
                UIApplication.SharedApplication.EndBackgroundTask(taskId);
                taskId = UIApplication.BackgroundTaskInvalid;
            }
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (didEnterBackgroundNotification != null)
                {
                    didEnterBackgroundNotification.Dispose();
                    didEnterBackgroundNotification = null;
                }
                if (willEnterForegroundNotification != null)
                {
                    willEnterForegroundNotification.Dispose();
                    willEnterForegroundNotification = null;
                }
            }
        }

        public abstract void ValidateRedirectUri(Uri redirectUri);
    }
}
