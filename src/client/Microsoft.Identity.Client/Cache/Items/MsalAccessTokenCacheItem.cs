// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Identity.Client.Cache.Keys;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Json.Linq;

namespace Microsoft.Identity.Client.Cache.Items
{
    internal class MsalAccessTokenCacheItem : MsalCredentialCacheItemBase
    {
        internal MsalAccessTokenCacheItem(
            string preferredCacheEnv,
            string clientId,
            MsalTokenResponse response,
            string tenantId,
            string homeAccountId,
            string keyId = null,
            string oboCacheKey = null)
            : this(
                scopes: response.Scope, // token providers send pre-sorted (alphabetically) scopes
                cachedAt: DateTimeOffset.UtcNow,
                expiresOn: DateTimeOffsetFromDuration(response.ExpiresIn),
                extendedExpiresOn: DateTimeOffsetFromDuration(response.ExtendedExpiresIn),
                refreshOn: DateTimeOffsetFromDuration(response.RefreshIn),
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

        private static DateTimeOffset? DateTimeOffsetFromDuration(long? duration)
        {
            if (duration.HasValue)
                return DateTimeOffsetFromDuration(duration.Value);

            return null;
        }

        private static DateTimeOffset DateTimeOffsetFromDuration(long duration)
        {
            return DateTime.UtcNow + TimeSpan.FromSeconds(duration);
        }

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

        internal static MsalAccessTokenCacheItem FromJsonString(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return FromJObject(JObject.Parse(json));
        }

        internal static MsalAccessTokenCacheItem FromJObject(JObject j)
        {
            long cachedAtUnixTimestamp = JsonUtils.ExtractParsedIntOrZero(j, StorageJsonKeys.CachedAt);
            long expiresOnUnixTimestamp = JsonUtils.ExtractParsedIntOrZero(j, StorageJsonKeys.ExpiresOn);
            long refreshOnUnixTimestamp = JsonUtils.ExtractParsedIntOrZero(j, StorageJsonKeys.RefreshOn);

            // This handles a bug with the name in previous MSAL.  It used "ext_expires_on" instead of
            // "extended_expires_on" per spec, so this works around that.
            long ext_expires_on = JsonUtils.ExtractParsedIntOrZero(j, StorageJsonKeys.ExtendedExpiresOn_MsalCompat);
            long extendedExpiresOnUnixTimestamp = JsonUtils.ExtractParsedIntOrZero(j, StorageJsonKeys.ExtendedExpiresOn);
            if (extendedExpiresOnUnixTimestamp == 0 && ext_expires_on > 0)
            {
                extendedExpiresOnUnixTimestamp = ext_expires_on;
            }
            string tenantId = JsonUtils.ExtractExistingOrEmptyString(j, StorageJsonKeys.Realm);
            string oboCacheKey = JsonUtils.ExtractExistingOrDefault<string>(j, StorageJsonKeys.UserAssertionHash);
            string keyId = JsonUtils.ExtractExistingOrDefault<string>(j, StorageJsonKeys.KeyId);
            string tokenType = JsonUtils.ExtractExistingOrDefault<string>(j, StorageJsonKeys.TokenType) ?? StorageJsonValues.TokenTypeBearer;
            string scopes = JsonUtils.ExtractExistingOrEmptyString(j, StorageJsonKeys.Target);

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

        internal MsalAccessTokenCacheKey GetKey()
        {
            return new MsalAccessTokenCacheKey(
                Environment,
                TenantId,
                HomeAccountId,
                ClientId,
                ScopeString, 
                TokenType);
        }

        internal MsalIdTokenCacheKey GetIdTokenItemKey()
        {
            return new MsalIdTokenCacheKey(Environment, TenantId, HomeAccountId, ClientId);
        }

        internal bool IsExpiredWithBuffer()
        {
            return ExpiresOn < DateTime.UtcNow + Constants.AccessTokenExpirationBuffer;
        }
    }
}
