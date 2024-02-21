// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Utils
{
    /// <summary>
    /// Singleton timer used to measure the duration tasks.
    /// </summary>
    internal static class StopwatchService

    {
        /// <summary>
        /// Singleton stopwatch.
        /// </summary>
        internal static readonly Stopwatch Watch = Stopwatch.StartNew();

        /// <summary>
        /// Current elapsed milliseconds of the stopwatch
        /// </summary>
        internal static long CurrentElapsedMilliseconds {
            get 
            {
                return Watch.ElapsedMilliseconds;
            }
        }

        /// <summary>
        /// Measures the duration of a code block
        /// </summary>
        /// <param name="codeBlock"></param>
        /// <returns></returns>
        internal static MeasureDurationResult MeasureCodeBlock(Action codeBlock)
        {
            _ = codeBlock ?? throw new ArgumentNullException(nameof(codeBlock));

            var startTicks = Watch.ElapsedTicks;
            codeBlock.Invoke();

            return new MeasureDurationResult(Watch.ElapsedTicks - startTicks);
        }

        /// <summary>
        /// Measures the duration of an asyncronous code block
        /// </summary>
        /// <param name="codeBlock"></param>
        /// <returns></returns>
        internal static async Task<MeasureDurationResult> MeasureCodeBlockAsync(Func<Task> codeBlock)
        {
            _ = codeBlock ?? throw new ArgumentNullException(nameof(codeBlock));
            var startTicks = Watch.ElapsedTicks;
            await codeBlock.Invoke().ConfigureAwait(false);

            return new MeasureDurationResult(Watch.ElapsedTicks - startTicks);
        }

        /// <summary>
        /// Measures the duration of an asyncronous code block
        /// </summary>
        /// <param name="codeBlock"></param>
        /// <returns></returns>
        internal static async Task<MeasureDurationResult<TResult>> MeasureCodeBlockAsync<TResult>(Func<Task<TResult>> codeBlock)
        {
            _ = codeBlock ?? throw new ArgumentNullException(nameof(codeBlock));
            var startTicks = Watch.ElapsedTicks;
            var result = await codeBlock.Invoke().ConfigureAwait(false);

            return new MeasureDurationResult<TResult>(result, Watch.ElapsedTicks - startTicks);
        }

        /// <summary>
        /// Measures duration of <paramref name="task"/> in ticks and milliseconds.
        /// </summary>
        internal static async Task<MeasureDurationResult> MeasureAsync(this Task task)
        {
            _ = task ?? throw new ArgumentNullException(nameof(task));

            var startTicks = Watch.ElapsedTicks;
            await task.ConfigureAwait(false);

            return new MeasureDurationResult(Watch.ElapsedTicks - startTicks);
            ;
        }

        /// <summary>
        /// Measures duration of <paramref name="task"/> in ticks and milliseconds.
        /// </summary>
        internal static async Task<MeasureDurationResult<TResult>> MeasureAsync<TResult>(this Task<TResult> task)
        {
            _ = task ?? throw new ArgumentNullException(nameof(task));

            var startTicks = Watch.ElapsedTicks;
            var taskResult = await task.ConfigureAwait(true);

            return new MeasureDurationResult<TResult>(taskResult, Watch.ElapsedTicks - startTicks);
        }
    }
}
