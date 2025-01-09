// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using JsonProperty = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Microsoft.Identity.Client.Instance.Oidc
{
    [Preserve(AllMembers = true)]
    internal class OidcMetadata
    {
        [JsonProperty("token_endpoint")]
        public string TokenEndpoint { get; set;  }

        [JsonProperty("authorization_endpoint")]
        public string AuthorizationEndpoint { get; set; }
    }
}
