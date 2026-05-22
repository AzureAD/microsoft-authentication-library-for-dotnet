// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    /// <summary>
    /// Integration tests for mTLS bearer transport (<c>SendCertificateOverMtls = true</c>)
    /// applied to all flows: S2S, OBO, refresh_token, and auth_code.
    ///
    /// Each test validates the two conditions required for mTLS bearer transport:
    ///   1. The token request goes to the mTLS endpoint (<c>mtlsauth.microsoft.com</c>).
    ///   2. <c>client_assertion</c> IS in the POST body — cert authenticates at the TLS layer
    ///      AND the body carries the assertion (required by ESTS for this preview).
    ///
    /// This is distinct from mTLS PoP (<c>.WithMtlsProofOfPossession()</c>), which binds the
    /// token cryptographically to a certificate and is only available on AcquireTokenForClient.
    /// </summary>
    [TestClass]
    public class MtlsTransportUserFlowTests
    {
        private static readonly string[] s_userReadScopes = { "User.Read" };

        [TestInitialize]
        public void TestInitialize()
        {
            ApplicationBase.ResetStateForTest();
        }

        /// <summary>
        /// Integration test — 2x2 matrix cell (OBO × mTLS bearer):
        /// Verifies that an OBO token request with <c>SendCertificateOverMtls = true</c>
        /// satisfies both mTLS transport conditions:
        ///   1. The request goes to <c>mtlsauth.microsoft.com</c> (not <c>login.microsoftonline.com</c>).
        ///   2. The <c>IMsalMtlsHttpClientFactory</c> cert overload is invoked, confirming the mTLS
        ///      transport factory is used for the OBO flow.
        ///
        /// Expected outcome: PASSES once <c>AppWebApi</c> is enabled for mTLS client auth in the lab.
        /// Until then, AAD returns HTTP 412 / <c>AADSTS51000: MtlsClientAuth is/are disabled</c>.
        /// This error means the app is not yet configured — it does NOT mean the grant is unsupported.
        /// See <see cref="OboFlow_WithClientSecret_BaselineAsync"/> for the (OBO × client_secret) cell
        /// that proves the OBO grant itself works and the mTLS failure is purely app-config.
        ///
        /// Full 2x2 matrix:
        ///   client_credentials + mTLS    → ClientCredentialsMtlsPopTests.Sni_Over_Mtls_Gets_Bearer_Token_Successfully_TestAsync (PASSES)
        ///   client_credentials + secret  → ClientCredentialsTests (PASSES)
        ///   OBO + mTLS                   → this test (FAILS until AppWebApi is mTLS-enabled)
        ///   OBO + secret                 → <see cref="OboFlow_WithClientSecret_BaselineAsync"/> (PASSES)
        /// </summary>
        [DoNotRunOnLinux]
        [TestMethod]
        public async Task OboFlow_WithSendCertificateOverMtls_AcquiresTokenAsync()
        {
            // Arrange
            X509Certificate2 mtlsCert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);
            Assert.IsNotNull(mtlsCert, "Lab cert must be installed to run this test.");

            var trackingFactory = new TrackingMtlsHttpClientFactory(mtlsCert);

            var appConfig = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppS2S).ConfigureAwait(false);
            var appApiConfig = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppWebApi).ConfigureAwait(false);
            var user = await LabResponseHelper.GetUserConfigAsync(KeyVaultSecrets.UserPublicCloud).ConfigureAwait(false);

            // Step 1: Acquire a user assertion via ROPC (public client — no mTLS needed here)
            var pca = PublicClientApplicationBuilder
                .Create(appConfig.AppId)
                .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs)
                .WithTestLogging()
                .Build();

