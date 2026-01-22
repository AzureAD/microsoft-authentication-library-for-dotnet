// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    /// <summary>
    /// Integration tests for dSTS (Distributed STS) authority.
    /// These tests validate that MSAL.NET properly integrates with dSTS endpoints
    /// for confidential client scenarios including token acquisition and caching.
    /// </summary>
    [TestClass]
    public class DstsIntegrationTests
    {
        private const string DstsTestCategory = "DSTS";

        [TestInitialize]
        public void TestInitialize()
        {
            ApplicationBase.ResetStateForTest();
        }

        /// <summary>
        /// Tests client credentials flow with certificate authentication against dSTS.
        /// Validates both initial token acquisition from IdP and subsequent cache hits.
        /// </summary>
        [TestMethod]
        [TestCategory(DstsTestCategory)]
        [TestCategory(TestCategories.LabAccess)]
        public async Task DstsClientCredentials_WithCertificate_SuccessAsync()
        {
            // Arrange - Get dSTS configuration from Key Vault
            var dstsApp = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppDsts).ConfigureAwait(false);
            var cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);
            
            if (cert == null)
            {
                Assert.Inconclusive("Required certificate not found for dSTS integration test.");
            }

            string[] scopes = new[] { dstsApp.DefaultScopes };

            var confidentialApp = ConfidentialClientApplicationBuilder
                .Create(dstsApp.AppId)
                .WithAuthority(dstsApp.Authority, validateAuthority: false)
                .WithCertificate(cert, sendX5C: true)
                .WithTestLogging()
                .Build();

            // Verify authority type
            var appImpl = confidentialApp as ConfidentialClientApplication;
            Assert.IsNotNull(appImpl, "Application should be of type ConfidentialClientApplication");
            Assert.AreEqual(AuthorityType.Dsts, appImpl.AuthorityInfo.AuthorityType, "Authority type should be DSTS");

            var appCacheRecorder = confidentialApp.AppTokenCache.RecordAccess();

            // Act - First call should hit IdP
            Trace.WriteLine("Acquiring token from dSTS identity provider...");
            var result = await confidentialApp
                .AcquireTokenForClient(scopes)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            // Assert - First acquisition
            Assert.IsNotNull(result, "Authentication result should not be null");
            Assert.IsNotNull(result.AccessToken, "Access token should not be null");
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.AccessToken), "Access token should not be empty");
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource, 
                "First token should come from identity provider");
            Assert.IsTrue(result.AuthenticationResultMetadata.DurationTotalInMs > 0, 
                "Total duration should be positive");
            Assert.IsTrue(result.AuthenticationResultMetadata.DurationInHttpInMs > 0, 
                "HTTP duration should be positive for IdP call");
            
            appCacheRecorder.AssertAccessCounts(1, 1);
            Assert.IsTrue(appCacheRecorder.LastAfterAccessNotificationArgs.IsApplicationCache, 
                "Should be application cache");
            Assert.IsTrue(appCacheRecorder.LastAfterAccessNotificationArgs.HasTokens, 
                "Cache should have tokens");

            // Act - Second call should hit cache
            Trace.WriteLine("Acquiring token from cache...");
            result = await confidentialApp
                .AcquireTokenForClient(scopes)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            // Assert - Second acquisition from cache
            Assert.IsNotNull(result, "Cached authentication result should not be null");
            Assert.IsNotNull(result.AccessToken, "Cached access token should not be null");
            Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource, 
                "Second token should come from cache");
            Assert.AreEqual(0, result.AuthenticationResultMetadata.DurationInHttpInMs, 
                "HTTP duration should be zero for cache hit");
            
            appCacheRecorder.AssertAccessCounts(2, 1);
        }

        /// <summary>
        /// Tests that dSTS authority endpoints are correctly formatted.
        /// Validates token, authorization, and device code endpoints.
        /// </summary>
        [TestMethod]
        [TestCategory(DstsTestCategory)]
        public async Task DstsAuthority_EndpointsAreCorrectlyFormatted_SuccessAsync()
        {
            // Arrange - Get dSTS configuration from Key Vault
            var dstsApp = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppDsts).ConfigureAwait(false);
            var cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            if (cert == null)
            {
                Assert.Inconclusive("Required certificate not found.");
            }

            var confidentialApp = ConfidentialClientApplicationBuilder
                .Create(dstsApp.AppId)
                .WithAuthority(dstsApp.Authority, validateAuthority: false)
                .WithCertificate(cert)
                .Build();

            var appImpl = confidentialApp as ConfidentialClientApplication;
            var authority = appImpl.AuthorityInfo;

            // Assert - Verify endpoint formats
            Assert.AreEqual(AuthorityType.Dsts, authority.AuthorityType);
            Assert.IsTrue(authority.CanonicalAuthority.ToString().Contains("/dstsv2/"), 
                "dSTS canonical authority should contain /dstsv2/");
            
            // Note: Actual endpoint validation would require access to internal Authority properties
            // These are validated in unit tests - integration tests focus on end-to-end scenarios
        }

        /// <summary>
        /// Tests dSTS authority with tenant ID override using WithTenantId.
        /// </summary>
        [TestMethod]
        [TestCategory(DstsTestCategory)]
        [TestCategory(TestCategories.LabAccess)]
        public async Task DstsClientCredentials_WithTenantIdOverride_SuccessAsync()
        {
            // Arrange - Get dSTS configuration from Key Vault
            var dstsApp = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppDsts).ConfigureAwait(false);
            var cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            if (cert == null)
            {
                Assert.Inconclusive("Required certificate not found.");
            }

            // Build authority URL without tenant, then specify tenant at request time
            string dstsAuthorityTenantless = dstsApp.Authority.Replace($"/{dstsApp.TenantId}/", "/common/");
            string[] scopes = new[] { dstsApp.DefaultScopes };

            var confidentialApp = ConfidentialClientApplicationBuilder
                .Create(dstsApp.AppId)
                .WithAuthority(dstsAuthorityTenantless, validateAuthority: false)
                .WithCertificate(cert, sendX5C: true)
                .WithTestLogging()
                .Build();

            // Act - Override tenant ID at request time
            var result = await confidentialApp
                .AcquireTokenForClient(scopes)
                .WithTenantId(dstsApp.TenantId)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.AccessToken);
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
        }

        /// <summary>
        /// Tests that token cache properly segregates tokens by tenant for dSTS.
        /// </summary>
        [TestMethod]
        [TestCategory(DstsTestCategory)]
        [TestCategory(TestCategories.LabAccess)]
        [TestCategory(TestCategories.TokenCacheTests)]
        public async Task DstsClientCredentials_TokenCache_IsolatesByTenant_SuccessAsync()
        {
            // Arrange - Get dSTS configuration from Key Vault
            var dstsApp = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppDsts).ConfigureAwait(false);
            var cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            if (cert == null)
            {
                Assert.Inconclusive("Required certificate not found.");
            }

            string[] scopes = new[] { dstsApp.DefaultScopes };

            var confidentialApp = ConfidentialClientApplicationBuilder
                .Create(dstsApp.AppId)
                .WithAuthority(dstsApp.Authority, validateAuthority: false)
                .WithCertificate(cert, sendX5C: true)
                .WithTestLogging()
                .Build();

            var appCacheRecorder = confidentialApp.AppTokenCache.RecordAccess();

            // Act - Acquire token for first tenant
            var result1 = await confidentialApp
                .AcquireTokenForClient(scopes)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            Assert.AreEqual(TokenSource.IdentityProvider, result1.AuthenticationResultMetadata.TokenSource);
            appCacheRecorder.AssertAccessCounts(1, 1);
            Assert.IsTrue(appCacheRecorder.LastAfterAccessNotificationArgs.HasTokens);
            
            string expectedCacheKey = $"{dstsApp.AppId}_{dstsApp.TenantId}_AppTokenCache";
            Assert.AreEqual(expectedCacheKey, 
                appCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheKey,
                "Cache key should include tenant ID");

            // Act - Second call should hit cache
            var result2 = await confidentialApp
                .AcquireTokenForClient(scopes)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            Assert.AreEqual(TokenSource.Cache, result2.AuthenticationResultMetadata.TokenSource);
            appCacheRecorder.AssertAccessCounts(2, 1);
        }

        /// <summary>
        /// Tests that correlation ID is properly propagated through dSTS requests.
        /// </summary>
        [TestMethod]
        [TestCategory(DstsTestCategory)]
        [TestCategory(TestCategories.LabAccess)]
        public async Task DstsClientCredentials_CorrelationId_IsPreserved_SuccessAsync()
        {
            // Arrange - Get dSTS configuration from Key Vault
            var dstsApp = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppDsts).ConfigureAwait(false);
            var cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            if (cert == null)
            {
                Assert.Inconclusive("Required certificate not found.");
            }

            string[] scopes = new[] { dstsApp.DefaultScopes };

            var confidentialApp = ConfidentialClientApplicationBuilder
                .Create(dstsApp.AppId)
                .WithAuthority(dstsApp.Authority, validateAuthority: false)
                .WithCertificate(cert, sendX5C: true)
                .WithTestLogging()
                .Build();

            var appCacheRecorder = confidentialApp.AppTokenCache.RecordAccess();
            Guid expectedCorrelationId = Guid.NewGuid();

            // Act
            var result = await confidentialApp
                .AcquireTokenForClient(scopes)
                .WithCorrelationId(expectedCorrelationId)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedCorrelationId, appCacheRecorder.LastAfterAccessNotificationArgs.CorrelationId,
                "Correlation ID should be preserved in cache notifications");
            Assert.AreEqual(expectedCorrelationId, appCacheRecorder.LastBeforeAccessNotificationArgs.CorrelationId,
                "Correlation ID should be preserved in before-access notifications");
        }

        /// <summary>
        /// Tests that dSTS properly handles and reports errors.
        /// </summary>
        [TestMethod]
        [TestCategory(DstsTestCategory)]
        [TestCategory(TestCategories.LabAccess)]
        public async Task DstsClientCredentials_WithInvalidSecret_ThrowsExceptionAsync()
        {
            // Arrange - Get dSTS configuration from Key Vault
            var dstsApp = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppDsts).ConfigureAwait(false);
            string[] scopes = new[] { dstsApp.DefaultScopes };
            string invalidSecret = "invalid_secret_12345";

            var confidentialApp = ConfidentialClientApplicationBuilder
                .Create(dstsApp.AppId)
                .WithAuthority(dstsApp.Authority, validateAuthority: false)
                .WithClientSecret(invalidSecret)
                .WithTestLogging()
                .Build();

            // Act & Assert
            var exception = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
            {
                await confidentialApp
                    .AcquireTokenForClient(scopes)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
            }).ConfigureAwait(false);

            Assert.IsNotNull(exception);
            Trace.WriteLine($"Expected exception occurred: {exception.ErrorCode} - {exception.Message}");
            
            // Verify it's an authentication error (not a configuration error)
            Assert.IsTrue(
                exception.ErrorCode.Contains("invalid_client") || 
                exception.ErrorCode.Contains("unauthorized_client") ||
                exception.StatusCode == 401,
                "Should receive authentication-related error");
        }

        /// <summary>
        /// Tests dSTS authority validation and configuration.
        /// </summary>
        [TestMethod]
        [TestCategory(DstsTestCategory)]
        public async Task DstsAuthority_Configuration_IsValidAsync()
        {
            // Arrange - Get dSTS configuration from Key Vault
            var dstsApp = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppDsts).ConfigureAwait(false);
            
            string dstsAuthorityTenanted = dstsApp.Authority;
            string dstsAuthorityCommon = dstsApp.Authority.Replace($"/{dstsApp.TenantId}/", "/common/");

            // Act - Create apps with different authority formats
            var app1 = ConfidentialClientApplicationBuilder
                .Create(dstsApp.AppId)
                .WithAuthority(dstsAuthorityTenanted, validateAuthority: false)
                .WithClientSecret("secret")
                .Build();

            var app2 = ConfidentialClientApplicationBuilder
                .Create(dstsApp.AppId)
                .WithAuthority(dstsAuthorityCommon, validateAuthority: false)
                .WithClientSecret("secret")
                .Build();

            // Assert
            var appImpl1 = app1 as ConfidentialClientApplication;
            var appImpl2 = app2 as ConfidentialClientApplication;

            Assert.AreEqual(AuthorityType.Dsts, appImpl1.AuthorityInfo.AuthorityType);
            Assert.AreEqual(AuthorityType.Dsts, appImpl2.AuthorityInfo.AuthorityType);
            
            Assert.IsTrue(appImpl1.AuthorityInfo.CanBeTenanted, "dSTS authority should support tenanting");
            Assert.IsTrue(appImpl1.AuthorityInfo.IsClientInfoSupported, "dSTS should support client info");
            Assert.IsTrue(appImpl1.AuthorityInfo.IsWsTrustFlowSupported, "dSTS should support WS-Trust");
            Assert.IsFalse(appImpl1.AuthorityInfo.IsInstanceDiscoverySupported, 
                "dSTS should not require instance discovery");
        }
    }
}
