// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.Serialization;
using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client.Instance.Discovery
{
    [DataContract]
    internal sealed class InstanceDiscoveryResponse : OAuth2ResponseBase
    {
        [DataMember(Name = "tenant_discovery_endpoint", IsRequired = false)]
        public string TenantDiscoveryEndpoint { get; set; }

        [DataMember(Name = "metadata")]
        public InstanceDiscoveryMetadataEntry[] Metadata { get; set; }
    }
}
