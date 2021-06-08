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
        public static long TotalAccessTokensFromIdP
        {
            get => _totalAccessTokensFromIdP;
            internal set => _totalAccessTokensFromIdP = value;
        }

        /// <summary>
        /// Total tokens obtained by MSAL from cache.
        /// </summary>
        public static long TotalAccessTokensFromCache
        {
            get => _totalAccessTokensFromCache;
            internal set => _totalAccessTokensFromCache = value;
        }

        /// <summary>
        /// Total time, in milliseconds, spent in MSAL for all requests.  Aggregate of <see cref="AuthenticationResultMetadata.DurationInCacheInMs"/>.
        /// </summary>
        public static long TotalDurationInMs
        {
            get => _totalDurationInMs;
            internal set => _totalDurationInMs = value;
        }

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
