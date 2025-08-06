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
    internal class CsrRequestResponse
    {
        [JsonProperty("client_id")]
        public string ClientId { get; }

        [JsonProperty("tenant_id")]
        public string TenantId { get; }

        [JsonProperty("client_credential")]
        public string ClientCredential { get; }

        [JsonProperty("regional_token_url")]
        public string RegionalTokenUrl { get; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; }

        [JsonProperty("refresh_in")]
        public int RefreshIn { get; }

        public CsrRequestResponse() { }

        public static bool ValidateCsrRequestResponse(CsrRequestResponse csrRequestResponse)
        {
            if (string.IsNullOrEmpty(csrRequestResponse.ClientId) ||
                string.IsNullOrEmpty(csrRequestResponse.TenantId) ||
                string.IsNullOrEmpty(csrRequestResponse.ClientCredential) ||
                string.IsNullOrEmpty(csrRequestResponse.RegionalTokenUrl) ||
                csrRequestResponse.ExpiresIn <= 0 ||
                csrRequestResponse.RefreshIn <= 0)
            {
                return false;
            }

            return true;
        }
    }
}
