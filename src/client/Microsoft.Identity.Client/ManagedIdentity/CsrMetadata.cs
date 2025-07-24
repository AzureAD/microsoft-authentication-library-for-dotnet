// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    /// <summary>
    /// Represents metadata required for Certificate Signing Request (CSR) operations.
    /// </summary>
    internal class CsrMetadata
    {
        public string ClientId { get; }  // client_id of the Managed Identity
        public string TenantId { get; }  // AAD Tenant of the Managed Identity
        public string Cuid { get; }  // VM unique Id
        public string AttestationEndpoint { get; }  // MAA Regional / Custom Endpoint for attestation purposes.

        private CsrMetadata(CsrMetadataResponse response)
        {
            ClientId = response.ClientId;
            TenantId = response.TenantId;
            Cuid = response.Cuid;
            AttestationEndpoint = response.AttestationEndpoint;
        }

        /// <summary>
        /// Tries to create a CsrMetadata instance from a CsrMetadataResponse, logs warnings and returns null if any field is missing.
        /// </summary>
        /// <param name="response">The CsrMetadataResponse object.</param>
        /// <param name="logger">The ILogger to log warnings.</param>
        /// <returns>CsrMetadata instance or null if any field is null.</returns>
        public static CsrMetadata TryCreate(CsrMetadataResponse response, ILoggerAdapter logger)
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
