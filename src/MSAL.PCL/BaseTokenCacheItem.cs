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
using System.Collections.Generic;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.OAuth2;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Token cache item
    /// </summary>
    internal class BaseTokenCacheItem
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        internal BaseTokenCacheItem(string authority, string clientId, string policy, TokenResponse response)
        {
            this.Authority = key.Authority;
            this.Scope = key.Scope;
            this.ClientId = key.ClientId;
            this.UniqueId = key.UniqueId;
            this.DisplayableId = key.DisplayableId;
            this.HomeObjectId = key.HomeObjectId;
            this.TenantId = result.TenantId;
            this.ExpiresOn = result.ExpiresOn;
            this.Token = result.Token;
            this.User = result.User;
            this.Policy = key.Policy;

            if (result.User != null)
            {
                this.Name = result.User.Name;
            }
        }

        /// <summary>
        /// Gets the Authority.
        /// </summary>
        public string Authority { get; }

        /// <summary>
        /// Gets the ClientId.
        /// </summary>
        public string ClientId { get; internal set; }

        /// <summary>
        /// Gets the Expiration.
        /// </summary>
        public DateTimeOffset ExpiresOn { get; internal set; }

        /// <summary>
        /// Gets the Version.
        /// </summary>
        public string FamilyName { get; internal set; }

        /// <summary>
        /// Gets the Name.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// Gets the IdentityProviderName.
        /// </summary>
        public string IdentityProvider { get; internal set; }

        /// <summary>
        /// Gets the Scope.
        /// </summary>
        public HashSet<string> Scope { get; internal set; }

        /// <summary>
        /// Gets the Policy.
        /// </summary>
        public string Policy { get; internal set; }

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

        internal string HomeObjectId { get; set; }

        /// <summary>
        /// Gets the Access Token requested.
        /// </summary>
        public string Token { get; internal set; }

        /// <summary>
        /// Gets the entire Profile Info if returned by the service or null if no Id Token is returned.
        /// </summary>
        public User User { get; internal set; }

        internal bool Match(TokenCacheKey key)
        {
            return key != null &&
                   (key.Authority == this.Authority && key.ScopeEquals(this.Scope) &&
                    key.Equals(key.ClientId, this.ClientId)
                    && key.UniqueId == this.UniqueId &&
                    key.Equals(key.DisplayableId, this.DisplayableId) && (key.HomeObjectId == this.HomeObjectId) &&
                    key.Equals(key.Policy, this.Policy));
        }
    }
}