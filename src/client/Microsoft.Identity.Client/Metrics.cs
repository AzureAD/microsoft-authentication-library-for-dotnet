// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client
{
    internal static class Metrics
    {
        /// <summary>
        /// Total tokens obtained by MSAL from the identity provider.
        /// </summary>
        public static long TotalAccessTokensFromIdP { get; set; }

        /// <summary>
        /// Total tokens obtained by MSAL from cache.
        /// </summary>
        public static long TotalAccessTokensFromCache { get; set; }

        /// <summary>
        /// Total time, in milliseconds, spent in MSAL for all requests.
        /// </summary>
        public static long TotalDurationInMs { get; set; }
    }
}
