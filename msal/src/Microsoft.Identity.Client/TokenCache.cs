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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;

using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Cache;
using Microsoft.Identity.Core.Helpers;
using Microsoft.Identity.Core.Instance;
using Microsoft.Identity.Core.OAuth2;
using Microsoft.Identity.Core.Telemetry;

namespace Microsoft.Identity.Client
{
#if !DESKTOP && !NET_CORE
#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved
#endif
    /// <summary>
    /// Token cache storing access and refresh tokens for accounts 
    /// This class is used in the constuctors of <see cref="PublicClientApplication"/> and <see cref="ConfidentialClientApplication"/>.
    /// In the case of ConfidentialClientApplication, two instances are used, one for the user token cache, and one for the application
    /// token cache (in the case of applications using the client credential flows).
    /// See also <see cref="TokenCacheExtensions"/> which contains extension methods used to customize the cache serialization
    /// </summary>
    public sealed class TokenCache
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved
    {
        internal const string NullPreferredUsernameDisplayLabel = "preferred_username not in id_token";

        static TokenCache()
        {
            ModuleInitializer.EnsureModuleInitialized();
        }

        private const int DefaultExpirationBufferInMinutes = 5;

        internal readonly TelemetryTokenCacheAccessor tokenCacheAccessor = new TelemetryTokenCacheAccessor();

        internal ILegacyCachePersistance legacyCachePersistance = new LegacyCachePersistance();

        /// <summary>
        /// Notification for certain token cache interactions during token acquisition. This delegate is 
        /// used in particular to provide a custom token cache serialization
        /// </summary>
        /// <param name="args">Arguments related to the cache item impacted</param>
        public delegate void TokenCacheNotification(TokenCacheNotificationArgs args);

        internal readonly object LockObject = new object();
        private volatile bool _hasStateChanged;

        internal string ClientId { get; set; }

        /// <summary>
        /// Notification method called before any library method accesses the cache.
        /// </summary>
        internal TokenCacheNotification BeforeAccess { get; set; }

        /// <summary>
        /// Notification method called before any library method writes to the cache. This notification can be used to reload
        /// the cache state from a row in database and lock that row. That database row can then be unlocked in the
        /// <see cref="AfterAccess"/>notification.
        /// </summary>
        internal TokenCacheNotification BeforeWrite { get; set; }

        /// <summary>
        /// Notification method called after any library method accesses the cache.
        /// </summary>
        internal TokenCacheNotification AfterAccess { get; set; }

        /// <summary>
        /// Gets or sets the flag indicating whether the state of the cache has changed.
        /// MSAL methods set this flag after any change.
        /// Caller applications should reset the flag after serializing and persisting the state of the cache.
        /// </summary>
        public bool HasStateChanged
        {
            get { return _hasStateChanged; }
            set { _hasStateChanged = value; }
        }

        internal void OnAfterAccess(TokenCacheNotificationArgs args)
        {
            AfterAccess?.Invoke(args);
        }

        internal void OnBeforeAccess(TokenCacheNotificationArgs args)
        {
            BeforeAccess?.Invoke(args);
        }

        internal void OnBeforeWrite(TokenCacheNotificationArgs args)
        {
            HasStateChanged = true;
            BeforeWrite?.Invoke(args);
        }

        internal Tuple<MsalAccessTokenCacheItem, MsalIdTokenCacheItem> SaveAccessAndRefreshToken
            (AuthenticationRequestParameters requestParams, MsalTokenResponse response)
        {
            var tenantId = Authority.CreateAuthority(requestParams.TenantUpdatedCanonicalAuthority, false)
                .GetTenantId();

            IdToken idToken = IdToken.Parse(response.IdToken);

            //The preferred_username value cannot be null or empty in order to comply with the ADAL/MSAL Unified cache schema. 
            //It will be set to "preferred_username not in idtoken" 
            var preferredUsername = !string.IsNullOrWhiteSpace(idToken?.PreferredUsername)? idToken.PreferredUsername : NullPreferredUsernameDisplayLabel;

            var instanceDiscoveryMetadataEntry = GetCachedAuthorityMetaData(requestParams.TenantUpdatedCanonicalAuthority);

            var environmentAliases = GetEnvironmentAliases(requestParams.TenantUpdatedCanonicalAuthority,
                instanceDiscoveryMetadataEntry);

            var preferredEnvironmentHost = GetPreferredEnvironmentHost(requestParams.Authority.Host,
                instanceDiscoveryMetadataEntry);

            var msalAccessTokenCacheItem =
                new MsalAccessTokenCacheItem(preferredEnvironmentHost, requestParams.ClientId, response, tenantId)
                {
                    UserAssertionHash = requestParams.UserAssertion?.AssertionHash
                };

            MsalRefreshTokenCacheItem msalRefreshTokenCacheItem = null;

            MsalIdTokenCacheItem msalIdTokenCacheItem = null;
            if (idToken != null)
            {
                msalIdTokenCacheItem = new MsalIdTokenCacheItem
                    (preferredEnvironmentHost, requestParams.ClientId, response, tenantId);
            }

            lock (LockObject)
            {
                try
                {
                    var args = new TokenCacheNotificationArgs
                    {
                        TokenCache = this,
                        ClientId = ClientId,
                        Account = msalAccessTokenCacheItem.HomeAccountId != null ?
                                    new Account(AccountId.FromClientInfo(msalAccessTokenCacheItem.ClientInfo),
                                    preferredUsername, preferredEnvironmentHost) : 
                                    null
                    };

                    HasStateChanged = true;
                    OnBeforeAccess(args);
                    OnBeforeWrite(args);

                    DeleteAccessTokensWithIntersectingScopes(requestParams, environmentAliases, tenantId, 
                        msalAccessTokenCacheItem.ScopeSet, msalAccessTokenCacheItem.HomeAccountId);

                    tokenCacheAccessor.SaveAccessToken(msalAccessTokenCacheItem, requestParams.RequestContext);

                    if (idToken != null)
                    {
                        tokenCacheAccessor.SaveIdToken(msalIdTokenCacheItem, requestParams.RequestContext);

                        var msalAccountCacheItem = new MsalAccountCacheItem(preferredEnvironmentHost, response, preferredUsername, tenantId);

                        tokenCacheAccessor.SaveAccount(msalAccountCacheItem, requestParams.RequestContext);
                    }

                    // if server returns the refresh token back, save it in the cache.
                    if (response.RefreshToken != null)
                    {
                        msalRefreshTokenCacheItem = new MsalRefreshTokenCacheItem(preferredEnvironmentHost, requestParams.ClientId, response);
                        var msg = "Saving RT in cache...";
                        requestParams.RequestContext.Logger.Info(msg);
                        requestParams.RequestContext.Logger.InfoPii(msg);
                        tokenCacheAccessor.SaveRefreshToken(msalRefreshTokenCacheItem, requestParams.RequestContext);
                    }

                    //save RT in ADAL cache for public clients
                    //do not save RT in ADAL cache for MSAL B2C scenarios
                    if (!requestParams.IsClientCredentialRequest && !requestParams.Authority.AuthorityType.Equals(Core.Instance.AuthorityType.B2C))
                    {
                        CacheFallbackOperations.WriteAdalRefreshToken
                            (legacyCachePersistance, msalRefreshTokenCacheItem, msalIdTokenCacheItem,
                            Authority.UpdateHost(requestParams.TenantUpdatedCanonicalAuthority, preferredEnvironmentHost),
                            msalIdTokenCacheItem.IdToken.ObjectId, response.Scope);
                    }

                    OnAfterAccess(args);

                    return Tuple.Create(msalAccessTokenCacheItem, msalIdTokenCacheItem);
                }
                finally
                {
                    HasStateChanged = false;
                }
            }
        }

        private void DeleteAccessTokensWithIntersectingScopes(AuthenticationRequestParameters requestParams,
           ISet<string> environmentAliases, string tenantId, SortedSet<string> scopeSet, string homeAccountId)
        {
            //delete all cache entries with intersecting scopes.
            //this should not happen but we have this as a safe guard
            //against multiple matches.
            var msg = "Looking for scopes for the authority in the cache which intersect with " +
                      requestParams.Scope.AsSingleString();
            requestParams.RequestContext.Logger.Info(msg);
            requestParams.RequestContext.Logger.InfoPii(msg);
            IList<MsalAccessTokenCacheItem> accessTokenItemList = new List<MsalAccessTokenCacheItem>();
            foreach (var accessTokenString in tokenCacheAccessor.GetAllAccessTokensAsString())
            {
                MsalAccessTokenCacheItem msalAccessTokenItem =
                    JsonHelper.TryToDeserializeFromJson<MsalAccessTokenCacheItem>(accessTokenString, requestParams.RequestContext);

                if (msalAccessTokenItem != null && msalAccessTokenItem.ClientId.Equals(ClientId, StringComparison.OrdinalIgnoreCase) &&
                    environmentAliases.Contains(msalAccessTokenItem.Environment) &&
                    msalAccessTokenItem.TenantId.Equals(tenantId, StringComparison.OrdinalIgnoreCase) &&
                    msalAccessTokenItem.ScopeSet.ScopeIntersects(scopeSet))
                {
                    msg = "Intersecting scopes found - " + msalAccessTokenItem.Scopes;
                    requestParams.RequestContext.Logger.Verbose(msg);
                    requestParams.RequestContext.Logger.VerbosePii(msg);
                    accessTokenItemList.Add(msalAccessTokenItem);
                }
            }

            msg = "Intersecting scope entries count - " + accessTokenItemList.Count;
            requestParams.RequestContext.Logger.Info(msg);
            requestParams.RequestContext.Logger.InfoPii(msg);

            if (!requestParams.IsClientCredentialRequest)
            {
                //filter by identifer of the user instead
                accessTokenItemList =
                    accessTokenItemList.Where(
                            item => item.HomeAccountId.Equals(homeAccountId, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                msg = "Matching entries after filtering by user - " + accessTokenItemList.Count;
                requestParams.RequestContext.Logger.Info(msg);
                requestParams.RequestContext.Logger.InfoPii(msg);
            }

            foreach (var cacheItem in accessTokenItemList)
            {
                tokenCacheAccessor.DeleteAccessToken(cacheItem.GetKey(), requestParams.RequestContext);
            }
        }

        internal async Task<MsalAccessTokenCacheItem> FindAccessTokenAsync(AuthenticationRequestParameters requestParams)
        {
            var cacheEvent = new CacheEvent(CacheEvent.TokenCacheLookup) { TokenType = CacheEvent.TokenTypes.AT };
            Telemetry.GetInstance().StartEvent(requestParams.RequestContext.TelemetryRequestId, cacheEvent);
            try
            {   
                ISet<string> environmentAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                string preferredEnvironmentAlias = null;

                if (requestParams.Authority != null)
                {
                    var instanceDiscoveryMetadataEntry = await GetCachedOrDiscoverAuthorityMetaDataAsync(requestParams.Authority.CanonicalAuthority,
                        requestParams.ValidateAuthority, requestParams.RequestContext).ConfigureAwait(false);

                    environmentAliases.UnionWith
                        (GetEnvironmentAliases(requestParams.Authority.CanonicalAuthority, instanceDiscoveryMetadataEntry));

                    preferredEnvironmentAlias = instanceDiscoveryMetadataEntry.PreferredCache;
                }

                return FindAccessTokenCommon
                    (requestParams, preferredEnvironmentAlias, environmentAliases);
            }
            finally
            {
                Telemetry.GetInstance().StopEvent(requestParams.RequestContext.TelemetryRequestId, cacheEvent);
            }
        }

        private MsalAccessTokenCacheItem FindAccessTokenCommon
            (AuthenticationRequestParameters requestParams, string preferredEnvironmentAlias, ISet<string> environmentAliases)
        {
            string msg;

            //no authority passed
            if (environmentAliases.Count == 0)
            {
                msg = "No authority provided. Skipping cache lookup ";
                requestParams.RequestContext.Logger.Warning(msg);
                requestParams.RequestContext.Logger.WarningPii(msg);

                return null;
            }

            lock (LockObject)
            {
                msg = "Looking up access token in the cache.";
                requestParams.RequestContext.Logger.Info(msg);
                requestParams.RequestContext.Logger.InfoPii(msg);
                MsalAccessTokenCacheItem msalAccessTokenCacheItem = null;
                TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                {
                    TokenCache = this,
                    ClientId = ClientId,
                    Account = requestParams.Account
                };

                OnBeforeAccess(args);
                //filtered by client id.
                ICollection<MsalAccessTokenCacheItem> tokenCacheItems = GetAllAccessTokensForClient(requestParams.RequestContext);
                OnAfterAccess(args);

                // this is OBO flow. match the cache entry with assertion hash,
                // Authority, ScopeSet and client Id.
                if (requestParams.UserAssertion != null)
                {
                    msg = "Filtering by user assertion...";
                    requestParams.RequestContext.Logger.Info(msg);
                    requestParams.RequestContext.Logger.InfoPii(msg);
                    tokenCacheItems =
                        tokenCacheItems.Where(
                                item =>
                                    !string.IsNullOrEmpty(item.UserAssertionHash) &&
                                    item.UserAssertionHash.Equals(requestParams.UserAssertion.AssertionHash, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                }
                else
                {
                    if (!requestParams.IsClientCredentialRequest)
                    {
                        msg = "Filtering by user identifier...";
                        requestParams.RequestContext.Logger.Info(msg);
                        requestParams.RequestContext.Logger.InfoPii(msg);
                        //filter by identifier of the user instead
                        tokenCacheItems =
                            tokenCacheItems
                                .Where(item => item.HomeAccountId.Equals(requestParams.Account?.HomeAccountId.Identifier, StringComparison.OrdinalIgnoreCase))
                                .ToList();
                    }
                }

                //no match found after initial filtering
                if (!tokenCacheItems.Any())
                {
                    msg = "No matching entry found for user or assertion";
                    requestParams.RequestContext.Logger.Info(msg);
                    requestParams.RequestContext.Logger.InfoPii(msg);
                    return null;
                }

                msg = "Matching entry count -" + tokenCacheItems.Count;
                requestParams.RequestContext.Logger.Info(msg);
                requestParams.RequestContext.Logger.InfoPii(msg);

                IEnumerable<MsalAccessTokenCacheItem> filteredItems =
                    tokenCacheItems.Where(
                            item =>
                                item.ScopeSet.ScopeContains(requestParams.Scope));

                msg = "Matching entry count after filtering by scopes - " + filteredItems.Count();
                requestParams.RequestContext.Logger.Info(msg);
                requestParams.RequestContext.Logger.InfoPii(msg);

                //filter by authority
                IEnumerable<MsalAccessTokenCacheItem> filteredByPreferredAlias =
                    filteredItems.Where
                    (item => item.Environment.Equals(preferredEnvironmentAlias, StringComparison.OrdinalIgnoreCase));

                if (filteredByPreferredAlias.Any())
                {
                    filteredItems = filteredByPreferredAlias;
                }
                else
                {
                    filteredItems = filteredItems.Where(
                        item => environmentAliases.Contains(item.Environment) &&
                        item.TenantId.Equals(requestParams.Authority.GetTenantId(), StringComparison.OrdinalIgnoreCase));
                }

                //no match
                if (!filteredItems.Any())
                {
                    msg = "No tokens found for matching authority, client_id, user and scopes.";
                    requestParams.RequestContext.Logger.Info(msg);
                    requestParams.RequestContext.Logger.InfoPii(msg);
                    return null;
                }

                //if only one cached token found
                if (filteredItems.Count() == 1)
                {
                    msalAccessTokenCacheItem = filteredItems.First();
                }
                else
                {
                    msg = "Multiple tokens found for matching authority, client_id, user and scopes.";
                    requestParams.RequestContext.Logger.Error(msg);
                    requestParams.RequestContext.Logger.ErrorPii(msg);

                    throw new MsalClientException(MsalClientException.MultipleTokensMatchedError,
                        MsalErrorMessage.MultipleTokensMatched);
                }

                if (msalAccessTokenCacheItem != null && msalAccessTokenCacheItem.ExpiresOn >
                    DateTime.UtcNow + TimeSpan.FromMinutes(DefaultExpirationBufferInMinutes))
                {
                    msg = "Access token is not expired. Returning the found cache entry..";
                    requestParams.RequestContext.Logger.Info(msg);
                    requestParams.RequestContext.Logger.InfoPii(msg);
                    return msalAccessTokenCacheItem;
                }

                if (msalAccessTokenCacheItem != null)
                {
                    msg = "Access token has expired or about to expire. Current time (" + DateTime.UtcNow +
                          ") - Expiration Time (" + msalAccessTokenCacheItem.ExpiresOn + ")";
                    requestParams.RequestContext.Logger.Info(msg);
                    requestParams.RequestContext.Logger.InfoPii(msg);
                }

                return null;
            }
        }

        internal async Task<MsalRefreshTokenCacheItem> FindRefreshTokenAsync(AuthenticationRequestParameters requestParams)
        {
            var cacheEvent = new CacheEvent(CacheEvent.TokenCacheLookup) { TokenType = CacheEvent.TokenTypes.RT };
            Telemetry.GetInstance().StartEvent(requestParams.RequestContext.TelemetryRequestId, cacheEvent);
            try
            {
                return await FindRefreshTokenCommonAsync(requestParams).ConfigureAwait(false);
            }
            finally
            {
                Telemetry.GetInstance().StopEvent(requestParams.RequestContext.TelemetryRequestId, cacheEvent);
            }
        }

        private async Task<MsalRefreshTokenCacheItem> FindRefreshTokenCommonAsync(AuthenticationRequestParameters requestParam)
        {
            if (requestParam.Authority == null)
            {
                return null;
            }

            var instanceDiscoveryMetadataEntry = await GetCachedOrDiscoverAuthorityMetaDataAsync(requestParam.Authority.CanonicalAuthority,
                requestParam.ValidateAuthority, requestParam.RequestContext).ConfigureAwait(false);

            var environmentAliases = GetEnvironmentAliases(requestParam.Authority.CanonicalAuthority,
                instanceDiscoveryMetadataEntry);

            var preferredEnvironmentHost = GetPreferredEnvironmentHost(requestParam.Authority.Host,
                instanceDiscoveryMetadataEntry);

            lock (LockObject)
            {
                var msg = "Looking up refresh token in the cache..";
                requestParam.RequestContext.Logger.Info(msg);
                requestParam.RequestContext.Logger.InfoPii(msg);

                TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                {
                    TokenCache = this,
                    ClientId = ClientId,
                    Account = requestParam.Account
                };

                MsalRefreshTokenCacheKey key = new MsalRefreshTokenCacheKey(
                    preferredEnvironmentHost, requestParam.ClientId, requestParam.Account?.HomeAccountId?.Identifier);

                OnBeforeAccess(args);
                try
                {
                    MsalRefreshTokenCacheItem msalRefreshTokenCacheItem =
                    JsonHelper.TryToDeserializeFromJson<MsalRefreshTokenCacheItem>(
                        tokenCacheAccessor.GetRefreshToken(key), requestParam.RequestContext);

                    // trying to find rt by authority aliases
                    if (msalRefreshTokenCacheItem == null)
                    {
                        var refreshTokensStr = tokenCacheAccessor.GetAllRefreshTokensAsString();

                        foreach (var refreshTokenStr in refreshTokensStr)
                        {
                            MsalRefreshTokenCacheItem msalRefreshToken =
                                JsonHelper.TryToDeserializeFromJson<MsalRefreshTokenCacheItem>(refreshTokenStr, requestParam.RequestContext);

                            if (msalRefreshToken != null &&
                                msalRefreshToken.ClientId.Equals(requestParam.ClientId, StringComparison.OrdinalIgnoreCase) &&
                                environmentAliases.Contains(msalRefreshToken.Environment) &&
                                requestParam.Account?.HomeAccountId.Identifier == msalRefreshToken.HomeAccountId)
                            {
                                msalRefreshTokenCacheItem = msalRefreshToken;
                                continue;
                            }
                        }
                    }

                    msg = "Refresh token found in the cache? - " + (msalRefreshTokenCacheItem != null);
                    requestParam.RequestContext.Logger.Info(msg);
                    requestParam.RequestContext.Logger.InfoPii(msg);

                    if (msalRefreshTokenCacheItem != null)
                    {
                        return msalRefreshTokenCacheItem;
                    }

                    requestParam.RequestContext.Logger.Info("Checking ADAL cache for matching RT");
                    requestParam.RequestContext.Logger.InfoPii("Checking ADAL cache for matching RT");

                    if (requestParam.Account == null)
                    {
                        return null;
                    }
                    return CacheFallbackOperations.GetAdalEntryForMsal(
                        legacyCachePersistance,
                        preferredEnvironmentHost,
                        environmentAliases,
                        requestParam.ClientId,
                        requestParam.LoginHint,
                        requestParam.Account.HomeAccountId?.Identifier,
                        null);
                }
                finally
                {
                    OnAfterAccess(args);
                }
            }
        }

        internal void DeleteRefreshToken(MsalRefreshTokenCacheItem msalRefreshTokenCacheItem, MsalIdTokenCacheItem msalIdTokenCacheItem, 
            RequestContext requestContext)
        {
            lock (LockObject)
            {
                try
                {
                    TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                    {
                        TokenCache = this,
                        ClientId = ClientId,
                        Account = new Account(
                            AccountId.FromClientInfo(msalIdTokenCacheItem.ClientInfo),
                            msalIdTokenCacheItem?.IdToken?.PreferredUsername, msalRefreshTokenCacheItem.Environment)
                    };

                    OnBeforeAccess(args);
                    OnBeforeWrite(args);
                    tokenCacheAccessor.DeleteRefreshToken(msalRefreshTokenCacheItem.GetKey(), requestContext);
                    OnAfterAccess(args);
                }
                finally
                {
                    HasStateChanged = false;
                }
            }
        }

        internal void DeleteAccessToken(MsalAccessTokenCacheItem msalAccessTokenCacheItem, MsalIdTokenCacheItem msalIdTokenCacheItem, 
            RequestContext requestContext)
        {
            lock (LockObject)
            {
                try
                {
                    TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                    {
                        TokenCache = this,
                        ClientId = ClientId,
                        Account = new Account(AccountId.FromClientInfo(msalAccessTokenCacheItem.ClientInfo),
                            msalIdTokenCacheItem?.IdToken?.PreferredUsername, msalAccessTokenCacheItem.Environment)
                    };

                    OnBeforeAccess(args);
                    OnBeforeWrite(args);
                    tokenCacheAccessor.DeleteAccessToken(msalAccessTokenCacheItem.GetKey(), requestContext);
                    OnAfterAccess(args);
                }
                finally
                {
                    HasStateChanged = false;
                }
            }
        }
        internal MsalAccessTokenCacheItem GetAccessTokenCacheItem(MsalAccessTokenCacheKey msalAccessTokenCacheKey, RequestContext requestContext)
        {
            lock (LockObject)
            {
                TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                {
                    TokenCache = this,
                    ClientId = ClientId,
                    Account = null
                };

                OnBeforeAccess(args);
                var accessTokenStr = tokenCacheAccessor.GetAccessToken(msalAccessTokenCacheKey);
                OnAfterAccess(args);

                return JsonHelper.TryToDeserializeFromJson<MsalAccessTokenCacheItem>(accessTokenStr, requestContext);
            }
        }

        internal MsalRefreshTokenCacheItem GetRefreshTokenCacheItem(MsalRefreshTokenCacheKey msalRefreshTokenCacheKey, RequestContext requestContext)
        {
            lock (LockObject)
            {
                TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                {
                    TokenCache = this,
                    ClientId = ClientId,
                    Account = null
                };

                OnBeforeAccess(args);
                var refreshTokenStr = tokenCacheAccessor.GetRefreshToken(msalRefreshTokenCacheKey);
                OnAfterAccess(args);

                return JsonHelper.TryToDeserializeFromJson<MsalRefreshTokenCacheItem>(refreshTokenStr, requestContext);
            }
        }

        internal MsalIdTokenCacheItem GetIdTokenCacheItem(MsalIdTokenCacheKey msalIdTokenCacheKey, RequestContext requestContext)
        {
            lock (LockObject)
            {
                TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                {
                    TokenCache = this,
                    ClientId = ClientId,
                    Account = null
                };

                OnBeforeAccess(args);
                var idTokenStr = tokenCacheAccessor.GetIdToken(msalIdTokenCacheKey);
                OnAfterAccess(args);

                return JsonHelper.TryToDeserializeFromJson<MsalIdTokenCacheItem>(idTokenStr, requestContext);
            }
        }

        internal MsalAccountCacheItem GetAccountCacheItem(MsalAccountCacheKey msalAccountCacheKey, RequestContext requestContext)
        {
            lock (LockObject)
            {
                TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                {
                    TokenCache = this,
                    ClientId = ClientId,
                    Account = null
                };

                OnBeforeAccess(args);
                var accountStr = tokenCacheAccessor.GetAccount(msalAccountCacheKey);
                OnAfterAccess(args);

                return JsonHelper.TryToDeserializeFromJson<MsalAccountCacheItem>(accountStr, requestContext);
            }
        }

        private async Task<InstanceDiscoveryMetadataEntry> GetCachedOrDiscoverAuthorityMetaDataAsync
            (string authority, bool validateAuthority, RequestContext requestContext)
        {
            InstanceDiscoveryMetadataEntry instanceDiscoveryMetadata = null;

            var authorityType = Authority.GetAuthorityType(authority);
            if (authorityType == Core.Instance.AuthorityType.Aad || authorityType == Core.Instance.AuthorityType.B2C)
            {
                instanceDiscoveryMetadata = await AadInstanceDiscovery.Instance.GetMetadataEntryAsync
                    (new Uri(authority), validateAuthority, requestContext).ConfigureAwait(false);
            }
            return instanceDiscoveryMetadata;
        }
         
        private InstanceDiscoveryMetadataEntry GetCachedAuthorityMetaData(string authority)
        {
            InstanceDiscoveryMetadataEntry instanceDiscoveryMetadata = null;

            var authorityType = Authority.GetAuthorityType(authority);
            if (authorityType == Core.Instance.AuthorityType.Aad || authorityType == Core.Instance.AuthorityType.B2C)
            {
                AadInstanceDiscovery.Instance.Cache.TryGetValue
                    (new Uri(authority).Host, out instanceDiscoveryMetadata);
            }
            return instanceDiscoveryMetadata;
        }

        private ISet<string> GetEnvironmentAliases(string authority, InstanceDiscoveryMetadataEntry metadata)
        {
            ISet<string> environmentAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                new Uri(authority).Host
            };

            if (metadata != null)
            {
                foreach (string environmentAlias in metadata.Aliases ?? Enumerable.Empty<string>())
                {
                    environmentAliases.Add(environmentAlias);
                }
            }

            return environmentAliases;
        }

        private string GetPreferredEnvironmentHost(string environmentHost, InstanceDiscoveryMetadataEntry metadata)
        {
            string preferredEnvironmentHost = environmentHost;

            if (metadata != null)
            {
                preferredEnvironmentHost = metadata.PreferredCache;
            }

            return preferredEnvironmentHost;
        }

        internal async Task<IEnumerable<IAccount>> GetAccountsAsync(string authority, bool validateAuthority, RequestContext requestContext)
        {
            var instanceDiscoveryMetadataEntry = 
                await GetCachedOrDiscoverAuthorityMetaDataAsync(authority, validateAuthority, requestContext).ConfigureAwait(false);

            var environmentAliases = GetEnvironmentAliases(authority, instanceDiscoveryMetadataEntry);

            var environment = new Uri(authority).Host;
            lock (LockObject)
            {
                TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                {
                    TokenCache = this,
                    ClientId = ClientId,
                    Account = null
                };

                OnBeforeAccess(args);
                ICollection<MsalRefreshTokenCacheItem> tokenCacheItems = GetAllRefreshTokensForClient(requestContext);
                ICollection<MsalAccountCacheItem> accountCacheItems = GetAllAccounts(requestContext);

                var tuple = CacheFallbackOperations.GetAllAdalUsersForMsal(legacyCachePersistance, environmentAliases, ClientId);
                OnAfterAccess(args);

                IDictionary<string, Account> clientInfoToAccountMap = new Dictionary<string, Account>();
                foreach (MsalRefreshTokenCacheItem rtItem in tokenCacheItems)
                {
                    if (environmentAliases.Contains(rtItem.Environment))
                    {
                        foreach (MsalAccountCacheItem account in accountCacheItems)
                        {
                            if (rtItem.HomeAccountId.Equals(account.HomeAccountId, StringComparison.OrdinalIgnoreCase) &&
                                environmentAliases.Contains(account.Environment))
                            {
                                clientInfoToAccountMap[rtItem.HomeAccountId] = new Account
                                    (AccountId.FromClientInfo(account.ClientInfo), account.PreferredUsername, environment);
                                break;
                            }
                        }
                    }
                }

                Dictionary<String, AdalUserInfo> clientInfoToAdalUserMap = tuple.Item1;
                List<AdalUserInfo> adalUsersWithoutClientInfo = tuple.Item2;

                foreach (KeyValuePair<string, AdalUserInfo> pair in clientInfoToAdalUserMap)
                {
                    ClientInfo clientInfo = ClientInfo.CreateFromJson(pair.Key);
                    string accountIdentifier = clientInfo.ToAccountIdentifier();

                    if (!clientInfoToAccountMap.ContainsKey(accountIdentifier))
                    {
                        clientInfoToAccountMap[accountIdentifier] = new Account(
                             AccountId.FromClientInfo(clientInfo), pair.Value.DisplayableId, environment);
                    }
                }

                ICollection<Account> accounts = new List<Account>(clientInfoToAccountMap.Values);
                List<string> uniqueUserNames = clientInfoToAccountMap.Values.Select(o => o.Username).Distinct().ToList();

                foreach (AdalUserInfo user in adalUsersWithoutClientInfo)
                {
                    if (!string.IsNullOrEmpty(user.DisplayableId) && !uniqueUserNames.Contains(user.DisplayableId))
                    {
                        accounts.Add(new Account(null, user.DisplayableId, environment));
                        uniqueUserNames.Add(user.DisplayableId);
                    }
                }
                return accounts;
            }
        }

        internal ICollection<MsalRefreshTokenCacheItem> GetAllRefreshTokensForClient(RequestContext requestContext)
        {
            lock (LockObject)
            {
                ICollection<MsalRefreshTokenCacheItem> allRefreshTokens = new List<MsalRefreshTokenCacheItem>();
                foreach (var refreshTokenString in tokenCacheAccessor.GetAllRefreshTokensAsString())
                {
                    MsalRefreshTokenCacheItem msalRefreshTokenCacheItem =
                    JsonHelper.TryToDeserializeFromJson<MsalRefreshTokenCacheItem>(refreshTokenString, requestContext);

                    if (msalRefreshTokenCacheItem != null && msalRefreshTokenCacheItem.ClientId.Equals(ClientId, StringComparison.OrdinalIgnoreCase))
                    {
                        allRefreshTokens.Add(msalRefreshTokenCacheItem);
                    }
                }
                return allRefreshTokens;
            }
        }

        internal ICollection<MsalAccessTokenCacheItem> GetAllAccessTokensForClient(RequestContext requestContext)
        {
            lock (LockObject)
            {
                ICollection<MsalAccessTokenCacheItem> allAccessTokens = new List<MsalAccessTokenCacheItem>();

                foreach (var accessTokenString in tokenCacheAccessor.GetAllAccessTokensAsString())
                {
                    MsalAccessTokenCacheItem msalAccessTokenCacheItem =
                    JsonHelper.TryToDeserializeFromJson<MsalAccessTokenCacheItem>(accessTokenString, requestContext);
                    if (msalAccessTokenCacheItem != null && msalAccessTokenCacheItem.ClientId.Equals(ClientId, StringComparison.OrdinalIgnoreCase))
                    {
                        allAccessTokens.Add(msalAccessTokenCacheItem);
                    }
                }

                return allAccessTokens;
            }
        }

        internal ICollection<MsalIdTokenCacheItem> GetAllIdTokensForClient(RequestContext requestContext)
        {
            lock (LockObject)
            {
                ICollection<MsalIdTokenCacheItem> allIdTokens = new List<MsalIdTokenCacheItem>();

                foreach (var idTokenString in tokenCacheAccessor.GetAllIdTokensAsString())
                {
                    MsalIdTokenCacheItem msalIdTokenCacheItem =
                    JsonHelper.TryToDeserializeFromJson<MsalIdTokenCacheItem>(idTokenString, requestContext);
                    if (msalIdTokenCacheItem != null && msalIdTokenCacheItem.ClientId.Equals(ClientId, StringComparison.OrdinalIgnoreCase))
                    {
                        allIdTokens.Add(msalIdTokenCacheItem);
                    }
                }

                return allIdTokens;
            }
        }

        internal MsalAccountCacheItem GetAccount(MsalRefreshTokenCacheItem refreshTokenCacheItem, RequestContext requestContext)
        {
            TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
            {
                TokenCache = this,
                ClientId = ClientId,
                Account = null
            };

            OnBeforeAccess(args);
            ICollection<MsalAccountCacheItem> accounts = GetAllAccounts(requestContext);
            OnAfterAccess(args);

            foreach (MsalAccountCacheItem account in accounts)
            {
                if (refreshTokenCacheItem.HomeAccountId.Equals(account.HomeAccountId, StringComparison.OrdinalIgnoreCase) &&
                    refreshTokenCacheItem.Environment.Equals(account.Environment, StringComparison.OrdinalIgnoreCase))
                {
                    return account;
                }
            }
            return null;
        }

        internal ICollection<MsalAccountCacheItem> GetAllAccounts(RequestContext requestContext)
        {
            lock (LockObject)
            {
                ICollection<MsalAccountCacheItem> allAccounts = new List<MsalAccountCacheItem>();

                foreach (var accountString in tokenCacheAccessor.GetAllAccountsAsString())
                {
                    MsalAccountCacheItem msalAccountCacheItem =
                    JsonHelper.TryToDeserializeFromJson<MsalAccountCacheItem>(accountString, requestContext);
                    if (msalAccountCacheItem != null)
                    {
                        allAccounts.Add(msalAccountCacheItem);
                    }
                }

                return allAccounts;
            }
        }

        internal async Task RemoveAsync(string authority, bool validateAuthority, IAccount account, RequestContext requestContext)
        {
            var instanceDiscoveryMetadataEntry =
                await GetCachedOrDiscoverAuthorityMetaDataAsync(authority, validateAuthority, requestContext).ConfigureAwait(false);

            var environmentAliases = GetEnvironmentAliases(authority, instanceDiscoveryMetadataEntry);

            lock (LockObject)
            {
                var msg = "Removing user from cache..";
                requestContext.Logger.Info(msg);
                requestContext.Logger.InfoPii(msg);

                try
                {
                    TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                    {
                        TokenCache = this,
                        ClientId = ClientId,
                        Account = account
                    };

                    OnBeforeAccess(args);
                    OnBeforeWrite(args);

                    RemoveMsalAccount(account, environmentAliases, requestContext);
                    RemoveAdalUser(account, environmentAliases);

                    OnAfterAccess(args);
                }
                finally
                {
                    HasStateChanged = false;
                }
            }
        }

        internal void RemoveMsalAccount(IAccount account, ISet<string> environmentAliases, RequestContext requestContext)
        {
            IList<MsalRefreshTokenCacheItem> allRefreshTokens = GetAllRefreshTokensForClient(requestContext)
                .Where(item => item.HomeAccountId.Equals(account.HomeAccountId.Identifier, StringComparison.OrdinalIgnoreCase) &&
                               environmentAliases.Contains(item.Environment))
                .ToList();
            foreach (MsalRefreshTokenCacheItem refreshTokenCacheItem in allRefreshTokens)
            {
                tokenCacheAccessor.DeleteRefreshToken(refreshTokenCacheItem.GetKey(), requestContext);
            }

            var msg = "Deleted refresh token count - " + allRefreshTokens.Count;
            requestContext.Logger.Info(msg);
            requestContext.Logger.InfoPii(msg);
            IList<MsalAccessTokenCacheItem> allAccessTokens = GetAllAccessTokensForClient(requestContext)
                .Where(item => item.HomeAccountId.Equals(account.HomeAccountId.Identifier, StringComparison.OrdinalIgnoreCase) &&
                               environmentAliases.Contains(item.Environment))
                .ToList();
            foreach (MsalAccessTokenCacheItem accessTokenCacheItem in allAccessTokens)
            {
                tokenCacheAccessor.DeleteAccessToken(accessTokenCacheItem.GetKey(), requestContext);
            }
            
            msg = "Deleted access token count - " + allAccessTokens.Count;
            requestContext.Logger.Info(msg);
            requestContext.Logger.InfoPii(msg);

            IList<MsalIdTokenCacheItem> allIdTokens = GetAllIdTokensForClient(requestContext)
                .Where(item => item.HomeAccountId.Equals(account.HomeAccountId.Identifier, StringComparison.OrdinalIgnoreCase) &&
                               environmentAliases.Contains(item.Environment))
                .ToList();
            foreach (MsalIdTokenCacheItem idTokenCacheItem in allIdTokens)
            {
                tokenCacheAccessor.DeleteIdToken(idTokenCacheItem.GetKey(), requestContext);
            }

            msg = "Deleted Id token count - " + allIdTokens.Count;
            requestContext.Logger.Info(msg);
            requestContext.Logger.InfoPii(msg);

            IList<MsalAccountCacheItem> allAccounts = GetAllAccounts(requestContext)
                .Where(item => item.HomeAccountId.Equals(account.HomeAccountId.Identifier, StringComparison.OrdinalIgnoreCase) &&
                               environmentAliases.Contains(item.Environment))
                .ToList();
            foreach (MsalAccountCacheItem accountCacheItem in allAccounts)
            {
                tokenCacheAccessor.DeleteAccount(accountCacheItem.GetKey(), requestContext);
            }

            msg = "Deleted Account count - " + allIdTokens.Count;
            requestContext.Logger.Info(msg);
            requestContext.Logger.InfoPii(msg);
        }

        internal void RemoveAdalUser(IAccount account, ISet<string> environmentAliases)
        {
            CacheFallbackOperations.RemoveAdalUser(legacyCachePersistance, account.Username, environmentAliases, account.HomeAccountId.Identifier);
        }

        internal ICollection<string> GetAllAccessTokenCacheItems(RequestContext requestContext)
        {
            // this method is called by serialize and does not require
            // delegates because serialize itself is called from delegates
            lock (LockObject)
            {
                ICollection<string> allTokens =
                    tokenCacheAccessor.GetAllAccessTokensAsString();
                return allTokens;
            }
        }

        internal ICollection<string> GetAllRefreshTokenCacheItems(RequestContext requestContext)
        {
            // this method is called by serialize and does not require
            // delegates because serialize itself is called from delegates
            lock (LockObject)
            {
                ICollection<string> allTokens =
                    tokenCacheAccessor.GetAllRefreshTokensAsString();
                return allTokens;
            }
        }

        internal ICollection<string> GetAllIdTokenCacheItems(RequestContext requestContext)
        {
            // this method is called by serialize and does not require
            // delegates because serialize itself is called from delegates
            lock (LockObject)
            {
                ICollection<string> allTokens =
                    tokenCacheAccessor.GetAllIdTokensAsString();
                return allTokens;
            }
        }

        internal ICollection<string> GetAllAccountCacheItems(RequestContext requestContext)
        {
            // this method is called by serialize and does not require
            // delegates because serialize itself is called from delegates
            lock (LockObject)
            {
                ICollection<string> allAccounts =
                    tokenCacheAccessor.GetAllAccountsAsString();
                return allAccounts;
            }
        }

        internal void AddAccessTokenCacheItem(MsalAccessTokenCacheItem msalAccessTokenCacheItem)
        {
            // this method is called by serialize and does not require
            // delegates because serialize itself is called from delegates
            lock (LockObject)
            {
                tokenCacheAccessor.SaveAccessToken(msalAccessTokenCacheItem);
            }
        }

        internal void AddRefreshTokenCacheItem(MsalRefreshTokenCacheItem msalRefreshTokenCacheItem)
        {
            // this method is called by serialize and does not require
            // delegates because serialize itself is called from delegates
            lock (LockObject)
            {
                tokenCacheAccessor.SaveRefreshToken(msalRefreshTokenCacheItem);
            }
        }

        internal void AddIdTokenCacheItem(MsalIdTokenCacheItem msalIdTokenCacheItem)
        {
            // this method is called by serialize and does not require
            // delegates because serialize itself is called from delegates
            lock (LockObject)
            {
                tokenCacheAccessor.SaveIdToken(msalIdTokenCacheItem);
            }
        }

        internal void AddAccountCacheItem(MsalAccountCacheItem msalAccountCacheItem)
        {
            // this method is called by serialize and does not require
            // delegates because serialize itself is called from delegates
            lock (LockObject)
            {
                tokenCacheAccessor.SaveAccount(msalAccountCacheItem);
            }
        }

        internal void Clear()
        {
            lock (LockObject)
            {
                ClearMsalCache();
                ClearAdalCache();
            }
        }

        internal void ClearAdalCache()
        {
            IDictionary<AdalTokenCacheKey, AdalResultWrapper> dictionary = AdalCacheOperations.Deserialize(legacyCachePersistance.LoadCache());
            dictionary.Clear();
            legacyCachePersistance.WriteCache(AdalCacheOperations.Serialize(dictionary));
        }

        internal void ClearMsalCache()
        {
            try
            {
                TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                {
                    TokenCache = this,
                    ClientId = ClientId,
                    Account = null
                };

                OnBeforeAccess(args);
                OnBeforeWrite(args);

                tokenCacheAccessor.Clear();

                OnAfterAccess(args);
            }
            finally
            {
                HasStateChanged = false;
            }
        }

        /// <summary>
        /// Only used by dev test apps
        /// </summary>
        internal void SaveAccesTokenCacheItem(MsalAccessTokenCacheItem msalAccessTokenCacheItem, MsalIdTokenCacheItem msalIdTokenCacheItem)
        {
            lock (LockObject)
            {
                TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                {
                    TokenCache = this,
                    ClientId = ClientId,
                    Account = msalIdTokenCacheItem != null ? new Account(
                        AccountId.FromClientInfo(msalIdTokenCacheItem.ClientInfo),
                        msalIdTokenCacheItem.IdToken?.PreferredUsername, 
                        msalAccessTokenCacheItem.Environment) : null
                };

                try
                {
                    HasStateChanged = true;
                    OnBeforeAccess(args);
                    OnBeforeWrite(args);

                    tokenCacheAccessor.SaveAccessToken(msalAccessTokenCacheItem);
                }
                finally
                {
                    OnAfterAccess(args);
                    HasStateChanged = false;
                }
            }
        }

        /// <summary>
        /// Only used by dev test apps
        /// </summary>
        /// <param name="msalRefreshTokenCacheItem"></param>
        /// <param name="msalIdTokenCacheItem"></param>
        internal void SaveRefreshTokenCacheItem(
            MsalRefreshTokenCacheItem msalRefreshTokenCacheItem, 
            MsalIdTokenCacheItem msalIdTokenCacheItem)
        {
            lock (LockObject)
            {
                TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                {
                    TokenCache = this,
                    ClientId = ClientId,
                    Account = msalIdTokenCacheItem != null ? 
                           new Account(
                               AccountId.FromClientInfo(msalIdTokenCacheItem.ClientInfo),
                               msalIdTokenCacheItem.IdToken.PreferredUsername, 
                               msalIdTokenCacheItem.IdToken.Name) : null
                };

                try
                {
                    HasStateChanged = true;
                    OnBeforeAccess(args);
                    OnBeforeWrite(args);

                    tokenCacheAccessor.SaveRefreshToken(msalRefreshTokenCacheItem);
                }
                finally
                {
                    OnAfterAccess(args);
                    HasStateChanged = false;
                }
            }
        }
    }
}