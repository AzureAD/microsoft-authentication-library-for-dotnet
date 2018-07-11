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
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Core.Helpers;
using Microsoft.Identity.Core.Instance;
using Microsoft.Identity.Core.OAuth2;

namespace Microsoft.Identity.Core.Cache
{
    [DataContract]
    internal class MsalAccessTokenCacheItem : MsalCredentialCacheItemBase
    {
        internal MsalAccessTokenCacheItem()
        {
            CredentialType = Cache.CredentialType.accesstoken.ToString();
        }

        internal MsalAccessTokenCacheItem
            (Authority authority, string clientId, MsalTokenResponse response, string tenantId) : 
            
            this(authority.Host, clientId, response.TokenType, response.Scope.AsLowerCaseSortedSet().AsSingleString(),
                 tenantId, response.AccessToken, response.AccessTokenExpiresOn, response.ClientInfo)
        {
        }

        internal MsalAccessTokenCacheItem
            (string environment, string clientId, string tokenType, string scopes,
             string tenantId, string secret, DateTimeOffset accessTokenExpiresOn, string rawClientInfo) : this()
        {
            Environment = environment;
            ClientId = clientId;
            TokenType = tokenType;
            Scopes = scopes;        
            TenantId = tenantId;
            Secret = secret;
            ExpiresOnUnixTimestamp = CoreHelpers.DateTimeToUnixTimestamp(accessTokenExpiresOn);
            CachedAt = CoreHelpers.CurrDateTimeInUnixTimestamp();
            RawClientInfo = rawClientInfo;

            InitUserIdentifier();
        }

        [DataMember(Name = "realm")]
        internal string TenantId { get; set; }

        [DataMember(Name = "target", IsRequired = true)]
        internal string Scopes { get; set; }

        [DataMember(Name = "cached_at", IsRequired = true)]
        internal long CachedAt { get; set; }

        [DataMember(Name = "expires_on", IsRequired = true)]
        internal long ExpiresOnUnixTimestamp { get; set; }

        [DataMember(Name = "user_assertion_hash")]
        public string UserAssertionHash { get; set; }

        [DataMember(Name = "access_token_type")]
        internal string TokenType { get; set; }

        internal string Authority
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, "https://{0}/{1}/", Environment, TenantId ?? "common");
            }
        }

        internal SortedSet<string> ScopeSet
        {
            get
            {
                return Scopes.AsLowerCaseSortedSet();
            }
        }
        internal DateTimeOffset ExpiresOn
        {
            get
            {
                DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                return dtDateTime.AddSeconds(ExpiresOnUnixTimestamp).ToUniversalTime();
            }
        }

        internal MsalAccessTokenCacheKey GetKey()
        {
            return new MsalAccessTokenCacheKey(Environment, TenantId, HomeAccountId, ClientId, Scopes);
        }
        internal MsalIdTokenCacheKey GetIdTokenItemKey()
        {
            return new MsalIdTokenCacheKey(Environment, TenantId, HomeAccountId, ClientId);
        }
    }
}
