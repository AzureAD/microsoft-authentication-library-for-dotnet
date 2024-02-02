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
using System.Buffers;
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
            
            return JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings() { 
                DateParseHandling = DateParseHandling.None, // Newtonsoft tries to be smart about dates, but System.Text.Json does not                
            });
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

            using var stream = new MemoryStream(jsonByteArray);
#if SUPPORTS_SYSTEM_TEXT_JSON
            return (T)JsonSerializer.Deserialize(stream, typeof(T), MsalJsonSerializerContext.Custom);
#else
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return (T)JsonSerializer.Create().Deserialize(reader, typeof(T));
#endif
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

        /// <summary>
        /// Merges two JSON objects into a single JSON object.
        /// </summary>
        /// <param name="originalJson">The original JSON object to merge.</param>
        /// <param name="newContent">The additional JSON object to merge.</param>
        /// <returns>A JObject representing the merged JSON.</returns>
        /// <remarks>
        /// This method parses the original and new JSON objects, merges their elements, and returns
        /// a JObject representing the merged JSON.
        /// Original Code Reference: https://github.com/dotnet/runtime/issues/31433
        /// </remarks>
        internal static JObject Merge(JObject originalJson, JObject newContent)
        {
            // Output buffer to store the merged JSON
            var outputBuffer = new ArrayBufferWriter<byte>();

            // Parse the original and new JSON content
            using (JsonDocument jDoc1 = JsonDocument.Parse(originalJson.ToJsonString()))
            using (JsonDocument jDoc2 = JsonDocument.Parse(newContent.ToJsonString()))
            using (var jsonWriter = new Utf8JsonWriter(outputBuffer, new JsonWriterOptions { Indented = true }))
            {
                // Merge the JSON elements
                MergeJsonElements(jsonWriter, jDoc1.RootElement, jDoc2.RootElement);
            }

            // Convert the merged JSON to a UTF-8 encoded string
            string mergedJsonString = Encoding.UTF8.GetString(outputBuffer.WrittenSpan);

            // Parse the merged JSON string to a JObject
            return ParseIntoJsonObject(mergedJsonString);
        }

        // Merges two JSON elements based on their value kind
        private static void MergeJsonElements(Utf8JsonWriter jsonWriter, JsonElement root1, JsonElement root2)
        {
            switch (root1.ValueKind)
            {
                case JsonValueKind.Object:
                    MergeObjects(jsonWriter, root1, root2);
                    break;
                case JsonValueKind.Array:
                    MergeArrays(jsonWriter, root1, root2);
                    break;
                default:
                    // If not an object or array, directly write the value to the output
                    root1.WriteTo(jsonWriter);
                    break;
            }
        }

        // Merges two JSON objects
        private static void MergeObjects(Utf8JsonWriter jsonWriter, JsonElement root1, JsonElement root2)
        {
            // Start writing the merged object
            jsonWriter.WriteStartObject();

            // Create a HashSet to track processed property names
            HashSet<string> processedProperties = new HashSet<string>();

            // Iterate through properties of the first JSON object
            foreach (JsonProperty property in root1.EnumerateObject())
            {
                string propertyName = property.Name;

                JsonValueKind newValueKind;

                // Check if the second JSON object has a property with the same name
                if (root2.TryGetProperty(propertyName, out JsonElement newValue) && (newValueKind = newValue.ValueKind) != JsonValueKind.Null)
                {
                    // Write the property name
                    jsonWriter.WritePropertyName(propertyName);
                    processedProperties.Add(propertyName);

                    JsonElement originalValue = property.Value;
                    JsonValueKind originalValueKind = originalValue.ValueKind;

                    // Recursively merge objects or arrays, otherwise, write the new value
                    if ((newValueKind == JsonValueKind.Object && originalValueKind == JsonValueKind.Object) ||
                        (newValueKind == JsonValueKind.Array && originalValueKind == JsonValueKind.Array))
                    {
                        MergeJsonElements(jsonWriter, originalValue, newValue);
                    }
                    else
                    {
                        newValue.WriteTo(jsonWriter);
                    }
                }
                else
                {
                    // If the second object does not have the property, write the original property
                    property.WriteTo(jsonWriter);
                }
            }

            // Iterate through properties unique to the second JSON object
            foreach (JsonProperty property in root2.EnumerateObject())
            {
                if (!processedProperties.Contains(property.Name))
                {
                    // Write properties unique to the second object
                    property.WriteTo(jsonWriter);
                }
            }

            // End writing the merged object
            jsonWriter.WriteEndObject();
        }

        // Merges two JSON arrays
        private static void MergeArrays(Utf8JsonWriter jsonWriter, JsonElement root1, JsonElement root2)
        {
            // Start writing the merged array
            jsonWriter.WriteStartArray();

            // Merge elements of the first array
            for (int i = 0; i < root1.GetArrayLength(); i++)
            {
                root1[i].WriteTo(jsonWriter);
            }

            // Merge elements of the second array
            for (int i = 0; i < root2.GetArrayLength(); i++)
            {
                root2[i].WriteTo(jsonWriter);
            }

            // End writing the merged array
            jsonWriter.WriteEndArray();
        }
#else
        internal static string JsonObjectToString(JObject jsonObject) => jsonObject.ToString(Formatting.None);

        internal static JObject ParseIntoJsonObject(string json) => JObject.Parse(json);

        internal static JObject ToJsonObject(JToken jsonNode) => (JObject)jsonNode;

        internal static bool TryGetValue(JObject json, string propertyName, out JToken value) => json.TryGetValue(propertyName, out value);

        internal static T GetValue<T>(JToken json) => json.Value<T>();
#endif
    }

}
