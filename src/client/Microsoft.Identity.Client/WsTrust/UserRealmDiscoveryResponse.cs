// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.Identity.Client.WsTrust
{
    [Preserve(AllMembers = true)]
    internal sealed class UserRealmDiscoveryResponse
    {
        [JsonPropertyName("ver")]
        public string Version { get; set; }

        [JsonPropertyName("account_type")]
        public string AccountType { get; set; }

        [JsonPropertyName("federation_protocol")]
        public string FederationProtocol { get; set; }

        [JsonPropertyName("federation_metadata_url")]
        public string FederationMetadataUrl { get; set; }

        [JsonPropertyName("federation_active_auth_url")]
        public string FederationActiveAuthUrl { get; set; }

        [JsonPropertyName("cloud_audience_urn")]
        public string CloudAudienceUrn { get; set; }

        [JsonPropertyName("domain_name")]
        public string DomainName { get; set; }

        public bool IsFederated => string.Equals(AccountType, "federated", StringComparison.OrdinalIgnoreCase);
        public bool IsManaged => string.Equals(AccountType, "managed", StringComparison.OrdinalIgnoreCase);
    }
}
