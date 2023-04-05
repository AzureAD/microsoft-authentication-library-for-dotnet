// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;
using System.Net;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.Integration.net45.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.NetFx.HeadlessTests
{
    [TestClass]
    public class CiamUsernamePasswordIntegrationTests
    {
        private const string _extraQParams = "dc=ESTS-PUB-EUS-AZ1-FD000-TEST1";
        private readonly string[] _ciamScopes = new[] { "openid" };
        private const string _ciamRedirectUri = "http://localhost";

        [TestMethod]
        [DataRow("https://{0}.ciamlogin.com/", 0)]
        [DataRow("https://{0}.ciamlogin.com/{1}.onmicrosoft.com", 1)]
        [DataRow("https://{0}.ciamlogin.com/{1}", 2)]
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
                .WithExtraQueryParameters(_extraQParams)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(result.AccessToken);
            Assert.AreEqual(result.AuthenticationResultMetadata.TokenSource, TokenSource.IdentityProvider);
            Assert.AreEqual($"{labResponse.User.LabName}.ciamlogin.com".ToLower(), result.Account.Environment);

            //Refresh tokens
            var accounts = await msalPublicClient.GetAccountsAsync().ConfigureAwait(false);

            result = await msalPublicClient
                .AcquireTokenSilent(_ciamScopes, accounts.FirstOrDefault())
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(result.AccessToken);
            Assert.AreEqual(result.AuthenticationResultMetadata.TokenSource, TokenSource.Cache);
            Assert.AreEqual($"{labResponse.User.LabName}.ciamlogin.com".ToLower(), result.Account.Environment);
        }
    }
}
