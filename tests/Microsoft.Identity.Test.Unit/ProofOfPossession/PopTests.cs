// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if !NET6_0
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.AuthScheme.PoP;
#if !NET6_WIN && !NET6_0
using Microsoft.Identity.Client.Broker;
#endif

#if NET6_WIN
using Microsoft.Identity.Client.Platforms.Features.RuntimeBroker;
#endif
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Common.Mocks;
using Microsoft.Identity.Test.Unit.BrokerTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.Pop
{

    [TestClass]
    public class PopTests : TestBase
    {
        private const string ProtectedUrl = "https://www.contoso.com/path1/path2?queryParam1=a&queryParam2=b";
        private const string ProtectedUrlWithPort = "https://www.contoso.com:5555/path1/path2?queryParam1=a&queryParam2=b";
        private const string CustomNonce = "my_nonce";

        [TestCleanup]
        public override void TestCleanup()
        {
            PoPProviderFactory.Reset();
            base.TestCleanup();
        }

        [TestMethod]
        public async Task POP_ShrValidation_Async()
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
                var popConfig = new PoPAuthenticationConfiguration(request);
                var provider = PoPProviderFactory.GetOrCreateProvider();

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(tokenType: "pop");

                // Act
                var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                    .WithTenantId(TestConstants.Utid)
                    .WithProofOfPossession(popConfig)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // access token parsing can be done with MSAL's id token parsing logic
                var claims = IdToken.Parse(result.AccessToken).ClaimsPrincipal;

                Assert.IsTrue(!string.IsNullOrEmpty(claims.FindAll("nonce").Single().Value));
                AssertSingedHttpRequestClaims(provider, claims);
            }
        }

        [TestMethod]
        public async Task POP_NoHttpRequest_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                ConfidentialClientApplication app =
                    ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithHttpManager(httpManager)
                                                              .WithExperimentalFeatures(true)
                                                              .BuildConcrete();

                // no HTTP method binding, but custom nonce
                var popConfig = new PoPAuthenticationConfiguration() { Nonce = CustomNonce };
                var provider = PoPProviderFactory.GetOrCreateProvider();

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(tokenType: "pop");

                // Act
                var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                    .WithTenantId(TestConstants.Utid)
                    .WithProofOfPossession(popConfig)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // access token parsing can be done with MSAL's id token parsing logic
                var claims = IdToken.Parse(result.AccessToken).ClaimsPrincipal;

                Assert.AreEqual(CustomNonce, claims.FindAll("nonce").Single().Value);
                AssertTsAndJwkClaims(provider, claims);

                Assert.IsFalse(claims.FindAll("m").Any());
                Assert.IsFalse(claims.FindAll("u").Any());
                Assert.IsFalse(claims.FindAll("p").Any());
            }
        }

        [TestMethod]
        public async Task POP_WithCustomNonce_Async()
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
                var popConfig = new PoPAuthenticationConfiguration(request) { Nonce = CustomNonce };
                var provider = PoPProviderFactory.GetOrCreateProvider();

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(tokenType: "pop");

                var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                    .WithTenantId(TestConstants.Utid)
                    .WithProofOfPossession(popConfig)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // access token parsing can be done with MSAL's id token parsing logic
                var claims = IdToken.Parse(result.AccessToken).ClaimsPrincipal;

                Assert.AreEqual(CustomNonce, claims.FindAll("nonce").Single().Value);
                AssertSingedHttpRequestClaims(provider, claims);
            }
        }

        [TestMethod]
        public async Task POP_WithMissingNonceForPCA_Async()
        {
            using (var httpManager = new MockHttpManager())
            {

                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithHttpManager(httpManager)
                                                              .WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows))
                                                              .BuildConcrete();

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri(ProtectedUrl));
                var popConfig = new PoPAuthenticationConfiguration(request);
                var provider = PoPProviderFactory.GetOrCreateProvider();

                await AssertException.TaskThrowsAsync<ArgumentNullException>(() =>
                                    app.AcquireTokenInteractive(TestConstants.s_scope.ToArray())
                                    .WithTenantId(TestConstants.Utid)
                                    .WithProofOfPossession(null, HttpMethod.Get, new Uri(app.Authority))
                                    .ExecuteAsync())
                                    .ConfigureAwait(false);

                await AssertException.TaskThrowsAsync<ArgumentNullException>(() =>
                                    app.AcquireTokenSilent(TestConstants.s_scope.ToArray(), "loginHint")
                                    .WithTenantId(TestConstants.Utid)
                                    .WithProofOfPossession(null, HttpMethod.Get, new Uri(app.Authority))
                                    .ExecuteAsync())
                                    .ConfigureAwait(false);
            }
        }

        [TestMethod]
        public void PopConfig()
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri(ProtectedUrl));
            var popConfig = new PoPAuthenticationConfiguration(request);

            Assert.AreEqual(HttpMethod.Get, popConfig.HttpMethod);
            Assert.AreEqual("www.contoso.com", popConfig.HttpHost);
            Assert.AreEqual("/path1/path2", popConfig.HttpPath);

            request = new HttpRequestMessage(HttpMethod.Post, new Uri(ProtectedUrlWithPort));
            popConfig = new PoPAuthenticationConfiguration(request);

            Assert.AreEqual(HttpMethod.Post, popConfig.HttpMethod);
            Assert.AreEqual("www.contoso.com:5555", popConfig.HttpHost);
            Assert.AreEqual("/path1/path2", popConfig.HttpPath);
        }

        [TestMethod]
        public async Task CacheKey_Includes_POPKid_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                ConfidentialClientApplication app =
                    ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithHttpManager(httpManager)
                                                              .WithExperimentalFeatures(true)
                                                              .BuildConcrete();
                var testTimeService = new TestTimeService();
                PoPProviderFactory.TimeService = testTimeService;

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri(ProtectedUrl));
                var popConfig = new PoPAuthenticationConfiguration(request);
                var cacheAccess = app.AppTokenCache.RecordAccess();

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(tokenType: "pop");

                // Act
                Trace.WriteLine("1. AcquireTokenForClient ");
                var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                    .WithTenantId(TestConstants.Utid)
                    .WithProofOfPossession(popConfig)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                string expectedKid = GetKidFromJwk(PoPProviderFactory.GetOrCreateProvider().CannonicalPublicKeyJwk);
                string actualCacheKey = cacheAccess.LastBeforeAccessNotificationArgs.SuggestedCacheKey;
                Assert.AreEqual(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}{1}_{2}_AppTokenCache",
                        expectedKid,
                        TestConstants.ClientId,
                        TestConstants.Utid),
                    actualCacheKey);

                // Arrange - force a new key by moving to the future
                (PoPProviderFactory.TimeService as TestTimeService).MoveToFuture(
                    PoPProviderFactory.KeyRotationInterval.Add(TimeSpan.FromMinutes(10)));

                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(tokenType: "pop");

                // Act
                Trace.WriteLine("1. AcquireTokenForClient again, after time passes - expect POP key rotation");
                result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                   .WithTenantId(TestConstants.Utid)
                   .WithProofOfPossession(popConfig)
                   .ExecuteAsync()
                   .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                string expectedKid2 = GetKidFromJwk(PoPProviderFactory.GetOrCreateProvider().CannonicalPublicKeyJwk);
                string actualCacheKey2 = cacheAccess.LastBeforeAccessNotificationArgs.SuggestedCacheKey;
                Assert.AreEqual(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}{1}_{2}_AppTokenCache",
                        expectedKid2,
                        TestConstants.ClientId,
                        TestConstants.Utid),
                    actualCacheKey2);

                Assert.AreNotEqual(actualCacheKey, actualCacheKey2);
            }
        }

        [TestMethod]
        public async Task PopWhenBrokerIsNotAvailableTest_Async()
        {
            //MSAL should not fall back to using the browser if the broker is not available when using POP
            // Arrange
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                var mockBroker = Substitute.For<IBroker>();
                mockBroker.IsBrokerInstalledAndInvokable(AuthorityType.Aad).Returns(false);
                mockBroker.IsPopSupported.Returns(true);

                var pcaBuilder = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithTestBroker(mockBroker)
                    .WithHttpManager(harness.HttpManager);

                pcaBuilder = pcaBuilder.WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows));

                PublicClientApplication pca = pcaBuilder.BuildConcrete();

                pca.ServiceBundle.Config.BrokerCreatorFunc = (_, _, _) => mockBroker;

                pca.ServiceBundle.ConfigureMockWebUI();

                // Act
                var exception = await AssertException.TaskThrowsAsync<MsalClientException>(async () =>
                {
                    await pca.AcquireTokenInteractive(TestConstants.s_graphScopes)
                             .WithProofOfPossession(TestConstants.Nonce, HttpMethod.Get, new Uri(TestConstants.AuthorityCommonTenant))
                             .ExecuteAsync()
                             .ConfigureAwait(false);
                }).ConfigureAwait(false);

                Assert.AreEqual(MsalError.BrokerApplicationRequired, exception.ErrorCode);
                Assert.AreEqual(MsalErrorMessage.CannotInvokeBrokerForPop, exception.Message);
            }
        }

        [TestMethod]
        public async Task PopWhenBrokerDoesNotSupportPop_Async()
        {
            // Arrange
            var mockBroker = Substitute.For<IBroker>();
            mockBroker.IsBrokerInstalledAndInvokable(AuthorityType.Aad).Returns(true);
            mockBroker.IsPopSupported.Returns(false);

            var pcaBuilder = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithTestBroker(mockBroker);

            pcaBuilder = pcaBuilder.WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows));

            PublicClientApplication pca = pcaBuilder.BuildConcrete();

            pca.ServiceBundle.Config.BrokerCreatorFunc = (_, _, _) => mockBroker;

            // Act
            MsalClientException ex = await AssertException.TaskThrowsAsync<MsalClientException>(async () =>
                    await pca.AcquireTokenInteractive(TestConstants.s_graphScopes)
                        .WithProofOfPossession(TestConstants.Nonce, HttpMethod.Get, new Uri(TestConstants.AuthorityCommonTenant))
                        .ExecuteAsync()
                        .ConfigureAwait(false))
                .ConfigureAwait(false);

            Assert.AreEqual(MsalError.BrokerDoesNotSupportPop, ex.ErrorCode);
            Assert.AreEqual(MsalErrorMessage.BrokerDoesNotSupportPop, ex.Message);
        }

        [TestMethod]
        public async Task PopWhithAdfsUserAndBroker_Async()
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                var mockBroker = Substitute.For<IBroker>();
                mockBroker.IsBrokerInstalledAndInvokable(AuthorityType.Aad).Returns(true);
                mockBroker.IsPopSupported.Returns(true);
                mockBroker.AcquireTokenSilentAsync(Arg.Any<AuthenticationRequestParameters>(), Arg.Any<AcquireTokenSilentParameters>()).Returns(CreateMsalPopTokenResponse());

                var pcaBuilder = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithAdfsAuthority(TestConstants.ADFSAuthority, false)
                    .WithTestBroker(mockBroker)
                    .WithHttpManager(harness.HttpManager);

                pcaBuilder = pcaBuilder.WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows));

                PublicClientApplication pca = pcaBuilder.BuildConcrete();

                TokenCacheHelper.PopulateCache(accessor: pca.UserTokenCacheInternal.Accessor,
                                               environment: "fs.msidlab8.com");
                TokenCacheHelper.ExpireAllAccessTokens(pca.UserTokenCacheInternal);

                pca.ServiceBundle.Config.BrokerCreatorFunc = (_, _, _) => mockBroker;

                // Act
                MsalClientException ex = await AssertException.TaskThrowsAsync<MsalClientException>(async () =>
                await pca.AcquireTokenSilent(TestConstants.s_graphScopes, TestConstants.DisplayableId)
                    .WithProofOfPossession(TestConstants.Nonce, HttpMethod.Get, new Uri(TestConstants.AuthorityCommonTenant))
                    .ExecuteAsync()
                    .ConfigureAwait(false)).ConfigureAwait(false);

                //Assert
                Assert.AreEqual(MsalError.BrokerApplicationRequired, ex.ErrorCode);
                Assert.AreEqual(MsalErrorMessage.CannotInvokeBrokerForPop, ex.Message);
            }
        }

        [TestMethod]
        public async Task EnsurePopTokenIsNotDoubleWrapped_Async()
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                var mockBroker = Substitute.For<IBroker>();
                mockBroker.IsBrokerInstalledAndInvokable(AuthorityType.Aad).Returns(true);
                mockBroker.IsPopSupported.Returns(true);
                mockBroker.AcquireTokenSilentAsync(
                    Arg.Any<AuthenticationRequestParameters>(),
                    Arg.Any<AcquireTokenSilentParameters>()).Returns(
                        MockHelpers.CreateMsalRunTimeBrokerTokenResponse(null, Constants.PoPAuthHeaderPrefix));

                var pcaBuilder = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithTestBroker(mockBroker)
                    .WithHttpManager(harness.HttpManager);

                pcaBuilder = pcaBuilder.WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows));

                PublicClientApplication pca = pcaBuilder.BuildConcrete();

                TokenCacheHelper.PopulateCache(pca.UserTokenCacheInternal.Accessor);
                TokenCacheHelper.ExpireAllAccessTokens(pca.UserTokenCacheInternal);

                pca.ServiceBundle.Config.BrokerCreatorFunc = (_, _, _) => mockBroker;

                // Act
                var result = await pca.AcquireTokenSilent(TestConstants.s_graphScopes, TestConstants.DisplayableId)
                    .WithProofOfPossession(TestConstants.Nonce, HttpMethod.Get, new Uri(TestConstants.AuthorityCommonTenant))
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                //Assert
                //Validate that access token from broker is not wrapped
                Assert.AreEqual(TestConstants.UserAccessToken, result.AccessToken);
                Assert.AreEqual(Constants.PoPAuthHeaderPrefix, result.TokenType);
                Assert.AreEqual(TokenSource.Broker, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public async Task EnsurePopTokenIsNotretrievedFromLocalCache_Async()
        {
            // Arrange
            var brokerAccessToken = "TokenFromBroker";

            var mockBroker = Substitute.For<IBroker>();
            mockBroker.IsBrokerInstalledAndInvokable(AuthorityType.Aad).Returns(true);
            mockBroker.IsPopSupported.Returns(true);
            mockBroker.AcquireTokenSilentAsync(
                Arg.Any<AuthenticationRequestParameters>(),
                Arg.Any<AcquireTokenSilentParameters>()).Returns(CreateMsalPopTokenResponse(brokerAccessToken));

            var pcaBuilder = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                            .WithTestBroker(mockBroker)
                            .WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows));

            PublicClientApplication pca = pcaBuilder.BuildConcrete();

            //Populate local cache with token
            TokenCacheHelper.PopulateCache(pca.UserTokenCacheInternal.Accessor);

            pca.ServiceBundle.Config.BrokerCreatorFunc = (_, _, _) => mockBroker;

            // Act
            var accounts = await pca.GetAccountsAsync().ConfigureAwait(false);
            var result = await pca.AcquireTokenSilent(TestConstants.s_graphScopes, accounts.FirstOrDefault())
                .WithProofOfPossession(TestConstants.Nonce, HttpMethod.Get, new Uri(TestConstants.AuthorityCommonTenant))
                .ExecuteAsync()
                .ConfigureAwait(false);

            //Assert
            //Validate that access token from broker and not local cache
            Assert.AreEqual(brokerAccessToken, result.AccessToken);
            Assert.AreEqual(Constants.PoPAuthHeaderPrefix, result.TokenType);
            Assert.AreEqual(TokenSource.Broker, result.AuthenticationResultMetadata.TokenSource);
        }

        private MsalTokenResponse CreateMsalPopTokenResponse(string accessToken = null)
        {
            return new MsalTokenResponse()
            {
                AccessToken = accessToken ?? TestConstants.UserAccessToken,
                IdToken = null,
                CorrelationId = null,
                Scope = TestConstants.ScopeStr,
                ExpiresIn = 3600,
                ClientInfo = null,
                TokenType = Constants.PoPAuthHeaderPrefix,
                WamAccountId = TestConstants.LocalAccountId,
                TokenSource = TokenSource.Broker
            };
        }

        [TestMethod]
        public async Task PopWhenBrokerIsNotEnabledForATS_Async()
        {
            // Arrange
            var pca = PublicClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithExperimentalFeatures()
                .BuildConcrete();

            // Act

            MsalClientException ex = await AssertException.TaskThrowsAsync<MsalClientException>(async () =>
                    await pca.AcquireTokenSilent(TestConstants.s_graphScopes, TestConstants.LocalAccountId)
                        .WithProofOfPossession(TestConstants.Nonce, HttpMethod.Get, new Uri(TestConstants.AuthorityCommonTenant))
                        .ExecuteAsync()
                        .ConfigureAwait(false))
                .ConfigureAwait(false);

            Assert.AreEqual(MsalError.BrokerRequiredForPop, ex.ErrorCode);
            Assert.AreEqual(MsalErrorMessage.BrokerRequiredForPop, ex.Message);
        }

