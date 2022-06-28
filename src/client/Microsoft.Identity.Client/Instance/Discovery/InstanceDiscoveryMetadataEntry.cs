// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Identity.Client.Instance.Discovery
{
    [Preserve(AllMembers = true)]
    internal sealed class InstanceDiscoveryMetadataEntry
    {       
        [JsonPropertyName("preferred_network")]
        public string PreferredNetwork { get; set; }

        [JsonPropertyName("preferred_cache")]
        public string PreferredCache { get; set; }

        [JsonPropertyName("aliases")]
        public string[] Aliases { get; set; }
    }
}
