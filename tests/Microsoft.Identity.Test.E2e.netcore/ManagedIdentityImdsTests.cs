// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace Microsoft.Identity.Test.E2E
{
    /// <summary>
    /// E2E tests that target the IMDS (default / VM) managed-identity endpoint.
    /// Executed on a hosted build agent by setting RUN_IMDS_E2E=true.
    /// </summary>
    [TestClass]
    public class ManagedIdentityImdsTests
    {
        private const string ArmScope = "https://management.azure.com";

        private static bool IsArc() =>
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("IDENTITY_ENDPOINT"));

        private static bool ShouldRunImds() =>
            !IsArc() && Environment.GetEnvironmentVariable("RUN_IMDS_E2E") == "true";

        private static IManagedIdentityApplication BuildSami()
        {
            var builder = ManagedIdentityApplicationBuilder
                            .Create(ManagedIdentityId.SystemAssigned);

            builder.Config.AccessorOptions = null;  
            return builder.Build();
        }

        [TestMethod]
        public async Task AcquireToken_SystemAssigned_OnImds_Succeeds()
        {
            if (!ShouldRunImds())
                Assert.Inconclusive("IMDS test skipped (RUN_IMDS_E2E not set).");

            var mi = BuildSami();
            
            var result = await mi.AcquireTokenForManagedIdentity(ArmScope)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsFalse(string.IsNullOrEmpty(result.AccessToken));
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
        }

        [TestMethod]
        public async Task AcquireToken_SecondCall_ComesFromCache()
        {
            if (!ShouldRunImds())
                Assert.Inconclusive("IMDS test skipped (RUN_IMDS_E2E not set).");

            var mi = BuildSami();

            var first = await mi.AcquireTokenForManagedIdentity(ArmScope)
                .ExecuteAsync()
                .ConfigureAwait(false);
            
            var second = await mi.AcquireTokenForManagedIdentity(ArmScope)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, first.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(TokenSource.Cache, second.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(first.AccessToken, second.AccessToken);
        }

        [TestMethod]
        public async Task ForceRefresh_BypassesCache_ReturnsNewToken()
        {
            if (!ShouldRunImds())
                Assert.Inconclusive("IMDS test skipped (RUN_IMDS_E2E not set).");

            var mi = BuildSami();
            await mi.AcquireTokenForManagedIdentity(ArmScope)
                .ExecuteAsync()
                .ConfigureAwait(false);

            var refreshed = await mi.AcquireTokenForManagedIdentity(ArmScope)
                                    .WithForceRefresh(true)
                                    .ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, refreshed.AuthenticationResultMetadata.TokenSource);
        }
    }
}
