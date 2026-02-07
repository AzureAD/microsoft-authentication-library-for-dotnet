// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;

namespace MsalMtlsPop.Vanilla
{
    /// <summary>
    /// Production implementation of vanilla mTLS PoP flow using Managed Identity.
    /// Supports SAMI (System-Assigned) and all 3 UAMI (User-Assigned) identifier types.
    /// </summary>
    /// <remarks>
    /// Example IDs used are from PR #5726 E2E tests:
    /// - UAMI Client ID: 6325cd32-9911-41f3-819c-416cdf9104e7
    /// - UAMI Resource ID: /subscriptions/c1686c51-b717-4fe0-9af3-24a20a41fb0c/resourcegroups/MSIV2-Testing-MSALNET/providers/Microsoft.ManagedIdentity/userAssignedIdentities/msiv2uami
    /// - UAMI Object ID: ecb2ad92-3e30-4505-b79f-ac640d069f24
    /// </remarks>
    public sealed class VanillaMsiMtlsPop
    {
        /// <summary>
        /// Acquires an mTLS PoP token using System-Assigned Managed Identity (SAMI).
        /// </summary>
        /// <param name="resource">Target resource URI (e.g., "https://graph.microsoft.com").</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Authentication result containing mTLS PoP token and binding certificate.</returns>
        /// <exception cref="MsalException">Thrown when token acquisition fails.</exception>
        /// <remarks>
        /// SAMI only works in Azure environments (VM, App Service, Functions, AKS, etc.).
        /// For local development, use UAMI or Confidential Client instead.
        /// </remarks>
        public static async Task<AuthenticationResult> AcquireTokenWithSamiAsync(
            string resource,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(resource);

            var app = ManagedIdentityApplicationBuilder.Create(
                ManagedIdentityId.SystemAssigned)
                .Build();

            var result = await app
                .AcquireTokenForManagedIdentity(resource)
                .WithMtlsProofOfPossession()
                .WithAttestationSupport()  // Credential Guard attestation
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Acquires an mTLS PoP token using User-Assigned Managed Identity by Client ID.
        /// </summary>
        /// <param name="clientId">UAMI Client ID (Application ID), e.g., "6325cd32-9911-41f3-819c-416cdf9104e7".</param>
        /// <param name="resource">Target resource URI (e.g., "https://vault.azure.net").</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Authentication result containing mTLS PoP token and binding certificate.</returns>
        /// <exception cref="MsalException">Thrown when token acquisition fails.</exception>
        public static async Task<AuthenticationResult> AcquireTokenWithUamiByClientIdAsync(
            string clientId,
            string resource,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(clientId);
            ArgumentNullException.ThrowIfNull(resource);

            var app = ManagedIdentityApplicationBuilder.Create(
                ManagedIdentityId.WithUserAssignedClientId(clientId))
                .Build();

            var result = await app
                .AcquireTokenForManagedIdentity(resource)
                .WithMtlsProofOfPossession()
                .WithAttestationSupport()  // Credential Guard attestation
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Acquires an mTLS PoP token using User-Assigned Managed Identity by Resource ID.
        /// </summary>
        /// <param name="resourceId">UAMI Azure Resource Manager path.</param>
        /// <param name="resource">Target resource URI (e.g., "https://storage.azure.com").</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Authentication result containing mTLS PoP token and binding certificate.</returns>
        /// <exception cref="MsalException">Thrown when token acquisition fails.</exception>
        /// <remarks>
        /// Example Resource ID: "/subscriptions/{sub-id}/resourcegroups/{rg-name}/providers/Microsoft.ManagedIdentity/userAssignedIdentities/{identity-name}"
        /// </remarks>
        public static async Task<AuthenticationResult> AcquireTokenWithUamiByResourceIdAsync(
            string resourceId,
            string resource,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(resourceId);
            ArgumentNullException.ThrowIfNull(resource);

            var app = ManagedIdentityApplicationBuilder.Create(
                ManagedIdentityId.WithUserAssignedResourceId(resourceId))
                .Build();

            var result = await app
                .AcquireTokenForManagedIdentity(resource)
                .WithMtlsProofOfPossession()
                .WithAttestationSupport()  // Credential Guard attestation
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Acquires an mTLS PoP token using User-Assigned Managed Identity by Object ID.
        /// </summary>
        /// <param name="objectId">UAMI Object ID (Principal ID), e.g., "ecb2ad92-3e30-4505-b79f-ac640d069f24".</param>
        /// <param name="resource">Target resource URI (e.g., "https://management.azure.com").</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Authentication result containing mTLS PoP token and binding certificate.</returns>
        /// <exception cref="MsalException">Thrown when token acquisition fails.</exception>
        public static async Task<AuthenticationResult> AcquireTokenWithUamiByObjectIdAsync(
            string objectId,
            string resource,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(objectId);
            ArgumentNullException.ThrowIfNull(resource);

            var app = ManagedIdentityApplicationBuilder.Create(
                ManagedIdentityId.WithUserAssignedObjectId(objectId))
                .Build();

            var result = await app
                .AcquireTokenForManagedIdentity(resource)
                .WithMtlsProofOfPossession()
                .WithAttestationSupport()  // Credential Guard attestation
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            return result;
        }
    }
}
