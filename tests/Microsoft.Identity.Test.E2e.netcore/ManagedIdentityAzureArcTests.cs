// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace Microsoft.Identity.Test.E2E
{
    [TestClass]
    public class ManagedIdentityAzureArcTests
    {
        private const string ArmScope = "https://management.azure.com";

        private static bool IsArc() =>
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("IDENTITY_ENDPOINT"));

        /// <summary>
        /// Builds a System-Assigned MI app with *per-instance* in-memory cache
        /// to avoid cross-test pollution.
        /// </summary>
        private static IManagedIdentityApplication BuildSami()
        {
            var builder = ManagedIdentityApplicationBuilder
                            .Create(ManagedIdentityId.SystemAssigned);

            builder.Config.AccessorOptions = null;

            return builder.Build();
        }

        [TestMethod]
        public async Task AcquireToken_ForSami_OnAzureArc_Succeeds()
        {
            if (!IsArc())
                Assert.Inconclusive("Arc-specific test skipped.");

            var mi = BuildSami();
            var result = await mi.AcquireTokenForManagedIdentity(ArmScope).ExecuteAsync().ConfigureAwait(false);

            Assert.IsFalse(string.IsNullOrEmpty(result.AccessToken));
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
        }

        [TestMethod]
        public async Task AcquireToken_SecondCall_ComesFromCache()
        {
            if (!IsArc())
                Assert.Inconclusive("Arc-specific test skipped.");

            var mi = BuildSami();

            var first = await mi.AcquireTokenForManagedIdentity(ArmScope).ExecuteAsync().ConfigureAwait(false);
            var second = await mi.AcquireTokenForManagedIdentity(ArmScope).ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, first.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(TokenSource.Cache, second.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(first.AccessToken, second.AccessToken, "Expected identical AT from cache.");
        }

        [TestMethod]
        public async Task ForceRefresh_BypassesCache_ReturnsNewToken()
        {
            if (!IsArc())
                Assert.Inconclusive("Arc-specific test skipped.");

            var mi = BuildSami();

            var result = await mi.AcquireTokenForManagedIdentity(ArmScope).ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

            result = await mi.AcquireTokenForManagedIdentity(ArmScope)
                                    .WithForceRefresh(true)
                                    .ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
        }
    }
}
