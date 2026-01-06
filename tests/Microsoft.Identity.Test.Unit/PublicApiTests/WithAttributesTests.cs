// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class WithAttributesTests : TestBase
    {
        private const string ClientId = "4df2cbbb-8612-49c1-87c8-f334d6d065ad";
        private const string TenantId = "tenantid";
        private readonly string[] _scope = new[] { "api://AzureFMITokenExchange/.default" };

        [TestMethod]
        public async Task WithAttributes_AddsAttributeToRequestBody_Async()
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

                // Add the mock handler and capture it
                var handler = httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                    token: "token_with_attribute");

                // Add expected body parameter
                handler.ExpectedPostData = new Dictionary<string, string>
                {
                    { OAuth2Parameter.Attributes, "test_attribute_value" }
                };

                // Act
                var result = await app.AcquireTokenForClient(_scope)
                    .WithAttributes("test_attribute_value")
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual("token_with_attribute", result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public async Task WithAttributes_IncludedInCacheKey_Async()
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
                    token: "token_with_attribute1");

                // Add expected query parameter - FIXED to match what we're sending
                handler.ExpectedPostData = new Dictionary<string, string>
                {
                    { OAuth2Parameter.Attributes, "attribute1" }  
                };

                // Act - First request with attribute1
                var result1 = await app.AcquireTokenForClient(_scope)
                    .WithAttributes("attribute1")
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert - Token from IdP
                Assert.AreEqual("token_with_attribute1", result1.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result1.AuthenticationResultMetadata.TokenSource);

                // Act - Second request with same attribute1 (should hit cache)
                var result2 = await app.AcquireTokenForClient(_scope)
                    .WithAttributes("attribute1")
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert - Token from cache
                Assert.AreEqual("token_with_attribute1", result2.AccessToken);
                Assert.AreEqual(TokenSource.Cache, result2.AuthenticationResultMetadata.TokenSource);

                // Arrange - Add mock for different attribute
                var handlerSecond = httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                    token: "token_with_attribute2");

                // Add expected query parameter
                handlerSecond.ExpectedPostData = new Dictionary<string, string>
                {
                    { OAuth2Parameter.Attributes, "{attribute2: {\"key\": \"value2\"}}" }
                };

                // Act - Third request with different attribute2 (should NOT hit cache)
                var result3 = await app.AcquireTokenForClient(_scope)
                    .WithAttributes("{attribute2: {\"key\": \"value2\"}}")
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert - New token from IdP
                Assert.AreEqual("token_with_attribute2", result3.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result3.AuthenticationResultMetadata.TokenSource);

                // Verify cache has 2 different tokens
                Assert.AreEqual(2, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
            }
        }

        [TestMethod]
        public async Task WithAttributes_NullOrEmpty_ThrowsException_Async()
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

                // Act & Assert - Null attribute should throw
                await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
                {
                    await app.AcquireTokenForClient(_scope)
                        .WithAttributes(null)
                        .ExecuteAsync()
                        .ConfigureAwait(false);
                }).ConfigureAwait(false);

                // Act & Assert - Empty attribute should throw
                await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
                {
                    await app.AcquireTokenForClient(_scope)
                        .WithAttributes("")
                        .ExecuteAsync()
                        .ConfigureAwait(false);
                }).ConfigureAwait(false);

                // Act & Assert - Whitespace attribute should throw
                await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
                {
                    await app.AcquireTokenForClient(_scope)
                        .WithAttributes("   ")
                        .ExecuteAsync()
                        .ConfigureAwait(false);
                }).ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task WithAttributes_CanBeCombinedWithOtherBuilderMethods_Async()
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
                    token: "token_with_attribute_and_fmi",
                    expectedPostData: new Dictionary<string, string>
                    {
                        { OAuth2Parameter.FmiPath, "SomeFmiPath" },
                        { OAuth2Parameter.Attributes, "test_attribute" }
                    });

                // Act - Combine WithAttributes and WithFmiPath
                var result = await app.AcquireTokenForClient(_scope)
                    .WithAttributes("test_attribute")
                    .WithFmiPath("SomeFmiPath")
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual("token_with_attribute_and_fmi", result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public async Task WithAttributes_SameValueReturnsCachedToken_Async()
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
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                    token: "cached_token");

                // Act - First call
                var result1 = await app.AcquireTokenForClient(_scope)
                    .WithAttributes("same_attribute")
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(TokenSource.IdentityProvider, result1.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual("cached_token", result1.AccessToken);

                // Act - Second call with same attribute
                var result2 = await app.AcquireTokenForClient(_scope)
                    .WithAttributes("same_attribute")
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert - Should be from cache
                Assert.AreEqual(TokenSource.Cache, result2.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual("cached_token", result2.AccessToken);

                // Verify only one token in cache
                Assert.AreEqual(1, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
            }
        }
    }
}
