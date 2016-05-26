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
using System.Xml.Linq;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal static class AdalErrorEx
    {
        public const string UnauthorizedUserInformationAccess = "unauthorized_user_information_access";
        public const string CannotAccessUserInformation = "user_information_access_failed";
        public const string NeedToSetCallbackUriAsLocalSetting = "need_to_set_callback_uri_as_local_setting";
        public const string DeviceCodeAuthorizationPendingError = "authorization_pending";
    }

    internal static class AdalErrorMessageEx
    {
        public const string CannotAccessUserInformation = "Cannot access user information. Check machine's Privacy settings or initialize UserCredential with userId";
        public const string RedirectUriUnsupportedWithPromptBehaviorNever = "PromptBehavior.Never is supported in SSO mode only (null or application's callback URI as redirectUri)";
        public const string UnauthorizedUserInformationAccess = "Unauthorized to access user information. Check application's 'Enterprise Authentication' capability";
        public const string NeedToSetCallbackUriAsLocalSetting = "You need to add the value of WebAuthenticationBroker.GetCurrentApplicationCallbackUri() to an application's local setting named CurrentApplicationCallbackUri.";
    }

    internal static class Constant
    {
        public const string MsAppScheme = "ms-app";
        public static readonly Uri SsoPlaceHolderUri = new Uri("https://sso");
    }

    /// <summary>
    /// Error code returned as a property in AdalException
    /// </summary>
    public static class AdalError
    {
        /// <summary>
        /// Unknown error.
        /// </summary>
        public const string Unknown = "unknown_error";

        /// <summary>
        /// Non https redirect failed
        /// </summary>
        public const string NonHttpsRedirectNotSupported = "non_https_redirect_failed";
        /// <summary>
        /// Invalid argument.
        /// </summary>
        public const string InvalidArgument = "invalid_argument";

        /// <summary>
        /// Authentication failed.
        /// </summary>
        public const string AuthenticationFailed = "authentication_failed";

        /// <summary>
        /// Authentication canceled.
        /// </summary>
        public const string AuthenticationCanceled = "authentication_canceled";

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
        public const string UnknownUserType = "unknown_user_type";

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
        ///    because user interaction was required.
        /// 2. An error occurred during a silent web authentication that prevented the authentication
        ///    flow from completing in a short enough time frame.
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

    /// <summary>
    /// The active directory authentication error message.
    /// </summary>
    internal static class AdalErrorMessage
    {
        public const string AccessingMetadataDocumentFailed = "Accessing WS metadata exchange failed";

        public const string AssemblyNotFoundTemplate =
            "Assembly required for the platform not found. Make sure assembly '{0}' exists";

        public const string AssemblyLoadFailedTemplate =
            "Loading an assembly required for the platform failed. Make sure assembly for the correct platform '{0}' exists";

        public const string AuthenticationUiFailed = "The browser based authentication dialog failed to complete";
        public const string AuthorityInvalidUriFormat = "'authority' should be in Uri format";
        public const string AuthorityNotInValidList = "'authority' is not in the list of valid addresses";
        public const string AuthorityValidationFailed = "Authority validation failed";
        public const string NonHttpsRedirectNotSupported = "Non-HTTPS url redirect is not supported in webview";
        public const string AuthorityUriInsecure = "'authority' should use the 'https' scheme";

        public const string AuthorityUriInvalidPath =
            "'authority' Uri should have at least one segment in the path (i.e. https://<host>/<path>/...)";

        public const string AuthorizationServerInvalidResponse = "The authorization server returned an invalid response";

        public const string CertificateKeySizeTooSmallTemplate =
            "The certificate used must have a key size of at least {0} bits";

        public const string EmailAddressSuffixMismatch =
            "No identity provider email address suffix matches the provided address";

        public const string EncodedTokenTooLong = "Encoded token size is beyond the upper limit";
        public const string FailedToAcquireTokenSilently = "Failed to acquire token silently as no token was found in the cache. Call method AcquireToken";
        public const string FailedToRefreshToken = "Failed to refresh access token";
        public const string FederatedServiceReturnedErrorTemplate = "Federated service at {0} returned error: {1}";
        public const string IdentityProtocolLoginUrlNull = "The LoginUrl property in identityProvider cannot be null";
        public const string IdentityProtocolMismatch = "No identity provider matches the requested protocol";

        public const string IdentityProviderRequestFailed =
            "Token request made to identity provider failed. Check InnerException for more details";

        public const string InvalidArgumentLength = "Parameter has invalid length";
        public const string InvalidAuthenticateHeaderFormat = "Invalid authenticate header format";
        public const string InvalidAuthorityTypeTemplate = "Invalid authority type. This method overload is not supported by '{0}'";
        public const string InvalidCredentialType = "Invalid credential type";
        public const string InvalidFormatParameterTemplate = "Parameter '{0}' has invalid format";
        public const string InvalidTokenCacheKeyFormat = "Invalid token cache key format";
        public const string MissingAuthenticateHeader = "WWW-Authenticate header was expected in the response";

        public const string MultipleTokensMatched =
            "The cache contains multiple tokens satisfying the requirements. Call AcquireToken again providing more arguments (e.g. UserId)";

        public const string NetworkIsNotAvailable = "The network is down so authentication cannot proceed";
        public const string NoDataFromSTS = "No data received from security token service";
        public const string NullParameterTemplate = "Parameter '{0}' cannot be null";
        public const string ParsingMetadataDocumentFailed = "Parsing WS metadata exchange failed";
        public const string ParsingWsTrustResponseFailed = "Parsing WS-Trust response failed";
        public const string PasswordRequiredForManagedUserError = "Password is required for managed user";
        public const string RedirectUriContainsFragment = "'redirectUri' must NOT include a fragment component";
        public const string ServiceReturnedError = "Service returned error. Check InnerException for more details";
        public const string BrokerReponseHashMismatch = "Unencrypted broker response hash did not match the expected hash";

        public const string StsMetadataRequestFailed =
            "Metadata request to Access Control service failed. Check InnerException for more details";

        public const string StsTokenRequestFailed =
            "Token request to security token service failed.  Check InnerException for more details";

        public const string UnauthorizedHttpStatusCodeExpected =
            "Unauthorized Http Status Code (401) was expected in the response";

        public const string UnauthorizedResponseExpected = "Unauthorized http response (status code 401) was expected";
        public const string UnexpectedAuthorityValidList = "Unexpected list of valid addresses";
        public const string Unknown = "Unknown error";
        public const string UnknownUser = "Could not identify logged in user";
        public const string UnknownUserType = "Unknown User Type";

        public const string UnsupportedAuthorityValidation =
            "Authority validation is not supported for this type of authority";

        public const string UnsupportedMultiRefreshToken =
            "This authority does not support refresh token for multiple resources. Pass null as a resource";

        public const string AuthenticationCanceled = "User canceled authentication";
        public const string UserMismatch = "User '{0}' returned by service does not match user '{1}' in the request";
        public const string UserCredentialAssertionTypeEmpty = "credential.AssertionType cannot be empty";

        public const string UserInteractionRequired =
            "One of two conditions was encountered: "
            +
            "1. The PromptBehavior.Never flag was passed, but the constraint could not be honored, because user interaction was required. "
            +
            "2. An error occurred during a silent web authentication that prevented the http authentication flow from completing in a short enough time frame";

        public const string UserRealmDiscoveryFailed = "User realm discovery failed";

        public const string WsTrustEndpointNotFoundInMetadataDocument =
            "WS-Trust endpoint not found in metadata document";

        public const string GetUserNameFailed = "Failed to get user name";

        public const string MissingFederationMetadataUrl =
            "Federation Metadata Url is missing for federated user. This user type is unsupported.";

        public const string SpecifyAnyUser =
            "If you do not need access token for any specific user, pass userId=UserIdentifier.AnyUser instead of userId=null.";

        public const string IntegratedAuthFailed =
            "Integrated authentication failed. You may try an alternative authentication method";

        public const string DuplicateQueryParameterTemplate = "Duplicate query parameter '{0}' in extraQueryParameters";
        
        public const string DeviceCertificateNotFoundTemplate = "Device Certificate was not found for {0}";
    }

    internal class XmlNamespace
    {
        public static readonly XNamespace Wsdl = "http://schemas.xmlsoap.org/wsdl/";
        public static readonly XNamespace Wsp = "http://schemas.xmlsoap.org/ws/2004/09/policy";
        public static readonly XNamespace Http = "http://schemas.microsoft.com/ws/06/2004/policy/http";
        public static readonly XNamespace Sp = "http://docs.oasis-open.org/ws-sx/ws-securitypolicy/200702";
        public static readonly XNamespace Sp2005 = "http://schemas.xmlsoap.org/ws/2005/07/securitypolicy";

        public static readonly XNamespace Wsu =
            "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";

        public static readonly XNamespace Soap12 = "http://schemas.xmlsoap.org/wsdl/soap12/";
        public static readonly XNamespace Wsa10 = "http://www.w3.org/2005/08/addressing";
        public static readonly XNamespace Trust = "http://docs.oasis-open.org/ws-sx/ws-trust/200512";
        public static readonly XNamespace Trust2005 = "http://schemas.xmlsoap.org/ws/2005/02/trust";
        public static readonly XNamespace Issue = "http://docs.oasis-open.org/ws-sx/ws-trust/200512/RST/Issue";
        public static readonly XNamespace Issue2005 = "http://schemas.xmlsoap.org/ws/2005/02/trust/RST/Issue";
        public static readonly XNamespace SoapEnvelope = "http://www.w3.org/2003/05/soap-envelope";
    }
    
}
