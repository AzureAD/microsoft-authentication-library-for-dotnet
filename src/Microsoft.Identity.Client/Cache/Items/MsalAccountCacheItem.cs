// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.Serialization;
using Microsoft.Identity.Client.Cache.Keys;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Json.Linq;

namespace Microsoft.Identity.Client.Cache.Items
{
    internal class MsalAccountCacheItem : MsalCacheItemBase
    {
        internal MsalAccountCacheItem()
        {
            AuthorityType = CacheAuthorityType.MSSTS.ToString();
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

        internal /* for test */ MsalAccountCacheItem(
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
        internal string PreferredUsername { get; set; }
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
            return new MsalAccountCacheKey(Environment, TenantId, HomeAccountId, PreferredUsername, AuthorityType);
        }

        internal static MsalAccountCacheItem FromJsonString(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

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
