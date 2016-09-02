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
        /// Unknown error.
        /// </summary>
        public const string Unknown = "unknown_error";

        /// <summary>
        /// Invalid argument.
        /// </summary>
        public const string InvalidArgument = "invalid_argument";

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
        /// Tenant discovery failed.
        /// </summary>
        public const string TenantDiscoveryFailed = "tenant_discovery_failed";

        /// <summary>
        /// Loading required assembly failed.
        /// </summary>
        public const string AssemblyLoadFailed = "assembly_load_failed";

        /// <summary>
        /// Assembly not found.
        /// </summary>
        public const string AssemblyNotFound = "assembly_not_found";

        /// <summary>
        /// Invalid owner window type.
        /// </summary>
        public const string InvalidOwnerWindowType = "invalid_owner_window_type";

        /// <summary>
        /// MultipleTokensMatched were matched.
        /// </summary>
        public const string MultipleTokensMatched = "multiple_matching_tokens_detected";

        /// <summary>
        /// Invalid cache operation was attempted.
        /// </summary>
        public const string InvalidCacheOperation = "invalid_cache_operation";

        /// <summary>
        /// Invalid authority type.
        /// </summary>
        public const string InvalidAuthorityType = "invalid_authority_type";

        /// <summary>
        /// Invalid credential type.
        /// </summary>
        public const string InvalidCredentialType = "invalid_credential_type";

        /// <summary>
        /// Invalid service URL.
        /// </summary>
        public const string InvalidServiceUrl = "invalid_service_url";

        /// <summary>
        /// failed_to_acquire_token_silently.
        /// </summary>
        public const string FailedToAcquireTokenSilently = "failed_to_acquire_token_silently";

        /// <summary>
        /// Certificate key size too small.
        /// </summary>
        public const string CertificateKeySizeTooSmall = "certificate_key_size_too_small";

        /// <summary>
        /// Identity protocol login URL Null.
        /// </summary>
        public const string IdentityProtocolLoginUrlNull = "identity_protocol_login_url_null";

        /// <summary>
        /// Identity protocol mismatch.
        /// </summary>
        public const string IdentityProtocolMismatch = "identity_protocol_mismatch";

        /// <summary>
        /// Email address suffix mismatch.
        /// </summary>
        public const string EmailAddressSuffixMismatch = "email_address_suffix_mismatch";

        /// <summary>
        /// Identity provider request failed.
        /// </summary>
        public const string IdentityProviderRequestFailed = "identity_provider_request_failed";

        /// <summary>
        /// STS token request failed.
        /// </summary>
        public const string StsTokenRequestFailed = "sts_token_request_failed";

        /// <summary>
        /// Encoded token too long.
        /// </summary>
        public const string EncodedTokenTooLong = "encoded_token_too_long";

        /// <summary>
        /// Service unavailable.
        /// </summary>
        public const string ServiceUnavailable = "service_unavailable";

        /// <summary>
        /// Service returned error.
        /// </summary>
        public const string ServiceReturnedError = "service_returned_error";

        /// <summary>
        /// Federated service returned error.
        /// </summary>
        public const string FederatedServiceReturnedError = "federated_service_returned_error";

        /// <summary>
        /// STS metadata request failed.
        /// </summary>
        public const string StsMetadataRequestFailed = "sts_metadata_request_failed";

        /// <summary>
        /// No data from STS.
        /// </summary>
        public const string NoDataFromSts = "no_data_from_sts";

        /// <summary>
        /// User Mismatch.
        /// </summary>
        public const string UserMismatch = "user_mismatch";

        /// <summary>
        /// Unknown User Type.
        /// </summary>
        public const string UnsupportedUserType = "unsupported_user_type";

        /// <summary>
        /// Unknown User.
        /// </summary>
        public const string UnknownUser = "unknown_user";

        /// <summary>
        /// User Realm Discovery Failed.
        /// </summary>
        public const string UserRealmDiscoveryFailed = "user_realm_discovery_failed";

        /// <summary>
        /// Accessing WS Metadata Exchange Failed.
        /// </summary>
        public const string AccessingWsMetadataExchangeFailed = "accessing_ws_metadata_exchange_failed";

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
        /// Password is required for managed user.
        /// </summary>
        public const string PasswordRequiredForManagedUserError = "password_required_for_managed_user";

        /// <summary>
        /// Failed to get user name.
        /// </summary>
        public const string GetUserNameFailed = "get_user_name_failed";

        /// <summary>
        /// Federation Metadata Url is missing for federated user.
        /// </summary>
        public const string MissingFederationMetadataUrl = "missing_federation_metadata_url";

        /// <summary>
        /// Failed to refresh token.
        /// </summary>
        public const string FailedToRefreshToken = "failed_to_refresh_token";

        /// <summary>
        /// Integrated authentication failed. You may try an alternative authentication method.
        /// </summary>
        public const string IntegratedAuthFailed = "integrated_authentication_failed";

        /// <summary>
        /// Duplicate query parameter in extraQueryParameters
        /// </summary>
        public const string DuplicateQueryParameter = "duplicate_query_parameter";

        /// <summary>
        /// Broker response hash did not match
        /// </summary>
        public const string BrokerReponseHashMismatch = "broker_response_hash_mismatch";

        /// <summary>
        /// Device certificate not found.
        /// </summary>
        public const string DeviceCertificateNotFound = "device_certificate_not_found";
    }
}