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

using System.Threading;
using System.Threading.Tasks;
using CommonCache.Test.Common;
using CommonCache.Test.Unit.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommonCache.Test.Unit
{
    [TestClass]
    public class CacheExecutionTests
    { 
        [DataTestMethod]
        [DataRow(CacheProgramType.AdalV3, CacheProgramType.AdalV3, CacheStorageType.Adal, DisplayName = "AdalV3->AdalV3 adal cache only")]
        [DataRow(CacheProgramType.AdalV3, CacheProgramType.AdalV4, CacheStorageType.Adal, DisplayName = "AdalV3->AdalV4 adal cache only")]
        [DataRow(CacheProgramType.AdalV3, CacheProgramType.AdalV5, CacheStorageType.Adal, DisplayName = "AdalV3->AdalV5 adal cache only")]
        [DataRow(CacheProgramType.AdalV3, CacheProgramType.MsalV2, CacheStorageType.Adal, DisplayName = "AdalV3->MsalV2 adal cache only")]
        [DataRow(CacheProgramType.AdalV3, CacheProgramType.MsalV3, CacheStorageType.Adal, DisplayName = "AdalV3->MsalV3 adal cache only")]
        public async Task TestAdalV3CacheCompatibilityAsync(
            CacheProgramType interactiveType,
            CacheProgramType silentType,
            CacheStorageType cacheStorageType)
        {
            var executor = new CacheTestExecutor(
                interactiveType,
                silentType,
                cacheStorageType,
                expectSecondTokenFromCache: true);

            await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
        }


        [DataTestMethod]
        [DataRow(CacheProgramType.AdalV4, CacheProgramType.AdalV3, CacheStorageType.Adal,   DisplayName = "AdalV4->AdalV3 adal cache only")]
        [DataRow(CacheProgramType.AdalV4, CacheProgramType.AdalV4, CacheStorageType.Adal,   DisplayName = "AdalV4->AdalV4 adal cache only")]
        [DataRow(CacheProgramType.AdalV4, CacheProgramType.AdalV5, CacheStorageType.Adal,   DisplayName = "AdalV4->AdalV5 adal cache only")]
        [DataRow(CacheProgramType.AdalV4, CacheProgramType.AdalV4, CacheStorageType.MsalV2, DisplayName = "AdalV4->AdalV4 msal v2 cache")]
        [DataRow(CacheProgramType.AdalV4, CacheProgramType.AdalV5, CacheStorageType.MsalV2, DisplayName = "AdalV4->AdalV5 msal v2 cache")]
        [DataRow(CacheProgramType.AdalV4, CacheProgramType.MsalV2, CacheStorageType.Adal,   DisplayName = "AdalV4->MsalV2 no msal cache")]
        [DataRow(CacheProgramType.AdalV4, CacheProgramType.MsalV2, CacheStorageType.MsalV2, DisplayName = "AdalV4->MsalV2 msal v2 cache")]
        [DataRow(CacheProgramType.AdalV4, CacheProgramType.MsalV3, CacheStorageType.Adal,   DisplayName = "AdalV4->MsalV3 no msal cache")]
        [DataRow(CacheProgramType.AdalV4, CacheProgramType.MsalV3, CacheStorageType.MsalV2, DisplayName = "AdalV4->MsalV3 msal v2 cache")]
        public async Task TestAdalV4CacheCompatibilityAsync(
            CacheProgramType interactiveType,
            CacheProgramType silentType,
            CacheStorageType cacheStorageType)
        {
            var executor = new CacheTestExecutor(
                interactiveType,
                silentType,
                cacheStorageType,
                expectSecondTokenFromCache: true);

            await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(CacheProgramType.AdalV5, CacheProgramType.AdalV3, CacheStorageType.Adal,   DisplayName = "AdalV5->AdalV3 adal cache only")]
        [DataRow(CacheProgramType.AdalV5, CacheProgramType.AdalV4, CacheStorageType.Adal,   DisplayName = "AdalV5->AdalV4 adal cache only")]
        [DataRow(CacheProgramType.AdalV5, CacheProgramType.AdalV5, CacheStorageType.Adal,   DisplayName = "AdalV5->AdalV5 adal cache only")]
        [DataRow(CacheProgramType.AdalV5, CacheProgramType.AdalV3, CacheStorageType.MsalV2, DisplayName = "AdalV5->AdalV3 msal v2 cache")]
        [DataRow(CacheProgramType.AdalV5, CacheProgramType.AdalV4, CacheStorageType.MsalV2, DisplayName = "AdalV5->AdalV4 msal v2 cache")]
        [DataRow(CacheProgramType.AdalV5, CacheProgramType.AdalV5, CacheStorageType.MsalV2, DisplayName = "AdalV5->AdalV5 msal v2 cache")]
        [DataRow(CacheProgramType.AdalV5, CacheProgramType.AdalV5, CacheStorageType.MsalV3, DisplayName = "AdalV5->AdalV5 msal v3 cache")]
        [DataRow(CacheProgramType.AdalV5, CacheProgramType.MsalV2, CacheStorageType.Adal,   DisplayName = "AdalV5->MsalV2 no msal cache")]
        [DataRow(CacheProgramType.AdalV5, CacheProgramType.MsalV2, CacheStorageType.MsalV2, DisplayName = "AdalV5->MsalV2 msal v2 cache")]
        [DataRow(CacheProgramType.AdalV5, CacheProgramType.MsalV3, CacheStorageType.Adal,   DisplayName = "AdalV5->MsalV3 no msal cache")]
        [DataRow(CacheProgramType.AdalV5, CacheProgramType.MsalV3, CacheStorageType.MsalV2, DisplayName = "AdalV5->MsalV3 msal v2 cache")]
        [DataRow(CacheProgramType.AdalV5, CacheProgramType.MsalV3, CacheStorageType.MsalV3, DisplayName = "AdalV5->MsalV3 msal v3 cache")]
        public async Task TestAdalV5CacheCompatibilityAsync(
            CacheProgramType interactiveType,
            CacheProgramType silentType,
            CacheStorageType cacheStorageType)
        {
            var executor = new CacheTestExecutor(
                interactiveType,
                silentType,
                cacheStorageType,
                expectSecondTokenFromCache: true);

            await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(CacheProgramType.MsalV2, CacheProgramType.AdalV3, CacheStorageType.Adal,   DisplayName = "MsalV2->AdalV3 no msal cache")]
        [DataRow(CacheProgramType.MsalV2, CacheProgramType.AdalV4, CacheStorageType.Adal,   DisplayName = "MsalV2->AdalV4 no msal cache")]
        [DataRow(CacheProgramType.MsalV2, CacheProgramType.AdalV4, CacheStorageType.MsalV2, DisplayName = "MsalV2->AdalV4 msal v2 cache")]
        [DataRow(CacheProgramType.MsalV2, CacheProgramType.AdalV5, CacheStorageType.Adal,   DisplayName = "MsalV2->AdalV5 no msal cache")]
        [DataRow(CacheProgramType.MsalV2, CacheProgramType.AdalV5, CacheStorageType.MsalV2, DisplayName = "MsalV2->AdalV5 msal v2 cache")]
        [DataRow(CacheProgramType.MsalV2, CacheProgramType.MsalV2, CacheStorageType.Adal,   DisplayName = "MsalV2->MsalV2 adal cache only")]
        [DataRow(CacheProgramType.MsalV2, CacheProgramType.MsalV2, CacheStorageType.MsalV2, DisplayName = "MsalV2->MsalV2 msal v2 cache")]
        [DataRow(CacheProgramType.MsalV2, CacheProgramType.MsalV3, CacheStorageType.Adal,   DisplayName = "MsalV2->AdalV3 adal cache only")]
        [DataRow(CacheProgramType.MsalV2, CacheProgramType.MsalV3, CacheStorageType.MsalV2, DisplayName = "MsalV2->AdalV3 msal v2 cache")]
        [DataRow(CacheProgramType.MsalV2, CacheProgramType.MsalV3, CacheStorageType.MsalV3, DisplayName = "MsalV2->AdalV3 msal v3 cache")]
        public async Task TestMsalV2CacheCompatibilityAsync(
            CacheProgramType interactiveType,
            CacheProgramType silentType,
            CacheStorageType cacheStorageType)
        {
            var executor = new CacheTestExecutor(
                interactiveType,
                silentType,
                cacheStorageType,
                expectSecondTokenFromCache: true);

            await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(CacheProgramType.MsalV3, CacheProgramType.AdalV3, CacheStorageType.Adal,   DisplayName = "MsalV3->AdalV3 no msal cache")]
        [DataRow(CacheProgramType.MsalV3, CacheProgramType.AdalV4, CacheStorageType.Adal,   DisplayName = "MsalV3->AdalV4 no msal cache")]
        [DataRow(CacheProgramType.MsalV3, CacheProgramType.AdalV4, CacheStorageType.MsalV2, DisplayName = "MsalV3->AdalV4 msal v2 cache")]
        [DataRow(CacheProgramType.MsalV3, CacheProgramType.AdalV5, CacheStorageType.Adal,   DisplayName = "MsalV3->AdalV5 no msal cache")]
        [DataRow(CacheProgramType.MsalV3, CacheProgramType.AdalV5, CacheStorageType.MsalV2, DisplayName = "MsalV3->AdalV5 msal v2 cache")]
        [DataRow(CacheProgramType.MsalV3, CacheProgramType.AdalV5, CacheStorageType.MsalV3, DisplayName = "MsalV3->AdalV5 msal v3 cache")]
        [DataRow(CacheProgramType.MsalV3, CacheProgramType.MsalV3, CacheStorageType.Adal,   DisplayName = "MsalV3->MsalV3 adal cache only")]
        [DataRow(CacheProgramType.MsalV3, CacheProgramType.MsalV3, CacheStorageType.MsalV2, DisplayName = "MsalV3->MsalV3 msal v2 cache")]
        [DataRow(CacheProgramType.MsalV3, CacheProgramType.MsalV3, CacheStorageType.MsalV3, DisplayName = "MsalV3->MsalV3 msal v3 cache")]
        public async Task TestMsalV3CacheCompatibilityAsync(
            CacheProgramType interactiveType,
            CacheProgramType silentType,
            CacheStorageType cacheStorageType)
        {
            var executor = new CacheTestExecutor(
                interactiveType,
                silentType,
                cacheStorageType,
                expectSecondTokenFromCache: true);

            await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
        }


        //#region Start With Adal V3, acquire token silent with others
        //[TestMethod]
        //public async Task AdalV3ToV4Async()
        //{
        //    // Sign in with adal v3, Token written to old Adal V3 cache format.
        //    // Run Common Cache Adal V4.  token is read from the cache via old adal v3 format.
        //    var executor = new CacheTestExecutor(
        //        CacheProgramType.AdalV3,
        //        CacheProgramType.AdalV4,
        //        4434,
        //        expectSecondTokenFromCache: true);

        //    await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
        //}

        //[TestMethod]
        //public async Task AdalV3ToV5Async()
        //{
        //    // Sign in with adal v3, Token written to old Adal V3 cache format.
        //    // Run Common Cache Adal V5.  token is read from the cache via old adal v3 format.
        //    var executor = new CacheTestExecutor(
        //        CacheProgramType.AdalV3,
        //        CacheProgramType.AdalV5,
        //        4434,
        //        expectSecondTokenFromCache: true);

        //    await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
        //}

        //[TestMethod]
        //public async Task AdalV3ToMsalV2Async()
        //{
        //    // Sign in with adal v3, token written to old adal v3 cache format.
        //    // Run msal V2. 
        //    var executor = new CacheTestExecutor(
        //        CacheProgramType.AdalV3,
        //        CacheProgramType.MsalV2,
        //        6250,
        //        expectSecondTokenFromCache: true);

        //    await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
        //}

        //        [TestMethod]
        //public async Task AdalV3ToMsalV3Async()
        //{
        //    // Sign in with adal v3, token written to old adal v3 cache format.
        //    // Run msal V3. 
        //    var executor = new CacheTestExecutor(
        //        CacheProgramType.AdalV3,
        //        CacheProgramType.MsalV3,
        //        6250,
        //        expectSecondTokenFromCache: true);

        //    await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
        //}
        //#endregion // Start With Adal V3, acquire token silent with others

        //[TestMethod]
        //public async Task AdalV4ToAdalV3Async()
        //{
        //    // Sign in via adal v4, token is written in cache with old adal v3 cache format
        //    // Run adal v3, token comes from cache as adalv3 should understand cache format.
        //    var executor = new CacheTestExecutor(
        //        CacheProgramType.AdalV4,
        //        CacheProgramType.AdalV3,
        //        5156,
        //        expectSecondTokenFromCache: true);

        //    await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
        //}

        //[TestMethod]
        //public async Task AdalV4ToMsalV2Async()
        //{
        //    // Sign in via adal v4, token is written in cache with old adal v3 cache format
        //    // Run msal V2.  this should FAIL since MSAL does not understand the ADALV3 token cache format and throws exception
        //    var executor = new CacheTestExecutor(
        //        CacheProgramType.AdalV4,
        //        CacheProgramType.MsalV2,
        //        5156,
        //        expectSecondTokenFromCache: true);

        //    await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
        //}

        //[TestMethod]
        //public async Task MsalV2ToAdalV3Async()
        //{
        //    // Sign in via msal v2, token written to cache with new cache format
        //    // run adal v3.  This fails as adal v3 does not recognize the new cache format.
        //    var executor = new CacheTestExecutor(
        //        CacheProgramType.MsalV2,
        //        CacheProgramType.AdalV3,
        //        expectedMsalCacheSizeBytes: 6997,
        //        expectSecondTokenFromCache: true);

        //    await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
        //}

        //[TestMethod]
        //public async Task MsalV2ToAdalV4Async()
        //{
        //    // Sign in via msal v2, token written to cache with new cache format
        //    // run adal v4.  This fails as adal v4 does not recognize the new cache format.
        //    var executor = new CacheTestExecutor(
        //        CacheProgramType.MsalV2,
        //        CacheProgramType.AdalV4,
        //        expectedMsalCacheSizeBytes: 6997,
        //        expectSecondTokenFromCache: true);

        //    await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
        //}
    }
}
