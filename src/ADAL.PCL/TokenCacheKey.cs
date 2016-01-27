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
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    /// <summary>
    /// Determines what type of subject the token was issued for.
    /// </summary>
    internal enum TokenSubjectType
    {
        /// <summary>
        /// User
        /// </summary>
        User,
        /// <summary>
        /// Client
        /// </summary>
        Client,
        /// <summary>
        /// UserPlusClient: This is for confidential clients used in middle tier.
        /// </summary>
        UserPlusClient
    };

    /// <summary>
    /// <see cref="TokenCacheKey"/> can be used with Linq to access items from the TokenCache dictionary.
    /// </summary>
    internal sealed class TokenCacheKey
    {
        internal TokenCacheKey(string authority, HashSet<string> scope, string policy, string clientId, TokenSubjectType tokenSubjectType, User user)
            : this(authority, scope, clientId, tokenSubjectType, (user != null) ? user.UniqueId : null, (user != null) ? user.DisplayableId : null, policy)
        {
        }

        internal TokenCacheKey(string authority, HashSet<string> scope, string clientId, TokenSubjectType tokenSubjectType, User user)
            : this(authority, scope, clientId, tokenSubjectType, (user != null) ? user.UniqueId : null, (user != null) ? user.DisplayableId : null, "")
        {
        }

        internal TokenCacheKey(string authority, HashSet<string> scope, string clientId, TokenSubjectType tokenSubjectType, string uniqueId, string displayableId)
            : this(authority, scope, clientId, tokenSubjectType, uniqueId, displayableId, "")
        {
        }

        internal TokenCacheKey(string authority, HashSet<string> scope, string clientId, TokenSubjectType tokenSubjectType, string uniqueId, string displayableId, string policy)
        {
            this.Authority = authority;
            this.Scope = scope;
            this.ClientId = clientId;
            this.TokenSubjectType = tokenSubjectType;
            this.UniqueId = uniqueId;
            this.DisplayableId = displayableId;
            this.Policy = policy;
        }

        public string Authority { get; private set; }

        public HashSet<string> Scope { get; internal set; }

        public string ClientId { get; private set; }

        public string UniqueId { get; private set; }

        public string DisplayableId { get; private set; }

        public string RootId { get; private set; }

        public string Policy { get; private set; }

        public TokenSubjectType TokenSubjectType { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return
                string.Format(
                    "Authority:{0}, Scope:{1}, ClientId:{2}, UniqueId:{3}, DisplayableId:{4}, RootId:{5}, Policy:{6}, TokenSubjectType:{7}",
                    this.Authority, MsalStringHelper.CreateSingleStringFromArray(this.Scope.ToArray()), this.ClientId,
                    this.UniqueId, this.DisplayableId, this.RootId, this.Policy, this.TokenSubjectType);
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
               && this.ClientIdEquals(other.ClientId)
               && (other.UniqueId == this.UniqueId)
               && this.DisplayableIdEquals(other.DisplayableId)
               && this.PolicyEquals(other.Policy)
               && (other.TokenSubjectType == this.TokenSubjectType));
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
                + MsalStringHelper.CreateSingleStringFromSet(this.Scope) + Delimiter
                + this.ClientId.ToLower() + Delimiter
                + this.UniqueId + Delimiter
                + ((this.DisplayableId != null) ? this.DisplayableId.ToLower() : null) + Delimiter
                + ((this.Policy != null) ? this.Policy.ToLower() : null) + Delimiter
                + (int)this.TokenSubjectType).GetHashCode();
        }

        internal bool ScopeContains(HashSet<string> otherScope)
        {
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
            if (Scope.Count == otherScope.Count)
            {
                return this.Scope.Intersect(otherScope).Count() == this.Scope.Count;
            }

            return false;
        }

        public bool ScopeIntersects(string[] otherScope)
        {
            return this.Scope.Intersect(otherScope).ToArray().Length > 0;
        }

        internal bool PolicyEquals(string otherPolicy)
        {
            if (string.IsNullOrEmpty(this.Policy) && string.IsNullOrEmpty(otherPolicy))
            {
                return true;
            }

            if (string.IsNullOrEmpty(this.Policy) || string.IsNullOrEmpty(otherPolicy))
            {
                return false;
            }


            return (string.Compare(otherPolicy, this.Policy, StringComparison.OrdinalIgnoreCase) == 0);
        }

        internal bool ClientIdEquals(string otherClientId)
        {
            return (string.Compare(otherClientId, this.ClientId, StringComparison.OrdinalIgnoreCase) == 0);
        }

        internal bool DisplayableIdEquals(string otherDisplayableId)
        {
            return (string.Compare(otherDisplayableId, this.DisplayableId, StringComparison.OrdinalIgnoreCase) == 0);
        }
    }
}