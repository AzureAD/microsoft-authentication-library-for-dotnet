// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.Platforms.Android
{
    internal abstract class WebviewBase : IWebUI
    {
        protected static SemaphoreSlim returnedUriReady;
        protected static AuthorizationResult authorizationResult;

        public static void SetAuthorizationResult(AuthorizationResult authorizationResultInput, RequestContext requestContext)
        {
            if (returnedUriReady != null)
            {
                authorizationResult = authorizationResultInput;
                returnedUriReady.Release();
            }
            else
            {
                requestContext.Logger.Info("No pending request for response from web ui.");
            }
        }

        public static void SetAuthorizationResult(AuthorizationResult authorizationResultInput)
        {
            authorizationResult = authorizationResultInput;
            returnedUriReady.Release();
        }

        public abstract Task<AuthorizationResult> AcquireAuthorizationAsync(
            Uri authorizationUri,
            Uri redirectUri,
            RequestContext requestContext,
            CancellationToken cancellationToken);

        public abstract void ValidateRedirectUri(Uri redirectUri);
    }
}
