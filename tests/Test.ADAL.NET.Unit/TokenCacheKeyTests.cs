using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.ADAL.NET.Unit
{
    public class TokenCacheKeyTests
    {
        public void ConstructorInitCombinations()
        {
            //no policy, user properties
            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityCommon,
                TestConstants.DefaultScope, TestConstants.DefaultClientId, TestConstants.DefaultTokenSubjectType,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId);
            this.ValidateTokenCacheKey(key, true);

            //with policy, user properties
            key = new TokenCacheKey(TestConstants.DefaultAuthorityCommon,
                TestConstants.DefaultScope, TestConstants.DefaultClientId, TestConstants.DefaultTokenSubjectType,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId, TestConstants.DefaultPolicy);
            this.ValidateTokenCacheKey(key, false);


            User user = new User();
            user.DisplayableId = TestConstants.DefaultDisplayableId;
            user.UniqueId = TestConstants.DefaultUniqueId;
            user.RootId = TestConstants.DefaultRootId;

            //no policy, user object
            key = new TokenCacheKey(TestConstants.DefaultAuthorityCommon,
                TestConstants.DefaultScope, TestConstants.DefaultClientId, TestConstants.DefaultTokenSubjectType, user);
            this.ValidateTokenCacheKey(key, true);

            //with policy, user object
            key = new TokenCacheKey(TestConstants.DefaultAuthorityCommon,
                TestConstants.DefaultScope, TestConstants.DefaultClientId, TestConstants.DefaultTokenSubjectType, user, TestConstants.DefaultPolicy);
            this.ValidateTokenCacheKey(key, false);

        }

        private void ValidateTokenCacheKey(TokenCacheKey key, bool policyMissing)
        {
            Assert.IsNotNull(key);
            Assert.AreEqual(TestConstants.DefaultAuthorityCommon, key.Authority);
            Assert.AreEqual(TestConstants.DefaultScope, key.Scope);
            Assert.AreEqual(TestConstants.DefaultClientId, key.ClientId);
            Assert.AreEqual(TestConstants.DefaultTokenSubjectType, key.TokenSubjectType);
            Assert.AreEqual(TestConstants.DefaultUniqueId, key.UniqueId);
            Assert.AreEqual(TestConstants.DefaultDisplayableId, key.DisplayableId);
            Assert.AreEqual(TestConstants.DefaultRootId, key.RootId);
            Assert.AreEqual(policyMissing, key.Policy == null);

            if (!policyMissing)
            {
                Assert.AreEqual(TestConstants.DefaultPolicy, key.Policy);
            }
        }
        
        public void TestEquals()
        {
            TokenCacheKey key1 = new TokenCacheKey(TestConstants.DefaultAuthorityCommon,
                TestConstants.DefaultScope, TestConstants.DefaultClientId, TestConstants.DefaultTokenSubjectType,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId, TestConstants.DefaultPolicy);

            TokenCacheKey key2 = new TokenCacheKey(TestConstants.DefaultAuthorityCommon,
                TestConstants.DefaultScope, TestConstants.DefaultClientId, TestConstants.DefaultTokenSubjectType,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId, TestConstants.DefaultPolicy);

            Assert.IsTrue(key1.Equals(key2));

            // mistmatched object
            Assert.IsFalse(key1.Equals(new object()));
        }
    }
}