// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
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
                    .WithExperimentalFeatures()
                    .WithHttpManager(httpManager)
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
                    .WithExperimentalFeatures()
                    .WithHttpManager(httpManager)
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
        public async Task WithAttributeTokens_ForClient_OrderInsensitive_SortedBeforeJoin_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                var app = ConfidentialClientApplicationBuilder
                    .Create(ClientId)
                    .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithExperimentalFeatures()
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                httpManager.AddInstanceDiscoveryMockHandler();

                // First call: pass tokens in reverse order; expect sorted body.
                var handler1 = httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                    token: "first_token");
                handler1.ExpectedPostData = new Dictionary<string, string>
                {
                    { OAuth2Parameter.AttributeTokens, "tokenA tokenB tokenC" }
                };

                var first = await app.AcquireTokenForClient(_scope)
                    .WithAttributeTokens(new[] { "tokenC", "tokenA", "tokenB" })
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual("first_token", first.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, first.AuthenticationResultMetadata.TokenSource);

                // Second call: same set of tokens in a different order; should be served from cache
                // (no new HTTP handler queued — would fail if a wire call were made).
                var second = await app.AcquireTokenForClient(_scope)
                    .WithAttributeTokens(new[] { "tokenB", "tokenC", "tokenA" })
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual("first_token", second.AccessToken);
                Assert.AreEqual(TokenSource.Cache, second.AuthenticationResultMetadata.TokenSource);
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
                    .WithExperimentalFeatures()
                    .WithHttpManager(httpManager)
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
                    .WithExperimentalFeatures()
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                httpManager.AddInstanceDiscoveryMockHandler();

                var handler = httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                    token: "token_without_attr_tokens");

                handler.ExpectedPostData = new Dictionary<string, string>
                {
                    { OAuth2Parameter.Scope, string.Join(" ", _scope) }
                };

                handler.UnExpectedPostData = new Dictionary<string, string>
                {
                    { OAuth2Parameter.AttributeTokens, null }
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
                    .WithExperimentalFeatures()
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                httpManager.AddInstanceDiscoveryMockHandler();

                var handler = httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                    token: "combined_token",
                    expectedPostData: new Dictionary<string, string>
                    {
                        { OAuth2Parameter.FmiPath, "SomeFmiPath" },
                        { OAuth2Parameter.AttributeTokens, "tok1 tok2" }
                    });

                // Act - Combine WithAttributeTokens and WithFmiPath in either order.
                // WithAttributeTokens<T> returns T (the concrete builder), so concrete-only methods
                // like WithFmiPath remain available after it in the fluent chain.
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
        public async Task WithAttributeTokens_ForClient_ChainBeforeConcreteMethod_TypePreserved_Async()
        {
            // Regression test for type narrowing: WithAttributeTokens<T> must return T so that
            // concrete-builder methods (like WithFmiPath) remain available later in the chain.
            using (var httpManager = new MockHttpManager())
            {
                var app = ConfidentialClientApplicationBuilder
                    .Create(ClientId)
                    .WithAuthority("https://login.microsoftonline.com/", TenantId)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithExperimentalFeatures()
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                httpManager.AddInstanceDiscoveryMockHandler();

                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                    token: "type_preserved_token",
                    expectedPostData: new Dictionary<string, string>
                    {
                        { OAuth2Parameter.FmiPath, "AnotherFmiPath" },
                        { OAuth2Parameter.AttributeTokens, "tA tB" }
                    });

                var result = await app.AcquireTokenForClient(_scope)
                    .WithAttributeTokens(new[] { "tA", "tB" })
                    .WithFmiPath("AnotherFmiPath")
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual("type_preserved_token", result.AccessToken);
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
                    .WithExperimentalFeatures()
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
        public async Task WithAttributeTokens_OnBehalfOf_SentInRequestBody_Async()
        {
            // Note: WithAttributeTokens stores attribute_tokens in the AT's
            // AdditionalCacheKeyComponents (item-level), NOT in the OBO partition key
            // (which is the UserAssertion hash). Multiple attribute-token variants for
            // the same assertion are stored side-by-side in the same partition and are
            // disambiguated on read by FilterTokensByAdditionalKeyComponents — provided
            // every read passes the same WithAttributeTokens set. Mixing attributed and
            // non-attributed reads against the same assertion can return unintended
            // cache entries (see the dedicated OBO cache tests at the bottom of this file).
            using (var httpManager = new MockHttpManager())
            {
                var app = ConfidentialClientApplicationBuilder
                    .Create(ClientId)
                    .WithAuthority("https://login.microsoftonline.com/", TenantId)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithExperimentalFeatures()
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                httpManager.AddInstanceDiscoveryMockHandler();

                httpManager.AddMockHandler(new MockHttpMessageHandler()
                {
                    ExpectedMethod = System.Net.Http.HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                        TestConstants.UniqueId,
                        TestConstants.DisplayableId,
                        _scope,
                        accessToken: "obo_at_AB"),
                    ExpectedPostData = new Dictionary<string, string>
                    {
                        { OAuth2Parameter.AttributeTokens, "oboTokenA oboTokenB" }
                    }
                });

                var userAssertion = new UserAssertion(TestConstants.DefaultAccessToken);

                // First call with tokens A,B - hits IDP and verifies attribute_tokens in body.
                var result1 = await app.AcquireTokenOnBehalfOf(_scope, userAssertion)
                    .WithAttributeTokens(new[] { "oboTokenA", "oboTokenB" })
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, result1.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual("obo_at_AB", result1.AccessToken);

                // Second call with same tokens should hit cache (no mock handler queued; if
                // MSAL went to network the test would throw on missing handler).
                var result2 = await app.AcquireTokenOnBehalfOf(_scope, userAssertion)
                    .WithAttributeTokens(new[] { "oboTokenA", "oboTokenB" })
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual(TokenSource.Cache, result2.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual("obo_at_AB", result2.AccessToken);

                // Third call with different tokens should bypass the cached A/B entry and
                // mint a new AT, with the new attribute_tokens value in the body.
                httpManager.AddMockHandler(new MockHttpMessageHandler()
                {
                    ExpectedMethod = System.Net.Http.HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                        TestConstants.UniqueId,
                        TestConstants.DisplayableId,
                        _scope,
                        accessToken: "obo_at_XY"),
                    ExpectedPostData = new Dictionary<string, string>
                    {
                        { OAuth2Parameter.AttributeTokens, "oboTokenX oboTokenY" }
                    }
                });

                var result3 = await app.AcquireTokenOnBehalfOf(_scope, userAssertion)
                    .WithAttributeTokens(new[] { "oboTokenX", "oboTokenY" })
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, result3.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual("obo_at_XY", result3.AccessToken);
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
                    .WithExperimentalFeatures()
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                httpManager.AddInstanceDiscoveryMockHandler();

                var handler = new MockHttpMessageHandler()
                {
                    ExpectedMethod = System.Net.Http.HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                    ExpectedPostData = new Dictionary<string, string>(),
                    UnExpectedPostData = new Dictionary<string, string>
                    {
                        { OAuth2Parameter.AttributeTokens, null }
                    }
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
                    .WithExperimentalFeatures()
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
                    .WithExperimentalFeatures()
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
                    .WithExperimentalFeatures()
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                httpManager.AddInstanceDiscoveryMockHandler();

                var handler = new MockHttpMessageHandler()
                {
                    ExpectedMethod = System.Net.Http.HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                    ExpectedPostData = new Dictionary<string, string>(),
                    UnExpectedPostData = new Dictionary<string, string>
                    {
                        { OAuth2Parameter.AttributeTokens, null }
                    }
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
                    .WithExperimentalFeatures()
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                httpManager.AddInstanceDiscoveryMockHandler();

                var handler = httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                    token: "token_no_attr_tokens");

                handler.ExpectedPostData = new Dictionary<string, string>
                {
                    { OAuth2Parameter.Scope, string.Join(" ", _scope) }
                };

                handler.UnExpectedPostData = new Dictionary<string, string>
                {
                    { OAuth2Parameter.AttributeTokens, null }
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
                    .WithExperimentalFeatures()
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                httpManager.AddInstanceDiscoveryMockHandler();

                var handler = httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                    token: "token_no_attr_tokens");

                handler.ExpectedPostData = new Dictionary<string, string>
                {
                    { OAuth2Parameter.Scope, string.Join(" ", _scope) }
                };

                handler.UnExpectedPostData = new Dictionary<string, string>
                {
                    { OAuth2Parameter.AttributeTokens, null }
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
                    .WithExperimentalFeatures()
                    .WithHttpManager(httpManager)
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

        // ---------------------------------------------------------------------
        // OBO cache-partition behavior tests
        //
        // These tests exercise the claim documented in
        // WithAttributeTokens_OnBehalfOf_SentInRequestBody_Async that "MSAL does
        // not currently keep entries with different cache-key components
        // side-by-side in the same partition" for OBO.
        //
        // What the OBO partition key actually is (CacheKeyFactory.GetOboKey):
        //   OboCacheKey ?? UserAssertion.AssertionHash ?? HomeAccountId
        //
        // The partition key does NOT include AdditionalCacheKeyComponents (and
        // thus does not include attribute_tokens). This means two OBO calls
        // that use the same user assertion but different attribute_tokens DO
        // land in the same in-memory partition — but they are stored as two
        // distinct cache items, distinguished by AdditionalCacheKeyComponents,
        // and FilterTokensByAdditionalKeyComponents disambiguates them on read
        // when the caller supplies WithAttributeTokens with the matching set.
        // ---------------------------------------------------------------------

        [TestMethod]
        public async Task WithAttributeTokens_OnBehalfOf_SameAssertion_DifferentAttributeTokens_StoredSideBySide_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                var app = ConfidentialClientApplicationBuilder
                    .Create(ClientId)
                    .WithAuthority("https://login.microsoftonline.com/", TenantId)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithExperimentalFeatures()
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                httpManager.AddInstanceDiscoveryMockHandler();

                var userAssertion = new UserAssertion(TestConstants.DefaultAccessToken);

                // First OBO call with attribute set A -> IDP
                httpManager.AddMockHandler(new MockHttpMessageHandler()
                {
                    ExpectedMethod = System.Net.Http.HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                        TestConstants.UniqueId,
                        TestConstants.DisplayableId,
                        _scope,
                        accessToken: "obo_at_A"),
                    ExpectedPostData = new Dictionary<string, string>
                    {
                        { OAuth2Parameter.AttributeTokens, "attrA1 attrA2" }
                    }
                });

                var resultA = await app.AcquireTokenOnBehalfOf(_scope, userAssertion)
                    .WithAttributeTokens(new[] { "attrA1", "attrA2" })
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, resultA.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual("obo_at_A", resultA.AccessToken);

                // Second OBO call, SAME assertion, different attribute set B -> IDP
                httpManager.AddMockHandler(new MockHttpMessageHandler()
                {
                    ExpectedMethod = System.Net.Http.HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                        TestConstants.UniqueId,
                        TestConstants.DisplayableId,
                        _scope,
                        accessToken: "obo_at_B"),
                    ExpectedPostData = new Dictionary<string, string>
                    {
                        { OAuth2Parameter.AttributeTokens, "attrB1 attrB2" }
                    }
                });

                var resultB = await app.AcquireTokenOnBehalfOf(_scope, userAssertion)
                    .WithAttributeTokens(new[] { "attrB1", "attrB2" })
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, resultB.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual("obo_at_B", resultB.AccessToken);

                // === Assert side-by-side storage in a SINGLE OBO partition ===
                var allAts = app.UserTokenCacheInternal.Accessor.GetAllAccessTokens();
                Assert.HasCount(2, allAts, "Both attribute-token variants should be cached.");

                // Both items share the same partition key (OboCacheKey == UserAssertion.AssertionHash).
                Assert.AreEqual(userAssertion.AssertionHash, allAts[0].OboCacheKey);
                Assert.AreEqual(userAssertion.AssertionHash, allAts[1].OboCacheKey);

                // ...but each carries a distinct AdditionalCacheKeyComponents entry.
                var atA = allAts.Single(at => at.Secret == "obo_at_A");
                var atB = allAts.Single(at => at.Secret == "obo_at_B");

                Assert.IsNotNull(atA.AdditionalCacheKeyComponents);
                Assert.IsNotNull(atB.AdditionalCacheKeyComponents);
                Assert.AreEqual("attrA1 attrA2", atA.AdditionalCacheKeyComponents[OAuth2Parameter.AttributeTokens]);
                Assert.AreEqual("attrB1 attrB2", atB.AdditionalCacheKeyComponents[OAuth2Parameter.AttributeTokens]);

                // === Assert filter-on-read disambiguates correctly ===
                // No new mock handler queued: any IDP call would fail the test.

                var resultA2 = await app.AcquireTokenOnBehalfOf(_scope, userAssertion)
                    .WithAttributeTokens(new[] { "attrA1", "attrA2" })
                    .ExecuteAsync()
                    .ConfigureAwait(false);
                Assert.AreEqual(TokenSource.Cache, resultA2.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual("obo_at_A", resultA2.AccessToken);

                var resultB2 = await app.AcquireTokenOnBehalfOf(_scope, userAssertion)
                    .WithAttributeTokens(new[] { "attrB1", "attrB2" })
                    .ExecuteAsync()
                    .ConfigureAwait(false);
                Assert.AreEqual(TokenSource.Cache, resultB2.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual("obo_at_B", resultB2.AccessToken);

                // Token-set order should not matter (deduped + ordinal-sorted by WithAttributeTokens).
                var resultA3 = await app.AcquireTokenOnBehalfOf(_scope, userAssertion)
                    .WithAttributeTokens(new[] { "attrA2", "attrA1", "attrA1" })
                    .ExecuteAsync()
                    .ConfigureAwait(false);
                Assert.AreEqual(TokenSource.Cache, resultA3.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual("obo_at_A", resultA3.AccessToken);
            }
        }

        [TestMethod]
        public async Task WithAttributeTokens_OnBehalfOf_DifferentAssertions_LiveInDifferentPartitions_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                var app = ConfidentialClientApplicationBuilder
                    .Create(ClientId)
                    .WithAuthority("https://login.microsoftonline.com/", TenantId)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithExperimentalFeatures()
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                httpManager.AddInstanceDiscoveryMockHandler();

                var userAssertion1 = new UserAssertion(TestConstants.DefaultAccessToken);
                var userAssertion2 = new UserAssertion(TestConstants.DefaultAccessToken + "_other_user");

                // OBO call for user 1 with attribute set A
                httpManager.AddMockHandler(new MockHttpMessageHandler()
                {
                    ExpectedMethod = System.Net.Http.HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                        TestConstants.UniqueId, TestConstants.DisplayableId, _scope,
                        accessToken: "obo_at_user1_A"),
                    ExpectedPostData = new Dictionary<string, string>
                    {
                        { OAuth2Parameter.AttributeTokens, "attrA" }
                    }
                });
                var r1 = await app.AcquireTokenOnBehalfOf(_scope, userAssertion1)
                    .WithAttributeTokens(new[] { "attrA" })
                    .ExecuteAsync()
                    .ConfigureAwait(false);
                Assert.AreEqual("obo_at_user1_A", r1.AccessToken);

                // OBO call for user 2 with attribute set A — different partition entirely
                httpManager.AddMockHandler(new MockHttpMessageHandler()
                {
                    ExpectedMethod = System.Net.Http.HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                        TestConstants.UniqueId + "_2", TestConstants.DisplayableId, _scope,
                        accessToken: "obo_at_user2_A"),
                    ExpectedPostData = new Dictionary<string, string>
                    {
                        { OAuth2Parameter.AttributeTokens, "attrA" }
                    }
                });
                var r2 = await app.AcquireTokenOnBehalfOf(_scope, userAssertion2)
                    .WithAttributeTokens(new[] { "attrA" })
                    .ExecuteAsync()
                    .ConfigureAwait(false);
                Assert.AreEqual("obo_at_user2_A", r2.AccessToken);

                var allAts = app.UserTokenCacheInternal.Accessor.GetAllAccessTokens();
                Assert.HasCount(2, allAts);

                // Different partition keys (OboCacheKey == UserAssertion.AssertionHash).
                Assert.AreNotEqual(userAssertion1.AssertionHash, userAssertion2.AssertionHash);
                Assert.IsTrue(allAts.Any(at => at.OboCacheKey == userAssertion1.AssertionHash));
                Assert.IsTrue(allAts.Any(at => at.OboCacheKey == userAssertion2.AssertionHash));

                // Each subsequent call hits its own partition's cached AT.
                var r1b = await app.AcquireTokenOnBehalfOf(_scope, userAssertion1)
                    .WithAttributeTokens(new[] { "attrA" })
                    .ExecuteAsync()
                    .ConfigureAwait(false);
                Assert.AreEqual(TokenSource.Cache, r1b.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual("obo_at_user1_A", r1b.AccessToken);

                var r2b = await app.AcquireTokenOnBehalfOf(_scope, userAssertion2)
                    .WithAttributeTokens(new[] { "attrA" })
                    .ExecuteAsync()
                    .ConfigureAwait(false);
                Assert.AreEqual(TokenSource.Cache, r2b.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual("obo_at_user2_A", r2b.AccessToken);
            }
        }

        [TestMethod]
        public async Task WithAttributeTokens_OnBehalfOf_NoAttributeTokens_ReturnsAttributedEntry_GotchaAsync()
        {
            // Edge case / real gotcha: an attributed AT is in cache; a follow-up OBO call
            // for the same assertion that does NOT pass WithAttributeTokens will:
            //   - bypass FilterTokensByAdditionalKeyComponents (because requestParams.CacheKeyComponents is null)
            //   - therefore match the attributed AT on every other filter
            //   - return it from cache as if it were a plain unattributed token.
            // This demonstrates the actual limitation: callers who mix attributed and
            // non-attributed reads on the same assertion can get unexpected token reuse.
            using (var httpManager = new MockHttpManager())
            {
                var app = ConfidentialClientApplicationBuilder
                    .Create(ClientId)
                    .WithAuthority("https://login.microsoftonline.com/", TenantId)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithExperimentalFeatures()
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                httpManager.AddInstanceDiscoveryMockHandler();
                var userAssertion = new UserAssertion(TestConstants.DefaultAccessToken);

                httpManager.AddMockHandler(new MockHttpMessageHandler()
                {
                    ExpectedMethod = System.Net.Http.HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                        TestConstants.UniqueId, TestConstants.DisplayableId, _scope,
                        accessToken: "obo_at_with_attrs"),
                    ExpectedPostData = new Dictionary<string, string>
                    {
                        { OAuth2Parameter.AttributeTokens, "attrA" }
                    }
                });
                await app.AcquireTokenOnBehalfOf(_scope, userAssertion)
                    .WithAttributeTokens(new[] { "attrA" })
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // No mock handler queued for the second call. If MSAL went to IDP the test
                // would throw on missing handler. Therefore reaching the assertion below
                // proves the lookup was served from the cache.
                var resultNoAttr = await app.AcquireTokenOnBehalfOf(_scope, userAssertion)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual(TokenSource.Cache, resultNoAttr.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual("obo_at_with_attrs", resultNoAttr.AccessToken,
                    "Non-attributed OBO call returned the attributed AT — this is the documented gotcha.");
            }
        }

        [TestMethod]
        public async Task WithAttributeTokens_OnBehalfOf_NoAttributeTokens_TwoAttributedEntries_ThrowsMultipleMatchingTokens_Async()
        {
            // Edge case 2 (the second documented failure mode): when the OBO partition holds
            // multiple attributed ATs (different attribute_tokens for the same assertion) and
            // a follow-up read does NOT pass WithAttributeTokens, the additional-key-components
            // filter is bypassed → both attributed ATs survive scope/env/clientId filters →
            // GetSingleToken sees count > 1 → throws MsalClientException(multiple_matching_tokens_detected).
            using (var httpManager = new MockHttpManager())
            {
                var app = ConfidentialClientApplicationBuilder
                    .Create(ClientId)
                    .WithAuthority("https://login.microsoftonline.com/", TenantId)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithExperimentalFeatures()
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                httpManager.AddInstanceDiscoveryMockHandler();
                var userAssertion = new UserAssertion(TestConstants.DefaultAccessToken);

                // Seed cache with two attributed ATs in the same OBO partition.
                httpManager.AddMockHandler(new MockHttpMessageHandler()
                {
                    ExpectedMethod = System.Net.Http.HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                        TestConstants.UniqueId, TestConstants.DisplayableId, _scope,
                        accessToken: "obo_at_A"),
                    ExpectedPostData = new Dictionary<string, string>
                    {
                        { OAuth2Parameter.AttributeTokens, "attrA" }
                    }
                });
                await app.AcquireTokenOnBehalfOf(_scope, userAssertion)
                    .WithAttributeTokens(new[] { "attrA" })
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                httpManager.AddMockHandler(new MockHttpMessageHandler()
                {
                    ExpectedMethod = System.Net.Http.HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                        TestConstants.UniqueId, TestConstants.DisplayableId, _scope,
                        accessToken: "obo_at_B"),
                    ExpectedPostData = new Dictionary<string, string>
                    {
                        { OAuth2Parameter.AttributeTokens, "attrB" }
                    }
                });
                await app.AcquireTokenOnBehalfOf(_scope, userAssertion)
                    .WithAttributeTokens(new[] { "attrB" })
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.HasCount(2, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens());

                // Non-attributed read against the same partition → ambiguous.
                var ex = await AssertException.TaskThrowsAsync<MsalClientException>(() =>
                    app.AcquireTokenOnBehalfOf(_scope, userAssertion)
                        .ExecuteAsync())
                    .ConfigureAwait(false);

                Assert.AreEqual(MsalError.MultipleTokensMatchedError, ex.ErrorCode);
            }
        }
    }
}
