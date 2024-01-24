// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client
{
    internal interface ITokenCacheInternal : ITokenCache, ITokenCacheSerializer
    {
        OptionalSemaphoreSlim Semaphore { get; }
        ILegacyCachePersistence LegacyPersistence { get; }
        ITokenCacheAccessor Accessor { get; }

        #region High-Level cache operations
        Task RemoveAccountAsync(IAccount account, AuthenticationRequestParameters requestParameters);
        Task<bool> StopLongRunningOboProcessAsync(string longRunningOboCacheKey, AuthenticationRequestParameters requestParameters);
        Task<IEnumerable<IAccount>> GetAccountsAsync(AuthenticationRequestParameters requestParameters);

        Task<(MsalAccessTokenCacheItem accessCacheItem, MsalIdTokenCacheItem tokenCacheItem, Account account)> SaveTokenResponseAsync(
            AuthenticationRequestParameters requestParams,
            MsalTokenResponse response);

        Task<MsalAccessTokenCacheItem> FindAccessTokenAsync(AuthenticationRequestParameters requestParams);
        MsalIdTokenCacheItem GetIdTokenCacheItem(MsalAccessTokenCacheItem msalAccessTokenCacheItem);

        /// <summary>
        /// Returns a RT for the request. If familyId is specified, it tries to return the FRT.
        /// </summary>
        Task<MsalRefreshTokenCacheItem> FindRefreshTokenAsync(
            AuthenticationRequestParameters requestParams,
            string familyId = null);

        Task<Account> GetAccountAssociatedWithAccessTokenAsync(AuthenticationRequestParameters requestParameters, MsalAccessTokenCacheItem msalAccessTokenCacheItem);

        #endregion

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

        /// <summary>
        /// True when MSAL has been configured to fire the serialization events. This can be done by the app developer or by MSAL itself (on UWP).
        /// </summary>
        bool IsAppSubscribedToSerializationEvents();

        /// <summary>
        /// True when the app developer subscribed to token cache serialization events.
        /// </summary>
        bool IsExternalSerializationConfiguredByUser();

        #endregion
    }
}
