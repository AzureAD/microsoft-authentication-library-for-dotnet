// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;

namespace Microsoft.Identity.Test.Common.Core.Helpers
{
    /// <summary>
    /// PerformanceValidator is a utility class for measuring the execution time of a code block and asserting that it does not exceed a specified threshold. It uses the IDisposable pattern to automatically measure the time taken by the code block enclosed within a using statement. If the elapsed time exceeds the maximum allowed time, it fails the test with an appropriate message. Otherwise, it logs the performance time for informational purposes. This is useful for validating that certain operations meet performance requirements in tests.
    /// </summary>
    public class PerformanceValidator : IDisposable
    {
        private readonly long _maxMilliseconds;
        private readonly string _message;
        private readonly Stopwatch _stopwatch;

        /// <summary>
        /// Initializes a new instance of the <see cref="PerformanceValidator"/> class.
        /// </summary>
        /// <param name="maxMilliseconds"></param>
        /// <param name="message"></param>
        public PerformanceValidator(long maxMilliseconds, string message)
        {
            _maxMilliseconds = maxMilliseconds;
            _message = message;
            _stopwatch = Stopwatch.StartNew();
        }

        /// <summary>
        /// Disposes the PerformanceValidator, stopping the stopwatch and asserting that the elapsed time does not exceed the specified maximum. If the elapsed time exceeds the threshold, it fails the test with a message indicating the failure. If the performance is within acceptable limits, it logs the elapsed time for informational purposes.
        /// </summary>
        public void Dispose()
        {
            _stopwatch.Stop();
            long elapsedMilliseconds = _stopwatch.ElapsedMilliseconds;

            if (elapsedMilliseconds > _maxMilliseconds)
            {
                ValidationHelpers.AssertFail(
                    $"Measured performance time EXCEEDED.  Max allowed: {_maxMilliseconds}ms.  Elapsed:  {elapsedMilliseconds}.  {_message}");
            }
            else
            {
                Trace.WriteLine(
                    $"Measured performance time OK.  Max allowed: {_maxMilliseconds}ms.  Elapsed:  {elapsedMilliseconds}.  {_message}");
            }
        }
    }
}
