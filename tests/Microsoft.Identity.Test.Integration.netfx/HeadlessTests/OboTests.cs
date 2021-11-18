// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.Integration.net45.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    [TestClass]
    public class OboTests
    {
        private static readonly string[] s_scopes = { "User.Read" };
        private static readonly string[] s_oboServiceScope = { "api://23c64cd8-21e4-41dd-9756-ab9e2c23f58c/access_as_user" };
        const string PublicClientID = "be9b0186-7dfd-448a-a944-f771029105bf";
        const string OboConfidentialClientID = "23c64cd8-21e4-41dd-9756-ab9e2c23f58c";

        private static InMemoryTokenCache s_inMemoryTokenCache = new InMemoryTokenCache();
        private string _confidentialClientSecret;

        private readonly KeyVaultSecretsProvider _keyVault = new KeyVaultSecretsProvider();

        #region Test Hooks
        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
            if (string.IsNullOrEmpty(_confidentialClientSecret))
            {
                _confidentialClientSecret = _keyVault.GetSecret(TestConstants.MsalOBOKeyVaultUri).Value;
            }
        }

        #endregion

        [TestMethod]
        public async Task OboAndClientWithRegionalTestAsync()
        {
            // Reuse the same CCA with regional for OBO and for client in different orders

            // Setup: Get lab users, create PCA and get user tokens
            var user1 = (await LabUserHelper.GetSpecificUserAsync("idlab1@msidlab4.onmicrosoft.com").ConfigureAwait(false)).User;
            var certificate = CertificateHelper.FindCertificateByThumbprint(TestConstants.AutomationTestThumbprint);
            Dictionary<string, string> queryParams = new Dictionary<string, string>
            {
                ["allowestsrnonmsi"] = "true" // allow regional for testing
            };

            var pca = PublicClientApplicationBuilder
                    .Create(PublicClientID)
                    .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs)
                    .Build();

            var userResult = await pca
                .AcquireTokenByUsernamePassword(s_oboServiceScope, user1.Upn, new NetworkCredential("", user1.GetOrFetchPassword()).SecurePassword)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            var cca = ConfidentialClientApplicationBuilder
                .Create(OboConfidentialClientID)
                .WithAuthority(new Uri($"https://login.microsoftonline.com/{userResult.TenantId}"), true)
                .WithClientSecret(_confidentialClientSecret)
                .WithAzureRegion(TestConstants.Region)
                .WithExtraQueryParameters(queryParams)
                .WithLegacyCacheCompatibility(false)
                .Build();

            // OBO uses global - IdP
            var oboResult = await cca.AcquireTokenOnBehalfOf(s_scopes, new UserAssertion(userResult.AccessToken))
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, oboResult.AuthenticationResultMetadata.TokenSource);
            Assert.IsFalse(oboResult.AuthenticationResultMetadata.TokenEndpoint.Contains(TestConstants.Region));

            // Client uses regional - IdP
            var clientResult = await cca.AcquireTokenForClient(new string[] { "https://graph.microsoft.com/.default" })
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, clientResult.AuthenticationResultMetadata.TokenSource);
            Assert.IsTrue(clientResult.AuthenticationResultMetadata.TokenEndpoint.Contains(TestConstants.Region));

            // OBO from cache
            var oboResult1 = await cca.AcquireTokenOnBehalfOf(s_scopes, new UserAssertion(userResult.AccessToken))
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, oboResult1.AuthenticationResultMetadata.TokenSource);

            // Client from cache
            var clientResult2 = await cca.AcquireTokenForClient(new string[] { "https://graph.microsoft.com/.default" })
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, clientResult2.AuthenticationResultMetadata.TokenSource);
        }

        /// <summary>
        /// Tests the behavior when calling both, long-running and normal OBO methods.
        /// Long-running OBO method return cached long-running tokens.
        /// Normal OBO method return cached normal tokens.
        /// Should be different partitions: by user-provided and by assertion hash 
        /// (if the user-provided key is not assertion hash)
        /// </summary>
        [TestMethod]
        public async Task AcquireTokenByObo_LongRunningAndNormalObo_WithDifferentKeys_TestAsync()
        {
            var user1 = (await LabUserHelper.GetSpecificUserAsync("idlab1@msidlab4.onmicrosoft.com").ConfigureAwait(false)).User;
            var pca = PublicClientApplicationBuilder
                .Create(PublicClientID)
                .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs)
                .Build();

            var userAuthResult = await pca
                .AcquireTokenByUsernamePassword(s_oboServiceScope, user1.Upn, new NetworkCredential("", user1.GetOrFetchPassword()).SecurePassword)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            var cca = BuildCCA(userAuthResult.TenantId);

            string oboCacheKey = "obo-cache-key";
            UserAssertion userAssertion = new UserAssertion(userAuthResult.AccessToken);

            var result = await cca.InitiateLongRunningProcessInWebApi(s_scopes, userAuthResult.AccessToken, ref oboCacheKey)
                .ExecuteAsync().ConfigureAwait(false);

            // Cache has 1 partition (user-provided key) with 1 token
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(1, cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
            Assert.AreEqual(1, cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count);

            result = await cca.AcquireTokenOnBehalfOf(s_scopes, userAssertion).ExecuteAsync().ConfigureAwait(false);

            // Cache has 2 partitions (user-provided key, assertion) with 1 token each
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(2, cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
            Assert.AreEqual(1, cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count);

            // Returns long-running token
            result = await cca.AcquireTokenInLongRunningProcess(s_scopes, oboCacheKey).ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);

            // Returns normal token
            result = await cca.AcquireTokenOnBehalfOf(s_scopes, userAssertion).ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
        }

        /// <summary>
        /// Tests the behavior when calling both, long-running and normal OBO methods.
        /// Both methods should return the same tokens, since the cache key is the same.
        /// Should be the same partition: by assertion hash.
        /// </summary>
        [TestMethod]
        public async Task AcquireTokenByObo_LongRunningThenNormalObo_WithTheSameKey_TestAsync()
        {
            var user1 = (await LabUserHelper.GetSpecificUserAsync("idlab1@msidlab4.onmicrosoft.com").ConfigureAwait(false)).User;
            var pca = PublicClientApplicationBuilder
                .Create(PublicClientID)
                .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs)
                .Build();

            var userAuthResult = await pca
                .AcquireTokenByUsernamePassword(s_oboServiceScope, user1.Upn, new NetworkCredential("", user1.GetOrFetchPassword()).SecurePassword)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            var cca = BuildCCA(userAuthResult.TenantId);

            string oboCacheKey = null;
            UserAssertion userAssertion = new UserAssertion(userAuthResult.AccessToken);

            // InitiateLR - Empty cache - AT via OBO flow (new AT, RT cached)
            var result = await cca.InitiateLongRunningProcessInWebApi(s_scopes, userAuthResult.AccessToken, ref oboCacheKey)
                                    .ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(userAssertion.AssertionHash, oboCacheKey);
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(1, cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
            Assert.AreEqual(1, cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count);

            // AcquireLR - AT from cache
            result = await cca.AcquireTokenInLongRunningProcess(s_scopes, oboCacheKey).ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);

            // AcquireNormal - AT from cache
            result = await cca.AcquireTokenOnBehalfOf(s_scopes, userAssertion).ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);

            // Expire AT
            TokenCacheHelper.ExpireAllAccessTokens(cca.UserTokenCacheInternal);

            // InitiateLR - AT from IdP via RT flow(new AT, RT cached)
            result = await cca.InitiateLongRunningProcessInWebApi(s_scopes, userAuthResult.AccessToken, ref oboCacheKey)
                                .ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(1, cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
            Assert.AreEqual(1, cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count);

            // Expire AT
            TokenCacheHelper.ExpireAllAccessTokens(cca.UserTokenCacheInternal);

            // AcquireLR - AT from IdP via RT flow (new AT, RT cached)
            result = await cca.AcquireTokenInLongRunningProcess(s_scopes, oboCacheKey).ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(1, cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
            Assert.AreEqual(1, cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count);

            // Expire AT
            TokenCacheHelper.ExpireAllAccessTokens(cca.UserTokenCacheInternal);

            // AcquireNormal - AT from IdP via OBO flow (only new AT cached, old RT still left in cache)
            result = await cca.AcquireTokenOnBehalfOf(s_scopes, userAssertion).ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(1, cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
            Assert.AreEqual(1, cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count);
        }

        /// <summary>
        /// Tests the behavior when calling both, long-running and normal OBO methods.
        /// Both methods should return the same tokens, since the cache key is the same.
        /// Should be the same partition: by assertion hash.
        /// </summary>
        [TestMethod]
        public async Task AcquireTokenByObo_NormalOboThenLongRunningAcquire_WithTheSameKey_TestAsync()
        {
            var user1 = (await LabUserHelper.GetSpecificUserAsync("idlab1@msidlab4.onmicrosoft.com").ConfigureAwait(false)).User;
            var pca = PublicClientApplicationBuilder
                .Create(PublicClientID)
                .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs)
                .Build();

            var userAuthResult = await pca
                .AcquireTokenByUsernamePassword(s_oboServiceScope, user1.Upn, new NetworkCredential("", user1.GetOrFetchPassword()).SecurePassword)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            var cca = BuildCCA(userAuthResult.TenantId);

            UserAssertion userAssertion = new UserAssertion(userAuthResult.AccessToken);
            string oboCacheKey = userAssertion.AssertionHash;

            // AcquireNormal - AT from IdP via OBO flow (only new AT cached, no RT in cache)
            var result = await cca.AcquireTokenOnBehalfOf(s_scopes, userAssertion).ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(1, cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
            Assert.AreEqual(0, cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count);

            // AcquireLR - AT from cache
            result = await cca.AcquireTokenInLongRunningProcess(s_scopes, oboCacheKey).ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);

            // Expire AT
            TokenCacheHelper.ExpireAllAccessTokens(cca.UserTokenCacheInternal);

            // AcquireLR - throws because no RT
            var exception = await AssertException.TaskThrowsAsync<MsalClientException>(
                () => cca.AcquireTokenInLongRunningProcess(s_scopes.ToArray(), oboCacheKey)
                .ExecuteAsync())
                .ConfigureAwait(false);

            Assert.AreEqual(MsalError.OboCacheKeyNotInCacheError, exception.ErrorCode);

            // Expire AT
            TokenCacheHelper.ExpireAllAccessTokens(cca.UserTokenCacheInternal);

            // InitiateLR - AT from IdP via OBO flow (new AT, RT cached)
            result = await cca.InitiateLongRunningProcessInWebApi(s_scopes, userAuthResult.AccessToken, ref oboCacheKey)
                    .ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(1, cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
            Assert.AreEqual(1, cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count);

            // Expire AT
            TokenCacheHelper.ExpireAllAccessTokens(cca.UserTokenCacheInternal);

            // AcquireLR - AT from IdP via RT flow (new AT, RT cached)
            result = await cca.AcquireTokenInLongRunningProcess(s_scopes, oboCacheKey).ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(1, cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
            Assert.AreEqual(1, cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count);
        }

        /// <summary>
        /// Tests the behavior when calling both, long-running and normal OBO methods.
        /// Both methods should return the same tokens, since the cache key is the same.
        /// Should be the same partition: by assertion hash.
        /// </summary>
        [TestMethod]
        public async Task AcquireTokenByObo_NormalOboThenLongRunningInitiate_WithTheSameKey_TestAsync()
        {
            var user1 = (await LabUserHelper.GetSpecificUserAsync("idlab1@msidlab4.onmicrosoft.com").ConfigureAwait(false)).User;
            var pca = PublicClientApplicationBuilder
                .Create(PublicClientID)
                .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs)
                .Build();

            var userAuthResult = await pca
                .AcquireTokenByUsernamePassword(s_oboServiceScope, user1.Upn, new NetworkCredential("", user1.GetOrFetchPassword()).SecurePassword)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            var cca = BuildCCA(userAuthResult.TenantId);

            UserAssertion userAssertion = new UserAssertion(userAuthResult.AccessToken);
            string oboCacheKey = userAssertion.AssertionHash;

            // AcquireNormal - AT from IdP via OBO flow(only new AT cached, no RT in cache)
            var result = await cca.AcquireTokenOnBehalfOf(s_scopes, userAssertion).ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(1, cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
            Assert.AreEqual(0, cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count);

            // InitiateLR - AT from cache
            result = await cca.InitiateLongRunningProcessInWebApi(s_scopes, userAuthResult.AccessToken, ref oboCacheKey)
                .ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);

            // Expire AT
            TokenCacheHelper.ExpireAllAccessTokens(cca.UserTokenCacheInternal);

            // InitiateLR - AT via OBO flow (new AT, RT cached)
            result = await cca.InitiateLongRunningProcessInWebApi(s_scopes, userAuthResult.AccessToken, ref oboCacheKey)
                .ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(1, cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
            Assert.AreEqual(1, cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count);

            // Expire AT
            TokenCacheHelper.ExpireAllAccessTokens(cca.UserTokenCacheInternal);

            // AcquireLR - AT from IdP via RT flow(new AT, RT cached)
            result = await cca.AcquireTokenInLongRunningProcess(s_scopes, oboCacheKey).ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(1, cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
            Assert.AreEqual(1, cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count);
        }

        [TestMethod]
        public async Task OBO_WithCache_MultipleUsers_Async()
        {
            var aadUser1 = (await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false)).User;
            var aadUser2 = (await LabUserHelper.GetAdfsUserAsync(FederationProvider.AdfsV2, true).ConfigureAwait(false)).User;
            var adfsUser = (await LabUserHelper.GetAdfsUserAsync(FederationProvider.ADFSv2019).ConfigureAwait(false)).User;

            await RunOnBehalfOfTestAsync(adfsUser, false).ConfigureAwait(false);
            await RunOnBehalfOfTestAsync(aadUser1, false).ConfigureAwait(false);
            await RunOnBehalfOfTestAsync(aadUser1, true).ConfigureAwait(false);
            await RunOnBehalfOfTestAsync(aadUser2, false).ConfigureAwait(false);
            await RunOnBehalfOfTestAsync(adfsUser, true).ConfigureAwait(false);
            await RunOnBehalfOfTestAsync(aadUser2, true).ConfigureAwait(false);
            await RunOnBehalfOfTestAsync(aadUser2, false, true).ConfigureAwait(false);
        }

        private async Task<IConfidentialClientApplication> RunOnBehalfOfTestAsync(
            LabUser user,
            bool silentCallShouldSucceed,
            bool forceRefresh = false)
        {
            SecureString securePassword = new NetworkCredential("", user.GetOrFetchPassword()).SecurePassword;
            AuthenticationResult authResult;

            var pca = PublicClientApplicationBuilder
                .Create(PublicClientID)
                .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs)
                .WithTestLogging()
                .Build();
            s_inMemoryTokenCache.Bind(pca.UserTokenCache);
            try
            {
                authResult = await pca
                    .AcquireTokenSilent(s_oboServiceScope, user.Upn)
                    .ExecuteAsync()
                    .ConfigureAwait(false);
                Assert.AreEqual(TokenSource.Cache, authResult.AuthenticationResultMetadata.TokenSource);
            }
            catch (MsalUiRequiredException)
            {
                Assert.IsFalse(silentCallShouldSucceed, "ATS should have found a token, but it didn't");
                authResult = await pca
                    .AcquireTokenByUsernamePassword(s_oboServiceScope, user.Upn, securePassword)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
                Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);
            }

            MsalAssert.AssertAuthResult(authResult, user);
            Assert.IsTrue(authResult.Scopes.Any(s => string.Equals(s, s_oboServiceScope.Single(), StringComparison.OrdinalIgnoreCase)));

            var cca = ConfidentialClientApplicationBuilder
                .Create(OboConfidentialClientID)
                .WithAuthority(new Uri("https://login.microsoftonline.com/" + authResult.TenantId), true)
                .WithTestLogging(out HttpSnifferClientFactory factory)
                .WithClientSecret(_confidentialClientSecret)
                .Build();
            s_inMemoryTokenCache.Bind(cca.UserTokenCache);

            authResult = await cca.AcquireTokenOnBehalfOf(s_scopes, new UserAssertion(authResult.AccessToken))
                .WithForceRefresh(forceRefresh)
                .WithCcsRoutingHint("597f86cd-13f3-44c0-bece-a1e77ba43228", "f645ad92-e38d-4d1a-b510-d1b09a74a8ca")
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            if (!forceRefresh)
            {
                Assert.AreEqual(
                silentCallShouldSucceed,
                authResult.AuthenticationResultMetadata.TokenSource == TokenSource.Cache);
            }
            else
            {
                Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);
            }

            MsalAssert.AssertAuthResult(authResult, user);
            Assert.IsNotNull(authResult.IdToken); // https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1950
            Assert.IsTrue(authResult.Scopes.Any(s => string.Equals(s, s_scopes.Single(), StringComparison.OrdinalIgnoreCase)));
            AssertExtraHTTPHeadersAreSent(factory);

            return cca;
        }

        private void AssertExtraHTTPHeadersAreSent(HttpSnifferClientFactory factory)
        {
            //Validate CCS Routing header
            if (!factory.RequestsAndResponses.Any())
            {
                return;
            }

            var (req, res) = factory.RequestsAndResponses.Single(x => x.Item1.RequestUri.AbsoluteUri.Contains("oauth2/v2.0/token") &&
            x.Item2.StatusCode == HttpStatusCode.OK);

            Assert.IsTrue(req.Headers.TryGetValues(Constants.CcsRoutingHintHeader, out var values));
            Assert.AreEqual("oid:597f86cd-13f3-44c0-bece-a1e77ba43228@f645ad92-e38d-4d1a-b510-d1b09a74a8ca", values.First());
        }

        private ConfidentialClientApplication BuildCCA(string tenantId)
        {
            return ConfidentialClientApplicationBuilder
             .Create(OboConfidentialClientID)
             .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}"), true)
             .WithClientSecret(_confidentialClientSecret)
             .WithLegacyCacheCompatibility(false)
             .BuildConcrete();
        }
    }
}
