// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.MtlsPop;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace Microsoft.Identity.Test.E2E
{
    [TestClass]
    public class ManagedIdentityImdsTests
    {
        private const string ArmScope = "https://management.azure.com";

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

        [RunOnAzureDevOps]
        [TestCategory("MI_E2E_Imds")]
        [DataTestMethod]
        [DataRow(null /*SAMI*/, null, DisplayName = "SAMI")]
        [DataRow("4b7a4b0b-ecb2-409e-879a-1e21a15ddaf6", "clientid", DisplayName = "UAMI-ClientId")]
        [DataRow("/subscriptions/c1686c51-b717-4fe0-9af3-24a20a41fb0c/resourcegroups/MSAL_MSI/providers/Microsoft.ManagedIdentity/userAssignedIdentities/LabVaultAccess_UAMI",
         "resourceid", DisplayName = "UAMI-ResourceId")]
        [DataRow("1eee55b7-168a-46be-8d19-30e830ee9611", "objectid", DisplayName = "UAMI-ObjectId")]
        public async Task AcquireToken_OnImds_Succeeds(string id, string idType)
        {
            var mi = BuildMi(id, idType);

            var result = await mi.AcquireTokenForManagedIdentity(ArmScope)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsFalse(string.IsNullOrEmpty(result.AccessToken), "AccessToken should not be empty.");

            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource,
                "First call must hit MSI endpoint.");

            var second = await mi.AcquireTokenForManagedIdentity(ArmScope)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(TokenSource.Cache, second.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(result.AccessToken, second.AccessToken);
        }

        [RunOnAzureDevOps]
        [TestCategory("MI_E2E_Imds")]
        [DataTestMethod]
        [DataRow(null /*SAMI*/, null, DisplayName = "SAMI")]
        [DataRow("4b7a4b0b-ecb2-409e-879a-1e21a15ddaf6", "clientid", DisplayName = "UAMI-ClientId")]
        [DataRow("/subscriptions/c1686c51-b717-4fe0-9af3-24a20a41fb0c/resourcegroups/MSAL_MSI/providers/Microsoft.ManagedIdentity/userAssignedIdentities/LabVaultAccess_UAMI",
         "resourceid", DisplayName = "UAMI-ResourceId")]
        [DataRow("1eee55b7-168a-46be-8d19-30e830ee9611", "objectid", DisplayName = "UAMI-ObjectId")]
        public async Task AcquireToken_OnImds_Fails_WithMtlsProofOfPossession(string id, string idType)
        {
            var mi = BuildMi(id, idType);

            var ex = await Assert.ThrowsExceptionAsync<MsalClientException>(async () =>
                await mi.AcquireTokenForManagedIdentity(ArmScope)
                .WithMtlsProofOfPossession()
                .ExecuteAsync().ConfigureAwait(false)
            ).ConfigureAwait(false);

            Assert.AreEqual(MsalError.MtlsPopTokenNotSupportedinImdsV1, ex.ErrorCode);
        }
    }
}
