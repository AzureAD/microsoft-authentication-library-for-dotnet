// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Platforms.net;
using JsonProperty = System.Text.Json.Serialization.JsonPropertyNameAttribute;

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
