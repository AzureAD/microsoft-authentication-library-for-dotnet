// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Unit.AuthExtension;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit
{
    [TestClass]
    public class AuthenticationOperationTests : TestBase
    {
        private const string ProtectedUrl = "https://www.contoso.com/path1/path2?queryParam1=a&queryParam2=b";

        [TestMethod]
        public async Task Should_UseCustomRequestHeaders_And_StoreAdditionalParameters()
        {
            using (var httpManager = new MockHttpManager())
            {
                ConfidentialClientApplication app =
                    ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithHttpManager(httpManager)
                                                              .WithExperimentalFeatures(true)
                                                              .BuildConcrete();

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri(ProtectedUrl));

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseWithAdditionalParamsMessage(tokenType: "someAccessTokenType", additionalparams: string.Empty);

                MsalAuthenticationExtension authExtension = new MsalAuthenticationExtension()
                {
                    AuthenticationOperation = new MsalTestAuthenticationOperation()
                };

                // Act
                var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                    .WithTenantId(TestConstants.Utid)
                    .WithAuthenticationExtension(authExtension)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                var expectedAt = "header.payload.signature" + "AccessTokenModifier";
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.IsFalse(result.AdditionalResponseParameters.Any());
                Assert.AreEqual(expectedAt, result.AccessToken);

                //Verify that the original AT token is cached and the extension is reused
                result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                    .WithTenantId(TestConstants.Utid)
                    .WithAuthenticationExtension(authExtension)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                Assert.IsTrue(result.AdditionalResponseParameters == null);
                Assert.AreEqual(expectedAt, result.AccessToken);
            }
        }

        [TestMethod]
        public async Task Should_UseCustomRequestHeaders_And_StoreAdditionalParametersWithCaching()
        {
            using (var httpManager = new MockHttpManager())
            {
                ConfidentialClientApplication app =
                    ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithHttpManager(httpManager)
                                                              .WithExperimentalFeatures(true)
                                                              .BuildConcrete();

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri(ProtectedUrl));

                Dictionary<string, string> expectedRequestHeaders = new Dictionary<string, string>
                {
                    { "key1", "value1" }
                };
                
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseWithAdditionalParamsMessage(tokenType: "someAccessTokenType", expectedRequestHeaders: expectedRequestHeaders);

                MsalAuthenticationExtension authExtension = new MsalAuthenticationExtension()
                {
                    OnBeforeTokenRequestHandler = async (data) =>
                    {
                        data.Headers.Add("key1", "value1");
                        await Task.CompletedTask.ConfigureAwait(false);
                    },

                    AuthenticationOperation = new MsalTestAuthenticationOperation(),
                    AdditionalCacheParameters = new[] { "additional_param1", "additional_param2" }
                };

                // Act
                var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                    .WithTenantId(TestConstants.Utid)
                    .WithAuthenticationExtension(authExtension)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                var expectedAt = "header.payload.signature"
                 + "AccessTokenModifier"
                 + result.AdditionalResponseParameters["additional_param1"]
                 + result.AdditionalResponseParameters["additional_param2"];
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.IsTrue(result.AdditionalResponseParameters.Keys.Contains("additional_param1"));
                Assert.IsTrue(result.AdditionalResponseParameters.Keys.Contains("additional_param2"));
                Assert.AreEqual(expectedAt, result.AccessToken);

                result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                    .WithTenantId(TestConstants.Utid)
                    .WithAuthenticationExtension(authExtension)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);

                Assert.IsTrue(result.AdditionalResponseParameters.Keys.Contains("additional_param1"));
                Assert.IsTrue(result.AdditionalResponseParameters.Keys.Contains("additional_param2"));
                Assert.AreEqual(expectedAt, result.AccessToken);
            }
        }

        [TestMethod]
        public async Task Should_UseEmptyExtension_And_Parameters()
        {
            using (var httpManager = new MockHttpManager())
            {
                ConfidentialClientApplication app =
                    ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithHttpManager(httpManager)
                                                              .WithExperimentalFeatures(true)
                                                              .BuildConcrete();

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri(ProtectedUrl));

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseWithAdditionalParamsMessage(additionalparams: string.Empty);

                MsalAuthenticationExtension authExtension = new MsalAuthenticationExtension();

                // Act
                var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                    .WithTenantId(TestConstants.Utid)
                    .WithAuthenticationExtension(authExtension)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                var expectedAt = "header.payload.signature";
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.IsFalse(result.AdditionalResponseParameters.Any());
                Assert.AreEqual(expectedAt, result.AccessToken);

                //Verify that the original AT token is cached and the extension is reused
                result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                    .WithTenantId(TestConstants.Utid)
                    .WithAuthenticationExtension(authExtension)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                Assert.IsTrue(result.AdditionalResponseParameters == null);
                Assert.AreEqual(expectedAt, result.AccessToken);
            }
        }
    }
}
