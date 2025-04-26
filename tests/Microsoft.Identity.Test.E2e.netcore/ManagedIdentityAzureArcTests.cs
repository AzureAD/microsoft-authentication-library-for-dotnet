// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.IdentityModel.Abstractions;
using Microsoft.Identity.Test.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace Microsoft.Identity.Test.E2E
{
    [TestClass]
    public class ManagedIdentityAzureArcTests
    {
        [TestMethod]
        public async Task AcquireToken_ForSami_OnAzureArc_Succeeds()
        {
            string identityEndpoint = Environment.GetEnvironmentVariable("IDENTITY_ENDPOINT");

            if (string.IsNullOrEmpty(identityEndpoint))
            {
                Assert.Inconclusive("IDENTITY_ENDPOINT not set. Skipping test because it is intended to run only on Azure Arc agent.");
            }

            IManagedIdentityApplication mi = ManagedIdentityApplicationBuilder
                .Create(ManagedIdentityId.SystemAssigned)
                .Build();

            string scope = "https://management.azure.com";

            AuthenticationResult result = await mi.AcquireTokenForManagedIdentity(scope)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.IsFalse(string.IsNullOrEmpty(result.AccessToken));
        }
    }
}
