// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using AuthenticationServices;
using Foundation;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.UI;
using SafariServices;
using UIKit;

namespace Microsoft.Identity.Client.Platforms.iOS
{
    internal abstract class WebviewBase : NSObject, IWebUI, ISFSafariViewControllerDelegate
    {
        protected static SemaphoreSlim s_returnedUriReady;
        protected static AuthorizationResult s_authorizationResult;
        protected static UIViewController s_viewController;

        protected SFSafariViewController safariViewController;
        protected SFAuthenticationSession sfAuthenticationSession;
        protected ASWebAuthenticationSession asWebAuthenticationSession;
        protected nint taskId = UIApplication.BackgroundTaskInvalid;
        protected NSObject didEnterBackgroundNotification;
        protected NSObject willEnterForegroundNotification;

        protected WebviewBase()
        {
            didEnterBackgroundNotification = NSNotificationCenter.DefaultCenter.AddObserver(
                UIApplication.DidEnterBackgroundNotification, OnMoveToBackground);
            willEnterForegroundNotification = NSNotificationCenter.DefaultCenter.AddObserver(
                UIApplication.WillEnterForegroundNotification, OnMoveToForeground);
        }

        public abstract Task<AuthorizationResult> AcquireAuthorizationAsync(
            Uri authorizationUri,
            Uri redirectUri,
            RequestContext requestContext,
            CancellationToken cancellationToken);

        public static bool ContinueAuthentication(string url, Core.ILoggerAdapter logger)
        {
            if (s_returnedUriReady == null)
            {
                bool containsBrokerSubString = url.Contains(iOSBrokerConstants.IdentifyiOSBrokerFromResponseUrl);
                
                logger?.Warning(
                    "Not expecting navigation to come back to WebviewBase. " +
                    "This can indicate  a badly setup OpenUrl hook " +
                    "where SetBrokerContinuationEventArgs is not called.");

                logger?.WarningPii(
                    $"Url: {url} is broker url? {containsBrokerSubString}",
                    $"Is broker url? {containsBrokerSubString}");
                
                return false;
            }

            s_authorizationResult = AuthorizationResult.FromUri(url);
            logger?.Verbose(() => "Response url parsed and the result is " + s_authorizationResult.Status);

            s_returnedUriReady.Release();

            return true;
        }

        protected void OnMoveToBackground(NSNotification notification)
        {
            // After iOS 11.3, it is necessary to keep a background task running while moving an app to the background
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
