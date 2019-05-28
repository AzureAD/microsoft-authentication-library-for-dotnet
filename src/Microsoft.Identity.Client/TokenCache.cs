// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance;
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
        private const string AzurePublicEnv = "login.microsoftonline.com";
        private const int DefaultExpirationBufferInMinutes = 5;

        private readonly ITokenCacheBlobStorage _defaultTokenCacheBlobStorage;
        private readonly IFeatureFlags _featureFlags;
        private readonly ITokenCacheAccessor _accessor;
        private volatile bool _hasStateChanged;

        private ICoreLogger Logger => ServiceBundle.DefaultLogger;

        internal IServiceBundle ServiceBundle { get; }
        internal ILegacyCachePersistence LegacyCachePersistence { get; }
        internal readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
        internal string ClientId => ServiceBundle.Config.ClientId;

        ITokenCacheAccessor ITokenCacheInternal.Accessor => _accessor;
        ILegacyCachePersistence ITokenCacheInternal.LegacyPersistence => LegacyCachePersistence;
        SemaphoreSlim ITokenCacheInternal.Semaphore => _semaphoreSlim;

        /// <summary>
        /// Constructor of a token cache. This constructor is left for compatibility with MSAL 2.x.
        /// The recommended way to get a cache is by using <see cref="IClientApplicationBase.UserTokenCache"/>
        /// and <c>IConfidentialClientApplication.AppTokenCache</c> once the app is created.
        /// </summary>
        public TokenCache() : this((IServiceBundle)null)
        {
        }

        internal TokenCache(IServiceBundle serviceBundle)
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

            // Must happen last, this code can access things like _accessor and such above.
            ServiceBundle = serviceBundle;
        }

        /// <summary>
        /// This method is so we can inject test ILegacyCachePersistence...
        /// </summary>
        /// <param name="serviceBundle"></param>
        /// <param name="legacyCachePersistenceForTest"></param>
        internal TokenCache(IServiceBundle serviceBundle, ILegacyCachePersistence legacyCachePersistenceForTest)
            : this(serviceBundle)
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
            ISet<string> environmentAliases,
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

        private static List<MsalAccessTokenCacheItem> FilterByHomeAccountTenantOrAssertion(AuthenticationRequestParameters requestParams, List<MsalAccessTokenCacheItem> tokenCacheItems)
        {
            // this is OBO flow. match the cache entry with assertion hash,
            // Authority, ScopeSet and client Id.
            if (requestParams.UserAssertion != null)
            {
                return tokenCacheItems.FilterWithLogging(item =>
                                !string.IsNullOrEmpty(item.UserAssertionHash) &&
                                item.UserAssertionHash.Equals(requestParams.UserAssertion.AssertionHash, StringComparison.OrdinalIgnoreCase),
                                requestParams.RequestContext.Logger,
                                "Filtering by user assertion id");
            }

            if (!requestParams.IsClientCredentialRequest)
            {
                tokenCacheItems = tokenCacheItems.FilterWithLogging(item => item.HomeAccountId.Equals(
                                requestParams.Account?.HomeAccountId?.Identifier, StringComparison.OrdinalIgnoreCase),
                                requestParams.RequestContext.Logger,
                                "Filtering by home account id");

                string tenantId = requestParams.Authority.GetTenantId();

                if (!String.IsNullOrEmpty(tenantId))
                {
                    requestParams.RequestContext.Logger.Info($"Tenant id: {tenantId}");
                    tokenCacheItems = tokenCacheItems.FilterWithLogging(item => item.TenantId.Equals(
                                   tenantId, StringComparison.OrdinalIgnoreCase),
                                    requestParams.RequestContext.Logger,
                                    "Filtering by tenant id");
                }
            }

            return tokenCacheItems;

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

        // TODO: TokenCache should not be responsible for knowing when to do instance dicovery or not
        // there should be an InstanceDiscoveryManager that encapsulates all the logic
        private async Task<InstanceDiscoveryMetadataEntry> GetCachedOrDiscoverAuthorityMetaDataAsync(
            string authority,
            RequestContext requestContext)
        {
            if (SupportsInstanceDicovery(authority))
            {
                var instanceDiscoveryMetadata = await ServiceBundle.AadInstanceDiscovery.GetMetadataEntryAsync(
                    new Uri(authority),
                    requestContext).ConfigureAwait(false);
                return instanceDiscoveryMetadata;
            }

            return null;
        }

        private bool SupportsInstanceDicovery(string authority)
        {
            var authorityType = Authority.GetAuthorityType(authority);
            return authorityType == AuthorityType.Aad ||
                // TODO: Not all discovery logic checks for this condition, this is a bug simialar to
                // https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1037
                (authorityType == AuthorityType.B2C &&
                    Authority.GetEnviroment(authority).Equals(AzurePublicEnv));
        }

        private InstanceDiscoveryMetadataEntry GetCachedAuthorityMetaData(string authority)
        {
            if (ServiceBundle?.AadInstanceDiscovery == null)
            {
                return null;
            }

            InstanceDiscoveryMetadataEntry instanceDiscoveryMetadata = null;
            var authorityType = Authority.GetAuthorityType(authority);
            if (authorityType == AuthorityType.Aad || authorityType == AuthorityType.B2C)
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

        private bool RtMatchesAccount(MsalRefreshTokenCacheItem rtItem, MsalAccountCacheItem account)
        {
            bool homeAccIdMatch = rtItem.HomeAccountId.Equals(account.HomeAccountId, StringComparison.OrdinalIgnoreCase);
            bool clientIdMatch =
                rtItem.IsFRT || // Cannot filter by client ID if the RT can be used by multiple clients
                rtItem.ClientId.Equals(ClientId, StringComparison.OrdinalIgnoreCase);

            return homeAccIdMatch && clientIdMatch;
        }

        /// <summary>
        /// Tries to get the env aliases of the authority for selecting accounts.
        /// This can be done without network discovery if all the accounts belong to known envs.
        /// If the list becomes stale (i.e. new env is introduced), GetAccounts will perform InstanceDiscovery
        /// The list of known envs should not be used in any other scenario!
        /// </summary>
        private async Task<IEnumerable<string>> GetEnvAliasesTryAvoidNetworkCallAsync(
            string authority,
            ISet<string> msalEnvs,
            ISet<string> adalEnvs,
            RequestContext requestContext)
        {
            var knownAadAliases = new List<HashSet<string>>()
            {
                new HashSet<string>(new[] { AzurePublicEnv, "login.windows.net", "login.microsoft.com", "sts.windows.net" }),
                new HashSet<string>(new[] { "login.partner.microsoftonline.cn", "login.chinacloudapi.cn" }),
                new HashSet<string>(new[] { "login.microsoftonline.de" }),
                new HashSet<string>(new[] { "login.microsoftonline.us", "login.usgovcloudapi.net" }),
                new HashSet<string>(new[] { "login-us.microsoftonline.com" }),
            };

            var envFromRequest = Authority.GetEnviroment(authority);
            var aliases = knownAadAliases
                .FirstOrDefault(cloudAliases => cloudAliases.ContainsOrdinalIgnoreCase(envFromRequest));

            bool canAvoidInstanceDiscovery =
                 aliases != null &&
                 (msalEnvs?.All(env => aliases.ContainsOrdinalIgnoreCase(env)) ?? true) &&
                 (adalEnvs?.All(env => aliases.ContainsOrdinalIgnoreCase(env)) ?? true);

            if (canAvoidInstanceDiscovery)
            {
                return await Task.FromResult(aliases).ConfigureAwait(false);
            }

            var instanceDiscoveryResult = await GetCachedOrDiscoverAuthorityMetaDataAsync(authority, requestContext)
                .ConfigureAwait(false);

            return instanceDiscoveryResult?.Aliases ?? new[] { envFromRequest };
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
