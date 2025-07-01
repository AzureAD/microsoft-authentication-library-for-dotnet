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
    internal sealed class PlatformMetadataResponse
    {
        // JSON: "client_id"
        [JsonProperty("client_id")]
        public string ClientId { get; set; }

        // JSON: "tenant_id"
        [JsonProperty("tenant_id")]
        public string TenantId { get; set; }

        // JSON: "CUID" (keep exact case)
        [JsonProperty("CUID")]
        public string Cuid { get; set; }

        // JSON: "attestation_endpoint"
        [JsonProperty("attestation_endpoint")]
        public string AttestationEndpoint { get; set; }
    }
}
