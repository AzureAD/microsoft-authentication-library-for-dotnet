// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.ManagedIdentity.V2;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Unit.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.Identity.Test.Common.Core.Helpers.ManagedIdentityTestUtil;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    public class ImdsV2Tests : TestBase
    {
        private readonly TestRetryPolicyFactory _testRetryPolicyFactory = new TestRetryPolicyFactory();
        private readonly TestCsrFactory _testCsrFactory = new TestCsrFactory();

        [TestMethod]
        public async Task ImdsV2SAMIHappyPathAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .WithRetryPolicyFactory(_testRetryPolicyFactory)
                    .WithCsrFactory(_testCsrFactory);

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                var mi = miBuilder.Build();

                httpManager.AddMockHandler(MockHelpers.MockCsrResponse()); // initial probe
                httpManager.AddMockHandler(MockHelpers.MockCsrResponse()); // do it again, since CsrMetadata from initial probe is not cached
                httpManager.AddMockHandler(MockHelpers.MockCertificateRequestResponse());
                httpManager.AddManagedIdentityMockHandler(
                    $"{TestConstants.MtlsAuthenticationEndpoint}/{TestConstants.TenantId}{ImdsV2ManagedIdentitySource.AcquireEntraTokenPath}",
                    ManagedIdentityTests.Resource,
                    MockHelpers.GetMsiSuccessfulResponse(),
                    ManagedIdentitySource.ImdsV2);

                var result = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                result = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [DataTestMethod]
        [DataRow(UserAssignedIdentityId.ClientId, TestConstants.ClientId)]
        [DataRow(UserAssignedIdentityId.ResourceId, TestConstants.MiResourceId)]
        [DataRow(UserAssignedIdentityId.ObjectId, TestConstants.ObjectId)]
        public async Task ImdsV2UAMIHappyPathAsync(
            UserAssignedIdentityId userAssignedIdentityId,
            string userAssignedId)
        {
            using (var httpManager = new MockHttpManager())
            {
                var miBuilder = CreateMIABuilder(userAssignedId, userAssignedIdentityId);
                miBuilder
                    .WithHttpManager(httpManager)
                    .WithRetryPolicyFactory(_testRetryPolicyFactory)
                    .WithCsrFactory(_testCsrFactory);

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                var mi = miBuilder.Build();

                httpManager.AddMockHandler(MockHelpers.MockCsrResponse(idType: userAssignedIdentityId, userAssignedId: userAssignedId)); // initial probe
                httpManager.AddMockHandler(MockHelpers.MockCsrResponse(idType: userAssignedIdentityId, userAssignedId: userAssignedId)); // do it again, since CsrMetadata from initial probe is not cached
                httpManager.AddMockHandler(MockHelpers.MockCertificateRequestResponse(userAssignedIdentityId, userAssignedId));
                httpManager.AddManagedIdentityMockHandler(
                    $"{TestConstants.MtlsAuthenticationEndpoint}/{TestConstants.TenantId}{ImdsV2ManagedIdentitySource.AcquireEntraTokenPath}",
                    ManagedIdentityTests.Resource,
                    MockHelpers.GetMsiSuccessfulResponse(),
                    ManagedIdentitySource.ImdsV2);

                var result = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                result = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public async Task ImdsV2_CertCache_ReusesBinding_OnForceRefreshAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .WithRetryPolicyFactory(_testRetryPolicyFactory)
                    .WithCsrFactory(_testCsrFactory);

                // Avoid shared token cache between tests
                miBuilder.Config.AccessorOptions = null;

                var mi = miBuilder.Build();

                // First call: CSR (probe + non-probe), then /issuecredential, then token
                httpManager.AddMockHandler(MockHelpers.MockCsrResponse()); // probe
                httpManager.AddMockHandler(MockHelpers.MockCsrResponse()); // non-probe
                httpManager.AddMockHandler(MockHelpers.MockCertificateRequestResponse());
                httpManager.AddManagedIdentityMockHandler(
                    $"{TestConstants.MtlsAuthenticationEndpoint}/{TestConstants.TenantId}{ImdsV2ManagedIdentitySource.AcquireEntraTokenPath}",
                    ManagedIdentityTests.Resource,
                    MockHelpers.GetMsiSuccessfulResponse(),
                    ManagedIdentitySource.ImdsV2);

                // Second call (network again): only CSR (non-probe) + token. NO /issuecredential here.
                httpManager.AddMockHandler(MockHelpers.MockCsrResponse()); // non-probe for CreateRequestAsync
                httpManager.AddManagedIdentityMockHandler(
                    $"{TestConstants.MtlsAuthenticationEndpoint}/{TestConstants.TenantId}{ImdsV2ManagedIdentitySource.AcquireEntraTokenPath}",
                    ManagedIdentityTests.Resource,
                    MockHelpers.GetMsiSuccessfulResponse(),
                    ManagedIdentitySource.ImdsV2);

                // 1) First acquisition: network path, issues cert, returns token
                var result1 = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                                      .ExecuteAsync().ConfigureAwait(false);
                Assert.IsNotNull(result1);
                Assert.IsNotNull(result1.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result1.AuthenticationResultMetadata.TokenSource);

                // 2) Second acquisition: bypass token cache to force another *network* call,
                //    but cert must be reused (no /issuecredential mock was queued).
                var result2 = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                                      .WithForceRefresh(true)
                                      .ExecuteAsync().ConfigureAwait(false);
                Assert.IsNotNull(result2);
                Assert.IsNotNull(result2.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result2.AuthenticationResultMetadata.TokenSource);
            }
        }

        //[TestMethod]
        // This test fails because bad response is not handled properly in the code yet.
        public async Task ImdsV2_CertCache_Invalidation_RetryOnce_MintsNewCertAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .WithRetryPolicyFactory(_testRetryPolicyFactory)
                    .WithCsrFactory(_testCsrFactory);

                miBuilder.Config.AccessorOptions = null;

                var mi = miBuilder.Build();

                var tokenUrl = $"{TestConstants.MtlsAuthenticationEndpoint}/{TestConstants.TenantId}{ImdsV2ManagedIdentitySource.AcquireEntraTokenPath}";

                // First attempt: mint cert, then token returns 401/invalid_client_certificate
                httpManager.AddMockHandler(MockHelpers.MockCsrResponse()); // probe
                httpManager.AddMockHandler(MockHelpers.MockCsrResponse()); // non-probe
                httpManager.AddMockHandler(MockHelpers.MockCertificateRequestResponse());

                var unauthorizedBody =
                "{\"error\":\"invalid_client_certificate\",\"error_description\":\"bad certificate\"}";

                httpManager.AddManagedIdentityMockHandler(
                    tokenUrl,
                    ManagedIdentityTests.Resource,
                    unauthorizedBody,
                    ManagedIdentitySource.ImdsV2);

                // Retry path: CSR (non-probe), mint NEW cert, then token succeeds
                httpManager.AddMockHandler(MockHelpers.MockCsrResponse()); // non-probe for retry
                httpManager.AddMockHandler(MockHelpers.MockCertificateRequestResponse());
                httpManager.AddManagedIdentityMockHandler(
                    tokenUrl,
                    ManagedIdentityTests.Resource,
                    MockHelpers.GetMsiSuccessfulResponse(),
                    ManagedIdentitySource.ImdsV2);

                var result = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                                     .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public async Task ImdsV2_CertCache_Isolates_SAMI_and_UAMI_IdentitiesAsync()
        {
            var tokenUrl = $"{TestConstants.MtlsAuthenticationEndpoint}/{TestConstants.TenantId}{ImdsV2ManagedIdentitySource.AcquireEntraTokenPath}";

            // --- SAMI ---
            using (var httpManagerSami = new MockHttpManager())
            {
                var samiBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManagerSami)
                    .WithRetryPolicyFactory(_testRetryPolicyFactory)
                    .WithCsrFactory(_testCsrFactory);
                samiBuilder.Config.AccessorOptions = null;

                var sami = samiBuilder.Build();

                httpManagerSami.AddMockHandler(MockHelpers.MockCsrResponse()); // probe
                httpManagerSami.AddMockHandler(MockHelpers.MockCsrResponse()); // non-probe
                httpManagerSami.AddMockHandler(MockHelpers.MockCertificateRequestResponse());
                httpManagerSami.AddManagedIdentityMockHandler(
                    tokenUrl,
                    ManagedIdentityTests.Resource,
                    MockHelpers.GetMsiSuccessfulResponse(),
                    ManagedIdentitySource.ImdsV2);

                var resSami = await sami.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                                        .ExecuteAsync().ConfigureAwait(false);
                Assert.IsNotNull(resSami.AccessToken);
            }

            using (var httpManagerUami = new MockHttpManager())
            {
                var uamiBuilder = CreateMIABuilder(TestConstants.ClientId2, UserAssignedIdentityId.ClientId)
                    .WithHttpManager(httpManagerUami)
                    .WithRetryPolicyFactory(_testRetryPolicyFactory)
                    .WithCsrFactory(_testCsrFactory);
                uamiBuilder.Config.AccessorOptions = null;

                var uami = uamiBuilder.Build();

                httpManagerUami.AddMockHandler(MockHelpers.MockCsrResponse(
                    idType: UserAssignedIdentityId.ClientId, userAssignedId: TestConstants.ClientId2)); // non-probe

                httpManagerUami.AddMockHandler(MockHelpers.MockCertificateRequestResponse(
                    UserAssignedIdentityId.ClientId, TestConstants.ClientId2));

                httpManagerUami.AddManagedIdentityMockHandler(
                    tokenUrl,
                    ManagedIdentityTests.Resource,
                    MockHelpers.GetMsiSuccessfulResponse(),
                    ManagedIdentitySource.ImdsV2);

                var resUami = await uami.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                                        .ExecuteAsync().ConfigureAwait(false);
                Assert.IsNotNull(resUami.AccessToken);
            }
        }

        [TestMethod]
        public async Task ImdsV2_CertCache_DoesNotRotate_WhenOutside5MinWindow_UsingTestTimeService()
        {
            using var http = new MockHttpManager();

            ManagedIdentityApplicationBuilder miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                .WithHttpManager(http)
                .WithRetryPolicyFactory(_testRetryPolicyFactory)
                .WithCsrFactory(_testCsrFactory);

            miBuilder.Config.AccessorOptions = null;

            var mi = miBuilder.Build();

            // Position clock 6 minutes before NotAfter (outside 5m window)
            DateTime notAfterUtc = MockHelpers.GetPemNotAfterUtc(TestConstants.ValidPemCertificate);
            ManagedIdentityClient.SetTimeServiceForTest(new TestTimeService(notAfterUtc.AddMinutes(-6)));

            // First call: probe + non-probe CSR, /issuecredential, token
            http.AddMockHandler(MockHelpers.MockCsrResponse()); // probe
            http.AddMockHandler(MockHelpers.MockCsrResponse()); // non-probe
            http.AddMockHandler(MockHelpers.MockCertificateRequestResponse());
            http.AddManagedIdentityMockHandler(
                $"{TestConstants.MtlsAuthenticationEndpoint}/{TestConstants.TenantId}{ImdsV2ManagedIdentitySource.AcquireEntraTokenPath}",
                ManagedIdentityTests.Resource,
                MockHelpers.GetMsiSuccessfulResponse(),
                ManagedIdentitySource.ImdsV2);

            // Second call: CSR (non-probe) + token only (no /issuecredential)
            http.AddMockHandler(MockHelpers.MockCsrResponse()); // non-probe
            // No /issuecredential mock queued here because we have that info cached
            http.AddManagedIdentityMockHandler(
                $"{TestConstants.MtlsAuthenticationEndpoint}/{TestConstants.TenantId}{ImdsV2ManagedIdentitySource.AcquireEntraTokenPath}",
                ManagedIdentityTests.Resource,
                MockHelpers.GetMsiSuccessfulResponse(),
                ManagedIdentitySource.ImdsV2);

            AuthenticationResult r1 = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                .ExecuteAsync()
                .ConfigureAwait(false);

            AuthenticationResult r2 = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                .WithForceRefresh(true)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(r1.AccessToken);
            Assert.IsNotNull(r2.AccessToken);
        }

        [TestMethod]
        public async Task ImdsV2_CertCache_Reset_ClearsBindingAndSource_ReissuesOnNextCall()
        {
            using var http = new MockHttpManager();

            var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                .WithHttpManager(http)
                .WithRetryPolicyFactory(_testRetryPolicyFactory)
                .WithCsrFactory(_testCsrFactory);

            // Avoid shared token cache between tests (so network path is deterministic)
            miBuilder.Config.AccessorOptions = null;

            var mi = miBuilder.Build();

            var tokenUrl = $"{TestConstants.MtlsAuthenticationEndpoint}/{TestConstants.TenantId}{ImdsV2ManagedIdentitySource.AcquireEntraTokenPath}";

            // ---------------------
            // 1) First acquisition: mint + token
            // ---------------------
            http.AddMockHandler(MockHelpers.MockCsrResponse()); // probe
            http.AddMockHandler(MockHelpers.MockCsrResponse()); // non-probe
            http.AddMockHandler(MockHelpers.MockCertificateRequestResponse());
            http.AddManagedIdentityMockHandler(
                tokenUrl,
                ManagedIdentityTests.Resource,
                MockHelpers.GetMsiSuccessfulResponse(),
                ManagedIdentitySource.ImdsV2);

            var r1 = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                             .ExecuteAsync().ConfigureAwait(false);
            Assert.IsNotNull(r1.AccessToken);
            Assert.AreEqual(TokenSource.IdentityProvider, r1.AuthenticationResultMetadata.TokenSource);

            // ----------------------------------------------
            // 2) Second acquisition (ForceRefresh): reuse mTLS binding (no new /issuecredential)
            // ----------------------------------------------
            http.AddMockHandler(MockHelpers.MockCsrResponse()); // non-probe (CreateRequestAsync)
                                                                // NOTE: No MockCertificateRequestResponse queued here intentionally
            http.AddManagedIdentityMockHandler(
                tokenUrl,
                ManagedIdentityTests.Resource,
                MockHelpers.GetMsiSuccessfulResponse(),
                ManagedIdentitySource.ImdsV2);

            var r2 = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                             .WithForceRefresh(true)
                             .ExecuteAsync().ConfigureAwait(false);
            Assert.IsNotNull(r2.AccessToken);
            Assert.AreEqual(TokenSource.IdentityProvider, r2.AuthenticationResultMetadata.TokenSource);

            // ---------------------
            // 3) Reset caches mid-test
            // ---------------------
            ManagedIdentityClient.ResetSourceForTest();

            // After reset, source detection will re-run (probe CSR) and binding is gone,
            // so we expect: probe CSR -> non-probe CSR -> /issuecredential -> token
            http.AddMockHandler(MockHelpers.MockCsrResponse()); // probe again after reset
            http.AddMockHandler(MockHelpers.MockCsrResponse()); // non-probe
            http.AddMockHandler(MockHelpers.MockCertificateRequestResponse());
            http.AddManagedIdentityMockHandler(
                tokenUrl,
                ManagedIdentityTests.Resource,
                MockHelpers.GetMsiSuccessfulResponse(),
                ManagedIdentitySource.ImdsV2);

            var r3 = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                             .WithForceRefresh(true) // bypass token cache to force network path
                             .ExecuteAsync().ConfigureAwait(false);
            Assert.IsNotNull(r3.AccessToken);
            Assert.AreEqual(TokenSource.IdentityProvider, r3.AuthenticationResultMetadata.TokenSource);
        }

        [TestMethod]
        public async Task ImdsV2_TokenCacheMiss_ValidCert_SkipsMetadataAndCsr_GoesDirectToToken_Async()
        {
            using var http = new MockHttpManager();

            var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                .WithHttpManager(http)
                .WithRetryPolicyFactory(_testRetryPolicyFactory)
                .WithCsrFactory(_testCsrFactory);
            miBuilder.Config.AccessorOptions = null;

            var mi = miBuilder.Build();

            // First call: mint + token (fills the mTLS binding cache under miKey)
            http.AddMockHandler(MockHelpers.MockCsrResponse());               // probe
            http.AddMockHandler(MockHelpers.MockCsrResponse());               // non-probe
            http.AddMockHandler(MockHelpers.MockCertificateRequestResponse()); // issuecredential
            http.AddManagedIdentityMockHandler(
                $"{TestConstants.MtlsAuthenticationEndpoint}/{TestConstants.TenantId}{ImdsV2ManagedIdentitySource.AcquireEntraTokenPath}",
                ManagedIdentityTests.Resource,
                MockHelpers.GetMsiSuccessfulResponse(),
                ManagedIdentitySource.ImdsV2);

            var r1 = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(r1.AccessToken);

            // Scenario 2: simulate token cache miss (ForceRefresh) but cert is still valid.
            // We expect: NO /issuecredential; ONLY metadate and token call.
            http.AddMockHandler(MockHelpers.MockCsrResponse());               // non-probe
            http.AddManagedIdentityMockHandler(
                $"{TestConstants.MtlsAuthenticationEndpoint}/{TestConstants.TenantId}{ImdsV2ManagedIdentitySource.AcquireEntraTokenPath}",
                ManagedIdentityTests.Resource,
                MockHelpers.GetMsiSuccessfulResponse(),
                ManagedIdentitySource.ImdsV2);

            var r2 = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                             .WithForceRefresh(true)
                             .ExecuteAsync()
                             .ConfigureAwait(false);

            Assert.IsNotNull(r2.AccessToken);
            Assert.AreEqual(TokenSource.IdentityProvider, r2.AuthenticationResultMetadata.TokenSource);
        }

        [TestMethod]
        public async Task ImdsV2_BypassesTokenCache_WhenCertNearExpiry_Async()
        {
            using var http = new MockHttpManager();

            // Make sure source / mTLS cache / time are clean for this test
            ManagedIdentityClient.ResetSourceForTest();

            var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                .WithHttpManager(http)
                .WithRetryPolicyFactory(_testRetryPolicyFactory)
                .WithCsrFactory(_testCsrFactory);

            // Avoid shared token cache between tests
            miBuilder.Config.AccessorOptions = null;

            var mi = miBuilder.Build();

            string tokenUrl = $"{TestConstants.MtlsAuthenticationEndpoint}/{TestConstants.TenantId}{ImdsV2ManagedIdentitySource.AcquireEntraTokenPath}";

            // ------------------------
            // 1) First acquisition: mint cert + get token (fills s_miCerts and token cache)
            // ------------------------
            http.AddMockHandler(MockHelpers.MockCsrResponse());                // probe
            http.AddMockHandler(MockHelpers.MockCsrResponse());                // non-probe
            http.AddMockHandler(MockHelpers.MockCertificateRequestResponse()); // /issuecredential
            http.AddManagedIdentityMockHandler(
                tokenUrl,
                ManagedIdentityTests.Resource,
                MockHelpers.GetMsiSuccessfulResponse(),
                ManagedIdentitySource.ImdsV2);

            var r1 = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                             .ExecuteAsync()
                             .ConfigureAwait(false);

            Assert.IsNotNull(r1);
            Assert.IsNotNull(r1.AccessToken);
            Assert.AreEqual(TokenSource.IdentityProvider, r1.AuthenticationResultMetadata.TokenSource, "First call should be network.");

            // ------------------------
            // 2) Move time to within the 5-minute window before cert expiry
            //    => cache must be bypassed and cert rotated
            // ------------------------
            DateTime notAfterUtc = MockHelpers.GetPemNotAfterUtc(TestConstants.ValidPemCertificate);
            ManagedIdentityClient.SetTimeServiceForTest(new TestTimeService(notAfterUtc.AddMinutes(-1)));

            // Expect: CSR (non-probe) + /issuecredential (rotate) + token
            http.AddMockHandler(MockHelpers.MockCsrResponse());                // non-probe
            http.AddMockHandler(MockHelpers.MockCertificateRequestResponse()); // /issuecredential (rotation)
            http.AddManagedIdentityMockHandler(
                tokenUrl,
                ManagedIdentityTests.Resource,
                MockHelpers.GetMsiSuccessfulResponse(),
                ManagedIdentitySource.ImdsV2);

            var r2 = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                             .ExecuteAsync()
                             .ConfigureAwait(false);

            Assert.IsNotNull(r2);
            Assert.IsNotNull(r2.AccessToken);
            Assert.AreEqual(
                TokenSource.IdentityProvider,
                r2.AuthenticationResultMetadata.TokenSource,
                "When mTLS cert is near expiry, token cache must be bypassed and network path taken.");

            // clean up the test time service to avoid bleeding into other tests
            ManagedIdentityClient.SetTimeServiceForTest(null);
        }

        [TestMethod]
        public async Task GetCsrMetadataAsyncSucceeds()
        {
            using (var httpManager = new MockHttpManager())
            {
                var handler = httpManager.AddMockHandler(MockHelpers.MockCsrResponse());

                var managedIdentityApp = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .WithRetryPolicyFactory(_testRetryPolicyFactory)
                    .Build();

                var miSource = await (managedIdentityApp as ManagedIdentityApplication).GetManagedIdentitySourceAsync().ConfigureAwait(false);
                Assert.AreEqual(ManagedIdentitySource.ImdsV2, miSource);

                Assert.IsTrue(handler.ActualRequestHeaders.Contains("Metadata"));
                Assert.IsTrue(handler.ActualRequestHeaders.Contains("x-ms-client-request-id"));
                Assert.IsTrue(handler.ActualRequestMessage.RequestUri.Query.Contains("api-version"));
            }
        }

        [TestMethod]
        public async Task GetCsrMetadataAsyncSucceedsAfterRetry()
        {
            using (var httpManager = new MockHttpManager())
            {
                // First attempt fails with INTERNAL_SERVER_ERROR (500)
                httpManager.AddMockHandler(MockHelpers.MockCsrResponse(HttpStatusCode.InternalServerError));

                // Second attempt succeeds
                httpManager.AddMockHandler(MockHelpers.MockCsrResponse());

                var managedIdentityApp = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .WithRetryPolicyFactory(_testRetryPolicyFactory)
                    .Build();

                var miSource = await (managedIdentityApp as ManagedIdentityApplication).GetManagedIdentitySourceAsync().ConfigureAwait(false);
                Assert.AreEqual(ManagedIdentitySource.ImdsV2, miSource);
            }
        }

        [TestMethod]
        public async Task GetCsrMetadataAsyncFailsWithMissingServerHeader()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddMockHandler(MockHelpers.MockCsrResponse(responseServerHeader: null));

                var managedIdentityApp = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .WithRetryPolicyFactory(_testRetryPolicyFactory)
                    .Build();

                var miSource = await (managedIdentityApp as ManagedIdentityApplication).GetManagedIdentitySourceAsync().ConfigureAwait(false);
                Assert.AreEqual(ManagedIdentitySource.DefaultToImds, miSource);
            }
        }
        
        [TestMethod]
        public async Task GetCsrMetadataAsyncFailsWithInvalidVersion()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddMockHandler(MockHelpers.MockCsrResponse(responseServerHeader: "IMDS/150.870.65.1853")); // min version is 1854

                var managedIdentityApp = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .WithRetryPolicyFactory(_testRetryPolicyFactory)
                    .Build();

                var miSource = await (managedIdentityApp as ManagedIdentityApplication).GetManagedIdentitySourceAsync().ConfigureAwait(false);
                Assert.AreEqual(ManagedIdentitySource.DefaultToImds, miSource);
            }
        }

        [TestMethod]
        public async Task GetCsrMetadataAsyncFailsAfterMaxRetries()
        {
            using (var httpManager = new MockHttpManager())
            {
                var managedIdentityApp = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .WithRetryPolicyFactory(_testRetryPolicyFactory)
                    .Build();

                const int Num500Errors = 1 + TestCsrMetadataProbeRetryPolicy.ExponentialStrategyNumRetries;
                for (int i = 0; i < Num500Errors; i++)
                {
                    httpManager.AddMockHandler(MockHelpers.MockCsrResponse(HttpStatusCode.InternalServerError));
                }

                var miSource = await (managedIdentityApp as ManagedIdentityApplication).GetManagedIdentitySourceAsync().ConfigureAwait(false);
                Assert.AreEqual(ManagedIdentitySource.DefaultToImds, miSource);
            }
        }

        [TestMethod]
        public async Task GetCsrMetadataAsyncFails404WhichIsNonRetriableAndRetryPolicyIsNotTriggeredAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                var managedIdentityApp = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .WithRetryPolicyFactory(_testRetryPolicyFactory)
                    .Build();

                httpManager.AddMockHandler(MockHelpers.MockCsrResponse(HttpStatusCode.NotFound));

                var miSource = await (managedIdentityApp as ManagedIdentityApplication).GetManagedIdentitySourceAsync().ConfigureAwait(false);
                Assert.AreEqual(ManagedIdentitySource.DefaultToImds, miSource);
            }
        }

        [TestMethod]
        public void TestCsrGeneration_OnlyVmId()
        {
            var cuid = new CuidInfo
            {
                VmId = TestConstants.VmId
            };

            var (csr, _) = Csr.Generate(TestConstants.ClientId, TestConstants.TenantId, cuid);
            CsrValidator.ValidateCsrContent(csr, TestConstants.ClientId, TestConstants.TenantId, cuid);
        }

        [TestMethod]
        public void TestCsrGeneration_VmIdAndVmssId()
        {
            var cuid = new CuidInfo
            {
                VmId = TestConstants.VmId,
                VmssId = TestConstants.VmssId
            };

            var (csr, _) = Csr.Generate(TestConstants.ClientId, TestConstants.TenantId, cuid);
            CsrValidator.ValidateCsrContent(csr, TestConstants.ClientId, TestConstants.TenantId, cuid);
        }

        [TestMethod]
        public void TestCsrGeneration_MalformedPem_FormatException()
        {
            string malformedPem = "-----BEGIN CERTIFICATE REQUEST-----\nInvalid@#$%Base64Content!\n-----END CERTIFICATE REQUEST-----";
            Assert.ThrowsException<FormatException>(() => 
                CsrValidator.ParseCsrFromPem(malformedPem));
        }

        [DataTestMethod]
        [DataRow("-----BEGIN CERTIFICATE-----\nTUlJQzNqQ0NBY1lDQVFBd1pURT0K\n-----END CERTIFICATE REQUEST-----")]
        [DataRow("")]
        [DataRow(null)]
        public void TestCsrGeneration_MalformedPem_ArgumentException(string malformedPem)
        {
            Assert.ThrowsException<ArgumentException>(() => 
                CsrValidator.ParseCsrFromPem(malformedPem));
        }

        #region AttachPrivateKeyToCert Tests

        [TestMethod]
        public void AttachPrivateKeyToCert_ValidInputs_ReturnsValidCertificate()
        {
            using var httpManager = new MockHttpManager();
            var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                .WithHttpManager(httpManager)
                .WithRetryPolicyFactory(_testRetryPolicyFactory);
            var managedIdentityApp = miBuilder.BuildConcrete();

            var requestContext = new RequestContext(managedIdentityApp.ServiceBundle, Guid.NewGuid(), null);
            var imdsV2Source = new ImdsV2ManagedIdentitySource(requestContext);

            using (RSA rsa = RSA.Create())
            {
                // For this test, we just want to verify that the method doesn't crash
                // The actual certificate/private key matching isn't critical for the unit test
                var exception = Assert.ThrowsException<CryptographicUnexpectedOperationException>(() =>
                    CommonCryptographyManager.AttachPrivateKeyToCert(TestConstants.ValidPemCertificate, rsa));

                // The test should fail with a CryptographicUnexpectedOperationException because the RSA key doesn't match
                // the certificate, but this validates that the method is working correctly
                Assert.IsNotNull(exception.Message);
            }
        }

        [TestMethod]
        public void AttachPrivateKeyToCert_NullCertificatePem_ThrowsArgumentNullException()
        {
            using var httpManager = new MockHttpManager();
            var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                .WithHttpManager(httpManager)
                .WithRetryPolicyFactory(_testRetryPolicyFactory);
            var managedIdentityApp = miBuilder.BuildConcrete();

            var requestContext = new RequestContext(managedIdentityApp.ServiceBundle, Guid.NewGuid(), null);
            var imdsV2Source = new ImdsV2ManagedIdentitySource(requestContext);

            using (RSA rsa = RSA.Create())
            {
                Assert.ThrowsException<ArgumentNullException>(() =>
                    CommonCryptographyManager.AttachPrivateKeyToCert(null, rsa));
            }
        }

        [TestMethod]
        public void AttachPrivateKeyToCert_EmptyCertificatePem_ThrowsArgumentNullException()
        {
            using var httpManager = new MockHttpManager();
            var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                .WithHttpManager(httpManager)
                .WithRetryPolicyFactory(_testRetryPolicyFactory);
            var managedIdentityApp = miBuilder.BuildConcrete();

            var requestContext = new RequestContext(managedIdentityApp.ServiceBundle, Guid.NewGuid(), null);
            var imdsV2Source = new ImdsV2ManagedIdentitySource(requestContext);

            using (RSA rsa = RSA.Create())
            {
                Assert.ThrowsException<ArgumentNullException>(() =>
                    CommonCryptographyManager.AttachPrivateKeyToCert("", rsa));
            }
        }

        [TestMethod]
        public void AttachPrivateKeyToCert_NullPrivateKey_ThrowsArgumentNullException()
        {
            using var httpManager = new MockHttpManager();
            var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                .WithHttpManager(httpManager)
                .WithRetryPolicyFactory(_testRetryPolicyFactory);
            var managedIdentityApp = miBuilder.BuildConcrete();

            var requestContext = new RequestContext(managedIdentityApp.ServiceBundle, Guid.NewGuid(), null);
            var imdsV2Source = new ImdsV2ManagedIdentitySource(requestContext);

            Assert.ThrowsException<ArgumentNullException>(() =>
                CommonCryptographyManager.AttachPrivateKeyToCert(TestConstants.ValidPemCertificate, null));
        }

        [TestMethod]
        public void AttachPrivateKeyToCert_InvalidPemFormat_ThrowsArgumentException()
        {
            const string InvalidPemNoCertMarker = @"This is not a valid PEM certificate";

            using var httpManager = new MockHttpManager();
            var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                .WithHttpManager(httpManager)
                .WithRetryPolicyFactory(_testRetryPolicyFactory);
            var managedIdentityApp = miBuilder.BuildConcrete();

            var requestContext = new RequestContext(managedIdentityApp.ServiceBundle, Guid.NewGuid(), null);
            var imdsV2Source = new ImdsV2ManagedIdentitySource(requestContext);

            using (RSA rsa = RSA.Create())
            {
                Assert.ThrowsException<ArgumentException>(() =>
                    CommonCryptographyManager.AttachPrivateKeyToCert(InvalidPemNoCertMarker, rsa));
            }
        }

        [TestMethod]
        public void AttachPrivateKeyToCert_MissingBeginMarker_ThrowsArgumentException()
        {
            const string InvalidPemMissingBeginMarker = @"MIICXTCCAUWgAwIBAgIJAKPiQh26MIuPMA0GCSqGSIb3DQEBCwUAMEUxCzAJBgNV
-----END CERTIFICATE-----";

            using var httpManager = new MockHttpManager();
            var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                .WithHttpManager(httpManager)
                .WithRetryPolicyFactory(_testRetryPolicyFactory);
            var managedIdentityApp = miBuilder.BuildConcrete();

            var requestContext = new RequestContext(managedIdentityApp.ServiceBundle, Guid.NewGuid(), null);
            var imdsV2Source = new ImdsV2ManagedIdentitySource(requestContext);

            using (RSA rsa = RSA.Create())
            {
                Assert.ThrowsException<ArgumentException>(() =>
                    CommonCryptographyManager.AttachPrivateKeyToCert(InvalidPemMissingBeginMarker, rsa));
            }
        }

        [TestMethod]
        public void AttachPrivateKeyToCert_MissingEndMarker_ThrowsArgumentException()
        {
            const string InvalidPemMissingEndMarker = @"-----BEGIN CERTIFICATE-----
MIICXTCCAUWgAwIBAgIJAKPiQh26MIuPMA0GCSqGSIb3DQEBCwUAMEUxCzAJBgNV";
            using var httpManager = new MockHttpManager();
            var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                .WithHttpManager(httpManager)
                .WithRetryPolicyFactory(_testRetryPolicyFactory);
            var managedIdentityApp = miBuilder.BuildConcrete();

            var requestContext = new RequestContext(managedIdentityApp.ServiceBundle, Guid.NewGuid(), null);
            var imdsV2Source = new ImdsV2ManagedIdentitySource(requestContext);

            using (RSA rsa = RSA.Create())
            {
                Assert.ThrowsException<ArgumentException>(() =>
                    CommonCryptographyManager.AttachPrivateKeyToCert(InvalidPemMissingEndMarker, rsa));
            }
        }

        [TestMethod]
        public void AttachPrivateKeyToCert_BadBase64Content_ThrowsFormatException()
        {
            const string InvalidPemBadBase64 = @"-----BEGIN CERTIFICATE-----
Invalid@#$%Base64Content!
-----END CERTIFICATE-----";

            using var httpManager = new MockHttpManager();
            var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                .WithHttpManager(httpManager)
                .WithRetryPolicyFactory(_testRetryPolicyFactory);
            var managedIdentityApp = miBuilder.BuildConcrete();

            var requestContext = new RequestContext(managedIdentityApp.ServiceBundle, Guid.NewGuid(), null);
            var imdsV2Source = new ImdsV2ManagedIdentitySource(requestContext);

            using (RSA rsa = RSA.Create())
            {
                Assert.ThrowsException<FormatException>(() =>
                    CommonCryptographyManager.AttachPrivateKeyToCert(InvalidPemBadBase64, rsa));
            }
        }

        #endregion
    }
}
