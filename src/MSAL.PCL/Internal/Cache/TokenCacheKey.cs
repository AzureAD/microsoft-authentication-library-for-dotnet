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
using System.Globalization;
using System.Linq;
using System.Text;

namespace Microsoft.Identity.Client.Internal.Cache
{
    /// <summary>
    /// <see cref="TokenCacheKey" /> can be used with Linq to access items from the TokenCache dictionary.
    /// </summary>
    internal sealed class TokenCacheKey
    {
        internal TokenCacheKey(string authority, SortedSet<string> scope, string clientId, User user, string policy)
            : this(
                authority, scope, clientId, (user != null) ? user.UniqueId : null,
                (user != null) ? user.DisplayableId : null, (user != null) ? user.HomeObjectId : null, policy)
        {
        }

        internal TokenCacheKey(string authority, SortedSet<string> scope, string clientId, User user)
            : this(
                authority, scope, clientId, (user != null) ? user.UniqueId : null,
                (user != null) ? user.DisplayableId : null, (user != null) ? user.HomeObjectId : null, null)
        {
        }

        internal TokenCacheKey(string authority, SortedSet<string> scope, string clientId, string uniqueId,
            string displayableId, string homeObjectId)
            : this(authority, scope, clientId, uniqueId, displayableId, homeObjectId, null)
        {
        }

        internal TokenCacheKey(string authority, SortedSet<string> scope, string clientId, string uniqueId,
            string displayableId, string homeObjectId, string policy)
        {
            this.Authority = authority;
            this.Scope = scope;
            this.ClientId = clientId;
            this.UniqueId = uniqueId;
            this.DisplayableId = displayableId;
            this.HomeObjectId = homeObjectId;
            this.Policy = policy;
        }

        public string Authority { get; }
        public SortedSet<string> Scope { get; }
        public string ClientId { get; }
        public string UniqueId { get; }
        public string DisplayableId { get; }
        public string HomeObjectId { get; }
        public string Policy { get; }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(EncodingHelper.Base64Encode(mAuthority) + "$");
            stringBuilder.Append(MSALUtils.Base64Encode(mClientId) + "$");
            // scope is treeSet to guarantee the order of the scopes when converting to string.
            stringBuilder.Append(MSALUtils.Base64Encode(Scope.AsSingleString()) + "$");
            stringBuilder.Append(MSALUtils.Base64Encode(mDisplayableId) + "$");
            stringBuilder.Append(MSALUtils.Base64Encode(mUniqueId) + "$");
            stringBuilder.Append(MSALUtils.Base64Encode(mHomeObjectId) + "$");
            stringBuilder.Append(MSALUtils.Base64Encode(mPolicy));
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <returns>
        /// true if the specified object is equal to the current object; otherwise, false.
        /// </returns>
        /// <param name="obj">The object to compare with the current object. </param>
        /// <filterpriority>2</filterpriority>
        public override bool Equals(object obj)
        {
            TokenCacheKey other = obj as TokenCacheKey;
            return (other != null) && this.Equals(other);
        }

        /// <summary>
        /// Determines whether the specified TokenCacheKey is equal to the current object.
        /// </summary>
        /// <returns>
        /// true if the specified TokenCacheKey is equal to the current object; otherwise, false.
        /// </returns>
        /// <param name="other">The TokenCacheKey to compare with the current object. </param>
        /// <filterpriority>2</filterpriority>
        public bool Equals(TokenCacheKey other)
        {
            return ReferenceEquals(this, other) ||
                   (other != null
                    && (other.Authority == this.Authority)
                    && this.ScopeEquals(other.Scope)
                    && this.Equals(this.ClientId, other.ClientId)
                    && (other.UniqueId == this.UniqueId)
                    && this.Equals(this.DisplayableId, other.DisplayableId)
                    && (this.HomeObjectId == other.HomeObjectId)
                    && this.Equals(this.Policy, other.Policy));
        }

        /// <summary>
        /// Returns the hash code for this TokenCacheKey.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer hash code.
        /// </returns>
        public override int GetHashCode()
        {
            const string Delimiter = ":::";
            return (this.Authority + Delimiter
                    + MsalStringHelper.AsSingleString(this.Scope) + Delimiter
                    + this.ClientId.ToLower() + Delimiter
                    + this.UniqueId + Delimiter
                    + this.HomeObjectId + Delimiter
                    + ((this.DisplayableId != null) ? this.DisplayableId.ToLower() : null) + Delimiter
                    + ((this.Policy != null) ? this.Policy.ToLower() : null)).GetHashCode();
        }

        internal bool ScopeEquals(SortedSet<string> otherScope)
        {
            if (this.Scope == null)
            {
                return otherScope == null;
            }

            if (otherScope == null)
            {
                return this.Scope == null;
            }

            if (Scope.Count == otherScope.Count)
            {
                return this.Scope.ToLower().Intersect(otherScope.ToLower()).Count() == this.Scope.Count;
            }

            return false;
        }

        internal bool Equals(string string1, string string2)
        {
            return (string.Compare(string2, string1, StringComparison.OrdinalIgnoreCase) == 0);
        }

        public static TokenCacheKey ExtractKeyForAT(TokenCacheItem accessTokenItem)
        {
            return new TokenCacheKey(accessTokenItem.Authority, accessTokenItem.Scope, accessTokenItem.ClientId,
                accessTokenItem.UniqueId,
                accessTokenItem.DisplayableId, accessTokenItem.HomeObjectId, accessTokenItem.Policy);
        }

        public static TokenCacheKey ExtractKeyForRT(RefreshTokenCacheItem refreshTokenItem)
        {
            //TODO - consider removing policy from refresh token cache

            return new TokenCacheKey(null, null, refreshTokenItem.ClientId, refreshTokenItem.UniqueId,
                refreshTokenItem.DisplayableId, refreshTokenItem.HomeObjectId, refreshTokenItem.Policy);
        }
    }
}