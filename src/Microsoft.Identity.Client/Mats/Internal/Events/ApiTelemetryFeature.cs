// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Mats.Internal.Events
{
    internal enum ApiTelemetryFeature
    {
        WithAccount,
        WithLoginHint,
        WithForceRefresh,
        WithRedirectUri,
        WithExtraScopesToConsent,
        WithUserAssertion,
        WithSendX5C,
        WithCurrentSynchronizationContext,
        WithEmbeddedWebView,
        WithParent,
        WithPrompt,
        WithUsername,
        WithClaims,
        WithExtraQueryParameters,
        WithAuthority,
        WithValidateAuthority,
        WithAdfsAuthority,
        WithB2CAuthority,
        WithCustomWebUi,
        WithTrustFrameworkPolicy
    }
}
