// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Test.Common.Core.Helpers;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    /// <summary>
    /// Tests for customer identity and access management (CIAM).
    /// </summary>
    /// <remarks>
    /// Custom user domain (CUD): <c>https://login.{customhost}}.com/{tenant}/v2.0/</c>.
    /// Standard: <c>https://{tenant}.ciamlogin.com</c>, <c>https://{tenant}.ciamlogin.com/{tenant}</c>, <c>https://{tenant}.ciamlogin.com/{tenantGuid}</c>
    /// </remarks>
    [TestClass]
    public class CiamIntegrationTests
    {
        private readonly string[] _ciamScopes = new[] { TestConstants.DefaultGraphScope };
        private const string _ciamRedirectUri = "http://localhost";

        [TestMethod]
        public async Task ROPC_Ciam_StandardDomains_CompletesSuccessfully()
        {
            string authority;
            //Get lab details
            var labResponse = await LabUserHelper.GetLabUserDataAsync(new UserQuery()
            {
                FederationProvider = FederationProvider.CIAMCUD,
                SignInAudience = SignInAudience.AzureAdMyOrg
            }).ConfigureAwait(false);

            //https://tenantName.ciamlogin.com/
            authority = string.Format("https://{0}.ciamlogin.com/", labResponse.User.LabName);
            await RunCiamRopcTest(authority, labResponse).ConfigureAwait(false);

            //https://tenantName.ciamlogin.com/tenantName.onmicrosoft.com
            authority = string.Format("https://{0}.ciamlogin.com/{1}.onmicrosoft.com", labResponse.User.LabName, labResponse.User.LabName);
            await RunCiamRopcTest(authority, labResponse).ConfigureAwait(false);

            //https://tenantName.ciamlogin.com/tenantGuid
            authority = string.Format("https://{0}.ciamlogin.com/{1}", labResponse.User.LabName, labResponse.Lab.TenantId);
            await RunCiamRopcTest(authority, labResponse).ConfigureAwait(false);
        }

        private async Task RunCiamRopcTest(string authority, LabResponse labResponse)
        {
            //Acquire tokens
            var msalPublicClient = PublicClientApplicationBuilder
                .Create(labResponse.App.AppId)
                .WithAuthority(new Uri(authority), false)
                .WithRedirectUri(_ciamRedirectUri)
                .Build();

            var result = await msalPublicClient
                .AcquireTokenByUsernamePassword(_ciamScopes, labResponse.User.Upn, labResponse.User.GetOrFetchPassword())
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(result.AccessToken);
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual($"{labResponse.User.LabName}{Constants.CiamAuthorityHostSuffix}".ToLower(), result.Account.Environment);

            //Fetch cached tokens
            var accounts = await msalPublicClient.GetAccountsAsync().ConfigureAwait(false);

            result = await msalPublicClient
                .AcquireTokenSilent(_ciamScopes, accounts.FirstOrDefault())
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(result.AccessToken);
            Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual($"{labResponse.User.LabName}{Constants.CiamAuthorityHostSuffix}".ToLower(), result.Account.Environment);
        }

        [TestMethod]
        public async Task ClientCredentialCiam_WithClientCredentials_ReturnsValidTokens()
        {
            string authority;
            //Get lab details
            var labResponse = await LabUserHelper.GetLabUserDataAsync(new UserQuery()
            {
                FederationProvider = FederationProvider.CIAMCUD,
                SignInAudience = SignInAudience.AzureAdMyOrg
            }).ConfigureAwait(false);


            //https://tenantName.ciamlogin.com/
            authority = string.Format("https://{0}.ciamlogin.com/", labResponse.User.LabName);
            await RunCiamCCATest(authority, labResponse.App.AppId).ConfigureAwait(false);

            //https://tenantName.ciamlogin.com/tenantName.onmicrosoft.com
            authority = string.Format("https://{0}.ciamlogin.com/{1}.onmicrosoft.com", labResponse.User.LabName, labResponse.User.LabName);
            await RunCiamCCATest(authority, labResponse.App.AppId).ConfigureAwait(false);

            //https://tenantName.ciamlogin.com/tenantGuid
            authority = string.Format("https://{0}.ciamlogin.com/{1}", labResponse.User.LabName, labResponse.Lab.TenantId);
            await RunCiamCCATest(authority, labResponse.App.AppId).ConfigureAwait(false);

            //Ciam CUD
            authority = "https://login.msidlabsciam.com/fe362aec-5d43-45d1-b730-9755e60dc3b9/v2.0/";
            string ciamClient = "b244c86f-ed88-45bf-abda-6b37aa482c79";
            await RunCiamCCATest(authority, ciamClient).ConfigureAwait(false);
        }

        private async Task RunCiamCCATest(string authority, string appId)
        {
            //Acquire tokens
            var msalConfidentialClientBuilder = ConfidentialClientApplicationBuilder
                .Create(appId)
                .WithCertificate(CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName))
                .WithExperimentalFeatures();

            if (authority.Contains(Constants.CiamAuthorityHostSuffix))
            {
                msalConfidentialClientBuilder.WithAuthority(authority, false);
            }
            else
            {
                msalConfidentialClientBuilder.WithOidcAuthority(authority);
            }


            var msalConfidentialClient = msalConfidentialClientBuilder.Build();

            var result = await msalConfidentialClient
                .AcquireTokenForClient(new[] { TestConstants.DefaultGraphScope })
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(result.AccessToken);
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

            //Fetch cached tokens
            result = await msalConfidentialClient
                .AcquireTokenForClient(_ciamScopes)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(result.AccessToken);
            Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
        }

        [TestMethod]
        public async Task OBOCiam_CustomDomain_ReturnsValidTokens()
        {
            string authorityCud = "https://login.msidlabsciam.com/fe362aec-5d43-45d1-b730-9755e60dc3b9/v2.0/";
            string ciamWebApi = "634de702-3173-4a71-b336-a4fab786a479";

            //Get lab details
            LabResponse labResponse = await LabUserHelper.GetLabUserDataAsync(new UserQuery()
            {
                FederationProvider = FederationProvider.CIAMCUD,
                SignInAudience = SignInAudience.AzureAdMyOrg
            }).ConfigureAwait(false);

            //Acquire tokens
            var msalPublicClient = PublicClientApplicationBuilder
                .Create(labResponse.App.AppId)
                .WithAuthority(labResponse.App.Authority, false)
                .WithRedirectUri(labResponse.App.RedirectUri)
                .Build();

            var result = await msalPublicClient
                .AcquireTokenByUsernamePassword(new[] { labResponse.App.DefaultScopes }, labResponse.User.Upn, labResponse.User.GetOrFetchPassword())
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(result.AccessToken);
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

            var userAssertion = new UserAssertion(result.AccessToken);
            string atHash = userAssertion.AssertionHash;

            //Acquire tokens for OBO
            var msalConfidentialClient = ConfidentialClientApplicationBuilder
                .Create(ciamWebApi)
                .WithCertificate(CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName))
                .WithAuthority(authorityCud, false)
                .WithRedirectUri(_ciamRedirectUri)
                .BuildConcrete();

            var userCacheRecorder = msalConfidentialClient.UserTokenCache.RecordAccess();

            var resultObo = await msalConfidentialClient.AcquireTokenOnBehalfOf(new[] { "User.Read" }, userAssertion)
                                  .ExecuteAsync(CancellationToken.None)
                                  .ConfigureAwait(false);

            Assert.IsNotNull(resultObo.AccessToken);
            Assert.IsFalse(userCacheRecorder.LastAfterAccessNotificationArgs.IsApplicationCache);
            Assert.IsTrue(userCacheRecorder.LastAfterAccessNotificationArgs.HasTokens);
            Assert.AreEqual(atHash, userCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheKey);
            Assert.AreEqual(TokenSource.IdentityProvider, resultObo.AuthenticationResultMetadata.TokenSource);

            //Fetch cached tokens
            resultObo = await msalConfidentialClient.AcquireTokenOnBehalfOf(new[] { "User.Read" }, userAssertion)
                                  .ExecuteAsync(CancellationToken.None)
                                  .ConfigureAwait(false);

            Assert.IsNotNull(resultObo.AccessToken);
            Assert.IsFalse(userCacheRecorder.LastAfterAccessNotificationArgs.IsApplicationCache);
            Assert.IsTrue(userCacheRecorder.LastAfterAccessNotificationArgs.HasTokens);
            Assert.AreEqual(atHash, userCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheKey);
            Assert.AreEqual(TokenSource.Cache, resultObo.AuthenticationResultMetadata.TokenSource);
        }

        private string GetCiamSecret()
        {
            KeyVaultSecretsProvider provider = new KeyVaultSecretsProvider();
            return provider.GetSecretByName("msidlabciam2-cc").Value;
        }
    }
}
