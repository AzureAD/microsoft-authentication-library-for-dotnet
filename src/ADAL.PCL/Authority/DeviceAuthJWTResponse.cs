//----------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
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
            return String.Format(CultureInfo.CurrentCulture, "{0}.{1}", 
                Base64UrlEncoder.Encode(JsonHelper.EncodeToJson(header).ToByteArray()), 
                Base64UrlEncoder.Encode(JsonHelper.EncodeToJson(payload).ToByteArray()));
        }
    }
}
