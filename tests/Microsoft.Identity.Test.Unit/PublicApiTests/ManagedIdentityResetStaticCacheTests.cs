// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.ManagedIdentity.V2;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.Identity.Test.Common.Core.Helpers.ManagedIdentityTestUtil;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    [DoNotParallelize] // Tests modify shared static state
    public class ManagedIdentityResetStaticCacheTests : TestBase
    {
        private static readonly CancellationToken s_cancellationToken =
            new CancellationTokenSource(TimeSpan.FromMinutes(5)).Token;

        private const string AppServiceEndpoint = "http://127.0.0.1:41564/msi/token";
        private const string CloudShellEndpoint = "http://localhost:40342/metadata/identity/oauth2/token";
        private const string Resource = "https://management.azure.com";

        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize(); // calls ApplicationBase.ResetStateForTest() which resets static state
        }

        [TestCleanup]
        public override void TestCleanup()
        {
            base.TestCleanup();
            ManagedIdentityApplicationBuilder.ResetStaticCache(); // ensure clean state after each test
        }

        /// <summary>
        /// Verify that ResetStaticCache() clears the static managed identity source cache.
        /// </summary>
        [TestMethod]
        public async Task ResetStaticCache_WithCachedSource_ClearsSourceCache()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.AppService, AppServiceEndpoint);

                var mi = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .Build() as ManagedIdentityApplication;

                // Force source detection and caching
                var sourceResult = await mi.GetManagedIdentitySourceAsync(s_cancellationToken).ConfigureAwait(false);
                Assert.AreEqual(ManagedIdentitySource.AppService, sourceResult.Source);
                Assert.AreEqual(ManagedIdentitySource.AppService, ManagedIdentityClient.s_sourceName, "Source should be cached as AppService.");

                // Act
                ManagedIdentityApplicationBuilder.ResetStaticCache();

                // Assert: static cache should be cleared
                Assert.AreEqual(ManagedIdentitySource.None, ManagedIdentityClient.s_sourceName, "Source cache should be cleared after reset.");
            }
        }

        /// <summary>
        /// Verify that ResetStaticCache() allows source detection to switch from ImdsV2 to ImdsV1.
        /// </summary>
        [TestMethod]
        public async Task ResetStaticCache_AfterImdsV2Detected_AllowsSwitchToImdsV1()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.ImdsV2, TestConstants.ImdsEndpoint);

                httpManager.AddMockHandler(MockHelpers.MockImdsProbe(ImdsVersion.V2));

                var mi = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .Build() as ManagedIdentityApplication;

                // Detect ImdsV2
                var sourceResult = await mi.GetManagedIdentitySourceAsync(s_cancellationToken).ConfigureAwait(false);
                Assert.AreEqual(ManagedIdentitySource.ImdsV2, sourceResult.Source);

                // Act: reset
                ManagedIdentityApplicationBuilder.ResetStaticCache();
                Assert.AreEqual(ManagedIdentitySource.None, ManagedIdentityClient.s_sourceName);

                // Now simulate ImdsV2 probe failure so ImdsV1 is detected
                httpManager.AddMockHandler(MockHelpers.MockImdsProbeFailure(ImdsVersion.V2));
                httpManager.AddMockHandler(MockHelpers.MockImdsProbe(ImdsVersion.V1));

                var mi2 = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .Build() as ManagedIdentityApplication;

                var sourceResult2 = await mi2.GetManagedIdentitySourceAsync(s_cancellationToken).ConfigureAwait(false);
                Assert.AreEqual(ManagedIdentitySource.Imds, sourceResult2.Source, "After reset, source detection should be able to detect ImdsV1.");
            }
        }

        /// <summary>
        /// Verify that ResetStaticCache() clears the preview mode fallback flag (s_imdsV1UsedForPreview).
        /// </summary>
        [TestMethod]
        public async Task ResetStaticCache_WithPreviewFlagSet_ClearsPreviewFlag()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.ImdsV2, TestConstants.ImdsEndpoint);

                httpManager.AddMockHandler(MockHelpers.MockImdsProbe(ImdsVersion.V2));
                httpManager.AddManagedIdentityMockHandler(
                    "http://169.254.169.254/metadata/identity/oauth2/token",
                    Resource,
                    MockHelpers.GetMsiSuccessfulResponse(),
                    ManagedIdentitySource.Imds);

                var mi = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .Build() as ManagedIdentityApplication;

                // Detect ImdsV2
                var sourceResult = await mi.GetManagedIdentitySourceAsync(s_cancellationToken).ConfigureAwait(false);
                Assert.AreEqual(ManagedIdentitySource.ImdsV2, sourceResult.Source);

                // A non-mTLS request causes fallback to ImdsV1, setting s_imdsV1UsedForPreview = true
                await mi.AcquireTokenForManagedIdentity(Resource)
                    // Without .WithMtlsProofOfPossession() to trigger ImdsV1 fallback
                    .ExecuteAsync(s_cancellationToken).ConfigureAwait(false);

                Assert.IsTrue(ManagedIdentityClient.s_imdsV1UsedForPreview, "Preview flag should be set after ImdsV1 fallback.");

                // Act: reset
                ManagedIdentityApplicationBuilder.ResetStaticCache();

                // Assert: preview flag should be cleared
                Assert.IsFalse(ManagedIdentityClient.s_imdsV1UsedForPreview, "Preview flag should be cleared after reset.");
                Assert.AreEqual(ManagedIdentitySource.None, ManagedIdentityClient.s_sourceName);
            }
        }

        /// <summary>
        /// Verify that ResetStaticCache() clears cached certificates.
        /// </summary>
        [TestMethod]
        public void ResetStaticCache_WithCachedCertificates_ClearsCertificateCache()
        {
            // Arrange: add a cert to the static cert cache directly
            var certBytes = Convert.FromBase64String(TestConstants.ValidRawCertificate);
            var cert = new X509Certificate2(certBytes);

            const string cacheKey = "test-cache-key";
            const string endpoint = "https://test.endpoint.com";
            const string clientId = "test-client-id";

            var cacheValue = new CertificateCacheValue(cert, endpoint, clientId);
            ImdsV2ManagedIdentitySource.s_mtlsCertificateCache.Set(cacheKey, cacheValue);

            // Verify cert was added
            Assert.IsTrue(
                ImdsV2ManagedIdentitySource.s_mtlsCertificateCache.TryGet(cacheKey, out _),
                "Cert should be in cache before reset.");

            // Act
            ManagedIdentityApplicationBuilder.ResetStaticCache();

            // Assert: cert cache should be cleared
            Assert.IsFalse(
                ImdsV2ManagedIdentitySource.s_mtlsCertificateCache.TryGet(cacheKey, out _),
                "Cert cache should be cleared after reset.");
        }

        /// <summary>
        /// Verify that calling ResetStaticCache() multiple times sequentially doesn't cause issues.
        /// </summary>
        [TestMethod]
        public void ResetStaticCache_CalledMultipleTimes_DoesNotThrow()
        {
            // Act: call reset multiple times
            ManagedIdentityApplicationBuilder.ResetStaticCache();
            ManagedIdentityApplicationBuilder.ResetStaticCache();
            ManagedIdentityApplicationBuilder.ResetStaticCache();

            // Assert: state should still be clean
            Assert.AreEqual(ManagedIdentitySource.None, ManagedIdentityClient.s_sourceName);
            Assert.IsFalse(ManagedIdentityClient.s_imdsV1UsedForPreview);
        }

        /// <summary>
        /// Verify that after ResetStaticCache(), a new managed identity app can re-detect the source.
        /// </summary>
        [TestMethod]
        public async Task ResetStaticCache_AfterReset_NewAppCanRedetectSource()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                // Step 1: Detect AppService source and cache it
                SetEnvironmentVariables(ManagedIdentitySource.AppService, AppServiceEndpoint);

                var mi1 = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .Build() as ManagedIdentityApplication;

                var sourceResult1 = await mi1.GetManagedIdentitySourceAsync(s_cancellationToken).ConfigureAwait(false);
                Assert.AreEqual(ManagedIdentitySource.AppService, sourceResult1.Source);

                // Step 2: Reset the cache
                ManagedIdentityApplicationBuilder.ResetStaticCache();

                // Step 3: Change environment to CloudShell
                // Clear AppService vars explicitly since EnvVariableContext only restores on dispose
                Environment.SetEnvironmentVariable("IDENTITY_ENDPOINT", null);
                Environment.SetEnvironmentVariable("IDENTITY_HEADER", null);
                SetEnvironmentVariables(ManagedIdentitySource.CloudShell, CloudShellEndpoint);

                // Step 4: New app re-detects source (should now detect CloudShell)
                var mi2 = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .Build() as ManagedIdentityApplication;

                var sourceResult2 = await mi2.GetManagedIdentitySourceAsync(s_cancellationToken).ConfigureAwait(false);
                Assert.AreEqual(ManagedIdentitySource.CloudShell, sourceResult2.Source,
                    "After reset, a new app should re-detect the source based on current environment variables.");
            }
        }
    }
}
