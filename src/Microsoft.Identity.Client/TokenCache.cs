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
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Exceptions;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Client.PlatformsCommon.Factories;

namespace Microsoft.Identity.Client
{
#if !DESKTOP && !NET_CORE
#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved
#endif
    /// <summary>
    /// Token cache storing access and refresh tokens for accounts
    /// This class is used in the constructors of <see cref="PublicClientApplication"/> and <see cref="ConfidentialClientApplication"/>.
    /// In the case of ConfidentialClientApplication, two instances are used, one for the user token cache, and one for the application
    /// token cache (in the case of applications using the client credential flows).
    /// </summary>
    public sealed class TokenCache : ITokenCacheInternal
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved
    {
        internal const string NullPreferredUsernameDisplayLabel = "Missing from the token response";
        private const string MicrosoftLogin = "login.microsoftonline.com";

        private ICoreLogger Logger => ServiceBundle.DefaultLogger;

        internal IServiceBundle ServiceBundle { get; private set; }

        internal RequestContext CreateRequestContext()
        {
            return new RequestContext(ServiceBundle?.Config.ClientId,
                MsalLogger.Create(Guid.Empty, ServiceBundle?.Config));
        }

        private const int DefaultExpirationBufferInMinutes = 5;

        private readonly ITokenCacheAccessor _accessor;
        internal ILegacyCachePersistence LegacyCachePersistence { get; private set; }

        ITokenCacheAccessor ITokenCacheInternal.Accessor => _accessor;
        ILegacyCachePersistence ITokenCacheInternal.LegacyPersistence => LegacyCachePersistence;

        /// <summary>
        ///
        /// </summary>
        public TokenCache()
        {
            ServiceBundle = null;
            var proxy = PlatformProxyFactory.CreatePlatformProxy(null);
            _accessor = proxy.CreateTokenCacheAccessor();
            LegacyCachePersistence = proxy.CreateLegacyCachePersistence();
        }

        internal TokenCache(IServiceBundle serviceBundle) : this()
        {
            SetServiceBundle(serviceBundle);
        }

        internal void SetServiceBundle(IServiceBundle serviceBundle)
        {
            ServiceBundle = serviceBundle;
#if iOS
            SetIosKeychainSecurityGroup(ServiceBundle.Config.IosKeychainSecurityGroup);
#endif // iOS
        }

        /// <summary>
        /// This method is so we can inject test ILegacyCachePersistence...
        /// </summary>
        /// <param name="serviceBundle"></param>
        /// <param name="legacyCachePersistenceForTest"></param>
        internal TokenCache(IServiceBundle serviceBundle, ILegacyCachePersistence legacyCachePersistenceForTest)
        {
            ServiceBundle = serviceBundle;
            _accessor = ServiceBundle.PlatformProxy.CreateTokenCacheAccessor();
            LegacyCachePersistence = legacyCachePersistenceForTest;
        }

        /// <summary>
        /// Notification for certain token cache interactions during token acquisition. This delegate is
        /// used in particular to provide a custom token cache serialization
        /// </summary>
        /// <param name="args">Arguments related to the cache item impacted</param>
        [Obsolete("Use Microsoft.Identity.Client.TokenCacheCallback instead.", true)]
        public delegate void TokenCacheNotification(TokenCacheNotificationArgs args);

        internal readonly object LockObject = new object();
        private volatile bool _hasStateChanged;

        internal string ClientId => ServiceBundle.Config.ClientId;

        /// <summary>
        /// Notification method called before any library method accesses the cache.
        /// </summary>
        internal TokenCacheCallback BeforeAccess { get; set; }

        /// <summary>
        /// Notification method called before any library method writes to the cache. This notification can be used to reload
        /// the cache state from a row in database and lock that row. That database row can then be unlocked in the
        /// <see cref="AfterAccess"/>notification.
        /// </summary>
        internal TokenCacheCallback BeforeWrite { get; set; }

        /// <summary>
        /// Notification method called after any library method accesses the cache.
        /// </summary>
        internal TokenCacheCallback AfterAccess { get; set; }

        /// <summary>
        /// Gets or sets the flag indicating whether the state of the cache has changed.
        /// MSAL methods set this flag after any change.
        /// Caller applications should reset the flag after serializing and persisting the state of the cache.
        /// </summary>
        [Obsolete("Please use the equivalent flag TokenCacheNotificationArgs.HasStateChanged, " +
            "which indicates if the operation triggering the notification is modifying the cache or not." +
            " Setting the flag is not required.")]
        public bool HasStateChanged
        {
            get => _hasStateChanged;
            set => _hasStateChanged = value;
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
#pragma warning disable CS0618 // Type or member is obsolete, but preserve old behavior until it is deleted
            HasStateChanged = true;
#pragma warning restore CS0618 // Type or member is obsolete
            args.HasStateChanged = true;
            BeforeWrite?.Invoke(args);
        }

        Tuple<MsalAccessTokenCacheItem, MsalIdTokenCacheItem> ITokenCacheInternal.SaveAccessAndRefreshToken(
            AuthenticationRequestParameters requestParams,
            MsalTokenResponse response)
        {
            // todo: could we look into modifying this to take tenantId to reduce the dependency on IValidatedAuthoritiesCache?
            var tenantId = Authority.CreateAuthority(ServiceBundle, requestParams.TenantUpdatedCanonicalAuthority)
                .GetTenantId();

            IdToken idToken = IdToken.Parse(response.IdToken);

            // The preferred_username value cannot be null or empty in order to comply with the ADAL/MSAL Unified cache schema.
            // It will be set to "preferred_username not in idtoken"
            var preferredUsername = !string.IsNullOrWhiteSpace(idToken?.PreferredUsername) ? idToken.PreferredUsername : NullPreferredUsernameDisplayLabel;

            var instanceDiscoveryMetadataEntry = GetCachedAuthorityMetaData(requestParams.TenantUpdatedCanonicalAuthority);

            var environmentAliases = GetEnvironmentAliases(requestParams.TenantUpdatedCanonicalAuthority,
                instanceDiscoveryMetadataEntry);

            var preferredEnvironmentHost = GetPreferredEnvironmentHost(requestParams.AuthorityInfo.Host,
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
                                    new Account(msalAccessTokenCacheItem.HomeAccountId,
                                    preferredUsername, preferredEnvironmentHost) :
                                    null,
                        HasStateChanged = true
                    };

#pragma warning disable CS0618 // Type or member is obsolete
                    HasStateChanged = true;
#pragma warning restore CS0618 // Type or member is obsolete

                    OnBeforeAccess(args);
                    OnBeforeWrite(args);

                    DeleteAccessTokensWithIntersectingScopes(requestParams, environmentAliases, tenantId,
                        msalAccessTokenCacheItem.ScopeSet, msalAccessTokenCacheItem.HomeAccountId);

                    _accessor.SaveAccessToken(msalAccessTokenCacheItem);

                    if (idToken != null)
                    {
                        _accessor.SaveIdToken(msalIdTokenCacheItem);

                        var msalAccountCacheItem = new MsalAccountCacheItem(preferredEnvironmentHost, response, preferredUsername, tenantId);

                        _accessor.SaveAccount(msalAccountCacheItem);
                    }

                    // if server returns the refresh token back, save it in the cache.
                    if (response.RefreshToken != null)
                    {
                        msalRefreshTokenCacheItem = new MsalRefreshTokenCacheItem(preferredEnvironmentHost, requestParams.ClientId, response);
                        requestParams.RequestContext.Logger.Info("Saving RT in cache...");
                        _accessor.SaveRefreshToken(msalRefreshTokenCacheItem);
                    }

                    // save RT in ADAL cache for public clients
                    // do not save RT in ADAL cache for MSAL B2C scenarios
                    if (!requestParams.IsClientCredentialRequest && !requestParams.AuthorityInfo.AuthorityType.Equals(AppConfig.AuthorityType.B2C))
                    {
                        CacheFallbackOperations.WriteAdalRefreshToken(
                            Logger,
                            LegacyCachePersistence,
                            msalRefreshTokenCacheItem,
                            msalIdTokenCacheItem,
                            Authority.CreateAuthorityUriWithHost(requestParams.TenantUpdatedCanonicalAuthority, preferredEnvironmentHost),
                            msalIdTokenCacheItem.IdToken.ObjectId, response.Scope);
                    }

                    OnAfterAccess(args);

                    return Tuple.Create(msalAccessTokenCacheItem, msalIdTokenCacheItem);
                }
                finally
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    HasStateChanged = false;
#pragma warning restore CS0618 // Type or member is obsolete
                }
            }
        }

        private void DeleteAccessTokensWithIntersectingScopes(AuthenticationRequestParameters requestParams,
           ISet<string> environmentAliases, string tenantId, SortedSet<string> scopeSet, string homeAccountId)
        {
            // delete all cache entries with intersecting scopes.
            // this should not happen but we have this as a safe guard
            // against multiple matches.
            requestParams.RequestContext.Logger.Info("Looking for scopes for the authority in the cache which intersect with " +
                      requestParams.Scope.AsSingleString());
            IList<MsalAccessTokenCacheItem> accessTokenItemList = new List<MsalAccessTokenCacheItem>();
            foreach (var accessTokenString in _accessor.GetAllAccessTokensAsString())
            {
                MsalAccessTokenCacheItem msalAccessTokenItem =
                    JsonHelper.TryToDeserializeFromJson<MsalAccessTokenCacheItem>(accessTokenString, requestParams.RequestContext);

                if (msalAccessTokenItem != null && msalAccessTokenItem.ClientId.Equals(ClientId, StringComparison.OrdinalIgnoreCase) &&
                    environmentAliases.Contains(msalAccessTokenItem.Environment) &&
                    msalAccessTokenItem.TenantId.Equals(tenantId, StringComparison.OrdinalIgnoreCase) &&
                    msalAccessTokenItem.ScopeSet.Overlaps(scopeSet))
                {
                    requestParams.RequestContext.Logger.Verbose("Intersecting scopes found - " + msalAccessTokenItem.NormalizedScopes);
                    accessTokenItemList.Add(msalAccessTokenItem);
                }
            }

            requestParams.RequestContext.Logger.Info("Intersecting scope entries count - " + accessTokenItemList.Count);

            if (!requestParams.IsClientCredentialRequest)
            {
                // filter by identifier of the user instead
                accessTokenItemList =
                    accessTokenItemList.Where(
                            item => item.HomeAccountId.Equals(homeAccountId, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                requestParams.RequestContext.Logger.Info("Matching entries after filtering by user - " + accessTokenItemList.Count);
            }

            foreach (var cacheItem in accessTokenItemList)
            {
                _accessor.DeleteAccessToken(cacheItem.GetKey());
            }
        }

        async Task<MsalAccessTokenCacheItem> ITokenCacheInternal.FindAccessTokenAsync(AuthenticationRequestParameters requestParams)
        {
            ISet<string> environmentAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string preferredEnvironmentAlias = null;

            if (requestParams.AuthorityInfo != null)
            {
                var instanceDiscoveryMetadataEntry = await GetCachedOrDiscoverAuthorityMetaDataAsync(
                    requestParams.AuthorityInfo.CanonicalAuthority,
                    requestParams.RequestContext).ConfigureAwait(false);

                environmentAliases.UnionWith
                    (GetEnvironmentAliases(requestParams.AuthorityInfo.CanonicalAuthority, instanceDiscoveryMetadataEntry));

                if (requestParams.AuthorityInfo.AuthorityType != AppConfig.AuthorityType.B2C)
                {
                    preferredEnvironmentAlias = instanceDiscoveryMetadataEntry.PreferredCache;
                }
            }

            return FindAccessTokenCommon
                (requestParams, preferredEnvironmentAlias, environmentAliases);
        }

        private MsalAccessTokenCacheItem FindAccessTokenCommon
            (AuthenticationRequestParameters requestParams, string preferredEnvironmentAlias, ISet<string> environmentAliases)
        {
            // no authority passed
            if (environmentAliases.Count == 0)
            {
                requestParams.RequestContext.Logger.Warning("No authority provided. Skipping cache lookup ");
                return null;
            }

            lock (LockObject)
            {
                requestParams.RequestContext.Logger.Info("Looking up access token in the cache.");
                MsalAccessTokenCacheItem msalAccessTokenCacheItem;
                TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                {
                    TokenCache = this,
                    ClientId = ClientId,
                    Account = requestParams.Account
                };

                OnBeforeAccess(args);
                // filtered by client id.
                ICollection<MsalAccessTokenCacheItem> tokenCacheItems = ((ITokenCacheInternal)this).GetAllAccessTokensForClient(requestParams.RequestContext);
                OnAfterAccess(args);

                // this is OBO flow. match the cache entry with assertion hash,
                // Authority, ScopeSet and client Id.
                if (requestParams.UserAssertion != null)
                {
                    requestParams.RequestContext.Logger.Info("Filtering by user assertion...");
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
                        requestParams.RequestContext.Logger.Info("Filtering by user identifier...");
                        // filter by identifier of the user instead
                        tokenCacheItems =
                            tokenCacheItems
                                .Where(item => item.HomeAccountId.Equals(requestParams.Account?.HomeAccountId.Identifier, StringComparison.OrdinalIgnoreCase))
                                .ToList();
                    }

                    tokenCacheItems = FilterToTenantIdSpecifiedByAuthenticationRequest(requestParams, tokenCacheItems);
                }

                // no match found after initial filtering
                if (!tokenCacheItems.Any())
                {
                    requestParams.RequestContext.Logger.Info("No matching entry found for user or assertion");
                    return null;
                }

                requestParams.RequestContext.Logger.Info("Matching entry count -" + tokenCacheItems.Count);

                IEnumerable<MsalAccessTokenCacheItem> filteredItems =
                    tokenCacheItems.Where(item => ScopeHelper.ScopeContains(item.ScopeSet, requestParams.Scope));

                requestParams.RequestContext.Logger.Info("Matching entry count after filtering by scopes - " + filteredItems.Count());

                // filter by authority
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

                // no match
                if (!filteredItems.Any())
                {
                    requestParams.RequestContext.Logger.Info("No tokens found for matching authority, client_id, user and scopes.");
                    return null;
                }

                // if only one cached token found
                if (filteredItems.Count() == 1)
                {
                    msalAccessTokenCacheItem = filteredItems.First();
                }
                else
                {
                    requestParams.RequestContext.Logger.Error("Multiple tokens found for matching authority, client_id, user and scopes.");

                    throw new MsalClientException(MsalClientException.MultipleTokensMatchedError,
                        MsalErrorMessage.MultipleTokensMatched);
                }

                if (msalAccessTokenCacheItem != null)
                {
                    if (msalAccessTokenCacheItem.ExpiresOn >
                        DateTime.UtcNow + TimeSpan.FromMinutes(DefaultExpirationBufferInMinutes))
                    {
                        requestParams.RequestContext.Logger.Info(
                            "Access token is not expired. Returning the found cache entry. " +
                            GetAccessTokenExpireLogMessageContent(msalAccessTokenCacheItem));
                        return msalAccessTokenCacheItem;
                    }

                    if (ServiceBundle.Config.IsExtendedTokenLifetimeEnabled && msalAccessTokenCacheItem.ExtendedExpiresOn >
                        DateTime.UtcNow + TimeSpan.FromMinutes(DefaultExpirationBufferInMinutes))
                    {
                        requestParams.RequestContext.Logger.Info(
                            "Access token is expired.  IsExtendedLifeTimeEnabled=TRUE and ExtendedExpiresOn is not exceeded.  Returning the found cache entry. " +
                            GetAccessTokenExpireLogMessageContent(msalAccessTokenCacheItem));

                        msalAccessTokenCacheItem.IsExtendedLifeTimeToken = true;
                        return msalAccessTokenCacheItem;
                    }

                    requestParams.RequestContext.Logger.Info(
                        "Access token has expired or about to expire. " +
                        GetAccessTokenExpireLogMessageContent(msalAccessTokenCacheItem));
                }

                return null;
            }
        }

        private ICollection<MsalAccessTokenCacheItem> FilterToTenantIdSpecifiedByAuthenticationRequest(
            AuthenticationRequestParameters requestParams, ICollection<MsalAccessTokenCacheItem> tokenCacheItems)
        {
            if (tokenCacheItems.Count <= 1)
            {
                return tokenCacheItems;
            }

            requestParams.RequestContext.Logger.Info(
                "Filtering by tenant specified in the authentication request parameters...");

            ICollection<MsalAccessTokenCacheItem> authorityCacheMatches = tokenCacheItems.Where(
                item => item.TenantId.Equals(
                    requestParams.Authority.GetTenantId(),
                    StringComparison.OrdinalIgnoreCase)).ToList();

            return authorityCacheMatches;
        }

        private string GetAccessTokenExpireLogMessageContent(MsalAccessTokenCacheItem msalAccessTokenCacheItem)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "[Current time ({0}) - Expiration Time ({1}) - Extended Expiration Time ({2})]",
                DateTime.UtcNow,
                msalAccessTokenCacheItem.ExpiresOn,
                msalAccessTokenCacheItem.ExtendedExpiresOn);
        }

        async Task<MsalRefreshTokenCacheItem> ITokenCacheInternal.FindRefreshTokenAsync(AuthenticationRequestParameters requestParams)
        {
            using (ServiceBundle.TelemetryManager.CreateTelemetryHelper(requestParams.RequestContext.TelemetryRequestId, requestParams.RequestContext.ClientId,
                new CacheEvent(CacheEvent.TokenCacheLookup) { TokenType = CacheEvent.TokenTypes.RT }))
            {
                return await FindRefreshTokenCommonAsync(requestParams).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public void SetIosKeychainSecurityGroup(string securityGroup)
        {
            #if iOS
            _accessor.SetiOSKeychainSecurityGroup(securityGroup);
            (LegacyCachePersistence as Microsoft.Identity.Client.Platforms.iOS.iOSLegacyCachePersistence).SetKeychainSecurityGroup(securityGroup);
            #endif
        }

        private async Task<MsalRefreshTokenCacheItem> FindRefreshTokenCommonAsync(AuthenticationRequestParameters requestParam)
        {
            if (requestParam.Authority == null)
            {
                return null;
            }

            var instanceDiscoveryMetadataEntry = await GetCachedOrDiscoverAuthorityMetaDataAsync(requestParam.AuthorityInfo.CanonicalAuthority,
                requestParam.RequestContext).ConfigureAwait(false);

            var environmentAliases = GetEnvironmentAliases(requestParam.AuthorityInfo.CanonicalAuthority,
                instanceDiscoveryMetadataEntry);

            var preferredEnvironmentHost = GetPreferredEnvironmentHost(requestParam.AuthorityInfo.Host,
                instanceDiscoveryMetadataEntry);

            lock (LockObject)
            {
                requestParam.RequestContext.Logger.Info("Looking up refresh token in the cache..");

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
                        _accessor.GetRefreshToken(key), requestParam.RequestContext);

                    // trying to find rt by authority aliases
                    if (msalRefreshTokenCacheItem == null)
                    {
                        var refreshTokensStr = _accessor.GetAllRefreshTokensAsString();

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

                    requestParam.RequestContext.Logger.Info("Refresh token found in the cache? - " + (msalRefreshTokenCacheItem != null));

                    if (msalRefreshTokenCacheItem != null)
                    {
                        return msalRefreshTokenCacheItem;
                    }

                    requestParam.RequestContext.Logger.Info("Checking ADAL cache for matching RT");

                    if (requestParam.Account == null)
                    {
                        return null;
                    }
                    return CacheFallbackOperations.GetAdalEntryForMsal(
                        Logger,
                        LegacyCachePersistence,
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
                            msalIdTokenCacheItem.HomeAccountId,
                            msalIdTokenCacheItem.IdToken?.PreferredUsername, msalRefreshTokenCacheItem.Environment),
                        HasStateChanged = true
                    };

                    OnBeforeAccess(args);
                    OnBeforeWrite(args);
                    _accessor.DeleteRefreshToken(msalRefreshTokenCacheItem.GetKey());
                    OnAfterAccess(args);
                }
                finally
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    HasStateChanged = false;
#pragma warning restore CS0618 // Type or member is obsolete
                }
            }
        }

        void ITokenCacheInternal.DeleteAccessToken(MsalAccessTokenCacheItem msalAccessTokenCacheItem, MsalIdTokenCacheItem msalIdTokenCacheItem,
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
                        Account = new Account(msalAccessTokenCacheItem.HomeAccountId,
                            msalIdTokenCacheItem?.IdToken?.PreferredUsername, msalAccessTokenCacheItem.Environment),
                        HasStateChanged = true
                    };

                    OnBeforeAccess(args);
                    OnBeforeWrite(args);
                    _accessor.DeleteAccessToken(msalAccessTokenCacheItem.GetKey());
                    OnAfterAccess(args);
                }
                finally
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    HasStateChanged = false;
#pragma warning restore CS0618 // Type or member is obsolete
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
                var accessTokenStr = _accessor.GetAccessToken(msalAccessTokenCacheKey);
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
                var refreshTokenStr = _accessor.GetRefreshToken(msalRefreshTokenCacheKey);
                OnAfterAccess(args);

                return JsonHelper.TryToDeserializeFromJson<MsalRefreshTokenCacheItem>(refreshTokenStr, requestContext);
            }
        }

        MsalIdTokenCacheItem ITokenCacheInternal.GetIdTokenCacheItem(MsalIdTokenCacheKey msalIdTokenCacheKey, RequestContext requestContext)
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
                var idTokenStr = _accessor.GetIdToken(msalIdTokenCacheKey);
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
                var accountStr = _accessor.GetAccount(msalAccountCacheKey);
                OnAfterAccess(args);

                return JsonHelper.TryToDeserializeFromJson<MsalAccountCacheItem>(accountStr, requestContext);
            }
        }

        private async Task<InstanceDiscoveryMetadataEntry> GetCachedOrDiscoverAuthorityMetaDataAsync(
            string authority,
            RequestContext requestContext)
        {
            Uri authorityHost = new Uri(authority);
            var authorityType = Authority.GetAuthorityType(authority);
            if (authorityType == AppConfig.AuthorityType.Aad ||
                authorityHost.Host.Equals(MicrosoftLogin, StringComparison.OrdinalIgnoreCase))
            {
                var instanceDiscoveryMetadata = await ServiceBundle.AadInstanceDiscovery.GetMetadataEntryAsync(
                    new Uri(authority),
                    requestContext).ConfigureAwait(false);
                return instanceDiscoveryMetadata;
            }
            return null;
        }

        private InstanceDiscoveryMetadataEntry GetCachedAuthorityMetaData(string authority)
        {
            if (ServiceBundle?.AadInstanceDiscovery == null)
            {
                return null;
            }

            InstanceDiscoveryMetadataEntry instanceDiscoveryMetadata = null;
            var authorityType = Authority.GetAuthorityType(authority);
            if (authorityType == AppConfig.AuthorityType.Aad || authorityType == AppConfig.AuthorityType.B2C)
            {
                ServiceBundle.AadInstanceDiscovery.TryGetValue(new Uri(authority).Host, out instanceDiscoveryMetadata);
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

        IEnumerable<IAccount> ITokenCacheInternal.GetAccounts(string authority, RequestContext requestContext)
        {
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
                ICollection<MsalRefreshTokenCacheItem> tokenCacheItems = ((ITokenCacheInternal)this).GetAllRefreshTokensForClient(requestContext);
                ICollection<MsalAccountCacheItem> accountCacheItems = ((ITokenCacheInternal)this).GetAllAccounts(requestContext);

                var adalUsersResult = CacheFallbackOperations.GetAllAdalUsersForMsal(Logger, LegacyCachePersistence, ClientId);
                OnAfterAccess(args);

                IDictionary<string, Account> clientInfoToAccountMap = new Dictionary<string, Account>();
                foreach (MsalRefreshTokenCacheItem rtItem in tokenCacheItems)
                {
                    foreach (MsalAccountCacheItem account in accountCacheItems)
                    {
                        if (rtItem.HomeAccountId.Equals(account.HomeAccountId, StringComparison.OrdinalIgnoreCase))
                        {
                            clientInfoToAccountMap[rtItem.HomeAccountId] = new Account
                                (account.HomeAccountId, account.PreferredUsername, environment);
                            break;
                        }
                    }
                }

                Dictionary<string, AdalUserInfo> clientInfoToAdalUserMap = adalUsersResult.ClientInfoUsers;
                List<AdalUserInfo> adalUsersWithoutClientInfo = adalUsersResult.UsersWithoutClientInfo;

                foreach (KeyValuePair<string, AdalUserInfo> pair in clientInfoToAdalUserMap)
                {
                    ClientInfo clientInfo = ClientInfo.CreateFromJson(pair.Key);
                    string accountIdentifier = clientInfo.ToAccountIdentifier();

                    if (!clientInfoToAccountMap.ContainsKey(accountIdentifier))
                    {
                        clientInfoToAccountMap[accountIdentifier] = new Account(
                             accountIdentifier, pair.Value.DisplayableId, environment);
                    }
                }

                var accounts = new List<IAccount>(clientInfoToAccountMap.Values);
                List<string> uniqueUserNames = clientInfoToAccountMap.Values.Select(o => o.Username).Distinct().ToList();

                foreach (AdalUserInfo user in adalUsersWithoutClientInfo)
                {
                    if (!string.IsNullOrEmpty(user.DisplayableId) && !uniqueUserNames.Contains(user.DisplayableId))
                    {
                        accounts.Add(new Account(null, user.DisplayableId, environment));
                        uniqueUserNames.Add(user.DisplayableId);
                    }
                }
                return accounts.AsEnumerable();
            }
        }

        ICollection<MsalRefreshTokenCacheItem> ITokenCacheInternal.GetAllRefreshTokensForClient(RequestContext requestContext)
        {
            lock (LockObject)
            {
                ICollection<MsalRefreshTokenCacheItem> allRefreshTokens = new List<MsalRefreshTokenCacheItem>();
                foreach (var refreshTokenString in _accessor.GetAllRefreshTokensAsString())
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

        ICollection<MsalAccessTokenCacheItem> ITokenCacheInternal.GetAllAccessTokensForClient(RequestContext requestContext)
        {
            lock (LockObject)
            {
                ICollection<MsalAccessTokenCacheItem> allAccessTokens = new List<MsalAccessTokenCacheItem>();

                foreach (var accessTokenString in _accessor.GetAllAccessTokensAsString())
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

        ICollection<MsalIdTokenCacheItem> ITokenCacheInternal.GetAllIdTokensForClient(RequestContext requestContext)
        {
            lock (LockObject)
            {
                ICollection<MsalIdTokenCacheItem> allIdTokens = new List<MsalIdTokenCacheItem>();

                foreach (var idTokenString in _accessor.GetAllIdTokensAsString())
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

        MsalAccountCacheItem ITokenCacheInternal.GetAccount(MsalRefreshTokenCacheItem refreshTokenCacheItem, RequestContext requestContext)
        {
            TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
            {
                TokenCache = this,
                ClientId = ClientId,
                Account = null
            };

            OnBeforeAccess(args);
            ICollection<MsalAccountCacheItem> accounts = ((ITokenCacheInternal)this).GetAllAccounts(requestContext);
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

        ICollection<MsalAccountCacheItem> ITokenCacheInternal.GetAllAccounts(RequestContext requestContext)
        {
            lock (LockObject)
            {
                ICollection<MsalAccountCacheItem> allAccounts = new List<MsalAccountCacheItem>();

                foreach (var accountString in _accessor.GetAllAccountsAsString())
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

        void ITokenCacheInternal.RemoveAccount(IAccount account, RequestContext requestContext)
        {
            lock (LockObject)
            {
                requestContext.Logger.Info("Removing user from cache..");

                try
                {
                    TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                    {
                        TokenCache = this,
                        ClientId = ClientId,
                        Account = account,
                        HasStateChanged = true
                    };

                    OnBeforeAccess(args);
                    OnBeforeWrite(args);

                    ((ITokenCacheInternal)this).RemoveMsalAccount(account, requestContext);
                    RemoveAdalUser(account);

                    OnAfterAccess(args);
                }
                finally
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    HasStateChanged = false;
#pragma warning restore CS0618 // Type or member is obsolete
                }
            }
        }

        void ITokenCacheInternal.RemoveMsalAccount(IAccount account, RequestContext requestContext)
        {
            if (account.HomeAccountId == null)
            {
                // adalv3 account
                return;
            }
            IList<MsalRefreshTokenCacheItem> allRefreshTokens = ((ITokenCacheInternal)this).GetAllRefreshTokensForClient(requestContext)
                .Where(item => item.HomeAccountId.Equals(account.HomeAccountId.Identifier, StringComparison.OrdinalIgnoreCase))
                .ToList();
            foreach (MsalRefreshTokenCacheItem refreshTokenCacheItem in allRefreshTokens)
            {
                _accessor.DeleteRefreshToken(refreshTokenCacheItem.GetKey());
            }

            requestContext.Logger.Info("Deleted refresh token count - " + allRefreshTokens.Count);
            IList<MsalAccessTokenCacheItem> allAccessTokens = ((ITokenCacheInternal)this).GetAllAccessTokensForClient(requestContext)
                .Where(item => item.HomeAccountId.Equals(account.HomeAccountId.Identifier, StringComparison.OrdinalIgnoreCase))
                .ToList();
            foreach (MsalAccessTokenCacheItem accessTokenCacheItem in allAccessTokens)
            {
                _accessor.DeleteAccessToken(accessTokenCacheItem.GetKey());
            }

            requestContext.Logger.Info("Deleted access token count - " + allAccessTokens.Count);

            IList<MsalIdTokenCacheItem> allIdTokens = ((ITokenCacheInternal)this).GetAllIdTokensForClient(requestContext)
                .Where(item => item.HomeAccountId.Equals(account.HomeAccountId.Identifier, StringComparison.OrdinalIgnoreCase))
                .ToList();
            foreach (MsalIdTokenCacheItem idTokenCacheItem in allIdTokens)
            {
                _accessor.DeleteIdToken(idTokenCacheItem.GetKey());
            }

            requestContext.Logger.Info("Deleted Id token count - " + allIdTokens.Count);
        }

        internal void RemoveAdalUser(IAccount account)
        {
            CacheFallbackOperations.RemoveAdalUser(
                Logger,
                LegacyCachePersistence,
                ClientId,
                account.Username,
                account.HomeAccountId.Identifier);
        }

        ICollection<string> ITokenCacheInternal.GetAllAccessTokenCacheItems(RequestContext requestContext)
        {
            // this method is called by serialize and does not require
            // delegates because serialize itself is called from delegates
            lock (LockObject)
            {
                ICollection<string> allTokens =
                    _accessor.GetAllAccessTokensAsString();
                return allTokens;
            }
        }

        ICollection<string> ITokenCacheInternal.GetAllRefreshTokenCacheItems(RequestContext requestContext)
        {
            // this method is called by serialize and does not require
            // delegates because serialize itself is called from delegates
            lock (LockObject)
            {
                ICollection<string> allTokens =
                    _accessor.GetAllRefreshTokensAsString();
                return allTokens;
            }
        }

        ICollection<string> ITokenCacheInternal.GetAllIdTokenCacheItems(RequestContext requestContext)
        {
            // this method is called by serialize and does not require
            // delegates because serialize itself is called from delegates
            lock (LockObject)
            {
                ICollection<string> allTokens =
                    _accessor.GetAllIdTokensAsString();
                return allTokens;
            }
        }

        ICollection<string> ITokenCacheInternal.GetAllAccountCacheItems(RequestContext requestContext)
        {
            // this method is called by serialize and does not require
            // delegates because serialize itself is called from delegates
            lock (LockObject)
            {
                ICollection<string> allAccounts =
                    _accessor.GetAllAccountsAsString();
                return allAccounts;
            }
        }

        void ITokenCacheInternal.AddAccessTokenCacheItem(MsalAccessTokenCacheItem msalAccessTokenCacheItem)
        {
            // this method is called by serialize and does not require
            // delegates because serialize itself is called from delegates
            lock (LockObject)
            {
                _accessor.SaveAccessToken(msalAccessTokenCacheItem);
            }
        }

        void ITokenCacheInternal.AddRefreshTokenCacheItem(MsalRefreshTokenCacheItem msalRefreshTokenCacheItem)
        {
            // this method is called by serialize and does not require
            // delegates because serialize itself is called from delegates
            lock (LockObject)
            {
                _accessor.SaveRefreshToken(msalRefreshTokenCacheItem);
            }
        }

        internal void AddIdTokenCacheItem(MsalIdTokenCacheItem msalIdTokenCacheItem)
        {
            // this method is called by serialize and does not require
            // delegates because serialize itself is called from delegates
            lock (LockObject)
            {
                _accessor.SaveIdToken(msalIdTokenCacheItem);
            }
        }

        internal void AddAccountCacheItem(MsalAccountCacheItem msalAccountCacheItem)
        {
            // this method is called by serialize and does not require
            // delegates because serialize itself is called from delegates
            lock (LockObject)
            {
                _accessor.SaveAccount(msalAccountCacheItem);
            }
        }

        void ITokenCacheInternal.Clear()
        {
            lock (LockObject)
            {
                TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                {
                    TokenCache = this,
                    ClientId = ClientId,
                    Account = null,
                    HasStateChanged = true
                };

                try
                {
                    OnBeforeAccess(args);
                    OnBeforeWrite(args);

                    ((ITokenCacheInternal)this).ClearMsalCache();
                    ((ITokenCacheInternal)this).ClearAdalCache();
                }
                finally
                {
                    OnAfterAccess(args);
#pragma warning disable CS0618 // Type or member is obsolete
                    HasStateChanged = false;
#pragma warning restore CS0618 // Type or member is obsolete
                }
            }
        }

        void ITokenCacheInternal.ClearAdalCache()
        {
            IDictionary<AdalTokenCacheKey, AdalResultWrapper> dictionary = AdalCacheOperations.Deserialize(Logger, LegacyCachePersistence.LoadCache());
            dictionary.Clear();
            LegacyCachePersistence.WriteCache(AdalCacheOperations.Serialize(Logger, dictionary));
        }

        void ITokenCacheInternal.ClearMsalCache()
        {
            _accessor.Clear();
        }

        /// <summary>
        /// Only used by dev test apps
        /// </summary>
        void ITokenCacheInternal.SaveAccessTokenCacheItem(MsalAccessTokenCacheItem msalAccessTokenCacheItem, MsalIdTokenCacheItem msalIdTokenCacheItem)
        {
            lock (LockObject)
            {
                TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                {
                    TokenCache = this,
                    ClientId = ClientId,
                    Account = msalIdTokenCacheItem != null ? new Account(
                        msalIdTokenCacheItem.HomeAccountId,
                        msalIdTokenCacheItem.IdToken?.PreferredUsername,
                        msalAccessTokenCacheItem.Environment) : null,
                    HasStateChanged = true
                };

                try
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    HasStateChanged = true;
#pragma warning restore CS0618 // Type or member is obsolete
                    OnBeforeAccess(args);
                    OnBeforeWrite(args);

                    _accessor.SaveAccessToken(msalAccessTokenCacheItem);
                }
                finally
                {
                    OnAfterAccess(args);
#pragma warning disable CS0618 // Type or member is obsolete
                    HasStateChanged = false;
#pragma warning restore CS0618 // Type or member is obsolete
                }
            }
        }

        /// <summary>
        /// Only used by dev test apps
        /// </summary>
        /// <param name="msalRefreshTokenCacheItem"></param>
        /// <param name="msalIdTokenCacheItem"></param>
        void ITokenCacheInternal.SaveRefreshTokenCacheItem(
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
                               msalIdTokenCacheItem.HomeAccountId,
                               msalIdTokenCacheItem.IdToken.PreferredUsername,
                               msalIdTokenCacheItem.IdToken.Name) : null,
                    HasStateChanged = true
                };

                try
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    HasStateChanged = true;
#pragma warning restore CS0618 // Type or member is obsolete
                    OnBeforeAccess(args);
                    OnBeforeWrite(args);

                    _accessor.SaveRefreshToken(msalRefreshTokenCacheItem);
                }
                finally
                {
                    OnAfterAccess(args);
#pragma warning disable CS0618 // Type or member is obsolete
                    HasStateChanged = false;
#pragma warning restore CS0618 // Type or member is obsolete
                }
            }
        }

#if !ANDROID_BUILDTIME && !iOS_BUILDTIME && !WINDOWS_APP_BUILDTIME
        // todo: where to put this documentation
        ///// <summary>
        ///// Extension methods used to subscribe to cache serialization events, and to effectively serialize and deserialize the cache
        ///// </summary>
        ///// <remarks>New in MSAL.NET 2.x: it's now possible to deserialize the token cache in two formats, the ADAL V3 legacy token cache
        ///// format, and the new unified cache format, common to ADAL.NET, MSAL.NET, and other libraries on the same platform (MSAL.objc, on iOS)</remarks>

        /// <summary>
        /// Sets a delegate to be notified before any library method accesses the cache. This gives an option to the
        /// delegate to deserialize a cache entry for the application and accounts specified in the <see cref="TokenCacheNotificationArgs"/>.
        /// See https://aka.ms/msal-net-token-cache-serialization
        /// </summary>
        /// <param name="beforeAccess">Delegate set in order to handle the cache deserialiation</param>
        /// <remarks>In the case where the delegate is used to deserialize the cache, it might
        /// want to call <see cref="Deserialize(byte[])"/></remarks>
        public void SetBeforeAccess(TokenCacheCallback beforeAccess)
        {
            GuardOnMobilePlatforms();
            BeforeAccess = beforeAccess;
        }

        /// <summary>
        /// Sets a delegate to be notified after any library method accesses the cache. This gives an option to the
        /// delegate to serialize a cache entry for the application and accounts specified in the <see cref="TokenCacheNotificationArgs"/>.
        /// See https://aka.ms/msal-net-token-cache-serialization
        /// </summary>
        /// <param name="afterAccess">Delegate set in order to handle the cache serialization in the case where the <see cref="TokenCache.HasStateChanged"/>
        /// member of the cache is <c>true</c></param>
        /// <remarks>In the case where the delegate is used to serialize the cache entierely (not just a row), it might
        /// want to call <see cref="Serialize()"/></remarks>
        public void SetAfterAccess(TokenCacheCallback afterAccess)
        {
            GuardOnMobilePlatforms();
            AfterAccess = afterAccess;
        }

        /// <summary>
        /// Sets a delegate called before any library method writes to the cache. This gives an option to the delegate
        /// to reload the cache state from a row in database and lock that row. That database row can then be unlocked in the delegate
        /// registered with <see cref="SetAfterAccess(TokenCacheCallback)"/>
        /// </summary>
        /// <param name="beforeWrite">Delegate set in order to prepare the cache serialization</param>
        public void SetBeforeWrite(TokenCacheCallback beforeWrite)
        {
            GuardOnMobilePlatforms();
            BeforeWrite = beforeWrite;
        }

        private const string AccessTokenKey = "access_tokens";
        private const string RefreshTokenKey = "refresh_tokens";
        private const string IdTokenKey = "id_tokens";
        private const string AccountKey = "accounts";

        /// <summary>
        /// Deserializes the token cache from a serialization blob in the unified cache format
        /// </summary>
        /// <param name="unifiedState">Array of bytes containing serialized Msal cache data</param>
        /// <remarks>
        /// <paramref name="unifiedState"/>Is a Json blob containing access tokens, refresh tokens, id tokens and accounts information.
        /// </remarks>
        public void Deserialize(byte[] unifiedState)
        {
            GuardOnMobilePlatforms();

            var requestContext = CreateRequestContext();

            lock (LockObject)
            {
                _accessor.Clear();

                Dictionary<string, IEnumerable<string>> cacheDict = JsonHelper
                    .DeserializeFromJson<Dictionary<string, IEnumerable<string>>>(unifiedState);

                if (cacheDict == null || cacheDict.Count == 0)
                {
                    Logger.Info("Msal Cache is empty.");
                    return;
                }

                if (cacheDict.ContainsKey(AccessTokenKey))
                {
                    foreach (var atItem in cacheDict[AccessTokenKey])
                    {
                        var msalAccessTokenCacheItem =
                            JsonHelper.TryToDeserializeFromJson<MsalAccessTokenCacheItem>(atItem, requestContext);
                        if (msalAccessTokenCacheItem != null)
                        {
                            _accessor.SaveAccessToken(msalAccessTokenCacheItem);
                        }
                    }
                }

                if (cacheDict.ContainsKey(RefreshTokenKey))
                {
                    foreach (var rtItem in cacheDict[RefreshTokenKey])
                    {
                        var msalRefreshTokenCacheItem =
                            JsonHelper.TryToDeserializeFromJson<MsalRefreshTokenCacheItem>(rtItem, requestContext);
                        if (msalRefreshTokenCacheItem != null)
                        {
                            _accessor.SaveRefreshToken(msalRefreshTokenCacheItem);
                        }
                    }
                }

                if (cacheDict.ContainsKey(IdTokenKey))
                {
                    foreach (var idItem in cacheDict[IdTokenKey])
                    {
                        var msalIdTokenCacheItem =
                            JsonHelper.TryToDeserializeFromJson<MsalIdTokenCacheItem>(idItem, requestContext);
                        if (msalIdTokenCacheItem != null)
                        {
                            _accessor.SaveIdToken(msalIdTokenCacheItem);
                        }
                    }
                }

                if (cacheDict.ContainsKey(AccountKey))
                {
                    foreach (var account in cacheDict[AccountKey])
                    {
                        var msalAccountCacheItem =
                            JsonHelper.TryToDeserializeFromJson<MsalAccountCacheItem>(account, requestContext);

                        if (msalAccountCacheItem != null)
                        {
                            _accessor.SaveAccount(msalAccountCacheItem);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Deserializes the token cache from a serialization blob in both format (ADAL V3 format, and unified cache format)
        /// </summary>
        /// <param name="cacheData">Array of bytes containing serialicache data</param>
        public void DeserializeUnifiedAndAdalCache(CacheData cacheData)
        {
            GuardOnMobilePlatforms();
            lock (LockObject)
            {
                Deserialize(cacheData.UnifiedState);
                LegacyCachePersistence.WriteCache(cacheData.AdalV3State);
            }
        }

        /// <summary>
        /// Serializes the entire token cache, in the unified cache format only
        /// </summary>
        /// <returns>array of bytes containing the serialized unified cache</returns>
        public byte[] Serialize()
        {
            GuardOnMobilePlatforms();
            // reads the underlying in-memory dictionary and dumps out the content as a JSON
            lock (LockObject)
            {
                // reads the underlying in-memory dictionary and dumps out the content as a JSON
                Dictionary<string, IEnumerable<string>> cacheDict = new Dictionary<string, IEnumerable<string>>
                {
                    [AccessTokenKey] = _accessor.GetAllAccessTokensAsString(),
                    [RefreshTokenKey] = _accessor.GetAllRefreshTokensAsString(),
                    [IdTokenKey] = _accessor.GetAllIdTokensAsString(),
                    [AccountKey] = _accessor.GetAllAccountsAsString()
                };

                return JsonHelper.SerializeToJson(cacheDict).ToByteArray();
            }
        }

        /// <summary>
        /// Serializes the entire token cache in both the ADAL V3 and unified cache formats.
        /// </summary>
        /// <returns>Serialized token cache <see cref="CacheData"/></returns>
        public CacheData SerializeUnifiedAndAdalCache()
        {
            GuardOnMobilePlatforms();
            // reads the underlying in-memory dictionary and dumps out the content as a JSON
            lock (LockObject)
            {
                var serializedUnifiedCache = Serialize();
                var serializeAdalCache = LegacyCachePersistence.LoadCache();

                return new CacheData()
                {
                    AdalV3State = serializeAdalCache,
                    UnifiedState = serializedUnifiedCache
                };
            }
        }

        private static void GuardOnMobilePlatforms()
        {
#if ANDROID || iOS || WINDOWS_APP
        throw new PlatformNotSupportedException("You should not use these TokenCache methods object on mobile platforms. " +
            "They meant to allow applications to define their own storage strategy on .net desktop and non-mobile platforms such as .net core. " +
            "On mobile platforms, a secure and performant storage mechanism is implemeted by MSAL. " +
            "For more details about custom token cache serialization, visit https://aka.ms/msal-net-serialization");
#endif
        }
#endif // !ANDROID_BUILDTIME && !iOS_BUILDTIME && !WINDOWS_APP_BUILDTIME

    }
}