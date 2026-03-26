// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class WithAttributeTokensTests : TestBase
    {
        private const string ClientId = "4df2cbbb-8612-49c1-87c8-f334d6d065ad";
        private const string TenantId = "tenantid";
        private readonly string[] _scope = new[] { "api://AzureFMITokenExchange/.default" };

        [TestMethod]
        public async Task WithAttributeTokens_ForClient_AddsSpaceSeparatedTokensToBody_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                // Arrange
                var app = ConfidentialClientApplicationBuilder
                    .Create(ClientId)
                    .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)
                    .BuildConcrete();

                httpManager.AddInstanceDiscoveryMockHandler();

                var handler = httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                    token: "token_with_attr_tokens");

                handler.ExpectedPostData = new Dictionary<string, string>
                {
                    { OAuth2Parameter.AttributeTokens, "tokenA tokenB tokenC" }
                };

                // Act
                var result = await app.AcquireTokenForClient(_scope)
                    .WithAttributeTokens(new[] { "tokenA", "tokenB", "tokenC" })
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual("token_with_attr_tokens", result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public async Task WithAttributeTokens_ForClient_SingleToken_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                // Arrange
                var app = ConfidentialClientApplicationBuilder
                    .Create(ClientId)
                    .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)
                    .BuildConcrete();

                httpManager.AddInstanceDiscoveryMockHandler();

                var handler = httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                    token: "single_attr_token");

                handler.ExpectedPostData = new Dictionary<string, string>
                {
                    { OAuth2Parameter.AttributeTokens, "singleToken" }
                };

                // Act
                var result = await app.AcquireTokenForClient(_scope)
                    .WithAttributeTokens(new[] { "singleToken" })
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual("single_attr_token", result.AccessToken);
            }
        }

        [TestMethod]
        public async Task WithAttributeTokens_ForClient_IncludedInCacheKey_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                // Arrange
                var app = ConfidentialClientApplicationBuilder
                    .Create(ClientId)
                    .WithAuthority("https://login.microsoftonline.com/", TenantId)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)
                    .BuildConcrete();

                httpManager.AddInstanceDiscoveryMockHandler();

                var handler1 = httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                    token: "token_tokens1");
                handler1.ExpectedPostData = new Dictionary<string, string>
                {
                    { OAuth2Parameter.AttributeTokens, "tokenA tokenB" }
                };

                // Act - First request
                var result1 = await app.AcquireTokenForClient(_scope)
                    .WithAttributeTokens(new[] { "tokenA", "tokenB" })
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, result1.AuthenticationResultMetadata.TokenSource);

                // Act - Same tokens, should hit cache
                var result2 = await app.AcquireTokenForClient(_scope)
                    .WithAttributeTokens(new[] { "tokenA", "tokenB" })
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual(TokenSource.Cache, result2.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual("token_tokens1", result2.AccessToken);

                // Act - Different tokens, should NOT hit cache
                var handler2 = httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                    token: "token_tokens2");
                handler2.ExpectedPostData = new Dictionary<string, string>
                {
                    { OAuth2Parameter.AttributeTokens, "tokenX tokenY" }
                };

                var result3 = await app.AcquireTokenForClient(_scope)
                    .WithAttributeTokens(new[] { "tokenX", "tokenY" })
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, result3.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual("token_tokens2", result3.AccessToken);

                // Verify cache has 2 entries
                Assert.HasCount(2, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
            }
        }

        [TestMethod]
        public void WithAttributeTokens_ForClient_Null_ThrowsException()
        {
            using (var httpManager = new MockHttpManager())
            {
                // Arrange
                var app = ConfidentialClientApplicationBuilder
                    .Create(ClientId)
                    .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)
                    .BuildConcrete();

                // Act & Assert - Null should throw
                Assert.Throws<ArgumentNullException>(() =>
                    app.AcquireTokenForClient(_scope)
                        .WithAttributeTokens(null));
            }
        }

        [TestMethod]
        public async Task WithAttributeTokens_ForClient_CombinedWithOtherMethods_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                // Arrange
                var app = ConfidentialClientApplicationBuilder
                    .Create(ClientId)
                    .WithAuthority("https://login.microsoftonline.com/", TenantId)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)
                    .BuildConcrete();

                httpManager.AddInstanceDiscoveryMockHandler();

                var handler = httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                    token: "combined_token",
                    expectedPostData: new Dictionary<string, string>
                    {
                        { OAuth2Parameter.FmiPath, "SomeFmiPath" },
                        { OAuth2Parameter.AttributeTokens, "tok1 tok2" }
                    });

                // Act - Combine WithAttributeTokens and WithFmiPath
                var result = await app.AcquireTokenForClient(_scope)
                    .WithAttributeTokens(new[] { "tok1", "tok2" })
                    .WithFmiPath("SomeFmiPath")
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual("combined_token", result.AccessToken);
            }
        }

        [TestMethod]
        public async Task WithAttributeTokens_OnBehalfOf_AddsSpaceSeparatedTokensToBody_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                // Arrange
                var app = ConfidentialClientApplicationBuilder
                    .Create(ClientId)
                    .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                httpManager.AddInstanceDiscoveryMockHandler();

                var handler = new MockHttpMessageHandler()
                {
                    ExpectedMethod = System.Net.Http.HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                    ExpectedPostData = new Dictionary<string, string>
                    {
                        { OAuth2Parameter.AttributeTokens, "oboToken1 oboToken2" }
                    }
                };
                httpManager.AddMockHandler(handler);

                // Act
                var result = await app.AcquireTokenOnBehalfOf(
                        _scope,
                        new UserAssertion(TestConstants.DefaultAccessToken))
                    .WithAttributeTokens(new[] { "oboToken1", "oboToken2" })
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public void WithAttributeTokens_OnBehalfOf_Null_ThrowsException()
        {
            using (var httpManager = new MockHttpManager())
            {
                // Arrange
                var app = ConfidentialClientApplicationBuilder
                    .Create(ClientId)
                    .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                // Act & Assert - Null should throw
                Assert.Throws<ArgumentNullException>(() =>
                    app.AcquireTokenOnBehalfOf(
                            _scope,
                            new UserAssertion(TestConstants.DefaultAccessToken))
                        .WithAttributeTokens(null));
            }
        }

        [TestMethod]
        public async Task WithAttributeTokens_ByAuthCode_AddsSpaceSeparatedTokensToBody_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                // Arrange
                var app = ConfidentialClientApplicationBuilder
                    .Create(ClientId)
                    .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithRedirectUri("https://localhost")
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                httpManager.AddInstanceDiscoveryMockHandler();

                var handler = new MockHttpMessageHandler()
                {
                    ExpectedMethod = System.Net.Http.HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                    ExpectedPostData = new Dictionary<string, string>
                    {
                        { OAuth2Parameter.AttributeTokens, "authCodeToken1 authCodeToken2 authCodeToken3" }
                    }
                };
                httpManager.AddMockHandler(handler);

                // Act
                var result = await app.AcquireTokenByAuthorizationCode(
                        _scope,
                        "some_auth_code")
                    .WithAttributeTokens(new[] { "authCodeToken1", "authCodeToken2", "authCodeToken3" })
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public void WithAttributeTokens_ByAuthCode_Null_ThrowsException()
        {
            using (var httpManager = new MockHttpManager())
            {
                // Arrange
                var app = ConfidentialClientApplicationBuilder
                    .Create(ClientId)
                    .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithRedirectUri("https://localhost")
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                // Act & Assert - Null should throw
                Assert.Throws<ArgumentNullException>(() =>
                    app.AcquireTokenByAuthorizationCode(_scope, "some_auth_code")
                        .WithAttributeTokens(null));
            }
        }
    }
}
