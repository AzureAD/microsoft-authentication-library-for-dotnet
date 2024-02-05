// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    [TestClass]
    public class LongRunningOnBehalfOfTests
    {
        private static readonly string[] s_scopes = { "User.Read" };
        private static readonly string[] s_oboServiceScope = { "api://23c64cd8-21e4-41dd-9756-ab9e2c23f58c/access_as_user" };
        const string PublicClientID = "be9b0186-7dfd-448a-a944-f771029105bf";
        const string OboConfidentialClientID = "23c64cd8-21e4-41dd-9756-ab9e2c23f58c";

        private string _confidentialClientSecret;

        private readonly KeyVaultSecretsProvider _keyVault = new KeyVaultSecretsProvider(KeyVaultInstance.MsalTeam);

        #region Test Hooks
        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
            if (string.IsNullOrEmpty(_confidentialClientSecret))
            {
                _confidentialClientSecret = _keyVault.GetSecretByName(TestConstants.MsalOBOKeyVaultSecretName).Value;
            }
        }

        #endregion

        /// <summary>
        /// Tests the behavior when calling both, long-running and normal OBO methods.
        /// Long-running OBO method return cached long-running tokens.
        /// Normal OBO method return cached normal tokens.
        /// Should be different partitions: by user-provided and by assertion hash 
        /// (if the user-provided key is not assertion hash)
        /// </summary>
        [RunOn(TargetFrameworks.NetCore)]
        public async Task LongRunningAndNormalObo_WithDifferentKeys_TestAsync()
        {
            var user1 = (await LabUserHelper.GetSpecificUserAsync("idlab1@msidlab4.onmicrosoft.com").ConfigureAwait(false)).User;
            var pca = PublicClientApplicationBuilder
                .Create(PublicClientID)
                .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs)
                .Build();

            var userAuthResult = await pca
                .AcquireTokenByUsernamePassword(s_oboServiceScope, user1.Upn, user1.GetOrFetchPassword())
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
        [RunOn(TargetFrameworks.NetCore)]
        public async Task LongRunningThenNormalObo_WithTheSameKey_TestAsync()
        {
            var user1 = (await LabUserHelper.GetSpecificUserAsync("idlab1@msidlab4.onmicrosoft.com").ConfigureAwait(false)).User;
            var pca = PublicClientApplicationBuilder
                .Create(PublicClientID)
                .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs)
                .Build();

            var userAuthResult = await pca
                .AcquireTokenByUsernamePassword(s_oboServiceScope, user1.Upn, user1.GetOrFetchPassword())
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
        [RunOn(TargetFrameworks.NetCore)]
        public async Task NormalOboThenLongRunningAcquire_WithTheSameKey_TestAsync()
        {
            var user1 = (await LabUserHelper.GetSpecificUserAsync("idlab1@msidlab4.onmicrosoft.com").ConfigureAwait(false)).User;
            var pca = PublicClientApplicationBuilder
                .Create(PublicClientID)
                .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs)
                .Build();

            var userAuthResult = await pca
                .AcquireTokenByUsernamePassword(s_oboServiceScope, user1.Upn, user1.GetOrFetchPassword())
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
        [RunOn(TargetFrameworks.NetCore)]
        public async Task NormalOboThenLongRunningInitiate_WithTheSameKey_TestAsync()
        {
            var user1 = (await LabUserHelper.GetSpecificUserAsync("idlab1@msidlab4.onmicrosoft.com").ConfigureAwait(false)).User;
            var pca = PublicClientApplicationBuilder
                .Create(PublicClientID)
                .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs)
                .Build();

            var userAuthResult = await pca
                .AcquireTokenByUsernamePassword(s_oboServiceScope, user1.Upn, user1.GetOrFetchPassword())
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

            // InitiateLR - AT from IdentityProvider
            result = await cca.InitiateLongRunningProcessInWebApi(s_scopes, userAuthResult.AccessToken, ref oboCacheKey)
                .ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

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

        [RunOn(TargetFrameworks.NetCore)]
        public async Task WithDifferentScopes_TestAsync()
        {
            string[] scopes2 = { "api://eec635da-5760-452d-940a-448220db047c/access_as_user" };
            var user1 = (await LabUserHelper.GetSpecificUserAsync("idlab1@msidlab4.onmicrosoft.com").ConfigureAwait(false)).User;
            var pca = PublicClientApplicationBuilder
                .Create(PublicClientID)
                .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs)
                .WithRedirectUri("https://login.microsoftonline.com/common/oauth2/nativeclient")
                .Build();

            var userAuthResult = await pca
                .AcquireTokenByUsernamePassword(s_oboServiceScope, user1.Upn, user1.GetOrFetchPassword())
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            var cca = BuildCCA(userAuthResult.TenantId);

            string oboCacheKey = "obo-cache-key";

            var result = await cca.InitiateLongRunningProcessInWebApi(s_scopes, userAuthResult.AccessToken, ref oboCacheKey)
                .ExecuteAsync().ConfigureAwait(false);

            // Cache has 1 partition (user-provided key) with 1 token
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(1, cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
            Assert.AreEqual(1, cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count);

            // No matching AT, uses RT to retrieve new AT.
            result = await cca.AcquireTokenInLongRunningProcess(scopes2, oboCacheKey).ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(CacheRefreshReason.NoCachedAccessToken, result.AuthenticationResultMetadata.CacheRefreshReason);
        }

        [RunOn(TargetFrameworks.NetCore)]
        public async Task AcquireTokenInLongRunningObo_WithNoTokensFound_TestAsync()
        {
            var cca = BuildCCA(Guid.NewGuid().ToString());

            string oboCacheKey = "obo-cache-key";

            var ex = await AssertException.TaskThrowsAsync<MsalClientException>(async () =>
                      await cca.AcquireTokenInLongRunningProcess(s_scopes, oboCacheKey).ExecuteAsync().ConfigureAwait(false)
                      ).ConfigureAwait(false);

            Assert.AreEqual(MsalError.OboCacheKeyNotInCacheError, ex.ErrorCode);
        }

        private ConfidentialClientApplication BuildCCA(string tenantId)
        {
            var builder = ConfidentialClientApplicationBuilder
             .Create(OboConfidentialClientID)
             .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}"), true)
             .WithClientSecret(_confidentialClientSecret)
             .WithLegacyCacheCompatibility(false);

            return builder.BuildConcrete();
        }
    }
}
