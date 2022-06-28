// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

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
                var capabilitiesJson = CreateClientCapabilitiesRequestJson(clientCapabilities);
                var mergedClaimsAndCapabilities = MergeClaimsIntoCapabilityJson(claims, capabilitiesJson);

                return mergedClaimsAndCapabilities.ToJsonString();
            }

            return claims;
        }

        internal static JsonObject MergeClaimsIntoCapabilityJson(string claims, JsonObject capabilitiesJson)
        {
            if (!string.IsNullOrEmpty(claims))
            {
                JsonObject claimsJson;
                try
                {
                    claimsJson = JsonNode.Parse(claims).AsObject();
                }
                catch (JsonException ex)
                {
                    throw new MsalClientException(
                        MsalError.InvalidJsonClaimsFormat,
                        MsalErrorMessage.InvalidJsonClaimsFormat(claims),
                        ex);
                }

                foreach (var claim in claimsJson)
                {
                    capabilitiesJson[claim.Key] = claim.Value != null ? JsonNode.Parse(claim.Value.ToJsonString()) : null;
                }
            }

            return capabilitiesJson;
        }

        private static JsonObject CreateClientCapabilitiesRequestJson(IEnumerable<string> clientCapabilities)
        {
            // "access_token": {
            //     "xms_cc": { 
            //         values: ["cp1", "cp2"]
            //     }
            //  }
            return new JsonObject
            {
                [AccessTokenClaim] = new JsonObject
                {
                    [XmsClientCapability] = new JsonObject
                    {
                        ["values"] = new JsonArray(clientCapabilities.Select(c => JsonValue.Create(c)).ToArray())
                    }
                }
            };
        }
    }
}
