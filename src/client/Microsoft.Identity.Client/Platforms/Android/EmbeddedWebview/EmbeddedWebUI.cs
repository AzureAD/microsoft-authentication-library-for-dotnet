// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.Platforms.Android.EmbeddedWebview
{
    internal class EmbeddedWebUI : WebviewBase
    {
        private readonly CoreUIParent _coreUIParent;
        public RequestContext RequestContext { get; internal set; }

        public EmbeddedWebUI(CoreUIParent coreUIParent)
        {
            _coreUIParent = coreUIParent;
        }

        public async override Task<AuthorizationResult> AcquireAuthorizationAsync(
            Uri authorizationUri,
            Uri redirectUri,
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
            returnedUriReady = new SemaphoreSlim(0);

            try
            {
                var agentIntent = new Intent(_coreUIParent.CallerActivity, typeof(AuthenticationAgentActivity));
                agentIntent.PutExtra("Url", authorizationUri.AbsoluteUri);
                agentIntent.PutExtra("Callback", redirectUri.AbsoluteUri);
                _coreUIParent.CallerActivity.StartActivityForResult(agentIntent, 0);
            }
            catch (Exception ex)
            {
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
            RedirectUriHelper.Validate(redirectUri, usesSystemBrowser: false);
            return redirectUri;
        }
    }
}
