// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Microsoft.Identity.Client.Cache
{
    [DataContract]
    internal class AdalResultWrapper
    {
        [DataMember]
        public AdalResult Result { get; set; }

        [DataMember]
        public string RawClientInfo { get; set; }

        /// <summary>
        /// Gets the Refresh Token associated with the requested Access Token. Note: not all operations will return a Refresh Token.
        /// </summary>
        [DataMember]
        public string RefreshToken { get; set; }

        /// <summary>
        /// Gets a value indicating whether the refresh token can be used for requesting access token for other resources.
        /// </summary>
        internal bool IsMultipleResourceRefreshToken => !string.IsNullOrWhiteSpace(RefreshToken) && !string.IsNullOrWhiteSpace(ResourceInResponse);

        // This is only needed for AcquireTokenByAuthorizationCode in which parameter resource is optional and we need
        // to get it from the STS response.
        [DataMember]
        internal string ResourceInResponse { get; set; }

        /// <summary>
        /// Serializes the object to a JSON string
        /// </summary>
        /// <returns>Deserialized authentication result</returns>
        public static AdalResultWrapper Deserialize(string serializedObject)
        {
            AdalResultWrapper resultEx;
            var serializer = new DataContractJsonSerializer(typeof(AdalResultWrapper));
            byte[] serializedObjectBytes = Encoding.UTF8.GetBytes(serializedObject);
            using (var stream = new MemoryStream(serializedObjectBytes))
            {
                resultEx = (AdalResultWrapper)serializer.ReadObject(stream);
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
            var serializer = new DataContractJsonSerializer(typeof(AdalResultWrapper));
            using (MemoryStream stream = new MemoryStream())
            {
                serializer.WriteObject(stream, this);
                serializedObject = Encoding.UTF8.GetString(stream.ToArray(), 0, (int)stream.Position);
            }

            return serializedObject;
        }

        internal Exception Exception { get; set; }

        [DataMember]
        public string UserAssertionHash { get; set; }

        internal AdalResultWrapper Clone()
        {
            return Deserialize(Serialize());
        }
    }
}
