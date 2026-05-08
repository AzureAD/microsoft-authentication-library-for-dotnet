// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig.Parameters;
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

        #region IAuthenticationOperation3 Integration Tests

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

                    var mockOp3 = new MockAuthenticationOperation3();

                    // Act — chain WithMtlsProofOfPossession + WithAuthenticationExtension (simulates CDT)
                    AuthenticationResult result = await app.AcquireTokenForClient(TestConstants.s_scope)
                        .WithMtlsProofOfPossession()
                        .WithAuthenticationExtension(new MsalAuthenticationExtension
                        {
                            AuthenticationOperation = mockOp3
                        })
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    // Assert — cert was injected, operation was NOT replaced
                    Assert.IsNotNull(mockOp3.InjectedCertificate,
                        "MtlsPopParametersInitializer should inject cert via IAuthenticationOperation3");
                    Assert.AreEqual(s_testCertificate.Thumbprint, mockOp3.InjectedCertificate.Thumbprint);
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

        #endregion

        #region IAuthenticationOperation3 Unit Tests

        [TestMethod]
        public void IAuthenticationOperation3_AccessTokenType_AdaptsBasedOnCert()
        {
            // Arrange
            var mockOp = new MockAuthenticationOperation3();

            // Act & Assert — before cert injection
            Assert.AreEqual("Bearer", mockOp.AccessTokenType,
                "Without cert, AccessTokenType should be Bearer");

            // Act — inject cert
            mockOp.MtlsCertificate = s_testCertificate;

            // Assert — after cert injection
            Assert.AreEqual("mtls_pop", mockOp.AccessTokenType,
                "With cert, AccessTokenType should be mtls_pop");
        }

        [TestMethod]
        public void IAuthenticationOperation3_GetTokenRequestParams_IncludesTokenTypeWhenCertPresent()
        {
            // Arrange
            var mockOp = new MockAuthenticationOperation3();

            // Act & Assert — before cert injection
            var paramsBefore = mockOp.GetTokenRequestParams();
            Assert.IsFalse(paramsBefore.ContainsKey("token_type"),
                "Without cert, token_type should not be in params");

            // Act — inject cert
            mockOp.MtlsCertificate = s_testCertificate;
            var paramsAfter = mockOp.GetTokenRequestParams();

            // Assert — after cert injection
            Assert.IsTrue(paramsAfter.ContainsKey("token_type"));
            Assert.AreEqual("mtls_pop", paramsAfter["token_type"]);
        }

        [TestMethod]
        public void IAuthenticationOperation3_FormatResult_SetsBindingCertificate()
        {
            // Arrange
            var mockOp = new MockAuthenticationOperation3();
            mockOp.MtlsCertificate = s_testCertificate;
            var authResult = new AuthenticationResult();

            // Act
            mockOp.FormatResult(authResult);

            // Assert
            Assert.AreSame(s_testCertificate, authResult.BindingCertificate,
                "FormatResult should set BindingCertificate when mTLS cert is injected");
        }

        [TestMethod]
        public void IAuthenticationOperation3_FormatResult_NoBindingCertWithoutMtls()
        {
            // Arrange
            var mockOp = new MockAuthenticationOperation3();
            var authResult = new AuthenticationResult();

            // Act
            mockOp.FormatResult(authResult);

            // Assert
            Assert.IsNull(authResult.BindingCertificate,
                "FormatResult should NOT set BindingCertificate when no mTLS cert");
        }

        [TestMethod]
        public void IAuthenticationOperation3_AuthorizationHeaderPrefix_AdaptsBasedOnCert()
        {
            // Arrange
            var mockOp = new MockAuthenticationOperation3();

            // Assert — before cert
            Assert.AreEqual("Bearer", mockOp.AuthorizationHeaderPrefix);

            // Act
            mockOp.MtlsCertificate = s_testCertificate;

            // Assert — after cert
            Assert.AreEqual("mtls_pop", mockOp.AuthorizationHeaderPrefix);
        }

        #endregion

        #region Mock IAuthenticationOperation3

        /// <summary>
        /// Mock implementation of IAuthenticationOperation3 that simulates
        /// CDT's behavior of adapting to mTLS POP when cert is injected.
        /// </summary>
        private class MockAuthenticationOperation3 : IAuthenticationOperation3
        {
            public X509Certificate2 InjectedCertificate { get; private set; }

            public X509Certificate2 MtlsCertificate
            {
                set => InjectedCertificate = value;
            }

            public string AccessTokenType => InjectedCertificate is not null ? "mtls_pop" : "Bearer";

            public string AuthorizationHeaderPrefix => InjectedCertificate is not null ? "mtls_pop" : "Bearer";

            public string KeyId => "test-key-id";

            public int TelemetryTokenType => 4;

            public IReadOnlyDictionary<string, string> GetTokenRequestParams()
            {
                var dict = new Dictionary<string, string>
                {
                    { "req_ds_cnf", "test-cnf-value" }
                };

                if (InjectedCertificate is not null)
                {
                    dict["token_type"] = "mtls_pop";
                }

                return dict;
            }

            public void FormatResult(AuthenticationResult authenticationResult)
            {
                if (InjectedCertificate is not null)
                {
                    authenticationResult.BindingCertificate = InjectedCertificate;
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

        #endregion
    }
}
