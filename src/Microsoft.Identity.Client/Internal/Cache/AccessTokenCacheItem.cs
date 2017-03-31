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
using Microsoft.Identity.Client.Internal.OAuth2;

namespace Microsoft.Identity.Client.Internal.Cache
{
    [DataContract]
    internal class AccessTokenCacheItem : BaseTokenCacheItem
    {
        public AccessTokenCacheItem()
        {
        }

        public AccessTokenCacheItem(string authority, string clientId, TokenResponse response)
            : base(clientId)
        {
            if (response.AccessToken != null)
            {
                AccessToken = response.AccessToken;
                ExpiresOnUnixTimestamp = MsalHelpers.DateTimeToUnixTimestamp(response.AccessTokenExpiresOn);
            }

            RawClientInfo = response.ClientInfo;
            RawIdToken = response.IdToken;
            IdToken = IdToken.Parse(response.IdToken);
            ClientInfo = ClientInfo.Parse(response.ClientInfo);
            if (IdToken != null)
            {
                User = User.Create(IdToken.PreferredUsername, IdToken.Name, IdToken.Issuer,
                    GetUserIdentifier());
            }

            TokenType = response.TokenType;
            Scope = response.Scope;
            ScopeSet = response.Scope.AsSet();
            Authority = authority;
        }

        /// <summary>
        /// Gets the AccessToken Type.
        /// </summary>
        [DataMember(Name = "token_type")]
        public string TokenType { get; set; }

        /// <summary>
        /// Gets the Access Token requested.
        /// </summary>
        [DataMember(Name = "access_token")]
        public string AccessToken { get; set; }

        [DataMember(Name = "id_token")]
        public string RawIdToken { get; set; }

        [DataMember(Name = "client_info")]
        public string RawClientInfo { get; set; }

        [DataMember(Name = "expires_on")]
        public long ExpiresOnUnixTimestamp { get; set; }

        /// <summary>
        /// Gets the Authority.
        /// </summary>
        [DataMember(Name = "authority")]
        public string Authority { get; set; }

        /// <summary>
        /// Gets the ScopeSet.
        /// </summary>
        [DataMember(Name = "scope")]
        public string Scope { get; set; }
        
        public SortedSet<string> ScopeSet { get; set; }

        [DataMember(Name = "user_assertion_hash")]
        public string UserAssertionHash { get; set; }

        public IdToken IdToken { get; set; }

        public ClientInfo ClientInfo { get; set; }

        public DateTimeOffset ExpiresOn
        {
            get
            {
                DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                return dtDateTime.AddSeconds(ExpiresOnUnixTimestamp).ToUniversalTime();
            }
            set
            {
                DateTimeOffset ignored = value;
            }
        }

        public AccessTokenCacheKey GetAccessTokenItemKey()
        {
            return new AccessTokenCacheKey(Authority, ScopeSet, ClientId, GetUserIdentifier());
        }

        public sealed override string GetUserIdentifier()
        {
            string Uid;
            string Utid;

            if (ClientInfo == null && IdToken == null)
            {
                return null;
            }

            if (ClientInfo != null)
            {
                Uid = ClientInfo.UniqueIdentifier;
                Utid = ClientInfo.UniqueTenantIdentifier;
            }
            else
            {
                Uid = IdToken.GetUniqueId();
                Utid = IdToken.TenantId;
            }

            return string.Format(CultureInfo.InvariantCulture, "{0}.{1}", MsalHelpers.EncodeToBase64Url(Uid),
                MsalHelpers.EncodeToBase64Url(Utid));
        }

        // This method is called after the object 
        // is completely deserialized.
        [OnDeserialized]
        void OnDeserialized(StreamingContext context)
        {
            ScopeSet = Scope.AsSet();
            IdToken = IdToken.Parse(RawIdToken);
            ClientInfo = ClientInfo.Parse(RawClientInfo);
            if (IdToken != null)
            {
                User = User.Create(IdToken.PreferredUsername, IdToken.Name, IdToken.Issuer,
                    GetUserIdentifier());
            }
        }
    }
}
