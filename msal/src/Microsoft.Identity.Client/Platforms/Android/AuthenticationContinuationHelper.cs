//------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System.Globalization;
using Android.App;
using Android.Content;
using Microsoft.Identity.Client.Internal;
using System;
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.UI.SystemWebview;
using Microsoft.Identity.Core.UI;

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
        public static void SetAuthenticationContinuationEventArgs(int requestCode, Result resultCode, Intent data)
        {
            RequestContext requestContext = new RequestContext(new MsalLogger(Guid.Empty, null));

            var msg = string.Format(CultureInfo.InvariantCulture, "Received Activity Result({0})", (int) resultCode);
            requestContext.Logger.Info(msg);
            requestContext.Logger.InfoPii(msg);
            AuthorizationResult authorizationResult = null;

            int code = (int)resultCode;

            if (data.Action != null && data.Action.Equals("ReturnFromEmbeddedWebview"))
            {
                authorizationResult = ProcessFromEmbeddedWebview(requestCode, resultCode, data);
            }
            else
            {
                authorizationResult = ProcessFromSystemWebview(requestCode, resultCode, data);
            }

            WebviewBase.SetAuthorizationResult(authorizationResult, requestContext);
        }

        private static AuthorizationResult ProcessFromEmbeddedWebview(int requestCode, Result resultCode, Intent data)
        {
            switch ((int)resultCode)
            {
                case (int)Result.Ok:
                    return new AuthorizationResult(AuthorizationStatus.Success, data.GetStringExtra("ReturnedUrl"));

                case (int)Result.Canceled:
                    return new AuthorizationResult(AuthorizationStatus.UserCancel, null);

                default:
                    return new AuthorizationResult(AuthorizationStatus.UnknownError, null);
            }
        }

        private static AuthorizationResult ProcessFromSystemWebview(int requestCode, Result resultCode, Intent data)
        {
            switch ((int)resultCode)
            {
                case AndroidConstants.AuthCodeReceived:
                    return CreateResultForOkResponse(data.GetStringExtra("com.microsoft.identity.client.finalUrl"));

                case AndroidConstants.Cancel:
                    return new AuthorizationResult(AuthorizationStatus.UserCancel, null);

                default:
                    return new AuthorizationResult(AuthorizationStatus.UnknownError, null);
            }
        }


            private static AuthorizationResult CreateResultForOkResponse(string url)
        {
            AuthorizationResult result = new AuthorizationResult(AuthorizationStatus.Success);

            if (!string.IsNullOrEmpty(url))
            {
                result.ParseAuthorizeResponse(url);
            }

            return result;
        }
    }
}