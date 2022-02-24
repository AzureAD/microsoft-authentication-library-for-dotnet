// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Internal.Broker
{
    /// <summary>
    /// For Android there are from: https://github.com/AzureAD/microsoft-authentication-library-common-for-android/blob/dev/common/src/main/java/com/microsoft/identity/common/internal/broker/BrokerResult.java
    /// </summary>
    internal static class BrokerResponseConst
    {
        public const string ErrorMetadata = "error_metadata";
        public const string BrokerErrorDomain = "broker_error_domain";
        public const string BrokerErrorCode = "broker_error_code";
        public const string BrokerErrorDescription = "error_description";
        public const string BrokerSubError = "oauth_sub_error";
        public const string BrokerHttpHeaders = "http_response_headers";
        public const string BrokerHttpBody = "http_response_body";
        public const string BrokerHttpStatusCode = "http_response_code";

        public const string BrokerErrorMessage = "broker_error_message";

        public const string Authority = "authority";
        public const string AccessToken = "access_token";
        public const string ClientId = "client_id";
        public const string RefreshToken = "refresh_token";
        public const string IdToken = "id_token";
        public const string Bearer = "Bearer";
        public const string CorrelationId = "correlation_id";
        public const string Scope = "scope";
        public const string AndroidScopes = "scopes";
        public const string ExpiresOn = "expires_on";
        public const string ExtendedExpiresOn = "ext_expires_on";
        public const string ClientInfo = "client_info";
        public const string Account = "mAccount";
        public const string HomeAccountId = "home_account_id";
        public const string LocalAccountId = "local_account_id";
        public const string TenantId = "tenant_id";
        public const string UserName = "username";
        public const string iOSBrokerNonce = "broker_nonce"; // included in request and response with iOS Broker v3
        public const string iOSBrokerTenantId = "utid";
        public const string Environment = "environment";

        public const string iOSBrokerUserCancellationErrorCode = "-50005";

        // The requested resource is protected by an Intune Conditional Access policy.
        // The calling app should integrate the Intune SDK and call the remediateComplianceForIdentity:silent: API,
        // please see https://aka.ms/intuneMAMSDK for more information. Handling of this error is optional (handle it only
        // if you are going to access resources protected by an Intune Conditional Access policy).
        public const string iOSBrokerProtectionPoliciesRequiredErrorCode = "-50004";

        public const string TokenType = "token_type";

        //Error codes returned from Android broker
        public const string AndroidNoTokenFound = "no_tokens_found";
        public const string AndroidNoAccountFound = "no_account_found";
        public const string AndroidUnauthorizedClient = "unauthorized_client";
        public const string AndroidInvalidRefreshToken = "Broker refresh token is invalid";
        public const string AndroidProtectionPolicyRequired = "protection_policy_required";
    }
}
