// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.OAuth2
{
    [DataContract]
    internal class DeviceAuthHeader
    {
        public DeviceAuthHeader(string base64EncodedCertificate)
        {
            this.Alg = "RS256";
            this.Type = "JWT";
            this.X5c = new List<string>();
            this.X5c.Add(base64EncodedCertificate);
        }

        [DataMember(Name = "x5c", IsRequired = true)]
        public List<string> X5c { get; set; }

        [DataMember(Name = "typ", IsRequired = true)]
        public string Type { get; set; }

        [DataMember(Name = "alg", IsRequired = true)]
        public string Alg { get; private set; }
    }

    [DataContract]
    internal class DeviceAuthPayload
    {
        public DeviceAuthPayload(string audience, string nonce)
        {
            this.Nonce = nonce;
            this.Audience = audience;
            var timeSpan = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0));
            this.Iat = (long)timeSpan.TotalSeconds;
        }

        [DataMember(Name = "iat", IsRequired = true)]
        public long Iat { get; set; }

        [DataMember(Name = "aud", IsRequired = true)]
        public string Audience { get; set; }

        [DataMember(Name = "nonce", IsRequired = true)]
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
