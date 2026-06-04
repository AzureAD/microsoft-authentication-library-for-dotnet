// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit
{
    /// <summary>
    /// Unit tests for mTLS bearer transport applied to user flows (OBO and refresh_token).
    ///
    /// These tests verify that when <see cref="CertificateOptions.SendCertificateOverMtls"/> is
    /// set to <c>true</c>, MSAL routes token requests to the mTLS endpoint
    /// (<c>mtlsauth.microsoft.com</c>) and includes <c>client_assertion</c> in the POST body
    /// for all flows — the cert authenticates at the TLS layer AND the body carries the assertion.
    /// This applies to all flows: S2S, OBO, refresh_token, and auth_code.
    /// </summary>
    [TestClass]
    public class MtlsBearerUserFlowTests : TestBase
    {
        private static X509Certificate2 s_testCertificate;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            s_testCertificate = CertHelper.GetOrCreateTestCert();

            if (s_testCertificate == null || string.IsNullOrEmpty(s_testCertificate.Thumbprint))
            {
                throw new InvalidOperationException("Failed to initialize a valid test certificate.");
            }
        }

        /// <summary>
        /// Verifies that an OBO token request with <c>SendCertificateOverMtls = true</c>:
        ///   1. Targets the global mTLS endpoint (mtlsauth.microsoft.com).
        ///   2. Includes <c>client_assertion</c> in the POST body (cert at TLS layer + assertion in body).
        /// </summary>
        [TestMethod]
        public async Task OboFlow_WithSendCertificateOverMtls_UsesGlobalMtlsEndpointAsync()
        {
            string tenantId = "123456-1234-2345-1234561234";
            string authorityUrl = $"https://login.microsoftonline.com/{tenantId}";
            string expectedTokenEndpoint = $"https://mtlsauth.microsoft.com/{tenantId}/oauth2/v2.0/token";
            string fakeUserAssertion = "fake.user.assertion.token";

            using (var envContext = new EnvVariableContext())
            {
                Environment.SetEnvironmentVariable("REGION_NAME", null);
                Environment.SetEnvironmentVariable("MSAL_FORCE_REGION", null);

                using (var harness = new MockHttpAndServiceBundle())
                {
                    var tokenHttpCallHandler = new MockHttpMessageHandler()
                    {
                        ExpectedUrl = expectedTokenEndpoint,
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                        ExpectedPostData = new Dictionary<string, string>
                        {
                            { OAuth2Parameter.ClientId, TestConstants.ClientId },
                            { OAuth2Parameter.GrantType, OAuth2GrantType.JwtBearer },
                            { OAuth2Parameter.RequestedTokenUse, OAuth2RequestedTokenUse.OnBehalfOf },
                            { OAuth2Parameter.ClientAssertionType, OAuth2AssertionType.JwtBearer },
                        },
                        // client_assertion value is a signed JWT — assert presence only, not value
                        AdditionalRequestValidation = req =>
                        {
                            string body = req.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                            StringAssert.Contains(body, "client_assertion=",
                                "client_assertion must be present in the OBO POST body.");
                        }
                    };

                    harness.HttpManager.AddMockHandler(tokenHttpCallHandler);

                    var app = ConfidentialClientApplicationBuilder
                        .Create(TestConstants.ClientId)
                        .WithAuthority(authorityUrl)
                        .WithHttpManager(harness.HttpManager)
                        .WithCertificate(s_testCertificate, new CertificateOptions { SendCertificateOverMtls = true })
                        .Build();

                    // Act
                    var result = await app
                        .AcquireTokenOnBehalfOf(TestConstants.s_scope, new UserAssertion(fakeUserAssertion))
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    // Assert
                    Assert.IsNotNull(result.AccessToken);
                    Assert.AreEqual(expectedTokenEndpoint, result.AuthenticationResultMetadata.TokenEndpoint);
                }
            }
        }

        /// <summary>
        /// Verifies that a user flow token request with <c>SendCertificateOverMtls = true</c> and a
        /// region configured uses the regional mTLS endpoint (e.g. eastus.mtlsauth.microsoft.com)
        /// and includes <c>client_assertion</c> in the POST body.
        ///
        /// OBO is used as the representative user flow here. The regional routing code
        /// (<c>RegionAndMtlsDiscoveryProvider</c>) is shared across all user flows (OBO, refresh_token,
        /// auth_code), so a single general-purpose test is sufficient to verify the routing logic.
        /// </summary>
        [TestMethod]
        public async Task UserFlow_WithSendCertificateOverMtls_WithRegion_UsesRegionalMtlsEndpointAsync()
        {
            string tenantId = "123456-1234-2345-1234561234";
            string authorityUrl = $"https://login.microsoftonline.com/{tenantId}";
            const string region = "eastus";
            string expectedTokenEndpoint = $"https://{region}.mtlsauth.microsoft.com/{tenantId}/oauth2/v2.0/token";
            string fakeUserAssertion = "fake.user.assertion.token";

            using (var envContext = new EnvVariableContext())
            {
                Environment.SetEnvironmentVariable("REGION_NAME", region);
                Environment.SetEnvironmentVariable("MSAL_FORCE_REGION", null);

                using (var harness = new MockHttpAndServiceBundle())
                {
                    var tokenHttpCallHandler = new MockHttpMessageHandler()
                    {
                        ExpectedUrl = expectedTokenEndpoint,
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                        ExpectedPostData = new Dictionary<string, string>
                        {
                            { OAuth2Parameter.ClientId, TestConstants.ClientId },
                            { OAuth2Parameter.GrantType, OAuth2GrantType.JwtBearer },
                            { OAuth2Parameter.RequestedTokenUse, OAuth2RequestedTokenUse.OnBehalfOf },
                            { OAuth2Parameter.ClientAssertionType, OAuth2AssertionType.JwtBearer },
                        },
                        // client_assertion value is a signed JWT — assert presence only, not value
                        AdditionalRequestValidation = req =>
                        {
                            string body = req.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                            StringAssert.Contains(body, "client_assertion=",
                                "client_assertion must be present in the regional OBO POST body.");
                        }
                    };

                    harness.HttpManager.AddMockHandler(tokenHttpCallHandler);

                    var app = ConfidentialClientApplicationBuilder
                        .Create(TestConstants.ClientId)
                        .WithAuthority(authorityUrl)
                        .WithHttpManager(harness.HttpManager)
                        .WithAzureRegion(ConfidentialClientApplication.AttemptRegionDiscovery)
                        .WithCertificate(s_testCertificate, new CertificateOptions { SendCertificateOverMtls = true })
                        .Build();

                    // Act
                    var result = await app
                        .AcquireTokenOnBehalfOf(TestConstants.s_scope, new UserAssertion(fakeUserAssertion))
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    // Assert
                    Assert.IsNotNull(result.AccessToken);
                    Assert.AreEqual(expectedTokenEndpoint, result.AuthenticationResultMetadata.TokenEndpoint);
                }
            }
        }

        /// <summary>
        /// Verifies that a refresh-token redemption (<c>IByRefreshToken</c>) with
        /// <c>SendCertificateOverMtls = true</c>:
        ///   1. Targets the global mTLS endpoint.
        ///   2. Includes <c>client_assertion</c> in the POST body (cert at TLS layer + assertion in body).
        /// </summary>
        [TestMethod]
        public async Task RefreshTokenFlow_WithSendCertificateOverMtls_UsesGlobalMtlsEndpointAsync()
        {
            string tenantId = "123456-1234-2345-1234561234";
            string authorityUrl = $"https://login.microsoftonline.com/{tenantId}";
            string expectedTokenEndpoint = $"https://mtlsauth.microsoft.com/{tenantId}/oauth2/v2.0/token";
            const string fakeRefreshToken = "my_test_refresh_token";

            using (var envContext = new EnvVariableContext())
            {
                Environment.SetEnvironmentVariable("REGION_NAME", null);
                Environment.SetEnvironmentVariable("MSAL_FORCE_REGION", null);

                using (var harness = new MockHttpAndServiceBundle())
                {
                    var tokenHttpCallHandler = new MockHttpMessageHandler()
                    {
                        ExpectedUrl = expectedTokenEndpoint,
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                        ExpectedPostData = new Dictionary<string, string>
                        {
                            { OAuth2Parameter.ClientId, TestConstants.ClientId },
                            { OAuth2Parameter.GrantType, OAuth2GrantType.RefreshToken },
                            { OAuth2Parameter.RefreshToken, fakeRefreshToken },
                            { OAuth2Parameter.ClientAssertionType, OAuth2AssertionType.JwtBearer },
                        },
                        // client_assertion value is a signed JWT — assert presence only, not value
                        AdditionalRequestValidation = req =>
                        {
                            string body = req.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                            StringAssert.Contains(body, "client_assertion=",
                                "client_assertion must be present in the RT POST body.");
                        }
                    };

                    harness.HttpManager.AddMockHandler(tokenHttpCallHandler);

                    var app = ConfidentialClientApplicationBuilder
                        .Create(TestConstants.ClientId)
                        .WithAuthority(authorityUrl)
                        .WithHttpManager(harness.HttpManager)
                        .WithCertificate(s_testCertificate, new CertificateOptions { SendCertificateOverMtls = true })
                        .Build();

                    // Act
                    var result = await ((IByRefreshToken)app)
                        .AcquireTokenByRefreshToken(TestConstants.s_scope, fakeRefreshToken)
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    // Assert
                    Assert.IsNotNull(result.AccessToken);
                    Assert.AreEqual(expectedTokenEndpoint, result.AuthenticationResultMetadata.TokenEndpoint);
                }
            }
        }

        /// <summary>
        /// Regression test for Bug #1: a second <c>AcquireTokenOnBehalfOf</c> call on the same app
        /// instance must NOT throw <c>MsalClientException(MtlsPopNotSupportedForEnvironment)</c>.
        ///
        /// Root cause: after the first call, the AT cache is populated with an entry whose
        /// Environment is <c>mtlsauth.microsoft.com</c>. On the second call, <c>FilterTokensByEnvironmentAsync</c>
        /// passes <c>requestParams.AuthorityInfo</c> (which is <c>mtlsauth.microsoft.com</c> after authority
        /// resolution) to <c>GetMetadataEntryTryAvoidNetworkAsync</c>, which throws because
        /// <c>RegionAndMtlsDiscoveryProvider</c> only accepts <c>login.*</c> hosts.
        /// The fix uses <c>requestParams.AuthorityManager.OriginalAuthority.AuthorityInfo</c> instead.
        /// </summary>
        [TestMethod]
        public async Task OboFlow_WithSendCertificateOverMtls_SecondCallDoesNotCrashAsync()
        {
            string tenantId = "123456-1234-2345-1234561234";
            string authorityUrl = $"https://login.microsoftonline.com/{tenantId}";
            string expectedTokenEndpoint = $"https://mtlsauth.microsoft.com/{tenantId}/oauth2/v2.0/token";
            string fakeUserAssertion = "fake.user.assertion.token";

            using (var envContext = new EnvVariableContext())
            {
                Environment.SetEnvironmentVariable("REGION_NAME", null);
                Environment.SetEnvironmentVariable("MSAL_FORCE_REGION", null);

                using (var harness = new MockHttpAndServiceBundle())
                {
                    // Only one network response — the second call must be served from cache.
                    harness.HttpManager.AddMockHandler(new MockHttpMessageHandler()
                    {
                        ExpectedUrl = expectedTokenEndpoint,
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                    });

                    var app = ConfidentialClientApplicationBuilder
                        .Create(TestConstants.ClientId)
                        .WithAuthority(authorityUrl)
                        .WithHttpManager(harness.HttpManager)
                        .WithCertificate(s_testCertificate, new CertificateOptions { SendCertificateOverMtls = true })
                        .Build();

                    var assertion = new UserAssertion(fakeUserAssertion);

                    // First call — hits network, populates cache.
                    var result1 = await app
                        .AcquireTokenOnBehalfOf(TestConstants.s_scope, assertion)
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    Assert.IsNotNull(result1.AccessToken);

                    // Second call — must not throw MsalClientException(MtlsPopNotSupportedForEnvironment).
                    // Should be served from cache without a network call.
                    var result2 = await app
                        .AcquireTokenOnBehalfOf(TestConstants.s_scope, assertion)
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    Assert.IsNotNull(result2.AccessToken);
                    Assert.AreEqual(result1.AccessToken, result2.AccessToken, "Second call should return the cached token.");
                }
            }
        }

        /// <summary>
        /// Regression test: without <c>SendCertificateOverMtls</c>, a cert-credential OBO request
        /// still uses the regular (non-mTLS) endpoint and sends <c>client_assertion</c>.
        /// </summary>
        [TestMethod]
        public async Task OboFlow_WithoutSendCertificateOverMtls_UsesRegularEndpointWithClientAssertionAsync()
        {
            string tenantId = "123456-1234-2345-1234561234";
            string authorityUrl = $"https://login.microsoftonline.com/{tenantId}";
            string expectedTokenEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";
            string fakeUserAssertion = "fake.user.assertion.token";

            using (var envContext = new EnvVariableContext())
            {
                Environment.SetEnvironmentVariable("REGION_NAME", null);
                Environment.SetEnvironmentVariable("MSAL_FORCE_REGION", null);

                using (var harness = new MockHttpAndServiceBundle())
                {
                    // Regular endpoint — no mTLS routing
                    var tokenHttpCallHandler = new MockHttpMessageHandler()
                    {
                        ExpectedUrl = expectedTokenEndpoint,
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                        // client_assertion_type MUST be present (indicates cert credential is being serialized as a JWT)
                        ExpectedPostData = new Dictionary<string, string>
                        {
                            { OAuth2Parameter.ClientId, TestConstants.ClientId },
                            { OAuth2Parameter.ClientAssertionType, OAuth2AssertionType.JwtBearer },
                        },
                    };

                    harness.HttpManager.AddMockHandler(tokenHttpCallHandler);

                    var app = ConfidentialClientApplicationBuilder
                        .Create(TestConstants.ClientId)
                        .WithAuthority(authorityUrl)
                        .WithHttpManager(harness.HttpManager)
                        .WithCertificate(s_testCertificate)
                        .WithInstanceDiscovery(false)
                        .Build();

                    // Act
                    var result = await app
                        .AcquireTokenOnBehalfOf(TestConstants.s_scope, new UserAssertion(fakeUserAssertion))
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    // Assert
                    Assert.IsNotNull(result.AccessToken);
                    StringAssert.Contains(result.AuthenticationResultMetadata.TokenEndpoint, "login.microsoftonline.com",
                        "Without SendCertificateOverMtls, OBO should use the regular login endpoint.");
                }
            }
        }
    }
}
