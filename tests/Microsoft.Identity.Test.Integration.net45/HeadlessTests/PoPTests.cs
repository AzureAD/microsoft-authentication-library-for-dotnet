// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Integration.net45.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    // Currently PoP is supported only on .Net Classic
#if DESKTOP || NET_CORE
    // Note: these tests require permission to a KeyVault Microsoft account;
    // Please ignore them if you are not a Microsoft FTE, they will run as part of the CI build
    [TestClass]
    public class PoPTests
    {
        // This endpoint is hosted in the MSID Lab and is able to verify any pop token bound to an HTTP request
        private const string PoPValidatorEndpoint = "https://signedhttprequest.azurewebsites.net/api/validateSHR";

        private static readonly string[] s_scopes = { "User.Read" };
        private static readonly string[] s_keyvaultScope = { "https://vault.azure.net/.default" };

        private const string PublicCloudConfidentialClientID = "16dab2ba-145d-4b1b-8569-bf4b9aed4dc8";
        private const string PublicCloudTestAuthority = "https://login.windows.net/72f988bf-86f1-41af-91ab-2d7cd011db47";
        private static string s_publicCloudCcaSecret;
        private KeyVaultSecretsProvider _keyVault;

        // Doesn't exist, but the POP validator endpoint will check if the POP token matches this HTTP request 

        private string _popValidationEndpointSecret;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
            if (_popValidationEndpointSecret == null)
            {
                _popValidationEndpointSecret = LabUserHelper.KeyVaultSecretsProvider.GetSecret(
                    "https://buildautomation.vault.azure.net/secrets/automation-pop-validation-endpoint/841fc7c2ccdd48d7a9ef727e4ae84325").Value;
            }

            if (_keyVault == null)
            {
                _keyVault = new KeyVaultSecretsProvider();
                s_publicCloudCcaSecret = _keyVault.GetSecret(TestConstants.MsalCCAKeyVaultUri).Value;
            }
        }

        [TestMethod]
        public async Task PoP_AcquireAndAcquireSilent_MultipleKeys_Async()
        {
            var labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);
            await AcquireAndAcquireSilent_MultipleKeys_Async(labResponse).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task PoP_BearerAndPoP_CanCoexist_Async()
        {
            var labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);
            await BearerAndPoP_CanCoexist_Async(labResponse).ConfigureAwait(false);
        }

        private async Task BearerAndPoP_CanCoexist_Async(LabResponse labResponse)
        {
            // Arrange 
            HttpRequestMessage request = new HttpRequestMessage(
                HttpMethod.Get,
                new Uri("https://www.contoso.com/path1/path2?queryParam1=a&queryParam2=b"));

            var user = labResponse.User;
            SecureString securePassword = new NetworkCredential("", user.GetOrFetchPassword()).SecurePassword;
            string clientId = labResponse.App.AppId;

            var pca = PublicClientApplicationBuilder
                .Create(clientId)
                .WithExperimentalFeatures()
                .WithTestLogging()
                .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs).Build();
            ConfigureInMemoryCache(pca);

            // Act - acquire both a PoP and a Bearer token
            Trace.WriteLine("Getting a PoP token");
            AuthenticationResult result = await pca
                .AcquireTokenByUsernamePassword(s_scopes, user.Upn, securePassword)
                .WithExtraQueryParameters(GetTestSliceParams())
                .WithProofOfPosession(request)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual("PoP", request.Headers.Authorization.Scheme);
            Assert.AreEqual("pop", result.TokenType);
            await VerifyPoPTokenAsync(clientId, request).ConfigureAwait(false);

            Trace.WriteLine("Getting a Bearer token");
            result = await pca
                .AcquireTokenByUsernamePassword(s_scopes, user.Upn, securePassword)
                .ExecuteAsync()
                .ConfigureAwait(false);
            Assert.AreEqual("Bearer", result.TokenType);
            Assert.AreEqual(
                2,
                (pca as PublicClientApplication).UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());

            // Arrange - block HTTP requests to make sure AcquireTokenSilent does not refresh the RT
            pca = PublicClientApplicationBuilder
                           .Create(clientId)
                           .WithExperimentalFeatures()
                           .WithTestLogging()
                           .WithHttpClientFactory(new NoAccessHttpClientFactory()) // token should be served from the cache, no network access necessary
                           .Build();
            ConfigureInMemoryCache(pca);

            var account = (await pca.GetAccountsAsync().ConfigureAwait(false)).Single();
            result = await pca.AcquireTokenSilent(s_scopes, account).ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual("Bearer", result.TokenType);

            result = await pca.AcquireTokenSilent(s_scopes, account)
                .WithProofOfPosession(request)
                .ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual("pop", result.TokenType);

        }

        private async Task AcquireAndAcquireSilent_MultipleKeys_Async(LabResponse labResponse)
        {
            HttpRequestMessage request1 = new HttpRequestMessage(HttpMethod.Get, new Uri("https://www.contoso.com/path1/path2?queryParam1=a&queryParam2=b"));
            HttpRequestMessage request2 = new HttpRequestMessage(HttpMethod.Post, new Uri("https://www.bing.com/path3/path4?queryParam5=c&queryParam6=d"));

            var user = labResponse.User;
            SecureString securePassword = new NetworkCredential("", user.GetOrFetchPassword()).SecurePassword;
            string clientId = labResponse.App.AppId;

            var pca = PublicClientApplicationBuilder.Create(clientId)
                .WithExperimentalFeatures()
                .WithTestLogging()
                .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs).Build();
            ConfigureInMemoryCache(pca);

            await pca
                .AcquireTokenByUsernamePassword(s_scopes, user.Upn, securePassword)
                .WithExtraQueryParameters(GetTestSliceParams())
                .WithProofOfPosession(request1)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual("PoP", request1.Headers.Authorization.Scheme);
            await VerifyPoPTokenAsync(
                 clientId,
                 request1).ConfigureAwait(false);

            // recreate the pca to ensure that the silent call is served from the cache, i.e. the key remains stable
            pca = PublicClientApplicationBuilder
                .Create(clientId)
                .WithExperimentalFeatures()
                .WithHttpClientFactory(new NoAccessHttpClientFactory()) // token should be served from the cache, no network access necessary
                .Build();
            ConfigureInMemoryCache(pca);

            var accounts = await pca.GetAccountsAsync().ConfigureAwait(false);
            await pca
                .AcquireTokenSilent(s_scopes, accounts.Single())
                .WithProofOfPosession(request1)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual("PoP", request1.Headers.Authorization.Scheme);
            await VerifyPoPTokenAsync(
                clientId,
                request1).ConfigureAwait(false);

            // Call some other Uri - the same pop assertion can be reused, i.e. no need to call Evo
            await pca
              .AcquireTokenSilent(s_scopes, accounts.Single())
              .WithProofOfPosession(request2)
              .ExecuteAsync()
              .ConfigureAwait(false);

            Assert.AreEqual("PoP", request2.Headers.Authorization.Scheme);
            await VerifyPoPTokenAsync(
                clientId,
                request2).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ConfidentialClientWithClientSecretTestAsync()
        {
            await RunTestWithClientSecretAsync(PublicCloudConfidentialClientID,
                                                           PublicCloudTestAuthority,
                                                           s_publicCloudCcaSecret).ConfigureAwait(false);
        }

        public async Task RunTestWithClientSecretAsync(string clientID, string authority, string secret)
        {
            HttpRequestMessage request1 = new HttpRequestMessage(HttpMethod.Get, new Uri("https://www.contoso.com/path1/path2?queryParam1=a&queryParam2=b"));

            var confidentialClientAuthority = authority;

            var confidentialApp = ConfidentialClientApplicationBuilder
                .Create(clientID)
                .WithExperimentalFeatures()
                .WithAuthority(new Uri(confidentialClientAuthority), true)
                .WithClientSecret(secret)
                .WithTestLogging()
                .Build();

            await confidentialApp.AcquireTokenForClient(s_keyvaultScope)
                .WithProofOfPosession(request1)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual("PoP", request1.Headers.Authorization.Scheme);
            await VerifyPoPTokenAsync(
                clientID,
                request1).ConfigureAwait(false);
        }

        private class NoAccessHttpClientFactory : IMsalHttpClientFactory
        {
            private const string Message = "Not expecting to make HTTP requests.";

            public HttpClient GetHttpClient()
            {
                Assert.Fail(Message);
                throw new InvalidOperationException(Message);
            }
        }

        /// <summary>
        /// This calls a special endpoint that validates any POP token against a configurable HTTP request.
        /// The HTTP request is configured through headers.
        /// </summary>
        private async Task VerifyPoPTokenAsync(string clientId, HttpRequestMessage initialRequest)
        {
            var httpClient = new HttpClient();
            HttpResponseMessage response;
            var request = new HttpRequestMessage(HttpMethod.Post, PoPValidatorEndpoint);

            request.Headers.Add("Secret", _popValidationEndpointSecret);
            request.Headers.Add("Authority", "https://sts.windows.net/72f988bf-86f1-41af-91ab-2d7cd011db47/");
            request.Headers.Add("ClientId", clientId);
            request.Headers.Authorization = initialRequest.Headers.Authorization;

            // the URI the POP token is bound to
            request.Headers.Add("ShrUri", initialRequest.RequestUri.ToString());

            // the method the POP token in bound to
            request.Headers.Add("ShrMethod", initialRequest.Method.ToString());

            response = await httpClient.SendAsync(request).ConfigureAwait(false);

            Assert.IsTrue(response.IsSuccessStatusCode);
        }


        private static Dictionary<string, string> GetTestSliceParams()
        {
            return new Dictionary<string, string>()
            {
                { "dc", "prod-wst-test1" },
            };
        }


        private string _inMemoryCache = "{}";
        private void ConfigureInMemoryCache(IPublicClientApplication pca)
        {
            pca.UserTokenCache.SetBeforeAccess(notificationArgs =>
            {
                byte[] bytes = Encoding.UTF8.GetBytes(_inMemoryCache);
                notificationArgs.TokenCache.DeserializeMsalV3(bytes);
            });

            pca.UserTokenCache.SetAfterAccess(notificationArgs =>
            {
                if (notificationArgs.HasStateChanged)
                {
                    byte[] bytes = notificationArgs.TokenCache.SerializeMsalV3();
                    _inMemoryCache = Encoding.UTF8.GetString(bytes);
                }
            });
        }
    }
#endif
}
