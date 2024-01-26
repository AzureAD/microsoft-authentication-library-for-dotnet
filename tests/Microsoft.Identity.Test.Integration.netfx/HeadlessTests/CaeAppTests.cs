// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.NetFx.HeadlessTests
{
    [TestClass]
    public class CaeAppTests
    {
        private string _confidentialClientSecret;
        private const string CaeAppClientId = LabApiConstants.LabCaeConfidentialClientId;
        private const string TenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47";
        private static readonly KeyVaultSecretsProvider s_keyVaultProvider = new(KeyVaultInstance.MsalTeam);
        private static readonly HttpClient s_httpClient = new();
        private static readonly TimeSpan s_delayTimeout = TimeSpan.FromMinutes(5);
        private static readonly string[] s_graphAppScope = ["https://graph.microsoft.com/.default"];

        [TestInitialize]
        public Task TestInitialize()
        {
            if (string.IsNullOrEmpty(_confidentialClientSecret))
            {
                _confidentialClientSecret = s_keyVaultProvider.GetSecretByName(TestConstants.MsalCCAKeyVaultSecretName).Value;
            }
            return LabUserHelper.EnableAppServicePrincipal(LabApiConstants.LabCaeConfidentialClientId);
        }

        [TestCleanup]
        public Task TestCleanup()
        {
            return LabUserHelper.EnableAppServicePrincipal(LabApiConstants.LabCaeConfidentialClientId);
        }

        /// <summary>
        /// [Init] Enable the app's service principal.
        /// Initialize CAE CCA and get an app token for Graph.
        /// Call Graph's /users endpoint with the token; should receive a successful response.
        /// Disable the CAE app's service principal and wait until the changes propagate to Graph.
        /// Use the cached token to call Graph again; should receive a 401 with claims. 
        /// Acquire a new token with claims; should receive an exception that the SP is disabled.
        /// [Cleanup] Enable the app's service principal again.
        /// </summary>
        [Ignore("CAE tests are not set up for automation.")]
        [TestMethod]
        public async Task ClientCredentials_WithDisabledServicePrincipal_ThrowsException()
        {
            var cca = ConfidentialClientApplicationBuilder
                .Create(CaeAppClientId)
                .WithClientSecret(_confidentialClientSecret)
                .WithTenantId(TenantId)
                .WithClientCapabilities(["cp1"])
                .Build();

            var result = await cca.AcquireTokenForClient(s_graphAppScope).ExecuteAsync().ConfigureAwait(false);

            var response1 = await s_httpClient.SendAsync(CreateRequest(result)).ConfigureAwait(false);
            Assert.AreEqual(HttpStatusCode.OK, response1.StatusCode);

            await LabUserHelper.DisableAppServicePrincipal(LabApiConstants.LabCaeConfidentialClientId).ConfigureAwait(false);
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

            var ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
                await cca.AcquireTokenForClient(s_graphAppScope)
                         .WithClaims(wwwAuthParameters.Claims).ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

            Assert.AreEqual(MsalError.UnauthorizedClient, ex.ErrorCode);
            Assert.IsTrue(ex.ErrorCodes.Contains("7000112")); // Service principal is disabled

            HttpRequestMessage CreateRequest(AuthenticationResult result)
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/users");
                request.Headers.Add("Authorization", result.CreateAuthorizationHeader());
                return request;
            }
        }
    }
}