#pragma warning disable CS0618
            AuthenticationResult userResult = await pca
                .AcquireTokenByUsernamePassword([appApiConfig.DefaultScopes], user.Upn, user.GetOrFetchPassword())
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);
#pragma warning restore CS0618

            Assert.IsNotNull(userResult?.AccessToken, "Failed to acquire user token via ROPC.");

            // Step 2: Build the OBO confidential client with SendCertificateOverMtls=true.
            // The cert authenticates the app at the TLS layer; no client_assertion is sent in the body.
            // NOTE: WithHttpClientFactory must come AFTER WithTestLogging to override the sniffer factory.
            var cca = ConfidentialClientApplicationBuilder
                .Create(appApiConfig.AppId)
                .WithAuthority(new Uri($"https://login.microsoftonline.com/{userResult.TenantId}"), true)
                .WithCertificate(mtlsCert, new CertificateOptions { SendCertificateOverMtls = true })
                .WithTestLogging()
                .WithHttpClientFactory(trackingFactory)
                .Build();

            // Act: OBO
            AuthenticationResult oboResult = await cca
                .AcquireTokenOnBehalfOf(s_userReadScopes, new UserAssertion(userResult.AccessToken))
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(oboResult, "OBO result should not be null.");
            Assert.IsNotNull(oboResult.AccessToken, "OBO access token should not be null.");
            Assert.AreEqual(TokenSource.IdentityProvider, oboResult.AuthenticationResultMetadata.TokenSource);
            StringAssert.Contains(
                oboResult.AuthenticationResultMetadata.TokenEndpoint, "mtlsauth",
                $"OBO token request should use the mTLS endpoint, but got: {oboResult.AuthenticationResultMetadata.TokenEndpoint}");
            Assert.IsGreaterThan(0, trackingFactory.GetHttpClientCallCount,
                "The mTLS-specific GetHttpClient(X509Certificate2) overload should have been called at least once for the OBO flow.");
        }

        /// <summary>
        /// Integration test: verifies that a refresh-token redemption with <c>SendCertificateOverMtls = true</c>
        /// satisfies both mTLS conditions:
        ///   1. The request goes to <c>mtlsauth.microsoft.com</c>.
        ///   2. The <c>IMsalMtlsHttpClientFactory</c> cert overload is invoked for the RT redemption.
        ///
        /// Note: token acquisition succeeds only if the app is registered in the lab for mTLS bearer transport.
        /// </summary>
        [DoNotRunOnLinux]
        [TestMethod]
        public async Task RefreshTokenFlow_WithSendCertificateOverMtls_AcquiresTokenAsync()
        {
            // Arrange
            X509Certificate2 mtlsCert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);
            Assert.IsNotNull(mtlsCert, "Lab cert must be installed to run this test.");

            var trackingFactory = new TrackingMtlsHttpClientFactory(mtlsCert);

            var appConfig = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppS2S).ConfigureAwait(false);
            var appApiConfig = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppWebApi).ConfigureAwait(false);
            var user = await LabResponseHelper.GetUserConfigAsync(KeyVaultSecrets.UserPublicCloud).ConfigureAwait(false);

            // Extract the refresh token from the PCA's token cache via internal accessor
            // (using BuildConcrete() allows access to internal APIs for test purposes)
            var pcaConcrete = PublicClientApplicationBuilder
                .Create(appConfig.AppId)
                .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs)
                .WithTestLogging()
                .BuildConcrete();

