// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Instance.Oidc;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CoreTests.InstanceTests
{
    [TestClass]
    public class GenericAuthorityTests : TestBase
    {
        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        /// AAD doesn't returns the "scope" in the response
        /// Duende does return the "scope" in the response
        public async Task GenericClientCredentialSuccessfulTestAsync(bool includeScopeInResonse)
        {
            using (var httpManager = new MockHttpManager())
            {
                string authority = "https://demo.duendesoftware.com";
                IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)
                    .WithGenericAuthority(authority)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .Build();

                httpManager.AddMockHandler(
                    CreateOidcHttpHandler(authority + @"/" + OidcRetrieverWithCache.OpenIdConfigurationEndpointSuffix));

                httpManager.AddMockHandler(
                    CreateTokenResponseHttpHandler(authority + "/connect/token", "api", includeScopeInResonse ? "api" : null));

                Assert.AreEqual(authority + "/", app.Authority);
                var confidentailClientApp = (ConfidentialClientApplication)app;
                Assert.AreEqual(AuthorityType.Generic, confidentailClientApp.AuthorityInfo.AuthorityType);

                AuthenticationResult result = await app
                    .AcquireTokenForClient(new[] { "api" })
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                result = await app
                    .AcquireTokenForClient(new[] { "api" })
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [DataTestMethod]
        [DataRow("oidc_response_not_json")]
        [DataRow("oidc_response_http_error")]
        public async Task BadOidcResponseAsync(string badOidcResponseType)
        {
            using (var httpManager = new MockHttpManager())
            {
                string authority = "https://demo.duendesoftware.com";
                IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)
                    .WithGenericAuthority(authority)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .Build();

                string oidcEndpoint = authority + @"/" + OidcRetrieverWithCache.OpenIdConfigurationEndpointSuffix;

                HttpResponseMessage responseMessage = null;
                switch (badOidcResponseType)
                {
                    case "oidc_response_not_json":
                        responseMessage = MockHelpers.CreateSuccessResponseMessage("bad_response_no_json");
                        break;
                    case "oidc_response_http_error":
                        responseMessage = MockHelpers.CreateFailureMessage(System.Net.HttpStatusCode.BadRequest, "");
                        break;
                    default:
                        throw new NotImplementedException();
                }

                var oidcMock = new MockHttpMessageHandler()
                {
                    ExpectedMethod = HttpMethod.Get,
                    ExpectedUrl = oidcEndpoint,
                    ResponseMessage = responseMessage
                };

                httpManager.AddMockHandler(oidcMock);

                Assert.AreEqual(authority + "/", app.Authority);
                var confidentailClientApp = (ConfidentialClientApplication)app;
                Assert.AreEqual(AuthorityType.Generic, confidentailClientApp.AuthorityInfo.AuthorityType);

                var ex = await AssertException.TaskThrowsAsync<MsalServiceException>(() =>
                         app.AcquireTokenForClient(new[] { "api" })
                             .ExecuteAsync())
                             .ConfigureAwait(false);

                switch (badOidcResponseType)
                {
                    case "oidc_response_not_json":
                        Assert.AreEqual("oidc_failure", ex.ErrorCode);
                        break;
                    case "oidc_response_http_error":
                        Assert.AreEqual("http_status_not_200", ex.ErrorCode);
                        Assert.AreEqual(400, ex.StatusCode);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        private static MockHttpMessageHandler CreateTokenResponseHttpHandler(
            string tokenEndpoint,
            string scopesInRequest, 
            string scopesInResponse)
        {
            IDictionary<string, string> expectedRequestBody = new Dictionary<string, string>
            {
                { "scope", scopesInRequest },
                { "grant_type", "client_credentials" },
                { "client_id", TestConstants.ClientId },
                { "client_secret", TestConstants.ClientSecret }
            };

            string responseWithScopes = @"{ ""access_token"":""secret"", ""expires_in"":3600, ""token_type"":""Bearer"", ""scope"":""api"" }";
            string responseWithoutScopes = @"{ ""access_token"":""secret"", ""expires_in"":3600, ""token_type"":""Bearer"" }";

            string response = scopesInResponse == null ? 
                responseWithScopes .Replace("api", scopesInResponse)
                : responseWithoutScopes;

            return new MockHttpMessageHandler()
            {
                ExpectedUrl = tokenEndpoint,
                ExpectedMethod = HttpMethod.Post,
                ExpectedPostData = expectedRequestBody,
                ResponseMessage = MockHelpers.CreateSuccessResponseMessage(response)
            };
        }

        private static MockHttpMessageHandler CreateOidcHttpHandler(string oidcEndpoint)
        {
            return new MockHttpMessageHandler()
            {
                ExpectedMethod = HttpMethod.Get,
                ExpectedUrl = oidcEndpoint,
                ResponseMessage = MockHelpers.CreateSuccessResponseMessage(TestConstants.GenericOidcResponse)
            };
        }
    }
}
