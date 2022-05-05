// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.OAuth2.Throttling;
using Microsoft.Identity.Client.Region;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Unit.Throttling;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.Identity.Client.TelemetryCore.Internal.Events.ApiEvent;

namespace Microsoft.Identity.Test.Unit.TelemetryTests
{
    [TestClass]
    public class MultiThreadRegionalTelemetryTests : BaseRegionTelemetryTests
    {
        [TestMethod]
        public void TelemetryAcceptanceTest_MultiThreads()
        {
            _isSingleThread = false;
            Environment.SetEnvironmentVariable(TestConstants.RegionName, TestConstants.Region);

            int iCountDiscovery = 0;

            var result = Parallel.For(0, 5, async (i) =>
            {
                try
                {
                    var result = await RunAcquireTokenForClientAsync(AcquireTokenForClientOutcome.Success, logCallback: DelayLogCallback).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Assert.Fail(ex.Message);
                }
            });

            void DelayLogCallback(LogLevel logLevel, string message, bool hasPii)
            {
                if (message.Contains("[Region discovery]"))
                {
                    Thread.Sleep(1 * 1000);// sleep 1 sec
                }

                if (message.Contains("[Region discovery] Region found in environment variable:"))
                {
                    iCountDiscovery++;
                }
            }

            Assert.AreEqual(1, iCountDiscovery);
            Assert.IsTrue(result.IsCompleted);
        }
    }
}
