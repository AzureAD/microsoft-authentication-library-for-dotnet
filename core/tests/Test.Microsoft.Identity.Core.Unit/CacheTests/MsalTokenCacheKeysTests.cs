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

using Microsoft.Identity.Core.Cache;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Test.Microsoft.Identity.Core.Unit.CacheTests
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

            AssertException.Throws<ArgumentNullException>(() => new MsalAccountCacheKey("", "tid", "uid"));
            AssertException.Throws<ArgumentNullException>(() => new MsalAccountCacheKey(null, "tid", "uid"));
        }

        [TestMethod]
        public void MsalRefreshTokenCacheKey()
        {
            MsalRefreshTokenCacheKey key = new MsalRefreshTokenCacheKey("env", "cid", "uid");

            Assert.AreEqual("uid-env-refreshtoken-cid-", key.ToString());

            Assert.AreEqual("uid-env", key.GetiOSAccountKey());
            Assert.AreEqual("refreshtoken-cid--", key.GetiOSServiceKey()); // not a bug?
            Assert.AreEqual("refreshtoken-cid-", key.GetiOSGenericKey());
        }

        [TestMethod]
        public void MsalAccessTokenCacheKey()
        {
            MsalAccessTokenCacheKey key = new MsalAccessTokenCacheKey("env", "tid", "uid", "cid", "scopes");

            Assert.AreEqual("uid-env-accesstoken-cid-tid-scopes", key.ToString());

            Assert.AreEqual("uid-env", key.GetiOSAccountKey());
            Assert.AreEqual("accesstoken-cid-tid-scopes-", key.GetiOSServiceKey()); 
            Assert.AreEqual("accesstoken-cid-tid", key.GetiOSGenericKey());
        }

        [TestMethod]
        public void MsalIdTokenCacheKey()
        {
            MsalIdTokenCacheKey key = new MsalIdTokenCacheKey("env", "tid", "uid", "cid");

            Assert.AreEqual("uid-env-idtoken-cid-tid-", key.ToString());

            Assert.AreEqual("uid-env", key.GetiOSAccountKey());
            Assert.AreEqual("idtoken-cid-tid-", key.GetiOSServiceKey());
            Assert.AreEqual("idtoken-cid-tid", key.GetiOSGenericKey());
        }

        [TestMethod]
        public void MsalAccountCacheKey()
        {
            MsalAccountCacheKey key = new MsalAccountCacheKey("env", "tid", "uid");

            Assert.AreEqual("uid-env-tid", key.ToString());

            Assert.AreEqual("uid-env", key.GetiOSAccountKey());
            Assert.AreEqual("tid", key.GetiOSServiceKey());
        }
    }
}
