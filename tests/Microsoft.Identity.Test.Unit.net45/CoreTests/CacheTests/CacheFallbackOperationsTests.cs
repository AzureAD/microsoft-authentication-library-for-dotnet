// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.CoreTests.CacheTests
{
    [TestClass]
    public class CacheFallbackOperationsTests
    {
        private InMemoryLegacyCachePersistence _legacyCachePersistence;
        private ICoreLogger _logger;

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();

            // Methods in CacheFallbackOperations silently catch all exceptions and log them;
            // By setting this to null, logging will fail, making the test fail.
            _logger = Substitute.For<ICoreLogger>();

            // Use the net45 accessor for tests
            _legacyCachePersistence = new InMemoryLegacyCachePersistence();
        }

        [TestMethod]
        public void GetAllAdalUsersForMsal_ScopedBy_ClientIdAndEnv()
        {
            // Arrange
            PopulateLegacyCache(_legacyCachePersistence);

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
                    TestConstants.SovereignNetworkEnvironment },
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
                    TestConstants.SovereignNetworkEnvironment },
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
        public void RemoveAdalUser_RemovesUserWithSameId()
        {
            // Arrange
            PopulateLegacyCache(_legacyCachePersistence);

            PopulateLegacyWithRtAndId( // different clientId -> should not be deleted
                _legacyCachePersistence,
                "other_client_id",
                TestConstants.ProductionPrefNetworkEnvironment,
                "uid1",
                "tenantId1",
                "user1_other_client_id");

            PopulateLegacyWithRtAndId( // different env -> should be deleted
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
                new[] { TestConstants.ProductionPrefNetworkEnvironment},
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
            PopulateLegacyCache(_legacyCachePersistence);

            PopulateLegacyWithRtAndId(
                _legacyCachePersistence,
                "other_client_id",
                TestConstants.ProductionPrefNetworkEnvironment,
                null,
                null,
                "no_client_info_user3"); // no client info, different client id -> won't be deleted

            PopulateLegacyWithRtAndId(
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
            PopulateLegacyCache(_legacyCachePersistence);
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
            // same user/account with client info and wihout
            // CacheFallbackOperations.RemoveAdalUser should remove both
            PopulateLegacyWithRtAndId(
                _legacyCachePersistence,
                TestConstants.ClientId,
                TestConstants.ProductionPrefNetworkEnvironment,
                TestConstants.Uid,
                TestConstants.Utid,
                TestConstants.DisplayableId,
                TestConstants.ScopeStr);

            AssertCacheEntryCount(1);

            PopulateLegacyWithRtAndId(
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

            var rtItem = new MsalRefreshTokenCacheItem(
                TestConstants.ProductionPrefNetworkEnvironment,
                TestConstants.ClientId,
                "someRT",
                MockHelpers.CreateClientInfo("u1", "ut1"));

            var idTokenCacheItem = new MsalIdTokenCacheItem(
                TestConstants.ProductionPrefCacheEnvironment, // different env
                TestConstants.ClientId,
                MockHelpers.CreateIdToken("u1", "username"),
                MockHelpers.CreateClientInfo("u1", "ut1"),
                "ut1");

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

            var rtItem = new MsalRefreshTokenCacheItem(
                TestConstants.ProductionPrefNetworkEnvironment,
                TestConstants.ClientId,
                "someRT",
                MockHelpers.CreateClientInfo("u1", "ut1"),
                "familyId");

            var idTokenCacheItem = new MsalIdTokenCacheItem(
                TestConstants.ProductionPrefNetworkEnvironment, // different env
                TestConstants.ClientId,
                MockHelpers.CreateIdToken("u1", "username"),
                MockHelpers.CreateClientInfo("u1", "ut1"),
                "ut1");

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

        private void PopulateLegacyCache(ILegacyCachePersistence legacyCachePersistence)
        {
            PopulateLegacyWithRtAndId(
                legacyCachePersistence,
                TestConstants.ClientId,
                TestConstants.ProductionPrefNetworkEnvironment,
                "uid1",
                "tenantId1",
                "user1");

            PopulateLegacyWithRtAndId(
                legacyCachePersistence,
                TestConstants.ClientId,
                TestConstants.ProductionPrefNetworkEnvironment,
                "uid2",
                "tenantId2",
                "user2");

            PopulateLegacyWithRtAndId(
                legacyCachePersistence,
                TestConstants.ClientId,
                TestConstants.ProductionPrefNetworkEnvironment,
                null,
                null,
                "no_client_info_user3");

            PopulateLegacyWithRtAndId(
                legacyCachePersistence,
                TestConstants.ClientId,
                TestConstants.ProductionPrefNetworkEnvironment,
                null,
                null,
                "no_client_info_user4");

            PopulateLegacyWithRtAndId(
                legacyCachePersistence,
                TestConstants.ClientId,
                TestConstants.SovereignNetworkEnvironment, // different env
                "uid4",
                "tenantId4",
                "sovereign_user5");

            PopulateLegacyWithRtAndId(
                legacyCachePersistence,
                "other_client_id", // different client id
                TestConstants.SovereignNetworkEnvironment,
                "uid5",
                "tenantId5",
                "user6");
        }

        private static void AssertUsersByDisplayName(
            IEnumerable<string> expectedUsernames,
            IEnumerable<AdalUserInfo> adalUserInfos,
            string errorMessage = "")
        {
            string[] actualUsernames = adalUserInfos.Select(x => x.DisplayableId).ToArray();

            CollectionAssert.AreEquivalent(expectedUsernames.ToArray(), actualUsernames, errorMessage);
        }

        private void PopulateLegacyWithRtAndId(
            ILegacyCachePersistence legacyCachePersistence,
            string clientId,
            string env,
            string uid,
            string uniqueTenantId,
            string username)
        {
            PopulateLegacyWithRtAndId(legacyCachePersistence, clientId, env, uid, uniqueTenantId, username, "scope1");
        }

        private void PopulateLegacyWithRtAndId(
            ILegacyCachePersistence legacyCachePersistence,
            string clientId,
            string env,
            string uid,
            string uniqueTenantId,
            string username,
            string scope)
        {
            string clientInfoString;
            if (string.IsNullOrEmpty(uid) || string.IsNullOrEmpty(uniqueTenantId))
            {
                clientInfoString = null;
            }
            else
            {
                clientInfoString = MockHelpers.CreateClientInfo(uid, uniqueTenantId);
            }

            var rtItem = new MsalRefreshTokenCacheItem(env, clientId, "someRT", clientInfoString);

            var idTokenCacheItem = new MsalIdTokenCacheItem(
                env,
                clientId,
                MockHelpers.CreateIdToken(uid, username),
                clientInfoString,
                uniqueTenantId);

            CacheFallbackOperations.WriteAdalRefreshToken(
                _logger,
                legacyCachePersistence,
                rtItem,
                idTokenCacheItem,
                "https://" + env + "/common",
                "uid",
                scope);
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
