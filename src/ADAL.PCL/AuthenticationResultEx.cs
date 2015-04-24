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
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    [DataContract]
    internal class AuthenticationResultEx
    {
        [DataMember]
        public AuthenticationResult Result { get; set; }

        /// <summary>
        /// Gets the Refresh Token associated with the requested Access Token. Note: not all operations will return a Refresh Token.
        /// </summary>
        [DataMember]
        public string RefreshToken { get; set; }

        /// <summary>
        /// Gets a value indicating whether the refresh token can be used for requesting access token for other resources.
        /// </summary>
        internal bool IsMultipleResourceRefreshToken
        {
            get
            {
                return (!string.IsNullOrWhiteSpace(this.RefreshToken) && !string.IsNullOrWhiteSpace(this.ResourceInResponse));
            }            
        }

        // This is only needed for AcquireTokenByAuthorizationCode in which parameter resource is optional and we need
        // to get it from the STS response.
        [DataMember]
        internal string ResourceInResponse { get; set; }


        /// <summary>
        /// Serializes the object to a JSON string
        /// </summary>
        /// <returns>Deserialized authentication result</returns>
        public static AuthenticationResultEx Deserialize(string serializedObject)
        {
            AuthenticationResultEx resultEx;
            var serializer = new DataContractJsonSerializer(typeof(AuthenticationResultEx));
            byte[] serializedObjectBytes = Encoding.UTF8.GetBytes(serializedObject);
            using (var stream = new MemoryStream(serializedObjectBytes))
            {
                resultEx = (AuthenticationResultEx)serializer.ReadObject(stream);
            }

            return resultEx;
        }

        /// <summary>
        /// Serializes the object to a JSON string
        /// </summary>
        /// <returns>Serialized authentication result</returns>
        public string Serialize()
        {
            string serializedObject;
            var serializer = new DataContractJsonSerializer(typeof(AuthenticationResultEx));
            using (MemoryStream stream = new MemoryStream())
            {
                serializer.WriteObject(stream, this);
                serializedObject = Encoding.UTF8.GetString(stream.ToArray(), 0, (int)stream.Position);
            }

            return serializedObject;
        }
    }
}
