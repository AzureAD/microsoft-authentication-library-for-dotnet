// ------------------------------------------------------------------------------
// 
// Copyright (c) Microsoft Corporation.
// All rights reserved.
// 
// This code is licensed under the MIT License.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// ------------------------------------------------------------------------------

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