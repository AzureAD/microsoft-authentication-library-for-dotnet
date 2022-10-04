// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
#if SUPPORTS_SYSTEM_TEXT_JSON
using Microsoft.Identity.Client.Platforms.net6;
using JsonProperty = System.Text.Json.Serialization.JsonPropertyNameAttribute;
#else
using Microsoft.Identity.Json;
#endif

namespace Microsoft.Identity.Client.Region
{
    [JsonObject]
    [Preserve(AllMembers = true)]
    internal sealed class LocalImdsErrorResponse
    {
        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("newest-versions")]
        public List<string> NewestVersions { get; set; }
    }
}