#pragma warning disable CS0618
            AuthenticationResult userResultConcrete = await pcaConcrete
                .AcquireTokenByUsernamePassword([appApiConfig.DefaultScopes], user.Upn, user.GetOrFetchPassword())
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);
#pragma warning restore CS0618

            var rtCacheItem = pcaConcrete.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().FirstOrDefault();
            Assert.IsNotNull(rtCacheItem, "Refresh token must be present in cache.");
            string refreshToken = rtCacheItem.Secret;

            // Build CCA with SendCertificateOverMtls=true: the cert authenticates at the TLS layer
            // and the factory provides the mTLS connection. No client_assertion is sent in the body.
            // NOTE: WithHttpClientFactory must come AFTER WithTestLogging to override the sniffer factory.
            var cca = ConfidentialClientApplicationBuilder
                .Create(appConfig.AppId)
                .WithAuthority(new Uri($"https://login.microsoftonline.com/{userResultConcrete.TenantId}"), true)
                .WithCertificate(mtlsCert, new CertificateOptions { SendCertificateOverMtls = true })
                .WithTestLogging()
                .WithHttpClientFactory(trackingFactory)
                .Build();

            // Act: AcquireTokenByRefreshToken
            AuthenticationResult refreshResult = await ((IByRefreshToken)cca)
                .AcquireTokenByRefreshToken([appApiConfig.DefaultScopes], refreshToken)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(refreshResult, "Refresh token result should not be null.");
            Assert.IsNotNull(refreshResult.AccessToken, "Access token should not be null after refresh.");
            StringAssert.Contains(
                refreshResult.AuthenticationResultMetadata.TokenEndpoint, "mtlsauth",
                $"RT redemption should use the mTLS endpoint, but got: {refreshResult.AuthenticationResultMetadata.TokenEndpoint}");
            Assert.IsGreaterThan(0, trackingFactory.GetHttpClientCallCount,
                "The mTLS-specific GetHttpClient(X509Certificate2) overload should have been called at least once for the refresh_token flow.");
        }

        /// <summary>
        /// Negative test: verifies that OBO WITHOUT <c>SendCertificateOverMtls = true</c> does NOT
        /// route to the mTLS endpoint. The certificate is presented as a JWT <c>client_assertion</c>
        /// in the POST body via the regular <c>login.microsoftonline.com</c> endpoint.
        ///
        /// This is the contrast case for <see cref="OboFlow_WithSendCertificateOverMtls_BothMtlsConditionsMet"/>:
        /// the opt-in flag is what distinguishes mTLS transport from standard cert-assertion auth.
        /// </summary>
        [DoNotRunOnLinux]
        [TestMethod]
        public async Task OboFlow_WithoutSendCertificateOverMtls_UsesRegularEndpointAsync()
        {
            X509Certificate2 mtlsCert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);
            Assert.IsNotNull(mtlsCert, "Lab cert must be installed to run this test.");

            var appConfig = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppS2S).ConfigureAwait(false);
            var appApiConfig = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppWebApi).ConfigureAwait(false);
            var user = await LabResponseHelper.GetUserConfigAsync(KeyVaultSecrets.UserPublicCloud).ConfigureAwait(false);

            // Step 1: Acquire user assertion via ROPC
            var pca = PublicClientApplicationBuilder
                .Create(appConfig.AppId)
                .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs)
                .WithTestLogging()
                .Build();

#pragma warning disable CS0618
            AuthenticationResult userResult = await pca
                .AcquireTokenByUsernamePassword([appApiConfig.DefaultScopes], user.Upn, user.GetOrFetchPassword())
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);
#pragma warning restore CS0618

            Assert.IsNotNull(userResult?.AccessToken, "Failed to acquire user token via ROPC.");

            // Step 2: OBO with cert but WITHOUT SendCertificateOverMtls — cert goes in the body as
            // client_assertion, NOT at the TLS layer. Request goes to login.microsoftonline.com.
            var recordingFactory = new RecordingMtlsHttpClientFactory();
            var cca = ConfidentialClientApplicationBuilder
                .Create(appApiConfig.AppId)
                .WithAuthority(new Uri($"https://login.microsoftonline.com/{userResult.TenantId}"), true)
                .WithCertificate(mtlsCert)  // no SendCertificateOverMtls
                .WithHttpClientFactory(recordingFactory)
                .Build();

            try
            {
                await cca
                    .AcquireTokenOnBehalfOf(s_userReadScopes, new UserAssertion(userResult.AccessToken))
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
            }
            catch (MsalServiceException)
            {
                // AAD may reject for cert/config reasons — we only care about the request format.
            }

            string requestUrl = recordingFactory.LastCapturedUrl ?? "(none captured)";
            string requestBody = recordingFactory.LastCapturedBody ?? "";

            // Without SendCertificateOverMtls, request must NOT go to mtlsauth
            Assert.DoesNotContain(requestUrl, "mtlsauth",
                $"OBO without SendCertificateOverMtls should use login.microsoftonline.com, but got: {requestUrl}");

            // The cert is sent as client_assertion in the body (standard cert auth)
            StringAssert.Contains(requestBody, "client_assertion",
                "OBO without SendCertificateOverMtls should include client_assertion in the body.");
        }

        /// <summary>
        /// Tests the two conditions required for mTLS transport auth on OBO:
        ///   1. Token request goes to the mTLS endpoint (mtlsauth.microsoft.com), not the regular endpoint.
        ///   2. <c>client_assertion</c> IS in the POST body — cert at TLS layer + assertion in body.
        ///
        /// Uses <c>CertificateOptions.SendCertificateOverMtls = true</c> to opt in to mTLS bearer transport.
        /// AAD may reject the request if the cert is not registered for AppWebApi, but MSAL's request
        /// format (endpoint + body) is verified via the recording factory before any AAD response.
        /// </summary>
        [DoNotRunOnLinux]
        [TestMethod]
        public async Task OboFlow_WithSendCertificateOverMtls_BothMtlsConditionsMet()
        {
            X509Certificate2 mtlsCert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);
            Assert.IsNotNull(mtlsCert, "Lab cert must be installed to run this test.");

            var appConfig = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppS2S).ConfigureAwait(false);
            var appApiConfig = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppWebApi).ConfigureAwait(false);
            var user = await LabResponseHelper.GetUserConfigAsync(KeyVaultSecrets.UserPublicCloud).ConfigureAwait(false);

            var pca = PublicClientApplicationBuilder
                .Create(appConfig.AppId)
                .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs)
                .WithTestLogging()
                .Build();

