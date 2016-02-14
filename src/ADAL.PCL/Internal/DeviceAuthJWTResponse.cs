//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory.Internal
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

        [DataMember(Name = "type", IsRequired = true)]
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
        private DeviceAuthHeader header;
        private DeviceAuthPayload payload;

        public DeviceAuthJWTResponse(string audience, string nonce,
            string base64EncodedCertificate)
        {
            this.header = new DeviceAuthHeader(base64EncodedCertificate);
            this.payload = new DeviceAuthPayload(audience, nonce);
        }

        public string GetResponseToSign()
        {
            return String.Format("{0}.{1}", 
                Base64UrlEncoder.Encode(JsonHelper.EncodeToJson(header).ToByteArray()), 
                Base64UrlEncoder.Encode(JsonHelper.EncodeToJson(payload).ToByteArray()));
        }
    }
}
