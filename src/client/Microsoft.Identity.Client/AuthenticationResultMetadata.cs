// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Cache;

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
        /// Time, in milliseconds, remaining before the token will be proactively refreshed.
        /// This value may be null.
        /// </summary>
        public DateTimeOffset? RemainingTimeBeforeRefresh { get; set; } = null;

        /// <summary>
        /// Specifies the reason for fetching the access token from the identity provider.
        /// </summary>
        public CacheMissReason CacheInfo { get; set; }
    }
}
