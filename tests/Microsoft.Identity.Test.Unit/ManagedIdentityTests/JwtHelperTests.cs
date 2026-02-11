// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.KeyAttestation.Attestation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    public class JwtHelperTests
    {
        [TestMethod]
        public void TryExtractTimestamps_WithValidJwt_ReturnsTrue()
        {
            // Arrange - Create a JWT with exp=1609459200 (2021-01-01 00:00:00 UTC) and iat=1609455600 (2020-12-31 23:00:00 UTC)
            // Header: {"alg":"RS256","typ":"JWT"}
            // Payload: {"exp":1609459200,"iat":1609455600}
            string jwt = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE2MDk0NTkyMDAsImlhdCI6MTYwOTQ1NTYwMH0.signature";

            // Act
            bool result = JwtHelper.TryExtractTimestamps(jwt, out var issuedAt, out var expiresAt);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(new DateTimeOffset(2020, 12, 31, 23, 0, 0, TimeSpan.Zero), issuedAt);
            Assert.AreEqual(new DateTimeOffset(2021, 1, 1, 0, 0, 0, TimeSpan.Zero), expiresAt);
        }

        [TestMethod]
        public void TryExtractTimestamps_WithValidJwtNoIat_UsesDefaultIat()
        {
            // Arrange - JWT with only exp claim (no iat)
            // Payload: {"exp":1609459200}
            string jwt = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE2MDk0NTkyMDB9.signature";

            // Act
            bool result = JwtHelper.TryExtractTimestamps(jwt, out var issuedAt, out var expiresAt);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(new DateTimeOffset(2021, 1, 1, 0, 0, 0, TimeSpan.Zero), expiresAt);
            // iat should be 1 hour before exp as default
            Assert.AreEqual(expiresAt.AddHours(-1), issuedAt);
        }

        [TestMethod]
        public void TryExtractTimestamps_WithNullJwt_ReturnsFalse()
        {
            // Act
            bool result = JwtHelper.TryExtractTimestamps(null, out var issuedAt, out var expiresAt);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(DateTimeOffset.MinValue, issuedAt);
            Assert.AreEqual(DateTimeOffset.MinValue, expiresAt);
        }

        [TestMethod]
        public void TryExtractTimestamps_WithEmptyJwt_ReturnsFalse()
        {
            // Act
            bool result = JwtHelper.TryExtractTimestamps("", out var issuedAt, out var expiresAt);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(DateTimeOffset.MinValue, issuedAt);
            Assert.AreEqual(DateTimeOffset.MinValue, expiresAt);
        }

        [TestMethod]
        public void TryExtractTimestamps_WithInvalidFormat_ReturnsFalse()
        {
            // Arrange - JWT with only 2 parts (missing signature)
            string jwt = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE2MDk0NTkyMDB9";

            // Act
            bool result = JwtHelper.TryExtractTimestamps(jwt, out var issuedAt, out var expiresAt);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TryExtractTimestamps_WithInvalidBase64_ReturnsFalse()
        {
            // Arrange - JWT with invalid base64 in payload
            string jwt = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.invalid!!!base64.signature";

            // Act
            bool result = JwtHelper.TryExtractTimestamps(jwt, out var issuedAt, out var expiresAt);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TryExtractTimestamps_WithNoExpClaim_ReturnsFalse()
        {
            // Arrange - JWT without exp claim
            // Payload: {"sub":"user123"}
            string jwt = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ1c2VyMTIzIn0.signature";

            // Act
            bool result = JwtHelper.TryExtractTimestamps(jwt, out var issuedAt, out var expiresAt);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TryExtractTimestamps_WithBase64UrlEncoding_HandlesCorrectly()
        {
            // Arrange - JWT with Base64Url encoding (- and _ characters)
            // This is a real-world JWT format test
            // Payload with special characters that require Base64Url encoding
            string jwt = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE2MDk0NTkyMDAsImlhdCI6MTYwOTQ1NTYwMCwic3ViIjoidXNlci0xMjNfNDU2In0.signature";

            // Act
            bool result = JwtHelper.TryExtractTimestamps(jwt, out var issuedAt, out var expiresAt);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(new DateTimeOffset(2020, 12, 31, 23, 0, 0, TimeSpan.Zero), issuedAt);
            Assert.AreEqual(new DateTimeOffset(2021, 1, 1, 0, 0, 0, TimeSpan.Zero), expiresAt);
        }

        [TestMethod]
        public void TryExtractTimestamps_WithPaddingRequired_HandlesCorrectly()
        {
            // Arrange - JWT where Base64 decoding requires padding
            // Payload length not divisible by 4 - requires padding
            string jwt = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE2MDk0NTkyMDAsImlhdCI6MTYwOTQ1NTYwMCwiYSI6ImIifQ.signature";

            // Act
            bool result = JwtHelper.TryExtractTimestamps(jwt, out var issuedAt, out var expiresAt);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(new DateTimeOffset(2020, 12, 31, 23, 0, 0, TimeSpan.Zero), issuedAt);
            Assert.AreEqual(new DateTimeOffset(2021, 1, 1, 0, 0, 0, TimeSpan.Zero), expiresAt);
        }
    }
}
