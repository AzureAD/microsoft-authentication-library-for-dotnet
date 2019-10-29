// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    [TestClass]
    public class ConfidentialClientIntegrationTests
    {
        private static readonly string[] s_scopes = { "User.Read" };
        private static readonly string[] s_oboServiceScope = { "api://23c64cd8-21e4-41dd-9756-ab9e2c23f58c/access_as_user" };
        private static readonly string[] s_keyvaultScope = { "https://vault.azure.net/.default" };
        private static readonly string[] s_adfsScopes = { "openid", "profile" };

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
            X509Certificate2 cert = GetCertificate();
            var confidentialClientAuthority = "https://login.windows.net/72f988bf-86f1-41af-91ab-2d7cd011db47";

            confidentialApp = ConfidentialClientApplicationBuilder
                .Create(ConfidentialClientID)
                .WithAuthority(new Uri(confidentialClientAuthority), true)
                .WithCertificate(cert)
                .Build();

            var appCacheRecorder = confidentialApp.AppTokenCache.RecordAccess();

            authResult = await confidentialApp
                .AcquireTokenForClient(s_keyvaultScope)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            MsalAssert.AssertAuthResult(authResult);
            appCacheRecorder.AssertAccessCounts(1, 1);
            Assert.IsTrue(appCacheRecorder.LastNotificationArgs.IsApplicationCache);

            // Call again to ensure token cache is hit
            authResult = await confidentialApp
               .AcquireTokenForClient(s_keyvaultScope)
               .ExecuteAsync()
               .ConfigureAwait(false);

            MsalAssert.AssertAuthResult(authResult);
            appCacheRecorder.AssertAccessCounts(2, 1);
            Assert.IsTrue(appCacheRecorder.LastNotificationArgs.IsApplicationCache);
        }

        [TestMethod]
        public async Task ConfidentialClientWithRSACertificateTestAsync()
        {
            AuthenticationResult authResult;
            IConfidentialClientApplication confidentialApp;
            X509Certificate2 cert = GetCertificate(true);
            var confidentialClientAuthority = "https://login.windows.net/72f988bf-86f1-41af-91ab-2d7cd011db47";

            confidentialApp = ConfidentialClientApplicationBuilder
                .Create(ConfidentialClientID)
                .WithAuthority(new Uri(confidentialClientAuthority), true)
                .WithCertificate(cert)
                .Build();
            var appCacheRecorder = confidentialApp.AppTokenCache.RecordAccess();

            authResult = await confidentialApp
                .AcquireTokenForClient(s_keyvaultScope)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            MsalAssert.AssertAuthResult(authResult);
            appCacheRecorder.AssertAccessCounts(1, 1);
            Assert.IsTrue(appCacheRecorder.LastNotificationArgs.IsApplicationCache);

            // Call again to ensure token cache is hit
            authResult = await confidentialApp
               .AcquireTokenForClient(s_keyvaultScope)
               .ExecuteAsync(CancellationToken.None)
               .ConfigureAwait(false);

            MsalAssert.AssertAuthResult(authResult);
            appCacheRecorder.AssertAccessCounts(2, 1);
            Assert.IsTrue(appCacheRecorder.LastNotificationArgs.IsApplicationCache);
        }

        [TestMethod]
        public async Task ConfidentialClientWithClientSecretTestAsync()
        {
            var keyvault = new KeyVaultSecretsProvider();
            var secret = keyvault.GetSecret(TestConstants.MsalCCAKeyVaultUri).Value;
            var confidentialClientAuthority = "https://login.windows.net/72f988bf-86f1-41af-91ab-2d7cd011db47";

            var confidentialApp = ConfidentialClientApplicationBuilder
                .Create(ConfidentialClientID)
                .WithAuthority(new Uri(confidentialClientAuthority), true)
                .WithClientSecret(secret)
                .Build();
            var appCacheRecorder = confidentialApp.AppTokenCache.RecordAccess();

            var authResult = await confidentialApp.AcquireTokenForClient(s_keyvaultScope)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            MsalAssert.AssertAuthResult(authResult);
            appCacheRecorder.AssertAccessCounts(1, 1);
            Assert.IsTrue(appCacheRecorder.LastNotificationArgs.IsApplicationCache);

            // Call again to ensure token cache is hit
            authResult = await confidentialApp.AcquireTokenForClient(s_keyvaultScope)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            MsalAssert.AssertAuthResult(authResult);
            appCacheRecorder.AssertAccessCounts(2, 1);
            Assert.IsTrue(appCacheRecorder.LastNotificationArgs.IsApplicationCache);
        }

        [TestMethod]
        public async Task ConfidentialClientWithNoDefaultClaimsTestAsync()
        {
            var keyvault = new KeyVaultSecretsProvider();
            var confidentialClientAuthority = "https://login.windows.net/72f988bf-86f1-41af-91ab-2d7cd011db47";
            var claims = GetClaims();

            X509Certificate2 cert = GetCertificate();

            var confidentialApp = ConfidentialClientApplicationBuilder
                .Create(ConfidentialClientID)
                .WithAuthority(new Uri(confidentialClientAuthority), true)
                .WithClientClaims(cert, claims, false)
                .Build();

            var authResult = await confidentialApp.AcquireTokenForClient(s_keyvaultScope)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            ValidateClaimsInAssertion(claims, ((ConfidentialClientApplication)confidentialApp).ClientCredential.CachedAssertion);

            MsalAssert.AssertAuthResult(authResult);
        }

        [TestMethod]
        public async Task ConfidentialClientWithDefaultClaimsTestAsync()
        {
            var keyvault = new KeyVaultSecretsProvider();
            var secret = keyvault.GetSecret(TestConstants.MsalCCAKeyVaultUri).Value;
            var confidentialClientAuthority = "https://login.windows.net/72f988bf-86f1-41af-91ab-2d7cd011db47";
            var claims = GetClaims(false);

            X509Certificate2 cert = GetCertificate();

            var confidentialApp = ConfidentialClientApplicationBuilder
                .Create(ConfidentialClientID)
                .WithAuthority(new Uri(confidentialClientAuthority), true)
                .WithClientClaims(cert, claims)
                .Build();

            var authResult = await confidentialApp.AcquireTokenForClient(s_keyvaultScope)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(((ConfidentialClientApplication)confidentialApp).ClientCredential.CachedAssertion);

            //checked if additional claim is in signed assertion
            var validClaim = claims.Where(x => x.Key == jsonToken.Claims.FirstOrDefault().Type && x.Value == jsonToken.Claims.FirstOrDefault().Value).FirstOrDefault();
            Assert.IsNotNull(validClaim);

            MsalAssert.AssertAuthResult(authResult);
        }

        [TestMethod]
        public async Task ConfidentialClientWithSignedAssertionTestAsync()
        {
            var keyvault = new KeyVaultSecretsProvider();
            var confidentialClientAuthority = "https://login.windows.net/72f988bf-86f1-41af-91ab-2d7cd011db47";
            var claims = GetClaims();

            var confidentialApp = ConfidentialClientApplicationBuilder
                .Create(ConfidentialClientID)
                .WithAuthority(new Uri(confidentialClientAuthority), true)
                .WithClientAssertion(GetSignedClientAssertion(ConfidentialClientID, claims))
                .Build();

            var appCacheRecorder = confidentialApp.AppTokenCache.RecordAccess();

            var authResult = await confidentialApp.AcquireTokenForClient(s_keyvaultScope)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            appCacheRecorder.AssertAccessCounts(1, 1);
            Assert.IsTrue(appCacheRecorder.LastNotificationArgs.IsApplicationCache);
            ValidateClaimsInAssertion(claims, ((ConfidentialClientApplication)confidentialApp).ClientCredential.SignedAssertion);
            MsalAssert.AssertAuthResult(authResult);

            // call again to ensure cache is hit
            authResult = await confidentialApp.AcquireTokenForClient(s_keyvaultScope)
               .ExecuteAsync(CancellationToken.None)
               .ConfigureAwait(false);

            appCacheRecorder.AssertAccessCounts(2, 1);
            Assert.IsTrue(appCacheRecorder.LastNotificationArgs.IsApplicationCache);
        }

        private void ValidateClaimsInAssertion(IDictionary<string, string> claims, string assertion)
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(assertion);

            Assert.AreEqual(jsonToken.Claims.Count(), claims.Count);

            foreach (KeyValuePair<string, string> claim in claims)
            {
                foreach (Claim assertionClaim in jsonToken.Claims)
                {
                    var validClaim = claims.Where(x => x.Key == assertionClaim.Type && x.Value == assertionClaim.Value).FirstOrDefault();
                    Assert.IsNotNull(validClaim);
                }
            }
        }

        internal static long ConvertToTimeT(DateTime time)
        {
            var startTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            TimeSpan diff = time - startTime;
            return (long)diff.TotalSeconds;
        }

        private static IDictionary<string, string> GetClaims(bool useDefaultClaims = true)
        {
            if (useDefaultClaims)
            {
                DateTime validFrom = DateTime.UtcNow;
                var nbf = ConvertToTimeT(validFrom);
                var exp = ConvertToTimeT(validFrom + TimeSpan.FromSeconds(TestConstants.JwtToAadLifetimeInSeconds));

                return new Dictionary<string, string>()
                {
                { "aud", TestConstants.ClientCredentialAudience },
                { "exp", exp.ToString(CultureInfo.InvariantCulture) },
                { "iss", ConfidentialClientID.ToString(CultureInfo.InvariantCulture) },
                { "jti", Guid.NewGuid().ToString() },
                { "nbf", nbf.ToString(CultureInfo.InvariantCulture) },
                { "sub", ConfidentialClientID.ToString(CultureInfo.InvariantCulture) },
                { "ip", "192.168.2.1" }
                };
            }
            else
            {
                return new Dictionary<string, string>()
                {
                    { "ip", "192.168.2.1" }
                };
            }
        }

        private static string GetSignedClientAssertion(string clientId, IDictionary<string, string> claims)
        {
#if NET_CORE
            var manager = new Client.Platforms.netcore.NetCoreCryptographyManager();
#else
            var manager = new Client.Platforms.net45.NetDesktopCryptographyManager();
#endif
            var jwtToken = new JsonWebToken(manager, clientId, TestConstants.ClientCredentialAudience, claims);
            var clientCredential = ClientCredentialWrapper.CreateWithCertificate(GetCertificate(), claims);
            return jwtToken.Sign(clientCredential, false);
        }

        private static X509Certificate2 GetCertificate(bool useRSACert = false)
        {
            X509Certificate2 cert = CertificateHelper.FindCertificateByThumbprint(useRSACert ? TestConstants.RSATestCertThumbprint : TestConstants.AutomationTestThumbprint);
            if (cert == null)
            {
                throw new InvalidOperationException(
                    "Test setup error - cannot find a certificate in the My store for KeyVault. This is available for Microsoft employees only.");
            }

            return cert;
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
            var secret = keyvault.GetSecret(TestConstants.MsalOBOKeyVaultUri).Value;
            //TODO: acquire scenario specific client ids from the lab resonse
            var publicClientID = "be9b0186-7dfd-448a-a944-f771029105bf";
            var oboConfidentialClientID = "23c64cd8-21e4-41dd-9756-ab9e2c23f58c";

            SecureString securePassword = new NetworkCredential("", user.GetOrFetchPassword()).SecurePassword;

            var msalPublicClient = PublicClientApplicationBuilder.Create(publicClientID).WithAuthority(TestConstants.AuthorityOrganizationsTenant).WithRedirectUri("urn:ietf:wg:oauth:2.0:oob").Build();

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
