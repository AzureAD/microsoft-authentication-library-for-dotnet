// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Platforms.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Microsoft.Identity.Client.Utils
{
    internal static class JsonHelper
    {
        internal static string SerializeToJson<T>(T toEncode)
        {
            return JsonSerializer.Serialize(toEncode, typeof(T), MsalJsonSerializerContext.Custom);
        }

        internal static T DeserializeFromJson<T>(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return default;
            }
            return (T)JsonSerializer.Deserialize(json, typeof(T), MsalJsonSerializerContext.Custom);
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
            return (T)JsonSerializer.Deserialize(stream, typeof(T), MsalJsonSerializerContext.Custom);
        }

        internal static string GetExistingOrEmptyString(JsonObject json, string key)
        {
            if (TryGetValue(json, key, out var val))
            {
                return GetValue<string>(val);
            }

            return string.Empty;
        }

        internal static string ExtractExistingOrEmptyString(JsonObject json, string key)
        {
            if (TryGetValue(json, key, out var val))
            {
                string strVal = GetValue<string>(val);
                json.Remove(key);
                return strVal;
            }

            return string.Empty;
        }

        internal static IDictionary<string, string> ExtractInnerJsonAsDictionary(JsonObject json, string key)
        {
            if (TryGetValue(json, key, out JsonNode val))
            {
                IDictionary<string, JsonNode> valueAsDict = ToJsonObject(val);
                Dictionary<string, string> dictionary =
                    valueAsDict.ToDictionary(pair => pair.Key, pair => (string)pair.Value);

                json.Remove(key);
                return dictionary;
            }

            return null;
        }

        internal static T ExtractExistingOrDefault<T>(JsonObject json, string key)
        {
            if (TryGetValue(json, key, out var val))
            {
                T obj = GetValue<T>(val);
                json.Remove(key);
                return obj;
            }

            return default;
        }

        internal static long ExtractParsedIntOrZero(JsonObject json, string key)
        {
            string strVal = ExtractExistingOrEmptyString(json, key);
            if (!string.IsNullOrWhiteSpace(strVal) && long.TryParse(strVal, out long result))
            {
                return result;
            }

            return 0;
        }

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
        /// <returns>A JsonObject representing the merged JSON.</returns>
        /// <remarks>
        /// This method parses the original and new JSON objects, merges their elements, and returns
        /// a JsonObject representing the merged JSON.
        /// Original Code Reference: https://github.com/dotnet/runtime/issues/31433
        /// </remarks>
        internal static JsonObject Merge(JsonObject originalJson, JsonObject newContent)
        {
            using var memoryStream = new MemoryStream();
            using (var jsonWriter = new Utf8JsonWriter(memoryStream, new JsonWriterOptions { Indented = true }))
            {
                using (JsonDocument jDoc1 = JsonDocument.Parse(originalJson.ToJsonString()))
                using (JsonDocument jDoc2 = JsonDocument.Parse(newContent.ToJsonString()))
                {
                    MergeJsonElements(jsonWriter, jDoc1.RootElement, jDoc2.RootElement);
                }
            }

            memoryStream.Position = 0;
            using var reader = new StreamReader(memoryStream);
            string mergedJsonString = reader.ReadToEnd();

            return JsonNode.Parse(mergedJsonString).AsObject();
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
    }
}
