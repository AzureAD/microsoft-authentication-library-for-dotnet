// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Cache.Keys;
using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client.CacheV2
{
    /// <summary>
    /// Given that we want to be able to migrate to CacheV2 with confidence, we want the existing cache
    /// infrastructure to work and be able to test the new cache.  This interface acts as the adapter
    /// between the product and the particular cache version being used.
    /// Once we've fully moved to the V2 cache, the goal is that this adapter infra will be removed
    /// and the implementation within this class for the V2 cache will move into the product code directly.
    /// </summary>
    internal interface ITokenCacheAdapter
    {
        ITokenCache TokenCache { get; set; }
        IEnumerable<IAccount> GetAccounts(string authority, bool validateAuthority, RequestContext requestContext);
        void RemoveAccount(IAccount account, RequestContext requestContext);

        bool TryReadCache(
            AuthenticationRequestParameters authenticationRequestParameters,
            out MsalTokenResponse msalTokenResponse,
            out IAccount account);

        AuthenticationResult SaveAccessAndRefreshToken(
            AuthenticationRequestParameters authenticationRequestParameters,
            MsalTokenResponse msalTokenResponse);

        Task<MsalAccessTokenCacheItem> FindAccessTokenAsync(AuthenticationRequestParameters authenticationRequestParameters);
        MsalIdTokenCacheItem GetIdTokenCacheItem(MsalIdTokenCacheKey msalIdTokenCacheKey, RequestContext requestContext);
        Task<MsalRefreshTokenCacheItem> FindRefreshTokenAsync(AuthenticationRequestParameters authenticationRequestParameters);

        void SetKeychainSecurityGroup(string keychainSecurityGroup);
        ICollection<string>  GetAllAccessTokenCacheItems(RequestContext requestContext);
        ICollection<MsalAccessTokenCacheItem> GetAllAccessTokensForClient(RequestContext requestContext);
        ICollection<MsalAccountCacheItem> GetAllAccounts(RequestContext requestContext);
        ICollection<string> GetAllAccountCacheItems(RequestContext requestContext);
        ICollection<MsalIdTokenCacheItem> GetAllIdTokensForClient(RequestContext requestContext);
        ICollection<string> GetAllIdTokenCacheItems(RequestContext requestContext);
        ICollection<MsalRefreshTokenCacheItem> GetAllRefreshTokensForClient(RequestContext requestContext);
        ICollection<string> GetAllRefreshTokenCacheItems(RequestContext requestContext);
        void RemoveMsalAccount(IAccount account, RequestContext requestContext);
    }
}
