// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Platforms.Json;
using JsonProperty = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Microsoft.Identity.Client.Instance.Discovery
{
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
