// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if SUPPORTS_SYSTEM_TEXT_JSON
using Microsoft.Identity.Client.Platforms.net6;
using JsonProperty = System.Text.Json.Serialization.JsonPropertyNameAttribute;
#else
using Microsoft.Identity.Json;
#endif

namespace Microsoft.Identity.Client.Instance.Oidc
{
    [JsonObject]
    [Preserve(AllMembers = true)]
    internal class OidcMetadata
    {
        [JsonProperty("token_endpoint")]
        public string TokenEndpoint { get; set;  }

        [JsonProperty("authorization_endpoint")]
        public string AuthorizationEndpoint { get; set; }
    }
}
