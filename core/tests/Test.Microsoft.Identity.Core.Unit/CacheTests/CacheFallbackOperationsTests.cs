using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Cache;
using Microsoft.Identity.Core.Instance;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using Test.Microsoft.Identity.Core.Unit.Mocks;

namespace Test.Microsoft.Identity.Core.Unit.CacheTests
{
    [TestClass]
    public class CacheFallbackOperationsTests
    {
        private TokenCacheAccessor tokenCacheAccessor;
        private InMemoryLegacyCachePersistance legacyCachePersistance;

        [TestInitialize]
        public void TestInitialize()
        {
            // Methods in CacheFallbackOperations silently catch all exceptions and log them;
            // By setting this to null, logging will fail, making the test fail.
            CoreExceptionFactory.Instance = null; 
            CoreLoggerBase.Default = Substitute.For<CoreLoggerBase>();
            AadInstanceDiscovery.Instance.Cache.Clear();

            // Use the net45 accessor for tests
            tokenCacheAccessor = new TokenCacheAccessor();
            legacyCachePersistance = new InMemoryLegacyCachePersistance();
        }


        [TestMethod]
        public void GetAllAdalUsersForMsal_ScopedBy_ClientIdAndEnv()
        {
            // Arrange
            PopulateLegacyCache(legacyCachePersistance);

            // Act - query users by env and clientId
            Tuple<Dictionary<string, AdalUserInfo>, List<AdalUserInfo>> userTuple = CacheFallbackOperations.GetAllAdalUsersForMsal(
                legacyCachePersistance,
                new HashSet<string> { TestConstants.ProductionPrefNetworkEnvironment, "bogus" },
                TestConstants.ClientId);

            AssertByUsername(
                userTuple,
                expectedUsersWithClientInfo: new[] { "user1", "user2" },
                expectedUsersWithoutClientInfo: new[] { "no_client_info_user3", "no_client_info_user4" });

            // Act - query users for different clientId and env
            userTuple = CacheFallbackOperations.GetAllAdalUsersForMsal(
               legacyCachePersistance,
               new HashSet<string> { TestConstants.SovereignEnvironment },
               "other_client_id");

            // Assert
            AssertByUsername(
               userTuple,
               expectedUsersWithClientInfo: new[] { "user6" },
               expectedUsersWithoutClientInfo: Enumerable.Empty<string>());
        }

        [TestMethod]
        public void RemoveAdalUser_RemovesUserWithSameId()
        {
            // Arrange
            PopulateLegacyCache(legacyCachePersistance);

            PopulateLegacyWithRtAndId( //different clientId -> should not be deleted
               legacyCachePersistance,
               "other_client_id",
               TestConstants.ProductionPrefNetworkEnvironment,
               "uid1", "tenantId1", "user1_other_client_id");

            PopulateLegacyWithRtAndId( //different env -> should not be deleted
                legacyCachePersistance,
                TestConstants.ClientId,
                "other_env",
                "uid1", "tenantId1", "user1_other_env");

            // Act - delete with id and displayname
            CacheFallbackOperations.RemoveAdalUser(
                legacyCachePersistance,
                new HashSet<string> { TestConstants.ProductionPrefNetworkEnvironment },
                TestConstants.ClientId,
                "username_does_not_matter",
                "uid1.tenantId1");

            // Assert 
            Tuple<Dictionary<string, AdalUserInfo>, List<AdalUserInfo>> userTuple = CacheFallbackOperations.GetAllAdalUsersForMsal(
                legacyCachePersistance,
                new HashSet<string> { TestConstants.ProductionPrefNetworkEnvironment, "other_env" },
                TestConstants.ClientId);

            AssertByUsername(
                userTuple,
                expectedUsersWithClientInfo: new[] { "user2", "user1_other_env" },
                expectedUsersWithoutClientInfo: new[] { "no_client_info_user3", "no_client_info_user4" });


        }

