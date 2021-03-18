using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Desktop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.Win8
{
    [TestClass]
    public class BrokerOnWin8Tests
    {
        [TestMethod]
        public void WamOnWin8()
        {
            var pcaBuilder = PublicClientApplicationBuilder
               .Create("d3adb33f-c0de-ed0c-c0de-deadb33fc0d3")
               .WithExperimentalFeatures()
               .WithWindowsBroker();


            Assert.IsFalse(pcaBuilder.IsBrokerAvailable());
        }
    }
}
