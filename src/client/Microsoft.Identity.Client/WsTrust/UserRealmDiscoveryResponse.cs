// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Platforms.Json;
using JsonProperty = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Microsoft.Identity.Client.WsTrust
{
    [Preserve(AllMembers = true)]
    internal sealed class UserRealmDiscoveryResponse
    {
        [JsonProperty("ver")]
        public string Version { get; set; }

        [JsonProperty("account_type")]
        public string AccountType { get; set; }

        [JsonProperty("federation_protocol")]
        public string FederationProtocol { get; set; }

        [JsonProperty("federation_metadata_url")]
        public string FederationMetadataUrl { get; set; }

        [JsonProperty("federation_active_auth_url")]
        public string FederationActiveAuthUrl { get; set; }

        [JsonProperty("cloud_audience_urn")]
        public string CloudAudienceUrn { get; set; }

        [JsonProperty("domain_name")]
        public string DomainName { get; set; }

        public bool IsFederated => string.Equals(AccountType, "federated", StringComparison.OrdinalIgnoreCase);
        public bool IsManaged => string.Equals(AccountType, "managed", StringComparison.OrdinalIgnoreCase);
    }
}
