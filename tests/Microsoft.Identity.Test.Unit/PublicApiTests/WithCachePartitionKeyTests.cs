// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class WithCachePartitionKeyTests
    {
        [TestMethod]
        public async Task WithCachePartitionKey_PopulatesCacheKeyComponents_Async()
        {
            // Arrange
            const string cachePartitionKey = "partition_key";
            const string cachePartitionValue = "partition_value";

            var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                .WithClientSecret(TestConstants.ClientSecret)
                .BuildConcrete();

            var builder = app.AcquireTokenForClient(TestConstants.s_scope)
                .WithCachePartitionKey(cachePartitionKey, cachePartitionValue);

            // Act
            var commonParameters = GetCommonParameters(builder);

            // Assert
            Assert.IsNotNull(commonParameters.CacheKeyComponents);
            Assert.HasCount(1, commonParameters.CacheKeyComponents);
            Assert.IsTrue(commonParameters.CacheKeyComponents.ContainsKey(cachePartitionKey));
            var cachedValue = await commonParameters.CacheKeyComponents[cachePartitionKey](CancellationToken.None)
                .ConfigureAwait(false);
            Assert.AreEqual(cachePartitionValue, cachedValue);
            Assert.IsNull(commonParameters.ExtraQueryParameters);
        }

        [TestMethod]
        public async Task WithCachePartitionKey_DoesNotAddToExtraQueryParameters_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                // Arrange
                const string cachePartitionKey = "partition_key";
                const string cachePartitionValue = "partition_value";

                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                httpManager.AddInstanceDiscoveryMockHandler();
                var handler = httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(token: "token_with_partition_key");
                handler.ExpectedPostData = new Dictionary<string, string>
                {
                    { OAuth2Parameter.Scope, TestConstants.s_scope.AsSingleString() }
                };
                handler.UnExpectedPostData = new Dictionary<string, string>
                {
                    { cachePartitionKey, null }
                };
                handler.AdditionalRequestValidation = request =>
                {
                    Assert.DoesNotContain(cachePartitionKey + "=", request.RequestUri.Query);
                };

                // Act
                var result = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .WithCachePartitionKey(cachePartitionKey, cachePartitionValue)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual("token_with_partition_key", result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public void WithCachePartitionKey_ThrowsOnNullOrEmptyKey()
        {
            // Arrange
            var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                .WithClientSecret(TestConstants.ClientSecret)
                .BuildConcrete();

            var builder = app.AcquireTokenForClient(TestConstants.s_scope);

            // Act
            ArgumentNullException nullKeyException = AssertException.Throws<ArgumentNullException>(() => builder.WithCachePartitionKey(null, "value"));
            ArgumentException emptyKeyException = AssertException.Throws<ArgumentException>(() => builder.WithCachePartitionKey(string.Empty, "value"));

            // Assert
            Assert.AreEqual("key", nullKeyException.ParamName);
            Assert.AreEqual("key", emptyKeyException.ParamName);
        }

        [TestMethod]
        public void WithCachePartitionKey_ThrowsOnNullValue()
        {
            // Arrange
            var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                .WithClientSecret(TestConstants.ClientSecret)
                .BuildConcrete();

            var builder = app.AcquireTokenForClient(TestConstants.s_scope);

            // Act
            ArgumentNullException exception = AssertException.Throws<ArgumentNullException>(() => builder.WithCachePartitionKey("key", null));

            // Assert
            Assert.AreEqual("value", exception.ParamName);
        }

        private static AcquireTokenCommonParameters GetCommonParameters(object builder)
        {
            Type currentType = builder.GetType();

            while (currentType != null)
            {
                var commonParametersProperty = currentType.GetProperty(
                    "CommonParameters",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (commonParametersProperty != null)
                {
                    return (AcquireTokenCommonParameters)commonParametersProperty.GetValue(builder);
                }

                currentType = currentType.BaseType;
            }

            Assert.Fail("CommonParameters property was not found on the builder.");
            return null;
        }
    }
}
