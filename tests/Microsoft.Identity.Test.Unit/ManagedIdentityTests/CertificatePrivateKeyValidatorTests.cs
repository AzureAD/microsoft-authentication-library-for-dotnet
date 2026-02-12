// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.ManagedIdentity.V2;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    public class CertificatePrivateKeyValidatorTests
    {
        private static X509Certificate2 CreateSelfSignedCert(TimeSpan lifetime, string subjectCn = "CN=ValidatorTest")
        {
            using var rsa = RSA.Create(2048);
            var req = new System.Security.Cryptography.X509Certificates.CertificateRequest(
                new X500DistinguishedName(subjectCn),
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            var notBefore = DateTimeOffset.UtcNow.AddMinutes(-2);
            var notAfter = notBefore.Add(lifetime);
            return req.CreateSelfSigned(notBefore, notAfter);
        }

        [TestMethod]
        public void ValidCert_WithPrivateKey_ReturnsTrue()
        {
            // Arrange
            using var cert = CreateSelfSignedCert(TimeSpan.FromDays(2));

            // Act
            bool result = CertificatePrivateKeyValidator.IsPrivateKeyAccessible(cert);

            // Assert
            Assert.IsTrue(result, "Certificate with valid private key should pass validation.");
        }

        [TestMethod]
        public void NullCert_ReturnsFalse()
        {
            // Arrange / Act
            bool result = CertificatePrivateKeyValidator.IsPrivateKeyAccessible(null);

            // Assert
            Assert.IsFalse(result, "Null certificate should fail validation.");
        }

        [TestMethod]
        public void PublicOnlyCert_ReturnsFalse()
        {
            // Arrange – export only the public portion (drops private key)
            using var full = CreateSelfSignedCert(TimeSpan.FromDays(2));
            byte[] publicOnly = full.Export(X509ContentType.Cert);
            using var pubCert = new X509Certificate2(publicOnly);

            // Act
            bool result = CertificatePrivateKeyValidator.IsPrivateKeyAccessible(pubCert);

            // Assert
            Assert.IsFalse(result, "Certificate without private key should fail validation.");
        }
    }
}
