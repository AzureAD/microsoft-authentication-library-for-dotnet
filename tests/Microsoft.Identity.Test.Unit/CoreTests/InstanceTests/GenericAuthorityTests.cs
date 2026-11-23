// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
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
        [TestMethod]
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
                    .WithOidcAuthority(authority)
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
        [DataRow("HighClient", "highclient", false)]
        [DataRow("highclient", "HighClient", true)]
        public async Task CaseDistinctClients_SharedUserCache_RemainsIsolatedAsync(
            string firstClientId, string secondClientId, bool isolatedReads)
        {
            // Arrange
            using var httpManager = new MockHttpManager();
            const string authority = "https://demo.duendesoftware.com";
            var scopes = new[] { "api" };
            byte[] sharedCache = null;
            var firstApp = CreateApp(firstClientId);
            var secondApp = CreateApp(secondClientId);
            httpManager.AddMockHandler(CreateOidcHttpHandler(authority + "/" + Constants.WellKnownOpenIdConfigurationPath));
            AddTokenResponse(firstClientId, "authorization_code", "first-token");

            // Act
            var firstResult = await firstApp.AcquireTokenByAuthorizationCode(scopes, "first-code").ExecuteAsync().ConfigureAwait(false);

            // Assert
            AssertCache("first-token");
            if (!isolatedReads)
            {
                var json = JsonNode.Parse(Encoding.UTF8.GetString(sharedCache)).AsObject();
                foreach (string type in new[] { "AccessToken", "RefreshToken", "IdToken", "AppMetadata" })
                {
                    if (json[type] is JsonObject items)
                    {
                        foreach (var entry in items.ToArray())
                        {
                            items.Remove(entry.Key);
                            items[entry.Key.ToLowerInvariant()] = entry.Value;
                        }
                    }
                }
                sharedCache = Encoding.UTF8.GetBytes(json.ToJsonString());
            }

            await Assert.ThrowsAsync<MsalUiRequiredException>(
                () => secondApp.AcquireTokenSilent(scopes, firstResult.Account).ExecuteAsync()).ConfigureAwait(false);
            Assert.IsNull(await secondApp.GetAccountAsync(firstResult.Account.HomeAccountId.Identifier).ConfigureAwait(false));
            var publicApp = PublicClientApplicationBuilder.Create(secondClientId)
                .WithExperimentalFeatures()
                .WithHttpManager(httpManager)
                .WithOidcAuthority(authority)
                .Build();
            publicApp.UserTokenCache.SetBeforeAccess(args =>
                args.TokenCache.DeserializeMsalV3(sharedCache, shouldClearExistingCache: true));
            Assert.IsNull(await publicApp.GetAccountAsync(firstResult.Account.HomeAccountId.Identifier).ConfigureAwait(false));

            AddTokenResponse(secondClientId, "authorization_code", "second-token");
            var secondResult = await secondApp.AcquireTokenByAuthorizationCode(scopes, "second-code").ExecuteAsync().ConfigureAwait(false);
            AssertCache("first-token", "second-token");

            var cachedFirst = await firstApp.AcquireTokenSilent(scopes, firstResult.Account).ExecuteAsync().ConfigureAwait(false);
            var cachedSecond = await secondApp.AcquireTokenSilent(scopes, secondResult.Account).ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual("first-token", cachedFirst.AccessToken);
            Assert.AreEqual("second-token", cachedSecond.AccessToken);
            Assert.AreEqual(TokenSource.Cache, cachedFirst.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(TokenSource.Cache, cachedSecond.AuthenticationResultMetadata.TokenSource);

            var refresh = AddTokenResponse(secondClientId, "refresh_token", "refreshed-second-token");
            refresh.ExpectedPostData["refresh_token"] = "refresh-" + secondClientId;
            var refreshed = await secondApp.AcquireTokenSilent(scopes, secondResult.Account)
                .WithForceRefresh(true).ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual("refreshed-second-token", refreshed.AccessToken);
            AssertCache("first-token", "refreshed-second-token");

            var beforeRemoval = CacheSerializationContract.FromJsonString(Encoding.UTF8.GetString(sharedCache));
            var familyToken = new MsalRefreshTokenCacheItem(new Uri(authority).Host, "family-client",
                "family-refresh", null, "1", firstResult.Account.HomeAccountId.Identifier);
            beforeRemoval.RefreshTokens[familyToken.CacheKey] = familyToken;
            sharedCache = Encoding.UTF8.GetBytes(beforeRemoval.ToJsonString());
            await secondApp.RemoveAsync(secondResult.Account).ConfigureAwait(false);
            AssertCache("first-token");
            Assert.AreEqual("family-refresh", CacheSerializationContract.FromJsonString(Encoding.UTF8.GetString(sharedCache))
                .RefreshTokens.Values.Single(item => item.IsFRT).Secret);
            cachedFirst = await firstApp.AcquireTokenSilent(scopes, firstResult.Account).ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual("first-token", cachedFirst.AccessToken);
            Assert.AreEqual(TokenSource.Cache, cachedFirst.AuthenticationResultMetadata.TokenSource);

            IConfidentialClientApplication CreateApp(string clientId)
            {
                var app = ConfidentialClientApplicationBuilder.Create(clientId)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithRedirectUri("http://some_redirect_uri")
                    .WithHttpManager(httpManager)
                    .WithLegacyCacheCompatibility(!isolatedReads)
                    .WithOidcAuthority(authority)
                    .Build();
                app.UserTokenCache.SetBeforeAccess(args =>
                    args.TokenCache.DeserializeMsalV3(sharedCache, shouldClearExistingCache: true));
                app.UserTokenCache.SetAfterAccess(args =>
                {
                    if (args.HasStateChanged)
                    {
                        sharedCache = args.TokenCache.SerializeMsalV3();
                    }
                });
                return app;
            }

            MockHttpMessageHandler AddTokenResponse(string clientId, string grant, string accessToken)
            {
                var handler = CreateTokenResponseHttpHandler(authority + "/connect/token", "api",
                    "api openid profile offline_access", grant, clientId, accessToken, "refresh-" + clientId);
                httpManager.AddMockHandler(handler);
                return handler;
            }

            void AssertCache(params string[] accessTokens)
            {
                var cache = CacheSerializationContract.FromJsonString(Encoding.UTF8.GetString(sharedCache));
                CollectionAssert.AreEquivalent(accessTokens, cache.AccessTokens.Values.Select(item => item.Secret).ToArray());
                var ordinaryRefreshTokens = cache.RefreshTokens.Values.Where(item => !item.IsFRT).ToArray();
                Assert.HasCount(accessTokens.Length, ordinaryRefreshTokens);
                Assert.HasCount(accessTokens.Length, cache.IdTokens);
                Assert.AreEqual(firstClientId, cache.AccessTokens.Values.Single(item => item.Secret == "first-token").ClientId);
                if (accessTokens.Length == 2)
                {
                    CollectionAssert.AreEquivalent(new[] { firstClientId, secondClientId },
                        ordinaryRefreshTokens.Select(item => item.ClientId).ToArray());
                    CollectionAssert.AreEquivalent(new[] { firstClientId, secondClientId },
                        cache.IdTokens.Values.Select(item => item.ClientId).ToArray());
                }
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
                    .WithRedirectUri("http://some_redirect_uri")
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(httpManager)
                    .WithLegacyCacheCompatibility(false)
                    .WithOidcAuthority(authority)
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

        [TestMethod]
        public async Task ExtraQueryParametersArePresevedTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                const string QP = "key1=val1&key2=val2";
                const string Authority = "https://demo.duendesoftware.com";
                const string AuthorityWithQP = $"{Authority}?{QP}";

                var requestedScopes = new[] { "all:catchreport", "email" };

                var cca = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithRedirectUri("http://some_redirect_uri")
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(httpManager)
                    .WithLegacyCacheCompatibility(false)
                    .WithOidcAuthority(AuthorityWithQP)
                    .Build();

                string oidcDocument = TestConstants.GenericOidcResponse;
                oidcDocument = oidcDocument.Replace(
                    "https://demo.duendesoftware.com/connect/authorize", 
                    $"https://demo.duendesoftware.com/connect/authorize?{QP}");

                oidcDocument = oidcDocument.Replace(
                   "https://demo.duendesoftware.com/connect/token",
                   $"https://demo.duendesoftware.com/connect/token?{QP}");

                var oidcHandler = new MockHttpMessageHandler()
                {
                    ExpectedMethod = HttpMethod.Get,                    
                    ResponseMessage = MockHelpers.CreateSuccessResponseMessage(oidcDocument)
                };

                var oidcHttpResult = httpManager.AddMockHandler(oidcHandler);

                var tokenHttpResult = httpManager.AddMockHandler(
                     CreateTokenResponseHttpHandler(
                          $"{Authority}/connect/token?{QP}",  // notice the QP
                         scopesInRequest: string.Join(" ", requestedScopes),
                         scopesInResponse: "openid profile email all:catchreport offline_access",
                         grant: "authorization_code"));

                Debug.WriteLine("Scopes returned: openid profile email all:catchreport offline_access");
                var result = await cca.AcquireTokenByAuthorizationCode(requestedScopes, "auth_code")
                                        .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(
                    $"{Authority}/connect/token?{QP}", 
                    tokenHttpResult.ActualRequestMessage.RequestUri.ToString());
                Assert.AreEqual(
                    $"{Authority}/.well-known/openid-configuration?{QP}",
                    oidcHttpResult.ActualRequestMessage.RequestUri.ToString());
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
                    .WithRedirectUri("http://some_redirect_uri")
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(httpManager)
                    .WithLegacyCacheCompatibility(false)
                    .WithOidcAuthority(authority)
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
                Assert.IsNull(account1.HomeAccountId.TenantId);

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

        [TestMethod]
        [DataRow("https://demo.duend esoftware.com")]
        public void MalformedAuthority_ThrowsException(string malformedAuthority)
        {
            // Tenant and authority modifiers
            Assert.Throws<ArgumentException>(() =>
                ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithOidcAuthority(malformedAuthority)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .Build());
        }

        [TestMethod]
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
                    .WithOidcAuthority(authority)
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

        [TestMethod]
        public async Task Oidc_Malformed_Failure_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                string authority = TestConstants.CiamCUDAuthorityMalformed;
                IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithHttpManager(httpManager)
                    .WithOidcAuthority(authority)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .Build();

                httpManager.AddMockHandler(
                    CreateOidcHttpHandler($"{authority}/{Constants.WellKnownOpenIdConfigurationPath}", authority));

                httpManager.AddFailureTokenEndpointResponse(
                                error: "error",
                                AadErrorCode: Constants.AadAccountTypeAndResourceIncompatibleErrorCode,
                                expectedUrl: $"{TestConstants.CiamCUDAuthorityMalformed}/connect/token");

                Assert.AreEqual(authority, app.Authority);
                var confidentailClientApp = (ConfidentialClientApplication)app;
                Assert.AreEqual(AuthorityType.Generic, confidentailClientApp.AuthorityInfo.AuthorityType);

                var ex = await AssertException.TaskThrowsAsync<MsalServiceException>(() =>
                         app.AcquireTokenForClient(new[] { "api" })
                             .ExecuteAsync())
                             .ConfigureAwait(false);

                Assert.Contains(string.Format( CultureInfo.InvariantCulture, MsalErrorMessage.MalformedOidcAuthorityFormat, TestConstants.CiamCUDAuthorityMalformed), ex.Message);

                httpManager.AddFailureTokenEndpointResponse(
                error: "error",
                AadErrorCode: Constants.AadMissingScopeErrorCode,
                expectedUrl: $"{TestConstants.CiamCUDAuthorityMalformed}/connect/token");

                ex = await AssertException.TaskThrowsAsync<MsalServiceException>(() =>
                         app.AcquireTokenForClient(new[] { "api" })
                             .ExecuteAsync())
                             .ConfigureAwait(false);

                Assert.Contains(string.Format( CultureInfo.InvariantCulture, MsalErrorMessage.MalformedOidcAuthorityFormat, TestConstants.CiamCUDAuthorityMalformed), ex.Message);
            }
        }

        [TestMethod]
        public async Task OidcIssuerValidation_AcceptsDifferentPath_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                // This test was made to cover an issue that realistically would only happen with Microsoft authorities in multi-tenant scenarios,
                // so it uses a Microsoft host instead of the custom domain used in other tests.
                string microsoftHost = "login.microsoftonline.com";
                string authority = $"https://{microsoftHost}/organizations/2.0/";
                string issuerWithDifferentPath = $"https://{microsoftHost}/someTenant/2.0/";

                IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithHttpManager(httpManager)
                    .WithOidcAuthority(authority)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .Build();

                // Create OIDC document with a Microsoft host and an issuer that has matching host but different path
                string oidcDocumentWithDifferentPath = TestConstants.GenericOidcResponse.Replace(
                        $"\"issuer\":\"{TestConstants.GenericAuthority}\"",
                        $"\"issuer\":\"{issuerWithDifferentPath}\"");
                oidcDocumentWithDifferentPath = oidcDocumentWithDifferentPath.Replace(
                        "demo.duendesoftware.com",
                        microsoftHost);

                // Mock OIDC endpoint response
                httpManager.AddMockHandler(new MockHttpMessageHandler
                {
                    ExpectedMethod = HttpMethod.Get,
                    ExpectedUrl = $"{authority}{Constants.WellKnownOpenIdConfigurationPath}",
                    ResponseMessage = MockHelpers.CreateSuccessResponseMessage(oidcDocumentWithDifferentPath)
                });

                // Mock token endpoint response
                httpManager.AddMockHandler(
                    CreateTokenResponseHttpHandler(
                        $"https://{microsoftHost}/connect/token",
                        scopesInRequest: "api",
                        scopesInResponse: "api", 
                        grant: "client_credentials"));

                // Should not throw an exception with our updated validation
                var result = await app.AcquireTokenForClient(new[] { "api" }).ExecuteAsync().ConfigureAwait(false);
                
                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
            }
        }

        [TestMethod]
        public async Task OidcIssuerValidation_ThrowsForNonMatchingIssuer_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                string wrongIssuer = "https://wrong.issuer.com";

                IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithHttpManager(httpManager)
                    .WithOidcAuthority(TestConstants.GenericAuthority)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .Build();

                // Create OIDC document with non-matching issuer
                string validOidcDocumentWithWrongIssuer = TestConstants.GenericOidcResponse.Replace(
                        $"\"issuer\":\"{TestConstants.GenericAuthority}\"",
                        $"\"issuer\":\"{wrongIssuer}\"");

                // Mock OIDC endpoint response
                httpManager.AddMockHandler(new MockHttpMessageHandler
                {
                    ExpectedMethod = HttpMethod.Get,
                    ExpectedUrl = $"{TestConstants.GenericAuthority}/{Constants.WellKnownOpenIdConfigurationPath}",
                    ResponseMessage = MockHelpers.CreateSuccessResponseMessage(validOidcDocumentWithWrongIssuer)
                });

                var ex = await AssertException.TaskThrowsAsync<MsalServiceException>(() =>
                    app.AcquireTokenForClient(new[] { "api" }).ExecuteAsync()
                ).ConfigureAwait(false);

                string expectedErrorMessage = string.Format(MsalErrorMessage.IssuerValidationFailed, app.Authority, wrongIssuer);

                Assert.AreEqual(MsalError.AuthorityValidationFailed, ex.ErrorCode);
                Assert.AreEqual(expectedErrorMessage, ex.Message, 
                    "Error message should match the expected error message.");
            }
        }

        private static MockHttpMessageHandler CreateTokenResponseHttpHandler(
            string tokenEndpoint,
            string scopesInRequest,
            string scopesInResponse, 
            string grant,
            string clientId = TestConstants.ClientId,
            string accessToken = "secret",
            string refreshToken = "secret")
        {
            IDictionary<string, string> expectedRequestBody = new Dictionary<string, string>
            {
                { "scope", scopesInRequest },
                { "grant_type", grant },
                { "client_id", clientId },
                { "client_secret", TestConstants.ClientSecret }
            };

            string responseWithScopes = @"{ ""access_token"":""secret"", ""expires_in"":3600, ""token_type"":""Bearer"", ""scope"":""scope_placeholder"" }";
            string responseWithoutScopes = @"{ ""access_token"":""secret"", ""expires_in"":3600, ""token_type"":""Bearer"" }";

            string response = scopesInResponse != null ?
                responseWithScopes.Replace("scope_placeholder", scopesInResponse)
                : responseWithoutScopes;
            response = response.Replace("\"access_token\":\"secret\"", "\"access_token\":\"" + accessToken + "\"");

            // user flows have ID token - inject it into the response
            if (grant != "client_credentials")
            {
                response = response.Insert(response.Length - 1, ",\"id_token\":\"" + CreateIdToken() + "\"");

                // insert a fake refresh token with key refresh_token
                response = response.Insert(response.Length - 1, ",\"refresh_token\":\"" + refreshToken + "\"");

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

        private static MockHttpMessageHandler CreateOidcHttpHandler(string oidcEndpoint, string authority = null)
        {
            return new MockHttpMessageHandler()
            {
                ExpectedMethod = HttpMethod.Get,
                ExpectedUrl = oidcEndpoint,
                ResponseMessage = MockHelpers.CreateSuccessResponseMessage(TestConstants.GetOidcResponse(authority))
            };
        }
    }
}
