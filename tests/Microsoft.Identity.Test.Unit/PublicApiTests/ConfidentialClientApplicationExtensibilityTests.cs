// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if !ANDROID && !iOS
using System;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
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
                Assert.IsFalse(app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Single().ToJObject().ContainsKey("certificate_thumbprint256"));

                // Act
                var cached = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync().ConfigureAwait(false);

                // Assert
                Assert.AreEqual(TokenSource.Cache, cached.AuthenticationResultMetadata.TokenSource);
                Assert.IsNull(cached.CertificateThumbprint256);
                Assert.AreEqual(3, providerCount, "A cache hit may resolve the current TLS certificate but must not report it as used.");
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task CertificateThumbprint256_OnlyFreshAcquisitionsReportCertificateAcrossRotationAsync(bool dynamicProvider)
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
                Assert.IsFalse(entry.ToJObject().ContainsKey("certificate_thumbprint256"));
                string cacheKey = entry.CacheKey;

                // Act
                currentCertificate = cert2;
                var cached = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync().ConfigureAwait(false);

                // Assert
                Assert.AreEqual(TokenSource.Cache, cached.AuthenticationResultMetadata.TokenSource);
                Assert.IsNull(cached.CertificateThumbprint256);
                Assert.AreEqual(dynamicProvider ? 1 : 0, providerCount);

                // Arrange
                var snapshot = ((ITokenCacheSnapshotProvider)app.AppTokenCache).CreateSnapshot();
                byte[] serializedSnapshot = snapshot.SerializeMsalV3();
                Assert.DoesNotContain("certificate_thumbprint256", Encoding.UTF8.GetString(serializedSnapshot));
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
                ((ITokenCacheSerializer)restored.AppTokenCache).DeserializeMsalV3(serializedSnapshot, shouldClearExistingCache: true);
                var deserializedResult = await restored.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync().ConfigureAwait(false);

                // Assert
                Assert.AreEqual(TokenSource.Cache, snapshotResult.AuthenticationResultMetadata.TokenSource);
                Assert.IsNull(snapshotResult.CertificateThumbprint256);
                Assert.AreEqual(TokenSource.Cache, deserializedResult.AuthenticationResultMetadata.TokenSource);
                Assert.IsNull(deserializedResult.CertificateThumbprint256);
                Assert.AreEqual(dynamicProvider ? 1 : 0, providerCount);

                // Act
                harness.HttpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
                var refreshed = await restored.AcquireTokenForClient(TestConstants.s_scope)
                    .WithForceRefresh(true).ExecuteAsync().ConfigureAwait(false);

                // Assert
                Assert.AreEqual(TokenSource.IdentityProvider, refreshed.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(thumbprint2562, refreshed.CertificateThumbprint256);
                Assert.AreEqual(dynamicProvider ? 2 : 0, providerCount);
                entry = restored.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Single();
                Assert.IsFalse(entry.ToJObject().ContainsKey("certificate_thumbprint256"));
                Assert.AreEqual(cacheKey, entry.CacheKey, "Request-only certificate metadata must not change the cache key.");
            }
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(false, true)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public async Task CertificateThumbprint256_UnknownCredentialAndOlderCacheEntry_ReturnNullAsync(bool clientAssertion, bool legacyMetadata)
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
                Assert.IsFalse(entry.ToJObject().ContainsKey("certificate_thumbprint256"));

                // Arrange
                if (legacyMetadata)
                {
                    entry.AdditionalFieldsJson = "{\"certificate_thumbprint256\":\"0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF\"}";
                }
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
        [DataRow(false)]
        [DataRow(true)]
        public async Task AcquireTokenForClient_CanceledDuringHttpRequest_InvokesOnCompletionOnceAndPropagatesCancellation(
            bool completionThrows)
        {
            // Arrange
            using var harness = CreateTestHarness();
            harness.HttpManager.AddInstanceDiscoveryMockHandler();
            using var certificate = CertHelper.CreateTestCert();
            using var cancellation = new CancellationTokenSource();
            using var handler = new CancelableTokenRequestHandler();
            harness.HttpManager.AddMockHandler(handler);

            int providerCount = 0;
            int completionCount = 0;
            int serviceFailureCount = 0;
            ExecutionResult completionResult = null;
            AssertionRequestOptions completionOptions = null;
            var completionEntered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithAuthority(TestConstants.AuthorityCommonTenant)
                .WithHttpManager(harness.HttpManager)
                .WithCertificate(_ =>
                {
                    providerCount++;
                    return Task.FromResult(certificate);
                }, new CertificateOptions())
                .OnMsalServiceFailure((_, _) =>
                {
                    serviceFailureCount++;
                    return Task.FromResult(true);
                })
                .OnCompletion(async (options, result) =>
                {
                    completionCount++;
                    completionResult = result;
                    completionOptions = options;
                    completionEntered.TrySetResult(true);
                    await releaseCompletion.Task.ConfigureAwait(false);
                    if (completionThrows)
                    {
                        throw new InvalidOperationException("Completion callback failed.");
                    }
                })
                .Build();

            // Act
            Task<AuthenticationResult> acquisition = app.AcquireTokenForClient(TestConstants.s_scope)
                .ExecuteAsync(cancellation.Token);
            try
            {
                Assert.AreSame(handler.RequestStarted,
                    await Task.WhenAny(handler.RequestStarted, acquisition, Task.Delay(TimeSpan.FromSeconds(10))).ConfigureAwait(false),
                    "The token POST must be in flight before cancellation.");
                cancellation.Cancel();
                await Task.WhenAny(completionEntered.Task, acquisition, Task.Delay(TimeSpan.FromSeconds(10))).ConfigureAwait(false);

                // Assert
                Assert.AreEqual(1, completionCount, "Cancellation must invoke OnCompletion exactly once.");
                Assert.IsFalse(acquisition.IsCompleted, "ExecuteAsync must await the completion callback.");
                releaseCompletion.TrySetResult(true);
                Assert.AreSame(acquisition,
                    await Task.WhenAny(acquisition, Task.Delay(TimeSpan.FromSeconds(10))).ConfigureAwait(false));
                var exception = await Assert.ThrowsAsync<OperationCanceledException>(() => acquisition).ConfigureAwait(false);

                Assert.IsTrue(acquisition.IsCanceled);
                Assert.AreEqual(1, providerCount);
                Assert.AreEqual(1, handler.PostCount);
                Assert.AreEqual(1, completionCount);
                Assert.AreEqual(0, serviceFailureCount, "Caller cancellation must not enter the service retry callback.");
                Assert.IsFalse(completionResult.Successful);
                Assert.IsNull(completionResult.Result);
                Assert.AreSame(certificate, completionResult.ClientCertificate);
                Assert.IsInstanceOfType<MsalClientException>(completionResult.Exception);
                Assert.AreEqual(MsalError.AuthenticationCanceledError, completionResult.Exception.ErrorCode);
                Assert.AreSame(exception, completionResult.Exception.InnerException);
                Assert.AreEqual(handler.TokenEndpoint, completionOptions.TokenEndpoint);
                Assert.IsTrue(certificate.HasPrivateKey);
                Assert.IsGreaterThan(0, certificate.GetCertHash().Length);
                using var signingKey = certificate.GetRSAPrivateKey();
                Assert.IsNotNull(signingKey.SignData(new byte[] { 1, 2, 3 }, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
            }
            finally
            {
                cancellation.Cancel();
                releaseCompletion.TrySetResult(true);
                await Task.WhenAny(acquisition, Task.Delay(TimeSpan.FromSeconds(10))).ConfigureAwait(false);
            }
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(true, false)]
        [DataRow(false, true)]
        public async Task AcquireTokenForClient_CanceledBeforeTokenPost_DoesNotInvokeCompletion(
            bool retryCertificateLoad, bool cancelBeforePost)
        {
            // Arrange
            using var harness = CreateTestHarness();
            harness.HttpManager.AddInstanceDiscoveryMockHandler();
            using var certificate = CertHelper.CreateTestCert();
            using var cancellation = new CancellationTokenSource();
            var loading = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            int providerCount = 0;
            int postCount = 0;
            int completionCount = 0;
            int serviceFailureCount = 0;
            if (retryCertificateLoad)
            {
                harness.HttpManager.AddFailureTokenEndpointResponse("invalid_client")
                    .AdditionalRequestValidation = _ => postCount++;
            }

            var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithAuthority(TestConstants.AuthorityCommonTenant)
                .WithHttpManager(harness.HttpManager)
                .WithCertificate(async options =>
                {
                    providerCount++;
                    if (cancelBeforePost || (retryCertificateLoad && providerCount == 1))
                    {
                        return certificate;
                    }

                    loading.TrySetResult(true);
                    await Task.Delay(Timeout.Infinite, options.CancellationToken).ConfigureAwait(false);
                    throw new AssertFailedException("Certificate loading must end through cancellation.");
                }, new CertificateOptions())
                .OnMsalServiceFailure((_, _) =>
                {
                    serviceFailureCount++;
                    return Task.FromResult(true);
                })
                .OnCompletion((_, _) =>
                {
                    completionCount++;
                    return Task.CompletedTask;
                })
                .Build();

            // Act
            Task<AuthenticationResult> acquisition = app.AcquireTokenForClient(TestConstants.s_scope)
                .OnBeforeTokenRequest(async data =>
                {
                    if (cancelBeforePost)
                    {
                        loading.TrySetResult(true);
                        await Task.Delay(Timeout.Infinite, data.CancellationToken).ConfigureAwait(false);
                    }
                })
                .ExecuteAsync(cancellation.Token);
            try
            {
                Assert.AreSame(loading.Task,
                    await Task.WhenAny(loading.Task, acquisition, Task.Delay(TimeSpan.FromSeconds(10))).ConfigureAwait(false));
                cancellation.Cancel();
                Assert.AreSame(acquisition,
                    await Task.WhenAny(acquisition, Task.Delay(TimeSpan.FromSeconds(10))).ConfigureAwait(false));
                await Assert.ThrowsAsync<OperationCanceledException>(() => acquisition).ConfigureAwait(false);

                // Assert
                Assert.IsTrue(acquisition.IsCanceled);
                Assert.AreEqual(retryCertificateLoad ? 2 : 1, providerCount);
                Assert.AreEqual(retryCertificateLoad ? 1 : 0, postCount);
                Assert.AreEqual(retryCertificateLoad ? 1 : 0, serviceFailureCount);
                Assert.AreEqual(0, completionCount, "Cancellation before the token POST must not notify completion.");
            }
            finally
            {
                cancellation.Cancel();
                await Task.WhenAny(acquisition, Task.Delay(TimeSpan.FromSeconds(10))).ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task AcquireTokenForClient_QueuedAppTokenProviderCanceled_DoesNotInvokeCompletion()
        {
            // Arrange
            using var harness = CreateTestHarness();
            harness.HttpManager.AddInstanceDiscoveryMockHandler();
            using var cancellation = new CancellationTokenSource();
            var providerEntered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseProvider = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var secondCallQueued = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            int providerCount = 0;
            int completionCount = 0;
            int semaphoreEntries = 0;
            var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithAuthority(TestConstants.AuthorityCommonTenant)
                .WithHttpManager(harness.HttpManager)
                .WithLogging((_, message, _) =>
                {
                    if (message.Contains("[ClientCredentialRequest] Entering client credential request semaphore.") &&
                        Interlocked.Increment(ref semaphoreEntries) == 2)
                    {
                        secondCallQueued.TrySetResult(true);
                    }
                }, LogLevel.Verbose)
                .WithAppTokenProvider(async _ =>
                {
                    Interlocked.Increment(ref providerCount);
                    providerEntered.TrySetResult(true);
                    await releaseProvider.Task.ConfigureAwait(false);
                    return new AppTokenProviderResult
                    {
                        AccessToken = TestConstants.DefaultAccessToken,
                        ExpiresInSeconds = 3600
                    };
                })
                .OnCompletion((_, _) =>
                {
                    Interlocked.Increment(ref completionCount);
                    return Task.CompletedTask;
                })
                .Build();
            Task<AuthenticationResult> firstCall = app.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync();
            Task<AuthenticationResult> queuedCall = null;

            try
            {
                // Act
                Assert.AreSame(providerEntered.Task,
                    await Task.WhenAny(providerEntered.Task, firstCall, Task.Delay(TimeSpan.FromSeconds(10))).ConfigureAwait(false));
                queuedCall = app.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync(cancellation.Token);
                Assert.AreSame(secondCallQueued.Task,
                    await Task.WhenAny(secondCallQueued.Task, queuedCall, Task.Delay(TimeSpan.FromSeconds(10))).ConfigureAwait(false));
                Assert.IsFalse(queuedCall.IsCompleted);
                cancellation.Cancel();
                Assert.AreSame(queuedCall,
                    await Task.WhenAny(queuedCall, Task.Delay(TimeSpan.FromSeconds(10))).ConfigureAwait(false));
                await Assert.ThrowsAsync<OperationCanceledException>(() => queuedCall).ConfigureAwait(false);

                // Assert
                Assert.IsTrue(queuedCall.IsCanceled);
                Assert.AreEqual(1, providerCount, "The queued call must not enter the provider.");
                Assert.AreEqual(0, completionCount, "Canceling a queued provider call must not notify completion.");
                releaseProvider.TrySetResult(true);
                Assert.AreSame(firstCall,
                    await Task.WhenAny(firstCall, Task.Delay(TimeSpan.FromSeconds(10))).ConfigureAwait(false));
                Assert.AreEqual(TestConstants.DefaultAccessToken, (await firstCall.ConfigureAwait(false)).AccessToken);
                Assert.AreEqual(1, completionCount, "The successful first call must still notify completion.");
            }
            finally
            {
                cancellation.Cancel();
                releaseProvider.TrySetResult(true);
                await Task.WhenAny(firstCall, Task.Delay(TimeSpan.FromSeconds(10))).ConfigureAwait(false);
                if (queuedCall is not null)
                {
                    await Task.WhenAny(queuedCall, Task.Delay(TimeSpan.FromSeconds(10))).ConfigureAwait(false);
                }
            }
        }

        [TestMethod]
        public async Task AcquireTokenForClient_BackgroundHttpRequestCanceled_DoesNotInvokeCompletion()
        {
            // Arrange
            using var harness = CreateTestHarness();
            harness.HttpManager.AddInstanceDiscoveryMockHandler();
            using var certificate = CertHelper.CreateTestCert();
            using var cancellation = new CancellationTokenSource();
            using var handler = new CancelableTokenRequestHandler();
            harness.HttpManager.AddMockHandler(handler);
            int completionCount = 0;
            var cancellationObserved = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithAuthority(AzureCloudInstance.AzurePublic, TestConstants.Utid)
                .WithHttpManager(harness.HttpManager)
                .WithCertificate(_ => Task.FromResult(certificate), new CertificateOptions())
                .WithLogging((_, message, _) =>
                {
                    if (message.Contains(SilentRequestHelper.ProactiveRefreshCancellationError))
                    {
                        cancellationObserved.TrySetResult(true);
                    }
                })
                .OnCompletion((_, _) =>
                {
                    Interlocked.Increment(ref completionCount);
                    return Task.CompletedTask;
                })
                .BuildConcrete();
            TokenCacheHelper.PopulateCache(app.AppTokenCacheInternal.Accessor, addSecondAt: false);
            TestCommon.UpdateATWithRefreshOn(app.AppTokenCacheInternal.Accessor);

            // Act
            Task<AuthenticationResult> acquisition = app.AcquireTokenForClient(TestConstants.s_scope)
                .ExecuteAsync(cancellation.Token);
            try
            {
                Assert.AreSame(acquisition,
                    await Task.WhenAny(acquisition, Task.Delay(TimeSpan.FromSeconds(10))).ConfigureAwait(false),
                    "The foreground cache hit must return without waiting for the background token POST.");
                var result = await acquisition.ConfigureAwait(false);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreSame(handler.RequestStarted,
                    await Task.WhenAny(handler.RequestStarted, Task.Delay(TimeSpan.FromSeconds(10))).ConfigureAwait(false));
                cancellation.Cancel();
                Assert.AreSame(cancellationObserved.Task,
                    await Task.WhenAny(cancellationObserved.Task, Task.Delay(TimeSpan.FromSeconds(10))).ConfigureAwait(false));

                // Assert
                Assert.AreEqual(0, completionCount, "Neither the cache hit nor the canceled background request should invoke OnCompletion.");
                Assert.AreEqual(1, handler.PostCount);
                Assert.IsTrue(certificate.HasPrivateKey);
            }
            finally
            {
                cancellation.Cancel();
                await Task.WhenAny(acquisition, Task.Delay(TimeSpan.FromSeconds(10))).ConfigureAwait(false);
                await Task.WhenAny(cancellationObserved.Task, Task.Delay(TimeSpan.FromSeconds(10))).ConfigureAwait(false);
            }
        }

        private sealed class CancelableTokenRequestHandler : MockHttpMessageHandler
        {
            private readonly TaskCompletionSource<bool> _requestStarted =
                new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            internal Task RequestStarted => _requestStarted.Task;
            internal int PostCount { get; private set; }
            internal string TokenEndpoint { get; private set; }

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Assert.AreEqual(HttpMethod.Post, request.Method);
                StringAssert.EndsWith(request.RequestUri.AbsolutePath, "/token");
                PostCount++;
                TokenEndpoint = request.RequestUri.AbsoluteUri;
                _requestStarted.TrySetResult(true);
                await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
                throw new AssertFailedException("The token request must end through cancellation.");
            }
        }

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
                Assert.IsNotNull(completion);
                Assert.IsTrue(completion.Successful);
                Assert.IsNull(completion.ClientCertificate);

                // Act
                var cached = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync().ConfigureAwait(false);

                // Assert
                Assert.AreEqual(TokenSource.Cache, cached.AuthenticationResultMetadata.TokenSource);
                Assert.IsNull(cached.CertificateThumbprint256);
                Assert.AreEqual(4, providerCount, "The cache hit should only invoke assertion preflight.");
            }
        }

        #endregion
    }
}
#endif
