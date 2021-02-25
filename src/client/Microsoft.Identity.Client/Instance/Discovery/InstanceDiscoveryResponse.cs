// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Json;
using Microsoft.Identity.Json.Linq;

namespace Microsoft.Identity.Client.Instance.Discovery
{
    [JsonObject]
    [Preserve(AllMembers = true)]
    internal sealed class InstanceDiscoveryResponse : OAuth2ResponseBase, IJsonSerializable<InstanceDiscoveryResponse>
    {
        private const string TenantDiscoveryEndpointPropertyName = "tenant_discovery_endpoint";
        private const string MetadataPropertyName = "metadata";

        [JsonProperty(PropertyName = TenantDiscoveryEndpointPropertyName)]
        public string TenantDiscoveryEndpoint { get; set; }

        [JsonProperty(PropertyName = MetadataPropertyName)]
        public InstanceDiscoveryMetadataEntry[] Metadata { get; set; }

        public new InstanceDiscoveryResponse DeserializeFromJson(string json) => DeserializeFromJObject(JObject.Parse(json));

        public new InstanceDiscoveryResponse DeserializeFromJObject(JObject jObject)
        {
            TenantDiscoveryEndpoint = jObject[TenantDiscoveryEndpointPropertyName]?.ToString();
            Metadata = jObject[MetadataPropertyName] != null ? ((JArray)jObject[MetadataPropertyName]).Select(c => new InstanceDiscoveryMetadataEntry().DeserializeFromJObject((JObject)c)).ToArray() : null;
            base.DeserializeFromJObject(jObject);

            return this;
        }

        public new string SerializeToJson() => SerializeToJObject().ToString(Formatting.None);

        public new JObject SerializeToJObject()
        {
            return new JObject(
                new JProperty(TenantDiscoveryEndpointPropertyName, TenantDiscoveryEndpoint),
                new JProperty(MetadataPropertyName, new JArray(Metadata.Select(i => i.SerializeToJObject()))),
                base.SerializeToJObject().Properties());
        }
    }
}
