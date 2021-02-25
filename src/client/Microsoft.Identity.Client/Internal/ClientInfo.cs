// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Json;
using Microsoft.Identity.Json.Linq;

namespace Microsoft.Identity.Client.Internal
{
    [JsonObject]
    [Preserve(AllMembers = true)]
    internal class ClientInfo : IJsonSerializable<ClientInfo>
    {
        [JsonProperty(PropertyName = ClientInfoClaim.UniqueIdentifier)]
        public string UniqueObjectIdentifier { get; set; }

        [JsonProperty(PropertyName = ClientInfoClaim.UniqueTenantIdentifier)]
        public string UniqueTenantIdentifier { get; set; }

        public ClientInfo DeserializeFromJson(string json) => DeserializeFromJObject(JObject.Parse(json));

        public ClientInfo DeserializeFromJObject(JObject jObject)
        {
            UniqueObjectIdentifier = jObject[ClientInfoClaim.UniqueIdentifier]?.ToString();
            UniqueTenantIdentifier = jObject[ClientInfoClaim.UniqueTenantIdentifier]?.ToString();

            return this;
        }

        public string SerializeToJson() => SerializeToJObject().ToString(Formatting.None);

        public JObject SerializeToJObject()
        {
            return new JObject(
                new JProperty(ClientInfoClaim.UniqueIdentifier, UniqueObjectIdentifier),
                new JProperty(ClientInfoClaim.UniqueTenantIdentifier, UniqueTenantIdentifier));
        }

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
                return JsonHelper.DeserializeNew<ClientInfo>(Base64UrlHelpers.DecodeToString(clientInfo));
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
