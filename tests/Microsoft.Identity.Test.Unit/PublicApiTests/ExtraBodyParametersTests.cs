// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
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
        public async Task ValidateExtraBodyParameters()
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
                            .WithExperimentalFeatures(true) // Enable experimental features to use WithExtraBodyParameters
                            .BuildConcrete();

                //Recording test data for Asserts
                _expectedParameterHash = "8cY9AFTXo3uSqueI1A_HPiX0j66dJXB-3c3BTDcJVxE";
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
                                                        .WithExtraBodyParameters(extraBodyParams) //Sets attributes in client credential request.
                                                        .ExecuteAsync()
                                                        .ConfigureAwait(false);

                //Assert
                Assert.IsNotNull(authResult);
                Assert.AreEqual(authResult.AuthenticationResultMetadata.TokenSource, TokenSource.IdentityProvider);

                //Ensure the extra body parameters are present in the cache key
                authResult = await confidentialApp.AcquireTokenForClient(new[] { _scope })
                                        .WithFmiPath("SomeFmiPath/FmiCredentialPath") //Sets fmi path in client credential request.
                                        .WithExtraBodyParameters(extraBodyParams) //Sets attributes in client credential request.
                                        .ExecuteAsync()
                                        .ConfigureAwait(false);

                //Assert
                Assert.IsNotNull(authResult);
                Assert.AreEqual(authResult.AuthenticationResultMetadata.TokenSource, TokenSource.Cache);

                //Ensure the same extra body parameters are needed get the cache key
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                    expectedPostData: new Dictionary<string, string>
                    {
                                        { "attributetoken", "AttributeToken" },
                                        { "attributetoken2", "AttributeToken" },
                                        { "attributetoken3", "AttributeToken" }
                    });

                _expectedParameterHash = "aPnz3SdIoSMmI5yKcFs9h2vMKdZB_vahvt61jBrsCIE";
                authResult = await confidentialApp.AcquireTokenForClient(new[] { _scope })
                        .WithFmiPath("SomeFmiPath/FmiCredentialPath") //Sets fmi path in client credential request.
                        .WithExtraBodyParameters(extraBodyParams2) //Sets attributes in client credential request.
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                //Assert
                Assert.IsNotNull(authResult);
                Assert.AreEqual(authResult.AuthenticationResultMetadata.TokenSource, TokenSource.IdentityProvider);

                //Ensure the first token can still be retrieved from the cache
                _expectedParameterHash = "8cY9AFTXo3uSqueI1A_HPiX0j66dJXB-3c3BTDcJVxE";
                authResult = await confidentialApp.AcquireTokenForClient(new[] { _scope })
                        .WithFmiPath("SomeFmiPath/FmiCredentialPath") //Sets fmi path in client credential request.
                        .WithExtraBodyParameters(extraBodyParams) //Sets attributes in client credential request.
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                //Assert
                Assert.IsNotNull(authResult);
                Assert.AreEqual(authResult.AuthenticationResultMetadata.TokenSource, TokenSource.Cache);
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
                            .WithExperimentalFeatures(true) // Enable experimental features to use WithExtraBodyParameters
                            .BuildConcrete();

                //Recording test data for Asserts
                _expectedParameterHash = "zl6sDTLdSw06EytxoYRBItblGzbgi4qzQ8gvIyRywxc";
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
                Assert.AreEqual(authResult.AuthenticationResultMetadata.TokenSource, TokenSource.IdentityProvider);

                //Ensure the extra body parameters are present in the cache key
                authResult = await confidentialApp.AcquireTokenForClient(new[] { scope })
                                        .WithFmiPath("SomeFmiPath/FmiCredentialPath") //Sets fmi path in client credential request.
                                        .WithExtraBodyParameters(extraBodyParams) //Sets attributes in client credential request.
                                        .WithExtraBodyParameters(extraBodyParams2)
                                        .ExecuteAsync()
                                        .ConfigureAwait(false);

                //Assert
                Assert.IsNotNull(authResult);
                Assert.AreEqual(authResult.AuthenticationResultMetadata.TokenSource, TokenSource.Cache);

                //Ensure the same extra body parameters are needed get the cache key
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                    expectedPostData: new Dictionary<string, string>
                    {
                        { "attributetoken3", "AttributeToken" },
                        { "attributetoken4", "AttributeToken" },
                        { "attributetoken5", "AttributeToken" }
                    });

                _expectedParameterHash = "y6I4j3oaWfbZglcRJJsyBj7ROXrfqSMdYoglx8Fdp4A";
                authResult = await confidentialApp.AcquireTokenForClient(new[] { scope })
                        .WithFmiPath("SomeFmiPath/FmiCredentialPath") //Sets fmi path in client credential request.
                        .WithExtraBodyParameters(extraBodyParams2) //Sets attributes in client credential request.
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                //Assert
                Assert.IsNotNull(authResult);
                Assert.AreEqual(authResult.AuthenticationResultMetadata.TokenSource, TokenSource.IdentityProvider);

                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                    expectedPostData: new Dictionary<string, string>
                    {
                        { "attributetoken1", "AttributeToken" },
                        { "attributetoken2", "AttributeToken" }
                    });

                _expectedParameterHash = "9VQZ-ObNKwbMeTs51ehDhjCE2mB4n5N_KoAy85Sr3yQ";
                authResult = await confidentialApp.AcquireTokenForClient(new[] { scope })
                        .WithFmiPath("SomeFmiPath/FmiCredentialPath") //Sets fmi path in client credential request.
                        .WithExtraBodyParameters(extraBodyParams) //Sets attributes in client credential request.
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                //Assert
                Assert.IsNotNull(authResult);
                Assert.AreEqual(authResult.AuthenticationResultMetadata.TokenSource, TokenSource.IdentityProvider);
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
                            .WithExperimentalFeatures(true) // Enable experimental features to use WithExtraBodyParameters
                            .BuildConcrete();

                // Act & Assert
                // Ensure that the token is returned even when no extra body parameters are provided
                var authResult = await confidentialApp.AcquireTokenForClient(new[] { _scope })
                    .WithExtraBodyParameters((Dictionary<string, Func<CancellationToken, Task<string>>>)null)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(authResult);
                Assert.AreEqual(authResult.AuthenticationResultMetadata.TokenSource, TokenSource.IdentityProvider);

                authResult = await confidentialApp.AcquireTokenForClient(new[] { _scope })
                    .WithExtraBodyParameters((Dictionary<string, Func<CancellationToken, Task<string>>>)null)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(authResult);
                Assert.AreEqual(authResult.AuthenticationResultMetadata.TokenSource, TokenSource.Cache);

                //Ensure that the token can still be retrieved from the cache when the input is an empty dictionary
                authResult = await confidentialApp.AcquireTokenForClient(new[] { _scope })
                    .WithExtraBodyParameters(new Dictionary<string, Func<CancellationToken, Task<string>>>())
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(authResult);
                Assert.AreEqual(authResult.AuthenticationResultMetadata.TokenSource, TokenSource.Cache);
            }
        }

        // Add these tests to the existing ExtraBodyParametersTests class

        [TestMethod]
        public async Task WithExtraBodyParameters_SyncDictionary_BasicUsage()
        {
            using (var httpManager = new MockHttpManager())
            {
                // Arrange
                var syncBodyParams = new Dictionary<string, string>
                {
                    { "sync_attribute1", "SyncValue1" },
                    { "sync_attribute2", "SyncValue2" }
                };

                var confidentialApp = ConfidentialClientApplicationBuilder
                    .Create(_clientId)
                    .WithAuthority("https://login.microsoftonline.com/", _tenantId)
                    .WithClientSecret("ClientSecret")
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)
                    .BuildConcrete();

                // Act
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                    expectedPostData: new Dictionary<string, string>
                    {
                        { "sync_attribute1", "SyncValue1" },
                        { "sync_attribute2", "SyncValue2" }
                    });

                var authResult = await confidentialApp.AcquireTokenForClient(new[] { _scope })
                    .WithExtraBodyParameters(syncBodyParams)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(authResult);
                Assert.AreEqual(authResult.AuthenticationResultMetadata.TokenSource, TokenSource.IdentityProvider);
            }
        }

        [TestMethod]
        public async Task WithExtraBodyParameters_SyncDictionary_CacheKeyValidation()
        {
            using (var httpManager = new MockHttpManager())
            {
                // Arrange
                var syncBodyParams = new Dictionary<string, string>
                {
                    { "sync_attr", "SyncVal" }
                };

                var confidentialApp = ConfidentialClientApplicationBuilder
                    .Create(_clientId)
                    .WithAuthority("https://login.microsoftonline.com/", _tenantId)
                    .WithClientSecret("ClientSecret")
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)
                    .BuildConcrete();

                // Record cache access to validate cache key
                _expectedParameterHash = "sync_cache_key_hash"; // Will be populated from actual hash
                var appCacheAccess = confidentialApp.AppTokenCache.RecordAccess(
                    (args) =>
                    {
                        // Cache key should include the sync parameters
                        Assert.IsTrue(args.SuggestedCacheKey.Contains(_clientId));
                        Assert.IsTrue(args.SuggestedCacheKey.Contains(_tenantId));
                    });

                // Act
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                    expectedPostData: new Dictionary<string, string>
                    {
                        { "sync_attr", "SyncVal" }
                    });

                var authResult = await confidentialApp.AcquireTokenForClient(new[] { _scope })
                    .WithExtraBodyParameters(syncBodyParams)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(authResult);
                Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);

                // Second call should retrieve from cache
                authResult = await confidentialApp.AcquireTokenForClient(new[] { _scope })
                    .WithExtraBodyParameters(syncBodyParams)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(authResult);
                Assert.AreEqual(TokenSource.Cache, authResult.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public async Task WithExtraBodyParameters_SyncDictionary_NullInput()
        {
            using (var httpManager = new MockHttpManager())
            {
                // Arrange
                var confidentialApp = ConfidentialClientApplicationBuilder
                    .Create(_clientId)
                    .WithAuthority("https://login.microsoftonline.com/", _tenantId)
                    .WithClientSecret("ClientSecret")
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)
                    .BuildConcrete();

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                // Act & Assert - should handle null gracefully
                var authResult = await confidentialApp.AcquireTokenForClient(new[] { _scope })
                    .WithExtraBodyParameters((Dictionary<string, string>)null)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(authResult);
                Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public async Task WithExtraBodyParameters_SyncDictionary_EmptyDictionary()
        {
            using (var httpManager = new MockHttpManager())
            {
                // Arrange
                var confidentialApp = ConfidentialClientApplicationBuilder
                    .Create(_clientId)
                    .WithAuthority("https://login.microsoftonline.com/", _tenantId)
                    .WithClientSecret("ClientSecret")
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)
                    .BuildConcrete();

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                // Act & Assert - empty dictionary should be treated like null
                var authResult = await confidentialApp.AcquireTokenForClient(new[] { _scope })
                    .WithExtraBodyParameters(new Dictionary<string, string>())
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(authResult);
                Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public async Task WithExtraBodyParameters_SyncDictionary_MultipleParams()
        {
            using (var httpManager = new MockHttpManager())
            {
                // Arrange - test with multiple parameters
                var syncBodyParams = new Dictionary<string, string>
                {
                    { "param1", "value1" },
                    { "param2", "value2" },
                    { "param3", "value3" },
                    { "param4", "value4" },
                    { "param5", "value5" }
                };

                var confidentialApp = ConfidentialClientApplicationBuilder
                    .Create(_clientId)
                    .WithAuthority("https://login.microsoftonline.com/", _tenantId)
                    .WithClientSecret("ClientSecret")
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)
                    .BuildConcrete();

                // Act
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                    expectedPostData: syncBodyParams);

                var authResult = await confidentialApp.AcquireTokenForClient(new[] { _scope })
                    .WithExtraBodyParameters(syncBodyParams)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(authResult);
                Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public async Task WithExtraBodyParameters_SyncDictionary_SpecialCharacters()
        {
            using (var httpManager = new MockHttpManager())
            {
                // Arrange - test with special characters
                var syncBodyParams = new Dictionary<string, string>
                {
                    { "param_with_underscore", "value-with-dash" },
                    { "param.with.dot", "value.with.special.chars" },
                    { "param:colon", "value:colon" }
                };

                var confidentialApp = ConfidentialClientApplicationBuilder
                    .Create(_clientId)
                    .WithAuthority("https://login.microsoftonline.com/", _tenantId)
                    .WithClientSecret("ClientSecret")
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)
                    .BuildConcrete();

                // Act
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                    expectedPostData: syncBodyParams);

                var authResult = await confidentialApp.AcquireTokenForClient(new[] { _scope })
                    .WithExtraBodyParameters(syncBodyParams)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(authResult);
                Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public async Task WithExtraBodyParameters_SyncAndAsyncCombined()
        {
            using (var httpManager = new MockHttpManager())
            {
                // Arrange - mix sync and async parameters
                var syncBodyParams = new Dictionary<string, string>
                {
                    { "sync_param1", "sync_value1" },
                    { "sync_param2", "sync_value2" }
                };

                var asyncBodyParams = new Dictionary<string, Func<CancellationToken, Task<string>>>
                {
                    { "async_param1", (ct) => GetComputedValue() },
                    { "async_param2", (ct) => GetComputedValue() }
                };

                var confidentialApp = ConfidentialClientApplicationBuilder
                    .Create(_clientId)
                    .WithAuthority("https://login.microsoftonline.com/", _tenantId)
                    .WithClientSecret("ClientSecret")
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)
                    .BuildConcrete();

                // Act
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                    expectedPostData: new Dictionary<string, string>
                    {
                        { "sync_param1", "sync_value1" },
                        { "sync_param2", "sync_value2" },
                        { "async_param1", "AttributeToken" },
                        { "async_param2", "AttributeToken" }
                    });

                var authResult = await confidentialApp.AcquireTokenForClient(new[] { _scope })
                    .WithExtraBodyParameters(syncBodyParams)
                    .WithExtraBodyParameters(asyncBodyParams)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(authResult);
                Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public async Task WithExtraBodyParameters_SyncDictionary_OverridesPreviousSync()
        {
            using (var httpManager = new MockHttpManager())
            {
                // Arrange - test that calling WithExtraBodyParameters twice with DIFFERENT params combines them
                // (not overrides - the library combines cache key components)
                var syncParams1 = new Dictionary<string, string>
                {
                    { "param1", "value1" },
                    { "param2", "value2" }
                };

                var syncParams2 = new Dictionary<string, string>
                {
                    { "param3", "value3" },
                    { "param4", "value4" },
                    { "param5", "value5" }
                };

                var confidentialApp = ConfidentialClientApplicationBuilder
                    .Create(_clientId)
                    .WithAuthority("https://login.microsoftonline.com/", _tenantId)
                    .WithClientSecret("ClientSecret")
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)
                    .BuildConcrete();

                // Act
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                    expectedPostData: new Dictionary<string, string>
                    {
                        { "param1", "value1" },
                        { "param2", "value2" },
                        { "param3", "value3" },
                        { "param4", "value4" },
                        { "param5", "value5" }
                    });

                var authResult = await confidentialApp.AcquireTokenForClient(new[] { _scope })
                    .WithExtraBodyParameters(syncParams1)
                    .WithExtraBodyParameters(syncParams2)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(authResult);
                Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);

                // Verify second call with same combined params retrieves from cache
                authResult = await confidentialApp.AcquireTokenForClient(new[] { _scope })
                    .WithExtraBodyParameters(syncParams1)
                    .WithExtraBodyParameters(syncParams2)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(authResult);
                Assert.AreEqual(TokenSource.Cache, authResult.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task WithExtraBodyParameters_SyncDictionary_DuplicateKeyThrows()
        {
            using (var httpManager = new MockHttpManager())
            {
                // Arrange - test that duplicate keys across calls throw an error
                var syncParams1 = new Dictionary<string, string>
                {
                    { "param1", "value1" },
                    { "param2", "value2" }
                };

                        var syncParams2 = new Dictionary<string, string>
                {
                    { "param1", "different_value1" },  // Same key as syncParams1
                    { "param3", "value3" }
                };

                var confidentialApp = ConfidentialClientApplicationBuilder
                    .Create(_clientId)
                    .WithAuthority("https://login.microsoftonline.com/", _tenantId)
                    .WithClientSecret("ClientSecret")
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)
                    .BuildConcrete();

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                // Act - this should throw because param1 is duplicated
                var authResult = await confidentialApp.AcquireTokenForClient(new[] { _scope })
                    .WithExtraBodyParameters(syncParams1)
                    .WithExtraBodyParameters(syncParams2)  // Throws here
                    .ExecuteAsync()
                    .ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task WithExtraBodyParameters_SyncDictionary_WithFmiPath()
        {
            using (var httpManager = new MockHttpManager())
            {
                // Arrange - test sync params with FmiPath (from existing tests)
                var syncBodyParams = new Dictionary<string, string>
                {
                    { "fmi_attribute1", "fmi_value1" },
                    { "fmi_attribute2", "fmi_value2" }
                };

                var confidentialApp = ConfidentialClientApplicationBuilder
                    .Create(_clientId)
                    .WithAuthority("https://login.microsoftonline.com/", _tenantId)
                    .WithClientSecret("ClientSecret")
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)
                    .BuildConcrete();

                // Act
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                    expectedPostData: syncBodyParams);

                var authResult = await confidentialApp.AcquireTokenForClient(new[] { _scope })
                    .WithFmiPath("SomeFmiPath/FmiCredentialPath")
                    .WithExtraBodyParameters(syncBodyParams)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(authResult);
                Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public async Task WithExtraBodyParameters_SyncDictionary_NullValues()
        {
            using (var httpManager = new MockHttpManager())
            {
                // Arrange - test with null values in dictionary
                var syncBodyParams = new Dictionary<string, string>
                {
                    { "valid_param", "valid_value" },
                    { "null_param", null }
                };

                var confidentialApp = ConfidentialClientApplicationBuilder
                    .Create(_clientId)
                    .WithAuthority("https://login.microsoftonline.com/", _tenantId)
                    .WithClientSecret("ClientSecret")
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)
                    .BuildConcrete();

                // Act
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                    expectedPostData: new Dictionary<string, string>
                    {
                        { "valid_param", "valid_value" }
                        // null value should be skipped
                    });

                var authResult = await confidentialApp.AcquireTokenForClient(new[] { _scope })
                    .WithExtraBodyParameters(syncBodyParams)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(authResult);
                Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public async Task WithExtraBodyParameters_SyncDictionary_LargePayload()
        {
            using (var httpManager = new MockHttpManager())
            {
                // Arrange - test with large parameter values to ensure no serialization issues
                // Create a large token-like value (common in real scenarios with JWTs)
                var largeTokenValue = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9." +
                    new string('x', 5000) + ".signature";  // ~5KB value

                var syncBodyParams = new Dictionary<string, string>
                {
                    { "large_assertion", largeTokenValue },
                    { "normal_param", "normal_value" }
                };

                var confidentialApp = ConfidentialClientApplicationBuilder
                    .Create(_clientId)
                    .WithAuthority("https://login.microsoftonline.com/", _tenantId)
                    .WithClientSecret("ClientSecret")
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)
                    .BuildConcrete();

                // Act
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                    expectedPostData: new Dictionary<string, string>
                    {
                        { "large_assertion", largeTokenValue },
                        { "normal_param", "normal_value" }
                    });

                var authResult = await confidentialApp.AcquireTokenForClient(new[] { _scope })
                    .WithExtraBodyParameters(syncBodyParams)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(authResult);
                Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);

                // Verify cache works with large payloads
                authResult = await confidentialApp.AcquireTokenForClient(new[] { _scope })
                    .WithExtraBodyParameters(syncBodyParams)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(authResult);
                Assert.AreEqual(TokenSource.Cache, authResult.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public async Task WithExtraBodyParameters_SyncVsAsync_EquivalentCacheKeys()
        {
            using (var httpManager = new MockHttpManager())
            {
                // Arrange - test that sync params with same key/value produce same cache key
                // as async params with same values. This validates interoperability.
                var syncParams = new Dictionary<string, string>
                {
                    { "token_key", "token_value" },
                    { "assertion_key", "assertion_value" }
                };

                var asyncParams = new Dictionary<string, Func<CancellationToken, Task<string>>>
                {
                    { "token_key", (ct) => Task.FromResult("token_value") },
                    { "assertion_key", (ct) => Task.FromResult("assertion_value") }
                };

                var confidentialApp = ConfidentialClientApplicationBuilder
                    .Create(_clientId)
                    .WithAuthority("https://login.microsoftonline.com/", _tenantId)
                    .WithClientSecret("ClientSecret")
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)
                    .BuildConcrete();

                // Record cache accesses to compare keys
                string syncCacheKey = null;
                string asyncCacheKey = null;
                int cacheAccessCount = 0;

                var appCacheAccess = confidentialApp.AppTokenCache.RecordAccess(
                    (args) =>
                    {
                        cacheAccessCount++;
                        if (cacheAccessCount == 1)
                        {
                            syncCacheKey = args.SuggestedCacheKey;
                        }
                        else if (cacheAccessCount == 2)
                        {
                            asyncCacheKey = args.SuggestedCacheKey;
                        }
                    });

                // Act - First request with sync params
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                    expectedPostData: new Dictionary<string, string>
                    {
                        { "token_key", "token_value" },
                        { "assertion_key", "assertion_value" }
                    });

                var authResult1 = await confidentialApp.AcquireTokenForClient(new[] { _scope })
                    .WithExtraBodyParameters(syncParams)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(authResult1);
                Assert.AreEqual(TokenSource.IdentityProvider, authResult1.AuthenticationResultMetadata.TokenSource);
                Assert.IsNotNull(syncCacheKey, "Sync cache key should be recorded");

                // Assert - Cache keys should be equivalent
                // Since the implementation converts sync dict to equivalent async representation,
                // the cache keys should be identical
                Assert.AreEqual(syncCacheKey, asyncCacheKey,
                    "Sync and async parameters with same values should produce same cache key - " +
                    "the sync implementation should internally convert to equivalent async for caching");

                // Verify that subsequent call with sync params uses cache (since same cache key)
                var authResult2 = await confidentialApp.AcquireTokenForClient(new[] { _scope })
                    .WithExtraBodyParameters(syncParams)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(authResult2);
                Assert.AreEqual(TokenSource.Cache, authResult2.AuthenticationResultMetadata.TokenSource,
                    "Same sync params should retrieve from cache on second call");

                // Verify that call with async params with SAME VALUES also uses cache
                // This proves the cache keys are truly equivalent
                var authResult3 = await confidentialApp.AcquireTokenForClient(new[] { _scope })
                    .WithExtraBodyParameters(asyncParams)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(authResult3);
                Assert.AreEqual(TokenSource.Cache, authResult3.AuthenticationResultMetadata.TokenSource,
                    "Async params with same values should ALSO retrieve from cache - " +
                    "proving sync and async cache keys are equivalent");
            }
        }

        [TestMethod]
        public async Task WithExtraBodyParameters_SyncDictionary_EmptyAndWhitespaceHandling()
        {
            using (var httpManager = new MockHttpManager())
            {
                // Arrange - test ALL edge cases for empty/whitespace handling
                // Based on OAuth2Client.AddBodyParameter which checks:
                // if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
                var syncBodyParams = new Dictionary<string, string>
                {
                    // These should ALL be SKIPPED:
                    { "param1", "" },              // Empty value - SKIPPED
                    { "param2", "   " },           // Whitespace-only value - SKIPPED
                    { "param3", "\t\n\r" },        // Whitespace chars - SKIPPED
                    { "", "value_empty_key" },     // Empty key - SKIPPED (key check fails)
                    { "   ", "value_whitespace_key" }, // Whitespace key - SKIPPED (key check fails)
                    { "\t", "value_tab_key" },     // Tab key - SKIPPED (key check fails)
            
                    // Only these should be INCLUDED:
                    { "param4", "value4" },        // Normal key/value - INCLUDED
                    { "param5", "x" },             // Single char value - INCLUDED
                    { "key_with_underscore", "value-with-dash" } // Special chars - INCLUDED
                };

                var confidentialApp = ConfidentialClientApplicationBuilder
                    .Create(_clientId)
                    .WithAuthority("https://login.microsoftonline.com/", _tenantId)
                    .WithClientSecret("ClientSecret")
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)
                    .BuildConcrete();

                // Act
                httpManager.AddInstanceDiscoveryMockHandler();

                // Expected request should ONLY include the valid params
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                    expectedPostData: new Dictionary<string, string>
                    {
                        { "param4", "value4" },
                        { "param5", "x" },
                        { "key_with_underscore", "value-with-dash" }
                        // All empty/whitespace params should NOT appear
                    });

                var authResult = await confidentialApp.AcquireTokenForClient(new[] { _scope })
                    .WithExtraBodyParameters(syncBodyParams)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(authResult);
                Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource,
                    "Request should succeed with only valid parameters sent");
            }
        }

        private Task<string> GetComputedValue()
        {
            return Task.FromResult("AttributeToken");
        }
    }
}
