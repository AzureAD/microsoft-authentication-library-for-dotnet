// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CoreTests
{
    [TestClass]
    public class ClientInfoTests
    {
        [TestMethod]
        [TestCategory("ClientInfoTests")]
        public void ParseTest()
        {
            ClientInfo clientInfo = ClientInfo.CreateFromJson("eyJ1aWQiOiJteS11aWQiLCJ1dGlkIjoibXktdXRpZCJ9");
            Assert.IsNotNull(clientInfo);
            Assert.AreEqual(MsalTestConstants.Uid, clientInfo.UniqueObjectIdentifier);
            Assert.AreEqual(MsalTestConstants.Utid, clientInfo.UniqueTenantIdentifier);
        }
    }
}
