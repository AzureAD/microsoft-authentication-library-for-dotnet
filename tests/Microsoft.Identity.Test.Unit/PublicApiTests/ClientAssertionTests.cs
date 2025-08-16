﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class ClientAssertionTests : TestBase
    {
        private const string AssertionFmiPath1 = "test-client-assertion1";
        private const string AssertionFmiPath2 = "test-client-assertion2";

        [TestMethod]
        public async Task SignedAssertionDelegateClientCredential_NoClaims()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var handler = httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
                handler.ExpectedPostData = new Dictionary<string, string>();

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithHttpManager(httpManager)
                    .WithClientAssertion(async (AssertionRequestOptions options) =>
                    {
                        // Ensure claims are  set when WithClaims is called
                        Assert.IsNull(options.Claims);
                        Assert.IsNull(options.ClientCapabilities);
                        Assert.IsNull(options.ClientAssertionFmiPath);
                        Assert.AreEqual(TestConstants.ClientId, options.ClientID);
                        return await Task.FromResult("dummy_assertion").ConfigureAwait(false);
                    })
                    .BuildConcrete();

                var result = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsFalse(handler.ActualRequestPostData.ContainsKey("claims"));
            }
        }

        [TestMethod]
        public async Task SignedAssertionDelegateClientCredential_WithClaims()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var handler = httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
                handler.ExpectedPostData = new Dictionary<string, string>();

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithHttpManager(httpManager)
                    .WithClientAssertion(async (AssertionRequestOptions options) =>
                    {
                        // Ensure claims are NOT set when WithClaims is not called
                        Assert.IsNull(options.Claims);
                        return await Task.FromResult("dummy_assertion").ConfigureAwait(false);
                    })
                    .BuildConcrete();

                var result = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsFalse(handler.ActualRequestPostData.ContainsKey("claims"));
            }
        }

        [TestMethod]
        public async Task FmiPathClientAssertion()
        {
            // Arrange
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                    expectedPostData: new Dictionary<string, string>() { { OAuth2Parameter.FmiPath, "fmiPath" } });

                string actualAssertionFmiPath = null;
                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(TestConstants.AuthorityTestTenant)
                    .WithExperimentalFeatures(true)
                    .WithHttpManager(httpManager)
                    .WithClientAssertion(async o =>
                    {
                        actualAssertionFmiPath = o.ClientAssertionFmiPath;
                        return await Task.FromResult("dummy_assertion").ConfigureAwait(false);

                    })
                    .Build();

                // Act 1
                actualAssertionFmiPath = null;
                var result = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .WithFmiPathForClientAssertion(AssertionFmiPath1)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(AssertionFmiPath1, actualAssertionFmiPath);

                // Act 2 - request a token, with a different cred fmi path, expect a new token
                actualAssertionFmiPath = null;
                result = await app.AcquireTokenForClient(TestConstants.s_scope)
                   .WithFmiPathForClientAssertion(AssertionFmiPath2)
                   .ExecuteAsync()
                   .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(AssertionFmiPath2, actualAssertionFmiPath);

                // Act 3 - request the token with the same path, expect cached token
                actualAssertionFmiPath = null;
                result = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .WithFmiPathForClientAssertion(AssertionFmiPath2)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                Assert.IsNull(actualAssertionFmiPath);

                // Act 4 - request the token with the same path 2, expect cached token
                actualAssertionFmiPath = null;
                result = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .WithFmiPathForClientAssertion(AssertionFmiPath1)
                    .ExecuteAsync()
                    .ConfigureAwait(false);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                Assert.IsNull(actualAssertionFmiPath);

                // Act 4 - request the token with the same path, and now add FMI path too 
                actualAssertionFmiPath = null;
                result = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .WithFmiPath("fmiPath")
                    .WithFmiPathForClientAssertion(AssertionFmiPath1)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(AssertionFmiPath1, actualAssertionFmiPath);
            }
        }

        [TestMethod]
        public async Task FmiPathClientAssertionObo()
        {
            const string AssertionFmiPath = "test-client-assertion";

            // Arrange
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandler(new MockHttpMessageHandler()
                {
                    ExpectedMethod = HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
                });

                bool verified = false;
                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(TestConstants.AuthorityTestTenant)
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)
                    .WithClientAssertion(async o =>
                    {
                        Assert.AreEqual(AssertionFmiPath, o.ClientAssertionFmiPath);
                        verified = true;
                        return await Task.FromResult("dummy_assertion").ConfigureAwait(false);
                    })
                    .Build();

                var userAssertion = new UserAssertion(TestConstants.DefaultAccessToken);

                // Act
                var result = await app.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion)
                    .WithFmiPathForClientAssertion(AssertionFmiPath)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.IsTrue(verified, "The client assertion delegate should have been called with the correct FMI path.");
            }
        }

        [TestMethod]
        public async Task FmiPathClientAssertionLongRunningObo()
        {
            // Arrange
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandler(new MockHttpMessageHandler()
                {
                    ExpectedMethod = HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
                });
                httpManager.AddMockHandler(new MockHttpMessageHandler()
                {
                    ExpectedMethod = HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
                });

                string actualAssertionFmiPath = null;
                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(TestConstants.AuthorityTestTenant)
                    .WithExperimentalFeatures(true)
                    .WithHttpManager(httpManager)
                  .WithClientAssertion(async o =>
                  {
                      actualAssertionFmiPath = o.ClientAssertionFmiPath;
                      return await Task.FromResult("dummy_assertion").ConfigureAwait(false);

                  })
                    .BuildConcrete();

                string oboCacheKey = "test-obo-cache-key";

                // Act
                actualAssertionFmiPath = null;
                var result = await app.InitiateLongRunningProcessInWebApi(TestConstants.s_scope, TestConstants.DefaultAccessToken, ref oboCacheKey)
                    .WithFmiPathForClientAssertion(AssertionFmiPath1)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(AssertionFmiPath1, actualAssertionFmiPath);

                // Act 2 - different path
                actualAssertionFmiPath = null;
                result = await app.InitiateLongRunningProcessInWebApi(TestConstants.s_scope, TestConstants.DefaultAccessToken, ref oboCacheKey)
                 .WithFmiPathForClientAssertion(AssertionFmiPath2)
                 .ExecuteAsync()
                 .ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(AssertionFmiPath2, actualAssertionFmiPath);
            }
        }

        [TestMethod]
        public async Task LongRunningObo_RunsSuccessfully_TestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandler(new MockHttpMessageHandler()
                {
                    ExpectedMethod = HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
                });

                bool verified = false;

                var cca = ConfidentialClientApplicationBuilder
                 .Create(TestConstants.ClientId)
                 .WithAuthority(TestConstants.AuthorityTestTenant)
                 .WithHttpManager(httpManager)
                 .WithExperimentalFeatures(true)
                 .WithClientAssertion(async o =>
                 {
                     Assert.AreEqual(AssertionFmiPath1, o.ClientAssertionFmiPath);
                     verified = true;
                     return await Task.FromResult("dummy_assertion").ConfigureAwait(false);
                 })
                 .BuildConcrete();

                string oboCacheKey = "obo-cache-key";
                var result = await cca.InitiateLongRunningProcessInWebApi(
                    TestConstants.s_scope,
                    TestConstants.DefaultAccessToken,
                    ref oboCacheKey)
                    .WithFmiPathForClientAssertion(AssertionFmiPath1)
                    .ExecuteAsync().ConfigureAwait(false);

                // Token's not in cache, searched by user assertion hash, retrieved from IdP, saved with the provided OBO cache key
                Assert.AreEqual(TestConstants.ATSecret, result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.IsTrue(verified, "The client assertion delegate should have been called with the correct FMI path.");

                verified = false;
                result = await cca.AcquireTokenInLongRunningProcess(TestConstants.s_scope, oboCacheKey)
                    .WithFmiPathForClientAssertion(AssertionFmiPath1)
                    .ExecuteAsync().ConfigureAwait(false);

                // Token is in the cache, retrieved by the provided OBO cache key
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                Assert.IsFalse(verified, "The client assertion delegate should not have been called.");

                TokenCacheHelper.ExpireAllAccessTokens(cca.UserTokenCacheInternal);
                httpManager.AddMockHandler(new MockHttpMessageHandler()
                {
                    ExpectedMethod = HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
                });
                verified = false;

                result = await cca.AcquireTokenInLongRunningProcess(TestConstants.s_scope, oboCacheKey)
                    .WithFmiPathForClientAssertion(AssertionFmiPath1)
                    .ExecuteAsync().ConfigureAwait(false);

                // Cached AT is expired, RT used to retrieve new AT
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.IsTrue(verified, "The client assertion delegate should have been called with the correct FMI path.");
            }
        }

        [TestMethod]
        public async Task ClientAssertion_BearerAsync()
        {
            using var http = new MockHttpManager();
            http.AddInstanceDiscoveryMockHandler();

            var handler = http.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
            var cca = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                       .WithClientSecret(TestConstants.ClientSecret)
                       .WithHttpManager(http)
                       .WithClientAssertion(BearerDelegate())
                       .BuildConcrete();

            var result = await cca.AcquireTokenForClient(TestConstants.s_scope)
                                  .ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

            Assert.AreEqual(
                "urn:ietf:params:oauth:client-assertion-type:jwt-bearer",
                handler.ActualRequestPostData["client_assertion_type"]);

            result = await cca.AcquireTokenForClient(TestConstants.s_scope)
                                  .ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
        }

        [TestMethod]
        public async Task ClientAssertion_WithPoPDelegate_No_Mtls_Api_SendsBearer_Async()
        {
            using var http = new MockHttpManager();
            {
                http.AddInstanceDiscoveryMockHandler();
                var handler = http.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
                var cca = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                           .WithClientSecret(TestConstants.ClientSecret)
                           .WithHttpManager(http)
                           .WithClientAssertion(PopDelegate())
                           .BuildConcrete();

                var result = await cca.AcquireTokenForClient(TestConstants.s_scope)
                                      .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                Assert.AreEqual(
                    "urn:ietf:params:oauth:client-assertion-type:jwt-bearer",
                    handler.ActualRequestPostData["client_assertion_type"]);
            }
        }

        [TestMethod]
        public async Task ClientAssertion_ReceivesClientCapabilitiesAsync()
        {
            using var http = new MockHttpManager();
            {
                http.AddInstanceDiscoveryMockHandler();
                http.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                bool checkedCaps = false;
                var cca = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                          .WithClientSecret(TestConstants.ClientSecret)
                          .WithClientCapabilities(TestConstants.ClientCapabilities)
                          .WithHttpManager(http)
                          .WithClientAssertion((opts, ct) =>
                          {
                              checkedCaps = true;
                              CollectionAssert.AreEqual(
                                  TestConstants.ClientCapabilities,
                                  opts.ClientCapabilities.ToList());
                              return Task.FromResult(new ClientSignedAssertion
                              {
                                  Assertion = "jwt"
                              });
                          })
                          .BuildConcrete();

                _ = await cca.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsTrue(checkedCaps);
            }
        }

        [TestMethod]
        public async Task ClientAssertion_EmptyJwt_ThrowsAsync()
        {
            var cca = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                      .WithClientSecret(TestConstants.ClientSecret)
                      .WithClientAssertion((o, c) =>
                          Task.FromResult(new ClientSignedAssertion { Assertion = string.Empty }))
                      .BuildConcrete();

            await AssertException.TaskThrowsAsync<MsalClientException>(() =>
                cca.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync())
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ClientAssertion_CancellationTokenPropagatesAsync()
        {
            using var cts = new CancellationTokenSource();

            var cca = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                      .WithClientSecret(TestConstants.ClientSecret)
                      .WithClientAssertion((o, ct) =>
                      {
                          Assert.AreEqual(cts.Token, ct);
                          cts.Cancel();
                          ct.ThrowIfCancellationRequested();
                          return Task.FromResult(new ClientSignedAssertion { Assertion = "jwt" });
                      })
                      .BuildConcrete();

            await AssertException.TaskThrowsAsync<OperationCanceledException>(() =>
                cca.AcquireTokenForClient(TestConstants.s_scope)
                   .ExecuteAsync(cts.Token))
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task WithMtlsPop_AfterPoPDelegate_Works()
        {
            const string region = "eastus";

            using (var envContext = new EnvVariableContext())
            {
                Environment.SetEnvironmentVariable("REGION_NAME", region);

                // Set the expected mTLS endpoint for public cloud
                string globalEndpoint = "mtlsauth.microsoft.com";
                string expectedTokenEndpoint = $"https://{region}.{globalEndpoint}/123456-1234-2345-1234561234/oauth2/v2.0/token";

                using (var httpManager = new MockHttpManager())
                {
                    // Set up mock handler with expected token endpoint URL
                    httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                        tokenType: "mtls_pop");

                    var cert = CertHelper.GetOrCreateTestCert();

                    var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                        .WithClientAssertion(PopDelegate())
                        .WithAuthority($"https://login.microsoftonline.com/123456-1234-2345-1234561234")
                        .WithAzureRegion(ConfidentialClientApplication.AttemptRegionDiscovery)
                        .WithHttpManager(httpManager)
                        .BuildConcrete();

                    // First token acquisition - should hit the identity provider
                    AuthenticationResult result = await app.AcquireTokenForClient(TestConstants.s_scope)
                        .WithMtlsProofOfPossession()
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    Assert.AreEqual("header.payload.signature", result.AccessToken);
                    Assert.AreEqual(Constants.MtlsPoPAuthHeaderPrefix, result.TokenType);
                    Assert.AreEqual(region, result.AuthenticationResultMetadata.RegionDetails.RegionUsed);
                    Assert.AreEqual(expectedTokenEndpoint, result.AuthenticationResultMetadata.TokenEndpoint);

                    Assert.IsNotNull(result.BindingCertificate, "BindingCertificate should be present.");
                    Assert.AreEqual(cert.Thumbprint, result.BindingCertificate.Thumbprint,
                        "BindingCertificate must match the cert passed to WithCertificate().");

                    // Second token acquisition - should retrieve from cache
                    AuthenticationResult secondResult = await app.AcquireTokenForClient(TestConstants.s_scope)
                        .WithMtlsProofOfPossession()
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    Assert.AreEqual("header.payload.signature", secondResult.AccessToken);
                    Assert.AreEqual(Constants.MtlsPoPAuthHeaderPrefix, secondResult.TokenType);
                    Assert.AreEqual(TokenSource.Cache, secondResult.AuthenticationResultMetadata.TokenSource);
                    Assert.AreEqual(expectedTokenEndpoint, result.AuthenticationResultMetadata.TokenEndpoint);
                    // Cached result must still carry the cert
                    Assert.IsNotNull(secondResult.BindingCertificate);
                    Assert.AreEqual(result.BindingCertificate.Thumbprint,
                        secondResult.BindingCertificate.Thumbprint);
                }
            }
        }

        [TestMethod]
        public async Task PoP_CachedTokenWithDifferentCertificate_IsBypassedAsync()
        {
            const string region = "eastus";

            // ─────────── Set up HTTP mocks ───────────
            using var httpManager = new MockHttpManager();
            using (var envContext = new EnvVariableContext())
            {
                Environment.SetEnvironmentVariable("REGION_NAME", region);

                // 1st network call returns token‑A
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                            tokenType: "mtls_pop");

                // 2nd network call returns token‑B
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                            tokenType: "mtls_pop");

                // ─────────── Two distinct certificates ───────────
                var certA = CertHelper.GetOrCreateTestCert();
                var certB = CertHelper.GetOrCreateTestCert(regenerateCert: true);

                // Delegate returns certA on first call, certB on second call
                int callCount = 0;
                Func<AssertionRequestOptions, CancellationToken, Task<ClientSignedAssertion>> popDelegate =
                    (opts, ct) =>
                    {
                        callCount++;
                        var cert = (callCount == 1) ? certA : certB;
                        return Task.FromResult(new ClientSignedAssertion
                        {
                            Assertion = $"jwt_{callCount}",      // payload not important for this test
                            TokenBindingCertificate = cert
                        });
                    };

                // ─────────── Build the app ───────────
                var cca = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                           .WithClientSecret(TestConstants.ClientSecret)
                           .WithClientAssertion(popDelegate)
                           .WithAuthority($"https://login.microsoftonline.com/123456-1234-2345-1234561234")
                           .WithAzureRegion(ConfidentialClientApplication.AttemptRegionDiscovery)
                           .WithHttpManager(httpManager)
                           .BuildConcrete();

                // ─────────── First acquire – network call, caches token‑A bound to certA ───────────
                AuthenticationResult first = await cca.AcquireTokenForClient(TestConstants.s_scope)
                    .WithMtlsProofOfPossession()
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, first.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(certA.Thumbprint, first.BindingCertificate.Thumbprint);

                // ─────────── Second acquire – delegate now returns certB ───────────
                AuthenticationResult second = await cca.AcquireTokenForClient(TestConstants.s_scope)
                    .WithMtlsProofOfPossession()
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // The serial number mismatch should have forced a network call, not a cache hit
                Assert.AreEqual(TokenSource.IdentityProvider, second.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(certB.Thumbprint, second.BindingCertificate.Thumbprint);
            }
        }

        [TestMethod]
        public async Task WithMtlsPop_AfterBearerDelegate_Throws()
        {
            var cca = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                      .WithClientSecret(TestConstants.ClientSecret)
                      .WithClientAssertion(BearerDelegate())
                      .BuildConcrete();

            var ex = await AssertException.TaskThrowsAsync<MsalClientException>(() =>
                cca.AcquireTokenForClient(TestConstants.s_scope)
                   .WithMtlsProofOfPossession()
                   .ExecuteAsync())
                .ConfigureAwait(false);

            Assert.AreEqual(MsalError.MtlsCertificateNotProvided, ex.ErrorCode);
        }

        [TestMethod]
        public async Task ClientAssertion_NotCalledWhenTokenFromCacheAsync()
        {
            using var http = new MockHttpManager();
            http.AddInstanceDiscoveryMockHandler();

            int callCount = 0;
            http.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(); // first call => network

            var cca = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                      .WithClientSecret(TestConstants.ClientSecret)
                      .WithHttpManager(http)
                      .WithClientAssertion((o, c) =>
                      {
                          callCount++;
                          return Task.FromResult(new ClientSignedAssertion { Assertion = "jwt" });
                      })
                      .BuildConcrete();

            _ = await cca.AcquireTokenForClient(TestConstants.s_scope)
                .ExecuteAsync()
                .ConfigureAwait(false);
            
            Assert.AreEqual(1, callCount);

            _ = await cca.AcquireTokenForClient(TestConstants.s_scope)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(1, callCount);
        }

        [TestMethod]
        public async Task WithMtlsPop_AfterPoPDelegate_NoRegion_ThrowsAsync()
        {
            using var http = new MockHttpManager();
            {
                // Arrange – CCA with PoP delegate (returns JWT + cert) but **no AzureRegion configured**
                var cert = CertHelper.GetOrCreateTestCert();
                var cca = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                              .WithClientAssertion(PopDelegate())
                              .WithHttpManager(http)
                              .BuildConcrete();

                // Act & Assert – should fail because region is missing
                var ex = await AssertException.TaskThrowsAsync<MsalClientException>(async () =>
                    await cca.AcquireTokenForClient(TestConstants.s_scope)
                             .WithMtlsProofOfPossession()
                             .ExecuteAsync()
                             .ConfigureAwait(false))
                    .ConfigureAwait(false);

                Assert.AreEqual(MsalError.MtlsPopWithoutRegion, ex.ErrorCode);
            }
        }

        #region Helper ---------------------------------------------------------------
        private static Func<AssertionRequestOptions, CancellationToken, Task<ClientSignedAssertion>>
        BearerDelegate(string jwt = "fake_jwt") =>
            (opts, ct) => Task.FromResult(new ClientSignedAssertion
            {
                Assertion = jwt,
                TokenBindingCertificate = null
            });

        private static Func<AssertionRequestOptions, CancellationToken, Task<ClientSignedAssertion>>
        PopDelegate(string jwt = "fake_jwt") =>
            (opts, ct) =>
            {
                // Obtain (or generate) the test certificate once per call
                X509Certificate2 cert = CertHelper.GetOrCreateTestCert();

                return Task.FromResult(new ClientSignedAssertion
                {
                    Assertion = jwt,
                    TokenBindingCertificate = cert
                });
            };
#endregion
    }
}
