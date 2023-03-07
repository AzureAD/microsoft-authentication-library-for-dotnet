// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
#if SUPPORTS_SYSTEM_TEXT_JSON
using Microsoft.Identity.Client.Platforms.net6;
using JsonProperty = System.Text.Json.Serialization.JsonPropertyNameAttribute;
#else
using Microsoft.Identity.Json;
#endif

namespace Microsoft.Identity.Client.ManagedIdentity
{
    [JsonObject]
    [Preserve(AllMembers = true)]
    internal class ManagedIdentityErrorResponse
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("correlationId")]
        public string CorrelationId { get; set; }

        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("error_description")]
        public string ErrorDescription { get; set; }
    }
}
