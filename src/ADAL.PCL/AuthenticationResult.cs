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
        /// <param name="accessTokenType">Type of the Access Token returned</param>
        /// <param name="accessToken">The Access Token requested</param>
        /// <param name="expiresOn">The point in time in which the Access Token returned in the AccessToken property ceases to be valid</param>
        internal AuthenticationResult(string accessTokenType, string accessToken, DateTimeOffset expiresOn)
        {
            this.AccessTokenType = accessTokenType;
            this.AccessToken = accessToken;
            this.ExpiresOn = DateTime.SpecifyKind(expiresOn.DateTime, DateTimeKind.Utc);
            this.ExtendedExpiresOn = DateTime.SpecifyKind(expiresOn.DateTime, DateTimeKind.Utc);
        }

        /// <summary>
        /// Creates result returned from AcquireToken. Except in advanced scenarios related to token caching, you do not need to create any instance of AuthenticationResult.
        /// </summary>
        /// <param name="accessTokenType">Type of the Access Token returned</param>
        /// <param name="accessToken">The Access Token requested</param>
        /// <param name="expiresOn">The point in time in which the Access Token returned in the AccessToken property ceases to be valid</param>
        /// <param name="extendedExpiresOn">The point in time in which the Access Token returned in the AccessToken property ceases to be valid</param>
        internal AuthenticationResult(string accessTokenType, string accessToken, DateTimeOffset expiresOn, DateTimeOffset extendedExpiresOn)
        {
            this.AccessTokenType = accessTokenType;
            this.AccessToken = accessToken;
            this.ExpiresOn = DateTime.SpecifyKind(expiresOn.DateTime, DateTimeKind.Utc);
            this.ExtendedExpiresOn = DateTime.SpecifyKind(extendedExpiresOn.DateTime, DateTimeKind.Utc);
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
        /// Gets the point in time in which the Access Token returned in the AccessToken property ceases to be valid.
        /// This value is calculated based on current UTC time measured locally and the value expiresIn received from the service.
        /// </summary>
        [DataMember]
        public DateTimeOffset ExpiresOn { get; internal set; }

        /// <summary>
        /// Gets the point in time in which the Access Token returned in the AccessToken property ceases to be valid in ADAL's extended LifeTime.
        /// This value is calculated based on current UTC time measured locally and the value ext_expiresIn received from the service.
        /// </summary>
        [DataMember]
        internal DateTimeOffset ExtendedExpiresOn { get; set; }

        /// <summary>
        /// Gives information to the developer whether token returned is during normal or extended lifetime.
        /// </summary>
        [DataMember]
        public bool ExtendedLifeTimeToken { get; internal set; }

        /// <summary>
        /// Gets an identifier for the tenant the token was acquired from. This property will be null if tenant information is not returned by the service.
        /// </summary>
        [DataMember]
        public string TenantId { get; internal set; }

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
        /// Creates authorization header from authentication result.
        /// </summary>
        /// <returns>Created authorization header</returns>
        public string CreateAuthorizationHeader()
        {
            return Oauth2AuthorizationHeader + this.AccessToken;
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
    }
}
