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
            this.AccessToken = result.AccessToken;
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
