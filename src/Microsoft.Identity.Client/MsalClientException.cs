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
using System.Globalization;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client
{
    public class MsalClientException : MsalException
    {
        private static readonly Dictionary<string, string> ErrorMessages = new Dictionary<string, string>
        {
            [MsalError.InvalidCredentialType] = MsalErrorMessage.InvalidCredentialType,
            [MsalError.IdentityProtocolLoginUrlNull] = MsalErrorMessage.IdentityProtocolLoginUrlNull,
            [MsalError.IdentityProtocolMismatch] = MsalErrorMessage.IdentityProtocolMismatch,
            [MsalError.EmailAddressSuffixMismatch] = MsalErrorMessage.EmailAddressSuffixMismatch,
            [MsalError.IdentityProviderRequestFailed] = MsalErrorMessage.IdentityProviderRequestFailed,
            [MsalError.StsTokenRequestFailed] = MsalErrorMessage.StsTokenRequestFailed,
            [MsalError.EncodedTokenTooLong] = MsalErrorMessage.EncodedTokenTooLong,
            [MsalError.StsMetadataRequestFailed] = MsalErrorMessage.StsMetadataRequestFailed,
            [MsalError.AuthorityNotInValidList] = MsalErrorMessage.AuthorityNotInValidList,
            [MsalError.UnsupportedUserType] = MsalErrorMessage.UnsupportedUserType,
            [MsalError.UnknownUser] = MsalErrorMessage.UnknownUser,
            [MsalError.UserRealmDiscoveryFailed] = MsalErrorMessage.UserRealmDiscoveryFailed,
            [MsalError.AccessingWsMetadataExchangeFailed] = MsalErrorMessage.AccessingMetadataDocumentFailed,
            [MsalError.ParsingWsMetadataExchangeFailed] = MsalErrorMessage.ParsingMetadataDocumentFailed,
            [MsalError.WsTrustEndpointNotFoundInMetadataDocument] = MsalErrorMessage.WsTrustEndpointNotFoundInMetadataDocument,
            [MsalError.ParsingWsTrustResponseFailed] = MsalErrorMessage.ParsingWsTrustResponseFailed,
            [MsalError.AuthenticationCanceled] = MsalErrorMessage.AuthenticationCanceled,
            [MsalError.NetworkNotAvailable] = MsalErrorMessage.NetworkIsNotAvailable,
            [MsalError.AuthenticationUiFailed] = MsalErrorMessage.AuthenticationUiFailed,
            [MsalError.UserInteractionRequired] = MsalErrorMessage.UserInteractionRequired,
            [MsalError.MissingFederationMetadataUrl] = MsalErrorMessage.MissingFederationMetadataUrl,
            [MsalError.IntegratedAuthFailed] = MsalErrorMessage.IntegratedAuthFailed,
            [MsalError.UnauthorizedResponseExpected] = MsalErrorMessage.UnauthorizedResponseExpected,
            [MsalError.MultipleTokensMatched] = MsalErrorMessage.MultipleTokensMatched,
            [MsalError.PasswordRequiredForManagedUserError] = MsalErrorMessage.PasswordRequiredForManagedUserError,
            [MsalError.GetUserNameFailed] = MsalErrorMessage.GetUserNameFailed,
            // MsalErrorMessage.Unknown will be set as the default error message in GetErrorMessage(string errorCode).
        };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="errorCode"></param>
        public MsalClientException(string errorCode) : base(errorCode)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="errorCode"></param>
        /// <param name="errorMessage"></param>
        public MsalClientException(string errorCode, string errorMessage):base(errorCode, errorMessage)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="errorCode"></param>
        /// <param name="errorMessage"></param>
        /// <param name="innerException"></param>
        public MsalClientException(string errorCode, string errorMessage, Exception innerException):base(errorCode, errorMessage, innerException)
        {
        }

        /// <summary>
        /// Gets the Error Message
        /// </summary>
        protected static string GetErrorMessage(string errorCode)
        {
            string message = ErrorMessages.ContainsKey(errorCode) ? ErrorMessages[errorCode] : MsalErrorMessage.Unknown;
            return string.Format(CultureInfo.InvariantCulture, "{0}: {1}", errorCode, message);
        }
    }
}
