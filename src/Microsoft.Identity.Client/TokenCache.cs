// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Cache.Keys;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.Mats.Internal.Events;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.PlatformsCommon.Factories;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

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
        private const string AzurePublicEnv = "login.microsoftonline.com";
        private const int DefaultExpirationBufferInMinutes = 5;

        private ICoreLogger _logger => ServiceBundle.DefaultLogger;

        private readonly ITokenCacheBlobStorage _defaultTokenCacheBlobStorage;
        private readonly IFeatureFlags _featureFlags;

        private TokenCacheCallback _userConfiguredBeforeAccess;
        private TokenCacheCallback _userConfiguredAfterAccess;
        private TokenCacheCallback _userConfiguredBeforeWrite;

        internal IServiceBundle ServiceBundle { get; }

        private readonly ITokenCacheAccessor _accessor;

        internal ILegacyCachePersistence LegacyCachePersistence { get; }

        ITokenCacheAccessor ITokenCacheInternal.Accessor => _accessor;
        ILegacyCachePersistence ITokenCacheInternal.LegacyPersistence => LegacyCachePersistence;

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
            LegacyCachePersistence = proxy.CreateLegacyCachePersistence();

#if iOS
            SetIosKeychainSecurityGroup(ServiceBundle.Config.IosKeychainSecurityGroup);
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

        /// <summary>
        /// Notification for certain token cache interactions during token acquisition. This delegate is
        /// used in particular to provide a custom token cache serialization
        /// </summary>
        /// <param name="args">Arguments related to the cache item impacted</param>
        [Obsolete("Use Microsoft.Identity.Client.TokenCacheCallback instead. See https://aka.msa/msal-net-3x-cache-breaking-change", true)]
        public delegate void TokenCacheNotification(TokenCacheNotificationArgs args);

        internal readonly object LockObject = new object();

        object ITokenCacheInternal.LockObject => LockObject;

        private volatile bool _hasStateChanged;

        internal string ClientId => ServiceBundle.Config.ClientId;

        #region Notifications
        /// <summary>
        /// Notification method called before any library method accesses the cache.
        /// </summary>
        internal TokenCacheCallback BeforeAccess
        {
            get => UserHasConfiguredBlobSerialization() ?
                    _userConfiguredBeforeAccess :
                    _defaultTokenCacheBlobStorage.OnBeforeAccess;
            set => _userConfiguredBeforeAccess = value;
        }

        /// <summary>
        /// Notification method called before any library method writes to the cache. This notification can be used to reload
        /// the cache state from a row in database and lock that row. That database row can then be unlocked in the
        /// <see cref="AfterAccess"/>notification.
        /// </summary>
        internal TokenCacheCallback BeforeWrite
        {
            get => UserHasConfiguredBlobSerialization() ?
                    _userConfiguredBeforeWrite :
                    _defaultTokenCacheBlobStorage.OnBeforeWrite;
            set => _userConfiguredBeforeWrite = value;
        }

        /// <summary>
        /// Notification method called after any library method accesses the cache.
        /// </summary>
        internal TokenCacheCallback AfterAccess
        {
            get => UserHasConfiguredBlobSerialization() ?
                    _userConfiguredAfterAccess :
                    _defaultTokenCacheBlobStorage.OnAfterAccess;
            set => _userConfiguredAfterAccess = value;
        }

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

        #endregion

        Tuple<MsalAccessTokenCacheItem, MsalIdTokenCacheItem> ITokenCacheInternal.SaveTokenResponse(
            AuthenticationRequestParameters requestParams,
            MsalTokenResponse response)
        {
           // TODO: ensure that instance metadata has occured, otherwise we will use

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
                    try
                    {
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
                            if (!_featureFlags.IsFociEnabled)
                            {
                                msalRefreshTokenCacheItem.FamilyId = null;
                            }

                            requestParams.RequestContext.Logger.Info("Saving RT in cache...");
                            _accessor.SaveRefreshToken(msalRefreshTokenCacheItem);
                        }

                        UpdateAppMetadata(requestParams.ClientId, preferredEnvironmentHost, response.FamilyId);

                        // save RT in ADAL cache for public clients
                        // do not save RT in ADAL cache for MSAL B2C scenarios
                        if (!requestParams.IsClientCredentialRequest && !requestParams.AuthorityInfo.AuthorityType.Equals(AuthorityType.B2C))
                        {
                            CacheFallbackOperations.WriteAdalRefreshToken(
                                _logger,
                                LegacyCachePersistence,
                                msalRefreshTokenCacheItem,
                                msalIdTokenCacheItem,
                                Authority.CreateAuthorityUriWithHost(requestParams.TenantUpdatedCanonicalAuthority, preferredEnvironmentHost),
                                msalIdTokenCacheItem.IdToken.ObjectId, response.Scope);
                        }

                    }
                    finally
                    {
                        OnAfterAccess(args);
                    }

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

        private void UpdateAppMetadata(string clientId, string environment, string familyId)
        {
            if (_featureFlags.IsFociEnabled)
            {
                var metadataCacheItem = new MsalAppMetadataCacheItem(clientId, environment, familyId);
                _accessor.SaveAppMetadata(metadataCacheItem);
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
            foreach (var accessToken in _accessor.GetAllAccessTokens())
            {
                if (accessToken.ClientId.Equals(ClientId, StringComparison.OrdinalIgnoreCase) &&
                    environmentAliases.Contains(accessToken.Environment) &&
                    accessToken.TenantId.Equals(tenantId, StringComparison.OrdinalIgnoreCase) &&
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

                if (requestParams.AuthorityInfo.AuthorityType != AuthorityType.B2C)
                {
                    preferredEnvironmentAlias = instanceDiscoveryMetadataEntry.PreferredCache;
                }
            }

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

                List<MsalAccessTokenCacheItem> tokenCacheItems;

                OnBeforeAccess(args);
                try
                {
                    // filtered by client id.
                    tokenCacheItems = ((ITokenCacheInternal)this).GetAllAccessTokens(true).ToList();
                }
                finally
                {
                    OnAfterAccess(args);
                }

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

                    tokenCacheItems = FilterToTenantIdSpecifiedByAuthenticationRequest(requestParams, tokenCacheItems).ToList();
                }

                // no match found after initial filtering
                if (!tokenCacheItems.Any())
                {
                    requestParams.RequestContext.Logger.Info("No matching entry found for user or assertion");
                    return null;
                }

                requestParams.RequestContext.Logger.Info("Matching entry count -" + tokenCacheItems.Count);

                IEnumerable<MsalAccessTokenCacheItem> filteredItems =
                    tokenCacheItems.Where(item => ScopeHelper.ScopeContains(item.ScopeSet, requestParams.Scope)).ToList();

                requestParams.RequestContext.Logger.Info("Matching entry count after filtering by scopes - " + filteredItems.Count());

                // filter by authority
                var filteredByPreferredAlias =
                    filteredItems.Where
                    (item => item.Environment.Equals(preferredEnvironmentAlias, StringComparison.OrdinalIgnoreCase)).ToList();

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

                    throw new MsalClientException(
                        MsalError.MultipleTokensMatchedError,
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

        private IEnumerable<MsalAccessTokenCacheItem> FilterToTenantIdSpecifiedByAuthenticationRequest(
            AuthenticationRequestParameters requestParams, IEnumerable<MsalAccessTokenCacheItem> tokenCacheItems)
        {
            var items = tokenCacheItems.ToList();
            if (items.ToList().Count <= 1)
            {
                return items;
            }

            requestParams.RequestContext.Logger.Info(
                "Filtering by tenant specified in the authentication request parameters...");

            var authorityCacheMatches = items.Where(
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

        async Task<MsalRefreshTokenCacheItem> ITokenCacheInternal.FindRefreshTokenAsync(
            AuthenticationRequestParameters requestParams,
            string familyId)
        {
            var cacheEvent = new CacheEvent(
                CacheEvent.TokenCacheLookup,
                requestParams.RequestContext.TelemetryCorrelationId)
                {
                    TokenType = CacheEvent.TokenTypes.RT
                };

            using (ServiceBundle.TelemetryManager.CreateTelemetryHelper(cacheEvent))
            {
                if (requestParams.Authority == null)
                {
                    return null;
                }

                var instanceDiscoveryMetadataEntry = await GetCachedOrDiscoverAuthorityMetaDataAsync(requestParams.AuthorityInfo.CanonicalAuthority,
                    requestParams.RequestContext).ConfigureAwait(false);

                var environmentAliases = GetEnvironmentAliases(requestParams.AuthorityInfo.CanonicalAuthority,
                    instanceDiscoveryMetadataEntry);

                var preferredEnvironmentHost = GetPreferredEnvironmentHost(requestParams.AuthorityInfo.Host,
                    instanceDiscoveryMetadataEntry);

                lock (LockObject)
                {
                    requestParams.RequestContext.Logger.Info("Looking up refresh token in the cache..");

                    TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                    {
                        TokenCache = this,
                        ClientId = ClientId,
                        Account = requestParams.Account
                    };

                    // make sure to check preferredEnvironmentHost first
                    var allEnvAliases = new List<string>() { preferredEnvironmentHost };
                    allEnvAliases.AddRange(environmentAliases);

                    var keysAcrossEnvs = allEnvAliases.Select(ea => new MsalRefreshTokenCacheKey(
                        ea,
                        requestParams.ClientId,
                        requestParams.Account?.HomeAccountId?.Identifier,
                        familyId));


                    OnBeforeAccess(args);
                    try
                    {
                        // Try to load from all env aliases, but stop at the first valid one
                        MsalRefreshTokenCacheItem msalRefreshTokenCacheItem = keysAcrossEnvs
                            .Select(key => _accessor.GetRefreshToken(key))
                            .FirstOrDefault(item => item != null);

                        requestParams.RequestContext.Logger.Info("Refresh token found in the cache? - " + (msalRefreshTokenCacheItem != null));

                        if (msalRefreshTokenCacheItem != null)
                        {
                            return msalRefreshTokenCacheItem;
                        }

                        requestParams.RequestContext.Logger.Info("Checking ADAL cache for matching RT");

                        // ADAL legacy cache does not store FRTs
                        if (requestParams.Account != null && string.IsNullOrEmpty(familyId))
                        {
                            return CacheFallbackOperations.GetAdalEntryForMsal(
                                _logger,
                                LegacyCachePersistence,
                                preferredEnvironmentHost,
                                environmentAliases,
                                requestParams.ClientId,
                                requestParams.LoginHint,
                                requestParams.Account.HomeAccountId?.Identifier,
                                null);
                        }

                        return null;

                    }
                    finally
                    {
                        OnAfterAccess(args);
                    }
                }
            }
        }

        async Task<bool?> ITokenCacheInternal.IsFociMemberAsync(AuthenticationRequestParameters requestParams, string familyId)
        {
            var logger = requestParams.RequestContext.Logger;
            if (requestParams?.AuthorityInfo?.CanonicalAuthority == null)
            {
                logger.Warning("No authority details, can't check app metadta. Returning unkown");
                return null;
            }

            var instanceDiscoveryMetadataEntry = await GetCachedOrDiscoverAuthorityMetaDataAsync(
                   requestParams.AuthorityInfo.CanonicalAuthority,
                   requestParams.RequestContext).ConfigureAwait(false);

            var environmentAliases = GetEnvironmentAliases(
                requestParams.AuthorityInfo.CanonicalAuthority,
                instanceDiscoveryMetadataEntry);

            TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
            {
                TokenCache = this,
                ClientId = ClientId,
                Account = requestParams?.Account,
                HasStateChanged = false
            };

            //TODO: bogavril - is the env ok here? Can I cache it or pass it in?
            MsalAppMetadataCacheItem appMetadata;
            lock (LockObject)
            {
                OnBeforeAccess(args);

                appMetadata =
                    environmentAliases
                    .Select(env => _accessor.GetAppMetadata(new MsalAppMetadataCacheKey(ClientId, env)))
                    .FirstOrDefault(item => item != null);

                OnAfterAccess(args);
            }

            if (appMetadata == null)
            {
                logger.Warning("No app metadata found. Returning unkown");
                return null;
            }

            return appMetadata.FamilyId == familyId;
        }


        /// <inheritdoc />
        public void SetIosKeychainSecurityGroup(string securityGroup)
        {
#if iOS
            _accessor.SetiOSKeychainSecurityGroup(securityGroup);
            (LegacyCachePersistence as Microsoft.Identity.Client.Platforms.iOS.iOSLegacyCachePersistence).SetKeychainSecurityGroup(securityGroup);
#endif
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
                try
                {
                    var idToken = _accessor.GetIdToken(msalIdTokenCacheKey);
                    return idToken;
                }
                finally
                {
                    OnAfterAccess(args);
                }
            }
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

        /// <remarks>
        /// Get accounts should not make a network call, if possible.
        /// </remarks>
        async Task<IEnumerable<IAccount>> ITokenCacheInternal.GetAccountsAsync(string authority, RequestContext requestContext)
        {
            var environment = Authority.GetEnviroment(authority);

            FetchAllAccountItemsFromCache(
                out IEnumerable<MsalRefreshTokenCacheItem> rtCacheItems,
                out IEnumerable<MsalAccountCacheItem> accountCacheItems,
                out AdalUsersForMsal adalUsersResult);

            // Multi-cloud support - must filter by env.
            // Use all env aliases to filter, in case PreferredCacheEnv changes in the future
            ISet<string> existingEnvs = new HashSet<string>(
                accountCacheItems.Select(aci => aci.Environment),
                StringComparer.OrdinalIgnoreCase);

            var aliases = await GetEnvAliasesTryAvoidNetworkCallAsync(
                authority,
                adalUsersResult.GetAdalUserEnviroments(),
                existingEnvs,
                requestContext)
                .ConfigureAwait(false);

            rtCacheItems = rtCacheItems.Where(rt => aliases.ContainsOrdinalIgnoreCase(rt.Environment));
            accountCacheItems = accountCacheItems.Where(acc => aliases.ContainsOrdinalIgnoreCase(acc.Environment));

            IDictionary<string, Account> clientInfoToAccountMap = new Dictionary<string, Account>();
            foreach (MsalRefreshTokenCacheItem rtItem in rtCacheItems)
            {
                foreach (MsalAccountCacheItem account in accountCacheItems)
                {
                    if (rtItem.HomeAccountId.Equals(account.HomeAccountId, StringComparison.OrdinalIgnoreCase))
                    {
                        clientInfoToAccountMap[rtItem.HomeAccountId] = new Account(
                            account.HomeAccountId,
                            account.PreferredUsername,
                            environment);  // Preserve the env passed in by the user

                        break;
                    }
                }
            }

            List<IAccount> accounts = UpdateWithAdalAccounts(
                environment,
                aliases,
                adalUsersResult,
                clientInfoToAccountMap);

            return accounts;
        }

        private void FetchAllAccountItemsFromCache(
            out IEnumerable<MsalRefreshTokenCacheItem> tokenCacheItems,
            out IEnumerable<MsalAccountCacheItem> accountCacheItems,
            out AdalUsersForMsal adalUsersResult)
        {
            bool filterByClientId = !_featureFlags.IsFociEnabled;

            lock (LockObject)
            {
                var args = new TokenCacheNotificationArgs
                {
                    TokenCache = this,
                    ClientId = ClientId,
                    Account = null
                };

                OnBeforeAccess(args);
                try
                {
                    tokenCacheItems = ((ITokenCacheInternal)this).GetAllRefreshTokens(filterByClientId);
                    accountCacheItems = ((ITokenCacheInternal)this).GetAllAccounts();

                    adalUsersResult = CacheFallbackOperations.GetAllAdalUsersForMsal(
                        _logger,
                        LegacyCachePersistence,
                        ClientId);
                }
                finally
                {
                    OnAfterAccess(args);
                }
            }
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


        IEnumerable<MsalRefreshTokenCacheItem> ITokenCacheInternal.GetAllRefreshTokens(bool filterByClientId)
        {
            lock (LockObject)
            {
                var refreshTokens = _accessor.GetAllRefreshTokens();
                return filterByClientId
                    ? refreshTokens.Where(x => x.ClientId.Equals(ClientId, StringComparison.OrdinalIgnoreCase))
                    : refreshTokens;
            }
        }

        IEnumerable<MsalAccessTokenCacheItem> ITokenCacheInternal.GetAllAccessTokens(bool filterByClientId)
        {
            lock (LockObject)
            {
                var accessTokens = _accessor.GetAllAccessTokens();
                return filterByClientId
                    ? accessTokens.Where(x => x.ClientId.Equals(ClientId, StringComparison.OrdinalIgnoreCase))
                    : accessTokens;
            }
        }

        IEnumerable<MsalIdTokenCacheItem> ITokenCacheInternal.GetAllIdTokens(bool filterByClientId)
        {
            lock (LockObject)
            {
                var idTokens = _accessor.GetAllIdTokens();
                return filterByClientId
                    ? idTokens.Where(x => x.ClientId.Equals(ClientId, StringComparison.OrdinalIgnoreCase))
                    : idTokens;
            }
        }

        IEnumerable<MsalAccountCacheItem> ITokenCacheInternal.GetAllAccounts()
        {
            lock (LockObject)
            {
                return _accessor.GetAllAccounts();
            }
        }


        #region Removal methods
        void ITokenCacheInternal.RemoveAccount(IAccount account, RequestContext requestContext)
        {
            lock (LockObject)
            {
                requestContext.Logger.Info("Removing user from cache..");

                try
                {
                    var args = new TokenCacheNotificationArgs
                    {
                        TokenCache = this,
                        ClientId = ClientId,
                        Account = account,
                        HasStateChanged = true
                    };

                    OnBeforeAccess(args);
                    try
                    {
                        OnBeforeWrite(args);

                        ((ITokenCacheInternal)this).RemoveMsalAccount(account, requestContext);
                        RemoveAdalUser(account);
                    }
                    finally
                    {
                        OnAfterAccess(args);
                    }
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

            bool filterByClientId = !_featureFlags.IsFociEnabled;

            // Delete ALL refresh tokens associated with this account
            var allRefreshTokens = ((ITokenCacheInternal)this).GetAllRefreshTokens(filterByClientId)
                .Where(item => item.HomeAccountId.Equals(account.HomeAccountId.Identifier, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (MsalRefreshTokenCacheItem refreshTokenCacheItem in allRefreshTokens)
            {
                _accessor.DeleteRefreshToken(refreshTokenCacheItem.GetKey());
            }

            requestContext.Logger.Info("Deleted refresh token count - " + allRefreshTokens.Count);
            IList<MsalAccessTokenCacheItem> allAccessTokens = ((ITokenCacheInternal)this).GetAllAccessTokens(filterByClientId)
                .Where(item => item.HomeAccountId.Equals(account.HomeAccountId.Identifier, StringComparison.OrdinalIgnoreCase))
                .ToList();
            foreach (MsalAccessTokenCacheItem accessTokenCacheItem in allAccessTokens)
            {
                _accessor.DeleteAccessToken(accessTokenCacheItem.GetKey());
            }

            requestContext.Logger.Info("Deleted access token count - " + allAccessTokens.Count);

            var allIdTokens = ((ITokenCacheInternal)this).GetAllIdTokens(filterByClientId)
                .Where(item => item.HomeAccountId.Equals(account.HomeAccountId.Identifier, StringComparison.OrdinalIgnoreCase))
                .ToList();
            foreach (MsalIdTokenCacheItem idTokenCacheItem in allIdTokens)
            {
                _accessor.DeleteIdToken(idTokenCacheItem.GetKey());
            }

            requestContext.Logger.Info("Deleted Id token count - " + allIdTokens.Count);

            ((ITokenCacheInternal)this).GetAllAccounts()
                .Where(item => item.HomeAccountId.Equals(account.HomeAccountId.Identifier, StringComparison.OrdinalIgnoreCase) &&
                               item.PreferredUsername.Equals(account.Username, StringComparison.OrdinalIgnoreCase))
                .ToList()
                .ForEach(accItem => _accessor.DeleteAccount(accItem.GetKey()));

        }

        private void RemoveAdalUser(IAccount account)
        {
            CacheFallbackOperations.RemoveAdalUser(
                _logger,
                LegacyCachePersistence,
                ClientId,
                account.Username,
                account.HomeAccountId.Identifier);
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

                OnBeforeAccess(args);
                try
                {
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
            IDictionary<AdalTokenCacheKey, AdalResultWrapper> dictionary = AdalCacheOperations.Deserialize(_logger, LegacyCachePersistence.LoadCache());
            dictionary.Clear();
            LegacyCachePersistence.WriteCache(AdalCacheOperations.Serialize(_logger, dictionary));
        }

        void ITokenCacheInternal.ClearMsalCache()
        {
            _accessor.Clear();
        }

        #endregion

        #region Serialization

        private bool UserHasConfiguredBlobSerialization()
        {
            return _userConfiguredBeforeAccess != null ||
                _userConfiguredBeforeAccess != null ||
                _userConfiguredBeforeWrite != null;
        }

#if !ANDROID_BUILDTIME && !iOS_BUILDTIME

        // Unkown token cache data support for forwards compatibility.
        private IDictionary<string, JToken> _unknownNodes;

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

        /// <summary>
        /// Serializes the entire token cache in both the ADAL V3 and unified cache formats.
        /// </summary>
        /// <returns>Serialized token cache <see cref="CacheData"/></returns>
        /// <remarks>
        /// <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/> is compatible with other MSAL libraries such as MSAL for Python and MSAL for Java.
        /// </remarks>
        [Obsolete("This is expected to be removed in MSAL.NET v3 and ADAL.NET v5. We recommend using SerializeMsalV3/DeserializeMsalV3. Read more: https://aka.ms/msal-net-3x-cache-breaking-change", false)]
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

        /// <summary>
        /// Deserializes the token cache from a serialization blob in both format (ADAL V3 format, and unified cache format)
        /// </summary>
        /// <param name="cacheData">Array of bytes containing serialize cache data</param>
        /// <remarks>
        /// <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/> is compatible with other MSAL libraries such as MSAL for Python and MSAL for Java.
        /// </remarks>
        [Obsolete("This is expected to be removed in MSAL.NET v3 and ADAL.NET v5. We recommend using SerializeMsalV3/DeserializeMsalV3. Read more: https://aka.ms/msal-net-3x-cache-breaking-change", false)]
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
        /// Serializes using the <see cref="SerializeMsalV2"/> serializer.
        /// Obsolete: Please use specialized Serialization methods.
        /// <see cref="SerializeMsalV2"/> replaces <see cref="Serialize"/>.
        /// <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/> Is our recommended way of serializing/deserializing.
        /// <see cref="SerializeAdalV3"/> For interoperability with ADAL.NET v3.
        /// </summary>
        /// <returns>array of bytes, <see cref="SerializeMsalV2"/></returns>
        /// <remarks>
        /// <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/> is compatible with other MSAL libraries such as MSAL for Python and MSAL for Java.
        /// </remarks>
        [Obsolete("This is expected to be removed in MSAL.NET v3 and ADAL.NET v5. We recommend using SerializeMsalV3/DeserializeMsalV3. Read more: https://aka.ms/msal-net-3x-cache-breaking-change", false)]
        public byte[] Serialize()
        {
            return SerializeMsalV2();
        }

        /// <summary>
        /// Deserializes the token cache from a serialization blob in the unified cache format
        /// Obsolete: Please use specialized Deserialization methods.
        /// <see cref="DeserializeMsalV2"/> replaces <see cref="Deserialize"/>
        /// <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/> Is our recommended way of serializing/deserializing.
        /// <see cref="DeserializeAdalV3"/> For interoperability with ADAL.NET v3
        /// </summary>
        /// <param name="msalV2State">Array of bytes containing serialized MSAL.NET V2 cache data</param>
        /// <remarks>
        /// <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/> is compatible with other MSAL libraries such as MSAL for Python and MSAL for Java.
        /// <paramref name="msalV2State"/>Is a Json blob containing access tokens, refresh tokens, id tokens and accounts information.
        /// </remarks>
        [Obsolete("This is expected to be removed in MSAL.NET v3 and ADAL.NET v5. We recommend using SerializeMsalV3/DeserializeMsalV3. Read more: https://aka.ms/msal-net-3x-cache-breaking-change", false)]
        public void Deserialize(byte[] msalV2State)
        {
            DeserializeMsalV2(msalV2State);
        }

        /// <summary>
        /// Serializes the token cache to the ADAL.NET 3.x cache format.
        /// If you need to maintain SSO between an application using ADAL 3.x or MSAL 2.x and this application using MSAL 3.x,
        /// you might also want to serialize and deserialize with <see cref="SerializeAdalV3"/>/<see cref="DeserializeAdalV3"/> or <see cref="SerializeMsalV2"/>/<see cref="DeserializeMsalV2"/>,
        /// otherwise just use <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/>.
        /// </summary>
        /// <returns>array of bytes containing the serialized ADAL.NET V3 cache data</returns>
        /// <remarks>
        /// <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/> is compatible with other MSAL libraries such as MSAL for Python and MSAL for Java.
        /// </remarks>
        public byte[] SerializeAdalV3()
        {
            GuardOnMobilePlatforms();

            lock (LockObject)
            {
                return LegacyCachePersistence.LoadCache();
            }
        }

        /// <summary>
        /// Deserializes the token cache to the ADAL.NET 3.x cache format.
        /// If you need to maintain SSO between an application using ADAL 3.x or MSAL 2.x and this application using MSAL 3.x,
        /// you might also want to serialize and deserialize with <see cref="SerializeAdalV3"/>/<see cref="DeserializeAdalV3"/> or <see cref="SerializeMsalV2"/>/<see cref="DeserializeMsalV2"/>,
        /// otherwise just use <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/>.
        /// </summary>
        /// <param name="adalV3State">Array of bytes containing serialized Adal.NET V3 cache data</param>
        /// <remarks>
        /// <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/> is compatible with other MSAL libraries such as MSAL for Python and MSAL for Java.
        /// </remarks>
        public void DeserializeAdalV3(byte[] adalV3State)
        {
            GuardOnMobilePlatforms();

            lock (LockObject)
            {
                LegacyCachePersistence.WriteCache(adalV3State);
            }
        }

        /// <summary>
        /// Serializes the token cache to the MSAL.NET 2.x unified cache format, which is compatible with ADAL.NET v4 and other MSAL.NET v2 applications.
        /// If you need to maintain SSO between an application using ADAL 3.x or MSAL 2.x and this application using MSAL 3.x,
        /// you might also want to serialize and deserialize with <see cref="SerializeAdalV3"/>/<see cref="DeserializeAdalV3"/> or <see cref="SerializeMsalV2"/>/<see cref="DeserializeMsalV2"/>,
        /// otherwise just use <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/>.
        /// </summary>
        /// <returns>array of bytes containing the serialized MsalV2 cache</returns>
        /// <remarks>
        /// <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/> is compatible with other MSAL libraries such as MSAL for Python and MSAL for Java.
        /// </remarks>
        public byte[] SerializeMsalV2()
        {
            GuardOnMobilePlatforms();
            // reads the underlying in-memory dictionary and dumps out the content as a JSON
            lock (LockObject)
            {
                return new TokenCacheDictionarySerializer(_accessor).Serialize(_unknownNodes);
            }
        }

        /// <summary>
        /// Deserializes the token cache to the MSAL.NET 2.x unified cache format, which is compatible with ADAL.NET v4 and other MSAL.NET v2 applications.
        /// If you need to maintain SSO between an application using ADAL 3.x or MSAL 2.x and this application using MSAL 3.x,
        /// you might also want to serialize and deserialize with <see cref="SerializeAdalV3"/>/<see cref="DeserializeAdalV3"/> or <see cref="SerializeMsalV2"/>/<see cref="DeserializeMsalV2"/>,
        /// otherwise just use <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/>.
        /// </summary>
        /// <param name="msalV2State">Array of bytes containing serialized MsalV2 cache data</param>
        /// <remarks>
        /// <paramref name="msalV2State"/>Is a Json blob containing access tokens, refresh tokens, id tokens and accounts information.
        /// </remarks>
        /// <remarks>
        /// <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/> is compatible with other MSAL libraries such as MSAL for Python and MSAL for Java.
        /// </remarks>
        public void DeserializeMsalV2(byte[] msalV2State)
        {
            GuardOnMobilePlatforms();

            if (msalV2State == null || msalV2State.Length == 0)
            {
                return;
            }

            lock (LockObject)
            {
                _unknownNodes = new TokenCacheDictionarySerializer(_accessor).Deserialize(msalV2State);
            }
        }

        /// <summary>
        /// Serializes the token cache, in the MSAL.NET V3 cache format.
        /// If you need to maintain SSO between an application using ADAL 3.x or MSAL 2.x and this application using MSAL 3.x,
        /// you might also want to serialize and deserialize with <see cref="SerializeAdalV3"/>/<see cref="DeserializeAdalV3"/> or <see cref="SerializeMsalV2"/>/<see cref="DeserializeMsalV2"/>,
        /// otherwise just use <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/>.
        /// </summary>
        /// <returns>Byte stream representation of the cache</returns>
        /// <remarks>
        /// <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/> is compatible with other MSAL libraries such as MSAL for Python and MSAL for Java.
        /// </remarks>
        public byte[] SerializeMsalV3()
        {
            GuardOnMobilePlatforms();

            lock (LockObject)
            {
                return new TokenCacheJsonSerializer(_accessor).Serialize(_unknownNodes);
            }
        }

        /// <summary>
        /// De-serializes from the MSAL.NET V3 cache format.
        /// If you need to maintain SSO between an application using ADAL 3.x or MSAL 2.x and this application using MSAL 3.x,
        /// you might also want to serialize and deserialize with <see cref="SerializeAdalV3"/>/<see cref="DeserializeAdalV3"/> or <see cref="SerializeMsalV2"/>/<see cref="DeserializeMsalV2"/>,
        /// otherwise just use <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/>.
        /// </summary>
        /// <param name="msalV3State">Byte stream representation of the cache</param>
        /// <remarks>
        /// This format is compatible with other MSAL libraries such as MSAL for Python and MSAL for Java.
        /// </remarks>
        /// <remarks>
        /// <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/> is compatible with other MSAL libraries such as MSAL for Python and MSAL for Java.
        /// </remarks>
        public void DeserializeMsalV3(byte[] msalV3State)
        {
            GuardOnMobilePlatforms();

            if (msalV3State == null || msalV3State.Length == 0)
            {
                return;
            }

            lock (LockObject)
            {
                _unknownNodes = new TokenCacheJsonSerializer(_accessor).Deserialize(msalV3State);
            }
        }



        private static void GuardOnMobilePlatforms()
        {
#if ANDROID || iOS
        throw new PlatformNotSupportedException("You should not use these TokenCache methods object on mobile platforms. " +
            "They meant to allow applications to define their own storage strategy on .net desktop and non-mobile platforms such as .net core. " +
            "On mobile platforms, a secure and performant storage mechanism is implemeted by MSAL. " +
            "For more details about custom token cache serialization, visit https://aka.ms/msal-net-serialization");
#endif
        }


#endif // !ANDROID_BUILDTIME && !iOS_BUILDTIME
        #endregion
    }
}
