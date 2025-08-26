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
                    .WithExtraBodyParameters(null)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(authResult);
                Assert.AreEqual(authResult.AuthenticationResultMetadata.TokenSource, TokenSource.IdentityProvider);

                authResult = await confidentialApp.AcquireTokenForClient(new[] { _scope })
                    .WithExtraBodyParameters(null)
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

        private Task<string> GetComputedValue()
        {
            return Task.FromResult("AttributeToken");
        }
    }
}
