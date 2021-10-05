// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Specifies the reason for fetching the access token from the identity provider.
    /// </summary>
    public enum CacheMissReason
    {
        /// <summary>
        /// When the cache is not supposed to be hit to make the request (interactive call, username password call, device code flow, etc.)
        /// </summary>
        NotApplicable = 0,
        /// <summary>
        /// When the token request goes to the identity provider because force_refresh was set to true
        /// </summary>
        ForceRefresh = 1,
        /// <summary>
        /// When the token request goes to the identity provider because no cached access token exists
        /// </summary>
        NoCachedAccessToken = 2,
        /// <summary>
        /// When the token request goes to the identity provider because cached access token expired
        /// </summary>
        Expired = 3,
        /// <summary>
        /// When the token request goes to the identity provider because refresh_in was used and the existing token needs to be refreshed
        /// </summary>
        ProactivelyRefreshed = 4
    }
}
