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
        /// VM unique Id
        /// </summary>
        [JsonProperty("cuid")]
        public CuidInfo Cuid { get; set; }

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
        /// <returns>false if any field is null.</returns>
        public static bool ValidateCsrMetadata(CsrMetadata csrMetadata)
        {
            if (csrMetadata == null ||
                csrMetadata.Cuid == null ||
                string.IsNullOrEmpty(csrMetadata.Cuid.Vmid) ||
                string.IsNullOrEmpty(csrMetadata.Cuid.Vmssid) ||
                string.IsNullOrEmpty(csrMetadata.ClientId) ||
                string.IsNullOrEmpty(csrMetadata.TenantId) ||
                string.IsNullOrEmpty(csrMetadata.AttestationEndpoint))
            {
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Represents VM unique Ids for CSR metadata.
    /// </summary>
    internal class CuidInfo
    {
        [JsonProperty("vmid")]
        public string Vmid { get; set; }

        [JsonProperty("vmssid")]
        public string Vmssid { get; set; }
    }
}
