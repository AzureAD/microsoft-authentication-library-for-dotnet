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

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    [DataContract]
    internal class DeviceCodeResponse
    {

        [DataMember(Name = "user_code", IsRequired = false)]
        public string UserCode { get; internal set; }

        [DataMember(Name = "device_code", IsRequired = false)]
        public string DeviceCode { get; internal set; }

        [DataMember(Name = "verification_url", IsRequired = false)]
        public string VerificationUrl { get; internal set; }
        
        [DataMember(Name = "expires_in", IsRequired = false)]
        public long ExpiresIn { get; internal set; }

        [DataMember(Name = "interval", IsRequired = false)]
        public long Interval { get; internal set; }

        [DataMember(Name = "message", IsRequired = false)]
        public string Message { get; internal set; }

        [DataMember(Name = "error", IsRequired = false)]
        public string Error { get; internal set; }

        [DataMember(Name = "error_description", IsRequired = false)]
        public string ErrorDescription { get; internal set; }

        public DeviceCodeResult GetResult(string clientId, string resource)
        {
            return new DeviceCodeResult()
            {
                ExpiresOn = DateTime.UtcNow.AddSeconds(this.ExpiresIn),
                Message = this.Message,
                DeviceCode = this.DeviceCode,
                UserCode = this.UserCode,
                Interval = this.Interval,
                VerificationUrl = this.VerificationUrl,
                ClientId = clientId,
                Resource = resource
            };
        }
    }
}