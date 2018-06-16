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
            MsalAccessTokenCacheItem atItem = new MsalAccessTokenCacheItem()
            {
                Authority = TestConstants.AuthorityHomeTenant,
                Environment = TestConstants.ProductionEnvironment,
                TenantId = TestConstants.Utid,
                ClientId = TestConstants.ClientId,
                TokenType = "Bearer",
                ExpiresOnUnixTimestamp = CoreHelpers.DateTimeToUnixTimestamp(new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn))),
                Scopes = TestConstants.Scope.AsSingleString(),
                ScopeSet = TestConstants.Scope
            };
            atItem.CreateDerivedProperties();
            atItem.Secret = atItem.GetAccessTokenItemKey().ToString();

            accessor.AccessTokenCacheDictionary[atItem.GetAccessTokenItemKey()] = JsonHelper.SerializeToJson(atItem);
        }

        internal static void PopulateCache(TokenCacheAccessor accessor)
        {
            MsalAccessTokenCacheItem atItem = new MsalAccessTokenCacheItem()
            {
                Authority = TestConstants.AuthorityHomeTenant,
                Environment = TestConstants.ProductionEnvironment,
                TenantId = TestConstants.Utid,
                ClientId = TestConstants.ClientId,
                TokenType = "Bearer",
                ExpiresOnUnixTimestamp = CoreHelpers.DateTimeToUnixTimestamp(new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn))),
                RawClientInfo = MockHelpers.CreateClientInfo(),
                Scopes = TestConstants.Scope.AsSingleString(),
                ScopeSet = TestConstants.Scope
            };
            atItem.CreateDerivedProperties();
            atItem.Secret = atItem.GetAccessTokenItemKey().ToString();

            //add access token
            accessor.AccessTokenCacheDictionary[atItem.GetAccessTokenItemKey()] = JsonHelper.SerializeToJson(atItem);

            MsalIdTokenCacheItem idTokenCacheItem = new MsalIdTokenCacheItem()
            {
                Authority = TestConstants.AuthorityHomeTenant,
                Environment = TestConstants.ProductionEnvironment,
                TenantId = TestConstants.Utid,
                ClientId = TestConstants.ClientId,
                RawClientInfo = MockHelpers.CreateClientInfo(),
                Secret = MockHelpers.CreateIdToken(TestConstants.UniqueId + "more", TestConstants.DisplayableId)
            };
            idTokenCacheItem.CreateDerivedProperties();
            accessor.IdTokenCacheDictionary[idTokenCacheItem.GetIdTokenItemKey()] = JsonHelper.SerializeToJson(idTokenCacheItem);

            MsalAccountCacheItem accountCacheItem = new MsalAccountCacheItem()
            {
                Environment = TestConstants.ProductionEnvironment,
                TenantId = TestConstants.Utid,
                RawClientInfo = MockHelpers.CreateClientInfo(),
            };
            accountCacheItem.InitRawClientInfoDerivedProperties();
            accessor.AccountCacheDictionary[accountCacheItem.GetAccountItemKey()] = JsonHelper.SerializeToJson(accountCacheItem);

            atItem = new MsalAccessTokenCacheItem()
            {
                Authority = TestConstants.AuthorityGuestTenant,
                Environment = TestConstants.ProductionEnvironment,
                TenantId = TestConstants.Utid,
                ClientId = TestConstants.ClientId,
                TokenType = "Bearer",
                ExpiresOnUnixTimestamp = CoreHelpers.DateTimeToUnixTimestamp(new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn))),
                //RawIdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId + "more", TestConstants.DisplayableId),
                RawClientInfo = MockHelpers.CreateClientInfo(),
                Scopes = TestConstants.ScopeForAnotherResource.AsSingleString(),
                ScopeSet = TestConstants.ScopeForAnotherResource
            };
            //item.IdToken = IdToken.Parse(item.RawIdToken);
            //item.ClientInfo = ClientInfo.CreateFromJson(item.RawClientInfo);
            atItem.CreateDerivedProperties();
            atItem.Secret = atItem.GetAccessTokenItemKey().ToString();

            //add another access token
            accessor.AccessTokenCacheDictionary[atItem.GetAccessTokenItemKey().ToString()] = JsonHelper.SerializeToJson(atItem);

            AddRefreshTokenToCache(accessor, TestConstants.Uid, TestConstants.Utid, TestConstants.Name);
        }

        public static void AddRefreshTokenToCache(TokenCacheAccessor accessor, string uid, string utid, string name)
        {
            var rtItem = new MsalRefreshTokenCacheItem
            {
                Environment = TestConstants.ProductionEnvironment,
                ClientId = TestConstants.ClientId,
                Secret = "someRT",
                RawClientInfo = MockHelpers.CreateClientInfo(uid, utid),
                TenantId = TestConstants.Utid
            };
            rtItem.InitRawClientInfoDerivedProperties();

            accessor.RefreshTokenCacheDictionary[rtItem.GetRefreshTokenItemKey().ToString()] =
                JsonHelper.SerializeToJson(rtItem);
        }

        public static void AddIdTokenToCache(TokenCacheAccessor accessor, string uid, string utid)
        {
            MsalIdTokenCacheItem idTokenCacheItem = new MsalIdTokenCacheItem()
            {
                Authority = TestConstants.AuthorityHomeTenant,
                Environment = TestConstants.ProductionEnvironment,
                TenantId = utid,
                ClientId = TestConstants.ClientId,
                RawClientInfo = MockHelpers.CreateClientInfo(uid, utid),
                Secret = MockHelpers.CreateIdToken(uid, TestConstants.DisplayableId)
            };
            idTokenCacheItem.CreateDerivedProperties();
            accessor.IdTokenCacheDictionary[idTokenCacheItem.GetIdTokenItemKey()] = JsonHelper.SerializeToJson(idTokenCacheItem);
        }

        public static void AddAccountToCache(TokenCacheAccessor accessor, string uid, string utid)
        {
            MsalAccountCacheItem accountCacheItem = new MsalAccountCacheItem()
            {
                Environment = TestConstants.ProductionEnvironment,
                TenantId = utid,
                RawClientInfo = MockHelpers.CreateClientInfo(uid, utid),
            };
            accountCacheItem.InitRawClientInfoDerivedProperties();
            accessor.AccountCacheDictionary[accountCacheItem.GetAccountItemKey()] = JsonHelper.SerializeToJson(accountCacheItem);
        }
    }
}