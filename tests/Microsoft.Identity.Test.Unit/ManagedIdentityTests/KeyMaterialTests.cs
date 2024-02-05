// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;
using System;
using Microsoft.Identity.Client.Core;
#if SUPPORTS_MIV2
using Microsoft.Identity.Client.Platforms.netcore;
#endif
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Client.Internal.Logger;
using System.Security.Cryptography;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    /// <summary>
    /// Unit tests for Key Materials used in the Credential flow for MI.
    /// </summary>
    [TestClass]
    public class KeyMaterialTests : TestBase
    {
        private readonly ILoggerAdapter _logger = new NullLogger();
#if SUPPORTS_MIV2
        [TestMethod]
        public void GetOrCreateCertificateFromCryptoKeyInfo_NoKey_ReturnsNoCertificate()
        {
            // Arrange
            var provider = new ManagedIdentityCertificateProvider(_logger);

            // Act
            X509Certificate2 result = provider.GetOrCreateCertificateFromCryptoKeyInfo();

            // Assert
            Assert.AreEqual(null, result, "Expected no certificate to be returned.");
        }

        [TestMethod]
        public void CertificateNeedsRotation_DefaultRotationValue_ReturnsFalse()
        {
            // Arrange
            X509Certificate2 certificate = new X509Certificate2Builder()
                .WithSubjectName("CN=TestCert")
                .WithNotBefore(DateTime.UtcNow.AddMonths(-2))
                .WithNotAfter(DateTime.UtcNow.AddMonths(6))
                .Build();

            // Act
            var result = ManagedIdentityCertificateProvider.CertificateNeedsRotation(certificate);

            // Assert
            Assert.IsFalse(result, "Expected false since the rotation threshold is not exceeded.");
        }

        [TestMethod]
        public void CertificateNeedsRotation_OverrideRotationValueHigher_ReturnsFalse()
        {
            // Arrange
            X509Certificate2 certificate = new X509Certificate2Builder()
                .WithSubjectName("CN=TestCert")
                .WithNotBefore(DateTime.UtcNow.AddMonths(-2))
                .WithNotAfter(DateTime.UtcNow.AddMonths(6))
                .Build();

            int rotationThreshold = 99;

            // Act
            var result = ManagedIdentityCertificateProvider.CertificateNeedsRotation(certificate, rotationThreshold);

            // Assert
            Assert.IsFalse(result, "Expected false since the rotation threshold is not exceeded.");
        }

        [TestMethod]
        public void CertificateNeedsRotation_OverrideRotationValueLower_ReturnsFalse()
        {
            // Arrange
            X509Certificate2 certificate = new X509Certificate2Builder()
                .WithSubjectName("CN=TestCert")
                .WithNotBefore(DateTime.UtcNow.AddMonths(-2))
                .WithNotAfter(DateTime.UtcNow.AddMonths(6))
                .Build();

            int rotationThreshold = 1;

            // Act
            var result = ManagedIdentityCertificateProvider.CertificateNeedsRotation(certificate, rotationThreshold);

            // Assert
            Assert.IsTrue(result, "Expected true since the rotation threshold is exceeded.");
        }

        [TestMethod]
        public void CertificateNeedsRotation_CertificateExpired_ReturnsTrue()
        {
            // Arrange
            X509Certificate2 certificate = new X509Certificate2Builder()
                .WithSubjectName("CN=TestCert")
                .WithNotBefore(DateTime.UtcNow.AddMonths(-12))
                .WithNotAfter(DateTime.UtcNow.AddMonths(-6))
                .Build();

            // Act
            var result = ManagedIdentityCertificateProvider.CertificateNeedsRotation(certificate);

            // Assert
            Assert.IsTrue(result, "Expected true since the certificate is expired.");
        }

        [TestMethod]
        public void CertificateNeedsRotation_CertificateForceRotate_ReturnsFalse()
        {
            // Arrange
            X509Certificate2 certificate = new X509Certificate2Builder()
                .WithSubjectName("CN=TestCert")
                .WithNotBefore(DateTime.UtcNow)
                .WithNotAfter(DateTime.UtcNow.AddDays(1))
                .Build();

            // Act
            var result = ManagedIdentityCertificateProvider.CertificateNeedsRotation(certificate, 0);

            // Assert
            Assert.IsTrue(result, "Expected true since we rotate always.");
        }

        [TestMethod]
        public void CertificateNeedsRotation_CertificateExpiresSoonButAboveCustomThreshold_ReturnsTrue()
        {
            // Arrange
            X509Certificate2 certificate = new X509Certificate2Builder()
                .WithSubjectName("CN=TestCert")
                .WithNotBefore(DateTime.UtcNow)
                .WithNotAfter(DateTime.UtcNow.AddDays(1))
                .Build();

            int rotationThreshold = 49;

            // Act
            var result = ManagedIdentityCertificateProvider.CertificateNeedsRotation(certificate, rotationThreshold);

            // Assert
            Assert.IsFalse(result, "Expected false since the certificate expiration is below the custom rotation threshold.");
        }

        [TestMethod]
        public void GetCngKey_NoKeyAvailable()
        {
            // Arrange
            var provider = new ManagedIdentityCertificateProvider(_logger);

            // Act
            ECDsaCng result = provider.GetCngKey();

            // Assert
            Assert.IsNull(result, "Expected no key to be returned.");
        }

        [TestMethod]
        public void TryGetKeyMaterial_KeyDoesNotExist_ReturnsFalseAndNullKey()
        {
            // Arrange
            var provider = new ManagedIdentityCertificateProvider(_logger);
            var keyProviderName = "YourKeyProvider";
            var keyName = "NonExistentKeyName";

            // Act
            var result = provider.TryGetKeyMaterial(keyProviderName, keyName, CngKeyOpenOptions.None, out var eCDsaCng);

            // Assert
            Assert.IsFalse(result, "Expected false since the key does not exist.");
            Assert.IsNull(eCDsaCng, "Expected a null key when the key does not exist.");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void CryptoKeyType_ShouldThrowException_IfNotInitialized()
        {
            // Arrange
            ILoggerAdapter logger = Substitute.For<ILoggerAdapter>();
            ManagedIdentityCertificateProvider provider = new(logger);

            // Act & Assert
            _ = provider.CryptoKeyType;
        }
#endif
    }
}
