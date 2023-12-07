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
        public void SignWithNonRsaCertificate_ThrowsException()
        {
            var serviceBundle = TestCommon.CreateDefaultServiceBundle();

            X509Certificate2 cert = CertHelper.GetOrCreateTestCert(KnownTestCertType.ECD);

            var crypto = serviceBundle.PlatformProxy.CryptographyManager;

            MsalClientException ex = AssertException.Throws<MsalClientException>(() =>
            {
                crypto.SignWithCertificate("TEST", cert);
            });

            Assert.AreEqual(ex.ErrorCode, MsalError.CertificateNotRsa);
            Assert.AreEqual(ex.Message, MsalErrorMessage.CertMustBeRsa(cert.PublicKey.Oid.FriendlyName));
        }
    }
#endif
}
