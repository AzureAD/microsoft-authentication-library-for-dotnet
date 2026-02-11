// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.KeyAttestation.Attestation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    public class MaaTokenCacheTests
    {
        private class TestPersistentCache : IPersistentMaaTokenCache
        {
            private MaaTokenCacheEntry _entry;
            private string _lastWriteKey;

            public bool TryRead(string cacheKey, out MaaTokenCacheEntry entry, Action<string> logVerbose)
            {
                entry = _entry;
                return _entry != null;
            }

            public void TryWrite(string cacheKey, MaaTokenCacheEntry entry, Action<string> logVerbose)
            {
                _entry = entry;
                _lastWriteKey = cacheKey;
            }

            public void TryDelete(string cacheKey, Action<string> logVerbose)
            {
                _entry = null;
            }

            public string LastWriteKey => _lastWriteKey;
        }

        [TestMethod]
        public async Task GetOrCreateAsync_WithNoCachedToken_CallsFactory()
        {
            // Arrange
            var cache = new MaaTokenCache(null);
            bool factoryCalled = false;

            Task<AttestationResult> Factory()
            {
                factoryCalled = true;
                // Create a valid JWT with exp and iat claims
                string jwt = CreateTestJwt(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));
                return Task.FromResult(new AttestationResult(AttestationStatus.Success, jwt, 0, null));
            }

            // Act
            var token = await cache.GetOrCreateAsync("test-key", Factory, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.IsTrue(factoryCalled);
            Assert.IsNotNull(token);
        }

        [TestMethod]
        public async Task GetOrCreateAsync_WithCachedToken_ReturnsFromCache()
        {
            // Arrange
            var cache = new MaaTokenCache(null);
            int factoryCallCount = 0;

            Task<AttestationResult> Factory()
            {
                factoryCallCount++;
                string jwt = CreateTestJwt(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));
                return Task.FromResult(new AttestationResult(AttestationStatus.Success, jwt, 0, null));
            }

            // Act - First call
            var token1 = await cache.GetOrCreateAsync("test-key", Factory, CancellationToken.None).ConfigureAwait(false);

            // Act - Second call (should use cache)
            var token2 = await cache.GetOrCreateAsync("test-key", Factory, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(1, factoryCallCount, "Factory should only be called once");
            Assert.AreEqual(token1, token2);
        }

        [TestMethod]
        public async Task GetOrCreateAsync_WithExpiredToken_CallsFactory()
        {
            // Arrange
            var cache = new MaaTokenCache(null);
            int factoryCallCount = 0;

            Task<AttestationResult> Factory()
            {
                factoryCallCount++;
                // First call: expired token, Second call: valid token
                if (factoryCallCount == 1)
                {
                    string jwt = CreateTestJwt(DateTimeOffset.UtcNow.AddHours(-2), DateTimeOffset.UtcNow.AddHours(-1));
                    return Task.FromResult(new AttestationResult(AttestationStatus.Success, jwt, 0, null));
                }
                else
                {
                    string jwt = CreateTestJwt(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));
                    return Task.FromResult(new AttestationResult(AttestationStatus.Success, jwt, 0, null));
                }
            }

            // Act - First call with expired token
            var token1 = await cache.GetOrCreateAsync("test-key", Factory, CancellationToken.None).ConfigureAwait(false);

            // Act - Second call (should call factory again due to expiration)
            var token2 = await cache.GetOrCreateAsync("test-key", Factory, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(2, factoryCallCount, "Factory should be called twice (expired + refresh)");
        }

        [TestMethod]
        public async Task GetOrCreateAsync_WithTokenNeedingRefresh_CallsFactory()
        {
            // Arrange
            var cache = new MaaTokenCache(null);
            int factoryCallCount = 0;

            Task<AttestationResult> Factory()
            {
                factoryCallCount++;
                if (factoryCallCount == 1)
                {
                    // First call: token with < 50% lifetime remaining (35 minutes elapsed out of 60 minutes)
                    string jwt = CreateTestJwt(DateTimeOffset.UtcNow.AddMinutes(-35), DateTimeOffset.UtcNow.AddMinutes(25));
                    return Task.FromResult(new AttestationResult(AttestationStatus.Success, jwt, 0, null));
                }
                else
                {
                    // Second call: fresh token
                    string jwt = CreateTestJwt(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));
                    return Task.FromResult(new AttestationResult(AttestationStatus.Success, jwt, 0, null));
                }
            }

            // Act - First call with token needing refresh
            var token1 = await cache.GetOrCreateAsync("test-key", Factory, CancellationToken.None).ConfigureAwait(false);

            // Act - Second call (should call factory due to 50% threshold)
            var token2 = await cache.GetOrCreateAsync("test-key", Factory, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(2, factoryCallCount, "Factory should be called twice (initial + refresh)");
        }

        [TestMethod]
        public async Task GetOrCreateAsync_WithPersistentCache_ReadsFromPersistent()
        {
            // Arrange
            var persistentCache = new TestPersistentCache();
            var cache = new MaaTokenCache(persistentCache);
            int factoryCallCount = 0;

            Task<AttestationResult> Factory()
            {
                factoryCallCount++;
                string jwt = CreateTestJwt(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));
                return Task.FromResult(new AttestationResult(AttestationStatus.Success, jwt, 0, null));
            }

            // Pre-populate persistent cache
            string prePopulatedJwt = CreateTestJwt(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));
            var entry = new MaaTokenCacheEntry(prePopulatedJwt, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));
            persistentCache.TryWrite("test-key", entry, null);

            // Act
            var token = await cache.GetOrCreateAsync("test-key", Factory, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(0, factoryCallCount, "Factory should not be called (persistent cache hit)");
            Assert.AreEqual(prePopulatedJwt, token);
        }

        [TestMethod]
        public async Task GetOrCreateAsync_WritesToPersistentCache()
        {
            // Arrange
            var persistentCache = new TestPersistentCache();
            var cache = new MaaTokenCache(persistentCache);

            Task<AttestationResult> Factory()
            {
                string jwt = CreateTestJwt(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));
                return Task.FromResult(new AttestationResult(AttestationStatus.Success, jwt, 0, null));
            }

            // Act
            var token = await cache.GetOrCreateAsync("test-key", Factory, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.AreEqual("test-key", persistentCache.LastWriteKey);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task GetOrCreateAsync_WithEmptyCacheKey_ThrowsArgumentException()
        {
            // Arrange
            var cache = new MaaTokenCache(null);

            // Act
            await cache.GetOrCreateAsync("", () => Task.FromResult(new AttestationResult(AttestationStatus.Success, "jwt", 0, null)), CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task GetOrCreateAsync_WithNullFactory_ThrowsArgumentNullException()
        {
            // Arrange
            var cache = new MaaTokenCache(null);

            // Act
            await cache.GetOrCreateAsync("test-key", null, CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task GetOrCreateAsync_WithFailedAttestation_ThrowsInvalidOperationException()
        {
            // Arrange
            var cache = new MaaTokenCache(null);

            Task<AttestationResult> Factory()
            {
                return Task.FromResult(new AttestationResult(AttestationStatus.NativeError, null, -1, "Native error"));
            }

            // Act
            await cache.GetOrCreateAsync("test-key", Factory, CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task GetOrCreateAsync_WithConcurrentCalls_OnlyCallsFactoryOnce()
        {
            // Arrange
            var cache = new MaaTokenCache(null);
            int factoryCallCount = 0;
            var factoryDelay = TimeSpan.FromMilliseconds(100);

            async Task<AttestationResult> Factory()
            {
                Interlocked.Increment(ref factoryCallCount);
                await Task.Delay(factoryDelay).ConfigureAwait(false);
                string jwt = CreateTestJwt(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));
                return new AttestationResult(AttestationStatus.Success, jwt, 0, null);
            }

            // Act - Start 5 concurrent calls
            var tasks = new Task<string>[5];
            for (int i = 0; i < 5; i++)
            {
                tasks[i] = cache.GetOrCreateAsync("test-key", Factory, CancellationToken.None);
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(1, factoryCallCount, "Factory should only be called once despite concurrent calls");
            // All tasks should return the same token
            for (int i = 1; i < 5; i++)
            {
                Assert.AreEqual(tasks[0].Result, tasks[i].Result);
            }
        }

        [TestMethod]
        public void ClearMemoryCache_RemovesAllEntries()
        {
            // Arrange
            var cache = new MaaTokenCache(null);
            int factoryCallCount = 0;

            Task<AttestationResult> Factory()
            {
                factoryCallCount++;
                string jwt = CreateTestJwt(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));
                return Task.FromResult(new AttestationResult(AttestationStatus.Success, jwt, 0, null));
            }

            // Pre-populate cache
            cache.GetOrCreateAsync("test-key", Factory, CancellationToken.None).Wait();

            // Act
            cache.ClearMemoryCache();

            // Re-fetch (should call factory again)
            cache.GetOrCreateAsync("test-key", Factory, CancellationToken.None).Wait();

            // Assert
            Assert.AreEqual(2, factoryCallCount, "Factory should be called twice (before and after clear)");
        }

        /// <summary>
        /// Helper to create a test JWT with specific timestamps.
        /// Format: header.payload.signature where payload contains exp and iat claims.
        /// </summary>
        private string CreateTestJwt(DateTimeOffset issuedAt, DateTimeOffset expiresAt)
        {
            // Create a simple JWT with exp and iat claims
            // Header: {"alg":"RS256","typ":"JWT"}
            string header = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9";

            // Payload with exp and iat
            long expUnix = expiresAt.ToUnixTimeSeconds();
            long iatUnix = issuedAt.ToUnixTimeSeconds();
            string payloadJson = $"{{\"exp\":{expUnix},\"iat\":{iatUnix}}}";
            string payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payloadJson))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

            return $"{header}.{payload}.signature";
        }
    }
}
