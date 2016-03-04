using System;
using Microsoft.Identity.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.MSAL.NET.Unit
{
    [TestClass]
    public class ConfidentialClientApplicationTests
    {
        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public void ConstructorsTest()
        {
            ConfidentialClientApplication app = new ConfidentialClientApplication(TestConstants.DefaultClientId,
                TestConstants.DefaultRedirectUri, new ClientCredential("secret"));
            Assert.IsNotNull(app);
        }
    }
}
