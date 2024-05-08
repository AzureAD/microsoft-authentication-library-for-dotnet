// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.PlatformsCommon.Factories;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    [TestClass]
    public class WwwAuthenticateParametersIntegrationTests
    {
        [TestMethod]
        public async Task CreateWwwAuthenticateResponseFromKeyVaultUrlAsync()
        {
            WwwAuthenticateParameters authParams = await WwwAuthenticateParameters.CreateFromAuthenticationResponseAsync(                
                "https://buildautomation.vault.azure.net/secrets/CertName/CertVersion",
                "Bearer")
                .ConfigureAwait(false);

            Assert.AreEqual("login.microsoftonline.com", new Uri(authParams.Authority).Host);
            Assert.AreEqual("72f988bf-86f1-41af-91ab-2d7cd011db47", authParams.GetTenantId()); // because the Key Vault resource belong to Microsoft Corp tenant
            Assert.AreEqual(2, authParams.RawParameters.Count);
            Assert.IsNull(authParams.Claims);
            Assert.IsNull(authParams.Error);
        }

        [TestMethod]
        public async Task CreateWwwAuthenticateResponseFromGraphUrlAsync()
        {
            WwwAuthenticateParameters authParams = await WwwAuthenticateParameters.CreateFromAuthenticationResponseAsync(
                "https://graph.microsoft.com/v1.0/me", "Bearer").ConfigureAwait(false);

            Assert.AreEqual("https://login.microsoftonline.com/common", authParams.Authority);
            Assert.AreEqual("common", authParams.GetTenantId());
            Assert.AreEqual(3, authParams.RawParameters.Count);
            Assert.IsNull(authParams.Claims);
            Assert.IsNull(authParams.Error);
        }

        /// <summary>
        /// Makes unauthorized call to Azure Resource Manager REST API https://learn.microsoft.com/rest/api/resources/subscriptions/get?view=rest-resources-2022-12-01&tabs=HTTP.
        /// Expects response 401 Unauthorized. Analyzes the WWW-Authenticate header values.
        /// </summary>
        /// <param name="hostName">ARM endpoint, e.g. Production or Dogfood</param>
        /// <param name="subscriptionId">Well-known subscription ID</param>
        /// <param name="authority">AAD endpoint, e.g. Production or PPE</param>
        /// <param name="tenantId">Expected Tenant ID</param>
        [RunOn(TargetFrameworks.NetCore)]
        public async Task CreateWwwAuthenticateResponseFromAzureResourceManagerUrlAsync()
        {
            // see API version at https://learn.microsoft.com/en-us/rest/api/resources/subscriptions/get 
            
            await RunTestForSettingsAsync(
                "management.azure.com",
                "2022-12-01",
                "c1686c51-b717-4fe0-9af3-24a20a41fb0c",
                "login.windows.net",
                "72f988bf-86f1-41af-91ab-2d7cd011db47").ConfigureAwait(false);

            await RunTestForSettingsAsync(
                "management.azure.com",
                "2020-08-01",
                "c1686c51-b717-4fe0-9af3-24a20a41fb0c",
                "login.windows.net",
                "72f988bf-86f1-41af-91ab-2d7cd011db47").ConfigureAwait(false);

            await RunTestForSettingsAsync(
                "management.azure.com",
                "2016-09-01",
                "c1686c51-b717-4fe0-9af3-24a20a41fb0c",
                "login.windows.net",
                "72f988bf-86f1-41af-91ab-2d7cd011db47").ConfigureAwait(false);
        }

        private static async Task RunTestForSettingsAsync(string hostName, string apiVersion, string subscriptionId, string authority, string tenantId)
        {
            var url = $"https://{hostName}/subscriptions/{subscriptionId}?api-version={apiVersion}";

            WwwAuthenticateParameters authParams = await WwwAuthenticateParameters
                .CreateFromAuthenticationResponseAsync(url, "Bearer")
                .ConfigureAwait(false);

            Assert.AreEqual($"https://{authority}/{tenantId}", authParams.Authority); // authority URI consists of AAD endpoint and tenant ID
            Assert.AreEqual(tenantId, authParams.GetTenantId()); // tenant ID is extracted out of authority URI
            Assert.AreEqual(3, authParams.RawParameters.Count);
            Assert.IsNull(authParams.Claims);
            Assert.AreEqual("invalid_token", authParams.Error);
            Assert.AreEqual($"https://{authority}/{tenantId}", authParams.RawParameters["authorization_uri"]);
        }

        [RunOn(TargetFrameworks.NetCore)]
        public async Task ExtractNonceFromWwwAuthHeadersAsync()
        {
            //Arrange & Act
            //Test for nonce in WWW-Authenticate header
            string popNonce = string.Empty;
            var parameterList = await WwwAuthenticateParameters.CreateFromAuthenticationResponseAsync(
                                                         "https://testingsts.azurewebsites.net/servernonce/invalidsignature")
                .ConfigureAwait(false);

            var parameters = parameterList.FirstOrDefault();

            if (parameters.AuthenticationScheme == "PoP" && parameters.RawParameters.Keys.Contains("nonce")) //Check if next nonce for POP is available
            {
                popNonce = parameters.RawParameters["nonce"];
            }

            //Assert
            Assert.IsTrue(parameterList.Any(param => param.AuthenticationScheme == Constants.PoPAuthHeaderPrefix));
            Assert.IsNotNull(parameterList.Single(param => param.AuthenticationScheme == Constants.PoPAuthHeaderPrefix).Nonce);
            Assert.IsTrue(!popNonce.IsNullOrEmpty());
            await PoPValidator.VerifyPopNonceAsync(popNonce).ConfigureAwait(false);
        }

        [RunOn(TargetFrameworks.NetCore)]
        public async Task ExtractNonceFromAuthInfoHeadersAsync()
        {
            //Arrange & Act
            var httpClientFactory = PlatformProxyFactory.CreatePlatformProxy(null).CreateDefaultHttpClientFactory();
            var httpClient = httpClientFactory.GetHttpClient();

            HttpResponseMessage httpResponseMessage = await httpClient.GetAsync("https://testingsts.azurewebsites.net/servernonce/authinfo", new CancellationToken()).ConfigureAwait(false);

            //Assert
            var authInfoParameters = AuthenticationInfoParameters.CreateFromResponseHeaders(httpResponseMessage.Headers);
            Assert.IsNotNull(authInfoParameters);
            Assert.IsNotNull(authInfoParameters.NextNonce);
            await PoPValidator.VerifyPopNonceAsync(authInfoParameters.NextNonce).ConfigureAwait(false);
        }

        [RunOn(TargetFrameworks.NetCore)]
        public async Task ExtractNonceWithAuthParserAsync()
        {
            //Arrange & Act
            //Test for nonce in WWW-Authenticate header
            var parsedHeaders = await AuthenticationHeaderParser.ParseAuthenticationHeadersAsync("https://testingsts.azurewebsites.net/servernonce/invalidsignature").ConfigureAwait(false);

            //Assert
            Assert.IsTrue(parsedHeaders.WwwAuthenticateParameters.Any(param => param.AuthenticationScheme == Constants.PoPAuthHeaderPrefix));
            var serverNonce = parsedHeaders.WwwAuthenticateParameters.Where(param => param.AuthenticationScheme == Constants.PoPAuthHeaderPrefix).Single().Nonce;
            Assert.IsNotNull(serverNonce);
            Assert.AreEqual(parsedHeaders.PopNonce, serverNonce);
            Assert.IsNull(parsedHeaders.AuthenticationInfoParameters);

            //Arrange & Act
            //Test for nonce in Authentication-Info header
            parsedHeaders = await AuthenticationHeaderParser.ParseAuthenticationHeadersAsync("https://testingsts.azurewebsites.net/servernonce/authinfo").ConfigureAwait(false);

            //Assert
            Assert.IsNotNull(parsedHeaders.AuthenticationInfoParameters.NextNonce);
            Assert.AreEqual(parsedHeaders.PopNonce, parsedHeaders.AuthenticationInfoParameters.NextNonce);

            Assert.IsFalse(parsedHeaders.WwwAuthenticateParameters.Any(param => param.AuthenticationScheme == Constants.PoPAuthHeaderPrefix));
            await PoPValidator.VerifyPopNonceAsync(parsedHeaders.PopNonce).ConfigureAwait(false);
        }
    }
}
