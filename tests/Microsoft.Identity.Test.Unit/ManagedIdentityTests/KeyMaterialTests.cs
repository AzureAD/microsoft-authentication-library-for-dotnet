// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;
using System;
using Microsoft.Identity.Client.Core;
#if SUPPORTS_MTLS
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
#if SUPPORTS_MTLS
        [TestMethod]
        public void GetOrCreateCertificateFromCryptoKeyInfo_NoKey_ReturnsCertificate()
        {
            // Arrange
            var provider = new ManagedIdentityCertificateProvider(_logger);

            // Act
            X509Certificate2 result = provider.GetOrCreateCertificateFromCryptoKeyInfo();

            // Assert
            Assert.IsNotNull(result, "Expected a certificate to be returned.");
            Assert.AreEqual("CN=ManagedIdentitySlcCertificate", result.Subject, "The certificate does not have the expected subject name.");
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
