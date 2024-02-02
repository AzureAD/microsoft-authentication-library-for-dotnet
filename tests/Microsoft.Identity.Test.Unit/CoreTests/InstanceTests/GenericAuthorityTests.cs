// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Utils;
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
        public async Task ClientCredential_Success_Async(bool includeScopeInResponse)
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
                    CreateOidcHttpHandler(authority + @"/" + Constants.WellKnownOpenIdConfigurationPath));

                httpManager.AddMockHandler(
                    CreateTokenResponseHttpHandler(
                        authority + "/connect/token",
                        scopesInRequest: "api",
                        scopesInResponse: includeScopeInResponse ? "api" : null, 
                        grant: "client_credentials"));

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
      
        [TestMethod]
        public async Task UserAuth_HappyPath_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                string authority = "https://demo.duendesoftware.com";
                var requestedScopes = new[] { "all:catchreport", "email" };

                var cca = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithExperimentalFeatures(true)
                    .WithRedirectUri("http://some_redirect_uri")
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(httpManager)
                    .WithLegacyCacheCompatibility(false)
                    .WithGenericAuthority(authority)
                    .Build();

                httpManager.AddMockHandler(
                    CreateOidcHttpHandler(authority + @"/" + Constants.WellKnownOpenIdConfigurationPath));

                httpManager.AddMockHandler(
                     CreateTokenResponseHttpHandler(
                         authority + "/connect/token",
                         scopesInRequest: string.Join(" ", requestedScopes),
                         scopesInResponse: "openid profile email all:catchreport offline_access",
                         grant: "authorization_code"));

                Debug.WriteLine("Scopes returned: openid profile email all:catchreport offline_access");
                var result = await cca.AcquireTokenByAuthorizationCode(requestedScopes, "auth_code")
                                        .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                var result2 = await cca.AcquireTokenSilent(requestedScopes, result.Account)                
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TokenSource.Cache, result2.AuthenticationResultMetadata.TokenSource);

                CoreAssert.AreAccountsEqual(
                    TestConstants.Email,
                    (new Uri(authority)).Host,
                    "sub",
                    null,
                    "sub",
                    result.Account, 
                    result2.Account);

            }
        }

        // Test for https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4474
        [TestMethod]
        public async Task UserAuth_ScopeOrder_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                string authority = "https://demo.duendesoftware.com";
                var requestedScopes = new[] { "all:catchreport", "email" };

                var cca = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithExperimentalFeatures(true)
                    .WithRedirectUri("http://some_redirect_uri")
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(httpManager)
                    .WithLegacyCacheCompatibility(false)
                    .WithGenericAuthority(authority)
                    .Build();

                httpManager.AddMockHandler(
                    CreateOidcHttpHandler(authority + @"/" + Constants.WellKnownOpenIdConfigurationPath));

                httpManager.AddMockHandler(
                     CreateTokenResponseHttpHandler(
                         authority + "/connect/token",
                         scopesInRequest: string.Join(" ", requestedScopes),
                         scopesInResponse: "openid profile email all:catchreport offline_access",
                         grant: "authorization_code"));

                Debug.WriteLine("Scopes returned: openid profile email all:catchreport offline_access");
                var result = await cca.AcquireTokenByAuthorizationCode(requestedScopes, "auth_code")
                                        .ExecuteAsync().ConfigureAwait(false);

#pragma warning disable CS0618 // Type or member is obsolete
                var accounts = (await cca.GetAccountsAsync().ConfigureAwait(false));
