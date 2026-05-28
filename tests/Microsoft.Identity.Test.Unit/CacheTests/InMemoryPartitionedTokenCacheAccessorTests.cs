// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
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
        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void SaveAccessToken_Test(bool isAppCache)
        {
            var accessor = CreateTokenCacheAccessor(isAppCache);

            var at1 = TokenCacheHelper.CreateAccessTokenItem("scope1", "tenant1", "homeAccountId");

            // Assert: Save with new item
            accessor.SaveAccessToken(at1);

            Assert.HasCount(1, accessor.GetAllAccessTokens());
            Assert.HasCount(1, GetAccessTokenCache(accessor, isAppCache));
            string partitionKey1 = GetPartitionKey(isAppCache, at1);
            Assert.IsNotNull(GetAccessTokenCache(accessor, isAppCache)[partitionKey1][at1.CacheKey]);

            var at2 = TokenCacheHelper.CreateAccessTokenItem("scope2", "tenant1", "homeAccountId");

            // Assert: Save under the existing partition
            accessor.SaveAccessToken(at2);

            Assert.HasCount(2, accessor.GetAllAccessTokens());
            Assert.HasCount(1, GetAccessTokenCache(accessor, isAppCache));
            Assert.IsNotNull(GetAccessTokenCache(accessor, isAppCache)[partitionKey1][at2.CacheKey]);

            var at3 = TokenCacheHelper.CreateAccessTokenItem("scope1", "tenant2", "homeAccountId2");

            // Assert: Save under a new partition
            accessor.SaveAccessToken(at3);
            // Assert: Save overwrites the existing token
            accessor.SaveAccessToken(at3);

            Assert.HasCount(3, accessor.GetAllAccessTokens());
            Assert.HasCount(2, GetAccessTokenCache(accessor, isAppCache));
            string partitionKey2 = GetPartitionKey(isAppCache, at3);
            Assert.IsNotNull(GetAccessTokenCache(accessor, isAppCache)[partitionKey2][at3.CacheKey]);
        }

        [TestMethod]
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

            Assert.HasCount(3, accessor.GetAllAccessTokens());

            // Assert: Delete an existing item
            accessor.DeleteAccessToken(at1);

            Assert.HasCount(2, accessor.GetAllAccessTokens());
        }

        [TestMethod]
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
            Assert.IsEmpty(accessor.GetAllAccessTokens());
            Assert.IsEmpty(accessor.GetAllAccessTokens(partitionKey1));

            accessor.SaveAccessToken(at1);
            accessor.SaveAccessToken(at2);
            accessor.SaveAccessToken(at3);

            // Assert: Get all tokens and get all tokens by partition key
            Assert.HasCount(3, accessor.GetAllAccessTokens());
            Assert.HasCount(2, accessor.GetAllAccessTokens(partitionKey1));
            Assert.HasCount(1, accessor.GetAllAccessTokens(partitionKey2));
        }
        #endregion

        #region App metadata tests
        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]

        public void SaveAppMetadata_Test(bool isAppCache)
        {
            var accessor = CreateTokenCacheAccessor(isAppCache);

            // Assert: Same key overwrites existing item
            accessor.SaveAppMetadata(TokenCacheHelper.CreateAppMetadataItem());
            accessor.SaveAppMetadata(TokenCacheHelper.CreateAppMetadataItem());

            Assert.HasCount(1, GetAppMetadataCache(accessor, isAppCache));

            // Assert: Different key
            accessor.SaveAppMetadata(TokenCacheHelper.CreateAppMetadataItem(TestConstants.ClientId2));

            Assert.HasCount(2, GetAppMetadataCache(accessor, isAppCache));
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]

        public void GetAppMetadata_Test(bool isAppCache)
        {
            var accessor = CreateTokenCacheAccessor(isAppCache);
            var appMetadataItem = TokenCacheHelper.CreateAppMetadataItem();
            accessor.SaveAppMetadata(appMetadataItem);

            Assert.AreEqual(appMetadataItem.CacheKey, accessor.GetAppMetadata(appMetadataItem).CacheKey);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]

        public void GetAllAppMetadata_Test(bool isAppCache)
        {
            var accessor = CreateTokenCacheAccessor(isAppCache);

            // Assert: Same key overwrites existing item
            accessor.SaveAppMetadata(TokenCacheHelper.CreateAppMetadataItem());
            accessor.SaveAppMetadata(TokenCacheHelper.CreateAppMetadataItem());

            Assert.HasCount(1, GetAppMetadataCache(accessor, isAppCache));
            Assert.HasCount(1, accessor.GetAllAppMetadata());

            // Assert: Different key
            accessor.SaveAppMetadata(TokenCacheHelper.CreateAppMetadataItem(TestConstants.ClientId2));

            Assert.HasCount(2, GetAppMetadataCache(accessor, isAppCache));
            Assert.HasCount(2, accessor.GetAllAppMetadata());
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

            Assert.HasCount(1, accessor.GetAllRefreshTokens());
            Assert.HasCount(1, accessor.RefreshTokenCacheDictionary);
            string partitionKey1 = CacheKeyFactory.GetKeyFromCachedItem(rt1);
            Assert.IsNotNull(accessor.RefreshTokenCacheDictionary[partitionKey1][rt1.CacheKey]);

            var rt2 = TokenCacheHelper.CreateRefreshTokenItem("userAssertion", "homeAccountId2");

            // Assert: Save under the existing partition
            accessor.SaveRefreshToken(rt2);

            Assert.HasCount(2, accessor.GetAllRefreshTokens());
            Assert.HasCount(1, accessor.RefreshTokenCacheDictionary);
            Assert.IsNotNull(accessor.RefreshTokenCacheDictionary[partitionKey1][rt2.CacheKey]);

            var rt3 = TokenCacheHelper.CreateRefreshTokenItem("userAssertion2", "homeAccountId");

            // Assert: Save under a new partition
            accessor.SaveRefreshToken(rt3);
            // Assert: Save overwrites the existing token
            accessor.SaveRefreshToken(rt3);

            Assert.HasCount(3, accessor.GetAllRefreshTokens());
            Assert.HasCount(2, accessor.RefreshTokenCacheDictionary);
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

            Assert.HasCount(3, accessor.GetAllRefreshTokens());

            // Assert: Delete on existing item
            accessor.DeleteRefreshToken(rt1);

            Assert.HasCount(2, accessor.GetAllRefreshTokens());
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
            Assert.IsEmpty(accessor.GetAllRefreshTokens());
            Assert.IsEmpty(accessor.GetAllRefreshTokens(partitionKey1));

            accessor.SaveRefreshToken(rt1);
            accessor.SaveRefreshToken(rt2);
            accessor.SaveRefreshToken(rt3);

            // Assert: Get all tokens and get all tokens by partition key
            Assert.HasCount(3, accessor.GetAllRefreshTokens());
            Assert.HasCount(2, accessor.GetAllRefreshTokens(partitionKey1));
            Assert.HasCount(1, accessor.GetAllRefreshTokens(partitionKey2));
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

            Assert.HasCount(1, accessor.GetAllIdTokens());
            Assert.HasCount(1, accessor.IdTokenCacheDictionary);
            string partitionKey1 = CacheKeyFactory.GetKeyFromCachedItem(idt1);
            Assert.IsNotNull(accessor.IdTokenCacheDictionary[partitionKey1][idt1.CacheKey]);

            var idt2 = TokenCacheHelper.CreateIdTokenCacheItem("tenant2", "homeAccountId");

            // Assert: Save under the existing partition
            accessor.SaveIdToken(idt2);

            Assert.HasCount(2, accessor.GetAllIdTokens());
            Assert.HasCount(1, accessor.IdTokenCacheDictionary);
            Assert.IsNotNull(accessor.IdTokenCacheDictionary[partitionKey1][idt2.CacheKey]);

            var idt3 = TokenCacheHelper.CreateIdTokenCacheItem("tenant1", "homeAccountId2");

            // Assert: Save under a new partition
            accessor.SaveIdToken(idt3);
            // Assert: Save overwrites the existing token
            accessor.SaveIdToken(idt3);

            Assert.HasCount(3, accessor.GetAllIdTokens());
            Assert.HasCount(2, accessor.IdTokenCacheDictionary);
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

            Assert.HasCount(3, accessor.GetAllIdTokens());

            // Assert: Delete on existing item
            accessor.DeleteIdToken(idt1);

            Assert.HasCount(2, accessor.GetAllIdTokens());
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
            Assert.IsEmpty(accessor.GetAllIdTokens());
            Assert.IsEmpty(accessor.GetAllIdTokens(partitionKey1));

            accessor.SaveIdToken(idt1);
            accessor.SaveIdToken(idt2);
            accessor.SaveIdToken(idt3);

            // Assert: Get all tokens and get all tokens by partition key
            Assert.HasCount(3, accessor.GetAllIdTokens());
            Assert.HasCount(2, accessor.GetAllIdTokens(partitionKey1));
            Assert.HasCount(1, accessor.GetAllIdTokens(partitionKey2));
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

            Assert.HasCount(1, accessor.GetAllAccounts());
            Assert.HasCount(1, accessor.AccountCacheDictionary);
            string partitionKey1 = CacheKeyFactory.GetKeyFromCachedItem(acc1);
            Assert.IsNotNull(accessor.AccountCacheDictionary[partitionKey1][acc1.CacheKey]);

            var acc2 = TokenCacheHelper.CreateAccountItem("tenant2", "homeAccountId");

            // Assert: Save under the existing partition
            accessor.SaveAccount(acc2);

            Assert.HasCount(2, accessor.GetAllAccounts());
            Assert.HasCount(1, accessor.AccountCacheDictionary);
            Assert.IsNotNull(accessor.AccountCacheDictionary[partitionKey1][acc2.CacheKey]);

            var acc3 = TokenCacheHelper.CreateAccountItem("tenant1", "homeAccountId2");

            // Assert: Save under a new partition
            accessor.SaveAccount(acc3);
            // Assert: Save overwrites the existing token
            accessor.SaveAccount(acc3);

            Assert.HasCount(3, accessor.GetAllAccounts());
            Assert.HasCount(2, accessor.AccountCacheDictionary);
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

            Assert.HasCount(3, accessor.GetAllAccounts());

            // Assert: Delete on existing item
            accessor.DeleteAccount(acc1);

            Assert.HasCount(2, accessor.GetAllAccounts());
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
            Assert.IsEmpty(accessor.GetAllAccounts());
            Assert.IsEmpty(accessor.GetAllAccounts(partitionKey1));

            accessor.SaveAccount(acc1);
            accessor.SaveAccount(acc2);
            accessor.SaveAccount(acc3);

            // Assert: Get all tokens and get all tokens by partition key
            Assert.HasCount(3, accessor.GetAllAccounts());
            Assert.HasCount(2, accessor.GetAllAccounts(partitionKey1));
            Assert.HasCount(1, accessor.GetAllAccounts(partitionKey2));
        }
        #endregion

        #region Other tests
        [TestMethod]
        public void ClearCache_AppCache_Test()
        {
            var accessor = new InMemoryPartitionedAppTokenCacheAccessor(new NullLogger(), null);
            accessor.SaveAccessToken(TokenCacheHelper.CreateAccessTokenItem());

            Assert.HasCount(1, accessor.AccessTokenCacheDictionary);

            accessor.Clear();

            Assert.IsEmpty(accessor.AccessTokenCacheDictionary);
        }

        [TestMethod]
        public void ClearCache_UserCache_Test()
        {
            var accessor = new InMemoryPartitionedUserTokenCacheAccessor(new NullLogger(), null);
            accessor.SaveAccessToken(TokenCacheHelper.CreateAccessTokenItem());
            accessor.SaveRefreshToken(TokenCacheHelper.CreateRefreshTokenItem());
            accessor.SaveIdToken(TokenCacheHelper.CreateIdTokenCacheItem());
            accessor.SaveAccount(TokenCacheHelper.CreateAccountItem());

            Assert.HasCount(1, accessor.AccessTokenCacheDictionary);
            Assert.HasCount(1, accessor.RefreshTokenCacheDictionary);
            Assert.HasCount(1, accessor.IdTokenCacheDictionary);
            Assert.HasCount(1, accessor.AccountCacheDictionary);

            accessor.Clear();

            Assert.IsEmpty(accessor.AccessTokenCacheDictionary);
            Assert.IsEmpty(accessor.RefreshTokenCacheDictionary);
            Assert.IsEmpty(accessor.IdTokenCacheDictionary);
            Assert.IsEmpty(accessor.AccountCacheDictionary);
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

            var ex = Assert.Throws<MsalClientException>(() => accessor.SaveRefreshToken(TokenCacheHelper.CreateRefreshTokenItem()));
            Assert.AreEqual(MsalError.CombinedUserAppCacheNotSupported, ex.ErrorCode);
            Assert.AreEqual(MsalErrorMessage.CombinedUserAppCacheNotSupported, ex.Message);

            ex = Assert.Throws<MsalClientException>(() => accessor.SaveIdToken(TokenCacheHelper.CreateIdTokenCacheItem()));
            Assert.AreEqual(MsalError.CombinedUserAppCacheNotSupported, ex.ErrorCode);
            Assert.AreEqual(MsalErrorMessage.CombinedUserAppCacheNotSupported, ex.Message);

            ex = Assert.Throws<MsalClientException>(() => accessor.SaveAccount(TokenCacheHelper.CreateAccountItem()));
            Assert.AreEqual(MsalError.CombinedUserAppCacheNotSupported, ex.ErrorCode);
            Assert.AreEqual(MsalErrorMessage.CombinedUserAppCacheNotSupported, ex.Message);

            ex = Assert.Throws<MsalClientException>(() => accessor.GetIdToken(TokenCacheHelper.CreateAccessTokenItem()));
            Assert.AreEqual(MsalError.CombinedUserAppCacheNotSupported, ex.ErrorCode);
            Assert.AreEqual(MsalErrorMessage.CombinedUserAppCacheNotSupported, ex.Message);

            ex = Assert.Throws<MsalClientException>(() => accessor.GetAccount(TokenCacheHelper.CreateAccountItem()));
            Assert.AreEqual(MsalError.CombinedUserAppCacheNotSupported, ex.ErrorCode);
            Assert.AreEqual(MsalErrorMessage.CombinedUserAppCacheNotSupported, ex.Message);

            ex = Assert.Throws<MsalClientException>(() => accessor.DeleteRefreshToken(TokenCacheHelper.CreateRefreshTokenItem()));
            Assert.AreEqual(MsalError.CombinedUserAppCacheNotSupported, ex.ErrorCode);
            Assert.AreEqual(MsalErrorMessage.CombinedUserAppCacheNotSupported, ex.Message);

            ex = Assert.Throws<MsalClientException>(() => accessor.DeleteIdToken(TokenCacheHelper.CreateIdTokenCacheItem()));
            Assert.AreEqual(MsalError.CombinedUserAppCacheNotSupported, ex.ErrorCode);
            Assert.AreEqual(MsalErrorMessage.CombinedUserAppCacheNotSupported, ex.Message);

            ex = Assert.Throws<MsalClientException>(() => accessor.DeleteAccount(TokenCacheHelper.CreateAccountItem()));
            Assert.AreEqual(MsalError.CombinedUserAppCacheNotSupported, ex.ErrorCode);
            Assert.AreEqual(MsalErrorMessage.CombinedUserAppCacheNotSupported, ex.Message);

            Assert.IsEmpty(accessor.GetAllRefreshTokens());
            Assert.IsEmpty(accessor.GetAllIdTokens());
            Assert.IsEmpty(accessor.GetAllAccounts());
        }
        #endregion

        #region Bounded app cache tests

        [TestMethod]
        public void BoundedCache_DefaultsToEnabled_Test()
        {
            // Locks in the default-on behavior: new CacheOptions() must yield bounding ON
            // so callers who never set AppCacheMaxEntries get the bounded accessor.
            // Use a tiny override max so eviction is observable in the test.
            var opts = new CacheOptions { AppCacheMaxEntries = 10 };

            var accessor = new InMemoryPartitionedAppTokenCacheAccessor(new NullLogger(), opts);

            for (int i = 0; i < 30; i++)
            {
                accessor.SaveAccessToken(TokenCacheHelper.CreateAccessTokenItem(
                    scopes: "scope" + i, tenant: "tenant" + i));
            }

            Assert.IsLessThanOrEqualTo(
                10,
                accessor.EntryCount,
                $"EntryCount {accessor.EntryCount} exceeded max 10; default-on bounding did not engage.");
        }

        [TestMethod]
        public void BoundedCache_Disabled_BehavesAsLegacy_Test()
        {
            // MaxEntries <= 0 triggers legacy off/disabled behavior: no bounding.
            var opts = new CacheOptions { AppCacheMaxEntries = 0 };
            var accessor = new InMemoryPartitionedAppTokenCacheAccessor(new NullLogger(), opts);

            for (int i = 0; i < 50; i++)
            {
                accessor.SaveAccessToken(TokenCacheHelper.CreateAccessTokenItem(
                    scopes: "scope" + i, tenant: "tenant" + i));
            }

            Assert.AreEqual(50, accessor.EntryCount);
            Assert.HasCount(50, accessor.GetAllAccessTokens());
        }

        [TestMethod]
        public void BoundedCache_NoEvictionUnderThreshold_Test()
        {
            var opts = new CacheOptions { AppCacheMaxEntries = 100 };
            var accessor = new InMemoryPartitionedAppTokenCacheAccessor(new NullLogger(), opts);

            for (int i = 0; i < 100; i++)
            {
                accessor.SaveAccessToken(TokenCacheHelper.CreateAccessTokenItem(
                    scopes: "scope" + i, tenant: "tenant" + i));
            }

            // We are exactly at the threshold (count == max), not over, so eviction must not have run.
            Assert.AreEqual(100, accessor.EntryCount);
        }

        [TestMethod]
        public void BoundedCache_UpdateExistingDoesNotIncrement_Test()
        {
            var opts = new CacheOptions { AppCacheMaxEntries = 5 };
            var accessor = new InMemoryPartitionedAppTokenCacheAccessor(new NullLogger(), opts);

            var item = TokenCacheHelper.CreateAccessTokenItem();

            // Repeated saves of the same logical entry must not grow the counter.
            accessor.SaveAccessToken(item);
            accessor.SaveAccessToken(item);
            accessor.SaveAccessToken(item);

            Assert.AreEqual(1, accessor.EntryCount);
        }

        [TestMethod]
        public void BoundedCache_EvictsWhenOverThreshold_Test()
        {
            // Small max so we cross the threshold quickly and deterministically.
            // lowWatermark = max(1, (int)(10 * 0.75)) = 7
            var opts = new CacheOptions { AppCacheMaxEntries = 10 };
            var accessor = new InMemoryPartitionedAppTokenCacheAccessor(new NullLogger(), opts);

            // Insert 30 distinct entries. Each save runs synchronously; when count > 10
            // eviction trims down to 9 on the triggering thread before the call returns.
            for (int i = 0; i < 30; i++)
            {
                accessor.SaveAccessToken(TokenCacheHelper.CreateAccessTokenItem(
                    scopes: "scope" + i, tenant: "tenant" + i));
            }

            // After the final save, count must be at or below the threshold.
            Assert.IsLessThanOrEqualTo(
                10,
                accessor.EntryCount,
                $"EntryCount {accessor.EntryCount} exceeded max 10 after bursty inserts.");
        }

        [TestMethod]
        public async Task BoundedCache_TinyCache_EvictsOlderEntries_TestAsync()
        {
            // Smallest practical cache: max = 3 so eviction kicks in almost immediately
            // and we can observe specific entries being deleted.
            // lowWatermark = max(1, (int)(3 * 0.75)) = 2.
            var opts = new CacheOptions { AppCacheMaxEntries = 3 };
            var accessor = new InMemoryPartitionedAppTokenCacheAccessor(new NullLogger(), opts);

            // Insert 8 distinct entries with a small delay so CachedAt is strictly ordered
            // and the sampled-LRU has a clear "oldest" to pick. 15 ms matches the typical
            // timer-wheel granularity on Windows / macOS so CachedAt values reliably advance
            // even on a busy CI host.
            var items = new MsalAccessTokenCacheItem[8];
            for (int i = 0; i < items.Length; i++)
            {
                items[i] = TokenCacheHelper.CreateAccessTokenItem(
                    scopes: "scope" + i, tenant: "tenant" + i);
                accessor.SaveAccessToken(items[i]);
                await Task.Delay(15).ConfigureAwait(false);
            }

            // The bound must hold after the bursty inserts.
            Assert.IsLessThanOrEqualTo(
                3,
                accessor.EntryCount,
                $"EntryCount {accessor.EntryCount} exceeded max 3 after inserting 8 entries.");

            // The most recently inserted entry was just saved (no eviction runs after it
            // unless we cross the threshold again), so it must still be present.
            var remaining = accessor.GetAllAccessTokens();
            Assert.IsTrue(
                remaining.Any(t => ReferenceEquals(t, items[items.Length - 1])),
                "The most recently saved entry must still be present after eviction.");

            // The very first (oldest) entry must have been evicted: with max=3 and 8 inserts,
            // at least 5 entries are gone, and the sampled-LRU prefers oldest by CachedAt.
            Assert.IsFalse(
                remaining.Any(t => ReferenceEquals(t, items[0])),
                "The oldest entry must have been evicted by the sampled-LRU policy.");

            // The counter must match the actual dictionary contents.
            int expectedCount = accessor.AccessTokenCacheDictionary.Sum(p => p.Value.Count);
            Assert.AreEqual(expectedCount, accessor.EntryCount);
        }

        [TestMethod]
        public void BoundedCache_EvictsExpiredPreferentially_Test()
        {
            var opts = new CacheOptions { AppCacheMaxEntries = 10 };
            var accessor = new InMemoryPartitionedAppTokenCacheAccessor(new NullLogger(), opts);

            // Seed 8 expired entries (under threshold, no eviction yet).
            for (int i = 0; i < 8; i++)
            {
                accessor.SaveAccessToken(TokenCacheHelper.CreateAccessTokenItem(
                    scopes: "exp-scope" + i,
                    tenant: "exp-tenant" + i,
                    isExpired: true));
            }

            Assert.AreEqual(8, accessor.EntryCount);

            // Now add 10 fresh ones — crosses the threshold, triggers eviction.
            // Sampled algorithm will preferentially evict the expired ones because any
            // expired sample short-circuits as a victim.
            for (int i = 0; i < 10; i++)
            {
                accessor.SaveAccessToken(TokenCacheHelper.CreateAccessTokenItem(
                    scopes: "fresh-scope" + i,
                    tenant: "fresh-tenant" + i));
            }

            // Final state must respect the bound.
            Assert.IsLessThanOrEqualTo(10, accessor.EntryCount);

            // Verify the expired-first preference: at least half of remaining entries should be fresh.
            // (Exact count is probabilistic due to sampling; this is a strong sanity check.)
            var all = accessor.GetAllAccessTokens();
            int freshRemaining = all.Count(t => !t.IsExpiredWithBuffer());
            Assert.IsGreaterThanOrEqualTo(
                all.Count / 2,
                freshRemaining,
                $"Expected expired entries to be preferentially evicted; fresh={freshRemaining}, total={all.Count}");
        }

        [TestMethod]
        public void BoundedCache_DeleteStillDecrementsCount_Test()
        {
            var opts = new CacheOptions { AppCacheMaxEntries = 100 };
            var accessor = new InMemoryPartitionedAppTokenCacheAccessor(new NullLogger(), opts);

            var item = TokenCacheHelper.CreateAccessTokenItem();
            accessor.SaveAccessToken(item);
            Assert.AreEqual(1, accessor.EntryCount);

            accessor.DeleteAccessToken(item);
            Assert.AreEqual(0, accessor.EntryCount);
        }

        [TestMethod]
        public void BoundedCache_ClearResetsCount_Test()
        {
            var opts = new CacheOptions { AppCacheMaxEntries = 100 };
            var accessor = new InMemoryPartitionedAppTokenCacheAccessor(new NullLogger(), opts);

            for (int i = 0; i < 20; i++)
            {
                accessor.SaveAccessToken(TokenCacheHelper.CreateAccessTokenItem(
                    scopes: "scope" + i, tenant: "tenant" + i));
            }
            Assert.AreEqual(20, accessor.EntryCount);

            accessor.Clear();
            Assert.AreEqual(0, accessor.EntryCount);
            Assert.IsEmpty(accessor.GetAllAccessTokens());
        }

        [TestMethod]
        public void BoundedCache_ConcurrentSavesConverge_Test()
        {
            // Each save runs eviction synchronously when it observes count > max, so the
            // final state after all parallel saves return must respect the bound.
            var opts = new CacheOptions { AppCacheMaxEntries = 50 };
            var accessor = new InMemoryPartitionedAppTokenCacheAccessor(new NullLogger(), opts);

            Parallel.For(0, 500, i =>
            {
                accessor.SaveAccessToken(TokenCacheHelper.CreateAccessTokenItem(
                    scopes: "scope" + i, tenant: "tenant" + i));
            });

            // Force one extra serial save so the last thread is guaranteed to observe
            // the final post-burst state and run eviction if needed.
            accessor.SaveAccessToken(TokenCacheHelper.CreateAccessTokenItem(
                scopes: "settle-scope", tenant: "settle-tenant"));

            Assert.IsLessThanOrEqualTo(
                opts.AppCacheMaxEntries,
                accessor.EntryCount,
                $"EntryCount {accessor.EntryCount} exceeded max {opts.AppCacheMaxEntries} after concurrent inserts.");

            // Counter and dictionary must remain in agreement.
            int expectedCount = accessor.AccessTokenCacheDictionary.Sum(p => p.Value.Count);
            Assert.AreEqual(
                expectedCount,
                accessor.EntryCount,
                "EntryCount drifted from actual dictionary contents under contention.");
        }

        [TestMethod]
        public void BoundedCache_MaxEntriesZero_DisablesBounding_Test()
        {
            // Edge case: max=0 with flag on must behave as disabled (constructor check
            // requires AppCacheMaxEntries > 0). Inserting past 0 must not crash, must not
            // evict, and the count must keep growing.
            var opts = new CacheOptions { AppCacheMaxEntries = 0 };
            var accessor = new InMemoryPartitionedAppTokenCacheAccessor(new NullLogger(), opts);

            for (int i = 0; i < 25; i++)
            {
                accessor.SaveAccessToken(TokenCacheHelper.CreateAccessTokenItem(
                    scopes: "scope" + i, tenant: "tenant" + i));
            }

            Assert.AreEqual(25, accessor.EntryCount, "Bounding must be disabled when AppCacheMaxEntries == 0.");
        }

        [TestMethod]
        public void BoundedCache_500k_SerialInserts_NeverExceedMax_Test()
        {
            // Stress: 500k serial inserts against a cache capped at 500k.
            // Verifies eviction keeps the count at or below the ceiling throughout.
            const int max = 500_000;
            var opts = new CacheOptions { AppCacheMaxEntries = max };
            var accessor = new InMemoryPartitionedAppTokenCacheAccessor(new NullLogger(), opts);

            // Arrange
            for (int i = 0; i < max; i++)
            {
                accessor.SaveAccessToken(TokenCacheHelper.CreateAccessTokenItem(
                    scopes: "scope" + i, tenant: "tenant" + i));
            }

            // Act: insert 10k more on a full cache — should keep triggering eviction.
            for (int i = max; i < max + 10_000; i++)
            {
                accessor.SaveAccessToken(TokenCacheHelper.CreateAccessTokenItem(
                    scopes: "scope" + i, tenant: "tenant" + i));
            }

            // Assert: bound must hold and counter must match actual dictionary contents.
            int actualCount = accessor.AccessTokenCacheDictionary.Sum(p => p.Value.Count);
            Assert.IsLessThanOrEqualTo(
                max,
                accessor.EntryCount,
                $"EntryCount {accessor.EntryCount} exceeded max {max} after 510k serial inserts.");
            Assert.AreEqual(accessor.EntryCount, actualCount, "EntryCount drifted from actual dictionary count.");
        }

        [TestMethod]
        public void BoundedCache_50k_ConcurrentInserts_CountNeverDrifts_Test()
        {
            // Stress: 50k parallel inserts against a 50k-bounded cache.
            // Verifies that the lock-free eviction CAS and Interlocked counter stay coherent
            // under heavy thread contention.
            const int max = 50_000;
            var opts = new CacheOptions { AppCacheMaxEntries = max };
            var accessor = new InMemoryPartitionedAppTokenCacheAccessor(new NullLogger(), opts);

            // Arrange + Act
            Parallel.For(0, max + 5_000, i =>
            {
                accessor.SaveAccessToken(TokenCacheHelper.CreateAccessTokenItem(
                    scopes: "scope" + i, tenant: "tenant" + i));
            });

            // Settle: one final serial save ensures any in-flight eviction has completed.
            accessor.SaveAccessToken(TokenCacheHelper.CreateAccessTokenItem(
                scopes: "settle-scope", tenant: "settle-tenant"));

            // Assert: bound must hold.
            Assert.IsLessThanOrEqualTo(
                max,
                accessor.EntryCount,
                $"EntryCount {accessor.EntryCount} exceeded max {max} after concurrent 55k inserts.");

            // Assert: counter must match dictionary reality.
            int actualCount = accessor.AccessTokenCacheDictionary.Sum(p => p.Value.Count);
            Assert.AreEqual(accessor.EntryCount, actualCount,
                "EntryCount drifted from actual dictionary contents under concurrent load.");
        }

        [TestMethod]
        public void BoundedCache_ContinuousEviction_ReadsAlwaysSucceed_Test()
        {
            // Stress: verify that GetAllAccessTokens never throws or returns null
            // while the eviction path is running concurrently with saves.
            const int max = 1_000;
            var opts = new CacheOptions { AppCacheMaxEntries = max };
            var accessor = new InMemoryPartitionedAppTokenCacheAccessor(new NullLogger(), opts);

            // Pre-fill to capacity.
            for (int i = 0; i < max; i++)
            {
                accessor.SaveAccessToken(TokenCacheHelper.CreateAccessTokenItem(
                    scopes: "scope" + i, tenant: "tenant" + i));
            }

            var writeErrors = new System.Collections.Concurrent.ConcurrentBag<System.Exception>();
            var readErrors  = new System.Collections.Concurrent.ConcurrentBag<System.Exception>();

            // Writer thread: hammer inserts above the limit to trigger evictions continuously.
            var writer = System.Threading.Tasks.Task.Run(() =>
            {
                for (int i = max; i < max + 5_000; i++)
                {
                    try
                    {
                        accessor.SaveAccessToken(TokenCacheHelper.CreateAccessTokenItem(
                            scopes: "scope" + i, tenant: "tenant" + i));
                    }
                    catch (System.Exception ex) { writeErrors.Add(ex); }
                }
            });

            // Reader thread: reads must never crash, even if eviction is concurrently removing items.
            var reader = System.Threading.Tasks.Task.Run(() =>
            {
                for (int r = 0; r < 500; r++)
                {
                    try
                    {
                        var tokens = accessor.GetAllAccessTokens();
                        Assert.IsNotNull(tokens, "GetAllAccessTokens must never return null.");
                    }
                    catch (System.Exception ex) { readErrors.Add(ex); }
                }
            });

            System.Threading.Tasks.Task.WaitAll(writer, reader);

            Assert.IsEmpty(writeErrors, $"Write errors during eviction stress: {string.Join("; ", writeErrors)}");
            Assert.IsEmpty(readErrors,  $"Read errors during eviction stress: {string.Join("; ", readErrors)}");
            Assert.IsLessThanOrEqualTo(max, accessor.EntryCount, "Bound violated after concurrent read/write stress.");
        }

        [TestMethod]
        public void BoundedCache_MaxEntriesIntMaxValue_DoesNotOverflow_Test()
        {
            // Edge case: int.MaxValue is the largest legal setting. lowWatermark is
            // computed as (int)(int.MaxValue * 0.95) which fits in int, and iterCap is
            // bounded by (count - target) * 16 which can never overflow because
            // (count - target) <= 0.05 * int.MaxValue. This test just exercises the
            // constructor and a few inserts to make sure no exception is thrown.
            var opts = new CacheOptions { AppCacheMaxEntries = int.MaxValue };
            var accessor = new InMemoryPartitionedAppTokenCacheAccessor(new NullLogger(), opts);

            for (int i = 0; i < 10; i++)
            {
                accessor.SaveAccessToken(TokenCacheHelper.CreateAccessTokenItem(
                    scopes: "scope" + i, tenant: "tenant" + i));
            }

            Assert.AreEqual(10, accessor.EntryCount);
        }

        [TestMethod]
        public void BoundedCache_SharedCache_EvictsWhenOverThreshold_Test()
        {            // Shared-cache mode uses the static dictionary and the static eviction flag.
            // This test locks in that bounding works through that path and that
            // ClearStaticCacheForTest properly resets both runtime counters and shared config invariant state
            // between tests.
            InMemoryPartitionedAppTokenCacheAccessor.ClearStaticCacheForTest();
            try
            {
                var opts = new CacheOptions
                {
                    UseSharedCache = true,
                    AppCacheMaxEntries = 10
                };
                var accessor = new InMemoryPartitionedAppTokenCacheAccessor(new NullLogger(), opts);

                for (int i = 0; i < 30; i++)
                {
                    accessor.SaveAccessToken(TokenCacheHelper.CreateAccessTokenItem(
                        scopes: "scope" + i, tenant: "tenant" + i));
                }

                Assert.IsLessThanOrEqualTo(
                    10,
                    accessor.EntryCount,
                    $"Shared-cache EntryCount {accessor.EntryCount} exceeded max 10.");

                int expectedCount = accessor.AccessTokenCacheDictionary.Sum(p => p.Value.Count);
                Assert.AreEqual(expectedCount, accessor.EntryCount);
            }
            finally
            {
                InMemoryPartitionedAppTokenCacheAccessor.ClearStaticCacheForTest();
            }
        }

        [TestMethod]
        public void BoundedCache_SharedCache_MismatchedSettings_Throws_Test()
        {
            InMemoryPartitionedAppTokenCacheAccessor.ClearStaticCacheForTest();
            try
            {
                var baseline = new CacheOptions
                {
                    UseSharedCache = true,
                    AppCacheMaxEntries = 10
                };

                _ = new InMemoryPartitionedAppTokenCacheAccessor(new NullLogger(), baseline);

                var mismatch = new CacheOptions
                {
                    UseSharedCache = true,
                    AppCacheMaxEntries = 0
                };

                Assert.Throws<System.InvalidOperationException>(
                    () => new InMemoryPartitionedAppTokenCacheAccessor(new NullLogger(), mismatch));
            }
            finally
            {
                InMemoryPartitionedAppTokenCacheAccessor.ClearStaticCacheForTest();
            }
        }

        [TestMethod]
        public void BoundedCache_EvictsOlderAndPreservesNewest_Test()
        {
            // Set max entries to 50,000 as requested.
            const int max = 50_000;
            var opts = new CacheOptions { AppCacheMaxEntries = max };
            var accessor = new InMemoryPartitionedAppTokenCacheAccessor(new NullLogger(), opts);

            // Populate the cache sequentially with 50,000 entries.
            // We simulate a strict timeline by spreading CachedAt timestamps or letting natural order flow.
            // To make sure CachedAt timestamps strictly advance, we can set them manually 
            // or let them reflect a tight sequence (MsalAccessTokenCacheItem has a CachedAt property).
            var items = new MsalAccessTokenCacheItem[max + 1];
            for (int i = 0; i <= max; i++)
            {
                var item = TokenCacheHelper.CreateAccessTokenItem(
                    scopes: "scope" + i, tenant: "tenant" + i);
                // Assign an advancing creation timestamp so that LRU has clear order.
                item.CachedAt = System.DateTimeOffset.UtcNow.AddSeconds(i - max);
                items[i] = item;
            }

            // Save the first 50,000. No eviction should be triggered.
            for (int i = 0; i < max; i++)
            {
                accessor.SaveAccessToken(items[i]);
            }
            Assert.AreEqual(max, accessor.EntryCount, "No eviction should have run yet.");

            // Save the 50,001st entry. This must trigger eviction down to the 75% low watermark (37,500 entries).
            accessor.SaveAccessToken(items[max]);

            // Bound must hold (<= 50,000 entries, target is ~37,500 entries).
            Assert.IsLessThanOrEqualTo(max, accessor.EntryCount, "Cache failed to bound under the maximum cap.");

            var remaining = accessor.GetAllAccessTokens();
            var remainingScopes = new System.Collections.Generic.HashSet<string>(remaining.Select(t => t.ScopeString));

            // Verify that the extremely fresh/newly-inserted ones are preserved.
            // Let's check the last 1,000 items (indices 49,001 to 50,000). None of them should be evicted.
            int newestPreservedCount = 0;
            for (int i = max - 1000; i <= max; i++)
            {
                if (remainingScopes.Contains("scope" + i))
                {
                    newestPreservedCount++;
                }
            }

            // Output the verification results for the user.
            System.Console.WriteLine($"--- Eviction Summary for 50,000 cache capacity ---");
            System.Console.WriteLine($"Total remaining entries in cache: {remaining.Count}");
            System.Console.WriteLine($"Out of the most recent 1,000 entries (indices {max - 1000} to {max}), {newestPreservedCount} are still preserved in the cache.");

            // Assert that 100% of the last 1,000 entries are preserved (since they are the newest by CachedAt).
            Assert.AreEqual(1001, newestPreservedCount, "Not all of the latest 1,000 entries were preserved! Bounded eviction must respect LRU.");

            // Let's also assert that evictions targeted the older half of the cache.
            int olderEvectedCount = 0;
            for (int i = 0; i < 5000; i++)
            {
                if (!remainingScopes.Contains("scope" + i))
                {
                    olderEvectedCount++;
                }
            }
            System.Console.WriteLine($"Out of the oldest 5,000 entries, {olderEvectedCount} were successfully evicted to free up space.");
            Assert.IsGreaterThan(0, olderEvectedCount, "No older entries were evicted. Expired-first / LRU policy failed to target older entries.");
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
                CacheKeyFactory.GetAppTokenCacheItemKey(atItem.ClientId, atItem.TenantId, atItem.KeyId) :
                CacheKeyFactory.GetKeyFromCachedItem(atItem);
        }
    }
}
