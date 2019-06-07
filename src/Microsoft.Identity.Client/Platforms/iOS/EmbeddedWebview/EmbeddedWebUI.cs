// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.UI;
using UIKit;

namespace Microsoft.Identity.Client.Platforms.iOS.EmbeddedWebview
{
    internal class EmbeddedWebUI : WebviewBase, IDisposable
    {
        public RequestContext RequestContext { get; internal set; }
        public CoreUIParent CoreUIParent { get; set; }

        public async override Task<AuthorizationResult> AcquireAuthorizationAsync(
            Uri authorizationUri,
            Uri redirectUri,
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
            returnedUriReady = new SemaphoreSlim(0);
            Authenticate(authorizationUri, redirectUri, requestContext);
            await returnedUriReady.WaitAsync(cancellationToken).ConfigureAwait(false);

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
