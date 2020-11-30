// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Json;

namespace Microsoft.Identity.Client.WsTrust
{
    [JsonObject]
    [Preserve(AllMembers = true)]
    internal sealed class UserRealmDiscoveryResponse
    {
        [JsonProperty(PropertyName = "ver")]
        public string Version { get; set; }

        [JsonProperty(PropertyName = "account_type")]
        public string AccountType { get; set; }

        [JsonProperty(PropertyName = "federation_protocol")]
        public string FederationProtocol { get; set; }

        [JsonProperty(PropertyName = "federation_metadata_url")]
        public string FederationMetadataUrl { get; set; }

        [JsonProperty(PropertyName = "federation_active_auth_url")]
        public string FederationActiveAuthUrl { get; set; }

        [JsonProperty(PropertyName = "cloud_audience_urn")]
        public string CloudAudienceUrn { get; set; }

        [JsonProperty(PropertyName = "domain_name")]
        public string DomainName { get; set; }

        public bool IsFederated => string.Equals(AccountType, "federated", StringComparison.OrdinalIgnoreCase);
        public bool IsManaged => string.Equals(AccountType, "managed", StringComparison.OrdinalIgnoreCase);
    }
}
