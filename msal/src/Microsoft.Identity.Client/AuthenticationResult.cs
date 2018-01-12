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
using Microsoft.Identity.Core.Cache;
using Microsoft.Identity.Core.Helpers;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Contains the results of one token acquisition operation.
    /// </summary>
    public class AuthenticationResult
    {
        private const string Oauth2AuthorizationHeader = "Bearer ";
        private readonly MsalAccessTokenCacheItem _msalAccessTokenCacheItem;

        internal AuthenticationResult()
        {
        }

        internal AuthenticationResult(MsalAccessTokenCacheItem msalAccessTokenCacheItem)
        {
            _msalAccessTokenCacheItem = msalAccessTokenCacheItem;
            User = new User(_msalAccessTokenCacheItem.GetUserIdentifier(),
                _msalAccessTokenCacheItem.IdToken?.PreferredUsername, _msalAccessTokenCacheItem.IdToken?.Name,
                _msalAccessTokenCacheItem.IdToken?.Issuer);
        }

        /// <summary>
        /// Gets the Access Token requested.
        /// </summary>
        public virtual string AccessToken => _msalAccessTokenCacheItem.AccessToken;

        /// <summary>
        /// Gets the Unique Id of the user.
        /// </summary>
        public virtual string UniqueId => _msalAccessTokenCacheItem.IdToken?.GetUniqueId();

        /// <summary>
        /// Gets the point in time in which the Access Token returned in the Token property ceases to be valid.
        /// This value is calculated based on current UTC time measured locally and the value expiresIn received from the
        /// service.
        /// </summary>
        public virtual DateTimeOffset ExpiresOn => _msalAccessTokenCacheItem.ExpiresOn;

        /// <summary>
        /// Gets an identifier for the tenant the token was acquired from. This property will be null if tenant information is
        /// not returned by the service.
        /// </summary>
        public virtual string TenantId => _msalAccessTokenCacheItem.IdToken?.TenantId;

        /// <summary>
        /// Gets the user object. Some elements in User might be null if not returned by the
        /// service. It can be passed back in some API overloads to identify which user should be used.
        /// </summary>
        public virtual IUser User { get; internal set; }

        /// <summary>
        /// Gets the entire Id Token if returned by the service or null if no Id Token is returned.
        /// </summary>
        public virtual string IdToken => _msalAccessTokenCacheItem.RawIdToken;

        /// <summary>
        /// Gets the scope values returned from the service.
        /// </summary>
        public virtual IEnumerable<string> Scopes => _msalAccessTokenCacheItem.ScopeSet.AsArray();

        /// <summary>
        /// Creates authorization header from authentication result.
        /// </summary>
        /// <returns>Created authorization header</returns>
        public virtual string CreateAuthorizationHeader()
        {
            return Oauth2AuthorizationHeader + AccessToken;
        }
    }
}