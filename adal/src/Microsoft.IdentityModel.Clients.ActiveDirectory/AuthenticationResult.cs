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
using System.Runtime.Serialization;
using Microsoft.Identity.Core.Cache;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    /// <summary>
    /// Contains the results of one token acquisition operation. 
    /// </summary>
    [DataContract]
    public sealed class AuthenticationResult
    {
        private const string Oauth2AuthorizationHeader = "Bearer ";
        private readonly AdalResult _adalResult;

        /// <summary>
        /// Creates result returned from AcquireToken.
        /// </summary>
        internal AuthenticationResult(AdalResult adalResult)
        {
            if (adalResult == null)
            {
                throw new ArgumentNullException(nameof(adalResult));
            }

            _adalResult = adalResult;
            UserInfo = new UserInfo(_adalResult.UserInfo);
        }

        /// <summary>
        /// Gets the type of the Access Token returned. 
        /// </summary>
        [DataMember]
        public string AccessTokenType => _adalResult.AccessTokenType;

        /// <summary>
        /// Gets the Access Token requested.
        /// </summary>
        [DataMember]
        public string AccessToken => _adalResult.AccessToken;

        /// <summary>
        /// Gets the point in time in which the Access Token returned in the AccessToken property ceases to be valid.
        /// This value is calculated based on current UTC time measured locally and the value expiresIn received from the service.
        /// </summary>
        [DataMember]
        public DateTimeOffset ExpiresOn => _adalResult.ExpiresOn;

        /// <summary>
        /// Gets the point in time in which the Access Token returned in the AccessToken property ceases to be valid in ADAL's extended LifeTime.
        /// This value is calculated based on current UTC time measured locally and the value ext_expiresIn received from the service.
        /// </summary>
        [DataMember]
        internal DateTimeOffset ExtendedExpiresOn => _adalResult.ExtendedExpiresOn;

        /// <summary>
        /// Gives information to the developer whether token returned is during normal or extended lifetime.
        /// </summary>
        [DataMember]
        public bool ExtendedLifeTimeToken => _adalResult.ExtendedLifeTimeToken;

        /// <summary>
        /// Gets an identifier for the tenant the token was acquired from. This property will be null if tenant information is not returned by the service.
        /// </summary>
        [DataMember]
        public string TenantId => _adalResult.TenantId;

        /// <summary>
        /// Gets user information including user Id. Some elements in UserInfo might be null if not returned by the service.
        /// </summary>
        [DataMember]
        public UserInfo UserInfo { get; internal set; }

        /// <summary>
        /// Gets the entire Id Token if returned by the service or null if no Id Token is returned.
        /// </summary>
        [DataMember]
        public string IdToken => _adalResult.IdToken;

        /// <summary>
        /// Gets the authority that has issued the token.
        /// </summary>
        public string Authority => _adalResult.Authority;

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
            _adalResult.TenantId = tenantId;
            _adalResult.IdToken = idToken;
            if (userInfo != null)
            {
                this.UserInfo = new UserInfo(userInfo);
            }
        }
    }
}