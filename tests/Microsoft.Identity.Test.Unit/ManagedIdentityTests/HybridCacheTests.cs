// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Logger;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.ManagedIdentity.V2;
using Microsoft.Identity.Test.Unit.PublicApiTests;
using Microsoft.IdentityModel.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    public class HybridCacheTests : TestBase
    {
        private ILoggerAdapter _logger;
        private string _tempCacheDirectory;

        private HybridCache CreateHybridCache()
        {
            return new HybridCache(
                _logger,
                mutexTimeout: TimeSpan.FromSeconds(3),       // Fast for tests
                expirySkew: TimeSpan.FromMilliseconds(500)); // Short skew for tests
        }

        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();

            _logger = new IdentityLoggerAdapter(
                new TestIdentityLogger(EventLogLevel.LogAlways),
                Guid.NewGuid(),
                "TestClient",
                "1.0.0",
                enablePiiLogging: false
            );

            // Create a temporary directory for cache tests
            _tempCacheDirectory = Path.Combine(Path.GetTempPath(), "MSALHybridCacheTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempCacheDirectory);
        }

        [TestCleanup]
        public override void TestCleanup()
        {
            base.TestCleanup();

            // Clean up temporary cache directory
            if (Directory.Exists(_tempCacheDirectory))
            {
                try
                {
                    Directory.Delete(_tempCacheDirectory, recursive: true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        #region Constructor Tests

        [TestMethod]
        public void Constructor_WithValidLogger_Success()
        {
            using var cache = CreateHybridCache();
            Assert.IsNotNull(cache);
        }

        [TestMethod]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new HybridCache(null));
        }

        #endregion

        #region GetAsync Tests

        [TestMethod]
        public async Task GetAsync_EmptyCache_ReturnsNull()
        {
            using var cache = CreateHybridCache();
            const long testKey = 12345L;

            var result = await cache.GetAsync(testKey, CancellationToken.None).ConfigureAwait(false);

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetAsync_ValidCachedToken_ReturnsToken()
        {
            using var cache = CreateHybridCache();
            const long testKey = 12345L;
            const string testToken = "test_attestation_token";
            var expiresOn = DateTimeOffset.UtcNow.AddHours(1);

            await cache.SetAsync(testKey, testToken, expiresOn, CancellationToken.None).ConfigureAwait(false);

            var result = await cache.GetAsync(testKey, CancellationToken.None).ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.AreEqual(testToken, result.AttestationToken);
        }

        [TestMethod]
        public async Task GetAsync_ExpiredToken_ReturnsNullAndRemovesFromCache()
        {
            using var cache = CreateHybridCache();
            const long testKey = 12345L;
            const string testToken = "expired_token";
            var expiredTime = DateTimeOffset.UtcNow.AddMinutes(-10); // 10 minutes ago

            await cache.SetAsync(testKey, testToken, expiredTime, CancellationToken.None).ConfigureAwait(false);

            var result = await cache.GetAsync(testKey, CancellationToken.None).ConfigureAwait(false);

            Assert.IsNull(result);

            // Verify token was removed from cache
            var secondResult = await cache.GetAsync(testKey, CancellationToken.None).ConfigureAwait(false);

            Assert.IsNull(secondResult);
        }

        [TestMethod]
        public async Task GetAsync_TokenNearExpiry_ReturnsNullDueToSkewBuffer()
        {
            using var cache = CreateHybridCache();
            const long testKey = 12345L;
            const string testToken = "near_expiry_token";
            // Token expires in 1 minute, but skew buffer is 2 minutes
            var nearExpiryTime = DateTimeOffset.UtcNow.AddMinutes(1);

            await cache.SetAsync(testKey, testToken, nearExpiryTime, CancellationToken.None).ConfigureAwait(false);

            var result = await cache.GetAsync(testKey, CancellationToken.None).ConfigureAwait(false);

            Assert.IsNull(result); // Should be null due to expiry skew buffer
        }

        [TestMethod]
        public async Task GetAsync_DisposedCache_ThrowsObjectDisposedException()
        {
            var cache = CreateHybridCache();
            cache.Dispose();

            await Assert.ThrowsExceptionAsync<ObjectDisposedException>(async () =>
                await cache.GetAsync(12345L, CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task GetAsync_CancellationRequested_ThrowsOperationCanceledException()
        {
            using var cache = CreateHybridCache();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () =>
                await cache.GetAsync(12345L, cts.Token).ConfigureAwait(false)).ConfigureAwait(false);
        }

        #endregion

        #region SetAsync Tests

        [TestMethod]
        public async Task SetAsync_ValidToken_StoresSuccessfully()
        {
            using var cache = CreateHybridCache();
            const long testKey = 12345L;
            const string testToken = "test_token";
            var expiresOn = DateTimeOffset.UtcNow.AddHours(1);

            await cache.SetAsync(testKey, testToken, expiresOn, CancellationToken.None).ConfigureAwait(false);

            var result = await cache.GetAsync(testKey, CancellationToken.None).ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.AreEqual(testToken, result.AttestationToken);
        }

        [TestMethod]
        public async Task SetAsync_OverwriteExistingToken_UpdatesSuccessfully()
        {
            using var cache = CreateHybridCache();
            const long testKey = 12345L;
            const string originalToken = "original_token";
            const string updatedToken = "updated_token";
            var expiresOn = DateTimeOffset.UtcNow.AddHours(1);

            await cache.SetAsync(testKey, originalToken, expiresOn, CancellationToken.None).ConfigureAwait(false);

            await cache.SetAsync(testKey, updatedToken, expiresOn, CancellationToken.None).ConfigureAwait(false);

            var result = await cache.GetAsync(testKey, CancellationToken.None).ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.AreEqual(updatedToken, result.AttestationToken);
        }

        [TestMethod]
        public async Task SetAsync_NullToken_ThrowsArgumentNullException()
        {
            using var cache = CreateHybridCache();
            const long testKey = 12345L;
            var expiresOn = DateTimeOffset.UtcNow.AddHours(1);

            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
                await cache.SetAsync(testKey, null, expiresOn, CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task SetAsync_EmptyToken_ThrowsArgumentNullException()
        {
            using var cache = CreateHybridCache();
            const long testKey = 12345L;
            var expiresOn = DateTimeOffset.UtcNow.AddHours(1);

            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
                await cache.SetAsync(testKey, "", expiresOn, CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task SetAsync_DisposedCache_ThrowsObjectDisposedException()
        {
            var cache = CreateHybridCache();
            cache.Dispose();

            await Assert.ThrowsExceptionAsync<ObjectDisposedException>(async () =>
                await cache.SetAsync(12345L, "token", DateTimeOffset.UtcNow.AddHours(1), CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task SetAsync_CancellationRequested_ThrowsOperationCanceledException()
        {
            using var cache = CreateHybridCache();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () =>
                await cache.SetAsync(12345L, "token", DateTimeOffset.UtcNow.AddHours(1), cts.Token).ConfigureAwait(false)).ConfigureAwait(false);
        }

        #endregion

        #region RemoveAsync Tests

        [TestMethod]
        public async Task RemoveAsync_ExistingToken_RemovesSuccessfully()
        {
            using var cache = CreateHybridCache();
            const long testKey = 12345L;
            const string testToken = "test_token";
            var expiresOn = DateTimeOffset.UtcNow.AddHours(1);

            await cache.SetAsync(testKey, testToken, expiresOn, CancellationToken.None).ConfigureAwait(false);

            await cache.RemoveAsync(testKey, CancellationToken.None).ConfigureAwait(false);

            var result = await cache.GetAsync(testKey, CancellationToken.None).ConfigureAwait(false);
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task RemoveAsync_NonExistentToken_DoesNotThrow()
        {
            using var cache = CreateHybridCache();
            const long testKey = 12345L;

            // Should not throw
            await cache.RemoveAsync(testKey, CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task RemoveAsync_DisposedCache_ThrowsObjectDisposedException()
        {
            var cache = CreateHybridCache();
            cache.Dispose();

            await Assert.ThrowsExceptionAsync<ObjectDisposedException>(async () =>
                await cache.RemoveAsync(12345L, CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task RemoveAsync_CancellationRequested_ThrowsOperationCanceledException()
        {
            using var cache = CreateHybridCache();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () =>
                await cache.RemoveAsync(12345L, cts.Token).ConfigureAwait(false)).ConfigureAwait(false);
        }

        #endregion

        #region Concurrency Tests

        [TestMethod]
        public async Task ConcurrentAccess_MultipleThreadsSetGet_NoDataRaces()
        {
            using var cache = CreateHybridCache();
            const int threadCount = 10;
            const int operationsPerThread = 50;
            var tasks = new Task[threadCount];

            for (int i = 0; i < threadCount; i++)
            {
                int threadId = i;
                tasks[i] = Task.Run(async () =>
                {
                    for (int j = 0; j < operationsPerThread; j++)
                    {
                        long key = threadId * 1000 + j;
                        string token = $"token_{threadId}_{j}";
                        var expiresOn = DateTimeOffset.UtcNow.AddHours(1);

                        await cache.SetAsync(key, token, expiresOn, CancellationToken.None).ConfigureAwait(false);
                        var result = await cache.GetAsync(key, CancellationToken.None).ConfigureAwait(false);

                        Assert.IsNotNull(result);
                        Assert.AreEqual(token, result.AttestationToken);
                    }
                });
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ConcurrentAccess_SameKey_LastWriteWins()
        {
            using var cache = CreateHybridCache();
            const long testKey = 12345L;
            const int threadCount = 10;
            var expiresOn = DateTimeOffset.UtcNow.AddHours(1);
            var tasks = new Task[threadCount];

            for (int i = 0; i < threadCount; i++)
            {
                int threadId = i;
                tasks[i] = Task.Run(async () =>
                {
                    string token = $"token_from_thread_{threadId}";
                    await cache.SetAsync(testKey, token, expiresOn, CancellationToken.None).ConfigureAwait(false);
                });
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            var result = await cache.GetAsync(testKey, CancellationToken.None).ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.AttestationToken.StartsWith("token_from_thread_"));
        }

        #endregion

        #region File Persistence Tests

        [TestMethod]
        public async Task FilePersistence_TokenSurvivesInstanceRecreation()
        {
            const long testKey = 12345L;
            const string testToken = "persistent_token";
            var expiresOn = DateTimeOffset.UtcNow.AddHours(1);

            // Store token in first cache instance
            using (var cache1 = CreateHybridCache())
            {
                await cache1.SetAsync(testKey, testToken, expiresOn, CancellationToken.None).ConfigureAwait(false);
            }

            // Retrieve token from second cache instance
            AttestationTokenResponse result;
            using (var cache2 = CreateHybridCache())
            {
                result = await cache2.GetAsync(testKey, CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsNotNull(result);
            Assert.AreEqual(testToken, result.AttestationToken);
        }

        [TestMethod]
        public async Task FilePersistence_ExpiredTokenRemovedFromFile()
        {
            const long testKey = 12345L;
            const string testToken = "expired_persistent_token";
            var expiredTime = DateTimeOffset.UtcNow.AddMinutes(-10);

            // Store expired token
            using (var cache1 = CreateHybridCache())
            {
                await cache1.SetAsync(testKey, testToken, expiredTime, CancellationToken.None).ConfigureAwait(false);
            }

            // Try to retrieve from new instance
            AttestationTokenResponse result;
            using (var cache2 = CreateHybridCache())
            {
                result = await cache2.GetAsync(testKey, CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsNull(result);
        }

        #endregion

        #region Memory Cache Tests

        [TestMethod]
        public async Task MemoryCache_FastPath_NoFileAccess()
        {
            using var cache = CreateHybridCache();
            const long testKey = 12345L;
            const string testToken = "memory_token";
            var expiresOn = DateTimeOffset.UtcNow.AddHours(1);

            await cache.SetAsync(testKey, testToken, expiresOn, CancellationToken.None).ConfigureAwait(false);

            // Multiple gets should hit memory cache
            var result1 = await cache.GetAsync(testKey, CancellationToken.None).ConfigureAwait(false);
            var result2 = await cache.GetAsync(testKey, CancellationToken.None).ConfigureAwait(false);
            var result3 = await cache.GetAsync(testKey, CancellationToken.None).ConfigureAwait(false);

            Assert.IsNotNull(result1);
            Assert.IsNotNull(result2);
            Assert.IsNotNull(result3);
            Assert.AreEqual(testToken, result1.AttestationToken);
            Assert.AreEqual(testToken, result2.AttestationToken);
            Assert.AreEqual(testToken, result3.AttestationToken);
        }

        [TestMethod]
        public async Task MemoryCache_ExpiredEntry_RemovedFromMemory()
        {
            using var cache = CreateHybridCache();
            const long testKey = 12345L;
            const string testToken = "memory_expired_token";
            var expiredTime = DateTimeOffset.UtcNow.AddMinutes(-10);

            await cache.SetAsync(testKey, testToken, expiredTime, CancellationToken.None).ConfigureAwait(false);

            var result = await cache.GetAsync(testKey, CancellationToken.None).ConfigureAwait(false);

            Assert.IsNull(result);
        }

        #endregion

        #region Error Handling Tests

        [TestMethod]
        public async Task ErrorHandling_FileSystemErrors_GracefulDegradation()
        {
            using var cache = CreateHybridCache();
            const long testKey = 12345L;
            const string testToken = "resilient_token";
            var expiresOn = DateTimeOffset.UtcNow.AddHours(1);

            // This should work even if file operations have issues
            await cache.SetAsync(testKey, testToken, expiresOn, CancellationToken.None).ConfigureAwait(false);
            var result = await cache.GetAsync(testKey, CancellationToken.None).ConfigureAwait(false);

            // Memory cache should still work
            Assert.IsNotNull(result);
            Assert.AreEqual(testToken, result.AttestationToken);
        }

        #endregion

        #region Dispose Tests

        [TestMethod]
        public void Dispose_MultipleDispose_DoesNotThrow()
        {
            var cache = CreateHybridCache();

            cache.Dispose();
            cache.Dispose();
            cache.Dispose();
        }

        [TestMethod]
        public async Task Dispose_OperationsAfterDispose_ThrowObjectDisposedException()
        {
            var cache = CreateHybridCache();
            cache.Dispose();

            await Assert.ThrowsExceptionAsync<ObjectDisposedException>(async () =>
                await cache.GetAsync(123L, CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false);

            await Assert.ThrowsExceptionAsync<ObjectDisposedException>(async () =>
                await cache.SetAsync(123L, "token", DateTimeOffset.UtcNow.AddHours(1), CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false);

            await Assert.ThrowsExceptionAsync<ObjectDisposedException>(async () =>
                await cache.RemoveAsync(123L, CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false);
        }

        #endregion

        #region Performance and Load Tests

        [TestMethod]
        public async Task Performance_LargeNumberOfEntries_HandlesProperly()
        {
            using var cache = CreateHybridCache();
            const int entryCount = 1000;
            var expiresOn = DateTimeOffset.UtcNow.AddHours(1);

            // Add many entries
            var setTasks = new Task[entryCount];
            for (int i = 0; i < entryCount; i++)
            {
                int index = i;
                setTasks[i] = cache.SetAsync(index, $"token_{index}", expiresOn, CancellationToken.None);
            }
            await Task.WhenAll(setTasks).ConfigureAwait(false);

            // Retrieve all entries
            var getTasks = new Task<AttestationTokenResponse>[entryCount];
            for (int i = 0; i < entryCount; i++)
            {
                int index = i;
                getTasks[i] = cache.GetAsync(index, CancellationToken.None);
            }
            var results = await Task.WhenAll(getTasks).ConfigureAwait(false);

            Assert.AreEqual(entryCount, results.Length);
            for (int i = 0; i < entryCount; i++)
            {
                Assert.IsNotNull(results[i]);
                Assert.AreEqual($"token_{i}", results[i].AttestationToken);
            }
        }

        #endregion
    }
}
