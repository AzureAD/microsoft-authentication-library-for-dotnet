// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace Microsoft.Identity.Client.Utils
{
    internal static class JsonUtils
    {
        public static string GetExistingOrEmptyString(JsonObject json, string key)
        {
            if (json.TryGetPropertyValue(key, out var val))
            {
                return val?.GetValue<string>();
            }

            return string.Empty;
        }

        public static string ExtractExistingOrEmptyString(JsonObject json, string key)
        {
            if (json.TryGetPropertyValue(key, out var val))
            {
                string strVal = val?.GetValue<string>();
                json.Remove(key);
                return strVal;
            }

            return string.Empty;
        }

        public static IDictionary<string, string> ExtractInnerJsonAsDictionary(JsonObject json, string key)
        {
            if (json.TryGetPropertyValue(key, out JsonNode val))
            {
                IDictionary<string, JsonNode> valueAsDict = val.AsObject();
                Dictionary<string, string> dictionary =
                    valueAsDict.ToDictionary(pair => pair.Key, pair => (string)pair.Value);

                json.Remove(key);
                return dictionary;
            }

            return null;
        }

        public static T ExtractExistingOrDefault<T>(JsonObject json, string key)
            where T : class
        {
            if (json.TryGetPropertyValue(key, out var val))
            {
                T obj = val?.GetValue<T>();
                json.Remove(key);
                return obj;
            }

            return default(T);
        }

        public static long ExtractParsedIntOrZero(JsonObject json, string key)
        {
            string strVal = ExtractExistingOrEmptyString(json, key);
            if (!string.IsNullOrWhiteSpace(strVal) && long.TryParse(strVal, out long result))
            {
                return result;
            }

            return 0;
        }
    }
}
