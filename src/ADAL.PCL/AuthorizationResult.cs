//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

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
        internal AuthorizationResult(AuthorizationStatus status, string returnedUriInput)
        {
            this.Status = status;

            if (this.Status == AuthorizationStatus.UserCancel)
            {
                this.Error = AdalError.AuthenticationCanceled;
                this.ErrorDescription = AdalErrorMessage.AuthenticationCanceled;
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
        public string Error { get; private set; }

        [DataMember]
        public string ErrorDescription { get; private set; }

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
                else if (response.ContainsKey(TokenResponseClaim.Error))
                {
                    this.Error = response[TokenResponseClaim.Error];
                    this.ErrorDescription = response.ContainsKey(TokenResponseClaim.ErrorDescription) ? response[TokenResponseClaim.ErrorDescription] : null;
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
