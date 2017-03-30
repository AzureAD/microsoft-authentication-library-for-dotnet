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
    /// <see cref="AccessTokenCacheKey" /> can be used with Linq to access items from the TokenCache dictionary.
    /// </summary>
    internal class AccessTokenCacheKey : TokenCacheKeyBase
    {
        public AccessTokenCacheKey(string authority, SortedSet<string> scope, string clientId, string userIdentifier) : base(clientId, userIdentifier)
        {
            Authority = authority;
            Scope = scope ?? new SortedSet<string>();
        }

        public string Authority { get; }

        public SortedSet<string> Scope { get; }

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
            stringBuilder.Append(MsalHelpers.Base64Encode(UserIdentifier) + "$");

            return stringBuilder.ToString();
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
            AccessTokenCacheKey other = obj as AccessTokenCacheKey;
            return (other != null) && Equals(other);
        }

        /// <summary>
        /// Determines whether the specified AccessTokenCacheKey is equal to the current object.
        /// </summary>
        /// <returns>
        /// true if the specified AccessTokenCacheKey is equal to the current object; otherwise, false.
        /// </returns>
        /// <param name="other">The AccessTokenCacheKey to compare with the current object. </param>
        /// <filterpriority>2</filterpriority>
        public bool Equals(AccessTokenCacheKey other)
        {
            return ReferenceEquals(this, other) ||
                   (other != null
                    && (other.Authority == Authority)
                    && ScopeEquals(other.Scope)
                    && Equals(ClientId, other.ClientId)
                    && UserIdentifier == other.UserIdentifier);
        }

        /// <summary>
        /// Returns the hash code for this AccessTokenCacheKey.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer hash code.
        /// </returns>
        public override int GetHashCode()
        {
            const string Delimiter = ":::";
            return (Authority + Delimiter
                    + MsalHelpers.AsSingleString(Scope) + Delimiter
                    + ClientId.ToLowerInvariant() + Delimiter
                    + UserIdentifier + Delimiter).GetHashCode();
        }

        internal bool ScopeEquals(SortedSet<string> otherScope)
        {
            if (Scope == null)
            {
                return otherScope == null;
            }

            if (otherScope == null)
            {
                return Scope == null;
            }

            if (Scope.Count == otherScope.Count)
            {
                return Scope.Intersect(otherScope, StringComparer.OrdinalIgnoreCase).Count() == Scope.Count;
            }

            return false;
        }
    }
}