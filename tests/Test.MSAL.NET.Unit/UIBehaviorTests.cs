using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.MSAL.NET.Unit
{
    [TestClass]
    public class UIBehaviorTests
    {

        [TestMethod()]
        [TestCategory("UIBehaviorTests")]
        public void EqualsTestError()
        {
            UIBehavior ub1 = UIBehavior.Never;
            UIBehavior ub2 = UIBehavior.ForceLogin;

            Assert.AreNotEqual(ub1, ub2);
            Assert.AreEqual(ub1, UIBehavior.Never);
        }
    }

}