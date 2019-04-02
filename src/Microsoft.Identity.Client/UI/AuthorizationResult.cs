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

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.Identity.Client.Exceptions;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.UI
{
    internal enum AuthorizationStatus
    {
        Success,
        ErrorHttp,
        ProtocolError,
        UserCancel,
        UnknownError
    }

    [DataContract]
    internal class AuthorizationResult
    {
        internal AuthorizationResult(AuthorizationStatus status, string returnedUriInput) : this(status)
        {
            if (Status == AuthorizationStatus.UserCancel)
            {
                Error = MsalClientException.AuthenticationCanceledError;
                #if ANDROID
                ErrorDescription = MsalErrorMessage.AuthenticationCanceledAndroid;
                #else
                ErrorDescription = MsalErrorMessage.AuthenticationCanceled;
                #endif
            }
            else if (Status == AuthorizationStatus.UnknownError)
            {
                Error = MsalException.UnknownError;
                ErrorDescription = MsalErrorMessage.Unknown;
            }
            else
            {
                ParseAuthorizeResponse(returnedUriInput);
            }
        }

        internal AuthorizationResult(AuthorizationStatus status)
        {
            Status = status;
        }

        public AuthorizationStatus Status { get; private set; }

        [DataMember]
        public string Code { get; private set; }

        [DataMember]
        public string Error { get; set; }

        [DataMember]
        public string ErrorDescription { get; set; }

        [DataMember]
        public string CloudInstanceHost { get; set; }

        /// <summary>
        /// A string that is added to each Authroization Request and is expected to be sent back along with the 
        /// authorization code. MSAL is responsible for validating that the state sent is identical to the state received.
        /// </summary>
        /// <remarks>
        /// This is in addition to PKCE, which is validated by the server to ensure that the system redeeming the auth code
        /// is the same as the system who asked for it. It protects against XSRF https://openid.net/specs/openid-connect-core-1_0.html
        /// </remarks>
        public string State { get; set; }

        public void ParseAuthorizeResponse(string webAuthenticationResult)
        {
            var resultUri = new Uri(webAuthenticationResult);

            // NOTE: The Fragment property actually contains the leading '#' character and that must be dropped
            string resultData = resultUri.Query;

            if (!string.IsNullOrWhiteSpace(resultData))
            {
                // RemoveAccount the leading '?' first
                Dictionary<string, string> response = CoreHelpers.ParseKeyValueList(resultData.Substring(1), '&',
                    true, null);

                if (response.ContainsKey(OAuth2Parameter.State))
                {
                    State = response[OAuth2Parameter.State];
                }

                if (response.ContainsKey(TokenResponseClaim.Code))
                {
                    Code = response[TokenResponseClaim.Code];
                }
                else if (webAuthenticationResult.StartsWith("msauth://", StringComparison.OrdinalIgnoreCase))
                {
                    Code = webAuthenticationResult;
                }
                else if (response.ContainsKey(TokenResponseClaim.Error))
                {
                    Error = response[TokenResponseClaim.Error];
                    ErrorDescription = response.ContainsKey(TokenResponseClaim.ErrorDescription)
                        ? response[TokenResponseClaim.ErrorDescription]
                        : null;
                    Status = AuthorizationStatus.ProtocolError;
                }
                else
                {
                    Error = MsalClientException.AuthenticationFailed;
                    ErrorDescription = MsalErrorMessage.AuthorizationServerInvalidResponse;
                    Status = AuthorizationStatus.UnknownError;
                }

                if (response.ContainsKey(TokenResponseClaim.CloudInstanceHost))
                {
                    CloudInstanceHost = response[TokenResponseClaim.CloudInstanceHost];
                }
            }
            else
            {
                Error = MsalClientException.AuthenticationFailed;
                ErrorDescription = MsalErrorMessage.AuthorizationServerInvalidResponse;
                Status = AuthorizationStatus.UnknownError;
            }
        }
    }
}
