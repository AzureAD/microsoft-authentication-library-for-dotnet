// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Mats.Internal.Events
{
    internal enum ApiTelemetryId
    {
        Unknown,
        AcquireTokenSilent,
        AcquireTokenWithDeviceCode,
        GetAuthorizationRequestUrl,
        AcquireTokenOnBehalfOf,
        AcquireTokenInteractive,
        AcquireTokenForClient,
        AcquireTokenByUsernamePassword,
        AcquireTokenByRefreshToken,
        AcquireTokenByIntegratedWindowsAuth,
        AcquireTokenByAuthorizationCode
    }
}
