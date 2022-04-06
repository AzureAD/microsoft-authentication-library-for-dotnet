// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Identity.Client.Cache.Keys;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Json;
using Microsoft.Identity.Json.Linq;

namespace Microsoft.Identity.Client.Cache.Items
{
    /// <summary>
    /// Example account json:
    /// 
    ///   "authority_type":"MSSTS",
    ///   "client_info":"",
    ///   "environment":"login.windows.net",
    ///   "family_name":"",
    ///   "given_name":"Some Name",
    ///   "home_account_id":"69c374a4-1df6-46f8-b83a-a2fcd8823ee2.49f548d0-12b7-4169-a390-bb5304d24462",
    ///   "local_account_id":"69c374a4-1df6-46f8-b83a-a2fcd8823ee2",
    ///   "middle_name":"",
    ///   "name":"Some Name",
    ///   "realm":"49f548d0-12b7-4169-a390-bb5304d24462",
    ///   "username":"subzero@bogavrilltd.onmicrosoft.com",
    ///   "wam_account_ids":"{\"00000000480FA373\":\"ob7b8h79td9gs6hfqoh2r37m\",\"4b0db8c2-9f26-4417-8bde-3f0e3656f8e0\":\"ob7b8h79td9gs6hfqoh2r37m\"}"
    ///
    /// </summary>
    [DebuggerDisplay("{PreferredUsername} {base.Environment}")]
    internal class MsalAccountCacheItem : MsalCacheItemBase
    {
        internal MsalAccountCacheItem()
        {
            AuthorityType = CacheAuthorityType.MSSTS.ToString();
        }

        internal MsalAccountCacheItem(
            string preferredCacheEnv,
            string clientInfo,
            string homeAccountId,
            IdToken idToken,
            string preferredUsername,
            string tenantId,
            IDictionary<string, string> wamAccountIds)
            : this()
        {
            Init(
                preferredCacheEnv,
                idToken?.ObjectId,
                clientInfo,
                homeAccountId,
                idToken?.Name,
                preferredUsername,
                tenantId,
                idToken?.GivenName,
                idToken?.FamilyName, 
                wamAccountIds);
        }

        internal /* for test */ MsalAccountCacheItem(
            string environment,
            string localAccountId,
            string rawClientInfo,
            string homeAccountId,
            string name,
            string preferredUsername,
            string tenantId,
            string givenName,
            string familyName, 
            IDictionary<string, string> wamAccountIds)
            : this()
        {
            Init(
                environment,
                localAccountId,
                rawClientInfo,
                homeAccountId,
                name,
                preferredUsername,
                tenantId,
                givenName,
                familyName,
                wamAccountIds);
        }

        internal string TenantId { get; set; }
        internal string PreferredUsername { get; set; }
        internal string Name { get; set; }
        internal string GivenName { get; set; }
        internal string FamilyName { get; set; }
        internal string LocalAccountId { get; set; }
        internal string AuthorityType { get; set; }

        /// <summary>
        /// WAM special implementation: MSA accounts (and also AAD accounts on UWP) cannot be discovered through WAM
        /// however the broker offers an interactive experience for the user to login, even with an MSA account.
        /// After an interactive login, MSAL must be able to silently login the MSA user. To do this, MSAL must save the 
        /// account ID in its token cache. Accounts with associated WAM account ID can be used in silent WAM flows.
        /// </summary>
        internal IDictionary<string, string> WamAccountIds { get; set; }

        private void Init(
            string environment,
            string localAccountId,
            string rawClientInfo,
            string homeAccountId,
            string name,
            string preferredUsername,
            string tenantId,
            string givenName,
            string familyName, 
            IDictionary<string, string> wamAccountIds)
        {
            Environment = environment;
            PreferredUsername = preferredUsername;
            Name = name;
            TenantId = tenantId;
            LocalAccountId = localAccountId;
            RawClientInfo = rawClientInfo;
            GivenName = givenName;
            FamilyName = familyName;
            HomeAccountId = homeAccountId;
            WamAccountIds = wamAccountIds;
        }

        internal MsalAccountCacheKey GetKey()
        {
            return new MsalAccountCacheKey(Environment, TenantId, HomeAccountId, PreferredUsername);
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
                WamAccountIds = JsonUtils.ExtractInnerJsonAsDictionary(j, StorageJsonKeys.WamAccountIds)
            };

            item.PopulateFieldsFromJObject(j);

            return item;
        }

        internal override JObject ToJObject()
        {
            var json = base.ToJObject();

            SetItemIfValueNotNull(json, StorageJsonKeys.Username, PreferredUsername);
            SetItemIfValueNotNull(json, StorageJsonKeys.Name, Name);
            SetItemIfValueNotNull(json, StorageJsonKeys.GivenName, GivenName);
            SetItemIfValueNotNull(json, StorageJsonKeys.FamilyName, FamilyName);
            SetItemIfValueNotNull(json, StorageJsonKeys.LocalAccountId, LocalAccountId);
            SetItemIfValueNotNull(json, StorageJsonKeys.AuthorityType, AuthorityType);
            SetItemIfValueNotNull(json, StorageJsonKeys.Realm, TenantId);
            if (WamAccountIds != null && WamAccountIds.Any())
            {
                json[StorageJsonKeys.WamAccountIds] = JObject.FromObject(WamAccountIds);                
            }

            return json;
        }

        internal string ToJsonString()
        {
            return ToJObject()
                .ToString();
        }
    }
}
