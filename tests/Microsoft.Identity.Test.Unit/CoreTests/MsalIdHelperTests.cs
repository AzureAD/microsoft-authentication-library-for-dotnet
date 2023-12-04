﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CoreTests
{
    [TestClass]
    public class MsalIdHelperTests
    {
        [TestMethod]        
        public void XClientOSTest()
        {
            // Act
            IDictionary<string, string> parameters = MsalIdHelper.GetMsalIdParameters(null);

            // Assert
            Assert.IsTrue(parameters.ContainsKey("x-client-sku"));
            Assert.IsTrue(parameters.ContainsKey("x-client-os"));
            Assert.IsTrue(parameters.ContainsKey("x-client-Ver"));

            string os = parameters["x-client-os"];

#if NET48_OR_GREATER
            Assert.AreEqual(AbstractPlatformProxy.WindowsOS, os);
#else
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.AreEqual(AbstractPlatformProxy.WindowsOS, os);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Assert.AreEqual(AbstractPlatformProxy.MacOs, os);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Assert.AreEqual(AbstractPlatformProxy.LinuxOS, os);
            }
            else
            {
                Assert.Fail("Unknown OS");
            }
#endif
        }
    }
}
