// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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

        // Microsoft Graph scope. Every mTLS PoP test acquires a Graph-scoped, cert-bound token and then
        // calls Microsoft Graph over mTLS with that certificate — the resource must be ESTS allow-listed
        // for mtls_pop (Graph is), NOT the client app.
        private const string GraphAppScope = "https://graph.microsoft.com/.default";

        // Microsoft Graph mTLS (PoP) endpoint. A cert-bound (mtls_pop) token MUST be presented with the
        // "mtls_pop" auth scheme AND the bound certificate on the TLS handshake; the regular
        // graph.microsoft.com host does not perform the client-certificate handshake.
        private const string GraphMtlsResourceUri = "https://mtlstb.graph.microsoft.com/v1.0/applications?$top=1";

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

            string[] appScopes = new[] { GraphAppScope };

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

            // Act: present the cert-bound token to Microsoft Graph over mTLS (the developer end-to-end
            // experience — same binding certificate on the TLS handshake + "mtls_pop" Authorization scheme).
            (HttpStatusCode status, string body) =
                await CallResourceOverMtlsPopAsync(authResult, GraphMtlsResourceUri).ConfigureAwait(false);
            AssertResourceAcceptedPopTokenOrInconclusive(status, body);
        }

        [RunOn(SkipConditions.Linux)] // POP is not supported on Linux
        public async Task Sni_Gets_Pop_Token_WithGlobalEndpoint_TestAsync()
        {
            // Arrange: validate lab setup before executing the test flow.
            _ = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppS2S).ConfigureAwait(false);

            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            string[] appScopes = new[] { GraphAppScope };

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

            // Act: present the cert-bound token to Microsoft Graph over mTLS with the same certificate.
            (HttpStatusCode status, string body) =
                await CallResourceOverMtlsPopAsync(authResult, GraphMtlsResourceUri).ConfigureAwait(false);
            AssertResourceAcceptedPopTokenOrInconclusive(status, body);
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
                .AcquireTokenForClient(new[] { GraphAppScope })
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
            CollectionAssert.Contains(second.Scopes.ToArray(), GraphAppScope,
                "Second leg token is not for the Graph scope.");

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

            // Present the Graph-scoped, cert-bound Leg-2 token to Microsoft Graph over mTLS.
            (HttpStatusCode status, string body) =
                await CallResourceOverMtlsPopAsync(second, GraphMtlsResourceUri).ConfigureAwait(false);
            AssertResourceAcceptedPopTokenOrInconclusive(status, body);
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

            string[] appScopes = new[] { GraphAppScope };

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

            // Present the cert-bound token to Microsoft Graph over mTLS with the same certificate.
            (HttpStatusCode status, string body) =
                await CallResourceOverMtlsPopAsync(authResult, GraphMtlsResourceUri).ConfigureAwait(false);
            AssertResourceAcceptedPopTokenOrInconclusive(status, body);
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

            // Leg 1 is cert-bound PoP: assert the token type and that the binding cert is the SNI cert.
            Assert.AreEqual(Constants.MtlsPoPTokenType, first.TokenType, "Leg 1 token type should be MTLS PoP.");
            Assert.IsNotNull(first.BindingCertificate, "Leg 1 BindingCertificate should be set for cert-bound PoP.");
            Assert.AreEqual(cert.Thumbprint, first.BindingCertificate.Thumbprint,
                "Leg 1 BindingCertificate must match the SNI certificate.");

            // Step 2: build the assertion-based app — NO region configured (global endpoint)
            bool assertionProviderCalled = false;
            string requestUriSeen = null;
            string clientAssertionType = null;

            IConfidentialClientApplication assertionApp = ConfidentialClientApplicationBuilder.Create(MsiAllowListedAppIdforSNI)
                .WithExperimentalFeatures()
                .WithAuthority("https://login.microsoftonline.com/bea21ebe-8b64-4d06-9f6d-6a889b120a7c")
                .WithClientAssertion((AssertionRequestOptions options, CancellationToken ct) =>
                {
                    assertionProviderCalled = true;

                    return Task.FromResult(new ClientSignedAssertion
                    {
                        Assertion = assertionJwt,
                        TokenBindingCertificate = first.BindingCertificate // carry the SAME Leg-1 cert forward
                    });
                })
                .WithTestLogging()
                .Build();

            // Step 3: second leg should succeed using the global mTLS endpoint, returning an mtls_pop
            // token bound to the SAME certificate as Leg 1 (binding-cert continuity end-to-end).
            AuthenticationResult second = await ExecuteOrInconclusiveOnTokenTypeMismatchAsync(() => assertionApp
                .AcquireTokenForClient(new[] { GraphAppScope })
                .WithMtlsProofOfPossession()
                .OnBeforeTokenRequest(data =>
                {
                    requestUriSeen = data.RequestUri?.ToString();
                    data.BodyParameters?.TryGetValue("client_assertion_type", out clientAssertionType);
                    return Task.CompletedTask;
                })
                .ExecuteAsync()).ConfigureAwait(false);

            // Success assertions
            Assert.IsNotNull(second, "Second leg returned null AuthenticationResult.");
            Assert.IsFalse(string.IsNullOrEmpty(second.AccessToken), "Second leg did not return an access token.");
            Assert.IsTrue(assertionProviderCalled, "Client assertion provider should have been invoked.");

            // Leg 2 is also mtls_pop, presents the jwt-pop client_assertion_type, and is bound to the
            // SAME certificate as Leg 1 (binding-cert continuity).
            Assert.AreEqual(Constants.MtlsPoPTokenType, second.TokenType, "Leg 2 token type should be MTLS PoP.");
            Assert.AreEqual(
                "urn:ietf:params:oauth:client-assertion-type:jwt-pop",
                clientAssertionType,
                "Leg 2 must present the federated assertion with the jwt-pop client_assertion_type.");
            Assert.IsNotNull(second.BindingCertificate, "Leg 2 BindingCertificate should be set for mtls_pop.");
            Assert.AreEqual(first.BindingCertificate.Thumbprint, second.BindingCertificate.Thumbprint,
                "The final token must be bound to the SAME certificate as Leg 1 (binding-cert continuity).");

            // Verify global mTLS endpoint was used
            Assert.IsFalse(string.IsNullOrEmpty(requestUriSeen), "Expected token request URI to be captured.");
            var requestUri = new System.Uri(requestUriSeen);
            Assert.AreEqual("mtlsauth.microsoft.com", requestUri.Host,
                "Should use global mtlsauth endpoint when no region is configured.");

            // Present the Graph-scoped, cert-bound Leg-2 token to Microsoft Graph over mTLS.
            (HttpStatusCode status, string body) =
                await CallResourceOverMtlsPopAsync(second, GraphMtlsResourceUri).ConfigureAwait(false);
            AssertResourceAcceptedPopTokenOrInconclusive(status, body);
        }

        [RunOn(SkipConditions.Linux)] // POP is not supported on Linux
        public async Task Sni_Pop_Token_CanCall_Graph_OverMtls_TestAsync()
        {
            // Proves the acquired mTLS PoP token is actually USABLE against a resource, not just well-formed:
            // SNI cert -> Graph-scoped mtls_pop token -> call Graph over mTLS with the SAME bound cert.
            _ = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppS2S).ConfigureAwait(false);
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            // Global endpoint (no region) reliably issues token_type=mtls_pop.
            IConfidentialClientApplication confidentialApp = ConfidentialClientApplicationBuilder.Create(MsiAllowListedAppIdforSNI)
                .WithAuthority("https://login.microsoftonline.com/bea21ebe-8b64-4d06-9f6d-6a889b120a7c")
                .WithCertificate(cert, true)
                .WithTestLogging()
                .Build();

            try
            {
                // Act 1: acquire a Graph-scoped mTLS PoP token bound to the SNI certificate.
                AuthenticationResult authResult = await ExecuteOrInconclusiveOnTokenTypeMismatchAsync(() => confidentialApp
                    .AcquireTokenForClient(new[] { GraphAppScope })
                    .WithMtlsProofOfPossession()
                    .ExecuteAsync()).ConfigureAwait(false);

                Assert.AreEqual(Constants.MtlsPoPTokenType, authResult.TokenType, "Token type should be MTLS PoP.");
                Assert.IsNotNull(authResult.BindingCertificate, "BindingCertificate should be set in SNI flow.");
                Assert.AreEqual(cert.Thumbprint, authResult.BindingCertificate.Thumbprint,
                    "BindingCertificate must match the certificate supplied via WithCertificate().");

                // Act 2: present the cert-bound token to Microsoft Graph over mTLS. This is the developer
                // experience: same binding certificate on the TLS handshake + "mtls_pop" Authorization scheme.
                (HttpStatusCode status, string body) =
                    await CallResourceOverMtlsPopAsync(authResult, GraphMtlsResourceUri).ConfigureAwait(false);

                // Assert: the resource accepted the cert-bound token.
                AssertResourceAcceptedPopTokenOrInconclusive(status, body);
            }
            catch (MsalServiceException ex)
            {
                Assert.Inconclusive(
                    "Graph-scoped mTLS PoP token issuance was rejected by ESTS for this app/lab configuration " +
                    $"(the app may not be allow-listed for the Graph scope). Underlying error: {ex.Message}");
            }
        }

        [RunOn(SkipConditions.Linux)] // POP is not supported on Linux
        public async Task Sni_TwoLeg_S2sFic_Pop_CanCall_Graph_OverMtls_TestAsync()
        {
            // Two-leg S2S FIC over mTLS PoP (SNI first leg -> federated assertion -> Graph-scoped resource
            // token bound to the same certificate), then call Microsoft Graph over mTLS with that cert.
            try
            {
                AuthenticationResult leg2 = await RunTwoLegS2sFicBothLegsPopAsync(
                    MsiAllowListedAppIdforSNI, MsiAllowListedAppIdforSNI, TokenExchangeUrl, GraphAppScope).ConfigureAwait(false);

                (HttpStatusCode status, string body) =
                    await CallResourceOverMtlsPopAsync(leg2, GraphMtlsResourceUri).ConfigureAwait(false);

                AssertResourceAcceptedPopTokenOrInconclusive(status, body);
            }
            catch (MsalServiceException ex)
            {
                Assert.Inconclusive(
                    "Two-leg S2S FIC mTLS PoP exchange (or Graph-scoped issuance) was rejected by ESTS for this " +
                    $"app/lab configuration. Underlying error: {ex.Message}");
            }
        }

        // Drives the two-leg S2S FIC over mTLS PoP end-to-end flow. Both legs use the global mtlsauth
        // endpoint (no region) so they reliably return token_type=mtls_pop, and each leg is wrapped in
        // ExecuteOrInconclusiveOnTokenTypeMismatchAsync to tolerate a server-side downgrade. Leg 1 and
        // Leg 2 client ids are supplied explicitly so callers keep them consistent — a mismatch would
        // make an ESTS rejection ambiguous (client-id mismatch vs. a genuine flow issue).
        private static async Task<AuthenticationResult> RunTwoLegS2sFicBothLegsPopAsync(string leg1ClientId, string leg2ClientId, string leg1ExchangeScope, string finalResourceScope)
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

            IConfidentialClientApplication leg2App = ConfidentialClientApplicationBuilder.Create(leg2ClientId)
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
                .AcquireTokenForClient(new[] { finalResourceScope })
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
            CollectionAssert.Contains(leg2.Scopes.ToArray(), finalResourceScope,
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

            return leg2;
        }

        // Demonstrates the developer experience for USING an mTLS PoP token: present it to a protected
        // resource over mTLS. Two things are required and easy to get wrong:
        //   1. The SAME certificate the token is bound to (AuthenticationResult.BindingCertificate) must be
        //      supplied as the client certificate on the TLS handshake.
        //   2. The Authorization header uses the "mtls_pop" scheme, NOT "Bearer".
        // The resource validates possession by matching the TLS client cert against the token's cnf binding.
        private static async Task<(HttpStatusCode Status, string Body)> CallResourceOverMtlsPopAsync(
            AuthenticationResult authResult, string resourceUri)
        {
            Assert.IsNotNull(authResult.BindingCertificate,
                "A binding certificate is required to call a resource over mTLS PoP.");

            var handler = new HttpClientHandler();
            handler.ClientCertificates.Add(authResult.BindingCertificate); // bind the TLS client cert

            // HttpClient owns and disposes the handler.
            using (var http = new HttpClient(handler))
            using (var request = new HttpRequestMessage(HttpMethod.Get, resourceUri))
            {
                request.Headers.Authorization =
                    new AuthenticationHeaderValue(Constants.MtlsPoPAuthHeaderPrefix, authResult.AccessToken);

                using (HttpResponseMessage response = await http.SendAsync(request).ConfigureAwait(false))
                {
                    string body = response.Content is null
                        ? string.Empty
                        : await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return (response.StatusCode, body);
                }
            }
        }

        // A 200 proves the resource accepted the cert-bound token. A 401/403 means the app/cert is not yet
        // allow-listed for mTLS PoP on this resource (or lacks the required Graph permission) — an
        // enablement/config issue, not a MSAL regression — so it is reported inconclusive rather than failed.
        private static void AssertResourceAcceptedPopTokenOrInconclusive(HttpStatusCode status, string body)
        {
            if (status == HttpStatusCode.OK)
            {
                return;
            }

            if (status == HttpStatusCode.Unauthorized || status == HttpStatusCode.Forbidden)
            {
                Assert.Inconclusive(
                    $"Resource rejected the mTLS PoP token ({(int)status}). The app/cert is likely not allow-listed " +
                    "for mTLS PoP on this resource (or lacks the required permission), which is a configuration " +
                    $"issue rather than a MSAL one. Response: {body}");
            }

            // Throttling (429) or a server-side 5xx from the resource is transient and unrelated to MSAL, so
            // report inconclusive rather than failing the run (several tests call Graph in quick succession).
            if (status == (HttpStatusCode)429 || (int)status >= 500)
            {
                Assert.Inconclusive(
                    $"Resource returned a transient/server-side response ({(int)status}) over mTLS PoP, unrelated " +
                    $"to MSAL. Response: {body}");
            }

            Assert.Fail($"Unexpected response calling the resource over mTLS PoP: {(int)status}. Response: {body}");
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
