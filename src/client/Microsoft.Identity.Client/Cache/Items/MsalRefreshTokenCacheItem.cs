// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Identity.Client.Cache.Keys;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;
using JObject = System.Text.Json.Nodes.JsonObject;

namespace Microsoft.Identity.Client.Cache.Items
{
    internal class MsalRefreshTokenCacheItem : MsalCredentialCacheItemBase
    {
        internal MsalRefreshTokenCacheItem()
        {
            CredentialType = StorageJsonValues.CredentialTypeRefreshToken;
        }

        internal MsalRefreshTokenCacheItem(
            string preferredCacheEnv,
            string clientId,
            MsalTokenResponse response,
            string homeAccountId,
            SortedList<string, string> cacheKeyComponents = null)
            : this(
                  preferredCacheEnv,
                  clientId,
                  response.RefreshToken,
                  response.ClientInfo,
                  response.FamilyId,
                  homeAccountId,
                  cacheKeyComponents: cacheKeyComponents)
        {
        }

        internal MsalRefreshTokenCacheItem(
            string preferredCacheEnv,
            string clientId,
            string secret,
            string rawClientInfo,
            string familyId,
            string homeAccountId,
            SortedList<string, string> cacheKeyComponents = null)
            : this()
        {
            ClientId = clientId;
            Environment = preferredCacheEnv;
            Secret = secret;
            RawClientInfo = rawClientInfo;
            FamilyId = familyId;
            HomeAccountId = homeAccountId;

            InitializeAdditionalCacheKeyComponents(cacheKeyComponents);
            InitCacheKey();
        }

        //Internal for test
        internal void InitCacheKey()
        {
            string key;
            // FRT
            if (!string.IsNullOrWhiteSpace(FamilyId))
            {
                char d = MsalCacheKeys.CacheKeyDelimiter;
                key = $"{HomeAccountId}{d}{Environment}{d}{StorageJsonValues.CredentialTypeRefreshToken}{d}{FamilyId}{d}{d}".ToLowerInvariant();

            }
            else
            {
                string credentialDescriptor = StorageJsonValues.CredentialTypeRefreshToken;
                string[] extraKeyParts = null;

                if (AdditionalCacheKeyComponents != null && AdditionalCacheKeyComponents.Any())
                {
                    credentialDescriptor = StorageJsonValues.CredentialTypeRefreshTokenExtended;
                    extraKeyParts = new[] { CoreHelpers.ComputeAccessTokenExtCacheKey(AdditionalCacheKeyComponents) };
                }

                key = MsalCacheKeys.GetCredentialKey(
                       HomeAccountId,
                       Environment,
                       credentialDescriptor,
                       ClientId,
                       tenantId: null,
                       scopes: null,
                       extraKeyParts);
            }

            CacheKey = key;

            iOSCacheKeyLazy = new Lazy<IiOSKey>(() => InitiOSKey());
        }

        private void InitializeAdditionalCacheKeyComponents(SortedList<string, string> cacheKeyComponents)
        {
            if (cacheKeyComponents != null && cacheKeyComponents.Any())
            {
                AdditionalCacheKeyComponents = cacheKeyComponents;
                CredentialType = StorageJsonValues.CredentialTypeRefreshTokenExtended;
            }
        }

        internal string ToLogString(bool piiEnabled = false)
        {
            return MsalCacheKeys.GetCredentialKey(
                piiEnabled ? HomeAccountId : HomeAccountId?.GetHashCode().ToString(),
                Environment,
                StorageJsonValues.CredentialTypeRefreshToken,
                ClientId,
                tenantId: null,
                scopes: null);
        }

        #region iOS
        private IiOSKey InitiOSKey()
        {
            string iOSService = GetiOSService();

            string iOSGeneric = GetiOSGeneric();

            string iOSAccount = MsalCacheKeys.GetiOSAccountKey(HomeAccountId, Environment);

            int iOSType = (int)MsalCacheKeys.iOSCredentialAttrType.RefreshToken;

            return new IosKey(iOSAccount, iOSService, iOSGeneric, iOSType);
        }

