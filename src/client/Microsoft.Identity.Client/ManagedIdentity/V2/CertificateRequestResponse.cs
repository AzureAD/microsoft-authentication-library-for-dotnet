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
    /// Represents the response for a Managed Identity CSR request.
    /// </summary>
    internal class CertificateRequestResponse
    {
        [JsonProperty("client_id")]
        public string ClientId { get; set; }

        [JsonProperty("tenant_id")]
        public string TenantId { get; set; }

        [JsonProperty("client_credential")]
        public string ClientCredential { get; set; }

        [JsonProperty("regional_token_url")]
        public string RegionalTokenUrl { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("refresh_in")]
        public int RefreshIn { get; set; }

        public CertificateRequestResponse() { }

        public static bool IsValid(CertificateRequestResponse certificateRequestResponse)
        {
            if (string.IsNullOrEmpty(certificateRequestResponse.ClientId) ||
                string.IsNullOrEmpty(certificateRequestResponse.TenantId) ||
                string.IsNullOrEmpty(certificateRequestResponse.ClientCredential) ||
                string.IsNullOrEmpty(certificateRequestResponse.RegionalTokenUrl) ||
                certificateRequestResponse.ExpiresIn <= 0 ||
                certificateRequestResponse.RefreshIn <= 0)
            {
                return false;
            }

            return true;
        }
    }
}
