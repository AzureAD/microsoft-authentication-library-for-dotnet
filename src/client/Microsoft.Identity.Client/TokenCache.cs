// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.PlatformsCommon.Factories;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.Utils;

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
    public sealed partial class TokenCache : ITokenCacheInternal
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved
    {
        internal const string NullPreferredUsernameDisplayLabel = "Missing from the token response";
        private const int DefaultExpirationBufferInMinutes = 5;

        private readonly ITokenCacheBlobStorage _defaultTokenCacheBlobStorage;
        private readonly IFeatureFlags _featureFlags;
        private readonly ITokenCacheAccessor _accessor;
        private volatile bool _hasStateChanged;

        private ICoreLogger Logger => ServiceBundle.DefaultLogger;

        internal IServiceBundle ServiceBundle { get; }
        internal ILegacyCachePersistence LegacyCachePersistence { get; }
        internal string ClientId => ServiceBundle.Config.ClientId;

        ITokenCacheAccessor ITokenCacheInternal.Accessor => _accessor;
        ILegacyCachePersistence ITokenCacheInternal.LegacyPersistence => LegacyCachePersistence;

        private bool IsAppTokenCache { get; }
        bool ITokenCacheInternal.IsApplicationCache => IsAppTokenCache;

        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        SemaphoreSlim ITokenCacheInternal.Semaphore => _semaphoreSlim;

        /// <summary>
        /// Constructor of a token cache. This constructor is left for compatibility with MSAL 2.x.
        /// The recommended way to get a cache is by using <see cref="IClientApplicationBase.UserTokenCache"/>
        /// and <c>IConfidentialClientApplication.AppTokenCache</c> once the app is created.
        /// </summary>
        [Obsolete("The recommended way to get a cache is by using IClientApplicationBase.UserTokenCache or IClientApplicationBase.AppTokenCache")]
        public TokenCache() : this((IServiceBundle)null, false)
        {
        }

        internal TokenCache(IServiceBundle serviceBundle, bool isApplicationTokenCache)
        {
            var proxy = serviceBundle?.PlatformProxy ?? PlatformProxyFactory.CreatePlatformProxy(null);
            _accessor = proxy.CreateTokenCacheAccessor();
            _featureFlags = proxy.GetFeatureFlags();
            _defaultTokenCacheBlobStorage = proxy.CreateTokenCacheBlobStorage();

            if (_defaultTokenCacheBlobStorage != null)
            {
                BeforeAccess = _defaultTokenCacheBlobStorage.OnBeforeAccess;
                AfterAccess = _defaultTokenCacheBlobStorage.OnAfterAccess;
                BeforeWrite = _defaultTokenCacheBlobStorage.OnBeforeWrite;
                AsyncBeforeAccess = null;
                AsyncAfterAccess = null;
                AsyncBeforeWrite = null;
            }

            LegacyCachePersistence = proxy.CreateLegacyCachePersistence();

#if iOS
            SetIosKeychainSecurityGroup(serviceBundle.Config.IosKeychainSecurityGroup);
#endif // iOS

            IsAppTokenCache = isApplicationTokenCache;

            // Must happen last, this code can access things like _accessor and such above.
            ServiceBundle = serviceBundle;
        }

        /// <summary>
        /// This method is so we can inject test ILegacyCachePersistence...
        /// </summary>
        internal TokenCache(IServiceBundle serviceBundle, ILegacyCachePersistence legacyCachePersistenceForTest, bool isApplicationTokenCache)
            : this(serviceBundle, isApplicationTokenCache)
        {
            LegacyCachePersistence = legacyCachePersistenceForTest;
        }

        /// <inheritdoc />
        public void SetIosKeychainSecurityGroup(string securityGroup)
        {
#if iOS
            _accessor.SetiOSKeychainSecurityGroup(securityGroup);
            (LegacyCachePersistence as Microsoft.Identity.Client.Platforms.iOS.iOSLegacyCachePersistence).SetKeychainSecurityGroup(securityGroup);
#endif
        }

        private void UpdateAppMetadata(string clientId, string environment, string familyId)
        {
            if (_featureFlags.IsFociEnabled)
            {
                var metadataCacheItem = new MsalAppMetadataCacheItem(clientId, environment, familyId);
                _accessor.SaveAppMetadata(metadataCacheItem);
            }
        }

        private void DeleteAccessTokensWithIntersectingScopes(
            AuthenticationRequestParameters requestParams,
            IEnumerable<string> environmentAliases,
            string tenantId,
            SortedSet<string> scopeSet,
            string homeAccountId)
        {
            // delete all cache entries with intersecting scopes.
            // this should not happen but we have this as a safe guard
            // against multiple matches.
            requestParams.RequestContext.Logger.Info("Looking for scopes for the authority in the cache which intersect with " +
                      requestParams.Scope.AsSingleString());
            IList<MsalAccessTokenCacheItem> accessTokenItemList = new List<MsalAccessTokenCacheItem>();
            foreach (var accessToken in _accessor.GetAllAccessTokens())
            {
                if (accessToken.ClientId.Equals(ClientId, StringComparison.OrdinalIgnoreCase) &&
                    environmentAliases.Contains(accessToken.Environment) &&
                    (accessToken.IsAdfs || accessToken.TenantId.Equals(tenantId, StringComparison.OrdinalIgnoreCase)) &&
                    accessToken.ScopeSet.Overlaps(scopeSet))
                {
                    requestParams.RequestContext.Logger.Verbose("Intersecting scopes found - " + accessToken.NormalizedScopes);
                    accessTokenItemList.Add(accessToken);
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

        private string GetAccessTokenExpireLogMessageContent(MsalAccessTokenCacheItem msalAccessTokenCacheItem)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "[Current time ({0}) - Expiration Time ({1}) - Extended Expiration Time ({2})]",
                DateTime.UtcNow,
                msalAccessTokenCacheItem.ExpiresOn,
                msalAccessTokenCacheItem.ExtendedExpiresOn);
        }

        private bool RtMatchesAccount(MsalRefreshTokenCacheItem rtItem, MsalAccountCacheItem account)
        {
            bool homeAccIdMatch = rtItem.HomeAccountId.Equals(account.HomeAccountId, StringComparison.OrdinalIgnoreCase);
            bool clientIdMatch =
                rtItem.IsFRT || // Cannot filter by client ID if the RT can be used by multiple clients
                rtItem.ClientId.Equals(ClientId, StringComparison.OrdinalIgnoreCase);

            return homeAccIdMatch && clientIdMatch;
        }

        private static List<IAccount> UpdateWithAdalAccounts(
            string envFromRequest,
            IEnumerable<string> envAliases,
            AdalUsersForMsal adalUsers,
            IDictionary<string, Account> clientInfoToAccountMap)
        {
            var accounts = new List<IAccount>();

            foreach (KeyValuePair<string, AdalUserInfo> pair in adalUsers.GetUsersWithClientInfo(envAliases))
            {
                var clientInfo = ClientInfo.CreateFromJson(pair.Key);
                string accountIdentifier = clientInfo.ToAccountIdentifier();

                if (!clientInfoToAccountMap.ContainsKey(accountIdentifier))
                {
                    clientInfoToAccountMap[accountIdentifier] = new Account(
                            accountIdentifier, pair.Value.DisplayableId, envFromRequest);
                }
            }

            accounts.AddRange(clientInfoToAccountMap.Values);
            var uniqueUserNames = clientInfoToAccountMap.Values.Select(o => o.Username).Distinct().ToList();

            foreach (AdalUserInfo user in adalUsers.GetUsersWithoutClientInfo(envAliases))
            {
                if (!string.IsNullOrEmpty(user.DisplayableId) && !uniqueUserNames.Contains(user.DisplayableId))
                {
                    accounts.Add(new Account(null, user.DisplayableId, envFromRequest));
                    uniqueUserNames.Add(user.DisplayableId);
                }
            }

            return accounts;
        }

        private IEnumerable<MsalRefreshTokenCacheItem> GetAllRefreshTokensWithNoLocks(bool filterByClientId)
        {
            var refreshTokens = _accessor.GetAllRefreshTokens();
            return filterByClientId
                ? refreshTokens.Where(x => x.ClientId.Equals(ClientId, StringComparison.OrdinalIgnoreCase))
                : refreshTokens;
        }

        private IEnumerable<MsalAccessTokenCacheItem> GetAllAccessTokensWithNoLocks(bool filterByClientId)
        {
            var accessTokens = _accessor.GetAllAccessTokens();
            return filterByClientId
                ? accessTokens.Where(x => x.ClientId.Equals(ClientId, StringComparison.OrdinalIgnoreCase))
                : accessTokens;
        }

        private IEnumerable<MsalIdTokenCacheItem> GetAllIdTokensWithNoLocks(bool filterByClientId)
        {
            var idTokens = _accessor.GetAllIdTokens();
            return filterByClientId
                ? idTokens.Where(x => x.ClientId.Equals(ClientId, StringComparison.OrdinalIgnoreCase))
                : idTokens;
        }

        private static bool FrtExists(List<MsalRefreshTokenCacheItem> allRefreshTokens)
        {
            return allRefreshTokens.Any(rt => rt.IsFRT);
        }

        private void RemoveAdalUser(IAccount account)
        {
            CacheFallbackOperations.RemoveAdalUser(
                Logger,
                LegacyCachePersistence,
                ClientId,
                account.Username,
                account.HomeAccountId?.Identifier);
        }
    }
}
