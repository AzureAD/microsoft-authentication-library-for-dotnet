// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Test.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Client.Internal;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;
using static Microsoft.Identity.Client.Internal.ClientCredentialWrapper;

namespace Microsoft.Identity.Test.Unit.RequestsTests
{
    [TestClass]
    [DeploymentItem(@"Resources\testCert.crtfile")]
    public class RequestValidationHelperTests
    {
        public const uint JwtToAadLifetimeInSeconds = 60 * 10; // Ten minutes

        private IServiceBundle _serviceBundle;
        readonly string _audience1 = "Audience1";
        readonly string _audience2 = "Audience2";

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
            _serviceBundle = TestCommon.CreateDefaultServiceBundle();
        }

        [TestMethod]
        public void CCACreatedWithoutAuthenticationType_Throws()
        {
            ApplicationConfiguration config = new ApplicationConfiguration();

           Assert.ThrowsException<MsalClientException>(
               () => new ClientCredentialWrapper(config));
        }

        [TestMethod]
        public void CCACreatedWithAuthenticationType_ClientSecret_DoesNotThrow()
        {
            // Arrange
            ApplicationConfiguration config = new ApplicationConfiguration
            {
                ClientSecret = TestConstants.ClientSecret,
                ConfidentialClientCredentialCount = 1

            };

            // Act
            ClientCredentialWrapper clientCredentialWrapper = new ClientCredentialWrapper(config);

            // Assert
            // no exception is thrown
            Assert.AreEqual(
                ConfidentialClientAuthenticationType.ClientSecret, 
                clientCredentialWrapper.AuthenticationType);
        }

        [TestMethod]
        public void CCACreatedWithAuthenticationType_ClientCertificate_DoesNotThrow()
        {
            // Arrange
            var cert = new X509Certificate2(
                ResourceHelper.GetTestResourceRelativePath("testCert.crtfile"), "passw0rd!");
            ApplicationConfiguration config = new ApplicationConfiguration
            {
                ClientCredentialCertificate = cert,
                ConfidentialClientCredentialCount = 1

            };

            // Act
            ClientCredentialWrapper clientCredentialWrapper = new ClientCredentialWrapper(config);

            // Assert
            // no exception is thrown
            Assert.AreEqual(
                ConfidentialClientAuthenticationType.ClientCertificate,
                clientCredentialWrapper.AuthenticationType);
        }

        [TestMethod]
        public void CCACreatedWithAuthenticationType_SignedClientAssertion_DoesNotThrow()
        {
            // Arrange
            ApplicationConfiguration config = new ApplicationConfiguration
            {
                SignedClientAssertion = "signed",
                ConfidentialClientCredentialCount = 1

            };

            // Act
            ClientCredentialWrapper clientCredentialWrapper = new ClientCredentialWrapper(config);

            // Assert
            // no exception is thrown
            Assert.AreEqual(
                ConfidentialClientAuthenticationType.SignedClientAssertion,
                clientCredentialWrapper.AuthenticationType);
        }

        [TestMethod]
        public void CCACreatedWithAuthenticationType_ClientCertificateWithNoClaims_DoesNotThrow()
        {
            // Arrange
            var cert = new X509Certificate2(
                ResourceHelper.GetTestResourceRelativePath("testCert.crtfile"), "passw0rd!");
            var claims = new Dictionary<string, string>();
            ApplicationConfiguration config = new ApplicationConfiguration
            {                
                ClientCredentialCertificate = cert,
                ClaimsToSign = claims,
                ConfidentialClientCredentialCount = 1

            };

            // Act
            ClientCredentialWrapper clientCredentialWrapper = new ClientCredentialWrapper(config);

            // Assert
            // no exception is thrown
            Assert.AreEqual(
                ConfidentialClientAuthenticationType.ClientCertificate,
                clientCredentialWrapper.AuthenticationType);
        }

        [TestMethod]
        public void CCACreatedWithAuthenticationType_ClientCertificateWithClaims_DoesNotThrow()
        {
            // Arrange
            var cert = new X509Certificate2(
                ResourceHelper.GetTestResourceRelativePath("testCert.crtfile"), "passw0rd!");
            var claims = new Dictionary<string, string>();
            claims.Add("cats", "are cool");

            ApplicationConfiguration config = new ApplicationConfiguration
            {
                ClientCredentialCertificate = cert,
                ClaimsToSign = claims,
                ConfidentialClientCredentialCount = 1
            };

            // Act
            ClientCredentialWrapper clientCredentialWrapper = new ClientCredentialWrapper(config);

            // Assert
            // no exception is thrown
            Assert.AreEqual(
                ConfidentialClientAuthenticationType.ClientCertificateWithClaims,
                clientCredentialWrapper.AuthenticationType);
        }

        internal static long ConvertToTimeT(DateTime time)
        {
            var startTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            TimeSpan diff = time - startTime;
            return (long)diff.TotalSeconds;
        }
    }
}
