// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Cache.Keys;
using Microsoft.Identity.Client.Core;
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
        Task<IEnumerable<IAccount>> GetAccountsAsync(string authority, RequestContext requestContext);

        /// <summary>
        /// Persists the AT and RT and updates app metadata (FOCI)
        /// </summary>
        /// <returns></returns>
        Task<Tuple<MsalAccessTokenCacheItem, MsalIdTokenCacheItem>> SaveTokenResponseAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            MsalTokenResponse msalTokenResponse);

        Task<MsalAccessTokenCacheItem> FindAccessTokenAsync(AuthenticationRequestParameters authenticationRequestParameters);
        Task<MsalIdTokenCacheItem> GetIdTokenCacheItemAsync(MsalIdTokenCacheKey getIdTokenItemKey, RequestContext requestContext);

        /// <summary>
        /// Returns a RT for the request. If familyId is specified, it tries to return the FRT.
        /// </summary>
        Task<MsalRefreshTokenCacheItem> FindRefreshTokenAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            string familyId = null);

        void SetIosKeychainSecurityGroup(string securityGroup);


        void RemoveMsalAccountWithNoLocks(IAccount account, RequestContext requestContext);

        Task<IEnumerable<MsalAccessTokenCacheItem>> GetAllAccessTokensAsync(bool filterByClientId);
        Task<IEnumerable<MsalRefreshTokenCacheItem>> GetAllRefreshTokensAsync(bool filterByClientId);
        Task<IEnumerable<MsalIdTokenCacheItem>> GetAllIdTokensAsync(bool filterByClientId);
        Task<IEnumerable<MsalAccountCacheItem>> GetAllAccountsAsync();

        /// <summary>
        /// FOCI - check in the app metadata to see if the app is part of the family
        /// </summary>
        /// <returns>null if unkown, true or false if app metadata has details</returns>
        Task<bool?> IsFociMemberAsync(AuthenticationRequestParameters authenticationRequestParameters, string familyId);

        void ClearAdalCache();
        void ClearMsalCache();
        Task ClearAsync();

        #endregion

        #region Cache notifications
        Task OnAfterAccessAsync(TokenCacheNotificationArgs args);
        Task OnBeforeAccessAsync(TokenCacheNotificationArgs args);
        Task OnBeforeWriteAsync(TokenCacheNotificationArgs args);

        #endregion
    }
}
