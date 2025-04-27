// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Test.E2E
{
    [TestClass]
    public class ManagedIdentityImdsTests
    {
        private const string ArmScope = "https://management.azure.com";

        private static bool IsArc() =>
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("IDENTITY_ENDPOINT"));

        private static bool ShouldRunImds() =>
            !IsArc() && Environment.GetEnvironmentVariable("RUN_IMDS_E2E") == "true";

        private static IManagedIdentityApplication BuildMi(
           string userAssignedId = null,
           string idType = null)
        {
            ManagedIdentityId miId = userAssignedId is null
                ? ManagedIdentityId.SystemAssigned
                : idType.ToLowerInvariant() switch
                {
                    "clientid" => ManagedIdentityId.WithUserAssignedClientId(userAssignedId),
                    "resourceid" => ManagedIdentityId.WithUserAssignedResourceId(userAssignedId),
                    "objectid" => ManagedIdentityId.WithUserAssignedObjectId(userAssignedId),
                    _ => throw new ArgumentOutOfRangeException(nameof(idType))
                };

            var builder = ManagedIdentityApplicationBuilder.Create(miId);
            builder.Config.AccessorOptions = null;
            return builder.Build();
        }

        [DataTestMethod]
        [DataRow(null, null,
            DisplayName = "SAMI")]
        [DataRow("04ca4d6a-c720-4ba1-aa06-f6634b73fe7a", ManagedIdentityIdType.ClientId, 
            DisplayName = "UAMI-ClientId")]
        [DataRow("/subscriptions/ff71c235-108e-4869-9779-5f275ce45c44/resourcegroups/RevoGuard/providers/Microsoft.ManagedIdentity/userAssignedIdentities/RevokeUAMI",
                 ManagedIdentityIdType.ResourceId, 
            DisplayName = "UAMI-ResourceId")]
        [DataRow("bfd0bb74-faf9-4db9-b7e7-784823369e7f", ManagedIdentityIdType.ObjectId, 
            DisplayName = "UAMI-ObjectId")]
        public async Task AcquireToken_UserAssignedVariants_OnImds_Succeed(string id, string idType)
        {
            if (!ShouldRunImds())
                Assert.Inconclusive("IMDS test skipped (RUN_IMDS_E2E not set).");

            var mi = BuildMi(id, idType);

            var result = await mi.AcquireTokenForManagedIdentity(ArmScope)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsFalse(string.IsNullOrEmpty(result.AccessToken), "AccessToken should not be empty.");

            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource,
                "First call must hit MSI endpoint.");
        }

        [TestMethod]
        public async Task AcquireToken_SecondCall_ComesFromCache()
        {
            if (!ShouldRunImds())
                Assert.Inconclusive("IMDS test skipped (RUN_IMDS_E2E not set).");

            var mi = BuildMi();

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

            var mi = BuildMi();
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
