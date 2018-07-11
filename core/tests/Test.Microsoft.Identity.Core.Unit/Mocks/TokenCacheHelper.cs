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
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Cache;
using Microsoft.Identity.Core.Helpers;
using Microsoft.Identity.Core.Instance;

namespace Test.Microsoft.Identity.Core.Unit.Mocks
{
    internal class TokenCacheHelper
    {
        public static long ValidExpiresIn = 28800;

        internal static void PopulateCacheForClientCredential(TokenCacheAccessor accessor)
        {
            MsalAccessTokenCacheItem atItem = new MsalAccessTokenCacheItem(
                TestConstants.ProductionEnvironment,
                TestConstants.ClientId,
                "Bearer",
                TestConstants.Scope.AsSingleString(),
                TestConstants.Utid,
                "",
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)),
                MockHelpers.CreateClientInfo());

            accessor.AccessTokenCacheDictionary[atItem.GetKey().ToString()] = JsonHelper.SerializeToJson(atItem);
        }

        internal static void PopulateCache(TokenCacheAccessor accessor)
        {
            MsalAccessTokenCacheItem atItem = new MsalAccessTokenCacheItem(
                TestConstants.ProductionEnvironment,
                TestConstants.ClientId,
                "Bearer",
                TestConstants.Scope.AsSingleString(),
                TestConstants.Utid,
                "",
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)),
                MockHelpers.CreateClientInfo());

            //add access token
            accessor.AccessTokenCacheDictionary[atItem.GetKey().ToString()] = JsonHelper.SerializeToJson(atItem);

            MsalIdTokenCacheItem idTokenCacheItem = new MsalIdTokenCacheItem(
                Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false), TestConstants.ClientId, 
                MockHelpers.CreateIdToken(TestConstants.UniqueId + "more", TestConstants.DisplayableId),
                MockHelpers.CreateClientInfo(), TestConstants.Utid);

            accessor.IdTokenCacheDictionary[idTokenCacheItem.GetKey().ToString()] = JsonHelper.SerializeToJson(idTokenCacheItem);

            MsalAccountCacheItem accountCacheItem = new MsalAccountCacheItem
                (TestConstants.ProductionEnvironment, null, MockHelpers.CreateClientInfo(), null, null, TestConstants.Utid);

            accessor.AccountCacheDictionary[accountCacheItem.GetKey().ToString()] = JsonHelper.SerializeToJson(accountCacheItem);

            atItem = new MsalAccessTokenCacheItem(
                TestConstants.ProductionEnvironment,
                TestConstants.ClientId,
                "Bearer",
                TestConstants.ScopeForAnotherResource.AsSingleString(),
                TestConstants.Utid,
                "",
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)),
                MockHelpers.CreateClientInfo());

            //add another access token
            accessor.AccessTokenCacheDictionary[atItem.GetKey().ToString()] = JsonHelper.SerializeToJson(atItem);

            AddRefreshTokenToCache(accessor, TestConstants.Uid, TestConstants.Utid, TestConstants.Name);
        }

        public static void AddRefreshTokenToCache(TokenCacheAccessor accessor, string uid, string utid, string name)
        {
            var rtItem = new MsalRefreshTokenCacheItem
                (TestConstants.ProductionEnvironment, TestConstants.ClientId, "someRT", MockHelpers.CreateClientInfo(uid, utid));

            accessor.RefreshTokenCacheDictionary[rtItem.GetKey().ToString()] =
                JsonHelper.SerializeToJson(rtItem);
        }

        public static void AddIdTokenToCache(TokenCacheAccessor accessor, string uid, string utid)
        {
            MsalIdTokenCacheItem idTokenCacheItem = new MsalIdTokenCacheItem(
                Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false),
                TestConstants.ClientId,
                MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId),
                MockHelpers.CreateClientInfo(), 
                TestConstants.Utid);

            accessor.IdTokenCacheDictionary[idTokenCacheItem.GetKey().ToString()] = JsonHelper.SerializeToJson(idTokenCacheItem);
        }

        public static void AddAccountToCache(TokenCacheAccessor accessor, string uid, string utid)
        {
            MsalAccountCacheItem accountCacheItem = new MsalAccountCacheItem
                (TestConstants.ProductionEnvironment, null, MockHelpers.CreateClientInfo(uid, utid), null, null, utid);

            accessor.AccountCacheDictionary[accountCacheItem.GetKey().ToString()] = JsonHelper.SerializeToJson(accountCacheItem);
        }
    }
}