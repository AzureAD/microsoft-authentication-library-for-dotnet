// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client.Instance.Discovery
{
    [Preserve(AllMembers = true)]
    internal sealed class InstanceDiscoveryResponse : OAuth2ResponseBase
    {
        [JsonPropertyName("tenant_discovery_endpoint")]
        public string TenantDiscoveryEndpoint { get; set; }

        [JsonPropertyName("metadata")]
        public InstanceDiscoveryMetadataEntry[] Metadata { get; set; }
    }
}
