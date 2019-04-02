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
    public static class MsalError
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
        /// Failed to acquire token silently. Used in broker scenarios.
        /// </summary>
        public const string FailedToAcquireTokenSilentlyFromBroker = "failed_to_acquire_token_silently_from_broker";

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

        /// <summary>
        /// Password is required for managed user.
        /// </summary>
        public const string PasswordRequiredForManagedUserError = "password_required_for_managed_user";

        /// <summary>
        /// Request is invalid.
        /// </summary>
        public const string InvalidRequest = "invalid_request";

        /// <summary>
        /// Cannot access the user from the OS (UWP)
        /// </summary>
        public const string UapCannotFindDomainUser = "user_information_access_failed";

        /// <summary>
        /// Cannot get the user from the OS (UWP)
        /// </summary>
        public const string UapCannotFindUpn = "uap_cannot_find_upn";

        /// <summary>
        /// An error response was returned by the OAuth2 server and it could not be parsed
        /// </summary>
        public const string NonParsableOAuthError = "non_parsable_oauth_error";

        /// <summary>
        /// In the context of Device code flow (See https://aka.ms/msal-net-device-code-flow),
        /// this error happens when the device code expired before the user signed-in on another device (this is usually after 15 mins).
        /// 
        /// Mitigation: None. Inform the user that they took too long to sign-in at the provided URL and enter the provided code.
        /// </summary>
        public const string CodeExpired = "code_expired";

        /// <summary>
        /// Integrated Windows Auth is only supported for "federated" users
        /// </summary>
        public const string IntegratedWindowsAuthNotSupportedForManagedUser = "integrated_windows_auth_not_supported_managed_user";

        /// <summary>
        /// TODO: UPDATE DOCUMENTATION!
        /// On Android, the UIParent constructor with an Activiy parameter must be used. See https://aka.ms/msal-interactive-android
        /// </summary>
        public const string ActivityRequired = "activity_required";

        /// <summary>
        /// Broker response hash did not match
        /// </summary>
        public const string BrokerResponseHashMismatch = "broker_response_hash_mismatch";

        /// <summary>
        /// Broker response returned an error
        /// </summary>
        public const string BrokerResponseReturnedError = "broker_response_returned_error";

        /// <summary>
        /// MSAL is not able to invoke the broker. Possible reasons are the broker is not installed on the user's device, 
        /// or there were issues with the UiParent or CallerViewController being null. See https://aka.ms/msal-brokers
        /// </summary>
        public const string CannotInvokeBroker = "cannot_invoke_broker";

        /// <summary>
        /// Error code used when the http response returns HttpStatusCode.NotFound
        /// </summary>
        public const string HttpStatusNotFound = "not_found";

        /// <summary>
        /// ErrorCode used when the http response returns something different from 200 (OK)
        /// </summary>
        /// <remarks>
        /// HttpStatusCode.NotFound have a specific error code. <see cref="MsalError.HttpStatusNotFound"/>
        /// </remarks>
        public const string HttpStatusCodeNotOk = "http_status_not_200";

        /// <summary>
        /// Error code used when the CustomWebUI has returned an uri, but it is invalid - it is either null or has no code.
        /// Consider throwing an exception if you are unable to intercept the uri containing the code. 
        /// </summary>
        public const string CustomWebUiReturnedInvalidUri = "custom_webui_returned_invalid_uri";

        /// <summary>
        /// Error code used when the CustomWebUI has returned an uri, but it does not match the Authroity and AbsolutePath of 
        /// the configured redirect uri.
        /// </summary>
        public const string CustomWebUiRedirectUriMismatch = "custom_webui_invalid_mismatch";

        /// <summary>
        /// Access denied.
        /// </summary>
        public const string AccessDenied = "access_denied";

        /// <summary>
        /// JSON Parse error.
        /// </summary>
        public const string JsonParseError = "json_parse_failed";

        /// <summary>
        /// Request Timeout.
        /// </summary>
        public const string RequestTimeout = "request_timeout";

        /// <summary>
        /// Service not available.
        /// </summary>
        public const string ServiceNotAvailable = "service_not_available";

        /// <summary>
        /// Invalid JWT.
        /// </summary>
        public const string InvalidJwtError = "invalid_jwt";

        /// <summary>
        /// Tenant Discovery Failed.
        /// </summary>
        public const string TenantDiscoveryFailedError = "tenant_discovery_failed";

        /// <summary>
        /// Authentication UI Failed.
        /// </summary>
        public const string AuthenticationUiFailedError = "authentication_ui_failed";

        /// <summary>
        /// Invalid Grant.
        /// </summary>
        public const string InvalidGrantError = "invalid_grant";

        /// <summary>
        /// Unknown Error.
        /// </summary>
        public const string UnknownError = "unknown_error";

        /// <summary>
        /// Authentication Canceled.
        /// </summary>
        public const string AuthenticationCanceledError = "authentication_canceled";

        /// <summary>
        /// UPN Required.
        /// </summary>
        public const string UpnRequired = "upn_required";

        /// <summary>
        /// Missing Passive Auth Endpoint.
        /// </summary>
        public const string MissingPassiveAuthEndpoint = "missing_passive_auth_endpoint";

        /// <summary>
        /// Invalid Authority.
        /// </summary>
        public const string InvalidAuthority = "invalid_authority";

        /// <summary>
        /// Platform is Not Supported.
        /// </summary>
        public const string PlatformNotSupported = "platform_not_supported";

        /// <summary>
        /// Cannot Access User Information or User is Not Domain Joined.
        /// </summary>
        public const string CannotAccessUserInformationOrUserNotDomainJoined = "user_information_access_failed";

        /// <summary>
        /// RedirectUri validation failed.
        /// </summary>
        public const string DefaultRedirectUriIsInvalid = "redirect_uri_validation_failed";

        /// <summary>
        /// No Redirect URI.
        /// </summary>
        public const string NoRedirectUri = "no_redirect_uri";

        /// <summary>
        /// No Redirect URI.
        /// </summary>
        public const string B2CHostNotTrusted = "B2C_host_not_trusted";

#if iOS
        /// <summary>
        /// Cannot Access Publisher KeyChain.
        /// </summary>
        public const string CannotAccessPublisherKeyChain = "cannot_access_publisher_keychain";

        /// <summary>
        /// Missing Entitlements.
        /// </summary>
        public const string MissingEntitlements = "missing_entitlements";
#endif

#if ANDROID
        /// <summary>
        /// Failed To Create Shared Preference.
        /// </summary>
        public const string FailedToCreateSharedPreference = "shared_preference_creation_failed";

        /// <summary>
        /// Android Activity Not Found.
        /// </summary>
        public const string AndroidActivityNotFound = "android_activity_not_found";

        /// <summary>
        /// Unresolvable Intent.
        /// </summary>
        public const string UnresolvableIntentError = "unresolvable_intent";
#endif
    }
}
