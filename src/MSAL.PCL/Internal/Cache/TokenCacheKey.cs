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
using System.Text;

namespace Microsoft.Identity.Client.Internal.Cache
{
    /// <summary>
    /// <see cref="TokenCacheKey" /> can be used with Linq to access items from the TokenCache dictionary.
    /// </summary>
    internal sealed class TokenCacheKey
    {
        internal TokenCacheKey(string authority, SortedSet<string> scope, string clientId, User user)
            : this(
                authority, scope, clientId, (user != null) ? user.HomeObjectId : null)
        {
        }

        internal TokenCacheKey(string authority, SortedSet<string> scope, string clientId, string homeObjectId)
        {
            this.Authority = authority;
            this.Scope = scope;
            if (this.Scope == null)
            {
                this.Scope = new SortedSet<string>();
            }

            this.ClientId = clientId;
            this.HomeObjectId = homeObjectId;
        }

        public string Authority { get; }
        public SortedSet<string> Scope { get; }
        public string ClientId { get; }
        public string HomeObjectId { get; }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(MsalHelpers.Base64Encode(Authority) + "$");
            stringBuilder.Append(MsalHelpers.Base64Encode(ClientId) + "$");
            // scope is treeSet to guarantee the order of the scopes when converting to string.
            stringBuilder.Append(MsalHelpers.Base64Encode(Scope.AsSingleString()) + "$");
            stringBuilder.Append(MsalHelpers.Base64Encode(HomeObjectId) + "$");

            return stringBuilder.ToString();
        }

/*        public static TokenCacheKey Deserialize(string serializedKey)
        {
            var sentences = new List<string>();
            int position = 0;
            int start = 0;
            // Extract from the string.
            do
            {
                position = serializedKey.IndexOf('$', start);
                if (position >= 0)
                {
                    sentences.Add(serializedKey.Substring(start, position - start + 1));
                    start = position + 1;
                }
            } while (position > 0);

        }*/

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
                    && (this.HomeObjectId == other.HomeObjectId));
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
                    + MsalHelpers.AsSingleString(this.Scope) + Delimiter
                    + this.ClientId.ToLower() + Delimiter
                    + this.HomeObjectId + Delimiter).GetHashCode();
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
    }
}