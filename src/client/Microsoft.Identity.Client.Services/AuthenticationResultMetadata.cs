// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Contains metadata of the authentication result. <see cref="Metrics"/> for additional MSAL-wide metrics.
    /// </summary>
    public class AuthenticationResultMetadata
    {

        /// <summary>
        /// Constructor for the class AuthenticationResultMetadata
        /// <param name="tokenSource">The token source.</param>
        /// </summary>
        public AuthenticationResultMetadata(TokenSource tokenSource)
        {
            TokenSource = tokenSource;
        }

        /// <summary>
        /// The source of the token in the result.
        /// </summary>
        public TokenSource TokenSource { get; }

        /// <summary>
        /// The token endpoint used to contact the Identity Provider (e.g. Azure Active Directory). 
        /// Can be null, for example when the token comes from the cache.
        /// </summary>
        /// <remarks>
        /// This may be different from the endpoint you'd infer from the authority configured in the application object:
        /// - if regional auth is used.
        /// - if AAD instructs MSAL to use a different environment. 
        /// - if the authority or tenant is overridden at the request level.
        /// - during a refresh_token operation, when MSAL must resolve "common" and "organizations" to a tenant ID.
        /// </remarks>
        public string TokenEndpoint { get; set; }

        /// <summary>
        /// Time, in milliseconds, spent to service this request. Includes time spent making HTTP requests <see cref="DurationInHttpInMs"/>, time spent
        /// in token cache callbacks <see cref="DurationInCacheInMs"/>, time spent in MSAL and context switching.
        /// </summary>
        public long DurationTotalInMs { get; set; }

        /// <summary>
        /// Time, in milliseconds, MSAL spent during this request reading and writing to the token cache, i.e. in the OnBeforeAccess, OnAfterAccess, etc. callbacks. 
        /// Does not include internal MSAL logic for searching through the cache once loaded.
        /// </summary>
        public long DurationInCacheInMs { get; set; }

        /// <summary>
        /// Time, in milliseconds, MSAL spent for HTTP communication during this request.
        /// </summary>
        public long DurationInHttpInMs { get; set; }

        /// <summary>
        /// Specifies the time when the cached token should be proactively refreshed.
        /// This value may be null if proactive refresh is not enabled.
        /// </summary>
        public DateTimeOffset? RefreshOn { get; set; } = null;

        /// <summary>
        /// Specifies the reason for fetching the access token from the identity provider.
        /// </summary>
        public CacheRefreshReason CacheRefreshReason { get; set; }

        /// <summary>
        /// Contains the Outcome of the region discovery if Region was used.
        /// </summary>
        public RegionDetails RegionDetails { get; set; }
    }
}
