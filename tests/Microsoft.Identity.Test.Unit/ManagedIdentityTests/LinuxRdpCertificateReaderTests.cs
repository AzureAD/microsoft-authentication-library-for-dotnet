// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if NET8_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    public class LinuxRdpCertificateReaderTests
    {
        [TestMethod]
        [DataRow("[Globals]")]
        [DataRow("[Globals]\ncertificate=\nkey_file=\nsecurity_layer=negotiate")]
        [DataRow("# comment\n[gLoBaLs]\nsecurity_layer=TLS ; comment\n[other]\ncertificate=/wrong.pem")]
        public void UsesDocumentedDefaultsOnlyWithinGlobals(string configuration)
        {
            // Act
            var paths = LinuxRdpCertificateReader.GetCertificatePaths(configuration);

            // Assert
            Assert.AreEqual("/etc/xrdp/cert.pem", paths.Certificate);
            Assert.AreEqual("/etc/xrdp/key.pem", paths.Key);
        }

        [TestMethod]
        public void UsesConfiguredAbsolutePathsAndIgnoresOtherSections()
        {
            // Arrange
            const string configuration = "[other]\ncertificate=/wrong.pem\n[Globals]\n" +
                "Certificate = /var/lib/xrdp/host.pem # certificate\nKEY_FILE=/var/lib/xrdp/host.key; key\n" +
                "[Logging]\nkey_file=/wrong.key";

            // Act
            var paths = LinuxRdpCertificateReader.GetCertificatePaths(configuration);

            // Assert
            Assert.AreEqual("/var/lib/xrdp/host.pem", paths.Certificate);
            Assert.AreEqual("/var/lib/xrdp/host.key", paths.Key);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("[other]\ncertificate=/etc/xrdp/cert.pem")]
        [DataRow("[Globals]\nsecurity_layer=rdp")]
        [DataRow("[Globals]\nsecurity_layer=unknown")]
        [DataRow("[Globals]\ncertificate=relative.pem")]
        [DataRow("[Globals]\nkey_file=$XRDP_KEY")]
        [DataRow("[Globals]\ncertificate=/one.pem\nCERTIFICATE=/two.pem")]
        public void InvalidOrAmbiguousConfigurationDoesNotReadCertificateFiles(string configuration)
        {
            // Arrange
            int reads = 0;

            // Act
            var error = Assert.ThrowsExactly<MsalClientException>(() =>
                LinuxRdpCertificateReader.Read(path =>
                {
                    reads++;
                    Assert.AreEqual(LinuxRdpCertificateReader.ConfigurationPath, path);
                    return configuration;
                }, Substitute.For<ILoggerAdapter>(), CancellationToken.None));

            // Assert
            Assert.AreEqual(RdpCertificateReader.CertificateUnavailable, error.ErrorCode);
            Assert.AreEqual(1, reads);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void LoadsOnlyConfiguredMatchingPemPairAndRetainsUsablePrivateKey(bool pkcs1)
        {
            // Arrange
            using RSA key = RSA.Create(2048);
            using var certificate = CreateCertificate(key);
            var files = new Dictionary<string, string>
            {
                [LinuxRdpCertificateReader.ConfigurationPath] =
                    "[Globals]\ncertificate=/configured/cert.pem\nkey_file=/configured/key.pem",
                ["/configured/cert.pem"] = certificate.ExportCertificatePem(),
                ["/configured/key.pem"] = pkcs1 ? key.ExportRSAPrivateKeyPem() : key.ExportPkcs8PrivateKeyPem()
            };
            var reads = new List<string>();

            // Act
            using var selected = LinuxRdpCertificateReader.Read(path =>
            {
                reads.Add(path);
                return files[path];
            }, Substitute.For<ILoggerAdapter>(), CancellationToken.None);

            // Assert
            Assert.HasCount(3, reads);
            Assert.AreEqual(certificate.Thumbprint, selected.Thumbprint);
            Assert.AreNotSame(certificate, selected);
            using RSA selectedKey = selected.GetRSAPrivateKey();
            byte[] data = { 1, 2, 3 };
            byte[] signature = selectedKey.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            Assert.IsTrue(key.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void MissingConfigurationIsDistinctFromAccessFailure(bool accessDenied)
        {
            // Arrange
            Exception failure = accessDenied ? new UnauthorizedAccessException() : new FileNotFoundException();
            int reads = 0;

            // Act
            var error = Assert.ThrowsExactly<MsalClientException>(() =>
                LinuxRdpCertificateReader.Read(_ =>
                {
                    reads++;
                    throw failure;
                }, Substitute.For<ILoggerAdapter>(), CancellationToken.None));

            // Assert
            Assert.AreEqual(accessDenied ? RdpCertificateReader.CertificateUnavailable : RdpCertificateReader.CertificateNotFound,
                error.ErrorCode);
            Assert.AreSame(failure, error.InnerException);
            Assert.AreEqual(1, reads);
        }

        [TestMethod]
        public void MissingConfiguredCertificateIsReportedAsAbsentBeforeReadingKey()
        {
            // Arrange
            int reads = 0;

            // Act
            var error = Assert.ThrowsExactly<MsalClientException>(() =>
                LinuxRdpCertificateReader.Read(path =>
                {
                    reads++;
                    if (path == LinuxRdpCertificateReader.ConfigurationPath)
                    {
                        return "[Globals]";
                    }
                    Assert.AreEqual("/etc/xrdp/cert.pem", path);
                    throw new FileNotFoundException();
                }, Substitute.For<ILoggerAdapter>(), CancellationToken.None));

            // Assert
            Assert.AreEqual(RdpCertificateReader.CertificateNotFound, error.ErrorCode);
            Assert.AreEqual(2, reads);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void MissingOrMismatchedKeyFailsWithoutSelectingAnotherKey(bool missing)
        {
            // Arrange
            using RSA certificateKey = RSA.Create(2048);
            using RSA wrongKey = RSA.Create(2048);
            using var certificate = CreateCertificate(certificateKey);
            int reads = 0;

            // Act
            var error = Assert.ThrowsExactly<MsalClientException>(() =>
                LinuxRdpCertificateReader.Read(path =>
                {
                    reads++;
                    if (path == LinuxRdpCertificateReader.ConfigurationPath)
                    {
                        return "[Globals]";
                    }
                    if (path == "/etc/xrdp/cert.pem")
                    {
                        return certificate.ExportCertificatePem();
                    }
                    Assert.AreEqual("/etc/xrdp/key.pem", path);
                    if (missing)
                    {
                        throw new FileNotFoundException();
                    }
                    return wrongKey.ExportPkcs8PrivateKeyPem();
                }, Substitute.For<ILoggerAdapter>(), CancellationToken.None));

            // Assert
            Assert.AreEqual(RdpCertificateReader.CertificateUnavailable, error.ErrorCode);
            Assert.AreEqual(3, reads);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void CancellationStopsBeforeCertificateOrKeyReads(bool cancelDuringConfiguration)
        {
            // Arrange
            using var cancellation = new CancellationTokenSource();
            if (!cancelDuringConfiguration)
            {
                cancellation.Cancel();
            }
            int reads = 0;

            // Act / Assert
            Assert.ThrowsExactly<OperationCanceledException>(() =>
                LinuxRdpCertificateReader.Read(_ =>
                {
                    reads++;
                    cancellation.Cancel();
                    return "[Globals]";
                }, Substitute.For<ILoggerAdapter>(), cancellation.Token));
            Assert.AreEqual(cancelDuringConfiguration ? 1 : 0, reads);
        }

        private static X509Certificate2 CreateCertificate(RSA key)
        {
            return new CertificateRequest("CN=Synthetic-xrdp-test", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
                .CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(1));
        }
    }
}
#endif
