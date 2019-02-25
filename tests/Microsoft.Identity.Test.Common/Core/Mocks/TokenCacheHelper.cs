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
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Unit;

namespace Microsoft.Identity.Test.Common.Core.Mocks
{
    internal class TokenCacheHelper
    {
        public static long ValidExpiresIn = 28800;
        public static long ValidExtendedExpiresIn = 57600;

        internal void PopulateCacheForClientCredential(ITokenCacheAccessor accessor)
        {
            MsalAccessTokenCacheItem atItem = new MsalAccessTokenCacheItem(
               MsalTestConstants.ProductionPrefCacheEnvironment,
               MsalTestConstants.ClientId,
               MsalTestConstants.Scope.AsSingleString(),
               MsalTestConstants.Utid,
               "",
               new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)),
               new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExtendedExpiresIn)),
               MockHelpers.CreateClientInfo());

            accessor.SaveAccessToken(atItem);
        }

        internal void PopulateCache(
            ITokenCacheAccessor accessor, 
            string uid = MsalTestConstants.Uid,
            string utid = MsalTestConstants.Utid)
        {
            MsalAccessTokenCacheItem atItem = new MsalAccessTokenCacheItem(
                MsalTestConstants.ProductionPrefCacheEnvironment,
                MsalTestConstants.ClientId,
                MsalTestConstants.Scope.AsSingleString(),
                uid,
                "",
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)),
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExtendedExpiresIn)),
                MockHelpers.CreateClientInfo(uid, utid));

            // add access token
            accessor.SaveAccessToken(atItem);

            var idTokenCacheItem = new MsalIdTokenCacheItem(
                MsalTestConstants.ProductionPrefCacheEnvironment, 
                MsalTestConstants.ClientId, 
                MockHelpers.CreateIdToken(MsalTestConstants.UniqueId + "more", MsalTestConstants.DisplayableId),
                MockHelpers.CreateClientInfo(uid, utid), 
                utid);

            accessor.SaveIdToken(idTokenCacheItem);

            var accountCacheItem = new MsalAccountCacheItem(
                MsalTestConstants.ProductionPrefNetworkEnvironment, 
                null, 
                MockHelpers.CreateClientInfo(uid, utid), 
                null, 
                MsalTestConstants.DisplayableId, 
                utid,
                null,
                null);

            accessor.SaveAccount(accountCacheItem);

            atItem = new MsalAccessTokenCacheItem(
                MsalTestConstants.ProductionPrefCacheEnvironment,
                MsalTestConstants.ClientId,
                MsalTestConstants.ScopeForAnotherResource.AsSingleString(),
                utid,
                "",
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)),
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExtendedExpiresIn)),
                MockHelpers.CreateClientInfo(uid, utid));

            // add another access token
            accessor.SaveAccessToken(atItem);

            AddRefreshTokenToCache(accessor,uid, utid, MsalTestConstants.Name);
        }

        internal void PopulateCacheWithOneAccessToken(ITokenCacheAccessor accessor)
        {
            MsalAccessTokenCacheItem atItem  = new MsalAccessTokenCacheItem(
               MsalTestConstants.ProductionPrefCacheEnvironment,
               MsalTestConstants.ClientId,
               MsalTestConstants.Scope.AsSingleString(),
               MsalTestConstants.Utid,
               "",
               new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)),
               new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExtendedExpiresIn)),
               MockHelpers.CreateClientInfo());

            // add access token
            accessor.SaveAccessToken(atItem);

            MsalIdTokenCacheItem idTokenCacheItem = new MsalIdTokenCacheItem(
                MsalTestConstants.ProductionPrefCacheEnvironment, MsalTestConstants.ClientId,
                MockHelpers.CreateIdToken(MsalTestConstants.UniqueId + "more", MsalTestConstants.DisplayableId),
                MockHelpers.CreateClientInfo(), MsalTestConstants.Utid);

            accessor.SaveIdToken(idTokenCacheItem);

            MsalAccountCacheItem accountCacheItem = new MsalAccountCacheItem
                (MsalTestConstants.ProductionPrefNetworkEnvironment, null, MockHelpers.CreateClientInfo(), null, null, MsalTestConstants.Utid,
                null, null);

            accessor.SaveAccount(accountCacheItem);

            atItem = new MsalAccessTokenCacheItem(
                MsalTestConstants.ProductionPrefCacheEnvironment,
                MsalTestConstants.ClientId,
                MsalTestConstants.ScopeForAnotherResource.AsSingleString(),
                MsalTestConstants.Utid,
                "",
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)),
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExtendedExpiresIn)),
                MockHelpers.CreateClientInfo());

            AddRefreshTokenToCache(accessor, MsalTestConstants.Uid, MsalTestConstants.Utid, MsalTestConstants.Name);
        }

        public static void AddRefreshTokenToCache(ITokenCacheAccessor accessor, string uid, string utid, string name)
        {
            var rtItem = new MsalRefreshTokenCacheItem
                (MsalTestConstants.ProductionPrefCacheEnvironment, MsalTestConstants.ClientId, "someRT", MockHelpers.CreateClientInfo(uid, utid));

            accessor.SaveRefreshToken(rtItem);
        }

        public static void AddIdTokenToCache(ITokenCacheAccessor accessor, string uid, string utid)
        {
            MsalIdTokenCacheItem idTokenCacheItem = new MsalIdTokenCacheItem(
                MsalTestConstants.ProductionPrefCacheEnvironment,
                MsalTestConstants.ClientId,
                MockHelpers.CreateIdToken(MsalTestConstants.UniqueId, MsalTestConstants.DisplayableId),
                MockHelpers.CreateClientInfo(),
                MsalTestConstants.Utid);

            accessor.SaveIdToken(idTokenCacheItem);
        }

        public static void AddAccountToCache(ITokenCacheAccessor accessor, string uid, string utid)
        {
            MsalAccountCacheItem accountCacheItem = new MsalAccountCacheItem
                (MsalTestConstants.ProductionPrefCacheEnvironment, null, MockHelpers.CreateClientInfo(uid, utid), null, null, utid, null, null);

            accessor.SaveAccount(accountCacheItem);
        }
    }
}