// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.UI;
using System;
using System.Threading;
using System.Threading.Tasks;
using AppKit;
using Foundation;

namespace Microsoft.Identity.Client.Platforms.Mac
{
    internal class MacEmbeddedWebUI : IWebUI
    {
        private SemaphoreSlim _returnedUriReady;
        private AuthorizationResult _authorizationResult;

        public CoreUIParent CoreUIParent { get; set; }
        public RequestContext RequestContext { get; set; }

        public async Task<AuthorizationResult> AcquireAuthorizationAsync(
            Uri authorizationUri,
            Uri redirectUri,
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
            _returnedUriReady = new SemaphoreSlim(0);
            Authenticate(authorizationUri, redirectUri, requestContext);
            await _returnedUriReady.WaitAsync(cancellationToken).ConfigureAwait(false);

            return _authorizationResult;
        }

        private void SetAuthorizationResult(AuthorizationResult authorizationResultInput)
        {
            _authorizationResult = authorizationResultInput;
            _returnedUriReady.Release();
        }

        private void Authenticate(Uri authorizationUri, Uri redirectUri, RequestContext requestContext)
        {
            try
            {
                // Ensure we create the NSViewController on the main thread.
                // Consumers of our library must ensure they do not block the main thread
                // or else they will cause a deadlock.
                // For example calling `AcquireTokenAsync(...).Result` from the main thread
                // would result in this delegate never executing.
                NSRunLoop.Main.BeginInvokeOnMainThread(() =>
                {
                    var windowController = new AuthenticationAgentNSWindowController(
                        authorizationUri.AbsoluteUri,
                        redirectUri.OriginalString,
                        SetAuthorizationResult);
                    windowController.Run(CoreUIParent.CallerWindow);
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

        public void ValidateRedirectUri(Uri redirectUri)
        {
            RedirectUriHelper.Validate(redirectUri, usesSystemBrowser: false);
        }
    }
}
