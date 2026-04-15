// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.


using System;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.OAuth2.Throttling;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.Throttling
{
    [TestClass]
    public class ThrottlingCacheExtendedTests : TestBase
    {
        private readonly ILoggerAdapter _logger = NSubstitute.Substitute.For<ILoggerAdapter>();
        private readonly MsalServiceException _ex1 = new MsalServiceException("code1", "msg1");
        private readonly MsalServiceException _ex2 = new MsalServiceException("code2", "msg2");

        [TestMethod]
        public void Clear_EmptiesCache()
        {
            var cache = new ThrottlingCache();
            cache.AddAndCleanup("k1", new ThrottlingCacheEntry(_ex1, TimeSpan.FromMinutes(10)), _logger);
            cache.AddAndCleanup("k2", new ThrottlingCacheEntry(_ex2, TimeSpan.FromMinutes(10)), _logger);

            Assert.IsFalse(cache.IsEmpty());

            cache.Clear();

            Assert.IsTrue(cache.IsEmpty());
        }

        [TestMethod]
        public void IsEmpty_EmptyCache_ReturnsTrue()
        {
            var cache = new ThrottlingCache();
            Assert.IsTrue(cache.IsEmpty());
        }

        [TestMethod]
        public void IsEmpty_NonEmptyCache_ReturnsFalse()
        {
            var cache = new ThrottlingCache();
            cache.AddAndCleanup("k1", new ThrottlingCacheEntry(_ex1, TimeSpan.FromMinutes(10)), _logger);
            Assert.IsFalse(cache.IsEmpty());
        }

        [TestMethod]
        public void TryGetOrRemoveExpired_MissingKey_ReturnsFalse()
        {
            var cache = new ThrottlingCache();
            bool found = cache.TryGetOrRemoveExpired("nonexistent", _logger, out var ex);
            Assert.IsFalse(found);
            Assert.IsNull(ex);
        }

        [TestMethod]
        public void AddAndCleanup_SameKey_KeepsNewerEntry()
        {
            var cache = new ThrottlingCache();
            var olderEntry = new ThrottlingCacheEntry(
                _ex1,
                DateTimeOffset.UtcNow.Subtract(TimeSpan.FromMinutes(1)),
                DateTimeOffset.UtcNow.AddMinutes(10));

            var newerEntry = new ThrottlingCacheEntry(
                _ex2,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddMinutes(10));

            cache.AddAndCleanup("k1", olderEntry, _logger);
            cache.AddAndCleanup("k1", newerEntry, _logger);

            cache.TryGetOrRemoveExpired("k1", _logger, out var foundEx);
            Assert.AreSame(_ex2, foundEx, "Should keep the newer entry");
        }

        [TestMethod]
        public void AddAndCleanup_SameKey_OlderEntryDoesNotReplaceNewer()
        {
            var cache = new ThrottlingCache();
            var newerEntry = new ThrottlingCacheEntry(
                _ex1,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddMinutes(10));

            var olderEntry = new ThrottlingCacheEntry(
                _ex2,
                DateTimeOffset.UtcNow.Subtract(TimeSpan.FromMinutes(1)),
                DateTimeOffset.UtcNow.AddMinutes(10));

            // Add newer first, then try to replace with older
            cache.AddAndCleanup("k1", newerEntry, _logger);
            cache.AddAndCleanup("k1", olderEntry, _logger);

            cache.TryGetOrRemoveExpired("k1", _logger, out var foundEx);
            Assert.AreSame(_ex1, foundEx, "Should keep the newer entry when an older entry is added later");
        }

        [TestMethod]
        public void CacheForTest_ExposesInternalDictionary()
        {
            var cache = new ThrottlingCache();
            Assert.IsNotNull(cache.CacheForTest);
            Assert.IsEmpty(cache.CacheForTest);

            cache.AddAndCleanup("k1", new ThrottlingCacheEntry(_ex1, TimeSpan.FromMinutes(10)), _logger);
            Assert.HasCount(1, cache.CacheForTest);
        }

        [TestMethod]
        public void Cleanup_RemovesOnlyExpiredEntries()
        {
            // Use a cleanup interval of 0 so cleanup triggers immediately on next add
            var cache = new ThrottlingCache(0);

            var expiredEntry = new ThrottlingCacheEntry(_ex1, TimeSpan.FromMilliseconds(-10000));
            var validEntry = new ThrottlingCacheEntry(_ex2, TimeSpan.FromMinutes(10));

            cache.AddAndCleanup("expired", expiredEntry, _logger);
            cache.AddAndCleanup("valid", validEntry, _logger);

            // Trigger cleanup via another add (interval is 0ms so it fires immediately)
            cache.AddAndCleanup("trigger", new ThrottlingCacheEntry(_ex2, TimeSpan.FromMinutes(10)), _logger);

            Assert.IsFalse(cache.TryGetOrRemoveExpired("expired", _logger, out _), "Expired entry should be cleaned up");
            Assert.IsTrue(cache.TryGetOrRemoveExpired("valid", _logger, out _), "Valid entry should remain");
            Assert.IsTrue(cache.TryGetOrRemoveExpired("trigger", _logger, out _), "Trigger entry should remain");
        }
    }
}
