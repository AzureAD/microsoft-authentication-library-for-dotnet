// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
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
using Microsoft.Identity.Test.Unit.Helpers;
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
                            .Build();

            MsalClientException ex = await AssertException.TaskThrowsAsync<MsalClientException>(() =>
                app.AcquireTokenForClient(TestConstants.s_scope)
                   .WithMtlsProofOfPossession() // Enables MTLS PoP
                   .ExecuteAsync())
                .ConfigureAwait(false);

            Assert.AreEqual(MsalError.ClientCredentialAuthenticationTypeMustBeDefined, ex.ErrorCode);
            Assert.AreEqual(MsalErrorMessage.ClientCredentialAuthenticationTypeMustBeDefined, ex.Message);
        }

        [TestMethod]
        public async Task MtlsPopWithoutCertificateAsync()
        {
            IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
                            .Create(TestConstants.ClientId)
                            .WithClientSecret(TestConstants.ClientSecret)
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
        public async Task MtlsPopWithoutCertificateWithClientClaimsAsync()
        {
            var ipAddress = new Dictionary<string, string>
                                    {
                                        { "client_ip", "192.168.1.2" }
                                    };

            IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
                            .Create(TestConstants.ClientId)
                            .WithClientClaims(s_testCertificate, ipAddress)
                            .Build();

            // Expecting an exception because MTLS PoP requires a certificate to sign the claims
            MsalClientException ex = await Assert.ThrowsExceptionAsync<MsalClientException>(() =>
                app.AcquireTokenForClient(TestConstants.s_scope)
                   .WithMtlsProofOfPossession() // Enables MTLS PoP
                   .ExecuteAsync())
                .ConfigureAwait(false);

            Assert.AreEqual(MsalError.MtlsCertificateNotProvided, ex.ErrorCode);
        }

        [TestMethod]
        public async Task MtlsPopWithoutCertificateWithClientAssertionAsync()
        {
            IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
                            .Create(TestConstants.ClientId)
                            .WithClientAssertion(() => { return TestConstants.DefaultClientAssertion; })
                            .Build();

            // Expecting an exception because MTLS PoP requires a certificate to sign the claims
            MsalClientException ex = await Assert.ThrowsExceptionAsync<MsalClientException>(() =>
                app.AcquireTokenForClient(TestConstants.s_scope)
                   .WithMtlsProofOfPossession() // Enables MTLS PoP
                   .ExecuteAsync())
                .ConfigureAwait(false);

            Assert.AreEqual(MsalError.MtlsCertificateNotProvided, ex.ErrorCode);
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task MtlsPop_WithoutRegion_ThrowsException(bool setAzureRegion)
        {
            using (var envContext = new EnvVariableContext())
            {
                IConfidentialClientApplication app;
                if (setAzureRegion)
                {
                    app = ConfidentialClientApplicationBuilder
                                    .Create(TestConstants.ClientId)
                                    .WithAuthority(TestConstants.AuthorityTenant)
                                    .WithCertificate(s_testCertificate)
                                    // Setting Azure region to ConfidentialClientApplicationBuilder.DisableForceRegion overrides the AzureRegion to null.
                                    .WithAzureRegion(ConfidentialClientApplicationBuilder.DisableForceRegion)
                                    .Build();
                }
                else
                {
                    app = ConfidentialClientApplicationBuilder
                                    .Create(TestConstants.ClientId)
                                    .WithAuthority(TestConstants.AuthorityTenant)
                                    .WithCertificate(s_testCertificate)
                                    .Build();
                }

                // Set WithMtlsProofOfPossession on the request
                MsalClientException ex = await AssertException.TaskThrowsAsync<MsalClientException>(() =>
                        app.AcquireTokenForClient(TestConstants.s_scope)
                           .WithMtlsProofOfPossession() // Enables MTLS PoP
                           .ExecuteAsync())
                        .ConfigureAwait(false);

                Assert.AreEqual(MsalError.MtlsPopWithoutRegion, ex.ErrorCode);
                Assert.AreEqual(MsalErrorMessage.MtlsPopWithoutRegion, ex.Message);
            }
        }

        [TestMethod]
        public async Task MtlsPop_WithUnsupportedNonTenantedAuthorityAsync_ThrowsException()
        {
            IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
                            .Create(TestConstants.ClientId)
                            .WithCertificate(s_testCertificate)
                            .Build();

            // Set WithMtlsProofOfPossession on the request without specifying an authority
            MsalClientException ex = await AssertException.TaskThrowsAsync<MsalClientException>(() =>
                app.AcquireTokenForClient(TestConstants.s_scope)
                   .WithMtlsProofOfPossession()
                   .ExecuteAsync())
                .ConfigureAwait(false);

            Assert.AreEqual(MsalError.MtlsPopWithoutRegion, ex.ErrorCode);
            Assert.AreEqual(MsalErrorMessage.MtlsPopWithoutRegion, ex.Message);
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

        [TestMethod]
        public void SchemeSetsCert()
        {
            var scheme = new MtlsPopAuthenticationOperation(s_testCertificate);
            AuthenticationResult ar = new AuthenticationResult();

            scheme.FormatResult(ar);

            Assert.AreSame(s_testCertificate, ar.BindingCertificate);
        }

        [TestMethod]
        public async Task SchemeSetsCertAsync()
        {
            var scheme = new MtlsPopAuthenticationOperation(s_testCertificate);
            AuthenticationResult ar = new AuthenticationResult();

            await scheme.FormatResultAsync(ar).ConfigureAwait(false);

            Assert.AreSame(s_testCertificate, ar.BindingCertificate);
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
            IReadOnlyDictionary<string, string> parameters = scheme.GetTokenRequestParams();

            Assert.AreEqual(Constants.MtlsPoPTokenType, parameters[OAuth2Parameter.TokenType]);
        }

        [TestMethod]
        public async Task AcquireTokenForClient_WithMtlsProofOfPossession_SuccessAsync()
        {
            const string region = "eastus";

            using (var envContext = new EnvVariableContext())
            {
                Environment.SetEnvironmentVariable("REGION_NAME", region);

                // Set the expected mTLS endpoint for public cloud
                string globalEndpoint = "mtlsauth.microsoft.com";
                string expectedTokenEndpoint = $"https://{region}.{globalEndpoint}/123456-1234-2345-1234561234/oauth2/v2.0/token";

                using (var httpManager = new MockHttpManager())
                {
                    // Set up mock handler with expected token endpoint URL
                    httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                        tokenType: "mtls_pop");

                    var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                        .WithCertificate(s_testCertificate)
                        .WithAuthority($"https://login.microsoftonline.com/123456-1234-2345-1234561234")
                        .WithAzureRegion(ConfidentialClientApplication.AttemptRegionDiscovery)
                        .WithHttpManager(httpManager)
                        .BuildConcrete();

                    // First token acquisition - should hit the identity provider
                    AuthenticationResult result = await app.AcquireTokenForClient(TestConstants.s_scope)
                        .WithMtlsProofOfPossession()
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    Assert.AreEqual("header.payload.signature", result.AccessToken);
                    Assert.AreEqual(Constants.MtlsPoPAuthHeaderPrefix, result.TokenType);
                    Assert.AreEqual(region, result.AuthenticationResultMetadata.RegionDetails.RegionUsed);
                    Assert.AreEqual(expectedTokenEndpoint, result.AuthenticationResultMetadata.TokenEndpoint);

                    Assert.IsNotNull(result.BindingCertificate, "BindingCertificate should be present.");
                    Assert.AreEqual(s_testCertificate.Thumbprint, result.BindingCertificate.Thumbprint, 
                        "BindingCertificate must match the cert passed to WithCertificate().");

                    // Second token acquisition - should retrieve from cache
                    AuthenticationResult secondResult = await app.AcquireTokenForClient(TestConstants.s_scope)
                        .WithMtlsProofOfPossession()
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    Assert.AreEqual("header.payload.signature", secondResult.AccessToken);
                    Assert.AreEqual(Constants.MtlsPoPAuthHeaderPrefix, secondResult.TokenType);
                    Assert.AreEqual(TokenSource.Cache, secondResult.AuthenticationResultMetadata.TokenSource);
                    Assert.AreEqual(expectedTokenEndpoint, result.AuthenticationResultMetadata.TokenEndpoint);
                    // Cached result must still carry the cert
                    Assert.IsNotNull(secondResult.BindingCertificate);
                    Assert.AreEqual(result.BindingCertificate.Thumbprint,
                        secondResult.BindingCertificate.Thumbprint);
                }
            }
        }

        [TestMethod]
        public async Task AcquireMtlsPopTokenForClientWithTenantId_SuccessAsync()
        {
            const string region = "eastus";

            using (var envContext = new EnvVariableContext())
            {
                Environment.SetEnvironmentVariable("REGION_NAME", region);

                // Set the expected mTLS endpoint for public cloud
                string globalEndpoint = "mtlsauth.microsoft.com";
                string expectedTokenEndpoint = $"https://{region}.{globalEndpoint}/123456-1234-2345-1234561234/oauth2/v2.0/token";

                using (var httpManager = new MockHttpManager())
                {
                    // Set up mock handler with expected token endpoint URL
                    httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                        tokenType: "mtls_pop");

                    var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                        .WithCertificate(s_testCertificate)
                        .WithTenantId("123456-1234-2345-1234561234")
                        .WithAzureRegion(ConfidentialClientApplication.AttemptRegionDiscovery)
                        .WithHttpManager(httpManager)
                        .BuildConcrete();

                    // First token acquisition - should hit the identity provider
                    AuthenticationResult result = await app.AcquireTokenForClient(TestConstants.s_scope)
                        .WithMtlsProofOfPossession()
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    Assert.AreEqual("header.payload.signature", result.AccessToken);
                    Assert.AreEqual(Constants.MtlsPoPAuthHeaderPrefix, result.TokenType);
                    Assert.AreEqual(region, result.AuthenticationResultMetadata.RegionDetails.RegionUsed);
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
        public async Task AcquireMtlsPopTokenForClientWithTenantIdCertChecks_Async()
        {
            const string region = "eastus";
            
            // ─────────── Two distinct certificates ───────────
            var certA = CertHelper.GetOrCreateTestCert();
            var certB = CertHelper.GetOrCreateTestCert(regenerateCert: true);

            using (var envContext = new EnvVariableContext())
            {
                Environment.SetEnvironmentVariable("REGION_NAME", region);

                // Set the expected mTLS endpoint for public cloud
                string globalEndpoint = "mtlsauth.microsoft.com";
                string expectedTokenEndpoint = $"https://{region}.{globalEndpoint}/123456-1234-2345-1234561234/oauth2/v2.0/token";

                using (var httpManager = new MockHttpManager())
                {
                    // Set up mock handler with expected token endpoint URL
                    httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                        tokenType: "mtls_pop");

                    var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                        .WithCertificate(certA)
                        .WithTenantId("123456-1234-2345-1234561234")
                        .WithAzureRegion(ConfidentialClientApplication.AttemptRegionDiscovery)
                        .WithHttpManager(httpManager)
                        .BuildConcrete();

                    // First token acquisition - should hit the identity provider
                    AuthenticationResult result = await app.AcquireTokenForClient(TestConstants.s_scope)
                        .WithMtlsProofOfPossession()
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    Assert.AreEqual("header.payload.signature", result.AccessToken);
                    Assert.AreEqual(Constants.MtlsPoPAuthHeaderPrefix, result.TokenType);
                    Assert.AreEqual(region, result.AuthenticationResultMetadata.RegionDetails.RegionUsed);
                    Assert.AreEqual(expectedTokenEndpoint, result.AuthenticationResultMetadata.TokenEndpoint);
                    Assert.AreEqual(certA.Thumbprint, result.BindingCertificate.Thumbprint);

                    app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                        .WithCertificate(certB)
                        .WithTenantId("123456-1234-2345-1234561234")
                        .WithAzureRegion(ConfidentialClientApplication.AttemptRegionDiscovery)
                        .WithHttpManager(httpManager)
                        .BuildConcrete();

                    // Set up mock handler with expected token endpoint URL
                    httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                        tokenType: "mtls_pop");

                    // Second token acquisition - should also be from IDP because we have a new cert
                    AuthenticationResult secondResult = await app.AcquireTokenForClient(TestConstants.s_scope)
                        .WithMtlsProofOfPossession()
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    Assert.AreEqual("header.payload.signature", secondResult.AccessToken);
                    Assert.AreEqual(Constants.MtlsPoPAuthHeaderPrefix, secondResult.TokenType);
                    Assert.AreEqual(TokenSource.IdentityProvider, secondResult.AuthenticationResultMetadata.TokenSource);
                    Assert.AreEqual(expectedTokenEndpoint, result.AuthenticationResultMetadata.TokenEndpoint);
                    Assert.AreEqual(certB.Thumbprint, secondResult.BindingCertificate.Thumbprint);
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
            string authority = "https://login.microsoftonline.com/123456-1234-2345-1234561234";
            string globalEndpoint = "mtlsauth.microsoft.com";
            string expectedTokenEndpoint = $"https://{region}.{globalEndpoint}/123456-1234-2345-1234561234/oauth2/v2.0/token";

            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddRegionDiscoveryMockHandler(region);
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(tokenType: "mtls_pop");

                IConfidentialClientApplication regionalApp1 = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithCertificate(s_testCertificate)
                    .WithAzureRegion(region)
                    .WithAuthority(authority)
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                IConfidentialClientApplication regionalApp2 = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithCertificate(s_testCertificate)
                    .WithAzureRegion(region)
                    .WithAuthority(authority)
                    .WithHttpManager(httpManager)
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
                using (var harness = new MockHttpAndServiceBundle())
                {
                    harness.ServiceBundle.Config.RetryPolicyFactory = new TestRetryPolicyFactory();

                    // for simplicity, return 404 so retry is not triggered
                    httpManager.AddRegionDiscoveryMockHandlerWithError(HttpStatusCode.NotFound);

                    ConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                        .WithCertificate(s_testCertificate)
                        .WithAuthority("https://login.microsoftonline.com/123456-1234-2345-1234561234")
                        .WithAzureRegion(ConfidentialClientApplication.AttemptRegionDiscovery)
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
        [DataRow("https://contoso.b2clogin.com/tfp/contoso.onmicrosoft.com/B2C_1_signupsignin", "B2C Authority", typeof(MsalServiceException))]
        [DataRow("https://contoso.adfs.contoso.com/adfs", "ADFS Authority", typeof(HttpRequestException))]
        public async Task MtlsPop_NonAadAuthorityAsync(string authorityUrl, string authorityType, Type expectedException)
        {
            IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
                            .Create(TestConstants.ClientId)
                            .WithCertificate(s_testCertificate)
                            .WithAuthority(authorityUrl)
                            .Build();

            // Set WithMtlsProofOfPossession on the request with a non-AAD authority
            try
            {
                await app
                    .AcquireTokenForClient(TestConstants.s_scope)
                    .WithMtlsProofOfPossession() // Enables MTLS PoP
                    .ExecuteAsync()
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Assert.AreEqual(expectedException, ex.GetType());
            }
        }

        [DataTestMethod]
        [DataRow("https://login.microsoftonline.com", TestConstants.Common, "Public Cloud")]
        [DataRow("https://login.microsoftonline.com", TestConstants.Organizations, "Public Cloud")]
        [DataRow("https://login.microsoftonline.us", TestConstants.Common, "Azure Government")]
        [DataRow("https://login.microsoftonline.us", TestConstants.Organizations, "Azure Government")]
        [DataRow("https://login.partner.microsoftonline.cn", TestConstants.Common, "Azure China")]
        [DataRow("https://login.partner.microsoftonline.cn", TestConstants.Organizations, "Azure China")]
        public async Task MtlsPop_WithUnsupportedNonTenantedAuthorityAsync_ThrowsException(string authorityUrl, string nonTenantValue, string cloudType)
        {
            const string region = "eastus";

            using (var envContext = new EnvVariableContext()) 
            {
                Environment.SetEnvironmentVariable("REGION_NAME", region);

                using (var httpManager = new MockHttpManager())
                {
                    var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                        .WithCertificate(s_testCertificate)
                        .WithAuthority($"{authorityUrl}/{nonTenantValue}")
                        .WithAzureRegion(ConfidentialClientApplication.AttemptRegionDiscovery)
                        .WithHttpManager(httpManager)
                        .BuildConcrete();

                    // Expect an exception due to using /common or /organizations with MTLS PoP
                    MsalClientException ex = await Assert.ThrowsExceptionAsync<MsalClientException>(async () =>
                        await app.AcquireTokenForClient(TestConstants.s_scope)
                            .WithMtlsProofOfPossession()
                            .ExecuteAsync()
                            .ConfigureAwait(false))
                        .ConfigureAwait(false);

                    Assert.AreEqual(MsalError.MissingTenantedAuthority, ex.ErrorCode);
                    Assert.AreEqual(MsalErrorMessage.MtlsNonTenantedAuthorityNotAllowedMessage, ex.Message);
                }
            }
        }

        [TestMethod]
        public async Task MtlsPop_ValidateExpectedUrlAsync()
        {
            string authorityUrl = "https://login.microsoftonline.com/123456-1234-2345-1234561234";

            using (var envContext = new EnvVariableContext())
            {
                // Arrange
                Environment.SetEnvironmentVariable("REGION_NAME", EastUsRegion);

                using (var harness = new MockHttpAndServiceBundle())
                {
                    var tokenHttpCallHandler = new MockHttpMessageHandler()
                    {
                        ExpectedUrl = $"https://eastus.mtlsauth.microsoft.com/123456-1234-2345-1234561234/oauth2/v2.0/token",
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = CreateResponse(tokenType : "mtls_pop"),
                        ExpectedPostData = new Dictionary<string, string>
                        {
                            { OAuth2Parameter.ClientId, "d3adb33f-c0de-ed0c-c0de-deadb33fc0d3" },
                            { OAuth2Parameter.Scope, TestConstants.s_scope.AsSingleString() },
                            { OAuth2Parameter.GrantType, OAuth2GrantType.ClientCredentials },
                            { "token_type", "mtls_pop" }
                        },
                        UnExpectedPostData = new Dictionary<string, string>
                        {
                            { "client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer" },
                            { "client_assertion", "eyJhbGciOiJQUzI1NiIsInR5cCI6IkpXVCIsIng1dCNTMjU2IjoiSnBmTm1PM1lpR2pHQ1pWY..." }
                        }
                    };

                    harness.HttpManager.AddMockHandler(tokenHttpCallHandler);

                    var app = ConfidentialClientApplicationBuilder
                                 .Create(TestConstants.ClientId)
                                 .WithAuthority(authorityUrl)
                                 .WithHttpManager(harness.HttpManager)
                                 .WithAzureRegion(ConfidentialClientApplication.AttemptRegionDiscovery)
                                 .WithCertificate(s_testCertificate)
                                 .Build();

                    // Act
                    var result = await app.AcquireTokenForClient(TestConstants.s_scope)
                        .WithMtlsProofOfPossession()
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    // Assert
                    Assert.IsNotNull(result.AccessToken);
                    Assert.AreEqual(EastUsRegion, result.AuthenticationResultMetadata.RegionDetails.RegionUsed);
                    Assert.AreEqual(RegionOutcome.AutodetectSuccess, result.AuthenticationResultMetadata.RegionDetails.RegionOutcome);
                }
            }
        }

        [DataTestMethod]
        [DataRow("login.microsoftonline.com", "mtlsauth.microsoft.com")]
        [DataRow("login.microsoftonline.us", "mtlsauth.microsoftonline.us")]
        [DataRow("login.usgovcloudapi.net", "mtlsauth.microsoftonline.us")]
        [DataRow("login.partner.microsoftonline.cn", "mtlsauth.partner.microsoftonline.cn")]
        [DataRow("login.chinacloudapi.cn", "mtlsauth.partner.microsoftonline.cn")]
        public async Task PublicAndSovereignCloud_UsesPreferredNetwork_AndNoDiscovery_Async(string inputEnv, string expectedEnv)
        {
            // Append the input environment to create the authority URL
            string authorityUrl = $"https://{inputEnv}/17b189bc-2b81-4ec5-aa51-3e628cbc931b";

            using (var envContext = new EnvVariableContext())
            {
                Environment.SetEnvironmentVariable("REGION_NAME", EastUsRegion);

                using (var harness = new MockHttpAndServiceBundle())
                {
                    var tokenHttpCallHandler = new MockHttpMessageHandler()
                    {
                        ExpectedUrl = $"https://{EastUsRegion}.{expectedEnv}/17b189bc-2b81-4ec5-aa51-3e628cbc931b/oauth2/v2.0/token",
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = CreateResponse(tokenType: "mtls_pop")
                    };
                    harness.HttpManager.AddMockHandler(tokenHttpCallHandler);

                    var app = ConfidentialClientApplicationBuilder
                                        .Create(TestConstants.ClientId)
                                        .WithAuthority(authorityUrl)
                                        .WithHttpManager(harness.HttpManager)
                                        .WithAzureRegion(ConfidentialClientApplication.AttemptRegionDiscovery)
                                        .WithCertificate(s_testCertificate)
                                        .Build();

                    AuthenticationResult result = await app
                        .AcquireTokenForClient(TestConstants.s_scope)
                        .WithMtlsProofOfPossession()
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    Assert.AreEqual("eastus", result.ApiEvent.RegionUsed);
                    Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                    // Verify that the full token endpoint URL was used correctly
                    string expectedTokenEndpoint = $"https://{EastUsRegion}.{expectedEnv}/17b189bc-2b81-4ec5-aa51-3e628cbc931b/oauth2/v2.0/token";
                    Assert.AreEqual(expectedTokenEndpoint, tokenHttpCallHandler.ExpectedUrl);

                    // Second token acquisition - should retrieve from cache
                    AuthenticationResult secondResult = await app.AcquireTokenForClient(TestConstants.s_scope)
                        .WithMtlsProofOfPossession()
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    Assert.AreEqual("header.payload.signature", secondResult.AccessToken);
                    Assert.AreEqual(Constants.MtlsPoPAuthHeaderPrefix, secondResult.TokenType);
                    Assert.AreEqual(TokenSource.Cache, secondResult.AuthenticationResultMetadata.TokenSource);
                    Assert.AreEqual(EastUsRegion, result.ApiEvent.RegionUsed);
                    Assert.AreEqual(EastUsRegion, result.AuthenticationResultMetadata.RegionDetails.RegionUsed);
                    Assert.AreEqual(RegionOutcome.AutodetectSuccess, result.AuthenticationResultMetadata.RegionDetails.RegionOutcome);
                    Assert.AreEqual(null, result.AuthenticationResultMetadata.RegionDetails.AutoDetectionError);
                }
            }
        }

        [TestMethod]
        public async Task AcquireTokenForClient_WithMtlsPop_NonStandardCloudAsync()
        {
            string nonStandardAuthority = "https://login.myLocalAAD.com/123456-1234-2345-1234561234";
            string expectedRegionPrefix = "eastus";
            string mtlsSubdomain = "mtlsauth";

            string expectedTokenEndpoint = $"https://{expectedRegionPrefix}.{mtlsSubdomain}.mylocalaad.com/123456-1234-2345-1234561234/oauth2/v2.0/token";

            using (var envContext = new EnvVariableContext())
            {
                Environment.SetEnvironmentVariable("REGION_NAME", EastUsRegion);

                using (var harness = new MockHttpAndServiceBundle())
                {
                    var tokenHttpCallHandler = new MockHttpMessageHandler()
                    {
                        ExpectedUrl = expectedTokenEndpoint,
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = CreateResponse(tokenType: "mtls_pop")
                    };
                    harness.HttpManager.AddMockHandler(tokenHttpCallHandler);

                    var app = ConfidentialClientApplicationBuilder
                                    .Create(TestConstants.ClientId)
                                    .WithAuthority(nonStandardAuthority)
                                    .WithHttpManager(harness.HttpManager)
                                    .WithAzureRegion(ConfidentialClientApplication.AttemptRegionDiscovery)
                                    .WithCertificate(s_testCertificate)
                                    .WithInstanceDiscovery(false)
                                    .Build();

                    AuthenticationResult result = await app
                        .AcquireTokenForClient(TestConstants.s_scope)
                        .WithMtlsProofOfPossession()
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    // Assert
                    Assert.IsNotNull(result);
                    Assert.AreEqual("header.payload.signature", result.AccessToken);
                    Assert.AreEqual(Constants.MtlsPoPAuthHeaderPrefix, result.TokenType);
                    Assert.AreEqual(expectedRegionPrefix, result.ApiEvent.RegionUsed);
                    Assert.AreEqual(expectedTokenEndpoint, tokenHttpCallHandler.ExpectedUrl);
                    Assert.AreEqual(RegionOutcome.AutodetectSuccess, result.AuthenticationResultMetadata.RegionDetails.RegionOutcome);
                }
            }
        }

        private static HttpResponseMessage CreateResponse(
            string tokenType,
            string token = "header.payload.signature",
            string expiresIn = "3599")
        {
            return MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage(token, expiresIn, tokenType);
        }

        [TestMethod]
        public async Task AcquireTokenForClient_WithMtlsPop_Dsts_SuccessAsync()
        {
            string authorityUrl = TestConstants.DstsAuthorityTenanted;

            // Modify the endpoint based on the authorityUrl
            string expectedTokenEndpoint = $"{authorityUrl}oauth2/v2.0/token";

            using (var httpManager = new MockHttpManager())
            {
                // Set up mock handler with expected token endpoint URL
                httpManager.AddMockHandler(new MockHttpMessageHandler()
                {
                    ExpectedUrl = expectedTokenEndpoint,
                    ExpectedMethod = HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage(tokenType: "mtls_pop")
                });

                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithCertificate(s_testCertificate)
                    .WithAuthority(authorityUrl)
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                // First token acquisition - should hit the identity provider
                AuthenticationResult result = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .WithMtlsProofOfPossession()
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual("header.payload.signature", result.AccessToken);
                Assert.AreEqual(Constants.MtlsPoPAuthHeaderPrefix, result.TokenType);
                Assert.AreEqual(null, result.AuthenticationResultMetadata.RegionDetails.RegionUsed);
                Assert.AreEqual(expectedTokenEndpoint, result.AuthenticationResultMetadata.TokenEndpoint);

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

        [DataTestMethod]
        [DataRow(TestConstants.DstsAuthorityCommon)]
        [DataRow(TestConstants.DstsAuthorityOrganizations)]
        public async Task MtlsPop_WithUnsupportedNonTenantedAuthorityAsyncForDsts_ThrowsException(string authorityUrl)
        {
            IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
                            .Create(TestConstants.ClientId)
                            .WithAuthority(authorityUrl)
                            .WithCertificate(s_testCertificate)
                            .Build();

            // Set WithMtlsProofOfPossession on the request specifying an authority
            HttpRequestException ex = await AssertException.TaskThrowsAsync<HttpRequestException>(() =>
                app.AcquireTokenForClient(TestConstants.s_scope)
                   .WithMtlsProofOfPossession()
                   .ExecuteAsync())
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task BindingCertificate_PopulatedForMtlsPop_AndNullForBearerAsync()
        {
            const string region = "eastus";
            using var env = new EnvVariableContext();
            Environment.SetEnvironmentVariable("REGION_NAME", region);

            using var httpManager = new MockHttpManager();
            {
                // Token call for MTLS-PoP
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                            tokenType: "mtls_pop");
                // Token call for bearer  – second AcquireToken uses this
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(); // defaults to Bearer

                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithCertificate(s_testCertificate)
                    .WithAuthority("https://login.microsoftonline.com/123456-1234-2345-1234561234")
                    .WithAzureRegion(ConfidentialClientApplication.AttemptRegionDiscovery)
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                // -------- 1st call: MTLS-PoP --------
                AuthenticationResult popResult = await app.AcquireTokenForClient(TestConstants.s_scope)
                                                          .WithMtlsProofOfPossession()
                                                          .ExecuteAsync()
                                                          .ConfigureAwait(false);

                Assert.AreEqual(Constants.MtlsPoPAuthHeaderPrefix, popResult.TokenType);
                Assert.IsNotNull(popResult.BindingCertificate, "BindingCertificate should be set for MTLS-PoP.");
                Assert.AreEqual(s_testCertificate.Thumbprint,
                                popResult.BindingCertificate.Thumbprint,
                                "BindingCertificate thumbprint should match the cert supplied via WithCertificate().");

                // -------- 2nd call: Bearer --------
                AuthenticationResult bearerResult = await app.AcquireTokenForClient(TestConstants.s_scope)
                                                             .ExecuteAsync()
                                                             .ConfigureAwait(false);

                Assert.AreEqual("Bearer", bearerResult.TokenType);
                Assert.IsNull(bearerResult.BindingCertificate, "BindingCertificate must be null for Bearer tokens.");
            }
        }
    }
}
