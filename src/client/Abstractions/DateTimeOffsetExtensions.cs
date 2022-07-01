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
        /// <summary>
        /// Adds <paramref name="timeSpan"/> to <paramref name="dateTime"/>.
        /// If sum of <paramref name="dateTime"/> and <paramref name="timeSpan"/>
        /// exeeds <see cref="DateTimeOffset.MaxValue"/>, the resulting <see cref="DateTimeOffset"/>,
        /// result will be set to to <see cref="DateTimeOffset.MaxValue"/>.
        /// </summary>
        public static DateTimeOffset AddOrCap(this DateTimeOffset dateTime, TimeSpan timeSpan)
        {
            if (dateTime == DateTimeOffset.MaxValue || timeSpan == TimeSpan.MaxValue)
                return DateTimeOffset.MaxValue;

            // checking only if timeSpan == TimeSpan.MaxValue is unsufficient
            // as jitter might be applied to the timeSpan.
            // based on: https://referencesource.microsoft.com/#mscorlib/system/timespan.cs,92
            long result = dateTime.UtcTicks + timeSpan.Ticks;
            if ((dateTime.UtcTicks >> 63 == timeSpan.Ticks >> 63) && (dateTime.UtcTicks >> 63 != result >> 63))
                return DateTimeOffset.MaxValue;
            else
                return dateTime.Add(timeSpan);
        }
    }
}
