// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonCache.Test.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommonCache.Test.Unit.Utils
{
    public class CacheTestExecutor
    {
        private readonly IEnumerable<LabUserData> _labUsers;
        private readonly CacheProgramType _firstProgram;
        private readonly CacheProgramType _secondProgram;

        private readonly CacheStorageType _cacheStorageType;

        public CacheTestExecutor(
            IEnumerable<LabUserData> labUsers,
            CacheProgramType firstProgram,
            CacheProgramType secondProgram,
            CacheStorageType cacheStorageType)
        {
            _labUsers = labUsers;
            _firstProgram = firstProgram;
            _secondProgram = secondProgram;
            _cacheStorageType = cacheStorageType;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            LogExecution();

            CommonCacheTestUtils.DeleteAllTestCaches();
            CommonCacheTestUtils.EnsureCacheFileDirectoryExists();

            var cacheProgramFirst = CacheProgramFactory.CreateCacheProgram(_firstProgram, _cacheStorageType);
            var cacheProgramSecond = CacheProgramFactory.CreateCacheProgram(_secondProgram, _cacheStorageType);

            var firstResults = await cacheProgramFirst.ExecuteAsync(_labUsers, cancellationToken).ConfigureAwait(false);
            var secondResults = await cacheProgramSecond.ExecuteAsync(_labUsers, cancellationToken).ConfigureAwait(false);

            Console.WriteLine();
            Console.WriteLine("------------------------------------");
            Console.WriteLine($"FirstResults: {_firstProgram}");
            Console.WriteLine("stdout:");
            Console.WriteLine(firstResults.StdOut);
            Console.WriteLine();
            Console.WriteLine("stderr:");
            Console.WriteLine(firstResults.StdErr);
            Console.WriteLine("------------------------------------");
            Console.WriteLine($"SecondResults: {_secondProgram}");
            Console.WriteLine("stdout:");
            Console.WriteLine(secondResults.StdOut);
            Console.WriteLine("stderr:");
            Console.WriteLine(secondResults.StdErr);
            Console.WriteLine("------------------------------------");
            Console.WriteLine();

            Assert.IsFalse(firstResults.ProcessExecutionFailed, $"{cacheProgramFirst.ExecutablePath} should not fail");
            Assert.IsFalse(secondResults.ProcessExecutionFailed, $"{cacheProgramSecond.ExecutablePath} should not fail");

            foreach (var upnResult in firstResults.ExecutionResults.Results)
            {
                Assert.IsFalse(upnResult.IsAuthResultFromCache, $"{upnResult.LabUserUpn} --> First result should not be from the cache");
                Assert.AreEqual(upnResult?.LabUserUpn?.ToLowerInvariant(), upnResult?.AuthResultUpn?.ToLowerInvariant());
            }

            foreach (var upnResult in secondResults.ExecutionResults.Results)
            {
                Assert.IsTrue(upnResult.IsAuthResultFromCache, $"{upnResult.LabUserUpn} --> Second result should be from the cache");
                Assert.AreEqual(upnResult?.LabUserUpn?.ToLowerInvariant(), upnResult?.AuthResultUpn?.ToLowerInvariant());
            }

            Assert.IsFalse(
                secondResults.ExecutionResults.IsError,
                "Second result should NOT have thrown an exception");

            PrintCacheInfo();
        }

        private static void PrintCacheInfo()
        {
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
        }

        private void LogExecution()
        {
            Console.WriteLine($"Running {_firstProgram} -> {_secondProgram}...");
        }
    }
}
