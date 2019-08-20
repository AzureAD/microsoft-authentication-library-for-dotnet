// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Unit;

namespace Microsoft.Identity.Test.Common.Core.Mocks
{
    internal class TokenCacheHelper
    {
        public static long ValidExpiresIn = 28800;
        public static long ValidExtendedExpiresIn = 57600;

        internal void PopulateCacheForClientCredential(ITokenCacheAccessor accessor)
        {
            MsalAccessTokenCacheItem atItem = new MsalAccessTokenCacheItem(
               TestConstants.ProductionPrefCacheEnvironment,
               TestConstants.ClientId,
               TestConstants.s_scope.AsSingleString(),
               TestConstants.Utid,
               "",
               new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)),
               new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExtendedExpiresIn)),
               MockHelpers.CreateClientInfo());

            accessor.SaveAccessToken(atItem);
        }

        internal void PopulateCache(
            ITokenCacheAccessor accessor,
            string uid = TestConstants.Uid,
            string utid = TestConstants.Utid,
            string clientId = TestConstants.ClientId,
            string environment = TestConstants.ProductionPrefCacheEnvironment,
            string displayableId = TestConstants.DisplayableId,
            string rtSecret = TestConstants.RTSecret,
            bool expiredAccessTokens = false, 
            bool addSecondAt = true)
        {
            var accessTokenExpiresOn = expiredAccessTokens ?
                new DateTimeOffset(DateTime.UtcNow) : 
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn));

            var extendedAccessTokenExpiresOn = expiredAccessTokens ?
                new DateTimeOffset(DateTime.UtcNow) :
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExtendedExpiresIn));

            MsalAccessTokenCacheItem atItem = new MsalAccessTokenCacheItem(
                environment,
                clientId,
                TestConstants.s_scope.AsSingleString(),
                utid,
                "",
                accessTokenExpiresOn,
                extendedAccessTokenExpiresOn,
                MockHelpers.CreateClientInfo(uid, utid));

            // add access token
            accessor.SaveAccessToken(atItem);

            var idTokenCacheItem = new MsalIdTokenCacheItem(
                environment,
                clientId,
                MockHelpers.CreateIdToken(TestConstants.UniqueId + "more", displayableId),
                MockHelpers.CreateClientInfo(uid, utid),
                utid);

            accessor.SaveIdToken(idTokenCacheItem);

            // add another access token
            if (addSecondAt)
            {
                atItem = new MsalAccessTokenCacheItem(
                  environment,
                  clientId,
                  TestConstants.s_scopeForAnotherResource.AsSingleString(),
                  utid,
                  "",
                  accessTokenExpiresOn,
                  extendedAccessTokenExpiresOn,
                  MockHelpers.CreateClientInfo(uid, utid));

                accessor.SaveAccessToken(atItem);
            }

            var accountCacheItem = new MsalAccountCacheItem(
                environment,
                null,
                MockHelpers.CreateClientInfo(uid, utid),
                null,
                displayableId,
                utid,
                null,
                null);

            accessor.SaveAccount(accountCacheItem);

            AddRefreshTokenToCache(accessor, uid, utid, clientId, environment, rtSecret);

            var appMetadataItem = new MsalAppMetadataCacheItem(
                clientId,
                environment,
                null);

            accessor.SaveAppMetadata(appMetadataItem);
        }

        internal void PopulateCacheWithOneAccessToken(ITokenCacheAccessor accessor)
        {
            MsalAccessTokenCacheItem atItem = new MsalAccessTokenCacheItem(
               TestConstants.ProductionPrefCacheEnvironment,
               TestConstants.ClientId,
               TestConstants.s_scope.AsSingleString(),
               TestConstants.Utid,
               "",
               new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)),
               new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExtendedExpiresIn)),
               MockHelpers.CreateClientInfo());

            // add access token
            accessor.SaveAccessToken(atItem);

            MsalIdTokenCacheItem idTokenCacheItem = new MsalIdTokenCacheItem(
                TestConstants.ProductionPrefCacheEnvironment, TestConstants.ClientId,
                MockHelpers.CreateIdToken(TestConstants.UniqueId + "more", TestConstants.DisplayableId),
                MockHelpers.CreateClientInfo(), TestConstants.Utid);

            accessor.SaveIdToken(idTokenCacheItem);

            MsalAccountCacheItem accountCacheItem = new MsalAccountCacheItem
                (TestConstants.ProductionPrefNetworkEnvironment, null, MockHelpers.CreateClientInfo(), null, null, TestConstants.Utid,
                null, null);

            accessor.SaveAccount(accountCacheItem);

            atItem = new MsalAccessTokenCacheItem(
                TestConstants.ProductionPrefCacheEnvironment,
                TestConstants.ClientId,
                TestConstants.s_scopeForAnotherResource.AsSingleString(),
                TestConstants.Utid,
                "",
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)),
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExtendedExpiresIn)),
                MockHelpers.CreateClientInfo());

            AddRefreshTokenToCache(accessor, TestConstants.Uid, TestConstants.Utid);
        }

        public static void AddRefreshTokenToCache(
            ITokenCacheAccessor accessor,
            string uid,
            string utid,
            string clientId = TestConstants.ClientId,
            string environment = TestConstants.ProductionPrefCacheEnvironment, 
            string rtSecret = TestConstants.RTSecret)
        {
            var rtItem = new MsalRefreshTokenCacheItem
                (environment, clientId, rtSecret, MockHelpers.CreateClientInfo(uid, utid));

            accessor.SaveRefreshToken(rtItem);
        }

        public static void AddIdTokenToCache(ITokenCacheAccessor accessor, string uid, string utid)
        {
            MsalIdTokenCacheItem idTokenCacheItem = new MsalIdTokenCacheItem(
                TestConstants.ProductionPrefCacheEnvironment,
                TestConstants.ClientId,
                MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId),
                MockHelpers.CreateClientInfo(),
                TestConstants.Utid);

            accessor.SaveIdToken(idTokenCacheItem);
        }

        public static void AddAccountToCache(ITokenCacheAccessor accessor, string uid, string utid)
        {
            MsalAccountCacheItem accountCacheItem = new MsalAccountCacheItem
                (TestConstants.ProductionPrefCacheEnvironment, null, MockHelpers.CreateClientInfo(uid, utid), null, null, utid, null, null);

            accessor.SaveAccount(accountCacheItem);
        }
    }
}
