// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Utils
{
    /// <summary>
    /// Structure that holds a <see cref="Task"/> result and duration of the <see cref="Task"/> in milliseconds
    /// </summary>
    internal struct MeasureDurationResult<TResult>(TResult result, long ticks)
    {
        private const int TicksPerMicrosecond = 10;
        private static readonly double s_tickFrequency = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;

        public TResult Result { get; } = result;

        /// <summary>
        /// Measured milliseconds
        /// </summary>
        public long Milliseconds { get; } = (long)(ticks * s_tickFrequency / TimeSpan.TicksPerMillisecond);

        /// <summary>
        /// Measured microseconds
        /// </summary>
        public long Microseconds { get; } = (long)(ticks * s_tickFrequency / TicksPerMicrosecond);

        /// <summary>
        /// Measured ticks
        /// </summary>
        public long Ticks { get; } = ticks;
    }

    /// <summary>
    /// Structure that holds a duration of the <see cref="Task"/> in milliseconds.
    /// </summary>
    internal struct MeasureDurationResult(long ticks)
    {
        private const int TicksPerMicrosecond = 10;
        private static readonly double s_tickFrequency = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;

        /// <summary>
        /// Measured milliseconds
        /// </summary>
        public long Milliseconds { get; } = (long)(ticks * s_tickFrequency / TimeSpan.TicksPerMillisecond);

        /// <summary>
        /// Measured microseconds
        /// </summary>
        public long Microseconds { get; } = (long)(ticks * s_tickFrequency / TicksPerMicrosecond);

        /// <summary>
        /// Measured ticks
        /// </summary>
        public long Ticks { get; } = ticks;
    }
}
