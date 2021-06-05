// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// MSAL-wide metrics.
    /// </summary>
    public class Metrics
    {
        private static long _totalAccessTokensFromIdP;
        private static long _totalAccessTokensFromCache;
        private static long _totalDurationInMs;

        private Metrics() { }

        /// <summary>
        /// Total tokens obtained by MSAL from the identity provider.
        /// </summary>
        public static long TotalAccessTokensFromIdP => _totalAccessTokensFromIdP;

        /// <summary>
        /// Total tokens obtained by MSAL from cache.
        /// </summary>
        public static long TotalAccessTokensFromCache => _totalAccessTokensFromCache;

        /// <summary>
        /// Total time, in milliseconds, spent in MSAL for all requests.  Aggregate of <see cref="AuthenticationResultMetadata.DurationInCacheInMs"/>.
        /// </summary>
        public static long TotalDurationInMs => _totalDurationInMs;

        internal static void IncrementTotalAccessTokensFromIdP()
        {
            Interlocked.Increment(ref _totalAccessTokensFromIdP);
        }

        internal static void IncrementTotalAccessTokensFromCache()
        {
            Interlocked.Increment(ref _totalAccessTokensFromCache);
        }

        internal static void IncrementTotalDurationInMs(long requestDurationInMs)
        {
            Interlocked.Add(ref _totalDurationInMs, requestDurationInMs);
        }
    }
}
