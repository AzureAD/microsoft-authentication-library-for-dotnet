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
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Test.ADAL.Common
{
    internal enum TokenCacheStoreType
    {
        Null,
        InMemory
    }

    internal enum PromptBehaviorProxy
    {
        NotSpecified,
        Always,
        Auto,
        AccessCodeOnly,
        Never,
        RefreshSession
    }

    internal enum AuthenticationStatusProxy
    {
        Success = 0,
        ClientError = -1,
        ServiceError = -2
    }

    [DataContract]
    internal class AuthenticationResultProxy
    {
        [DataMember]
        public string AccessTokenType { get; set; }

        [DataMember]
        public string AccessToken { get; set; }

        [DataMember]
        public string RefreshToken { get; set; }

        [DataMember]
        public string IdToken { get; set; }

        [DataMember]
        public DateTimeOffset ExpiresOn { get; set; }

        [DataMember]
        public string TenantId { get; set; }

        [DataMember]
        public UserInfo UserInfo { get; set; }

        [DataMember]
        public bool IsMultipleResourceRefreshToken { get; set; }

        [DataMember]
        public AuthenticationStatusProxy Status { get; set; }

        [DataMember]
        public string Error { get; set; }

        [DataMember]
        public string ErrorDescription { get; set; }

        [DataMember]
        public string AuthenticationParametersAuthority { get; internal set; }

        [DataMember]
        public string AuthenticationParametersResource { get; internal set; }

        public Exception Exception { get; set; }

        public int ExceptionStatusCode { get; set; }

        internal static AuthenticationResultProxy Deserialize(string obj)
        {
            AuthenticationResultProxy output = null;
            var serializer = new DataContractJsonSerializer(typeof(AuthenticationResultProxy));
            byte[] serializedObjectBytes = Encoding.UTF8.GetBytes(obj);
            using (var stream = new MemoryStream(serializedObjectBytes))
            {
                output = (AuthenticationResultProxy)serializer.ReadObject(stream);
            }

            return output;
        }

        internal string Serialize()
        {
            string output = string.Empty;
            var serializer = new DataContractJsonSerializer(typeof(AuthenticationResultProxy));
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, this);
                stream.Position = 0;
                using (var reader = new StreamReader(stream))
                {
                    output = reader.ReadToEnd();
                }
            }

            return output;
        }
    }
}
