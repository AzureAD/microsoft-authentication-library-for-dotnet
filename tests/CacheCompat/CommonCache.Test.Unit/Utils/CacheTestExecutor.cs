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
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommonCache.Test.Unit.Utils
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

            var api = new LabServiceApi(new KeyVaultSecretsProvider());
            var labUser = api.GetLabResponse(
                new UserQueryParameters
                {
                    UserType = UserType.Member,
                    IsFederatedUser = false
                }).User;

            Console.WriteLine($"Received LabUser: {labUser.Upn} from LabServiceApi.");

            var cacheProgramFirst = CacheProgramFactory.CreateCacheProgram(_firstProgram);
            var cacheProgramSecond = CacheProgramFactory.CreateCacheProgram(_secondProgram);

            var firstResults = await cacheProgramFirst.ExecuteAsync(labUser.Upn, labUser.Password, cancellationToken).ConfigureAwait(false);
            var secondResults = await cacheProgramSecond.ExecuteAsync(labUser.Upn, labUser.Password, cancellationToken).ConfigureAwait(false);

            Console.WriteLine($"FirstResults: {_firstProgram}");
            Console.WriteLine($"stdout: {firstResults.StdOut}");
            Console.WriteLine($"stderr: {firstResults.StdErr}");

            Console.WriteLine($"SecondResults: {_secondProgram}");
            Console.WriteLine($"stdout: {secondResults.StdOut}");
            Console.WriteLine($"stderr: {secondResults.StdErr}");

            Assert.IsFalse(firstResults.ProcessExecutionFailed, $"{cacheProgramFirst.ExecutablePath} should not fail");
            Assert.IsFalse(secondResults.ProcessExecutionFailed, $"{cacheProgramSecond.ExecutablePath} should not fail");

            Assert.IsFalse(firstResults.ExecutionResults.ReceivedTokenFromCache, "First result should not be from the cache");

            if (File.Exists(CommonCacheTestUtils.AdalV3CacheFilePath))
            {
                Console.WriteLine($"Adal Cache Exists at: {CommonCacheTestUtils.AdalV3CacheFilePath}");
                Console.WriteLine("Adal Cache Size: " + Convert.ToInt32(new FileInfo(CommonCacheTestUtils.AdalV3CacheFilePath).Length));
            }
            else
            {
                Console.WriteLine($"Adal Cache DOES NOT EXIST at: {CommonCacheTestUtils.AdalV3CacheFilePath}");
            }

            if (File.Exists(CommonCacheTestUtils.MsalV2CacheFilePath))
            {
                Console.WriteLine($"MSAL Cache Exists at: {CommonCacheTestUtils.MsalV2CacheFilePath}");
                Console.WriteLine("MSAL Cache Size: " + Convert.ToInt32(new FileInfo(CommonCacheTestUtils.MsalV2CacheFilePath).Length));
            }
            else
            {
                Console.WriteLine($"MSAL Cache DOES NOT EXIST at: {CommonCacheTestUtils.MsalV2CacheFilePath}");
            }

            // TODO: cache size seems to be variant/inconsistent.  Need to validate if it should be the same every time.
            //if (_expectedAdalCacheSizeBytes > 0)
            //{
            //    Assert.AreEqual(_expectedAdalCacheSizeBytes, Convert.ToInt32(new FileInfo(CommonCacheTestUtils.AdalV3CacheFilePath).Length), "Expected Adal Cache Size");
            //}
            //if (_expectedMsalCacheSizeBytes > 0)
            //{
            //    Assert.AreEqual(_expectedMsalCacheSizeBytes, Convert.ToInt32(new FileInfo(CommonCacheTestUtils.MsalV2CacheFilePath).Length), "Expected Msal Cache Size");
            //}

            if (_expectSecondTokenFromCache)
            {
                Assert.IsTrue(
                    secondResults.ExecutionResults.ReceivedTokenFromCache,
                    "Second result should be from the cache");
            }
            else
            {
                Assert.IsFalse(
                    secondResults.ExecutionResults.ReceivedTokenFromCache,
                    "Second result should NOT be from the cache");
            }

            if (_expectSecondTokenException)
            {
                Assert.IsTrue(
                    secondResults.ExecutionResults.IsError,
                    "Second result should have thrown an exception");
            }
            else
            {
                Assert.IsFalse(
                    secondResults.ExecutionResults.IsError,
                    "Second result should NOT have thrown an exception");
            }
        }

        private void LogExecution()
        {
            Console.WriteLine($"Running {_firstProgram} -> {_secondProgram}...");
        }
    }
}