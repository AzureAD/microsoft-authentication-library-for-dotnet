// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
#if SUPPORTS_SYSTEM_TEXT_JSON
using Microsoft.Identity.Client.Platforms.net;
using JsonProperty = System.Text.Json.Serialization.JsonPropertyNameAttribute;
#else
using Microsoft.Identity.Json;
#endif

namespace Microsoft.Identity.Client.ManagedIdentity
{
    [JsonObject]
    [Preserve(AllMembers = true)]
    internal class CsrMetadataResponse
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
    }
}
