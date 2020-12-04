// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using CommonCache.Test.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommonCache.Test.Unit.Utils
{
    public class CacheTestExecutor
    {
        private readonly IEnumerable<LabUserData> _labUsers;
        private readonly CacheStorageType _cacheStorageType;

        public CacheTestExecutor(
            IEnumerable<LabUserData> labUsers,
            CacheStorageType cacheStorageType)
        {
            _labUsers = labUsers;
            _cacheStorageType = cacheStorageType;
        }

        public async Task ExecuteAsync(
            CacheProgramType firstProgram,
            CacheProgramType secondProgram,
            CancellationToken cancellationToken)
        {
            Trace.WriteLine($"Running {firstProgram} -> {secondProgram}...");

            CommonCacheTestUtils.DeleteAllTestCaches();
            CommonCacheTestUtils.EnsureCacheFileDirectoryExists();

            await ExecuteCacheProgramAsync(firstProgram, true, cancellationToken).ConfigureAwait(false);
            await ExecuteCacheProgramAsync(secondProgram, false, cancellationToken).ConfigureAwait(false);

            PrintCacheInfo();
        }

        private async Task ExecuteCacheProgramAsync(CacheProgramType cacheProgramType, bool isFirst, CancellationToken cancellationToken)
        {
            var cacheProgramFirst = CacheProgramFactory.CreateCacheProgram(cacheProgramType, _cacheStorageType);

            var results = await cacheProgramFirst.ExecuteAsync(_labUsers, cancellationToken).ConfigureAwait(false);
           
            Trace.WriteLine("------------------------------------");
            if (isFirst)
            {
                Trace.WriteLine($"First Results: {cacheProgramType}");
            }
            else
            {
                Trace.WriteLine($"Second Results: {cacheProgramType}");
            }
            Trace.WriteLine("stdout:");
            Trace.WriteLine(results.StdOut);
            Trace.WriteLine("-------");
            Trace.WriteLine("stderr:");
            Trace.WriteLine(results.StdErr);
            Trace.WriteLine("------------------------------------");
            Assert.IsFalse(results.ExecutionResults.IsError, $"{cacheProgramType} should not fail: {results.ExecutionResults.ErrorMessage}");
            Assert.IsFalse(results.ProcessExecutionFailed, $"{cacheProgramFirst.ExecutablePath} should not fail");

            foreach (var upnResult in results.ExecutionResults.Results)
            {
                if (isFirst)
                {
                    Assert.IsFalse(upnResult.IsAuthResultFromCache, $"{upnResult.LabUserUpn} --> First result should not be from the cache");
                }
                else
                {
                    Assert.IsTrue(upnResult.IsAuthResultFromCache, $"{upnResult.LabUserUpn} --> Second result should be from the cache");
                }
                Assert.AreEqual(upnResult?.LabUserUpn?.ToLowerInvariant(), upnResult?.AuthResultUpn?.ToLowerInvariant());
            }
        }

        private static void PrintCacheInfo()
        {
            if (File.Exists(CommonCacheTestUtils.AdalV3CacheFilePath))
            {
                Trace.WriteLine($"Adal Cache Exists at: {CommonCacheTestUtils.AdalV3CacheFilePath}");
                Trace.WriteLine("Adal Cache Size: " + Convert.ToInt32(new FileInfo(CommonCacheTestUtils.AdalV3CacheFilePath).Length));
            }
            else
            {
                Trace.WriteLine($"Adal Cache DOES NOT EXIST at: {CommonCacheTestUtils.AdalV3CacheFilePath}");
            }

            if (File.Exists(CommonCacheTestUtils.MsalV2CacheFilePath))
            {
                Trace.WriteLine($"MSAL V2 Cache Exists at: {CommonCacheTestUtils.MsalV2CacheFilePath}");
                Trace.WriteLine("MSAL V2 Cache Size: " + Convert.ToInt32(new FileInfo(CommonCacheTestUtils.MsalV2CacheFilePath).Length));
            }
            else
            {
                Trace.WriteLine($"MSAL V2 Cache DOES NOT EXIST at: {CommonCacheTestUtils.MsalV2CacheFilePath}");
            }

            if (File.Exists(CommonCacheTestUtils.MsalV3CacheFilePath))
            {
                Trace.WriteLine($"MSAL V3 Cache Exists at: {CommonCacheTestUtils.MsalV3CacheFilePath}");
                Trace.WriteLine("MSAL V3 Cache Size: " + Convert.ToInt32(new FileInfo(CommonCacheTestUtils.MsalV3CacheFilePath).Length));
            }
            else
            {
                Trace.WriteLine($"MSAL V3 Cache DOES NOT EXIST at: {CommonCacheTestUtils.MsalV3CacheFilePath}");
            }
        }
    }
}
