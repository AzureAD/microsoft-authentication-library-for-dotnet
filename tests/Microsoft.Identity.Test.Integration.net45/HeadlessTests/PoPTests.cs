// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    // Currently PoP is supported only on .Net Classic
#if DESKTOP
    // Note: these tests require permission to a KeyVault Microsoft account;
    // Please ignore them if you are not a Microsoft FTE, they will run as part of the CI build
    [TestClass]
    public class PoPTests
    {
        // This endpoint is hosted in the MSID Lab and is able to verify any pop token bound to an HTTP request
        private const string PoPValidatorEndpoint = "https://signedhttprequest.azurewebsites.net/api/validateSHR";

        private static readonly string[] s_scopes = { "User.Read" };

        Uri protectedResournceUri1 = new Uri("https://www.contoso.com/path1/path2?queryParam1=a&queryParam2=b");
        Uri protectedResournceUri2 = new Uri("https://www.bing.com/path3/path4?queryParam5=c&queryParam6=d");

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
        }

        [TestMethod]
        public async Task POPAsync()
        {
            var labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);
            await RunHappyPathTestAsync(labResponse).ConfigureAwait(false);
        }

        private async Task RunHappyPathTestAsync(LabResponse labResponse)
        {
            var user = labResponse.User;
            SecureString securePassword = new NetworkCredential("", user.GetOrFetchPassword()).SecurePassword;
            string clientId = labResponse.App.AppId;

            var pca = PublicClientApplicationBuilder.Create(clientId).WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs).Build();
            ConfigureInMemoryCache(pca);

            AuthenticationResult authResult = await pca
                .AcquireTokenByUsernamePassword(s_scopes, user.Upn, securePassword)
                .WithExtraQueryParameters(GetTestSliceParams())
                .WithPoPAuthenticationScheme(protectedResournceUri1, HttpMethod.Get)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            await VerifyPoPTokenAsync(
                 clientId,
                 authResult.CreateAuthorizationHeader(),
                 protectedResournceUri1,
                 HttpMethod.Get).ConfigureAwait(false);

            // recreate the pca to ensure that the silent call is served from the cache, i.e. the key remains stable
            pca = PublicClientApplicationBuilder
                .Create(clientId)
                .WithHttpClientFactory(new NoAccessHttpClientFactory()) // token should be served from the cache, no network access necessary
                .Build();
            ConfigureInMemoryCache(pca);

            var accounts = await pca.GetAccountsAsync().ConfigureAwait(false);
            authResult = await pca
                .AcquireTokenSilent(s_scopes, accounts.Single())
                .WithPoPAuthenticationScheme(protectedResournceUri1, HttpMethod.Get)
                .ExecuteAsync()
                .ConfigureAwait(false);

            await VerifyPoPTokenAsync(
                clientId, 
                authResult.CreateAuthorizationHeader(),
                protectedResournceUri1,
                HttpMethod.Get).ConfigureAwait(false);

            // Call some other Uri - the same pop assertion can be reused, i.e. no need to call Evo
            authResult = await pca
              .AcquireTokenSilent(s_scopes, accounts.Single())
              .WithPoPAuthenticationScheme(protectedResournceUri2, HttpMethod.Post)
              .ExecuteAsync()
              .ConfigureAwait(false);

            await VerifyPoPTokenAsync(
                clientId,
                authResult.CreateAuthorizationHeader(),
                protectedResournceUri2,
                HttpMethod.Post).ConfigureAwait(false);
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
        private async Task VerifyPoPTokenAsync(string clientId, string popAuthHeader, Uri uri, HttpMethod httpMethod)
        {
            var httpClient = new HttpClient();
            HttpResponseMessage response;
            var request = new HttpRequestMessage(HttpMethod.Post, PoPValidatorEndpoint);

            request.Headers.Add("Authorization", popAuthHeader);
            request.Headers.Add("Secret", _popValidationEndpointSecret);
            request.Headers.Add("Authority", "https://sts.windows.net/72f988bf-86f1-41af-91ab-2d7cd011db47/");
            request.Headers.Add("ClientId", clientId);

            // the URI the POP token is bound to
            request.Headers.Add("ShrUri", uri.ToString());

            // the method the POP token in bound to
            request.Headers.Add("ShrMethod", httpMethod.ToString());

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
