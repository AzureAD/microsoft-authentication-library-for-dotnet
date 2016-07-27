//----------------------------------------------------------------------
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

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    /// <summary>
    /// Static class that consumes the response from the Authentication flow and continues token acquisition. This class should be called in OnActivityResult() of the activity doing authentication.
    /// </summary>
    public static class AuthenticationAgentContinuationHelper
    {
        /// <summary>
        /// Sets authentication response from the webview or broker for token acquisition continuation.
        /// </summary>
        /// <param name="requestCode">Request response code</param>
        /// <param name="resultCode">Result code from authentication</param>
        /// <param name="data">Response data from authentication</param>
        public static void SetAuthenticationAgentContinuationEventArgs(int requestCode, Result resultCode, Intent data)
        {
            AuthorizationResult authorizationResult = null;
            PlatformPlugin.Logger.Information(null, string.Format(CultureInfo.InvariantCulture,"Received Activity Result({0})", (int)resultCode));
            switch ((int)resultCode)
            {
                case (int)Result.Ok:
                    authorizationResult = new AuthorizationResult(AuthorizationStatus.Success, data.GetStringExtra("ReturnedUrl"));
                    break;

                case (int)Result.Canceled:
                    authorizationResult = new AuthorizationResult(AuthorizationStatus.UserCancel, null);
                    break;

                case BrokerResponseCode.ResponseReceived:
                case BrokerResponseCode.BrowserCodeError:
                case BrokerResponseCode.UserCancelled:
                    BrokerHelper.SetBrokerResult(data, (int)resultCode);
                    break;

                default:
                    authorizationResult = new AuthorizationResult(AuthorizationStatus.UnknownError, null);
                    break;
            }

            if (authorizationResult != null)
            {
                WebUI.SetAuthorizationResult(authorizationResult);
            }
        }
    }
}
