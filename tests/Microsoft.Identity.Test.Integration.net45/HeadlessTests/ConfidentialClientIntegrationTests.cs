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
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.net45.HeadlessTests
{
    [TestClass]
    public class ConfidentialClientIntegrationTests
    {
        private static readonly string[] s_scopes = { "User.Read" };
        private static readonly string[] s_oboServiceScope = { "api://23c64cd8-21e4-41dd-9756-ab9e2c23f58c/access_as_user" };

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        [TestMethod]
        public async Task ClientSecretAuthenticationAsync()
        {
            var keyvault = new KeyVaultSecretsProvider();
            var secret = keyvault.GetSecret("https://buildautomation.vault.azure.net/secrets/AzureADIdentityDivisionTestAgentSecret/e360740b3411452b887e6c3097cb1037").Value;
            var confidentialClientID = "16dab2ba-145d-4b1b-8569-bf4b9aed4dc8";
            var confidentialClientAuthority = "https://login.windows.net/72f988bf-86f1-41af-91ab-2d7cd011db47";
            string[] scope = { "https://vault.azure.net/.default" };

            var confidentialApp = ConfidentialClientApplicationBuilder
                .Create(confidentialClientID)
                .WithAuthority(new Uri(confidentialClientAuthority), true)
                .WithClientSecret(secret)
                .Build();

            var authResult = await confidentialApp.AcquireTokenForClient(scope)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            MsalAssert.AssertAuthResult(authResult);
        }

        [TestMethod]
        public async Task WebAPIAccessingGraphOnBehalfOfTestAsync()
        {
            var keyvault = new KeyVaultSecretsProvider();
            var secret = keyvault.GetSecret("https://buildautomation.vault.azure.net/secrets/IdentityDivisionDotNetOBOServiceSecret/243c858fe7b9411cbcf05a2a284d8a84").Value;
            var labResponse = LabUserHelper.GetSpecificUser("IDLAB@msidlab4.onmicrosoft.com");
            var user = labResponse.User;
            var publicClientID = "be9b0186-7dfd-448a-a944-f771029105bf";
            var confidentialClientID = "23c64cd8-21e4-41dd-9756-ab9e2c23f58c";

            SecureString securePassword = new NetworkCredential("", user.GetOrFetchPassword()).SecurePassword;

            var msalPublicClient = PublicClientApplicationBuilder.Create(publicClientID).WithAuthority(MsalTestConstants.AuthorityOrganizationsTenant).WithRedirectUri("urn:ietf:wg:oauth:2.0:oob").Build();

            AuthenticationResult authResult = await msalPublicClient
                .AcquireTokenByUsernamePassword(s_oboServiceScope, user.Upn, securePassword)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            var confidentialApp = ConfidentialClientApplicationBuilder
                .Create(confidentialClientID)
                .WithAuthority(new Uri(" https://login.microsoftonline.com/" + authResult.TenantId), true)
                .WithClientSecret(secret)
                .Build();

            authResult = await confidentialApp.AcquireTokenOnBehalfOf(s_scopes, new UserAssertion(authResult.AccessToken))
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            MsalAssert.AssertAuthResult(authResult, user);
        }
    }
}
