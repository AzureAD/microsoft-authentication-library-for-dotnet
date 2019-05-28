// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using Android.App;
using Android.Content;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Platforms.Android;
using Microsoft.Identity.Client.Platforms.Android.SystemWebview;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Static class that consumes the response from the Authentication flow and continues token acquisition. This class should be called in OnActivityResult() of the activity doing authentication.
    /// </summary>
    public static class AuthenticationContinuationHelper
    {
        /// <summary>
        /// Sets authentication response from the webview for token acquisition continuation.
        /// </summary>
        /// <param name="requestCode">Request response code</param>
        /// <param name="resultCode">Result code from authentication</param>
        /// <param name="data">Response data from authentication</param>
        [CLSCompliant(false)]
        public static void SetAuthenticationContinuationEventArgs(int requestCode, Result resultCode, Intent data)
        {
            // TODO(migration): how can a public static method get access to the proper ClientRequestBase to wire into the logger and appropriate requestcontext?
            // Can we move this call to be somewhere on the ClientApplicationBase or something else that's wired into that?
            var logger = MsalLogger.Create(Guid.Empty, null);
            logger.Info(string.Format(CultureInfo.InvariantCulture, "Received Activity Result({0})", (int)resultCode));

            AuthorizationResult authorizationResult;
            if (data.Action != null && data.Action.Equals("ReturnFromEmbeddedWebview", StringComparison.OrdinalIgnoreCase))
            {
                authorizationResult = ProcessFromEmbeddedWebview(requestCode, resultCode, data);
            }
            else
            {
                authorizationResult = ProcessFromSystemWebview(requestCode, resultCode, data);
            }

            WebviewBase.SetAuthorizationResult(authorizationResult, logger);
        }

        private static AuthorizationResult ProcessFromEmbeddedWebview(int requestCode, Result resultCode, Intent data)
        {
            switch ((int)resultCode)
            {
            case (int)Result.Ok:
                return AuthorizationResult.FromUri(data.GetStringExtra("ReturnedUrl"));


            case (int)Result.Canceled:
                return AuthorizationResult.FromStatus(AuthorizationStatus.UserCancel);

            default:
                return AuthorizationResult.FromStatus(AuthorizationStatus.UnknownError);
            }
        }

        private static AuthorizationResult ProcessFromSystemWebview(int requestCode, Result resultCode, Intent data)
        {
            switch ((int)resultCode)
            {
            case AndroidConstants.AuthCodeReceived:
                return AuthorizationResult.FromUri(data.GetStringExtra("com.microsoft.identity.client.finalUrl"));

            case AndroidConstants.Cancel:
                return AuthorizationResult.FromStatus(AuthorizationStatus.UserCancel);

            default:
                return AuthorizationResult.FromStatus(AuthorizationStatus.UnknownError);
            }
        }
    }
}
