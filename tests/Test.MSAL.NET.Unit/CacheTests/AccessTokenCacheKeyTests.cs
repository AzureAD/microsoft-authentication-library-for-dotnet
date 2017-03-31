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

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal.Cache;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.MSAL.NET.Unit.CacheTests
{
    [TestClass]
    public class AccessTokenCacheKeyTests
    {
        [TestMethod]
        [TestCategory("AccessTokenCacheKeyTests")]
        public void ConstructorTest()
        {
            AccessTokenCacheKey key = new AccessTokenCacheKey(TestConstants.AuthorityHomeTenant,
                TestConstants.Scope, TestConstants.ClientId, TestConstants.UserIdentifier);
            ValidateTokenCacheKey(key);

            key = new AccessTokenCacheKey(TestConstants.AuthorityHomeTenant,
                null, TestConstants.ClientId, TestConstants.UserIdentifier);
            
            Assert.IsNotNull(key.Scope);
            Assert.AreEqual(0, key.Scope.Count);

        }

        private void ValidateTokenCacheKey(AccessTokenCacheKey key)
        {
            Assert.IsNotNull(key);
            Assert.AreEqual(TestConstants.AuthorityHomeTenant, key.Authority);
            Assert.AreEqual(TestConstants.Scope, key.Scope);
            Assert.AreEqual(TestConstants.ClientId, key.ClientId);
            Assert.AreEqual(TestConstants.UserIdentifier, key.UserIdentifier);
        }

        [TestMethod]
        [TestCategory("AccessTokenCacheKeyTests")]
        public void TestEquals()
        {
            AccessTokenCacheKey key1 = new AccessTokenCacheKey(TestConstants.AuthorityHomeTenant,
                TestConstants.Scope, TestConstants.ClientId, TestConstants.UserIdentifier);

            AccessTokenCacheKey key2 = new AccessTokenCacheKey(TestConstants.AuthorityHomeTenant,
                TestConstants.Scope, TestConstants.ClientId, TestConstants.UserIdentifier);
            Assert.IsTrue(key1.Equals(key2));

            //scope
            key2 = new AccessTokenCacheKey(TestConstants.AuthorityHomeTenant,
                TestConstants.ScopeForAnotherResource, TestConstants.ClientId, TestConstants.UserIdentifier);
            Assert.IsFalse(key1.Equals(key2));

            //different case scope
            SortedSet<string> uppercaseScope = new SortedSet<string>();
            foreach (var item in TestConstants.Scope)
            {
                uppercaseScope.Add(item.ToUpper(CultureInfo.InvariantCulture));
            }

            key2 = new AccessTokenCacheKey(TestConstants.AuthorityHomeTenant,
                uppercaseScope, TestConstants.ClientId, TestConstants.UserIdentifier);
            Assert.IsTrue(key1.Equals(key2));

            //authority
            key2 = new AccessTokenCacheKey(TestConstants.AuthorityHomeTenant + "more",
                TestConstants.Scope, TestConstants.ClientId,  TestConstants.UserIdentifier);
            Assert.IsFalse(key1.Equals(key2));

            key2 = new AccessTokenCacheKey(null,
                TestConstants.Scope, TestConstants.ClientId, TestConstants.UserIdentifier);
            Assert.IsFalse(key1.Equals(key2));

            //null scope
            key2 = new AccessTokenCacheKey(TestConstants.AuthorityHomeTenant,
                null, TestConstants.ClientId, TestConstants.UserIdentifier);
            Assert.IsFalse(key1.Equals(key2));

            //client id
            key2 = new AccessTokenCacheKey(TestConstants.AuthorityHomeTenant,
                TestConstants.Scope, null, TestConstants.UserIdentifier);
            Assert.IsFalse(key1.Equals(key2));

            key2 = new AccessTokenCacheKey(TestConstants.AuthorityHomeTenant,
                TestConstants.Scope, TestConstants.ClientId + "more",  TestConstants.UserIdentifier);
            Assert.IsFalse(key1.Equals(key2));
            
            key2 = new AccessTokenCacheKey(TestConstants.AuthorityHomeTenant,
                TestConstants.Scope, TestConstants.ClientId, TestConstants.UserIdentifier + "more");
            Assert.IsFalse(key1.Equals(key2));
            
            // mistmatched object
            Assert.IsFalse(key1.Equals(new object()));
        }

        [TestMethod]
        [TestCategory("AccessTokenCacheKeyTests")]
        public void TestScopeEquals()
        {

            AccessTokenCacheKey key = new AccessTokenCacheKey(TestConstants.AuthorityHomeTenant,
                TestConstants.Scope, TestConstants.ClientId, TestConstants.UserIdentifier);

            SortedSet<string> otherScope = null;
            Assert.IsFalse(key.ScopeEquals(otherScope));

            otherScope = new SortedSet<string>(TestConstants.Scope.ToArray());
            Assert.IsTrue(key.ScopeEquals(otherScope));

            otherScope.Add("anotherscope");
            Assert.IsFalse(key.ScopeEquals(otherScope));

            otherScope.Clear();
            Assert.IsFalse(key.ScopeEquals(otherScope));
        }

        [TestMethod]
        [TestCategory("AccessTokenCacheKeyTests")]
        public void TestHashCode()
        {
            AccessTokenCacheKey key1 = new AccessTokenCacheKey(TestConstants.AuthorityHomeTenant,
                TestConstants.Scope, TestConstants.ClientId, TestConstants.UserIdentifier);

            AccessTokenCacheKey key2 = new AccessTokenCacheKey(TestConstants.AuthorityHomeTenant,
                TestConstants.Scope, TestConstants.ClientId, TestConstants.UserIdentifier);
            Assert.AreEqual(key1.GetHashCode(), key2.GetHashCode());

            //environment
            key2 = new AccessTokenCacheKey(TestConstants.SovereignEnvironment,
                TestConstants.Scope, TestConstants.ClientId, TestConstants.UserIdentifier);
            Assert.AreNotEqual(key1.GetHashCode(), key2.GetHashCode());

            key2 = new AccessTokenCacheKey(TestConstants.AuthorityHomeTenant,
                TestConstants.Scope, TestConstants.ClientId + "more", TestConstants.UserIdentifier);
            Assert.AreNotEqual(key1.GetHashCode(), key2.GetHashCode());

            key2 = new AccessTokenCacheKey(TestConstants.AuthorityHomeTenant,
                TestConstants.Scope, TestConstants.ClientId, TestConstants.UserIdentifier + "more");
            Assert.AreNotEqual(key1.GetHashCode(), key2.GetHashCode());
        }
    }
}
