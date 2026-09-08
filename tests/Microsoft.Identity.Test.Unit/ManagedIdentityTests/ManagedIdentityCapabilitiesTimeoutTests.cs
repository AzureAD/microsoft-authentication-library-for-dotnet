// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Http.Retry;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.ManagedIdentity.KeyProviders;
using Microsoft.Identity.Client.ManagedIdentity.V2;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Unit.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using static Microsoft.Identity.Test.Common.Core.Helpers.ManagedIdentityTestUtil;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    public class ManagedIdentityCapabilitiesTimeoutTests : TestBase
    {
        private static readonly TimeSpan s_testTimeout = TimeSpan.FromSeconds(2);

        [TestMethod]
        public void GetManagedIdentityCapabilities_WithInvalidOptions_ThrowsSynchronously()
        {
            // Arrange
            using (var httpManager = new MockHttpManager())
            {
                ManagedIdentityApplication application = CreateApplication(httpManager);

                // Act / Assert
                Assert.ThrowsExactly<ArgumentNullException>(
                    () => application.GetManagedIdentityCapabilitiesAsync(
                        options: null,
                        cancellationToken: CancellationToken.None));

                Assert.ThrowsExactly<ArgumentOutOfRangeException>(
                    () => application.GetManagedIdentityCapabilitiesAsync(
                        new ManagedIdentityCapabilitiesOptions { CapabilityDiscoveryTimeout = TimeSpan.Zero },
                        CancellationToken.None));

                Assert.ThrowsExactly<ArgumentOutOfRangeException>(
                    () => application.GetManagedIdentityCapabilitiesAsync(
                        new ManagedIdentityCapabilitiesOptions { CapabilityDiscoveryTimeout = TimeSpan.FromMilliseconds(-1) },
                        CancellationToken.None));

                Assert.ThrowsExactly<ArgumentOutOfRangeException>(
                    () => application.GetManagedIdentityCapabilitiesAsync(
                        new ManagedIdentityCapabilitiesOptions
                        {
                            CapabilityDiscoveryTimeout = TimeSpan.FromMilliseconds((double)int.MaxValue + 1)
                        },
                        CancellationToken.None));
            }
        }

        [TestMethod]
        public async Task GetManagedIdentityCapabilities_InvalidOptions_ThrowsWhenResultIsCachedAsync()
        {
            // Arrange
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.AppService, ManagedIdentityTests.AppServiceEndpoint);
                ManagedIdentityApplication application = CreateApplication(httpManager);

                await application
                    .GetManagedIdentityCapabilitiesAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                // Act / Assert
                Assert.ThrowsExactly<ArgumentOutOfRangeException>(
                    () => application.GetManagedIdentityCapabilitiesAsync(
                        new ManagedIdentityCapabilitiesOptions { CapabilityDiscoveryTimeout = TimeSpan.Zero },
                        CancellationToken.None));
            }
        }

        [TestMethod]
        public async Task GetManagedIdentityCapabilities_PreCanceledToken_ReturnsCachedResultAsync()
        {
            // Arrange
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            using (var callerCancellationSource = new CancellationTokenSource())
            {
                SetEnvironmentVariables(ManagedIdentitySource.AppService, ManagedIdentityTests.AppServiceEndpoint);
                ManagedIdentityApplication application = CreateApplication(httpManager);

                await application
                    .GetManagedIdentityCapabilitiesAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                callerCancellationSource.Cancel();

                // Act
                ManagedIdentityCapabilities capabilities = await application
                    .GetManagedIdentityCapabilitiesAsync(
                        new ManagedIdentityCapabilitiesOptions { CapabilityDiscoveryTimeout = TimeSpan.FromSeconds(5) },
                        callerCancellationSource.Token)
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(ManagedIdentitySource.AppService, capabilities.Source);
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task GetManagedIdentityCapabilities_PreCanceledTokenWithoutTimeout_DetectsEnvironmentAsync(
            bool useOptionsOverload)
        {
            // Arrange
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            using (var callerCancellationSource = new CancellationTokenSource())
            {
                SetEnvironmentVariables(ManagedIdentitySource.AppService, ManagedIdentityTests.AppServiceEndpoint);
                ManagedIdentityApplication application = CreateApplication(httpManager);
                callerCancellationSource.Cancel();

                // Act
                ManagedIdentityCapabilities capabilities = useOptionsOverload
                    ? await application.GetManagedIdentityCapabilitiesAsync(
                        new ManagedIdentityCapabilitiesOptions { CapabilityDiscoveryTimeout = null },
                        callerCancellationSource.Token).ConfigureAwait(false)
                    : await application.GetManagedIdentityCapabilitiesAsync(
                        callerCancellationSource.Token).ConfigureAwait(false);

                // Assert
                Assert.AreEqual(ManagedIdentitySource.AppService, capabilities.Source);
            }
        }

        [TestMethod]
        public async Task GetManagedIdentityCapabilities_PreCanceledTokenWithTimeout_ThrowsBeforeEnvironmentDetectionAsync()
        {
            // Arrange
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            using (var callerCancellationSource = new CancellationTokenSource())
            {
                SetEnvironmentVariables(ManagedIdentitySource.AppService, ManagedIdentityTests.AppServiceEndpoint);
                ManagedIdentityApplication application = CreateApplication(httpManager);
                callerCancellationSource.Cancel();

                // Act / Assert
                await Assert.ThrowsExactlyAsync<OperationCanceledException>(
                    async () => await application.GetManagedIdentityCapabilitiesAsync(
                        new ManagedIdentityCapabilitiesOptions
                        {
                            CapabilityDiscoveryTimeout = TimeSpan.FromSeconds(5)
                        },
                        callerCancellationSource.Token).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task GetManagedIdentityCapabilities_DefaultLiteral_UsesExistingOverloadAsync()
        {
            // Arrange
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.AppService, ManagedIdentityTests.AppServiceEndpoint);
                ManagedIdentityApplication application = CreateApplication(httpManager);

                // Act
                ManagedIdentityCapabilities capabilities = await application
                    .GetManagedIdentityCapabilitiesAsync(default)
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(ManagedIdentitySource.AppService, capabilities.Source);
            }
        }

        [TestMethod]
        [Timeout(10000, CooperativeCancellation = true)]
        public async Task GetManagedIdentityCapabilities_NullTimeout_DoesNotBoundDiscoveryAsync()
        {
            // Arrange
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, ManagedIdentityTests.ImdsEndpoint);

                var retryPolicy = new GatedImdsProbeRetryPolicy();
                ManagedIdentityApplication application = CreateApplication(
                    httpManager,
                    new GatedImdsProbeRetryPolicyFactory(retryPolicy),
                    new InMemoryManagedIdentityKeyProvider());

                httpManager.AddMockHandler(MockHelpers.MockImdsProbeFailure(ImdsVersion.V2, retry: true));
                httpManager.AddMockHandler(MockHelpers.MockImdsProbe(ImdsVersion.V2));

                Task<ManagedIdentityCapabilities> discoveryTask = application.GetManagedIdentityCapabilitiesAsync(
                    new ManagedIdentityCapabilitiesOptions { CapabilityDiscoveryTimeout = null },
                    CancellationToken.None);

                await retryPolicy.DelayStarted.ConfigureAwait(false);

                // Act
                Assert.IsFalse(discoveryTask.IsCompleted);
                retryPolicy.ReleaseDelay();
                ManagedIdentityCapabilities capabilities = await discoveryTask.ConfigureAwait(false);

                // Assert
                Assert.AreEqual(ManagedIdentitySource.Imds, capabilities.Source);
                Assert.AreEqual(MtlsBindingStrength.Software, capabilities.MaxSupportedBindingStrength);
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        [Timeout(10000, CooperativeCancellation = true)]
        public async Task GetManagedIdentityCapabilities_CallerCancellationDuringRetryDelayWithoutTimeout_PreservesDelayAsync(
            bool useOptionsOverload)
        {
            // Arrange
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            using (var callerCancellationSource = new CancellationTokenSource())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, ManagedIdentityTests.ImdsEndpoint);

                var retryPolicy = new GatedImdsProbeRetryPolicy();
                ManagedIdentityApplication application = CreateApplication(
                    httpManager,
                    new GatedImdsProbeRetryPolicyFactory(retryPolicy),
                    new InMemoryManagedIdentityKeyProvider());

                httpManager.AddMockHandler(MockHelpers.MockImdsProbeFailure(ImdsVersion.V2, retry: true));
                httpManager.AddMockHandler(MockHelpers.MockImdsProbe(ImdsVersion.V2));

                Task<ManagedIdentityCapabilities> discoveryTask = useOptionsOverload
                    ? application.GetManagedIdentityCapabilitiesAsync(
                        new ManagedIdentityCapabilitiesOptions { CapabilityDiscoveryTimeout = null },
                        callerCancellationSource.Token)
                    : application.GetManagedIdentityCapabilitiesAsync(callerCancellationSource.Token);

                await retryPolicy.DelayStarted.ConfigureAwait(false);

                // Act
                callerCancellationSource.Cancel();
                retryPolicy.ReleaseDelay();

                Exception exception = null;
                try
                {
                    await discoveryTask.ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }

                // Assert
                Assert.IsInstanceOfType<OperationCanceledException>(exception);
                Assert.IsFalse(retryPolicy.CancellationObserved.IsCompleted);
                Assert.AreEqual(0, httpManager.QueueSize);
            }
        }

        [TestMethod]
        [Timeout(10000, CooperativeCancellation = true)]
        public async Task GetManagedIdentityCapabilities_TimeoutDuringProbe_ThrowsRequestTimeoutAsync()
        {
            // Arrange
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, ManagedIdentityTests.ImdsEndpoint);

                var handler = new BlockingMockHttpMessageHandler();
                httpManager.AddMockHandler(handler);
                ManagedIdentityApplication application = CreateApplication(httpManager);

                Task<ManagedIdentityCapabilities> discoveryTask = application.GetManagedIdentityCapabilitiesAsync(
                    new ManagedIdentityCapabilitiesOptions { CapabilityDiscoveryTimeout = s_testTimeout },
                    CancellationToken.None);

                await handler.RequestStarted.ConfigureAwait(false);
                await handler.CancellationObserved.ConfigureAwait(false);

                // Act
                MsalServiceException exception = await Assert.ThrowsExactlyAsync<MsalServiceException>(
                    async () => await discoveryTask.ConfigureAwait(false)).ConfigureAwait(false);

                // Assert
                Assert.AreEqual(MsalError.RequestTimeout, exception.ErrorCode);
                Assert.AreEqual(MsalErrorMessage.ManagedIdentityCapabilityDiscoveryTimeout, exception.Message);
            }
        }

        [TestMethod]
        [Timeout(10000, CooperativeCancellation = true)]
        public async Task GetManagedIdentityCapabilities_TimeoutDuringRetryDelay_ThrowsRequestTimeoutAsync()
        {
            // Arrange
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, ManagedIdentityTests.ImdsEndpoint);

                var retryPolicy = new GatedImdsProbeRetryPolicy();
                ManagedIdentityApplication application = CreateApplication(
                    httpManager,
                    new GatedImdsProbeRetryPolicyFactory(retryPolicy));

                httpManager.AddMockHandler(MockHelpers.MockImdsProbeFailure(ImdsVersion.V2, retry: true));

                Task<ManagedIdentityCapabilities> discoveryTask = application.GetManagedIdentityCapabilitiesAsync(
                    new ManagedIdentityCapabilitiesOptions { CapabilityDiscoveryTimeout = s_testTimeout },
                    CancellationToken.None);

                await retryPolicy.DelayStarted.ConfigureAwait(false);
                await retryPolicy.CancellationObserved.ConfigureAwait(false);

                // Act
                MsalServiceException exception = await Assert.ThrowsExactlyAsync<MsalServiceException>(
                    async () => await discoveryTask.ConfigureAwait(false)).ConfigureAwait(false);

                // Assert
                Assert.AreEqual(MsalError.RequestTimeout, exception.ErrorCode);
                Assert.AreEqual(MsalErrorMessage.ManagedIdentityCapabilityDiscoveryTimeout, exception.Message);
            }
        }

        [TestMethod]
        [Timeout(12000, CooperativeCancellation = true)]
        public async Task GetManagedIdentityCapabilities_V2DelayLeavesOnlyRemainingBudgetForV1Async()
        {
            // Arrange
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, ManagedIdentityTests.ImdsEndpoint);
                ManagedIdentityApplication application = CreateApplication(httpManager);
                var v2Handler = new GatedResponseMockHttpMessageHandler(HttpStatusCode.NotFound);
                var v1Handler = new BlockingMockHttpMessageHandler();
                httpManager.AddMockHandler(v2Handler);
                httpManager.AddMockHandler(v1Handler);

                Task<ManagedIdentityCapabilities> discoveryTask = application
                    .GetManagedIdentityCapabilitiesAsync(
                        new ManagedIdentityCapabilitiesOptions
                        {
                            CapabilityDiscoveryTimeout = TimeSpan.FromSeconds(4)
                        },
                        CancellationToken.None);

                await v2Handler.RequestStarted.ConfigureAwait(false);
                await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
                v2Handler.Release();
                await v1Handler.RequestStarted.ConfigureAwait(false);
                var remainingBudgetStopwatch = Stopwatch.StartNew();

                // Act
                MsalServiceException exception = await Assert.ThrowsExactlyAsync<MsalServiceException>(
                    async () => await discoveryTask.ConfigureAwait(false)).ConfigureAwait(false);
                remainingBudgetStopwatch.Stop();

                // Assert
                Assert.AreEqual(MsalError.RequestTimeout, exception.ErrorCode);
                Assert.AreEqual(MsalErrorMessage.ManagedIdentityCapabilityDiscoveryTimeout, exception.Message);
                Assert.IsTrue(
                    remainingBudgetStopwatch.Elapsed < TimeSpan.FromSeconds(3),
                    $"IMDSv1 received a fresh timeout instead of the remaining budget. Elapsed: {remainingBudgetStopwatch.Elapsed}.");
            }
        }

        [TestMethod]
        public async Task GetManagedIdentityCapabilities_FastV2Failure_FallsBackToV1WithinBudgetAsync()
        {
            // Arrange
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, ManagedIdentityTests.ImdsEndpoint);
                ManagedIdentityApplication application = CreateApplication(
                    httpManager,
                    new TestRetryPolicyFactory());

                httpManager.AddMockHandler(MockHelpers.MockImdsProbeFailure(ImdsVersion.V2));
                httpManager.AddMockHandler(MockHelpers.MockImdsProbe(ImdsVersion.V1));
                httpManager.AddMockHandler(MockHelpers.MockImdsComputeMetadata());

                // Act
                ManagedIdentityCapabilities capabilities = await application
                    .GetManagedIdentityCapabilitiesAsync(
                        new ManagedIdentityCapabilitiesOptions
                        {
                            CapabilityDiscoveryTimeout = TimeSpan.FromSeconds(5)
                        },
                        CancellationToken.None)
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(ManagedIdentitySource.Imds, capabilities.Source);
                Assert.AreEqual(MtlsBindingStrength.Software, capabilities.MaxSupportedBindingStrength);
                Assert.AreEqual(0, httpManager.QueueSize);
            }
        }

        [TestMethod]
        public async Task GetManagedIdentityCapabilities_CallerCancellation_IsNotTranslatedAsync()
        {
            // Arrange
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            using (var callerCancellationSource = new CancellationTokenSource())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, ManagedIdentityTests.ImdsEndpoint);
                ManagedIdentityApplication application = CreateApplication(httpManager);

                var handler = MockHelpers.MockImdsProbe(ImdsVersion.V2);
                handler.AdditionalRequestValidation = _ => callerCancellationSource.Cancel();
                httpManager.AddMockHandler(handler);

                // Act
                Exception exception = null;
                try
                {
                    await application.GetManagedIdentityCapabilitiesAsync(
                        new ManagedIdentityCapabilitiesOptions { CapabilityDiscoveryTimeout = TimeSpan.FromSeconds(30) },
                        callerCancellationSource.Token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }

                // Assert
                Assert.IsInstanceOfType<OperationCanceledException>(exception);
                Assert.IsNotInstanceOfType<MsalException>(exception);
            }
        }

        [TestMethod]
        [Timeout(10000, CooperativeCancellation = true)]
        public async Task GetManagedIdentityCapabilities_TimeoutDuringComputeMetadata_DoesNotCacheResultAsync()
        {
            // Arrange
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, ManagedIdentityTests.ImdsEndpoint);
                ManagedIdentityApplication application = CreateApplication(
                    httpManager,
                    new TestRetryPolicyFactory());
                var metadataHandler = new BlockingMockHttpMessageHandler();

                httpManager.AddMockHandler(MockHelpers.MockImdsProbeFailure(ImdsVersion.V2));
                httpManager.AddMockHandler(MockHelpers.MockImdsProbe(ImdsVersion.V1));
                httpManager.AddMockHandler(metadataHandler);

                Task<ManagedIdentityCapabilities> firstDiscovery = application
                    .GetManagedIdentityCapabilitiesAsync(
                        new ManagedIdentityCapabilitiesOptions
                        {
                            CapabilityDiscoveryTimeout = s_testTimeout
                        },
                        CancellationToken.None);

                await metadataHandler.RequestStarted.ConfigureAwait(false);
                await metadataHandler.CancellationObserved.ConfigureAwait(false);

                MsalServiceException timeoutException = await Assert.ThrowsExactlyAsync<MsalServiceException>(
                    async () => await firstDiscovery.ConfigureAwait(false)).ConfigureAwait(false);

                httpManager.AddMockHandler(MockHelpers.MockImdsProbeFailure(ImdsVersion.V2));
                httpManager.AddMockHandler(MockHelpers.MockImdsProbe(ImdsVersion.V1));
                httpManager.AddMockHandler(MockHelpers.MockImdsComputeMetadata());

                // Act
                ManagedIdentityCapabilities capabilities = await application
                    .GetManagedIdentityCapabilitiesAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(MsalError.RequestTimeout, timeoutException.ErrorCode);
                Assert.AreEqual(MsalErrorMessage.ManagedIdentityCapabilityDiscoveryTimeout, timeoutException.Message);
                Assert.AreEqual(ManagedIdentitySource.Imds, capabilities.Source);
                Assert.AreEqual(MtlsBindingStrength.Software, capabilities.MaxSupportedBindingStrength);
                Assert.AreEqual(0, httpManager.QueueSize);
            }
        }

        [TestMethod]
        [Timeout(10000, CooperativeCancellation = true)]
        public async Task GetManagedIdentityCapabilities_TimeoutAfterKeyWork_DoesNotCacheResultAsync()
        {
            // Arrange
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, ManagedIdentityTests.ImdsEndpoint);

                var keyProvider = new GatedManagedIdentityKeyProvider();
                ManagedIdentityApplication application = CreateApplication(
                    httpManager,
                    new TestRetryPolicyFactory(),
                    keyProvider);

                httpManager.AddMockHandler(MockHelpers.MockImdsProbe(ImdsVersion.V2));

                Task<ManagedIdentityCapabilities> firstDiscovery = application.GetManagedIdentityCapabilitiesAsync(
                    new ManagedIdentityCapabilitiesOptions { CapabilityDiscoveryTimeout = s_testTimeout },
                    CancellationToken.None);

                await keyProvider.Entered.ConfigureAwait(false);
                await keyProvider.CancellationObserved.ConfigureAwait(false);
                keyProvider.Release();

                MsalServiceException timeoutException = await Assert.ThrowsExactlyAsync<MsalServiceException>(
                    async () => await firstDiscovery.ConfigureAwait(false)).ConfigureAwait(false);

                httpManager.AddMockHandler(MockHelpers.MockImdsProbe(ImdsVersion.V2));

                // Act
                ManagedIdentityCapabilities capabilities = await application
                    .GetManagedIdentityCapabilitiesAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(MsalError.RequestTimeout, timeoutException.ErrorCode);
                Assert.AreEqual(MsalErrorMessage.ManagedIdentityCapabilityDiscoveryTimeout, timeoutException.Message);
                Assert.AreEqual(ManagedIdentitySource.Imds, capabilities.Source);
                Assert.AreEqual(MtlsBindingStrength.Software, capabilities.MaxSupportedBindingStrength);
                Assert.AreEqual(0, httpManager.QueueSize);
            }
        }

        [TestMethod]
        [Timeout(10000, CooperativeCancellation = true)]
        public async Task GetManagedIdentityCapabilities_TimedOutWaiter_DoesNotCancelLockOwnerAsync()
        {
            // Arrange
            using (new EnvVariableContext())
            using (var ownerHttpManager = new MockHttpManager())
            using (var waiterHttpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, ManagedIdentityTests.ImdsEndpoint);

                var ownerKeyProvider = new GatedManagedIdentityKeyProvider();
                ManagedIdentityApplication owner = CreateApplication(
                    ownerHttpManager,
                    new TestRetryPolicyFactory(),
                    ownerKeyProvider);
                ManagedIdentityApplication waiter = CreateApplication(waiterHttpManager);

                ownerHttpManager.AddMockHandler(MockHelpers.MockImdsProbe(ImdsVersion.V2));

                Task<ManagedIdentityCapabilities> ownerTask =
                    owner.GetManagedIdentityCapabilitiesAsync(CancellationToken.None);
                await ownerKeyProvider.Entered.ConfigureAwait(false);

                Task<ManagedIdentityCapabilities> waiterTask = waiter.GetManagedIdentityCapabilitiesAsync(
                    new ManagedIdentityCapabilitiesOptions
                    {
                        CapabilityDiscoveryTimeout = TimeSpan.FromMilliseconds(200)
                    },
                    CancellationToken.None);

                try
                {
                    MsalServiceException timeoutException = await Assert.ThrowsExactlyAsync<MsalServiceException>(
                        async () => await waiterTask.ConfigureAwait(false)).ConfigureAwait(false);

                    Assert.AreEqual(MsalError.RequestTimeout, timeoutException.ErrorCode);
                    Assert.IsFalse(ownerTask.IsCompleted);
                }
                finally
                {
                    ownerKeyProvider.Release();
                }

                // Act
                ManagedIdentityCapabilities ownerCapabilities = await ownerTask.ConfigureAwait(false);

                // Assert
                Assert.AreEqual(ManagedIdentitySource.Imds, ownerCapabilities.Source);
                Assert.AreEqual(MtlsBindingStrength.Software, ownerCapabilities.MaxSupportedBindingStrength);
                Assert.AreEqual(0, waiterHttpManager.QueueSize);
            }
        }

        [TestMethod]
        [Timeout(5000, CooperativeCancellation = true)]
        public async Task DefaultAndRegionRetryPolicies_IgnoreCancellationDuringDelayAsync()
        {
            // Arrange
            IRetryPolicy stsRetryPolicy = new DefaultRetryPolicy(RequestType.STS);
            IRetryPolicy managedIdentityRetryPolicy = new DefaultRetryPolicy(RequestType.ManagedIdentityDefault);
            IRetryPolicy regionDiscoveryRetryPolicy = new RegionDiscoveryRetryPolicy();
            ILoggerAdapter logger = Substitute.For<ILoggerAdapter>();
            var response = new HttpResponse { StatusCode = HttpStatusCode.InternalServerError };
            using (var cancellationSource = new CancellationTokenSource())
            {
                cancellationSource.Cancel();

                // Act
                Task<bool> retryStsTask = stsRetryPolicy.PauseForRetryAsync(
                    response,
                    exception: null,
                    retryCount: 0,
                    logger,
                    cancellationSource.Token);
                Task<bool> retryManagedIdentityTask = managedIdentityRetryPolicy.PauseForRetryAsync(
                    response,
                    exception: null,
                    retryCount: 0,
                    logger,
                    cancellationSource.Token);
                Task<bool> retryRegionDiscoveryTask = regionDiscoveryRetryPolicy.PauseForRetryAsync(
                    response,
                    exception: null,
                    retryCount: 0,
                    logger,
                    cancellationSource.Token);
                bool[] retryResults = await Task.WhenAll(
                    retryStsTask,
                    retryManagedIdentityTask,
                    retryRegionDiscoveryTask).ConfigureAwait(false);

                // Assert
                Assert.IsTrue(retryResults[0]);
                Assert.IsTrue(retryResults[1]);
                Assert.IsTrue(retryResults[2]);
            }
        }

        private static ManagedIdentityApplication CreateApplication(
            MockHttpManager httpManager,
            IRetryPolicyFactory retryPolicyFactory = null,
            IManagedIdentityKeyProvider keyProvider = null)
        {
            ManagedIdentityApplicationBuilder builder = ManagedIdentityApplicationBuilder
                .Create(ManagedIdentityId.SystemAssigned)
                .WithHttpManager(httpManager);

            if (retryPolicyFactory is not null)
            {
                builder.WithRetryPolicyFactory(retryPolicyFactory);
            }

            var application = builder.Build() as ManagedIdentityApplication;

            if (keyProvider is not null)
            {
                var platformProxy = Substitute.For<IPlatformProxy>();
                platformProxy.ManagedIdentityKeyProvider.Returns(keyProvider);
                application.ServiceBundle.SetPlatformProxyForTest(platformProxy);
            }

            return application;
        }

        private sealed class GatedImdsProbeRetryPolicyFactory : TestRetryPolicyFactory
        {
            private readonly IRetryPolicy _retryPolicy;

            internal GatedImdsProbeRetryPolicyFactory(IRetryPolicy retryPolicy)
            {
                _retryPolicy = retryPolicy;
            }

            public override IRetryPolicy GetRetryPolicy(RequestType requestType)
            {
                return requestType == RequestType.ImdsProbe
                    ? _retryPolicy
                    : base.GetRetryPolicy(requestType);
            }
        }

        private sealed class GatedImdsProbeRetryPolicy : ImdsProbeRetryPolicy
        {
            private readonly TaskCompletionSource<bool> _delayStarted =
                new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            private readonly TaskCompletionSource<bool> _cancellationObserved =
                new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            private readonly SemaphoreSlim _delayRelease = new SemaphoreSlim(0, 1);

            internal Task DelayStarted => _delayStarted.Task;
            internal Task CancellationObserved => _cancellationObserved.Task;

            internal void ReleaseDelay()
            {
                _delayRelease.Release();
            }

            internal override async Task DelayAsync(int milliseconds, CancellationToken cancellationToken)
            {
                _delayStarted.TrySetResult(true);

                try
                {
                    await _delayRelease.WaitAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    _cancellationObserved.TrySetResult(true);
                    throw;
                }
            }
        }

        private sealed class GatedManagedIdentityKeyProvider : IManagedIdentityKeyProvider
        {
            private readonly InMemoryManagedIdentityKeyProvider _innerProvider =
                new InMemoryManagedIdentityKeyProvider();
            private readonly TaskCompletionSource<bool> _entered =
                new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            private readonly TaskCompletionSource<bool> _cancellationObserved =
                new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            private readonly TaskCompletionSource<bool> _release =
                new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            internal Task Entered => _entered.Task;
            internal Task CancellationObserved => _cancellationObserved.Task;

            internal void Release()
            {
                _release.TrySetResult(true);
            }

            public async Task<ManagedIdentityKeyInfo> GetOrCreateKeyAsync(
                ILoggerAdapter logger,
                CancellationToken cancellationToken)
            {
                using (cancellationToken.Register(() => _cancellationObserved.TrySetResult(true)))
                {
                    _entered.TrySetResult(true);
                    await _release.Task.ConfigureAwait(false);
                }

                return await _innerProvider
                    .GetOrCreateKeyAsync(logger, CancellationToken.None)
                    .ConfigureAwait(false);
            }
        }

        private sealed class GatedResponseMockHttpMessageHandler : MockHttpMessageHandler
        {
            private readonly HttpStatusCode _statusCode;
            private readonly TaskCompletionSource<bool> _requestStarted =
                new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            private readonly TaskCompletionSource<bool> _release =
                new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            internal GatedResponseMockHttpMessageHandler(HttpStatusCode statusCode)
            {
                _statusCode = statusCode;
            }

            internal Task RequestStarted => _requestStarted.Task;

            internal void Release()
            {
                _release.TrySetResult(true);
            }

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                _requestStarted.TrySetResult(true);
                await _release.Task.ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                return new HttpResponseMessage(_statusCode);
            }
        }

        private sealed class BlockingMockHttpMessageHandler : MockHttpMessageHandler
        {
            private readonly TaskCompletionSource<bool> _requestStarted =
                new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            private readonly TaskCompletionSource<bool> _cancellationObserved =
                new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            internal Task RequestStarted => _requestStarted.Task;
            internal Task CancellationObserved => _cancellationObserved.Task;

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                _requestStarted.TrySetResult(true);

                try
                {
                    await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    _cancellationObserved.TrySetResult(true);
                    throw;
                }

                return new HttpResponseMessage(HttpStatusCode.OK);
            }
        }
    }
}
