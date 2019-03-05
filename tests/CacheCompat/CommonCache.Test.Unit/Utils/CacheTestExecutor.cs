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

        private readonly bool _expectSecondTokenFromCache;
        private readonly bool _expectSecondTokenException;
        private readonly CacheStorageType _cacheStorageType;

        public CacheTestExecutor(
            CacheProgramType firstProgram,
            CacheProgramType secondProgram,
            CacheStorageType cacheStorageType,
            bool expectSecondTokenFromCache = true,
            bool expectSecondTokenException = false)
        {
            _firstProgram = firstProgram;
            _secondProgram = secondProgram;
            _cacheStorageType = cacheStorageType;

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
                new UserQuery
                {
                    UserType = UserType.Member,
                    IsFederatedUser = false
                }).User;

            Console.WriteLine($"Received LabUser: {labUser.Upn} from LabServiceApi.");

            var cacheProgramFirst = CacheProgramFactory.CreateCacheProgram(_firstProgram, _cacheStorageType);
            var cacheProgramSecond = CacheProgramFactory.CreateCacheProgram(_secondProgram, _cacheStorageType);

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
                Console.WriteLine($"MSAL V2 Cache Exists at: {CommonCacheTestUtils.MsalV2CacheFilePath}");
                Console.WriteLine("MSAL V2 Cache Size: " + Convert.ToInt32(new FileInfo(CommonCacheTestUtils.MsalV2CacheFilePath).Length));
            }
            else
            {
                Console.WriteLine($"MSAL V2 Cache DOES NOT EXIST at: {CommonCacheTestUtils.MsalV2CacheFilePath}");
            }

            if (File.Exists(CommonCacheTestUtils.MsalV3CacheFilePath))
            {
                Console.WriteLine($"MSAL V3 Cache Exists at: {CommonCacheTestUtils.MsalV3CacheFilePath}");
                Console.WriteLine("MSAL V3 Cache Size: " + Convert.ToInt32(new FileInfo(CommonCacheTestUtils.MsalV3CacheFilePath).Length));
            }
            else
            {
                Console.WriteLine($"MSAL V3 Cache DOES NOT EXIST at: {CommonCacheTestUtils.MsalV3CacheFilePath}");
            }

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
