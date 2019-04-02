// ------------------------------------------------------------------------------
// 
// Copyright (c) Microsoft Corporation.
// All rights reserved.
// 
// This code is licensed under the MIT License.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Exceptions;
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
                    MsalTestConstants.ClientId,
                    MsalTestConstants.ProductionPrefNetworkEnvironment);

            AssertByUsername(
                adalUsers,
                new[]
                {
                    "user1",
                    "user2",
                },
                new[]
                {
                    "no_client_info_user3",
                    "no_client_info_user4"
                });

            // Act - query users for different clientId and env
            adalUsers = CacheFallbackOperations.GetAllAdalUsersForMsal(
                _logger,
                _legacyCachePersistence,
                "other_client_id",
                MsalTestConstants.SovereignNetworkEnvironment);

            // Assert
            AssertByUsername(
                adalUsers,
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
                MsalTestConstants.ProductionPrefNetworkEnvironment,
                "uid1",
                "tenantId1",
                "user1_other_client_id");

            PopulateLegacyWithRtAndId( // different env -> should be deleted
                _legacyCachePersistence,
                MsalTestConstants.ClientId,
                "other_env",
                "uid1",
                "tenantId1",
                "user1_other_env");

            // Act - delete with id and displayname
            CacheFallbackOperations.RemoveAdalUser(
                _logger,
                _legacyCachePersistence,
                MsalTestConstants.ClientId,
                "username_does_not_matter",
                "uid1.tenantId1");

            // Assert 
            var adalUsers =
                CacheFallbackOperations.GetAllAdalUsersForMsal(
                    _logger,
                    _legacyCachePersistence,
                    MsalTestConstants.ClientId,
                    MsalTestConstants.ProductionPrefNetworkEnvironment);

            AssertByUsername(
                adalUsers,
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
                MsalTestConstants.ProductionPrefNetworkEnvironment,
                null,
                null,
                "no_client_info_user3"); // no client info, different client id -> won't be deleted

            PopulateLegacyWithRtAndId(
                _legacyCachePersistence,
                MsalTestConstants.ClientId,
                "other_env",
                null,
                null,
                "no_client_info_user3"); // no client info, different env -> won't be deleted

            AssertCacheEntryCount(8);

            var adalUsers =
                CacheFallbackOperations.GetAllAdalUsersForMsal(
                    _logger,
                    _legacyCachePersistence,
                    MsalTestConstants.ClientId,
                    MsalTestConstants.ProductionPrefNetworkEnvironment);

            AssertByUsername(
                adalUsers,
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
                MsalTestConstants.ClientId,
                "no_client_info_user3",
                "");

            AssertCacheEntryCount(6);

            // Assert 
            adalUsers = CacheFallbackOperations.GetAllAdalUsersForMsal(
                _logger,
                _legacyCachePersistence,
                MsalTestConstants.ClientId ,
                MsalTestConstants.ProductionPrefNetworkEnvironment);

            AssertByUsername(
                adalUsers,
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
                MsalTestConstants.ClientId,
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
                MsalTestConstants.ClientId,
                MsalTestConstants.ProductionPrefNetworkEnvironment,
                MsalTestConstants.Uid,
                MsalTestConstants.Utid,
                MsalTestConstants.DisplayableId,
                MsalTestConstants.ScopeStr);

            AssertCacheEntryCount(1);

            PopulateLegacyWithRtAndId(
                _legacyCachePersistence,
                MsalTestConstants.ClientId,
                MsalTestConstants.ProductionPrefNetworkEnvironment,
                null,
                null,
                MsalTestConstants.DisplayableId,
                MsalTestConstants.ScopeForAnotherResourceStr);

            AssertCacheEntryCount(2);

            CacheFallbackOperations.RemoveAdalUser(
                _logger,
                _legacyCachePersistence,
                MsalTestConstants.ClientId,
                MsalTestConstants.DisplayableId,
                MsalTestConstants.Uid + "." + MsalTestConstants.Utid);

            AssertCacheEntryCount(0);
        }

        [TestMethod]
        public void WriteAdalRefreshToken_ErrorLog()
        {
            // Arrange
            _legacyCachePersistence.ThrowOnWrite = true;

            var rtItem = new MsalRefreshTokenCacheItem(
                MsalTestConstants.ProductionPrefNetworkEnvironment,
                MsalTestConstants.ClientId,
                "someRT",
                MockHelpers.CreateClientInfo("u1", "ut1"));

            var idTokenCacheItem = new MsalIdTokenCacheItem(
                MsalTestConstants.ProductionPrefCacheEnvironment, // different env
                MsalTestConstants.ClientId,
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
                MsalTestConstants.ProductionPrefNetworkEnvironment,
                MsalTestConstants.ClientId,
                "someRT",
                MockHelpers.CreateClientInfo("u1", "ut1"),
                "familyId");

            var idTokenCacheItem = new MsalIdTokenCacheItem(
                MsalTestConstants.ProductionPrefNetworkEnvironment, // different env
                MsalTestConstants.ClientId,
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
                MsalTestConstants.ClientId,
                MsalTestConstants.ProductionPrefNetworkEnvironment,
                "uid1",
                "tenantId1",
                "user1");

            PopulateLegacyWithRtAndId(
                legacyCachePersistence,
                MsalTestConstants.ClientId,
                MsalTestConstants.ProductionPrefNetworkEnvironment,
                "uid2",
                "tenantId2",
                "user2");

            PopulateLegacyWithRtAndId(
                legacyCachePersistence,
                MsalTestConstants.ClientId,
                MsalTestConstants.ProductionPrefNetworkEnvironment,
                null,
                null,
                "no_client_info_user3");

            PopulateLegacyWithRtAndId(
                legacyCachePersistence,
                MsalTestConstants.ClientId,
                MsalTestConstants.ProductionPrefNetworkEnvironment,
                null,
                null,
                "no_client_info_user4");

            PopulateLegacyWithRtAndId(
                legacyCachePersistence,
                MsalTestConstants.ClientId,
                MsalTestConstants.SovereignNetworkEnvironment, // different env
                "uid4",
                "tenantId4",
                "sovereign_user5");

            PopulateLegacyWithRtAndId(
                legacyCachePersistence,
                "other_client_id", // different client id
                MsalTestConstants.SovereignNetworkEnvironment,
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
            AdalUsersForMsalResult adalUsers,
            IEnumerable<string> expectedUsersWithClientInfo,
            IEnumerable<string> expectedUsersWithoutClientInfo)
        {
            // Assert
            var usersWithClientInfo = adalUsers.ClientInfoUsers.Values;
            List<AdalUserInfo> usersWithoutClientInfo = adalUsers.UsersWithoutClientInfo;

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
