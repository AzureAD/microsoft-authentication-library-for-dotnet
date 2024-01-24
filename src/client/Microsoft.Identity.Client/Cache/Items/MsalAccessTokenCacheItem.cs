// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.AuthScheme;
using Microsoft.Identity.Client.Cache.Keys;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;
#if SUPPORTS_SYSTEM_TEXT_JSON
using JObject = System.Text.Json.Nodes.JsonObject;
#else
using Microsoft.Identity.Json.Linq;
#endif

namespace Microsoft.Identity.Client.Cache.Items
{
    internal class MsalAccessTokenCacheItem : MsalCredentialCacheItemBase
    {
        private string[] _extraKeyParts;
        private string _credentialDescriptor;

        internal MsalAccessTokenCacheItem(
            string preferredCacheEnv,
            string clientId,
            MsalTokenResponse response,
            string tenantId,
            string homeAccountId,
            string keyId = null,
            string oboCacheKey = null)
            : this(
                scopes: ScopeHelper.OrderScopesAlphabetically(response.Scope), // order scopes to avoid cache duplication. This is not in the hot path.
                cachedAt: DateTimeOffset.UtcNow,
                expiresOn: DateTimeHelpers.DateTimeOffsetFromDuration(response.ExpiresIn),
                extendedExpiresOn: DateTimeHelpers.DateTimeOffsetFromDuration(response.ExtendedExpiresIn),
                refreshOn: DateTimeHelpers.DateTimeOffsetFromDuration(response.RefreshIn),
                tenantId: tenantId,
                keyId: keyId,
                tokenType: response.TokenType)
        {
            Environment = preferredCacheEnv;
            ClientId = clientId;
            Secret = response.AccessToken;
            RawClientInfo = response.ClientInfo;
            HomeAccountId = homeAccountId;
            OboCacheKey = oboCacheKey;

            InitCacheKey();
        }

      
        internal /* for test */ MsalAccessTokenCacheItem(
            string preferredCacheEnv,
            string clientId,
            string scopes,
            string tenantId,
            string secret,
            DateTimeOffset cachedAt,
            DateTimeOffset expiresOn,
            DateTimeOffset extendedExpiresOn,
            string rawClientInfo,
            string homeAccountId,
            string keyId = null,
            DateTimeOffset? refreshOn = null,
            string tokenType = StorageJsonValues.TokenTypeBearer,
            string oboCacheKey = null)
            : this(scopes, cachedAt, expiresOn, extendedExpiresOn, refreshOn, tenantId, keyId, tokenType)
        {
            Environment = preferredCacheEnv;
            ClientId = clientId;
            Secret = secret;
            RawClientInfo = rawClientInfo;
            HomeAccountId = homeAccountId;
            OboCacheKey = oboCacheKey;

            InitCacheKey();
        }

        private MsalAccessTokenCacheItem(
            string scopes,
            DateTimeOffset cachedAt,
            DateTimeOffset expiresOn,
            DateTimeOffset extendedExpiresOn,
            DateTimeOffset? refreshOn,
            string tenantId,
            string keyId,
            string tokenType)
        {
            CredentialType = StorageJsonValues.CredentialTypeAccessToken;

            ScopeString = scopes;
            ScopeSet = ScopeHelper.ConvertStringToScopeSet(ScopeString);

            ExpiresOn = expiresOn;
            ExtendedExpiresOn = extendedExpiresOn;
            RefreshOn = refreshOn;

            TenantId = tenantId ?? "";
            KeyId = keyId;
            TokenType = tokenType;

            CachedAt = cachedAt;
        }

        /// <summary>
        /// Creates a new object with a different expires on
        /// </summary>
        internal MsalAccessTokenCacheItem WithExpiresOn(DateTimeOffset expiresOn)
        {
            MsalAccessTokenCacheItem newAtItem = new MsalAccessTokenCacheItem(
               Environment,
               ClientId,
               ScopeString,
               TenantId,
               Secret,
               CachedAt,
               expiresOn,
               ExtendedExpiresOn,
               RawClientInfo,
               HomeAccountId,
               KeyId,
               RefreshOn,
               TokenType,
               OboCacheKey);

            return newAtItem;
        }

