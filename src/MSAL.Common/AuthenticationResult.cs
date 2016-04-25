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
using System.Runtime.Serialization;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    /// <summary>
    /// Represents the outcome of one authentication operation.
    /// </summary>
    public enum AuthenticationStatus
    {
        /// <summary>
        /// Authentication Success.
        /// </summary>
        Success = 0,

        /// <summary>
        /// Authentication failed due to error on client side.
        /// </summary>
        ClientError = -1,

        /// <summary>
        /// Authentication failed due to error returned by service.
        /// </summary>
        ServiceError = -2,
    }

    /// <summary>
    /// Contains the results of one token acquisition operation. 
    /// It can either contain the requested token (and supporting data) or information about why the token acquisition failed.
    /// </summary>
    [DataContract]
    public sealed partial class AuthenticationResult
    {
        internal AuthenticationResult(Exception ex)
        {
            this.Status = AuthenticationStatus.ClientError;
            this.StatusCode = 0;
            if (ex is ArgumentNullException)
            {
                this.Error = AdalError.InvalidArgument;
                this.ErrorDescription = string.Format(AdalErrorMessage.NullParameterTemplate, ((ArgumentNullException)ex).ParamName);
            }
            else if (ex is ArgumentException)
            {
                this.Error = AdalError.InvalidArgument;
                this.ErrorDescription = ex.Message;
            }
            else if (ex is AdalException)
            {
                this.Error = ((AdalException)ex).ErrorCode;
                this.ErrorDescription = (ex.InnerException != null) ? ex.Message + ". " + ex.InnerException.Message : ex.Message;
                AdalServiceException serviceException = ex as AdalServiceException;
                if (serviceException != null)
                {
                    this.Status = AuthenticationStatus.ServiceError;
                    this.StatusCode = serviceException.StatusCode;
                }
            }
            else
            {
                this.Error = AdalError.AuthenticationFailed;
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

        /// <summary>
        /// Gets the status code returned from http layer if any error happens. This status code is either the HttpStatusCode in the inner WebException response or
        /// NavigateError Event Status Code in browser based flow (See http://msdn.microsoft.com/en-us/library/bb268233(v=vs.85).aspx).
        /// You can use this code for purposes such as implementing retry logic or error investigation.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// The Windows Runtime string type is a value type and has no null value. 
        /// The .NET projection prohibits passing a null .NET string across the Windows Runtime ABI boundary for this reason.
        /// </summary>
        internal void ReplaceNullStringPropertiesWithEmptyString()
        {
            this.AccessToken = this.AccessToken ?? string.Empty;
            this.AccessTokenType = this.AccessTokenType ?? string.Empty;
            this.Error = this.Error ?? string.Empty;
            this.ErrorDescription = this.ErrorDescription ?? string.Empty;
            this.IdToken = this.IdToken ?? string.Empty;
            this.RefreshToken = this.RefreshToken ?? string.Empty;
            this.TenantId = this.TenantId ?? string.Empty;
            if (this.UserInfo != null)
            {
                this.UserInfo.DisplayableId = this.UserInfo.DisplayableId ?? string.Empty;
                this.UserInfo.FamilyName = this.UserInfo.FamilyName ?? string.Empty;
                this.UserInfo.GivenName = this.UserInfo.GivenName ?? string.Empty;
                this.UserInfo.IdentityProvider = this.UserInfo.IdentityProvider ?? string.Empty;
            }
        }
    }
}
