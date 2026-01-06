// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Microsoft.Identity.Test.E2E
{
    [TestClass]
    public class ManagedIdentityAzureArcTests
    {
        private const string ArmScope = "https://management.azure.com";

        private static IManagedIdentityApplication BuildSami()
        {
            var builder = ManagedIdentityApplicationBuilder
                            .Create(ManagedIdentityId.SystemAssigned);

            builder.Config.AccessorOptions = null;

            return builder.Build();
        }

        [TestCategory("MI_E2E_AzureArc")]
        [RunOnAzureDevOps]
        [TestMethod]
        public async Task AcquireToken_ForSami_OnAzureArc_Succeeds()
        {
            var mi = BuildSami();
            var result = await mi.AcquireTokenForManagedIdentity(ArmScope)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsFalse(string.IsNullOrEmpty(result.AccessToken));
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

            var second = await mi.AcquireTokenForManagedIdentity(ArmScope).ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(TokenSource.Cache, second.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(result.AccessToken, second.AccessToken, "Expected identical AT from cache.");
        }
    }
}
