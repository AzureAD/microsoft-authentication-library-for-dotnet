// ------------------------------------------------------------------------------
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
// ------------------------------------------------------------------------------

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
        IEnumerable<IAccount> GetAccounts(string authority);

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
