// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommonCache.Test.Common;
using CommonCache.Test.Unit.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommonCache.Test.Unit
{
    [TestClass]
    [DoNotParallelize]
    public class CacheStorageTests
    {
        [TestMethod]
        [DataRow(CacheStorageType.MsalV2)]
        [DataRow(CacheStorageType.MsalV2 | CacheStorageType.MsalV3)]
        public async Task CurrentClient_RejectsRetiredV2StorageBeforeSignInAsync(CacheStorageType storageType)
        {
            // Arrange
            var executable = CacheProgramFactory.CreateCacheProgram(CacheProgramType.MsalV3, storageType);
            string resultsPath = Path.GetTempFileName();
            var program = new CacheProgram(executable.ExecutablePath, resultsPath, storageType);
            var users = new[]
            {
                new LabUserData(
                    "user@contoso.com",
                    "unused",
                    "00000000-0000-0000-0000-000000000001",
                    "00000000-0000-0000-0000-000000000002")
            };

            try
            {
                // Act
                var result = await program.ExecuteAsync(users, CancellationToken.None).ConfigureAwait(false);

                // Assert
                Assert.IsFalse(result.ProcessExecutionFailed, result.StdErr);
                Assert.IsTrue(result.ExecutionResults.IsError);
                StringAssert.Contains(result.ExecutionResults.ErrorMessage, "requires MSAL v3 cache storage");
                StringAssert.Contains(result.ExecutionResults.StackTrace, "FileBasedTokenCacheHelper.ConfigureUserCache");
                Assert.HasCount(0, result.ExecutionResults.Results);
            }
            finally
            {
                File.Delete(resultsPath);
            }
        }
    }
}
