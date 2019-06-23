// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Cache.Keys;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CoreTests.CacheTests
{
    [TestClass]
    public class MsalTokenCacheKeysTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
        }

        [TestMethod]
        public void ArgNull()
        {
            AssertException.Throws<ArgumentNullException>(() => new MsalRefreshTokenCacheKey("", "clientId", "uid", "1"));
            AssertException.Throws<ArgumentNullException>(() => new MsalRefreshTokenCacheKey(null, "clientId", "uid", "1"));
            AssertException.Throws<ArgumentNullException>(() => new MsalRefreshTokenCacheKey(null, "clientId", "uid", "1"));
            AssertException.Throws<ArgumentNullException>(() => new MsalRefreshTokenCacheKey("env", null, "uid", "1"));

            AssertException.Throws<ArgumentNullException>(() => new MsalIdTokenCacheKey("", "tid", "uid", "cid"));
            AssertException.Throws<ArgumentNullException>(() => new MsalIdTokenCacheKey(null, "tid", "uid", "cid"));
            AssertException.Throws<ArgumentNullException>(() => new MsalIdTokenCacheKey("env", "tid", "uid", ""));
            AssertException.Throws<ArgumentNullException>(() => new MsalIdTokenCacheKey("env", "tid", "uid", null));

            AssertException.Throws<ArgumentNullException>(() => new MsalAccessTokenCacheKey("", "tid", "uid", "cid", "scopes"));
            AssertException.Throws<ArgumentNullException>(() => new MsalAccessTokenCacheKey(null, "tid", "uid", "cid", "scopes"));
            AssertException.Throws<ArgumentNullException>(() => new MsalAccessTokenCacheKey("env", "tid", "uid", "", "scopes"));
            AssertException.Throws<ArgumentNullException>(() => new MsalAccessTokenCacheKey("env", "tid", "uid", null, "scopes"));

            AssertException.Throws<ArgumentNullException>(() => new MsalAccountCacheKey("", "tid", "uid", "localid", "aad"));
            AssertException.Throws<ArgumentNullException>(() => new MsalAccountCacheKey(null, "tid", "uid", "localid", "msa"));
        }

        [TestMethod]
        public void MsalAccessTokenCacheKey()
        {
            var key = new MsalAccessTokenCacheKey("login.microsoftonline.com", "contoso.com", "uid.utid", "clientid", "user.read user.write");

            Assert.AreEqual("uid.utid-login.microsoftonline.com-accesstoken-clientid-contoso.com-user.read user.write", key.ToString());

            Assert.AreEqual("uid.utid-login.microsoftonline.com", key.iOSAccount);
            Assert.AreEqual("accesstoken-clientid-contoso.com-user.read user.write", key.iOSService);
            Assert.AreEqual("accesstoken-clientid-contoso.com", key.iOSGeneric);
            Assert.AreEqual((int)MsalCacheKeys.iOSCredentialAttrType.AccessToken, key.iOSType);

            Assert.AreEqual(
                key,
                new MsalAccessTokenCacheKey("login.microsoftonline.com", "contoso.com", "uid.utid", "clientid", "user.read user.write"));
        }

        [TestMethod]
        public void MsalRefreshTokenCacheKey()
        {
            var key = new MsalRefreshTokenCacheKey("login.microsoftonline.com", "clientid", "uid.utid", "");

            Assert.AreEqual("uid.utid-login.microsoftonline.com-refreshtoken-clientid--", key.ToString());

            Assert.AreEqual("uid.utid-login.microsoftonline.com", key.iOSAccount);
            Assert.AreEqual("refreshtoken-clientid--", key.iOSService);
            Assert.AreEqual("refreshtoken-clientid-", key.iOSGeneric);
            Assert.AreEqual((int)MsalCacheKeys.iOSCredentialAttrType.RefreshToken, key.iOSType);

            Assert.AreEqual(
                key,
                new MsalRefreshTokenCacheKey("login.microsoftonline.com", "clientid", "uid.utid", ""));
        }

        [TestMethod]
        public void MsalFamilyRefreshTokenCacheKey()
        {
            var key = new MsalRefreshTokenCacheKey("login.microsoftonline.com", "CLIENT_ID_NOT_USED", "uid.utid", "1");

            Assert.AreEqual("uid.utid-login.microsoftonline.com-refreshtoken-1--", key.ToString());

            Assert.AreEqual("uid.utid-login.microsoftonline.com", key.iOSAccount);
            Assert.AreEqual("refreshtoken-1--", key.iOSService);
            Assert.AreEqual("refreshtoken-1-", key.iOSGeneric);
            Assert.AreEqual((int)MsalCacheKeys.iOSCredentialAttrType.RefreshToken, key.iOSType);

            Assert.AreEqual(
                key,
                new MsalRefreshTokenCacheKey("login.microsoftonline.com", "CLIENT_ID_NOT_USED", "uid.utid", "1"));
        }

        [TestMethod]
        public void MsalIdTokenCacheKey()
        {
            var key = new MsalIdTokenCacheKey("login.microsoftonline.com", "contoso.com", "uid.utid", "clientid");

            Assert.AreEqual("uid.utid-login.microsoftonline.com-idtoken-clientid-contoso.com-", key.ToString());

            Assert.AreEqual("uid.utid-login.microsoftonline.com", key.iOSAccount);
            Assert.AreEqual("idtoken-clientid-contoso.com-", key.iOSService);
            Assert.AreEqual("idtoken-clientid-contoso.com", key.iOSGeneric);
            Assert.AreEqual((int)MsalCacheKeys.iOSCredentialAttrType.IdToken, key.iOSType);

            Assert.AreEqual(
                key,
                new MsalIdTokenCacheKey("login.microsoftonline.com", "contoso.com", "uid.utid", "clientid"));
        }

        [TestMethod]
        public void MsalAccountCacheKey()
        {
            var key = new MsalAccountCacheKey(
                "login.microsoftonline.com",
                "contoso.com",
                "uid.utid",
                "localId",
                "AAD");

            Assert.AreEqual("uid.utid-login.microsoftonline.com-contoso.com", key.ToString());

            Assert.AreEqual("uid.utid-login.microsoftonline.com", key.iOSAccount);
            Assert.AreEqual("contoso.com", key.iOSService);
            Assert.AreEqual("localid", key.iOSGeneric);
            Assert.AreEqual(MsalCacheKeys.iOSAuthorityTypeToAttrType["AAD"], key.iOSType);

            Assert.AreEqual(key, new MsalAccountCacheKey(
                "login.microsoftonline.com",
                "contoso.com",
                "uid.utid",
                "localId",
                "AAD"));
        }

        [TestMethod]
        public void MsalAppMetadataCacheKey()
        {
            var key = new MsalAppMetadataCacheKey("clientid", "login.microsoftonline.com");

            Assert.AreEqual("appmetadata-clientid", key.iOSService);
            Assert.AreEqual("login.microsoftonline.com", key.iOSAccount);
            Assert.AreEqual("1", key.iOSGeneric);
            Assert.AreEqual((int)MsalCacheKeys.iOSCredentialAttrType.AppMetadata, key.iOSType);
        }
    }
}
