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

        public new InstanceDiscoveryResponse DeserializeFromJson(string json)
        {
            JObject jObject = JObject.Parse(json);

            TenantDiscoveryEndpoint = jObject[TenantDiscoveryEndpointPropertyName]?.ToString();
            Metadata = ((JArray)jObject[MetadataPropertyName]).Select(c => new InstanceDiscoveryMetadataEntry().DeserializeFromJson(c.ToString())).ToArray();
            base.DeserializeFromJson(json);

            return this;
        }

        public new string SerializeToJson()
        {
            JObject jObject = new JObject(
                new JProperty(TenantDiscoveryEndpointPropertyName, TenantDiscoveryEndpoint),
                new JProperty(MetadataPropertyName, new JArray(Metadata.Select(i => JObject.Parse(i.SerializeToJson())))),
                JObject.Parse(base.SerializeToJson()).Properties());
            
            return jObject.ToString(Formatting.None);
        }
    }
}
