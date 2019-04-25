// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.net45.HeadlessTests
{
    [TestClass]
    public class OnBehalfOfTests
    {
        private static readonly string[] s_scopes = { "User.Read" };
        string[] resource = { "api://ae55a6cc-da5e-42f8-b75d-c37e41a1a0d9/access_as_user" };

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        [TestMethod]
        public async Task OnBehalfOfTestAsync()
        {
            await runTestAsync().ConfigureAwait(false);
        }

        private async Task runTestAsync()
        {
            var keyvault = new KeyVaultSecretsProvider();
            var labResponse = LabUserHelper.GetDefaultUser();
            var user = labResponse.User;

            SecureString securePassword = new NetworkCredential("", user.GetOrFetchPassword()).SecurePassword;

            var msalPublicClient = PublicClientApplicationBuilder.Create(labResponse.AppId).WithAuthority(MsalTestConstants.AuthorityOrganizationsTenant).Build();

            //AuthenticationResult authResult = await msalPublicClient.AcquireTokenByUsernamePasswordAsync(Scopes, user.Upn, securePassword).ConfigureAwait(false);
            AuthenticationResult authResult = await msalPublicClient
                .AcquireTokenByUsernamePassword(s_scopes, user.Upn, securePassword)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            var confidentialApp = ConfidentialClientApplicationBuilder
                .Create(labResponse.AppId)
                .WithAuthority(new Uri(" https://login.microsoftonline.com/" + authResult.TenantId), true)
                .WithClientSecret("")
                .Build();

            authResult = await confidentialApp.AcquireTokenOnBehalfOf(resource, new UserAssertion(authResult.AccessToken))
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);
        }
    }
}
