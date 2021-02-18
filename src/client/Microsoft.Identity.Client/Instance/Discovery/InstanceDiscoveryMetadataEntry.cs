// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Json;
using Microsoft.Identity.Json.Linq;

namespace Microsoft.Identity.Client.Instance.Discovery
{
    [JsonObject]
    [Preserve(AllMembers = true)]
    internal sealed class InstanceDiscoveryMetadataEntry : IJsonSerializable<InstanceDiscoveryMetadataEntry>
    {
        private const string PreferredNetworkPropertyName = "preferred_network";
        private const string PreferredCachePropertyName = "preferred_cache";
        private const string AliasesPropertyName = "aliases";

        [JsonProperty(PropertyName = PreferredNetworkPropertyName)]
        public string PreferredNetwork { get; set; }

        [JsonProperty(PropertyName = PreferredCachePropertyName)]
        public string PreferredCache { get; set; }

        [JsonProperty(PropertyName = AliasesPropertyName)]
        public string[] Aliases { get; set; }

        public InstanceDiscoveryMetadataEntry DeserializeFromJson(string json)
        {
            JObject jObject = JObject.Parse(json);

            PreferredNetwork = jObject[PreferredNetworkPropertyName]?.ToString();
            PreferredCache = jObject[PreferredCachePropertyName]?.ToString();
            Aliases = jObject[AliasesPropertyName] != null ? ((JArray)jObject[AliasesPropertyName]).Select(c => (string)c).ToArray() : null;

            return this;
        }

        public string SerializeToJson()
        {
            JObject jObject = new JObject(
                new JProperty(PreferredNetworkPropertyName, PreferredNetwork),
                new JProperty(PreferredCachePropertyName, PreferredCache),
                new JProperty(AliasesPropertyName, new JArray(Aliases)));

            return jObject.ToString(Formatting.None);
        }
    }
}
