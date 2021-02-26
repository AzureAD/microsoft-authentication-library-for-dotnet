// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Json;
using Microsoft.Identity.Json.Linq;

namespace Microsoft.Identity.Client.Region
{
    [JsonObject]
    [Preserve(AllMembers = true)]
    internal sealed class LocalImdsErrorResponse : IJsonSerializable<LocalImdsErrorResponse>
    {
        private const string ErrorPropertyName = "error";
        private const string NewestVersionsPropertyName = "newest-versions";

        [JsonProperty(PropertyName = ErrorPropertyName)]
        public string Error { get; set; }

        [JsonProperty(PropertyName = NewestVersionsPropertyName)]
        public List<string> NewestVersions { get; set; }

        public LocalImdsErrorResponse DeserializeFromJson(string json) => DeserializeFromJObject(JObject.Parse(json));

        public LocalImdsErrorResponse DeserializeFromJObject(JObject jObject)
        {
            Error = jObject[ErrorPropertyName]?.ToString();
            NewestVersions = jObject[NewestVersionsPropertyName] != null ? ((JArray)jObject[NewestVersionsPropertyName]).Select(c => (string)c).ToList() : null;

            return this;
        }

        public string SerializeToJson() => SerializeToJObject().ToString(Formatting.None);

        public JObject SerializeToJObject()
        {
            return new JObject(
                new JProperty(ErrorPropertyName, Error),
                new JProperty(NewestVersionsPropertyName, NewestVersions));
        }
    }
}