        //internal for test
        internal void InitCacheKey()
        {
            _extraKeyParts = null;
            _credentialDescriptor = StorageJsonValues.CredentialTypeAccessToken;

            if (AuthSchemeHelper.StoreTokenTypeInCacheKey(TokenType))
            {
                _extraKeyParts = new[] { TokenType };
                _credentialDescriptor = StorageJsonValues.CredentialTypeAccessTokenWithAuthScheme;
            }

            CacheKey = MsalCacheKeys.GetCredentialKey(
                HomeAccountId,
                Environment,
                _credentialDescriptor,
                ClientId,
                TenantId,
                ScopeString,
                _extraKeyParts);

            iOSCacheKeyLazy = new Lazy<IiOSKey>(InitiOSKey);
        }

        internal string ToLogString(bool piiEnabled = false)
        {
            return MsalCacheKeys.GetCredentialKey(
                piiEnabled ? HomeAccountId : HomeAccountId?.GetHashCode().ToString(),
                Environment,
                _credentialDescriptor,
                ClientId,
                TenantId,
                ScopeString,
                _extraKeyParts);
        }

        #region iOS

        private IiOSKey InitiOSKey()
        {
            string iOSAccount = MsalCacheKeys.GetiOSAccountKey(HomeAccountId, Environment);

            string iOSService = MsalCacheKeys.GetiOSServiceKey(_credentialDescriptor, ClientId, TenantId, ScopeString, _extraKeyParts);

            string iOSGeneric = MsalCacheKeys.GetiOSGenericKey(_credentialDescriptor, ClientId, TenantId);

            int iOSType = (int)MsalCacheKeys.iOSCredentialAttrType.AccessToken;

            return new IosKey(iOSAccount, iOSService, iOSGeneric, iOSType);
        }

        #endregion

        internal string TenantId
        {
            get; private set;
        }

        /// <summary>
        /// Used to find the token in the cache. 
        /// Can be a token assertion hash (normal OBO flow) or a user provided key (long-running OBO flow).
        /// </summary>
        internal string OboCacheKey { get; set; }

        /// <summary>
        /// Used when the token is bound to a public / private key pair which is identified by a key id (kid). 
        /// Currently used by PoP tokens
        /// </summary>
        internal string KeyId { get; }

        internal string TokenType { get; }

        internal HashSet<string> ScopeSet { get; }

        internal string ScopeString { get; }

        internal DateTimeOffset ExpiresOn { get; private set; }

        internal DateTimeOffset ExtendedExpiresOn { get; private set; }

        internal DateTimeOffset? RefreshOn { get; private set; }

        internal DateTimeOffset CachedAt { get; private set; }

        public bool IsExtendedLifeTimeToken { get; set; }

        internal string CacheKey { get; private set; }

        private Lazy<IiOSKey> iOSCacheKeyLazy;
        public IiOSKey iOSCacheKey => iOSCacheKeyLazy.Value;

        internal static MsalAccessTokenCacheItem FromJsonString(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return FromJObject(JsonHelper.ParseIntoJsonObject(json));
        }

