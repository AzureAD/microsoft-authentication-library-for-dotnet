// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
#if NET6_0_OR_GREATER
using JsonProperty = System.Text.Json.Serialization.JsonPropertyNameAttribute;
#else
using Microsoft.Identity.Json;
#endif

namespace Microsoft.Identity.Client.Region
{
#if !NET6_0_OR_GREATER
    [JsonObject]
#endif
    [Preserve(AllMembers = true)]
    internal sealed class LocalImdsErrorResponse
    {
        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("newest-versions")]
        public List<string> NewestVersions { get; set; }
    }
}

