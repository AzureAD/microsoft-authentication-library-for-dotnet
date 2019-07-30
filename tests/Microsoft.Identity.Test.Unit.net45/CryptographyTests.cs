// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
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
        public void SignWithCertificate()
        {
            var serviceBundle = TestCommon.CreateDefaultServiceBundle();
            // Tests the cryptography libraries used by MSAL to sign with certificates
            var cert = new X509Certificate2(
                ResourceHelper.GetTestResourceRelativePath("testCert.crtfile"), "passw0rd!");
            var crypto = serviceBundle.PlatformProxy.CryptographyManager;
            byte[] result = crypto.SignWithCertificate("TEST", cert);
            string value = Base64UrlHelpers.Encode(result);
            Assert.IsNotNull(value);
            Assert.AreEqual("MrknKHbOAVu2iuLHMFSk2SK773H1ysxaAjAPcTXYSfH4P2fUfvzP6aIb9MkBknjoE_aBYtTnQ7jOAvyQETvogdeSH7pRDPhCk2aX_8VIQw0bjo_zBZj5yJYVWQDLIu8XvbuzIGEvVaXKz4jJ1nYM6toun4tM74rEHvwa0ferafmqHWOd5puPhlKH1VVK2RPuNOoKNLWBprVBaAQVJVFOdRcd3iR0INBHykxtOsG0pgo0Q2uQBlKP7KQb7Ox8i_sw-M21BuUzdIdGs_oeUYh0B8s-eIGf34JmHRWMwWCnRWzZgY9YuIjRoaWNqlWYb8ASjKOxzvk99x8eFEYKOjgAcA", value);
        }
    }
#endif
}
