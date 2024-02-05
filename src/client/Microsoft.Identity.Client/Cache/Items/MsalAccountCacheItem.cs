// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using Microsoft.Identity.Client.Cache.Keys;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Utils;
#if SUPPORTS_SYSTEM_TEXT_JSON
using System.Text.Json.Nodes;
using JObject = System.Text.Json.Nodes.JsonObject;
#else
using Microsoft.Identity.Json.Linq;
#endif

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
                idToken?.GetUniqueId(),
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

        internal MsalAccountCacheItem(
            string environment, 
            string tenantId, 
            string homeAccountId, 
            string preferredUsername)
            : this()
        {
            Environment = environment;
            TenantId = tenantId;
            HomeAccountId = homeAccountId;
            PreferredUsername = preferredUsername;

            InitCacheKey();
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
        public string CacheKey { get; private set; }

        private Lazy<IiOSKey> iOSCacheKeyLazy;
        public IiOSKey iOSCacheKey => iOSCacheKeyLazy.Value;

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

            InitCacheKey();
        }

        internal void InitCacheKey()
        {
            CacheKey =  $"{HomeAccountId}{MsalCacheKeys.CacheKeyDelimiter}{Environment}{MsalCacheKeys.CacheKeyDelimiter}{TenantId}";

            iOSCacheKeyLazy = new Lazy<IiOSKey>(InitiOSKey);
        }

        #region iOS

        private IiOSKey InitiOSKey()
        {
            string iOSAccount = MsalCacheKeys.GetiOSAccountKey(HomeAccountId, Environment);

            string iOSService = (TenantId ?? "").ToLowerInvariant();

            string iOSGeneric = PreferredUsername?.ToLowerInvariant();

            // This is a known issue.
            // Normally AuthorityType should be passed here but since while building the MsalAccountCacheItem it is defaulted to "MSSTS",
            // keeping the default value here.
            int iOSType = MsalCacheKeys.iOSAuthorityTypeToAttrType[CacheAuthorityType.MSSTS.ToString()];

            return new IosKey(iOSAccount, iOSService, iOSGeneric, iOSType);
        }

        #endregion

        internal static MsalAccountCacheItem FromJsonString(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return FromJObject(JsonHelper.ParseIntoJsonObject(json));
        }

        internal static MsalAccountCacheItem FromJObject(JObject j)
        {
            var item = new MsalAccountCacheItem
            {
                PreferredUsername = JsonHelper.ExtractExistingOrEmptyString(j, StorageJsonKeys.Username),
                Name = JsonHelper.ExtractExistingOrEmptyString(j, StorageJsonKeys.Name),
                GivenName = JsonHelper.ExtractExistingOrEmptyString(j, StorageJsonKeys.GivenName),
                FamilyName = JsonHelper.ExtractExistingOrEmptyString(j, StorageJsonKeys.FamilyName),
                LocalAccountId = JsonHelper.ExtractExistingOrEmptyString(j, StorageJsonKeys.LocalAccountId),
                AuthorityType = JsonHelper.ExtractExistingOrEmptyString(j, StorageJsonKeys.AuthorityType),
                TenantId = JsonHelper.ExtractExistingOrEmptyString(j, StorageJsonKeys.Realm),
                WamAccountIds = JsonHelper.ExtractInnerJsonAsDictionary(j, StorageJsonKeys.WamAccountIds)
            };

            item.PopulateFieldsFromJObject(j);

            item.InitCacheKey();

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
#if SUPPORTS_SYSTEM_TEXT_JSON
                var obj = new JsonObject();

                foreach (KeyValuePair<string, string> accId in WamAccountIds)
                {
                    obj[accId.Key] = accId.Value;
                }

                json[StorageJsonKeys.WamAccountIds] = obj;
#else
                json[StorageJsonKeys.WamAccountIds] = JObject.FromObject(WamAccountIds);
#endif
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
