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
using Microsoft.Identity.Client.Utils;
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
    /// set to <c>true</c>, MSAL routes user-flow token requests to the mTLS endpoint
    /// (<c>mtlsauth.microsoft.com</c>) and omits <c>client_assertion</c> from the POST body —
    /// the same behaviour already implemented for <c>AcquireTokenForClient</c>.
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
        ///   2. Does NOT include <c>client_assertion</c> in the POST body.
        /// </summary>
        [TestMethod]
        public async Task OboFlow_WithSendCertificateOverMtls_UsesGlobalMtlsEndpointAndNoClientAssertionAsync()
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
                        },
                        UnExpectedPostData = new Dictionary<string, string>
                        {
                            { OAuth2Parameter.ClientAssertionType, OAuth2AssertionType.JwtBearer },
                            { OAuth2Parameter.ClientAssertion, "placeholder" }
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
        /// region configured uses the regional mTLS endpoint (e.g. eastus.mtlsauth.microsoft.com).
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
                        UnExpectedPostData = new Dictionary<string, string>
                        {
                            { OAuth2Parameter.ClientAssertionType, OAuth2AssertionType.JwtBearer },
                            { OAuth2Parameter.ClientAssertion, "placeholder" }
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
        ///   2. Does NOT include <c>client_assertion</c> in the POST body.
        /// </summary>
        [TestMethod]
        public async Task RefreshTokenFlow_WithSendCertificateOverMtls_UsesGlobalMtlsEndpointAndNoClientAssertionAsync()
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
                        },
                        UnExpectedPostData = new Dictionary<string, string>
                        {
                            { OAuth2Parameter.ClientAssertionType, OAuth2AssertionType.JwtBearer },
                            { OAuth2Parameter.ClientAssertion, "placeholder" }
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

        /// <summary>
        /// Regression test: verifies that a second OBO call with the same user assertion
        /// retrieves the token from cache (not the network) when <c>SendCertificateOverMtls = true</c>.
        ///
        /// After <c>ResolveAuthorityAsync</c>, <c>requestParams.AuthorityInfo</c> points to
        /// <c>mtlsauth.microsoft.com</c>. The cache alias lookup in <c>FilterTokensByEnvironmentAsync</c>
        /// must still resolve aliases from the original <c>login.*</c> host so the cached token
        /// (stored under <c>login.microsoftonline.com</c>) is found.
        ///
        /// If the fix is missing, the second call will either throw
        /// <c>MsalClientException(MtlsPopNotSupportedForEnvironment)</c> or miss the cache and
        /// fail because no mock HTTP handler is queued for a second network call.
        /// </summary>
        [TestMethod]
        public async Task OboFlow_WithSendCertificateOverMtls_SecondCall_ReturnsCachedTokenAsync()
        {
            // Arrange
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
                    // Only ONE mock handler — the second call must come from cache
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
                        },
                        UnExpectedPostData = new Dictionary<string, string>
                        {
                            { OAuth2Parameter.ClientAssertionType, OAuth2AssertionType.JwtBearer },
                            { OAuth2Parameter.ClientAssertion, "placeholder" }
                        }
                    };

                    harness.HttpManager.AddMockHandler(tokenHttpCallHandler);

                    var app = ConfidentialClientApplicationBuilder
                        .Create(TestConstants.ClientId)
                        .WithAuthority(authorityUrl)
                        .WithHttpManager(harness.HttpManager)
                        .WithCertificate(s_testCertificate, new CertificateOptions { SendCertificateOverMtls = true })
                        .Build();

                    // Act — first call hits the (mocked) identity provider
                    var firstResult = await app
                        .AcquireTokenOnBehalfOf(TestConstants.s_scope, new UserAssertion(fakeUserAssertion))
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    Assert.IsNotNull(firstResult.AccessToken);
                    Assert.AreEqual(TokenSource.IdentityProvider, firstResult.AuthenticationResultMetadata.TokenSource);

                    // Act — second call with same assertion should return from cache
                    var secondResult = await app
                        .AcquireTokenOnBehalfOf(TestConstants.s_scope, new UserAssertion(fakeUserAssertion))
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    // Assert
                    Assert.IsNotNull(secondResult.AccessToken);
                    Assert.AreEqual(TokenSource.Cache, secondResult.AuthenticationResultMetadata.TokenSource,
                        "Second OBO call with same assertion should return a cached token, not hit the network. " +
                        "If this fails, FilterTokensByEnvironmentAsync is likely passing the mTLS-rewritten authority " +
                        "(mtlsauth.microsoft.com) to instance discovery, which either throws or returns incorrect aliases.");
                }
            }
        }
    }
}