        internal static MsalAccessTokenCacheItem FromJObject(JObject j)
        {
            long cachedAtUnixTimestamp = JsonHelper.ExtractParsedIntOrZero(j, StorageJsonKeys.CachedAt);
            long expiresOnUnixTimestamp = JsonHelper.ExtractParsedIntOrZero(j, StorageJsonKeys.ExpiresOn);
            long refreshOnUnixTimestamp = JsonHelper.ExtractParsedIntOrZero(j, StorageJsonKeys.RefreshOn);

            // This handles a bug with the name in previous MSAL.  It used "ext_expires_on" instead of
            // "extended_expires_on" per spec, so this works around that.
            long ext_expires_on = JsonHelper.ExtractParsedIntOrZero(j, StorageJsonKeys.ExtendedExpiresOn_MsalCompat);
            long extendedExpiresOnUnixTimestamp = JsonHelper.ExtractParsedIntOrZero(j, StorageJsonKeys.ExtendedExpiresOn);
            if (extendedExpiresOnUnixTimestamp == 0 && ext_expires_on > 0)
            {
                extendedExpiresOnUnixTimestamp = ext_expires_on;
            }
            string tenantId = JsonHelper.ExtractExistingOrEmptyString(j, StorageJsonKeys.Realm);
            string oboCacheKey = JsonHelper.ExtractExistingOrDefault<string>(j, StorageJsonKeys.UserAssertionHash);
            string keyId = JsonHelper.ExtractExistingOrDefault<string>(j, StorageJsonKeys.KeyId);
            string tokenType = JsonHelper.ExtractExistingOrDefault<string>(j, StorageJsonKeys.TokenType) ?? StorageJsonValues.TokenTypeBearer;
            string scopes = JsonHelper.ExtractExistingOrEmptyString(j, StorageJsonKeys.Target);

            var item = new MsalAccessTokenCacheItem(
                scopes: scopes,
                expiresOn: DateTimeHelpers.UnixTimestampToDateTime(expiresOnUnixTimestamp),
                extendedExpiresOn: DateTimeHelpers.UnixTimestampToDateTime(extendedExpiresOnUnixTimestamp),
                refreshOn: DateTimeHelpers.UnixTimestampToDateTimeOrNull(refreshOnUnixTimestamp),
                cachedAt: DateTimeHelpers.UnixTimestampToDateTime(cachedAtUnixTimestamp),
                tenantId: tenantId,
                keyId: keyId,
                tokenType: tokenType);

            item.OboCacheKey = oboCacheKey;
            item.PopulateFieldsFromJObject(j);

            item.InitCacheKey();

            return item;
        }

        internal override JObject ToJObject()
        {
            var json = base.ToJObject();

            var extExpiresUnixTimestamp = DateTimeHelpers.DateTimeToUnixTimestamp(ExtendedExpiresOn);
            SetItemIfValueNotNull(json, StorageJsonKeys.Realm, TenantId);
            SetItemIfValueNotNull(json, StorageJsonKeys.Target, ScopeString);
            SetItemIfValueNotNull(json, StorageJsonKeys.UserAssertionHash, OboCacheKey);
            SetItemIfValueNotNull(json, StorageJsonKeys.CachedAt, DateTimeHelpers.DateTimeToUnixTimestamp(CachedAt));
            SetItemIfValueNotNull(json, StorageJsonKeys.ExpiresOn, DateTimeHelpers.DateTimeToUnixTimestamp(ExpiresOn));
            SetItemIfValueNotNull(json, StorageJsonKeys.ExtendedExpiresOn, extExpiresUnixTimestamp);
            SetItemIfValueNotNull(json, StorageJsonKeys.KeyId, KeyId);
            SetItemIfValueNotNullOrDefault(json, StorageJsonKeys.TokenType, TokenType, StorageJsonValues.TokenTypeBearer);
            SetItemIfValueNotNull(
                json,
                StorageJsonKeys.RefreshOn,
                RefreshOn.HasValue ? DateTimeHelpers.DateTimeToUnixTimestamp(RefreshOn.Value) : null);

            // previous versions of MSAL used "ext_expires_on" instead of the correct "extended_expires_on".
            // this is here for back compatibility
            SetItemIfValueNotNull(json, StorageJsonKeys.ExtendedExpiresOn_MsalCompat, extExpiresUnixTimestamp);

            return json;
        }

        internal string ToJsonString()
        {
            return ToJObject().ToString();
        }

        internal MsalIdTokenCacheItem GetIdTokenItem()
        {
            return new MsalIdTokenCacheItem(Environment, ClientId, Secret, RawClientInfo, HomeAccountId, TenantId);
        }

        internal bool IsExpiredWithBuffer()
        {
            return ExpiresOn < DateTime.UtcNow + Constants.AccessTokenExpirationBuffer;
        }
    }
}
