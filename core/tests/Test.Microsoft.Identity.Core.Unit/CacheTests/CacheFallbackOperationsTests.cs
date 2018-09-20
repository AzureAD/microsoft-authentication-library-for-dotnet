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
        private ILegacyCachePersistance legacyCachePersistance;

        [TestInitialize]
        public void TestInitialize()
        {
            // do not initialize CoreExceptionFactory because all cache operations are silent
            // so the tests would at least fail with null reference when the logic throws
            CoreLoggerBase.Default = Substitute.For<CoreLoggerBase>();
            AadInstanceDiscovery.Instance.Cache.Clear();

            // Use the net45 accessor for tests
            tokenCacheAccessor = new TokenCacheAccessor();
            legacyCachePersistance = new InMemoryLegacyCachePersistance();
        }


        [TestMethod]
        public void GetAllAdalUsersForMsal_ScopedBy_ClientId_And_Env()
        {
            // Arrange
            PopulateLegacyCache(legacyCachePersistance);

            // Act - query users by env and clientId
            Tuple<Dictionary<string, AdalUserInfo>, List<AdalUserInfo>> userTuple = CacheFallbackOperations.GetAllAdalUsersForMsal(
                legacyCachePersistance,
                new HashSet<string> { TestConstants.ProductionPrefNetworkEnvironment, "bogus" },
                TestConstants.ClientId);

            // Assert
            var usersWithClientInfo = userTuple.Item1.Values;
            var usersWithoutClientInfo = userTuple.Item2;

            AssertUsersByDisplayName(new[] { "user1", "user2" }, usersWithClientInfo,
                "Expecting only user1 and user2 because the other users either have no ClientInfo or have a different env or clientid");
            AssertUsersByDisplayName(new[] { "no_client_info_user3", "no_client_info_user4" }, usersWithoutClientInfo);

            // Act - query users for different clientId and env
            userTuple = CacheFallbackOperations.GetAllAdalUsersForMsal(
               legacyCachePersistance,
               new HashSet<string> { TestConstants.SovereignEnvironment },
               "other_client_id");

            // Assert
            usersWithClientInfo = userTuple.Item1.Values;
            usersWithoutClientInfo = userTuple.Item2;

            AssertUsersByDisplayName(new[] { "user6" }, usersWithClientInfo);
            AssertUsersByDisplayName(Enumerable.Empty<string>(), usersWithoutClientInfo);
        }

        [TestMethod]
        public void RemoveAdalUser_RemovesUserWithSameId()
        {
            // Arrange
            PopulateLegacyCache(legacyCachePersistance);

            PopulateLegacyWithRtAndId( //same id, different clientId -> should be deleted
               legacyCachePersistance,
               "other_client_id",
               TestConstants.ProductionPrefNetworkEnvironment,
               "uid1", "tenantId1", "user1_other_client_id");

            PopulateLegacyWithRtAndId( // same id, different env -> should not be deleted
                legacyCachePersistance,
                TestConstants.ClientId,
                "other_env",
                "uid1", "tenantId1", "user1_other_env");

            // Act - delete with id and displayname
            CacheFallbackOperations.RemoveAdalUser(
                legacyCachePersistance,
                new HashSet<string> { TestConstants.ProductionPrefNetworkEnvironment }, // this should be ignored when deleting 
                "username_does_not_matter",
                "uid1.tenantId1");

            // Assert 
            Tuple<Dictionary<string, AdalUserInfo>, List<AdalUserInfo>> userTuple = CacheFallbackOperations.GetAllAdalUsersForMsal(
                legacyCachePersistance,
                new HashSet<string> { TestConstants.ProductionPrefNetworkEnvironment, "other_env" },
                TestConstants.ClientId);

            var usersWithClientInfo = userTuple.Item1.Values;
            var usersWithoutClientInfo = userTuple.Item2;

            AssertUsersByDisplayName(new[] { "user2", "user1_other_env" }, usersWithClientInfo, "user2 should have been deleted");
            AssertUsersByDisplayName(new[] { "no_client_info_user3", "no_client_info_user4" }, usersWithoutClientInfo);

        }

        [TestMethod]
        public void RemoveAdalUser_RemovesUserNoClientInfo()
        {
            // Arrange
            PopulateLegacyCache(legacyCachePersistance);

            // ClientInfo is null -> we cannot differentiate between these entries so they are overwritten
            PopulateLegacyWithRtAndId(
                 legacyCachePersistance,
                 "other_client_id",
                 TestConstants.ProductionPrefNetworkEnvironment,
                 null,
                 null,
                 "no_client_info_user3"); // no client info, different client id 

            PopulateLegacyWithRtAndId(
                legacyCachePersistance,
                "yet_another_client_id",
                "other_env",
                null,
                null,
                "no_client_info_user3"); // no client info, different env 

            Tuple<Dictionary<string, AdalUserInfo>, List<AdalUserInfo>> userTuple = CacheFallbackOperations.GetAllAdalUsersForMsal(
                legacyCachePersistance,
                new HashSet<string> { TestConstants.ProductionPrefNetworkEnvironment, "other_env" },
                TestConstants.ClientId);

            var usersWithoutClientInfo = userTuple.Item2;
            AssertUsersByDisplayName(new[] { "no_client_info_user3", "no_client_info_user4" }, usersWithoutClientInfo);

            Assert.AreEqual(2, usersWithoutClientInfo.Count);

            // Act - delete with no client info -> displayable id is used
            CacheFallbackOperations.RemoveAdalUser(
                legacyCachePersistance,
                new HashSet<string> { TestConstants.ProductionPrefNetworkEnvironment },
                "no_client_info_user3",
                "");

            // Assert 
            userTuple = CacheFallbackOperations.GetAllAdalUsersForMsal(
                legacyCachePersistance,
                new HashSet<string> { TestConstants.ProductionPrefNetworkEnvironment, "other_env" },
                TestConstants.ClientId);

            usersWithoutClientInfo = userTuple.Item2;
            usersWithoutClientInfo = userTuple.Item2;
            AssertUsersByDisplayName(new[] {"no_client_info_user4" }, usersWithoutClientInfo);

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
                "",
                "");

            // Assert 
            IDictionary<AdalTokenCacheKey, AdalResultWrapper> adalCacheAfterDelete =
                AdalCacheOperations.Deserialize(legacyCachePersistance.LoadCache());
            Assert.AreEqual(6, adalCacheBeforeDelete.Count);

            CoreLoggerBase.Default.Received().Error(
                Arg.Is<string>(CoreErrorMessages.InternalErrorCacheEmptyUsername));

            CoreLoggerBase.Default.Received().ErrorPii(
                Arg.Is<string>(CoreErrorMessages.InternalErrorCacheEmptyUsername));

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
            var clientInfoString = MockHelpers.CreateClientInfo(uid, uniqueTenantId);

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
    }

    class InMemoryLegacyCachePersistance : ILegacyCachePersistance
    {
        private byte[] data;
        public byte[] LoadCache()
        {
            return data;
        }

        public void WriteCache(byte[] serializedCache)
        {
            data = serializedCache;
        }
    }
}
