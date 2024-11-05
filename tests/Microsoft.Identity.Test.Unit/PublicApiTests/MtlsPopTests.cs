// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Region;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit
{
    [TestClass]
    public class MtlsPopTests : TestBase
    {
        public const string EastUsRegion = "eastus";
        private static X509Certificate2 s_testCertificate;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            // Create a self-signed test certificate for testing
            s_testCertificate = CertHelper.GetOrCreateTestCert();

            // Ensure the certificate is valid
            if (s_testCertificate == null || string.IsNullOrEmpty(s_testCertificate.Thumbprint))
            {
                throw new InvalidOperationException("Failed to initialize a valid test certificate.");
            }
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            s_testCertificate.Dispose();
        }

        [TestCleanup]
        public override void TestCleanup()
        {
            Environment.SetEnvironmentVariable("REGION_NAME", null);
        }

        [TestMethod]
        public async Task MtlsPopWithoutCertificateAsync()
        {
            IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
                            .Create(TestConstants.ClientId)
                            .WithClientSecret(TestConstants.ClientSecret)
                            .WithExperimentalFeatures()
                            .Build();

            // Set WithMtlsProofOfPossession on the request without a certificate
            MsalClientException ex = await AssertException.TaskThrowsAsync<MsalClientException>(() =>
                app.AcquireTokenForClient(TestConstants.s_scope)
                   .WithMtlsProofOfPossession() // Enables MTLS PoP
                   .ExecuteAsync())
                .ConfigureAwait(false);

            Assert.AreEqual(MsalError.MtlsCertificateNotProvided, ex.ErrorCode);
        }

        [TestMethod]
        public async Task MtlsPopWithoutRegionAsync()
        {
            Environment.SetEnvironmentVariable("REGION_NAME", null); // Ensure no region is set

            IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
                            .Create(TestConstants.ClientId)
                            .WithCertificate(s_testCertificate)
                            .WithExperimentalFeatures()
                            .Build();

            // Set WithMtlsProofOfPossession on the request without specifying a region
            MsalClientException ex = await AssertException.TaskThrowsAsync<MsalClientException>(() =>
                app.AcquireTokenForClient(TestConstants.s_scope)
                   .WithMtlsProofOfPossession() // Enables MTLS PoP
                   .ExecuteAsync())
                .ConfigureAwait(false);

            Assert.AreEqual(MsalError.MtlsPopWithoutRegion, ex.ErrorCode);
        }

        [TestMethod]
        public void Constructor_ValidCertificate()
        {
            var scheme = new MtlsPopAuthenticationOperation(s_testCertificate);

            // Compute the expected KeyId using SHA-256 on the public key
            var expectedKeyId = ComputeExpectedKeyId(s_testCertificate);

            Assert.AreEqual(expectedKeyId, scheme.KeyId);
            Assert.AreEqual(Constants.MtlsPoPTokenType, scheme.AccessTokenType);
        }

        private static string ComputeExpectedKeyId(X509Certificate2 certificate)
        {
            // Get the raw public key bytes
            var publicKey = certificate.GetPublicKey();

            // Compute the SHA-256 hash of the public key
            using (var sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(publicKey);
                return Base64UrlHelpers.Encode(hash);
            }
        }

        [TestMethod]
        public void GetTokenRequestParams_ExpectedValues()
        {
            var scheme = new MtlsPopAuthenticationOperation(s_testCertificate);
            System.Collections.Generic.IReadOnlyDictionary<string, string> parameters = scheme.GetTokenRequestParams();

            Assert.AreEqual(Constants.MtlsPoPTokenType, parameters[OAuth2Parameter.TokenType]);
        }

        [TestMethod]
        public void FormatResult_SetsMtlsCertificate()
        {
            var scheme = new MtlsPopAuthenticationOperation(s_testCertificate);
            var authenticationResult = new AuthenticationResult();

            scheme.FormatResult(authenticationResult);

            Assert.AreEqual(s_testCertificate, authenticationResult.MtlsCertificate);
        }

        [TestMethod]
        public async Task AcquireTokenForClient_WithMtlsProofOfPossession_SuccessAsync()
        {
            const string region = "eastus";
            Environment.SetEnvironmentVariable("REGION_NAME", region);

            using (var httpManager = new MockHttpManager())
            {
                ConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithCertificate(s_testCertificate)
                    .WithAzureRegion(ConfidentialClientApplication.AttemptRegionDiscovery)
                    .WithExperimentalFeatures()
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(tokenType: "mtls_pop");

                AuthenticationResult result = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .WithMtlsProofOfPossession()
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual("header.payload.signature", result.AccessToken);
                Assert.AreEqual(Constants.MtlsPoPAuthHeaderPrefix, result.TokenType);

                // Second token acquisition - should retrieve from cache
                AuthenticationResult secondResult = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .WithMtlsProofOfPossession()
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual("header.payload.signature", secondResult.AccessToken);
                Assert.AreEqual(Constants.MtlsPoPAuthHeaderPrefix, secondResult.TokenType);
                Assert.AreEqual(TokenSource.Cache, secondResult.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public void MtlsPop_RegionDiscoveryWithCustomMetadataThrowsException()
        {
            using (var httpManager = new MockHttpManager())
            {
                var ex = Assert.ThrowsException<MsalClientException>(() => ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAzureRegion(ConfidentialClientApplication.AttemptRegionDiscovery)
                    .WithCertificate(s_testCertificate)
                    .WithInstanceDiscoveryMetadata(File.ReadAllText(ResourceHelper.GetTestResourceRelativePath("CustomInstanceMetadata.json")))
                    .WithHttpManager(httpManager)
                    .Build());

                Assert.AreEqual(MsalError.RegionDiscoveryWithCustomInstanceMetadata, ex.ErrorCode);
                Assert.AreEqual(MsalErrorMessage.RegionDiscoveryWithCustomInstanceMetadata, ex.Message);
            }
        }

        [TestMethod]
        public async Task MtlsPop_KnownRegionAsync()
        {
            const string region = "centralus";

            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddRegionDiscoveryMockHandler(region);
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(tokenType: "mtls_pop");

                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithCertificate(s_testCertificate)
                    .WithAzureRegion(region)
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures()
                    .BuildConcrete();

                AuthenticationResult result = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .WithMtlsProofOfPossession()
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual("header.payload.signature", result.AccessToken);
                Assert.AreEqual(region, result.ApiEvent.RegionUsed);
                Assert.AreEqual(RegionOutcome.UserProvidedValid, result.ApiEvent.RegionOutcome);
            }
        }

        [TestMethod]
        public async Task MtlsPop_RegionalTokenCacheInterchangeabilityAsync()
        {
            const string region = "centralus";

            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddRegionDiscoveryMockHandler(region);
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(tokenType: "mtls_pop");

                IConfidentialClientApplication regionalApp1 = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithCertificate(s_testCertificate)
                    .WithAzureRegion(region)
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures()
                    .BuildConcrete();

                IConfidentialClientApplication regionalApp2 = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithCertificate(s_testCertificate)
                    .WithAzureRegion(region)
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures()
                    .BuildConcrete();

                var memoryTokenCache = new InMemoryTokenCache();
                memoryTokenCache.Bind(regionalApp1.AppTokenCache);
                memoryTokenCache.Bind(regionalApp2.AppTokenCache);

                // Acquire a token with one regional configuration
                var regionalResult1 = await regionalApp1.AcquireTokenForClient(TestConstants.s_scope)
                    .WithMtlsProofOfPossession()
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual(region, regionalResult1.ApiEvent.RegionUsed);
                Assert.AreEqual(TokenSource.IdentityProvider, regionalResult1.AuthenticationResultMetadata.TokenSource);

                // Attempt acquisition with the other regional app, should retrieve from cache
                var regionalResult2 = await regionalApp2.AcquireTokenForClient(TestConstants.s_scope)
                    .WithMtlsProofOfPossession()
                    .ExecuteAsync()
                    .ConfigureAwait(false);
                
                Assert.AreEqual(TokenSource.Cache, regionalResult2.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public async Task MtlsPop_RegionFallbackToGlobalAsync()
        {
            Environment.SetEnvironmentVariable("REGION_NAME", null);  // No region set

            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddRegionDiscoveryMockHandlerNotFound();
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(tokenType: "mtls_pop");

                ConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithCertificate(s_testCertificate)
                    .WithAzureRegion(ConfidentialClientApplication.AttemptRegionDiscovery)
                    .WithExperimentalFeatures()
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                AuthenticationResult result = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .WithMtlsProofOfPossession()
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual("header.payload.signature", result.AccessToken);
                Assert.IsNull(result.ApiEvent.RegionUsed);
                Assert.AreEqual(RegionOutcome.FallbackToGlobal, result.ApiEvent.RegionOutcome);
            }
        }
    }
}
