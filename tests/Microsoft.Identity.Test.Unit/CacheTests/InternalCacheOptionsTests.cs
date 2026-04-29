// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Common.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CacheTests
{
    [TestClass]
    public class InternalCacheOptionsTests : TestBase
    {
        [TestMethod]
        public void OptionsAndExternalCacheAreExclusive()
        {
            var app =
                    ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
                                                              .Build();

            AssertExclusivity(app.UserTokenCache);
            AssertExclusivity(app.AppTokenCache);

            var app2 =
                     ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                               .WithClientSecret(TestConstants.ClientSecret)                                                               
                                                               .Build();
            app2.AppTokenCache.SetCacheOptions(CacheOptions.EnableSharedCacheOptions);
            app2.UserTokenCache.SetCacheOptions(CacheOptions.EnableSharedCacheOptions);

            AssertExclusivity(app2.AppTokenCache);
            AssertExclusivity(app2.UserTokenCache);

            var app3 =
                     ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                               .WithClientSecret(TestConstants.ClientSecret)
                                                               .Build();
            app3.UserTokenCache.SetAfterAccess((_) => { });
            app3.AppTokenCache.SetBeforeAccess((_) => { });
            var ex = AssertException.Throws<MsalClientException>(() => app3.UserTokenCache.SetCacheOptions(CacheOptions.EnableSharedCacheOptions));
            Assert.AreEqual(MsalError.StaticCacheWithExternalSerialization, ex.ErrorCode);
            ex = AssertException.Throws<MsalClientException>(() => app3.AppTokenCache.SetCacheOptions(CacheOptions.EnableSharedCacheOptions));
            Assert.AreEqual(MsalError.StaticCacheWithExternalSerialization, ex.ErrorCode);

            void AssertExclusivity(ITokenCache tokenCache)
            {
                var ex = AssertException.Throws<MsalClientException>(() => tokenCache.SetAfterAccess((_) => { }));
                Assert.AreEqual(MsalError.StaticCacheWithExternalSerialization, ex.ErrorCode);
                ex = AssertException.Throws<MsalClientException>(() => tokenCache.SetBeforeAccess((_) => { }));
                Assert.AreEqual(MsalError.StaticCacheWithExternalSerialization, ex.ErrorCode);
                ex = AssertException.Throws<MsalClientException>(() => tokenCache.SetBeforeWrite((_) => { }));
                Assert.AreEqual(MsalError.StaticCacheWithExternalSerialization, ex.ErrorCode);

                ex = AssertException.Throws<MsalClientException>(() => tokenCache.SetBeforeAccessAsync((_) => Task.CompletedTask));
                Assert.AreEqual(MsalError.StaticCacheWithExternalSerialization, ex.ErrorCode);
                ex = AssertException.Throws<MsalClientException>(() => tokenCache.SetAfterAccessAsync((_) => Task.CompletedTask));
                Assert.AreEqual(MsalError.StaticCacheWithExternalSerialization, ex.ErrorCode);
                ex = AssertException.Throws<MsalClientException>(() => tokenCache.SetBeforeWriteAsync((_) => Task.CompletedTask));
                Assert.AreEqual(MsalError.StaticCacheWithExternalSerialization, ex.ErrorCode);

            }
        }

        [TestMethod]
        public async Task ClientCreds_StaticCache_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                ConfidentialClientApplication app1 =
                    ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                        .WithClientSecret(TestConstants.ClientSecret)
                                                        .WithHttpManager(httpManager)
                                                        .WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
                                                        .BuildConcrete();

                ConfidentialClientApplication app2 =
                   ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                       .WithClientSecret(TestConstants.ClientSecret)
                                                       .WithHttpManager(httpManager)
                                                       .BuildConcrete();

                app2.AppTokenCache.SetCacheOptions(new CacheOptions(true));

                ConfidentialClientApplication app_withoutStaticCache =
                  ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                      .WithClientSecret(TestConstants.ClientSecret)
                                                      .WithHttpManager(httpManager)
                                                      .BuildConcrete();

                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
                await ClientCredsAcquireAndAssertTokenSourceAsync(app1, "S1", TokenSource.IdentityProvider, 1).ConfigureAwait(false);

                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
                await ClientCredsAcquireAndAssertTokenSourceAsync(app1, "S2", TokenSource.IdentityProvider, 2).ConfigureAwait(false);

                await ClientCredsAcquireAndAssertTokenSourceAsync(app2, "S1", TokenSource.Cache, 2).ConfigureAwait(false);
                await ClientCredsAcquireAndAssertTokenSourceAsync(app2, "S2", TokenSource.Cache, 2).ConfigureAwait(false);

                ConfidentialClientApplication app3 =
                     ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                         .WithClientSecret(TestConstants.ClientSecret)
                                                         .WithHttpManager(httpManager)
                                                         .WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
                                                         .BuildConcrete();

                await ClientCredsAcquireAndAssertTokenSourceAsync(app3, "S1", TokenSource.Cache, 2).ConfigureAwait(false);
                await ClientCredsAcquireAndAssertTokenSourceAsync(app3, "S2", TokenSource.Cache, 2).ConfigureAwait(false);
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
                await ClientCredsAcquireAndAssertTokenSourceAsync(app3, "S3", TokenSource.IdentityProvider, 3).ConfigureAwait(false);

                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
                await ClientCredsAcquireAndAssertTokenSourceAsync(app_withoutStaticCache, "S1", TokenSource.IdentityProvider, 1).ConfigureAwait(false);

                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
                await ClientCredsAcquireAndAssertTokenSourceAsync(app_withoutStaticCache, "S2", TokenSource.IdentityProvider, 2).ConfigureAwait(false);

            }
        }

        [TestMethod]
        public async Task PublicClient_StaticCache_Async()
        {
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                var app1 = PublicClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithHttpManager(harness.HttpManager)
                    .WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
                    .BuildConcrete();

                app1.ServiceBundle.ConfigureMockWebUI();

                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(TestConstants.AuthorityCommonTenant);

                AuthenticationResult result = await app1
                    .AcquireTokenInteractive(TestConstants.s_scope)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(1, result.AuthenticationResultMetadata.CachedAccessTokenCount);

                var accounts = await app1.GetAccountsAsync().ConfigureAwait(false);
                Assert.AreEqual(1, accounts.Count());
                result = await app1.AcquireTokenSilent(TestConstants.s_scope, accounts.Single()).ExecuteAsync().ConfigureAwait(false);
                Assert.AreEqual(1, result.AuthenticationResultMetadata.CachedAccessTokenCount);

                var app2 = PublicClientApplicationBuilder
                   .Create(TestConstants.ClientId)
                   .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                   .WithHttpManager(harness.HttpManager)
                   .WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
                   .BuildConcrete();

                accounts = await app2.GetAccountsAsync().ConfigureAwait(false);
                Assert.AreEqual(1, accounts.Count());
                result = await app2.AcquireTokenSilent(TestConstants.s_scope, accounts.Single()).ExecuteAsync().ConfigureAwait(false);
                Assert.AreEqual(1, result.AuthenticationResultMetadata.CachedAccessTokenCount);
            }
        }

        private async Task<AuthenticationResult> ClientCredsAcquireAndAssertTokenSourceAsync(
            IConfidentialClientApplication app, 
            string scope, 
            TokenSource expectedSource, 
            int expectedAccessTokenCount)
        {
            var result = await app.AcquireTokenForClient(new[] { scope })
                 .WithTenantId(TestConstants.Utid)
                 .ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(
               expectedSource,
               result.AuthenticationResultMetadata.TokenSource);
               

            Assert.AreEqual(expectedAccessTokenCount, 
                result.AuthenticationResultMetadata.CachedAccessTokenCount);

            return result;
        }

        /// <summary>Static helper and the bool property have correct values.</summary>
        [TestMethod]
        public void DisableInternalCacheOptions_StaticProperty_HasCorrectValues()
        {
            var disabled = CacheOptions.DisableInternalCacheOptions;
            Assert.IsNotNull(disabled);
            Assert.IsTrue(disabled.IsInternalCacheDisabled, "DisableInternalCacheOptions.IsInternalCacheDisabled should be true");
            Assert.IsFalse(disabled.UseSharedCache, "DisableInternalCacheOptions.UseSharedCache should be false");

            var defaults = new CacheOptions();
            Assert.IsFalse(defaults.IsInternalCacheDisabled, "Default CacheOptions should have IsInternalCacheDisabled == false");
        }

        /// <summary>CacheOptions.IsDisabledFor is null-safe and returns the correct value for all inputs.</summary>
        [TestMethod]
        public void CacheOptions_IsDisabledFor_NullSafeAndCorrect()
        {
            Assert.IsFalse(CacheOptions.IsDisabledFor(null),
                "IsDisabledFor(null) should return false — null options means cache is not disabled.");
            Assert.IsFalse(CacheOptions.IsDisabledFor(new CacheOptions()),
                "IsDisabledFor(default CacheOptions) should return false.");
            Assert.IsTrue(CacheOptions.IsDisabledFor(CacheOptions.DisableInternalCacheOptions),
                "IsDisabledFor(DisableInternalCacheOptions) should return true.");
        }

        /// <summary>GetRefreshToken() extension returns the refresh token from a real token flow.</summary>
        [TestMethod]
        public async Task GetRefreshToken_AcquireTokenByAuthCode_ReturnsToken_Async()
        {
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();
                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(TestConstants.AuthorityCommonTenant);

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithRedirectUri(TestConstants.RedirectUri)
                    .WithHttpManager(harness.HttpManager)
                    .BuildConcrete();

                AuthenticationResult result = await app
                    .AcquireTokenByAuthorizationCode(TestConstants.s_scope, TestConstants.DefaultAuthorizationCode)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                string rt = result.GetRefreshToken();
                Assert.IsNotNull(rt, "GetRefreshToken() should return a non-null refresh token.");
                Assert.AreEqual(TestConstants.RTSecret, rt, "GetRefreshToken() should return the refresh token from the token response.");
            }
        }

        /// <summary>GetRefreshToken() returns null for public client applications — RT exposure is confidential client only.</summary>
        [TestMethod]
        public async Task GetRefreshToken_PublicClient_ReturnsNull_Async()
        {
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();
                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(TestConstants.AuthorityCommonTenant);

                var pca = PublicClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithHttpManager(harness.HttpManager)
                    .BuildConcrete();

                pca.ServiceBundle.ConfigureMockWebUI();

                AuthenticationResult result = await pca
                    .AcquireTokenInteractive(TestConstants.s_scope)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNull(result.GetRefreshToken(), "GetRefreshToken() must return null for public client applications.");
            }
        }

        /// <summary>When DisableInternalCacheOptions is set, AcquireTokenForClient always hits the network and nothing is stored.</summary>
        [TestMethod]
        public async Task DisableInternalCacheOptions_AcquireTokenForClient_NeverCaches_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(httpManager)
                    .WithCacheOptions(CacheOptions.DisableInternalCacheOptions)
                    .BuildConcrete();

                // Two separate network calls expected because the cache is disabled.
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
                var result1 = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .WithTenantId(TestConstants.Utid)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
                var result2 = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .WithTenantId(TestConstants.Utid)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, result1.AuthenticationResultMetadata.TokenSource,
                    "First call should come from the network, not the cache.");
                Assert.AreEqual(CacheRefreshReason.CacheDisabled, result1.AuthenticationResultMetadata.CacheRefreshReason,
                    "CacheRefreshReason should be CacheDisabled when the internal cache is disabled.");
                Assert.AreEqual(TokenSource.IdentityProvider, result2.AuthenticationResultMetadata.TokenSource,
                    "Second call should also come from the network because the internal cache is disabled.");
                Assert.AreEqual(CacheRefreshReason.CacheDisabled, result2.AuthenticationResultMetadata.CacheRefreshReason,
                    "CacheRefreshReason should be CacheDisabled when the internal cache is disabled.");

                // Client credentials does not include a refresh token in the token response.
                Assert.IsNull(result1.GetRefreshToken(),
                    "GetRefreshToken() must return null for AcquireTokenForClient — the server does not issue refresh tokens for client credentials.");

                Assert.IsEmpty(app.AppTokenCacheInternal.Accessor.GetAllAccessTokens(),
                    "No access tokens should have been stored in the internal cache.");
            }
        }

        /// <summary>DisableInternalCacheOptions also skips the user token cache.</summary>
        [TestMethod]
        public async Task DisableInternalCacheOptions_AcquireTokenByAuthCode_DoesNotCacheTokens_Async()
        {
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();
                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(TestConstants.AuthorityCommonTenant);

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithRedirectUri(TestConstants.RedirectUri)
                    .WithHttpManager(harness.HttpManager)
                    .WithCacheOptions(CacheOptions.DisableInternalCacheOptions)
                    .BuildConcrete();

                await app
                    .AcquireTokenByAuthorizationCode(TestConstants.s_scope, TestConstants.DefaultAuthorizationCode)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsEmpty(app.UserTokenCacheInternal.Accessor.GetAllAccessTokens(),
                    "No access tokens should be stored when the internal cache is disabled.");
                Assert.IsEmpty(app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens(),
                    "No refresh tokens should be stored when the internal cache is disabled.");
                Assert.IsEmpty(app.UserTokenCacheInternal.Accessor.GetAllIdTokens(),
                    "No ID tokens should be stored when the internal cache is disabled.");
            }
        }

        /// <summary>AcquireTokenSilent throws MsalUiRequiredException (the established "silent failed" contract) when the internal cache is disabled.</summary>
        [TestMethod]
        public async Task DisableInternalCacheOptions_AcquireTokenSilent_ThrowsWithCorrectError_Async()
        {
            var app = PublicClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithCacheOptions(CacheOptions.DisableInternalCacheOptions)
                .Build();

            var account = new Account("aid.tid", "user@contoso.com", "login.microsoftonline.com");

            var ex = await AssertException.TaskThrowsAsync<MsalUiRequiredException>(
                () => app.AcquireTokenSilent(TestConstants.s_scope, account).ExecuteAsync())
                .ConfigureAwait(false);

            Assert.AreEqual(MsalError.InternalCacheDisabled, ex.ErrorCode,
                "The error code should identify that the internal cache is disabled.");
            StringAssert.Contains(ex.Message, "AcquireTokenByRefreshToken",
                "The error message should guide the caller towards AcquireTokenByRefreshToken.");
            Assert.AreEqual(UiRequiredExceptionClassification.AcquireTokenSilentFailed, ex.Classification,
                "Classification should signal that silent auth failed.");
        }

        /// <summary>Mutual exclusivity: IsInternalCacheDisabled and UseSharedCache cannot both be set.</summary>
        [TestMethod]
        public void DisableInternalCacheOptions_AndUseSharedCache_ThrowsOnBuild()
        {
            var conflictingOptions = new CacheOptions
            {
                IsInternalCacheDisabled = true,
                UseSharedCache = true
            };

            var ex = AssertException.Throws<MsalClientException>(
                () => ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithCacheOptions(conflictingOptions)
                    .Build());

            Assert.AreEqual(MsalError.InvalidRequest, ex.ErrorCode,
                "Setting both IsInternalCacheDisabled and UseSharedCache should throw an InvalidRequest error.");
        }

        /// <summary>
        /// Short-running OBO with DisableInternalCacheOptions: every call always goes to the network
        /// and nothing is written to the internal cache.
        /// </summary>
        [TestMethod]
        public async Task DisableInternalCacheOptions_ShortRunningObo_AlwaysHitsNetwork_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var cca = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithAuthority(TestConstants.AuthorityCommonTenant)
                    .WithHttpManager(httpManager)
                    .WithCacheOptions(CacheOptions.DisableInternalCacheOptions)
                    .BuildConcrete();

                var userAssertion = new UserAssertion(TestConstants.DefaultAccessToken);

                // First OBO call — must hit the network.
                httpManager.AddMockHandler(new MockHttpMessageHandler
                {
                    ExpectedUrl = TestConstants.AuthorityCommonTenant + "oauth2/v2.0/token",
                    ExpectedMethod = HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
                });

                var result1 = await cca.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, result1.AuthenticationResultMetadata.TokenSource,
                    "First OBO call should hit the network.");
                Assert.AreEqual(CacheRefreshReason.CacheDisabled, result1.AuthenticationResultMetadata.CacheRefreshReason,
                    "CacheRefreshReason should be CacheDisabled for OBO when the internal cache is disabled.");
                Assert.IsNull(result1.GetRefreshToken(),
                    "Normal OBO does not expose a refresh token (MSAL intentionally clears it).");

                // Second OBO call with the same assertion — must hit the network again because the cache is disabled.
                httpManager.AddMockHandler(new MockHttpMessageHandler
                {
                    ExpectedUrl = TestConstants.AuthorityCommonTenant + "oauth2/v2.0/token",
                    ExpectedMethod = HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
                });

                var result2 = await cca.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, result2.AuthenticationResultMetadata.TokenSource,
                    "Second OBO call should also hit the network because the internal cache is disabled.");
                Assert.AreEqual(CacheRefreshReason.CacheDisabled, result2.AuthenticationResultMetadata.CacheRefreshReason,
                    "CacheRefreshReason should be CacheDisabled for OBO when the internal cache is disabled.");

                Assert.IsEmpty(cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens(),
                    "No access tokens should have been stored in the internal cache.");
                Assert.IsEmpty(cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens(),
                    "No refresh tokens should have been stored in the internal cache.");
            }
        }

        /// <summary>
        /// When DisableInternalCacheOptions is set, Account.TenantProfiles should still contain
        /// the current tenant's profile derived from the freshly received ID token (no cache reads needed).
        /// </summary>
        [TestMethod]
        public async Task DisableInternalCacheOptions_OboResult_HasTenantProfile_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var cca = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithAuthority(TestConstants.AuthorityCommonTenant)
                    .WithHttpManager(httpManager)
                    .WithCacheOptions(CacheOptions.DisableInternalCacheOptions)
                    .BuildConcrete();

                var userAssertion = new UserAssertion(TestConstants.DefaultAccessToken);

                httpManager.AddMockHandler(new MockHttpMessageHandler
                {
                    ExpectedUrl = TestConstants.AuthorityCommonTenant + "oauth2/v2.0/token",
                    ExpectedMethod = HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
                });

                var result = await cca.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion)
                    .ExecuteAsync().ConfigureAwait(false);

                var profiles = result.Account.GetTenantProfiles()?.ToList();
                Assert.IsNotNull(profiles, "TenantProfiles should not be null when DisableInternalCacheOptions is set.");
                Assert.HasCount(1, profiles, "Should have exactly one TenantProfile from the freshly received ID token.");
                Assert.AreEqual(TestConstants.Utid, profiles[0].TenantId,
                    "TenantProfile.TenantId should match the ID token's tenant.");
            }
        }

        /// <summary>
        /// Long-running OBO with DisableInternalCacheOptions: InitiateLongRunningProcessInWebApi always hits
        /// the network and stores nothing. AcquireTokenInLongRunningProcess cannot go to the network
        /// (no user assertion to exchange) and throws MsalUiRequiredException with error code
        /// MsalError.InternalCacheDisabled to surface the root cause directly.
        /// </summary>
        [TestMethod]
        public async Task DisableInternalCacheOptions_LongRunningObo_InitiateAlwaysHitsNetwork_AcquireThrows_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var cca = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithAuthority(TestConstants.AuthorityCommonTenant)
                    .WithHttpManager(httpManager)
                    .WithCacheOptions(CacheOptions.DisableInternalCacheOptions)
                    .BuildConcrete();

                string oboCacheKey = "obo-cache-key";

                // Initiate — must hit the network.
                httpManager.AddMockHandler(new MockHttpMessageHandler
                {
                    ExpectedUrl = TestConstants.AuthorityCommonTenant + "oauth2/v2.0/token",
                    ExpectedMethod = HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
                });

                var result = await cca
                    .InitiateLongRunningProcessInWebApi(TestConstants.s_scope, TestConstants.DefaultAccessToken, ref oboCacheKey)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource,
                    "Initiate should always hit the network when the internal cache is disabled.");
                Assert.AreEqual(CacheRefreshReason.CacheDisabled, result.AuthenticationResultMetadata.CacheRefreshReason,
                    "CacheRefreshReason should be CacheDisabled for long-running OBO when the internal cache is disabled.");

                Assert.IsEmpty(cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens(),
                    "No access tokens should have been stored in the internal cache.");
                Assert.IsEmpty(cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens(),
                    "No refresh tokens should have been stored in the internal cache.");

                // AcquireTokenInLongRunningProcess cannot go to the network (no user assertion to
                // exchange). When the cache is disabled it surfaces the root cause directly.
                var ex = await AssertException.TaskThrowsAsync<MsalUiRequiredException>(
                    () => cca.AcquireTokenInLongRunningProcess(TestConstants.s_scope, oboCacheKey).ExecuteAsync())
                    .ConfigureAwait(false);

                Assert.AreEqual(MsalError.InternalCacheDisabled, ex.ErrorCode,
                    "AcquireTokenInLongRunningProcess should throw MsalError.InternalCacheDisabled when the cache is disabled.");
                Assert.AreEqual(UiRequiredExceptionClassification.AcquireTokenSilentFailed, ex.Classification,
                    "Classification should signal that silent auth failed.");
            }
        }

        /// <summary>
        /// AcquireTokenSilent on a CCA (confidential client) throws the same MsalUiRequiredException
        /// as on a PCA when DisableInternalCacheOptions is set.
        /// </summary>
        [TestMethod]
        public async Task DisableInternalCacheOptions_AcquireTokenSilent_CcaVariant_ThrowsWithCorrectError_Async()
        {
            var cca = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithClientSecret(TestConstants.ClientSecret)
                .WithCacheOptions(CacheOptions.DisableInternalCacheOptions)
                .Build();

            var account = new Account("aid.tid", "user@contoso.com", "login.microsoftonline.com");

            var ex = await AssertException.TaskThrowsAsync<MsalUiRequiredException>(
                () => cca.AcquireTokenSilent(TestConstants.s_scope, account).ExecuteAsync())
                .ConfigureAwait(false);

            Assert.AreEqual(MsalError.InternalCacheDisabled, ex.ErrorCode,
                "The error code should identify that the internal cache is disabled.");
            StringAssert.Contains(ex.Message, "AcquireTokenByRefreshToken",
                "The error message should guide the caller towards AcquireTokenByRefreshToken.");
            Assert.AreEqual(UiRequiredExceptionClassification.AcquireTokenSilentFailed, ex.Classification,
                "Classification should signal that silent auth failed.");
        }
    }
}