        private string GetiOSGeneric()
        {

            // FRT
            if (!string.IsNullOrWhiteSpace(FamilyId))
            {
                return $"{StorageJsonValues.CredentialTypeRefreshToken}{MsalCacheKeys.CacheKeyDelimiter}{FamilyId}{MsalCacheKeys.CacheKeyDelimiter}".ToLowerInvariant();
            }

            return MsalCacheKeys.GetiOSGenericKey(StorageJsonValues.CredentialTypeRefreshToken, ClientId, tenantId: null);

        }

        public string GetiOSService()
        {
            // FRT
            if (!string.IsNullOrWhiteSpace(FamilyId))
            {
                return $"{StorageJsonValues.CredentialTypeRefreshToken}{MsalCacheKeys.CacheKeyDelimiter}{FamilyId}{MsalCacheKeys.CacheKeyDelimiter}{MsalCacheKeys.CacheKeyDelimiter}".ToLowerInvariant();
            }

            return MsalCacheKeys.GetiOSServiceKey(StorageJsonValues.CredentialTypeRefreshToken, ClientId, tenantId: null, scopes: null);
        }

        #endregion

        /// <summary>
        /// Optional. A value here means the token in an FRT.
        /// </summary>
        public string FamilyId { get; set; }

        /// <summary>
        /// Used to find the token in the cache.
        /// Can be a token assertion hash (normal OBO flow) or a user provided key (long-running OBO flow).
        /// </summary>
        internal string OboCacheKey { get; set; }

        /// <summary>
        /// Family Refresh Tokens, can be used for all clients part of the family
        /// </summary>
        public bool IsFRT => !string.IsNullOrEmpty(FamilyId);

        public string CacheKey { get; private set; }

        internal SortedList<string, string> AdditionalCacheKeyComponents { get; private set; }

        private Lazy<IiOSKey> iOSCacheKeyLazy;
        public IiOSKey iOSCacheKey => iOSCacheKeyLazy.Value;

        internal static MsalRefreshTokenCacheItem FromJsonString(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return FromJObject(JsonHelper.ParseIntoJsonObject(json));
        }

        internal static MsalRefreshTokenCacheItem FromJObject(JObject j)
        {
            var item = new MsalRefreshTokenCacheItem();
            item.FamilyId = JsonHelper.ExtractExistingOrEmptyString(j, StorageJsonKeys.FamilyId);
            item.OboCacheKey = JsonHelper.ExtractExistingOrEmptyString(j, StorageJsonKeys.UserAssertionHash);

            var additionalCacheKeyComponents = JsonHelper.ExtractInnerJsonAsDictionary(j, StorageJsonKeys.CacheExtensions);
            if (additionalCacheKeyComponents != null)
            {
                item.AdditionalCacheKeyComponents = new SortedList<string, string>(additionalCacheKeyComponents);
                item.CredentialType = StorageJsonValues.CredentialTypeRefreshTokenExtended;
            }

            item.PopulateFieldsFromJObject(j);
            item.InitCacheKey();

            return item;
        }

        internal override JObject ToJObject()
        {
            var json = base.ToJObject();
            SetItemIfValueNotNull(json, StorageJsonKeys.FamilyId, FamilyId);
            SetItemIfValueNotNull(json, StorageJsonKeys.UserAssertionHash, OboCacheKey);

            if (AdditionalCacheKeyComponents != null)
            {
                var obj = new System.Text.Json.Nodes.JsonObject();
                foreach (KeyValuePair<string, string> value in AdditionalCacheKeyComponents)
                {
                    obj[value.Key] = value.Value;
                }
                json[StorageJsonKeys.CacheExtensions] = obj;
            }

            return json;
        }

        internal string ToJsonString()
        {
            return ToJObject().ToString();
        }
    }
}
