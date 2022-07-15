// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.ServiceEssentials
{
    // CAN be made internal and moved to implementation

    /// <summary>
    /// <see cref="DateTimeOffset"/> extension methods.
    /// </summary>
    public static class DateTimeOffsetExtensions
    {
        private static readonly Random _random = new Random();

        /// <summary>
        /// </summary>
        public static DateTimeOffset AddOrCap(this DateTimeOffset dateTime, int seconds)
        {
            var timeSpan = GetRandomSpan(seconds);

            if (dateTime == DateTimeOffset.MaxValue)
                return DateTimeOffset.MaxValue;

            // safeguard
            // checking only if timeSpan == TimeSpan.MaxValue is unsufficient
            // as jitter might be applied to the timeSpan.
            // based on: https://referencesource.microsoft.com/#mscorlib/system/timespan.cs,92
            long result = dateTime.UtcTicks + timeSpan.Ticks;
            if ((dateTime.UtcTicks >> 63 == timeSpan.Ticks >> 63) && (dateTime.UtcTicks >> 63 != result >> 63))
                return DateTimeOffset.MaxValue;
            else
                return dateTime.Add(timeSpan);
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
    }
}
