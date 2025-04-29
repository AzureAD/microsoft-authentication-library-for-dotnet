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

        /// <summary>
        /// Check if the test is running on Azure Arc.   
        /// </summary>
        private static bool IsArc() =>
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("IDENTITY_ENDPOINT"));

        /// <summary>
        /// Check if the test should run against IMDS.
        /// </summary>
        private static bool ShouldRunImds() =>
            !IsArc() && Environment.GetEnvironmentVariable("RUN_IMDS_E2E") == "true";

        /// <summary>
        /// Builds a Managed Identity application based on the provided parameters.
        /// </summary>
        /// <param name="userAssignedId"></param>
        /// <param name="idType"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
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

        /// <summary>
        /// Test for Managed Identities on IMDS.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="idType"></param>
        [DataTestMethod]
        [DataRow(null /*SAMI*/, null, DisplayName = "SAMI")]
        [DataRow("4b7a4b0b-ecb2-409e-879a-1e21a15ddaf6", "clientid", DisplayName = "UAMI-ClientId")]
        [DataRow("/subscriptions/c1686c51-b717-4fe0-9af3-24a20a41fb0c/resourcegroups/" +
            "MSAL_MSI/providers/Microsoft.ManagedIdentity/userAssignedIdentities/LabVaultAccess_UAMI",
         "resourceid", DisplayName = "UAMI-ResourceId")]
        [DataRow("1eee55b7-168a-46be-8d19-30e830ee9611", "objectid", DisplayName = "UAMI-ObjectId")]
        public async Task AcquireToken_OnImds_Succeeds(string id, string idType)
        {
            //if you want to run this test, set the environment variable RUN_IMDS_E2E=true
            if (!ShouldRunImds())
                Assert.Inconclusive("IMDS test skipped (RUN_IMDS_E2E not set).");

            IManagedIdentityApplication mi = BuildMi(id, idType);

            AuthenticationResult result = await mi.AcquireTokenForManagedIdentity(ArmScope)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsFalse(string.IsNullOrEmpty(result.AccessToken), "AccessToken should not be empty.");

            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource,
                "First call must hit MSI endpoint.");
        }

        /// <summary>
        /// Test for Managed Identities on IMDS. 
        /// Calling AcquireToken twice, second call should come from cache.
        /// </summary>
        [TestMethod]
        public async Task AcquireToken_SecondCall_ComesFromCache()
        {
            //if you want to run this test, set the environment variable RUN_IMDS_E2E=true
            if (!ShouldRunImds())
                Assert.Inconclusive("IMDS test skipped (RUN_IMDS_E2E not set).");

            IManagedIdentityApplication mi = BuildMi();

            AuthenticationResult first = await mi.AcquireTokenForManagedIdentity(ArmScope)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, first.AuthenticationResultMetadata.TokenSource);

            AuthenticationResult second = await mi.AcquireTokenForManagedIdentity(ArmScope)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, second.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(first.AccessToken, second.AccessToken);
        }

        /// <summary>
        /// Test for Managed Identities on IMDS. 
        /// Forcing a refresh, the second call should come from the server.
        /// </summary>
        [TestMethod]
        public async Task ForceRefresh_BypassesCache_ReturnsNewToken()
        {
            if (!ShouldRunImds())
                Assert.Inconclusive("IMDS test skipped (RUN_IMDS_E2E not set).");

            IManagedIdentityApplication mi = BuildMi();

            // first call should hit the server
            AuthenticationResult result = await mi.AcquireTokenForManagedIdentity(ArmScope)
                .ExecuteAsync()
                .ConfigureAwait(false);
            
            // check the token is from IDP
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

            // second call should also hit the server
            AuthenticationResult refreshed = await mi.AcquireTokenForManagedIdentity(ArmScope)
                                    .WithForceRefresh(true)
                                    .ExecuteAsync().ConfigureAwait(false);

            // check the token is from IDP
            Assert.AreEqual(TokenSource.IdentityProvider, refreshed.AuthenticationResultMetadata.TokenSource);
        }
    }
}
