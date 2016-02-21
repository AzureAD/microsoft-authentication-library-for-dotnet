using System.Collections.Generic;
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
            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultScope, TestConstants.DefaultClientId,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId);
            this.ValidateTokenCacheKey(key, true);

            //with policy, user properties
            key = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultScope, TestConstants.DefaultClientId,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId,
                TestConstants.DefaultPolicy);
            this.ValidateTokenCacheKey(key, false);


            User user = new User();
            user.DisplayableId = TestConstants.DefaultDisplayableId;
            user.UniqueId = TestConstants.DefaultUniqueId;
            user.RootId = TestConstants.DefaultRootId;

            //no policy, user object
            key = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultScope, TestConstants.DefaultClientId, user);
            this.ValidateTokenCacheKey(key, true);

            //with policy, user object
            key = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultScope, TestConstants.DefaultClientId, user,
                TestConstants.DefaultPolicy);
            this.ValidateTokenCacheKey(key, false);

        }

        private void ValidateTokenCacheKey(TokenCacheKey key, bool policyMissing)
        {
            Assert.IsNotNull(key);
            Assert.AreEqual(TestConstants.DefaultAuthorityHomeTenant, key.Authority);
            Assert.AreEqual(TestConstants.DefaultScope, key.Scope);
            Assert.AreEqual(TestConstants.DefaultClientId, key.ClientId);
            Assert.AreEqual(TestConstants.DefaultUniqueId, key.UniqueId);
            Assert.AreEqual(TestConstants.DefaultDisplayableId, key.DisplayableId);
            Assert.AreEqual(TestConstants.DefaultRootId, key.RootId);
            Assert.AreEqual(policyMissing, key.Policy == null);

            if (!policyMissing)
            {
                Assert.AreEqual(TestConstants.DefaultPolicy, key.Policy);
            }
        }

        [TestMethod]
        [TestCategory("TokenCacheKeyTests")]
        public void TestEquals()
        {
            TokenCacheKey key1 = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultScope, TestConstants.DefaultClientId,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId,
                TestConstants.DefaultPolicy);

            TokenCacheKey key2 = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultScope, TestConstants.DefaultClientId,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId,
                TestConstants.DefaultPolicy);
            Assert.IsTrue(key1.Equals(key2));

            //authority
            key2 = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant + "more",
                TestConstants.DefaultScope, TestConstants.DefaultClientId,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId,
                TestConstants.DefaultPolicy);
            Assert.IsFalse(key1.Equals(key2));

            key2 = new TokenCacheKey(null,
                TestConstants.DefaultScope, TestConstants.DefaultClientId,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId,
                TestConstants.DefaultPolicy);
            Assert.IsFalse(key1.Equals(key2));

            //null scope
            key2 = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                null, TestConstants.DefaultClientId,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId,
                TestConstants.DefaultPolicy);
            Assert.IsFalse(key1.Equals(key2));

            //client id
            key2 = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultScope, null,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId,
                TestConstants.DefaultPolicy);
            Assert.IsFalse(key1.Equals(key2));

            key2 = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultScope, TestConstants.DefaultClientId + "more",
               
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId,
                TestConstants.DefaultPolicy);
            Assert.IsFalse(key1.Equals(key2));


            //token subject type
            key2 = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultScope, TestConstants.DefaultClientId,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId,
                TestConstants.DefaultPolicy);
            Assert.IsFalse(key1.Equals(key2));

            //unique id
            key2 = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultScope, TestConstants.DefaultClientId,
                null, TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId, TestConstants.DefaultPolicy);
            Assert.IsFalse(key1.Equals(key2));

            key2 = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultScope, TestConstants.DefaultClientId,
                TestConstants.DefaultUniqueId + "more", TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId,
                TestConstants.DefaultPolicy);
            Assert.IsFalse(key1.Equals(key2));

            //displayable id
            key2 = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultScope, TestConstants.DefaultClientId,
                TestConstants.DefaultUniqueId, null, TestConstants.DefaultRootId, TestConstants.DefaultPolicy);
            Assert.IsFalse(key1.Equals(key2));

            key2 = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultScope, TestConstants.DefaultClientId,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId + "more", TestConstants.DefaultRootId,
                TestConstants.DefaultPolicy);
            Assert.IsFalse(key1.Equals(key2));

            //root id
            key2 = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultScope, TestConstants.DefaultClientId,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, null, TestConstants.DefaultPolicy);
            Assert.IsFalse(key1.Equals(key2));

            key2 = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultScope, TestConstants.DefaultClientId,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId + "more",
                TestConstants.DefaultPolicy);
            Assert.IsFalse(key1.Equals(key2));

            //policy
            key2 = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultScope, TestConstants.DefaultClientId,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId, null);
            Assert.IsFalse(key1.Equals(key2));

            key2 = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultScope, TestConstants.DefaultClientId,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId,
                TestConstants.DefaultPolicy + "more");
            Assert.IsFalse(key1.Equals(key2));

            // mistmatched object
            Assert.IsFalse(key1.Equals(new object()));
        }

        [TestMethod]
        [TestCategory("TokenCacheKeyTests")]
        public void TestScopeEquals()
        {

            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultScope, TestConstants.DefaultClientId,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId,
                TestConstants.DefaultPolicy);

            HashSet<string> otherScope = null;
            Assert.IsFalse(key.ScopeEquals(otherScope));

            otherScope = new HashSet<string>(TestConstants.DefaultScope.ToArray());
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
            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                null, TestConstants.DefaultClientId,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId,
                TestConstants.DefaultPolicy);

            //null will intersect with null
            HashSet<string> otherScope = null;
            Assert.IsTrue(key.ScopeIntersects(otherScope));

            //put scope value
            key = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultScope, TestConstants.DefaultClientId,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId,
                TestConstants.DefaultPolicy);
            Assert.IsFalse(key.ScopeIntersects(otherScope));

            otherScope = new HashSet<string>(TestConstants.DefaultScope.ToArray());
            Assert.IsTrue(key.ScopeIntersects(otherScope));

            otherScope.Add("anotherscope");
            Assert.IsTrue(key.ScopeIntersects(otherScope));

            //put values in scope for the key
            key = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                otherScope, TestConstants.DefaultClientId,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId,
                TestConstants.DefaultPolicy);

            Assert.IsTrue(key.ScopeIntersects(TestConstants.DefaultScope));
        }

        [TestMethod]
        [TestCategory("TokenCacheKeyTests")]
        public void TestScopeContains()
        {
            //null scope
            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                null, TestConstants.DefaultClientId,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId,
                TestConstants.DefaultPolicy);

            //null will contain null
            HashSet<string> otherScope = null;
            Assert.IsTrue(key.ScopeContains(otherScope));
            Assert.IsFalse(key.ScopeContains(new HashSet<string>()));

            //put scope value
            key = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultScope, TestConstants.DefaultClientId,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId,
                TestConstants.DefaultPolicy);
            Assert.IsTrue(key.ScopeContains(otherScope));
            Assert.IsTrue(key.ScopeContains(new HashSet<string>()));

            otherScope = new HashSet<string>(TestConstants.DefaultScope.ToArray());
            Assert.IsTrue(key.ScopeContains(otherScope));

            // other scope has more
            otherScope.Add("anotherscope");
            Assert.IsFalse(key.ScopeContains(otherScope));
        }
    }
}