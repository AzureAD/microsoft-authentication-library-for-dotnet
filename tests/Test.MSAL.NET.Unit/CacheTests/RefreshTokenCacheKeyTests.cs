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

using Microsoft.Identity.Client.Internal.Cache;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.MSAL.NET.Unit.CacheTests
{
    [TestClass]
    public class RefreshTokenCacheKeyTests
    {
        [TestMethod]
        [TestCategory("RefreshTokenCacheKeyTests")]
        public void ConstructorTest()
        {
            RefreshTokenCacheKey key = new RefreshTokenCacheKey(TestConstants.ProductionEnvironment,
                TestConstants.ClientId, TestConstants.UserIdentifier);

            Assert.IsNotNull(key);
            Assert.AreEqual(TestConstants.ProductionEnvironment, key.Environment);
            Assert.AreEqual(TestConstants.ClientId, key.ClientId);
            Assert.AreEqual(TestConstants.UserIdentifier, key.UserIdentifier);
        }

        [TestMethod]
        [TestCategory("RefreshTokenCacheKeyTests")]
        public void TestEquals()
        {
            RefreshTokenCacheKey key1 = new RefreshTokenCacheKey(TestConstants.ProductionEnvironment,
                TestConstants.ClientId, TestConstants.UserIdentifier);

            RefreshTokenCacheKey key2 = new RefreshTokenCacheKey(TestConstants.ProductionEnvironment,
                TestConstants.ClientId, TestConstants.UserIdentifier);
            Assert.IsTrue(key1.Equals(key2));

            //environment
            key2 = new RefreshTokenCacheKey(TestConstants.SovereignEnvironment,
                TestConstants.ClientId, TestConstants.UserIdentifier);
            Assert.IsFalse(key1.Equals(key2));

            //null environment
            key2 = new RefreshTokenCacheKey(null,
                TestConstants.ClientId, TestConstants.UserIdentifier);
            Assert.IsFalse(key1.Equals(key2));
            
            //client id
            key2 = new RefreshTokenCacheKey(TestConstants.ProductionEnvironment,
                null, TestConstants.UserIdentifier);
            Assert.IsFalse(key1.Equals(key2));

            key2 = new RefreshTokenCacheKey(TestConstants.ProductionEnvironment,
                TestConstants.ClientId + "more", TestConstants.UserIdentifier);
            Assert.IsFalse(key1.Equals(key2));

            key2 = new RefreshTokenCacheKey(TestConstants.ProductionEnvironment,
                TestConstants.ClientId, TestConstants.UserIdentifier + "more");
            Assert.IsFalse(key1.Equals(key2));

            // mistmatched object
            Assert.IsFalse(key1.Equals(new object()));

            // null
            Assert.IsFalse(key1.Equals(null));
        }

        [TestMethod]
        [TestCategory("RefreshTokenCacheKeyTests")]
        public void TestHashCode()
        {
            RefreshTokenCacheKey key1 = new RefreshTokenCacheKey(TestConstants.ProductionEnvironment,
                TestConstants.ClientId, TestConstants.UserIdentifier);

            RefreshTokenCacheKey key2 = new RefreshTokenCacheKey(TestConstants.ProductionEnvironment,
                TestConstants.ClientId, TestConstants.UserIdentifier);
            Assert.AreEqual(key1.GetHashCode(), key2.GetHashCode());

            //environment
            key2 = new RefreshTokenCacheKey(TestConstants.SovereignEnvironment,
                TestConstants.ClientId, TestConstants.UserIdentifier);
            Assert.AreNotEqual(key1.GetHashCode(), key2.GetHashCode());

            key2 = new RefreshTokenCacheKey(TestConstants.ProductionEnvironment,
                TestConstants.ClientId + "more", TestConstants.UserIdentifier);
            Assert.AreNotEqual(key1.GetHashCode(), key2.GetHashCode());

            key2 = new RefreshTokenCacheKey(TestConstants.ProductionEnvironment,
                TestConstants.ClientId, TestConstants.UserIdentifier + "more");
            Assert.AreNotEqual(key1.GetHashCode(), key2.GetHashCode());
        }
    }
}
