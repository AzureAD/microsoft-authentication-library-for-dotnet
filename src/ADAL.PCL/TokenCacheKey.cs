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
        internal TokenCacheKey(string authority, string resource, string clientId, TokenSubjectType tokenSubjectType, UserInfo userInfo)
            : this(authority, resource, clientId, tokenSubjectType, (userInfo != null) ? userInfo.UniqueId : null, (userInfo != null) ? userInfo.DisplayableId : null)
        {
        }

        internal TokenCacheKey(string authority, string resource, string clientId, TokenSubjectType tokenSubjectType, string uniqueId, string displayableId)
        {
            this.Authority = authority;
            this.Resource = resource;
            this.ClientId = clientId;
            this.TokenSubjectType = tokenSubjectType;
            this.UniqueId = uniqueId;
            this.DisplayableId = displayableId;
        }

        public string Authority { get; private set; }

        public string Resource { get; internal set; }

        public string ClientId { get; private set; }

        public string UniqueId { get; private set; }

        public string DisplayableId { get; private set; }

        public TokenSubjectType TokenSubjectType { get; private set; }

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
               && this.ResourceEquals(other.Resource)
               && this.ClientIdEquals(other.ClientId)
               && (other.UniqueId == this.UniqueId)
               && this.DisplayableIdEquals(other.DisplayableId)
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
                + this.Resource.ToLower() + Delimiter
                + this.ClientId.ToLower() + Delimiter
                + this.UniqueId + Delimiter
                + ((this.DisplayableId != null) ? this.DisplayableId.ToLower() : null) + Delimiter
                + (int)this.TokenSubjectType).GetHashCode();
        }

        internal bool ResourceEquals(string otherResource)
        {
            return (string.Compare(otherResource, this.Resource, StringComparison.OrdinalIgnoreCase) == 0);
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