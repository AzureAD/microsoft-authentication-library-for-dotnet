// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Identity.Client.Utils;
#if SUPPORTS_SYSTEM_TEXT_JSON
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using JObject = System.Text.Json.Nodes.JsonObject;
using System.Buffers;
using System.Diagnostics;
#else
using Microsoft.Identity.Json;
using Microsoft.Identity.Json.Linq;
#endif

namespace Microsoft.Identity.Client.Internal
{
    internal static class ClaimsHelper
    {
        private const string AccessTokenClaim = "access_token";
        private const string XmsClientCapability = "xms_cc";

        internal static string GetMergedClaimsAndClientCapabilities(
            string claims,
            IEnumerable<string> clientCapabilities)
        {
            if (clientCapabilities != null && clientCapabilities.Any())
            {
                JObject capabilitiesJson = CreateClientCapabilitiesRequestJson(clientCapabilities);
                JObject mergedClaimsAndCapabilities = MergeClaimsIntoCapabilityJson(claims, capabilitiesJson);

                return JsonHelper.JsonObjectToString(mergedClaimsAndCapabilities);
            }

            return claims;
        }

        internal static JObject MergeClaimsIntoCapabilityJson(string claims, JObject capabilitiesJson)
        {
            if (!string.IsNullOrEmpty(claims))
            {
                JObject claimsJson;
                try
                {
                    claimsJson = JsonHelper.ParseIntoJsonObject(claims);
                }
                catch (JsonException ex)
                {
                    throw new MsalClientException(
                        MsalError.InvalidJsonClaimsFormat,
                        MsalErrorMessage.InvalidJsonClaimsFormat(claims),
                        ex);
                }
#if SUPPORTS_SYSTEM_TEXT_JSON
                capabilitiesJson = JsonHelper.Merge(capabilitiesJson, claimsJson);
#else
                capabilitiesJson.Merge(claimsJson, new JsonMergeSettings
                {
                    // union array values together to avoid duplicates
                    MergeArrayHandling = MergeArrayHandling.Union
                });
#endif
            }

            return capabilitiesJson;
        }

        private static JObject CreateClientCapabilitiesRequestJson(IEnumerable<string> clientCapabilities)
        {
            // "access_token": {
            //     "xms_cc": { 
            //         values: ["cp1", "cp2"]
            //     }
            //  }
            return new JObject
            {
                [AccessTokenClaim] = new JObject
                {
                    [XmsClientCapability] = new JObject
                    {
#if SUPPORTS_SYSTEM_TEXT_JSON
                        ["values"] = new JsonArray(clientCapabilities.Select(c => JsonValue.Create(c)).ToArray())
#else
                        ["values"] = new JArray(clientCapabilities)
#endif
                    }
                }
            };
        }

#if SUPPORTS_SYSTEM_TEXT_JSON
        public static string Merge(string originalJson, string newContent)
        {
            var outputBuffer = new ArrayBufferWriter<byte>();

            using (JsonDocument jDoc1 = JsonDocument.Parse(originalJson))
            using (JsonDocument jDoc2 = JsonDocument.Parse(newContent))
            using (var jsonWriter = new Utf8JsonWriter(outputBuffer, new JsonWriterOptions { Indented = true }))
            {
                MergeElements(jsonWriter, jDoc1.RootElement, jDoc2.RootElement);
            }

            return Encoding.UTF8.GetString(outputBuffer.WrittenSpan);
        }

        private static void MergeElements(Utf8JsonWriter jsonWriter, JsonElement root1, JsonElement root2)
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
                    root1.WriteTo(jsonWriter);
                    break;
            }
        }

        private static void MergeObjects(Utf8JsonWriter jsonWriter, JsonElement root1, JsonElement root2)
        {
            jsonWriter.WriteStartObject();

            foreach (JsonProperty property in root1.EnumerateObject())
            {
                string propertyName = property.Name;

                JsonValueKind newValueKind;

                if (root2.TryGetProperty(propertyName, out JsonElement newValue) && (newValueKind = newValue.ValueKind) != JsonValueKind.Null)
                {
                    jsonWriter.WritePropertyName(propertyName);

                    JsonElement originalValue = property.Value;
                    JsonValueKind originalValueKind = originalValue.ValueKind;

                    if ((newValueKind == JsonValueKind.Object && originalValueKind == JsonValueKind.Object) ||
                        (newValueKind == JsonValueKind.Array && originalValueKind == JsonValueKind.Array))
                    {
                        MergeElements(jsonWriter, originalValue, newValue);
                    }
                    else
                    {
                        newValue.WriteTo(jsonWriter);
                    }
                }
                else
                {
                    property.WriteTo(jsonWriter);
                }
            }

            foreach (JsonProperty property in root2.EnumerateObject())
            {
                if (!root1.TryGetProperty(property.Name, out _))
                {
                    property.WriteTo(jsonWriter);
                }
            }

            jsonWriter.WriteEndObject();
        }

        private static void MergeArrays(Utf8JsonWriter jsonWriter, JsonElement root1, JsonElement root2)
        {
            jsonWriter.WriteStartArray();

            foreach (JsonElement element in root1.EnumerateArray())
            {
                element.WriteTo(jsonWriter);
            }
            foreach (JsonElement element in root2.EnumerateArray())
            {
                element.WriteTo(jsonWriter);
            }

            jsonWriter.WriteEndArray();
        }

#endif
    }
}
