// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Collections.Generic;

namespace KerberosConsole
{
    /// <summary>
    /// Represents an one page of results from an AAD Graph query.
    /// </summary>
    public class Page
    {
        /// <summary>
        /// Request meta data.
        /// </summary>
        [JsonProperty("odata.metadata")]
        public string MetaData { get; set; }

        /// <summary>
        /// Request next page.
        /// </summary>
        [JsonProperty("odata.nextLink")]
        public string NextLink { get; set; }

        /// <summary>
        /// Request result objects.
        /// </summary>
        [JsonProperty("value")]
        public List<JObject> Results { get; set; }

        /// <summary>
        /// Convert the object to a JSON string.
        /// </summary>
        /// <returns>The JSON string.</returns>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
        }
    }
}
