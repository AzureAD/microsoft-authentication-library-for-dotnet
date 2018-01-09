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

using System.IO;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.ADAL.Common.Unit;
using Test.ADAL.NET.Common;
using Test.ADAL.NET.Common.Mocks;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Http;

namespace Test.ADAL.NET.Unit
{
    [TestClass]
    [DeploymentItem("oldcache.serialized")]
    public class TokenCacheUnitTests
    {
        [TestInitialize]
        public void Initialize()
        {
            HttpMessageHandlerFactory.InitializeMockProvider();
            InstanceDiscovery.InstanceCache.Clear();
            HttpMessageHandlerFactory.AddMockHandler(MockHelpers.CreateInstanceDiscoveryMockHandler(TestConstants.GetDiscoveryEndpoint(TestConstants.DefaultAuthorityCommonTenant)));
        }

        [TestMethod]
        [Description("Test to store in default token cache")]
        [TestCategory("AdalDotNetUnit")]
        public void DefaultTokenCacheTest()
        {
            TokenCacheTests.DefaultTokenCacheTest();
        }

        [TestMethod]
        [TestCategory("AdalDotNetUnit")]
        public async Task TestUniqueIdDisplayableIdLookup()
        {
            await TokenCacheTests.TestUniqueIdDisplayableIdLookup();
        }

        [TestMethod]
        [Description("Test for TokenCache")]
        [TestCategory("AdalDotNetUnit")]
        public async Task TokenCacheKeyTestAsync()
        {
            await TokenCacheTests.TokenCacheKeyTestAsync(new PlatformParameters(PromptBehavior.Auto, null));
        }

        [TestMethod]
        [Description("Test for Token Cache Operations")]
        [TestCategory("AdalDotNetUnit")]
        public async Task TokenCacheOperationsTest()
        {
            await TokenCacheTests.TokenCacheOperationsTest().ConfigureAwait(false);
        }

        [TestMethod]
        [Description("Test for Token Cache Cross-Tenant operations")]
        [TestCategory("AdalDotNetUnit")]
        public void TokenCacheCrossTenantOperationsTest()
        {
            TokenCacheTests.TokenCacheCrossTenantOperationsTest();
        }

        [TestMethod]
        [Description("Test for Token Cache Capacity")]
        [TestCategory("AdalDotNetUnit")]
        public void TokenCacheCapacityTest()
        {
            TokenCacheTests.TokenCacheCapacityTest();
        }

        [TestMethod]
        [Description("Test for Multiple User tokens found, hash fallback test")]
        [TestCategory("AdalDotNetUnit")]
        public async Task MultipleUserAssertionHashTest()
        {
            await TokenCacheTests.MultipleUserAssertionHashTest().ConfigureAwait(false);
        }

        [TestMethod]
        [Description("Test for Token Cache Serialization")]
        [TestCategory("AdalDotNetUnit")]
        public void TokenCacheSerializationTest()
        {
            TokenCacheTests.TokenCacheSerializationTest();
        }

        [TestMethod]
        [Description("Test for Token Cache backwasrd compatiblity where new attribute is added in AdalResultWrapper")]
        [TestCategory("AdalDotNetUnit")]
        public void TokenCacheBackCompatTest()
        {
            TokenCacheTests.TokenCacheBackCompatTest(File.ReadAllBytes("oldcache.serialized"));
        }

        [TestMethod]
        [Description("Positive Test for Parallel stores on cache")]
        [TestCategory("AdalDotNet.Unit")]
        public void ParallelStoreTest()
        {
            TokenCacheTests.ParallelStorePositiveTest(File.ReadAllBytes("oldcache.serialized"));
        }
    }
}
