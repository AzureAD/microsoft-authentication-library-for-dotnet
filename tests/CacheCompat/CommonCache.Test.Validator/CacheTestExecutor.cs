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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommonCache.Test.Common;

namespace CommonCache.Test.Validator
{
    public class CacheTestExecutor
    {
        private readonly CacheProgramType _firstProgram;
        private readonly CacheProgramType _secondProgram;

        private readonly int _expectedAdalCacheSizeBytes;
        private readonly int _expectedMsalCacheSizeBytes;
        private readonly bool _expectSecondTokenFromCache;
        private readonly bool _expectSecondTokenException;

        public CacheTestExecutor(
            CacheProgramType firstProgram,
            CacheProgramType secondProgram,
            int expectedAdalCacheSizeBytes = 0,
            int expectedMsalCacheSizeBytes = 0,
            bool expectSecondTokenFromCache = false,
            bool expectSecondTokenException = false)
        {
            _firstProgram = firstProgram;
            _secondProgram = secondProgram;

            _expectedAdalCacheSizeBytes = expectedAdalCacheSizeBytes;
            _expectedMsalCacheSizeBytes = expectedMsalCacheSizeBytes;
            _expectSecondTokenFromCache = expectSecondTokenFromCache;
            _expectSecondTokenException = expectSecondTokenException;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            LogExecution();

            CommonCacheTestUtils.DeleteAllTestCaches();
            CommonCacheTestUtils.EnsureCacheFileDirectoryExists();

            var cacheProgramFirst = CacheProgramFactory.CreateCacheProgram(_firstProgram);
            var cacheProgramSecond = CacheProgramFactory.CreateCacheProgram(_secondProgram);

            var firstResults = await cacheProgramFirst.ExecuteAsync(cancellationToken);
            var secondResults = await cacheProgramSecond.ExecuteAsync(cancellationToken);

            ValidateBooleanEquals(false, firstResults.ProcessExecutionFailed, $"{cacheProgramFirst.ExecutablePath} should not fail");
            ValidateBooleanEquals(false, secondResults.ProcessExecutionFailed, $"{cacheProgramSecond.ExecutablePath} should not fail");

            ValidateBooleanEquals(false, firstResults.ExecutionResults.ReceivedTokenFromCache, "First result should not be from the cache");

            if (_expectedAdalCacheSizeBytes > 0)
            {
                ValidateIntegerEquals(_expectedAdalCacheSizeBytes, Convert.ToInt32(new FileInfo(CommonCacheTestUtils.AdalV3CacheFilePath).Length), "Expected Adal Cache Size");
            }
            if (_expectedMsalCacheSizeBytes > 0)
            {
                ValidateIntegerEquals(_expectedMsalCacheSizeBytes, Convert.ToInt32(new FileInfo(CommonCacheTestUtils.MsalV2CacheFilePath).Length), "Expected Msal Cache Size");
            }

            if (_expectSecondTokenFromCache)
            {
                ValidateBooleanEquals(
                    true,
                    secondResults.ExecutionResults.ReceivedTokenFromCache,
                    "Second result should be from the cache");
            }
            else
            {
                ValidateBooleanEquals(
                    false,
                    secondResults.ExecutionResults.ReceivedTokenFromCache,
                    "Second result should NOT be from the cache");
            }

            if (_expectSecondTokenException)
            {
                ValidateBooleanEquals(
                    true,
                    secondResults.ExecutionResults.IsError,
                    "Second result should have thrown an exception");
            }
            else
            {
                ValidateBooleanEquals(
                    false,
                    secondResults.ExecutionResults.IsError,
                    "Second result should NOT have thrown an exception");
            }
        }

        private void LogExecution()
        {
            Console.WriteLine($"Running {_firstProgram} -> {_secondProgram}...");
        }

        private void ValidateBooleanEquals(bool expected, bool actual, string message)
        {
            string result = expected == actual ? "OK" : "FAIL";
            Console.WriteLine($"{message}: Expected({expected}) Actual({actual}) --> {result}");
        }

        private void ValidateIntegerEquals(int expected, int actual, string message)
        {
            string result = expected == actual ? "OK" : "FAIL";
            Console.WriteLine($"{message}: Expected({expected}) Actual({actual}) --> {result}");
        }
    }
}