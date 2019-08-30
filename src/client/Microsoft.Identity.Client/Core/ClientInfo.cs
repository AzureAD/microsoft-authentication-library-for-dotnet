// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Runtime.Serialization;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Core
{
    [DataContract]
    internal class ClientInfo
    {
        [DataMember(Name = ClientInfoClaim.UniqueIdentifier, IsRequired = false)]
        public string UniqueObjectIdentifier { get; set; }

        [DataMember(Name = ClientInfoClaim.UniqueTenantIdentifier, IsRequired = false)]
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
                return JsonHelper.DeserializeFromJson<ClientInfo>(Base64UrlHelpers.DecodeToBytes(clientInfo));
            }
            catch (Exception exc)
            {
                throw new MsalClientException(
                     MsalError.JsonParseError,
                     "Failed to parse the returned client info.",
                     exc);
            }
        }

        public string ToEncodedJson()
        {
            return Base64UrlHelpers.Encode(JsonHelper.SerializeToJson<ClientInfo>(this));
        }

        public string ToAccountIdentifier()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}.{1}", UniqueObjectIdentifier, UniqueTenantIdentifier);
        }
    }
}
