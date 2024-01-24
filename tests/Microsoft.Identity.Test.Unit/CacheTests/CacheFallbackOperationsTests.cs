// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.CacheTests
{
    [TestClass]
    public class CacheFallbackOperationsTests
    {
        private InMemoryLegacyCachePersistence _legacyCachePersistence;
        private ILoggerAdapter _logger;

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();

            // Methods in CacheFallbackOperations silently catch all exceptions and log them;
            // By setting this to null, logging will fail, making the test fail.
            _logger = Substitute.For<ILoggerAdapter>();

            // Use the netfx accessor for tests
            _legacyCachePersistence = new InMemoryLegacyCachePersistence();
        }

        [TestMethod]
        public void GetAllAdalUsersForMsal_ScopedBy_ClientIdAndEnv()
        {
            // Arrange
            LegacyTokenCacheHelper.PopulateLegacyCache(_logger, _legacyCachePersistence);

            // Act - query users by env and clientId
            var adalUsers =
                CacheFallbackOperations.GetAllAdalUsersForMsal(
                    _logger,
                    _legacyCachePersistence,
                    TestConstants.ClientId);

            AssertByUsername(
                adalUsers,
                new[] {
                    TestConstants.ProductionPrefNetworkEnvironment,
                    TestConstants.SovereignNetworkEnvironmentDE },
                new[]
                {
                    "user1",
                    "user2",
                    "sovereign_user5"
                },
                new[]
                {
                    "no_client_info_user3",
                    "no_client_info_user4"
                });

            AssertByUsername(
              adalUsers,
              new[] {
                    TestConstants.SovereignNetworkEnvironmentDE },
              new[]
              {
                    "sovereign_user5"
              },
              Enumerable.Empty<string>());

            // Act - query users for different clientId and env
            adalUsers = CacheFallbackOperations.GetAllAdalUsersForMsal(
                _logger,
                _legacyCachePersistence,
                "other_client_id");

            // Assert
            AssertByUsername(
                adalUsers,
                null,
                new[]
                {
                    "user6"
                },
                Enumerable.Empty<string>());
        }

        [TestMethod]
        public void GetAllAdalEntriesForMsal_FilterBy_Upn()
        {
            // Arrange
            LegacyTokenCacheHelper.PopulateLegacyCache(_logger, _legacyCachePersistence);

            // Act - query Adal Entries For Msal with valid Upn as a filter
            var rt =
                CacheFallbackOperations.GetRefreshToken(
                    _logger,
                    _legacyCachePersistence,
                    new[] {
                        TestConstants.ProductionPrefNetworkEnvironment,
                        TestConstants.SovereignNetworkEnvironmentDE },
                    TestConstants.ClientId,
                    new Account(null, "User1", null));

            Assert.AreEqual("uid1.tenantId1", rt.HomeAccountId);

            // Act - query Adal Entries For Msal with invalid Upn as a filter
            rt =
                CacheFallbackOperations.GetRefreshToken(
                    _logger,
                    _legacyCachePersistence,
                    new[] {
                        TestConstants.ProductionPrefNetworkEnvironment,
                        TestConstants.SovereignNetworkEnvironmentDE },
                    TestConstants.ClientId,
                    new Account(null, "UserX", null));

            Assert.IsNull(rt, "Expected to find no items");
        }

        [TestMethod]
        public void GetAllAdalEntriesForMsal_FilterBy_UniqueId()
        {
            // Arrange
            LegacyTokenCacheHelper.PopulateLegacyCache(_logger, _legacyCachePersistence);

            // Act - query Adal Entries For Msal with valid UniqueId as a filter
            var rt =
                CacheFallbackOperations.GetRefreshToken(
                    _logger,
                    _legacyCachePersistence,
                    new[] {
                        TestConstants.ProductionPrefNetworkEnvironment,
                        TestConstants.SovereignNetworkEnvironmentDE },
                    TestConstants.ClientId,
                    new Account("uid2.tenant", null, null));

            Assert.AreEqual("uid2.tenantId2", rt.HomeAccountId);

            // Act - query Adal Entries For Msal with invalid UniqueId as a filter
            rt =
                CacheFallbackOperations.GetRefreshToken(
                    _logger,
                    _legacyCachePersistence,
                    new[] {
                        TestConstants.ProductionPrefNetworkEnvironment,
                        TestConstants.SovereignNetworkEnvironmentDE },
                    TestConstants.ClientId,
                    new Account("udiX.tenant", null, null));

            Assert.IsNull(rt, "Expected to find no items");
        }

        [TestMethod]
        public void GetAllAdalEntriesForMsal_NoFilter()
        {
            // Arrange
            LegacyTokenCacheHelper.PopulateLegacyCache(_logger, _legacyCachePersistence);

            // Act - query Adal Entries For Msal with valid Upn and UniqueId as a filter
            var rt =
                CacheFallbackOperations.GetRefreshToken(
                    _logger,
                    _legacyCachePersistence,
                    new[] {
                        TestConstants.ProductionPrefNetworkEnvironment,
                        TestConstants.SovereignNetworkEnvironmentDE },
                    TestConstants.ClientId,
                    new Account(null, null, null)); // too little info here, do not return RT

            Assert.IsNull(rt, "Expected to find no items");
        }

        [TestMethod]
        public void GetAllAdalEntriesForMsal_FilterBy_UniqueIdAndUpn()
        {
            // Arrange
            LegacyTokenCacheHelper.PopulateLegacyCache(_logger, _legacyCachePersistence);

            // Act - query Adal Entries For Msal with valid Upn and UniqueId as a filter
            var rt =
                CacheFallbackOperations.GetRefreshToken(
                    _logger,
                    _legacyCachePersistence,
                    new[] {
                        TestConstants.ProductionPrefNetworkEnvironment,
                        TestConstants.SovereignNetworkEnvironmentDE },
                    TestConstants.ClientId,
                    new Account("uid1.utid", "User1", null));

            Assert.AreEqual("uid1.tenantId1", rt.HomeAccountId);

            // Act - query Adal Entries For Msal with invalid Upn and valid UniqueId as a filter
            rt =
                CacheFallbackOperations.GetRefreshToken(
                    _logger,
                    _legacyCachePersistence,
                    new[] {
                        TestConstants.ProductionPrefNetworkEnvironment,
                        TestConstants.SovereignNetworkEnvironmentDE },
                    TestConstants.ClientId,
                    new Account("uid1.utid", "UserX", null));

            Assert.IsNull(rt, "Expected to find no items");

            // Act - query Adal Entries For Msal with valid Upn and invalid UniqueId as a filter
            rt =
                CacheFallbackOperations.GetRefreshToken(
                    _logger,
                    _legacyCachePersistence,
                    new[] {
                        TestConstants.ProductionPrefNetworkEnvironment,
                        TestConstants.SovereignNetworkEnvironmentDE },
                    TestConstants.ClientId,
                    new Account("uidX.utid", "User1", null));

            Assert.IsNull(rt, "Expected to find no items");
        }

        [TestMethod]
        public void GetAllAdalEntriesForMsal_MultipleRTsPerEnv()
        {
            // Arrange
            LegacyTokenCacheHelper.PopulateLegacyWithRtAndId(
                _logger,
                _legacyCachePersistence,
                TestConstants.ClientId,
                TestConstants.ProductionPrefNetworkEnvironment,
                "uid",
                "tenantId1",
                "user1");

            LegacyTokenCacheHelper.PopulateLegacyWithRtAndId(
                _logger,
                _legacyCachePersistence,
                TestConstants.ClientId,
                TestConstants.ProductionPrefNetworkEnvironment,
                "uid",
                "tenantId2",
                "user2");

            // Act
            var rt = CacheFallbackOperations.GetRefreshToken(
                _logger,
                _legacyCachePersistence,
                new[] { TestConstants.ProductionPrefNetworkEnvironment },
                TestConstants.ClientId,
                new Account("uid.b", null, null));

            // Assert
            Assert.IsNotNull(rt, "One of the tokens should be returned");
        }

        [TestMethod]
        public void RemoveAdalUser_RemovesUserWithSameId()
        {
            // Arrange
            LegacyTokenCacheHelper.PopulateLegacyCache(_logger, _legacyCachePersistence);

            LegacyTokenCacheHelper.PopulateLegacyWithRtAndId( // different clientId -> should not be deleted
                _logger,
                _legacyCachePersistence,
                "other_client_id",
                TestConstants.ProductionPrefNetworkEnvironment,
                "uid1",
                "tenantId1",
                "user1_other_client_id");

            LegacyTokenCacheHelper.PopulateLegacyWithRtAndId( // different env -> should be deleted
                _logger,
                _legacyCachePersistence,
                TestConstants.ClientId,
                "other_env",
                "uid1",
                "tenantId1",
                "user1_other_env");

            // Act - delete with id and displayname
            CacheFallbackOperations.RemoveAdalUser(
                _logger,
                _legacyCachePersistence,
                TestConstants.ClientId,
                "username_does_not_matter",
                "uid1.tenantId1");

            // Assert
            var adalUsers =
                CacheFallbackOperations.GetAllAdalUsersForMsal(
                    _logger,
                    _legacyCachePersistence,
                    TestConstants.ClientId);

            AssertByUsername(
                adalUsers,
                new[] { TestConstants.ProductionPrefNetworkEnvironment },
                new[]
                {
                    "user2",
                },
                new[]
                {
                    "no_client_info_user3",
                    "no_client_info_user4"
                });
        }

        [TestMethod]
        public void RemoveAdalUser_RemovesUserNoClientInfo()
        {
            // Arrange
            LegacyTokenCacheHelper.PopulateLegacyCache(_logger, _legacyCachePersistence);

            LegacyTokenCacheHelper.PopulateLegacyWithRtAndId(
                _logger,
                _legacyCachePersistence,
                "other_client_id",
                TestConstants.ProductionPrefNetworkEnvironment,
                null,
                null,
                "no_client_info_user3"); // no client info, different client id -> won't be deleted

            LegacyTokenCacheHelper.PopulateLegacyWithRtAndId(
                _logger,
                _legacyCachePersistence,
                TestConstants.ClientId,
                "other_env",
                null,
                null,
                "no_client_info_user3"); // no client info, different env -> won't be deleted

            AssertCacheEntryCount(8);

            var adalUsers =
                CacheFallbackOperations.GetAllAdalUsersForMsal(
                    _logger,
                    _legacyCachePersistence,
                    TestConstants.ClientId);

            AssertByUsername(
                adalUsers,
                new[] { TestConstants.ProductionPrefNetworkEnvironment },
                new[]
                {
                    "user2",
                    "user1",
                },
                new[]
                {
                    "no_client_info_user3",
                    "no_client_info_user4"
                });

            // Act - delete with no client info -> displayable id is used
            CacheFallbackOperations.RemoveAdalUser(
                _logger,
                _legacyCachePersistence,
                TestConstants.ClientId,
                "no_client_info_user3",
                "");

            AssertCacheEntryCount(6);

            // Assert
            adalUsers = CacheFallbackOperations.GetAllAdalUsersForMsal(
                _logger,
                _legacyCachePersistence,
                TestConstants.ClientId);

            AssertByUsername(
                adalUsers,
                new[] { TestConstants.ProductionPrefNetworkEnvironment },
                new[]
                {
                    "user2",
                    "user1",
                },
                new[]
                {
                    "no_client_info_user4"
                });
        }

        private void AssertCacheEntryCount(int expectedEntryCount)
        {
            IDictionary<AdalTokenCacheKey, AdalResultWrapper> cache =
                AdalCacheOperations.Deserialize(_logger, _legacyCachePersistence.LoadCache());
            Assert.AreEqual(expectedEntryCount, cache.Count);
        }

        [TestMethod]
        public void RemoveAdalUser_RemovesUserNoClientInfo_And_NoDisplayName()
        {
            // Arrange
            LegacyTokenCacheHelper.PopulateLegacyCache(_logger, _legacyCachePersistence);
            IDictionary<AdalTokenCacheKey, AdalResultWrapper> adalCacheBeforeDelete =
                AdalCacheOperations.Deserialize(_logger, _legacyCachePersistence.LoadCache());
            Assert.AreEqual(6, adalCacheBeforeDelete.Count);

            // Act - nothing happens and a message is logged
            CacheFallbackOperations.RemoveAdalUser(
                _logger,
                _legacyCachePersistence,
                TestConstants.ClientId,
                "",
                "");

            // Assert
            AssertCacheEntryCount(6);

            _logger.Received().Error(Arg.Is<string>(MsalErrorMessage.InternalErrorCacheEmptyUsername));
        }

        [TestMethod]
        public void RemoveAdalUser_RemovesAdalEntitiesWithClientInfoAndWithout()
        {
            // in case of adalv3 -> adalv4 -> msal2 migration
            // adal cache can have different cache entities for the
            // same user/account with client info and without
            // CacheFallbackOperations.RemoveAdalUser should remove both
            LegacyTokenCacheHelper.PopulateLegacyWithRtAndId(
                _logger,
                _legacyCachePersistence,
                TestConstants.ClientId,
                TestConstants.ProductionPrefNetworkEnvironment,
                TestConstants.Uid,
                TestConstants.Utid,
                TestConstants.DisplayableId,
                TestConstants.ScopeStr);

            AssertCacheEntryCount(1);

            LegacyTokenCacheHelper.PopulateLegacyWithRtAndId(
                _logger,
                _legacyCachePersistence,
                TestConstants.ClientId,
                TestConstants.ProductionPrefNetworkEnvironment,
                null,
                null,
                TestConstants.DisplayableId,
                TestConstants.ScopeForAnotherResourceStr);

            AssertCacheEntryCount(2);

            CacheFallbackOperations.RemoveAdalUser(
                _logger,
                _legacyCachePersistence,
                TestConstants.ClientId,
                TestConstants.DisplayableId,
                TestConstants.Uid + "." + TestConstants.Utid);

            AssertCacheEntryCount(0);
        }

        [TestMethod]
        public void WriteAdalRefreshToken_ErrorLog()
        {
            // Arrange
            _legacyCachePersistence.ThrowOnWrite = true;
            string clientInfo = MockHelpers.CreateClientInfo("u1", "ut1");
            string homeAccountId = ClientInfo.CreateFromJson(clientInfo).ToAccountIdentifier();

            var rtItem = new MsalRefreshTokenCacheItem(
                TestConstants.ProductionPrefNetworkEnvironment,
                TestConstants.ClientId,
                "someRT",
                clientInfo,
                null,
                homeAccountId);

            var idTokenCacheItem = new MsalIdTokenCacheItem(
                TestConstants.ProductionPrefCacheEnvironment, // different env
                TestConstants.ClientId,
                MockHelpers.CreateIdToken("u1", "username"),
                clientInfo,
                tenantId: "ut1",
                homeAccountId: homeAccountId);

            // Act
            CacheFallbackOperations.WriteAdalRefreshToken(
                _logger,
                _legacyCachePersistence,
                rtItem,
                idTokenCacheItem,
                "https://some_env.com/common", // yet another env
                "uid",
                "scope1");

            // Assert
            _logger.Received().Error(Arg.Is<string>(CacheFallbackOperations.DifferentAuthorityError));

            _logger.Received().Error(Arg.Is<string>(CacheFallbackOperations.DifferentEnvError));
        }

        [TestMethod]
        public void DoNotWriteFRTs()
        {
            // Arrange
            _legacyCachePersistence.ThrowOnWrite = true;
            string clientInfo = MockHelpers.CreateClientInfo("u1", "ut1");
            string homeAccountId = ClientInfo.CreateFromJson(clientInfo).ToAccountIdentifier();

            var rtItem = new MsalRefreshTokenCacheItem(
                TestConstants.ProductionPrefNetworkEnvironment,
                TestConstants.ClientId,
                "someRT",
                clientInfo,
                "familyId",
                homeAccountId);

            var idTokenCacheItem = new MsalIdTokenCacheItem(
                TestConstants.ProductionPrefNetworkEnvironment, // different env
                TestConstants.ClientId,
                MockHelpers.CreateIdToken("u1", "username"),
                clientInfo,
                tenantId: "ut1",
                homeAccountId: homeAccountId);

            // Act
            CacheFallbackOperations.WriteAdalRefreshToken(
                _logger,
                _legacyCachePersistence,
                rtItem,
                idTokenCacheItem,
                "https://some_env.com/common", // yet another env
                "uid",
                "scope1");

            AssertCacheEntryCount(0);
        }

        private static void AssertUsersByDisplayName(
            IEnumerable<string> expectedUsernames,
            IEnumerable<AdalUserInfo> adalUserInfos,
            string errorMessage = "")
        {
            string[] actualUsernames = adalUserInfos.Select(x => x.DisplayableId).ToArray();

            CollectionAssert.AreEquivalent(expectedUsernames.ToArray(), actualUsernames, errorMessage);
        }

        private static void AssertByUsername(
            AdalUsersForMsal adalUsers,
            IEnumerable<string> enviroments,
            IEnumerable<string> expectedUsersWithClientInfo,
            IEnumerable<string> expectedUsersWithoutClientInfo)
        {
            // Assert
            var usersWithClientInfo = adalUsers.GetUsersWithClientInfo(enviroments).Select(x => x.Value);
            IEnumerable<AdalUserInfo> usersWithoutClientInfo = adalUsers.GetUsersWithoutClientInfo(enviroments);

            AssertUsersByDisplayName(
                expectedUsersWithClientInfo,
                usersWithClientInfo,
                "Expecting only user1 and user2 because the other users either have no ClientInfo or have a different env or clientid");
            AssertUsersByDisplayName(expectedUsersWithoutClientInfo, usersWithoutClientInfo);
        }
    }

    public class InMemoryLegacyCachePersistence : ILegacyCachePersistence
    {
        private byte[] data;
        public bool ThrowOnWrite { get; set; } = false;

        public byte[] LoadCache()
        {
            return data;
        }

        public void WriteCache(byte[] serializedCache)
        {
            if (ThrowOnWrite)
            {
                throw new InvalidOperationException();
            }

            data = serializedCache;
        }
    }
}
