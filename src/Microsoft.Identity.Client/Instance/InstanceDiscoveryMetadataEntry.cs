// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.Serialization;

namespace Microsoft.Identity.Client.Instance
{
    [DataContract]
    internal sealed class InstanceDiscoveryMetadataEntry
    {
        [DataMember(Name = "preferred_network")]
        public string PreferredNetwork { get; set; }

        [DataMember(Name = "preferred_cache")]
        public string PreferredCache { get; set; }

        [DataMember(Name = "aliases")]
        public string[] Aliases { get; set; }
    }
}
