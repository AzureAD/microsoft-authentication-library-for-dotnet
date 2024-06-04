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
    internal struct MeasureDurationResult<TResult>
    {
        private const int TicksPerMicrosecond = 10;
        private static readonly double s_tickFrequency = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;

        public MeasureDurationResult(TResult result, long ticks)
        {
            Result = result;
            Milliseconds = (long)(ticks / s_tickFrequency / TimeSpan.TicksPerMillisecond);
            Microseconds = (long)(ticks * s_tickFrequency / TicksPerMicrosecond % 1000);
            Ticks = ticks;
        }

        public TResult Result { get; }

        /// <summary>
        /// Measured milliseconds
        /// </summary>
        public long Milliseconds { get; }

        /// <summary>
        /// Measured microseconds
        /// </summary>
        public long Microseconds { get; }

        /// <summary>
        /// Measured ticks
        /// </summary>
        public long Ticks { get; }
    }

    /// <summary>
    /// Structure that holds a duration of the <see cref="Task"/> in milliseconds.
    /// </summary>
    internal struct MeasureDurationResult
    {
        private const int TicksPerMicrosecond = 10;
        private static readonly double s_tickFrequency = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;

        public MeasureDurationResult(long ticks)
        {
            Milliseconds = (long)(ticks * s_tickFrequency / TimeSpan.TicksPerMillisecond % 1000);
            Microseconds = (long)(ticks * s_tickFrequency / TicksPerMicrosecond % 1000);
            Ticks = ticks;
        }

        /// <summary>
        /// Measured milliseconds
        /// </summary>
        public long Milliseconds { get; }

        /// <summary>
        /// Measured microseconds
        /// </summary>
        public long Microseconds { get; }

        /// <summary>
        /// Measured ticks
        /// </summary>
        public long Ticks { get; }
    }
}
