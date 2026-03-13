// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Test.Unit.CoreTests
{
    [TestClass]
    public class CoreHelperTests
    {
        [TestMethod]
        public void UrlEncodeDecodeTest()
        {
            // url without blank can be converted correctly.
            Assert.AreEqual("https%3A%2F%2Flogin.microsoftonline.com%2Forganizations%2Foauth2%2Fv2.0%2Fauthorize", CoreHelpers.UrlEncode("https://login.microsoftonline.com/organizations/oauth2/v2.0/authorize"));
            // url with blank can be converted correctly. " " needs to be replaced by "%20"
            Assert.AreEqual("https%3A%2F%2Fmanagement.core.windows.net%2F%2F.default%20openid%20profile%20offline_access", CoreHelpers.UrlEncode("https://management.core.windows.net//.default openid profile offline_access"));

            // Encoded url should be decoded correctly.
            Assert.AreEqual("https://login.microsoftonline.com/organizations/oauth2/v2.0/authorize", CoreHelpers.UrlDecode("https%3A%2F%2Flogin.microsoftonline.com%2Forganizations%2Foauth2%2Fv2.0%2Fauthorize"));
            // Encoded url with blank should be decoded correctly.
            Assert.AreEqual("https://management.core.windows.net//.default openid profile offline_access", CoreHelpers.UrlDecode("https%3A%2F%2Fmanagement.core.windows.net%2F%2F.default%20openid%20profile%20offline_access"));
            // Encoded url with "+" should be decoded correctly.
            Assert.AreEqual("https://management.core.windows.net//.default openid profile offline_access", CoreHelpers.UrlDecode("https%3A%2F%2Fmanagement.core.windows.net%2F%2F.default+openid+profile+offline_access"));

            // Test special OAuth characters (query string scenarios)
            Assert.AreEqual("redirect_uri%3Dhttps%3A%2F%2Fexample.com%2Fcallback%3Fcode%3D1234", CoreHelpers.UrlEncode("redirect_uri=https://example.com/callback?code=1234"));
            Assert.AreEqual("redirect_uri=https://example.com/callback?code=1234", CoreHelpers.UrlDecode("redirect_uri%3Dhttps%3A%2F%2Fexample.com%2Fcallback%3Fcode%3D1234"));
        }
    }
}
