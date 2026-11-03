// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if !ANDROID && !iOS
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    [DeploymentItem(@"Resources\testCert.crtfile")]
    public class ConfidentialClientApplicationExtensibilityTests : TestBase
    {
        private CertificateOptions _certificateOptions = new CertificateOptions();

        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();
        }

        #region CertificateOptions Tests

        [TestMethod]
        [Description("WithCertificate with CertificateOptions SendX5C=false stores options correctly")]
        public void WithCertificate_CertificateOptions_SendX5C_False()
        {
            // Arrange
            var certificate = CertHelper.GetOrCreateTestCert();
            var certificateOptions = new CertificateOptions { SendX5C = false };

            // Act
            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithAuthority(TestConstants.AuthorityCommonTenant)
                .WithCertificate((AssertionRequestOptions options) =>
                {
                    return Task.FromResult(certificate);
                }, certificateOptions)
                .BuildConcrete();

            // Assert
            Assert.IsFalse((app.AppConfig as ApplicationConfiguration).SendX5C, "SendX5C should be false when CertificateOptions.SendX5C is false");
        }

        [TestMethod]
        [Description("WithCertificate with CertificateOptions SendX5C=true stores options correctly")]
        public void WithCertificate_CertificateOptions_SendX5C_True()
        {
            // Arrange
            var certificate = CertHelper.GetOrCreateTestCert();
            var certificateOptions = new CertificateOptions { SendX5C = true };

            // Act
            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithAuthority(TestConstants.AuthorityCommonTenant)
                .WithCertificate((AssertionRequestOptions options) =>
                {
                    return Task.FromResult(certificate);
                }, certificateOptions)
                .BuildConcrete();

            // Assert
            Assert.IsTrue((app.AppConfig as ApplicationConfiguration).SendX5C, "SendX5C should be true when CertificateOptions.SendX5C is true");
        }

        [TestMethod]
        [Description("WithCertificate with null CertificateOptions defaults SendX5C to false")]
        public void WithCertificate_NullCertificateOptions_DefaultsToFalse()
        {
            // Arrange
            var certificate = CertHelper.GetOrCreateTestCert();

            // Act
            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithAuthority(TestConstants.AuthorityCommonTenant)
                .WithCertificate((AssertionRequestOptions options) =>
                {
                    return Task.FromResult(certificate);
                }, null)
                .BuildConcrete();

            // Assert
            Assert.IsFalse((app.AppConfig as ApplicationConfiguration).SendX5C, "SendX5C should default to false when CertificateOptions is null");
        }

        [TestMethod]
        [Description("CertificateOptions with SendX5C=true works in full token acquisition flow")]
        public async Task CertificateOptions_SendX5C_True_InTokenAcquisitionAsync()
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                var certificate = CertHelper.GetOrCreateTestCert();
                var certificateOptions = new CertificateOptions { SendX5C = true };

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(TestConstants.AuthorityCommonTenant)
                    .WithHttpManager(harness.HttpManager)
                    .WithCertificate((AssertionRequestOptions options) =>
                    {
                        return Task.FromResult(certificate);
                    }, certificateOptions)
                    .Build();

                harness.HttpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(sendX5C: true);

                // Act
                var result = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        [Description("CertificateOptions with SendX5C=false works in full token acquisition flow")]
        public async Task CertificateOptions_SendX5C_False_InTokenAcquisitionAsync()
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                var certificate = CertHelper.GetOrCreateTestCert();
                var certificateOptions = new CertificateOptions { SendX5C = false };

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(TestConstants.AuthorityCommonTenant)
                    .WithHttpManager(harness.HttpManager)
                    .WithCertificate((AssertionRequestOptions options) =>
                    {
                        return Task.FromResult(certificate);
                    }, certificateOptions)
                    .Build();

                harness.HttpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                // Act
                var result = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        [Description("CertificateOptions works with retry scenario")]
        public async Task CertificateOptions_WorksWithRetryScenarioAsync()
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                var certificate = CertHelper.GetOrCreateTestCert();
                var certificateOptions = new CertificateOptions { SendX5C = true };
                int retryCount = 0;

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(TestConstants.AuthorityCommonTenant)
                    .WithHttpManager(harness.HttpManager)
                    .WithCertificate((AssertionRequestOptions options) =>
                    {
                        return Task.FromResult(certificate);
                    }, certificateOptions)
                    .OnMsalServiceFailure((AssertionRequestOptions options, ExecutionResult result) =>
                    {
                        retryCount++;
                        return Task.FromResult(retryCount < 2);
                    })
                    .Build();

                // Mock: fail once, then succeed
                harness.HttpManager.AddFailureTokenEndpointResponse("request_failed");
                harness.HttpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(sendX5C: true);

                // Act
                var result = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(1, retryCount, "Should have retried once");
            }
        }

        #endregion

        #region WithCertificate (Dynamic Provider) Integration Tests

        [TestMethod]
        public async Task CertificateThumbprint256_UsesTlsCertificateWhenProviderRotatesBetweenLookupsAsync()
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                var tlsCertificate = CertHelper.GetOrCreateTestCert();
                var laterCertificate = CertHelper.GetOrCreateTestCert(regenerateCert: true);
                using var sha256 = SHA256.Create();
                string tlsThumbprint256 = BitConverter.ToString(sha256.ComputeHash(tlsCertificate.RawData)).Replace("-", string.Empty);
                int providerCount = 0;
                var handler = harness.HttpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
                handler.ExpectedMtlsBindingCertificate = tlsCertificate;

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(TestConstants.AuthorityCommonTenant)
                    .WithInstanceDiscovery(false)
                    .WithHttpManager(harness.HttpManager)
                    .WithCertificate((AssertionRequestOptions options) =>
                    {
                        providerCount++;
                        return Task.FromResult(providerCount == 1 ? tlsCertificate : laterCertificate);
                    }, new CertificateOptions { SendCertificateOverMtls = true })
                    .BuildConcrete();

                // Act
                var acquired = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync().ConfigureAwait(false);

                // Assert
                Assert.AreEqual(2, providerCount, "The provider must rotate between TLS preflight and credential resolution.");
                Assert.AreEqual(TokenSource.IdentityProvider, acquired.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(tlsThumbprint256, acquired.CertificateThumbprint256);
                Assert.AreEqual(tlsThumbprint256, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Single().CertificateThumbprint256);

                // Act
                var cached = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync().ConfigureAwait(false);

                // Assert
                Assert.AreEqual(TokenSource.Cache, cached.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(tlsThumbprint256, cached.CertificateThumbprint256);
                Assert.AreEqual(tlsThumbprint256, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Single().CertificateThumbprint256);
                Assert.AreEqual(3, providerCount, "A cache hit may resolve the current TLS certificate but must preserve acquisition provenance.");
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task CertificateThumbprint256_PersistsAcrossCacheHitsAndRotationAsync(bool dynamicProvider)
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();
                harness.HttpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
                var cert1 = CertHelper.GetOrCreateTestCert();
                var cert2 = CertHelper.GetOrCreateTestCert(regenerateCert: true);
                using var sha256 = SHA256.Create();
                string thumbprint2561 = BitConverter.ToString(sha256.ComputeHash(cert1.RawData)).Replace("-", string.Empty);
                string thumbprint2562 = BitConverter.ToString(sha256.ComputeHash(cert2.RawData)).Replace("-", string.Empty);
                var currentCertificate = cert1;
                int providerCount = 0;

                var builder = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(TestConstants.AuthorityCommonTenant)
                    .WithHttpManager(harness.HttpManager);
                if (dynamicProvider)
                {
                    builder.WithCertificate((AssertionRequestOptions options) =>
                    {
                        providerCount++;
                        return Task.FromResult(currentCertificate);
                    }, _certificateOptions);
                }
                else
                {
                    builder.WithCertificate(cert1);
                }

                var app = builder.BuildConcrete();

                // Act
                var acquired = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync().ConfigureAwait(false);

                // Assert
                Assert.AreEqual(thumbprint2561, acquired.CertificateThumbprint256);
                var entry = app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Single();
                Assert.AreEqual(thumbprint2561, entry.CertificateThumbprint256);
                string cacheKey = entry.CacheKey;

                // Act
                currentCertificate = cert2;
                var cached = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync().ConfigureAwait(false);

                // Assert
                Assert.AreEqual(TokenSource.Cache, cached.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(thumbprint2561, cached.CertificateThumbprint256);
                Assert.AreEqual(dynamicProvider ? 1 : 0, providerCount);
                Assert.AreEqual(thumbprint2561, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Single().CertificateThumbprint256);

                // Arrange
                var snapshot = ((ITokenCacheSnapshotProvider)app.AppTokenCache).CreateSnapshot();
                var restoredBuilder = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(TestConstants.AuthorityCommonTenant)
                    .WithHttpManager(harness.HttpManager);
                if (dynamicProvider)
                {
                    restoredBuilder.WithCertificate((AssertionRequestOptions options) =>
                    {
                        providerCount++;
                        return Task.FromResult(currentCertificate);
                    }, _certificateOptions);
                }
                else
                {
                    restoredBuilder.WithCertificate(cert2);
                }
                var restored = restoredBuilder.BuildConcrete();

                // Act
                ((ITokenCacheSnapshotProvider)restored.AppTokenCache).RestoreSnapshot(snapshot);
                var snapshotResult = await restored.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync().ConfigureAwait(false);
                ((ITokenCacheSerializer)restored.AppTokenCache).DeserializeMsalV3(snapshot.SerializeMsalV3(), shouldClearExistingCache: true);
                var deserializedResult = await restored.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync().ConfigureAwait(false);

                // Assert
                Assert.AreEqual(TokenSource.Cache, snapshotResult.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(thumbprint2561, snapshotResult.CertificateThumbprint256);
                Assert.AreEqual(TokenSource.Cache, deserializedResult.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(thumbprint2561, deserializedResult.CertificateThumbprint256);
                Assert.AreEqual(dynamicProvider ? 1 : 0, providerCount);
                Assert.AreEqual(thumbprint2561, restored.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Single().CertificateThumbprint256);

                // Act
                harness.HttpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
                var refreshed = await restored.AcquireTokenForClient(TestConstants.s_scope)
                    .WithForceRefresh(true).ExecuteAsync().ConfigureAwait(false);

                // Assert
                Assert.AreEqual(TokenSource.IdentityProvider, refreshed.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(thumbprint2562, refreshed.CertificateThumbprint256);
                Assert.AreEqual(dynamicProvider ? 2 : 0, providerCount);
                entry = restored.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Single();
                Assert.AreEqual(thumbprint2562, entry.CertificateThumbprint256);
                Assert.AreEqual(cacheKey, entry.CacheKey, "Certificate provenance must not change the cache key.");
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task CertificateThumbprint256_UnknownCredentialAndOlderCacheEntry_ReturnNullAsync(bool clientAssertion)
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();
                harness.HttpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
                var builder = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(TestConstants.AuthorityCommonTenant)
                    .WithHttpManager(harness.HttpManager);
                if (clientAssertion)
                {
                    builder.WithClientAssertion((AssertionRequestOptions options) => Task.FromResult("external-assertion"));
                }
                else
                {
                    builder.WithClientSecret(TestConstants.ClientSecret);
                }
                var app = builder.BuildConcrete();

                // Act
                var acquired = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync().ConfigureAwait(false);

                // Assert
                Assert.IsNull(acquired.CertificateThumbprint256);
                var entry = app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Single();
                Assert.IsNull(entry.CertificateThumbprint256);
                Assert.IsFalse(entry.ToJObject().ContainsKey(StorageJsonKeys.CertificateThumbprint256));

                // Arrange
                byte[] serialized = ((ITokenCacheSerializer)app.AppTokenCache).SerializeMsalV3();
                var restored = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(TestConstants.AuthorityCommonTenant)
                    .WithHttpManager(harness.HttpManager)
                    .WithCertificate(CertHelper.GetOrCreateTestCert())
                    .BuildConcrete();
                ((ITokenCacheSerializer)restored.AppTokenCache).DeserializeMsalV3(serialized);

                // Act
                var cached = await restored.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync().ConfigureAwait(false);

                // Assert
                Assert.AreEqual(TokenSource.Cache, cached.AuthenticationResultMetadata.TokenSource);
                Assert.IsNull(cached.CertificateThumbprint256);
                Assert.IsNull(restored.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Single().CertificateThumbprint256);
            }
        }

        [TestMethod]
        [Description("Dynamic certificate provider is invoked and cert is used for client assertion")]
        public async Task DynamicCertificateProvider_IsInvoked_AndUsedForAssertionAsync()
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                bool providerInvoked = false;
                AssertionRequestOptions capturedOptions = null;

                var certificate = CertHelper.GetOrCreateTestCert();

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(TestConstants.AuthorityCommonTenant)
                    .WithHttpManager(harness.HttpManager)
                    .WithCertificate((AssertionRequestOptions options) =>
                    {
                        providerInvoked = true;
                        capturedOptions = options;
                        
                        // Validate options
                        Assert.AreEqual(TestConstants.ClientId, options.ClientID);
                        Assert.IsNotNull(options.TokenEndpoint);
                        
                        return Task.FromResult(certificate);
                    }, _certificateOptions)
                    .Build();

                harness.HttpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                // Act
                var result = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.IsTrue(providerInvoked, "ClientCertificate provider should have been invoked");
                Assert.IsNotNull(capturedOptions);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        [Description("Dynamic certificate provider returning null throws appropriate exception")]
        public async Task DynamicCertificateProvider_ReturnsNull_ThrowsExceptionAsync()
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(TestConstants.AuthorityCommonTenant)
                    .WithHttpManager(harness.HttpManager)
                    .WithCertificate((AssertionRequestOptions options) =>
                    {
                        return Task.FromResult<System.Security.Cryptography.X509Certificates.X509Certificate2>(null); // Provider returns null
                    }, _certificateOptions)
                    .Build();

                // Act & Assert
                var exception = await Assert.ThrowsAsync<MsalClientException>(async () =>
                {
                    await app.AcquireTokenForClient(TestConstants.s_scope)
                        .ExecuteAsync()
                        .ConfigureAwait(false);
                }).ConfigureAwait(false);

                Assert.AreEqual(MsalError.InvalidClientAssertion, exception.ErrorCode);
                Assert.Contains("returned null", exception.Message);
            }
        }

        #endregion

        #region OnMsalServiceFailure Integration Tests

        [TestMethod]
        [Description("OnMsalServiceFailure is invoked on service exception and retries successfully")]
        public async Task OnMsalServiceFailure_RetriesOnServiceError_SucceedsAsync()
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                int failureCallbackCount = 0;
                MsalServiceException capturedException = null;
                var cert = CertHelper.GetOrCreateTestCert();

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(TestConstants.AuthorityCommonTenant)
                    .WithCertificate(cert)
                    .WithHttpManager(harness.HttpManager)
                    .OnMsalServiceFailure((AssertionRequestOptions options, ExecutionResult result) =>
                    {
                        failureCallbackCount++;
                        capturedException = result.Exception as MsalServiceException;
                        
                        // Validate ExecutionResult contains exception and certificate
                        Assert.IsFalse(result.Successful, "Result should indicate failure");
                        Assert.IsNull(result.Result, "Result should be null on failure");
                        Assert.IsNotNull(result.Exception, "Exception should be present");
                        Assert.IsNotNull(result.ClientCertificate, "ClientCertificate should be present");
                        Assert.IsNotNull(capturedException, "Exception should be MsalServiceException");
                        Assert.AreEqual(TestConstants.ClientId, options.ClientID);
                        Assert.IsNotNull(options.TokenEndpoint, "TokenEndpoint should be available in failure callback");
                        
                        // Retry on 503
                        return Task.FromResult(capturedException.StatusCode == 400 && failureCallbackCount < 3);
                    })
                    .Build();

                // Mock 2 failures, then success
                harness.HttpManager.AddFailureTokenEndpointResponse("request_failed");
                harness.HttpManager.AddFailureTokenEndpointResponse("request_failed");
                harness.HttpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                // Act
                var result = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(2, failureCallbackCount, "Callback should be invoked twice");
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(400, capturedException.StatusCode);
            }
        }

        [TestMethod]
        [Description("OnMsalServiceFailure returns false and exception is propagated")]
        public async Task OnMsalServiceFailure_ReturnsFalse_PropagatesExceptionAsync()
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                bool callbackInvoked = false;

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(TestConstants.AuthorityCommonTenant)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(harness.HttpManager)
                    .OnMsalServiceFailure((AssertionRequestOptions options, ExecutionResult result) =>
                    {
                        callbackInvoked = true;
                        // Validate ExecutionResult
                        Assert.IsFalse(result.Successful);
                        Assert.IsNotNull(result.Exception);
                        Assert.IsNotNull(result.ClientCertificate);
                        return Task.FromResult(false); // Don't retry
                    })
                    .Build();

                harness.HttpManager.AddFailureTokenEndpointResponse("request_failed");

                // Act & Assert
                await Assert.ThrowsAsync<MsalServiceException>(async () =>
                {
                    await app.AcquireTokenForClient(TestConstants.s_scope)
                        .ExecuteAsync()
                        .ConfigureAwait(false);
                }).ConfigureAwait(false);

                Assert.IsTrue(callbackInvoked);
            }
        }

        [TestMethod]
        [Description("OnMsalServiceFailure is NOT invoked for client exceptions")]
        public async Task OnMsalServiceFailure_NotInvokedForClientExceptionsAsync()
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                bool callbackInvoked = false;

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(TestConstants.AuthorityCommonTenant)
                    .WithCertificate((AssertionRequestOptions options) =>
                    {
                        return Task.FromResult<System.Security.Cryptography.X509Certificates.X509Certificate2>(null); // Will cause MsalClientException
                    }, _certificateOptions)
                    .OnMsalServiceFailure((AssertionRequestOptions options, ExecutionResult result) =>
                    {
                        callbackInvoked = true;
                        return Task.FromResult(false);
                    })
                    .Build();

                // Act & Assert
                var exception = await Assert.ThrowsAsync<MsalClientException>(async () =>
                {
                    await app.AcquireTokenForClient(TestConstants.s_scope)
                        .ExecuteAsync()
                        .ConfigureAwait(false);
                }).ConfigureAwait(false);

                Assert.IsFalse(callbackInvoked, "Callback should NOT be invoked for client exceptions");
                Assert.AreEqual(MsalError.InvalidClientAssertion, exception.ErrorCode);
            }
        }

        #endregion

        #region OnSuccess Integration Tests

        [TestMethod]
        [Description("OnCompletion is invoked with successful result")]
        public async Task OnSuccess_InvokedWithSuccessfulResultAsync()
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                bool observerInvoked = false;
                ExecutionResult capturedResult = null;
                AssertionRequestOptions capturedOptions = null;

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(TestConstants.AuthorityCommonTenant)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(harness.HttpManager)
                    .OnCompletion((AssertionRequestOptions options, ExecutionResult result) =>
                    {
                        observerInvoked = true;
                        capturedResult = result;
                        capturedOptions = options;
                        
                        Assert.IsTrue(result.Successful);
                        Assert.IsNotNull(result.Result);
                        Assert.IsNotNull(result.ClientCertificate);
                        Assert.IsNull(result.Exception);
                        Assert.AreEqual(TestConstants.ClientId, options.ClientID);
                        Assert.IsNotNull(options.TokenEndpoint, "TokenEndpoint should be available in success callback");
                        
                        return Task.CompletedTask;
                    })
                    .Build();

                harness.HttpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                // Act
                var result = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.IsTrue(observerInvoked, "Observer should be invoked");
                Assert.IsNotNull(capturedResult);
                Assert.IsTrue(capturedResult.Successful);
                Assert.IsNotNull(capturedResult.Result);
                Assert.AreEqual(result.AccessToken, capturedResult.Result.AccessToken);
            }
        }

        [TestMethod]
        [Description("OnCompletion is invoked with failure result after retries exhausted")]
        public async Task OnSuccess_InvokedWithFailureResult_AfterRetriesExhaustedAsync()
        {
            // Arrange
            var logMessages = new System.Collections.Generic.List<string>();
            LogCallback logCallback = (level, message, pii) => logMessages.Add(message);
            
            using (var harness = CreateTestHarness(logCallback: logCallback))
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                int retryCount = 0;
                bool observerInvoked = false;
                ExecutionResult capturedResult = null;

                var certificate = CertHelper.GetOrCreateTestCert();

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(TestConstants.AuthorityCommonTenant)
                    .WithCertificate(certificate)
                    .WithHttpManager(harness.HttpManager)
                    .WithLogging(logCallback, LogLevel.Info, enablePiiLogging: true, enableDefaultPlatformLogging: false)
                    .OnMsalServiceFailure((AssertionRequestOptions options, ExecutionResult result) =>
                    {
                        retryCount++;
                        // Validate ExecutionResult
                        Assert.IsFalse(result.Successful);
                        Assert.IsNotNull(result.Exception);
                        Assert.IsNotNull(result.ClientCertificate);
                        return Task.FromResult(retryCount < 2); // Retry once, then give up
                    })
                    .OnCompletion((AssertionRequestOptions options, ExecutionResult result) =>
                    {
                        observerInvoked = true;
                        capturedResult = result;
                        
                        Assert.IsFalse(result.Successful);
                        Assert.IsNull(result.Result);
                        Assert.IsNotNull(result.Exception);
                        Assert.IsNotNull(result.ClientCertificate);
                        Assert.IsInstanceOfType(result.Exception, typeof(MsalServiceException));
                        Assert.IsNotNull(options.TokenEndpoint, "TokenEndpoint should be available even on failure");
                        
                        return Task.CompletedTask;
                    })
                    .Build();

                // Mock 2 failures
                harness.HttpManager.AddFailureTokenEndpointResponse("request_failed");
                harness.HttpManager.AddFailureTokenEndpointResponse("request_failed");

                // Act & Assert
                var exception = await Assert.ThrowsAsync<MsalServiceException>(async () =>
                {
                    await app.AcquireTokenForClient(TestConstants.s_scope)
                        .ExecuteAsync()
                        .ConfigureAwait(false);
                }).ConfigureAwait(false);

                Assert.IsTrue(observerInvoked, "Observer should be invoked even on failure");
                Assert.IsNotNull(capturedResult);
                Assert.IsFalse(capturedResult.Successful);
                Assert.AreEqual(exception, capturedResult.Exception);
                
                // Verify retry logging
                Assert.IsTrue(logMessages.Any(m => m.Contains("[ClientCredentialRequest] OnMsalServiceFailure returned true. Retrying token request (Retry #1).")), 
                    "Should log retry #1");
            }
        }

        [TestMethod]
        [Description("OnCompletion exception is caught and logged, doesn't disrupt flow")]
        public async Task OnSuccess_ExceptionIsCaught_DoesNotDisruptFlowAsync()
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(TestConstants.AuthorityCommonTenant)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(harness.HttpManager)
                    .OnCompletion((AssertionRequestOptions options, ExecutionResult result) =>
                    {
                        throw new InvalidOperationException("Observer threw exception");
                    })
                    .Build();

                harness.HttpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                // Act - should NOT throw, observer exception should be caught
                var result = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
            }
        }

        #endregion

        #region Combined Scenarios

        [TestMethod]
        [Description("All three extensibility points work together: cert provider, retry, observer")]
        public async Task AllThreeExtensibilityPoints_WorkTogetherAsync()
        {
            // Arrange
            var logMessages = new System.Collections.Generic.List<string>();
            LogCallback logCallback = (level, message, pii) => logMessages.Add(message);
            
            using (var harness = CreateTestHarness(logCallback: logCallback))
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                int certProviderCount = 0;
                int retryCallbackCount = 0;
                bool observerInvoked = false;

                var certificate = CertHelper.GetOrCreateTestCert();

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(TestConstants.AuthorityCommonTenant)
                    .WithHttpManager(harness.HttpManager)
                    .WithLogging(logCallback, LogLevel.Info, enablePiiLogging: true, enableDefaultPlatformLogging: false)
                    .WithCertificate((AssertionRequestOptions options) =>
                    {
                        certProviderCount++;
                        Assert.AreEqual(TestConstants.ClientId, options.ClientID);
                        Assert.IsNotNull(options.TokenEndpoint, "TokenEndpoint should be available in cert provider");
                        return Task.FromResult(certificate);
                    }, _certificateOptions)
                    .OnMsalServiceFailure((AssertionRequestOptions options, ExecutionResult result) =>
                    {
                        retryCallbackCount++;
                        // Validate ExecutionResult contains exception and certificate
                        Assert.IsFalse(result.Successful, "Result should indicate failure");
                        Assert.IsNotNull(result.Exception, "Exception should be present");
                        Assert.IsNotNull(result.ClientCertificate, "ClientCertificate should be present from cert provider");
                        Assert.IsInstanceOfType(result.Exception, typeof(MsalServiceException));
                        Assert.IsNotNull(options.TokenEndpoint, "TokenEndpoint should be available in retry callback");
                        return Task.FromResult(retryCallbackCount < 2); // Retry once
                    })
                    .OnCompletion((AssertionRequestOptions options, ExecutionResult result) =>
                    {
                        observerInvoked = true;
                        Assert.IsTrue(result.Successful);
                        Assert.IsNotNull(result.Result);
                        Assert.IsNotNull(options.TokenEndpoint, "TokenEndpoint should be available in success callback");
                        return Task.CompletedTask;
                    })
                    .Build();

                // Mock: fail once, then succeed
                harness.HttpManager.AddFailureTokenEndpointResponse("request_failed");
                harness.HttpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                // Act
                var result = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(2, certProviderCount, "Cert provider invoked for initial + retry");
                Assert.AreEqual(1, retryCallbackCount, "Retry callback invoked once");
                Assert.IsTrue(observerInvoked, "Observer invoked once at completion");
                Assert.IsNotNull(result.AccessToken);
                
                // Verify retry logging
                Assert.IsTrue(logMessages.Any(m => m.Contains("[ClientCredentialRequest] OnMsalServiceFailure returned true. Retrying token request (Retry #1).")), 
                    "Should log retry #1");
            }
        }

        [TestMethod]
        [Description("ClientCertificate rotation scenario: different cert returned on retry")]
        public async Task CertificateRotation_DifferentCertOnRetryAsync()
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                int certProviderCount = 0;
                var cert1 = CertHelper.GetOrCreateTestCert();
                var cert2 = CertHelper.GetOrCreateTestCert(regenerateCert: true);

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(TestConstants.AuthorityCommonTenant)
                    .WithHttpManager(harness.HttpManager)
                    .WithCertificate((AssertionRequestOptions options) =>
                    {
                        certProviderCount++;
                        // Return different cert on retry
                        return Task.FromResult(certProviderCount == 1 ? cert1 : cert2);
                    }, _certificateOptions)
                    .OnMsalServiceFailure((AssertionRequestOptions options, ExecutionResult result) =>
                    {
                        // Validate ExecutionResult
                        Assert.IsFalse(result.Successful);
                        Assert.IsNotNull(result.Exception);
                        Assert.IsNotNull(result.ClientCertificate, "ClientCertificate should be present (cert1 in this case)");
                        return Task.FromResult(true); // Always retry once
                    })
                    .Build();

                // First call fails (cert1), second succeeds (cert2)
                harness.HttpManager.AddFailureTokenEndpointResponse("request_failed");
                harness.HttpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                // Act
                var result = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(2, certProviderCount, "Provider should be called twice");
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(cert2.ComputeSha256Thumbprint(), result.CertificateThumbprint256);
                Assert.AreEqual(result.CertificateThumbprint256,
                    ((ITokenCacheInternal)app.AppTokenCache).Accessor.GetAllAccessTokens().Single().CertificateThumbprint256);
            }
        }

        [TestMethod]
        public async Task CertificateThumbprint256_RetryWithoutResolvedCertificate_DoesNotReportFailedAttemptCertificateAsync()
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                var certificate = CertHelper.GetOrCreateTestCert();
                int providerCount = 0;
                int failureCount = 0;
                ExecutionResult failedAttempt = null;
                ExecutionResult completion = null;
                harness.HttpManager.AddFailureTokenEndpointResponse("request_failed");
                var successHandler = harness.HttpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(TestConstants.AuthorityCommonTenant)
                    .WithInstanceDiscovery(false)
                    .WithHttpManager(harness.HttpManager)
                    .WithClientAssertion((AssertionRequestOptions options, CancellationToken cancellationToken) =>
                    {
                        providerCount++;
                        return Task.FromResult(new ClientSignedAssertion
                        {
                            Assertion = "external-assertion",
                            TokenBindingCertificate = providerCount == 2 ? certificate : null
                        });
                    })
                    .OnMsalServiceFailure((AssertionRequestOptions options, ExecutionResult result) =>
                    {
                        failedAttempt = result;
                        failureCount++;
                        return Task.FromResult(failureCount == 1);
                    })
                    .OnCompletion((AssertionRequestOptions options, ExecutionResult result) =>
                    {
                        completion = result;
                        return Task.CompletedTask;
                    })
                    .BuildConcrete();

                // Act
                var acquired = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync().ConfigureAwait(false);

                // Assert
                Assert.AreEqual(3, providerCount, "Resolve no TLS certificate in preflight, a certificate on failure, and none on retry.");
                Assert.AreEqual(1, failureCount);
                Assert.IsNotNull(failedAttempt);
                Assert.AreSame(certificate, failedAttempt.ClientCertificate);
                Assert.IsEmpty(successHandler.ClientCertificates);
                Assert.AreEqual(TokenSource.IdentityProvider, acquired.AuthenticationResultMetadata.TokenSource);
                Assert.IsNull(acquired.CertificateThumbprint256);
                Assert.IsNull(app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Single().CertificateThumbprint256);
                Assert.IsNotNull(completion);
                Assert.IsTrue(completion.Successful);
                Assert.IsNull(completion.ClientCertificate);

                // Act
                var cached = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync().ConfigureAwait(false);

                // Assert
                Assert.AreEqual(TokenSource.Cache, cached.AuthenticationResultMetadata.TokenSource);
                Assert.IsNull(cached.CertificateThumbprint256);
                Assert.IsNull(app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Single().CertificateThumbprint256);
                Assert.AreEqual(4, providerCount, "The cache hit should only invoke assertion preflight.");
            }
        }

        #endregion
    }
}
#endif
