//------------------------------------------------------------------------------
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
using System.Runtime.Serialization;
using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client.Internal
{
    [DataContract]
    internal class DeviceCodeResponse : OAuth2ResponseBase
    {
        [DataMember(Name = "user_code", IsRequired = false)]
        public string UserCode { get; internal set; }

        [DataMember(Name = "device_code", IsRequired = false)]
        public string DeviceCode { get; internal set; }

        [DataMember(Name = "verification_url", IsRequired = false)]
        public string VerificationUrl { get; internal set; }

        // This is the OAuth2 standards compliant value.
        // It should be used if it's present, if it's not then fallback to VerificiationUrl
        [DataMember(Name = "verification_uri", IsRequired = false)]
        public string VerificationUri { get; internal set; }

        [DataMember(Name = "expires_in", IsRequired = false)]
        public long ExpiresIn { get; internal set; }

        [DataMember(Name = "interval", IsRequired = false)]
        public long Interval { get; internal set; }

        [DataMember(Name = "message", IsRequired = false)]
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