#pragma warning disable CS0618
            AuthenticationResult userResult = await pca
                .AcquireTokenByUsernamePassword([appApiConfig.DefaultScopes], user.Upn, user.GetOrFetchPassword())
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);
#pragma warning restore CS0618

            Assert.IsNotNull(userResult?.AccessToken, "Failed to acquire user token via ROPC.");

            var recordingFactory = new RecordingMtlsHttpClientFactory();
            var cca = ConfidentialClientApplicationBuilder
                .Create(appApiConfig.AppId)
                .WithAuthority(new Uri($"https://login.microsoftonline.com/{userResult.TenantId}"), true)
                .WithCertificate(mtlsCert, new CertificateOptions { SendCertificateOverMtls = true })
                .WithHttpClientFactory(recordingFactory)
                .Build();

            try
            {
                await cca
                    .AcquireTokenOnBehalfOf(s_userReadScopes, new UserAssertion(userResult.AccessToken))
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
            }
            catch (MsalServiceException)
            {
                // AAD may reject if AppWebApi is not yet registered for mTLS bearer transport.
                // The assertions below verify MSAL's request-level behaviour regardless.
            }

            string lastBody = recordingFactory.LastCapturedBody;
            string requestUrl = recordingFactory.LastCapturedUrl ?? "(none captured)";

            // Condition 1: request must go to the mTLS endpoint
            StringAssert.Contains(requestUrl, "mtlsauth",
                $"Condition 1 FAILED: OBO token request went to '{requestUrl}' instead of mtlsauth.microsoft.com.");

            // Condition 2: client_assertion must be in body (cert at TLS + assertion in body)
            StringAssert.Contains(lastBody, "client_assertion",
                "Condition 2 FAILED: client_assertion is NOT present in the OBO POST body — should be present for mTLS transport.");
        }

        /// <summary>
        /// Tests the two conditions required for mTLS transport auth on refresh_token redemption:
        ///   1. Token request goes to the mTLS endpoint (mtlsauth.microsoft.com).
        ///   2. <c>client_assertion</c> IS in the POST body.
        ///
        /// Uses <c>CertificateOptions.SendCertificateOverMtls = true</c> to opt in to mTLS bearer transport.
        /// </summary>
        [DoNotRunOnLinux]
        [TestMethod]
        public async Task RefreshTokenFlow_WithSendCertificateOverMtls_BothMtlsConditionsMet()
        {
            X509Certificate2 mtlsCert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);
            Assert.IsNotNull(mtlsCert, "Lab cert must be installed to run this test.");

            var appConfig = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppS2S).ConfigureAwait(false);
            var appApiConfig = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppWebApi).ConfigureAwait(false);
            var user = await LabResponseHelper.GetUserConfigAsync(KeyVaultSecrets.UserPublicCloud).ConfigureAwait(false);

            var pcaConcrete = PublicClientApplicationBuilder
                .Create(appConfig.AppId)
                .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs)
                .WithTestLogging()
                .BuildConcrete();

