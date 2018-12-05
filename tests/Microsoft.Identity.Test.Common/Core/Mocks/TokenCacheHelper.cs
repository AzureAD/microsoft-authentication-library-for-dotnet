//----------------------------------------------------------------------
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
//------------------------------------------------------------------------------

using System;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;

namespace Microsoft.Identity.Test.Common.Core.Mocks
{
    internal class TokenCacheHelper
    {
        public static long ValidExpiresIn = 28800;
        public static long ValidExtendedExpiresIn = 57600;
        private MsalAccessTokenCacheItem _atItem;

        internal void PopulateCacheForClientCredential(ITokenCacheAccessor accessor)
        {
             _atItem = new MsalAccessTokenCacheItem(
                CoreTestConstants.ProductionPrefCacheEnvironment,
                CoreTestConstants.ClientId,
                "Bearer",
                CoreTestConstants.Scope.AsSingleString(),
                CoreTestConstants.Utid,
                "",
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)),
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExtendedExpiresIn)),
                MockHelpers.CreateClientInfo());

            accessor.SaveAccessToken(_atItem);
        }

        internal void PopulateCache(ITokenCacheAccessor accessor)
        {
            PopulateCacheWithOneAccessToken(accessor);

            // add another access token
            accessor.SaveAccessToken(_atItem);

            AddRefreshTokenToCache(accessor, CoreTestConstants.Uid, CoreTestConstants.Utid, CoreTestConstants.Name);
        }
        
        internal void PopulateCacheWithOneAccessToken(ITokenCacheAccessor accessor)
        {
             _atItem = new MsalAccessTokenCacheItem(
                CoreTestConstants.ProductionPrefCacheEnvironment,
                CoreTestConstants.ClientId,
                "Bearer",
                CoreTestConstants.Scope.AsSingleString(),
                CoreTestConstants.Utid,
                "",
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)),
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExtendedExpiresIn)),
                MockHelpers.CreateClientInfo());

            // add access token
            accessor.SaveAccessToken(_atItem);

            MsalIdTokenCacheItem idTokenCacheItem = new MsalIdTokenCacheItem(
                CoreTestConstants.ProductionPrefCacheEnvironment, CoreTestConstants.ClientId, 
                MockHelpers.CreateIdToken(CoreTestConstants.UniqueId + "more", CoreTestConstants.DisplayableId),
                MockHelpers.CreateClientInfo(), CoreTestConstants.Utid);

            accessor.SaveIdToken(idTokenCacheItem);

            MsalAccountCacheItem accountCacheItem = new MsalAccountCacheItem
                (CoreTestConstants.ProductionPrefNetworkEnvironment, null, MockHelpers.CreateClientInfo(), null, null, CoreTestConstants.Utid,
                null, null);

            accessor.SaveAccount(accountCacheItem);

            _atItem = new MsalAccessTokenCacheItem(
                CoreTestConstants.ProductionPrefCacheEnvironment,
                CoreTestConstants.ClientId,
                "Bearer",
                CoreTestConstants.ScopeForAnotherResource.AsSingleString(),
                CoreTestConstants.Utid,
                "",
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)),
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExtendedExpiresIn)),
                MockHelpers.CreateClientInfo());

            AddRefreshTokenToCache(accessor, CoreTestConstants.Uid, CoreTestConstants.Utid, CoreTestConstants.Name);
        }

        public static void AddRefreshTokenToCache(ITokenCacheAccessor accessor, string uid, string utid, string name)
        {
            var rtItem = new MsalRefreshTokenCacheItem
                (CoreTestConstants.ProductionPrefCacheEnvironment, CoreTestConstants.ClientId, "someRT", MockHelpers.CreateClientInfo(uid, utid));

            accessor.SaveRefreshToken(rtItem);
        }

        public static void AddIdTokenToCache(ITokenCacheAccessor accessor, string uid, string utid)
        {
            MsalIdTokenCacheItem idTokenCacheItem = new MsalIdTokenCacheItem(
                CoreTestConstants.ProductionPrefCacheEnvironment,
                CoreTestConstants.ClientId,
                MockHelpers.CreateIdToken(CoreTestConstants.UniqueId, CoreTestConstants.DisplayableId),
                MockHelpers.CreateClientInfo(),
                CoreTestConstants.Utid);

            accessor.SaveIdToken(idTokenCacheItem);
        }

        public static void AddAccountToCache(ITokenCacheAccessor accessor, string uid, string utid)
        {
            MsalAccountCacheItem accountCacheItem = new MsalAccountCacheItem
                (CoreTestConstants.ProductionPrefCacheEnvironment, null, MockHelpers.CreateClientInfo(uid, utid), null, null, utid, null, null);

            accessor.SaveAccount(accountCacheItem);
        }
    }
}