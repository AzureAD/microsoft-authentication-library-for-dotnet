// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Region
{
    [Preserve(AllMembers = true)]
    internal sealed class LocalImdsErrorResponse
    {
        [JsonPropertyName("error")]
        public string Error { get; set; }

        [JsonPropertyName("newest-versions")]
        public List<string> NewestVersions { get; set; }
    }
}

