// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
#if SUPPORTS_SYSTEM_TEXT_JSON
using Microsoft.Identity.Client.Platforms.net6;
using JsonProperty = System.Text.Json.Serialization.JsonPropertyNameAttribute;
#else
using Microsoft.Identity.Json;
#endif

namespace Microsoft.Identity.Client.Credential
{
    [JsonObject]
    [Preserve(AllMembers = true)]
    internal class CredentialResponse
    {
        [JsonProperty("client_id")]
        public string ClientId { get; set; }

        [JsonProperty("credential")]
        public string Credential { get; set; }

        [JsonProperty("expires_on")]
        public long ExpiresOn { get; set; }

        [JsonProperty("identity_type")]
        public string IdentityType { get; set; }

        [JsonProperty("refresh_in")]
        public long RefreshIn { get; set; }

        [JsonProperty("regional_token_url")]
        public string RegionalTokenUrl { get; set; }

        [JsonProperty("tenant_id")]
        public string TenantId { get; set; }

    }
}
