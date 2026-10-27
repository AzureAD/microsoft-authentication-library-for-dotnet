// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Reflection;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace Microsoft.Identity.Test.Compatibility
{
    [TestClass]
    public class CertificateCompatibilityTests
    {
        [TestMethod]
        public void OlderRuntimeSelectsNonExportingCompatibilityAsset()
        {
            // Arrange
            Assembly assembly = typeof(PublicClientApplication).Assembly;

            // Act
            string framework = assembly.GetCustomAttribute<TargetFrameworkAttribute>().FrameworkName;

            // Assert
            Assert.AreEqual(".NETStandard,Version=v2.1", framework);
        }

        [TestMethod]
        [SupportedOSPlatform("windows")]
        public void AttachedNonExportableKeySignsAfterOriginalKeyIsDisposed()
        {
            if (!OperatingSystem.IsWindows())
            {
                Assert.Inconclusive("Non-exportable CNG keys require Windows.");
            }

            // Arrange
            string keyName = $"msal-compat-{Guid.NewGuid():N}";
            CngKey key = CngKey.Create(CngAlgorithm.Rsa, keyName, new CngKeyCreationParameters
            {
                Provider = CngProvider.MicrosoftSoftwareKeyStorageProvider,
                ExportPolicy = CngExportPolicies.None,
                KeyUsage = CngKeyUsages.Signing
            });
            try
            {
                X509Certificate2 certificate;
                using (key)
                using (RSA rsa = new RSACng(key))
                {
                    Assert.ThrowsExactly<CryptographicException>(() => rsa.ExportParameters(true));
                    var request = new CertificateRequest(
                        "CN=MSAL compatibility test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                    using X509Certificate2 publicCertificate = request.CreateSelfSigned(
                        DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(5));

                    // Act
                    certificate = CommonCryptographyManager.AttachPrivateKeyToCert(
                        Convert.ToBase64String(publicCertificate.RawData), rsa);
                }

                // Assert
                using (certificate)
                using (RSA privateKey = certificate.GetRSAPrivateKey())
                using (RSA publicKey = certificate.GetRSAPublicKey())
                {
                    byte[] message = Encoding.UTF8.GetBytes("certificate binding");
                    byte[] signature = privateKey.SignData(message, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                    Assert.IsTrue(publicKey.VerifyData(
                        message, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
                    Assert.ThrowsExactly<CryptographicException>(() => privateKey.ExportParameters(true));
                }
            }
            finally
            {
                using CngKey persistedKey = CngKey.Open(keyName, CngProvider.MicrosoftSoftwareKeyStorageProvider);
                persistedKey.Delete();
            }
        }
    }
}
