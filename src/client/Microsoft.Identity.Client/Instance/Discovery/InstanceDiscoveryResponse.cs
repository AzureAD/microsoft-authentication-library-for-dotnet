// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.OAuth2;
#if SUPPORTS_SYSTEM_TEXT_JSON
using Microsoft.Identity.Client.Platforms.net6;
using JsonProperty = System.Text.Json.Serialization.JsonPropertyNameAttribute;
#else
using Microsoft.Identity.Json;
#endif

namespace Microsoft.Identity.Client.Instance.Discovery
{
    [JsonObject]
    [Preserve(AllMembers = true)]
    internal sealed class InstanceDiscoveryResponse : OAuth2ResponseBase
    {
        [JsonProperty("tenant_discovery_endpoint")]
        public string TenantDiscoveryEndpoint { get; set; }

        [JsonProperty("metadata")]
        public InstanceDiscoveryMetadataEntry[] Metadata { get; set; }
    }
}
