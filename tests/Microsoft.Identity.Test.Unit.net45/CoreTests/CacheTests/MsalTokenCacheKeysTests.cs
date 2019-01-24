//----------------------------------------------------------------------
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
//------------------------------------------------------------------------------

using System;
using System.Linq;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CoreTests.CacheTests
{
    [TestClass]
    public class MsalTokenCacheKeysTests
    {
        [TestMethod]
        public void ArgNull()
        {
            AssertException.Throws<ArgumentNullException>(() => new MsalRefreshTokenCacheKey("", "clientId", "uid"));
            AssertException.Throws<ArgumentNullException>(() => new MsalRefreshTokenCacheKey(null, "clientId", "uid"));
            AssertException.Throws<ArgumentNullException>(() => new MsalRefreshTokenCacheKey(null, "clientId", "uid"));
            AssertException.Throws<ArgumentNullException>(() => new MsalRefreshTokenCacheKey("env", null, "uid"));

            AssertException.Throws<ArgumentNullException>(() => new MsalIdTokenCacheKey("", "tid", "uid", "cid"));
            AssertException.Throws<ArgumentNullException>(() => new MsalIdTokenCacheKey(null, "tid", "uid", "cid"));
            AssertException.Throws<ArgumentNullException>(() => new MsalIdTokenCacheKey("env", "tid", "uid", ""));
            AssertException.Throws<ArgumentNullException>(() => new MsalIdTokenCacheKey("env", "tid", "uid", null));

            AssertException.Throws<ArgumentNullException>(() => new MsalAccessTokenCacheKey("", "tid", "uid", "cid", "scopes"));
            AssertException.Throws<ArgumentNullException>(() => new MsalAccessTokenCacheKey(null, "tid", "uid", "cid", "scopes"));
            AssertException.Throws<ArgumentNullException>(() => new MsalAccessTokenCacheKey("env", "tid", "uid", "", "scopes"));
            AssertException.Throws<ArgumentNullException>(() => new MsalAccessTokenCacheKey("env", "tid", "uid", null, "scopes"));

            AssertException.Throws<ArgumentNullException>(() => new MsalAccountCacheKey("", "tid", "uid", "localid"));
            AssertException.Throws<ArgumentNullException>(() => new MsalAccountCacheKey(null, "tid", "uid", "localid"));
        }

        [TestMethod]
        public void MsalAccessTokenCacheKey()
        {
            MsalAccessTokenCacheKey key = new MsalAccessTokenCacheKey("login.microsoftonline.com", "contoso.com", "uid.utid", "clientid", "user.read user.write");

            Assert.AreEqual("uid.utid-login.microsoftonline.com-accesstoken-clientid-contoso.com-user.read user.write", key.ToString());

            Assert.AreEqual("uid.utid-login.microsoftonline.com", key.GetiOSAccountKey());
            Assert.AreEqual("accesstoken-clientid-contoso.com-user.read user.write", key.GetiOSServiceKey());
            Assert.AreEqual("accesstoken-clientid-contoso.com", key.GetiOSGenericKey());
            var serviceBundle = TestCommon.CreateDefaultServiceBundle();
            Assert.AreEqual("uid.utid-m7wizgxzfro0k4ytgwbclbecpmuf5trhsuba0vptum8=-accesstoken-clientid-contoso.com-n5wvhdusof/wfsjgk1muxrk89nwfynymsl4qefkynbu=", key.GetUWPFixedSizeKey(serviceBundle.PlatformProxy.CryptographyManager));
        }

        [TestMethod]
        public void MsalRefreshTokenCacheKey()
        {
            MsalRefreshTokenCacheKey key = new MsalRefreshTokenCacheKey("login.microsoftonline.com", "clientid", "uid.utid");

            Assert.AreEqual("uid.utid-login.microsoftonline.com-refreshtoken-clientid--", key.ToString());

            Assert.AreEqual("uid.utid-login.microsoftonline.com", key.GetiOSAccountKey());
            Assert.AreEqual("refreshtoken-clientid--", key.GetiOSServiceKey());
            Assert.AreEqual("refreshtoken-clientid-", key.GetiOSGenericKey());
        }

        [TestMethod]
        public void MsalAccessTokenCacheKey_IsDifferentWhenEnvAndScopesAreDifferent()
        {
            MsalAccessTokenCacheKey key1 = new MsalAccessTokenCacheKey("env", "tid", "uid", "cid", "scope1 scope2");
            MsalAccessTokenCacheKey key2 = new MsalAccessTokenCacheKey("env", "tid", "uid", "cid", 
                string.Join(" ", Enumerable.Range(1, 100).Select(i => "scope" + i)));

            var serviceBundle = TestCommon.CreateDefaultServiceBundle();
            var crypto = serviceBundle.PlatformProxy.CryptographyManager;

            Assert.AreNotEqual(key1.GetUWPFixedSizeKey(crypto), key2.GetUWPFixedSizeKey(crypto));
            Assert.IsTrue(key2.GetUWPFixedSizeKey(crypto).Length < 255);
            Assert.IsTrue(key1.GetUWPFixedSizeKey(crypto).Length < 255);

        }

        [TestMethod]
        public void MsalIdTokenCacheKey()
        {
            MsalIdTokenCacheKey key = new MsalIdTokenCacheKey("login.microsoftonline.com", "contoso.com", "uid.utid", "clientid");

            Assert.AreEqual("uid.utid-login.microsoftonline.com-idtoken-clientid-contoso.com-", key.ToString());

            Assert.AreEqual("uid.utid-login.microsoftonline.com", key.GetiOSAccountKey());
            Assert.AreEqual("idtoken-clientid-contoso.com-", key.GetiOSServiceKey());
            Assert.AreEqual("idtoken-clientid-contoso.com", key.GetiOSGenericKey());
        }

        [TestMethod]
        public void MsalAccountCacheKey()
        {
            MsalAccountCacheKey key = new MsalAccountCacheKey("login.microsoftonline.com", "contoso.com", "uid.utid", "localId");

            Assert.AreEqual("uid.utid-login.microsoftonline.com-contoso.com", key.ToString());

            Assert.AreEqual("uid.utid-login.microsoftonline.com", key.GetiOSAccountKey());
            Assert.AreEqual("contoso.com", key.GetiOSServiceKey());
            Assert.AreEqual("localid", key.GetiOSGenericKey());
        }
    }
}
