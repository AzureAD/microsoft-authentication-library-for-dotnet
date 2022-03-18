// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    [TestClass]
    public class WwwAuthenticateParametersIntegrationTests
    {
        [TestMethod]
        public async Task CreateWwwAuthenticateResponseFromKeyVaultUrlAsync()
        {
            var authParams = await WwwAuthenticateParameters.CreateFromResourceResponseAsync("https://buildautomation.vault.azure.net/secrets/CertName/CertVersion").ConfigureAwait(false);

            Assert.AreEqual("login.windows.net", new Uri(authParams.Authority).Host);
            Assert.AreEqual("72f988bf-86f1-41af-91ab-2d7cd011db47", authParams.GetTenantId()); // because the Key Vault resource belong to Microsoft Corp tenant
            Assert.AreEqual(2, authParams.RawParameters.Count);
            Assert.IsNull(authParams.Claims);
            Assert.IsNull(authParams.Error);
        }

        [TestMethod]
        public async Task CreateWwwAuthenticateResponseFromGraphUrlAsync()
        {
            var authParams = await WwwAuthenticateParameters.CreateFromResourceResponseAsync("https://graph.microsoft.com/v1.0/me").ConfigureAwait(false);

            Assert.AreEqual("https://login.microsoftonline.com/common", authParams.Authority);
            Assert.AreEqual("common", authParams.GetTenantId());
            Assert.AreEqual(3, authParams.RawParameters.Count);
            Assert.IsNull(authParams.Claims);
            Assert.IsNull(authParams.Error);
        }

        /// <summary>
        /// Makes unauthorized call to Azure Resource Manager REST API https://docs.microsoft.com/en-us/rest/api/resources/subscriptions/get.
        /// Expects response 401 Unauthorized. Analyzes the WWW-Authenticate header values.
        /// </summary>
        /// <param name="hostName">ARM endpoint, e.g. Production or Dogfood</param>
        /// <param name="subscriptionId">Well-known subscription ID</param>
        /// <param name="authority">AAD endpoint, e.g. Production or PPE</param>
        /// <param name="tenantId">Expected Tenant ID</param>
        [TestMethod]
        [DataRow("management.azure.com", "c1686c51-b717-4fe0-9af3-24a20a41fb0c", "login.windows.net", "72f988bf-86f1-41af-91ab-2d7cd011db47")]
        [DataRow("api-dogfood.resources.windows-int.net", "1835ad3d-4585-4c5f-b55a-b0c3cbda1103", "login.windows-ppe.net", "f686d426-8d16-42db-81b7-ab578e110ccd")]
        public async Task CreateWwwAuthenticateResponseFromAzureResourceManagerUrlAsync(string hostName, string subscriptionId, string authority, string tenantId)
        {
            const string apiVersion = "2020-08-01"; // current latest API version for /subscriptions/get
            var url = $"https://{hostName}/subscriptions/{subscriptionId}?api-version={apiVersion}";

            var authParams = await WwwAuthenticateParameters.CreateFromResourceResponseAsync(url).ConfigureAwait(false);

            Assert.AreEqual($"https://{authority}/{tenantId}", authParams.Authority); // authority URI consists of AAD endpoint and tenant ID
            Assert.AreEqual(tenantId, authParams.GetTenantId()); // tenant ID is extracted out of authority URI
            Assert.AreEqual(3, authParams.RawParameters.Count);
            Assert.IsNull(authParams.Claims);
            Assert.AreEqual("invalid_token", authParams.Error);
        }
    }
}
