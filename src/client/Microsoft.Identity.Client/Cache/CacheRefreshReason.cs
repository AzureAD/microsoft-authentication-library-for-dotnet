// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Specifies the reason for fetching the access token from the identity provider when using AcquireTokenSilent, AcquireTokenForClient or AcquireTokenOnBehalfOf.
    /// </summary>
    public enum CacheRefreshReason
    {
        /// <summary>
        /// When a token is found in the cache or the cache is not supposed to be hit when making the request (interactive call, username password call, device code flow, etc.)
        /// </summary>
        NotApplicable = 0,
        /// <summary>
        /// When the token request goes to the identity provider because force_refresh was set to true. Also occurs if WithClaims() is used.
        /// </summary>
        [Obsolete("Use ForceRefresh or WithClaims instead.")]
        ForceRefreshOrClaims = 1,
        /// <summary>
        /// When the token request goes to the identity provider because force_refresh was set to true.
        /// </summary>
        ForceRefresh = 2,
        /// <summary>
        /// When the token request goes to the identity provider because no cached access token exists
        /// </summary>
        NoCachedAccessToken = 3,
        /// <summary>
        /// When the token request goes to the identity provider because cached access token expired
        /// </summary>
        Expired = 4,
        /// <summary>
        /// When the token request goes to the identity provider because refresh_in was used and the existing token needs to be refreshed
        /// </summary>
        ProactivelyRefreshed = 5,
        /// <summary>
        /// When the token request goes to the identity provider because WithClaims() was used.
        /// </summary>
        WithClaims = 6,
        /// <summary>
        /// Indicates that the resource (e.g., Microsoft Graph) has rejected the provided token. 
        /// </summary>
        TokenRejectedByResource = 7
    }
}
