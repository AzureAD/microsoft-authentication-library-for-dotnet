// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.OAuth2;
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
    internal class DeviceCodeResponse : OAuth2ResponseBase
    {
        [JsonProperty("user_code")]
        public string UserCode { get; set; }

        [JsonProperty("device_code")]
        public string DeviceCode { get; set; }

        [JsonProperty("verification_url")]
        public string VerificationUrl { get; set; }

        // This is the OAuth2 standards compliant value.
        // It should be used if it's present, if it's not then fallback to VerificationUrl
        [JsonProperty("verification_uri")]
        public string VerificationUri { get; set; }

        [JsonProperty("expires_in")]
        public long ExpiresIn { get; set; }

        [JsonProperty("interval")]
        public long Interval { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        public DeviceCodeResult GetResult(string clientId, ISet<string> scopes)
        {
            // VerificationUri should be used if it's present, and if not fall back to VerificationUrl
            string verification = string.IsNullOrWhiteSpace(VerificationUri) ? VerificationUrl : VerificationUri;

            return new DeviceCodeResult(
                UserCode,
                DeviceCode,
                verification,
                DateTime.UtcNow.AddSeconds(ExpiresIn),
                Interval,
                Message,
                clientId,
                scopes);
        }
    }
}
