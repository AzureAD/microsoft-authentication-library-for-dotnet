// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
#if !IS_APPCENTER_BUILD
using AuthenticationServices;
#endif
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
        /* For app center builds, this will need to build on a hosted mac agent. The mac agent does not have the latest SDK's required to build 'ASWebAuthenticationSession'
        * Until the agents are updated, appcenter build will need to ignore the use of 'ASWebAuthenticationSession' for iOS 12.*/
#if !IS_APPCENTER_BUILD
        protected ASWebAuthenticationSession asWebAuthenticationSession;
#endif
        protected nint taskId = UIApplication.BackgroundTaskInvalid;
        protected NSObject didEnterBackgroundNotification;
        protected NSObject willEnterForegroundNotification;

        public WebviewBase()
        {
            didEnterBackgroundNotification = NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.DidEnterBackgroundNotification, OnMoveToBackground);
            willEnterForegroundNotification = NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.WillEnterForegroundNotification, OnMoveToForeground);
        }

        public abstract Task<AuthorizationResult> AcquireAuthorizationAsync(
            Uri authorizationUri,
            Uri redirectUri,
            RequestContext requestContext,
            CancellationToken cancellationToken);

        public static bool ContinueAuthentication(string url)
        {
            if (returnedUriReady == null)
            {
                return false;
            }

            authorizationResult = AuthorizationResult.FromUri(url);
            returnedUriReady.Release();

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

        public abstract Uri UpdateRedirectUri(Uri redirectUri);
    }
}
