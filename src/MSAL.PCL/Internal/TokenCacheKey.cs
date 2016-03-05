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
using System.Linq;

namespace Microsoft.Identity.Client.Internal
{
    /// <summary>
    /// <see cref="TokenCacheKey"/> can be used with Linq to access items from the TokenCache dictionary.
    /// </summary>
    internal sealed class TokenCacheKey
    {
        internal TokenCacheKey(string authority, HashSet<string> scope, string clientId, User user, string policy)
            : this(authority, scope, clientId, (user != null) ? user.UniqueId : null, (user != null) ? user.DisplayableId : null, (user != null) ? user.HomeObjectId : null, policy)
        {
        }

        internal TokenCacheKey(string authority, HashSet<string> scope, string clientId, User user)
            : this(authority, scope, clientId, (user != null) ? user.UniqueId : null, (user != null) ? user.DisplayableId : null, (user != null) ? user.HomeObjectId : null, null)
        {
        }

        internal TokenCacheKey(string authority, HashSet<string> scope, string clientId, string uniqueId, string displayableId, string homeObjectId)
            : this(authority, scope, clientId, uniqueId, displayableId, homeObjectId, null)
        {
        }

        internal TokenCacheKey(string authority, HashSet<string> scope, string clientId, string uniqueId, string displayableId, string homeObjectId, string policy)
        {
            this.Authority = authority;
            this.Scope = scope;
            this.ClientId = clientId;
            this.UniqueId = uniqueId;
            this.DisplayableId = displayableId;
            this.HomeObjectId = homeObjectId;
            this.Policy = policy;
        }

        public string Authority { get; private set; }

        public HashSet<string> Scope { get; internal set; }

        public string ClientId { get; private set; }

        public string UniqueId { get; private set; }

        public string DisplayableId { get; private set; }

        public string HomeObjectId { get; private set; }

        public string Policy { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return
                string.Format(
                    "Authority:{0}, Scope:{1}, ClientId:{2}, UniqueId:{3}, DisplayableId:{4}, HomeObjectId:{5}, Policy:{6}",
                    this.Authority, MsalStringHelper.AsSingleString(this.Scope.ToArray()), this.ClientId,
                    this.UniqueId, this.DisplayableId, this.HomeObjectId, this.Policy);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <returns>
        /// true if the specified object is equal to the current object; otherwise, false.
        /// </returns>
        /// <param name="obj">The object to compare with the current object. </param><filterpriority>2</filterpriority>
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
        /// <param name="other">The TokenCacheKey to compare with the current object. </param><filterpriority>2</filterpriority>
        public bool Equals(TokenCacheKey other)
        {
            return ReferenceEquals(this, other) ||
                   (other != null
                    && (other.Authority == this.Authority)
                    && this.ScopeEquals(other.Scope)
                    && this.Equals(this.ClientId, other.ClientId)
                    && this.Equals(other.UniqueId, this.UniqueId)
                    && this.Equals(this.DisplayableId, other.DisplayableId)
                    && this.Equals(this.HomeObjectId, other.HomeObjectId)
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

        internal bool ScopeContains(HashSet<string> otherScope)
        {
            if (this.Scope == null)
            {
                return otherScope == null;
            }

            if (otherScope == null)
            {
                return true;
            }

            foreach (string otherString in otherScope)
            {
                if (!this.Scope.Contains(otherString))
                {
                    return false;
                }
            }

            return true;
        }

        internal bool ScopeEquals(HashSet<string> otherScope)
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
                return this.Scope.Intersect(otherScope).Count() == this.Scope.Count;
            }

            return false;
        }

        public bool ScopeIntersects(HashSet<string> otherScope)
        {
            if (this.Scope == null)
            {
                return otherScope == null;
            }

            if (otherScope == null)
            {
                return this.Scope == null;
            }

            return this.Scope.Intersect(otherScope).ToArray().Length > 0;
        }
        
        internal bool Equals(string string1, string string2)
        {
            return (string.Compare(string2, string1, StringComparison.OrdinalIgnoreCase) == 0);
        }
    }
}