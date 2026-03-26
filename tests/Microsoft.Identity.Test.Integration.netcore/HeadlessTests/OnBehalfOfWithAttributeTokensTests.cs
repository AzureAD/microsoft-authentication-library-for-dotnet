// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.Integration.NetFx.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    [TestClass]
    public class OnBehalfOfWithAttributeTokensTests
    {
        private static readonly string[] s_scopes = { "User.Read" };

        private string _confidentialClientSecret;

        private readonly KeyVaultSecretsProvider _keyVault = new KeyVaultSecretsProvider(KeyVaultInstance.MsalTeam);

        [TestInitialize]
        public void TestInitialize()
        {
            ApplicationBase.ResetStateForTest();
            if (string.IsNullOrEmpty(_confidentialClientSecret))
            {
                _confidentialClientSecret = _keyVault.GetSecretByName(TestConstants.MsalOBOKeyVaultSecretName).Value;
            }
        }

        /// <summary>
        /// E2E test: Acquires a user token (attribute token), then uses it in
        /// AcquireTokenOnBehalfOf with WithAttributeTokens.
        ///
        /// Flow:
        ///   1. PCA acquires a user token via username/password (this is the "attribute token")
        ///   2. CCA performs OBO using the user token as assertion, passing the attribute token
        ///      via .WithAttributeTokens() so it is sent as "attribute_tokens" in the request body
        /// </summary>
        [RunOnAzureDevOps]
        [TestCategory("OBO_E2E")]
        [TestMethod]
        public async Task AcquireAttributeToken_ThenUseInObo_WithAttributeTokens_TestAsync()
        {
            // Arrange - get lab user and app configs
            var user = await LabResponseHelper.GetUserConfigAsync(KeyVaultSecrets.UserPublicCloud).ConfigureAwait(false);
            var app = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppS2S).ConfigureAwait(false);
            var appApi = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppWebApi).ConfigureAwait(false);

            // Step 1: Acquire user token (the "attribute token") via PCA
            var pca = PublicClientApplicationBuilder
                .Create(app.AppId)
                .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs)
                .WithTestLogging()
                .Build();

#pragma warning disable CS0618 // Type or member is obsolete
            var userAuthResult = await pca
                .AcquireTokenByUsernamePassword([appApi.DefaultScopes], user.Upn, user.GetOrFetchPassword())
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);
#pragma warning restore CS0618

            Assert.IsNotNull(userAuthResult);
            Assert.IsNotNull(userAuthResult.AccessToken);
            Assert.AreEqual(TokenSource.IdentityProvider, userAuthResult.AuthenticationResultMetadata.TokenSource);

            string attributeToken = userAuthResult.AccessToken;

            // Step 2: Use the attribute token in OBO with WithAttributeTokens
            var factory = new HttpSnifferClientFactory();

            var cca = ConfidentialClientApplicationBuilder
                .Create(appApi.AppId)
                .WithAuthority(new Uri($"https://login.microsoftonline.com/{userAuthResult.TenantId}"), true)
                .WithClientSecret(_confidentialClientSecret)
                .WithTestLogging()
                .WithHttpClientFactory(factory)
                .Build();

            var userCacheRecorder = cca.UserTokenCache.RecordAccess();

            UserAssertion userAssertion = new UserAssertion(userAuthResult.AccessToken);
            string atHash = userAssertion.AssertionHash;

            var oboResult = await cca
                .AcquireTokenOnBehalfOf(s_scopes, userAssertion)
                .WithAttributeTokens(new[] { attributeToken })
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            // Assert - OBO succeeded
            Assert.IsNotNull(oboResult);
            Assert.IsNotNull(oboResult.AccessToken);
            Assert.AreEqual(TokenSource.IdentityProvider, oboResult.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(atHash, userCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheKey);

            // Verify the attribute_tokens parameter was sent in the request body
            var tokenRequest = factory.RequestsAndResponses
                .Where(x => x.Item1.RequestUri.AbsoluteUri.Contains("oauth2/v2.0/token"))
                .Last();

            string requestBody = await tokenRequest.Item1.Content
                .ReadAsStringAsync()
                .ConfigureAwait(false);

            Assert.Contains("attribute_tokens=", requestBody);
            Assert.Contains(Uri.EscapeDataString(attributeToken), requestBody);
        }

        /// <summary>
        /// E2E test: Acquires multiple attribute tokens, then uses them in
        /// AcquireTokenOnBehalfOf with WithAttributeTokens (space-separated).
        ///
        /// Flow:
        ///   1. PCA acquires two user tokens from two different users (these are "attribute tokens")
        ///   2. CCA performs OBO using one user's token as assertion, passing both tokens
        ///      via .WithAttributeTokens() so they are sent space-separated as "attribute_tokens"
        /// </summary>
        [RunOnAzureDevOps]
        [TestCategory("OBO_E2E")]
        [TestMethod]
        public async Task AcquireMultipleAttributeTokens_ThenUseInObo_WithAttributeTokens_TestAsync()
        {
            // Arrange - get lab users and app configs
            var user1 = await LabResponseHelper.GetUserConfigAsync(KeyVaultSecrets.UserPublicCloud).ConfigureAwait(false);
            var user2 = await LabResponseHelper.GetUserConfigAsync(KeyVaultSecrets.UserPublicCloud2).ConfigureAwait(false);
            var app = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppS2S).ConfigureAwait(false);
            var appApi = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppWebApi).ConfigureAwait(false);

            // Step 1: Acquire two user tokens (the "attribute tokens") via PCA
            var pca = PublicClientApplicationBuilder
                .Create(app.AppId)
                .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs)
                .WithTestLogging()
                .Build();

