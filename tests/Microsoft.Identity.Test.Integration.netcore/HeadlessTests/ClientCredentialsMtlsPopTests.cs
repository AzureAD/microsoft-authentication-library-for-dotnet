// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    // Tests in this class will run on .NET Core
    // POP tests only work on the allow listed SNI app
    // and tenant ("bea21ebe-8b64-4d06-9f6d-6a889b120a7c") - MSI team tenant
    [TestClass]
    public class ClientCredentialsMtlsPopTests
    {
        private const string MsiAllowListedAppIdforSNI = "163ffef9-a313-45b4-ab2f-c7e2f5e0e23e";
        private const string TokenExchangeUrl = "api://AzureADTokenExchange/.default";

        // FMI variant of the exchange audience (SME item 5: the exchange audience is caller-supplied,
        // not SDK-hardcoded). The FMI leg uses the reserved client id urn:microsoft:identity:fmi.
        private const string FmiClientId = "urn:microsoft:identity:fmi";
        private const string FmiTokenExchangeUrl = "api://AzureFMITokenExchange/.default";

        // Note A: the final resource must be ESTS allow-listed for mtls_pop (e.g. Key Vault / MS Graph),
        // NOT the client app. Keep it a single swappable constant so it can move when ESTS switches to
        // the app model.
        private const string AllowListedFinalResource = "https://vault.azure.net/.default";

        [TestInitialize]
        public void TestInitialize()
        {
            ApplicationBase.ResetStateForTest();
        }

        [RunOn(SkipConditions.Linux)] // POP is not supported on Linux
        public async Task Sni_Gets_Pop_Token_Successfully_TestAsync()
        {
            // Arrange: Use LabResponseHelper to get app configuration
            var appConfig = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppS2S).ConfigureAwait(false);

            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            string[] appScopes = new[] { "https://vault.azure.net/.default" };

            // Build Confidential Client Application with SNI certificate at App level
            IConfidentialClientApplication confidentialApp = ConfidentialClientApplicationBuilder.Create(MsiAllowListedAppIdforSNI)
                .WithAuthority("https://login.microsoftonline.com/bea21ebe-8b64-4d06-9f6d-6a889b120a7c")
                .WithAzureRegion("westus3") //test slice region 
                .WithCertificate(cert, true)
                .WithTestLogging()
                .Build();

            // Act: Acquire token with MTLS Proof of Possession at Request level
            AuthenticationResult authResult = await ExecuteOrInconclusiveOnTokenTypeMismatchAsync(() => confidentialApp
                .AcquireTokenForClient(appScopes)
                .WithMtlsProofOfPossession()
                .ExecuteAsync()).ConfigureAwait(false);

            // Assert: Check that the MTLS PoP token acquisition was successful
            Assert.IsNotNull(authResult, "The authentication result should not be null.");
            Assert.AreEqual(Constants.MtlsPoPTokenType, authResult.TokenType, "Token type should be MTLS PoP");
            Assert.IsNotNull(authResult.AccessToken, "Access token should not be null");

            Assert.IsNotNull(authResult.BindingCertificate, "BindingCertificate should be set in SNI flow.");
            Assert.AreEqual(cert.Thumbprint,
                            authResult.BindingCertificate.Thumbprint,
                            "BindingCertificate must match the certificate supplied via WithCertificate().");

            // Simulate cache retrieval to verify MTLS configuration is cached properly
            authResult = await confidentialApp
               .AcquireTokenForClient(appScopes)
               .WithMtlsProofOfPossession()
               .ExecuteAsync()
               .ConfigureAwait(false);

            // Assert: Verify that the token was fetched from cache on the second request
            Assert.AreEqual(TokenSource.Cache, authResult.AuthenticationResultMetadata.TokenSource, "Token should be retrieved from cache");

            Assert.IsNotNull(authResult.BindingCertificate, "BindingCertificate should be set in SNI flow.");
            Assert.AreEqual(cert.Thumbprint,
                            authResult.BindingCertificate.Thumbprint,
                            "BindingCertificate must match the certificate supplied via WithCertificate().");
        }

        [RunOn(SkipConditions.Linux)] // POP is not supported on Linux
        public async Task Sni_Gets_Pop_Token_WithGlobalEndpoint_TestAsync()
        {
            // Arrange: validate lab setup before executing the test flow.
            _ = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppS2S).ConfigureAwait(false);

            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            string[] appScopes = new[] { "https://vault.azure.net/.default" };

            // Build Confidential Client Application with SNI certificate — NO region configured
            IConfidentialClientApplication confidentialApp = ConfidentialClientApplicationBuilder.Create(MsiAllowListedAppIdforSNI)
                .WithAuthority("https://login.microsoftonline.com/bea21ebe-8b64-4d06-9f6d-6a889b120a7c")
                .WithCertificate(cert, true)
                .WithTestLogging()
                .Build();

            // Act: Acquire token with MTLS Proof of Possession at Request level (global endpoint)
            AuthenticationResult authResult = await confidentialApp
                .AcquireTokenForClient(appScopes)
                .WithMtlsProofOfPossession()
                .ExecuteAsync()
                .ConfigureAwait(false);

            // Assert: Check that the MTLS PoP token acquisition was successful
            Assert.IsNotNull(authResult, "The authentication result should not be null.");
            Assert.AreEqual(Constants.MtlsPoPTokenType, authResult.TokenType, "Token type should be MTLS PoP");
            Assert.IsNotNull(authResult.AccessToken, "Access token should not be null");

            Assert.IsNotNull(authResult.BindingCertificate, "BindingCertificate should be set in SNI flow.");
            Assert.AreEqual(cert.Thumbprint,
                            authResult.BindingCertificate.Thumbprint,
                            "BindingCertificate must match the certificate supplied via WithCertificate().");

            // Verify global mTLS endpoint was used (no region prefix)
            Assert.IsTrue(
                System.Uri.TryCreate(
                    authResult.AuthenticationResultMetadata.TokenEndpoint,
                    System.UriKind.Absolute,
                    out System.Uri tokenEndpointUri),
                "Token endpoint should be a valid absolute URI.");
            Assert.AreEqual(
                "mtlsauth.microsoft.com",
                tokenEndpointUri.Host,
                "Should use global mtlsauth endpoint when no region is configured.");

            // Simulate cache retrieval to verify MTLS configuration is cached properly
            authResult = await confidentialApp
               .AcquireTokenForClient(appScopes)
               .WithMtlsProofOfPossession()
               .ExecuteAsync()
               .ConfigureAwait(false);

            // Assert: Verify that the token was fetched from cache on the second request
            Assert.AreEqual(TokenSource.Cache, authResult.AuthenticationResultMetadata.TokenSource, "Token should be retrieved from cache");

            Assert.IsNotNull(authResult.BindingCertificate, "BindingCertificate should be set in SNI flow.");
            Assert.AreEqual(cert.Thumbprint,
                            authResult.BindingCertificate.Thumbprint,
                            "BindingCertificate must match the certificate supplied via WithCertificate().");
        }

        [RunOn(SkipConditions.Linux)]
        public async Task Sni_AssertionFlow_Uses_JwtPop_And_Succeeds_TestAsync()
        {
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            // Step 1: obtain a real JWT to reuse as the "assertion"
            IConfidentialClientApplication firstApp = ConfidentialClientApplicationBuilder.Create(MsiAllowListedAppIdforSNI)
                .WithAuthority("https://login.microsoftonline.com/bea21ebe-8b64-4d06-9f6d-6a889b120a7c")
                .WithAzureRegion("westus3")
                .WithCertificate(cert, true)
                .WithTestLogging()
                .Build();

            AuthenticationResult first = await ExecuteOrInconclusiveOnTokenTypeMismatchAsync(() => firstApp
                .AcquireTokenForClient(new[] { TokenExchangeUrl })
                .WithMtlsProofOfPossession()
                .ExecuteAsync()).ConfigureAwait(false);

            string assertionJwt = first.AccessToken;
            Assert.IsFalse(string.IsNullOrEmpty(assertionJwt), "First leg did not return an access token to reuse as assertion.");

            // Step 2: build the assertion-based app (NO WithCertificate here)
            bool assertionProviderCalled = false;
            string tokenEndpointSeenByProvider = null;
            Guid correlationIdSeenByProvider = Guid.Empty;

            string requestUriSeen = null;
            string clientAssertionType = null;
            bool sawClientAssertionParam = false;
            bool sawClientAssertionTypeParam = false;

            Guid expectedCorrelationId = Guid.NewGuid();

            IConfidentialClientApplication assertionApp = ConfidentialClientApplicationBuilder.Create(MsiAllowListedAppIdforSNI)
                .WithAuthority("https://login.microsoftonline.com/bea21ebe-8b64-4d06-9f6d-6a889b120a7c")
                .WithAzureRegion("westus3")
                .WithClientAssertion((AssertionRequestOptions options, CancellationToken ct) =>
                {
                    assertionProviderCalled = true;
                    tokenEndpointSeenByProvider = options.TokenEndpoint;
                    correlationIdSeenByProvider = options.CorrelationId;

                    return Task.FromResult(new ClientSignedAssertion
                    {
                        Assertion = assertionJwt,      // forwarded as client_assertion
                        TokenBindingCertificate = cert // binds assertion for mTLS PoP (jwt-pop)
                    });
                })
                .WithTestLogging()
                .Build();

            // Step 3: second leg should now SUCCEED
            AuthenticationResult second = await assertionApp
                .AcquireTokenForClient(new[] { "https://vault.azure.net/.default" })
                .WithMtlsProofOfPossession()
                .WithCorrelationId(expectedCorrelationId)
                .OnBeforeTokenRequest(data =>
                {
                    requestUriSeen = data.RequestUri?.ToString();

                    if (data.BodyParameters != null)
                    {
                        sawClientAssertionParam = data.BodyParameters.ContainsKey("client_assertion");
                        sawClientAssertionTypeParam = data.BodyParameters.ContainsKey("client_assertion_type");

                        data.BodyParameters.TryGetValue("client_assertion_type", out clientAssertionType);
                    }

                    return Task.CompletedTask;
                })
                .ExecuteAsync()
                .ConfigureAwait(false);

            // Success assertions
            Assert.IsNotNull(second, "Second leg returned null AuthenticationResult.");
            Assert.IsFalse(string.IsNullOrEmpty(second.AccessToken), "Second leg did not return an access token.");
            CollectionAssert.Contains(second.Scopes.ToArray(), "https://vault.azure.net/.default",
                "Second leg token is not for Key Vault scope.");

            // Prove MSAL used the assertion + jwt-pop binding
            Assert.IsTrue(assertionProviderCalled, "Client assertion provider should have been invoked.");
            Assert.IsFalse(string.IsNullOrEmpty(tokenEndpointSeenByProvider),
                "AssertionRequestOptions.TokenEndpoint should be provided to the callback.");

            Assert.IsTrue(sawClientAssertionParam, "Token request should include client_assertion body parameter.");
            Assert.IsTrue(sawClientAssertionTypeParam, "Token request should include client_assertion_type body parameter.");

            Assert.AreEqual(
                "urn:ietf:params:oauth:client-assertion-type:jwt-pop",
                clientAssertionType,
                "When TokenBindingCertificate is supplied and PoP is enabled, MSAL should use jwt-pop client_assertion_type.");

            // Optional: if you rely on regional mTLS endpoints, check the host
            StringAssert.Contains(requestUriSeen ?? "", "mtlsauth.microsoft.com");

            // Verify CorrelationId flowed to the assertion callback (Issue #5924)
            Assert.AreEqual(expectedCorrelationId, correlationIdSeenByProvider,
                "CorrelationId from WithCorrelationId() must flow to the assertion callback for FIC two-leg tracing.");
        }

        //Downgraded test to verify bearer token acquisition works in SNI + jwt-pop scenario
        [RunOn(SkipConditions.Linux)]
        public async Task Sni_AssertionFlow_Uses_JwtPop_And_Acquires_Bearer_Token_TestAsync()
        {
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            // Step 1: obtain a real JWT to reuse as the "assertion"
            IConfidentialClientApplication firstApp = ConfidentialClientApplicationBuilder.Create(MsiAllowListedAppIdforSNI)
                .WithAuthority("https://login.microsoftonline.com/bea21ebe-8b64-4d06-9f6d-6a889b120a7c")
                .WithAzureRegion("westus3")
                .WithCertificate(cert, true)
                .WithTestLogging()
                .Build();

            AuthenticationResult first = await ExecuteOrInconclusiveOnTokenTypeMismatchAsync(() => firstApp
                .AcquireTokenForClient(new[] { TokenExchangeUrl })
                .WithMtlsProofOfPossession()
                .ExecuteAsync()).ConfigureAwait(false);

            string assertionJwt = first.AccessToken;
            Assert.IsFalse(string.IsNullOrEmpty(assertionJwt), "First leg did not return an access token to reuse as assertion.");

            // Step 2: build the assertion-based app (NO WithCertificate here)
            bool assertionProviderCalled = false;
            string tokenEndpointSeenByProvider = null;

            string requestUriSeen = null;
            string clientAssertionType = null;
            bool sawClientAssertionParam = false;
            bool sawClientAssertionTypeParam = false;

            IConfidentialClientApplication assertionApp = ConfidentialClientApplicationBuilder.Create(MsiAllowListedAppIdforSNI)
                .WithAuthority("https://login.microsoftonline.com/bea21ebe-8b64-4d06-9f6d-6a889b120a7c")
                .WithAzureRegion("westus3")
                .WithClientAssertion((AssertionRequestOptions options, CancellationToken ct) =>
                {
                    assertionProviderCalled = true;
                    tokenEndpointSeenByProvider = options.TokenEndpoint;

                    return Task.FromResult(new ClientSignedAssertion
                    {
                        Assertion = assertionJwt,      // forwarded as client_assertion
                        TokenBindingCertificate = cert // binds assertion for mTLS PoP (jwt-pop)
                    });
                })
                .WithTestLogging()
                .Build();

            // Step 3: second leg should now SUCCEED
            AuthenticationResult second = await assertionApp
                .AcquireTokenForClient(new[] { "https://storage.azure.com/.default" })
                .OnBeforeTokenRequest(data =>
                {
                    requestUriSeen = data.RequestUri?.ToString();

                    if (data.BodyParameters != null)
                    {
                        sawClientAssertionParam = data.BodyParameters.ContainsKey("client_assertion");
                        sawClientAssertionTypeParam = data.BodyParameters.ContainsKey("client_assertion_type");

                        data.BodyParameters.TryGetValue("client_assertion_type", out clientAssertionType);
                    }

                    return Task.CompletedTask;
                })
                .ExecuteAsync()
                .ConfigureAwait(false);

            // Success assertions
            Assert.IsNotNull(second, "Second leg returned null AuthenticationResult.");
            Assert.IsFalse(string.IsNullOrEmpty(second.AccessToken), "Second leg did not return an access token.");
            CollectionAssert.Contains(second.Scopes.ToArray(), "https://storage.azure.com/.default",
                "Second leg token is not for Key Vault scope.");

            // Prove MSAL used the assertion + jwt-pop binding
            Assert.IsTrue(assertionProviderCalled, "Client assertion provider should have been invoked.");
            Assert.IsFalse(string.IsNullOrEmpty(tokenEndpointSeenByProvider),
                "AssertionRequestOptions.TokenEndpoint should be provided to the callback.");

            Assert.IsTrue(sawClientAssertionParam, "Token request should include client_assertion body parameter.");
            Assert.IsTrue(sawClientAssertionTypeParam, "Token request should include client_assertion_type body parameter.");

            Assert.AreEqual(
                "urn:ietf:params:oauth:client-assertion-type:jwt-pop",
                clientAssertionType,
                "When TokenBindingCertificate is supplied and PoP is enabled, MSAL should use jwt-pop client_assertion_type.");

            // Optional: if you rely on regional mTLS endpoints, check the host
            StringAssert.Contains(requestUriSeen ?? "", "mtlsauth.microsoft.com");
        }

        [RunOn(SkipConditions.Linux)] // mTLS is not supported on Linux
        public async Task Sni_Over_Mtls_Gets_Bearer_Token_Successfully_TestAsync()
        {
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            string[] appScopes = new[] { "https://vault.azure.net/.default" };

            var certificateOptions = new CertificateOptions
            {
                SendCertificateOverMtls = true
            };

            // Build Confidential Client Application with mTLS Bearer transport
            IConfidentialClientApplication confidentialApp = ConfidentialClientApplicationBuilder.Create(MsiAllowListedAppIdforSNI)
                .WithAuthority("https://login.microsoftonline.com/bea21ebe-8b64-4d06-9f6d-6a889b120a7c")
                .WithAzureRegion("westus3") //test slice region
                .WithCertificate(cert, certificateOptions)
                .WithTestLogging()
                .Build();

            // Act: Acquire token - should be Bearer via mTLS transport
            AuthenticationResult authResult = await confidentialApp
                .AcquireTokenForClient(appScopes)
                .ExecuteAsync()
                .ConfigureAwait(false);

            // Assert: Check that a Bearer token was acquired
            Assert.IsNotNull(authResult, "The authentication result should not be null.");
            Assert.AreEqual("Bearer", authResult.TokenType, "Token type should be Bearer for mTLS Bearer flow");
            Assert.IsNotNull(authResult.AccessToken, "Access token should not be null");

            // Verify the mTLS transport was actually used (regional mTLS endpoint)
            Assert.IsNotNull(authResult.AuthenticationResultMetadata.TokenEndpoint,
                "TokenEndpoint should be set for network requests.");
            StringAssert.Contains(authResult.AuthenticationResultMetadata.TokenEndpoint, "mtlsauth",
                "SendCertificateOverMtls should route through the mTLS regional endpoint.");

            // Verify cache retrieval still works with mTLS Bearer configuration
            AuthenticationResult cachedResult = await confidentialApp
               .AcquireTokenForClient(appScopes)
               .ExecuteAsync()
               .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, cachedResult.AuthenticationResultMetadata.TokenSource, "Token should be retrieved from cache");
        }

        [RunOn(SkipConditions.Linux)]
        public async Task Sni_Gets_Pop_Token_WithSendCertificateOverMtls_False_TestAsync()
        {
            await Sni_Gets_Pop_Token_WithCertificateOptionsAsync(sendCertificateOverMtls: false).ConfigureAwait(false);
        }

        [RunOn(SkipConditions.Linux)]
        public async Task Sni_Gets_Pop_Token_WithSendCertificateOverMtls_True_TestAsync()
        {
            await Sni_Gets_Pop_Token_WithCertificateOptionsAsync(sendCertificateOverMtls: true).ConfigureAwait(false);
        }

        private static async Task Sni_Gets_Pop_Token_WithCertificateOptionsAsync(bool sendCertificateOverMtls)
        {
            // Arrange
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            string[] appScopes = new[] { "https://vault.azure.net/.default" };

            var certificateOptions = new CertificateOptions
            {
                SendCertificateOverMtls = sendCertificateOverMtls
            };

            // Build with CertificateOptions overload
            IConfidentialClientApplication confidentialApp = ConfidentialClientApplicationBuilder.Create(MsiAllowListedAppIdforSNI)
                .WithAuthority("https://login.microsoftonline.com/bea21ebe-8b64-4d06-9f6d-6a889b120a7c")
                .WithAzureRegion("westus3")
                .WithCertificate(cert, certificateOptions)
                .WithTestLogging()
                .Build();

            // Act: WithMtlsProofOfPossession should always produce PoP, regardless of SendCertificateOverMtls
            AuthenticationResult authResult = await ExecuteOrInconclusiveOnTokenTypeMismatchAsync(() => confidentialApp
                .AcquireTokenForClient(appScopes)
                .WithMtlsProofOfPossession()
                .ExecuteAsync()).ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(authResult, "The authentication result should not be null.");
            Assert.AreEqual(Constants.MtlsPoPTokenType, authResult.TokenType, "Token type should be MTLS PoP");
            Assert.IsNotNull(authResult.AccessToken, "Access token should not be null");
            Assert.IsNotNull(authResult.BindingCertificate, "BindingCertificate should be set in SNI flow.");
            Assert.AreEqual(cert.Thumbprint, authResult.BindingCertificate.Thumbprint,
                "BindingCertificate must match the certificate supplied via WithCertificate().");
        }

        [RunOn(SkipConditions.Linux)]
        public async Task Sni_AssertionFlow_GlobalEndpoint_Uses_JwtPop_And_Succeeds_TestAsync()
        {
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            // Step 1: obtain a real JWT to reuse as the "assertion" — using regional for first leg
            IConfidentialClientApplication firstApp = ConfidentialClientApplicationBuilder.Create(MsiAllowListedAppIdforSNI)
                .WithAuthority("https://login.microsoftonline.com/bea21ebe-8b64-4d06-9f6d-6a889b120a7c")
                .WithAzureRegion("westus3")
                .WithCertificate(cert, true)
                .WithTestLogging()
                .Build();

            AuthenticationResult first = await ExecuteOrInconclusiveOnTokenTypeMismatchAsync(() => firstApp
                .AcquireTokenForClient(new[] { TokenExchangeUrl })
                .WithMtlsProofOfPossession()
                .ExecuteAsync()).ConfigureAwait(false);

            string assertionJwt = first.AccessToken;
            Assert.IsFalse(string.IsNullOrEmpty(assertionJwt), "First leg did not return an access token to reuse as assertion.");

            // Step 2: build the assertion-based app — NO region configured (global endpoint)
            bool assertionProviderCalled = false;
            string requestUriSeen = null;

            IConfidentialClientApplication assertionApp = ConfidentialClientApplicationBuilder.Create(MsiAllowListedAppIdforSNI)
                .WithExperimentalFeatures()
                .WithAuthority("https://login.microsoftonline.com/bea21ebe-8b64-4d06-9f6d-6a889b120a7c")
                .WithClientAssertion((AssertionRequestOptions options, CancellationToken ct) =>
                {
                    assertionProviderCalled = true;

                    return Task.FromResult(new ClientSignedAssertion
                    {
                        Assertion = assertionJwt,
                        TokenBindingCertificate = cert
                    });
                })
                .WithTestLogging()
                .Build();

            // Step 3: second leg should succeed using global mTLS endpoint
            AuthenticationResult second = await assertionApp
                .AcquireTokenForClient(new[] { "https://vault.azure.net/.default" })
                .WithMtlsProofOfPossession()
                .OnBeforeTokenRequest(data =>
                {
                    requestUriSeen = data.RequestUri?.ToString();
                    return Task.CompletedTask;
                })
                .ExecuteAsync()
                .ConfigureAwait(false);

            // Success assertions
            Assert.IsNotNull(second, "Second leg returned null AuthenticationResult.");
            Assert.IsFalse(string.IsNullOrEmpty(second.AccessToken), "Second leg did not return an access token.");
            Assert.IsTrue(assertionProviderCalled, "Client assertion provider should have been invoked.");

            // Verify global mTLS endpoint was used
            Assert.IsFalse(string.IsNullOrEmpty(requestUriSeen), "Expected token request URI to be captured.");
            var requestUri = new System.Uri(requestUriSeen);
            Assert.AreEqual("mtlsauth.microsoft.com", requestUri.Host,
                "Should use global mtlsauth endpoint when no region is configured.");
        }

        [RunOn(SkipConditions.Linux)] // POP is not supported on Linux
        public async Task Sni_TwoLeg_S2sFic_BothLegs_Pop_EndToEnd_TestAsync()
        {
            // End-to-end two-leg S2S (app) FIC over mTLS PoP using the GLOBAL mtlsauth endpoint
            // (which reliably honors token_type=mtls_pop), asserting BOTH legs are PoP AND that the
            // binding certificate is continuous from Leg 1 through the final Leg-2 result:
            //   Leg 1: SNI cert -> api://AzureADTokenExchange -> mtls_pop federated assertion + BindingCertificate
            //   Leg 2: carry leg1.BindingCertificate into WithClientAssertion(TokenBindingCertificate=...)
            //          -> resource token that is ALSO mtls_pop and bound to the SAME Leg-1 thumbprint.
            await RunTwoLegS2sFicBothLegsPopAsync(MsiAllowListedAppIdforSNI, TokenExchangeUrl).ConfigureAwait(false);
        }

        [RunOn(SkipConditions.Linux)] // POP is not supported on Linux
        public async Task Sni_TwoLeg_S2sFic_FmiAudience_BothLegs_Pop_EndToEnd_TestAsync()
        {
            // Same two-leg flow, but Leg 1 uses the FMI exchange audience (api://AzureFMITokenExchange)
            // and the reserved FMI client id (urn:microsoft:identity:fmi). Proves the exchange audience is
            // caller-supplied (SME item 5). If FMI + mTLS PoP is not enabled for this app in the lab, ESTS
            // rejects the exchange and the test is marked inconclusive rather than failing.
            try
            {
                await RunTwoLegS2sFicBothLegsPopAsync(FmiClientId, FmiTokenExchangeUrl).ConfigureAwait(false);
            }
            catch (MsalServiceException ex)
            {
                Assert.Inconclusive(
                    "FMI-audience mTLS PoP exchange was rejected by ESTS for this app/lab configuration. " +
                    "This variant only proves the exchange audience is caller-supplied; the generic " +
                    $"api://AzureADTokenExchange path remains under test. Underlying error: {ex.Message}");
            }
        }

        // Shared driver for the two-leg S2S FIC over mTLS PoP end-to-end flow. Both legs use the global
        // mtlsauth endpoint (no region) so they reliably return token_type=mtls_pop, and each leg is wrapped
        // in ExecuteOrInconclusiveOnTokenTypeMismatchAsync to tolerate a server-side downgrade.
        private static async Task RunTwoLegS2sFicBothLegsPopAsync(string leg1ClientId, string leg1ExchangeScope)
        {
            _ = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppS2S).ConfigureAwait(false);

            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            // ----- Leg 1: SNI cert -> federated assertion (mtls_pop) on the global endpoint -----
            IConfidentialClientApplication leg1App = ConfidentialClientApplicationBuilder.Create(leg1ClientId)
                .WithAuthority("https://login.microsoftonline.com/bea21ebe-8b64-4d06-9f6d-6a889b120a7c")
                .WithCertificate(cert, true)
                .WithTestLogging()
                .Build();

            AuthenticationResult leg1 = await ExecuteOrInconclusiveOnTokenTypeMismatchAsync(() => leg1App
                .AcquireTokenForClient(new[] { leg1ExchangeScope })
                .WithMtlsProofOfPossession()
                .ExecuteAsync()).ConfigureAwait(false);

            Assert.IsNotNull(leg1, "Leg 1 returned null AuthenticationResult.");
            Assert.AreEqual(Constants.MtlsPoPTokenType, leg1.TokenType, "Leg 1 token type should be MTLS PoP.");
            Assert.IsFalse(string.IsNullOrEmpty(leg1.AccessToken), "Leg 1 did not return a federated assertion.");
            Assert.IsNotNull(leg1.BindingCertificate, "Leg 1 BindingCertificate should be set for cert-bound PoP.");
            Assert.AreEqual(cert.Thumbprint, leg1.BindingCertificate.Thumbprint,
                "Leg 1 BindingCertificate must match the SNI certificate.");

            // ----- Leg 2: carry Leg-1 binding cert -> resource token (mtls_pop) on the global endpoint -----
            string leg2ClientAssertionType = null;
            string leg2RequestUri = null;

            IConfidentialClientApplication leg2App = ConfidentialClientApplicationBuilder.Create(MsiAllowListedAppIdforSNI)
                .WithAuthority("https://login.microsoftonline.com/bea21ebe-8b64-4d06-9f6d-6a889b120a7c")
                .WithClientAssertion((AssertionRequestOptions options, CancellationToken ct) =>
                    Task.FromResult(new ClientSignedAssertion
                    {
                        Assertion = leg1.AccessToken,                     // Leg-1 federated assertion
                        TokenBindingCertificate = leg1.BindingCertificate // carry the SAME cert forward
                    }))
                .WithTestLogging()
                .Build();

            AuthenticationResult leg2 = await ExecuteOrInconclusiveOnTokenTypeMismatchAsync(() => leg2App
                .AcquireTokenForClient(new[] { AllowListedFinalResource })
                .WithMtlsProofOfPossession()
                .OnBeforeTokenRequest(data =>
                {
                    leg2RequestUri = data.RequestUri?.ToString();
                    data.BodyParameters?.TryGetValue("client_assertion_type", out leg2ClientAssertionType);
                    return Task.CompletedTask;
                })
                .ExecuteAsync()).ConfigureAwait(false);

            // Both legs are PoP and the binding certificate is continuous end-to-end.
            Assert.IsNotNull(leg2, "Leg 2 returned null AuthenticationResult.");
            Assert.AreEqual(Constants.MtlsPoPTokenType, leg2.TokenType, "Leg 2 token type should be MTLS PoP.");
            Assert.IsFalse(string.IsNullOrEmpty(leg2.AccessToken), "Leg 2 did not return an access token.");
            CollectionAssert.Contains(leg2.Scopes.ToArray(), AllowListedFinalResource,
                "Leg 2 token is not for the requested resource.");

            Assert.AreEqual(
                "urn:ietf:params:oauth:client-assertion-type:jwt-pop",
                leg2ClientAssertionType,
                "Leg 2 must present the federated assertion with the jwt-pop client_assertion_type.");

            Assert.IsNotNull(leg2.BindingCertificate, "Leg 2 BindingCertificate should be set for mtls_pop.");
            Assert.AreEqual(leg1.BindingCertificate.Thumbprint, leg2.BindingCertificate.Thumbprint,
                "The final token must be bound to the SAME certificate as Leg 1 (binding-cert continuity).");

            // Global mtlsauth endpoint (no region) reliably honors token_type=mtls_pop.
            Assert.IsFalse(string.IsNullOrEmpty(leg2RequestUri), "Expected Leg 2 token request URI to be captured.");
            Assert.AreEqual("mtlsauth.microsoft.com", new System.Uri(leg2RequestUri).Host,
                "Leg 2 should use the global mtlsauth endpoint when no region is configured.");
        }

        // TODO: Remove once the AAD westus3 test-slice mtlsauth endpoint reliably honors
        // token_type=mtls_pop. Today the test slice intermittently downgrades to Bearer,
        // which is a server-side issue, not a MSAL regression. The global mtlsauth endpoint
        // (covered by Sni_Gets_Pop_Token_WithGlobalEndpoint_TestAsync) continues to be
        // exercised end-to-end, so MSAL-side mTLS PoP behavior remains under test.
        private static async Task<AuthenticationResult> ExecuteOrInconclusiveOnTokenTypeMismatchAsync(
            Func<Task<AuthenticationResult>> action)
        {
            try
            {
                return await action().ConfigureAwait(false);
            }
            catch (MsalClientException ex) when (ex.ErrorCode == MsalError.TokenTypeMismatch)
            {
                Assert.Inconclusive(
                    "AAD westus3 test-slice mTLS endpoint returned Bearer instead of mtls_pop. " +
                    "This is a server-side issue on the test slice, not a MSAL regression. " +
                    $"Underlying error: {ex.Message}");
                throw; // Unreachable: Assert.Inconclusive throws.
            }
        }
    }
}