#pragma warning disable CS0618
            AuthenticationResult userResult = await pcaConcrete
                .AcquireTokenByUsernamePassword([appApiConfig.DefaultScopes], user.Upn, user.GetOrFetchPassword())
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);
#pragma warning restore CS0618

            var rtItem = pcaConcrete.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().FirstOrDefault();
            Assert.IsNotNull(rtItem, "Refresh token must be present in cache.");
            string refreshToken = rtItem.Secret;

            var recordingFactory = new RecordingMtlsHttpClientFactory();
            var cca = ConfidentialClientApplicationBuilder
                .Create(appConfig.AppId)
                .WithAuthority(new Uri($"https://login.microsoftonline.com/{userResult.TenantId}"), true)
                .WithCertificate(mtlsCert, new CertificateOptions { SendCertificateOverMtls = true })
                .WithHttpClientFactory(recordingFactory)
                .Build();

            try
            {
                await ((IByRefreshToken)cca)
                    .AcquireTokenByRefreshToken([appApiConfig.DefaultScopes], refreshToken)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
            }
            catch (MsalServiceException)
            {
                // AAD may reject if the app is not yet registered for mTLS bearer transport.
                // The assertions below verify MSAL's request-level behaviour regardless.
            }

            string lastBody = recordingFactory.LastCapturedBody;
            string requestUrl = recordingFactory.LastCapturedUrl ?? "(none captured)";

            // Condition 1: request must go to the mTLS endpoint
            StringAssert.Contains(requestUrl, "mtlsauth",
                $"Condition 1 FAILED: RT token request went to '{requestUrl}' instead of mtlsauth.microsoft.com.");

            // Condition 2: client_assertion must be in body
            StringAssert.Contains(lastBody, "client_assertion",
                "Condition 2 FAILED: client_assertion is NOT present in the RT POST body — should be present for mTLS transport.");
        }

        /// <summary>
        /// Control test: verifies that for AcquireTokenForClient with SendCertificateOverMtls=true,
        /// BOTH mTLS transport conditions ARE met:
        ///   1. Request goes to mtlsauth.microsoft.com (mTLS endpoint).
        ///   2. <c>client_assertion</c> IS in the POST body (cert at TLS + assertion in body).
        ///
        /// Uses the MSI-allowlisted app (163ffef9) which has the lab cert registered.
        /// </summary>
        [DoNotRunOnLinux]
        [TestMethod]
        public async Task ClientCredentials_WithSendCertificateOverMtls_BothMtlsConditionsMet()
        {
            const string MsiAllowListedAppId = "163ffef9-a313-45b4-ab2f-c7e2f5e0e23e";
            string[] vaultScopes = ["https://vault.azure.net/.default"];

            X509Certificate2 mtlsCert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);
            Assert.IsNotNull(mtlsCert, "Lab cert must be installed to run this test.");

            var recordingFactory = new RecordingMtlsHttpClientFactory();

            var cca = ConfidentialClientApplicationBuilder
                .Create(MsiAllowListedAppId)
                .WithAuthority("https://login.microsoftonline.com/bea21ebe-8b64-4d06-9f6d-6a889b120a7c")
                .WithAzureRegion("westus3")
                .WithCertificate(mtlsCert, new CertificateOptions { SendCertificateOverMtls = true })
                .WithHttpClientFactory(recordingFactory)
                .Build();

            AuthenticationResult result = await cca
                .AcquireTokenForClient(vaultScopes)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(result?.AccessToken, "Token acquisition should succeed for the MSI-allowlisted app.");

            string lastBody = recordingFactory.LastCapturedBody;
            string requestUrl = recordingFactory.LastCapturedUrl ?? "(none captured)";

            // Condition 1: request must go to mTLS endpoint
            StringAssert.Contains(requestUrl, "mtlsauth",
                $"Expected mTLS endpoint (mtlsauth) but got: {requestUrl}");

            // Condition 2: client_assertion must be in body
            StringAssert.Contains(lastBody, "client_assertion",
                "client_assertion should be in the body when SendCertificateOverMtls=true (cert at TLS + assertion in body).");
        }

        /// <summary>
        /// Tests the two conditions required for mTLS transport on the auth_code flow:
        ///   1. Token request goes to the mTLS endpoint (mtlsauth.microsoft.com).
        ///   2. <c>client_assertion</c> IS in the POST body (cert at TLS layer + assertion in body).
        ///
        /// Uses a fake/expired auth code to trigger the token request without a real browser session.
        /// AAD will reject the code, but the assertions verify MSAL's request format before the response.
        /// </summary>
        [DoNotRunOnLinux]
        [TestMethod]
        public async Task AuthCodeFlow_WithSendCertificateOverMtls_BothMtlsConditionsMetAsync()
        {
            X509Certificate2 mtlsCert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);
            Assert.IsNotNull(mtlsCert, "Lab cert must be installed to run this test.");

            var appConfig = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppS2S).ConfigureAwait(false);
            var user = await LabResponseHelper.GetUserConfigAsync(KeyVaultSecrets.UserPublicCloud).ConfigureAwait(false);

            var recordingFactory = new RecordingMtlsHttpClientFactory();
            var cca = ConfidentialClientApplicationBuilder
                .Create(appConfig.AppId)
                .WithAuthority(new Uri($"https://login.microsoftonline.com/{user.TenantId}"), true)
                .WithCertificate(mtlsCert, new CertificateOptions { SendCertificateOverMtls = true })
                .WithRedirectUri("http://localhost")
                .WithHttpClientFactory(recordingFactory)
                .Build();

            try
            {
                // Use a fake auth code to trigger the token request.
                // AAD will reject it, but MSAL sends the request first — we capture and assert on it.
                await cca
                    .AcquireTokenByAuthorizationCode(s_userReadScopes, "fake_auth_code_for_mtls_format_test")
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
            }
            catch (MsalServiceException)
            {
                // Expected — AAD rejects the fake code. We assert on the captured request below.
            }

            string requestUrl = recordingFactory.LastCapturedUrl ?? "(none captured)";
            string requestBody = recordingFactory.LastCapturedBody ?? "";

            // Condition 1: request must go to the mTLS endpoint
            StringAssert.Contains(requestUrl, "mtlsauth",
                $"Condition 1 FAILED: auth_code token request went to '{requestUrl}' instead of mtlsauth.microsoft.com.");

            // Condition 2: client_assertion must be in body
            StringAssert.Contains(requestBody, "client_assertion",
                "Condition 2 FAILED: client_assertion is NOT present in the auth_code POST body — should be present for mTLS transport.");
        }

        /// <summary>
        /// A recording mTLS factory that captures request URLs and bodies from BOTH the plain
        /// and cert-bearing HTTP client paths. Unlike HttpSnifferClientFactory, the cert path
        /// also uses a RecordingHandler so we can assert on the token endpoint URL.
        /// </summary>
        private class RecordingMtlsHttpClientFactory : IMsalMtlsHttpClientFactory
        {
            private readonly List<(string Url, string Body)> _captured = new();

            public IReadOnlyList<(string Url, string Body)> Captured => _captured;

            public string LastCapturedUrl => _captured.LastOrDefault(c => c.Url.Contains("/oauth2/")).Url;
            public string LastCapturedBody => _captured.LastOrDefault(c => !string.IsNullOrEmpty(c.Body)).Body;

            private HttpClient BuildRecordingClient(X509Certificate2 cert = null)
            {
                var inner = new HttpClientHandler();
                if (cert != null) inner.ClientCertificates.Add(cert);

                var recording = new RecordingHandler((req, _) =>
                {
                    string body = null;
                    if (req.Content != null)
                    {
                        req.Content.LoadIntoBufferAsync().GetAwaiter().GetResult();
                        body = req.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    }
                    lock (_captured) { _captured.Add((req.RequestUri?.AbsoluteUri ?? "", body ?? "")); }
                });
                recording.InnerHandler = inner;
                return new HttpClient(recording);
            }

            public HttpClient GetHttpClient() => BuildRecordingClient();
            public HttpClient GetHttpClient(X509Certificate2 cert) => BuildRecordingClient(cert);
        }

        private class TrackingMtlsHttpClientFactory : IMsalMtlsHttpClientFactory
        {
            private readonly X509Certificate2 _cert;
            private readonly HttpClient _mtlsClient;
            private readonly HttpClient _plainClient;
            private int _callCount;
            private int _mtlsUsedCount;

            public int GetHttpClientCallCount => _callCount;
            public int MtlsClientUsedCount => _mtlsUsedCount;

            public TrackingMtlsHttpClientFactory(X509Certificate2 cert)
            {
                _cert = cert ?? throw new ArgumentNullException(nameof(cert));

                var handler = new HttpClientHandler();
                handler.ClientCertificates.Add(_cert);
                _mtlsClient = new HttpClient(handler);

                _plainClient = new HttpClient();
            }

            public HttpClient GetHttpClient()
            {
                // Plain HTTP (no mTLS) — used for non-mTLS scenarios
                return _plainClient;
            }

            public HttpClient GetHttpClient(X509Certificate2 x509Certificate2)
            {
                Interlocked.Increment(ref _callCount);

                // Always return the mTLS client, even when x509Certificate2 is null.
                // This simulates how a real-world mTLS factory for user flows would behave —
                // the cert is baked in at construction, not passed per-call.
                Interlocked.Increment(ref _mtlsUsedCount);
                return _mtlsClient;
            }
        }
    }
}
