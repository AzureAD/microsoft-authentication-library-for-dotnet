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

        [RunOn(SkipConditions.Linux)] // PoP is not supported on Linux
        public async Task Credential_X509_Output_Pop_TestAsync()
        {
            // X509 (SNI) credential -> Graph-scoped mtls_pop token on the global endpoint, then call
            // Microsoft Graph over mTLS with the bound certificate to prove the token is actually usable.
            _ = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppS2S).ConfigureAwait(false);
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            IConfidentialClientApplication confidentialApp = ConfidentialClientApplicationBuilder.Create(MsiAllowListedAppIdforSNI)
                .WithAuthority("https://login.microsoftonline.com/bea21ebe-8b64-4d06-9f6d-6a889b120a7c")
                .WithCertificate(cert, true)
                .WithTestLogging()
                .Build();

            AuthenticationResult authResult = await confidentialApp
                .AcquireTokenForClient(new[] { GraphAppScope })
                .WithMtlsProofOfPossession()
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(Constants.MtlsPoPTokenType, authResult.TokenType, "Token type should be MTLS PoP.");
            Assert.IsNotNull(authResult.BindingCertificate, "BindingCertificate should be set in the SNI flow.");
            Assert.AreEqual(cert.Thumbprint, authResult.BindingCertificate.Thumbprint,
                "BindingCertificate must match the certificate supplied via WithCertificate().");

            (HttpStatusCode status, string body) =
                await CallResourceOverMtlsPopAsync(authResult, GraphMtlsResourceUri).ConfigureAwait(false);
            AssertResourceAcceptedPopToken(status, body);
        }

        [RunOn(SkipConditions.Linux)] // mTLS is not supported on Linux
        public async Task Credential_X509_Output_Bearer_TestAsync()
        {
            // X509 (SNI) credential over mTLS transport WITHOUT requesting PoP -> a plain Bearer token.
            // A Bearer token is not certificate-bound, so there is no Graph-over-mTLS call here.
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            var certificateOptions = new CertificateOptions
            {
                SendCertificateOverMtls = true
            };

            IConfidentialClientApplication confidentialApp = ConfidentialClientApplicationBuilder.Create(MsiAllowListedAppIdforSNI)
                .WithAuthority("https://login.microsoftonline.com/bea21ebe-8b64-4d06-9f6d-6a889b120a7c")
                .WithAzureRegion("westus3")
                .WithCertificate(cert, certificateOptions)
                .WithTestLogging()
                .Build();

            AuthenticationResult authResult = await confidentialApp
                .AcquireTokenForClient(new[] { "https://vault.azure.net/.default" })
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(authResult, "The authentication result should not be null.");
            Assert.AreEqual("Bearer", authResult.TokenType, "Token type should be Bearer when PoP is not requested.");
            Assert.IsNotNull(authResult.AccessToken, "Access token should not be null.");
        }

        [RunOn(SkipConditions.Linux)] // PoP is not supported on Linux
        public async Task Credential_Fic_Output_Pop_TestAsync()
        {
            // Two-leg S2S FIC over mTLS PoP: SNI cert (leg 1) -> federated assertion -> resource token
            // bound to the SAME certificate (leg 2), then call Microsoft Graph over mTLS with that cert.
            AuthenticationResult leg2 = await RunTwoLegS2sFicBothLegsPopAsync(
                MsiAllowListedAppIdforSNI, TokenExchangeUrl, GraphAppScope).ConfigureAwait(false);

            (HttpStatusCode status, string body) =
                await CallResourceOverMtlsPopAsync(leg2, GraphMtlsResourceUri).ConfigureAwait(false);
            AssertResourceAcceptedPopToken(status, body);
        }

        [RunOn(SkipConditions.Linux)] // PoP is not supported on Linux
        public async Task Credential_Fic_Output_Bearer_TestAsync()
        {
            // Two-leg S2S FIC where leg 2 does NOT request PoP -> a plain Bearer token. The federated
            // assertion still carries a TokenBindingCertificate, so it is presented with the jwt-pop
            // client_assertion_type. No Graph-over-mTLS call for a (non-bound) Bearer token.
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            // Leg 1: SNI cert -> federated assertion (mtls_pop).
            IConfidentialClientApplication leg1App = ConfidentialClientApplicationBuilder.Create(MsiAllowListedAppIdforSNI)
                .WithAuthority("https://login.microsoftonline.com/bea21ebe-8b64-4d06-9f6d-6a889b120a7c")
                .WithAzureRegion("westus3")
                .WithCertificate(cert, true)
                .WithTestLogging()
                .Build();

            AuthenticationResult leg1 = await leg1App
                .AcquireTokenForClient(new[] { TokenExchangeUrl })
                .WithMtlsProofOfPossession()
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsFalse(string.IsNullOrEmpty(leg1.AccessToken), "Leg 1 did not return a federated assertion.");

            // Leg 2: carry the assertion + binding cert, but do NOT request PoP -> Bearer output.
            bool assertionProviderCalled = false;
            string clientAssertionType = null;

            IConfidentialClientApplication leg2App = ConfidentialClientApplicationBuilder.Create(MsiAllowListedAppIdforSNI)
                .WithAuthority("https://login.microsoftonline.com/bea21ebe-8b64-4d06-9f6d-6a889b120a7c")
                .WithAzureRegion("westus3")
                .WithClientAssertion((AssertionRequestOptions options, CancellationToken ct) =>
                {
                    assertionProviderCalled = true;
                    return Task.FromResult(new ClientSignedAssertion
                    {
                        Assertion = leg1.AccessToken,
                        TokenBindingCertificate = leg1.BindingCertificate
                    });
                })
                .WithTestLogging()
                .Build();

            AuthenticationResult leg2 = await leg2App
                .AcquireTokenForClient(new[] { "https://storage.azure.com/.default" })
                .OnBeforeTokenRequest(data =>
                {
                    data.BodyParameters?.TryGetValue("client_assertion_type", out clientAssertionType);
                    return Task.CompletedTask;
                })
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsFalse(string.IsNullOrEmpty(leg2.AccessToken), "Leg 2 did not return an access token.");
            Assert.AreEqual("Bearer", leg2.TokenType, "Leg 2 did not request PoP, so the output token should be Bearer.");
            CollectionAssert.Contains(leg2.Scopes.ToArray(), "https://storage.azure.com/.default",
                "Leg 2 token is not for the Storage scope.");
            Assert.IsTrue(assertionProviderCalled, "Client assertion provider should have been invoked.");
            Assert.AreEqual(
                "urn:ietf:params:oauth:client-assertion-type:jwt-pop",
                clientAssertionType,
                "A TokenBindingCertificate-backed assertion should use the jwt-pop client_assertion_type.");
        }

        // Drives the two-leg S2S FIC over mTLS PoP flow end-to-end. Leg 1 uses the SNI certificate to
        // obtain a federated assertion; leg 2 carries that assertion plus the SAME binding certificate to
        // obtain a resource token bound to that certificate. Both legs use the global mtlsauth endpoint
        // (no region) so they reliably return token_type=mtls_pop. Wire-format details
        // (endpoint host, jwt-pop client_assertion_type) are asserted in the unit tests; here we verify the
        // end-to-end round trip, binding-certificate continuity, and correlation-id propagation (#5924).
        private static async Task<AuthenticationResult> RunTwoLegS2sFicBothLegsPopAsync(string clientId, string leg1ExchangeScope, string finalResourceScope)
        {
            _ = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppS2S).ConfigureAwait(false);

            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            // Leg 1: SNI cert -> federated assertion (mtls_pop) on the global endpoint.
            IConfidentialClientApplication leg1App = ConfidentialClientApplicationBuilder.Create(clientId)
                .WithAuthority("https://login.microsoftonline.com/bea21ebe-8b64-4d06-9f6d-6a889b120a7c")
                .WithCertificate(cert, true)
                .WithTestLogging()
                .Build();

            AuthenticationResult leg1 = await leg1App
                .AcquireTokenForClient(new[] { leg1ExchangeScope })
                .WithMtlsProofOfPossession()
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(Constants.MtlsPoPTokenType, leg1.TokenType, "Leg 1 token type should be MTLS PoP.");
            Assert.IsFalse(string.IsNullOrEmpty(leg1.AccessToken), "Leg 1 did not return a federated assertion.");
            Assert.IsNotNull(leg1.BindingCertificate, "Leg 1 BindingCertificate should be set for cert-bound PoP.");
            Assert.AreEqual(cert.Thumbprint, leg1.BindingCertificate.Thumbprint,
                "Leg 1 BindingCertificate must match the SNI certificate.");

            // Leg 2: carry the Leg-1 assertion + binding cert -> resource token (mtls_pop) on the global endpoint.
            Guid expectedCorrelationId = Guid.NewGuid();
            Guid correlationIdSeenByProvider = Guid.Empty;

            IConfidentialClientApplication leg2App = ConfidentialClientApplicationBuilder.Create(clientId)
                .WithAuthority("https://login.microsoftonline.com/bea21ebe-8b64-4d06-9f6d-6a889b120a7c")
                .WithClientAssertion((AssertionRequestOptions options, CancellationToken ct) =>
                {
                    correlationIdSeenByProvider = options.CorrelationId;
                    return Task.FromResult(new ClientSignedAssertion
                    {
                        Assertion = leg1.AccessToken,                     // Leg-1 federated assertion
                        TokenBindingCertificate = leg1.BindingCertificate // carry the SAME cert forward
                    });
                })
                .WithTestLogging()
                .Build();

            AuthenticationResult leg2 = await leg2App
                .AcquireTokenForClient(new[] { finalResourceScope })
                .WithMtlsProofOfPossession()
                .WithCorrelationId(expectedCorrelationId)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(Constants.MtlsPoPTokenType, leg2.TokenType, "Leg 2 token type should be MTLS PoP.");
            Assert.IsFalse(string.IsNullOrEmpty(leg2.AccessToken), "Leg 2 did not return an access token.");
            CollectionAssert.Contains(leg2.Scopes.ToArray(), finalResourceScope,
                "Leg 2 token is not for the requested resource.");
            Assert.IsNotNull(leg2.BindingCertificate, "Leg 2 BindingCertificate should be set for mtls_pop.");
            Assert.AreEqual(leg1.BindingCertificate.Thumbprint, leg2.BindingCertificate.Thumbprint,
                "The final token must be bound to the SAME certificate as Leg 1 (binding-cert continuity).");

            // #5924: the correlation id from WithCorrelationId() must flow to the assertion callback.
            Assert.AreEqual(expectedCorrelationId, correlationIdSeenByProvider,
                "CorrelationId from WithCorrelationId() must flow to the assertion callback for FIC two-leg tracing.");

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

        private static void AssertResourceAcceptedPopToken(HttpStatusCode status, string body)
        {
            if (status == HttpStatusCode.OK)
            {
                return;
            }

            if (status == HttpStatusCode.Unauthorized || status == HttpStatusCode.Forbidden)
            {
                Assert.Fail(
                    $"Resource rejected the mTLS PoP token ({(int)status}). The app/cert is allow-listed for mTLS PoP " +
                    "on this resource, so this indicates a regression (e.g., the binding cert was not presented on the " +
                    $"TLS handshake, or the Authorization scheme was not \"mtls_pop\"). Response: {TruncateForLog(body)}");
            }

            Assert.Fail($"Unexpected response calling the resource over mTLS PoP: {(int)status}. Response: {TruncateForLog(body)}");
        }

        // External response bodies are truncated before being emitted into (public) CI logs so a failing
        // assertion stays diagnosable (status + error prefix) without dumping full tenant/application data.
        private static string TruncateForLog(string body, int maxChars = 200)
        {
            if (string.IsNullOrEmpty(body))
            {
                return "<empty>";
            }

            return body.Length <= maxChars ? body : body.Substring(0, maxChars) + "...(truncated)";
        }
    }
}
