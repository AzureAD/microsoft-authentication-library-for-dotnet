// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class CertificateConfigurationTests : TestBase
    {
        private static X509Certificate2 s_testCertificate;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            s_testCertificate = CertHelper.GetOrCreateTestCert();
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            s_testCertificate?.Dispose();
        }

        [TestMethod]
        public void CertificateConfiguration_NullCertificate_ThrowsArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new CertificateConfiguration(null));
        }

        [TestMethod]
        public void CertificateConfiguration_BasicConfiguration()
        {
            var config = new CertificateConfiguration(s_testCertificate);

            Assert.AreEqual(s_testCertificate, config.Certificate);
            Assert.IsFalse(config.SendX5C);
            Assert.IsNull(config.ClaimsToSign);
            Assert.IsTrue(config.MergeWithDefaultClaims);
            Assert.IsFalse(config.EnableMtlsProofOfPossession);
        }

        [TestMethod]
        public void CertificateConfiguration_WithAllOptions()
        {
            var claims = new Dictionary<string, string> { { "client_ip", "192.168.1.1" } };
            var config = new CertificateConfiguration(s_testCertificate)
            {
                SendX5C = true,
                ClaimsToSign = claims,
                MergeWithDefaultClaims = false,
                EnableMtlsProofOfPossession = true,
                UseBearerTokenWithMtls = true,
                Claims = "{\"access_token\":{\"acrs\":{\"essential\":true}}}"
            };

            Assert.AreEqual(s_testCertificate, config.Certificate);
            Assert.IsTrue(config.SendX5C);
            Assert.AreEqual(claims, config.ClaimsToSign);
            Assert.IsFalse(config.MergeWithDefaultClaims);
            Assert.IsTrue(config.EnableMtlsProofOfPossession);
            Assert.IsTrue(config.UseBearerTokenWithMtls);
            Assert.IsNotNull(config.Claims);
        }

        [TestMethod]
        public void WithCertificate_BasicCertificate()
        {
            var certConfig = new CertificateConfiguration(s_testCertificate);

            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithCertificate(certConfig)
                .BuildConcrete();

            Assert.IsNotNull(app);
            Assert.IsNotNull(app.AppConfig.ClientCredential);
        }

        [TestMethod]
        public void WithCertificate_WithX5C()
        {
            var certConfig = new CertificateConfiguration(s_testCertificate)
            {
                SendX5C = true
            };

            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithCertificate(certConfig)
                .BuildConcrete();

            Assert.IsTrue(app.AppConfig.SendX5C);
        }

        [TestMethod]
        public void WithCertificate_WithClaims()
        {
            var claims = new Dictionary<string, string> { { "client_ip", "192.168.1.1" } };
            var certConfig = new CertificateConfiguration(s_testCertificate)
            {
                ClaimsToSign = claims,
                MergeWithDefaultClaims = false
            };

            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithCertificate(certConfig)
                .BuildConcrete();

            Assert.IsNotNull(app.AppConfig.ClientCredential);
        }

        [TestMethod]
        public void WithCertificate_WithMtlsPoP()
        {
            var certConfig = new CertificateConfiguration(s_testCertificate)
            {
                EnableMtlsProofOfPossession = true
            };

            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithCertificate(certConfig)
                .BuildConcrete();

            Assert.IsTrue(app.AppConfig.IsMtlsPopEnabledByCertificateConfiguration);
        }

        [TestMethod]
        public void WithCertificate_AllOptions()
        {
            var claims = new Dictionary<string, string> 
            { 
                { "client_ip", "192.168.1.1" },
                { "custom_claim", "value" }
            };

            var certConfig = new CertificateConfiguration(s_testCertificate)
            {
                SendX5C = true,
                AssociateTokensWithCertificateSerialNumber = true,
                ClaimsToSign = claims,
                MergeWithDefaultClaims = true,
                EnableMtlsProofOfPossession = true,
                UseBearerTokenWithMtls = false,
                Claims = "{\"access_token\":{\"acrs\":{\"essential\":true}}}"
            };

            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithCertificate(certConfig)
                .BuildConcrete();

            Assert.IsTrue(app.AppConfig.SendX5C);
            Assert.AreEqual(s_testCertificate.SerialNumber, app.AppConfig.CertificateIdToAssociateWithToken);
            Assert.IsTrue(app.AppConfig.IsMtlsPopEnabledByCertificateConfiguration);
            Assert.IsFalse(app.AppConfig.UseBearerTokenWithMtls);
            Assert.IsNotNull(app.AppConfig.CertificateConfigurationClaims);
        }

        [TestMethod]
        public void WithCertificate_UseBearerTokenWithMtls()
        {
            var certConfig = new CertificateConfiguration(s_testCertificate)
            {
                EnableMtlsProofOfPossession = true,
                UseBearerTokenWithMtls = true
            };

            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithCertificate(certConfig)
                .BuildConcrete();

            Assert.IsTrue(app.AppConfig.IsMtlsPopEnabledByCertificateConfiguration);
            Assert.IsTrue(app.AppConfig.UseBearerTokenWithMtls);
        }

        [TestMethod]
        public void WithCertificate_WithClaimsChallenge()
        {
            var claimsChallengeValue = "{\"access_token\":{\"acrs\":{\"essential\":true,\"value\":\"urn:microsoft:req1\"}}}";
            
            var certConfig = new CertificateConfiguration(s_testCertificate)
            {
                Claims = claimsChallengeValue
            };

            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithCertificate(certConfig)
                .BuildConcrete();

            Assert.AreEqual(claimsChallengeValue, app.AppConfig.CertificateConfigurationClaims);
        }

        [TestMethod]
        public void WithCertificate_NullConfiguration_ThrowsArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
                ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithCertificate(null));
        }

        [TestMethod]
        public void WithCertificate_CertificateWithoutPrivateKey_ThrowsMsalClientException()
        {
            // Create a certificate without private key (just the public key)
            var certWithoutPrivateKey = new X509Certificate2(s_testCertificate.Export(X509ContentType.Cert));

            var certConfig = new CertificateConfiguration(certWithoutPrivateKey);

            var ex = Assert.ThrowsException<MsalClientException>(() =>
                ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithCertificate(certConfig));

            Assert.AreEqual(MsalError.CertWithoutPrivateKey, ex.ErrorCode);
        }

        [TestMethod]
        public async Task AcquireTokenForClient_WithCertificate_AutoEnablesMtlsPoP()
        {
            const string region = "eastus";

            using (var envContext = new EnvVariableContext())
            {
                Environment.SetEnvironmentVariable("REGION_NAME", region);

                using (var httpManager = new MockHttpManager())
                {
                    httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                        tokenType: "mtls_pop");

                    var certConfig = new CertificateConfiguration(s_testCertificate)
                    {
                        EnableMtlsProofOfPossession = true
                    };

                    var app = ConfidentialClientApplicationBuilder
                        .Create(TestConstants.ClientId)
                        .WithAuthority($"https://login.microsoftonline.com/123456-1234-2345-1234561234")
                        .WithAzureRegion(ConfidentialClientApplication.AttemptRegionDiscovery)
                        .WithCertificate(certConfig)
                        .WithHttpManager(httpManager)
                        .BuildConcrete();

                    // Token acquisition should automatically use mTLS PoP without explicitly calling WithMtlsProofOfPossession
                    AuthenticationResult result = await app.AcquireTokenForClient(TestConstants.s_scope)
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    Assert.AreEqual("header.payload.signature", result.AccessToken);
                    Assert.AreEqual(Constants.MtlsPoPAuthHeaderPrefix, result.TokenType);
                    Assert.IsNotNull(result.BindingCertificate);
                    Assert.AreEqual(s_testCertificate.Thumbprint, result.BindingCertificate.Thumbprint);
                }
            }
        }
    }
}
