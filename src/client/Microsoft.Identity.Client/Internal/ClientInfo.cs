// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Identity.Client.Utils;
#if SUPPORTS_SYSTEM_TEXT_JSON
using Microsoft.Identity.Client.Platforms.net;
using JsonProperty = System.Text.Json.Serialization.JsonPropertyNameAttribute;
#else
using Microsoft.Identity.Json;
#endif

namespace Microsoft.Identity.Client.Internal
{
    [JsonObject]
    [Preserve(AllMembers = true)]
    internal class ClientInfo
    {
        [JsonProperty(ClientInfoClaim.UniqueIdentifier)]
        public string UniqueObjectIdentifier { get; set; }

        [JsonProperty(ClientInfoClaim.UniqueTenantIdentifier)]
        public string UniqueTenantIdentifier { get; set; }

        public Dictionary<string, string> AdditionalResponseParameters { get; private set; }

        public static ClientInfo CreateFromJson(string clientInfo)
        {
            if (string.IsNullOrEmpty(clientInfo))
            {
                throw new MsalClientException(
                    MsalError.JsonParseError,
                    "client info is null");
            }

            try
            {
                var decodedBytes = Base64UrlHelpers.DecodeBytes(clientInfo);
                
                // Deserialize into a dictionary to get all properties
                var allProperties = JsonHelper.DeserializeFromJson<Dictionary<string, object>>(decodedBytes);
                
                var clientInfoObj = new ClientInfo();
                var additionalParams = new Dictionary<string, string>();
                
                // Extract known claims and store the rest in AdditionalResponseParameters
                foreach (var kvp in allProperties)
                {
                    if (kvp.Key == ClientInfoClaim.UniqueIdentifier)
                    {
                        clientInfoObj.UniqueObjectIdentifier = kvp.Value?.ToString();

                    }
                    else if (kvp.Key == ClientInfoClaim.UniqueTenantIdentifier)
                    {
                        clientInfoObj.UniqueTenantIdentifier = kvp.Value?.ToString();
                    }
                    else
                    {
                        additionalParams[kvp.Key] = kvp.Value?.ToString() ?? string.Empty;
                    }
                }
                
                clientInfoObj.AdditionalResponseParameters = additionalParams;
                return clientInfoObj;
            }
            catch (Exception exc)
            {
                throw new MsalClientException(
                     MsalError.JsonParseError,
                     "Failed to parse the returned client info.",
                     exc);
            }
        }

        public string ToAccountIdentifier()
        {
            return $"{UniqueObjectIdentifier}.{UniqueTenantIdentifier}";
        }
    }
}
