// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
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
        [TestMethod]
        [DataRow(null /*SAMI*/, null, DisplayName = "AcquireToken_OnImds_Succeeds-SAMI")]
        [DataRow("6325cd32-9911-41f3-819c-416cdf9104e7", "clientid", DisplayName = "AcquireToken_OnImds_Succeeds-UAMI-ClientId")]
        [DataRow("/subscriptions/c1686c51-b717-4fe0-9af3-24a20a41fb0c/resourcegroups/MSIV2-Testing-MSALNET/providers/Microsoft.ManagedIdentity/userAssignedIdentities/msiv2uami",
         "resourceid", DisplayName = "AcquireToken_OnImds_Succeeds-UAMI-ResourceId")]
        [DataRow("ecb2ad92-3e30-4505-b79f-ac640d069f24", "objectid", DisplayName = "AcquireToken_OnImds_Succeeds-UAMI-ObjectId")]
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
        [TestMethod]
        [DataRow(null /*SAMI*/, null, DisplayName = "AcquireToken_OnImds_Fails_WithMtlsProofOfPossession-SAMI")]
        [DataRow("6325cd32-9911-41f3-819c-416cdf9104e7", "clientid", DisplayName = "AcquireToken_OnImds_Fails_WithMtlsProofOfPossession-UAMI-ClientId")]
        [DataRow("/subscriptions/c1686c51-b717-4fe0-9af3-24a20a41fb0c/resourcegroups/MSIV2-Testing-MSALNET/providers/Microsoft.ManagedIdentity/userAssignedIdentities/msiv2uami",
         "resourceid", DisplayName = "AcquireToken_OnImds_Fails_WithMtlsProofOfPossession-UAMI-ResourceId")]
        [DataRow("ecb2ad92-3e30-4505-b79f-ac640d069f24", "objectid", DisplayName = "AcquireToken_OnImds_Fails_WithMtlsProofOfPossession-UAMI-ObjectId")]
        public async Task AcquireToken_OnImds_Fails_WithMtlsProofOfPossession(string id, string idType)
        {
            var mi = BuildMi(id, idType);

            var ex = await Assert.ThrowsAsync<MsalClientException>(async () =>
                await mi.AcquireTokenForManagedIdentity(ArmScope)
                .WithMtlsProofOfPossession()
                .ExecuteAsync().ConfigureAwait(false)
            ).ConfigureAwait(false);

            Assert.AreEqual(MsalError.MtlsPopTokenNotSupportedinImdsV1, ex.ErrorCode);
        }
    }
}
