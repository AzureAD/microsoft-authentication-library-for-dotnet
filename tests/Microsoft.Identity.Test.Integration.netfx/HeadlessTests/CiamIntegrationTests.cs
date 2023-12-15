// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Net;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    [TestClass]
    public class CiamIntegrationTests
    {
        private readonly string[] _ciamScopes = new[] { TestConstants.DefaultGraphScope };
        private const string _ciamRedirectUri = "http://localhost";

        [TestMethod]
        [DataRow("https://{0}.ciamlogin.com/", 0)] //https://tenantName.ciamlogin.com/
        [DataRow("https://{0}.ciamlogin.com/{1}.onmicrosoft.com", 1)] //https://tenantName.ciamlogin.com/tenantName.onmicrosoft.com
        [DataRow("https://{0}.ciamlogin.com/{1}", 2)] //https://tenantName.ciamlogin.com/tenantId
        public async Task ROPC_Ciam_Async(string authorityFormat, int authorityVersion)
        {
            //Get lab details
            var labResponse = await LabUserHelper.GetLabUserDataAsync(new UserQuery()
            {
                FederationProvider = FederationProvider.CIAM,
                SignInAudience = SignInAudience.AzureAdMyOrg,
                PublicClient = PublicClient.no
            }).ConfigureAwait(false);

            string authority = string.Empty;

            //Compute authority from format and lab response
            switch (authorityVersion)
            {
                case 0:
                    authority = string.Format(authorityFormat, labResponse.User.LabName);
                    break;

                case 1:
                    authority = string.Format(authorityFormat, labResponse.User.LabName, labResponse.User.LabName);
                    break;

                case 2:
                    authority = string.Format(authorityFormat, labResponse.User.LabName, labResponse.Lab.TenantId);
                    break;
            }

            //Acquire tokens
            var msalPublicClient = PublicClientApplicationBuilder
                .Create(labResponse.App.AppId)
                .WithAuthority(authority, false)
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
        [DataRow("https://{0}.ciamlogin.com/", 0)] //https://tenantName.ciamlogin.com/
        [DataRow("https://{0}.ciamlogin.com/{1}.onmicrosoft.com", 1)] //https://tenantName.ciamlogin.com/tenantName.onmicrosoft.com
        [DataRow("https://{0}.ciamlogin.com/{1}", 2)] //https://tenantName.ciamlogin.com/tenantId
        public async Task ClientCredentialWithClientSecret_Ciam_Async(string authorityFormat, int authorityVersion)
        {
            //Get lab details
            var labResponse = await LabUserHelper.GetLabUserDataAsync(new UserQuery()
            {
                FederationProvider = FederationProvider.CIAM,
                SignInAudience = SignInAudience.AzureAdMyOrg,
                PublicClient = PublicClient.no
            }).ConfigureAwait(false);

            string authority = string.Empty;

            //Compute authority from format and lab response
            switch (authorityVersion)
            {
                case 0:
                    authority = string.Format(authorityFormat, labResponse.User.LabName);
                    break;

                case 1:
                    authority = string.Format(authorityFormat, labResponse.User.LabName, labResponse.User.LabName);
                    break;

                case 2:
                    authority = string.Format(authorityFormat, labResponse.User.LabName, labResponse.Lab.TenantId);
                    break;
            }

            //Acquire tokens
            var msalConfidentialClient = ConfidentialClientApplicationBuilder
                .Create(labResponse.App.AppId)
                .WithClientSecret(GetCiamSecret())
                .WithAuthority(authority, false)
                .WithRedirectUri(_ciamRedirectUri)
                .Build();

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

        private string GetCiamSecret()
        {
            KeyVaultSecretsProvider provider = new KeyVaultSecretsProvider();
            return provider.GetSecretByName("msidlabciam2-cc").Value;
        }
    }
}
