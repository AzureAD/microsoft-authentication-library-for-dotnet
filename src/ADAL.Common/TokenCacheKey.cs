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
#if ADAL_WINRT
using Windows.Foundation.Metadata;
#endif

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    /// <summary>
    /// <see cref="TokenCacheKey"/> can be used with Linq to access items from the TokenCacheStore.
    /// </summary>
    internal sealed class TokenCacheKey
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public TokenCacheKey()
        {
        }

        /// <summary>
        /// Instantiates a key from a <see cref="AuthenticationResult"/>, it can be null.
        /// </summary>
        /// <param name="result">Result used for creating cache key</param>
        internal TokenCacheKey(AuthenticationResult result)
        {
            if (result == null)
            {
                return;
            }

            this.ExpiresOn = result.ExpiresOn;
            this.TenantId = result.TenantId;
            this.IsMultipleResourceRefreshToken = result.IsMultipleResourceRefreshToken;

            if (result.UserInfo != null)
            {
                this.FamilyName = result.UserInfo.FamilyName;
                this.GivenName = result.UserInfo.GivenName;
                this.IdentityProviderName = result.UserInfo.IdentityProvider;
                this.UniqueId = result.UserInfo.UniqueId;
                this.DisplayableId = result.UserInfo.DisplayableId;
            }
        }

        /// <summary>
        /// Gets or sets the Authority.
        /// </summary>
        public string Authority { get; set; }

        /// <summary>
        /// Gets or sets the ClientId.
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Gets or sets the Expiration.
        /// </summary>
        public DateTimeOffset ExpiresOn { get; set; }

        /// <summary>
        /// Gets or sets the FamilyName.
        /// </summary>
        public string FamilyName { get; set; }

        /// <summary>
        /// Gets or sets the GivenName.
        /// </summary>
        public string GivenName { get; set; }

        /// <summary>
        /// Gets or sets the IdentityProviderName.
        /// </summary>
        public string IdentityProviderName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the RefreshToken applies to multiple resources.
        /// </summary>
        public bool IsMultipleResourceRefreshToken { get; set; }

        /// <summary>
        /// Gets or sets the Resource.
        /// </summary>
        public string Resource { get; set; }

        /// <summary>
        /// Gets or sets the TenantId.
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the user's unique Id.
        /// </summary>
        public string UniqueId { get; set; }

        /// <summary>
        /// Gets or sets the user's displayable Id.
        /// </summary>
        public string DisplayableId { get; set; }

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
#if ADAL_WINRT
        [DefaultOverload]
#endif
        public bool Equals(TokenCacheKey other)
        {
            return ReferenceEquals(this, other) ||
               (other != null
               && (other.Authority == this.Authority)
               && (other.ClientId == this.ClientId)
               && (other.ExpiresOn == this.ExpiresOn)
               && (other.FamilyName == this.FamilyName)
               && (other.GivenName == this.GivenName)
               && (other.IdentityProviderName == this.IdentityProviderName)
               && (other.IsMultipleResourceRefreshToken == this.IsMultipleResourceRefreshToken)
               && (other.Resource == this.Resource)
               && (other.TenantId == this.TenantId)
               && (other.UniqueId == this.UniqueId)
               && (other.DisplayableId == this.DisplayableId));
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
                + this.ClientId + Delimiter
                + this.ExpiresOn + Delimiter
                + this.FamilyName + Delimiter
                + this.GivenName + Delimiter
                + this.IdentityProviderName + Delimiter
                + this.IsMultipleResourceRefreshToken + Delimiter
                + this.Resource + Delimiter
                + this.TenantId + Delimiter
                + this.UniqueId + Delimiter
                + this.DisplayableId).GetHashCode();
        }
    }
}