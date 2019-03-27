// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Mats.Internal.Constants
{
    internal static class ApiTelemetryFeatureKey
    {
        // todo(mats): define these values...
        public const string Unknown = "api_feature_unknown";
        public const string WithAccount = "api_with_account";
        public const string WithForceRefresh = "api_with_force_refresh";
        public const string WithLoginHint = "api_with_login_hint";

        public const string WithRedirectUri = "api_with_redirect_uri";
        public const string WithExtraScopesToConsent = "api_with_extra_scopes_to_consent";
        public const string WithUserAssertion = "api_with_user_assertion";
        public const string WithSendX5C = "api_with_send_x5c";
        public const string WithCurrentSynchronizationContext = "api_with_current_sync_context";
        public const string WithEmbeddedWebView = "api_with_embedded_web_view";
        public const string WithParent = "api_with_parent";
        public const string WithPrompt = "api_with_prompt";
        public const string WithUsername = "api_with_username";
        public const string WithClaims = "api_with_claims";
        public const string WithExtraQueryParameters = "api_with_extra_query_parameters";
        public const string WithAuthority = "api_with_authority";
        public const string WithValidateAuthority = "api_with_validate_authority";
        public const string WithAdfsAuthority = "api_with_adfs_authority";
        public const string WithB2CAuthority = "api_with_b2c_authority";
        public const string WithCustomWebUi = "api_with_custom_webui";
    }
}
