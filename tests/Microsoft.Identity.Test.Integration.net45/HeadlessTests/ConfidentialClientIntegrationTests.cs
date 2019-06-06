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
using System.Web;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

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
        private const string ConfidentialClientID = "16dab2ba-145d-4b1b-8569-bf4b9aed4dc8";
        private const string RedirectUri = "https://login.microsoftonline.com/common/oauth2/nativeclient";

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
        // Regression test for https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1193
        public async Task GetAuthorizationRequestUrl_ReturnsUri_Async()
        {
            var cca = ConfidentialClientApplicationBuilder
               .Create(ConfidentialClientID)
               .WithRedirectUri(RedirectUri)
               .Build();

            var uri1 = await cca.GetAuthorizationRequestUrl(s_scopes).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            var uri2 = await cca.GetAuthorizationRequestUrl(s_scopes).ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(uri1.Host, uri2.Host);
            Assert.AreEqual(uri1.LocalPath, uri2.LocalPath);

            var uriParams1 = HttpUtility.ParseQueryString(uri1.Query);
            var uriParams2 = HttpUtility.ParseQueryString(uri2.Query);

            CoreAssert.AreEqual("offline_access openid profile User.Read", uriParams1["scope"], uriParams2["scope"]);
            CoreAssert.AreEqual("code", uriParams1["response_type"], uriParams2["response_type"]);
            CoreAssert.AreEqual(ConfidentialClientID, uriParams1["client_id"], uriParams2["client_id"]);
            CoreAssert.AreEqual(RedirectUri, uriParams1["redirect_uri"], uriParams2["redirect_uri"]);
            CoreAssert.AreEqual("select_account", uriParams1["prompt"], uriParams2["prompt"]);

            Assert.AreEqual(uriParams1["x-client-CPU"], uriParams2["x-client-CPU"]);
            Assert.AreEqual(uriParams1["x-client-OS"], uriParams2["x-client-OS"]);
            Assert.AreEqual(uriParams1["x-client-Ver"], uriParams2["x-client-Ver"]);
            Assert.AreEqual(uriParams1["x-client-SKU"], uriParams2["x-client-SKU"]);
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
                .Create(ConfidentialClientID)
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
                .Create(ConfidentialClientID)
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
            await RunOnBehalfOfTestAsync(await LabUserHelper.GetSpecificUserAsync("IDLAB@msidlab4.onmicrosoft.com").ConfigureAwait(false)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task WebAPIAccessingGraphOnBehalfOfADFS2019UserTestAsync()
        {

            await RunOnBehalfOfTestAsync(await LabUserHelper.GetAdfsUserAsync(FederationProvider.ADFSv2019).ConfigureAwait(false)).ConfigureAwait(false);
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
