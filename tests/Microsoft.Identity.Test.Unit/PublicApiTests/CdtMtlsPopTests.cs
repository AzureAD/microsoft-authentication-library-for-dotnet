// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AuthScheme;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Unit.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class CdtMtlsPopTests : TestBase
    {
        private static X509Certificate2 s_testCertificate;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            s_testCertificate = CertHelper.GetOrCreateTestCert();
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            // Do not dispose — cert is shared via CertHelper.GetOrCreateTestCert()
            // and managed by the static CertHelper dictionary.
        }

        #region Integration Tests — real MtlsPopParametersInitializer code path

        [TestMethod]
        public async Task MtlsPopWithIAuthenticationOperation3_InjectsCertAndPreservesOperation()
        {
            // Arrange
            const string region = "eastus";

            using (var envContext = new EnvVariableContext())
            {
                Environment.SetEnvironmentVariable("REGION_NAME", region);

                using (var httpManager = new MockHttpManager())
                {
                    httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                        tokenType: "mtls_pop");

                    var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                        .WithCertificate(s_testCertificate)
                        .WithAuthority($"https://login.microsoftonline.com/123456-1234-2345-1234561234")
                        .WithAzureRegion(ConfidentialClientApplication.AttemptRegionDiscovery)
                        .WithHttpManager(httpManager)
                        .BuildConcrete();

                    var fakeOp = new FakeAuthOp3();

                    // Act — chain WithMtlsProofOfPossession + WithAuthenticationExtension
                    AuthenticationResult result = await app.AcquireTokenForClient(TestConstants.s_scope)
                        .WithMtlsProofOfPossession()
                        .WithAuthenticationExtension(new MsalAuthenticationExtension
                        {
                            AuthenticationOperation = fakeOp
                        })
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    // Assert — AfterCredentialEvaluation was called, operation was NOT replaced
                    Assert.AreEqual(1, fakeOp.CallbackInvocationCount,
                        "AfterCredentialEvaluation must fire exactly once per token request");
                    Assert.IsNotNull(fakeOp.LastCertificate,
                        "AfterCredentialEvaluation must receive the mTLS certificate");
                    Assert.AreEqual(s_testCertificate.Thumbprint, fakeOp.LastCertificate.Thumbprint);
                    Assert.IsNotNull(result.BindingCertificate,
                        "BindingCertificate should be set by the operation's FormatResult");
                }
            }
        }

        [TestMethod]
        public async Task MtlsPopWithoutIAuthenticationOperation3_ReplacesOperation()
        {
            // Arrange
            const string region = "eastus";

            using (var envContext = new EnvVariableContext())
            {
                Environment.SetEnvironmentVariable("REGION_NAME", region);

                using (var httpManager = new MockHttpManager())
                {
                    httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                        tokenType: "mtls_pop");

                    var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                        .WithCertificate(s_testCertificate)
                        .WithAuthority($"https://login.microsoftonline.com/123456-1234-2345-1234561234")
                        .WithAzureRegion(ConfidentialClientApplication.AttemptRegionDiscovery)
                        .WithHttpManager(httpManager)
                        .BuildConcrete();

                    // Act — only WithMtlsProofOfPossession, no IAuthenticationOperation3
                    AuthenticationResult result = await app.AcquireTokenForClient(TestConstants.s_scope)
                        .WithMtlsProofOfPossession()
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    // Assert — standard mTLS POP behavior
                    Assert.AreEqual(Constants.MtlsPoPAuthHeaderPrefix, result.TokenType);
                    Assert.IsNotNull(result.BindingCertificate);
                }
            }
        }

        [TestMethod]
        public async Task MtlsPopWithIAuthenticationOperation3_CallbackFiresOnEveryExecuteAsync()
        {
            // Arrange
            const string region = "eastus";

            using (var envContext = new EnvVariableContext())
            {
                Environment.SetEnvironmentVariable("REGION_NAME", region);

                using (var httpManager = new MockHttpManager())
                {
                    httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                        tokenType: "mtls_pop");
                    // Second handler for cache-hit path — TryInitAsync may trigger
                    // a new token request if the cache key doesn't match
                    httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                        tokenType: "mtls_pop");

                    var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                        .WithCertificate(s_testCertificate)
                        .WithAuthority($"https://login.microsoftonline.com/123456-1234-2345-1234561234")
                        .WithAzureRegion(ConfidentialClientApplication.AttemptRegionDiscovery)
                        .WithHttpManager(httpManager)
                        .BuildConcrete();

                    var fakeOp = new FakeAuthOp3();

                    // Act — first call (cache miss)
                    await app.AcquireTokenForClient(TestConstants.s_scope)
                        .WithMtlsProofOfPossession()
                        .WithAuthenticationExtension(new MsalAuthenticationExtension
                        {
                            AuthenticationOperation = fakeOp
                        })
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    Assert.AreEqual(1, fakeOp.CallbackInvocationCount,
                        "First call must fire the callback");

                    // Act — second call
                    await app.AcquireTokenForClient(TestConstants.s_scope)
                        .WithMtlsProofOfPossession()
                        .WithAuthenticationExtension(new MsalAuthenticationExtension
                        {
                            AuthenticationOperation = fakeOp
                        })
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    // Assert — callback must fire on every ExecuteAsync call
                    Assert.AreEqual(2, fakeOp.CallbackInvocationCount,
                        "Second call must also fire the callback");
                    Assert.IsNotNull(fakeOp.LastCertificate,
                        "Cert must be provided on every callback invocation");
                }
            }
        }

        [TestMethod]
        public async Task MtlsPopWithIAuthenticationOperation3_ReceivesCorrectCert()
        {
            // Arrange
            const string region = "eastus";

            using (var envContext = new EnvVariableContext())
            {
                Environment.SetEnvironmentVariable("REGION_NAME", region);

                using (var httpManager = new MockHttpManager())
                {
                    httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                        tokenType: "mtls_pop");

                    var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                        .WithCertificate(s_testCertificate)
                        .WithAuthority($"https://login.microsoftonline.com/123456-1234-2345-1234561234")
                        .WithAzureRegion(ConfidentialClientApplication.AttemptRegionDiscovery)
                        .WithHttpManager(httpManager)
                        .BuildConcrete();

                    var fakeOp = new FakeAuthOp3();

                    // Act
                    await app.AcquireTokenForClient(TestConstants.s_scope)
                        .WithMtlsProofOfPossession()
                        .WithAuthenticationExtension(new MsalAuthenticationExtension
                        {
                            AuthenticationOperation = fakeOp
                        })
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    // Assert — cert must match what was configured on the CCA
                    Assert.AreSame(s_testCertificate, fakeOp.LastCertificate,
                        "Callback must receive the exact cert instance configured via WithCertificate");
                }
            }
        }

        [TestMethod]
        public async Task WithoutMtlsPop_IAuthenticationOperation3_CallbackNotFired()
        {
            // Arrange
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithAuthority($"https://login.microsoftonline.com/123456-1234-2345-1234561234")
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                var fakeOp = new FakeAuthOp3();

                // Act — NO WithMtlsProofOfPossession, just the IAuthOp3 extension
                await app.AcquireTokenForClient(TestConstants.s_scope)
                    .WithAuthenticationExtension(new MsalAuthenticationExtension
                    {
                        AuthenticationOperation = fakeOp
                    })
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert — callback should NOT fire when mTLS POP is not requested
                Assert.AreEqual(0, fakeOp.CallbackInvocationCount,
                    "Without WithMtlsProofOfPossession, AfterCredentialEvaluation must not be called");
                Assert.IsNull(fakeOp.LastCertificate);
                Assert.AreEqual("Bearer", fakeOp.AccessTokenType,
                    "Without mTLS POP, operation should stay in Bearer mode");
            }
        }

        #endregion

        #region Unit Tests — AfterCredentialEvaluation behavior

        [TestMethod]
        public void AfterCredentialEvaluation_InjectsCert_OperationAdapts()
        {
            // Arrange
            var fakeOp = new FakeAuthOp3();

            // Assert — before callback
            Assert.AreEqual("Bearer", fakeOp.AccessTokenType);
            Assert.AreEqual(0, fakeOp.CallbackInvocationCount);

            // Act
            fakeOp.AfterCredentialEvaluationAsync_Sync(new TokenAcquisitionContext
            {
                MtlsCertificate = s_testCertificate
            });

            // Assert — after callback
            Assert.AreEqual(1, fakeOp.CallbackInvocationCount);
            Assert.AreEqual("mtls_pop", fakeOp.AccessTokenType);
            Assert.AreEqual("mtls_pop", fakeOp.AuthorizationHeaderPrefix);
            Assert.IsTrue(fakeOp.GetTokenRequestParams().ContainsKey("token_type"));
            Assert.AreEqual("mtls_pop", fakeOp.GetTokenRequestParams()["token_type"]);
        }

        [TestMethod]
        public void AfterCredentialEvaluation_SetsBindingCertOnFormatResult()
        {
            // Arrange
            var fakeOp = new FakeAuthOp3();
            fakeOp.AfterCredentialEvaluationAsync_Sync(new TokenAcquisitionContext
            {
                MtlsCertificate = s_testCertificate
            });
            var authResult = new AuthenticationResult();

            // Act
            fakeOp.FormatResult(authResult);

            // Assert
            Assert.AreSame(s_testCertificate, authResult.BindingCertificate);
        }

        [TestMethod]
        public void AfterCredentialEvaluation_NotCalled_BearerDefaults()
        {
            // Arrange
            var fakeOp = new FakeAuthOp3();
            var authResult = new AuthenticationResult();

            // Act
            fakeOp.FormatResult(authResult);

            // Assert
            Assert.AreEqual("Bearer", fakeOp.AccessTokenType);
            Assert.AreEqual("Bearer", fakeOp.AuthorizationHeaderPrefix);
            Assert.IsFalse(fakeOp.GetTokenRequestParams().ContainsKey("token_type"));
            Assert.IsNull(authResult.BindingCertificate);
        }

        [TestMethod]
        public void TokenAcquisitionContext_ExposesExpectedProperties()
        {
            // Arrange & Act
            var context = new TokenAcquisitionContext
            {
                MtlsCertificate = s_testCertificate
            };

            // Assert
            Assert.AreSame(s_testCertificate, context.MtlsCertificate);
        }

        #endregion

        #region Cert rotation

        [TestMethod]
        public void AfterCredentialEvaluationAsync_CertRotation_OperationSeesNewCert()
        {
            // Arrange
            var fakeOp = new FakeAuthOp3();
            var cert1 = CertHelper.GetOrCreateTestCert();
            var cert2 = CertHelper.GetOrCreateTestCert(regenerateCert: true);

            // Act — call 1 with cert1
            fakeOp.AfterCredentialEvaluationAsync_Sync(new TokenAcquisitionContext { MtlsCertificate = cert1 });
            var observed1 = fakeOp.LastCertificate.Thumbprint;

            // Act — rotate to cert2
            fakeOp.AfterCredentialEvaluationAsync_Sync(new TokenAcquisitionContext { MtlsCertificate = cert2 });
            var observed2 = fakeOp.LastCertificate.Thumbprint;

            // Assert — operation must see the rotated cert
            Assert.AreNotEqual(observed1, observed2, "After rotation, callback must provide the new cert");
            Assert.AreEqual(cert2.Thumbprint, observed2);
        }

        #endregion

        #region Exception propagation

        [TestMethod]
        public async Task AfterCredentialEvaluationAsync_Throws_PropagatesDirectlyToExecuteAsync()
        {
            // Arrange
            const string region = "eastus";

            using (var envContext = new EnvVariableContext())
            {
                Environment.SetEnvironmentVariable("REGION_NAME", region);

                using (var httpManager = new MockHttpManager())
                {
                    // No mock handler — exception fires before any HTTP call
                    var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                        .WithCertificate(s_testCertificate)
                        .WithAuthority($"https://login.microsoftonline.com/123456-1234-2345-1234561234")
                        .WithAzureRegion(ConfidentialClientApplication.AttemptRegionDiscovery)
                        .WithHttpManager(httpManager)
                        .BuildConcrete();

                    var throwingOp = new ThrowingAuthOp3();

                    // Act & Assert — exception propagates unwrapped to ExecuteAsync
                    var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                        await app.AcquireTokenForClient(TestConstants.s_scope)
                            .WithMtlsProofOfPossession()
                            .WithAuthenticationExtension(new MsalAuthenticationExtension
                            {
                                AuthenticationOperation = throwingOp
                            })
                            .ExecuteAsync()
                            .ConfigureAwait(false))
                        .ConfigureAwait(false);

                    Assert.AreEqual("Test: cert fetch failed", ex.Message);
                }
            }
        }

        #endregion

        #region Test Fakes

        /// <summary>
        /// Fake implementation of IAuthenticationOperation3 that tracks
        /// AfterCredentialEvaluation invocations and adapts behavior based on cert.
        /// </summary>
        private sealed class FakeAuthOp3 : IAuthenticationOperation3
        {
            public int CallbackInvocationCount { get; private set; }
            public X509Certificate2 LastCertificate { get; private set; }

            public void AfterCredentialEvaluationAsync_Sync(TokenAcquisitionContext context)
            {
                CallbackInvocationCount++;
                LastCertificate = context.MtlsCertificate;
            }

            public Task AfterCredentialEvaluationAsync(TokenAcquisitionContext context, System.Threading.CancellationToken cancellationToken = default)
            {
                AfterCredentialEvaluationAsync_Sync(context);
                return Task.CompletedTask;
            }

            public string AccessTokenType => LastCertificate is not null ? "mtls_pop" : "Bearer";
            public string AuthorizationHeaderPrefix => LastCertificate is not null ? "mtls_pop" : "Bearer";
            // KeyId is hardcoded — this test validates callback invocation, not cache partitioning.
            // Real implementations should derive KeyId from the cert (see CdtCryptoProvider.KeyId).
            public string KeyId => "test-key-id";
            public int TelemetryTokenType => 4;

            public IReadOnlyDictionary<string, string> GetTokenRequestParams()
            {
                var dict = new Dictionary<string, string>
                {
                    { "req_ds_cnf", "test-cnf-value" }
                };

                if (LastCertificate is not null)
                {
                    dict["token_type"] = "mtls_pop";
                }

                return dict;
            }

            public void FormatResult(AuthenticationResult authenticationResult)
            {
                if (LastCertificate is not null)
                {
                    authenticationResult.BindingCertificate = LastCertificate;
                }
            }

            public Task FormatResultAsync(AuthenticationResult authenticationResult, System.Threading.CancellationToken cancellationToken = default)
            {
                FormatResult(authenticationResult);
                return Task.CompletedTask;
            }

            public Task<bool> ValidateCachedTokenAsync(MsalCacheValidationData cachedTokenData)
            {
                return Task.FromResult(true);
            }
        }

        /// <summary>
        /// Fake that throws from AfterCredentialEvaluationAsync to test exception propagation.
        /// </summary>
        private sealed class ThrowingAuthOp3 : IAuthenticationOperation3
        {
            public Task AfterCredentialEvaluationAsync(TokenAcquisitionContext context, System.Threading.CancellationToken cancellationToken = default)
            {
                throw new InvalidOperationException("Test: cert fetch failed");
            }

            public string AccessTokenType => "mtls_pop";
            public string AuthorizationHeaderPrefix => "mtls_pop";
            public string KeyId => "test-key-id";
            public int TelemetryTokenType => 4;
            public IReadOnlyDictionary<string, string> GetTokenRequestParams() => new Dictionary<string, string>();
            public void FormatResult(AuthenticationResult authenticationResult) { }
            public Task FormatResultAsync(AuthenticationResult authenticationResult, System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task<bool> ValidateCachedTokenAsync(MsalCacheValidationData cachedTokenData) => Task.FromResult(true);
        }

        #endregion

    }
}

