// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Test.Unit.CoreTests
{
    public class CoreHelperTests
    {
        [TestMethod]
        public void UrlEncodeDecodeTest()
        {
            ClientInfo clientInfo = ClientInfo.CreateFromJson("eyJ1aWQiOiJteS11aWQiLCJ1dGlkIjoibXktdXRpZCJ9");
            Assert.IsNotNull(clientInfo);
            Assert.AreEqual(TestConstants.Uid, clientInfo.UniqueObjectIdentifier);
            Assert.AreEqual(TestConstants.Utid, clientInfo.UniqueTenantIdentifier);

            string originalUrl = "https://login.microsoftonline.com/organizations/oauth2/v2.0/authorize?scope=openid%20profile%20offline_access&response_type=code";
            string urlWithPlus = "https://login.microsoftonline.com/organizations/oauth2/v2.0/authorize?scope=openid+profile+offline_access&response_type=code";

            string encodedUrl = CoreHelpers.UrlEncode(originalUrl);
            Assert.AreEqual(encodedUrl, originalUrl);
            string decodedUIrl = CoreHelpers.UrlDecode(urlWithPlus);
            Assert.AreEqual(decodedUIrl, originalUrl);
        }
    }
}
