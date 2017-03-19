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
using Microsoft.Identity.Client.Internal.OAuth2;

namespace Microsoft.Identity.Client.Internal
{
    internal enum AuthorizationStatus
    {
        Success,
        ErrorHttp,
        ProtocolError,
        UserCancel,
        UnknownError
    }
    
    internal class AuthorizationResult
    {
        internal AuthorizationResult(AuthorizationStatus status, string returnedUriInput) : this(status)
        {
            if (Status == AuthorizationStatus.UserCancel)
            {
                Error = MsalError.AuthenticationCanceled;
                ErrorDescription = MsalErrorMessage.AuthenticationCanceled;
            }
            else if (Status == AuthorizationStatus.UnknownError)
            {
                Error = MsalError.Unknown;
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
        
        public string Code { get; private set; }
        
        public string Error { get; set; }
        
        public string ErrorDescription { get; set; }
        
        public string State { get; set; }

        public void ParseAuthorizeResponse(string webAuthenticationResult)
        {
            var resultUri = new Uri(webAuthenticationResult);

            // NOTE: The Fragment property actually contains the leading '#' character and that must be dropped
            string resultData = resultUri.Query;

            if (!string.IsNullOrWhiteSpace(resultData))
            {
                // Remove the leading '?' first
                Dictionary<string, string> response = MsalHelpers.ParseKeyValueList(resultData.Substring(1), '&',
                    true, null);

                if (response.ContainsKey(OAuth2Parameter.State))
                {
                    State = response[OAuth2Parameter.State];
                }

                if (response.ContainsKey(TokenResponseClaim.Code))
                {
                    Code = response[TokenResponseClaim.Code];
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
                    Error = MsalError.AuthenticationFailed;
                    ErrorDescription = MsalErrorMessage.AuthorizationServerInvalidResponse;
                    Status = AuthorizationStatus.UnknownError;
                }
            }
            else
            {
                Error = MsalError.AuthenticationFailed;
                ErrorDescription = MsalErrorMessage.AuthorizationServerInvalidResponse;
                Status = AuthorizationStatus.UnknownError;
            }
        }
    }
}