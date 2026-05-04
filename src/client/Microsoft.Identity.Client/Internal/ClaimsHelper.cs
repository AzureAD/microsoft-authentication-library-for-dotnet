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

        internal static string GetMergedClaimsAndClientCapabilities(
            string claims,
            IEnumerable<string> clientCapabilities)
        {
            return GetMergedClaimsAndClientCapabilities(claims, clientCapabilities, clientClaims: null);
        }

        /// <summary>
        /// Merges server claims (from WithClaims), client-originated claims (from WithClaimsFromClient),
        /// and client capabilities into a single OIDC-compliant claims JSON string.
        /// </summary>
        internal static string GetMergedClaimsAndClientCapabilities(
            string claims,
            IEnumerable<string> clientCapabilities,
            string clientClaims)
        {
            if (clientCapabilities != null && clientCapabilities.Any())
            {
                JObject capabilitiesJson = CreateClientCapabilitiesRequestJson(clientCapabilities);
                // Merge client claims first, then server claims — server claims take precedence on conflicts
                capabilitiesJson = MergeClaimsIntoCapabilityJson(clientClaims, capabilitiesJson);
                capabilitiesJson = MergeClaimsIntoCapabilityJson(claims, capabilitiesJson);

                return JsonHelper.JsonObjectToString(capabilitiesJson);
            }

            // No capabilities — merge client claims first, server claims second (server wins on conflicts)
            if (!string.IsNullOrEmpty(claims) && !string.IsNullOrEmpty(clientClaims))
            {
                JObject clientClaimsJson = ParseClaimsJson(clientClaims);
                JObject serverClaimsJson = ParseClaimsJson(claims);
                JObject merged = JsonHelper.Merge(clientClaimsJson, serverClaimsJson);
                return JsonHelper.JsonObjectToString(merged);
            }

            return !string.IsNullOrEmpty(clientClaims) ? clientClaims : claims;
        }

        internal static JObject MergeClaimsIntoCapabilityJson(string claims, JObject capabilitiesJson)
        {
            if (!string.IsNullOrEmpty(claims))
            {
                JObject claimsJson = ParseClaimsJson(claims);
                capabilitiesJson = JsonHelper.Merge(capabilitiesJson, claimsJson);
            }

            return capabilitiesJson;
        }

        private static JObject ParseClaimsJson(string claims)
        {
            try
            {
                var parsed = JsonHelper.ParseIntoJsonObject(claims);
                if (parsed is null)
                {
                    throw new MsalClientException(
                        MsalError.InvalidJsonClaimsFormat,
                        "The claims parameter must be a valid JSON object. See https://openid.net/specs/openid-connect-core-1_0.html#ClaimsParameter.");
                }
                return parsed;
            }
            catch (MsalClientException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new MsalClientException(
                    MsalError.InvalidJsonClaimsFormat,
                    "The claims parameter is not valid JSON. Inspect the inner exception for details.",
                    ex);
            }
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
