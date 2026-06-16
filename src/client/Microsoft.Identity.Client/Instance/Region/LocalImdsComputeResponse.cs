// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Platforms.net;
using JsonProperty = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Microsoft.Identity.Client.Region
{
    [JsonObject]
    [Preserve(AllMembers = true)]
    internal sealed class LocalImdsComputeResponse
    {
        [JsonProperty("location")]
        public string Location { get; set; }
    }
}
