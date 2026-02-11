// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Identity.Client.KeyAttestation.Attestation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    public class PersistentMaaTokenCacheTests
    {
        private static readonly bool s_isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        [TestMethod]
        public void TryRead_OnNonWindows_ReturnsFalse()
        {
            // Arrange
            var cache = new PersistentMaaTokenCache();

            // Act
            bool result = cache.TryRead("test-key", out var entry, null);

            // Assert
            if (!s_isWindows)
            {
                Assert.IsFalse(result, "On non-Windows platforms, TryRead should return false");
                Assert.IsNull(entry);
            }
        }

        [TestMethod]
        public void TryWrite_OnNonWindows_DoesNotThrow()
        {
            // Arrange
            var cache = new PersistentMaaTokenCache();
            var entry = new MaaTokenCacheEntry("test-token", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));

            // Act & Assert - Should not throw on non-Windows
            cache.TryWrite("test-key", entry, null);
        }

        [TestMethod]
        public void TryDelete_OnNonWindows_DoesNotThrow()
        {
            // Arrange
            var cache = new PersistentMaaTokenCache();

            // Act & Assert - Should not throw on non-Windows
            cache.TryDelete("test-key", null);
        }

        [TestMethod]
        [TestCategory("WindowsOnly")]
        public void TryWrite_ThenTryRead_OnWindows_RoundTripsSuccessfully()
        {
            if (!s_isWindows)
            {
                Assert.Inconclusive("Test only runs on Windows");
                return;
            }

            // Arrange
            var cache = new PersistentMaaTokenCache();
            var issuedAt = DateTimeOffset.UtcNow;
            var expiresAt = issuedAt.AddHours(1);
            var token = "test-token-12345";
            var entry = new MaaTokenCacheEntry(token, issuedAt, expiresAt);
            var cacheKey = "test-key-" + Guid.NewGuid().ToString();

            try
            {
                // Act - Write
                cache.TryWrite(cacheKey, entry, null);

                // Act - Read
                bool readResult = cache.TryRead(cacheKey, out var readEntry, null);

                // Assert
                Assert.IsTrue(readResult, "TryRead should return true after TryWrite");
                Assert.IsNotNull(readEntry);
                Assert.AreEqual(token, readEntry.Token);
                Assert.AreEqual(issuedAt.ToUnixTimeSeconds(), readEntry.IssuedAt.ToUnixTimeSeconds());
                Assert.AreEqual(expiresAt.ToUnixTimeSeconds(), readEntry.ExpiresAt.ToUnixTimeSeconds());
            }
            finally
            {
                // Cleanup
                cache.TryDelete(cacheKey, null);
            }
        }

        [TestMethod]
        [TestCategory("WindowsOnly")]
        public void TryRead_WithExpiredToken_OnWindows_ReturnsFalse()
        {
            if (!s_isWindows)
            {
                Assert.Inconclusive("Test only runs on Windows");
                return;
            }

            // Arrange
            var cache = new PersistentMaaTokenCache();
            var issuedAt = DateTimeOffset.UtcNow.AddHours(-2);
            var expiresAt = issuedAt.AddHours(1); // Expired 1 hour ago
            var token = "expired-token";
            var entry = new MaaTokenCacheEntry(token, issuedAt, expiresAt);
            var cacheKey = "expired-key-" + Guid.NewGuid().ToString();

            try
            {
                // Act - Write expired token
                cache.TryWrite(cacheKey, entry, null);

                // Act - Try to read (should fail due to expiration)
                bool readResult = cache.TryRead(cacheKey, out var readEntry, null);

                // Assert
                Assert.IsFalse(readResult, "TryRead should return false for expired token");
                Assert.IsNull(readEntry);
            }
            finally
            {
                // Cleanup
                cache.TryDelete(cacheKey, null);
            }
        }

        [TestMethod]
        [TestCategory("WindowsOnly")]
        public void TryRead_WithNonExistentKey_OnWindows_ReturnsFalse()
        {
            if (!s_isWindows)
            {
                Assert.Inconclusive("Test only runs on Windows");
                return;
            }

            // Arrange
            var cache = new PersistentMaaTokenCache();
            var cacheKey = "non-existent-key-" + Guid.NewGuid().ToString();

            // Act
            bool result = cache.TryRead(cacheKey, out var entry, null);

            // Assert
            Assert.IsFalse(result);
            Assert.IsNull(entry);
        }

        [TestMethod]
        [TestCategory("WindowsOnly")]
        public void TryDelete_RemovesExpiredEntries_OnWindows()
        {
            if (!s_isWindows)
            {
                Assert.Inconclusive("Test only runs on Windows");
                return;
            }

            // Arrange
            var cache = new PersistentMaaTokenCache();
            var cacheKey = "delete-test-key-" + Guid.NewGuid().ToString();

            // Create and write an expired entry
            var issuedAt = DateTimeOffset.UtcNow.AddHours(-2);
            var expiresAt = issuedAt.AddHours(1);
            var entry = new MaaTokenCacheEntry("expired-token", issuedAt, expiresAt);

            try
            {
                cache.TryWrite(cacheKey, entry, null);

                // Act - Delete expired entries
                cache.TryDelete(cacheKey, null);

                // Assert - Should not be able to read after delete
                bool readResult = cache.TryRead(cacheKey, out var readEntry, null);
                Assert.IsFalse(readResult);
                Assert.IsNull(readEntry);
            }
            finally
            {
                // Cleanup (in case delete didn't work)
                cache.TryDelete(cacheKey, null);
            }
        }

        [TestMethod]
        [TestCategory("WindowsOnly")]
        public void TryWrite_WithMultipleKeys_OnWindows_IsolatesEntries()
        {
            if (!s_isWindows)
            {
                Assert.Inconclusive("Test only runs on Windows");
                return;
            }

            // Arrange
            var cache = new PersistentMaaTokenCache();
            var key1 = "multi-key-1-" + Guid.NewGuid().ToString();
            var key2 = "multi-key-2-" + Guid.NewGuid().ToString();

            var entry1 = new MaaTokenCacheEntry("token-1", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));
            var entry2 = new MaaTokenCacheEntry("token-2", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(2));

            try
            {
                // Act - Write both entries
                cache.TryWrite(key1, entry1, null);
                cache.TryWrite(key2, entry2, null);

                // Assert - Both should be readable independently
                Assert.IsTrue(cache.TryRead(key1, out var read1, null));
                Assert.IsTrue(cache.TryRead(key2, out var read2, null));
                Assert.AreEqual("token-1", read1.Token);
                Assert.AreEqual("token-2", read2.Token);
            }
            finally
            {
                // Cleanup
                cache.TryDelete(key1, null);
                cache.TryDelete(key2, null);
            }
        }

        [TestMethod]
        public void PersistentCache_WithNullLogVerbose_DoesNotThrow()
        {
            // Arrange
            var cache = new PersistentMaaTokenCache();
            var entry = new MaaTokenCacheEntry("test-token", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));
            var cacheKey = "null-log-key-" + Guid.NewGuid().ToString();

            try
            {
                // Act & Assert - Should not throw with null logVerbose
                cache.TryWrite(cacheKey, entry, null);
                cache.TryRead(cacheKey, out _, null);
                cache.TryDelete(cacheKey, null);
            }
            finally
            {
                // Cleanup
                if (s_isWindows)
                {
                    cache.TryDelete(cacheKey, null);
                }
            }
        }

        [TestMethod]
        [TestCategory("WindowsOnly")]
        public void TryWrite_Overwrites_ExistingEntry_OnWindows()
        {
            if (!s_isWindows)
            {
                Assert.Inconclusive("Test only runs on Windows");
                return;
            }

            // Arrange
            var cache = new PersistentMaaTokenCache();
            var cacheKey = "overwrite-key-" + Guid.NewGuid().ToString();

            var entry1 = new MaaTokenCacheEntry("token-1", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));
            var entry2 = new MaaTokenCacheEntry("token-2", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(2));

            try
            {
                // Act - Write first entry
                cache.TryWrite(cacheKey, entry1, null);

                // Act - Overwrite with second entry
                cache.TryWrite(cacheKey, entry2, null);

                // Assert - Should read the second entry
                bool result = cache.TryRead(cacheKey, out var readEntry, null);
                Assert.IsTrue(result);
                Assert.AreEqual("token-2", readEntry.Token);
            }
            finally
            {
                // Cleanup
                cache.TryDelete(cacheKey, null);
            }
        }
    }
}
