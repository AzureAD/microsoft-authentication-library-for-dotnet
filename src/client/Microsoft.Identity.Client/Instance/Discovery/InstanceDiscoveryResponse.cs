// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Json;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Instance.Discovery
{
    [JsonObject]
    [Preserve(AllMembers = true)]
    internal sealed class InstanceDiscoveryResponse : OAuth2ResponseBase, IJsonSerializable<InstanceDiscoveryResponse>
    {
        [JsonProperty(PropertyName = "tenant_discovery_endpoint")]
        public string TenantDiscoveryEndpoint { get; set; }

        [JsonProperty(PropertyName = "metadata")]
        public InstanceDiscoveryMetadataEntry[] Metadata { get; set; }

        public InstanceDiscoveryResponse FromJsonString(string json)
        {
            throw new System.NotImplementedException();
        }

        public string ToJsonString(InstanceDiscoveryResponse objectToSerialize)
        {
            throw new System.NotImplementedException();
        }
    }
}
