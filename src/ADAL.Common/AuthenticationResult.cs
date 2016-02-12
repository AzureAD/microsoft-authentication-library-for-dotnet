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

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    /// <summary>
    /// Contains the results of one token acquisition operation. 
    /// </summary>
    public sealed partial class AuthenticationResult
    {
        private const string Oauth2AuthorizationHeader = "Bearer ";

        /// <summary>
        /// Creates result returned from AcquireToken. Except in advanced scenarios related to token caching, you do not need to create any instance of AuthenticationResult.
        /// </summary>
        /// <param name="accessTokenType">Type of the Access Token returned</param>
        /// <param name="accessToken">The Access Token requested</param>
        /// <param name="refreshToken">The Refresh Token associated with the requested Access Token</param>
        /// <param name="expiresOn">The point in time in which the Access Token returned in the AccessToken property ceases to be valid</param>
        internal AuthenticationResult(string accessTokenType, string accessToken, string refreshToken, DateTimeOffset expiresOn)
        {
            this.AccessTokenType = accessTokenType;
            this.AccessToken = accessToken;
            this.RefreshToken = refreshToken;
            this.ExpiresOn = DateTime.SpecifyKind(expiresOn.DateTime, DateTimeKind.Utc);
        }

        /// <summary>
        /// Gets the type of the Access Token returned. 
        /// </summary>
        [DataMember]
        public string AccessTokenType { get; private set; }

        /// <summary>
        /// Gets the Access Token requested.
        /// </summary>
        [DataMember]
        public string AccessToken { get; internal set; }

        /// <summary>
        /// Gets the Refresh Token associated with the requested Access Token. Note: not all operations will return a Refresh Token.
        /// </summary>
        [DataMember]
        public string RefreshToken { get; internal set; }

        /// <summary>
        /// Gets the point in time in which the Access Token returned in the AccessToken property ceases to be valid.
        /// This value is calculated based on current UTC time measured locally and the value expiresIn received from the service.
        /// </summary>
        [DataMember]
        public DateTimeOffset ExpiresOn { get; internal set; }

        /// <summary>
        /// Gets an identifier for the tenant the token was acquired from. This property will be null if tenant information is not returned by the service.
        /// </summary>
        [DataMember]
        public string TenantId { get; private set; }

        /// <summary>
        /// Gets user information including user Id. Some elements in UserInfo might be null if not returned by the service.
        /// </summary>
        [DataMember]
        public UserInfo UserInfo { get; internal set; }

        /// <summary>
        /// Gets the entire Id Token if returned by the service or null if no Id Token is returned.
        /// </summary>
        [DataMember]
        public string IdToken { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether the refresh token can be used for requesting access token for other resources.
        /// </summary>
        [DataMember]
        public bool IsMultipleResourceRefreshToken { get; internal set; }

        /// <summary>
        /// Serializes the object to a JSON string
        /// </summary>
        /// <returns>Deserialized authentication result</returns>
        public static AuthenticationResult Deserialize(string serializedObject)
        {
            AuthenticationResult result;
            var serializer = new DataContractJsonSerializer(typeof(AuthenticationResult));
            byte[] serializedObjectBytes = Encoding.UTF8.GetBytes(serializedObject);
            using (var stream = new MemoryStream(serializedObjectBytes))
            {
                result = (AuthenticationResult)serializer.ReadObject(stream);
            }

            return result;
        }

        /// <summary>
        /// Creates authorization header from authentication result.
        /// </summary>
        /// <returns>Created authorization header</returns>
        public string CreateAuthorizationHeader()
        {
            return Oauth2AuthorizationHeader + this.AccessToken;
        }

        /// <summary>
        /// Serializes the object to a JSON string
        /// </summary>
        /// <returns>Serialized authentication result</returns>
        public string Serialize()
        {
            string serializedObject;
            var serializer = new DataContractJsonSerializer(typeof(AuthenticationResult));
            using (MemoryStream stream = new MemoryStream())
            {
                serializer.WriteObject(stream, this);
                serializedObject = Encoding.UTF8.GetString(stream.ToArray(), 0, (int)stream.Position);
            }

            return serializedObject;
        }

        internal void UpdateTenantAndUserInfo(string tenantId, string idToken, UserInfo userInfo)
        {
            this.TenantId = tenantId;
            this.IdToken = idToken;
            if (userInfo != null)
            {
                this.UserInfo = new UserInfo(userInfo);
            }
        }

        [DataMember]
        internal String UserAssertionHash { get; set; }
    }
}