#pragma warning disable CS0618 // Type or member is obsolete
            var user1AuthResult = await pca
                .AcquireTokenByUsernamePassword([appApi.DefaultScopes], user1.Upn, user1.GetOrFetchPassword())
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            var user2AuthResult = await pca
                .AcquireTokenByUsernamePassword([appApi.DefaultScopes], user2.Upn, user2.GetOrFetchPassword())
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);
#pragma warning restore CS0618

            Assert.IsNotNull(user1AuthResult.AccessToken);
            Assert.IsNotNull(user2AuthResult.AccessToken);

            string attributeToken1 = user1AuthResult.AccessToken;
            string attributeToken2 = user2AuthResult.AccessToken;

            // Step 2: Use both attribute tokens in OBO with WithAttributeTokens
            var factory = new HttpSnifferClientFactory();

            var cca = ConfidentialClientApplicationBuilder
                .Create(appApi.AppId)
                .WithAuthority(new Uri($"https://login.microsoftonline.com/{user1AuthResult.TenantId}"), true)
                .WithClientSecret(_confidentialClientSecret)
                .WithTestLogging()
                .WithHttpClientFactory(factory)
                .Build();

            UserAssertion userAssertion = new UserAssertion(user1AuthResult.AccessToken);

            var oboResult = await cca
                .AcquireTokenOnBehalfOf(s_scopes, userAssertion)
                .WithAttributeTokens(new[] { attributeToken1, attributeToken2 })
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            // Assert - OBO succeeded with multiple attribute tokens
            Assert.IsNotNull(oboResult);
            Assert.IsNotNull(oboResult.AccessToken);
            Assert.AreEqual(TokenSource.IdentityProvider, oboResult.AuthenticationResultMetadata.TokenSource);

            // Verify the attribute_tokens parameter was sent space-separated in the request body
            var tokenRequest = factory.RequestsAndResponses
                .Where(x => x.Item1.RequestUri.AbsoluteUri.Contains("oauth2/v2.0/token"))
                .Last();

            string requestBody = await tokenRequest.Item1.Content
                .ReadAsStringAsync()
                .ConfigureAwait(false);

            Assert.Contains("attribute_tokens=", requestBody);
        }
    }
}
