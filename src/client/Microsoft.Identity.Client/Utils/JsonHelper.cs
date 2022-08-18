// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
#if NET6_0_OR_GREATER
using System.Text.Json;
using System.Text.Json.Nodes;
#else
using Microsoft.Identity.Json;
using Microsoft.Identity.Json.Linq;
#endif

#if NET6_0_OR_GREATER
#else
#endif

namespace Microsoft.Identity.Client.Utils
{
    internal static class JsonHelper
    {
        internal static string SerializeToJson<T>(T toEncode)
        {
            return JsonConvert.SerializeObject(toEncode);
        }

        internal static T DeserializeFromJson<T>(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return default;
            }

            return JsonConvert.DeserializeObject<T>(json);
        }

        internal static T TryToDeserializeFromJson<T>(string json, RequestContext requestContext = null)
        {
            if (string.IsNullOrEmpty(json))
            {
                return default;
            }

            T result = default;
            try
            {
                result = DeserializeFromJson<T>(json.ToByteArray());
            }
            catch (JsonException ex)
            {
                requestContext?.Logger?.WarningPii(ex);
            }

            return result;
        }

        internal static T DeserializeFromJson<T>(byte[] jsonByteArray)
        {
            if (jsonByteArray == null || jsonByteArray.Length == 0)
            {
                return default;
            }

            using (var stream = new MemoryStream(jsonByteArray))
            using (var reader = new StreamReader(stream, Encoding.UTF8))
                return (T)JsonSerializer.Create().Deserialize(reader, typeof(T));
        }

        internal static string GetExistingOrEmptyString(JObject json, string key)
        {
            if (json.TryGetValue(key, out var val))
            {
                return val.ToObject<string>();
            }

            return string.Empty;
        }

        internal static string ExtractExistingOrEmptyString(JObject json, string key)
        {
            if (json.TryGetValue(key, out var val))
            {
                string strVal = val.ToObject<string>();
                json.Remove(key);
                return strVal;
            }

            return string.Empty;
        }

        internal static IDictionary<string, string> ExtractInnerJsonAsDictionary(JObject json, string key)
        {
            if (json.TryGetValue(key, out JToken val))
            {
                IDictionary<string, JToken> valueAsDict = (JObject)val;
                Dictionary<string, string> dictionary =
                    valueAsDict.ToDictionary(pair => pair.Key, pair => (string)pair.Value);

                json.Remove(key);
                return dictionary;
            }

            return null;
        }

        internal static T ExtractExistingOrDefault<T>(JObject json, string key)
        {
            if (json.TryGetValue(key, out var val))
            {
                T obj = val.ToObject<T>();
                json.Remove(key);
                return obj;
            }

            return default;
        }

        internal static long ExtractParsedIntOrZero(JObject json, string key)
        {
            string strVal = ExtractExistingOrEmptyString(json, key);
            if (!string.IsNullOrWhiteSpace(strVal) && long.TryParse(strVal, out long result))
            {
                return result;
            }

            return 0;
        }

#if NET6_0_OR_GREATER
        //internal static JsonObject CreateJsonObject() => new();

        internal static string JsonObjectToString(JsonObject jsonObject) => jsonObject.ToJsonString();

        //internal static JsonNode ParseIntoJsonNode(string json) => JsonNode.Parse(json);

        internal static JsonObject ParseIntoJsonObject(string json) => JsonNode.Parse(json).AsObject();

        internal static bool TryGetValue(JsonObject json, string propertyName, out JsonNode value) => json.TryGetPropertyValue(propertyName, out value);

        internal static T GetValue<T>(JsonNode json) => json.GetValue<T>();
#else
        //internal static JObject CreateJsonObject() => new();

        internal static string JsonObjectToString(JObject jsonObject) => jsonObject.ToString(Formatting.None);

        //internal static JToken ParseIntoJsonNode(string json) => JToken.Parse(json);

        internal static JObject ParseIntoJsonObject(string json) => JObject.Parse(json);

        internal static bool TryGetValue(JObject json, string propertyName, out JToken value) => json.TryGetValue(propertyName, out value);

        internal static T GetValue<T>(JToken json) => json.Value<T>();
#endif
    }
}
