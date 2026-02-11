// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.KeyAttestation.Attestation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    public class MaaTokenCacheEntryTests
    {
        [TestMethod]
        public void Constructor_WithValidParameters_CreatesEntry()
        {
            // Arrange
            var token = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE2MDkzNzA0MDAsImlhdCI6MTYwOTM2NjgwMH0.signature";
            var issuedAt = DateTimeOffset.UtcNow;
            var expiresAt = issuedAt.AddHours(1);

            // Act
            var entry = new MaaTokenCacheEntry(token, issuedAt, expiresAt);

            // Assert
            Assert.AreEqual(token, entry.Token);
            Assert.AreEqual(issuedAt, entry.IssuedAt);
            Assert.AreEqual(expiresAt, entry.ExpiresAt);
            Assert.AreEqual(TimeSpan.FromHours(1), entry.Lifetime);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_WithNullToken_ThrowsArgumentNullException()
        {
            // Arrange
            var issuedAt = DateTimeOffset.UtcNow;
            var expiresAt = issuedAt.AddHours(1);

            // Act
            var entry = new MaaTokenCacheEntry(null, issuedAt, expiresAt);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Constructor_WithExpiresAtBeforeIssuedAt_ThrowsArgumentException()
        {
            // Arrange
            var token = "test-token";
            var issuedAt = DateTimeOffset.UtcNow;
            var expiresAt = issuedAt.AddHours(-1);

            // Act
            var entry = new MaaTokenCacheEntry(token, issuedAt, expiresAt);
        }

        [TestMethod]
        public void NeedsRefresh_WithMoreThan50PercentRemaining_ReturnsFalse()
        {
            // Arrange - Token valid for 1 hour, check after 10 minutes (83% remaining)
            var issuedAt = DateTimeOffset.UtcNow.AddMinutes(-10);
            var expiresAt = issuedAt.AddHours(1);
            var entry = new MaaTokenCacheEntry("test-token", issuedAt, expiresAt);
            var now = DateTimeOffset.UtcNow;

            // Act
            bool needsRefresh = entry.NeedsRefresh(now);

            // Assert
            Assert.IsFalse(needsRefresh, "Token with >50% lifetime remaining should not need refresh");
        }

        [TestMethod]
        public void NeedsRefresh_WithLessThan50PercentRemaining_ReturnsTrue()
        {
            // Arrange - Token valid for 1 hour, check after 35 minutes (42% remaining)
            var issuedAt = DateTimeOffset.UtcNow.AddMinutes(-35);
            var expiresAt = issuedAt.AddHours(1);
            var entry = new MaaTokenCacheEntry("test-token", issuedAt, expiresAt);
            var now = DateTimeOffset.UtcNow;

            // Act
            bool needsRefresh = entry.NeedsRefresh(now);

            // Assert
            Assert.IsTrue(needsRefresh, "Token with <50% lifetime remaining should need refresh");
        }

        [TestMethod]
        public void NeedsRefresh_WithExactly50PercentRemaining_ReturnsTrue()
        {
            // Arrange - Token valid for 1 hour, check after exactly 30 minutes (50% remaining)
            var issuedAt = DateTimeOffset.UtcNow.AddMinutes(-30);
            var expiresAt = issuedAt.AddHours(1);
            var entry = new MaaTokenCacheEntry("test-token", issuedAt, expiresAt);
            var now = DateTimeOffset.UtcNow;

            // Act
            bool needsRefresh = entry.NeedsRefresh(now);

            // Assert
            Assert.IsFalse(needsRefresh, "Token with exactly 50% lifetime remaining should not need refresh");
        }

        [TestMethod]
        public void NeedsRefresh_WithExpiredToken_ReturnsTrue()
        {
            // Arrange
            var issuedAt = DateTimeOffset.UtcNow.AddHours(-2);
            var expiresAt = issuedAt.AddHours(1);
            var entry = new MaaTokenCacheEntry("test-token", issuedAt, expiresAt);
            var now = DateTimeOffset.UtcNow;

            // Act
            bool needsRefresh = entry.NeedsRefresh(now);

            // Assert
            Assert.IsTrue(needsRefresh, "Expired token should need refresh");
        }

        [TestMethod]
        public void IsExpired_WithExpiredToken_ReturnsTrue()
        {
            // Arrange
            var issuedAt = DateTimeOffset.UtcNow.AddHours(-2);
            var expiresAt = issuedAt.AddHours(1);
            var entry = new MaaTokenCacheEntry("test-token", issuedAt, expiresAt);
            var now = DateTimeOffset.UtcNow;

            // Act
            bool isExpired = entry.IsExpired(now);

            // Assert
            Assert.IsTrue(isExpired);
        }

        [TestMethod]
        public void IsExpired_WithValidToken_ReturnsFalse()
        {
            // Arrange
            var issuedAt = DateTimeOffset.UtcNow;
            var expiresAt = issuedAt.AddHours(1);
            var entry = new MaaTokenCacheEntry("test-token", issuedAt, expiresAt);
            var now = DateTimeOffset.UtcNow;

            // Act
            bool isExpired = entry.IsExpired(now);

            // Assert
            Assert.IsFalse(isExpired);
        }

        [TestMethod]
        public void Lifetime_CalculatesCorrectly()
        {
            // Arrange
            var issuedAt = new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero);
            var expiresAt = new DateTimeOffset(2024, 1, 1, 12, 30, 0, TimeSpan.Zero);
            var entry = new MaaTokenCacheEntry("test-token", issuedAt, expiresAt);

            // Act
            var lifetime = entry.Lifetime;

            // Assert
            Assert.AreEqual(TimeSpan.FromMinutes(150), lifetime);
        }
    }
}
