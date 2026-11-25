// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Client.Extensibility;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class ExtraBodyParametersTests : TestBase
    {
        private string _clientId = "4df2cbbb-8612-49c1-87c8-f334d6d065ad";
        private string _scope = "api://AzureFMITokenExchange/.default";
        private string _tenantId = "tenantid";
        private string _expectedParameterHash = "";

        string _expectedExternalCacheKey => $"{_clientId}_{_tenantId}_{_expectedParameterHash}_AppTokenCache";

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task ValidateExtraBodyParameters(bool enableExperimentalFeatures)
        {
            using (var httpManager = new MockHttpManager())
            {
                //Arrange
                var extraBodyParams = new Dictionary<string, Func<CancellationToken, Task<string>>>
                    {
                        { "attributetoken", (CancellationToken ct) => GetComputedValue() },
                        { "attributetoken2", (CancellationToken ct) => GetComputedValue() }
                    };

                var extraBodyParams2 = new Dictionary<string, Func<CancellationToken, Task<string>>>
                    {
                        { "attributetoken", (CancellationToken ct) => GetComputedValue() },
                        { "attributetoken2", (CancellationToken ct) => GetComputedValue() },
                        { "attributetoken3", (CancellationToken ct) => GetComputedValue() }
                    };

                //Act
                //Create application
                var confidentialApp = ConfidentialClientApplicationBuilder
                            .Create(_clientId)
                            .WithAuthority("https://login.microsoftonline.com/", _tenantId)
                            .WithClientSecret("ClientSecret")
                            .WithHttpManager(httpManager)
                            .WithExperimentalFeatures(enableExperimentalFeatures)
                            .BuildConcrete();

                //Recording test data for Asserts
                _expectedParameterHash = "CIrQtkl8Nr9NUGzgmYq6F2GfO9tfuTaNmcWjfuwD_IY";
                var appCacheAccess = confidentialApp.AppTokenCache.RecordAccess(
                    (args) => 
                    { 
                        Assert.AreEqual(_expectedExternalCacheKey, args.SuggestedCacheKey); }
                    );

                //Acquire AuthN
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                    expectedPostData: new Dictionary<string, string>
                    {
                        { "attributetoken", "AttributeToken" },
                        { "attributetoken2", "AttributeToken" }
                    });

                var authResult = await confidentialApp.AcquireTokenForClient(new[] { _scope })
                                                        .WithFmiPath("SomeFmiPath/FmiCredentialPath") //Sets fmi path in client credential request.
                                                        .WithExtraBodyParameters<AcquireTokenForClientParameterBuilder>(extraBodyParams)
                                                        .ExecuteAsync()
                                                        .ConfigureAwait(false);

                //Assert
                Assert.IsNotNull(authResult);
                Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);

                //Ensure the extra body parameters are present in the cache key
                authResult = await confidentialApp.AcquireTokenForClient(new[] { _scope })
                                        .WithFmiPath("SomeFmiPath/FmiCredentialPath") //Sets fmi path in client credential request.
                                        .WithExtraBodyParameters(extraBodyParams) //Sets attributes in client credential request.
                                        .ExecuteAsync()
                                        .ConfigureAwait(false);

                //Assert
                Assert.IsNotNull(authResult);
                Assert.AreEqual(TokenSource.Cache, authResult.AuthenticationResultMetadata.TokenSource);

                //Ensure the same extra body parameters are needed get the cache key
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                    expectedPostData: new Dictionary<string, string>
                    {
                                        { "attributetoken", "AttributeToken" },
                                        { "attributetoken2", "AttributeToken" },
                                        { "attributetoken3", "AttributeToken" }
                    });

                _expectedParameterHash = "dWRpwsigCH5bvOwffB1LVopCmuTUdKxxHqRxrEYm_-4";
                authResult = await confidentialApp.AcquireTokenForClient(new[] { _scope })
                        .WithFmiPath("SomeFmiPath/FmiCredentialPath") //Sets fmi path in client credential request.
                        .WithExtraBodyParameters(extraBodyParams2) //Sets attributes in client credential request.
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                //Assert
                Assert.IsNotNull(authResult);
                Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);

                //Ensure the first token can still be retrieved from the cache
                _expectedParameterHash = "CIrQtkl8Nr9NUGzgmYq6F2GfO9tfuTaNmcWjfuwD_IY";
                authResult = await confidentialApp.AcquireTokenForClient(new[] { _scope })
                        .WithFmiPath("SomeFmiPath/FmiCredentialPath") //Sets fmi path in client credential request.
                        .WithExtraBodyParameters(extraBodyParams) //Sets attributes in client credential request.
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                //Assert
                Assert.IsNotNull(authResult);
                Assert.AreEqual(TokenSource.Cache, authResult.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public async Task ValidateExtraBodyParametersAreCombined()
        {
            using (var httpManager = new MockHttpManager())
            {
                //Arrange
                var clientId = "4df2cbbb-8612-49c1-87c8-f334d6d065ad";
                var scope = "api://AzureFMITokenExchange/.default";
                var tenantId = "tenantid";

                //Act
                //Create application
                var confidentialApp = ConfidentialClientApplicationBuilder
                            .Create(clientId)
                            .WithAuthority("https://login.microsoftonline.com/", tenantId)
                            .WithClientSecret("ClientSecret")
                            .WithHttpManager(httpManager)
                            .BuildConcrete();

                //Recording test data for Asserts
                _expectedParameterHash = "gbZ-K5rm2VgvPYtqD0pP4Oihs91IlhZeEa6y341hT8Q";
                var appCacheAccess = confidentialApp.AppTokenCache.RecordAccess(
                    (args) =>
                        {
                            Assert.AreEqual(_expectedExternalCacheKey, args.SuggestedCacheKey);
                        }
                    );

                //Acquire AuthN
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                    expectedPostData: new Dictionary<string, string>
                    {
                        { "attributetoken1", "AttributeToken" },
                        { "attributetoken2", "AttributeToken" },
                        { "attributetoken3", "AttributeToken" },
                        { "attributetoken4", "AttributeToken" },
                        { "attributetoken5", "AttributeToken" }
                    });

                var extraBodyParams = new Dictionary<string, Func<CancellationToken, Task<string>>>
                    {
                        { "attributetoken1", (CancellationToken ct) => GetComputedValue() },
                        { "attributetoken2", (CancellationToken ct) => GetComputedValue() }
                    };

                var extraBodyParams2 = new Dictionary<string, Func<CancellationToken, Task<string>>>
                    {
                        { "attributetoken3", (CancellationToken ct) => GetComputedValue() },
                        { "attributetoken4", (CancellationToken ct) => GetComputedValue() },
                        { "attributetoken5", (CancellationToken ct) => GetComputedValue() }
                    };

                var authResult = await confidentialApp.AcquireTokenForClient(new[] { scope })
                                                        .WithFmiPath("SomeFmiPath/FmiCredentialPath") //Sets fmi path in client credential request.
                                                        .WithExtraBodyParameters(extraBodyParams) //Sets attributes in client credential request.
                                                        .WithExtraBodyParameters(extraBodyParams2)
                                                        .ExecuteAsync()
                                                        .ConfigureAwait(false);

                //Assert
                Assert.IsNotNull(authResult);
                Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);

                //Ensure the extra body parameters are present in the cache key
                authResult = await confidentialApp.AcquireTokenForClient(new[] { scope })
                                        .WithFmiPath("SomeFmiPath/FmiCredentialPath") //Sets fmi path in client credential request.
                                        .WithExtraBodyParameters(extraBodyParams) //Sets attributes in client credential request.
                                        .WithExtraBodyParameters(extraBodyParams2)
                                        .ExecuteAsync()
                                        .ConfigureAwait(false);

                //Assert
                Assert.IsNotNull(authResult);
                Assert.AreEqual(TokenSource.Cache, authResult.AuthenticationResultMetadata.TokenSource);

                //Ensure the same extra body parameters are needed get the cache key
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                    expectedPostData: new Dictionary<string, string>
                    {
                        { "attributetoken3", "AttributeToken" },
                        { "attributetoken4", "AttributeToken" },
                        { "attributetoken5", "AttributeToken" }
                    });

                _expectedParameterHash = "Ade7sX3kx0BV8-08QgWceDSrim2hnNeUFVvX3g75WpU";
                authResult = await confidentialApp.AcquireTokenForClient(new[] { scope })
                        .WithFmiPath("SomeFmiPath/FmiCredentialPath") //Sets fmi path in client credential request.
                        .WithExtraBodyParameters(extraBodyParams2) //Sets attributes in client credential request.
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                //Assert
                Assert.IsNotNull(authResult);
                Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);

                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                    expectedPostData: new Dictionary<string, string>
                    {
                        { "attributetoken1", "AttributeToken" },
                        { "attributetoken2", "AttributeToken" }
                    });

                _expectedParameterHash = "9yULcb0-qBhB25_0PpAL-m1Cdz-49CQLOTywgx1Ie24";
                authResult = await confidentialApp.AcquireTokenForClient(new[] { scope })
                        .WithFmiPath("SomeFmiPath/FmiCredentialPath") //Sets fmi path in client credential request.
                        .WithExtraBodyParameters(extraBodyParams) //Sets attributes in client credential request.
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                //Assert
                Assert.IsNotNull(authResult);
                Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public async Task WithExtraBodyParameters_NullInput_ReturnToken()
        {
            using (var httpManager = new MockHttpManager())
            {
                // Arrange
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                var confidentialApp = ConfidentialClientApplicationBuilder
                            .Create(_clientId)
                            .WithAuthority("https://login.microsoftonline.com/", _tenantId)
                            .WithClientSecret("ClientSecret")
                            .WithHttpManager(httpManager)
                            .BuildConcrete();

                // Act & Assert
                // Ensure that the token is returned even when no extra body parameters are provided
                var authResult = await confidentialApp.AcquireTokenForClient(new[] { _scope })
                    .WithExtraBodyParameters((Dictionary<string, Func<CancellationToken, Task<string>>>)null)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(authResult);
                Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);

                authResult = await confidentialApp.AcquireTokenForClient(new[] { _scope })
                    .WithExtraBodyParameters((Dictionary<string, Func<CancellationToken, Task<string>>>)null)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(authResult);
                Assert.AreEqual(TokenSource.Cache, authResult.AuthenticationResultMetadata.TokenSource);

                //Ensure that the token can still be retrieved from the cache when the input is an empty dictionary
                authResult = await confidentialApp.AcquireTokenForClient(new[] { _scope })
                    .WithExtraBodyParameters(new Dictionary<string, Func<CancellationToken, Task<string>>>())
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(authResult);
                Assert.AreEqual(TokenSource.Cache, authResult.AuthenticationResultMetadata.TokenSource);

                // Synchronous overload with explicit typed null
                authResult = await confidentialApp.AcquireTokenForClient(new[] { _scope })
                    .WithExtraBodyParameters((Dictionary<string, string>)null)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(authResult);
                Assert.AreEqual(TokenSource.Cache, authResult.AuthenticationResultMetadata.TokenSource);

                // Synchronous overload with empty dictionary
                authResult = await confidentialApp.AcquireTokenForClient(new[] { _scope })
                    .WithExtraBodyParameters(new Dictionary<string, string>())
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(authResult);
                Assert.AreEqual(TokenSource.Cache, authResult.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        [DataRow("v")]
        [DataRow("override")]
        public async Task SynchronousExtraBodyParameters_ExplicitCachePartitionKey_TakesPrecedence(string partitionValue)
        {
            // Arrange
            using var httpManager = new MockHttpManager();
            var app = ConfidentialClientApplicationBuilder.Create(_clientId)
                .WithAuthority("https://login.microsoftonline.com/", _tenantId)
                .WithClientSecret("ClientSecret")
                .WithHttpManager(httpManager)
                .WithExperimentalFeatures(true)
                .BuildConcrete();
            httpManager.AddInstanceDiscoveryMockHandler();
            httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                expectedPostData: new Dictionary<string, string> { { "k", "v" } });

            // Act
            var result = await app.AcquireTokenForClient(new[] { _scope })
                .WithExtraBodyParameters(new Dictionary<string, string> { { "k", "v" } })
                .WithCachePartitionKey("k", partitionValue, partitionRefreshToken: true)
                .ExecuteAsync()
                .ConfigureAwait(false);

            // Assert
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            var cachedToken = app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Single();
            Assert.AreEqual(partitionValue, cachedToken.AdditionalCacheKeyComponents["k"]);

            result = await app.AcquireTokenForClient(new[] { _scope })
                .WithExtraBodyParameters(new Dictionary<string, Func<CancellationToken, Task<string>>>
                {
                    { "k", _ => Task.FromResult("v") }
                })
                .WithCachePartitionKey("k", partitionValue, partitionRefreshToken: true)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
            cachedToken = app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Single();
            Assert.AreEqual(partitionValue, cachedToken.AdditionalCacheKeyComponents["k"]);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task SynchronousExtraBodyParameters_QueryCacheKeyCollision_Throws(bool queryFirst)
        {
            // Arrange
            using var httpManager = new MockHttpManager();
            var app = ConfidentialClientApplicationBuilder.Create(_clientId)
                .WithAuthority("https://login.microsoftonline.com/", _tenantId)
                .WithClientSecret("ClientSecret")
                .WithHttpManager(httpManager)
                .WithExperimentalFeatures(true)
                .BuildConcrete();
            var query = new Dictionary<string, (string Value, bool IncludeInCacheKey)>
            {
                { "k", ("q", true) }
            };
            httpManager.AddInstanceDiscoveryMockHandler();
            httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
            await app.AcquireTokenForClient(new[] { _scope })
                .WithExtraQueryParameters(query)
                .ExecuteAsync()
                .ConfigureAwait(false);
            Assert.AreEqual("q", app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Single().AdditionalCacheKeyComponents["k"]);

            // Act and Assert
            foreach (string bodyValue in new[] { "v1", "v2" })
            {
                await Assert.ThrowsAsync<ArgumentException>(async () =>
                {
                    var builder = app.AcquireTokenForClient(new[] { _scope });
                    var body = new Dictionary<string, string> { { "k", bodyValue } };
                    if (queryFirst)
                    {
                        builder.WithExtraQueryParameters(query).WithExtraBodyParameters(body);
                    }
                    else
                    {
                        builder.WithExtraBodyParameters(body).WithExtraQueryParameters(query);
                    }

                    await builder.ExecuteAsync().ConfigureAwait(false);
                }).ConfigureAwait(false);
                Assert.AreEqual("q", app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Single().AdditionalCacheKeyComponents["k"]);
            }
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task ExtraBodyParameters_MixedSyncAsyncDuplicate_Throws(bool synchronousFirst)
        {
            // Arrange
            using var httpManager = new MockHttpManager();
            var app = ConfidentialClientApplicationBuilder.Create(_clientId)
                .WithAuthority("https://login.microsoftonline.com/", _tenantId)
                .WithClientSecret("ClientSecret")
                .WithHttpManager(httpManager)
                .WithExperimentalFeatures(true)
                .BuildConcrete();
            var syncBody = new Dictionary<string, string> { { "k", "sync" } };
            var asyncBody = new Dictionary<string, Func<CancellationToken, Task<string>>>
            {
                { "k", _ => Task.FromResult("async") }
            };
            httpManager.AddInstanceDiscoveryMockHandler();
            httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                expectedPostData: new Dictionary<string, string> { { "k", "async" } });
            await app.AcquireTokenForClient(new[] { _scope })
                .WithExtraBodyParameters(asyncBody)
                .ExecuteAsync()
                .ConfigureAwait(false);
            Assert.AreEqual("async", app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Single().AdditionalCacheKeyComponents["k"]);

            // Act and Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                var builder = app.AcquireTokenForClient(new[] { _scope });
                if (synchronousFirst)
                {
                    builder.WithExtraBodyParameters(syncBody).WithExtraBodyParameters(asyncBody);
                }
                else
                {
                    builder.WithExtraBodyParameters(asyncBody).WithExtraBodyParameters(syncBody);
                }

                await builder.ExecuteAsync().ConfigureAwait(false);
            }).ConfigureAwait(false);
            Assert.AreEqual("async", app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Single().AdditionalCacheKeyComponents["k"]);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void ExtraBodyParameters_ConflictingSources_CannotBeHiddenByPartitionOverride(bool queryCollision)
        {
            // Arrange
            var app = ConfidentialClientApplicationBuilder.Create(_clientId)
                .WithClientSecret("ClientSecret")
                .WithExperimentalFeatures(true)
                .BuildConcrete();
            var builder = app.AcquireTokenForClient(new[] { _scope })
                .WithExtraBodyParameters(new Dictionary<string, string> { { "k", "sync" } });
            if (queryCollision)
            {
                builder.WithExtraQueryParameters(new Dictionary<string, (string Value, bool IncludeInCacheKey)>
                {
                    { "k", ("q", true) }
                });
            }
            else
            {
                builder.WithExtraBodyParameters(new Dictionary<string, Func<CancellationToken, Task<string>>>
                {
                    { "k", _ => Task.FromResult("async") }
                });
            }

            // Act and Assert
            Assert.Throws<ArgumentException>(() => builder.WithCachePartitionKey("k", "override"));
            Assert.AreEqual("sync", builder.CommonParameters.SyncCacheKeyComponents["k"]);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void ExtraBodyParameters_DuplicateBodyKey_Throws(bool synchronous)
        {
            // Arrange
            var app = ConfidentialClientApplicationBuilder.Create(_clientId)
                .WithClientSecret("ClientSecret")
                .WithExperimentalFeatures(true)
                .BuildConcrete();
            var builder = app.AcquireTokenForClient(new[] { _scope });

            // Act and Assert
            if (synchronous)
            {
                var parameters = new Dictionary<string, string> { { "k", "v" } };
                builder.WithExtraBodyParameters(parameters);
                Assert.Throws<ArgumentException>(() => builder.WithExtraBodyParameters(parameters));
            }
            else
            {
                var parameters = new Dictionary<string, Func<CancellationToken, Task<string>>>
                {
                    { "k", _ => Task.FromResult("v") }
                };
                builder.WithExtraBodyParameters(parameters);
                Assert.Throws<ArgumentException>(() => builder.WithExtraBodyParameters(parameters));
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task ValidateSynchronousExtraBodyParametersAndCacheKey(bool enableExperimentalFeatures)
        {
            using (var httpManager = new MockHttpManager())
            {
                // Arrange
                var confidentialApp = ConfidentialClientApplicationBuilder
                    .Create(_clientId)
                    .WithAuthority("https://login.microsoftonline.com/", _tenantId)
                    .WithClientSecret("ClientSecret")
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(enableExperimentalFeatures)
                    .BuildConcrete();

                var firstParameters = new Dictionary<string, string>
                {
                    { "attributetoken", "AttributeToken" }
                };
                var secondParameters = new Dictionary<string, string>
                {
                    { "attributetoken", "DifferentAttributeToken" }
                };

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                    expectedPostData: new Dictionary<string, string>
                    {
                        { "attributetoken", "AttributeToken" }
                    });

                var builder = confidentialApp.AcquireTokenForClient(new[] { _scope })
                    .WithExtraBodyParameters(firstParameters);

                Assert.IsNull(builder.CommonParameters.OnBeforeTokenRequestHandler);
                Assert.AreEqual("AttributeToken", builder.CommonParameters.SyncCacheKeyComponents["attributetoken"]);

                // Act and Assert
                var authResult = await builder
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);

                authResult = await confidentialApp.AcquireTokenForClient(new[] { _scope })
                    .WithExtraBodyParameters(firstParameters)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual(TokenSource.Cache, authResult.AuthenticationResultMetadata.TokenSource);

                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                    expectedPostData: new Dictionary<string, string>
                    {
                        { "attributetoken", "DifferentAttributeToken" }
                    });

                authResult = await confidentialApp.AcquireTokenForClient(new[] { _scope })
                    .WithExtraBodyParameters(secondParameters)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);

                authResult = await confidentialApp.AcquireTokenForClient(new[] { _scope })
                    .WithExtraBodyParameters(firstParameters)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual(TokenSource.Cache, authResult.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public async Task WithExtraBodyParameters_ReservedParameterCollision_ThrowsArgumentException()
        {
            using (var httpManager = new MockHttpManager())
            {
                // Arrange
                httpManager.AddInstanceDiscoveryMockHandler();

                var confidentialApp = ConfidentialClientApplicationBuilder
                    .Create(_clientId)
                    .WithAuthority("https://login.microsoftonline.com/", _tenantId)
                    .WithClientSecret("ClientSecret")
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)
                    .BuildConcrete();

                var collidingParameters = new Dictionary<string, string>
                {
                    { "client_id", "colliding_client_id" }
                };

                // Act & Assert
                var ex = await AssertException.TaskThrowsAsync<ArgumentException>(() =>
                    confidentialApp.AcquireTokenForClient(new[] { _scope })
                        .WithExtraBodyParameters(collidingParameters)
                        .ExecuteAsync()).ConfigureAwait(false);

                Assert.IsNotNull(ex);
            }
        }

        [TestMethod]
        public async Task WithExtraBodyParameters_EmptyValueIsIncludedInRequest()
        {
            using (var httpManager = new MockHttpManager())
            {
                // Arrange
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                    expectedPostData: new Dictionary<string, string>
                    {
                        { "optional_parameter", string.Empty }
                    });

                var confidentialApp = ConfidentialClientApplicationBuilder
                    .Create(_clientId)
                    .WithAuthority("https://login.microsoftonline.com/", _tenantId)
                    .WithClientSecret("ClientSecret")
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)
                    .BuildConcrete();

                // Act
                var authResult = await confidentialApp.AcquireTokenForClient(new[] { _scope })
                    .WithExtraBodyParameters(new Dictionary<string, string>
                    {
                        { "optional_parameter", string.Empty }
                    })
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public async Task ValidateSynchronousExtraBodyParameters_CacheSerializationRoundTrip()
        {
            using (var httpManager = new MockHttpManager())
            {
                // Arrange
                var confidentialApp1 = ConfidentialClientApplicationBuilder
                    .Create(_clientId)
                    .WithAuthority("https://login.microsoftonline.com/", _tenantId)
                    .WithClientSecret("ClientSecret")
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)
                    .BuildConcrete();

                var parameters = new Dictionary<string, string>
                {
                    { "a_b", "valueUnderscore" },
                    { "a-b", "valueHyphen" }
                };

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                    expectedPostData: new Dictionary<string, string>
                    {
                        { "a-b", "valueHyphen" },
                        { "a_b", "valueUnderscore" }
                    });

                var authResult = await confidentialApp1.AcquireTokenForClient(new[] { _scope })
                    .WithExtraBodyParameters(parameters)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);

                // Serialize the cache state from app 1
                byte[] serializedCache = ((ITokenCacheSerializer)confidentialApp1.AppTokenCache).SerializeMsalV3();

                // Create app 2 and deserialize the cache state into it
                var confidentialApp2 = ConfidentialClientApplicationBuilder
                    .Create(_clientId)
                    .WithAuthority("https://login.microsoftonline.com/", _tenantId)
                    .WithClientSecret("ClientSecret")
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)
                    .BuildConcrete();

                ((ITokenCacheSerializer)confidentialApp2.AppTokenCache).DeserializeMsalV3(serializedCache);

                // Act - Acquire token using app 2 with the same parameters
                var cachedResult = await confidentialApp2.AcquireTokenForClient(new[] { _scope })
                    .WithExtraBodyParameters(parameters)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert - Must be a cache hit even after deserialization
                Assert.AreEqual(TokenSource.Cache, cachedResult.AuthenticationResultMetadata.TokenSource);
            }
        }

        private Task<string> GetComputedValue()
        {
            return Task.FromResult("AttributeToken");
        }
    }
}
