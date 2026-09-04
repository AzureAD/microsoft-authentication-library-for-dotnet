// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
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
                        new ManagedIdentityCapabilitiesOptions { ImdsProbeTimeout = TimeSpan.Zero },
                        CancellationToken.None));

                Assert.ThrowsExactly<ArgumentOutOfRangeException>(
                    () => application.GetManagedIdentityCapabilitiesAsync(
                        new ManagedIdentityCapabilitiesOptions { ImdsProbeTimeout = TimeSpan.FromMilliseconds(-1) },
                        CancellationToken.None));

                Assert.ThrowsExactly<ArgumentOutOfRangeException>(
                    () => application.GetManagedIdentityCapabilitiesAsync(
                        new ManagedIdentityCapabilitiesOptions
                        {
                            ImdsProbeTimeout = TimeSpan.FromMilliseconds((double)int.MaxValue + 1)
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
                        new ManagedIdentityCapabilitiesOptions { ImdsProbeTimeout = TimeSpan.Zero },
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
                        new ManagedIdentityCapabilitiesOptions { ImdsProbeTimeout = TimeSpan.FromSeconds(5) },
                        callerCancellationSource.Token)
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(ManagedIdentitySource.AppService, capabilities.Source);
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
                    new ManagedIdentityCapabilitiesOptions { ImdsProbeTimeout = null },
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
                    new ManagedIdentityCapabilitiesOptions { ImdsProbeTimeout = s_testTimeout },
                    CancellationToken.None);

                await handler.RequestStarted.ConfigureAwait(false);
                await handler.CancellationObserved.ConfigureAwait(false);

                // Act
                MsalServiceException exception = await Assert.ThrowsExactlyAsync<MsalServiceException>(
                    async () => await discoveryTask.ConfigureAwait(false)).ConfigureAwait(false);

                // Assert
                Assert.AreEqual(MsalError.RequestTimeout, exception.ErrorCode);
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
                    new ManagedIdentityCapabilitiesOptions { ImdsProbeTimeout = s_testTimeout },
                    CancellationToken.None);

                await retryPolicy.DelayStarted.ConfigureAwait(false);
                await retryPolicy.CancellationObserved.ConfigureAwait(false);

                // Act
                MsalServiceException exception = await Assert.ThrowsExactlyAsync<MsalServiceException>(
                    async () => await discoveryTask.ConfigureAwait(false)).ConfigureAwait(false);

                // Assert
                Assert.AreEqual(MsalError.RequestTimeout, exception.ErrorCode);
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
                        new ManagedIdentityCapabilitiesOptions { ImdsProbeTimeout = TimeSpan.FromSeconds(5) },
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
                        new ManagedIdentityCapabilitiesOptions { ImdsProbeTimeout = TimeSpan.FromSeconds(30) },
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
                    new ManagedIdentityCapabilitiesOptions { ImdsProbeTimeout = s_testTimeout },
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
                        ImdsProbeTimeout = TimeSpan.FromMilliseconds(200)
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
        public async Task RetryPolicies_NoResponseException_DoNotRetryAsync()
        {
            // Arrange
            IRetryPolicy imdsRetryPolicy = new TestImdsRetryPolicy();
            IRetryPolicy imdsProbeRetryPolicy = new TestImdsProbeRetryPolicy();
            IRetryPolicy regionDiscoveryRetryPolicy = new TestRegionDiscoveryRetryPolicy();
            ILoggerAdapter logger = Substitute.For<ILoggerAdapter>();
            var timeoutException = new TaskCanceledException();

            // Act
            bool retryImds = await imdsRetryPolicy.PauseForRetryAsync(
                response: null,
                timeoutException,
                retryCount: 0,
                logger,
                CancellationToken.None).ConfigureAwait(false);
            bool retryImdsProbe = await imdsProbeRetryPolicy.PauseForRetryAsync(
                response: null,
                timeoutException,
                retryCount: 0,
                logger,
                CancellationToken.None).ConfigureAwait(false);
            bool retryRegionDiscovery = await regionDiscoveryRetryPolicy.PauseForRetryAsync(
                response: null,
                timeoutException,
                retryCount: 0,
                logger,
                CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.IsFalse(retryImds);
            Assert.IsFalse(retryImdsProbe);
            Assert.IsFalse(retryRegionDiscovery);
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
