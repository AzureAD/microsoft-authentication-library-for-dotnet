// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Json;
#if iOS
using Foundation;
#endif
#if ANDROID
using Android.Runtime;
#endif

namespace Microsoft.Identity.Client.WsTrust
{
    [JsonObject]
#if ANDROID || iOS
    [Preserve(AllMembers = true)]
#endif
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

        public bool IsFederated => string.Equals(AccountType, "federated", StringComparison.OrdinalIgnoreCase);
        public bool IsManaged => string.Equals(AccountType, "managed", StringComparison.OrdinalIgnoreCase);
    }
}
