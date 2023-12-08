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
    }
}
