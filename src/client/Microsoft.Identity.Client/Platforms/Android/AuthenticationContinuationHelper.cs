// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using Android.App;
using Android.Content;
using Java.Sql;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Logger;
using Microsoft.Identity.Client.Platforms.Android;
using Microsoft.Identity.Client.Platforms.Android.Broker;
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

            if (data != null && data.Action != null && data.Action.Equals("ReturnFromEmbeddedWebview", StringComparison.OrdinalIgnoreCase))
            {
                authorizationResult = ProcessFromEmbeddedWebview(requestCode, resultCode, data);
            }
            else if (data != null && (!String.IsNullOrEmpty(data.GetStringExtra(BrokerConstants.BrokerResultV2)) || requestCode == BrokerConstants.BrokerRequestId))                
            {
                //The BrokerRequestId is an ID that is attached to the activity launch during brokered authentication
                // that indicates that the response returned to this class is for the broker.
                AndroidBroker.SetBrokerResult(data, (int)resultCode);
                return;
            }
            else if (data != null || AndroidConstants.AuthCodeReceived != (int)resultCode)
            {
                authorizationResult = ProcessFromSystemWebview(requestCode, resultCode, data);
            } 
            else
            {
                logger.Info("SetAuthenticationContinuationEventArgs - ignoring intercepted null intent.");
                return;
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
