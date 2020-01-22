// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Json;
#if iOS
using Foundation;
#endif
#if ANDROID
using Android.Runtime;
#endif

namespace Microsoft.Identity.Client.Core
{
    [JsonObject]
#if ANDROID || iOS
    [Preserve(AllMembers = true)]
#endif
    internal class ClientInfo
    {
        [JsonProperty(PropertyName = ClientInfoClaim.UniqueIdentifier )]
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

        public string ToAccountIdentifier()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}.{1}", UniqueObjectIdentifier, UniqueTenantIdentifier);
        }
    }
}
