// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Json;
#if iOS
using Foundation;
#endif
#if ANDROID
using Android.Runtime;
#endif

namespace Microsoft.Identity.Client.Internal
{
    [JsonObject]
    [Preserve]
    internal class DeviceCodeResponse : OAuth2ResponseBase
    {
        [JsonProperty(PropertyName = "user_code")]
        public string UserCode { get; internal set; }

        [JsonProperty(PropertyName = "device_code")]
        public string DeviceCode { get; internal set; }

        [JsonProperty(PropertyName = "verification_url")]
        public string VerificationUrl { get; internal set; }

        // This is the OAuth2 standards compliant value.
        // It should be used if it's present, if it's not then fallback to VerificiationUrl
        [JsonProperty(PropertyName = "verification_uri")]
        public string VerificationUri { get; internal set; }

        [JsonProperty(PropertyName = "expires_in")]
        public long ExpiresIn { get; internal set; }

        [JsonProperty(PropertyName = "interval")]
        public long Interval { get; internal set; }

        [JsonProperty(PropertyName = "message")]
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
