// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Test.Common.Core.Helpers;
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
        public async Task WithAttributeTokens_ForClient_Null_NoOp_Async()
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
                    token: "token_without_attr_tokens");

                handler.ExpectedPostData = new Dictionary<string, string>
                {
                    { OAuth2Parameter.Scope, string.Join(" ", _scope) }
                };

                // Act
                var result = await app.AcquireTokenForClient(_scope)
                    .WithAttributeTokens(null)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual("token_without_attr_tokens", result.AccessToken);
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
        public async Task WithAttributeTokens_OnBehalfOf_IncludedInCacheKey_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                // Arrange
                var app = ConfidentialClientApplicationBuilder
                    .Create(ClientId)
                    .WithAuthority("https://login.microsoftonline.com/", TenantId)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                httpManager.AddInstanceDiscoveryMockHandler();

                var handler1 = new MockHttpMessageHandler()
                {
                    ExpectedMethod = System.Net.Http.HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                        string.Join(" ", _scope),
                        MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId),
                        MockHelpers.CreateClientInfo()),
                    ExpectedPostData = new Dictionary<string, string>
                    {
                        { OAuth2Parameter.AttributeTokens, "oboTokenA oboTokenB" }
                    }
                };
                httpManager.AddMockHandler(handler1);

                var userAssertion = new UserAssertion(TestConstants.DefaultAccessToken);

                // Act - First request with tokens A,B
                var result1 = await app.AcquireTokenOnBehalfOf(_scope, userAssertion)
                    .WithAttributeTokens(new[] { "oboTokenA", "oboTokenB" })
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, result1.AuthenticationResultMetadata.TokenSource);

                // Act - Same tokens, should hit cache
                var result2 = await app.AcquireTokenOnBehalfOf(_scope, userAssertion)
                    .WithAttributeTokens(new[] { "oboTokenA", "oboTokenB" })
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual(TokenSource.Cache, result2.AuthenticationResultMetadata.TokenSource);

                // Act - Different tokens, should NOT hit cache
                var handler2 = new MockHttpMessageHandler()
                {
                    ExpectedMethod = System.Net.Http.HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                        string.Join(" ", _scope),
                        MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId),
                        MockHelpers.CreateClientInfo()),
                    ExpectedPostData = new Dictionary<string, string>
                    {
                        { OAuth2Parameter.AttributeTokens, "oboTokenX oboTokenY" }
                    }
                };
                httpManager.AddMockHandler(handler2);

                var result3 = await app.AcquireTokenOnBehalfOf(_scope, userAssertion)
                    .WithAttributeTokens(new[] { "oboTokenX", "oboTokenY" })
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, result3.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public async Task WithAttributeTokens_OnBehalfOf_Null_NoOp_Async()
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
                    ExpectedPostData = new Dictionary<string, string>()
                };

                httpManager.AddMockHandler(handler);

                // Act
                var result = await app.AcquireTokenOnBehalfOf(
                        _scope,
                        new UserAssertion(TestConstants.DefaultAccessToken))
                    .WithAttributeTokens(null)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
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
        public async Task WithAttributeTokens_ByAuthCode_NotServedFromCache_Async()
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

                var handler1 = new MockHttpMessageHandler()
                {
                    ExpectedMethod = System.Net.Http.HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                    ExpectedPostData = new Dictionary<string, string>
                    {
                        { OAuth2Parameter.AttributeTokens, "authCodeTokenA authCodeTokenB" }
                    }
                };
                httpManager.AddMockHandler(handler1);

                // Act - First auth-code redemption
                var result1 = await app.AcquireTokenByAuthorizationCode(
                        _scope,
                        "auth_code_1")
                    .WithAttributeTokens(new[] { "authCodeTokenA", "authCodeTokenB" })
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, result1.AuthenticationResultMetadata.TokenSource);

                var handler2 = new MockHttpMessageHandler()
                {
                    ExpectedMethod = System.Net.Http.HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                    ExpectedPostData = new Dictionary<string, string>
                    {
                        { OAuth2Parameter.AttributeTokens, "authCodeTokenA authCodeTokenB" }
                    }
                };
                httpManager.AddMockHandler(handler2);

                // Act - Second auth-code redemption with same attribute tokens should still go to IDP
                var result2 = await app.AcquireTokenByAuthorizationCode(
                        _scope,
                        "auth_code_2")
                    .WithAttributeTokens(new[] { "authCodeTokenA", "authCodeTokenB" })
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(TokenSource.IdentityProvider, result2.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public async Task WithAttributeTokens_ByAuthCode_Null_NoOp_Async()
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
                    ExpectedPostData = new Dictionary<string, string>()
                };

                httpManager.AddMockHandler(handler);

                // Act
                var result = await app.AcquireTokenByAuthorizationCode(_scope, "some_auth_code")
                    .WithAttributeTokens(null)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public void WithAttributeTokens_ForClient_EmbeddedSpace_ThrowsArgumentException()
        {
            var app = ConfidentialClientApplicationBuilder
                .Create(ClientId)
                .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                .WithClientSecret(TestConstants.ClientSecret)
                .WithExperimentalFeatures(true)
                .BuildConcrete();

            var ex = AssertException.Throws<ArgumentException>(() =>
                app.AcquireTokenForClient(_scope)
                    .WithAttributeTokens(new[] { "valid", "invalid token", "another" }));

            Assert.Contains("Attribute tokens must not contain whitespace", ex.Message);
            Assert.Contains("invalid token", ex.Message);
        }

        [TestMethod]
        public void WithAttributeTokens_ForClient_EmbeddedTab_ThrowsArgumentException()
        {
            var app = ConfidentialClientApplicationBuilder
                .Create(ClientId)
                .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                .WithClientSecret(TestConstants.ClientSecret)
                .WithExperimentalFeatures(true)
                .BuildConcrete();

            var ex = AssertException.Throws<ArgumentException>(() =>
                app.AcquireTokenForClient(_scope)
                    .WithAttributeTokens(new[] { "token\twith\ttab" }));

            Assert.Contains("Attribute tokens must not contain whitespace", ex.Message);
        }

        [TestMethod]
        public void WithAttributeTokens_ForClient_EmbeddedNewline_ThrowsArgumentException()
        {
            var app = ConfidentialClientApplicationBuilder
                .Create(ClientId)
                .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                .WithClientSecret(TestConstants.ClientSecret)
                .WithExperimentalFeatures(true)
                .BuildConcrete();

            AssertException.Throws<ArgumentException>(() =>
                app.AcquireTokenForClient(_scope)
                    .WithAttributeTokens(new[] { "token\nwith\nnewline" }));

            AssertException.Throws<ArgumentException>(() =>
                app.AcquireTokenForClient(_scope)
                    .WithAttributeTokens(new[] { "token\rwith\rreturn" }));
        }

        [TestMethod]
        public async Task WithAttributeTokens_ForClient_EmptyCollection_NoOp_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                var app = ConfidentialClientApplicationBuilder
                    .Create(ClientId)
                    .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)
                    .BuildConcrete();

                httpManager.AddInstanceDiscoveryMockHandler();

                var handler = httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                    token: "token_no_attr_tokens");

                handler.ExpectedPostData = new Dictionary<string, string>
                {
                    { OAuth2Parameter.Scope, string.Join(" ", _scope) }
                };

                // Act
                var result = await app.AcquireTokenForClient(_scope)
                    .WithAttributeTokens(new string[0])
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual("token_no_attr_tokens", result.AccessToken);
            }
        }

        [TestMethod]
        public async Task WithAttributeTokens_ForClient_WhitespaceOnlyTokens_Skipped_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                var app = ConfidentialClientApplicationBuilder
                    .Create(ClientId)
                    .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)
                    .BuildConcrete();

                httpManager.AddInstanceDiscoveryMockHandler();

                var handler = httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                    token: "token_no_attr_tokens");

                handler.ExpectedPostData = new Dictionary<string, string>
                {
                    { OAuth2Parameter.Scope, string.Join(" ", _scope) }
                };

                // Act - all tokens are whitespace-only, should be skipped entirely
                var result = await app.AcquireTokenForClient(_scope)
                    .WithAttributeTokens(new[] { "", " ", "  ", "\t", "\n" })
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual("token_no_attr_tokens", result.AccessToken);
            }
        }

        [TestMethod]
        public async Task WithAttributeTokens_ForClient_LeadingTrailingWhitespace_Trimmed_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                var app = ConfidentialClientApplicationBuilder
                    .Create(ClientId)
                    .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)
                    .BuildConcrete();

                httpManager.AddInstanceDiscoveryMockHandler();

                var handler = httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                    token: "trimmed_token");

                handler.ExpectedPostData = new Dictionary<string, string>
                {
                    { OAuth2Parameter.AttributeTokens, "tokenA tokenB tokenC" }
                };

                // Act - tokens with leading/trailing whitespace should be trimmed
                var result = await app.AcquireTokenForClient(_scope)
                    .WithAttributeTokens(new[] { "  tokenA  ", "\ttokenB\t", " tokenC " })
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual("trimmed_token", result.AccessToken);
            }
        }

        [TestMethod]
        public void WithAttributeTokens_OnBehalfOf_EmbeddedSpace_ThrowsArgumentException()
        {
            var app = ConfidentialClientApplicationBuilder
                .Create(ClientId)
                .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                .WithClientSecret(TestConstants.ClientSecret)
                .BuildConcrete();

            var ex = AssertException.Throws<ArgumentException>(() =>
                app.AcquireTokenOnBehalfOf(
                        _scope,
                        new UserAssertion(TestConstants.DefaultAccessToken))
                    .WithAttributeTokens(new[] { "token with space" }));

            Assert.Contains("Attribute tokens must not contain whitespace", ex.Message);
        }

        [TestMethod]
        public void WithAttributeTokens_ByAuthCode_EmbeddedSpace_ThrowsArgumentException()
        {
            var app = ConfidentialClientApplicationBuilder
                .Create(ClientId)
                .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                .WithRedirectUri("https://localhost")
                .WithClientSecret(TestConstants.ClientSecret)
                .BuildConcrete();

            var ex = AssertException.Throws<ArgumentException>(() =>
                app.AcquireTokenByAuthorizationCode(_scope, "some_auth_code")
                    .WithAttributeTokens(new[] { "token with space" }));

            Assert.Contains("Attribute tokens must not contain whitespace", ex.Message);
        }
    }
}
