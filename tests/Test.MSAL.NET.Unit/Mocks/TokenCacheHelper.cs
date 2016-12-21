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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Cache;

namespace Test.MSAL.NET.Unit.Mocks
{
    internal class TokenCacheHelper
    {
        public static long ValidExpiresIn = 28800;
        
        public static void PopulateCache(TokenCachePlugin cachePlugin)
        {
            TokenCacheKey key = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                TestConstants.Scope, TestConstants.ClientId,
                TestConstants.UniqueId, TestConstants.DisplayableId, TestConstants.HomeObjectId,
                TestConstants.Policy);
            TokenCacheItem item = new TokenCacheItem()
            {
                Token = key.ToString(),
                TokenType = "Bearer",
                ExpiresOn = new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)),
                User = new User
                {
                    DisplayableId = TestConstants.DisplayableId,
                    UniqueId = TestConstants.UniqueId,
                    HomeObjectId = TestConstants.HomeObjectId
                },
                Scope = TestConstants.Scope
            };
            cachePlugin.TokenCacheDictionary[key.ToString()] = JsonHelper.SerializeToJson(item);


            key = new TokenCacheKey(TestConstants.AuthorityGuestTenant,
                TestConstants.ScopeForAnotherResource, TestConstants.ClientId,
                TestConstants.UniqueId + "more", TestConstants.DisplayableId, TestConstants.HomeObjectId,
                TestConstants.Policy);

            item = new TokenCacheItem()
            {
                Token = key.ToString(),
                TokenType = "Bearer",
                ExpiresOn = new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)),
                User = new User
                {
                    DisplayableId = TestConstants.DisplayableId,
                    UniqueId = TestConstants.UniqueId+"more",
                    HomeObjectId = TestConstants.HomeObjectId
                },
                Scope = TestConstants.ScopeForAnotherResource
            };
            cachePlugin.TokenCacheDictionary[key.ToString()] = JsonHelper.SerializeToJson(item);

            TokenCacheKey rtKey = new TokenCacheKey(null, null, TestConstants.ClientId,
                TestConstants.UniqueId, TestConstants.DisplayableId, TestConstants.HomeObjectId,
                TestConstants.Policy);
            RefreshTokenCacheItem rtItem = new RefreshTokenCacheItem()
            {
                RefreshToken = "someRT",
                User = new User
                {
                    DisplayableId = TestConstants.DisplayableId,
                    UniqueId = TestConstants.UniqueId,
                    HomeObjectId = TestConstants.HomeObjectId
                }
            };
            cachePlugin.TokenCacheDictionary[rtKey.ToString()] = JsonHelper.SerializeToJson(rtItem);
        }
    }
}
