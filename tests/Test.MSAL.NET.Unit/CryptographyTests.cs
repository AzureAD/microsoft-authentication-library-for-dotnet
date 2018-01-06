//----------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.MSAL.NET.Unit
{
    [TestClass]
    [DeploymentItem(@"Resources\testCert.crtfile")]
    public class CryptographyTests
    {
        [TestMethod]
        [TestCategory("CryptographyTests")]
        public void SignWithCertificate()
        {
            //Tests the cryptography libraries used by MSAL to sign with certificates
            var cert = new X509Certificate2("testCert.crtfile", "passw0rd!");
            CryptographyHelper helper = new CryptographyHelper();
            byte[] result = helper.SignWithCertificate("TEST", cert);
            string value = Base64UrlHelpers.Encode(result);
            Assert.IsNotNull(value);
            Assert.AreEqual("MrknKHbOAVu2iuLHMFSk2SK773H1ysxaAjAPcTXYSfH4P2fUfvzP6aIb9MkBknjoE_aBYtTnQ7jOAvyQETvogdeSH7pRDPhCk2aX_8VIQw0bjo_zBZj5yJYVWQDLIu8XvbuzIGEvVaXKz4jJ1nYM6toun4tM74rEHvwa0ferafmqHWOd5puPhlKH1VVK2RPuNOoKNLWBprVBaAQVJVFOdRcd3iR0INBHykxtOsG0pgo0Q2uQBlKP7KQb7Ox8i_sw-M21BuUzdIdGs_oeUYh0B8s-eIGf34JmHRWMwWCnRWzZgY9YuIjRoaWNqlWYb8ASjKOxzvk99x8eFEYKOjgAcA", value);
        }
    }
}
