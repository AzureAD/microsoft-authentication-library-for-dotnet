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
using System.Globalization;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal static class ExceptionHelper
    {
        internal enum ErrorFormat
        {
            Json,
            Other
        }

        public static string GetErrorMessage(string errorCode)
        {
            string message = null;
            switch (errorCode)
            {
                case AdalError.InvalidCredentialType: 
                    message = AdalErrorMessage.InvalidCredentialType;
                    break;
#if ADAL_WINRT
#else
                case AdalError.CertificateKeySizeTooSmall:
                    message = string.Format(CultureInfo.InvariantCulture, AdalErrorMessage.CertificateKeySizeTooSmallTemplate, X509CertificateCredential.MinKeySizeInBits);
                    break;
#endif
                case AdalError.IdentityProtocolLoginUrlNull:
                    message = AdalErrorMessage.IdentityProtocolLoginUrlNull;
                    break;

                case AdalError.IdentityProtocolMismatch:
                    message = AdalErrorMessage.IdentityProtocolMismatch;
                    break;

                case AdalError.EmailAddressSuffixMismatch:
                    message = AdalErrorMessage.EmailAddressSuffixMismatch;
                    break;

                case AdalError.IdentityProviderRequestFailed:
                    message = AdalErrorMessage.IdentityProviderRequestFailed;
                    break;
            }

            // The switch case is divided into two to address the strange behavior of winmdidl tool for generating idl file from winmd for ADAL WinRT.
            if (message == null)
            {
                switch (errorCode)
                {
                    case AdalError.StsTokenRequestFailed:
                        message = AdalErrorMessage.StsTokenRequestFailed;
                        break;

                    case AdalError.EncodedTokenTooLong:
                        message = AdalErrorMessage.EncodedTokenTooLong;
                        break;

                    case AdalError.StsMetadataRequestFailed:
                        message = AdalErrorMessage.StsMetadataRequestFailed;
                        break;

                    case AdalError.AuthorityNotInValidList:
                        message = AdalErrorMessage.AuthorityNotInValidList;
                        break;

                    case AdalError.UnknownUserType:
                        message = AdalErrorMessage.UnknownUserType;
                        break;
                }
            }

            // The switch case is divided into two to address the strange behavior of winmdidl tool for generating idl file from winmd for ADAL WinRT.
            if (message == null)
            {
                switch (errorCode)
                {
                    case AdalError.UnknownUser:
                        message = AdalErrorMessage.UnknownUser;
                        break;

                    case AdalError.UserRealmDiscoveryFailed:
                        message = AdalErrorMessage.UserRealmDiscoveryFailed;
                        break;

                    case AdalError.AccessingWsMetadataExchangeFailed:
                        message = AdalErrorMessage.AccessingMetadataDocumentFailed;
                        break;

                    case AdalError.ParsingWsMetadataExchangeFailed:
                        message = AdalErrorMessage.ParsingMetadataDocumentFailed;
                        break;
                }
            }

            // The switch case is divided into two to address the strange behavior of winmdidl tool for generating idl file from winmd for ADAL WinRT.
            if (message == null)
            {
                switch (errorCode)
                {
                    case AdalError.WsTrustEndpointNotFoundInMetadataDocument:
                        message = AdalErrorMessage.WsTrustEndpointNotFoundInMetadataDocument;
                        break;

                    case AdalError.ParsingWsTrustResponseFailed:
                        message = AdalErrorMessage.ParsingWsTrustResponseFailed;
                        break;

                    case AdalError.AuthenticationCanceled:
                        message = AdalErrorMessage.AuthenticationCanceled;
                        break;

                    case AdalError.NetworkNotAvailable:
                        message = AdalErrorMessage.NetworkIsNotAvailable;
                        break;
                }
            }

            // The switch case is divided into two to address the strange behavior of winmdidl tool for generating idl file from winmd for ADAL WinRT.
            if (message == null)
            {
                switch (errorCode)
                {
                    case AdalError.AuthenticationUiFailed:
                        message = AdalErrorMessage.AuthenticationUiFailed;
                        break;

                    case AdalError.UserInteractionRequired:
                        message = AdalErrorMessage.UserInteractionRequired;
                        break;

                    case AdalError.FailedToAcquireTokenSilently:
                        message = AdalErrorMessage.FailedToAcquireTokenSilently;
                        break;
                }
            }

#if ADAL_WINRT
            // The switch case is divided into two to address the strange behavior of winmdidl tool for generating idl file from winmd for ADAL WinRT.
            if (message == null)
            {
                switch (errorCode)
                {
                    case AdalError.UnauthorizedUserInformationAccess:
                        message = AdalErrorMessage.UnauthorizedUserInformationAccess;
                        break;

                    case AdalError.CannotAccessUserInformation:
                        message = AdalErrorMessage.CannotAccessUserInformation;
                        break;
                }
            }
#endif

            // The switch case is divided into two to address the strange behavior of winmdidl tool for generating idl file from winmd for ADAL WinRT.
            if (message == null)
            {
                switch (errorCode)
                {
                    case AdalError.UnauthorizedResponseExpected:
                        message = AdalErrorMessage.UnauthorizedResponseExpected;
                        break;

                    case AdalError.MultipleTokensMatched:
                        message = AdalErrorMessage.MultipleTokensMatched;
                        break;

                    case AdalError.PasswordRequiredForManagedUserError:
                        message = AdalErrorMessage.PasswordRequiredForManagedUserError;
                        break;

                    default:
                        message = AdalErrorMessage.Unknown;
                        break;
                }
            }

            return String.Format(CultureInfo.InvariantCulture, "{0}: {1}", errorCode, message);
        }
    }
}
