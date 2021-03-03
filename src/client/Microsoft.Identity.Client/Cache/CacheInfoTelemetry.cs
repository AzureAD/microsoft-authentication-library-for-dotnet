// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.;

namespace Microsoft.Identity.Client.Cache
{
    // Enum to be used only for telemetry to log the reason for cache refresh and fetching access token from ESTS.
    internal enum CacheInfoTelemetry
    {
        None = 0, // When the cache is not hit to make the request (interactive call, username password call, device code flow, etc.)
        ForceRefresh = 1, // When the token request goes to ESTS because force_refresh was set to true
        NoCachedAT = 2, // When the token request goes to ESTS because no cached access token exists
        Expired = 3, // When the token request goes to ESTS because cached access token expired
        RefreshIn = 4, // When the token request goes to ESTS because refresh_in was used and the existing token needs to be refreshed
        //NonMsal = 5, // Reserved for non-MSAL customers (included here for consistency with the spec)
    }
}
