// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using Microsoft.Identity.Client.Utils;
#if SUPPORTS_SYSTEM_TEXT_JSON
using Microsoft.Identity.Client.Platforms.net6;
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
                return JsonHelper.DeserializeFromJson<ClientInfo>(Base64UrlHelpers.DecodeBytes(clientInfo));
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
