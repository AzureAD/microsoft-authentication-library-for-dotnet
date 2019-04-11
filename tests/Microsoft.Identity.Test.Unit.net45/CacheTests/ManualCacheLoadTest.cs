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
using System.IO;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CacheTests
{
    [TestClass]
    public class ManualCacheLoadTest
    {
        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
        }

        // This is a manual run test to be able to load a cache file from python manually until we get automated tests across the other languages/platforms.
        [TestMethod]
        [Ignore]
        public async Task TestLoadCacheAsync()
        {
            // string authority = "https://login.microsoftonline.com/72f988bf-86f1-41af-91ab-2d7cd011db47/";
            string authority = "https://login.microsoftonline.com/organizations/";
            string scope = "https://graph.microsoft.com/.default";
            string clientId = "b945c513-3946-4ecd-b179-6499803a2167";
            string accountId = "13dd2c19-84cd-416a-ae7d-49573e425619.26039cce-489d-4002-8293-5b0c5134eacb";

            string filePathCacheBin = @"C:\Users\mark\Downloads\python_msal_cache.bin";

            var pca = PublicClientApplicationBuilder.Create(clientId).WithAuthority(authority).Build();
            pca.UserTokenCache.DeserializeMsalV3(File.ReadAllBytes(filePathCacheBin));

            var account = await pca.GetAccountAsync(accountId).ConfigureAwait(false);
            var result = await pca.AcquireTokenSilent(new List<string> { scope }, account).ExecuteAsync().ConfigureAwait(false);

            Console.WriteLine();
        }
    }
}
