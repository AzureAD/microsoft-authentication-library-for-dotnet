// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.UI;
using Uri = System.Uri;

namespace Microsoft.Identity.Client.Platforms.Android.SystemWebview
{
    [Preserve(AllMembers = true)]
    internal class SystemWebUI : WebviewBase
    {
        private readonly CoreUIParent _parent;

        public SystemWebUI(CoreUIParent parent)
        {
            _parent = parent;
        }

        public override async Task<AuthorizationResult> AcquireAuthorizationAsync(
            Uri authorizationUri,
            Uri redirectUri,
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
            AuthenticationContinuationHelper.LastRequestLogger = requestContext.Logger;

            returnedUriReady = new SemaphoreSlim(0);

            var pendingRequest = new SystemWebViewRequest(authorizationUri, redirectUri, requestContext);
            AuthenticationActivity.PendingRequest = pendingRequest;

            try
            {
                try
                {
                    var agentIntent = new Intent(_parent.Activity, typeof(AuthenticationActivity));
                    agentIntent.PutExtra(AndroidConstants.RequestUrlKey, authorizationUri.AbsoluteUri);
                    _parent.Activity.RunOnUiThread(()=> _parent.Activity.StartActivityForResult(agentIntent, 0));
                }
                catch (Exception ex)
                {
                    requestContext.Logger.ErrorPii(ex);
                    throw new MsalClientException(
                        MsalError.AuthenticationUiFailedError,
                        "AuthenticationActivity failed to start",
                        ex);
                }

                await returnedUriReady.WaitAsync(cancellationToken).ConfigureAwait(false);
                return authorizationResult;
            }
            finally
            {
                if (ReferenceEquals(AuthenticationActivity.PendingRequest, pendingRequest))
                {
                    AuthenticationActivity.PendingRequest = null;
                }
            }
        }

        public override Uri UpdateRedirectUri(Uri redirectUri)
        {
            RedirectUriHelper.Validate(redirectUri, usesSystemBrowser: true);
            return redirectUri;
        }
    }
}