#if NET_CORE
        [TestMethod]
        public void CheckPopRuntimeBrokerSupportTest()
        {
            //Broker enabled
            var pcaBuilder = PublicClientApplicationBuilder
                                            .Create(TestConstants.ClientId);

            pcaBuilder = pcaBuilder.WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows));

            IPublicClientApplication app = pcaBuilder.Build();

            Assert.IsTrue(app.IsProofOfPossessionSupportedByClient());

            //Broker disabled
            pcaBuilder = PublicClientApplicationBuilder
                                .Create(TestConstants.ClientId);

#if NET6_WIN
            pcaBuilder.WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows));
#else
            pcaBuilder.WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.None));
#endif

            app = pcaBuilder.Build();

            Assert.IsFalse(app.IsProofOfPossessionSupportedByClient());

            //Broker not configured
            app = PublicClientApplicationBuilder
                                .Create(TestConstants.ClientId)
                                .Build();

            Assert.IsFalse(app.IsProofOfPossessionSupportedByClient());
        }
#endif

        /// <summary>
        /// A key ID that uniquely describes a public / private key pair. While KeyID is not normally
        /// strict, AAD support for PoP requires that we use the base64 encoded JWK thumbprint, as described by 
        /// https://tools.ietf.org/html/rfc7638
        /// </summary>
        private string GetKidFromJwk(string jwk)
        {

            using (SHA256 hash = SHA256.Create())
            {
                byte[] hashBytes = hash.ComputeHash(Encoding.UTF8.GetBytes(jwk));
                return Base64UrlHelpers.Encode(hashBytes);
            }
        }

        private static void AssertSingedHttpRequestClaims(IPoPCryptoProvider popCryptoProvider, System.Security.Claims.ClaimsPrincipal claims)
        {
            Assert.AreEqual("GET", claims.FindAll("m").Single().Value);
            Assert.AreEqual("www.contoso.com", claims.FindAll("u").Single().Value);
            Assert.AreEqual("/path1/path2", claims.FindAll("p").Single().Value);

            AssertTsAndJwkClaims(popCryptoProvider, claims);
        }

        private static void AssertTsAndJwkClaims(IPoPCryptoProvider popCryptoProvider, System.Security.Claims.ClaimsPrincipal claims)
        {
            long ts = long.Parse(claims.FindAll("ts").Single().Value);
            CoreAssert.IsWithinRange(DateTimeOffset.UtcNow, DateTimeHelpers.UnixTimestampToDateTime(ts), TimeSpan.FromSeconds(5));

            string jwkClaim = claims.FindAll("cnf").Single().Value;
            JToken publicKey = JToken.Parse(popCryptoProvider.CannonicalPublicKeyJwk);
            JObject jwkInConfig = new JObject(new JProperty(PoPClaimTypes.JWK, publicKey));
            var jwkInToken = JObject.Parse(jwkClaim);

            Assert.IsTrue(JObject.DeepEquals(jwkInConfig, jwkInToken));
        }

       
    }
}
#endif
