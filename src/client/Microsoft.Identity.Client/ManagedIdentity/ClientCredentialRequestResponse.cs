// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if SUPPORTS_SYSTEM_TEXT_JSON
    using JsonProperty = System.Text.Json.Serialization.JsonPropertyNameAttribute;
#else
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Json;
#endif

namespace Microsoft.Identity.Client.ManagedIdentity
{
    /// <summary>
    /// Represents the response for a Managed Identity CSR request.
    /// </summary>
    internal class ClientCredentialRequestResponse
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

        public ClientCredentialRequestResponse() { }

        public static bool IsValid(ClientCredentialRequestResponse clientCredentialRequestResponse)
        {
            if (string.IsNullOrEmpty(clientCredentialRequestResponse.ClientId) ||
                string.IsNullOrEmpty(clientCredentialRequestResponse.TenantId) ||
                string.IsNullOrEmpty(clientCredentialRequestResponse.ClientCredential) ||
                string.IsNullOrEmpty(clientCredentialRequestResponse.RegionalTokenUrl) ||
                clientCredentialRequestResponse.ExpiresIn <= 0 ||
                clientCredentialRequestResponse.RefreshIn <= 0)
            {
                return false;
            }

            return true;
        }
    }
}
