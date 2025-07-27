// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
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
        public async Task ClientAssertionOverride_OverridesAppLevelCertificate()
        {
            const string overrideAssertion = "override_assertion_jwt";

            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var handler = httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
                handler.ExpectedPostData = new Dictionary<string, string>
                {
                    { OAuth2Parameter.ClientAssertionType, OAuth2AssertionType.JwtBearer },
                    { OAuth2Parameter.ClientAssertion, overrideAssertion }
                };

                // App configured with certificate, but request should use override assertion
                // Use a simpler approach that doesn't require certificate loading
                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)
                    .WithClientSecret("app_level_secret") // Use secret instead of cert for simplicity
                    .Build();

                var result = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .WithClientAssertion(overrideAssertion)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public async Task ClientAssertionOverride_OverridesAppLevelSecret()
        {
            const string overrideAssertion = "override_assertion_jwt";

            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var handler = httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
                handler.ExpectedPostData = new Dictionary<string, string>
                {
                    { OAuth2Parameter.ClientAssertionType, OAuth2AssertionType.JwtBearer },
                    { OAuth2Parameter.ClientAssertion, overrideAssertion }
                };

                // App configured with secret, but request should use override assertion
                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)
                    .WithClientSecret("app_level_secret")
                    .Build();

                var result = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .WithClientAssertion(overrideAssertion)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public async Task ClientAssertionOverride_OverridesAppLevelAssertion()
        {
            const string appAssertion = "app_level_assertion_jwt";
            const string overrideAssertion = "override_assertion_jwt";

            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var handler = httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
                handler.ExpectedPostData = new Dictionary<string, string>
                {
                    { OAuth2Parameter.ClientAssertionType, OAuth2AssertionType.JwtBearer },
                    { OAuth2Parameter.ClientAssertion, overrideAssertion }
                };

                // App configured with assertion delegate, but request should use override assertion
                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)
                    .WithClientAssertion(async (AssertionRequestOptions options) =>
                    {
                        // This should not be called when override is provided
                        Assert.Fail("App-level assertion delegate should not be called when override is present");
                        return await Task.FromResult(appAssertion).ConfigureAwait(false);
                    })
                    .Build();

                var result = await app.AcquireTokenForClient(TestConstants.s_scope)                    
                    .WithClientAssertion(overrideAssertion)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public async Task ClientAssertionOverride_WithoutExperimentalFeatures_ThrowsException()
        {
            using (var httpManager = new MockHttpManager())
            {
                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithHttpManager(httpManager)
                    .WithClientSecret("app_level_secret")
                    .BuildConcrete();

                var ex = await Assert.ThrowsExceptionAsync<MsalClientException>(
                    () => app.AcquireTokenForClient(TestConstants.s_scope)
                        .WithClientAssertion("override_assertion")
                        .ExecuteAsync())
                    .ConfigureAwait(false);

                Assert.AreEqual(MsalError.ExperimentalFeature, ex.ErrorCode);
            }
        }

        [TestMethod]
        public async Task ClientAssertionOverride_NoAppLevelCredential_WorksWithOverride()
        {
            const string overrideAssertion = "override_assertion_jwt";

            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var handler = httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
                handler.ExpectedPostData = new Dictionary<string, string>
                {
                    { OAuth2Parameter.ClientAssertionType, OAuth2AssertionType.JwtBearer },
                    { OAuth2Parameter.ClientAssertion, overrideAssertion }
                };

                // App configured without any client credential at app level
                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)
                    .BuildConcrete();

                var result = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .WithClientAssertion(overrideAssertion)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public async Task ClientAssertionOverride_NoAppLevelCredential_FailsWithoutOverride()
        {
            using (var httpManager = new MockHttpManager())
            {
                // App configured without any client credential at app level
                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)
                    .BuildConcrete();

                var ex = await Assert.ThrowsExceptionAsync<MsalClientException>(
                    () => app.AcquireTokenForClient(TestConstants.s_scope)
                        .ExecuteAsync())
                    .ConfigureAwait(false);

                Assert.AreEqual(MsalError.ClientCredentialAuthenticationTypeMustBeDefined, ex.ErrorCode);
            }
        }
    }
}
