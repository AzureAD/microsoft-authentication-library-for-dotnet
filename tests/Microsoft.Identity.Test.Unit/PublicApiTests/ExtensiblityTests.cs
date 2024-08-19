﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Client.Extensibility;
using System.Threading;
using Microsoft.Identity.Client.AuthScheme;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class ExtensiblityTests : TestBase
    {
        internal class ExampleAuthScheme : IAuthenticationScheme
        {
            private readonly X509Certificate2 _cert; // TODO: can use in-memory keys as well, but using cert here for simplicity

            public ExampleAuthScheme(X509Certificate2 cert)
            {
                _cert = cert;
            }

            public TokenType TelemetryTokenType => TokenType.External; // TODO: needs more extensiblity, link an int

            public string AuthorizationHeaderPrefix => "CDT"; // ??

            public string KeyId => _cert.Thumbprint;

            public string AccessTokenType => "CDT"; // should match what ESTS puts on the wire in "token_type"

            public void FormatResult(AuthenticationResult authenticationResult)
            {
                // 1. create constraint token and sign with cert
                // 2. perform extra step with authenticationResult.AdditionalResponseParameters["ds_nonce"];
                // 3. create CDT token 
                // 4. update authenticationResult.AccessToken
            }

            public IReadOnlyDictionary<string, string> GetTokenRequestParams()
            {
                return new Dictionary<string, string>
                {
                    { "req_ds_cnf", "JWK" }
                };
            }
        }

        [TestMethod]
        public async Task AddInTestAsync()
        {
            

            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();
                MockHttpMessageHandler handler = httpManager.
                    AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                MsalAddIn exampleAddIn = new MsalAddIn()
                {
                    AdditionalAccessTokenPropertiesToCache = new[] { "ds_nonce", "ds_enc" },
                    // OnBeforeTokenRequestHandler not needed for this flow
                    AuthenticationScheme = new ExampleAuthScheme(default)
                };

                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                              .WithAuthority("https://login.microsoftonline.com/tid/")
                              .WithExperimentalFeatures(true)
                              .WithHttpManager(httpManager)
                              .Build();

                var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                    .WithAddIn(exampleAddIn)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

            }
        }

        [TestMethod]
        public async Task ChangeTokenUriAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                              .WithAuthority("https://login.microsoftonline.com/tid/")
                              .WithExperimentalFeatures(true)
                              .WithHttpManager(httpManager)
                              .Build();

                MockHttpMessageHandler handler = httpManager.
                    AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                    .OnBeforeTokenRequest(
                        (data) =>
                        {
                            Assert.AreEqual(
                                "https://login.microsoftonline.com/tid/oauth2/v2.0/token",
                                data.RequestUri.ToString());

                            // change the token URI
                            data.RequestUri = new Uri("https://mtlsauth.login.microsoftonline.com/tid/oauth2/v2.0/token");
                            return Task.CompletedTask;
                        })
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual(
                    "https://mtlsauth.login.microsoftonline.com/tid/oauth2/v2.0/token",                    
                    handler.ActualRequestMessage.RequestUri.ToString());
            }
        }

        [TestMethod]
        public async Task CertificateOverrideAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                              .WithAuthority("https://login.microsoftonline.com/tid/")
                              .WithExperimentalFeatures(true)
                              .WithHttpManager(httpManager)
                              .Build();

                MockHttpMessageHandler handler = httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                    .WithProofOfPosessionKeyId("key1")
                    .OnBeforeTokenRequest(ModifyRequestAsync)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual("Bearer", result.TokenType);

                Assert.AreEqual("val1", handler.ActualRequestPostData["param1"]);
                Assert.AreEqual("val2", handler.ActualRequestPostData["param2"]);
                Assert.AreEqual("hval1", handler.ActualRequestHeaders.GetValues("header1").Single());
                Assert.AreEqual("hval2", handler.ActualRequestHeaders.GetValues("header2").Single());
                Assert.IsFalse(handler.ActualRequestPostData.ContainsKey(OAuth2Parameter.ClientAssertion));
                Assert.IsFalse(handler.ActualRequestPostData.ContainsKey(OAuth2Parameter.ClientAssertionType));
                Assert.AreEqual("key1", (app.AppTokenCache as ITokenCacheInternal).Accessor.GetAllAccessTokens().Single().KeyId);

                result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                    .WithProofOfPosessionKeyId("key1")
                    .OnBeforeTokenRequest(ModifyRequestAsync)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual("Bearer", result.TokenType);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);

                Assert.AreEqual(
                    "key1",
                    (app.AppTokenCache as ITokenCacheInternal).Accessor.GetAllAccessTokens().Single().KeyId);

                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
                result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                 .OnBeforeTokenRequest(ModifyRequestAsync)
                 .ExecuteAsync()
                 .ConfigureAwait(false);

                Assert.AreEqual("Bearer", result.TokenType);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                IReadOnlyList<Client.Cache.Items.MsalAccessTokenCacheItem> ats = (app.AppTokenCache as ITokenCacheInternal).Accessor.GetAllAccessTokens();
                Assert.AreEqual(2, ats.Count);
                Assert.IsTrue(ats.Single(at => at.KeyId == "key1") != null);
                Assert.IsTrue(ats.Single(at => at.KeyId == null) != null);
            }
        }

        private static Task ModifyRequestAsync(OnBeforeTokenRequestData requestData)
        {
            Assert.AreEqual("https://login.microsoftonline.com/tid/oauth2/v2.0/token", requestData.RequestUri.AbsoluteUri);
            requestData.BodyParameters.Add("param1", "val1");
            requestData.BodyParameters.Add("param2", "val2");

            requestData.Headers.Add("header1", "hval1");
            requestData.Headers.Add("header2", "hval2");

            return Task.CompletedTask;
        }

        [TestMethod]
        public async Task ValidateAppTokenProviderAsync()
        {
            using (var harness = base.CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                bool usingClaims = false;
                string differentScopesForAt = string.Empty;
                int callbackInvoked = 0;
                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithAppTokenProvider((AppTokenProviderParameters parameters) =>
                                                              {
                                                                  Assert.IsNotNull(parameters.Scopes);
                                                                  Assert.IsNotNull(parameters.CorrelationId);
                                                                  Assert.IsNotNull(parameters.TenantId);
                                                                  Assert.IsNotNull(parameters.CancellationToken);

                                                                  if (usingClaims)
                                                                  {
                                                                      Assert.IsNotNull(parameters.Claims);
                                                                  }

                                                                  Interlocked.Increment(ref callbackInvoked);

                                                                  return Task.FromResult(GetAppTokenProviderResult(differentScopesForAt));
                                                              })
                                                              .WithHttpManager(harness.HttpManager)
                                                              .BuildConcrete();

                // AcquireToken from app provider
                AuthenticationResult result = await app.AcquireTokenForClient(TestConstants.s_scope)
                                                        .ExecuteAsync(new CancellationToken()).ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TestConstants.DefaultAccessToken, result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(1, callbackInvoked);

                var tokens = app.AppTokenCacheInternal.Accessor.GetAllAccessTokens();

                Assert.AreEqual(1, tokens.Count);

                var token = tokens.FirstOrDefault();
                Assert.IsNotNull(token);
                Assert.AreEqual(TestConstants.DefaultAccessToken, token.Secret);

                // AcquireToken from cache
                result = await app.AcquireTokenForClient(TestConstants.s_scope)
                                                        .ExecuteAsync(new CancellationToken()).ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TestConstants.DefaultAccessToken, result.AccessToken);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(1, callbackInvoked);

                // Expire token
                TokenCacheHelper.ExpireAllAccessTokens(app.AppTokenCacheInternal);

                // Acquire token from app provider with expired token
                result = await app.AcquireTokenForClient(TestConstants.s_scope)
                                                        .ExecuteAsync(new CancellationToken()).ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TestConstants.DefaultAccessToken, result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(2, callbackInvoked);

                differentScopesForAt = "new scope";

                // Acquire token from app provider with new scopes
                result = await app.AcquireTokenForClient(new[] { differentScopesForAt })
                                                        .ExecuteAsync(new CancellationToken()).ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TestConstants.DefaultAccessToken + differentScopesForAt, result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Count, 2);
                Assert.AreEqual(3, callbackInvoked);

                // Acquire token from app provider with claims. Should not use cache
                result = await app.AcquireTokenForClient(TestConstants.s_scope)
                                                        .WithClaims(TestConstants.Claims)
                                                        .ExecuteAsync(new CancellationToken()).ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TestConstants.DefaultAccessToken + differentScopesForAt, result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(4, callbackInvoked);
            }
        }

        private AppTokenProviderResult GetAppTokenProviderResult(string differentScopesForAt = "", long? refreshIn = 1000)
        {
            var token = new AppTokenProviderResult();
            token.AccessToken = TestConstants.DefaultAccessToken + differentScopesForAt; //Used to indicate that there is a new access token for a different set of scopes
            token.ExpiresInSeconds = 3600;
            token.RefreshInSeconds = refreshIn;

            return token;
        }
    }
}
