// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography;
using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit
{
#if !WINDOWS_APP // not available on UWP
    [TestClass]
    [DeploymentItem(@"Resources\testCert.crtfile")]
    public class CryptographyTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
        }

        [TestMethod]
        [TestCategory("CryptographyTests")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Internal.Analyzers", "IA5352:DoNotMisuseCryptographicApi", Justification = "Suppressing RoslynAnalyzers: Rule: IA5352 - Do Not Misuse Cryptographic APIs in test only code")]
        public void SignWithCertificate()
        {
            var serviceBundle = TestCommon.CreateDefaultServiceBundle();
            // Tests the cryptography libraries used by MSAL to sign with certificates
            var cert = new X509Certificate2(
                ResourceHelper.GetTestResourceRelativePath("testCert.crtfile"), TestConstants.TestCertPassword);
            var crypto = serviceBundle.PlatformProxy.CryptographyManager;
            byte[] result = crypto.SignWithCertificate("TEST", cert);
            string value = Base64UrlHelpers.Encode(result);
            Assert.IsNotNull(value);
            Assert.AreEqual("MrknKHbOAVu2iuLHMFSk2SK773H1ysxaAjAPcTXYSfH4P2fUfvzP6aIb9MkBknjoE_aBYtTnQ7jOAvyQETvogdeSH7pRDPhCk2aX_8VIQw0bjo_zBZj5yJYVWQDLIu8XvbuzIGEvVaXKz4jJ1nYM6toun4tM74rEHvwa0ferafmqHWOd5puPhlKH1VVK2RPuNOoKNLWBprVBaAQVJVFOdRcd3iR0INBHykxtOsG0pgo0Q2uQBlKP7KQb7Ox8i_sw-M21BuUzdIdGs_oeUYh0B8s-eIGf34JmHRWMwWCnRWzZgY9YuIjRoaWNqlWYb8ASjKOxzvk99x8eFEYKOjgAcA", value);
        }

        [TestMethod]
        [TestCategory("CryptographyTests")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Internal.Analyzers", "IA5352:DoNotMisuseCryptographicApi", Justification = "Suppressing RoslynAnalyzers: Rule: IA5352 - Do Not Misuse Cryptographic APIs in test only code")]
        public void SignWithNonRsaCertificate()
        {
            var serviceBundle = TestCommon.CreateDefaultServiceBundle();

            X509Certificate2 cert = GenerateSelfSignedCertificate();
            var crypto = serviceBundle.PlatformProxy.CryptographyManager;

            MsalClientException ex = AssertException.Throws<MsalClientException>(() =>
            {
                crypto.SignWithCertificate("TEST", cert);
            });

            Assert.AreEqual(ex.ErrorCode, MsalError.CertificateNotRsa);
            Assert.AreEqual(ex.Message, MsalErrorMessage.CertMustBeRsa);
        }

        public static X509Certificate2 GenerateSelfSignedCertificate()
        {
            string secp256r1Oid = "1.2.840.10045.3.1.7";  //oid for prime256v1(7)  other identifier: secp256r1

            string subjectName = "SelfSignedEdcCert";

            var ecdsa = ECDsa.Create(ECCurve.CreateFromValue(secp256r1Oid));

            var certRequest = new CertificateRequest($"CN={subjectName}", ecdsa, HashAlgorithmName.SHA256);

            X509Certificate2 generatedCert = certRequest.CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddYears(10)); // generate the cert and sign!

            X509Certificate2 pfxGeneratedCert = new X509Certificate2(generatedCert.Export(X509ContentType.Pfx)); //has to be turned into pfx or Windows at least throws a security credentials not found during sslStream.connectAsClient or HttpClient request...

            return pfxGeneratedCert;
        }
    }
#endif
}
