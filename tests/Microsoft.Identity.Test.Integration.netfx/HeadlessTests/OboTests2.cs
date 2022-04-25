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
using System.Web;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Json.Utilities;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.Integration.net45.Infrastructure;
using Microsoft.Identity.Test.Integration.NetFx.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    [TestClass]
    public class OboTests2
    {
        private static readonly string[] s_scopes = { "User.Read" };
        private static readonly string[] s_publicCloudOBOServiceScope = { "api://23c64cd8-21e4-41dd-9756-ab9e2c23f58c/access_as_user" };
        private static readonly string[] s_arlingtonOBOServiceScope = { "https://arlmsidlab1.us/IDLABS_APP_Confidential_Client/user_impersonation" };

        //TODO: acquire scenario specific client ids from the lab response
        private const string PublicCloudPublicClientIDOBO = "be9b0186-7dfd-448a-a944-f771029105bf";
        private const string PublicCloudConfidentialClientIDOBO = "23c64cd8-21e4-41dd-9756-ab9e2c23f58c";
        private const string ArlingtonConfidentialClientIDOBO = "c0555d2d-02f2-4838-802e-3463422e571d";
        private const string ArlingtonPublicClientIDOBO = "cb7faed4-b8c0-49ee-b421-f5ed16894c83";

        //The following client ids are for applications that are within PPE
        private const string OBOClientPpeClientID = "9793041b-9078-4942-b1d2-babdc472cc0c";
        private const string OBOServicePpeClientID = "c84e9c32-0bc9-4a73-af05-9efe9982a322";
        private const string OBOServiceDownStreamApiPpeClientID = "23d08a1e-1249-4f7c-b5a5-cb11f29b6923";
        private const string PPEAuthenticationAuthority = "https://login.windows-ppe.net/f686d426-8d16-42db-81b7-ab578e110ccd";

        private const string PublicCloudHost = "https://login.microsoftonline.com/";
        private const string ArlingtonCloudHost = "https://login.microsoftonline.us/";

        private KeyVaultSecretsProvider _keyVault;

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();

            if (_keyVault == null)
            {
                _keyVault = new KeyVaultSecretsProvider();
            }
        }
        
        /// <summary>
        /// Client -> Middletier -> RP
        /// This is OBO for SP without RT support.
        /// Currently this is supported only by 1p, i.e. Client (3P) -> Middletier (1p) -> RP (1p)
        /// </summary>
        /// <remarks>
        /// For details see https://aadwiki.windows-int.net/index.php?title=App_OBO_aka._Service_Principal_OBO, which explains
        /// the structure of the access token received from OBO.
        /// </remarks>
        [TestMethod]
        public async Task ServicePrincipal_OBO_PPE_Async()
        {
            //An explanation of the OBO for service principal scenario can be found here https://aadwiki.windows-int.net/index.php?title=App_OBO_aka._Service_Principal_OBO
            
            var settings = ConfidentialAppSettings.GetSettings(Cloud.Public);
            var cert = settings.GetCertificate(); 

            IReadOnlyList<string> middleTierApiScopes = new List<string>() { OBOServicePpeClientID + "/.default" };
            IReadOnlyList<string> downstreamApiScopes = new List<string>() { OBOServiceDownStreamApiPpeClientID + "/.default" };

            var clientConfidentialApp = ConfidentialClientApplicationBuilder
                                    .Create(OBOClientPpeClientID)
                                    .WithAuthority(PPEAuthenticationAuthority)
                                    .WithCertificate(cert)
                                    .WithTestLogging()
                                    .Build();

            var authenticationResult = await clientConfidentialApp
                .AcquireTokenForClient(middleTierApiScopes)
                .ExecuteAsync()
                .ConfigureAwait(false);

            string appToken = authenticationResult.AccessToken;
            var userAssertion = new UserAssertion(appToken);
            string atHash = userAssertion.AssertionHash;

            var middletierServiceApp = ConfidentialClientApplicationBuilder
                .Create(OBOServicePpeClientID)
                .WithAuthority(PPEAuthenticationAuthority)
                .WithCertificate(cert)
                .Build();
            var userCacheRecorder = middletierServiceApp.UserTokenCache.RecordAccess();

            authenticationResult = await middletierServiceApp
                .AcquireTokenOnBehalfOf(downstreamApiScopes, userAssertion)
                .ExecuteAsync()
                .ConfigureAwait(false);
            
            Assert.IsNotNull(authenticationResult.AccessToken);
            Assert.IsFalse(userCacheRecorder.LastAfterAccessNotificationArgs.IsApplicationCache);
            Assert.IsTrue(userCacheRecorder.LastAfterAccessNotificationArgs.HasTokens);
            Assert.AreEqual(atHash, userCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheKey);
            Assert.AreEqual(TokenSource.IdentityProvider, authenticationResult.AuthenticationResultMetadata.TokenSource);
            Assert.IsNotNull(
                userCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheExpiry,
                "When writing the OBO token response, MSAL should ignore the RT and propose expiry");

            authenticationResult = await middletierServiceApp
                .AcquireTokenOnBehalfOf(downstreamApiScopes, userAssertion)
                .ExecuteAsync()
                .ConfigureAwait(false);
           
            Assert.IsNotNull(authenticationResult.AccessToken);
            Assert.IsTrue(!userCacheRecorder.LastAfterAccessNotificationArgs.IsApplicationCache);
            Assert.IsTrue(userCacheRecorder.LastAfterAccessNotificationArgs.HasTokens);
            Assert.AreEqual(atHash, userCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheKey);
            Assert.AreEqual(TokenSource.Cache, authenticationResult.AuthenticationResultMetadata.TokenSource);
            Assert.IsNull(
                userCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheExpiry,
                "The cache expiry is not set because the node did not change");
        }


        [TestMethod]
        public async Task ServicePrincipal_OBO_LongRunningProcess_PPE_Async()
        {
            //An explanation of the OBO for service principal scenario can be found here https://aadwiki.windows-int.net/index.php?title=App_OBO_aka._Service_Principal_OBO

            var settings = ConfidentialAppSettings.GetSettings(Cloud.Public);
            var cert = settings.GetCertificate();

            IReadOnlyList<string> middleTierApiScopes = new List<string>() { OBOServicePpeClientID + "/.default" };
            IReadOnlyList<string> downstreamApiScopes = new List<string>() { OBOServiceDownStreamApiPpeClientID + "/.default" };

            
            var clientConfidentialApp = ConfidentialClientApplicationBuilder
                                    .Create(OBOClientPpeClientID)
                                    .WithAuthority(PPEAuthenticationAuthority)
                                    .WithCertificate(cert)
                                    .WithTestLogging()
                                    .Build();



            var middletierServiceApp = ConfidentialClientApplicationBuilder
                                    .Create(OBOServicePpeClientID)
                                    .WithAuthority(PPEAuthenticationAuthority)
                                    .WithCertificate(cert)
                                    .Build();
            var userCacheRecorder = middletierServiceApp.UserTokenCache.RecordAccess();

            Trace.WriteLine("1. Upstream client gets an app token");            
            var authenticationResult = await clientConfidentialApp
                .AcquireTokenForClient(middleTierApiScopes)
                .ExecuteAsync()
                .ConfigureAwait(false);
            string clientToken = authenticationResult.AccessToken;

            Trace.WriteLine("2. MidTier kicks off the long running process by getting an OBO token");            
            string cacheKey = null;
            authenticationResult = await (middletierServiceApp as ILongRunningWebApi).
                InitiateLongRunningProcessInWebApi(downstreamApiScopes, clientToken, ref cacheKey)
                .ExecuteAsync().ConfigureAwait(false);

            Assert.IsNotNull(authenticationResult.AccessToken);
            Assert.IsTrue(!userCacheRecorder.LastAfterAccessNotificationArgs.IsApplicationCache);
            Assert.IsTrue(userCacheRecorder.LastAfterAccessNotificationArgs.HasTokens);
            Assert.AreEqual(cacheKey, userCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheKey);
            Assert.AreEqual(TokenSource.IdentityProvider, authenticationResult.AuthenticationResultMetadata.TokenSource);
            Assert.IsNull(
                userCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheExpiry,
                "The cache expiry is not set because there is an RT in the cache");

            Trace.WriteLine("3. Later, mid-tier needs the token again, and one is in the cache");
            authenticationResult = await (middletierServiceApp as ILongRunningWebApi)
                .AcquireTokenInLongRunningProcess(downstreamApiScopes, cacheKey)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, authenticationResult.AuthenticationResultMetadata.TokenSource);

            Trace.WriteLine("4. After the original token expires, the mid-tier needs a token again. RT will be used.");
            TokenCacheHelper.ExpireAllAccessTokens(middletierServiceApp.UserTokenCache as ITokenCacheInternal);

            authenticationResult = await (middletierServiceApp as ILongRunningWebApi)
               .AcquireTokenInLongRunningProcess(downstreamApiScopes, cacheKey)
               .ExecuteAsync()
               .ConfigureAwait(false);

            Assert.IsNotNull(authenticationResult.AccessToken);
            Assert.IsTrue(!userCacheRecorder.LastAfterAccessNotificationArgs.IsApplicationCache);
            Assert.IsTrue(userCacheRecorder.LastAfterAccessNotificationArgs.HasTokens);
            Assert.AreEqual(cacheKey, userCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheKey);
            Assert.AreEqual(TokenSource.IdentityProvider, authenticationResult.AuthenticationResultMetadata.TokenSource);
            Assert.IsNull(
                userCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheExpiry,
                "The cache expiry is not set because there is an RT in the cache");
        }

        [TestMethod]
        public async Task WebAPIAccessingGraphOnBehalfOfUserTestAsync()
        {
            await RunOnBehalfOfTestAsync(await LabUserHelper.GetSpecificUserAsync("IDLAB@msidlab4.onmicrosoft.com").ConfigureAwait(false)).ConfigureAwait(false);
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
        public async Task WebAPIAccessingGraphOnBehalfOfUserWithCacheTestAsync()
        {
            await RunOnBehalfOfTestWithTokenCacheAsync(await LabUserHelper.GetSpecificUserAsync("IDLAB@msidlab4.onmicrosoft.com").ConfigureAwait(false)).ConfigureAwait(false);
        }

        //Since this test performs a large number of operations it should not be rerun on other clouds.
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
            var factory = new HttpSnifferClientFactory();

            var msalPublicClient = PublicClientApplicationBuilder.Create(publicClientID)
                                                                 .WithAuthority(authority)
                                                                 .WithRedirectUri(TestConstants.RedirectUri)
                                                                 .WithTestLogging()
                                                                 .WithHttpClientFactory(factory)
                                                                 .Build();

            var authResult = await msalPublicClient.AcquireTokenByUsernamePassword(oboScope, user.Upn, securePassword)
                .ExecuteAsync()
                .ConfigureAwait(false);

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
            Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);

            //Run OBO again. Should get token from cache
            authResult = await confidentialApp.AcquireTokenOnBehalfOf(s_scopes, userAssertion)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(authResult);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.IsNotNull(authResult.IdToken);
            Assert.IsTrue(!userCacheRecorder.LastAfterAccessNotificationArgs.IsApplicationCache);
            Assert.IsTrue(userCacheRecorder.LastAfterAccessNotificationArgs.HasTokens);
            Assert.AreEqual(atHash, userCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheKey);
            Assert.AreEqual(TokenSource.Cache, authResult.AuthenticationResultMetadata.TokenSource);

            //Expire access tokens
            TokenCacheHelper.ExpireAllAccessTokens(confidentialApp.UserTokenCacheInternal);

            //Run OBO again. Should do OBO flow since the AT is expired and RTs aren't cached for normal OBO flow
            authResult = await confidentialApp.AcquireTokenOnBehalfOf(s_scopes, userAssertion)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(authResult);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.IsNotNull(authResult.IdToken);
            Assert.IsTrue(!userCacheRecorder.LastAfterAccessNotificationArgs.IsApplicationCache);
            Assert.IsTrue(userCacheRecorder.LastAfterAccessNotificationArgs.HasTokens);
            Assert.AreEqual(atHash, userCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheKey);
            Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);
            AssertLastHttpContent("on_behalf_of");

            //creating second app with no refresh tokens
            var atItems = confidentialApp.UserTokenCacheInternal.Accessor.GetAllAccessTokens();
            var confidentialApp2 = ConfidentialClientApplicationBuilder
                .Create(confidentialClientID)
                .WithAuthority(new Uri(oboHost + authResult.TenantId), true)
                .WithClientSecret(secret)
                .WithTestLogging()
                .WithHttpClientFactory(factory)
                .BuildConcrete();

            TokenCacheHelper.ExpireAccessToken(confidentialApp2.UserTokenCacheInternal, atItems.FirstOrDefault());

            //Should perform OBO flow since the access token is expired and the refresh token does not exist
            authResult = await confidentialApp2.AcquireTokenOnBehalfOf(s_scopes, userAssertion)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(authResult);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.IsNotNull(authResult.IdToken);
            Assert.IsTrue(!userCacheRecorder.LastAfterAccessNotificationArgs.IsApplicationCache);
            Assert.IsTrue(userCacheRecorder.LastAfterAccessNotificationArgs.HasTokens);
            Assert.AreEqual(atHash, userCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheKey);
            Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);
            AssertLastHttpContent("on_behalf_of");

            TokenCacheHelper.ExpireAllAccessTokens(confidentialApp2.UserTokenCacheInternal);
            TokenCacheHelper.UpdateUserAssertions(confidentialApp2);

            //Should perform OBO flow since the access token and the refresh token contains the wrong user assertion hash
            authResult = await confidentialApp2.AcquireTokenOnBehalfOf(s_scopes, userAssertion)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(authResult);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.IsNotNull(authResult.IdToken);
            Assert.IsTrue(!userCacheRecorder.LastAfterAccessNotificationArgs.IsApplicationCache);
            Assert.IsTrue(userCacheRecorder.LastAfterAccessNotificationArgs.HasTokens);
            Assert.AreEqual(atHash, userCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheKey);
            Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);
            AssertLastHttpContent("on_behalf_of");
        }

        private void AssertLastHttpContent(string content)
        {
            Assert.IsTrue(HttpSnifferClientFactory.LastHttpContentData.Contains(content));
            HttpSnifferClientFactory.LastHttpContentData = string.Empty;
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

            var authResult = await msalPublicClient.AcquireTokenByUsernamePassword(oboScope, user.Upn, securePassword)
                .ExecuteAsync()
                .ConfigureAwait(false);

            var ccaAuthority = new Uri(oboHost + authResult.TenantId);
            var confidentialApp = ConfidentialClientApplicationBuilder
                .Create(confidentialClientID)
                .WithAuthority(ccaAuthority, true)
                .WithAzureRegion(TestConstants.Region) // should be ignored by OBO
                .WithClientSecret(secret)
                .WithTestLogging()
                .Build();

            var userCacheRecorder = confidentialApp.UserTokenCache.RecordAccess();

            UserAssertion userAssertion = new UserAssertion(authResult.AccessToken);

            string atHash = userAssertion.AssertionHash;

            authResult = await confidentialApp.AcquireTokenOnBehalfOf(s_scopes, userAssertion)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            MsalAssert.AssertAuthResult(authResult, user);
            Assert.AreEqual(atHash, userCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheKey);
            Assert.AreEqual(
                ccaAuthority.ToString() + "/oauth2/v2.0/token",
                authResult.AuthenticationResultMetadata.TokenEndpoint,
                "OBO does not obey region");

#pragma warning disable CS0618 // Type or member is obsolete
            await confidentialApp.GetAccountsAsync().ConfigureAwait(false);
#pragma warning restore CS0618 // Type or member is obsolete
            Assert.IsNull(userCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheKey);
        }

    }
}
