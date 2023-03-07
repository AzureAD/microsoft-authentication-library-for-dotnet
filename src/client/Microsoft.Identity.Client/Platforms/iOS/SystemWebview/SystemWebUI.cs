// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.UI;
using SafariServices;
using UIKit;

namespace Microsoft.Identity.Client.Platforms.iOS.SystemWebview
{
    internal class SystemWebUI : WebviewBase
    {
        public RequestContext RequestContext { get; set; }

        internal SystemWebViewOptions WebViewOptions { get; set; }

        public override async Task<AuthorizationResult> AcquireAuthorizationAsync(
            Uri authorizationUri,
            Uri redirectUri,
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
            AuthenticationContinuationHelper.LastRequestLogger = requestContext.Logger;
            requestContext.Logger.InfoPii(
              () => $"Starting the iOS system webui. Start Uri: {authorizationUri} Redirect URI:{redirectUri} ",
              () => $"Starting the iOS system webui. Redirect URI: {redirectUri}");

            s_viewController = null;
            InvokeOnMainThread(() =>
            {
                UIWindow window = UIApplication.SharedApplication.KeyWindow;
                s_viewController = CoreUIParent.FindCurrentViewController(window.RootViewController);
            });

            s_returnedUriReady = new SemaphoreSlim(0);
            Authenticate(authorizationUri, redirectUri, requestContext);
            await s_returnedUriReady.WaitAsync(cancellationToken).ConfigureAwait(false);
            s_returnedUriReady.Dispose();
            s_returnedUriReady = null;

            //dismiss safariviewcontroller
            s_viewController.InvokeOnMainThread(() =>
            {
                safariViewController?.DismissViewController(false, null);
            });

            return s_authorizationResult;
        }

        public void Authenticate(Uri authorizationUri, Uri redirectUri, RequestContext requestContext)
        {
            try
            {
                if (UIDevice.CurrentDevice.CheckSystemVersion(12, 0))
                {
                    asWebAuthenticationSession = new AuthenticationServices.ASWebAuthenticationSession(new NSUrl(authorizationUri.AbsoluteUri),
                        redirectUri.Scheme, (callbackUrl, error) =>
                        {
                            if (error != null)
                            {
                                ProcessCompletionHandlerError(error);
                            }
                            else
                            {
                                ContinueAuthentication(callbackUrl.ToString(), RequestContext.Logger);
                            }
                        });

                    asWebAuthenticationSession.BeginInvokeOnMainThread(() =>
                    {
                        if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
                        {
                            // If the presentationContext is missing from the session,
                            // MSAL.NET will pick up an "authentication cancelled" error
                            // With the addition of the presentationContext, .Start() must
                            // be called on the main UI thread
                            asWebAuthenticationSession.PresentationContextProvider =
                            new ASWebAuthenticationPresentationContextProviderWindow();

                            // If iOSHidePrivacyPrompt has a value, it will be set. Else, it will be false.
                            asWebAuthenticationSession.PrefersEphemeralWebBrowserSession = WebViewOptions?.iOSHidePrivacyPrompt ?? false;
                        }
                        asWebAuthenticationSession.Start();
                    });
                }

                else if (UIDevice.CurrentDevice.CheckSystemVersion(11, 0))
                {
                    sfAuthenticationSession = new SFAuthenticationSession(new NSUrl(authorizationUri.AbsoluteUri),
                        redirectUri.Scheme, (callbackUrl, error) =>
                        {
                            if (error != null)
                            {
                                ProcessCompletionHandlerError(error);
                            }
                            else
                            {
                                ContinueAuthentication(callbackUrl.ToString(), RequestContext.Logger);
                            }
                        });

                    sfAuthenticationSession.Start();
                }
                else
                {
                    safariViewController = new SFSafariViewController(new NSUrl(authorizationUri.AbsoluteUri), false)
                    {
                        Delegate = this
                    };
                    s_viewController.InvokeOnMainThread(() =>
                    {
                        s_viewController.PresentViewController(safariViewController, false, null);
                    });
                }
            }
            catch (Exception ex)
            {
                requestContext.Logger.ErrorPii(ex);
                throw new MsalClientException(
                    MsalError.AuthenticationUiFailedError,
                    ex.Message,
                    ex);
            }
        }

        public void ProcessCompletionHandlerError(NSError error)
        {
            if (s_returnedUriReady != null)
            {
                // The authorizationResult is set on the class and sent back to the InteractiveRequest
                // There it's processed in VerifyAuthorizationResult() and an MsalClientException
                // will be thrown.
                s_authorizationResult = AuthorizationResult.FromStatus(AuthorizationStatus.UserCancel);
                s_returnedUriReady.Release();
            }
        }

        [Export("safariViewControllerDidFinish:")]
        public void DidFinish(SFSafariViewController controller)
        {
            controller.DismissViewController(true, null);

            if (s_returnedUriReady != null)
            {
                s_authorizationResult = AuthorizationResult.FromStatus(AuthorizationStatus.UserCancel);
                s_returnedUriReady.Release();
            }
        }

        public override Uri UpdateRedirectUri(Uri redirectUri)
        {
            RedirectUriHelper.Validate(redirectUri, usesSystemBrowser: true);
            return redirectUri;
        }
    }
}
