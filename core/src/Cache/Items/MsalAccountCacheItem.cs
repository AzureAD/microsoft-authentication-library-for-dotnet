//----------------------------------------------------------------------
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

using Microsoft.Identity.Core.OAuth2;
using System.Runtime.Serialization;

namespace Microsoft.Identity.Core.Cache
{
    [DataContract]
    internal class MsalAccountCacheItem : MsalCacheItemBase
    {
        internal MsalAccountCacheItem(){
            AuthorityType = Cache.AuthorityType.MSSTS.ToString();
        }
        internal MsalAccountCacheItem(string environment, MsalTokenResponse response) : this()
        {
            IdToken idToken = IdToken.Parse(response.IdToken);

            Init(environment, idToken?.ObjectId, response.ClientInfo, idToken.Name, idToken.PreferredUsername, idToken.TenantId);
        }

        internal MsalAccountCacheItem(string environment, MsalTokenResponse response, string preferredUsername, string tenantID) : this()
        {
            IdToken idToken = IdToken.Parse(response.IdToken);

            Init(environment, idToken?.ObjectId, response.ClientInfo, idToken.Name, preferredUsername, tenantID);
        }

        internal MsalAccountCacheItem(string environment, string localAccountId, string rawClientInfo,
            string name, string preferredUsername, string tenantId) : this()
        {
            Init(environment, localAccountId, rawClientInfo, name, preferredUsername, tenantId);
        }

        private void Init(string environment, string localAccountId, string rawClientInfo, 
            string name, string preferredUsername, string tenantId)
        {
            Environment = environment;
            PreferredUsername = preferredUsername;
            Name = name;
            TenantId = tenantId;
            LocalAccountId = localAccountId;
            RawClientInfo = rawClientInfo;

            InitUserIdentifier();
        }

        [DataMember(Name = "realm")]
        internal string TenantId { get; set; }

        [DataMember(Name = "username")]
        public string PreferredUsername { get; internal set; }

        [DataMember(Name = "name")]
        internal string Name { get; set; }

        [DataMember(Name = "given_name")]
        internal string GivenName { get; set; }

        [DataMember(Name = "family_name")]
        internal string FamilyName { get; set; }

        [DataMember(Name = "local_account_id")]
        internal string LocalAccountId { get; set; }

        [DataMember(Name = "authority_type")]
        internal string AuthorityType { get; set; }

        internal MsalAccountCacheKey GetKey()
        {
            return new MsalAccountCacheKey(Environment, TenantId, HomeAccountId);
        }
    }
}
