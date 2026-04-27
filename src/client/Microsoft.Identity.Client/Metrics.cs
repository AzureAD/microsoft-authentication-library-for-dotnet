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
        private static long s_totalAccessTokensFromIdP;
        private static long s_totalAccessTokensFromCache;
        private static long s_totalAccessTokensFromBroker;
        private static long s_totalDurationInMs;

        private Metrics() { }

        /// <summary>
        /// Total tokens obtained by MSAL from the identity provider.
        /// </summary>
        public static long TotalAccessTokensFromIdP
        {
            get => s_totalAccessTokensFromIdP;
            internal set => s_totalAccessTokensFromIdP = value;
        }

        /// <summary>
        /// Total tokens obtained by MSAL from cache.
        /// </summary>
        public static long TotalAccessTokensFromCache
        {
            get => s_totalAccessTokensFromCache;
            internal set => s_totalAccessTokensFromCache = value;
        }

        /// <summary>
        /// Total tokens obtained by MSAL from broker.
        /// </summary>
        public static long TotalAccessTokensFromBroker
        {
            get => s_totalAccessTokensFromBroker;
            internal set => s_totalAccessTokensFromBroker = value;
        }

        /// <summary>
        /// Total time, in milliseconds, spent in MSAL for all requests.  Aggregate of <see cref="AuthenticationResultMetadata.DurationTotalInMs"/>.
        /// </summary>
        public static long TotalDurationInMs
        {
            get => s_totalDurationInMs;
            internal set => s_totalDurationInMs = value;
        }

        internal static void IncrementTotalAccessTokensFromIdP()
        {
            Interlocked.Increment(ref s_totalAccessTokensFromIdP);
        }

        internal static void IncrementTotalAccessTokensFromCache()
        {
            Interlocked.Increment(ref s_totalAccessTokensFromCache);
        }

        internal static void IncrementTotalAccessTokensFromBroker()
        {
            Interlocked.Increment(ref s_totalAccessTokensFromBroker);
        }

        internal static void IncrementTotalDurationInMs(long requestDurationInMs)
        {
            Interlocked.Add(ref s_totalDurationInMs, requestDurationInMs);
        }
    }
}
