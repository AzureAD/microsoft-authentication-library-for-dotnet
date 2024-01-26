// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.NetFx.SeleniumTests
{
    [TestClass]
    public class CaeUserTests
    {
        private const string CertificateName = "for-cca-testing";
        private const string ConfidentialClientID = "35dc5034-9b65-4a5d-ad81-73cca468c1e0"; //msidlab4.com app
        private const string TenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47";
        private static readonly KeyVaultSecretsProvider s_keyVaultProvider = new(KeyVaultInstance.MsalTeam);
        private static readonly HttpClient s_httpClient = new();
        private static readonly TimeSpan s_delayTimeout = TimeSpan.FromMinutes(5);
        private static readonly string[] s_graphUserScope = ["User.Read"];
        private static readonly TimeSpan s_loginTimeout = TimeSpan.FromMinutes(1);

        public TestContext TestContext { get; set; }

        /// <summary>
        /// Login with the test user using Selenium; the cache should have a user token.
        /// Acquire token silently from the cache and call Graph; should receive a successful response.
        /// Revoke the user's session and wait until the changes propagate to Graph.
        /// Use the cached token to call Graph again; should receive a 401 with claims. 
        /// Acquire a new token with claims; should receive an MsalUiRequiredException.
        /// </summary>
        [TestMethod]
        public async Task UserFlow_WithSessionRevoked_ThrowsMsalUiRequiredException()
        {
            var cca = await BuildCca().ConfigureAwait(false);

            var labResponse = await LabUserHelper.GetCaeUserAsync().ConfigureAwait(false);

            await LoginUser(cca, labResponse.User).ConfigureAwait(false);

            var result = await cca.AcquireTokenSilent(s_graphUserScope, labResponse.User.Upn).ExecuteAsync().ConfigureAwait(false);

            var response1 = await s_httpClient.SendAsync(CreateRequest(result)).ConfigureAwait(false);
            Assert.AreEqual(HttpStatusCode.OK, response1.StatusCode);

            await LabUserHelper.RevokeUserSessionAsync(labResponse.User).ConfigureAwait(false);
            await Task.Delay(s_delayTimeout).ConfigureAwait(false);

            var response2 = await s_httpClient.SendAsync(CreateRequest(result)).ConfigureAwait(false);
            Assert.AreEqual(HttpStatusCode.Unauthorized, response2.StatusCode);

            // A WWW-Authenticate header with claims looks like this:
            //
            // Bearer realm="", authorization_uri="https://login.microsoftonline.com/common/oauth2/authorize",
            // client_id="00000003-0000-0000-c000-000000000000",
            // errorDescription="Continuous access evaluation resulted in challenge with result: InteractionRequired and code: TokenIssuedBeforeRevocationTimestamp",
            // error="insufficient_claims",
            // claims="eyhhY2Nlc3NfdG8rZW4iOnsibmJmIjpwfjIzNTQzMiJ9fX0="
            // 
            // Decoded claims challenge looks like {"access_token":{"nbf":{"essential":true, "value":"1502185177"}}}

            var wwwAuthParameters = WwwAuthenticateParameters.CreateFromAuthenticationHeaders(response2.Headers).FirstOrDefault();

            Assert.IsNotNull(wwwAuthParameters);
            Assert.IsNotNull(wwwAuthParameters.Claims);
            Assert.AreNotEqual(0, wwwAuthParameters.Claims.Length);
            Assert.IsTrue(wwwAuthParameters.Error.Equals("insufficient_claims", StringComparison.OrdinalIgnoreCase));

            var ex = await Assert.ThrowsExceptionAsync<MsalUiRequiredException>(async () =>
                await cca.AcquireTokenSilent(s_graphUserScope, labResponse.User.Upn)
                         .WithClaims(wwwAuthParameters.Claims).ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

            Assert.AreEqual(MsalError.InvalidGrantError, ex.ErrorCode);
            Assert.IsTrue(ex.ErrorCodes.Contains("50173")); // Expired token, reauthentication needed

            HttpRequestMessage CreateRequest(AuthenticationResult result)
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me");
                request.Headers.Add("Authorization", result.CreateAuthorizationHeader());
                return request;
            }
        }

        private async Task<ConfidentialClientApplication> BuildCca()
        {
            var certificate = await s_keyVaultProvider.GetCertificateWithPrivateMaterialAsync(CertificateName).ConfigureAwait(false);
            var redirectUri = SeleniumWebUI.FindFreeLocalhostRedirectUri();

            var cca = ConfidentialClientApplicationBuilder
                        .Create(ConfidentialClientID)
                        .WithTenantId(TenantId)
                        .WithCertificate(certificate)
                        .WithClientCapabilities(new[] { "cp1" })
                        .WithRedirectUri(redirectUri)
                        .BuildConcrete();

            return cca;
        }

        private async Task LoginUser(ConfidentialClientApplication cca, LabUser labUser)
        {
            string codeVerifier;
            Uri authUri = await cca
                .GetAuthorizationRequestUrl(s_graphUserScope)
                .WithPkce(out codeVerifier)
                .ExecuteAsync().ConfigureAwait(false);

            var seleniumUi = new SeleniumWebUI((driver) =>
            {
                Trace.WriteLine("Starting Selenium automation");
                driver.PerformLogin(labUser, Prompt.SelectAccount, false, false);
            }, TestContext);

            CancellationTokenSource cts = new(s_loginTimeout);
            Uri authCodeUri = await seleniumUi.AcquireAuthorizationCodeAsync(
                authUri,
                new Uri(cca.AppConfig.RedirectUri),
                cts.Token)
                .ConfigureAwait(false);

            var authorizationResult = AuthorizationResult.FromUri(authCodeUri.AbsoluteUri);
            Assert.AreEqual(AuthorizationStatus.Success, authorizationResult.Status);

            await cca.AcquireTokenByAuthorizationCode(s_graphUserScope, authorizationResult.Code)
               .WithPkceCodeVerifier(codeVerifier)
               .ExecuteAsync()
               .ConfigureAwait(false);

            Assert.AreEqual(1, cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
        }
    }
}
