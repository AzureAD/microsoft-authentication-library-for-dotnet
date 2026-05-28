// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Identity.Client.Utils;
using JObject = System.Text.Json.Nodes.JsonObject;

namespace Microsoft.Identity.Client.Internal
{
    internal static class ClaimsHelper
    {
        private const string AccessTokenClaim = "access_token";
        private const string XmsClientCapability = "xms_cc";

        /// <summary>
        /// Normalizes a claims JSON string so that semantically identical claims always produce
        /// the same string. This prevents cache key fragmentation when callers pass the same
        /// logical claims in different whitespace or key-ordering variants.
        /// </summary>
        internal static string NormalizeClaimsJson(string claimsJson)
        {
            if (string.IsNullOrWhiteSpace(claimsJson))
            {
                return claimsJson;
            }

            try
            {
                JObject parsed = JsonHelper.ParseIntoJsonObject(claimsJson);
                JObject sorted = SortJsonObjectKeys(parsed);
                return JsonHelper.JsonObjectToString(sorted);
            }
            catch (Exception ex) when (ex is JsonException || ex is InvalidOperationException)
            {
                // InvalidOperationException is thrown by JsonNode.AsObject() when the root token is
                // valid JSON but not an object (e.g. an array or a scalar).
                // Do not include the raw claimsJson in the message — it may contain sensitive data.
                throw new MsalClientException(
                    MsalError.InvalidJsonClaimsFormat,
                    "The client_claims value is not valid JSON. Inspect the inner exception for parsing details. " +
                    "See https://openid.net/specs/openid-connect-core-1_0.html#ClaimsParameter.",
                    ex);
            }
        }

        /// <summary>
        /// Merges two JSON claims objects. If either is null/empty the other is returned as-is.
        /// </summary>
        internal static string MergeClaimsObjects(string claims1, string claims2)
        {
            if (string.IsNullOrEmpty(claims1)) return claims2;
            if (string.IsNullOrEmpty(claims2)) return claims1;

            try
            {
                JObject obj1 = JsonHelper.ParseIntoJsonObject(claims1);
                JObject obj2 = JsonHelper.ParseIntoJsonObject(claims2);
                JObject merged = JsonHelper.Merge(obj1, obj2);
                return JsonHelper.JsonObjectToString(merged);
            }
            catch (Exception ex) when (ex is JsonException || ex is InvalidOperationException)
            {
                // InvalidOperationException is thrown by JsonNode.AsObject() when a value is
                // valid JSON but not an object (e.g. an array or a scalar).
                // Do not include the raw claims payload in the message — it may contain
                // sensitive data; details remain available on the inner exception.
                throw new MsalClientException(
                    MsalError.InvalidJsonClaimsFormat,
                    "The client_claims value is not valid JSON. Inspect the inner exception for parsing details. " +
                    "See https://openid.net/specs/openid-connect-core-1_0.html#ClaimsParameter.",
                    ex);
            }
        }

        private static JObject SortJsonObjectKeys(JObject obj)
        {
            var sorted = new JObject();
            foreach (var key in obj.Select(kvp => kvp.Key).OrderBy(k => k, StringComparer.Ordinal))
            {
                sorted[key] = CloneSorted(obj[key]);
            }
            return sorted;
        }

        // Recursively clones a JsonNode, sorting keys inside any nested JsonObject (including
        // those contained inside JsonArrays). Array element order is preserved — only object
        // key order is normalized. This is required so semantically identical claims such as
        // {"x":[{"a":1,"b":2}]} and {"x":[{"b":2,"a":1}]} produce the same normalized string,
        // and therefore the same cache key.
        private static JsonNode CloneSorted(JsonNode value)
        {
            if (value is null)
            {
                return null;
            }

            if (value is JObject nestedObj)
            {
                return SortJsonObjectKeys(nestedObj);
            }

            if (value is JsonArray array)
            {
                var newArray = new JsonArray();
                foreach (var element in array)
                {
                    newArray.Add(CloneSorted(element));
                }
                return newArray;
            }

            // Scalar (JsonValue) — re-parse to detach from the original tree.
            // JsonNode.DeepClone is .NET 6+; use Parse(ToJsonString()) for portability.
            return JsonNode.Parse(value.ToJsonString());
        }

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
                capabilitiesJson = JsonHelper.Merge(capabilitiesJson, claimsJson);
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
                        ["values"] = new JsonArray(clientCapabilities.Select(c => JsonValue.Create(c)).ToArray())
                    }
                }
            };
        }
    }
}
