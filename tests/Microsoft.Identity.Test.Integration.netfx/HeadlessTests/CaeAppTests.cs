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
        private readonly string _caeAppClientId = LabApiConstants.LabCaeConfidentialClientId;
        private readonly string _tenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47";
        private readonly KeyVaultSecretsProvider _keyVaultProvider = new(KeyVaultInstance.MsalTeam);
        private readonly HttpClient _httpClient = new();
        private readonly TimeSpan _delayTimeout = TimeSpan.FromMinutes(5);
        private readonly string[] _graphAppScope = ["https://graph.microsoft.com/.default"];

        [TestInitialize]
        public Task TestInitialize()
        {
            if (string.IsNullOrEmpty(_confidentialClientSecret))
            {
                _confidentialClientSecret = _keyVaultProvider.GetSecretByName(TestConstants.MsalCCAKeyVaultSecretName).Value;
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
        [TestMethod]
        public async Task ClientCredentials_WithDisabledServicePrincipal_ThrowsException()
        {
            var cca = ConfidentialClientApplicationBuilder
                .Create(_caeAppClientId)
                .WithClientSecret(_confidentialClientSecret)
                .WithTenantId(_tenantId)
                .WithClientCapabilities(new[] { "cp1" })
                .Build();

            var result = await cca.AcquireTokenForClient(_graphAppScope).ExecuteAsync().ConfigureAwait(false);           

            var response1 = await _httpClient.SendAsync(CreateRequest(result)).ConfigureAwait(false);
            Assert.AreEqual(HttpStatusCode.OK, response1.StatusCode);

            await LabUserHelper.DisableAppServicePrincipal(LabApiConstants.LabCaeConfidentialClientId).ConfigureAwait(false);
            await Task.Delay(_delayTimeout).ConfigureAwait(false);

            var response2 = await _httpClient.SendAsync(CreateRequest(result)).ConfigureAwait(false);
            Assert.AreEqual(HttpStatusCode.Unauthorized, response2.StatusCode);

            var claims = WwwAuthenticateParameters.GetClaimChallengeFromResponseHeaders(response2.Headers);

            Assert.IsNotNull(claims);
            Assert.AreNotEqual(0, claims.Length);

            MsalServiceException ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
                await cca.AcquireTokenForClient(_graphAppScope)
                         .WithClaims(claims).ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

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
