// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.CacheImpl;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.PlatformsCommon.Factories;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client
{
#if !SUPPORTS_CONFIDENTIAL_CLIENT
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
        internal const int ExpirationTooLongInDays = 10 * 365;

        private readonly IFeatureFlags _featureFlags;
        private volatile bool _hasStateChanged;

        internal ITokenCacheAccessor Accessor { get; set; }

        internal IServiceBundle ServiceBundle { get; }
        internal ILegacyCachePersistence LegacyCachePersistence { get; set; }

        /// <summary>
        /// Set to true on some platforms (UWP) where MSAL adds a serializer on its own.
        /// </summary>
        internal bool UsesDefaultSerialization { get; set; } = false;

        internal string ClientId => ServiceBundle.Config.ClientId;

        ITokenCacheAccessor ITokenCacheInternal.Accessor => Accessor;
        ILegacyCachePersistence ITokenCacheInternal.LegacyPersistence => LegacyCachePersistence;

        internal bool IsAppTokenCache { get; }
        bool ITokenCacheInternal.IsApplicationCache => IsAppTokenCache;

        private readonly OptionalSemaphoreSlim _semaphoreSlim;
        OptionalSemaphoreSlim ITokenCacheInternal.Semaphore => _semaphoreSlim;

        /// <summary>
        /// Constructor of a token cache. This constructor is left for compatibility with MSAL 2.x.
        /// The recommended way to get a cache is by using <see cref="IClientApplicationBase.UserTokenCache"/>
        /// and <c>IConfidentialClientApplication.AppTokenCache</c> once the app is created.
        /// </summary>
        [Obsolete("The recommended way to get a cache is by using IClientApplicationBase.UserTokenCache or IClientApplicationBase.AppTokenCache")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public TokenCache() : this((IServiceBundle)null, false, null)
        {
        }

        internal TokenCache(IServiceBundle serviceBundle, bool isApplicationTokenCache, ICacheSerializationProvider optionalDefaultSerializer = null)
        {
            if (serviceBundle == null)
                throw new ArgumentNullException(nameof(serviceBundle));

            // useRealSemaphore= false for MyApps and potentially for all apps when using non-singleton MSAL
            _semaphoreSlim = new OptionalSemaphoreSlim(useRealSemaphore: serviceBundle.Config.CacheSynchronizationEnabled);

            var proxy = serviceBundle?.PlatformProxy ?? PlatformProxyFactory.CreatePlatformProxy(null);
            Accessor = proxy.CreateTokenCacheAccessor(serviceBundle.Config.AccessorOptions, isApplicationTokenCache);
            _featureFlags = proxy.GetFeatureFlags();

            UsesDefaultSerialization = optionalDefaultSerializer != null;
            optionalDefaultSerializer?.Initialize(this);

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
        internal TokenCache(
            IServiceBundle serviceBundle,
            ILegacyCachePersistence legacyCachePersistenceForTest,
            bool isApplicationTokenCache,
            ICacheSerializationProvider optionalDefaultCacheSerializer = null)
            : this(serviceBundle, isApplicationTokenCache, optionalDefaultCacheSerializer)
        {
            LegacyCachePersistence = legacyCachePersistenceForTest;
        }

        /// <inheritdoc/>
        public void SetIosKeychainSecurityGroup(string securityGroup)
        {
#if iOS
            Accessor.SetiOSKeychainSecurityGroup(securityGroup);
            (LegacyCachePersistence as Microsoft.Identity.Client.Platforms.iOS.iOSLegacyCachePersistence).SetKeychainSecurityGroup(securityGroup);
#endif
        }

        private void UpdateAppMetadata(string clientId, string environment, string familyId)
        {
            if (_featureFlags.IsFociEnabled)
            {
                var metadataCacheItem = new MsalAppMetadataCacheItem(clientId, environment, familyId);
                Accessor.SaveAppMetadata(metadataCacheItem);
            }
        }

        /// <summary>
        /// delete all cache entries with intersecting scopes.
        /// this should not happen but we have this as a safe guard
        /// against multiple matches.
        /// </summary>
        private void DeleteAccessTokensWithIntersectingScopes(
            AuthenticationRequestParameters requestParams,
            IEnumerable<string> environmentAliases,
            string tenantId,
            HashSet<string> scopeSet,
            string homeAccountId,
            string tokenType)
        {
            if (requestParams.RequestContext.Logger.IsLoggingEnabled(LogLevel.Info))
            {
                requestParams.RequestContext.Logger.Info(
                    () => "Looking for scopes for the authority in the cache which intersect with " +
                    requestParams.Scope.AsSingleString());
            }

            var accessTokensToDelete = new List<MsalAccessTokenCacheItem>();
            var partitionKeyFromResponse = CacheKeyFactory.GetInternalPartitionKeyFromResponse(requestParams, homeAccountId);
            Debug.Assert(partitionKeyFromResponse != null || !requestParams.AppConfig.IsConfidentialClient, "On confidential client, cache must be partitioned.");

            foreach (var accessToken in Accessor.GetAllAccessTokens(partitionKeyFromResponse))
            {
                if (accessToken.ClientId.Equals(ClientId, StringComparison.OrdinalIgnoreCase) &&
                    environmentAliases.Contains(accessToken.Environment) &&
                    string.Equals(accessToken.TokenType ?? "", tokenType ?? "", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(accessToken.TenantId, tenantId, StringComparison.OrdinalIgnoreCase) &&
                    accessToken.ScopeSet.Overlaps(scopeSet))
                {
                    requestParams.RequestContext.Logger.Verbose(() => $"Intersecting scopes found: {scopeSet}");
                    accessTokensToDelete.Add(accessToken);
                }
            }

            requestParams.RequestContext.Logger.Info(() => "Intersecting scope entries count - " + accessTokensToDelete.Count);

            if (!requestParams.IsClientCredentialRequest &&
                requestParams.ApiId != ApiEvent.ApiIds.AcquireTokenForSystemAssignedManagedIdentity &&
                requestParams.ApiId != ApiEvent.ApiIds.AcquireTokenForUserAssignedManagedIdentity)
            {
                // filter by identifier of the user instead
                accessTokensToDelete.RemoveAll(
                            item => !item.HomeAccountId.Equals(homeAccountId, StringComparison.OrdinalIgnoreCase));
                requestParams.RequestContext.Logger.Info(() => "Matching entries after filtering by user - " + accessTokensToDelete.Count);
            }

            foreach (var cacheItem in accessTokensToDelete)
            {
                Accessor.DeleteAccessToken(cacheItem);
            }
        }

        private static string GetAccessTokenExpireLogMessageContent(MsalAccessTokenCacheItem msalAccessTokenCacheItem)
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

        private static bool FrtExists(IEnumerable<MsalRefreshTokenCacheItem> refreshTokens)
        {
            return refreshTokens.Any(rt => rt.IsFRT);
        }
    }
}
