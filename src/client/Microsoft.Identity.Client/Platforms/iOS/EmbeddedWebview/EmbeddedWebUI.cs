// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.UI;
using UIKit;

namespace Microsoft.Identity.Client.Platforms.iOS.EmbeddedWebview
{
    internal class EmbeddedWebUI : WebviewBase
    {
        public RequestContext RequestContext { get; internal set; }
        public CoreUIParent CoreUIParent { get; set; }

        public async override Task<AuthorizationResult> AcquireAuthorizationAsync(
            Uri authorizationUri,
            Uri redirectUri,
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
            AuthenticationContinuationHelper.LastRequestLogger = requestContext.Logger;
            requestContext.Logger.InfoPii(
                () => $"Starting the iOS embedded webui. Start Uri: {authorizationUri} Redirect URI:{redirectUri} ",
                () => $"Starting the iOS embedded webui. Redirect URI: {redirectUri}"); 
                
            s_returnedUriReady = new SemaphoreSlim(0);
            Authenticate(authorizationUri, redirectUri, requestContext);
            await s_returnedUriReady.WaitAsync(cancellationToken).ConfigureAwait(false);

            return s_authorizationResult;
        }

        public static void SetAuthorizationResult(AuthorizationResult authorizationResultInput)
        {
            s_authorizationResult = authorizationResultInput;
            s_returnedUriReady.Release();
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
                        new MsalAuthenticationAgentUINavigationController(authorizationUri.AbsoluteUri,
                            redirectUri.OriginalString, CallbackMethod, CoreUIParent.PreferredStatusBarStyle)
                        {
                            ModalPresentationStyle = CoreUIParent.ModalPresentationStyle,
                            ModalTransitionStyle = CoreUIParent.ModalTransitionStyle,
                            TransitioningDelegate = viewController.TransitioningDelegate
                        };

                    viewController.PresentViewController(navigationController, true, null);
                });
            }
            catch (Exception ex)
            {
                throw new MsalClientException(
                    MsalError.AuthenticationUiFailed,
                    "See inner exception for details",
                    ex);
            }
        }

        private static void CallbackMethod(AuthorizationResult result)
        {
            SetAuthorizationResult(result);
        }

        public override Uri UpdateRedirectUri(Uri redirectUri)
        {
            RedirectUriHelper.Validate(redirectUri, usesSystemBrowser: false);
            return redirectUri;
        }
    }
}
