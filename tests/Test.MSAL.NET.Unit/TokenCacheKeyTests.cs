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
using Microsoft.Identity.Client.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.MSAL.NET.Unit
{
    [TestClass]
    public class TokenCacheKeyTests
    {
        [TestMethod]
        [TestCategory("TokenCacheKeyTests")]
        public void ConstructorInitCombinations()
        {
            //no policy, user properties
            TokenCacheKey key = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                TestConstants.Scope, TestConstants.ClientId,
                TestConstants.UniqueId, TestConstants.DisplayableId, TestConstants.HomeObjectId);
            this.ValidateTokenCacheKey(key, true);

            //with policy, user properties
            key = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                TestConstants.Scope, TestConstants.ClientId,
                TestConstants.UniqueId, TestConstants.DisplayableId, TestConstants.HomeObjectId,
                TestConstants.Policy);
            this.ValidateTokenCacheKey(key, false);


            User user = new User();
            user.DisplayableId = TestConstants.DisplayableId;
            user.UniqueId = TestConstants.UniqueId;
            user.HomeObjectId = TestConstants.HomeObjectId;

            //no policy, user object
            key = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                TestConstants.Scope, TestConstants.ClientId, user);
            this.ValidateTokenCacheKey(key, true);

            //with policy, user object
            key = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                TestConstants.Scope, TestConstants.ClientId, user,
                TestConstants.Policy);
            this.ValidateTokenCacheKey(key, false);

        }

        private void ValidateTokenCacheKey(TokenCacheKey key, bool policyMissing)
        {
            Assert.IsNotNull(key);
            Assert.AreEqual(TestConstants.AuthorityHomeTenant, key.Authority);
            Assert.AreEqual(TestConstants.Scope, key.Scope);
            Assert.AreEqual(TestConstants.ClientId, key.ClientId);
            Assert.AreEqual(TestConstants.UniqueId, key.UniqueId);
            Assert.AreEqual(TestConstants.DisplayableId, key.DisplayableId);
            Assert.AreEqual(TestConstants.HomeObjectId, key.HomeObjectId);
            Assert.AreEqual(policyMissing, key.Policy == null);

            if (!policyMissing)
            {
                Assert.AreEqual(TestConstants.Policy, key.Policy);
            }
        }

        [TestMethod]
        [TestCategory("TokenCacheKeyTests")]
        public void TestEquals()
        {
            TokenCacheKey key1 = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                TestConstants.Scope, TestConstants.ClientId,
                TestConstants.UniqueId, TestConstants.DisplayableId, TestConstants.HomeObjectId,
                TestConstants.Policy);

            TokenCacheKey key2 = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                TestConstants.Scope, TestConstants.ClientId,
                TestConstants.UniqueId, TestConstants.DisplayableId, TestConstants.HomeObjectId,
                TestConstants.Policy);
            Assert.IsTrue(key1.Equals(key2));

            //scope
            key2 = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                TestConstants.ScopeForAnotherResource, TestConstants.ClientId,
                TestConstants.UniqueId, TestConstants.DisplayableId, TestConstants.HomeObjectId,
                TestConstants.Policy);
            Assert.IsFalse(key1.Equals(key2));

            //different case scope
            SortedSet<string> uppercaseScope = new SortedSet<string>();
            foreach (var item in TestConstants.Scope)
            {
                uppercaseScope.Add(item.ToUpper(CultureInfo.InvariantCulture));
            }

            key2 = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                uppercaseScope, TestConstants.ClientId,
                TestConstants.UniqueId, TestConstants.DisplayableId, TestConstants.HomeObjectId,
                TestConstants.Policy);
            Assert.IsTrue(key1.Equals(key2));

            //authority
            key2 = new TokenCacheKey(TestConstants.AuthorityHomeTenant + "more",
                TestConstants.Scope, TestConstants.ClientId,
                TestConstants.UniqueId, TestConstants.DisplayableId, TestConstants.HomeObjectId,
                TestConstants.Policy);
            Assert.IsFalse(key1.Equals(key2));

            key2 = new TokenCacheKey(null,
                TestConstants.Scope, TestConstants.ClientId,
                TestConstants.UniqueId, TestConstants.DisplayableId, TestConstants.HomeObjectId,
                TestConstants.Policy);
            Assert.IsFalse(key1.Equals(key2));

            //null scope
            key2 = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                null, TestConstants.ClientId,
                TestConstants.UniqueId, TestConstants.DisplayableId, TestConstants.HomeObjectId,
                TestConstants.Policy);
            Assert.IsFalse(key1.Equals(key2));

            //client id
            key2 = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                TestConstants.Scope, null,
                TestConstants.UniqueId, TestConstants.DisplayableId, TestConstants.HomeObjectId,
                TestConstants.Policy);
            Assert.IsFalse(key1.Equals(key2));

            key2 = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                TestConstants.Scope, TestConstants.ClientId + "more",
               
                TestConstants.UniqueId, TestConstants.DisplayableId, TestConstants.HomeObjectId,
                TestConstants.Policy);
            Assert.IsFalse(key1.Equals(key2));

            //unique id
            key2 = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                TestConstants.Scope, TestConstants.ClientId,
                null, TestConstants.DisplayableId, TestConstants.HomeObjectId, TestConstants.Policy);
            Assert.IsFalse(key1.Equals(key2));

            key2 = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                TestConstants.Scope, TestConstants.ClientId,
                TestConstants.UniqueId + "more", TestConstants.DisplayableId, TestConstants.HomeObjectId,
                TestConstants.Policy);
            Assert.IsFalse(key1.Equals(key2));

            //displayable id
            key2 = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                TestConstants.Scope, TestConstants.ClientId,
                TestConstants.UniqueId, null, TestConstants.HomeObjectId, TestConstants.Policy);
            Assert.IsFalse(key1.Equals(key2));

            key2 = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                TestConstants.Scope, TestConstants.ClientId,
                TestConstants.UniqueId, TestConstants.DisplayableId + "more", TestConstants.HomeObjectId,
                TestConstants.Policy);
            Assert.IsFalse(key1.Equals(key2));

            //root id
            key2 = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                TestConstants.Scope, TestConstants.ClientId,
                TestConstants.UniqueId, TestConstants.DisplayableId, null, TestConstants.Policy);
            Assert.IsFalse(key1.Equals(key2));

            key2 = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                TestConstants.Scope, TestConstants.ClientId,
                TestConstants.UniqueId, TestConstants.DisplayableId, TestConstants.HomeObjectId + "more",
                TestConstants.Policy);
            Assert.IsFalse(key1.Equals(key2));

            //policy
            key2 = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                TestConstants.Scope, TestConstants.ClientId,
                TestConstants.UniqueId, TestConstants.DisplayableId, TestConstants.HomeObjectId, null);
            Assert.IsFalse(key1.Equals(key2));

            key2 = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                TestConstants.Scope, TestConstants.ClientId,
                TestConstants.UniqueId, TestConstants.DisplayableId, TestConstants.HomeObjectId,
                TestConstants.Policy + "more");
            Assert.IsFalse(key1.Equals(key2));

            // mistmatched object
            Assert.IsFalse(key1.Equals(new object()));
        }

        [TestMethod]
        [TestCategory("TokenCacheKeyTests")]
        public void TestScopeEquals()
        {

            TokenCacheKey key = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                TestConstants.Scope, TestConstants.ClientId,
                TestConstants.UniqueId, TestConstants.DisplayableId, TestConstants.HomeObjectId,
                TestConstants.Policy);

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
        [TestCategory("TokenCacheKeyTests")]
        public void TestScopeIntersects()
        {
            //null scope
            TokenCacheKey key = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                null, TestConstants.ClientId,
                TestConstants.UniqueId, TestConstants.DisplayableId, TestConstants.HomeObjectId,
                TestConstants.Policy);

            //null will intersect with null
            SortedSet<string> otherScope = null;
            Assert.IsTrue(key.ScopeIntersects(otherScope));

            //put scope value
            key = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                TestConstants.Scope, TestConstants.ClientId,
                TestConstants.UniqueId, TestConstants.DisplayableId, TestConstants.HomeObjectId,
                TestConstants.Policy);
            Assert.IsFalse(key.ScopeIntersects(otherScope));

            otherScope = new SortedSet<string>(TestConstants.Scope.ToArray());
            Assert.IsTrue(key.ScopeIntersects(otherScope));

            otherScope.Add("anotherscope");
            Assert.IsTrue(key.ScopeIntersects(otherScope));

            //put values in scope for the key
            key = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                otherScope, TestConstants.ClientId,
                TestConstants.UniqueId, TestConstants.DisplayableId, TestConstants.HomeObjectId,
                TestConstants.Policy);

            Assert.IsTrue(key.ScopeIntersects(TestConstants.Scope));
        }

        [TestMethod]
        [TestCategory("TokenCacheKeyTests")]
        public void TestScopeContains()
        {
            //null scope
            TokenCacheKey key = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                null, TestConstants.ClientId,
                TestConstants.UniqueId, TestConstants.DisplayableId, TestConstants.HomeObjectId,
                TestConstants.Policy);

            //null will contain null
            SortedSet<string> otherScope = null;
            Assert.IsTrue(key.ScopeContains(otherScope));
            Assert.IsFalse(key.ScopeContains(new SortedSet<string>()));

            //put scope value
            key = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                TestConstants.Scope, TestConstants.ClientId,
                TestConstants.UniqueId, TestConstants.DisplayableId, TestConstants.HomeObjectId,
                TestConstants.Policy);
            Assert.IsTrue(key.ScopeContains(otherScope));
            Assert.IsTrue(key.ScopeContains(new SortedSet<string>()));

            otherScope = new SortedSet<string>(TestConstants.Scope.ToArray());
            Assert.IsTrue(key.ScopeContains(otherScope));

            // other scope has more
            otherScope.Add("anotherscope");
            Assert.IsFalse(key.ScopeContains(otherScope));
        }
    }
}