        [TestMethod]
        public void RemoveAdalUser_RemovesUserNoClientInfo()
        {
            // Arrange
            PopulateLegacyCache(legacyCachePersistance);

            PopulateLegacyWithRtAndId(
                 legacyCachePersistance,
                 "other_client_id",
                 TestConstants.ProductionPrefNetworkEnvironment,
                 null,
                 null,
                 "no_client_info_user3"); // no client info, different client id -> won't be deleted

            PopulateLegacyWithRtAndId(
                legacyCachePersistance,
                TestConstants.ClientId,
                "other_env",
                null,
                null,
                "no_client_info_user3"); // no client info, different env -> won't be deleted

            AssertCacheEntryCount(8);

            Tuple<Dictionary<string, AdalUserInfo>, List<AdalUserInfo>> userTuple = CacheFallbackOperations.GetAllAdalUsersForMsal(
                legacyCachePersistance,
                new HashSet<string> { TestConstants.ProductionPrefNetworkEnvironment, "other_env" },
                TestConstants.ClientId);

            AssertByUsername(
                userTuple,
                expectedUsersWithClientInfo: new[] { "user2", "user1" },
                expectedUsersWithoutClientInfo: new[] { "no_client_info_user3", "no_client_info_user3", "no_client_info_user4" });

            // Act - delete with no client info -> displayable id is used
            CacheFallbackOperations.RemoveAdalUser(
                legacyCachePersistance,
                new HashSet<string> { TestConstants.ProductionPrefNetworkEnvironment },
                TestConstants.ClientId,
                "no_client_info_user3",
                "");

            AssertCacheEntryCount(7);

            // Assert 
            userTuple = CacheFallbackOperations.GetAllAdalUsersForMsal(
                legacyCachePersistance,
                new HashSet<string> { TestConstants.ProductionPrefNetworkEnvironment, "other_env" },
                TestConstants.ClientId);

            AssertByUsername(
                userTuple,
                expectedUsersWithClientInfo: new[] { "user2", "user1" },
                expectedUsersWithoutClientInfo: new[] { "no_client_info_user3", "no_client_info_user4" });

        }

        private void AssertCacheEntryCount(int expectedEntryCount)
        {
            IDictionary<AdalTokenCacheKey, AdalResultWrapper> cache =
                AdalCacheOperations.Deserialize(legacyCachePersistance.LoadCache());
            Assert.AreEqual(expectedEntryCount, cache.Count);
        }

        [TestMethod]
        public void RemoveAdalUser_RemovesUserNoClientInfo_And_NoDisplayName()
        {
            // Arrange
            PopulateLegacyCache(legacyCachePersistance);
            IDictionary<AdalTokenCacheKey, AdalResultWrapper> adalCacheBeforeDelete =
                AdalCacheOperations.Deserialize(legacyCachePersistance.LoadCache());
            Assert.AreEqual(6, adalCacheBeforeDelete.Count);

            // Act - nothing happens and a message is logged
            CacheFallbackOperations.RemoveAdalUser(
                legacyCachePersistance,
                new HashSet<string> { TestConstants.ProductionPrefNetworkEnvironment },
                TestConstants.ClientId,
                "",
                "");

            // Assert 
            AssertCacheEntryCount(6);

            CoreLoggerBase.Default.Received().Error(
                Arg.Is<string>(CoreErrorMessages.InternalErrorCacheEmptyUsername));
        }

        [TestMethod]
        public void WriteAdalRefreshToken_ErrorLog()
        {
            // Arrange
            legacyCachePersistance.ThrowOnWrite = true;
            CoreExceptionFactory.Instance = new TestExceptionFactory(); 

            MsalRefreshTokenCacheItem rtItem = new MsalRefreshTokenCacheItem(
              TestConstants.ProductionPrefNetworkEnvironment,
              TestConstants.ClientId,
              "someRT",
              MockHelpers.CreateClientInfo("u1", "ut1"));

            MsalIdTokenCacheItem idTokenCacheItem = new MsalIdTokenCacheItem(
               TestConstants.ProductionPrefCacheEnvironment, // different env
               TestConstants.ClientId,
               MockHelpers.CreateIdToken("u1", "username"),
               MockHelpers.CreateClientInfo("u1", "ut1"),
               "ut1");

            // Act
            CacheFallbackOperations.WriteAdalRefreshToken(
                legacyCachePersistance,
                rtItem,
                idTokenCacheItem,
                "https://some_env.com/common", // yet another env
                "uid",
                "scope1");

            // Assert
            CoreLoggerBase.Default.Received().Error(
                Arg.Is<string>(CacheFallbackOperations.DifferentAuthorityError));

            CoreLoggerBase.Default.Received().Error(
                Arg.Is<string>(CacheFallbackOperations.DifferentEnvError));
        }


