// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if SUPPORTS_SYSTEM_TEXT_JSON
    using JsonProperty = System.Text.Json.Serialization.JsonPropertyNameAttribute;
#else
    using Microsoft.Identity.Json;
#endif

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    /// <summary>
    /// Represents VM unique Ids for CSR metadata.
    /// </summary>
    internal class CuidInfo
    {
        [JsonProperty("vmId")]
        public string VmId { get; set; }

        [JsonProperty("vmssId")]
        public string VmssId { get; set; }
    }

    /// <summary>
    /// Represents metadata required for Certificate Signing Request (CSR) operations.
    /// </summary>
    internal class CsrMetadata
    {
        /// <summary>
        /// VM unique Id
        /// </summary>
        [JsonProperty("cuId")]
        public CuidInfo CuId { get; set; }

        /// <summary>
        /// client_id of the Managed Identity
        /// </summary>
        [JsonProperty("clientId")]
        public string ClientId { get; set; }

        /// <summary>
        /// AAD Tenant of the Managed Identity
        /// </summary>
        [JsonProperty("tenantId")] 
        public string TenantId { get; set; }

        /// <summary>
        /// MAA Regional / Custom Endpoint for attestation purposes.
        /// </summary>
        [JsonProperty("attestationEndpoint")] 
        public string AttestationEndpoint { get; set; }

        // Parameterless constructor for deserialization
        public CsrMetadata() { }

        /// <summary>
        /// Validates a JSON decoded CsrMetadata instance.
        /// </summary>
        /// <param name="csrMetadata">The CsrMetadata object.</param>
        /// <returns>false if any required field is null. Note: VmId is required, VmssId is optional.</returns>
        public static bool ValidateCsrMetadata(CsrMetadata csrMetadata)
        {
            if (csrMetadata == null ||
                csrMetadata.CuId == null ||
                string.IsNullOrEmpty(csrMetadata.CuId.VmId) ||
                string.IsNullOrEmpty(csrMetadata.ClientId) ||
                string.IsNullOrEmpty(csrMetadata.TenantId) ||
                string.IsNullOrEmpty(csrMetadata.AttestationEndpoint))
            {
                return false;
            }

            return true;
        }
    }
}
