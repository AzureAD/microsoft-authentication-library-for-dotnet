using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class ObjectsAndInterfaces
    {
        [TestMethod]
        [Description(
            "This test checks that we don't accidently expose a public property or method on" +
            " an object and forget to expose it on the associated interface.")]
        public void InterfaceExposesPublicSurfaceOfObjects()
        {
            CoreAssert.InterfaceExposesObject(
                typeof(ConfidentialClientApplication),
                typeof(IConfidentialClientApplication));

            CoreAssert.InterfaceExposesObject(
                typeof(PublicClientApplication),
                typeof(IPublicClientApplication));

            CoreAssert.InterfaceExposesObject(
               typeof(ClientApplicationBase),
               typeof(IClientApplicationBase));

            CoreAssert.InterfaceExposesObject(
                 typeof(Account),
                 typeof(IAccount),
                 new[] { "System.String ToString()" }); // ToString overriden as convenience 

            CoreAssert.InterfaceExposesObject(
               typeof(TokenCache),
               typeof(ITokenCache)); 
        }
    }
}
