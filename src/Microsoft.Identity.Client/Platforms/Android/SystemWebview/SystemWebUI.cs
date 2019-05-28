// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.UI;
using Uri = System.Uri;

namespace Microsoft.Identity.Client.Platforms.Android.SystemWebview
{
    [global::Android.Runtime.Preserve(AllMembers = true)]
    internal class SystemWebUI : WebviewBase
    {
        private readonly CoreUIParent _parent;

        public SystemWebUI(CoreUIParent parent)
        {
            _parent = parent;
        }

        public RequestContext RequestContext { get; set; }

        public async override Task<AuthorizationResult> AcquireAuthorizationAsync(
            Uri authorizationUri,
            Uri redirectUri,
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
            returnedUriReady = new SemaphoreSlim(0);

            try
            {
                var agentIntent = new Intent(_parent.Activity, typeof(AuthenticationActivity));
                agentIntent.PutExtra(AndroidConstants.RequestUrlKey, authorizationUri.AbsoluteUri);
                agentIntent.PutExtra(AndroidConstants.CustomTabRedirect, redirectUri.OriginalString);
                AuthenticationActivity.RequestContext = RequestContext;
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

        public override Uri UpdateRedirectUri(Uri redirectUri)
        {
            RedirectUriHelper.Validate(redirectUri, usesSystemBrowser: true);
            return redirectUri;
        }
    }
}
