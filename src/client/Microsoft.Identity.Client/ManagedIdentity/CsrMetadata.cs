// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if SUPPORTS_SYSTEM_TEXT_JSON
    using Microsoft.Identity.Client.Platforms.net;
    using JsonProperty = System.Text.Json.Serialization.JsonPropertyNameAttribute;
#else
    using Microsoft.Identity.Json;
#endif

namespace Microsoft.Identity.Client.ManagedIdentity
{
    /// <summary>
    /// Represents metadata required for Certificate Signing Request (CSR) operations.
    /// </summary>
    internal class CsrMetadata
    {
        /// <summary>
        /// client_id of the Managed Identity
        /// </summary>
        [JsonProperty("client_id")]
        public string ClientId { get; set; }

        /// <summary>
        /// AAD Tenant of the Managed Identity
        /// </summary>
        [JsonProperty("tenant_id")] 
        public string TenantId { get; set; }

        /// <summary>
        /// VM unique Id
        /// </summary>
        [JsonProperty("CUID")] 
        public string Cuid { get; set; }

        /// <summary>
        /// MAA Regional / Custom Endpoint for attestation purposes.
        /// </summary>
        [JsonProperty("attestation_endpoint")] 
        public string AttestationEndpoint { get; set; }

        // Parameterless constructor for deserialization
        public CsrMetadata() { }

        /// <summary>
        /// Tries to create a CsrMetadata instance from a CsrMetadataResponse.
        /// </summary>
        /// <param name="csrMetadata">TheCcsrMetadata object.</param>
        /// <returns>false if any field is null.</returns>
        public static bool ValidateCsrMetadata(CsrMetadata csrMetadata)
        {
            if (csrMetadata == null ||
                string.IsNullOrEmpty(csrMetadata.ClientId) ||
                string.IsNullOrEmpty(csrMetadata.TenantId) ||
                string.IsNullOrEmpty(csrMetadata.Cuid) ||
                string.IsNullOrEmpty(csrMetadata.AttestationEndpoint))
            {
                return false;
            }

            return true;
        }
    }
}
