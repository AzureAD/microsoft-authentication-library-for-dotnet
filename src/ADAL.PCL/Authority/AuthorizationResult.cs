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

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
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
        internal AuthorizationResult(AuthorizationStatus status)
        {
            this.Status = status;
        }

        internal AuthorizationResult(AuthorizationStatus status, string returnedUriInput) :this(status)
        {
            if (this.Status == AuthorizationStatus.UserCancel)
            {
                this.Error = AdalError.AuthenticationCanceled;
                this.ErrorDescription = AdalErrorMessage.AuthenticationCanceled;
            }
            else if (this.Status == AuthorizationStatus.UnknownError)
            {
                this.Error = AdalError.Unknown;
                this.ErrorDescription = AdalErrorMessage.Unknown;
            }
            else
            {
                this.ParseAuthorizeResponse(returnedUriInput);
            }
        }

        public AuthorizationStatus Status { get; private set; }

        [DataMember]
        public string Code { get; private set; }

        [DataMember]
        public string Error { get; set; }

        [DataMember]
        public string ErrorDescription { get; set; }

        public void ParseAuthorizeResponse(string webAuthenticationResult)
        {
            var resultUri = new Uri(webAuthenticationResult);

            // NOTE: The Fragment property actually contains the leading '#' character and that must be dropped
            string resultData = resultUri.Query;

            if (!string.IsNullOrWhiteSpace(resultData))
            {
                // Remove the leading '?' first
                Dictionary<string, string> response = EncodingHelper.ParseKeyValueList(resultData.Substring(1), '&', true, null);

                if (response.ContainsKey(TokenResponseClaim.Code))
                {
                    this.Code = response[TokenResponseClaim.Code];
                }
                else if (webAuthenticationResult.StartsWith("msauth://", StringComparison.CurrentCultureIgnoreCase))
                {
                    this.Code = webAuthenticationResult;
                }
                else if (response.ContainsKey(TokenResponseClaim.Error))
                {
                    this.Error = response[TokenResponseClaim.Error];
                    this.ErrorDescription = response.ContainsKey(TokenResponseClaim.ErrorDescription)
                        ? response[TokenResponseClaim.ErrorDescription]
                        : null;
                    this.Status = AuthorizationStatus.ProtocolError;
                }
                else
                {
                    this.Error = AdalError.AuthenticationFailed;
                    this.ErrorDescription = AdalErrorMessage.AuthorizationServerInvalidResponse;
                    this.Status = AuthorizationStatus.UnknownError;
                }
            }
            else
            {
                this.Error = AdalError.AuthenticationFailed;
                this.ErrorDescription = AdalErrorMessage.AuthorizationServerInvalidResponse;
                this.Status = AuthorizationStatus.UnknownError;
            }
        }
    }
}
