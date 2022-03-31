// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Json;

namespace Microsoft.Identity.Client.OAuth2
{
    [JsonObject]
    [Preserve(AllMembers = true)]
    internal class DeviceAuthHeader
    {
        public DeviceAuthHeader(string base64EncodedCertificate)
        {
            Alg = "RS256";
            Type = "JWT";
            X5c = new List<string>();
            X5c.Add(base64EncodedCertificate);
        }

        [JsonProperty("x5c")]
        public IList<string> X5c { get; set; }

        [JsonProperty("typ")]
        public string Type { get; set; }

        [JsonProperty("alg")]
        public string Alg { get; private set; }
    }

    [JsonObject]
    [Preserve(AllMembers = true)]
    internal class DeviceAuthPayload
    {
        private Lazy<long> _defaultDeviceAuthJWTTimeSpan = new Lazy<long>(() => (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds);
        
        public DeviceAuthPayload(string audience, string nonce)
        {
            Nonce = nonce;
            Audience = audience;
            Iat = _defaultDeviceAuthJWTTimeSpan.Value;
        }

        [JsonProperty("iat")]
        public long Iat { get; set; }

        [JsonProperty("aud")]
        public string Audience { get; set; }

        [JsonProperty("nonce")]
        public string Nonce { get; private set; }
    }

    internal class DeviceAuthJWTResponse
    {
        private readonly DeviceAuthHeader _header;
        private readonly DeviceAuthPayload _payload;

        public DeviceAuthJWTResponse(string audience, string nonce,
            string base64EncodedCertificate)
        {
            _header = new DeviceAuthHeader(base64EncodedCertificate);
            _payload = new DeviceAuthPayload(audience, nonce);
        }

        public string GetResponseToSign()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}.{1}",
                Base64UrlHelpers.Encode(JsonHelper.SerializeToJson(_header).ToByteArray()),
                Base64UrlHelpers.Encode(JsonHelper.SerializeToJson(_payload).ToByteArray()));
        }
    }
}
