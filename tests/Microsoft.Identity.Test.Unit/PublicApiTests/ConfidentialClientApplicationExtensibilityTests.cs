// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if !ANDROID && !iOS
using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    [DeploymentItem(@"Resources\testCert.crtfile")]
    public class ConfidentialClientApplicationExtensibilityTests : TestBase
    {
        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();
        }

        #region WithCertificate (Dynamic Provider) Integration Tests

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
                    .WithExperimentalFeatures()
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
                    })
                    .Build();

                harness.HttpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                // Act
                var result = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.IsTrue(providerInvoked, "Certificate provider should have been invoked");
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
                    .WithExperimentalFeatures()
                    .WithAuthority(TestConstants.AuthorityCommonTenant)
                    .WithHttpManager(harness.HttpManager)
                    .WithCertificate((AssertionRequestOptions options) =>
                    {
                        return Task.FromResult<System.Security.Cryptography.X509Certificates.X509Certificate2>(null); // Provider returns null
                    })
                    .Build();

                // Act & Assert
                var exception = await Assert.ThrowsExceptionAsync<MsalClientException>(async () =>
                {
                    await app.AcquireTokenForClient(TestConstants.s_scope)
                        .ExecuteAsync()
                        .ConfigureAwait(false);
                }).ConfigureAwait(false);

                Assert.AreEqual(MsalError.InvalidClientAssertion, exception.ErrorCode);
                Assert.IsTrue(exception.Message.Contains("returned null"));
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

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithExperimentalFeatures()
                    .WithAuthority(TestConstants.AuthorityCommonTenant)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(harness.HttpManager)
                    .OnMsalServiceFailure((AssertionRequestOptions options, MsalException ex) =>
                    {
                        failureCallbackCount++;
                        capturedException = ex as MsalServiceException;
                        
                        Assert.IsNotNull(capturedException, "Exception should be MsalServiceException");
                        Assert.AreEqual(TestConstants.ClientId, options.ClientID);
                        
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
                    .WithExperimentalFeatures()
                    .WithAuthority(TestConstants.AuthorityCommonTenant)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(harness.HttpManager)
                    .OnMsalServiceFailure((AssertionRequestOptions options, MsalException ex) =>
                    {
                        callbackInvoked = true;
                        return Task.FromResult(false); // Don't retry
                    })
                    .Build();

                harness.HttpManager.AddFailureTokenEndpointResponse("request_failed");

                // Act & Assert
                await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
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
                    .WithExperimentalFeatures()
                    .WithAuthority(TestConstants.AuthorityCommonTenant)
                    .WithCertificate((AssertionRequestOptions options) =>
                    {
                        return Task.FromResult<System.Security.Cryptography.X509Certificates.X509Certificate2>(null); // Will cause MsalClientException
                    })
                    .OnMsalServiceFailure((AssertionRequestOptions options, MsalException ex) =>
                    {
                        callbackInvoked = true;
                        return Task.FromResult(false);
                    })
                    .Build();

                // Act & Assert
                var exception = await Assert.ThrowsExceptionAsync<MsalClientException>(async () =>
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
        [Description("OnSuccess is invoked with successful result")]
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
                    .WithExperimentalFeatures()
                    .WithAuthority(TestConstants.AuthorityCommonTenant)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(harness.HttpManager)
                    .OnSuccess((AssertionRequestOptions options, ExecutionResult result) =>
                    {
                        observerInvoked = true;
                        capturedResult = result;
                        capturedOptions = options;
                        
                        Assert.IsTrue(result.Successful);
                        Assert.IsNotNull(result.Result);
                        Assert.IsNull(result.Exception);
                        Assert.AreEqual(TestConstants.ClientId, options.ClientID);
                        
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
        [Description("OnSuccess is invoked with failure result after retries exhausted")]
        public async Task OnSuccess_InvokedWithFailureResult_AfterRetriesExhaustedAsync()
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                int retryCount = 0;
                bool observerInvoked = false;
                ExecutionResult capturedResult = null;

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithExperimentalFeatures()
                    .WithAuthority(TestConstants.AuthorityCommonTenant)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(harness.HttpManager)
                    .OnMsalServiceFailure((AssertionRequestOptions options, MsalException ex) =>
                    {
                        retryCount++;
                        return Task.FromResult(retryCount < 2); // Retry once, then give up
                    })
                    .OnSuccess((AssertionRequestOptions options, ExecutionResult result) =>
                    {
                        observerInvoked = true;
                        capturedResult = result;
                        
                        Assert.IsFalse(result.Successful);
                        Assert.IsNull(result.Result);
                        Assert.IsNotNull(result.Exception);
                        Assert.IsInstanceOfType(result.Exception, typeof(MsalServiceException));
                        
                        return Task.CompletedTask;
                    })
                    .Build();

                // Mock 2 failures
                harness.HttpManager.AddFailureTokenEndpointResponse("request_failed");
                harness.HttpManager.AddFailureTokenEndpointResponse("request_failed");

                // Act & Assert
                var exception = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
                {
                    await app.AcquireTokenForClient(TestConstants.s_scope)
                        .ExecuteAsync()
                        .ConfigureAwait(false);
                }).ConfigureAwait(false);

                Assert.IsTrue(observerInvoked, "Observer should be invoked even on failure");
                Assert.IsNotNull(capturedResult);
                Assert.IsFalse(capturedResult.Successful);
                Assert.AreEqual(exception, capturedResult.Exception);
            }
        }

        [TestMethod]
        [Description("OnSuccess exception is caught and logged, doesn't disrupt flow")]
        public async Task OnSuccess_ExceptionIsCaught_DoesNotDisruptFlowAsync()
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithExperimentalFeatures()
                    .WithAuthority(TestConstants.AuthorityCommonTenant)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(harness.HttpManager)
                    .OnSuccess((AssertionRequestOptions options, ExecutionResult result) =>
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
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                int certProviderCount = 0;
                int retryCallbackCount = 0;
                bool observerInvoked = false;

                var certificate = CertHelper.GetOrCreateTestCert();

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithExperimentalFeatures()
                    .WithAuthority(TestConstants.AuthorityCommonTenant)
                    .WithHttpManager(harness.HttpManager)
                    .WithCertificate((AssertionRequestOptions options) =>
                    {
                        certProviderCount++;
                        Assert.AreEqual(TestConstants.ClientId, options.ClientID);
                        return Task.FromResult(certificate);
                    })
                    .OnMsalServiceFailure((AssertionRequestOptions options, MsalException ex) =>
                    {
                        retryCallbackCount++;
                        Assert.IsInstanceOfType(ex, typeof(MsalServiceException));
                        return Task.FromResult(retryCallbackCount < 2); // Retry once
                    })
                    .OnSuccess((AssertionRequestOptions options, ExecutionResult result) =>
                    {
                        observerInvoked = true;
                        Assert.IsTrue(result.Successful);
                        Assert.IsNotNull(result.Result);
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
            }
        }

        [TestMethod]
        [Description("Certificate rotation scenario: different cert returned on retry")]
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
                    .WithExperimentalFeatures()
                    .WithAuthority(TestConstants.AuthorityCommonTenant)
                    .WithHttpManager(harness.HttpManager)
                    .WithCertificate((AssertionRequestOptions options) =>
                    {
                        certProviderCount++;
                        // Return different cert on retry
                        return Task.FromResult(certProviderCount == 1 ? cert1 : cert2);
                    })
                    .OnMsalServiceFailure((AssertionRequestOptions options, MsalException ex) =>
                    {
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
            }
        }

        #endregion
    }
}
#endif
