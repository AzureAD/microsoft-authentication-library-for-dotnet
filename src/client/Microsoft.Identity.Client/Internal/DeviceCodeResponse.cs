// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.OAuth2;
using System.Text.Json.Serialization;
using Microsoft.Identity.Client.Platforms.Json;
using JsonProperty = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Microsoft.Identity.Client.Internal
{
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
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long ExpiresIn { get; set; }

        [JsonProperty("interval")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
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
