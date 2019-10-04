// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.UI;
using SafariServices;
using UIKit;

namespace Microsoft.Identity.Client.Platforms.iOS.SystemWebview
{
    internal class SystemWebUI : WebviewBase, IDisposable
    {
        public RequestContext RequestContext { get; set; }

        public override async Task<AuthorizationResult> AcquireAuthorizationAsync(
            Uri authorizationUri,
            Uri redirectUri,
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
            viewController = null;
            InvokeOnMainThread(() =>
            {
                UIWindow window = UIApplication.SharedApplication.KeyWindow;
                viewController = CoreUIParent.FindCurrentViewController(window.RootViewController);
            });

            returnedUriReady = new SemaphoreSlim(0);
            Authenticate(authorizationUri, redirectUri, requestContext);
            await returnedUriReady.WaitAsync(cancellationToken).ConfigureAwait(false);

            //dismiss safariviewcontroller
            viewController.InvokeOnMainThread(() =>
            {
                safariViewController?.DismissViewController(false, null);
            });

            return authorizationResult;
        }

        public void Authenticate(Uri authorizationUri, Uri redirectUri, RequestContext requestContext)
        {
            try
            {
                /* For app center builds, this will need to build on a hosted mac agent. The mac agent does not have the latest SDK's required to build 'ASWebAuthenticationSession'
                * Until the agents are updated, appcenter build will need to ignore the use of 'ASWebAuthenticationSession' for iOS 12.*/
#if !IS_APPCENTER_BUILD
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
                                ContinueAuthentication(callbackUrl.ToString());
                            }
                        });

                    // iOS 13 requires a PresentationContextProvider
                    if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
                    {
                        asWebAuthenticationSession.BeginInvokeOnMainThread(() =>
                        {
                            asWebAuthenticationSession.PresentationContextProvider =
                            new ASWebAuthenticationPresentationContextProviderWindow();
                        });
                    }

                    asWebAuthenticationSession.Start();
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
                                ContinueAuthentication(callbackUrl.ToString());
                            }
                        });

                    sfAuthenticationSession.Start();
                }
#else
                if (UIDevice.CurrentDevice.CheckSystemVersion(11, 0))
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
                                ContinueAuthentication(callbackUrl.ToString());
                            }
                        });

                    sfAuthenticationSession.Start();
                }
#endif
                else
                {
                    safariViewController = new SFSafariViewController(new NSUrl(authorizationUri.AbsoluteUri), false)
                    {
                        Delegate = this
                    };
                    viewController.InvokeOnMainThread(() =>
                    {
                        viewController.PresentViewController(safariViewController, false, null);
                    });
                }
            }
            catch (Exception ex)
            {
                requestContext.Logger.ErrorPii(ex);
                throw new MsalClientException(
                    MsalError.AuthenticationUiFailedError,
                    "Failed to invoke SFSafariViewController",
                    ex);
            }
        }

        public void ProcessCompletionHandlerError(NSError error)
        {
            if (returnedUriReady != null)
            {
                // The authorizationResult is set on the class and sent back to the InteractiveRequest
                // There it's processed in VerifyAuthorizationResult() and an MsalClientException
                // will be thrown.
                authorizationResult = AuthorizationResult.FromStatus(AuthorizationStatus.UserCancel);
                returnedUriReady.Release();
            }
        }

        [Export("safariViewControllerDidFinish:")]
        public void DidFinish(SFSafariViewController controller)
        {
            controller.DismissViewController(true, null);

            if (returnedUriReady != null)
            {
                authorizationResult = AuthorizationResult.FromStatus(AuthorizationStatus.UserCancel);
                returnedUriReady.Release();
            }
        }

        public override Uri UpdateRedirectUri(Uri redirectUri)
        {
            RedirectUriHelper.Validate(redirectUri, usesSystemBrowser: true);
            return redirectUri;
        }
    }
}
