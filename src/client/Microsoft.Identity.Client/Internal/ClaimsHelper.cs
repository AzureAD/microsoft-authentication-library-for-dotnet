// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
                try
                {
                    JObject capabilitiesJson = CreateClientCapabilitiesRequestJson(clientCapabilities);
                    JObject mergedClaimsAndCapabilities = MergeClaimsIntoCapabilityJson(claims, capabilitiesJson);

                    return JsonHelper.JsonObjectToString(mergedClaimsAndCapabilities);
                }
                catch (PlatformNotSupportedException pns)
                {
                    throw CreateJsonEncoderException(pns);
                }
                catch (TypeInitializationException tie) when (tie.InnerException is PlatformNotSupportedException)
                {
                    throw CreateJsonEncoderException(tie.InnerException as PlatformNotSupportedException);
                }
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

        private static MsalClientException CreateJsonEncoderException(PlatformNotSupportedException innerException)
        {
            string processArchitecture = RuntimeInformation.ProcessArchitecture.ToString();
            bool is64BitProcess = Environment.Is64BitProcess;
            string hwIntrinsicEnvValue = Environment.GetEnvironmentVariable("DOTNET_EnableHWIntrinsic") 
                                         ?? Environment.GetEnvironmentVariable("COMPlus_EnableHWIntrinsic");

            return new MsalClientException(
                MsalError.JsonEncoderIntrinsicsUnsupported,
                MsalErrorMessage.JsonEncoderIntrinsicsUnsupported(processArchitecture, is64BitProcess, hwIntrinsicEnvValue),
                innerException);
        }
    }
}
