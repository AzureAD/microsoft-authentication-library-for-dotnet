// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common;
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
        private static readonly string[] s_keyvaultScope = { "https://vault.azure.net/.default" };
        private static readonly string[] s_adfsScopes = { string.Format(CultureInfo.CurrentCulture, "{0}/email openid", Adfs2019LabConstants.AppId) };
        //TODO: acquire scenario specific client ids from the lab resonse
        private const string _confidentialClientID = "16dab2ba-145d-4b1b-8569-bf4b9aed4dc8";

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
        }

        [TestMethod]
        public async Task ConfidentialClientWithCertificateTestAsync()
        {
            AuthenticationResult authResult;
            IConfidentialClientApplication confidentialApp;
            X509Certificate2 cert;
            var confidentialClientAuthority = "https://login.windows.net/72f988bf-86f1-41af-91ab-2d7cd011db47";

            cert = CertificateHelper.FindCertificateByThumbprint("79fbcbeb5cd28994e50daff8035bacf764b14306");
            if (cert == null)
            {
                throw new InvalidOperationException(
                    "Test setup error - cannot find a certificate in the My store for KeyVault. This is available for Microsoft employees only.");
            }

            confidentialApp = ConfidentialClientApplicationBuilder
                .Create(_confidentialClientID)
                .WithAuthority(new Uri(confidentialClientAuthority), true)
                .WithCertificate(cert)
                .Build();

            authResult = await confidentialApp
                .AcquireTokenForClient(s_keyvaultScope)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            MsalAssert.AssertAuthResult(authResult);
        }

        [TestMethod]
        public async Task ConfidentialClientWithClientSecretTestAsync()
        {
            var keyvault = new KeyVaultSecretsProvider();
            var secret = keyvault.GetSecret(MsalTestConstants.MsalCCAKeyVaultUri).Value;
            var confidentialClientAuthority = "https://login.windows.net/72f988bf-86f1-41af-91ab-2d7cd011db47";

            var confidentialApp = ConfidentialClientApplicationBuilder
                .Create(_confidentialClientID)
                .WithAuthority(new Uri(confidentialClientAuthority), true)
                .WithClientSecret(secret)
                .Build();

            var authResult = await confidentialApp.AcquireTokenForClient(s_keyvaultScope)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            MsalAssert.AssertAuthResult(authResult);
        }

        [TestMethod]
        public async Task WebAPIAccessingGraphOnBehalfOfUserTestAsync()
        {
            await RunOnBehalfOfTestAsync(LabUserHelper.GetSpecificUser("IDLAB@msidlab4.onmicrosoft.com")).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task WebAPIAccessingGraphOnBehalfOfADFS2019UserTestAsync()
        {
            UserQuery query = new UserQuery
            {
                FederationProvider = FederationProvider.ADFSv2019,
                IsMamUser = false,
                IsMfaUser = false,
                IsFederatedUser = true
            };

            await RunOnBehalfOfTestAsync(LabUserHelper.GetLabUserData(query)).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ClientSecretIntegrationTests")]
        public async Task AcquireTokenWithClientSecretFromAdfsAsync()
        {
            KeyVaultSecretsProvider secretProvider = new KeyVaultSecretsProvider();
            SecretBundle secret = secretProvider.GetSecret(Adfs2019LabConstants.ADFS2019ClientSecretURL);

            ConfidentialClientApplication msalConfidentialClient = ConfidentialClientApplicationBuilder.Create(Adfs2019LabConstants.ConfidentialClientId)
                                            .WithAdfsAuthority(Adfs2019LabConstants.Authority, true)
                                            .WithRedirectUri(Adfs2019LabConstants.ClientRedirectUri)
                                            .WithClientSecret(secret.Value)
                                            .BuildConcrete();

            //AuthenticationResult authResult = await msalConfidentialClient.AcquireTokenForClientAsync(AdfsScopes).ConfigureAwait(false);
            AuthenticationResult authResult = await msalConfidentialClient.AcquireTokenForClient(s_adfsScopes).ExecuteAsync().ConfigureAwait(false);
            Assert.IsNotNull(authResult);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.IsNull(authResult.IdToken);
        }

        private async Task RunOnBehalfOfTestAsync(LabResponse labResponse)
        {
            var user = labResponse.User;

            var keyvault = new KeyVaultSecretsProvider();
            var secret = keyvault.GetSecret(MsalTestConstants.MsalOBOKeyVaultUri).Value;
            //TODO: acquire scenario specific client ids from the lab resonse
            var publicClientID = "be9b0186-7dfd-448a-a944-f771029105bf";
            var oboConfidentialClientID = "23c64cd8-21e4-41dd-9756-ab9e2c23f58c";

            SecureString securePassword = new NetworkCredential("", user.GetOrFetchPassword()).SecurePassword;

            var msalPublicClient = PublicClientApplicationBuilder.Create(publicClientID).WithAuthority(MsalTestConstants.AuthorityOrganizationsTenant).WithRedirectUri("urn:ietf:wg:oauth:2.0:oob").Build();

            AuthenticationResult authResult = await msalPublicClient
                .AcquireTokenByUsernamePassword(s_oboServiceScope, user.Upn, securePassword)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            var confidentialApp = ConfidentialClientApplicationBuilder
                .Create(oboConfidentialClientID)
                .WithAuthority(new Uri("https://login.microsoftonline.com/" + authResult.TenantId), true)
                .WithClientSecret(secret)
                .Build();

            authResult = await confidentialApp.AcquireTokenOnBehalfOf(s_scopes, new UserAssertion(authResult.AccessToken))
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            MsalAssert.AssertAuthResult(authResult, user);
        }
    }
}
