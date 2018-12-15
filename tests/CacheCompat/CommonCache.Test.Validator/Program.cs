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
using System.Threading;
using System.Threading.Tasks;

namespace CommonCache.Test.Validator
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Running V3->V4 cache compat");
            var cacheTestExecutors = new List<CacheTestExecutor>
            {
                // Sign in with adal v3, Token written to old Adal V3 cache format (expected size 5014 bytes)
                // Run Common Cache Adal V4.  token is read from the cache via old adal v3 format.
                new CacheTestExecutor(
                    CacheProgramType.AdalV3,
                    CacheProgramType.AdalV4,
                    expectedAdalCacheSizeBytes: 5014,
                    expectSecondTokenFromCache: true),

                // Sign in with adal v3, token written to old adal v3 cache format (expected size 5014 bytes)
                // Run msal V2.  this should FAIL since MSAL does not understand the ADALV3 token cache format and throws exception
                new CacheTestExecutor(
                    CacheProgramType.AdalV3, 
                    CacheProgramType.MsalV2,
                    expectedAdalCacheSizeBytes: 5014,
                    expectSecondTokenException: true),

                // Sign in via adal v4, token is written in cache with old adal v3 cache format (expected size 5156 bytes)
                // Run adal v3, token comes from cache as adalv3 should understand cache format.
                new CacheTestExecutor(
                    CacheProgramType.AdalV4, 
                    CacheProgramType.AdalV3,
                    expectedAdalCacheSizeBytes: 5156,
                    expectSecondTokenFromCache: true),

                // Sign in via adal v4, token is written in cache with old adal v3 cache format (expected size 5156 bytes)
                // Run msal V2.  this should FAIL since MSAL does not understand the ADALV3 token cache format and throws exception
                new CacheTestExecutor(
                    CacheProgramType.AdalV4, 
                    CacheProgramType.MsalV2,
                    expectedAdalCacheSizeBytes: 5156,
                    expectSecondTokenException: true),

                // Sign in via msal v2, token written to cache with new cache format (expected msal token cache size 6997 bytes)
                // run adal v3.  This fails as adal v3 does not recognize the new cache format.
                new CacheTestExecutor(
                    CacheProgramType.MsalV2, 
                    CacheProgramType.AdalV3,
                    expectedMsalCacheSizeBytes: 6997,
                    expectSecondTokenException: true),

                // Sign in via msal v2, token written to cache with new cache format (expected msal token cache size 6997 bytes)
                // run adal v4.  This fails as adal v4 does not recognize the new cache format.
                new CacheTestExecutor(
                    CacheProgramType.MsalV2, 
                    CacheProgramType.AdalV4,
                    expectedMsalCacheSizeBytes: 6997,
                    expectSecondTokenException: true),
            };

            foreach (var executor in cacheTestExecutors)
            {
                await executor.ExecuteAsync(CancellationToken.None);
            }
        }
    }
}