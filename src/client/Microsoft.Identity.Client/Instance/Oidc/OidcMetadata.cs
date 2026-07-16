// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Platforms.net;
using JsonProperty = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Microsoft.Identity.Client.Instance.Oidc
{
    [JsonObject]
    [Preserve(AllMembers = true)]
    internal class OidcMetadata
    {
        [JsonProperty("issuer")]
        public string Issuer { get; set; }

        [JsonProperty("token_endpoint")]
        public string TokenEndpoint { get; set;  }

        [JsonProperty("authorization_endpoint")]
        public string AuthorizationEndpoint { get; set; }
    }
}
