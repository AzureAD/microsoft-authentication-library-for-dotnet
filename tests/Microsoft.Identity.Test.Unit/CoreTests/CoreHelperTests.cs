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
            Assert.AreEqual(CoreHelpers.UrlEncode("https://login.microsoftonline.com/organizations/oauth2/v2.0/authorize"), "https%3A%2F%2Flogin.microsoftonline.com%2Forganizations%2Foauth2%2Fv2.0%2Fauthorize");
            Assert.AreEqual(CoreHelpers.UrlEncode("https://management.core.windows.net//.default openid profile offline_access"), "https%3A%2F%2Fmanagement.core.windows.net%2F%2F.default%20openid%20profile%20offline_access");

            Assert.AreEqual(CoreHelpers.UrlDecode("https%3A%2F%2Flogin.microsoftonline.com%2Forganizations%2Foauth2%2Fv2.0%2Fauthorize"), "https://login.microsoftonline.com/organizations/oauth2/v2.0/authorize");
            Assert.AreEqual(CoreHelpers.UrlDecode("https%3A%2F%2Fmanagement.core.windows.net%2F%2F.default%20openid%20profile%20offline_access"), "https://management.core.windows.net//.default openid profile offline_access");
            Assert.AreEqual(CoreHelpers.UrlDecode("https%3A%2F%2Fmanagement.core.windows.net%2F%2F.default+openid+profile+offline_access"), "https://management.core.windows.net//.default openid profile offline_access");
        }
    }
}
