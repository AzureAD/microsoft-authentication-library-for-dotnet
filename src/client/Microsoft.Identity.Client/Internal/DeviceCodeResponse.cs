// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client.Internal
{
    [Preserve(AllMembers = true)]
    internal class DeviceCodeResponse : OAuth2ResponseBase
    {
        [JsonPropertyName("user_code")]
        [JsonInclude]
        public string UserCode { get; internal set; }

        [JsonPropertyName("device_code")]
        [JsonInclude]
        public string DeviceCode { get; internal set; }

        [JsonPropertyName("verification_url")]
        [JsonInclude]
        public string VerificationUrl { get; internal set; }

        // This is the OAuth2 standards compliant value.
        // It should be used if it's present, if it's not then fallback to VerificationUrl
        [JsonPropertyName("verification_uri")]
        [JsonInclude]
        public string VerificationUri { get; internal set; }

        [JsonPropertyName("expires_in")]
        [JsonInclude]
        public long ExpiresIn { get; internal set; }

        [JsonPropertyName("interval")]
        [JsonInclude]
        public long Interval { get; internal set; }

        [JsonPropertyName("message")]
        [JsonInclude]
        public string Message { get; internal set; }

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
