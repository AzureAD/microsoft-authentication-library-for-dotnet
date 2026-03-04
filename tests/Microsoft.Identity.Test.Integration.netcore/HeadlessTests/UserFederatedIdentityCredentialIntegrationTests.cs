// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    /// <summary>
    /// Integration tests for the User Federated Identity Credential (UserFIC) flow.
    /// The same app ID is used to both acquire the assertion token and the final user token.
    /// Tenant and user UPN are retrieved from Key Vault secrets at class initialization.
    /// </summary>
    [TestClass]
    public class UserFederatedIdentityCredentialIntegrationTests
    {
        private const string ClientId = "979a25aa-0daf-41a5-bcad-cebec5c7c254";
        private static readonly string[] s_scopes = { "User.Read" };
        private const string TokenExchangeAudience = "api://AzureADTokenExchange/.default";

        private static string s_tenant;
        private static string s_authority;
        private static string s_userUpn;

        [ClassInitialize]
        public static void ClassInitialize(TestContext _)
        {
            s_tenant = LabResponseHelper.FetchSecretString("TSETenantDomain", LabResponseHelper.KeyVaultSecretsProviderMsid);
            s_userUpn = LabResponseHelper.FetchSecretString("FicUserUsername", LabResponseHelper.KeyVaultSecretsProviderMsid);
            s_authority = "https://login.microsoftonline.com/" + s_tenant;
        }

        [TestInitialize]
        public void TestInitialize()
        {
            ApplicationBase.ResetStateForTest();
        }

        /// <summary>
        /// Tests that the initial UserFIC token acquisition goes to the identity provider.
        /// </summary>
        [TestMethod]
        [RunOn(TargetFrameworks.NetCore)]
        public async Task UserFic_InitialAcquisition_FromIdentityProvider_Async()
        {
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            // Assertion app: same app ID, used to acquire the user_fic assertion
            var assertionApp = ConfidentialClientApplicationBuilder
                .Create(ClientId)
                .WithAuthority(s_authority)
                .WithCertificate(cert)
                .WithTestLogging()
                .Build();

            // Main app: same app ID, acquires the final user token via user_fic grant
            var app = ConfidentialClientApplicationBuilder
                .Create(ClientId)
                .WithAuthority(s_authority)
                .WithCertificate(cert)
                .WithTestLogging()
                .Build();

            var assertionProvider = FederatedCredentialProvider.FromConfidentialClient(assertionApp, TokenExchangeAudience);

            var result = await (app as IByUserFederatedIdentityCredential)
                .AcquireTokenByUserFederatedIdentityCredential(s_scopes, s_userUpn, assertionProvider)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.AccessToken);
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            Assert.IsNotNull(result.Account);
            Assert.IsTrue(
                string.Equals(s_userUpn, result.Account.Username, System.StringComparison.OrdinalIgnoreCase),
                $"Expected username '{s_userUpn}' but got '{result.Account.Username}'");
        }

        /// <summary>
        /// Tests that after initial acquisition, AcquireTokenSilent returns a cached token.
        /// </summary>
        [TestMethod]
        [RunOn(TargetFrameworks.NetCore)]
        public async Task UserFic_SilentCacheHit_ReturnsFromCache_Async()
        {
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            var assertionApp = ConfidentialClientApplicationBuilder
                .Create(ClientId)
                .WithAuthority(s_authority)
                .WithCertificate(cert, sendX5C: true)
                .WithTestLogging()
                .Build();

            var app = ConfidentialClientApplicationBuilder
                .Create(ClientId)
                .WithAuthority(s_authority)
                .WithCertificate(cert, sendX5C: true)
                .WithTestLogging()
                .Build();

            var assertionProvider = FederatedCredentialProvider.FromConfidentialClient(assertionApp, TokenExchangeAudience);

            // Step 1: Acquire token from identity provider
            var firstResult = await (app as IByUserFederatedIdentityCredential)
                .AcquireTokenByUserFederatedIdentityCredential(s_scopes, s_userUpn, assertionProvider)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(firstResult);
            Assert.AreEqual(TokenSource.IdentityProvider, firstResult.AuthenticationResultMetadata.TokenSource);

            // Step 2: Acquire token silently - should come from cache
            var account = firstResult.Account;
            var silentResult = await app
                .AcquireTokenSilent(s_scopes, account)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(silentResult);
            Assert.AreEqual(TokenSource.Cache, silentResult.AuthenticationResultMetadata.TokenSource,
                "Second call should hit the user token cache.");
            Assert.AreEqual(firstResult.AccessToken, silentResult.AccessToken);
        }

        /// <summary>
        /// Tests that WithForceRefresh(true) re-acquires from the identity provider even when cache is populated.
        /// </summary>
        [TestMethod]
        [RunOn(TargetFrameworks.NetCore)]
        public async Task UserFic_ForceRefresh_AcquiresFromIdentityProvider_Async()
        {
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            var assertionApp = ConfidentialClientApplicationBuilder
                .Create(ClientId)
                .WithAuthority(s_authority)
                .WithCertificate(cert, sendX5C: true)
                .WithTestLogging()
                .Build();

            var app = ConfidentialClientApplicationBuilder
                .Create(ClientId)
                .WithAuthority(s_authority)
                .WithCertificate(cert, sendX5C: true)
                .WithTestLogging()
                .Build();

            var assertionProvider = FederatedCredentialProvider.FromConfidentialClient(assertionApp, TokenExchangeAudience);

            // Step 1: Initial acquisition from identity provider
            var firstResult = await (app as IByUserFederatedIdentityCredential)
                .AcquireTokenByUserFederatedIdentityCredential(s_scopes, s_userUpn, assertionProvider)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, firstResult.AuthenticationResultMetadata.TokenSource);

            // Step 2: Force refresh - should go to identity provider again
            var forceRefreshResult = await (app as IByUserFederatedIdentityCredential)
                .AcquireTokenByUserFederatedIdentityCredential(s_scopes, s_userUpn, assertionProvider)
                .WithForceRefresh(true)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(forceRefreshResult);
            Assert.AreEqual(TokenSource.IdentityProvider, forceRefreshResult.AuthenticationResultMetadata.TokenSource,
                "WithForceRefresh(true) should bypass the cache and call the identity provider.");
        }
    }
}
