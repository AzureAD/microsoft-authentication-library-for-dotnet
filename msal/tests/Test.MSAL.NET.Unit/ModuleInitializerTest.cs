using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.MSAL.NET.Unit
{
    [TestClass]
    public class ModuleInitializerTest
    {
        [TestMethod]
        public void InitializesExceptionsAndLogs()
        {
            // Act
            ModuleInitializer.EnsureModuleInitialized();

            // Assert
            MsalExceptionService factory = CoreExceptionService.Instance as MsalExceptionService;
            MsalLogger logger = CoreLoggerBase.Default as MsalLogger;
            Assert.IsNotNull(factory);
            Assert.IsNotNull(logger);

            // Act
            ModuleInitializer.EnsureModuleInitialized();

            // Assert
            Assert.AreEqual(factory, CoreExceptionService.Instance, "Initialization should have happened only once");
            Assert.AreEqual(logger, CoreLoggerBase.Default, "Initialization should have happened only once");
        }
    }
}
