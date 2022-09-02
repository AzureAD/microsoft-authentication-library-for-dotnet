// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
    internal sealed class InstanceDiscoveryMetadataEntry
    {
        [JsonProperty("preferred_network")]
        public string PreferredNetwork { get; set; }

        [JsonProperty("preferred_cache")]
        public string PreferredCache { get; set; }

        [JsonProperty("aliases")]
        public string[] Aliases { get; set; }
    }
}
