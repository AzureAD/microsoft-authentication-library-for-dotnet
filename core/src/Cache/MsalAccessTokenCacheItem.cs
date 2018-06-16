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
            CredentialType = Cache.CredentialType.AccessToken.ToString();
        }

        internal MsalAccessTokenCacheItem
            (Authority authority, string clientId, MsalTokenResponse response, string tenantId){

            CredentialType = Cache.CredentialType.AccessToken.ToString();

            ClientId = clientId;
            TokenType = response.TokenType;
            Scopes = response.Scope;
            Authority = authority.CanonicalAuthority;

            TenantId = tenantId;
            Environment = authority.Host;

            Secret = response.AccessToken;
            ExpiresOnUnixTimestamp = CoreHelpers.DateTimeToUnixTimestamp(response.AccessTokenExpiresOn);
            CachedAt = CoreHelpers.CurrDateTimeInUnixTimestamp();

            RawClientInfo = response.ClientInfo;
            CreateDerivedProperties();
        }

        [DataMember(Name = "realm")]
        internal string TenantId { get; set; }

        [DataMember(Name = "target", IsRequired = true)]
        public string Scopes { get; internal set; }

        [DataMember(Name = "cached_at", IsRequired = true)]
        internal long CachedAt { get; set; }

        [DataMember(Name = "expires_on", IsRequired = true)]
        public long ExpiresOnUnixTimestamp { get; internal set; }

        /*
        [DataMember(Name = "extended_expires_on")]
        internal string ExtendedExpiresOn { get; set; }
        */

        [DataMember(Name = "user_assertion_hash")]
        public string UserAssertionHash { get; set; }

        [DataMember(Name = "authority")]
        public string Authority { get; internal set; }

        [DataMember(Name = "access_token_type")]
        internal string TokenType { get; set; }

        internal SortedSet<string> ScopeSet { get; set; }

        internal string GetAccessTokenItemKey()
        {
            return new MsalAccessTokenCacheKey(Environment, TenantId, UserIdentifier, ClientId, ScopeSet).ToString();
        }
        internal string GetIdTokenItemKey()
        {
            return new MsalIdTokenCacheKey(Environment, TenantId, UserIdentifier, ClientId).ToString();
        }

        public DateTimeOffset ExpiresOn
        {
            get
            {
                DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                return dtDateTime.AddSeconds(ExpiresOnUnixTimestamp).ToUniversalTime();
            }
        }

        internal void CreateDerivedProperties()
        {
            ScopeSet = Scopes.AsSet();

            InitRawClientInfoDerivedProperties();
        }

        // This method is called after the object 
        // is completely deserialized.
        [OnDeserialized]
        void OnDeserialized(StreamingContext context)
        {
            CreateDerivedProperties();
        }
    }
}
