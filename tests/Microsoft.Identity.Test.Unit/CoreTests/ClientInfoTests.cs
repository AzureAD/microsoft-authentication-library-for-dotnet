// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Test.Unit.CoreTests
{
    [TestClass]
    public class ClientInfoTests
    {
        [TestMethod]
        public void ParseTest()
        {
            ClientInfo clientInfo = ClientInfo.CreateFromJson("eyJ1aWQiOiJteS11aWQiLCJ1dGlkIjoibXktdXRpZCJ9");
            Assert.IsNotNull(clientInfo);
            Assert.AreEqual(TestConstants.Uid, clientInfo.UniqueObjectIdentifier);
            Assert.AreEqual(TestConstants.Utid, clientInfo.UniqueTenantIdentifier);
        }
    }
}
