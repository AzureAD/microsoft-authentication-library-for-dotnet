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

        [TestMethod]
        public async Task MtlsPop_AadAuthorityWithoutCertificateAsync()
        {
            IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
                            .Create(TestConstants.ClientId)
                            .WithAuthority("https://login.microsoftonline.com/123456-1234-2345-1234561234")
                            .WithExperimentalFeatures()
                            .Build();

            MsalClientException ex = await AssertException.TaskThrowsAsync<MsalClientException>(() =>
                app.AcquireTokenForClient(TestConstants.s_scope)
                   .WithMtlsProofOfPossession() // Enables MTLS PoP
                   .ExecuteAsync())
                .ConfigureAwait(false);

            Assert.AreEqual(MsalError.MtlsCertificateNotProvided, ex.ErrorCode);
            Assert.AreEqual(MsalErrorMessage.MtlsCertificateNotProvidedMessage, ex.Message);
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
            using (var envContext = new EnvVariableContext())
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
        }

        [TestMethod]
        public async Task MtlsPopWithoutAuthorityAsync()
        {
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

        [DataTestMethod]
        [DataRow("https://login.microsoftonline.com", "Public Cloud")]
        [DataRow("https://login.microsoftonline.us", "Azure Government")]
        [DataRow("https://login.partner.microsoftonline.cn", "Azure China")]
        public async Task AcquireTokenForClient_WithMtlsProofOfPossession_SuccessAsync(string authorityUrl, string cloudType)
        {
            const string region = "eastus";

            using (var envContext = new EnvVariableContext())
            {
                Environment.SetEnvironmentVariable("REGION_NAME", region);

                // Set the expected mTLS endpoint based on the cloud type
                string globalEndpoint = authorityUrl.Contains("microsoftonline.com")
                    ? "mtlsauth.microsoft.com"
                    : authorityUrl.Contains("microsoftonline.us")
                        ? "mtlsauth.microsoftonline.us"
                        : "mtlsauth.partner.microsoftonline.cn";

                string expectedTokenEndpoint = $"https://{region}.{globalEndpoint}/123456-1234-2345-1234561234/oauth2/v2.0/token";

                using (var httpManager = new MockHttpManager())
                {
                    // Set up mock handler with expected token endpoint URL
                    httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                        tokenType: "mtls_pop");

                    var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                        .WithCertificate(s_testCertificate)
                        .WithAuthority($"{authorityUrl}/123456-1234-2345-1234561234")
                        .WithAzureRegion(ConfidentialClientApplication.AttemptRegionDiscovery)
                        .WithExperimentalFeatures()
                        .WithHttpManager(httpManager)
                        .BuildConcrete();

                    // First token acquisition - should hit the identity provider
                    AuthenticationResult result = await app.AcquireTokenForClient(TestConstants.s_scope)
                        .WithMtlsProofOfPossession()
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    Assert.AreEqual("header.payload.signature", result.AccessToken);
                    Assert.AreEqual(Constants.MtlsPoPAuthHeaderPrefix, result.TokenType);
                    Assert.AreEqual(region, result.AuthenticationResultMetadata.RegionDetails.RegionUsed, $"Expected region for {cloudType}");
                    Assert.AreEqual(expectedTokenEndpoint, result.AuthenticationResultMetadata.TokenEndpoint);

                    // Second token acquisition - should retrieve from cache
                    AuthenticationResult secondResult = await app.AcquireTokenForClient(TestConstants.s_scope)
                        .WithMtlsProofOfPossession()
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    Assert.AreEqual("header.payload.signature", secondResult.AccessToken);
                    Assert.AreEqual(Constants.MtlsPoPAuthHeaderPrefix, secondResult.TokenType);
                    Assert.AreEqual(TokenSource.Cache, secondResult.AuthenticationResultMetadata.TokenSource);
                    Assert.AreEqual(expectedTokenEndpoint, result.AuthenticationResultMetadata.TokenEndpoint);
                }
            }
        }

        [TestMethod]
        public async Task MtlsPop_KnownRegionAsync()
        {
            const string region = "centralus";
            string authorityUrl = "https://login.microsoftonline.com/123456-1234-2345-1234561234";
            string globalEndpoint = "mtlsauth.microsoft.com";
            string expectedTokenEndpoint = $"https://{region}.{globalEndpoint}/123456-1234-2345-1234561234/oauth2/v2.0/token";

            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddRegionDiscoveryMockHandler(region);
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(tokenType: "mtls_pop");

                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithCertificate(s_testCertificate)
                    .WithAuthority(authorityUrl)
                    .WithAzureRegion(region)
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures()
                    .BuildConcrete();

                AuthenticationResult result = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .WithMtlsProofOfPossession()
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual("header.payload.signature", result.AccessToken);
                Assert.AreEqual(region, result.AuthenticationResultMetadata.RegionDetails.RegionUsed);
                Assert.AreEqual(RegionOutcome.UserProvidedValid, result.ApiEvent.RegionOutcome);
                Assert.AreEqual(expectedTokenEndpoint, result.AuthenticationResultMetadata.TokenEndpoint);
            }
        }

        [TestMethod]
        public async Task MtlsPop_RegionalTokenCacheInterchangeabilityAsync()
        {
            const string region = "centralus";
            string authorityUrl = "https://login.microsoftonline.com/123456-1234-2345-1234561234";
            string globalEndpoint = "mtlsauth.microsoft.com";
            string expectedTokenEndpoint = $"https://{region}.{globalEndpoint}/123456-1234-2345-1234561234/oauth2/v2.0/token";

            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddRegionDiscoveryMockHandler(region);
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(tokenType: "mtls_pop");

                IConfidentialClientApplication regionalApp1 = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithCertificate(s_testCertificate)
                    .WithAzureRegion(region)
                    .WithAuthority("https://login.microsoftonline.com/123456-1234-2345-1234561234")
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures()
                    .BuildConcrete();

                IConfidentialClientApplication regionalApp2 = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithCertificate(s_testCertificate)
                    .WithAzureRegion(region)
                    .WithAuthority("https://login.microsoftonline.com/123456-1234-2345-1234561234")
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

                Assert.AreEqual(region, regionalResult1.AuthenticationResultMetadata.RegionDetails.RegionUsed);
                Assert.AreEqual(TokenSource.IdentityProvider, regionalResult1.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(expectedTokenEndpoint, regionalResult1.AuthenticationResultMetadata.TokenEndpoint);

                // Attempt acquisition with the other regional app, should retrieve from cache
                var regionalResult2 = await regionalApp2.AcquireTokenForClient(TestConstants.s_scope)
                    .WithMtlsProofOfPossession()
                    .ExecuteAsync()
                    .ConfigureAwait(false);
                
                Assert.AreEqual(TokenSource.Cache, regionalResult2.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(region, regionalResult2.AuthenticationResultMetadata.RegionDetails.RegionUsed);
            }
        }

        [TestMethod]
        public async Task MtlsPop_ThrowsExceptionWhenRegionAutoDetectFailsAsync()
        {
            using (var envContext = new EnvVariableContext())
            {
                Environment.SetEnvironmentVariable("REGION_NAME", null);  // Ensure no region is set

                using (var httpManager = new MockHttpManager())
                {
                    httpManager.AddRegionDiscoveryMockHandlerNotFound();

                    ConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                        .WithCertificate(s_testCertificate)
                        .WithAuthority("https://login.microsoftonline.com/123456-1234-2345-1234561234")
                        .WithAzureRegion(ConfidentialClientApplication.AttemptRegionDiscovery)
                        .WithExperimentalFeatures()
                        .WithHttpManager(httpManager)
                        .BuildConcrete();

                    // Expect an MsalServiceException due to missing region for MTLS POP
                    MsalServiceException ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
                        await app.AcquireTokenForClient(TestConstants.s_scope)
                            .WithMtlsProofOfPossession()
                            .ExecuteAsync()
                            .ConfigureAwait(false))
                        .ConfigureAwait(false);

                    Assert.AreEqual(MsalError.RegionRequiredForMtlsPop, ex.ErrorCode);
                    Assert.AreEqual(MsalErrorMessage.RegionRequiredForMtlsPopMessage, ex.Message);
                }
            }
        }

        [TestMethod]
        [DataTestMethod]
        [DataRow("https://contoso.b2clogin.com/tfp/contoso.onmicrosoft.com/B2C_1_signupsignin", "B2C Authority")]
        [DataRow("https://contoso.adfs.contoso.com/adfs", "ADFS Authority")]
        public async Task MtlsPop_NonAadAuthorityAsync(string authorityUrl, string authorityType)
        {
            IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
                            .Create(TestConstants.ClientId)
                            .WithCertificate(s_testCertificate)
                            .WithAuthority(authorityUrl)
                            .WithExperimentalFeatures()
                            .Build();

            // Set WithMtlsProofOfPossession on the request with a non-AAD authority
            MsalClientException ex = await AssertException.TaskThrowsAsync<MsalClientException>(() =>
                app.AcquireTokenForClient(TestConstants.s_scope)
                   .WithMtlsProofOfPossession() // Enables MTLS PoP
                   .ExecuteAsync())
                .ConfigureAwait(false);

            Assert.AreEqual(MsalError.InvalidAuthorityType, ex.ErrorCode);
            Assert.AreEqual(MsalErrorMessage.MtlsInvalidAuthorityTypeMessage, ex.Message, $"{authorityType} test failed.");
        }

        [DataTestMethod]
        [DataRow("https://login.microsoftonline.com", "Public Cloud")]
        [DataRow("https://login.microsoftonline.us", "Azure Government")]
        [DataRow("https://login.partner.microsoftonline.cn", "Azure China")]
        public async Task MtlsPop_WithCommonAsync(string authorityUrl, string cloudType)
        {
            const string region = "eastus";

            using (var envContext = new EnvVariableContext()) 
            {
                Environment.SetEnvironmentVariable("REGION_NAME", region);

                using (var httpManager = new MockHttpManager())
                {
                    var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                        .WithCertificate(s_testCertificate)
                        .WithAuthority($"{authorityUrl}/common")
                        .WithAzureRegion(ConfidentialClientApplication.AttemptRegionDiscovery)
                        .WithExperimentalFeatures()
                        .WithHttpManager(httpManager)
                        .BuildConcrete();

                    // Expect an exception due to using /common with MTLS PoP
                    MsalClientException ex = await Assert.ThrowsExceptionAsync<MsalClientException>(async () =>
                        await app.AcquireTokenForClient(TestConstants.s_scope)
                            .WithMtlsProofOfPossession()
                            .ExecuteAsync()
                            .ConfigureAwait(false))
                        .ConfigureAwait(false);

                    Assert.AreEqual(MsalError.AuthorityHostMismatch, ex.ErrorCode);
                    Assert.AreEqual(MsalErrorMessage.MtlsCommonAuthorityNotAllowedMessage, ex.Message);
                }
            }
        }
    }
}
