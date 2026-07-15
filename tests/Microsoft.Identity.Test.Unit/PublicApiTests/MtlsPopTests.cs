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
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.Extensibility;
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
        public async Task MtlsPop_WithDynamicCertificate_WithoutRegion_UsesGlobalMtlsEndpointAsync()
        {
            // Dynamic cert + mTLS PoP without region should fall through to the global mTLS endpoint,
            // matching the static-cert behavior validated by MtlsPop_WithoutRegion_UsesGlobalMtlsEndpoint.
            const string globalEndpoint = "mtlsauth.microsoft.com";
            string expectedTokenEndpoint = $"https://{globalEndpoint}/{TestConstants.TenantId}/oauth2/v2.0/token";

            using (var envContext = new EnvVariableContext())
            {
                Environment.SetEnvironmentVariable("REGION_NAME", null);
                Environment.SetEnvironmentVariable("MSAL_FORCE_REGION", null);

                using (var httpManager = new MockHttpManager())
                {
                    httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                        tokenType: "mtls_pop");

                    var app = ConfidentialClientApplicationBuilder
                        .Create(TestConstants.ClientId)
                        .WithExperimentalFeatures()
                        .WithAuthority(TestConstants.AuthorityTenant)
                        .WithCertificate(_ => Task.FromResult(s_testCertificate), new CertificateOptions())
                        .WithHttpManager(httpManager)
                        .Build();

                    AuthenticationResult result = await app.AcquireTokenForClient(TestConstants.s_scope)
                        .WithMtlsProofOfPossession()
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    Assert.AreEqual(Constants.MtlsPoPAuthHeaderPrefix, result.TokenType);
                    Assert.AreEqual(expectedTokenEndpoint, result.AuthenticationResultMetadata.TokenEndpoint);
                }
            }
        }

        [TestMethod]
        public async Task MtlsPop_WithDynamicCertificate_NullFromProvider_ThrowsAsync()
        {
            using (var envContext = new EnvVariableContext())
            {
                Environment.SetEnvironmentVariable("REGION_NAME", EastUsRegion);

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithExperimentalFeatures()
                    .WithAuthority(TestConstants.AuthorityTenant)
                    .WithCertificate(_ => Task.FromResult<X509Certificate2>(null), new CertificateOptions())
                    .WithAzureRegion(ConfidentialClientApplication.AttemptRegionDiscovery)
                    .Build();

                MsalClientException ex = await AssertException.TaskThrowsAsync<MsalClientException>(() =>
                    app.AcquireTokenForClient(TestConstants.s_scope)
                       .WithMtlsProofOfPossession()
                       .ExecuteAsync())
                    .ConfigureAwait(false);

                Assert.AreEqual(MsalError.MtlsCertificateNotProvided, ex.ErrorCode);
            }
        }

        [TestMethod]
        public async Task MtlsPop_WithDynamicCertificate_SuccessAsync()
        {
            const string region = "eastus";
            int providerCallCount = 0;

            using (var envContext = new EnvVariableContext())
            {
                Environment.SetEnvironmentVariable("REGION_NAME", region);

                string globalEndpoint = "mtlsauth.microsoft.com";
                string expectedTokenEndpoint = $"https://{region}.{globalEndpoint}/123456-1234-2345-1234561234/oauth2/v2.0/token";

                using (var httpManager = new MockHttpManager())
                {
                    httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                        tokenType: "mtls_pop");

                    var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                        .WithExperimentalFeatures()
                        .WithCertificate(
                            _ =>
                            {
                                Interlocked.Increment(ref providerCallCount);
                                return Task.FromResult(s_testCertificate);
                            },
                            new CertificateOptions())
                        .WithAuthority($"https://login.microsoftonline.com/123456-1234-2345-1234561234")
                        .WithAzureRegion(ConfidentialClientApplication.AttemptRegionDiscovery)
                        .WithHttpManager(httpManager)
                        .BuildConcrete();

                    AuthenticationResult result = await app.AcquireTokenForClient(TestConstants.s_scope)
                        .WithMtlsProofOfPossession()
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    Assert.AreEqual("header.payload.signature", result.AccessToken);
                    Assert.AreEqual(Constants.MtlsPoPAuthHeaderPrefix, result.TokenType);
                    Assert.AreEqual(region, result.AuthenticationResultMetadata.RegionDetails.RegionUsed);
                    Assert.AreEqual(expectedTokenEndpoint, result.AuthenticationResultMetadata.TokenEndpoint);

                    Assert.IsNotNull(result.BindingCertificate, "BindingCertificate should be present.");
                    Assert.AreEqual(s_testCertificate.Thumbprint, result.BindingCertificate.Thumbprint);

                    // Provider must be invoked exactly once per mTLS PoP request.
                    // Preflight (MtlsPopParametersInitializer) resolves the cert and stashes it
                    // on the request; runtime (CredentialMaterialResolver.ResolveAsync) detects
                    // the preflight-resolved cert on a certificate credential and short-circuits
                    // the credential roundtrip. This locks in the single-invocation principle
                    // from issue #5943.
                    Assert.AreEqual(1, providerCallCount, "The certificate provider must be invoked exactly once per mTLS PoP token request (#5943 principle). If this assertion fails with count=2, the resolver short-circuit in CredentialMaterialResolver.ResolveAsync is no longer reusing the preflight-resolved certificate on requestParams.MtlsCertificate.");
                }
            }
        }

        [TestMethod]
        public async Task MtlsPopWithoutCertificateWithClientClaimsAsync()
        {
            var ipAddress = new Dictionary<string, string>
                                    {
                                        { "client_ip", "192.168.1.2" }
                                    };

#pragma warning disable CS0618 // Type or member is obsolete
            IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
                            .Create(TestConstants.ClientId)
                            .WithClientClaims(s_testCertificate, ipAddress)
                            .Build();
#pragma warning restore CS0618 // Type or member is obsolete

            // Expecting an exception because MTLS PoP requires a certificate to sign the claims
            MsalClientException ex = await Assert.ThrowsAsync<MsalClientException>(() =>
                app.AcquireTokenForClient(TestConstants.s_scope)
                   .WithMtlsProofOfPossession() // Enables MTLS PoP
                   .ExecuteAsync())
                .ConfigureAwait(false);

            Assert.AreEqual(MsalError.MtlsCertificateNotProvided, ex.ErrorCode);

            // Lock in the message wording so a future "centralise error messages" refactor cannot
            // silently re-broaden it back to MtlsCertificateNotProvidedMessage and lose the
            // WithClientClaims-specific diagnostic.
            StringAssert.Contains(ex.Message, "WithClientClaims");
        }

        [TestMethod]
        public async Task MtlsPopWithoutCertificateWithClientAssertionAsync()
        {
            IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
                            .Create(TestConstants.ClientId)
                            .WithClientAssertion(() => { return TestConstants.DefaultClientAssertion; })
                            .Build();

            // Expecting an exception because MTLS PoP requires a certificate to sign the claims
            MsalClientException ex = await Assert.ThrowsAsync<MsalClientException>(() =>
                app.AcquireTokenForClient(TestConstants.s_scope)
                   .WithMtlsProofOfPossession() // Enables MTLS PoP
                   .ExecuteAsync())
                .ConfigureAwait(false);

            Assert.AreEqual(MsalError.MtlsCertificateNotProvided, ex.ErrorCode);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task MtlsPop_WithoutRegion_UsesGlobalMtlsEndpoint(bool setAzureRegion)
        {
            string globalEndpoint = "mtlsauth.microsoft.com";
            string expectedTokenEndpoint = $"https://{globalEndpoint}/{TestConstants.TenantId}/oauth2/v2.0/token";

            using (var envContext = new EnvVariableContext())
            {
                Environment.SetEnvironmentVariable("REGION_NAME", null);
                Environment.SetEnvironmentVariable("MSAL_FORCE_REGION", null);

                using (var httpManager = new MockHttpManager())
                {
                    httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                        tokenType: "mtls_pop");

                    ConfidentialClientApplicationBuilder builder = ConfidentialClientApplicationBuilder
                                    .Create(TestConstants.ClientId)
                                    .WithAuthority(TestConstants.AuthorityTenant)
                                    .WithCertificate(s_testCertificate)
                                    .WithHttpManager(httpManager);

                    if (setAzureRegion)
                    {
                        builder = builder.WithAzureRegion(ConfidentialClientApplicationBuilder.DisableForceRegion);
                    }

                    IConfidentialClientApplication app = builder.Build();

                    AuthenticationResult result = await app.AcquireTokenForClient(TestConstants.s_scope)
                        .WithMtlsProofOfPossession()
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    Assert.AreEqual("header.payload.signature", result.AccessToken);
                    Assert.AreEqual(Constants.MtlsPoPAuthHeaderPrefix, result.TokenType);
                    Assert.AreEqual(expectedTokenEndpoint, result.AuthenticationResultMetadata.TokenEndpoint);
                }
            }
        }

        [TestMethod]
        public void Constructor_ValidCertificate()
        {
            var scheme = new MtlsPopAuthenticationOperation(s_testCertificate);

            // Compute the expected KeyId using SHA-256 over the certificate DER (x5t#S256)
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
            // Compute the SHA-256 hash of the full DER-encoded certificate (x5t#S256, RFC 8705),
            // matching what ESTS/MSS bind the mTLS PoP token to.
            using (var sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(certificate.RawData);
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

        /// <summary>
        /// Repro for AADSTS500181 (CertificateValidationFailedTlsCertMismatch) in the two-leg mTLS PoP flow.
        ///
        /// MSAL keys the mTLS-PoP token cache with <see cref="CoreHelpers.ComputeX5tS256KeyId"/>, which hashes
        /// only the certificate's PUBLIC KEY. ESTS/MSS, however, bind and validate the token against the DER of
        /// the presented certificate (x5t#S256, RFC 8705).
        ///
        /// When the binding certificate is renewed with the SAME key but a new DER (serial/validity) — exactly
        /// what IMDS/KeyGuard produces when it reissues the cert over the same non-exportable key — the
        /// public-key-hash cache key is unchanged. MSAL therefore cannot tell the old cert from the renewed one
        /// and serves the STALE token (bound to the old cert's DER) while the renewed cert is on the wire,
        /// producing the certificate/assertion mismatch.
        ///
        /// Correct behavior: a same-key renewal is a DIFFERENT certificate and MUST cause a cache miss, re-minting
        /// a token bound to the renewed cert. This test FAILS on current code (second acquisition returns
        /// TokenSource.Cache with the cert-A-bound token) and PASSES once the cache key is derived from the DER.
        /// </summary>
        [TestMethod]
        public async Task MtlsPop_SameKeyCertRenewal_MustNotServeStaleCachedTokenAsync()
        {
            const string region = "eastus";

            // Two certificates that share ONE RSA key pair but differ in DER (validity + serial).
            // This models a same-key certificate renewal.
            using RSA sharedKey = RSA.Create(2048);

            X509Certificate2 certA = new CertificateRequest(
                    "CN=MtlsPopSameKeyRenewal", sharedKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
                .CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow.AddDays(30));

            X509Certificate2 certB = new CertificateRequest(
                    "CN=MtlsPopSameKeyRenewal", sharedKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
                .CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(60));

            try
            {
                // Precondition 1: identical public key (same key pair).
                CollectionAssert.AreEqual(
                    certA.GetPublicKey(), certB.GetPublicKey(),
                    "Test setup invalid: the two certificates must share the same public key.");

                // Precondition 2: different certificates (different DER => different x5t#S256 => different thumbprint).
                Assert.AreNotEqual(
                    certA.Thumbprint, certB.Thumbprint,
                    "Test setup invalid: the two certificates must be different (different DER).");

                // The binding certificate presented on the wire. Starts as cert A, then "renews" to cert B.
                X509Certificate2 currentCert = certA;

                using (var envContext = new EnvVariableContext())
                {
                    Environment.SetEnvironmentVariable("REGION_NAME", region);

                    using (var httpManager = new MockHttpManager())
                    {
                        // Distinct tokens per mint so we can prove exactly which one is served.
                        httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                            token: "token_bound_to_certA", tokenType: "mtls_pop");

                        var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                            .WithExperimentalFeatures()
                            .WithCertificate(_ => Task.FromResult(currentCert), new CertificateOptions())
                            .WithAuthority("https://login.microsoftonline.com/123456-1234-2345-1234561234")
                            .WithAzureRegion(ConfidentialClientApplication.AttemptRegionDiscovery)
                            .WithHttpManager(httpManager)
                            .BuildConcrete();

                        // ── Request 1: bind a PoP token to cert A. Hits the IdP and caches. ──
                        AuthenticationResult first = await app.AcquireTokenForClient(TestConstants.s_scope)
                            .WithMtlsProofOfPossession()
                            .ExecuteAsync()
                            .ConfigureAwait(false);

                        Assert.AreEqual(TokenSource.IdentityProvider, first.AuthenticationResultMetadata.TokenSource);
                        Assert.AreEqual("token_bound_to_certA", first.AccessToken);
                        Assert.AreEqual(certA.Thumbprint, first.BindingCertificate.Thumbprint);

                        // ── Same-key renewal: the platform reissues the binding cert over the same key. ──
                        currentCert = certB;

                        // A fresh IdP response is available for the (expected) cache miss on the renewed cert.
                        httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                            token: "token_bound_to_certB", tokenType: "mtls_pop");

                        // ── Request 2: cert B is now on the wire. ──
                        AuthenticationResult second = await app.AcquireTokenForClient(TestConstants.s_scope)
                            .WithMtlsProofOfPossession()
                            .ExecuteAsync()
                            .ConfigureAwait(false);

                        // ─────────────────────────────── The bug ───────────────────────────────
                        // Cert B has a different DER than cert A, so the cert-A-bound cached token is invalid for
                        // cert B. Correct behavior is a cache MISS and a re-mint bound to cert B. On current code,
                        // ComputeX5tS256KeyId hashes the public key (identical across the renewal), so the lookup
                        // HITS the cert-A-bound token and returns it from cache while cert B is on the wire —
                        // reproducing AADSTS500181.
                        Assert.AreEqual(
                            TokenSource.IdentityProvider,
                            second.AuthenticationResultMetadata.TokenSource,
                            "A same-key certificate renewal (same public key, different DER) MUST invalidate the " +
                            "cached mTLS-PoP token. Serving the stale cert-A-bound token while cert B is on the wire " +
                            "is the root cause of AADSTS500181 (CertificateValidationFailedTlsCertMismatch).");

                        Assert.AreEqual(
                            "token_bound_to_certB",
                            second.AccessToken,
                            "MSAL served the cert-A-bound token for a request presenting cert B.");
                    }
                }
            }
            finally
            {
                certA.Dispose();
                certB.Dispose();
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
                Assert.AreEqual(RegionOutcome.UserProvided, result.ApiEvent.RegionOutcome);
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
        public async Task MtlsPop_UsesGlobalEndpointWhenRegionAutoDetectFailsAsync()
        {
            string globalEndpoint = "mtlsauth.microsoft.com";
            string tenantId = "123456-1234-2345-1234561234";
            string expectedTokenEndpoint = $"https://{globalEndpoint}/{tenantId}/oauth2/v2.0/token";

            using (var envContext = new EnvVariableContext())
            {
                Environment.SetEnvironmentVariable("REGION_NAME", null);  // Ensure no region is set

                using (var httpManager = new MockHttpManager())
                using (var harness = new MockHttpAndServiceBundle())
                {
                    harness.ServiceBundle.Config.RetryPolicyFactory = new TestRetryPolicyFactory();

                    // for simplicity, return 404 so retry is not triggered
                    httpManager.AddRegionDiscoveryMockHandlerWithError(HttpStatusCode.NotFound);

                    httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                        tokenType: "mtls_pop");

                    ConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                        .WithCertificate(s_testCertificate)
                        .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
                        .WithAzureRegion(ConfidentialClientApplication.AttemptRegionDiscovery)
                        .WithHttpManager(httpManager)
                        .BuildConcrete();

                    AuthenticationResult result = await app.AcquireTokenForClient(TestConstants.s_scope)
                        .WithMtlsProofOfPossession()
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    Assert.AreEqual("header.payload.signature", result.AccessToken);
                    Assert.AreEqual(Constants.MtlsPoPAuthHeaderPrefix, result.TokenType);
                    Assert.AreEqual(expectedTokenEndpoint, result.AuthenticationResultMetadata.TokenEndpoint);
                }
            }
        }

        [TestMethod]
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

        [TestMethod]
        [DataRow("https://login.microsoftonline.com", TestConstants.Common, "Public Cloud")]
        [DataRow("https://login.microsoftonline.com", TestConstants.Organizations, "Public Cloud")]
        [DataRow("https://login.microsoftonline.us", TestConstants.Common, "Azure Government")]
        [DataRow("https://login.microsoftonline.us", TestConstants.Organizations, "Azure Government")]
        [DataRow("https://login.partner.microsoftonline.cn", TestConstants.Common, "Azure China")]
        [DataRow("https://login.partner.microsoftonline.cn", TestConstants.Organizations, "Azure China")]
        [DataRow("https://login.sovcloud-identity.fr", TestConstants.Common, "Azure Sovereign - France Bleu")]
        [DataRow("https://login.sovcloud-identity.fr", TestConstants.Organizations, "Azure Sovereign - France Bleu")]
        [DataRow("https://login.sovcloud-identity.de", TestConstants.Common, "Azure Sovereign - Germany Delos")]
        [DataRow("https://login.sovcloud-identity.de", TestConstants.Organizations, "Azure Sovereign - Germany Delos")]
        [DataRow("https://login.sovcloud-identity.sg", TestConstants.Common, "Azure Sovereign - Singapore Gov SG")]
        [DataRow("https://login.sovcloud-identity.sg", TestConstants.Organizations, "Azure Sovereign - Singapore Gov SG")]
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
                    MsalClientException ex = await Assert.ThrowsAsync<MsalClientException>(async () =>
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

        [TestMethod]
        [DataRow("login.microsoftonline.com", "mtlsauth.microsoft.com")]
        [DataRow("login.microsoftonline.us", "mtlsauth.microsoftonline.us")]
        [DataRow("login.partner.microsoftonline.cn", "mtlsauth.partner.microsoftonline.cn")]
        [DataRow("login.sovcloud-identity.fr", "mtlsauth.sovcloud-identity.fr")]
        [DataRow("login.sovcloud-identity.de", "mtlsauth.sovcloud-identity.de")]
        [DataRow("login.sovcloud-identity.sg", "mtlsauth.sovcloud-identity.sg")]
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
                    Assert.IsNull(result.AuthenticationResultMetadata.RegionDetails.AutoDetectionError);
                }
            }
        }

        [TestMethod]
        [DataRow("login.microsoftonline.com", "mtlsauth.microsoft.com")]
        [DataRow("login.microsoftonline.us", "mtlsauth.microsoftonline.us")]
        [DataRow("login.partner.microsoftonline.cn", "mtlsauth.partner.microsoftonline.cn")]
        [DataRow("login.sovcloud-identity.fr", "mtlsauth.sovcloud-identity.fr")]
        [DataRow("login.sovcloud-identity.de", "mtlsauth.sovcloud-identity.de")]
        [DataRow("login.sovcloud-identity.sg", "mtlsauth.sovcloud-identity.sg")]
        public async Task PublicAndSovereignCloud_NoRegion_UsesGlobalMtlsEndpoint_Async(string inputEnv, string expectedMtlsEnv)
        {
            string tenantId = "17b189bc-2b81-4ec5-aa51-3e628cbc931b";
            string authorityUrl = $"https://{inputEnv}/{tenantId}";
            string expectedTokenEndpoint = $"https://{expectedMtlsEnv}/{tenantId}/oauth2/v2.0/token";

            using (var envContext = new EnvVariableContext())
            {
                Environment.SetEnvironmentVariable("REGION_NAME", null);
                Environment.SetEnvironmentVariable("MSAL_FORCE_REGION", null);

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
                                        .WithAuthority(authorityUrl)
                                        .WithHttpManager(harness.HttpManager)
                                        .WithCertificate(s_testCertificate)
                                        .Build();

                    AuthenticationResult result = await app
                        .AcquireTokenForClient(TestConstants.s_scope)
                        .WithMtlsProofOfPossession()
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    Assert.AreEqual("header.payload.signature", result.AccessToken);
                    Assert.AreEqual(Constants.MtlsPoPAuthHeaderPrefix, result.TokenType);
                    Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                    Assert.AreEqual(expectedTokenEndpoint, result.AuthenticationResultMetadata.TokenEndpoint);

                    // Second token acquisition - should retrieve from cache
                    AuthenticationResult secondResult = await app.AcquireTokenForClient(TestConstants.s_scope)
                        .WithMtlsProofOfPossession()
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    Assert.AreEqual(TokenSource.Cache, secondResult.AuthenticationResultMetadata.TokenSource);
                }
            }
        }

        [TestMethod]
        public async Task MtlsPop_GlobalEndpoint_ValidateExpectedUrlAsync()
        {
            string tenantId = "123456-1234-2345-1234561234";
            string authorityUrl = $"https://login.microsoftonline.com/{tenantId}";
            string expectedTokenEndpoint = $"https://mtlsauth.microsoft.com/{tenantId}/oauth2/v2.0/token";

            using (var envContext = new EnvVariableContext())
            {
                Environment.SetEnvironmentVariable("REGION_NAME", null);
                Environment.SetEnvironmentVariable("MSAL_FORCE_REGION", null);

                using (var harness = new MockHttpAndServiceBundle())
                {
                    var tokenHttpCallHandler = new MockHttpMessageHandler()
                    {
                        ExpectedUrl = expectedTokenEndpoint,
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = CreateResponse(tokenType: "mtls_pop"),
                        ExpectedPostData = new Dictionary<string, string>
                        {
                            { OAuth2Parameter.ClientId, "d3adb33f-c0de-ed0c-c0de-deadb33fc0d3" },
                            { OAuth2Parameter.Scope, TestConstants.s_scope.AsSingleString() },
                            { OAuth2Parameter.GrantType, OAuth2GrantType.ClientCredentials },
                            { "token_type", "mtls_pop" }
                        }
                    };

                    harness.HttpManager.AddMockHandler(tokenHttpCallHandler);

                    var app = ConfidentialClientApplicationBuilder
                                 .Create(TestConstants.ClientId)
                                 .WithAuthority(authorityUrl)
                                 .WithHttpManager(harness.HttpManager)
                                 .WithCertificate(s_testCertificate)
                                 .Build();

                    var result = await app.AcquireTokenForClient(TestConstants.s_scope)
                        .WithMtlsProofOfPossession()
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    Assert.IsNotNull(result.AccessToken);
                    Assert.AreEqual(Constants.MtlsPoPAuthHeaderPrefix, result.TokenType);
                    Assert.AreEqual(expectedTokenEndpoint, result.AuthenticationResultMetadata.TokenEndpoint);
                }
            }
        }

        [TestMethod]
        public async Task MtlsPop_GlobalEndpoint_NonStandardCloudAsync()
        {
            string nonStandardAuthority = "https://login.myLocalAAD.com/123456-1234-2345-1234561234";
            string mtlsSubdomain = "mtlsauth";
            string expectedTokenEndpoint = $"https://{mtlsSubdomain}.mylocalaad.com/123456-1234-2345-1234561234/oauth2/v2.0/token";

            using (var envContext = new EnvVariableContext())
            {
                Environment.SetEnvironmentVariable("REGION_NAME", null);
                Environment.SetEnvironmentVariable("MSAL_FORCE_REGION", null);

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
                                    .WithCertificate(s_testCertificate)
                                    .WithInstanceDiscovery(false)
                                    .Build();

                    AuthenticationResult result = await app
                        .AcquireTokenForClient(TestConstants.s_scope)
                        .WithMtlsProofOfPossession()
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    Assert.IsNotNull(result);
                    Assert.AreEqual("header.payload.signature", result.AccessToken);
                    Assert.AreEqual(Constants.MtlsPoPAuthHeaderPrefix, result.TokenType);
                    Assert.AreEqual(expectedTokenEndpoint, result.AuthenticationResultMetadata.TokenEndpoint);
                }
            }
        }

        [TestMethod]
        [DataRow("login.usgovcloudapi.net", MsalErrorMessage.MtlsPopNotSupportedForUsGovCloudApiMessage)]
        [DataRow("login.chinacloudapi.cn", MsalErrorMessage.MtlsPopNotSupportedForChinaCloudApiMessage)]
        public async Task UnsupportedSovereignHosts_ThrowsMsalClientException_Async(string unsupportedHost, string expectedErrorMessage)
        {
            // Arrange
            string authorityUrl = $"https://{unsupportedHost}/17b189bc-2b81-4ec5-aa51-3e628cbc931b";

            using (var envContext = new EnvVariableContext())
            {
                Environment.SetEnvironmentVariable("REGION_NAME", EastUsRegion);

                using (var harness = new MockHttpAndServiceBundle())
                {
                    var app = ConfidentialClientApplicationBuilder
                                        .Create(TestConstants.ClientId)
                                        .WithAuthority(authorityUrl)
                                        .WithHttpManager(harness.HttpManager)
                                        .WithAzureRegion(ConfidentialClientApplication.AttemptRegionDiscovery)
                                        .WithCertificate(s_testCertificate)
                                        .Build();

                    // Act & Assert
                    var exception = await Assert.ThrowsAsync<MsalClientException>(async () =>
                    {
                        await app.AcquireTokenForClient(TestConstants.s_scope)
                            .WithMtlsProofOfPossession()
                            .ExecuteAsync()
                            .ConfigureAwait(false);
                    }).ConfigureAwait(false);

                    Assert.AreEqual(MsalError.MtlsPopNotSupportedForEnvironment, exception.ErrorCode);
                    Assert.AreEqual(expectedErrorMessage, exception.Message);
                }
            }
        }

        [TestMethod]
        [DataRow("mtlsauth.microsoft.com")]
        [DataRow("sts.windows.net")]
        [DataRow("graph.microsoft.com")]
        public async Task NonLoginHosts_ThrowsMsalClientException_Async(string nonLoginHost)
        {
            // Arrange
            string authorityUrl = $"https://{nonLoginHost}/17b189bc-2b81-4ec5-aa51-3e628cbc931b";

            using (var envContext = new EnvVariableContext())
            {
                Environment.SetEnvironmentVariable("REGION_NAME", EastUsRegion);

                using (var harness = new MockHttpAndServiceBundle())
                {
                    var app = ConfidentialClientApplicationBuilder
                                        .Create(TestConstants.ClientId)
                                        .WithAuthority(authorityUrl)
                                        .WithHttpManager(harness.HttpManager)
                                        .WithAzureRegion(ConfidentialClientApplication.AttemptRegionDiscovery)
                                        .WithCertificate(s_testCertificate)
                                        .Build();

                    // Act & Assert
                    var exception = await Assert.ThrowsAsync<MsalClientException>(async () =>
                    {
                        await app.AcquireTokenForClient(TestConstants.s_scope)
                            .WithMtlsProofOfPossession()
                            .ExecuteAsync()
                            .ConfigureAwait(false);
                    }).ConfigureAwait(false);

                    Assert.AreEqual(MsalError.MtlsPopNotSupportedForEnvironment, exception.ErrorCode);
                    Assert.AreEqual(MsalErrorMessage.MtlsPopNotSupportedForNonLoginHostMessage, exception.Message);
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
                Assert.IsNull(result.AuthenticationResultMetadata.RegionDetails.RegionUsed);
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

        [TestMethod]
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

        #region SNI trust path and S2S FIC carry-over

        [TestMethod]
        public async Task MtlsPop_SniSendX5C_OmitsClientAssertionAndReqCnfAsync()
        {
            // Vanilla SNI over mTLS PoP: the cert is presented on the TLS connection and ESTS
            // resolves Subject Name + Issuer trust from the TLS-presented cert. Even with sendX5C:true,
            // the mTLS body must carry NO client_assertion / client_assertion_type / req_cnf — the cert
            // (cnf / x5t#S256) is the binding, not a signed assertion.
            string authorityUrl = "https://login.microsoftonline.com/123456-1234-2345-1234561234";

            using (var envContext = new EnvVariableContext())
            {
                Environment.SetEnvironmentVariable("REGION_NAME", EastUsRegion);

                using (var harness = new MockHttpAndServiceBundle())
                {
                    var tokenHttpCallHandler = new MockHttpMessageHandler()
                    {
                        ExpectedUrl = $"https://{EastUsRegion}.mtlsauth.microsoft.com/123456-1234-2345-1234561234/oauth2/v2.0/token",
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = CreateResponse(tokenType: "mtls_pop"),
                        ExpectedPostData = new Dictionary<string, string>
                        {
                            { OAuth2Parameter.ClientId, TestConstants.ClientId },
                            { OAuth2Parameter.GrantType, OAuth2GrantType.ClientCredentials },
                            { "token_type", "mtls_pop" }
                        },
                        UnExpectedPostData = new Dictionary<string, string>
                        {
                            { "client_assertion", "n/a" },
                            { "client_assertion_type", "n/a" },
                            { OAuth2Parameter.RequestConfirmation, "n/a" }
                        }
                    };

                    harness.HttpManager.AddMockHandler(tokenHttpCallHandler);

                    var app = ConfidentialClientApplicationBuilder
                                 .Create(TestConstants.ClientId)
                                 .WithAuthority(authorityUrl)
                                 .WithHttpManager(harness.HttpManager)
                                 .WithAzureRegion(ConfidentialClientApplication.AttemptRegionDiscovery)
                                 .WithCertificate(s_testCertificate, sendX5C: true)
                                 .Build();

                    AuthenticationResult result = await app.AcquireTokenForClient(TestConstants.s_scope)
                        .WithMtlsProofOfPossession()
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    Assert.AreEqual(Constants.MtlsPoPAuthHeaderPrefix, result.TokenType);
                    Assert.IsNotNull(result.BindingCertificate);
                    Assert.AreEqual(s_testCertificate.Thumbprint, result.BindingCertificate.Thumbprint);
                }
            }
        }

        [TestMethod]
        public async Task MtlsPop_S2sFic_ClientAssertionCarryOver_SendsJwtPopAndBindsToCarriedCertAsync()
        {
            // S2S (app) FIC "Leg 2" over mTLS PoP: the caller supplies the Leg-1 federated assertion
            // together with the Leg-1 binding certificate via ClientSignedAssertion.TokenBindingCertificate.
            // MSAL must forward the assertion as client_assertion, set client_assertion_type to jwt-pop,
            // present the carried cert on the mTLS connection, and return a token bound to that cert.
            const string leg1Assertion = "eyLeg1.federated.assertion";
            var carriedCert = CertHelper.GetOrCreateTestCert(regenerateCert: true);
            Assert.AreNotEqual(s_testCertificate.Thumbprint, carriedCert.Thumbprint);

            string authorityUrl = "https://login.microsoftonline.com/123456-1234-2345-1234561234";

            using (var envContext = new EnvVariableContext())
            {
                Environment.SetEnvironmentVariable("REGION_NAME", EastUsRegion);

                using (var harness = new MockHttpAndServiceBundle())
                {
                    var tokenHttpCallHandler = new MockHttpMessageHandler()
                    {
                        ExpectedUrl = $"https://{EastUsRegion}.mtlsauth.microsoft.com/123456-1234-2345-1234561234/oauth2/v2.0/token",
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = CreateResponse(tokenType: "mtls_pop"),
                        ExpectedPostData = new Dictionary<string, string>
                        {
                            { OAuth2Parameter.ClientId, TestConstants.ClientId },
                            { OAuth2Parameter.GrantType, OAuth2GrantType.ClientCredentials },
                            { "token_type", "mtls_pop" },
                            { "client_assertion", leg1Assertion },
                            { "client_assertion_type", OAuth2AssertionType.JwtPop }
                        }
                    };

                    harness.HttpManager.AddMockHandler(tokenHttpCallHandler);

                    var app = ConfidentialClientApplicationBuilder
                                 .Create(TestConstants.ClientId)
                                 .WithAuthority(authorityUrl)
                                 .WithHttpManager(harness.HttpManager)
                                 .WithAzureRegion(ConfidentialClientApplication.AttemptRegionDiscovery)
                                 .WithClientAssertion((AssertionRequestOptions _, CancellationToken _) =>
                                     Task.FromResult(new ClientSignedAssertion
                                     {
                                         Assertion = leg1Assertion,
                                         TokenBindingCertificate = carriedCert
                                     }))
                                 .Build();

                    AuthenticationResult result = await app.AcquireTokenForClient(TestConstants.s_scope)
                        .WithMtlsProofOfPossession()
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    Assert.AreEqual(Constants.MtlsPoPAuthHeaderPrefix, result.TokenType);
                    Assert.IsNotNull(result.BindingCertificate);
                    Assert.AreEqual(carriedCert.Thumbprint, result.BindingCertificate.Thumbprint,
                        "The final token must be bound to the carried Leg-1 certificate.");
                }
            }
        }

        [TestMethod]
        public void MtlsPop_DefaultHttpClientFactory_IsMtlsCapable_TransportOwnedByMsal()
        {
            // mTLS requires MSAL to own the transport handler so it can attach the client certificate.
            // MSAL's default factory must be mTLS-capable (IMsalMtlsHttpClientFactory); a plain
            // caller-supplied IMsalHttpClientFactory cannot carry the mTLS cert.
            IMsalHttpClientFactory defaultFactory =
                Microsoft.Identity.Client.PlatformsCommon.Factories.PlatformProxyFactory
                    .CreatePlatformProxy(null)
                    .CreateDefaultHttpClientFactory();

            Assert.IsInstanceOfType(defaultFactory, typeof(IMsalMtlsHttpClientFactory),
                "MSAL's default HTTP transport must be mTLS-capable so it can present the client certificate " +
                "on the mutual-TLS connection to the token endpoint.");
        }

        #endregion

        #region SendCertificateOverMtls tests

        [TestMethod]
        public async Task SendCertificateOverMtls_NoRegion_UsesGlobalMtlsEndpointAsync()
        {
            // Since region is no longer required for mTLS (global endpoint is used as fallback),
            // SendCertificateOverMtls=true without WithAzureRegion should succeed.
            string globalEndpoint = "mtlsauth.microsoft.com";
            string expectedTokenEndpoint = $"https://{globalEndpoint}/123456-1234-2345-1234561234/oauth2/v2.0/token";

            using (var envContext = new EnvVariableContext())
            {
                Environment.SetEnvironmentVariable("REGION_NAME", null);
                Environment.SetEnvironmentVariable("MSAL_FORCE_REGION", null);

                using (var httpManager = new MockHttpManager())
                {
                    httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(tokenType: "Bearer");

                    var options = new CertificateOptions
                    {
                        SendCertificateOverMtls = true
                    };

                    var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                        .WithCertificate(s_testCertificate, options)
                        .WithAuthority("https://login.microsoftonline.com/123456-1234-2345-1234561234")
                        .WithHttpManager(httpManager)
                        .Build();

                    AuthenticationResult result = await app.AcquireTokenForClient(TestConstants.s_scope)
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    Assert.AreEqual("Bearer", result.TokenType,
                        "SendCertificateOverMtls without WithMtlsProofOfPossession should produce a Bearer token.");
                    Assert.AreEqual(expectedTokenEndpoint, result.AuthenticationResultMetadata.TokenEndpoint,
                        "Should use global mTLS endpoint when no region is configured.");
                }
            }
        }

        [TestMethod]
        public async Task SendCertificateOverMtls_WithRegion_AcquiresBearerTokenAsync()
        {
            const string region = EastUsRegion;

            using (var envContext = new EnvVariableContext())
            {
                Environment.SetEnvironmentVariable("REGION_NAME", region);

                using (var httpManager = new MockHttpManager())
                {
                    httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(tokenType: "Bearer");

                    var options = new CertificateOptions
                    {
                        SendCertificateOverMtls = true
                    };

                    var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                        .WithCertificate(s_testCertificate, options)
                        .WithAuthority("https://login.microsoftonline.com/123456-1234-2345-1234561234")
                        .WithAzureRegion(ConfidentialClientApplication.AttemptRegionDiscovery)
                        .WithHttpManager(httpManager)
                        .BuildConcrete();

                    AuthenticationResult result = await app.AcquireTokenForClient(TestConstants.s_scope)
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    Assert.AreEqual("Bearer", result.TokenType,
                        "SendCertificateOverMtls without WithMtlsProofOfPossession should produce a Bearer token.");
                    Assert.AreEqual(region, result.AuthenticationResultMetadata.RegionDetails.RegionUsed,
                        "Token should be acquired from the regional endpoint.");
                }
            }
        }

        [TestMethod]
        public async Task SendCertificateOverMtls_WithPopRequest_StillProducesPopTokenAsync()
        {
            const string region = EastUsRegion;

            using (var envContext = new EnvVariableContext())
            {
                Environment.SetEnvironmentVariable("REGION_NAME", region);

                using (var httpManager = new MockHttpManager())
                {
                    httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(tokenType: "mtls_pop");

                    var options = new CertificateOptions
                    {
                        SendCertificateOverMtls = true
                    };

                    var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                        .WithCertificate(s_testCertificate, options)
                        .WithAuthority("https://login.microsoftonline.com/123456-1234-2345-1234561234")
                        .WithAzureRegion(ConfidentialClientApplication.AttemptRegionDiscovery)
                        .WithHttpManager(httpManager)
                        .BuildConcrete();

                    AuthenticationResult result = await app.AcquireTokenForClient(TestConstants.s_scope)
                        .WithMtlsProofOfPossession()
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    Assert.AreEqual(Constants.MtlsPoPAuthHeaderPrefix, result.TokenType,
                        "WithMtlsProofOfPossession() must produce an mTLS PoP token even when SendCertificateOverMtls=true.");
                    Assert.IsNotNull(result.BindingCertificate,
                        "BindingCertificate should be present for mTLS PoP.");
                }
            }
        }

        [TestMethod]
        public async Task SendCertificateOverMtls_WithClientClaims_ThrowsClearMessageAsync()
        {
            // Regression test for the misconfiguration where an app combines:
            //   .WithCertificate(cert, new CertificateOptions { SendCertificateOverMtls = true })
            //   .WithClientClaims(cert, claims)   // overwrites credential, keeps options
            // and does NOT call .WithMtlsProofOfPossession().
            //
            // ConfidentialClientApplicationBuilder.Validate() allows this combo (the credential is
            // still a CertificateAndClaimsClientCredential, so the cert-only guard passes). At token
            // request time, TryInitImplicitBearerOverMtlsAsync.Case 1 fires on SendCertificateOverMtls
            // and asks the credential for material in mTLS mode, which trips the
            // _claimsToSign != null guard in CertificateAndClaimsClientCredential.
            //
            // The message must NOT falsely blame Proof-of-Possession — the user never requested PoP.
            // It must name both transports (PoP and SendCertificateOverMtls) and the WithClientClaims
            // incompatibility so the diagnostic is actionable.
            var ipAddress = new Dictionary<string, string>
            {
                { "client_ip", "192.168.1.2" }
            };

            var options = new CertificateOptions { SendCertificateOverMtls = true };

#pragma warning disable CS0618 // WithClientClaims is obsolete
            IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithCertificate(s_testCertificate, options)
                .WithClientClaims(s_testCertificate, ipAddress)
                .WithAuthority("https://login.microsoftonline.com/123456-1234-2345-1234561234")
                .Build();
#pragma warning restore CS0618

            // No .WithMtlsProofOfPossession() — Bearer-over-mTLS path only.
            MsalClientException ex = await Assert.ThrowsAsync<MsalClientException>(() =>
                app.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync())
                .ConfigureAwait(false);

            Assert.AreEqual(MsalError.MtlsCertificateNotProvided, ex.ErrorCode);

            // The diagnostic must name both transports and the offending API.
            StringAssert.Contains(ex.Message, "WithClientClaims");
            StringAssert.Contains(ex.Message, "SendCertificateOverMtls");
            StringAssert.Contains(ex.Message, "Proof-of-Possession");
        }

        #endregion
    }
}
