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
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Cache;

namespace Test.MSAL.NET.Unit.Mocks
{
    internal class TokenCacheHelper
    {
        public static long ValidExpiresIn = 28800;

        public static void PopulateCache(TokenCacheAccessor accessor)
        {
            AccessTokenCacheItem item = new AccessTokenCacheItem()
            {
                Authority = TestConstants.AuthorityHomeTenant,
                ClientId = TestConstants.ClientId,
                TokenType = "Bearer",
                ExpiresOnUnixTimestamp = MsalHelpers.DateTimeToUnixTimestamp(new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn))),
                RawIdToken = MockHelpers.DefaultIdToken,
                RawClientInfo = MockHelpers.CreateClientInfo(),
                Scope = TestConstants.Scope.AsSingleString(),
                ScopeSet = TestConstants.Scope
            };
            item.IdToken = IdToken.Parse(item.RawIdToken);
            item.ClientInfo = ClientInfo.Parse(item.RawClientInfo);
            item.AccessToken = item.GetAccessTokenItemKey().ToString();
            //add access token
            accessor.AccessTokenCacheDictionary[item.GetAccessTokenItemKey().ToString()] = JsonHelper.SerializeToJson(item);

            item = new AccessTokenCacheItem()
            {
                Authority = TestConstants.AuthorityGuestTenant,
                ClientId = TestConstants.ClientId,
                TokenType = "Bearer",
                ExpiresOnUnixTimestamp = MsalHelpers.DateTimeToUnixTimestamp(new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn))),
                RawIdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId + "more", TestConstants.DisplayableId),
                RawClientInfo = MockHelpers.CreateClientInfo(),
                Scope = TestConstants.ScopeForAnotherResource.AsSingleString(),
                ScopeSet = TestConstants.ScopeForAnotherResource
            };
            item.IdToken = IdToken.Parse(item.RawIdToken);
            item.ClientInfo = ClientInfo.Parse(item.RawClientInfo);
            item.AccessToken = item.GetAccessTokenItemKey().ToString();
            //add another access token
            accessor.AccessTokenCacheDictionary[item.GetAccessTokenItemKey().ToString()] = JsonHelper.SerializeToJson(item);

            RefreshTokenCacheItem rtItem = new RefreshTokenCacheItem()
            {
                Environment= TestConstants.ProductionEnvironment,
                ClientId = TestConstants.ClientId,
                RefreshToken = "someRT",
                RawClientInfo = MockHelpers.CreateClientInfo(),
                DisplayableId = TestConstants.DisplayableId,
                IdentityProvider = TestConstants.IdentityProvider,
                Name = TestConstants.Name
            };
            rtItem.ClientInfo = ClientInfo.Parse(rtItem.RawClientInfo);
            accessor.RefreshTokenCacheDictionary[rtItem.GetRefreshTokenItemKey().ToString()] = JsonHelper.SerializeToJson(rtItem);
        }
    }
}