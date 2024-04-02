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
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Test.Common.Core.Helpers;

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

            string authority = ComputeCiamAuthority(authorityFormat, authorityVersion, labResponse);

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
        [DataRow("https://login.msidlabsciam.com/fe362aec-5d43-45d1-b730-9755e60dc3b9/v2.0/", 3)] //CIAM CUD
        public async Task ClientCredentialWithClientSecret_Ciam_Async(string authorityFormat, int authorityVersion)
        {
            //Get lab details
            var labResponse = await LabUserHelper.GetLabUserDataAsync(new UserQuery()
            {
                FederationProvider = FederationProvider.CIAM,
                SignInAudience = SignInAudience.AzureAdMyOrg,
                PublicClient = PublicClient.no
            }).ConfigureAwait(false);

            string authority = ComputeCiamAuthority(authorityFormat, authorityVersion, labResponse);

            //Acquire tokens
            string appId = authorityVersion < 3 ? labResponse.App.AppId : "b244c86f-ed88-45bf-abda-6b37aa482c79";

            //Acquire tokens
            var msalConfidentialClientBuilder = ConfidentialClientApplicationBuilder
                .Create(appId)
                .WithExperimentalFeatures();

            if (authorityVersion < 3)
            {
                msalConfidentialClientBuilder.WithClientSecret(GetCiamSecret())
                                             .WithAuthority(authority, false);
            }
            else
            {
                msalConfidentialClientBuilder.WithCertificate(CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName))
                                             .WithOidcAuthority(authority);
            }


            var msalConfidentialClient = msalConfidentialClientBuilder.Build();

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

        [TestMethod]
        public async Task Ciam_Cud_Obo_Test_Async()
        {
            string authorityNonCud = "https://MSIDLABCIAM6.ciamlogin.com";
            string authorityCud = "https://login.msidlabsciam.com/fe362aec-5d43-45d1-b730-9755e60dc3b9/v2.0/";
            string ciamWebapp = "b244c86f-ed88-45bf-abda-6b37aa482c79";
            string ciamWebApi = "634de702-3173-4a71-b336-a4fab786a479";
            string ciamEmail = "idlab@msidlabciam6.onmicrosoft.com";

            //Get lab details
            var labResponse = await LabUserHelper.GetLabUserDataAsync(new UserQuery()
            {
                FederationProvider = FederationProvider.CIAM,
                SignInAudience = SignInAudience.AzureAdMyOrg,
                PublicClient = PublicClient.no
            }).ConfigureAwait(false);

            //Acquire tokens
            var msalPublicClient = PublicClientApplicationBuilder
                .Create(ciamWebapp)
                //.Create(ciamWebApi)
                .WithAuthority(authorityNonCud, false)
                .WithRedirectUri(_ciamRedirectUri)
                //.WithRedirectUri("https://login.microsoftonline.com/common/oauth2/nativeclient")
                .Build();

            var result = await msalPublicClient
                .AcquireTokenByUsernamePassword(new[] { $"api://{ciamWebApi}/.default" }, ciamEmail, LabUserHelper.FetchUserPassword("msidlabciam6"))
                //.AcquireTokenInteractive(new[] { "User.Read" })
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(result.AccessToken);
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

            string appToken = result.AccessToken;
            var userAssertion = new UserAssertion(appToken);
            string atHash = userAssertion.AssertionHash;

            //Acquire tokens for OBO
            var msalConfidentialClient = ConfidentialClientApplicationBuilder
                .Create(ciamWebApi)
                .WithCertificate(CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName))
                .WithAuthority(authorityCud, false)
                .WithRedirectUri(_ciamRedirectUri)
                .BuildConcrete();

            var userCacheRecorder = msalConfidentialClient.UserTokenCache.RecordAccess();

            var resultObo = await msalConfidentialClient.AcquireTokenOnBehalfOf(new[] { "User.Read" }, userAssertion)
                                  .ExecuteAsync(CancellationToken.None)
                                  .ConfigureAwait(false);

            Assert.IsNotNull(resultObo.AccessToken);
            Assert.IsFalse(userCacheRecorder.LastAfterAccessNotificationArgs.IsApplicationCache);
            Assert.IsTrue(userCacheRecorder.LastAfterAccessNotificationArgs.HasTokens);
            Assert.AreEqual(atHash, userCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheKey);
            Assert.AreEqual(TokenSource.IdentityProvider, resultObo.AuthenticationResultMetadata.TokenSource);

            //Fetch cached tokens
            resultObo = await msalConfidentialClient.AcquireTokenOnBehalfOf(new[] { "User.Read" }, userAssertion)
                                  .ExecuteAsync(CancellationToken.None)
                                  .ConfigureAwait(false);

            Assert.IsNotNull(resultObo.AccessToken);
            Assert.IsFalse(userCacheRecorder.LastAfterAccessNotificationArgs.IsApplicationCache);
            Assert.IsTrue(userCacheRecorder.LastAfterAccessNotificationArgs.HasTokens);
            Assert.AreEqual(atHash, userCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheKey);
            Assert.AreEqual(TokenSource.Cache, resultObo.AuthenticationResultMetadata.TokenSource);
        }

        private string ComputeCiamAuthority(string authorityFormat, int authorityVersion, LabResponse labResponse)
        {
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
                case 3:
                    authority = authorityFormat;
                    break;
            }

            return authority;
        }

        private string GetCiamSecret()
        {
            KeyVaultSecretsProvider provider = new KeyVaultSecretsProvider();
            return provider.GetSecretByName("msidlabciam2-cc").Value;
        }
    }
}
