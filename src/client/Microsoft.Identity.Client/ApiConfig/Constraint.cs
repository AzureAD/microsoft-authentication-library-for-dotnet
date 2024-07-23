// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.OAuth2;
#if SUPPORTS_SYSTEM_TEXT_JSON
using Microsoft.Identity.Client.Platforms.net6;
using JsonProperty = System.Text.Json.Serialization.JsonPropertyNameAttribute;
#else
using Microsoft.Identity.Json;
#endif

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Delagated Constraint
    /// </summary>
    public class Constraint
    {
        /// <summary>
        /// Specifies the type of constraint
        /// </summary>
        [JsonProperty("typ")]
        public string Type { get; set; }

        /// <summary>
        /// Specifies the action of constraint
        /// </summary>
        [JsonProperty("action")]
        public string Action { get; set; }

        /// <summary>
        /// specifies the constraint value
        /// </summary>
        [JsonProperty("target")]
        public IEnumerable<string> Values { get; set; }
    }
}
