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

namespace Test.MSAL.NET.Unit.Mocks
{
    internal class TokenCacheHelper
    {
        public static long ValidExpiresIn = 28800;
        
        public static TokenCache CreateCacheWithItems()
        {
            TokenCache cache = new TokenCache();
            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultScope, TestConstants.DefaultClientId,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultHomeObjectId,
                TestConstants.DefaultPolicy);
            AuthenticationResultEx ex = new AuthenticationResultEx();
            ex.Result = new AuthenticationResult("Bearer", key.ToString(),
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)));
            ex.Result.User = new User
            {
                DisplayableId = TestConstants.DefaultDisplayableId,
                UniqueId = TestConstants.DefaultUniqueId,
                HomeObjectId = TestConstants.DefaultHomeObjectId
            };
            ex.Result.ScopeSet = TestConstants.DefaultScope;

            ex.Result.FamilyId = "1";
            ex.RefreshToken = "someRT";
            cache.tokenCacheDictionary[key] = ex;

            key = new TokenCacheKey(TestConstants.DefaultAuthorityGuestTenant,
                TestConstants.ScopeForAnotherResource, TestConstants.DefaultClientId,
                TestConstants.DefaultUniqueId + "more", TestConstants.DefaultDisplayableId, TestConstants.DefaultHomeObjectId,
                TestConstants.DefaultPolicy);
            ex = new AuthenticationResultEx();
            ex.Result = new AuthenticationResult("Bearer", key.ToString(),
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)));
            ex.Result.User = new User
            {
                DisplayableId = TestConstants.DefaultDisplayableId,
                UniqueId = TestConstants.DefaultUniqueId + "more",
                HomeObjectId = TestConstants.DefaultHomeObjectId
            };
            ex.Result.ScopeSet = TestConstants.ScopeForAnotherResource;
            ex.RefreshToken = "someRT";
            cache.tokenCacheDictionary[key] = ex;

            return cache;
        }

        public static void ExpireCacheItems(TokenCache cache)
        {
            foreach (var value in cache.tokenCacheDictionary.Values)
            {
                value.Result.ExpiresOn = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
            }
        }
    }
}
