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
    [DataContract]
    public sealed class AuthenticationResult
    {
        private const string Oauth2AuthorizationHeader = "Bearer ";

        /// <summary>
        /// Creates result returned from AcquireToken. Except in advanced scenarios related to token caching, you do not need to create any instance of AuthenticationResult.
        /// </summary>
        /// <param name="accessTokenType">Type of the Access AccessToken returned</param>
        /// <param name="accessToken">The Access AccessToken requested</param>
        /// <param name="expiresOn">The point in time in which the Access AccessToken returned in the AccessToken property ceases to be valid</param>
        internal AuthenticationResult(string accessTokenType, string accessToken, DateTimeOffset expiresOn)
        {
            this.AccessTokenType = accessTokenType;
            this.AccessToken = accessToken;
            this.ExpiresOn = DateTime.SpecifyKind(expiresOn.DateTime, DateTimeKind.Utc);
        }

        /// <summary>
        /// Gets the type of the Access AccessToken returned. 
        /// </summary>
        [DataMember]
        public string AccessTokenType { get; private set; }

        /// <summary>
        /// Gets the Access AccessToken requested.
        /// </summary>
        [DataMember]
        public string AccessToken { get; internal set; }

        /// <summary>
        /// Gets the point in time in which the Access AccessToken returned in the AccessToken property ceases to be valid.
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
        /// Gets an identifier for the tenant the token was acquired from. This property will be null if tenant information is not returned by the service.
        /// </summary>
        [DataMember]
        internal string FamilyId { get; set; }


        /// <summary>
        /// Gets user information including user Id. Some elements in User might be null if not returned by the service.
        /// </summary>
        [DataMember]
        public User User { get; internal set; }

        /// <summary>
        /// Gets the entire Id AccessToken if returned by the service or null if no Id AccessToken is returned.
        /// </summary>
        [DataMember]
        public string IdToken { get; internal set; }

        /// <summary>
        /// Creates authorization header from authentication result.
        /// </summary>
        /// <returns>Created authorization header</returns>
        public string CreateAuthorizationHeader()
        {
            return Oauth2AuthorizationHeader + this.AccessToken;
        }

        internal void UpdateTenantAndUser(string tenantId, string idToken, User user)
        {
            this.TenantId = tenantId;
            this.IdToken = idToken;
            if (User != null)
            {
                this.User = new User(user);
            }
        }
    }
}
