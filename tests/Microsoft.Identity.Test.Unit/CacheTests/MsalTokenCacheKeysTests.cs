// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Cache.Keys;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CacheTests
{
    [TestClass]
    public class MsalTokenCacheKeysTests : TestBase
    {
        [TestMethod]
        public void MsalAccessTokenCacheKey()
        {
            var tokenResponse = new MsalTokenResponse();
            tokenResponse.Scope = "user.read user.write";
            tokenResponse.TokenType = "bearer";

            var item = new MsalAccessTokenCacheItem("login.microsoftonline.com", "clientId", tokenResponse, "contoso.com", "uid.utid");
            var iOSKey = item.iOSCacheKey;
            Assert.AreEqual("uid.utid-login.microsoftonline.com-accesstoken-clientid-contoso.com-user.read user.write", item.CacheKey);

            Assert.AreEqual("uid.utid-login.microsoftonline.com", iOSKey.iOSAccount);
            Assert.AreEqual("accesstoken-clientid-contoso.com-user.read user.write", iOSKey.iOSService);
            Assert.AreEqual("accesstoken-clientid-contoso.com", iOSKey.iOSGeneric);
            Assert.AreEqual((int)MsalCacheKeys.iOSCredentialAttrType.AccessToken, iOSKey.iOSType);
        }

            [TestMethod]
        public void MsalPOPAccessTokenCacheKey()
        {
            var tokenResponse = new MsalTokenResponse();
            tokenResponse.Scope = "user.read user.write";
            tokenResponse.TokenType = "pop";

            var item = new MsalAccessTokenCacheItem("login.microsoftonline.com", "clientId", tokenResponse, "contoso.com", "uid.utid");
            var iOSKey = item.iOSCacheKey;

            Assert.AreEqual("uid.utid-login.microsoftonline.com-accesstoken_with_authscheme-clientid-contoso.com-user.read user.write-pop", item.CacheKey);

            Assert.AreEqual("uid.utid-login.microsoftonline.com", iOSKey.iOSAccount);
            Assert.AreEqual("accesstoken_with_authscheme-clientid-contoso.com-user.read user.write-pop", iOSKey.iOSService);
            Assert.AreEqual("accesstoken_with_authscheme-clientid-contoso.com", iOSKey.iOSGeneric);
            Assert.AreEqual((int)MsalCacheKeys.iOSCredentialAttrType.AccessToken, iOSKey.iOSType);
        }

        [TestMethod]
        public void MsalRefreshTokenCacheKey()
        {
            var item = new MsalRefreshTokenCacheItem("login.microsoftonline.com", "clientid", "secret", "rawClientInfo", "", "uid.utid");
            IiOSKey iOSKey = item.iOSCacheKey;

            Assert.AreEqual("uid.utid-login.microsoftonline.com-refreshtoken-clientid--", item.CacheKey);

            Assert.AreEqual("uid.utid-login.microsoftonline.com", iOSKey.iOSAccount);
            Assert.AreEqual("refreshtoken-clientid--", iOSKey.iOSService);
            Assert.AreEqual("refreshtoken-clientid-", iOSKey.iOSGeneric);
            Assert.AreEqual((int)MsalCacheKeys.iOSCredentialAttrType.RefreshToken, iOSKey.iOSType);
        }

        [TestMethod]
        public void MsalFamilyRefreshTokenCacheKey()
        {
            var item = new MsalRefreshTokenCacheItem("login.microsoftonline.com", "CLIENT_ID_NOT_USED", "secret", "rawClientInfo", "1", "uid.utid");
            IiOSKey iOSKey = item.iOSCacheKey;

            Assert.AreEqual("uid.utid-login.microsoftonline.com-refreshtoken-1--", item.CacheKey);

            Assert.AreEqual("uid.utid-login.microsoftonline.com", iOSKey.iOSAccount);
            Assert.AreEqual("refreshtoken-1--", iOSKey.iOSService);
            Assert.AreEqual("refreshtoken-1-", iOSKey.iOSGeneric);
            Assert.AreEqual((int)MsalCacheKeys.iOSCredentialAttrType.RefreshToken, iOSKey.iOSType);
        }

        [TestMethod]
        public void MsalIdTokenCacheKey()
        {
            var item = new MsalIdTokenCacheItem("login.microsoftonline.com", "clientid", "secret", "rawClientInfo", "uid.utid", "contoso.com");
            IiOSKey iOSKey = item.iOSCacheKey;

            Assert.AreEqual("uid.utid-login.microsoftonline.com-idtoken-clientid-contoso.com-", item.CacheKey);

            Assert.AreEqual("uid.utid-login.microsoftonline.com", iOSKey.iOSAccount);
            Assert.AreEqual("idtoken-clientid-contoso.com-", iOSKey.iOSService);
            Assert.AreEqual("idtoken-clientid-contoso.com", iOSKey.iOSGeneric);
            Assert.AreEqual((int)MsalCacheKeys.iOSCredentialAttrType.IdToken, iOSKey.iOSType);
        }

        [TestMethod]
        public void MsalAccountCacheKey()
        {
            var item = new MsalAccountCacheItem(
                "login.microsoftonline.com",
                "contoso.com",
                "uid.utid",
                null,
                "localId");

            var iOSKey = item.iOSCacheKey;

            Assert.AreEqual("uid.utid-login.microsoftonline.com-contoso.com", item.CacheKey);

            Assert.AreEqual("uid.utid-login.microsoftonline.com", iOSKey.iOSAccount);
            Assert.AreEqual("contoso.com", iOSKey.iOSService);
            Assert.AreEqual("localid", iOSKey.iOSGeneric);
            Assert.AreEqual(MsalCacheKeys.iOSAuthorityTypeToAttrType["MSSTS"], iOSKey.iOSType);
        }

        [TestMethod]
        public void MsalAppMetadataCacheKey()
        {
            var item = new MsalAppMetadataCacheItem("clientid", "login.microsoftonline.com", null);
            IiOSKey iOSKey = item.iOSCacheKey;

            Assert.AreEqual("appmetadata-login.microsoftonline.com-clientid", item.CacheKey);

            Assert.AreEqual("appmetadata-clientid", iOSKey.iOSService);
            Assert.AreEqual("login.microsoftonline.com", iOSKey.iOSAccount);
            Assert.AreEqual("1", iOSKey.iOSGeneric);
            Assert.AreEqual((int)MsalCacheKeys.iOSCredentialAttrType.AppMetadata, iOSKey.iOSType);
        }

        [TestMethod]
        public void MsalRefreshTokenCacheKey_WithAdditionalCacheKeyComponents_DifferentiatesCacheKeys()
        {
            // Arrange - simulate two agents sharing a single CCA (same clientId = "blueprintId")
            // but with different AdditionalCacheKeyComponents to differentiate them.
            string sharedClientId = "blueprintId";
            string homeAccountId = "uid.utid";
            string env = "login.microsoftonline.com";

            var agent1Components = new SortedList<string, string>
            {
                { "agent_client_id", "agent-app-id-1" }
            };
            var agent2Components = new SortedList<string, string>
            {
                { "agent_client_id", "agent-app-id-2" }
            };

            // Act
            var rtAgent1 = new MsalRefreshTokenCacheItem(env, sharedClientId, "rt-secret-1", "rawClientInfo", "", homeAccountId,
                cacheKeyComponents: agent1Components);
            var rtAgent2 = new MsalRefreshTokenCacheItem(env, sharedClientId, "rt-secret-2", "rawClientInfo", "", homeAccountId,
                cacheKeyComponents: agent2Components);
            var rtNoComponents = new MsalRefreshTokenCacheItem(env, sharedClientId, "rt-secret-3", "rawClientInfo", "", homeAccountId);

            // Assert - cache keys should be different when components differ
            Assert.AreNotEqual(rtAgent1.CacheKey, rtAgent2.CacheKey,
                "RTs with different AdditionalCacheKeyComponents should have different cache keys");
            Assert.AreNotEqual(rtAgent1.CacheKey, rtNoComponents.CacheKey,
                "RT with components should differ from RT without components");

            // Both should still contain the standard parts
            Assert.Contains("blueprintid", rtAgent1.CacheKey);
            // When components are present, the credential type is RTExt
            Assert.Contains("rtext", rtAgent1.CacheKey);
            // When components are absent, the credential type is RefreshToken
            Assert.Contains("refreshtoken", rtNoComponents.CacheKey);

            // The AdditionalCacheKeyComponents should be persisted
            Assert.IsNotNull(rtAgent1.AdditionalCacheKeyComponents);
            Assert.AreEqual("agent-app-id-1", rtAgent1.AdditionalCacheKeyComponents["agent_client_id"]);
            Assert.IsNull(rtNoComponents.AdditionalCacheKeyComponents);
        }

        [TestMethod]
        public void MsalRefreshTokenCacheKey_WithoutAdditionalCacheKeyComponents_BackwardCompatible()
        {
            // Verify that RTs without AdditionalCacheKeyComponents produce the same cache key as before
            var item = new MsalRefreshTokenCacheItem("login.microsoftonline.com", "clientid", "secret", "rawClientInfo", "", "uid.utid");

            Assert.AreEqual("uid.utid-login.microsoftonline.com-refreshtoken-clientid--", item.CacheKey);
            Assert.IsNull(item.AdditionalCacheKeyComponents);
        }

        [TestMethod]
        public void MsalRefreshTokenCacheKey_Serialization_PreservesAdditionalCacheKeyComponents()
        {
            // Arrange
            var components = new SortedList<string, string>
            {
                { "agent_client_id", "agent-app-id-1" },
                { "extra_key", "extra_value" }
            };

            var original = new MsalRefreshTokenCacheItem("login.microsoftonline.com", "clientid", "secret", "rawClientInfo", "", "uid.utid",
                cacheKeyComponents: components);

            // Act - serialize then deserialize
            string json = original.ToJsonString();
            var deserialized = MsalRefreshTokenCacheItem.FromJsonString(json);

            // Assert
            Assert.AreEqual(original.CacheKey, deserialized.CacheKey);
            Assert.IsNotNull(deserialized.AdditionalCacheKeyComponents);
            Assert.HasCount(2, deserialized.AdditionalCacheKeyComponents);
            Assert.AreEqual("agent-app-id-1", deserialized.AdditionalCacheKeyComponents["agent_client_id"]);
            Assert.AreEqual("extra_value", deserialized.AdditionalCacheKeyComponents["extra_key"]);
        }

        [TestMethod]
        public void MsalRefreshTokenCacheKey_Serialization_BackwardCompatible_NoComponents()
        {
            // RTs without components should serialize/deserialize identically to the old format
            var original = new MsalRefreshTokenCacheItem("login.microsoftonline.com", "clientid", "secret", "rawClientInfo", "", "uid.utid");

            string json = original.ToJsonString();
            var deserialized = MsalRefreshTokenCacheItem.FromJsonString(json);

            Assert.AreEqual(original.CacheKey, deserialized.CacheKey);
            Assert.IsNull(deserialized.AdditionalCacheKeyComponents);
            Assert.AreEqual("uid.utid-login.microsoftonline.com-refreshtoken-clientid--", deserialized.CacheKey);
        }

        [TestMethod]
        public void MsalAccessTokenCacheKey_WithAdditionalCacheKeyComponents()
        {
            // Verify AT behavior with AdditionalCacheKeyComponents as a reference for RT parity
            var tokenResponse = new MsalTokenResponse();
            tokenResponse.Scope = "user.read";
            tokenResponse.TokenType = "bearer";

            var components = new SortedList<string, string>
            {
                { "agent_client_id", "agent-app-id-1" }
            };

            var atWithComponents = new MsalAccessTokenCacheItem(
                "login.microsoftonline.com", "clientId", tokenResponse, "contoso.com", "uid.utid",
                cacheKeyComponents: components);
            var atWithout = new MsalAccessTokenCacheItem(
                "login.microsoftonline.com", "clientId", tokenResponse, "contoso.com", "uid.utid");

            // AT already differentiates - verify
            Assert.AreNotEqual(atWithComponents.CacheKey, atWithout.CacheKey);
            Assert.IsNotNull(atWithComponents.AdditionalCacheKeyComponents);
        }
    }
}
