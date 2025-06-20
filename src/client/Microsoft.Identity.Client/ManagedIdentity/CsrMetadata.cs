// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    /// <summary>
    /// Represents metadata required for Certificate Signing Request (CSR) operations.
    /// </summary>
    internal interface CsrMetadataInterface
    {
        string ClientId { get; } // client_id of the Managed Identity
        string TenantId { get; } // AAD Tenant of the Managed Identity
        string Cuid { get; } // VM unique Id
        string AttestationEndpoint { get; } // MAA Regional / Custom Endpoint for attestation purposes. 
    }

    /// <summary>
    /// Concrete implementation of the CsrMetadata interface.
    /// </summary>
    internal class CsrMetadata : CsrMetadataInterface
    {
        public string ClientId { get; }
        public string TenantId { get; }
        public string Cuid { get; }
        public string AttestationEndpoint { get; }

        public CsrMetadata(CsrMetadataResponse response)
        {
            ClientId = response.ClientId;
            TenantId = response.TenantId;
            Cuid = response.Cuid;
            AttestationEndpoint = response.AttestationEndpoint;
        }

        /// <summary>
        /// Creates a CsrMetadata instance from a CsrMetadataResponse, logging warnings and returning null if any field is missing.
        /// </summary>
        /// <param name="response">The CsrMetadataResponse object.</param>
        /// <param name="logger">The ILogger to log warnings.</param>
        /// <returns>CsrMetadata instance or null if any field is null.</returns>
        public static CsrMetadata CreateOrNull(CsrMetadataResponse response, ILoggerAdapter logger)
        {
            bool hasNull = false;

            if (response == null)
            {
                logger?.Warning("[CsrMetadata] CsrMetadataResponse is null.");
                return null;
            }

            if (string.IsNullOrEmpty(response.ClientId))
            {
                logger?.Warning("[CsrMetadata] ClientId is null or empty in CsrMetadataResponse.");
                hasNull = true;
            }
            if (string.IsNullOrEmpty(response.TenantId))
            {
                logger?.Warning("[CsrMetadata] TenantId is null or empty in CsrMetadataResponse.");
                hasNull = true;
            }
            if (string.IsNullOrEmpty(response.Cuid))
            {
                logger?.Warning("[CsrMetadata] Cuid is null or empty in CsrMetadataResponse.");
                hasNull = true;
            }
            if (string.IsNullOrEmpty(response.AttestationEndpoint))
            {
                logger?.Warning("[CsrMetadata] AttestationEndpoint is null or empty in CsrMetadataResponse.");
                hasNull = true;
            }

            if (hasNull)
            {
                return null;
            }

            return new CsrMetadata(response);
        }
    }
}
