// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.OAuth2;
#if NET6_0_OR_GREATER
using System.Text.Json.Serialization;
using JsonProperty = System.Text.Json.Serialization.JsonPropertyNameAttribute;
#else
using Microsoft.Identity.Json;
#endif

namespace Microsoft.Identity.Client.Internal
{
#if !NET6_0_OR_GREATER
    [JsonObject]
#endif
    [Preserve(AllMembers = true)]
    internal class DeviceCodeResponse : OAuth2ResponseBase
    {
        [JsonProperty("user_code")]
#if NET6_0_OR_GREATER
        [JsonInclude]
#endif
        public string UserCode { get; internal set; }

        [JsonProperty("device_code")]
#if NET6_0_OR_GREATER
        [JsonInclude]
#endif
        public string DeviceCode { get; internal set; }

        [JsonProperty("verification_url")]
#if NET6_0_OR_GREATER
        [JsonInclude]
#endif
        public string VerificationUrl { get; internal set; }

        // This is the OAuth2 standards compliant value.
        // It should be used if it's present, if it's not then fallback to VerificationUrl
        [JsonProperty("verification_uri")]
#if NET6_0_OR_GREATER
        [JsonInclude]
#endif
        public string VerificationUri { get; internal set; }

        [JsonProperty("expires_in")]
#if NET6_0_OR_GREATER
        [JsonInclude]
#endif
        public long ExpiresIn { get; internal set; }

        [JsonProperty("interval")]
#if NET6_0_OR_GREATER
        [JsonInclude]
#endif
        public long Interval { get; internal set; }

        [JsonProperty("message")]
#if NET6_0_OR_GREATER
        [JsonInclude]
#endif
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
