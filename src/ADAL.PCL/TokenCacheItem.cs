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

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    /// <summary>
    /// Token cache item
    /// </summary>
    public sealed class TokenCacheItem
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        internal TokenCacheItem(TokenCacheKey key, AuthenticationResult result)
        {
            this.Authority = key.Authority;
            this.Resource = key.Resource;
            this.ClientId = key.ClientId;
            this.TokenSubjectType = key.TokenSubjectType;
            this.UniqueId = key.UniqueId;
            this.DisplayableId = key.DisplayableId;
            this.TenantId = result.TenantId;
            this.ExpiresOn = result.ExpiresOn;
            this.IsMultipleResourceRefreshToken = result.IsMultipleResourceRefreshToken;
            this.AccessToken = result.AccessToken;
            this.RefreshToken = result.RefreshToken;
            this.IdToken = result.IdToken;

            if (result.UserInfo != null)
            {
                this.FamilyName = result.UserInfo.FamilyName;
                this.GivenName = result.UserInfo.GivenName;
                this.IdentityProvider = result.UserInfo.IdentityProvider;
            }
        }

        /// <summary>
        /// Gets the Authority.
        /// </summary>
        public string Authority { get; private set; }

        /// <summary>
        /// Gets the ClientId.
        /// </summary>
        public string ClientId { get; internal set; }

        /// <summary>
        /// Gets the Expiration.
        /// </summary>
        public DateTimeOffset ExpiresOn { get; internal set; }

        /// <summary>
        /// Gets the FamilyName.
        /// </summary>
        public string FamilyName { get; internal set; }

        /// <summary>
        /// Gets the GivenName.
        /// </summary>
        public string GivenName { get; internal set; }

        /// <summary>
        /// Gets the IdentityProviderName.
        /// </summary>
        public string IdentityProvider { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether the RefreshToken applies to multiple resources.
        /// </summary>
        public bool IsMultipleResourceRefreshToken { get; internal set; }

        /// <summary>
        /// Gets the Resource.
        /// </summary>
        public string Resource { get; internal set; }

        /// <summary>
        /// Gets the TenantId.
        /// </summary>
        public string TenantId { get; internal set; }

        /// <summary>
        /// Gets the user's unique Id.
        /// </summary>
        public string UniqueId { get; internal set; }

        /// <summary>
        /// Gets the user's displayable Id.
        /// </summary>
        public string DisplayableId { get; internal set; }

        /// <summary>
        /// Gets the Access Token requested.
        /// </summary>
        public string AccessToken { get; internal set; }

        /// <summary>
        /// Gets the Refresh Token associated with the requested Access Token. Note: not all operations will return a Refresh Token.
        /// </summary>
        public string RefreshToken { get; internal set; }

        /// <summary>
        /// Gets the entire Id Token if returned by the service or null if no Id Token is returned.
        /// </summary>
        public string IdToken { get; internal set; }

        internal TokenSubjectType TokenSubjectType { get; set; }

        internal bool Match(TokenCacheKey key)
        {
            return (key.Authority == this.Authority && key.ResourceEquals(this.Resource) && key.ClientIdEquals(this.ClientId)
                    && key.TokenSubjectType == this.TokenSubjectType && key.UniqueId == this.UniqueId && key.DisplayableIdEquals(this.DisplayableId));
        }
    }
}