//------------------------------------------------------------------------------
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

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Error code returned as a property in MsalException
    /// </summary>
    internal static class MsalError
    {
        /// <summary>
        /// Authentication failed.
        /// </summary>
        public const string AuthenticationFailed = "authentication_failed";


        /// <summary>
        /// Authority validation failed.
        /// </summary>
        public const string AuthorityValidationFailed = "authority_validation_failed";

        /// <summary>
        /// Invalid owner window type.
        /// </summary>
        public const string InvalidOwnerWindowType = "invalid_owner_window_type";

        /// <summary>
        /// Invalid authority type.
        /// </summary>
        public const string InvalidAuthorityType = "invalid_authority_type";

        /// <summary>
        /// Invalid service URL.
        /// </summary>
        public const string InvalidServiceUrl = "invalid_service_url";

        /// <summary>
        /// Encoded token too long.
        /// </summary>
        public const string EncodedTokenTooLong = "encoded_token_too_long";

        /// <summary>
        /// No data from STS.
        /// </summary>
        public const string NoDataFromSts = "no_data_from_sts";

        /// <summary>
        /// User Mismatch.
        /// </summary>
        public const string UserMismatch = "user_mismatch";

        /// <summary>
        /// Failed to refresh token.
        /// </summary>
        public const string FailedToRefreshToken = "failed_to_refresh_token";

        /// <summary>
        /// RedirectUri validation failed.
        /// </summary>
        public const string RedirectUriValidationFailed = "redirect_uri_validation_failed";

        /// <summary>
        /// The request could not be preformed because of an unknown failure in the UI flow.
        /// </summary>
        public const string AuthenticationUiFailed = "authentication_ui_failed";

        /// <summary>
        /// Non https redirect failed
        /// </summary>
        public const string NonHttpsRedirectNotSupported = "non_https_redirect_failed";

        /// <summary>
        /// Internal error
        /// </summary>
        public const string InternalError = "internal_error";

        /// <summary>
        /// Accessing WS Metadata Exchange Failed.
        /// </summary>
        public const string AccessingWsMetadataExchangeFailed = "accessing_ws_metadata_exchange_failed";

        /// <summary>
        /// Federated service returned error.
        /// </summary>
        public const string FederatedServiceReturnedError = "federated_service_returned_error";

        /// <summary>
        /// User Realm Discovery Failed.
        /// </summary>
        public const string UserRealmDiscoveryFailed = "user_realm_discovery_failed";

        /// <summary>
        /// Federation Metadata Url is missing for federated user.
        /// </summary>
        public const string MissingFederationMetadataUrl = "missing_federation_metadata_url";

        /// <summary>
        /// Parsing WS Metadata Exchange Failed.
        /// </summary>
        public const string ParsingWsMetadataExchangeFailed = "parsing_ws_metadata_exchange_failed";

        /// <summary>
        /// WS-Trust Endpoint Not Found in Metadata Document.
        /// </summary>
        public const string WsTrustEndpointNotFoundInMetadataDocument = "wstrust_endpoint_not_found";

        /// <summary>
        /// Parsing WS-Trust Response Failed.
        /// </summary>
        public const string ParsingWsTrustResponseFailed = "parsing_wstrust_response_failed";

        /// <summary>
        /// Unknown User Type.
        /// </summary>
        public const string UnknownUserType = "unknown_user_type";

        /// <summary>
        /// Unknown User.
        /// </summary>
        public const string UnknownUser = "unknown_user";

        /// <summary>
        /// Failed to get user name.
        /// </summary>
        public const string GetUserNameFailed = "get_user_name_failed";
    }
}