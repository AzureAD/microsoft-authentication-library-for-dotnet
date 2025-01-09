﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Identity.Client.Platforms.Json;
using JsonProperty = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Microsoft.Identity.Client.Region
{
    [Preserve(AllMembers = true)]
    internal sealed class LocalImdsErrorResponse
    {
        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("newest-versions")]
        public List<string> NewestVersions { get; set; }
    }
}

