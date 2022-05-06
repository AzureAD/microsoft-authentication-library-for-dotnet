// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Json;

namespace Microsoft.Identity.Client.Internal
{

    [JsonObject]
    [Preserve(AllMembers = true)]
    internal class ClientInfo
    {
        [JsonProperty(PropertyName = ClientInfoClaim.UniqueIdentifier)]
        public string UniqueObjectIdentifier { get; set; }

        [JsonProperty(PropertyName = ClientInfoClaim.UniqueTenantIdentifier)]
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
            return string.Format(CultureInfo.InvariantCulture, "{0}.{1}", UniqueObjectIdentifier, UniqueTenantIdentifier);
        }
    }
}
