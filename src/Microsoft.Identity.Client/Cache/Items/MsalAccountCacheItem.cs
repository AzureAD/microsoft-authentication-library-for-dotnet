// ------------------------------------------------------------------------------
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
// ------------------------------------------------------------------------------

using System.Runtime.Serialization;
using Microsoft.Identity.Client.CacheV2.Impl.Utils;
using Microsoft.Identity.Client.CacheV2.Schema;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Json.Linq;

namespace Microsoft.Identity.Client.Cache.Items
{
    [DataContract]
    internal class MsalAccountCacheItem : MsalCacheItemBase
    {
        internal MsalAccountCacheItem()
        {
            AuthorityType = Cache.AuthorityType.MSSTS.ToString();
        }

        internal MsalAccountCacheItem(string environment, MsalTokenResponse response)
            : this()
        {
            var idToken = IdToken.Parse(response.IdToken);

            Init(
                environment,
                idToken?.ObjectId,
                response.ClientInfo,
                idToken.Name,
                idToken.PreferredUsername,
                idToken.TenantId,
                idToken.GivenName,
                idToken.FamilyName);
        }

        internal MsalAccountCacheItem(
            string environment,
            MsalTokenResponse response,
            string preferredUsername,
            string tenantId)
            : this()
        {
            var idToken = IdToken.Parse(response.IdToken);

            Init(
                environment,
                idToken?.ObjectId,
                response.ClientInfo,
                idToken.Name,
                preferredUsername,
                tenantId,
                idToken.GivenName,
                idToken.FamilyName);
        }

        internal MsalAccountCacheItem(
            string environment,
            string localAccountId,
            string rawClientInfo,
            string name,
            string preferredUsername,
            string tenantId,
            string givenName,
            string familyName)
            : this()
        {
            Init(
                environment,
                localAccountId,
                rawClientInfo,
                name,
                preferredUsername,
                tenantId,
                givenName,
                familyName);
        }

        internal string TenantId { get; set; }
        public string PreferredUsername { get; internal set; }
        internal string Name { get; set; }
        internal string GivenName { get; set; }
        internal string FamilyName { get; set; }
        internal string LocalAccountId { get; set; }
        internal string AuthorityType { get; set; }

        private void Init(
            string environment,
            string localAccountId,
            string rawClientInfo,
            string name,
            string preferredUsername,
            string tenantId,
            string givenName,
            string familyName)
        {
            Environment = environment;
            PreferredUsername = preferredUsername;
            Name = name;
            TenantId = tenantId;
            LocalAccountId = localAccountId;
            RawClientInfo = rawClientInfo;
            GivenName = givenName;
            FamilyName = familyName;

            InitUserIdentifier();
        }

        internal MsalAccountCacheKey GetKey()
        {
            return new MsalAccountCacheKey(Environment, TenantId, HomeAccountId, PreferredUsername);
        }

        internal static MsalAccountCacheItem FromJsonString(string json)
        {
            return FromJObject(JObject.Parse(json));
        }

        internal static MsalAccountCacheItem FromJObject(JObject j)
        {
            var item = new MsalAccountCacheItem
            {
                PreferredUsername = JsonUtils.ExtractExistingOrEmptyString(j, StorageJsonKeys.Username),
                Name = JsonUtils.ExtractExistingOrEmptyString(j, StorageJsonKeys.Name),
                GivenName = JsonUtils.ExtractExistingOrEmptyString(j, StorageJsonKeys.GivenName),
                FamilyName = JsonUtils.ExtractExistingOrEmptyString(j, StorageJsonKeys.FamilyName),
                LocalAccountId = JsonUtils.ExtractExistingOrEmptyString(j, StorageJsonKeys.LocalAccountId),
                AuthorityType = JsonUtils.ExtractExistingOrEmptyString(j, StorageJsonKeys.AuthorityType),
                TenantId = JsonUtils.ExtractExistingOrEmptyString(j, StorageJsonKeys.Realm),
            };

            item.PopulateFieldsFromJObject(j);

            return item;
        }

        internal override JObject ToJObject()
        {
            var json = base.ToJObject();

            json[StorageJsonKeys.Username] = PreferredUsername;
            json[StorageJsonKeys.Name] = Name;
            json[StorageJsonKeys.GivenName] = GivenName;
            json[StorageJsonKeys.FamilyName] = FamilyName;
            // todo(cache): we don't support middle name json[StorageJsonKeys.MiddleName] = MiddleName;
            json[StorageJsonKeys.LocalAccountId] = LocalAccountId;
            json[StorageJsonKeys.AuthorityType] = AuthorityType;
            json[StorageJsonKeys.Realm] = TenantId;

            return json;
        }

        internal string ToJsonString()
        {
            return ToJObject()
                .ToString();
        }
    }
}