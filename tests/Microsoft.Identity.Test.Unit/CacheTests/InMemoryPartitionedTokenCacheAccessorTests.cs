// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Internal.Logger;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CacheTests
{
    [TestClass]
    public class InMemoryPartitionedTokenCacheAccessorTests
    {
        [TestMethod]
        public void SaveAccessToken_Test()
        {
            var accessor = new InMemoryPartitionedTokenCacheAccessor(new NullLogger());
            var at1 = CreateAccessTokenItem("tenant1", "scope1");

            // Act: Saves with new tenant and scope
            accessor.SaveAccessToken(at1);

            Assert.AreEqual(1, accessor.GetAllAccessTokens().Count);
            Assert.AreEqual(1, accessor.AccessTokenCacheDictionary.Count);
            Assert.IsNotNull(accessor.AccessTokenCacheDictionary["tenant1"][at1.GetKey().ToString()]);

            var at2 = CreateAccessTokenItem("tenant1", "scope2");

            // Act: Saves with existing tenant
            accessor.SaveAccessToken(at2);

            Assert.AreEqual(2, accessor.GetAllAccessTokens().Count);
            Assert.AreEqual(1, accessor.AccessTokenCacheDictionary.Count);
            Assert.IsNotNull(accessor.AccessTokenCacheDictionary["tenant1"][at2.GetKey().ToString()]);

            var at3 = CreateAccessTokenItem("tenant2", "scope1");

            // Act: Saves with new tenant
            accessor.SaveAccessToken(at3);
            // Act: Save overwrites existing tokens
            accessor.SaveAccessToken(at3);

            Assert.AreEqual(3, accessor.GetAllAccessTokens().Count);
            Assert.AreEqual(2, accessor.AccessTokenCacheDictionary.Count);
            Assert.IsNotNull(accessor.AccessTokenCacheDictionary["tenant2"][at3.GetKey().ToString()]);
        }

        [TestMethod]
        public void GetAccessToken_Test()
        {
            var accessor = new InMemoryPartitionedTokenCacheAccessor(new NullLogger());
            var at1 = CreateAccessTokenItem("tenant1", "scope1");
            var at2 = CreateAccessTokenItem("tenant1", "scope2");
            var at3 = CreateAccessTokenItem("tenant2", "scope1");

            // Act: Returns null from empty collection
            var result = accessor.GetAccessToken(at1.GetKey());

            Assert.IsNull(result);

            accessor.SaveAccessToken(at1);
            accessor.SaveAccessToken(at2);
            accessor.SaveAccessToken(at3);

            // Act
            result = accessor.GetAccessToken(at1.GetKey());

            Assert.IsNotNull(result);
            Assert.AreEqual(at1.GetKey(), result.GetKey());
        }

        [TestMethod]
        public void DeleteAccessToken_Test()
        {
            var accessor = new InMemoryPartitionedTokenCacheAccessor(new NullLogger());
            var at1 = CreateAccessTokenItem("tenant1", "scope1");
            var at2 = CreateAccessTokenItem("tenant1", "scope2");
            var at3 = CreateAccessTokenItem("tenant2", "scope1");

            // Act: Delete on empty collection doesn't throw
            accessor.DeleteAccessToken(at1.GetKey());

            accessor.SaveAccessToken(at1);
            accessor.SaveAccessToken(at2);
            accessor.SaveAccessToken(at3);

            var result = accessor.GetAccessToken(at1.GetKey());

            Assert.IsNotNull(result);
            Assert.AreEqual(3, accessor.GetAllAccessTokens().Count);

            // Act
            accessor.DeleteAccessToken(at1.GetKey());

            result = accessor.GetAccessToken(at1.GetKey());

            Assert.IsNull(result);
            Assert.AreEqual(2, accessor.GetAllAccessTokens().Count);
        }

        [TestMethod]
        public void GetAllAccessTokens_Test()
        {
            var accessor = new InMemoryPartitionedTokenCacheAccessor(new NullLogger());
            var at1 = CreateAccessTokenItem("tenant1", "scope1");
            var at2 = CreateAccessTokenItem("tenant1", "scope2");
            var at3 = CreateAccessTokenItem("tenant2", "scope1");

            // Act: Returns empty collection
            Assert.AreEqual(0, accessor.GetAllAccessTokens().Count);
            Assert.AreEqual(0, accessor.GetAllAccessTokens("tenant1").Count);

            accessor.SaveAccessToken(at1);
            accessor.SaveAccessToken(at2);
            accessor.SaveAccessToken(at3);

            // Act: Get all tokens and get all tokens by tenant
            Assert.AreEqual(3, accessor.GetAllAccessTokens().Count);
            Assert.AreEqual(2, accessor.GetAllAccessTokens("tenant1").Count);
            Assert.AreEqual(1, accessor.GetAllAccessTokens("tenant2").Count);
        }

        [TestMethod]
        public void ClearCache_Test()
        {
            var accessor = new InMemoryPartitionedTokenCacheAccessor(new NullLogger());
            accessor.SaveAccessToken(CreateAccessTokenItem());

            Assert.AreEqual(1, accessor.AccessTokenCacheDictionary.Count);

            accessor.Clear();

            Assert.AreEqual(0, accessor.AccessTokenCacheDictionary.Count);
        }

        private MsalAccessTokenCacheItem CreateAccessTokenItem(
            string tenant = TestConstants.AuthorityUtidTenant,
            string scopes = TestConstants.ScopeStr)
        {
            return new MsalAccessTokenCacheItem(scopes)
            {
                ClientId = TestConstants.ClientId,
                Environment = "env",
                ExpiresOnUnixTimestamp = "12345",
                ExtendedExpiresOnUnixTimestamp = "23456",
                CachedAt = "34567",
                HomeAccountId = TestConstants.HomeAccountId,
                IsExtendedLifeTimeToken = false,
                Secret = "access_token_secret",
                TenantId = tenant,
                RawClientInfo = string.Empty,
                UserAssertionHash = "assertion_hash",
                TokenType = StorageJsonValues.TokenTypeBearer
            };
        }
    }
}
