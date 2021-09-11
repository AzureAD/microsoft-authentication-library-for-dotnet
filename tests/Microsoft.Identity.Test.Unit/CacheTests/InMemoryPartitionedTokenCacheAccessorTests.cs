// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
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
        #region Access token tests
        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void SaveAccessToken_Test(bool isAppCache)
        {
            var accessor = CreateTokenCacheAccessor(isAppCache);

            var at1 = CreateAccessTokenItem("tenant1", "scope1");

            // Assert: Saves with new tenant and scope
            accessor.SaveAccessToken(at1);

            Assert.AreEqual(1, accessor.GetAllAccessTokens().Count);
            Assert.AreEqual(1, GetAccessTokenCache(accessor, isAppCache).Count);
            string partitionKey1 = CacheKeyFactory.GetClientCredentialKey(TestConstants.ClientId, "tenant1");
            Assert.IsNotNull(GetAccessTokenCache(accessor, isAppCache)[partitionKey1][at1.GetKey().ToString()]);

            var at2 = CreateAccessTokenItem("tenant1", "scope2");

            // Assert: Saves with existing tenant
            accessor.SaveAccessToken(at2);

            Assert.AreEqual(2, accessor.GetAllAccessTokens().Count);
            Assert.AreEqual(1, GetAccessTokenCache(accessor, isAppCache).Count);
            Assert.IsNotNull(GetAccessTokenCache(accessor, isAppCache)[partitionKey1][at2.GetKey().ToString()]);

            var at3 = CreateAccessTokenItem("tenant2", "scope1");

            // Assert: Saves with new tenant
            accessor.SaveAccessToken(at3);
            // Assert: Save overwrites existing tokens
            accessor.SaveAccessToken(at3);

            Assert.AreEqual(3, accessor.GetAllAccessTokens().Count);
            Assert.AreEqual(2, GetAccessTokenCache(accessor, isAppCache).Count);
            string partitionKey2 = CacheKeyFactory.GetClientCredentialKey(TestConstants.ClientId, "tenant2");

            Assert.IsNotNull(GetAccessTokenCache(accessor, isAppCache)[partitionKey2][at3.GetKey().ToString()]);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void DeleteAccessToken_Test(bool isAppCache)
        {
            var accessor = CreateTokenCacheAccessor(isAppCache);
            var at1 = CreateAccessTokenItem("tenant1", "scope1");
            var at2 = CreateAccessTokenItem("tenant1", "scope2");
            var at3 = CreateAccessTokenItem("tenant2", "scope1");

            // Assert: Delete on empty collection doesn't throw
            accessor.DeleteAccessToken(at1);

            accessor.SaveAccessToken(at1);
            accessor.SaveAccessToken(at2);
            accessor.SaveAccessToken(at3);

            Assert.AreEqual(3, accessor.GetAllAccessTokens().Count);

            // Assert: Delete on existing item
            accessor.DeleteAccessToken(at1);

            Assert.AreEqual(2, accessor.GetAllAccessTokens().Count);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void GetAllAccessTokens_Test(bool isAppCache)
        {
            var accessor = CreateTokenCacheAccessor(isAppCache);
            var at1 = CreateAccessTokenItem("tenant1", "scope1");
            var at2 = CreateAccessTokenItem("tenant1", "scope2");
            var at3 = CreateAccessTokenItem("tenant2", "scope1");

            // Assert: Returns empty collection
            Assert.AreEqual(0, accessor.GetAllAccessTokens().Count);
            Assert.AreEqual(0, accessor.GetAllAccessTokens("tenant1").Count);

            accessor.SaveAccessToken(at1);
            accessor.SaveAccessToken(at2);
            accessor.SaveAccessToken(at3);

            // Assert: Get all tokens and get all tokens by tenant
            Assert.AreEqual(3, accessor.GetAllAccessTokens().Count);
            string partitionKey_tenant1 = CacheKeyFactory.GetClientCredentialKey(TestConstants.ClientId, "tenant1");
            string partitionKey_tenant2 = CacheKeyFactory.GetClientCredentialKey(TestConstants.ClientId, "tenant2");
            Assert.AreEqual(2, accessor.GetAllAccessTokens(partitionKey_tenant1).Count);
            Assert.AreEqual(1, accessor.GetAllAccessTokens(partitionKey_tenant2).Count);
        }
        #endregion

        #region App metadata tests
        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]

        public void SaveAppMetadata_Test(bool isAppCache)
        {
            var accessor = CreateTokenCacheAccessor(isAppCache);

            // Assert: Same key overwrites existing item
            accessor.SaveAppMetadata(CreateAppMetadataItem());
            accessor.SaveAppMetadata(CreateAppMetadataItem());

            Assert.AreEqual(1, GetAppMetadataCache(accessor, isAppCache).Count);

            // Assert: Different key
            accessor.SaveAppMetadata(CreateAppMetadataItem(TestConstants.ClientId2));

            Assert.AreEqual(2, GetAppMetadataCache(accessor, isAppCache).Count);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]

        public void GetAppMetadata_Test(bool isAppCache)
        {
            var accessor = CreateTokenCacheAccessor(isAppCache);
            var appMetadataItem = CreateAppMetadataItem();
            accessor.SaveAppMetadata(appMetadataItem);

            Assert.AreEqual(appMetadataItem.GetKey(), accessor.GetAppMetadata(appMetadataItem.GetKey()));
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]

        public void GetAllAppMetadata_Test(bool isAppCache)
        {
            var accessor = CreateTokenCacheAccessor(isAppCache);

            // Assert: Same key overwrites existing item
            accessor.SaveAppMetadata(CreateAppMetadataItem());
            accessor.SaveAppMetadata(CreateAppMetadataItem());

            Assert.AreEqual(1, GetAppMetadataCache(accessor, isAppCache).Count);
            Assert.AreEqual(1, accessor.GetAllAppMetadata().Count);

            // Assert: Different key
            accessor.SaveAppMetadata(CreateAppMetadataItem(TestConstants.ClientId2));

            Assert.AreEqual(2, GetAppMetadataCache(accessor, isAppCache).Count);
            Assert.AreEqual(1, accessor.GetAllAppMetadata().Count);
        }
        #endregion

        #region Refresh token tests
        #endregion

        #region ID token tests
        #endregion

        #region Account tests
        #endregion

        #region Other tests
        [TestMethod]
        public void ClearCache_AppCache_Test()
        {
            var accessor = new InMemoryPartitionedAppTokenCacheAccessor(new NullLogger());
            accessor.SaveAccessToken(CreateAccessTokenItem());

            Assert.AreEqual(1, accessor.AccessTokenCacheDictionary.Count);

            accessor.Clear();

            Assert.AreEqual(0, accessor.AccessTokenCacheDictionary.Count);
        }

        [TestMethod]
        public void ClearCache_UserCache_Test()
        {
            var accessor = new InMemoryPartitionedUserTokenCacheAccessor(new NullLogger());
            accessor.SaveAccessToken(CreateAccessTokenItem());
            accessor.SaveRefreshToken(CreateRefreshTokenItem());
            accessor.SaveIdToken(CreateIdTokenCacheItem());
            accessor.SaveAccount(CreateAccountItem());

            Assert.AreEqual(1, accessor.AccessTokenCacheDictionary.Count);
            Assert.AreEqual(1, accessor.RefreshTokenCacheDictionary.Count);
            Assert.AreEqual(1, accessor.IdTokenCacheDictionary.Count);
            Assert.AreEqual(1, accessor.AccountCacheDictionary.Count);

            accessor.Clear();

            Assert.AreEqual(0, accessor.AccessTokenCacheDictionary.Count);
            Assert.AreEqual(0, accessor.RefreshTokenCacheDictionary.Count);
            Assert.AreEqual(0, accessor.IdTokenCacheDictionary.Count);
            Assert.AreEqual(0, accessor.AccountCacheDictionary.Count);
        }

        [TestMethod]
        public void HasAccessOrRefreshTokens_AppCache_Test()
        {
            var accessor = new InMemoryPartitionedAppTokenCacheAccessor(new NullLogger());

            // Assert: false with empty collection
            Assert.IsFalse(accessor.HasAccessOrRefreshTokens());

            accessor.SaveAccessToken(CreateAccessTokenItem());

            // Assert: false with expired token
            Assert.IsFalse(accessor.HasAccessOrRefreshTokens());

            accessor.SaveAccessToken(CreateAccessTokenItem(isExpired: false));

            // Assert: true with valid token
            Assert.IsFalse(accessor.HasAccessOrRefreshTokens());
        }

        [TestMethod]
        public void HasAccessOrRefreshTokens_UserCache_Test()
        {
            var accessor = new InMemoryPartitionedUserTokenCacheAccessor(new NullLogger());

            // Assert: false with empty collection
            Assert.IsFalse(accessor.HasAccessOrRefreshTokens());

            accessor.SaveAccessToken(CreateAccessTokenItem());

            // Assert: false with expired access token
            Assert.IsFalse(accessor.HasAccessOrRefreshTokens());

            accessor.SaveAccessToken(CreateAccessTokenItem(isExpired: false));

            // Assert: true with access token
            Assert.IsTrue(accessor.HasAccessOrRefreshTokens());

            accessor.Clear();
            accessor.SaveRefreshToken(CreateRefreshTokenItem());

            // Assert: true with valid refresh token
            Assert.IsTrue(accessor.HasAccessOrRefreshTokens());
        }

        [TestMethod]
        public void NoSupportedMethods_AppCache_Test()
        {
            var accessor = new InMemoryPartitionedAppTokenCacheAccessor(new NullLogger());

            Assert.ThrowsException<NotSupportedException>(() =>
                accessor.SaveRefreshToken(CreateRefreshTokenItem())
            );

            Assert.ThrowsException<NotSupportedException>(() =>
                accessor.SaveIdToken(CreateIdTokenCacheItem())
            );

            Assert.ThrowsException<NotSupportedException>(() =>
                accessor.SaveAccount(CreateAccountItem())
            );

            Assert.ThrowsException<NotSupportedException>(() =>
                accessor.GetIdToken(CreateAccessTokenItem())
            );

            Assert.ThrowsException<NotSupportedException>(() =>
                accessor.GetAccount(CreateAccountItem().GetKey())
            );

            Assert.ThrowsException<NotSupportedException>(() =>
                accessor.DeleteRefreshToken(CreateRefreshTokenItem())
            );

            Assert.ThrowsException<NotSupportedException>(() =>
                accessor.DeleteIdToken(CreateIdTokenCacheItem())
            );

            Assert.ThrowsException<NotSupportedException>(() =>
                accessor.DeleteAccount(CreateAccountItem())
            );

            Assert.Equals(0, accessor.GetAllRefreshTokens().Count);
            Assert.Equals(0, accessor.GetAllIdTokens().Count);
            Assert.Equals(0, accessor.GetAllAccounts().Count);
        }
        #endregion

        private MsalAccessTokenCacheItem CreateAccessTokenItem(
            string tenant = TestConstants.AuthorityUtidTenant,
            string scopes = TestConstants.ScopeStr,
            bool isExpired = true)
        {
            var expiresOnUnixTimestamp = isExpired ? "12345" : long.MaxValue.ToString();
            return new MsalAccessTokenCacheItem(scopes)
            {
                ClientId = TestConstants.ClientId,
                Environment = TestConstants.ProductionPrefCacheEnvironment,
                ExpiresOnUnixTimestamp = expiresOnUnixTimestamp,
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

        private MsalRefreshTokenCacheItem CreateRefreshTokenItem()
        {
            return new MsalRefreshTokenCacheItem()
            {
                ClientId = TestConstants.ClientId,
                Environment = TestConstants.ProductionPrefCacheEnvironment,
                HomeAccountId = TestConstants.HomeAccountId,
                UserAssertionHash = "assertion_hash",
            };
        }

        private MsalIdTokenCacheItem CreateIdTokenCacheItem()
        {
            return new MsalIdTokenCacheItem()
            {
                ClientId = TestConstants.ClientId,
                Environment = TestConstants.ProductionPrefCacheEnvironment,
                HomeAccountId = TestConstants.HomeAccountId,
            };
        }

        private MsalAccountCacheItem CreateAccountItem()
        {
            return new MsalAccountCacheItem()
            {
                Environment = TestConstants.ProductionPrefCacheEnvironment,
                HomeAccountId = TestConstants.HomeAccountId,
            };
        }

        private MsalAppMetadataCacheItem CreateAppMetadataItem(
            string clientId = TestConstants.ClientId)
        {
            return new MsalAppMetadataCacheItem(
                clientId,
                TestConstants.ProductionPrefCacheEnvironment,
                null);
        }

        private ITokenCacheAccessor CreateTokenCacheAccessor(bool isAppCache)
        {
            if (isAppCache)
            {
                return new InMemoryPartitionedAppTokenCacheAccessor(new NullLogger());
            }
            else
            {
                return new InMemoryPartitionedUserTokenCacheAccessor(new NullLogger());
            }
        }

        private ConcurrentDictionary<string, ConcurrentDictionary<string, MsalAccessTokenCacheItem>> GetAccessTokenCache(ITokenCacheAccessor accessor, bool isAppCache)
        {
            if (isAppCache)
            {
                return (accessor as InMemoryPartitionedAppTokenCacheAccessor)?.AccessTokenCacheDictionary;
            }
            else
            {
                return (accessor as InMemoryPartitionedUserTokenCacheAccessor)?.AccessTokenCacheDictionary;
            }
        }

        private ConcurrentDictionary<string, MsalAppMetadataCacheItem> GetAppMetadataCache(ITokenCacheAccessor accessor, bool isAppCache)
        {
            if (isAppCache)
            {
                return (accessor as InMemoryPartitionedAppTokenCacheAccessor)?.AppMetadataDictionary;
            }
            else
            {
                return (accessor as InMemoryPartitionedUserTokenCacheAccessor)?.AppMetadataDictionary;
            }
        }
    }
}