        private static void PopulateLegacyCache(
            ILegacyCachePersistance legacyCachePersistance)
        {
            PopulateLegacyWithRtAndId(
                legacyCachePersistance,
                TestConstants.ClientId,
                TestConstants.ProductionPrefNetworkEnvironment,
                "uid1", "tenantId1", "user1");

            PopulateLegacyWithRtAndId(
                legacyCachePersistance,
                TestConstants.ClientId,
                TestConstants.ProductionPrefNetworkEnvironment,
                "uid2", "tenantId2", "user2");

            PopulateLegacyWithRtAndId(
                legacyCachePersistance,
                TestConstants.ClientId,
                TestConstants.ProductionPrefNetworkEnvironment,
                null, null, "no_client_info_user3");

            PopulateLegacyWithRtAndId(
               legacyCachePersistance,
               TestConstants.ClientId,
               TestConstants.ProductionPrefNetworkEnvironment,
               null, null, "no_client_info_user4");

            PopulateLegacyWithRtAndId(
               legacyCachePersistance,
               TestConstants.ClientId,
               TestConstants.SovereignEnvironment, // different env
               "uid4", "tenantId4", "sovereign_user5");

            PopulateLegacyWithRtAndId(
                legacyCachePersistance,
                "other_client_id", // different client id
                TestConstants.SovereignEnvironment,
                "uid5", "tenantId5", "user6");
        }

        private static void AssertUsersByDisplayName(
            IEnumerable<string> expectedUsernames,
            IEnumerable<AdalUserInfo> adalUserInfos,
            string errorMessage = "")
        {
            var actualUsernames = adalUserInfos.Select(x => x.DisplayableId).ToArray();

            CollectionAssert.AreEquivalent(
               expectedUsernames.ToArray(),
               actualUsernames,
               errorMessage);
        }

        private static void PopulateLegacyWithRtAndId(
            ILegacyCachePersistance legacyCachePersistance,
            string clientId,
            string env,
            string uid,
            string uniqueTenantId,
            string username)
        {
            string clientInfoString;
            if (String.IsNullOrEmpty(uid) || String.IsNullOrEmpty(uniqueTenantId))
            {
                clientInfoString = null;
            }
            else
            {
                clientInfoString = MockHelpers.CreateClientInfo(uid, uniqueTenantId);
            }

            MsalRefreshTokenCacheItem rtItem = new MsalRefreshTokenCacheItem(
                env,
                clientId,
                "someRT",
                clientInfoString);

            MsalIdTokenCacheItem idTokenCacheItem = new MsalIdTokenCacheItem(
               env,
               clientId,
               MockHelpers.CreateIdToken(uid, username),
               clientInfoString,
               uniqueTenantId);

            CacheFallbackOperations.WriteAdalRefreshToken(
                legacyCachePersistance,
                rtItem,
                idTokenCacheItem,
                "https://" + env + "/common",
                "uid",
                "scope1");
        }


        private static void AssertByUsername(
            Tuple<Dictionary<string, AdalUserInfo>, List<AdalUserInfo>> userTuple,
            IEnumerable<string> expectedUsersWithClientInfo,
            IEnumerable<string> expectedUsersWithoutClientInfo)
        {
            // Assert
            var usersWithClientInfo = userTuple.Item1.Values;
            var usersWithoutClientInfo = userTuple.Item2;

            AssertUsersByDisplayName(expectedUsersWithClientInfo, usersWithClientInfo,
                "Expecting only user1 and user2 because the other users either have no ClientInfo or have a different env or clientid");
            AssertUsersByDisplayName(expectedUsersWithoutClientInfo, usersWithoutClientInfo);
        }

    }

    public class InMemoryLegacyCachePersistance : ILegacyCachePersistance
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
