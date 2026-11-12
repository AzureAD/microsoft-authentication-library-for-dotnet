// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    public class RdpCertificateReaderTests
    {
        [TestMethod]
        [DataRow("Remote Desktop")]
        [DataRow("My")]
        public void SelectsOnlyConfiguredCertificateAndReturnsUsableIndependentCopy(string targetStore)
        {
            // Arrange
            using RSA key = RSA.Create(2048);
            using var configured = CreateCertificate(key, "configured");
            using var unrelated = CreateCertificate(key, "unrelated");
            var enumerated = new List<X509Certificate2>();
            var stores = new List<string>();
            var logger = Substitute.For<ILoggerAdapter>();

            // Act
            using var selected = RdpCertificateReader.FindCertificate(" " + configured.Thumbprint.ToLowerInvariant() + "\t",
                name =>
                {
                    stores.Add(name);
                    var certificate = new X509Certificate2(name == targetStore ? configured : unrelated);
                    enumerated.Add(certificate);
                    return new X509Certificate2Collection(certificate);
                }, logger);

            // Assert
            Assert.AreEqual(configured.Thumbprint, selected.Thumbprint);
            Assert.AreNotSame(configured, selected);
            Assert.HasCount(targetStore == "My" ? 2 : 1, stores);
            foreach (var certificate in enumerated)
            {
                Assert.AreEqual(IntPtr.Zero, certificate.Handle);
            }
            using RSA privateKey = selected.GetRSAPrivateKey();
            Assert.IsNotEmpty(privateKey.SignData(new byte[] { 1 }, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("not-a-thumbprint")]
        public void InvalidListenerThumbprintDoesNotSearchStores(string thumbprint)
        {
            // Arrange
            bool read = false;

            // Act
            var error = Assert.ThrowsExactly<MsalClientException>(() =>
                RdpCertificateReader.FindCertificate(thumbprint, _ =>
                {
                    read = true;
                    return new X509Certificate2Collection();
                }, Substitute.For<ILoggerAdapter>()));

            // Assert
            Assert.AreEqual(string.IsNullOrEmpty(thumbprint)
                ? RdpCertificateReader.CertificateNotFound : RdpCertificateReader.CertificateUnavailable, error.ErrorCode);
            Assert.IsFalse(read);
        }

        [TestMethod]
        public void MissingConfiguredCertificateDoesNotSelectAnUnrelatedCertificate()
        {
            // Arrange
            using RSA key = RSA.Create(2048);
            using var configured = CreateCertificate(key, "configured");
            using var unrelated = CreateCertificate(key, "unrelated");

            // Act
            var error = Assert.ThrowsExactly<MsalClientException>(() =>
                RdpCertificateReader.FindCertificate(configured.Thumbprint,
                    _ => new X509Certificate2Collection(new X509Certificate2(unrelated)),
                    Substitute.For<ILoggerAdapter>()));

            // Assert
            Assert.AreEqual(RdpCertificateReader.CertificateNotFound, error.ErrorCode);
        }

        [TestMethod]
        public void MatchingCertificateWithoutPrivateKeyFailsExplicitly()
        {
            // Arrange
            using RSA key = RSA.Create(2048);
            using var configured = CreateCertificate(key, "configured");

            // Act
            var error = Assert.ThrowsExactly<MsalClientException>(() =>
                RdpCertificateReader.FindCertificate(configured.Thumbprint,
                    _ => new X509Certificate2Collection(new X509Certificate2(configured.RawData)),
                    Substitute.For<ILoggerAdapter>()));

            // Assert
            Assert.AreEqual("managed_identity_binding_certificate_invalid", error.ErrorCode);
        }

        private static X509Certificate2 CreateCertificate(RSA key, string name)
        {
            return new CertificateRequest("CN=" + name, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
                .CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(1));
        }
    }
}
