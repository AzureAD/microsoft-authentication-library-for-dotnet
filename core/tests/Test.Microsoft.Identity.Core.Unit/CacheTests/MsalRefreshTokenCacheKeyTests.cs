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

using Microsoft.Identity.Core.Cache;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.Microsoft.Identity.Core.Unit.CacheTests
{
    [TestClass]
    public class MsalRefreshTokenCacheKeyTests
    {
        [TestMethod]
        [TestCategory("RefreshTokenCacheKeyTests")]
        public void ConstructorTest()
        {
            MsalRefreshTokenCacheKey key = new MsalRefreshTokenCacheKey(TestConstants.ProductionEnvironment,
                TestConstants.ClientId, TestConstants.UserIdentifier);

            Assert.IsNotNull(key);
            Assert.AreEqual(TestConstants.ProductionEnvironment, key.Environment);
            Assert.AreEqual(TestConstants.ClientId, key.ClientId);
            Assert.AreEqual(TestConstants.UserIdentifier, key.HomeAccountId);
        }

        [TestMethod]
        [TestCategory("RefreshTokenCacheKeyTests")]
        public void ToStringTest()
        {
            MsalRefreshTokenCacheKey key = new MsalRefreshTokenCacheKey(TestConstants.ProductionEnvironment,
                TestConstants.ClientId, TestConstants.UserIdentifier);

            Assert.IsNotNull(key);
            Assert.AreEqual(TestConstants.ProductionEnvironment, key.Environment);
            Assert.AreEqual(TestConstants.ClientId, key.ClientId);
            Assert.AreEqual(TestConstants.UserIdentifier, key.HomeAccountId);
        }
    }
}
