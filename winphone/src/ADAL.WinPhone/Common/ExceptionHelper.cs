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
                case ActiveDirectoryAuthenticationError.InvalidCredentialType: 
                    message = ActiveDirectoryAuthenticationErrorMessage.InvalidCredentialType;
                    break;
#if ADAL_WINPHONE
#else
                case ActiveDirectoryAuthenticationError.CertificateKeySizeTooSmall:
                    message = string.Format(CultureInfo.InvariantCulture, ActiveDirectoryAuthenticationErrorMessage.CertificateKeySizeTooSmallTemplate, X509CertificateCredential.MinKeySizeInBits);
                    break;
#endif
                case ActiveDirectoryAuthenticationError.IdentityProtocolLoginUrlNull:
                    message = ActiveDirectoryAuthenticationErrorMessage.IdentityProtocolLoginUrlNull;
                    break;

                case ActiveDirectoryAuthenticationError.IdentityProtocolMismatch:
                    message = ActiveDirectoryAuthenticationErrorMessage.IdentityProtocolMismatch;
                    break;

                case ActiveDirectoryAuthenticationError.EmailAddressSuffixMismatch:
                    message = ActiveDirectoryAuthenticationErrorMessage.EmailAddressSuffixMismatch;
                    break;

                case ActiveDirectoryAuthenticationError.IdentityProviderRequestFailed:
                    message = ActiveDirectoryAuthenticationErrorMessage.IdentityProviderRequestFailed;
                    break;
            }

            // The switch case is divided into two to address the strange behavior of winmdidl tool for generating idl file from winmd for ADAL WinRT.
            if (message == null)
            {
                switch (errorCode)
                {
                    case ActiveDirectoryAuthenticationError.StsTokenRequestFailed:
                        message = ActiveDirectoryAuthenticationErrorMessage.StsTokenRequestFailed;
                        break;

                    case ActiveDirectoryAuthenticationError.EncodedTokenTooLong:
                        message = ActiveDirectoryAuthenticationErrorMessage.EncodedTokenTooLong;
                        break;

                    case ActiveDirectoryAuthenticationError.StsMetadataRequestFailed:
                        message = ActiveDirectoryAuthenticationErrorMessage.StsMetadataRequestFailed;
                        break;

                    case ActiveDirectoryAuthenticationError.AuthorityNotInValidList:
                        message = ActiveDirectoryAuthenticationErrorMessage.AuthorityNotInValidList;
                        break;

                    case ActiveDirectoryAuthenticationError.UnknownUserType:
                        message = ActiveDirectoryAuthenticationErrorMessage.UnknownUserType;
                        break;
                }
            }

            // The switch case is divided into two to address the strange behavior of winmdidl tool for generating idl file from winmd for ADAL WinRT.
            if (message == null)
            {
                switch (errorCode)
                {
                    case ActiveDirectoryAuthenticationError.UnknownUser:
                        message = ActiveDirectoryAuthenticationErrorMessage.UnknownUser;
                        break;

                    case ActiveDirectoryAuthenticationError.UserRealmDiscoveryFailed:
                        message = ActiveDirectoryAuthenticationErrorMessage.UserRealmDiscoveryFailed;
                        break;

                    case ActiveDirectoryAuthenticationError.AccessingWsMetadataExchangeFailed:
                        message = ActiveDirectoryAuthenticationErrorMessage.AccessingMetadataDocumentFailed;
                        break;

                    case ActiveDirectoryAuthenticationError.ParsingWsMetadataExchangeFailed:
                        message = ActiveDirectoryAuthenticationErrorMessage.ParsingMetadataDocumentFailed;
                        break;
                }
            }

            // The switch case is divided into two to address the strange behavior of winmdidl tool for generating idl file from winmd for ADAL WinRT.
            if (message == null)
            {
                switch (errorCode)
                {
                    case ActiveDirectoryAuthenticationError.WsTrustEndpointNotFoundInMetadataDocument:
                        message = ActiveDirectoryAuthenticationErrorMessage.WsTrustEndpointNotFoundInMetadataDocument;
                        break;

                    case ActiveDirectoryAuthenticationError.ParsingWsTrustResponseFailed:
                        message = ActiveDirectoryAuthenticationErrorMessage.ParsingWsTrustResponseFailed;
                        break;

                    case ActiveDirectoryAuthenticationError.AuthenticationCanceled:
                        message = ActiveDirectoryAuthenticationErrorMessage.AuthenticationCanceled;
                        break;

                    case ActiveDirectoryAuthenticationError.NetworkNotAvailable:
                        message = ActiveDirectoryAuthenticationErrorMessage.NetworkIsNotAvailable;
                        break;
                }
            }

            // The switch case is divided into two to address the strange behavior of winmdidl tool for generating idl file from winmd for ADAL WinRT.
            if (message == null)
            {
                switch (errorCode)
                {
                    case ActiveDirectoryAuthenticationError.AuthenticationUiFailed:
                        message = ActiveDirectoryAuthenticationErrorMessage.AuthenticationUiFailed;
                        break;

                    case ActiveDirectoryAuthenticationError.UserInteractionRequired:
                        message = ActiveDirectoryAuthenticationErrorMessage.UserInteractionRequired;
                        break;

                    case ActiveDirectoryAuthenticationError.FailedToAcquireTokenSilently:
                        message = ActiveDirectoryAuthenticationErrorMessage.FailedToAcquireTokenSilently;
                        break;

                    default:
                        message = ActiveDirectoryAuthenticationErrorMessage.Unknown;
                        break;
                }
            }

            return String.Format(CultureInfo.InvariantCulture, "{0}: {1}", errorCode, message);
        }
    }
}
