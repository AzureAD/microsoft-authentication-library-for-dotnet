// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Internal.Logger;
using Microsoft.Identity.Client.PlatformsCommon.Factories;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Test.Common.Core.Mocks;
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

            var at1 = TokenCacheHelper.CreateAccessTokenItem("scope1", "tenant1", "homeAccountId");

            // Assert: Save with new item
            accessor.SaveAccessToken(at1);

            Assert.AreEqual(1, accessor.GetAllAccessTokens().Count);
            Assert.AreEqual(1, GetAccessTokenCache(accessor, isAppCache).Count);
            string partitionKey1 = GetPartitionKey(isAppCache, at1);
            Assert.IsNotNull(GetAccessTokenCache(accessor, isAppCache)[partitionKey1][at1.CacheKey]);

            var at2 = TokenCacheHelper.CreateAccessTokenItem("scope2", "tenant1", "homeAccountId");

            // Assert: Save under the existing partition
            accessor.SaveAccessToken(at2);

            Assert.AreEqual(2, accessor.GetAllAccessTokens().Count);
            Assert.AreEqual(1, GetAccessTokenCache(accessor, isAppCache).Count);
            Assert.IsNotNull(GetAccessTokenCache(accessor, isAppCache)[partitionKey1][at2.CacheKey]);

            var at3 = TokenCacheHelper.CreateAccessTokenItem("scope1", "tenant2", "homeAccountId2");

            // Assert: Save under a new partition
            accessor.SaveAccessToken(at3);
            // Assert: Save overwrites the existing token
            accessor.SaveAccessToken(at3);

            Assert.AreEqual(3, accessor.GetAllAccessTokens().Count);
            Assert.AreEqual(2, GetAccessTokenCache(accessor, isAppCache).Count);
            string partitionKey2 = GetPartitionKey(isAppCache, at3);
            Assert.IsNotNull(GetAccessTokenCache(accessor, isAppCache)[partitionKey2][at3.CacheKey]);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void DeleteAccessToken_Test(bool isAppCache)
        {
            var accessor = CreateTokenCacheAccessor(isAppCache);
            var at1 = TokenCacheHelper.CreateAccessTokenItem("scope1", "tenant1", "homeAccountId");
            var at2 = TokenCacheHelper.CreateAccessTokenItem("scope2", "tenant1", "homeAccountId");
            var at3 = TokenCacheHelper.CreateAccessTokenItem("scope1", "tenant2", "homeAccountId2");

            // Assert: Delete on empty collection doesn't throw
            accessor.DeleteAccessToken(at1);

            accessor.SaveAccessToken(at1);
            accessor.SaveAccessToken(at2);
            accessor.SaveAccessToken(at3);

            Assert.AreEqual(3, accessor.GetAllAccessTokens().Count);

            // Assert: Delete an existing item
            accessor.DeleteAccessToken(at1);

            Assert.AreEqual(2, accessor.GetAllAccessTokens().Count);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void GetAllAccessTokens_Test(bool isAppCache)
        {
            var accessor = CreateTokenCacheAccessor(isAppCache);
            var at1 = TokenCacheHelper.CreateAccessTokenItem("scope1", "tenant1", "homeAccountId");
            var at2 = TokenCacheHelper.CreateAccessTokenItem("scope2", "tenant1", "homeAccountId");
            var at3 = TokenCacheHelper.CreateAccessTokenItem("scope1", "tenant2", "homeAccountId2");
            string partitionKey1 = GetPartitionKey(isAppCache, at1);
            string partitionKey2 = GetPartitionKey(isAppCache, at3);

            // Assert: Returns empty collection
            Assert.AreEqual(0, accessor.GetAllAccessTokens().Count);
            Assert.AreEqual(0, accessor.GetAllAccessTokens(partitionKey1).Count);

            accessor.SaveAccessToken(at1);
            accessor.SaveAccessToken(at2);
            accessor.SaveAccessToken(at3);

            // Assert: Get all tokens and get all tokens by partition key
            Assert.AreEqual(3, accessor.GetAllAccessTokens().Count);
            Assert.AreEqual(2, accessor.GetAllAccessTokens(partitionKey1).Count);
            Assert.AreEqual(1, accessor.GetAllAccessTokens(partitionKey2).Count);
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
            accessor.SaveAppMetadata(TokenCacheHelper.CreateAppMetadataItem());
            accessor.SaveAppMetadata(TokenCacheHelper.CreateAppMetadataItem());

            Assert.AreEqual(1, GetAppMetadataCache(accessor, isAppCache).Count);

            // Assert: Different key
            accessor.SaveAppMetadata(TokenCacheHelper.CreateAppMetadataItem(TestConstants.ClientId2));

            Assert.AreEqual(2, GetAppMetadataCache(accessor, isAppCache).Count);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]

        public void GetAppMetadata_Test(bool isAppCache)
        {
            var accessor = CreateTokenCacheAccessor(isAppCache);
            var appMetadataItem = TokenCacheHelper.CreateAppMetadataItem();
            accessor.SaveAppMetadata(appMetadataItem);

            Assert.AreEqual(appMetadataItem.CacheKey, accessor.GetAppMetadata(appMetadataItem).CacheKey);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]

        public void GetAllAppMetadata_Test(bool isAppCache)
        {
            var accessor = CreateTokenCacheAccessor(isAppCache);

            // Assert: Same key overwrites existing item
            accessor.SaveAppMetadata(TokenCacheHelper.CreateAppMetadataItem());
            accessor.SaveAppMetadata(TokenCacheHelper.CreateAppMetadataItem());

            Assert.AreEqual(1, GetAppMetadataCache(accessor, isAppCache).Count);
            Assert.AreEqual(1, accessor.GetAllAppMetadata().Count);

            // Assert: Different key
            accessor.SaveAppMetadata(TokenCacheHelper.CreateAppMetadataItem(TestConstants.ClientId2));

            Assert.AreEqual(2, GetAppMetadataCache(accessor, isAppCache).Count);
            Assert.AreEqual(2, accessor.GetAllAppMetadata().Count);
        }
        #endregion

        #region Refresh token tests
        [TestMethod]
        public void SaveRefreshToken_Test()
        {
            var accessor = new InMemoryPartitionedUserTokenCacheAccessor(new NullLogger(), null);

            var rt1 = TokenCacheHelper.CreateRefreshTokenItem("userAssertion", "homeAccountId");

            // Assert: Saves with new item
            accessor.SaveRefreshToken(rt1);

            Assert.AreEqual(1, accessor.GetAllRefreshTokens().Count);
            Assert.AreEqual(1, accessor.RefreshTokenCacheDictionary.Count);
            string partitionKey1 = CacheKeyFactory.GetKeyFromCachedItem(rt1);
            Assert.IsNotNull(accessor.RefreshTokenCacheDictionary[partitionKey1][rt1.CacheKey]);

            var rt2 = TokenCacheHelper.CreateRefreshTokenItem("userAssertion", "homeAccountId2");

            // Assert: Save under the existing partition
            accessor.SaveRefreshToken(rt2);

            Assert.AreEqual(2, accessor.GetAllRefreshTokens().Count);
            Assert.AreEqual(1, accessor.RefreshTokenCacheDictionary.Count);
            Assert.IsNotNull(accessor.RefreshTokenCacheDictionary[partitionKey1][rt2.CacheKey]);

            var rt3 = TokenCacheHelper.CreateRefreshTokenItem("userAssertion2", "homeAccountId");

            // Assert: Save under a new partition
            accessor.SaveRefreshToken(rt3);
            // Assert: Save overwrites the existing token
            accessor.SaveRefreshToken(rt3);

            Assert.AreEqual(3, accessor.GetAllRefreshTokens().Count);
            Assert.AreEqual(2, accessor.RefreshTokenCacheDictionary.Count);
            string partitionKey2 = CacheKeyFactory.GetKeyFromCachedItem(rt3);
            Assert.IsNotNull(accessor.RefreshTokenCacheDictionary[partitionKey2][rt3.CacheKey]);
        }

        [TestMethod]
        public void DeleteRefreshToken_Test()
        {
            var accessor = new InMemoryPartitionedUserTokenCacheAccessor(new NullLogger(), null);
            var rt1 = TokenCacheHelper.CreateRefreshTokenItem("userAssertion", "homeAccountId");
            var rt2 = TokenCacheHelper.CreateRefreshTokenItem("userAssertion", "homeAccountId2");
            var rt3 = TokenCacheHelper.CreateRefreshTokenItem("userAssertion2", "homeAccountId");

            // Assert: Delete on empty collection doesn't throw
            accessor.DeleteRefreshToken(rt1);

            accessor.SaveRefreshToken(rt1);
            accessor.SaveRefreshToken(rt2);
            accessor.SaveRefreshToken(rt3);

            Assert.AreEqual(3, accessor.GetAllRefreshTokens().Count);

            // Assert: Delete on existing item
            accessor.DeleteRefreshToken(rt1);

            Assert.AreEqual(2, accessor.GetAllRefreshTokens().Count);
        }

        [TestMethod]
        public void GetAllRefreshTokens_Test()
        {
            var accessor = new InMemoryPartitionedUserTokenCacheAccessor(new NullLogger(), null);
            var rt1 = TokenCacheHelper.CreateRefreshTokenItem("userAssertion", "homeAccountId");
            var rt2 = TokenCacheHelper.CreateRefreshTokenItem("userAssertion", "homeAccountId2");
            var rt3 = TokenCacheHelper.CreateRefreshTokenItem("userAssertion2", "homeAccountId");
            string partitionKey1 = CacheKeyFactory.GetKeyFromCachedItem(rt1);
            string partitionKey2 = CacheKeyFactory.GetKeyFromCachedItem(rt3);

            // Assert: Returns empty collection
            Assert.AreEqual(0, accessor.GetAllRefreshTokens().Count);
            Assert.AreEqual(0, accessor.GetAllRefreshTokens(partitionKey1).Count);

            accessor.SaveRefreshToken(rt1);
            accessor.SaveRefreshToken(rt2);
            accessor.SaveRefreshToken(rt3);

            // Assert: Get all tokens and get all tokens by partition key
            Assert.AreEqual(3, accessor.GetAllRefreshTokens().Count);
            Assert.AreEqual(2, accessor.GetAllRefreshTokens(partitionKey1).Count);
            Assert.AreEqual(1, accessor.GetAllRefreshTokens(partitionKey2).Count);
        }
        #endregion

        #region ID token tests
        [TestMethod]
        public void SaveIdToken_Test()
        {
            var accessor = new InMemoryPartitionedUserTokenCacheAccessor(new NullLogger(), null);

            var idt1 = TokenCacheHelper.CreateIdTokenCacheItem("tenant1", "homeAccountId");

            // Assert: Saves with new item
            accessor.SaveIdToken(idt1);

            Assert.AreEqual(1, accessor.GetAllIdTokens().Count);
            Assert.AreEqual(1, accessor.IdTokenCacheDictionary.Count);
            string partitionKey1 = CacheKeyFactory.GetKeyFromCachedItem(idt1);
            Assert.IsNotNull(accessor.IdTokenCacheDictionary[partitionKey1][idt1.CacheKey]);

            var idt2 = TokenCacheHelper.CreateIdTokenCacheItem("tenant2", "homeAccountId");

            // Assert: Save under the existing partition
            accessor.SaveIdToken(idt2);

            Assert.AreEqual(2, accessor.GetAllIdTokens().Count);
            Assert.AreEqual(1, accessor.IdTokenCacheDictionary.Count);
            Assert.IsNotNull(accessor.IdTokenCacheDictionary[partitionKey1][idt2.CacheKey]);

            var idt3 = TokenCacheHelper.CreateIdTokenCacheItem("tenant1", "homeAccountId2");

            // Assert: Save under a new partition
            accessor.SaveIdToken(idt3);
            // Assert: Save overwrites the existing token
            accessor.SaveIdToken(idt3);

            Assert.AreEqual(3, accessor.GetAllIdTokens().Count);
            Assert.AreEqual(2, accessor.IdTokenCacheDictionary.Count);
            string partitionKey2 = CacheKeyFactory.GetKeyFromCachedItem(idt3);
            Assert.IsNotNull(accessor.IdTokenCacheDictionary[partitionKey2][idt3.CacheKey]);
        }

        [TestMethod]
        public void DeleteIdToken_Test()
        {
            var accessor = new InMemoryPartitionedUserTokenCacheAccessor(new NullLogger(), null);
            var idt1 = TokenCacheHelper.CreateIdTokenCacheItem("tenant1", "homeAccountId");
            var idt2 = TokenCacheHelper.CreateIdTokenCacheItem("tenant1", "homeAccountId2");
            var idt3 = TokenCacheHelper.CreateIdTokenCacheItem("tenant2", "homeAccountId");

            // Assert: Delete on empty collection doesn't throw
            accessor.DeleteIdToken(idt1);

            accessor.SaveIdToken(idt1);
            accessor.SaveIdToken(idt2);
            accessor.SaveIdToken(idt3);

            Assert.AreEqual(3, accessor.GetAllIdTokens().Count);

            // Assert: Delete on existing item
            accessor.DeleteIdToken(idt1);

            Assert.AreEqual(2, accessor.GetAllIdTokens().Count);
        }

        [TestMethod]
        public void GetIdToken_Test()
        {
            var accessor = new InMemoryPartitionedUserTokenCacheAccessor(new NullLogger(), null);
            var idt1 = TokenCacheHelper.CreateIdTokenCacheItem("tenant1", "homeAccountId");
            var idt2 = TokenCacheHelper.CreateIdTokenCacheItem("tenant1", "homeAccountId2");
            var idt3 = TokenCacheHelper.CreateIdTokenCacheItem("tenant2", "homeAccountId");
            var at2 = TokenCacheHelper.CreateAccessTokenItem("scope1", "tenant1", "homeAccountId2");
            var at3 = TokenCacheHelper.CreateAccessTokenItem("scope1", "tenant2", "homeAccountId");

            // Assert: Null non-existing item
            Assert.IsNull(accessor.GetIdToken(at2));

            accessor.SaveIdToken(idt1);
            accessor.SaveIdToken(idt2);
            accessor.SaveIdToken(idt3);

            // Assert: Get token by key
            Assert.AreEqual(idt2.CacheKey, accessor.GetIdToken(at2).CacheKey);
            Assert.AreEqual(idt3.CacheKey, accessor.GetIdToken(at3).CacheKey);
        }

        [TestMethod]
        public void GetAllIdTokens_Test()
        {
            var accessor = new InMemoryPartitionedUserTokenCacheAccessor(new NullLogger(), null);
            var idt1 = TokenCacheHelper.CreateIdTokenCacheItem("tenant1", "homeAccountId");
            var idt2 = TokenCacheHelper.CreateIdTokenCacheItem("tenant1", "homeAccountId2");
            var idt3 = TokenCacheHelper.CreateIdTokenCacheItem("tenant2", "homeAccountId");
            string partitionKey1 = CacheKeyFactory.GetKeyFromCachedItem(idt1);
            string partitionKey2 = CacheKeyFactory.GetKeyFromCachedItem(idt2);

            // Assert: Returns empty collection
            Assert.AreEqual(0, accessor.GetAllIdTokens().Count);
            Assert.AreEqual(0, accessor.GetAllIdTokens(partitionKey1).Count);

            accessor.SaveIdToken(idt1);
            accessor.SaveIdToken(idt2);
            accessor.SaveIdToken(idt3);

            // Assert: Get all tokens and get all tokens by partition key
            Assert.AreEqual(3, accessor.GetAllIdTokens().Count);
            Assert.AreEqual(2, accessor.GetAllIdTokens(partitionKey1).Count);
            Assert.AreEqual(1, accessor.GetAllIdTokens(partitionKey2).Count);
        }
        #endregion

        #region Account tests
        [TestMethod]
        public void SaveAccount_Test()
        {
            var accessor = new InMemoryPartitionedUserTokenCacheAccessor(new NullLogger(), null);

            var acc1 = TokenCacheHelper.CreateAccountItem("tenant1", "homeAccountId");

            // Assert: Saves with new item
            accessor.SaveAccount(acc1);

            Assert.AreEqual(1, accessor.GetAllAccounts().Count);
            Assert.AreEqual(1, accessor.AccountCacheDictionary.Count);
            string partitionKey1 = CacheKeyFactory.GetKeyFromCachedItem(acc1);
            Assert.IsNotNull(accessor.AccountCacheDictionary[partitionKey1][acc1.CacheKey]);

            var acc2 = TokenCacheHelper.CreateAccountItem("tenant2", "homeAccountId");

            // Assert: Save under the existing partition
            accessor.SaveAccount(acc2);

            Assert.AreEqual(2, accessor.GetAllAccounts().Count);
            Assert.AreEqual(1, accessor.AccountCacheDictionary.Count);
            Assert.IsNotNull(accessor.AccountCacheDictionary[partitionKey1][acc2.CacheKey]);

            var acc3 = TokenCacheHelper.CreateAccountItem("tenant1", "homeAccountId2");

            // Assert: Save under a new partition
            accessor.SaveAccount(acc3);
            // Assert: Save overwrites the existing token
            accessor.SaveAccount(acc3);

            Assert.AreEqual(3, accessor.GetAllAccounts().Count);
            Assert.AreEqual(2, accessor.AccountCacheDictionary.Count);
            string partitionKey2 = CacheKeyFactory.GetKeyFromCachedItem(acc3);
            Assert.IsNotNull(accessor.AccountCacheDictionary[partitionKey2][acc3.CacheKey]);
        }

        [TestMethod]
        public void DeleteAccount_Test()
        {
            var accessor = new InMemoryPartitionedUserTokenCacheAccessor(new NullLogger(), null);
            var acc1 = TokenCacheHelper.CreateAccountItem("tenant1", "homeAccountId");
            var acc2 = TokenCacheHelper.CreateAccountItem("tenant1", "homeAccountId2");
            var acc3 = TokenCacheHelper.CreateAccountItem("tenant2", "homeAccountId");

            // Assert: Delete on empty collection doesn't throw
            accessor.DeleteAccount(acc1);

            accessor.SaveAccount(acc1);
            accessor.SaveAccount(acc2);
            accessor.SaveAccount(acc3);

            Assert.AreEqual(3, accessor.GetAllAccounts().Count);

            // Assert: Delete on existing item
            accessor.DeleteAccount(acc1);

            Assert.AreEqual(2, accessor.GetAllAccounts().Count);
        }

        [TestMethod]
        public void GetAccount_Test()
        {
            var accessor = new InMemoryPartitionedUserTokenCacheAccessor(new NullLogger(), null);
            var acc1 = TokenCacheHelper.CreateAccountItem("tenant1", "homeAccountId");
            var acc2 = TokenCacheHelper.CreateAccountItem("tenant1", "homeAccountId2");
            var acc3 = TokenCacheHelper.CreateAccountItem("tenant2", "homeAccountId");

            // Assert: Null non-existing item
            Assert.IsNull(accessor.GetAccount(acc1));

            accessor.SaveAccount(acc1);
            accessor.SaveAccount(acc2);
            accessor.SaveAccount(acc3);

            // Assert: Get token by key
            Assert.AreEqual(acc2.CacheKey, accessor.GetAccount(acc2).CacheKey);
            Assert.AreEqual(acc3.CacheKey, accessor.GetAccount(acc3).CacheKey);
        }

        [TestMethod]
        public void GetAllAccounts_Test()
        {
            var accessor = new InMemoryPartitionedUserTokenCacheAccessor(new NullLogger(), null);
            var acc1 = TokenCacheHelper.CreateAccountItem("tenant1", "homeAccountId");
            var acc2 = TokenCacheHelper.CreateAccountItem("tenant1", "homeAccountId2");
            var acc3 = TokenCacheHelper.CreateAccountItem("tenant2", "homeAccountId");
            string partitionKey1 = CacheKeyFactory.GetKeyFromCachedItem(acc1);
            string partitionKey2 = CacheKeyFactory.GetKeyFromCachedItem(acc2);

            // Assert: Returns empty collection
            Assert.AreEqual(0, accessor.GetAllAccounts().Count);
            Assert.AreEqual(0, accessor.GetAllAccounts(partitionKey1).Count);

            accessor.SaveAccount(acc1);
            accessor.SaveAccount(acc2);
            accessor.SaveAccount(acc3);

            // Assert: Get all tokens and get all tokens by partition key
            Assert.AreEqual(3, accessor.GetAllAccounts().Count);
            Assert.AreEqual(2, accessor.GetAllAccounts(partitionKey1).Count);
            Assert.AreEqual(1, accessor.GetAllAccounts(partitionKey2).Count);
        }
        #endregion

        #region Other tests
        [TestMethod]
        public void ClearCache_AppCache_Test()
        {
            var accessor = new InMemoryPartitionedAppTokenCacheAccessor(new NullLogger(), null);
            accessor.SaveAccessToken(TokenCacheHelper.CreateAccessTokenItem());

            Assert.AreEqual(1, accessor.AccessTokenCacheDictionary.Count);

            accessor.Clear();

            Assert.AreEqual(0, accessor.AccessTokenCacheDictionary.Count);
        }

        [TestMethod]
        public void ClearCache_UserCache_Test()
        {
            var accessor = new InMemoryPartitionedUserTokenCacheAccessor(new NullLogger(), null);
            accessor.SaveAccessToken(TokenCacheHelper.CreateAccessTokenItem());
            accessor.SaveRefreshToken(TokenCacheHelper.CreateRefreshTokenItem());
            accessor.SaveIdToken(TokenCacheHelper.CreateIdTokenCacheItem());
            accessor.SaveAccount(TokenCacheHelper.CreateAccountItem());

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
            var accessor = new InMemoryPartitionedAppTokenCacheAccessor(new NullLogger(), null);

            // Assert: false with empty collection
            Assert.IsFalse(accessor.HasAccessOrRefreshTokens());

            accessor.SaveAccessToken(TokenCacheHelper.CreateAccessTokenItem(isExpired: true));

            // Assert: false with expired token
            Assert.IsFalse(accessor.HasAccessOrRefreshTokens());

            accessor.SaveAccessToken(TokenCacheHelper.CreateAccessTokenItem());

            // Assert: true with valid token
            Assert.IsTrue(accessor.HasAccessOrRefreshTokens());
        }

        [TestMethod]
        public void HasAccessOrRefreshTokens_UserCache_Test()
        {
            var accessor = new InMemoryPartitionedUserTokenCacheAccessor(new NullLogger(), null);

            // Assert: false with empty collection
            Assert.IsFalse(accessor.HasAccessOrRefreshTokens());

            accessor.SaveAccessToken(TokenCacheHelper.CreateAccessTokenItem(isExpired: true));

            // Assert: false with expired access token
            Assert.IsFalse(accessor.HasAccessOrRefreshTokens());

            accessor.SaveAccessToken(TokenCacheHelper.CreateAccessTokenItem());

            // Assert: true with access token
            Assert.IsTrue(accessor.HasAccessOrRefreshTokens());

            accessor.Clear();
            accessor.SaveRefreshToken(TokenCacheHelper.CreateRefreshTokenItem());

            // Assert: true with valid refresh token
            Assert.IsTrue(accessor.HasAccessOrRefreshTokens());
        }

        [TestMethod]
        public void NoSupportedMethods_AppCache_Test()
        {
            var accessor = new InMemoryPartitionedAppTokenCacheAccessor(new NullLogger(), null);

            Assert.ThrowsException<NotSupportedException>(() =>
                accessor.SaveRefreshToken(TokenCacheHelper.CreateRefreshTokenItem())
            );

            Assert.ThrowsException<NotSupportedException>(() =>
                accessor.SaveIdToken(TokenCacheHelper.CreateIdTokenCacheItem())
            );

            Assert.ThrowsException<NotSupportedException>(() =>
                accessor.SaveAccount(TokenCacheHelper.CreateAccountItem())
            );

            Assert.ThrowsException<NotSupportedException>(() =>
                accessor.GetIdToken(TokenCacheHelper.CreateAccessTokenItem())
            );

            Assert.ThrowsException<NotSupportedException>(() =>
                accessor.GetAccount(TokenCacheHelper.CreateAccountItem())
            );

            Assert.ThrowsException<NotSupportedException>(() =>
                accessor.DeleteRefreshToken(TokenCacheHelper.CreateRefreshTokenItem())
            );

            Assert.ThrowsException<NotSupportedException>(() =>
                accessor.DeleteIdToken(TokenCacheHelper.CreateIdTokenCacheItem())
            );

            Assert.ThrowsException<NotSupportedException>(() =>
                accessor.DeleteAccount(TokenCacheHelper.CreateAccountItem())
            );

            Assert.AreEqual(0, accessor.GetAllRefreshTokens().Count);
            Assert.AreEqual(0, accessor.GetAllIdTokens().Count);
            Assert.AreEqual(0, accessor.GetAllAccounts().Count);
        }
        #endregion

        private ITokenCacheAccessor CreateTokenCacheAccessor(bool isAppCache)
        {
            return PlatformProxyFactory.CreatePlatformProxy(new NullLogger()).CreateTokenCacheAccessor(null, isAppCache);
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

        private string GetPartitionKey(bool isAppCache, MsalAccessTokenCacheItem atItem)
        {
            return isAppCache ?
                CacheKeyFactory.GetClientCredentialKey(atItem.ClientId, atItem.TenantId, atItem.KeyId) :
                CacheKeyFactory.GetKeyFromCachedItem(atItem);
        }
    }
}
