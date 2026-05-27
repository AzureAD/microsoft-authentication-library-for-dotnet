// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
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
                // InvalidOperationException is thrown by JsonNode.AsObject() when the root token is
                // valid JSON but not an object (e.g. an array, a scalar, or the literal 'null').
                // Do not include the raw claimsJson in the message — it may contain sensitive data.
                throw new MsalClientException(
                    MsalError.InvalidJsonClaimsFormat,
                    "The claims value is not a valid JSON object. Inspect the inner exception for parsing details. " +
                    "See https://openid.net/specs/openid-connect-core-1_0.html#ClaimsParameter.",
                    ex);
            }
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
                catch (Exception ex) when (ex is JsonException || ex is InvalidOperationException)
                {
                    // InvalidOperationException is thrown by JsonNode.AsObject() when the root token is
                    // valid JSON but not an object (e.g. an array, a scalar, or the literal 'null').
                    // This method also handles server-issued claims from .WithClaims(), so use a neutral
                    // message rather than naming client_claims specifically.
                    throw new MsalClientException(
                        MsalError.InvalidJsonClaimsFormat,
                        "The claims value is not a valid JSON object. Inspect the inner exception for parsing details. " +
                        "See https://openid.net/specs/openid-connect-core-1_0.html#ClaimsParameter.",
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
