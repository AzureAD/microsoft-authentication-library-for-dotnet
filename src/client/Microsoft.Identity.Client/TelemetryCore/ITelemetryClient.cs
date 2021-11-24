// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.TelemetryCore
{
    internal enum AuthOutcome
    {
        Succeeded,
        Cancelled,
        Failed,
        Incomplete
    }

    internal enum ErrorSource
    {
        None,
        Service,
        AuthSdk,
        Client
    }

    internal enum InteractiveAuthContainerType
    {
        Embedded,
        CompanyPortal,
        Wam,
        Authenticator,
        SystemWebView,
        Browser
    }

    internal enum CustomIdentityService
    {
        EmailHrd,
        Basic,
        Fba,
        Kerberos,
        OnPremUnspecified
    }
}
