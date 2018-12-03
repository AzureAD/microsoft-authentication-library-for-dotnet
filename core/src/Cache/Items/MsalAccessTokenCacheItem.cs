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
using System.Runtime.Serialization;
using Microsoft.Identity.Core.Helpers;
using Microsoft.Identity.Core.OAuth2;

namespace Microsoft.Identity.Core.Cache
{
    [DataContract]
    internal class MsalAccessTokenCacheItem : MsalCredentialCacheItemBase
    {
        internal MsalAccessTokenCacheItem()
        {
            CredentialType = MsalCacheCommon.AccessToken;
        }

        internal MsalAccessTokenCacheItem
            (string environment, string clientId, MsalTokenResponse response, string tenantId) :

            this(environment, clientId, response.TokenType, response.Scope,
                 tenantId, response.AccessToken, response.AccessTokenExpiresOn, response.AccessTokenExtendedExpiresOn, response.ClientInfo)
        {
        }

        internal MsalAccessTokenCacheItem
            (string environment, string clientId, string tokenType, string scopes,
             string tenantId, string secret, DateTimeOffset accessTokenExpiresOn, DateTimeOffset accessTokenExtendedExpiresOn, string rawClientInfo) : this()
        {
            Environment = environment;
            ClientId = clientId;
            TokenType = tokenType;
            NormalizedScopes = scopes;
            TenantId = tenantId;
            Secret = secret;
            ExpiresOnUnixTimestamp = CoreHelpers.DateTimeToUnixTimestamp(accessTokenExpiresOn);
            ExtendedExpiresOnUnixTimestamp = CoreHelpers.DateTimeToUnixTimestamp(accessTokenExtendedExpiresOn);
            CachedAt = CoreHelpers.CurrDateTimeInUnixTimestamp();
            RawClientInfo = rawClientInfo;

            InitUserIdentifier();
        }

        [DataMember(Name = "realm")]
        internal string TenantId { get; set; }

        /// <summary>
        /// String comprised of scopes that have been lowercased and ordered.
        /// </summary>
        /// <remarks>Normalization is important when creating unique keys.</remarks>
        [DataMember(Name = "target", IsRequired = true)]
        internal string NormalizedScopes { get; set; }

        [DataMember(Name = "cached_at", IsRequired = true)]
        internal string CachedAt { get; set; }

        [DataMember(Name = "expires_on", IsRequired = true)]
        internal string ExpiresOnUnixTimestamp { get; set; }

        [DataMember(Name = "ext_expires_on")]
        internal string ExtendedExpiresOnUnixTimestamp { get; set; }

        [DataMember(Name = "user_assertion_hash", EmitDefaultValue = false)]
        public string UserAssertionHash { get; set; }

        [DataMember(Name = "access_token_type")]
        internal string TokenType { get; set; }

        internal string Authority => string.Format(CultureInfo.InvariantCulture, "https://{0}/{1}/", Environment, TenantId ?? "common");
        internal SortedSet<string> ScopeSet => ScopeHelper.ConvertStringToLowercaseSortedSet(NormalizedScopes);

        internal DateTimeOffset ExpiresOn
        {
            get
            {
                DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                var expiresLong = Convert.ToInt64(ExpiresOnUnixTimestamp, CultureInfo.InvariantCulture);
                return dtDateTime.AddSeconds(expiresLong).ToUniversalTime();
            }
        }

        internal DateTimeOffset ExtendedExpiresOn
        {
            get
            {
                DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                var expiresLong = Convert.ToInt64(ExtendedExpiresOnUnixTimestamp, CultureInfo.InvariantCulture);

                return dtDateTime.AddSeconds(expiresLong).ToUniversalTime();
            }
        }

        public bool IsExtendedLifeTimeToken { get; set; }

        internal MsalAccessTokenCacheKey GetKey()
        {
            return new MsalAccessTokenCacheKey(Environment, TenantId, HomeAccountId, ClientId, NormalizedScopes);
        }
        internal MsalIdTokenCacheKey GetIdTokenItemKey()
        {
            return new MsalIdTokenCacheKey(Environment, TenantId, HomeAccountId, ClientId);
        }
    }
}
