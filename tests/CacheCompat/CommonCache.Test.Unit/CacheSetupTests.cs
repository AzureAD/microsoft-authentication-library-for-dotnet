// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CommonCache.Test.Common;
using CommonCache.Test.Unit.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommonCache.Test.Unit
{
    [TestClass]
    [DoNotParallelize]
    public class CacheSetupTests
    {
        private string _previousFixture;
        private string _previousDeviceCode;
        private string _fixturePath;

        [TestInitialize]
        public void Initialize()
        {
            _previousFixture = Environment.GetEnvironmentVariable("MSAL_V5_DELEGATED_TOKEN_FIXTURE");
            _previousDeviceCode = Environment.GetEnvironmentVariable("MSAL_CACHECOMPAT_DEVICE_CODE");
            _fixturePath = Path.GetTempFileName();
            Environment.SetEnvironmentVariable("MSAL_V5_DELEGATED_TOKEN_FIXTURE", null);
            Environment.SetEnvironmentVariable("MSAL_CACHECOMPAT_DEVICE_CODE", null);
        }

        [TestCleanup]
        public void Cleanup()
        {
            Environment.SetEnvironmentVariable("MSAL_V5_DELEGATED_TOKEN_FIXTURE", _previousFixture);
            Environment.SetEnvironmentVariable("MSAL_CACHECOMPAT_DEVICE_CODE", _previousDeviceCode);
            File.Delete(_fixturePath);
        }

        [TestMethod]
        [DataRow(CacheStorageType.MsalV2)]
        [DataRow(CacheStorageType.MsalV3)]
        public async Task MissingDelegatedSetup_DoesNotDeleteCacheAsync(CacheStorageType storageType)
        {
            // Arrange
            var user = new LabUserData("user@contoso.com", "unused", "test-client", "test-tenant");
            var executor = new CacheTestExecutor(new[] { user }, storageType);
            string cachePath = storageType == CacheStorageType.MsalV2
                ? CommonCacheTestUtils.MsalV2CacheFilePath
                : CommonCacheTestUtils.MsalV3CacheFilePath;
            bool createdCache = !File.Exists(cachePath);
            if (createdCache)
            {
                CommonCacheTestUtils.EnsureCacheFileDirectoryExists();
                File.WriteAllText(cachePath, "cache-preflight-sentinel");
            }

            try
            {
                // Act / Assert
                await Assert.ThrowsAsync<AssertInconclusiveException>(() =>
                    executor.ExecuteAsync(CacheProgramType.MsalV3, CacheProgramType.MsalV3, CancellationToken.None))
                    .ConfigureAwait(false);
                Assert.IsTrue(File.Exists(cachePath), "Missing provisioning must not delete existing caches.");
                if (createdCache)
                {
                    Assert.AreEqual("cache-preflight-sentinel", File.ReadAllText(cachePath));
                }
            }
            finally
            {
                if (createdCache)
                {
                    File.Delete(cachePath);
                }
            }
        }

        [TestMethod]
        public void DelegatedSetup_RequiresEveryClientUserPair()
        {
            // Arrange
            var users = new[]
            {
                new LabUserData("first@contoso.com", "unused", "first-client", "test-tenant"),
                new LabUserData("second@contoso.com", "unused", "second-client", "test-tenant")
            };
            var executor = new CacheTestExecutor(users, CacheStorageType.MsalV3);
            Environment.SetEnvironmentVariable("MSAL_V5_DELEGATED_TOKEN_FIXTURE", _fixturePath);
            File.WriteAllText(_fixturePath, JsonSerializer.Serialize(users.Take(1).Select(user => new
            {
                user.ClientId,
                Username = user.Upn,
                RefreshToken = Guid.NewGuid().ToString()
            })));

            // Act / Assert
            Assert.ThrowsExactly<AssertInconclusiveException>(() =>
                executor.ValidateSetupPrerequisites(CacheProgramType.MsalV3));

            File.WriteAllText(_fixturePath, JsonSerializer.Serialize(users.Select(user => new
            {
                user.ClientId,
                Username = user.Upn,
                RefreshToken = Guid.NewGuid().ToString()
            })));
            executor.ValidateSetupPrerequisites(CacheProgramType.MsalV3);
        }

        [TestMethod]
        public void JavaProducer_DoesNotRequireDotNetBootstrapFixture()
        {
            // Arrange
            var user = new LabUserData("user@contoso.com", "unused", "test-client", "test-tenant");
            var executor = new CacheTestExecutor(new[] { user }, CacheStorageType.MsalV3);

            // Act / Assert
            executor.ValidateSetupPrerequisites(CacheProgramType.MsalJava);
        }
    }
}
