// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
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

        [TestInitialize]
        public void TestInitialize()
        {
            ApplicationBase.ResetStateForTest();
        }

        [DoNotRunOnLinux] // POP is not supported on Linux
        [TestMethod]
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

        [DoNotRunOnLinux]
        [TestMethod]
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

            AuthenticationResult first = await firstApp
                .AcquireTokenForClient(new[] { TokenExchangeUrl })
                .WithMtlsProofOfPossession()
                .ExecuteAsync()
                .ConfigureAwait(false);

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
                .WithExperimentalFeatures()
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
                .AcquireTokenForClient(new[] { "https://vault.azure.net/.default" })
                .WithMtlsProofOfPossession()
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
        }

        [DoNotRunOnLinux]
        //[TestMethod] // Temporarily disabled due to feature not ready in ESTS
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

            AuthenticationResult first = await firstApp
                .AcquireTokenForClient(new[] { TokenExchangeUrl })
                .WithMtlsProofOfPossession()
                .ExecuteAsync()
                .ConfigureAwait(false);

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
                .WithExperimentalFeatures()
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
                .AcquireTokenForClient(new[] { "https://vault.azure.net/.default" })
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
        }
    }
}
