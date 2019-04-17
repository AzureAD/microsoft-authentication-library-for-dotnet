// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Cache.Keys;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client
{
    internal interface ITokenCacheInternal : ITokenCache
    {
        object LockObject { get; }

        void RemoveAccount(IAccount account, RequestContext requestContext);
        Task<IEnumerable<IAccount>> GetAccountsAsync(string authority, RequestContext requestContext);

        /// <summary>
        /// Persists the AT and RT and updates app metadata (FOCI)
        /// </summary>
        /// <returns></returns>
        Tuple<MsalAccessTokenCacheItem, MsalIdTokenCacheItem> SaveTokenResponse(
            AuthenticationRequestParameters authenticationRequestParameters,
            MsalTokenResponse msalTokenResponse);

        Task<MsalAccessTokenCacheItem> FindAccessTokenAsync(AuthenticationRequestParameters authenticationRequestParameters);
        MsalIdTokenCacheItem GetIdTokenCacheItem(MsalIdTokenCacheKey getIdTokenItemKey, RequestContext requestContext);

        /// <summary>
        /// Returns a RT for the request. If familyId is specified, it tries to return the FRT.
        /// </summary>
        Task<MsalRefreshTokenCacheItem> FindRefreshTokenAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            string familyId = null);

        void SetIosKeychainSecurityGroup(string securityGroup);

        ILegacyCachePersistence LegacyPersistence { get; }
        ITokenCacheAccessor Accessor { get; }

        void RemoveMsalAccount(IAccount account, RequestContext requestContext);

        IEnumerable<MsalAccessTokenCacheItem> GetAllAccessTokens(bool filterByClientId);
        IEnumerable<MsalRefreshTokenCacheItem> GetAllRefreshTokens(bool filterByClientId);
        IEnumerable<MsalIdTokenCacheItem> GetAllIdTokens(bool filterByClientId);
        IEnumerable<MsalAccountCacheItem> GetAllAccounts();

        /// <summary>
        /// FOCI - check in the app metadata to see if the app is part of the family
        /// </summary>
        /// <returns>null if unkown, true or false if app metadata has details</returns>
        Task<bool?> IsFociMemberAsync(AuthenticationRequestParameters authenticationRequestParameters, string familyId);

        void ClearAdalCache();
        void ClearMsalCache();
        void Clear();
    }
}
