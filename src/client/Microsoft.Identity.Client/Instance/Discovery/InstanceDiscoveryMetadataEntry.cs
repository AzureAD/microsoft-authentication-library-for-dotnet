// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.Serialization;
using Microsoft.Identity.Json;

namespace Microsoft.Identity.Client.Instance.Discovery
{
    [JsonObject]
    [DataContract]
    [Preserve]
    internal sealed class InstanceDiscoveryMetadataEntry
    {
        [JsonProperty(PropertyName = "preferred_network")]
        public string PreferredNetwork { get; set; }

        [JsonProperty(PropertyName = "preferred_cache")]
        public string PreferredCache { get; set; }

        [JsonProperty(PropertyName = "aliases")]
        public string[] Aliases { get; set; }
    }
}
