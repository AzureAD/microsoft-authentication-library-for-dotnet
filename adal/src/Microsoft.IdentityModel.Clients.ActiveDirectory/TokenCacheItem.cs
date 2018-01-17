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
using Microsoft.Identity.Core.Cache;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Cache;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    /// <summary>
    /// Token cache item
    /// </summary>
    public sealed class TokenCacheItem
    {
        private readonly AdalTokenCacheKey _key;
        private readonly AdalResult _result;

        /// <summary>
        /// Default constructor.
        /// </summary>
        internal TokenCacheItem(AdalTokenCacheKey key, AdalResult result)
        {
            _key = key;
            _result = result;
        }

        /// <summary>
        /// Gets the Authority.
        /// </summary>
        public string Authority => _key.Authority;

        /// <summary>
        /// Gets the ClientId.
        /// </summary>
        public string ClientId => _key.ClientId;

        /// <summary>
        /// Gets the Expiration.
        /// </summary>
        public DateTimeOffset ExpiresOn => _result.ExpiresOn;

        /// <summary>
        /// Gets the FamilyName.
        /// </summary>
        public string FamilyName => _result.UserInfo?.FamilyName;

        /// <summary>
        /// Gets the GivenName.
        /// </summary>
        public string GivenName => _result.UserInfo?.GivenName;

        /// <summary>
        /// Gets the IdentityProviderName.
        /// </summary>
        public string IdentityProvider => _result.UserInfo?.IdentityProvider;

        /// <summary>
        /// Gets the Resource.
        /// </summary>
        public string Resource => _key.Resource;

        /// <summary>
        /// Gets the TenantId.
        /// </summary>
        public string TenantId => _result.TenantId;

        /// <summary>
        /// Gets the user's unique Id.
        /// </summary>
        public string UniqueId => _key.UniqueId;

        /// <summary>
        /// Gets the user's displayable Id.
        /// </summary>
        public string DisplayableId => _key.DisplayableId;

        /// <summary>
        /// Gets the Access Token requested.
        /// </summary>
        public string AccessToken => _result.AccessToken;

        /// <summary>
        /// Gets the entire Id Token if returned by the service or null if no Id Token is returned.
        /// </summary>
        public string IdToken => _result.IdToken;

        internal TokenSubjectType TokenSubjectType => _key.TokenSubjectType;

        internal bool Match(AdalTokenCacheKey key)
        {
            return (key.Authority == this.Authority && key.ResourceEquals(this.Resource) &&
                    key.ClientIdEquals(this.ClientId)
                    && key.TokenSubjectType == this.TokenSubjectType && key.UniqueId == this.UniqueId &&
                    key.DisplayableIdEquals(this.DisplayableId));
        }
    }
}