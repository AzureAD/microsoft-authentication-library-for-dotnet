// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Reflection;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.UtilTests
{
    [TestClass]
    public class CertificateAttachmentTests
    {
#if MSAL_TEST_NETSTANDARD20
        [TestMethod]
        public void AttachmentRegression_LoadsNetStandard20Asset()
        {
            // Arrange
            var assembly = typeof(ManagedIdentityApplicationBuilder).Assembly;

            // Act
            string framework = assembly.GetCustomAttribute<TargetFrameworkAttribute>().FrameworkName;

            // Assert
            Assert.AreEqual(".NETStandard,Version=v2.0", framework);
        }
#endif

#if NET_CORE || MSAL_TEST_NETSTANDARD20
        [RunOnWindows]
        public void NonExportableCngKey_SignsAfterOriginalObjectsAreDisposed()
        {
            // Arrange
            string keyName = "msal-attachment-" + Guid.NewGuid().ToString("N");
            CngKey key = CreateKey(keyName);
            try
            {
                X509Certificate2 attached;
                using (key)
                using (var rsa = new RSACng(key))
                using (var issued = IssueCertificate(rsa))
                {
                    Assert.Throws<CryptographicException>(() => rsa.ExportParameters(true));

                    // Act
                    attached = CommonCryptographyManager.AttachPrivateKeyToCert(
                        Convert.ToBase64String(issued.RawData), rsa);
                }

                // Assert
                using (attached)
                using (RSA signing = attached.GetRSAPrivateKey())
                using (RSA verification = attached.GetRSAPublicKey())
                {
                    Assert.IsNotNull(signing);
                    byte[] data = { 6, 2, 4, 5 };
                    byte[] signature = signing.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                    Assert.IsTrue(verification.VerifyData(
                        data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
                    Assert.AreEqual(CngExportPolicies.None, ((RSACng)signing).Key.ExportPolicy);
                    Assert.Throws<CryptographicException>(() => signing.ExportParameters(true));
                }

                using (CngKey reopened = CngKey.Open(keyName, CngProvider.MicrosoftSoftwareKeyStorageProvider))
                using (var rsa = new RSACng(reopened))
                {
                    Assert.IsNotNull(rsa.SignData(new byte[] { 1 }, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1),
                        "Disposing the certificate must not delete the application's persisted key.");
                }
            }
            finally
            {
                key.Dispose();
                using (CngKey cleanup = CngKey.Open(keyName, CngProvider.MicrosoftSoftwareKeyStorageProvider))
                {
                    cleanup.Delete();
                }
            }
        }

        [RunOnWindows]
        public void MismatchedCngKey_PreservesArgumentException()
        {
            // Arrange
            string keyName = "msal-mismatch-" + Guid.NewGuid().ToString("N");
            using (CngKey key = CreateKey(keyName))
            {
                try
                {
                    using (var rsa = new RSACng(key))
                    using (RSA other = RSA.Create())
                    using (var certificate = IssueCertificate(other))
                    {
                        // Act
                        var exception = Assert.ThrowsExactly<MsalServiceException>(() =>
                            CommonCryptographyManager.AttachPrivateKeyToCert(
                                Convert.ToBase64String(certificate.RawData), rsa));

                        // Assert
                        Assert.AreEqual(MsalError.InvalidCertificate, exception.ErrorCode);
                        Assert.IsInstanceOfType<ArgumentException>(exception.InnerException);
                    }
                }
                finally
                {
                    key.Delete();
                }
            }
        }
#endif

#if MSAL_TEST_NETSTANDARD20
        [RunOnWindows]
        public void NonExportableEphemeralCngKey_IsExplicitlyUnsupported()
        {
            // Arrange
            using (CngKey key = CreateKey(null))
            using (var rsa = new RSACng(key))
            using (var certificate = IssueCertificate(rsa))
            {
                Assert.IsTrue(key.IsEphemeral);

                // Act
                var exception = Assert.ThrowsExactly<MsalServiceException>(() =>
                    CommonCryptographyManager.AttachPrivateKeyToCert(
                        Convert.ToBase64String(certificate.RawData), rsa));

                // Assert
                Assert.AreEqual(MsalError.InvalidCertificate, exception.ErrorCode);
                Assert.IsInstanceOfType<NotSupportedException>(exception.InnerException);
                Assert.AreEqual(MsalErrorMessage.CertificatePrivateKeyAttachmentFailed, exception.Message);
            }
        }

        [RunOnWindows]
        public void DeletedNamedCngKey_PreservesCryptographicFailure()
        {
            // Arrange
            string keyName = "msal-deleted-" + Guid.NewGuid().ToString("N");
            using (CngKey key = CreateKey(keyName))
            using (var rsa = new RSACng(key))
            using (var certificate = IssueCertificate(rsa))
            {
                key.Delete();
                var nativeFailure = Assert.ThrowsExactly<CryptographicException>(() =>
                    rsa.ExportParameters(false));

                // Act
                var exception = Assert.ThrowsExactly<MsalServiceException>(() =>
                    CommonCryptographyManager.AttachPrivateKeyToCert(
                        Convert.ToBase64String(certificate.RawData), rsa));

                // Assert
                Assert.AreEqual(MsalError.InvalidCertificate, exception.ErrorCode);
                Assert.IsInstanceOfType<CryptographicException>(exception.InnerException);
                Assert.AreEqual(nativeFailure.HResult, exception.InnerException.HResult);
                Assert.AreEqual(MsalErrorMessage.CertificatePrivateKeyAttachmentFailed, exception.Message);
            }
        }
#endif

        [TestMethod]
        public void InvalidCertificate_PreservesParsingDiagnostic()
        {
            // Arrange
            using (RSA rsa = RSA.Create())
            {
                // Act
                var exception = Assert.ThrowsExactly<MsalServiceException>(() =>
                    CommonCryptographyManager.AttachPrivateKeyToCert("not base64", rsa));

                // Assert
                Assert.AreEqual(MsalError.InvalidCertificate, exception.ErrorCode);
                Assert.AreEqual(MsalErrorMessage.InvalidCertificate, exception.Message);
                Assert.IsInstanceOfType<FormatException>(exception.InnerException);
            }
        }

        private static CngKey CreateKey(string name)
        {
            return CngKey.Create(CngAlgorithm.Rsa, name, new CngKeyCreationParameters
            {
                Provider = CngProvider.MicrosoftSoftwareKeyStorageProvider,
                ExportPolicy = CngExportPolicies.None,
                KeyUsage = CngKeyUsages.Signing
            });
        }

        private static X509Certificate2 IssueCertificate(RSA rsa)
        {
            var request = new CertificateRequest(
                "CN=MSAL attachment regression", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return request.Create(request.SubjectName,
                X509SignatureGenerator.CreateForRSA(rsa, RSASignaturePadding.Pkcs1),
                DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1), new byte[] { 1 });
        }
    }
}
