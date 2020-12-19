// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Test.Unit;

namespace Microsoft.Identity.Test.Common.Core.Mocks
{
    internal class LegacyTokenCacheHelper
    {
        internal static void PopulateLegacyCache(ICoreLogger logger, ILegacyCachePersistence legacyCachePersistence, int tokenQuantity = 1)
        {
            for (int i = 1; i <= tokenQuantity; i++)
            {
                PopulateLegacyWithRtAndId(
                    logger,
                    legacyCachePersistence,
                    TestConstants.ClientId,
                    TestConstants.ProductionPrefNetworkEnvironment,
                    TestConstants.Uid,
                    TestConstants.Utid,
                    $"{i}{TestConstants.DisplayableId}");
            }
        }

        internal static void PopulateLegacyCache(ICoreLogger logger, ILegacyCachePersistence legacyCachePersistence)
        {
            PopulateLegacyWithRtAndId(
                logger,
                legacyCachePersistence,
                TestConstants.ClientId,
                TestConstants.ProductionPrefNetworkEnvironment,
                "uid1",
                "tenantId1",
                "user1");

            PopulateLegacyWithRtAndId(
                logger,
                legacyCachePersistence,
                TestConstants.ClientId,
                TestConstants.ProductionPrefNetworkEnvironment,
                "uid2",
                "tenantId2",
                "user2");

            PopulateLegacyWithRtAndId(
                logger,
                legacyCachePersistence,
                TestConstants.ClientId,
                TestConstants.ProductionPrefNetworkEnvironment,
                null,
                null,
                "no_client_info_user3");

            PopulateLegacyWithRtAndId(
                logger,
                legacyCachePersistence,
                TestConstants.ClientId,
                TestConstants.ProductionPrefNetworkEnvironment,
                null,
                null,
                "no_client_info_user4");

            PopulateLegacyWithRtAndId(
                logger,
                legacyCachePersistence,
                TestConstants.ClientId,
                TestConstants.SovereignNetworkEnvironment, // different env
                "uid4",
                "tenantId4",
                "sovereign_user5");

            PopulateLegacyWithRtAndId(
                logger,
                legacyCachePersistence,
                "other_client_id", // different client id
                TestConstants.SovereignNetworkEnvironment,
                "uid5",
                "tenantId5",
                "user6");
        }

        internal static void PopulateLegacyWithRtAndId(
            ICoreLogger logger,
            ILegacyCachePersistence legacyCachePersistence,
            string clientId,
            string env,
            string uid,
            string uniqueTenantId,
            string username)
        {
            PopulateLegacyWithRtAndId(logger, legacyCachePersistence, clientId, env, uid, uniqueTenantId, username, "scope1");
        }

        internal static void PopulateLegacyWithRtAndId(
            ICoreLogger logger,
            ILegacyCachePersistence legacyCachePersistence,
            string clientId,
            string env,
            string uid,
            string uniqueTenantId,
            string username,
            string scope)
        {
            string clientInfoString;
            string homeAccountId;
            if (string.IsNullOrEmpty(uid) || string.IsNullOrEmpty(uniqueTenantId))
            {
                clientInfoString = null;
                homeAccountId = null;
            }
            else
            {
                clientInfoString = MockHelpers.CreateClientInfo(uid, uniqueTenantId);
                homeAccountId = ClientInfo.CreateFromJson(clientInfoString).ToAccountIdentifier();
            }

            var rtItem = new MsalRefreshTokenCacheItem(env, clientId, "someRT", clientInfoString, null, homeAccountId);

            var idTokenCacheItem = new MsalIdTokenCacheItem(
                env,
                clientId,
                MockHelpers.CreateIdToken(uid, username),
                clientInfoString,
                homeAccountId,
                tenantId: uniqueTenantId);

            CacheFallbackOperations.WriteAdalRefreshToken(
                logger,
                legacyCachePersistence,
                rtItem,
                idTokenCacheItem,
                "https://" + env + "/common",
                uid,
                scope);
        }
    }
}
