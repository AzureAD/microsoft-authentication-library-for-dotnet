// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.;

namespace Microsoft.Identity.Client.Cache
{
    /// <summary>
    /// Enum describing the reason for cache refresh and fetching access token from ESTS.
    /// </summary>
    public enum CacheInfo
    {
        /// <summary>
        /// When the cache is not hit to make the request (interactive call, username password call, device code flow, etc.)
        /// </summary>
        None = 0,
        /// <summary>
        /// When the token request goes to ESTS because force_refresh was set to true
        /// </summary>
        ForceRefresh = 1,
        /// <summary>
        /// When the token request goes to ESTS because no cached access token exists
        /// </summary>
        NoCachedAT = 2,
        /// <summary>
        /// When the token request goes to ESTS because cached access token expired
        /// </summary>
        Expired = 3,
        /// <summary>
        /// When the token request goes to ESTS because refresh_in was used and the existing token needs to be refreshed
        /// </summary>
        RefreshIn = 4,
        //NonMsal = 5, // Reserved for non-MSAL customers (included here for consistency with the spec)
    }
}
