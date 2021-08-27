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
            string keyId = null)
            : this(
                preferredCacheEnv,
                clientId,
                response.Scope, // token providers send pre-sorted (alphabetically) scopes
                tenantId,
                response.AccessToken,
                response.AccessTokenExpiresOn,
                response.AccessTokenExtendedExpiresOn,
                response.ClientInfo,
                homeAccountId,
                keyId,
                response.AccessTokenRefreshOn,
                response.TokenType)
        {
        }

        internal /* for test only */ MsalAccessTokenCacheItem(string scopes)
        {
            CredentialType = StorageJsonValues.CredentialTypeAccessToken;
            _scopes = scopes;
            ScopeSet = ScopeHelper.ConvertStringToScopeSet(_scopes);
        }

        internal /* for test only */ MsalAccessTokenCacheItem(
            string preferredCacheEnv,
            string clientId,
            string scopes,
            string tenantId,
            string secret,
            DateTimeOffset accessTokenExpiresOn,
            DateTimeOffset accessTokenExtendedExpiresOn,
            string rawClientInfo,
            string homeAccountId,
            string keyId = null,
            DateTimeOffset? accessTokenRefreshOn = null,
            string tokenType = StorageJsonValues.TokenTypeBearer) : this(scopes)
        {
            Environment = preferredCacheEnv;
            ClientId = clientId;
            TenantId = tenantId;
            Secret = secret;
            ExpiresOnUnixTimestamp = CoreHelpers.DateTimeToUnixTimestamp(accessTokenExpiresOn);
            ExtendedExpiresOnUnixTimestamp = CoreHelpers.DateTimeToUnixTimestamp(accessTokenExtendedExpiresOn);
            CachedAt = CoreHelpers.CurrDateTimeInUnixTimestamp().ToString(CultureInfo.InvariantCulture);
            RawClientInfo = rawClientInfo;
            KeyId = keyId;
            TokenType = tokenType;

            if (accessTokenRefreshOn.HasValue)
            {
                RefreshOnUnixTimestamp = CoreHelpers.DateTimeToUnixTimestamp(accessTokenRefreshOn.Value);
            }

            HomeAccountId = homeAccountId;
            AddJitterToTokenRefreshOn();
        }        

        private string _tenantId;

        internal string TenantId
        {
            get => _tenantId;
            set => _tenantId = value ?? string.Empty;
        }

        private string _scopes;

        internal string CachedAt { get; set; }
        internal string ExpiresOnUnixTimestamp { get; set; }
        internal string ExtendedExpiresOnUnixTimestamp { get; set; }

        /// <summary>
        /// Used to find the token in the cache. Can be a token assertion hash or a user provided key.
        /// </summary>
        internal string OboCacheKey { get; set; }

        /// <summary>
        /// Used when the token is bound to a public / private key pair which is identified by a key id (kid). 
        /// Currently used by PoP tokens
        /// </summary>
        internal string KeyId { get; set; }

        internal string TokenType { get; set; }

        /// <summary>
        /// If set, AT should be refreshed earlier to make sure the token cache
        /// always has ATs with a long life. 
        /// </summary>
        internal string RefreshOnUnixTimestamp { get; set; }

        // BUGBUG: this is wrong - it isn't persisted to the cache and is never recalculated
        // so when reading a token from the cache, IsAdfs is always false.
        // (also logically this is a bad place for it)
        internal bool IsAdfs { get; set; }

        internal string Authority =>
                                    IsAdfs ? string.Format(CultureInfo.InvariantCulture, "https://{0}/{1}/", Environment, "adfs") :
                                    string.Format(CultureInfo.InvariantCulture, "https://{0}/{1}/", Environment, TenantId ?? "common");

        internal HashSet<string> ScopeSet
        {
            get;
        }

        internal DateTimeOffset ExpiresOn => CoreHelpers.UnixTimestampStringToDateTime(ExpiresOnUnixTimestamp);
        internal DateTimeOffset ExtendedExpiresOn => CoreHelpers.UnixTimestampStringToDateTime(ExtendedExpiresOnUnixTimestamp);
        internal DateTimeOffset? RefreshOn
        {
            get
            {
                return !string.IsNullOrEmpty(RefreshOnUnixTimestamp) ?
                    CoreHelpers.UnixTimestampStringToDateTime(RefreshOnUnixTimestamp):
                    (DateTimeOffset?)null;
            }
        }

        internal DateTimeOffset CachedAtOffset => CoreHelpers.UnixTimestampStringToDateTime(CachedAt);

        public bool IsExtendedLifeTimeToken { get; set; }

        private void AddJitterToTokenRefreshOn()
        {
            if (!string.IsNullOrEmpty(RefreshOnUnixTimestamp))
            {
                Random r = new Random();
                int jitter = r.Next(-Constants.DefaultJitterRangeInSeconds, Constants.DefaultJitterRangeInSeconds);
                RefreshOnUnixTimestamp = (Convert.ToInt64(RefreshOnUnixTimestamp, CultureInfo.InvariantCulture) - jitter).ToString();
            }
        }

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
            long cachedAt = JsonUtils.ExtractParsedIntOrZero(j, StorageJsonKeys.CachedAt);
            long expiresOn = JsonUtils.ExtractParsedIntOrZero(j, StorageJsonKeys.ExpiresOn);

            // This handles a bug with the name in previous MSAL.  It used "ext_expires_on" instead of
            // "extended_expires_on" per spec, so this works around that.
            long ext_expires_on = JsonUtils.ExtractParsedIntOrZero(j, StorageJsonKeys.ExtendedExpiresOn_MsalCompat);
            long extendedExpiresOn = JsonUtils.ExtractParsedIntOrZero(j, StorageJsonKeys.ExtendedExpiresOn);
            if (extendedExpiresOn == 0 && ext_expires_on > 0)
            {
                extendedExpiresOn = ext_expires_on;
            }

            var item = new MsalAccessTokenCacheItem(JsonUtils.ExtractExistingOrEmptyString(j, StorageJsonKeys.Target))
            {
                TenantId = JsonUtils.ExtractExistingOrEmptyString(j, StorageJsonKeys.Realm),
                CachedAt = cachedAt.ToString(CultureInfo.InvariantCulture),
                ExpiresOnUnixTimestamp = expiresOn.ToString(CultureInfo.InvariantCulture),
                ExtendedExpiresOnUnixTimestamp = extendedExpiresOn.ToString(CultureInfo.InvariantCulture),
                RefreshOnUnixTimestamp = JsonUtils.ExtractExistingOrDefault<string>(j, StorageJsonKeys.RefreshOn),
                OboCacheKey = JsonUtils.ExtractExistingOrEmptyString(j, StorageJsonKeys.UserAssertionHash),
                KeyId = JsonUtils.ExtractExistingOrDefault<string>(j, StorageJsonKeys.KeyId),
                TokenType = JsonUtils.ExtractExistingOrDefault<string>(j, StorageJsonKeys.TokenType) ?? StorageJsonValues.TokenTypeBearer
            };

            item.PopulateFieldsFromJObject(j);

            return item;
        }

        internal override JObject ToJObject()
        {
            var json = base.ToJObject();

            SetItemIfValueNotNull(json, StorageJsonKeys.Realm, TenantId);
            SetItemIfValueNotNull(json, StorageJsonKeys.Target, _scopes);
            SetItemIfValueNotNull(json, StorageJsonKeys.UserAssertionHash, OboCacheKey);
            SetItemIfValueNotNull(json, StorageJsonKeys.CachedAt, CachedAt);
            SetItemIfValueNotNull(json, StorageJsonKeys.ExpiresOn, ExpiresOnUnixTimestamp);
            SetItemIfValueNotNull(json, StorageJsonKeys.ExtendedExpiresOn, ExtendedExpiresOnUnixTimestamp);
            SetItemIfValueNotNull(json, StorageJsonKeys.KeyId, KeyId);
            SetItemIfValueNotNullOrDefault(json, StorageJsonKeys.TokenType, TokenType, StorageJsonValues.TokenTypeBearer);
            SetItemIfValueNotNull(json, StorageJsonKeys.RefreshOn, RefreshOnUnixTimestamp);

            // previous versions of MSAL used "ext_expires_on" instead of the correct "extended_expires_on".
            // this is here for back compatibility
            SetItemIfValueNotNull(json, StorageJsonKeys.ExtendedExpiresOn_MsalCompat, ExtendedExpiresOnUnixTimestamp);

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
                _scopes,
                TokenType);
        }

        internal MsalIdTokenCacheKey GetIdTokenItemKey()
        {
            return new MsalIdTokenCacheKey(Environment, TenantId, HomeAccountId, ClientId);
        }

        internal bool NeedsRefresh()
        {
            return RefreshOn.HasValue &&
                RefreshOn.Value < DateTime.UtcNow;
        }
    }
}
