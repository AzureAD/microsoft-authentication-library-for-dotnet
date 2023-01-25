// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
#if SUPPORTS_SYSTEM_TEXT_JSON
using Microsoft.Identity.Client.Platforms.net6;
using System.Text.Json;
using System.Text.Json.Nodes;
using JObject = System.Text.Json.Nodes.JsonObject;
using JToken = System.Text.Json.Nodes.JsonNode;
#else
using Microsoft.Identity.Json;
using Microsoft.Identity.Json.Linq;
#endif

namespace Microsoft.Identity.Client.Utils
{
    internal static class JsonHelper
    {
        internal static string SerializeToJson<T>(T toEncode)
        {
#if SUPPORTS_SYSTEM_TEXT_JSON
            return JsonSerializer.Serialize(toEncode, typeof(T), MsalJsonSerializerContext.Custom);
#else
            return JsonConvert.SerializeObject(toEncode);
#endif
        }

        internal static T DeserializeFromJson<T>(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return default;
            }
#if SUPPORTS_SYSTEM_TEXT_JSON
            return (T)JsonSerializer.Deserialize(json, typeof(T), MsalJsonSerializerContext.Custom);
#else
            return JsonConvert.DeserializeObject<T>(json);
#endif
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
            {
#if SUPPORTS_SYSTEM_TEXT_JSON
                return (T)JsonSerializer.Deserialize(stream, typeof(T), MsalJsonSerializerContext.Custom);
#else
                return (T)JsonSerializer.Create().Deserialize(reader, typeof(T));
#endif
            }
        }

        internal static string GetExistingOrEmptyString(JObject json, string key)
        {
            if (TryGetValue(json, key, out var val))
            {
                return GetValue<string>(val);
            }

            return string.Empty;
        }

        internal static string ExtractExistingOrEmptyString(JObject json, string key)
        {
            if (TryGetValue(json, key, out var val))
            {
                string strVal = GetValue<string>(val);
                json.Remove(key);
                return strVal;
            }

            return string.Empty;
        }

        internal static IDictionary<string, string> ExtractInnerJsonAsDictionary(JObject json, string key)
        {
            if (TryGetValue(json, key, out JToken val))
            {
                IDictionary<string, JToken> valueAsDict = ToJsonObject(val);
                Dictionary<string, string> dictionary =
                    valueAsDict.ToDictionary(pair => pair.Key, pair => (string)pair.Value);

                json.Remove(key);
                return dictionary;
            }

            return null;
        }

        internal static T ExtractExistingOrDefault<T>(JObject json, string key)
        {
            if (TryGetValue(json, key, out var val))
            {
                T obj = GetValue<T>(val);
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

#if SUPPORTS_SYSTEM_TEXT_JSON
        internal static string JsonObjectToString(JsonObject jsonObject) => jsonObject.ToJsonString();

        internal static JsonObject ParseIntoJsonObject(string json) => JsonNode.Parse(json).AsObject();

        internal static JsonObject ToJsonObject(JsonNode jsonNode) => jsonNode.AsObject();

        internal static bool TryGetValue(JsonObject json, string propertyName, out JsonNode value) => json.TryGetPropertyValue(propertyName, out value);

        internal static T GetValue<T>(JsonNode json) => json != null ? json.GetValue<T>() : default;
#else
        internal static string JsonObjectToString(JObject jsonObject) => jsonObject.ToString(Formatting.None);

        internal static JObject ParseIntoJsonObject(string json) => JObject.Parse(json);

        internal static JObject ToJsonObject(JToken jsonNode) => (JObject)jsonNode;

        internal static bool TryGetValue(JObject json, string propertyName, out JToken value) => json.TryGetValue(propertyName, out value);

        internal static T GetValue<T>(JToken json) => json.Value<T>();
#endif
    }
}
