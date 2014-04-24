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
using System.Runtime.Serialization;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    using System.Collections.Generic;
    using System.Net;

    /// <summary>
    /// Represents the outcome of one authentication operation.
    /// </summary>
    public enum AuthenticationStatus
    {
        /// <summary>
        /// Authentication Succeeded.
        /// </summary>
        Succeeded = 0,

        /// <summary>
        /// Authentication Failed.
        /// </summary>
        Failed = -1,
    }

    /// <summary>
    /// Contains the results of one token acquisition operation. 
    /// It can either contain the requested token (and supporting data) or information about why the token acquisition failed.
    /// </summary>
    [DataContract]
    public sealed partial class AuthenticationResult
    {
        internal AuthenticationResult(string error, string errorDescription)
        {
            this.Status = AuthenticationStatus.Failed;
            this.Error = error;
            this.ErrorDescription = errorDescription;
        }

        internal AuthenticationResult(Exception ex)
        {
            this.Status = AuthenticationStatus.Failed;
            if (ex is ArgumentNullException)
            {
                this.Error = ActiveDirectoryAuthenticationError.InvalidArgument;
                this.ErrorDescription = string.Format(ActiveDirectoryAuthenticationErrorMessage.NullParameterTemplate, ((ArgumentNullException)ex).ParamName);
            }
            else if (ex is ArgumentException)
            {
                this.Error = ActiveDirectoryAuthenticationError.InvalidArgument;
                this.ErrorDescription = ex.Message;
            }
            else if (ex is ActiveDirectoryAuthenticationException)
            {
                this.Error = ((ActiveDirectoryAuthenticationException)ex).ErrorCode;
                this.ErrorDescription = (ex.InnerException != null) ? ex.Message + ". " + ex.InnerException.Message : ex.Message;
                WebException webException = ex.InnerException as WebException;
                if (webException != null && webException.Response != null)
                {
                    var expectedResponseHeaders = new Dictionary<string, string> 
                        {
                            // Set to null to be filled in method SendPostRequestAndDeserializeJsonResponseAsync
                            { OAuthHeader.CorrelationId, null }
                        };

                    HttpHelper.CopyHeadersTo(webException.Response.Headers, expectedResponseHeaders);
                }
            }
            else
            {
                this.Error = ActiveDirectoryAuthenticationError.AuthenticationFailed;
                this.ErrorDescription = ex.Message;
            }
        }

        /// <summary>
        /// Gets the outcome of the token acquisition operation.
        /// </summary>
        [DataMember]
        public AuthenticationStatus Status { get; private set; }

        /// <summary>
        /// Gets provides error type in case of error.
        /// </summary>
        [DataMember]
        public string Error { get; private set; }

        /// <summary>
        /// Gets detailed information in case of error.
        /// </summary>
        [DataMember]
        public string ErrorDescription { get; private set; }
    }
}
