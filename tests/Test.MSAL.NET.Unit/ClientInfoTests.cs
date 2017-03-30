using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.OAuth2;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.MSAL.NET.Unit
{
    [TestClass]
    public class ClientInfoTests
    {
        [TestMethod]
        [TestCategory("ClientInfoTests")]
        public void ParseTest()
        {
            ClientInfo clientInfo = ClientInfo.Parse("eyJ1aWQiOiJteS1VSUQiLCJ1dGlkIjoibXktVVRJRCJ9");
            Assert.IsNotNull(clientInfo);
            Assert.AreEqual(TestConstants.Uid, clientInfo.UniqueIdentifier);
            Assert.AreEqual(TestConstants.Utid, clientInfo.UniqueTenantIdentifier);
        }
    }
}
