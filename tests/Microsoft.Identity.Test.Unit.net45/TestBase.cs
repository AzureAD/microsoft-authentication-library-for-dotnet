using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit
{
    [TestClass]
    public class TestBase
    {
        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context)
        {
            EnableFileTracingOnEnvVar();
            Trace.WriteLine("Test run started");
        }

        [AssemblyCleanup()]
        public static void AssemblyCleanup()
        {
            Trace.WriteLine("Test run finished");
            Trace.Flush();
        }

        [TestInitialize]
        public virtual void TestInitialize()
        {
            Trace.WriteLine("Test started " + TestContext.TestName);
            TestCommon.ResetInternalStaticCaches();
        }

        [TestCleanup]
        public virtual void TestCleanup()
        {
            Trace.WriteLine("Test finished " + TestContext.TestName);
        }

        public TestContext TestContext { get; set; }

        internal MockHttpAndServiceBundle CreateTestHarness(
            TelemetryCallback telemetryCallback = null,
            LogCallback logCallback = null,
            bool isExtendedTokenLifetimeEnabled = false)
        {
            return new MockHttpAndServiceBundle(
                telemetryCallback,
                logCallback,
                isExtendedTokenLifetimeEnabled,
                testContext: TestContext);
        }


        private static void EnableFileTracingOnEnvVar()
        {
            string traceFile = Environment.GetEnvironmentVariable("MsalTracePath");

            if (!String.IsNullOrEmpty(traceFile))
            {
                Trace.Listeners.Add(new TextWriterTraceListener(traceFile, "testListener"));
            }
        }
    }
}