#pragma warning restore CS0618 // Type or member is obsolete

                var account1 = accounts.First();
                Assert.AreEqual("sub", account1.HomeAccountId.Identifier);
                Assert.AreEqual(null, account1.HomeAccountId.TenantId);

                // This is because of how we've done it in ADFS. Probably doesn't matter what value is used here, as it is not defined for non-Microsoft.
                Assert.AreEqual("sub", account1.HomeAccountId.ObjectId);

                // the account id is based only on the sub claim
                var account2 = await cca.GetAccountAsync("sub").ConfigureAwait(false);

                CoreAssert.AreAccountsEqual(
                    TestConstants.Email,
                    (new Uri(authority)).Host,
                    "sub",
                    null,
                    "sub",
                    account1,
                    account2,
                    result.Account);

                Debug.WriteLine("STS scopes returned in a different order ");
                httpManager.AddMockHandler(
                    CreateTokenResponseHttpHandler(
                        authority + "/connect/token",
                        scopesInRequest: string.Join(" ", requestedScopes),
                        scopesInResponse: "email openid profile all:catchreport offline_access", // this is different from above!
                        grant: "refresh_token"));

                var result2 = await cca.AcquireTokenSilent(requestedScopes, result.Account)
                    .WithForceRefresh(true)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, result2.AuthenticationResultMetadata.TokenSource);

                Debug.WriteLine("This would result in multiple matching tokens error");
                var result3 = await cca.AcquireTokenSilent(requestedScopes, result.Account)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TokenSource.Cache, result3.AuthenticationResultMetadata.TokenSource);
            }
        }

        [DataTestMethod]
        [DataRow("https://demo.duend esoftware.com")]
        [ExpectedException(typeof(ArgumentException))]
        public void MalformedAuthority_ThrowsException(string malformedAuthority)
        {
            // Tenant and authority modifiers
            ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithExperimentalFeatures()
                .WithGenericAuthority(malformedAuthority)
                .WithClientSecret(TestConstants.ClientSecret)
                .Build();
        }

        [DataTestMethod]
        [DataRow("oidc_response_not_json")]
        [DataRow("oidc_response_http_error")]
        public async Task BadOidcResponse_ThrowsException_Async(string badOidcResponseType)
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

                string oidcEndpoint = authority + @"/" + Constants.WellKnownOpenIdConfigurationPath;

                HttpResponseMessage responseMessage;
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
            string scopesInResponse, 
            string grant)
        {
            IDictionary<string, string> expectedRequestBody = new Dictionary<string, string>
            {
                { "scope", scopesInRequest },
                { "grant_type", grant },
                { "client_id", TestConstants.ClientId },
                { "client_secret", TestConstants.ClientSecret }
            };

            string responseWithScopes = @"{ ""access_token"":""secret"", ""expires_in"":3600, ""token_type"":""Bearer"", ""scope"":""scope_placeholder"" }";
            string responseWithoutScopes = @"{ ""access_token"":""secret"", ""expires_in"":3600, ""token_type"":""Bearer"" }";

            string response = scopesInResponse != null ?
                responseWithScopes.Replace("scope_placeholder", scopesInResponse)
                : responseWithoutScopes;

            // user flows have ID token - inject it into the response
            if (grant != "client_credentials")
            {
                response = response.Insert(response.Length - 1, ",\"id_token\":\"" + CreateIdToken() + "\"");

                // insert a fake refresh token with key refresh_token
                response = response.Insert(response.Length - 1, ",\"refresh_token\":\"secret\"");

                expectedRequestBody["scope"] = scopesInRequest + " openid profile offline_access";
            }

            return new MockHttpMessageHandler()
            {
                ExpectedUrl = tokenEndpoint,
                ExpectedMethod = HttpMethod.Post,
                ExpectedPostData = expectedRequestBody,
                ResponseMessage = MockHelpers.CreateSuccessResponseMessage(response)
            };
        }        

        private static string CreateIdToken()
        {
            // as per https://openid.net/specs/openid-connect-core-1_0.html#StandardClaims
            // but not all claims are required. Adding only a few that MSAL uses.
            string id = "{\"aud\": \"e854a4a7-6c34-449c-b237-fc7a28093d84\"," +
                       "\"iss\": \"https://demo.duendesoftware.com\"," +
                       "\"iat\": 1455833828," +
                       "\"exp\": 1455837728," +
                       "\"name\": \""+ TestConstants.Email + "\"," +
                       "\"email\": \""+ TestConstants.Email + "\"," +
                       "\"sub\": \"sub\"" +
                "}";

            return string.Format(CultureInfo.InvariantCulture, "someheader.{0}.somesignature", Base64UrlHelpers.Encode(id));
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
