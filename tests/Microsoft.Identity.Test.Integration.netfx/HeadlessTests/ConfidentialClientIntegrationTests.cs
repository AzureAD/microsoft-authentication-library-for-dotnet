// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.Integration.net45.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    [TestClass]
    public class ConfidentialClientIntegrationTests
    {
        private static readonly string[] s_scopes = { "User.Read" };
        private static readonly string[] s_publicCloudOBOServiceScope = { "api://23c64cd8-21e4-41dd-9756-ab9e2c23f58c/access_as_user" };
        private static readonly string[] s_arlingtonOBOServiceScope = { "https://arlmsidlab1.us/IDLABS_APP_Confidential_Client/user_impersonation" };
        private static readonly string[] s_keyvaultScope = { "https://vault.azure.net/.default" };
        private static readonly string[] s_adfsScopes = { "openid", "profile" };

        //TODO: acquire scenario specific client ids from the lab resonse
        private const string PublicCloudPublicClientIDOBO = "be9b0186-7dfd-448a-a944-f771029105bf";
        private const string PublicCloudConfidentialClientIDOBO = "23c64cd8-21e4-41dd-9756-ab9e2c23f58c";
        private const string PublicCloudConfidentialClientID = "16dab2ba-145d-4b1b-8569-bf4b9aed4dc8";
        private const string ArlingtonConfidentialClientIDOBO = "c0555d2d-02f2-4838-802e-3463422e571d";
        private const string ArlingtonPublicClientIDOBO = "cb7faed4-b8c0-49ee-b421-f5ed16894c83";
        private const string ArlingtonAuthority = "https://login.microsoftonline.us/45ff0c17-f8b5-489b-b7fd-2fedebbec0c4";
        //The following client ids are for applications that are within PPE
        private const string OBOClientPpeClientID = "9793041b-9078-4942-b1d2-babdc472cc0c";
        private const string OBOServicePpeClientID = "c84e9c32-0bc9-4a73-af05-9efe9982a322";
        private const string OBOServiceDownStreamApiPpeClientID = "23d08a1e-1249-4f7c-b5a5-cb11f29b6923";
        private const string PPEAuthenticationAuthority = "https://login.windows-ppe.net/f686d426-8d16-42db-81b7-ab578e110ccd";

        private const string PublicCloudHost = "https://login.microsoftonline.com/";
        private const string ArlingtonCloudHost = "https://login.microsoftonline.us/";

        private const string RedirectUri = "https://login.microsoftonline.com/common/oauth2/nativeclient";
        private const string PublicCloudTestAuthority = "https://login.windows.net/72f988bf-86f1-41af-91ab-2d7cd011db47";
        private const string AdfsCertName = "IDLABS-APP-Confidential-Client-Cert-OnPrem";
        private KeyVaultSecretsProvider _keyVault;
        private static string s_publicCloudCcaSecret;
        private static string s_arlingtonCCASecret;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();

            if (_keyVault == null)
            {
                _keyVault = new KeyVaultSecretsProvider();
                s_publicCloudCcaSecret = _keyVault.GetSecret(TestConstants.MsalCCAKeyVaultUri).Value;
                s_arlingtonCCASecret = _keyVault.GetSecret(TestConstants.MsalArlingtonCCAKeyVaultUri).Value;
            }
        }

        [TestMethod]
        // Regression test for https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1193
        public async Task GetAuthorizationRequestUrl_ReturnsUri_Async()
        {
            var cca = ConfidentialClientApplicationBuilder
               .Create(PublicCloudConfidentialClientID)
               .WithClientSecret(s_publicCloudCcaSecret)
               .WithRedirectUri(RedirectUri)
               .WithTestLogging()
               .Build();

            var uri1 = await cca.GetAuthorizationRequestUrl(s_scopes).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            var uri2 = await cca.GetAuthorizationRequestUrl(s_scopes).ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(uri1.Host, uri2.Host);
            Assert.AreEqual(uri1.LocalPath, uri2.LocalPath);

            var uriParams1 = uri1.ParseQueryString();
            var uriParams2 = uri2.ParseQueryString();

            CollectionAssert.AreEquivalent(
                "offline_access openid profile User.Read".Split(' '),
                uriParams1["scope"].Split(' '));
            CollectionAssert.AreEquivalent(
                "offline_access openid profile User.Read".Split(' '),
                uriParams2["scope"].Split(' '));
            CoreAssert.AreEqual("code", uriParams1["response_type"], uriParams2["response_type"]);
            CoreAssert.AreEqual(PublicCloudConfidentialClientID, uriParams1["client_id"], uriParams2["client_id"]);
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
            var confidentialClientAuthority = PublicCloudTestAuthority;

            confidentialApp = ConfidentialClientApplicationBuilder
                .Create(PublicCloudConfidentialClientID)
                .WithAuthority(new Uri(confidentialClientAuthority), true)
                .WithCertificate(cert)
                .WithTestLogging()
                .Build();

            var appCacheRecorder = confidentialApp.AppTokenCache.RecordAccess();

            authResult = await confidentialApp
                .AcquireTokenForClient(s_keyvaultScope)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            MsalAssert.AssertAuthResult(authResult);
            appCacheRecorder.AssertAccessCounts(1, 1);
            Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);
            Assert.IsTrue(appCacheRecorder.LastAfterAccessNotificationArgs.IsApplicationCache);
            Assert.IsTrue(appCacheRecorder.LastAfterAccessNotificationArgs.HasTokens);
            Assert.IsTrue(authResult.AuthenticationResultMetadata.DurationTotalInMs > 0);
            Assert.IsTrue(authResult.AuthenticationResultMetadata.DurationInHttpInMs > 0);

            // Call again to ensure token cache is hit
            authResult = await confidentialApp
               .AcquireTokenForClient(s_keyvaultScope)
               .ExecuteAsync()
               .ConfigureAwait(false);

            MsalAssert.AssertAuthResult(authResult);
            Assert.IsTrue(authResult.AuthenticationResultMetadata.DurationTotalInMs > 0);
            Assert.IsTrue(authResult.AuthenticationResultMetadata.DurationInHttpInMs == 0);

            appCacheRecorder.AssertAccessCounts(2, 1);
            Assert.AreEqual(TokenSource.Cache, authResult.AuthenticationResultMetadata.TokenSource);
            Assert.IsTrue(appCacheRecorder.LastAfterAccessNotificationArgs.IsApplicationCache);
            Assert.IsTrue(appCacheRecorder.LastAfterAccessNotificationArgs.HasTokens);
        }

        [TestMethod]
        public async Task ConfidentialClientWithRSACertificateTestAsync()
        {
            AuthenticationResult authResult;
            IConfidentialClientApplication confidentialApp;
            X509Certificate2 cert = GetCertificate(true);

            confidentialApp = ConfidentialClientApplicationBuilder
                .Create(PublicCloudConfidentialClientID)
                .WithCertificate(cert)
                .Build();
            var appCacheRecorder = confidentialApp.AppTokenCache.RecordAccess();

            authResult = await confidentialApp
                .AcquireTokenForClient(s_keyvaultScope)
                .WithAuthority(PublicCloudTestAuthority, true) // authority can also be specified at request level
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            MsalAssert.AssertAuthResult(authResult);
            appCacheRecorder.AssertAccessCounts(1, 1);
            Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);
            Assert.IsTrue(appCacheRecorder.LastAfterAccessNotificationArgs.IsApplicationCache);
            Assert.AreEqual(
                GetExpectedCacheKey(PublicCloudConfidentialClientID, Authority.CreateAuthority(PublicCloudTestAuthority, false).TenantId),
                appCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheKey);

            // Call again to ensure token cache is hit
            authResult = await confidentialApp
               .AcquireTokenForClient(s_keyvaultScope)
               .WithAuthority(PublicCloudTestAuthority, true) // authority can also be specified at request level
               .ExecuteAsync(CancellationToken.None)
               .ConfigureAwait(false);

            MsalAssert.AssertAuthResult(authResult);
            appCacheRecorder.AssertAccessCounts(2, 1);
            Assert.AreEqual(TokenSource.Cache, authResult.AuthenticationResultMetadata.TokenSource);
            Assert.IsTrue(appCacheRecorder.LastAfterAccessNotificationArgs.IsApplicationCache);
            Assert.AreEqual(
                GetExpectedCacheKey(PublicCloudConfidentialClientID, Authority.CreateAuthority(PublicCloudTestAuthority, false).TenantId),
                appCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheKey);
        }

        private string GetExpectedCacheKey(string clientId, string tenantId)
        {
            return $"{clientId}_{tenantId ?? ""}_AppTokenCache";
        }

        [TestMethod]
        public async Task ConfidentialClientWithClientSecretTestAsync()
        {
            await RunTestWithClientSecretAsync(PublicCloudConfidentialClientID,
                                                           PublicCloudTestAuthority,
                                                           s_publicCloudCcaSecret).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory(TestCategories.Arlington)]
        public async Task ArlingtonConfidentialClientWithClientSecretTestAsync()
        {
            await RunTestWithClientSecretAsync(ArlingtonConfidentialClientIDOBO,
                                                           ArlingtonAuthority,
                                                           s_arlingtonCCASecret).ConfigureAwait(false);
        }

        public async Task RunTestWithClientSecretAsync(string clientID, string confidentialClientAuthority, string secret)
        {
            var confidentialApp = ConfidentialClientApplicationBuilder
                .Create(clientID)
                .WithAuthority(new Uri(confidentialClientAuthority), true)
                .WithClientSecret(secret)
                .WithTestLogging()
                .Build();
            var appCacheRecorder = confidentialApp.AppTokenCache.RecordAccess();

            var authResult = await confidentialApp.AcquireTokenForClient(s_keyvaultScope)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            MsalAssert.AssertAuthResult(authResult);
            appCacheRecorder.AssertAccessCounts(1, 1);
            Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);
            Assert.IsTrue(appCacheRecorder.LastAfterAccessNotificationArgs.IsApplicationCache);
            Assert.AreEqual(
                GetExpectedCacheKey(clientID, Authority.CreateAuthority(confidentialClientAuthority, false).TenantId),
                appCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheKey);

            // Call again to ensure token cache is hit
            authResult = await confidentialApp.AcquireTokenForClient(s_keyvaultScope)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            MsalAssert.AssertAuthResult(authResult);
            appCacheRecorder.AssertAccessCounts(2, 1);
            Assert.AreEqual(TokenSource.Cache, authResult.AuthenticationResultMetadata.TokenSource);

            Assert.AreEqual(
                GetExpectedCacheKey(clientID, Authority.CreateAuthority(confidentialClientAuthority, false).TenantId),
                appCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheKey);
            Assert.IsTrue(appCacheRecorder.LastAfterAccessNotificationArgs.IsApplicationCache);
        }

        [TestMethod]
        public async Task ConfidentialClientWithNoDefaultClaimsTestAsync()
        {
            var confidentialClientAuthority = PublicCloudTestAuthority;
            var claims = GetClaims();

            X509Certificate2 cert = GetCertificate();

            var confidentialApp = ConfidentialClientApplicationBuilder
                .Create(PublicCloudConfidentialClientID)
                .WithAuthority(new Uri(confidentialClientAuthority), true)
                .WithClientClaims(cert, claims, false)
                .WithTestLogging()
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
            var confidentialClientAuthority = PublicCloudTestAuthority;
            var claims = GetClaims(false);

            X509Certificate2 cert = GetCertificate();

            var confidentialApp = ConfidentialClientApplicationBuilder
                .Create(PublicCloudConfidentialClientID)
                .WithAuthority(new Uri(confidentialClientAuthority), true)
                .WithClientClaims(cert, claims)
                .WithTestLogging()
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
            var confidentialClientAuthority = PublicCloudTestAuthority;
            var claims = GetClaims();

            var confidentialApp = ConfidentialClientApplicationBuilder
                .Create(PublicCloudConfidentialClientID)
                .WithAuthority(new Uri(confidentialClientAuthority), true)
                .WithClientAssertion(GetSignedClientAssertionUsingMsalInternal(PublicCloudConfidentialClientID, claims))
                .WithTestLogging()
                .Build();

            var appCacheRecorder = confidentialApp.AppTokenCache.RecordAccess();

            var authResult = await confidentialApp.AcquireTokenForClient(s_keyvaultScope)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            appCacheRecorder.AssertAccessCounts(1, 1);
            Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(
                GetExpectedCacheKey(PublicCloudConfidentialClientID, Authority.CreateAuthority(confidentialClientAuthority, false).TenantId),
                appCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheKey);
            Assert.IsTrue(appCacheRecorder.LastAfterAccessNotificationArgs.IsApplicationCache);
            ValidateClaimsInAssertion(claims, ((ConfidentialClientApplication)confidentialApp).ClientCredential.SignedAssertion);
            MsalAssert.AssertAuthResult(authResult);

            // call again to ensure cache is hit
            authResult = await confidentialApp.AcquireTokenForClient(s_keyvaultScope)
               .ExecuteAsync(CancellationToken.None)
               .ConfigureAwait(false);

            appCacheRecorder.AssertAccessCounts(2, 1);
            Assert.AreEqual(TokenSource.Cache, authResult.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(
                GetExpectedCacheKey(PublicCloudConfidentialClientID, Authority.CreateAuthority(confidentialClientAuthority, false).TenantId),
                appCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheKey);
            Assert.IsTrue(appCacheRecorder.LastAfterAccessNotificationArgs.IsApplicationCache);
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
                { "iss", PublicCloudConfidentialClientID.ToString(CultureInfo.InvariantCulture) },
                { "jti", Guid.NewGuid().ToString() },
                { "nbf", nbf.ToString(CultureInfo.InvariantCulture) },
                { "sub", PublicCloudConfidentialClientID.ToString(CultureInfo.InvariantCulture) },
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

        private static string GetSignedClientAssertionUsingMsalInternal(string clientId, IDictionary<string, string> claims)
        {
#if NET_CORE
            var manager = new Client.Platforms.netcore.NetCoreCryptographyManager();
#else
            var manager = new Client.Platforms.net45.NetDesktopCryptographyManager();
#endif
            var jwtToken = new Client.Internal.JsonWebToken(manager, clientId, TestConstants.ClientCredentialAudience, claims);
            var clientCredential = ClientCredentialWrapper.CreateWithCertificate(GetCertificate(), claims);
            return jwtToken.Sign(clientCredential, false);
        }

        private static X509Certificate2 GetCertificate(bool useRSACert = false)
        {
            X509Certificate2 cert = CertificateHelper.FindCertificateByThumbprint(useRSACert ?
                TestConstants.RSATestCertThumbprint :
                TestConstants.AutomationTestThumbprint);
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
        public async Task WebAPIAccessingGraphOnBehalfOfUserWithCacheTestAsync()
        {
            await RunOnBehalfOfTestWithTokenCacheAsync(await LabUserHelper.GetSpecificUserAsync("IDLAB@msidlab4.onmicrosoft.com").ConfigureAwait(false)).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory(TestCategories.Arlington)]
        public async Task ArlingtonWebAPIAccessingGraphOnBehalfOfUserTestAsync()
        {
            var labResponse = await LabUserHelper.GetArlingtonUserAsync().ConfigureAwait(false);
            await RunOnBehalfOfTestAsync(labResponse).ConfigureAwait(false);
        }

        [TestCategory(TestCategories.ADFS)]
        public async Task WebAPIAccessingGraphOnBehalfOfADFS2019UserTestAsync()
        {
            await RunOnBehalfOfTestAsync(await LabUserHelper.GetAdfsUserAsync(FederationProvider.ADFSv2019, true).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory(TestCategories.ADFS)]
        public async Task ClientCreds_WithClientSecret_Adfs_Async()
        {
            SecretBundle secret = _keyVault.GetSecret(Adfs2019LabConstants.ADFS2019ClientSecretURL);

            ConfidentialClientApplication msalConfidentialClient = ConfidentialClientApplicationBuilder.Create(Adfs2019LabConstants.ConfidentialClientId)
                                            .WithAdfsAuthority(Adfs2019LabConstants.Authority, true)
                                            .WithRedirectUri(Adfs2019LabConstants.ClientRedirectUri)
                                            .WithClientSecret(secret.Value)
                                            .WithTestLogging()
                                            .BuildConcrete();

            AuthenticationResult authResult = await msalConfidentialClient
                .AcquireTokenForClient(s_adfsScopes)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(authResult);
            Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.IsNull(authResult.IdToken);
        }

        [TestMethod]
        [TestCategory(TestCategories.ADFS)]
        public async Task ClientCreds_WithCertificate_Adfs_Async()
        {
            var cert = await _keyVault.GetCertificateWithPrivateMaterialAsync(AdfsCertName)
                .ConfigureAwait(false);

            ConfidentialClientApplication msalConfidentialClient = ConfidentialClientApplicationBuilder.Create(Adfs2019LabConstants.ConfidentialClientId)
                                            .WithAdfsAuthority(Adfs2019LabConstants.Authority, true)
                                            .WithRedirectUri(Adfs2019LabConstants.ClientRedirectUri)
                                            .WithTestLogging()
                                            .WithCertificate(cert)
                                            .BuildConcrete();

            AuthenticationResult authResult = await msalConfidentialClient
                .AcquireTokenForClient(s_adfsScopes)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(authResult);
            Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.IsNull(authResult.IdToken);
        }


        [TestMethod]
        [TestCategory(TestCategories.ADFS)]
        public async Task ClientCreds_WithClientAssertion_Adfs_Async()
        {
            var cert = await _keyVault.GetCertificateWithPrivateMaterialAsync(AdfsCertName)
                .ConfigureAwait(false);
            string clientAssertion =
                GetSignedClientAssertionUsingWilson(
                    Adfs2019LabConstants.ConfidentialClientId,
                    // ADFS token endpoint is ${authority}/oauth2/token
                    // AAD token endpoint is ${authority}/oauth2/v2.0/token
                    $"{Adfs2019LabConstants.Authority}/oauth2/token",
                    cert);

            ConfidentialClientApplication msalConfidentialClient =
                ConfidentialClientApplicationBuilder.Create(Adfs2019LabConstants.ConfidentialClientId)
                                            .WithAdfsAuthority(Adfs2019LabConstants.Authority, true)
                                            .WithTestLogging()
                                            .WithRedirectUri(Adfs2019LabConstants.ClientRedirectUri)
                                            .WithClientAssertion(clientAssertion)
                                            .BuildConcrete();

            AuthenticationResult authResult = await msalConfidentialClient
                .AcquireTokenForClient(s_adfsScopes)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(authResult);
            Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.IsNull(authResult.IdToken);
        }

        private static string GetSignedClientAssertionUsingWilson(
            string issuer,
            string aud,
            X509Certificate2 cert)
        {
            //string aud = $"{authority}/oauth2/token";

            // no need to add exp, nbf as JsonWebTokenHandler will add them by default.
            var claims = new Dictionary<string, object>()
            {
                { "aud", aud },
                { "iss", issuer },
                { "jti", Guid.NewGuid().ToString() },
                { "sub", issuer }
            };

            var securityTokenDescriptor = new SecurityTokenDescriptor
            {
                Claims = claims,
                SigningCredentials = new X509SigningCredentials(cert)
            };

            var handler = new JsonWebTokenHandler();
            var signedClientAssertion = handler.CreateToken(securityTokenDescriptor);

            return signedClientAssertion;
        }

        private async Task RunOnBehalfOfTestAsync(LabResponse labResponse)
        {
            LabUser user = labResponse.User;
            string oboHost;
            string secret;
            string authority;
            string publicClientID;
            string confidentialClientID;
            string[] oboScope;

            switch (labResponse.User.AzureEnvironment)
            {
                case AzureEnvironment.azureusgovernment:
                    oboHost = ArlingtonCloudHost;
                    secret = _keyVault.GetSecret(TestConstants.MsalArlingtonOBOKeyVaultUri).Value;
                    authority = labResponse.Lab.Authority + "organizations";
                    publicClientID = ArlingtonPublicClientIDOBO;
                    confidentialClientID = ArlingtonConfidentialClientIDOBO;
                    oboScope = s_arlingtonOBOServiceScope;
                    break;
                default:
                    oboHost = PublicCloudHost;
                    secret = _keyVault.GetSecret(TestConstants.MsalOBOKeyVaultUri).Value;
                    authority = TestConstants.AuthorityOrganizationsTenant;
                    publicClientID = PublicCloudPublicClientIDOBO;
                    confidentialClientID = PublicCloudConfidentialClientIDOBO;
                    oboScope = s_publicCloudOBOServiceScope;
                    break;
            }

            //TODO: acquire scenario specific client ids from the lab response

            SecureString securePassword = new NetworkCredential("", user.GetOrFetchPassword()).SecurePassword;

            var msalPublicClient = PublicClientApplicationBuilder.Create(publicClientID)
                                                                 .WithAuthority(authority)
                                                                 .WithRedirectUri(TestConstants.RedirectUri)
                                                                 .WithTestLogging()
                                                                 .Build();

            var builder = msalPublicClient.AcquireTokenByUsernamePassword(oboScope, user.Upn, securePassword);

            builder.WithAuthority(authority);

            var authResult = await builder.ExecuteAsync().ConfigureAwait(false);

            var confidentialApp = ConfidentialClientApplicationBuilder
                .Create(confidentialClientID)
                .WithAuthority(new Uri(oboHost + authResult.TenantId), true)
                .WithClientSecret(secret)
                .WithTestLogging()
                .BuildConcrete();

            var userCacheRecorder = confidentialApp.UserTokenCache.RecordAccess();

            UserAssertion userAssertion = new UserAssertion(authResult.AccessToken);

            string atHash = userAssertion.AssertionHash;

            authResult = await confidentialApp.AcquireTokenOnBehalfOf(s_scopes, userAssertion)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            MsalAssert.AssertAuthResult(authResult, user);
            Assert.AreEqual(atHash, userCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheKey);

#pragma warning disable CS0618 // Type or member is obsolete
            await confidentialApp.GetAccountsAsync().ConfigureAwait(false);
#pragma warning restore CS0618 // Type or member is obsolete
            Assert.IsNull(userCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheKey);
        }

        private async Task RunOnBehalfOfTestWithTokenCacheAsync(LabResponse labResponse)
        {
            LabUser user = labResponse.User;
            string oboHost;
            string secret;
            string authority;
            string publicClientID;
            string confidentialClientID;
            string[] oboScope;

            oboHost = PublicCloudHost;
            secret = _keyVault.GetSecret(TestConstants.MsalOBOKeyVaultUri).Value;
            authority = TestConstants.AuthorityOrganizationsTenant;
            publicClientID = PublicCloudPublicClientIDOBO;
            confidentialClientID = PublicCloudConfidentialClientIDOBO;
            oboScope = s_publicCloudOBOServiceScope;
            
            //TODO: acquire scenario specific client ids from the lab response

            SecureString securePassword = new NetworkCredential("", user.GetOrFetchPassword()).SecurePassword;

            var msalPublicClient = PublicClientApplicationBuilder.Create(publicClientID)
                                                                 .WithAuthority(authority)
                                                                 .WithRedirectUri(TestConstants.RedirectUri)
                                                                 .WithTestLogging()
                                                                 .Build();

            var builder = msalPublicClient.AcquireTokenByUsernamePassword(oboScope, user.Upn, securePassword);

            builder.WithAuthority(authority);

            var authResult = await builder.ExecuteAsync().ConfigureAwait(false);

            var confidentialApp = ConfidentialClientApplicationBuilder
                .Create(confidentialClientID)
                .WithAuthority(new Uri(oboHost + authResult.TenantId), true)
                .WithClientSecret(secret)
                .WithTestLogging()
                .BuildConcrete();

            var userCacheRecorder = confidentialApp.UserTokenCache.RecordAccess();

            UserAssertion userAssertion = new UserAssertion(authResult.AccessToken);

            string atHash = userAssertion.AssertionHash;

            authResult = await confidentialApp.AcquireTokenOnBehalfOf(s_scopes, userAssertion)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            MsalAssert.AssertAuthResult(authResult, user);
            Assert.AreEqual(atHash, userCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheKey);

#pragma warning disable CS0618 // Type or member is obsolete
            await confidentialApp.GetAccountsAsync().ConfigureAwait(false);
#pragma warning restore CS0618 // Type or member is obsolete
            Assert.IsNull(userCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheKey);

            //Run OBO again. Should get token from cache
            authResult = await confidentialApp.AcquireTokenOnBehalfOf(s_scopes, userAssertion)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(authResult);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.IsTrue(!userCacheRecorder.LastAfterAccessNotificationArgs.IsApplicationCache);
            Assert.IsTrue(userCacheRecorder.LastAfterAccessNotificationArgs.HasTokens);
            Assert.AreEqual(atHash, userCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheKey);
            Assert.AreEqual(TokenSource.Cache, authResult.AuthenticationResultMetadata.TokenSource);

            //Expire access tokens
            TokenCacheHelper.ExpireAccessTokens(confidentialApp.UserTokenCacheInternal);

            //Run OBO again. Should do token refresh since the AT is expired
            authResult = await confidentialApp.AcquireTokenOnBehalfOf(s_scopes, userAssertion)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(authResult);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.IsTrue(!userCacheRecorder.LastAfterAccessNotificationArgs.IsApplicationCache);
            Assert.IsTrue(userCacheRecorder.LastAfterAccessNotificationArgs.HasTokens);
            Assert.AreEqual(atHash, userCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheKey);
            Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);

            //creating second app with no refresh tokens
            var atItems = await confidentialApp.UserTokenCacheInternal.GetAllAccessTokensAsync(true).ConfigureAwait(false);
            var confidentialApp2 = ConfidentialClientApplicationBuilder
                .Create(confidentialClientID)
                .WithAuthority(new Uri(oboHost + authResult.TenantId), true)
                .WithClientSecret(secret)
                .WithTestLogging()
                .BuildConcrete();

            TokenCacheHelper.ExpireAndSaveAccessToken(confidentialApp2.UserTokenCacheInternal, atItems.FirstOrDefault());

            //Should perform OBO flow since the access token is expired and the refresh token does not exist
            authResult = await confidentialApp2.AcquireTokenOnBehalfOf(s_scopes, userAssertion)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(authResult);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.IsTrue(!userCacheRecorder.LastAfterAccessNotificationArgs.IsApplicationCache);
            Assert.IsTrue(userCacheRecorder.LastAfterAccessNotificationArgs.HasTokens);
            Assert.AreEqual(atHash, userCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheKey);
            Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);

            TokenCacheHelper.ExpireAccessTokens(confidentialApp2.UserTokenCacheInternal);
            UpdateUserAssertions(confidentialApp2);

            //Should perform OBO flow since the access token and the refresh token contains the wrong user assertion hash
            authResult = await confidentialApp2.AcquireTokenOnBehalfOf(s_scopes, userAssertion)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(authResult);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.IsTrue(!userCacheRecorder.LastAfterAccessNotificationArgs.IsApplicationCache);
            Assert.IsTrue(userCacheRecorder.LastAfterAccessNotificationArgs.HasTokens);
            Assert.AreEqual(atHash, userCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheKey);
            Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);
        }

        private void UpdateUserAssertions(ConfidentialClientApplication app)
        {
            TokenCacheHelper.UpdateAccessTokenAssertions(app.UserTokenCacheInternal);
            TokenCacheHelper.UpdateRefreshTokenAssertions(app.UserTokenCacheInternal);
        }

        [TestMethod]
        public async Task ClientCreds_ClientAssertion_AAD_Wilson_Async()
        {
            try
            {
                string signedAssertionWilson = GetSignedClientAssertionUsingWilson(
                       PublicCloudConfidentialClientID,
                       PublicCloudTestAuthority + "/oauth2/v2.0/token", // for AAD use v2.0, but not for ADFS
                       GetCertificate());

                var confidentialApp = ConfidentialClientApplicationBuilder
                    .Create(PublicCloudConfidentialClientID)
                    .WithAuthority(new Uri(PublicCloudTestAuthority), true)
                    .WithClientAssertion(signedAssertionWilson)
                    .WithTestLogging()
                    .Build();

                var authResult = await confidentialApp.AcquireTokenForClient(s_keyvaultScope)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
                throw;
            }
        }

        [TestMethod]
        public async Task ClientCreds_ClientAssertion_AAD_NoWilson_Async()
        {
            try
            {
                string signedAssertionNoWilson = GetSignedClientAssertionDirectly(
                       PublicCloudConfidentialClientID,
                       PublicCloudTestAuthority + "/oauth2/v2.0/token", // for AAD use v2.0, but not for ADFS
                       GetCertificate());

                var confidentialApp = ConfidentialClientApplicationBuilder
                    .Create(PublicCloudConfidentialClientID)
                    .WithAuthority(new Uri(PublicCloudTestAuthority), true)
                    .WithClientAssertion(signedAssertionNoWilson)
                    .WithTestLogging()
                    .Build();

                var authResult = await confidentialApp.AcquireTokenForClient(s_keyvaultScope)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
                throw;
            }
        }

        [TestMethod]
        public async Task ClientCreds_ServicePrincipal_OBO_PPE_Async()
        {
            //An explination of the OBO for service principal scenario can be found here https://aadwiki.windows-int.net/index.php?title=App_OBO_aka._Service_Principal_OBO
            X509Certificate2 cert = GetCertificate();
            IReadOnlyList<string> scopes = new List<string>() { OBOServicePpeClientID + "/.default" };
            IReadOnlyList<string> scopes2 = new List<string>() { OBOServiceDownStreamApiPpeClientID + "/.default" };

            var confidentialSPApp = ConfidentialClientApplicationBuilder
                                    .Create(OBOClientPpeClientID)
                                    .WithAuthority(PPEAuthenticationAuthority)
                                    .WithCertificate(cert)
                                    .WithTestLogging()
                                    .Build();

            var authenticationResult = await confidentialSPApp.AcquireTokenForClient(scopes).ExecuteAsync().ConfigureAwait(false);

            string appToken = authenticationResult.AccessToken;
            var userAssertion = new UserAssertion(appToken);
            string atHash = userAssertion.AssertionHash;

            var _confidentialApp = ConfidentialClientApplicationBuilder
                .Create(OBOServicePpeClientID)
                .WithCertificate(cert)
                .Build();

            var userCacheRecorder = _confidentialApp.UserTokenCache.RecordAccess();

            authenticationResult = await _confidentialApp.AcquireTokenOnBehalfOf(scopes2, userAssertion)
                                                         .WithAuthority(PPEAuthenticationAuthority)
                                                         .ExecuteAsync().ConfigureAwait(false);

            Assert.IsNotNull(authenticationResult);
            Assert.IsNotNull(authenticationResult.AccessToken);
            Assert.AreEqual(TokenSource.IdentityProvider, authenticationResult.AuthenticationResultMetadata.TokenSource);

            authenticationResult = await _confidentialApp.AcquireTokenOnBehalfOf(scopes2, userAssertion)
                                                         .WithAuthority(PPEAuthenticationAuthority)
                                                         .ExecuteAsync().ConfigureAwait(false);

            Assert.IsNotNull(authenticationResult);
            Assert.IsNotNull(authenticationResult.AccessToken);
            Assert.IsTrue(!userCacheRecorder.LastAfterAccessNotificationArgs.IsApplicationCache);
            Assert.IsTrue(userCacheRecorder.LastAfterAccessNotificationArgs.HasTokens);
            Assert.AreEqual(atHash, userCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheKey);
            Assert.AreEqual(TokenSource.Cache, authenticationResult.AuthenticationResultMetadata.TokenSource);
        }

        private string GetSignedClientAssertionDirectly(
            string issuer, // client ID
            string audience, // ${authority}/oauth2/v2.0/token for AAD or ${authority}/oauth2/token for ADFS
            X509Certificate2 certificate)
        {
            const uint JwtToAadLifetimeInSeconds = 60 * 10; // Ten minutes
            DateTime validFrom = DateTime.UtcNow;
            var nbf = ConvertToTimeT(validFrom);
            var exp = ConvertToTimeT(validFrom + TimeSpan.FromSeconds(JwtToAadLifetimeInSeconds));

            var payload = new Dictionary<string, string>()
            {
                { "aud", audience },
                { "exp", exp.ToString(CultureInfo.InvariantCulture) },
                { "iss", issuer },
                { "jti", Guid.NewGuid().ToString() },
                { "nbf", nbf.ToString(CultureInfo.InvariantCulture) },
                { "sub", issuer }
            };

            RSACng rsa = certificate.GetRSAPrivateKey() as RSACng;

            //alg represents the desired signing algorithm, which is SHA-256 in this case
            //kid represents the certificate thumbprint
            var header = new Dictionary<string, string>()
            {
              { "alg", "RS256"},
              { "kid", Encode(certificate.GetCertHash()) }
            };

            string token = Encode(
                Encoding.UTF8.GetBytes(JObject.FromObject(header).ToString())) +
                "." +
                Encode(Encoding.UTF8.GetBytes(JObject.FromObject(payload).ToString()));

            string signature = Encode(
                rsa.SignData(
                    Encoding.UTF8.GetBytes(token),
                    HashAlgorithmName.SHA256,
                    System.Security.Cryptography.RSASignaturePadding.Pkcs1));
            return string.Concat(token, ".", signature);
        }

        private static string Encode(byte[] arg)
        {
            char Base64PadCharacter = '=';
            char Base64Character62 = '+';
            char Base64Character63 = '/';
            char Base64UrlCharacter62 = '-';
            char Base64UrlCharacter63 = '_';


            string s = Convert.ToBase64String(arg);
            s = s.Split(Base64PadCharacter)[0]; // RemoveAccount any trailing padding
            s = s.Replace(Base64Character62, Base64UrlCharacter62); // 62nd char of encoding
            s = s.Replace(Base64Character63, Base64UrlCharacter63); // 63rd char of encoding

            return s;
        }

    }
}
