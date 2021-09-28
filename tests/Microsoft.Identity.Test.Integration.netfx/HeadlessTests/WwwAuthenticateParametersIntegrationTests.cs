﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.NetFx.HeadlessTests
{
    [TestClass]
    public class WwwAuthenticateParametersIntegrationTests
    {
        [TestMethod]
        public async Task CreateWwwAuthenticateResponseFromKeyVaultUrlAsync()
        {
            var authParams = await WwwAuthenticateParameters.CreateFromResourceResponseAsync("https://buildautomation.vault.azure.net/secrets/CertName/CertVersion").ConfigureAwait(false);

            Assert.AreEqual("https://vault.azure.net", authParams.Resource);
            Assert.AreEqual("login.windows.net", new Uri(authParams.Authority).Host);
            Assert.AreEqual("https://vault.azure.net/.default", authParams.Scopes.FirstOrDefault());
            Assert.AreEqual(2, authParams.RawParameters.Count);
            Assert.IsNull(authParams.Claims);
            Assert.IsNull(authParams.Error);
        }

        [TestMethod]
        public async Task CreateWwwAuthenticateResponseFromGraphUrlAsync()
        {
            var authParams = await WwwAuthenticateParameters.CreateFromResourceResponseAsync("https://graph.microsoft.com/v1.0/me").ConfigureAwait(false);

            Assert.AreEqual("00000003-0000-0000-c000-000000000000", authParams.Resource);
            Assert.AreEqual("https://login.microsoftonline.com/common", authParams.Authority);
            Assert.AreEqual("00000003-0000-0000-c000-000000000000/.default", authParams.Scopes.FirstOrDefault());
            Assert.AreEqual(3, authParams.RawParameters.Count);
            Assert.IsNull(authParams.Claims);
            Assert.IsNull(authParams.Error);
        }

        [DataRow("management.azure.com", "a645d3be-5a4a-40ef-82a1-d5f5358a424c", "login.windows.net", "72f988bf-86f1-41af-91ab-2d7cd011db47")]
        [DataRow("api-dogfood.resources.windows-int.net", "5d04a672-05ce-492b-958f-d225b6a67926", "login.windows-ppe.net", "f686d426-8d16-42db-81b7-ab578e110ccd")]
        [DataTestMethod]
        public async Task CreateWwwAuthenticateResponseFromAzureResourceManagerUrlAsync(string hostName, string subscriptionId, string authority, string tenantId)
        {
            const string apiVersion = "2020-08-01";
            var url = $"https://{hostName}/subscriptions/{subscriptionId}?api-version={apiVersion}";
            var authParams = await WwwAuthenticateParameters.CreateFromResourceResponseAsync(url).ConfigureAwait(false);

            Assert.IsNull(authParams.Resource);
            Assert.AreEqual($"https://{authority}/{tenantId}", authParams.Authority);
            Assert.IsNull(authParams.Scopes);
            Assert.AreEqual(3, authParams.RawParameters.Count);
            Assert.IsNull(authParams.Claims);
            Assert.AreEqual("invalid_token", authParams.Error);
        }
    }
}
