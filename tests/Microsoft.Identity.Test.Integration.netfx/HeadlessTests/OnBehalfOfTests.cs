// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
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
    public class OnBehalfOfTests
    {
        private static readonly string[] s_scopes = { "User.Read" };
        private static readonly string[] s_oboServiceScope = { "api://23c64cd8-21e4-41dd-9756-ab9e2c23f58c/access_as_user" };
        const string PublicClientID = "be9b0186-7dfd-448a-a944-f771029105bf";
        const string OboConfidentialClientID = "23c64cd8-21e4-41dd-9756-ab9e2c23f58c";

        private static InMemoryTokenCache s_inMemoryTokenCache = new InMemoryTokenCache();
        private string _confidentialClientSecret;

        private readonly KeyVaultSecretsProvider _keyVault = new KeyVaultSecretsProvider(KeyVaultInstance.MsalTeam);
        private readonly KeyVaultSecretsProvider _keyVaultMsidLab = new KeyVaultSecretsProvider(KeyVaultInstance.MSIDLab);

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
        /// Tests the behavior when calling OBO and silent in different orders with multiple users.
        /// OBO calls should return tokens for correct users, silent calls should throw.
        /// </summary>
        [DataTestMethod]
        [DataRow(false, false)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public async Task OboAndSilent_ReturnsCorrectTokens_TestAsync(bool serializeCache, bool usePartitionedSerializationCache)
        {
            // Setup: Get lab users, create PCA and get user tokens
            var user1 = (await LabUserHelper.GetSpecificUserAsync("idlab1@msidlab4.onmicrosoft.com").ConfigureAwait(false)).User;
            var user2 = (await LabUserHelper.GetSpecificUserAsync("idlab@msidlab4.onmicrosoft.com").ConfigureAwait(false)).User;
            var partitionedInMemoryTokenCache = new InMemoryPartitionedTokenCache();
            var nonPartitionedInMemoryTokenCache = new InMemoryTokenCache();
            var oboTokens = new HashSet<string>();

            var pca = PublicClientApplicationBuilder
                .Create(PublicClientID)
                .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs)
                .Build();

            var user1AuthResult = await pca
                .AcquireTokenByUsernamePassword(s_oboServiceScope, user1.Upn, user1.GetOrFetchPassword())
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            var user2AuthResult = await pca
                .AcquireTokenByUsernamePassword(s_oboServiceScope, user2.Upn, user2.GetOrFetchPassword())
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(user1AuthResult.TenantId, user2AuthResult.TenantId);

            var cca = CreateCCA();

            // Asserts
            // Silent calls should throw
            await AssertException.TaskThrowsAsync<MsalUiRequiredException>(() =>
                cca.AcquireTokenSilent(s_scopes, user1AuthResult.Account)
                    .ExecuteAsync(CancellationToken.None)
            ).ConfigureAwait(false);

            await AssertException.TaskThrowsAsync<MsalUiRequiredException>(() =>
                cca.AcquireTokenSilent(s_scopes, user2AuthResult.Account)
                    .ExecuteAsync(CancellationToken.None)
            ).ConfigureAwait(false);

            // User1 - no AT, RT in cache - retrieves from IdP
            var authResult = await cca.AcquireTokenOnBehalfOf(s_scopes, new UserAssertion(user1AuthResult.AccessToken))
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);
            Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(CacheRefreshReason.NoCachedAccessToken, authResult.AuthenticationResultMetadata.CacheRefreshReason);
            oboTokens.Add(authResult.AccessToken);

            // User1 - finds AT in cache
            authResult = await cca.AcquireTokenOnBehalfOf(s_scopes, new UserAssertion(user1AuthResult.AccessToken))
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);
            Assert.AreEqual(TokenSource.Cache, authResult.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(CacheRefreshReason.NotApplicable, authResult.AuthenticationResultMetadata.CacheRefreshReason);
            oboTokens.Add(authResult.AccessToken);

            // User2 - no AT, RT - retrieves from IdP
            authResult = await cca.AcquireTokenOnBehalfOf(s_scopes, new UserAssertion(user2AuthResult.AccessToken))
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);
            Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(CacheRefreshReason.NoCachedAccessToken, authResult.AuthenticationResultMetadata.CacheRefreshReason);
            oboTokens.Add(authResult.AccessToken);

            // User2 - finds AT in cache
            authResult = await cca.AcquireTokenOnBehalfOf(s_scopes, new UserAssertion(user2AuthResult.AccessToken))
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);
            Assert.AreEqual(TokenSource.Cache, authResult.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(CacheRefreshReason.NotApplicable, authResult.AuthenticationResultMetadata.CacheRefreshReason);
            oboTokens.Add(authResult.AccessToken);

            Assert.AreEqual(2, oboTokens.Count);

            // Silent calls should throw
            await AssertException.TaskThrowsAsync<MsalUiRequiredException>(() =>
                cca.AcquireTokenSilent(s_scopes, user1AuthResult.Account)
                    .ExecuteAsync(CancellationToken.None)
            ).ConfigureAwait(false);

            await AssertException.TaskThrowsAsync<MsalUiRequiredException>(() =>
                cca.AcquireTokenSilent(s_scopes, user2AuthResult.Account)
                    .ExecuteAsync(CancellationToken.None)
            ).ConfigureAwait(false);

            IConfidentialClientApplication CreateCCA()
            {
                var app = ConfidentialClientApplicationBuilder
                .Create(OboConfidentialClientID)
                .WithAuthority(new Uri($"https://login.microsoftonline.com/{user1AuthResult.TenantId}"), true)
                .WithClientSecret(_confidentialClientSecret)
                .WithLegacyCacheCompatibility(false)
                .Build();

                if (serializeCache)
                {
                    if (usePartitionedSerializationCache)
                    {
                        partitionedInMemoryTokenCache.Bind(app.UserTokenCache);
                    }
                    else
                    {
                        nonPartitionedInMemoryTokenCache.Bind(app.UserTokenCache);
                    }
                }

                return app;
            }
        }

        /// <summary>
        /// Reuse the same CCA with regional for OBO and for client calls in different orders.
        /// Client calls should go to regional, OBO calls should go to global
        /// </summary>
        [RunOn(TargetFrameworks.NetCore | TargetFrameworks.NetFx)]
        public async Task OboAndClientCredentials_WithRegional_ReturnsCorrectTokens_TestAsync()
        {
            // Setup: Get lab user, create PCA and get user tokens
            var user = (await LabUserHelper.GetSpecificUserAsync("idlab1@msidlab4.onmicrosoft.com").ConfigureAwait(false)).User;

            var pca = PublicClientApplicationBuilder
                    .Create(PublicClientID)
                    .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs)
                    .Build();

            var userResult = await pca
                .AcquireTokenByUsernamePassword(s_oboServiceScope, user.Upn, user.GetOrFetchPassword())
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            // Act and Assert different scenarios
            var cca = BuildCca(userResult.TenantId, true);

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
            oboResult = await cca.AcquireTokenOnBehalfOf(s_scopes, new UserAssertion(userResult.AccessToken))
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, oboResult.AuthenticationResultMetadata.TokenSource);

            // Client from cache
            clientResult = await cca.AcquireTokenForClient(new string[] { "https://graph.microsoft.com/.default" })
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, clientResult.AuthenticationResultMetadata.TokenSource);
        }

        [RunOn(TargetFrameworks.NetFx)]
        public async Task WithMultipleUsers_TestAsync()
        {
            var aadUser1 = (await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false)).User;
            var aadUser2 = (await LabUserHelper.GetDefaultUser2Async().ConfigureAwait(false)).User;
            var adfsUser = (await LabUserHelper.GetAdfsUserAsync(FederationProvider.ADFSv2019).ConfigureAwait(false)).User;

            await RunOnBehalfOfTestAsync(adfsUser, false).ConfigureAwait(false);
            await RunOnBehalfOfTestAsync(aadUser1, false).ConfigureAwait(false);
            await RunOnBehalfOfTestAsync(aadUser1, true).ConfigureAwait(false);
            await RunOnBehalfOfTestAsync(aadUser2, false).ConfigureAwait(false);
            await RunOnBehalfOfTestAsync(adfsUser, true).ConfigureAwait(false);
            await RunOnBehalfOfTestAsync(aadUser2, true).ConfigureAwait(false);
            await RunOnBehalfOfTestAsync(aadUser2, false, true).ConfigureAwait(false);
        }

        [RunOn(TargetFrameworks.NetCore)]
        [TestCategory(TestCategories.Arlington)]
        public async Task ArlingtonWebAPIAccessingGraphOnBehalfOfUserTestAsync()
        {
            var arligntonUser = (await LabUserHelper.GetArlingtonUserAsync().ConfigureAwait(false)).User;

            var msalPublicClient = PublicClientApplicationBuilder.Create("cb7faed4-b8c0-49ee-b421-f5ed16894c83")
                                                                 .WithAuthority("https://login.microsoftonline.us/organizations")
                                                                 .WithRedirectUri(TestConstants.RedirectUri)
                                                                 .WithTestLogging()
                                                                 .Build();

            var authResult = await msalPublicClient.AcquireTokenByUsernamePassword(
                new[] { "https://arlmsidlab1.us/IDLABS_APP_Confidential_Client/user_impersonation" }, arligntonUser.Upn, arligntonUser.GetOrFetchPassword())
                .ExecuteAsync()
                .ConfigureAwait(false);

            var ccaAuthority = new Uri("https://login.microsoftonline.us/" + authResult.TenantId);
            var confidentialApp = ConfidentialClientApplicationBuilder
                .Create("c0555d2d-02f2-4838-802e-3463422e571d")
                .WithAuthority(ccaAuthority, true)
                .WithAzureRegion(TestConstants.Region) // should be ignored by OBO
                .WithClientSecret(_keyVaultMsidLab.GetSecretByName(TestConstants.MsalArlingtonOBOKeyVaultSecretName).Value)
                .WithTestLogging()
                .Build();

            var userCacheRecorder = confidentialApp.UserTokenCache.RecordAccess();

            UserAssertion userAssertion = new UserAssertion(authResult.AccessToken);

            string atHash = userAssertion.AssertionHash;

            authResult = await confidentialApp.AcquireTokenOnBehalfOf(s_scopes, userAssertion)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            MsalAssert.AssertAuthResult(authResult, arligntonUser);
            Assert.AreEqual(atHash, userCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheKey);
            Assert.AreEqual(
                ccaAuthority.ToString() + "/oauth2/v2.0/token",
                authResult.AuthenticationResultMetadata.TokenEndpoint,
                "OBO does not obey region");

#pragma warning disable CS0618 // Type or member is obsolete
            await confidentialApp.GetAccountsAsync().ConfigureAwait(false);
#pragma warning restore CS0618 // Type or member is obsolete
            Assert.IsNull(userCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheKey);
        }

        [RunOn(TargetFrameworks.NetCore)]
        public async Task WithCache_TestAsync()
        {
            LabUser user = (await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false)).User;

            var factory = new HttpSnifferClientFactory();

            var msalPublicClient = PublicClientApplicationBuilder.Create(PublicClientID)
                                                                 .WithAuthority(TestConstants.AuthorityOrganizationsTenant)
                                                                 .WithRedirectUri(TestConstants.RedirectUri)
                                                                 .WithTestLogging()
                                                                 .WithHttpClientFactory(factory)
                                                                 .Build();

            var authResult = await msalPublicClient.AcquireTokenByUsernamePassword(s_oboServiceScope, user.Upn, user.GetOrFetchPassword())
                .ExecuteAsync()
                .ConfigureAwait(false);

            var confidentialApp = ConfidentialClientApplicationBuilder
                .Create(OboConfidentialClientID)
                .WithAuthority(new Uri("https://login.microsoftonline.com/" + authResult.TenantId), true)
                .WithClientSecret(_confidentialClientSecret)
                .WithTestLogging()
                .BuildConcrete();

            var userCacheRecorder = confidentialApp.UserTokenCache.RecordAccess();

            UserAssertion userAssertion = new UserAssertion(authResult.AccessToken);

            string atHash = userAssertion.AssertionHash;

            authResult = await confidentialApp.AcquireTokenOnBehalfOf(s_scopes, userAssertion)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            MsalAssert.AssertAuthResult(authResult, user);
            Assert.AreEqual(atHash, userCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheKey);
            Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);

            //Run OBO again. Should get token from cache
            authResult = await confidentialApp.AcquireTokenOnBehalfOf(s_scopes, userAssertion)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(authResult);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.IsNotNull(authResult.IdToken);
            Assert.IsFalse(userCacheRecorder.LastAfterAccessNotificationArgs.IsApplicationCache);
            Assert.IsTrue(userCacheRecorder.LastAfterAccessNotificationArgs.HasTokens);
            Assert.AreEqual(atHash, userCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheKey);
            Assert.AreEqual(TokenSource.Cache, authResult.AuthenticationResultMetadata.TokenSource);

            //Expire access tokens
            TokenCacheHelper.ExpireAllAccessTokens(confidentialApp.UserTokenCacheInternal);

            //Run OBO again. Should do OBO flow since the AT is expired and RTs aren't cached for normal OBO flow
            authResult = await confidentialApp.AcquireTokenOnBehalfOf(s_scopes, userAssertion)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(authResult);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.IsNotNull(authResult.IdToken);
            Assert.IsFalse(userCacheRecorder.LastAfterAccessNotificationArgs.IsApplicationCache);
            Assert.IsTrue(userCacheRecorder.LastAfterAccessNotificationArgs.HasTokens);
            Assert.AreEqual(atHash, userCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheKey);
            Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);
            AssertLastHttpContent("on_behalf_of");

            //creating second app with no refresh tokens
            var atItems = confidentialApp.UserTokenCacheInternal.Accessor.GetAllAccessTokens();
            var confidentialApp2 = ConfidentialClientApplicationBuilder
                .Create(OboConfidentialClientID)
                .WithAuthority(new Uri("https://login.microsoftonline.com/" + authResult.TenantId), true)
                .WithClientSecret(_confidentialClientSecret)
                .WithTestLogging()
                .WithHttpClientFactory(factory)
                .BuildConcrete();

            TokenCacheHelper.ExpireAccessToken(confidentialApp2.UserTokenCacheInternal, atItems.FirstOrDefault());

            //Should perform OBO flow since the access token is expired and the refresh token does not exist
            authResult = await confidentialApp2.AcquireTokenOnBehalfOf(s_scopes, userAssertion)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(authResult);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.IsNotNull(authResult.IdToken);
            Assert.IsFalse(userCacheRecorder.LastAfterAccessNotificationArgs.IsApplicationCache);
            Assert.IsTrue(userCacheRecorder.LastAfterAccessNotificationArgs.HasTokens);
            Assert.AreEqual(atHash, userCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheKey);
            Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);
            AssertLastHttpContent("on_behalf_of");

            TokenCacheHelper.ExpireAllAccessTokens(confidentialApp2.UserTokenCacheInternal);
            TokenCacheHelper.UpdateUserAssertions(confidentialApp2);

            //Should perform OBO flow since the access token and the refresh token contains the wrong user assertion hash
            authResult = await confidentialApp2.AcquireTokenOnBehalfOf(s_scopes, userAssertion)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(authResult);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.IsNotNull(authResult.IdToken);
            Assert.IsFalse(userCacheRecorder.LastAfterAccessNotificationArgs.IsApplicationCache);
            Assert.IsTrue(userCacheRecorder.LastAfterAccessNotificationArgs.HasTokens);
            Assert.AreEqual(atHash, userCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheKey);
            Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);
            AssertLastHttpContent("on_behalf_of");

            void AssertLastHttpContent(string content)
            {
                Assert.IsTrue(HttpSnifferClientFactory.LastHttpContentData.Contains(content));
                HttpSnifferClientFactory.LastHttpContentData = string.Empty;
            }
        }

        private async Task<IConfidentialClientApplication> RunOnBehalfOfTestAsync(
            LabUser user,
            bool silentCallShouldSucceed,
            bool forceRefresh = false)
        {
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
                    .AcquireTokenByUsernamePassword(s_oboServiceScope, user.Upn, user.GetOrFetchPassword())
                    //.AcquireTokenInteractive(s_oboServiceScope)
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
            AssertExtraHttpHeadersAreSent(factory);

            return cca;

            void AssertExtraHttpHeadersAreSent(HttpSnifferClientFactory factory)
            {
                //Validate CCS Routing header
                if (!factory.RequestsAndResponses.Any())
                {
                    return;
                }

                var (req, _) = factory.RequestsAndResponses.Single(x =>
                    x.Item1.RequestUri.AbsoluteUri.Contains("oauth2/v2.0/token") &&
                    x.Item2.StatusCode == HttpStatusCode.OK);

                Assert.IsTrue(req.Headers.TryGetValues(Constants.CcsRoutingHintHeader, out var values));
                Assert.AreEqual("oid:597f86cd-13f3-44c0-bece-a1e77ba43228@f645ad92-e38d-4d1a-b510-d1b09a74a8ca", values.First());
            }
        }

        private ConfidentialClientApplication BuildCca(string tenantId, bool withRegion = false)
        {
            var builder = ConfidentialClientApplicationBuilder
             .Create(OboConfidentialClientID)
             .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}"), true)
             .WithClientSecret(_confidentialClientSecret)
             .WithLegacyCacheCompatibility(false);

            if (withRegion)
            {
                builder
                    .WithAzureRegion(TestConstants.Region);
            }

            return builder.BuildConcrete();
        }
    }
}
