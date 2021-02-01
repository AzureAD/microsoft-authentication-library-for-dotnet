// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Cache.Keys;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client
{
    internal interface ITokenCacheInternal : ITokenCache, ITokenCacheSerializer
    {
        SemaphoreSlim Semaphore { get; }
        ILegacyCachePersistence LegacyPersistence { get; }
        ITokenCacheAccessor Accessor { get; }

        #region High-Level cache operations
        Task RemoveAccountAsync(IAccount account, RequestContext requestContext);
        Task<IEnumerable<IAccount>> GetAccountsAsync(AuthenticationRequestParameters requestParameters);
      
        Task<Tuple<MsalAccessTokenCacheItem, MsalIdTokenCacheItem, Account>> SaveTokenResponseAsync(
            AuthenticationRequestParameters requestParams,
            MsalTokenResponse response);

        Task<MsalAccessTokenCacheItem> FindAccessTokenAsync(AuthenticationRequestParameters requestParams);
        MsalIdTokenCacheItem GetIdTokenCacheItem(MsalIdTokenCacheKey msalIdTokenCacheKey);

        /// <summary>
        /// Returns a RT for the request. If familyId is specified, it tries to return the FRT.
        /// </summary>
        Task<MsalRefreshTokenCacheItem> FindRefreshTokenAsync(
            AuthenticationRequestParameters requestParams,
            string familyId = null);

        #endregion

        #region For test
        Task<IEnumerable<MsalAccessTokenCacheItem>> GetAllAccessTokensAsync(bool filterByClientId);
        Task<IEnumerable<MsalRefreshTokenCacheItem>> GetAllRefreshTokensAsync(bool filterByClientId);
        Task<IEnumerable<MsalIdTokenCacheItem>> GetAllIdTokensAsync(bool filterByClientId);
        Task<IEnumerable<MsalAccountCacheItem>> GetAllAccountsAsync();
        #endregion

        void RemoveMsalAccountWithNoLocks(IAccount account, RequestContext requestContext);

        /// <summary>
        /// FOCI - check in the app metadata to see if the app is part of the family
        /// </summary>
        /// <returns>null if unknown, true or false if app metadata has details</returns>
        Task<bool?> IsFociMemberAsync(AuthenticationRequestParameters requestParams, string familyId);     

        void SetIosKeychainSecurityGroup(string securityGroup);


        #region Cache notifications
        Task OnAfterAccessAsync(TokenCacheNotificationArgs args);
        Task OnBeforeAccessAsync(TokenCacheNotificationArgs args);
        Task OnBeforeWriteAsync(TokenCacheNotificationArgs args);

        bool IsApplicationCache { get; }

        /// <summary>
        /// Shows if MSAL's in-memory token cache has any kind of RT or non-expired AT. Does not trigger a cache notification.
        /// Ignores ADAL's cache.
        /// </summary>
        bool HasTokensNoLocks();

        bool IsTokenCacheSerialized();

        #endregion
    }
}
