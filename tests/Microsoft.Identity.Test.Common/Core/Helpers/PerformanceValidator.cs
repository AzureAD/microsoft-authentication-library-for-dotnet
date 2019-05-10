// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Common.Core.Helpers
{
    public class PerformanceValidator : IDisposable
    {
        private readonly long _maxMilliseconds;
        private readonly string _message;
        private readonly Stopwatch _stopwatch;

        public PerformanceValidator(long maxMilliseconds, string message)
        {
            _maxMilliseconds = maxMilliseconds;
            _message = message;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            long elapsedMilliseconds = _stopwatch.ElapsedMilliseconds;

            if (elapsedMilliseconds > _maxMilliseconds)
            {
                Assert.Fail(
                    $"Measured performance time EXCEEDED.  Max allowed: {_maxMilliseconds}ms.  Elapsed:  {elapsedMilliseconds}.  {_message}");
            }
            else
            {
                Debug.WriteLine(
                    $"Measured performance time OK.  Max allowed: {_maxMilliseconds}ms.  Elapsed:  {elapsedMilliseconds}.  {_message}");
            }
        }
    }
}
