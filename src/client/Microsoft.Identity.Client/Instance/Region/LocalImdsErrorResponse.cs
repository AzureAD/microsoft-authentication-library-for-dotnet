// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Json;

namespace Microsoft.Identity.Client.Region
{
    [JsonObject]
    [Preserve(AllMembers = true)]
    internal sealed class LocalImdsErrorResponse
    {
        [JsonProperty(PropertyName = "error")]
        public string Error { get; set; }

        [JsonProperty(PropertyName = "newest-versions")]
        public List<string> NewestVersions { get; set; }
    }
}

