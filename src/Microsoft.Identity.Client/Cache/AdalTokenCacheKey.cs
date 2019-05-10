// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.Identity.Client.Cache
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
    /// <see cref="AdalTokenCacheKey"/> can be used with Linq to access items from the TokenCache dictionary.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal sealed class AdalTokenCacheKey
    {
        internal AdalTokenCacheKey(string authority, string resource, string clientId, TokenSubjectType tokenSubjectType, AdalUserInfo adalUserInfo)
            : this(authority, resource, clientId, tokenSubjectType, adalUserInfo?.UniqueId, adalUserInfo?.DisplayableId)
        {
        }

        internal AdalTokenCacheKey(string authority, string resource, string clientId, TokenSubjectType tokenSubjectType, string uniqueId, string displayableId)
        {
            Authority = authority;
            Resource = resource;
            ClientId = clientId;
            TokenSubjectType = tokenSubjectType;
            UniqueId = uniqueId;
            DisplayableId = displayableId;
        }

        public string Authority { get; }

        public string Resource { get; }

        public string ClientId { get; }

        public string UniqueId { get; }

        public string DisplayableId { get; }

        public TokenSubjectType TokenSubjectType { get; }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <returns>
        /// true if the specified object is equal to the current object; otherwise, false.
        /// </returns>
        /// <param name="obj">The object to compare with the current object. </param><filterpriority>2</filterpriority>
        public override bool Equals(object obj)
        {
            return (obj is AdalTokenCacheKey other) && Equals(other);
        }

        /// <summary>
        /// Determines whether the specified TokenCacheKey is equal to the current object.
        /// </summary>
        /// <returns>
        /// true if the specified TokenCacheKey is equal to the current object; otherwise, false.
        /// </returns>
        /// <param name="other">The TokenCacheKey to compare with the current object. </param><filterpriority>2</filterpriority>
        public bool Equals(AdalTokenCacheKey other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return other != null
                && other.Authority == Authority
                && ResourceEquals(other.Resource)
                && ClientIdEquals(other.ClientId)
                && other.UniqueId == UniqueId
                && DisplayableIdEquals(other.DisplayableId)
                && other.TokenSubjectType == TokenSubjectType;
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
            var hashString = Authority + Delimiter
                           + Resource.ToLowerInvariant() + Delimiter
                           + ClientId.ToLowerInvariant() + Delimiter
                           + UniqueId + Delimiter
                           + DisplayableId?.ToLowerInvariant() + Delimiter
                           + (int)TokenSubjectType;
            return hashString.GetHashCode();
        }

        internal bool ResourceEquals(string otherResource)
        {
            return string.Compare(otherResource, Resource, StringComparison.OrdinalIgnoreCase) == 0;
        }

        internal bool ClientIdEquals(string otherClientId)
        {
            return string.Compare(otherClientId, ClientId, StringComparison.OrdinalIgnoreCase) == 0;
        }

        internal bool DisplayableIdEquals(string otherDisplayableId)
        {
            return string.Compare(otherDisplayableId, DisplayableId, StringComparison.OrdinalIgnoreCase) == 0;
        }

        private string DebuggerDisplay =>
            string.Format(
                CultureInfo.InvariantCulture,
                "AdalTokenCacheKey: {0} {1} {2} {3} {4}",
                Authority, Resource, ClientId, UniqueId, DisplayableId);
    }
}
