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

namespace Microsoft.Identity.Client.Internal
{
    /// <summary>
    /// Error code returned as a property in MsalException
    /// </summary>
    public static class MsalError
    {
        /// <summary>
        /// Tenant discovery failed.
        /// </summary>
        public const string TenantDiscoveryFailed = "tenant_discovery_failed";


        /// <summary>
        /// Unknown error.
        /// </summary>
        public const string Unknown = "unknown_error";

        /// <summary>
        /// Authentication failed.
        /// </summary>
        public const string AuthenticationFailed = "authentication_failed";

        /// <summary>
        /// Invalid credential type.
        /// </summary>
        public const string NonHttpsRedirectNotSupported = "non_https_redirect_failed";

        /// <summary>
        /// Authentication canceled.
        /// </summary>
        public const string AuthenticationCanceled = "authentication_canceled";

        /// <summary>
        /// Invalid credential type.
        /// </summary>
        public const string HttpRequestCancelled = "http_request_cancelled";

        /// <summary>
        /// Unauthorized response expected from resource server.
        /// </summary>
        public const string UnauthorizedResponseExpected = "unauthorized_response_expected";

        /// <summary>
        /// 'authority' is not in the list of valid addresses.
        /// </summary>
        public const string AuthorityNotInValidList = "authority_not_in_valid_list";

        /// <summary>
        /// Authority validation failed.
        /// </summary>
        public const string AuthorityValidationFailed = "authority_validation_failed";

        /// <summary>
        /// Invalid owner window type.
        /// </summary>
        public const string InvalidOwnerWindowType = "invalid_owner_window_type";

        /// <summary>
        /// MultipleTokensMatched were matched.
        /// </summary>
        public const string MultipleTokensMatched = "multiple_matching_tokens_detected";

        /// <summary>
        /// Invalid authority type.
        /// </summary>
        public const string InvalidAuthorityType = "invalid_authority_type";

        /// <summary>
        /// Invalid service URL.
        /// </summary>
        public const string InvalidServiceUrl = "invalid_service_url";

        /// <summary>
        /// Certificate key size too small.
        /// </summary>
        public const string CertificateKeySizeTooSmall = "certificate_key_size_too_small";

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
        /// The request could not be preformed because the network is down.
        /// </summary>
        public const string NetworkNotAvailable = "network_not_available";

        /// <summary>
        /// The request could not be preformed because of an unknown failure in the UI flow.
        /// </summary>
        public const string AuthenticationUiFailed = "authentication_ui_failed";

        /// <summary>
        /// One of two conditions was encountered.
        /// 1. The PromptBehavior.Never flag was passed and but the constraint could not be honored
        /// because user interaction was required.
        /// 2. An error occurred during a silent web authentication that prevented the authentication
        /// flow from completing in a short enough time frame.
        /// </summary>
        public const string UserInteractionRequired = "user_interaction_required";

        /// <summary>
        /// Failed to refresh token.
        /// </summary>
        public const string FailedToRefreshToken = "failed_to_refresh_token";

        /// <summary>
        /// Duplicate query parameter in extraQueryParameters
        /// </summary>
        public const string DuplicateQueryParameter = "duplicate_query_parameter";
        
    }
}