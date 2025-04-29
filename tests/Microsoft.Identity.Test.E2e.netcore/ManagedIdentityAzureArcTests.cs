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

        /// <summary>
        /// Check if the test is running on Azure Arc.
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Test for System-Assigned MI on Azure Arc. (No UAMI support)
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task AcquireToken_ForSami_OnAzureArc_Succeeds()
        {
            if (!IsArc())
                Assert.Inconclusive("Arc-specific test skipped.");

            IManagedIdentityApplication mi = BuildSami();

            AuthenticationResult result = await mi.AcquireTokenForManagedIdentity(ArmScope)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsFalse(string.IsNullOrEmpty(result.AccessToken));

            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
        }

        /// <summary>
        /// Test for System-Assigned MI on Azure Arc. (No UAMI support)
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task AcquireToken_SecondCall_ComesFromCache()
        {
            if (!IsArc())
                Assert.Inconclusive("Arc-specific test skipped.");

            IManagedIdentityApplication mi = BuildSami();

            AuthenticationResult first = await mi.AcquireTokenForManagedIdentity(ArmScope)
                .ExecuteAsync()
                .ConfigureAwait(false);
            
            // first call should hit MSI endpoint
            Assert.AreEqual(TokenSource.IdentityProvider, first.AuthenticationResultMetadata.TokenSource);

            AuthenticationResult second = await mi.AcquireTokenForManagedIdentity(ArmScope)
                .ExecuteAsync()
                .ConfigureAwait(false);

            //second call should come from cache
            Assert.AreEqual(TokenSource.Cache, second.AuthenticationResultMetadata.TokenSource);

            // check that the access tokens are identical
            Assert.AreEqual(first.AccessToken, second.AccessToken, "Expected identical AT from cache.");
        }

        /// <summary>
        /// Test for System-Assigned MI on Azure Arc. (No UAMI support)
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task ForceRefresh_BypassesCache_ReturnsNewToken()
        {
            if (!IsArc())
                Assert.Inconclusive("Arc-specific test skipped.");

            IManagedIdentityApplication mi = BuildSami();

            AuthenticationResult result = await mi.AcquireTokenForManagedIdentity(ArmScope)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

            result = await mi.AcquireTokenForManagedIdentity(ArmScope)
                                    .WithForceRefresh(true)
                                    .ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
        }
    }
}
