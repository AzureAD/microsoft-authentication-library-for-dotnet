// Copyright (c) Microsoft Corporation. All rights reserved.
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
    }
}
