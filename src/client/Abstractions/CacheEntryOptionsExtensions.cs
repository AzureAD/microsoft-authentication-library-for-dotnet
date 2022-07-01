// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.ServiceEssentials
{
    // CAN be made internal and moved to implementation

    /// <summary>
    /// <see cref="CacheEntryOptions"/> extension methods.
    /// </summary>
    public static class CacheEntryOptionsExtensions
    {
        private static readonly Random _random = new Random();

        /// <summary>
        /// Returns <see cref="CacheEntryOptions.TimeToRefresh"/> of <paramref name="cacheEntryOptions"/>
        /// randomized by <see cref="CacheEntryOptions.JitterInSeconds"/>
        /// </summary>
        /// <param name="cacheEntryOptions">
        /// <see cref="CacheEntryOptions"/> instance that contains <see cref="CacheEntryOptions.TimeToRefresh"/>
        /// and <see cref="CacheEntryOptions.JitterInSeconds"/>.
        /// </param>
        /// <returns>
        /// <see cref="CacheEntryOptions.TimeToRefresh"/> of <paramref name="cacheEntryOptions"/>
        /// randomized by <see cref="CacheEntryOptions.JitterInSeconds"/>.
        /// </returns>
        /// <remarks>
        /// Jitter should be applied  where possible, to avoid the thundering herd problem.
        /// </remarks>
        public static TimeSpan GetTimeToRefreshWithJitter(this CacheEntryOptions cacheEntryOptions)
        {
            _ = cacheEntryOptions ?? throw new ArgumentNullException(nameof(cacheEntryOptions));

            if (cacheEntryOptions.TimeToRefresh == TimeSpan.MaxValue)
                return cacheEntryOptions.TimeToRefresh;

            var randomSpan = GetRandomSpan(cacheEntryOptions.JitterInSeconds);
            return cacheEntryOptions.TimeToRefresh.AddOrCap(randomSpan);
        }

        /// <summary>
        /// Returns <see cref="CacheEntryOptions.TimeToRemove"/> of <paramref name="cacheEntryOptions"/>
        /// randomized by <see cref="CacheEntryOptions.JitterInSeconds"/>
        /// </summary>
        /// <param name="cacheEntryOptions">
        /// <see cref="CacheEntryOptions"/> instance that contains <see cref="CacheEntryOptions.TimeToRemove"/>
        /// and <see cref="CacheEntryOptions.JitterInSeconds"/>.
        /// </param>
        /// <returns>
        /// <see cref="CacheEntryOptions.TimeToRemove"/> of <paramref name="cacheEntryOptions"/>
        /// randomized by <see cref="CacheEntryOptions.JitterInSeconds"/>.
        /// </returns>
        /// <remarks>
        /// Jitter should be applied  where possible, to avoid the thundering herd problem.
        /// </remarks>
        public static TimeSpan GetTimeToRemoveWithJitter(this CacheEntryOptions cacheEntryOptions)
        {
            _ = cacheEntryOptions ?? throw new ArgumentNullException(nameof(cacheEntryOptions));

            if (cacheEntryOptions.TimeToRemove == TimeSpan.MaxValue)
                return cacheEntryOptions.TimeToRefresh;

            var randomSpan = GetRandomSpan(cacheEntryOptions.JitterInSeconds);
            return cacheEntryOptions.TimeToRemove.AddOrCap(randomSpan);
        }

        /// <summary>
        /// Returns <see cref="CacheEntryOptions.TimeToExpire"/> of <paramref name="cacheEntryOptions"/>
        /// randomized by <see cref="CacheEntryOptions.JitterInSeconds"/>
        /// </summary>
        /// <param name="cacheEntryOptions">
        /// <see cref="CacheEntryOptions"/> instance that contains <see cref="CacheEntryOptions.TimeToExpire"/>
        /// and <see cref="CacheEntryOptions.JitterInSeconds"/>.
        /// </param>
        /// <returns>
        /// <see cref="CacheEntryOptions.TimeToExpire"/> of <paramref name="cacheEntryOptions"/>
        /// randomized by <see cref="CacheEntryOptions.JitterInSeconds"/>.
        /// </returns>
        /// <remarks>
        /// Jitter should be applied  where possible, to avoid the thundering herd problem.
        /// </remarks>
        public static TimeSpan GetTimeToExpireWithJitter(this CacheEntryOptions cacheEntryOptions)
        {
            _ = cacheEntryOptions ?? throw new ArgumentNullException(nameof(cacheEntryOptions));

            if (cacheEntryOptions.TimeToExpire == TimeSpan.MaxValue)
                return cacheEntryOptions.TimeToExpire;

            var randomSpan = GetRandomSpan(cacheEntryOptions.JitterInSeconds);
            return cacheEntryOptions.TimeToExpire.AddOrCap(randomSpan);
        }

        private static TimeSpan GetRandomSpan(int jitterSpanInSeconds)
        {
            if (jitterSpanInSeconds == 0)
                return TimeSpan.Zero;
            else if (jitterSpanInSeconds < 0)
                return TimeSpan.FromSeconds((long)((_random.NextDouble()) * jitterSpanInSeconds));
            else
                return TimeSpan.FromSeconds((long)((_random.NextDouble() * 2.0 - 1.0) * jitterSpanInSeconds));
        }

        private static TimeSpan AddOrCap(this TimeSpan timeSpan1, TimeSpan timeSpan2)
        {
            if (timeSpan1 == TimeSpan.MaxValue || timeSpan2 == TimeSpan.MaxValue)
                return TimeSpan.MaxValue;

            // checking only if timeSpan == TimeSpan.MaxValue is unsufficient
            // as jitter might be applied to the timeSpan.
            // based on: https://referencesource.microsoft.com/#mscorlib/system/timespan.cs,92
            long result = timeSpan1.Ticks + timeSpan2.Ticks;
            if ((timeSpan1.Ticks >> 63 == timeSpan2.Ticks >> 63) && (timeSpan1.Ticks >> 63 != result >> 63))
                return TimeSpan.MaxValue;
            else
                return timeSpan1.Add(timeSpan2);
        }
    }
}
