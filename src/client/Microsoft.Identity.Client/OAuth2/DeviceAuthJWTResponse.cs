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
    internal class DeviceAuthHeader
    {
        public DeviceAuthHeader(string base64EncodedCertificate)
        {
            this.Alg = "RS256";
            this.Type = "JWT";
            this.X5c = new List<string>();
            this.X5c.Add(base64EncodedCertificate);
        }

        [JsonProperty("x5c")]
        public IList<string> X5c { get; set; }

        [JsonProperty("typ")]
        public string Type { get; set; }

        [JsonProperty("alg")]
        public string Alg { get; private set; }
    }

    [JsonObject]
    internal class DeviceAuthPayload
    {
        private Lazy<long> _defaultDeviceAuthJWTTimeSpan = new Lazy<long>(() => (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds);
        
        public DeviceAuthPayload(string audience, string nonce)
        {
            this.Nonce = nonce;
            this.Audience = audience;
            this.Iat = _defaultDeviceAuthJWTTimeSpan.Value;
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
        private readonly DeviceAuthHeader header;
        private readonly DeviceAuthPayload payload;

        public DeviceAuthJWTResponse(string audience, string nonce,
            string base64EncodedCertificate)
        {
            this.header = new DeviceAuthHeader(base64EncodedCertificate);
            this.payload = new DeviceAuthPayload(audience, nonce);
        }

        public string GetResponseToSign()
        {
            return String.Format(CultureInfo.InvariantCulture, "{0}.{1}",
                Base64UrlHelpers.Encode(JsonHelper.SerializeToJson(header).ToByteArray()),
                Base64UrlHelpers.Encode(JsonHelper.SerializeToJson(payload).ToByteArray()));
        }
    }
}
